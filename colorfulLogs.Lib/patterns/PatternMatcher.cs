using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using colorfulLogs.structs;

namespace colorfulLogs.patterns
{
    public class PatternMatcher
    {
        private List<Pattern> _processingOrder;
        private ConcurrentDictionary<Guid, Regex> _compiledRegexes;

        public PatternMatcher(List<Pattern> patterns, IPatternCompiler patternCompiler)
        {
            var (compiledRegexes, sortedPatterns) = patternCompiler.CompilePatterns(patterns);
            _compiledRegexes = new ConcurrentDictionary<Guid, Regex>(compiledRegexes.ToDictionary(
                kvp => kvp.Key,
                kvp => new Regex(kvp.Value, RegexOptions.Compiled)));
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

                    return; // Exit after first match
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

            // Process components using NAMED GROUPS instead of the entire match
            if (pattern.Components.Count > 0)
            {
                foreach (var component in pattern.Components)
                {
                    string placeholderName = component.PlaceholderName;
                    Group group = match.Groups[placeholderName];

                    if (group.Success)
                    {
                        int groupStartInLine = group.Index;
                        string groupValue = group.Value;

                        // Process only the child pattern on the captured group's value
                        ProcessTextSegment(
                            line,
                            groupValue,
                            groupStartInLine,
                            [component.ChildPattern],
                            indexedValues
                        );
                    }
                }
            }
        }
    }
}
