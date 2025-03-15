using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using consoleAppTest.structs;

namespace consoleAppTest.database
{
    public class PatternMatcher
    {
        private List<Pattern> _processingOrder;
        private Dictionary<Guid, Regex> _compiledRegexes;

        public PatternMatcher(List<Pattern> patterns)
        {
            var (compiledRegexes, sortedPatterns) = PatternCompiler.CompilePatterns(patterns);
            _compiledRegexes = compiledRegexes.ToDictionary(
                kvp => kvp.Key,
                kvp => new Regex(kvp.Value, RegexOptions.Compiled));
            _processingOrder = sortedPatterns;

        }

        public List<IndexedValue> ProcessLine(IndexedLine line)
        {
            var indexedValues = new List<IndexedValue>();
            ProcessTextSegment(line, line.LineText, 0, _processingOrder, indexedValues);
            return indexedValues;
        }

        private void ProcessTextSegment(
            IndexedLine line,
            string textSegment,
            int baseIndex,
            List<Pattern> patternsToProcess,
            List<IndexedValue> indexedValues)
        {
            // Try all patterns in processing order
            foreach (var pattern in patternsToProcess)
            {
                if (!_compiledRegexes.TryGetValue(pattern.Id, out var regex)) continue;

                var matches = regex.Matches(textSegment);
                foreach (Match match in matches)
                {
                    if (!match.Success) continue;

                    // Process text before this match
                    if (match.Index > 0)
                    {
                        ProcessTextSegment(
                            line,
                            textSegment[..match.Index],
                            baseIndex,
                            patternsToProcess,
                            indexedValues
                        );
                    }

                    // Process this match
                    ProcessMatch(line, baseIndex, pattern, match, indexedValues);

                    // Process text after this match
                    if (match.Index + match.Length < textSegment.Length)
                    {
                        ProcessTextSegment(
                            line,
                            textSegment[(match.Index + match.Length)..],
                            baseIndex + match.Index + match.Length,
                            patternsToProcess,
                            indexedValues
                        );
                    }

                    // Exit after first match to avoid overlapping processing
                    return;
                }
            }
        }

        private void ProcessMatch(
            IndexedLine line,
            int baseIndex,
            Pattern pattern,
            Match match,
            List<IndexedValue> indexedValues)
        {
            int startInLine = baseIndex + match.Index;

            var indexedValue = new IndexedValue
            {
                Id = Guid.NewGuid(),
                Value = match.Value,
                Pattern = pattern,
                TagInstances = []
            };

            var tag = new TagInstance
            {
                Id = Guid.NewGuid(),
                IndexedValue = indexedValue,
                IndexedLine = line,
                StartIndex = startInLine,
                Length = match.Length
            };

            indexedValue.TagInstances.Add(tag);
            line.TagInstances.Add(tag);
            indexedValues.Add(indexedValue);

            // Process components within the matched text
            if (pattern.Components.Count > 0)
            {
                var childPatterns = pattern.Components
                    .Select(c => c.ChildPattern)
                    .ToList();

                ProcessTextSegment(
                    line,
                    match.Value,
                    startInLine,
                    childPatterns,
                    indexedValues
                );
            }
        }
    }
}