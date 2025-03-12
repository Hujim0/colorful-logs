using System.Text.RegularExpressions;

namespace consoleAppTest.structs
{
    public static class PatternCompiler
    {
        public static (Dictionary<Guid, string> CompiledRegexes, List<Pattern> SortedPatterns) CompilePatterns(List<Pattern> patterns)
        {
            var compiledRegexes = new Dictionary<Guid, string>();
            var placeholderMappings = new Dictionary<Guid, Dictionary<string, Pattern>>();

            // Preprocess each pattern to map PlaceholderName to ChildPattern
            foreach (var pattern in patterns)
            {
                var componentNameToPattern = pattern.Components.ToDictionary(c => c.PlaceholderName, c => c.ChildPattern);
                placeholderMappings[pattern.Id] = componentNameToPattern;
            }

            // Compile each pattern, handling dependencies and cycles
            foreach (var pattern in patterns)
            {
                if (!compiledRegexes.ContainsKey(pattern.Id))
                {
                    CompilePattern(pattern, compiledRegexes, placeholderMappings, new HashSet<Guid>());
                }
            }

            // Sort the patterns by regex length descending
            var sortedPatterns = patterns
                .OrderByDescending(pattern => compiledRegexes[pattern.Id].Length)
                .ToList();

            return (compiledRegexes, sortedPatterns);
        }

        private static void CompilePattern(
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