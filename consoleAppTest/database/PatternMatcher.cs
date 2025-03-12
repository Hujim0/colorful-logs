using consoleAppTest.structs;

namespace consoleAppTest.database
{
    public class PatternMatcher
    {
        private List<Pattern> _processingOrder;
        private Dictionary<Guid, string> _cachedRegexes;


        public PatternMatcher(List<Pattern> patterns)
        {
            var (CompiledRegexes, SortedPatterns) = PatternCompiler.CompilePatterns(patterns);

            _cachedRegexes = CompiledRegexes;
            _processingOrder = SortedPatterns;
        }
    }
}