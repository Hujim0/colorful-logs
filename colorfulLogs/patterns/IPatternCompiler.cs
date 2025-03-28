using colorfulLogs.structs;

namespace colorfulLogs.patterns
{
    public interface IPatternCompiler
    {
        (Dictionary<Guid, string> ResolvedRegexes, List<Pattern> SortedPatterns) CompilePatterns(List<Pattern> patterns);

        void CompilePattern(Pattern pattern, Dictionary<Guid, string> compiledRegexes, Dictionary<Guid, Dictionary<string, Pattern>> placeholderMappings, HashSet<Guid> visited);
    }
}
