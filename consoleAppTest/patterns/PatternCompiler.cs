using System.Text.RegularExpressions;
using consoleAppTest.patterns;
using System.Collections.Generic;
using System.Linq;
using System;

namespace consoleAppTest.structs
{
    public class PatternCompiler : IPatternCompiler
    {
        public (Dictionary<Guid, string> ResolvedRegexes, List<Pattern> SortedPatterns) CompilePatterns(List<Pattern> patterns)
        {
            var resolvedRegexes = new Dictionary<Guid, string>();
            var placeholderMappings = new Dictionary<Guid, Dictionary<string, Pattern>>();

            // Collect all patterns referenced by the provided patterns, including children recursively
            var allPatterns = new HashSet<Pattern>(patterns);
            var queue = new Queue<Pattern>(patterns);
            while (queue.Count > 0)
            {
                var currentPattern = queue.Dequeue();
                foreach (var component in currentPattern.Components)
                {
                    var childPattern = component.ChildPattern;
                    if (allPatterns.Add(childPattern))
                    {
                        queue.Enqueue(childPattern);
                    }
                }
            }

            // Preprocess each pattern to map PlaceholderName to ChildPattern
            foreach (var pattern in allPatterns)
            {
                var componentNameToPattern = pattern.Components.ToDictionary(c => c.PlaceholderName, c => c.ChildPattern);
                placeholderMappings[pattern.Id] = componentNameToPattern;
            }

            // Compile each pattern in allPatterns
            foreach (var pattern in allPatterns)
            {
                if (!resolvedRegexes.ContainsKey(pattern.Id))
                {
                    CompilePattern(pattern, resolvedRegexes, placeholderMappings, new HashSet<Guid>());
                }
            }

            // Sort the provided patterns by regex length descending
            var sortedPatterns = patterns
                .OrderByDescending(pattern => resolvedRegexes[pattern.Id].Length)
                .ToList();

            return (resolvedRegexes, sortedPatterns);
        }

        public void CompilePattern(
            Pattern pattern,
            Dictionary<Guid, string> compiledRegexes,
            Dictionary<Guid, Dictionary<string, Pattern>> placeholderMappings,
            HashSet<Guid> visited)
        {
            if (compiledRegexes.ContainsKey(pattern.Id))
            {
                return;
            }

            if (visited.Contains(pattern.Id))
            {
                throw new InvalidOperationException($"Circular reference detected in pattern {pattern.Id}");
            }

            visited.Add(pattern.Id);

            try
            {
                var components = placeholderMappings[pattern.Id];
                var syntax = pattern.SyntaxString;

                if (components.Count > 0)
                {
                    var placeholders = components.Keys.OrderByDescending(name => name.Length).ToList();

                    foreach (var placeholderName in placeholders)
                    {
                        var childPattern = components[placeholderName];
                        // Ensure child pattern is compiled
                        if (!compiledRegexes.ContainsKey(childPattern.Id))
                        {
                            CompilePattern(childPattern, compiledRegexes, placeholderMappings, visited);
                        }

                        var childRegex = compiledRegexes[childPattern.Id];
                        syntax = Regex.Replace(syntax, $@"\${Regex.Escape(placeholderName)}", childRegex);
                    }
                }

                compiledRegexes[pattern.Id] = syntax;
            }
            finally
            {
                visited.Remove(pattern.Id);
            }
        }
    }
}