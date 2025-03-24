using consoleAppTest.structs;
using Xunit;

namespace consoleAppTest.Tests
{
    public class PatternCompilerTests
    {
        [Fact]
        public void CompilePatterns_BasicCompilation_NoPlaceholders()
        {
            // Arrange
            var pattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "patternName",
                SyntaxString = "^test$",
                Components = []
            };
            var patterns = new List<Pattern> { pattern };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert
            Assert.Equal("^test$", ResolvedRegexes[pattern.Id]);
            Assert.Single(SortedPatterns);
        }

        [Fact]
        public void CompilePatterns_PlaceholderReplacement_SingleLevel()
        {
            // Arrange
            var childPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "childPattern",
                SyntaxString = @"\d+",
                Components = []
            };
            var parentPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "parentPattern",
                SyntaxString = @"$child",
                Components = []
            };
            parentPattern.Components.Add(new PatternComponent
            {
                PlaceholderName = "child",
                ChildPattern = childPattern,
                ParentPattern = parentPattern
            });
            var patterns = new List<Pattern> { parentPattern, childPattern };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert: Parent regex should have named group for "child"
            Assert.Equal(@"(?<child>\d+)", ResolvedRegexes[parentPattern.Id]);
        }

        [Fact]
        public void CompilePatterns_NestedPlaceholders_ReplacesAllLevels()
        {
            // Arrange
            var patternC = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "patternC",
                SyntaxString = "C",
                Components = []
            };

            var patternB = new Pattern
            {
                Id = Guid.NewGuid(),
                SyntaxString = "$C",
                PatternName = "patternB",
                Components = []
            };
            patternB.Components.Add(new PatternComponent
            {
                PlaceholderName = "C",
                ChildPattern = patternC,
                ParentPattern = patternB
            });

            var patternA = new Pattern
            {
                Id = Guid.NewGuid(),
                SyntaxString = "$B",
                PatternName = "patternA",
                Components = []
            };
            patternA.Components.Add(new PatternComponent
            {
                PlaceholderName = "B",
                ChildPattern = patternB,
                ParentPattern = patternA
            });
            var patterns = new List<Pattern> { patternA, patternB, patternC };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert: Nested named groups should be present
            Assert.Equal("(?<B>(?<C>C))", ResolvedRegexes[patternA.Id]);
        }

        [Fact]
        public void CompilePatterns_CircularReference_ThrowsException()
        {
            // Arrange
            var patternA = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "patternA",
                SyntaxString = "$B",
                Components = []
            };
            var patternB = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "patternB",
                SyntaxString = "$A",
                Components = []
            };
            patternA.Components.Add(new PatternComponent { PlaceholderName = "B", ChildPattern = patternB, ParentPattern = patternA });
            patternB.Components.Add(new PatternComponent { PlaceholderName = "A", ChildPattern = patternA, ParentPattern = patternB });
            var patterns = new List<Pattern> { patternA, patternB };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new PatternCompiler().CompilePatterns(patterns));
        }

        [Fact]
        public void CompilePatterns_SortOrder_ByRegexLengthDescending()
        {
            // Arrange
            var middlePattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "middle",
                SyntaxString = "aaaaa"
            };
            var longestPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "longest",
                SyntaxString = "aaaaaaaaaa"
            };
            var shortestPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "shortest",
                SyntaxString = "aaa"
            };
            var patterns = new List<Pattern> { middlePattern, longestPattern, shortestPattern };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert
            var expectedOrder = new List<Pattern> { longestPattern, middlePattern, shortestPattern };
            Assert.Equal(expectedOrder, SortedPatterns);
        }

        [Fact]
        public void CompilePatterns_ValidPlaceholderNameWithUnderscore_ReplacesCorrectly()
        {
            // Arrange
            var child = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "child",
                SyntaxString = "b",
                Components = []
            };
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "parent",
                SyntaxString = "$a_plus",
                Components = []
            };
            parent.Components.Add(new PatternComponent
            {
                PlaceholderName = "a_plus",
                ChildPattern = child,
                ParentPattern = parent
            });
            var patterns = new List<Pattern> { parent, child };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert: Named group with valid name "a_plus"
            Assert.Equal("(?<a_plus>b)", ResolvedRegexes[parent.Id]);
        }

        [Fact]
        public void CompilePatterns_PlaceholderWithoutComponent_LeavesAsIs()
        {
            // Arrange
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "parent",
                SyntaxString = "$unknown",
                Components = []
            };
            var patterns = new List<Pattern> { parent };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert
            Assert.Equal("$unknown", ResolvedRegexes[parent.Id]);
        }

        // New test to verify multiple placeholders in the same pattern
        [Fact]
        public void CompilePatterns_MultiplePlaceholders_GeneratesNamedGroups()
        {
            // Arrange
            var child1 = new Pattern { Id = Guid.NewGuid(), PatternName = "name", SyntaxString = "1" };
            var child2 = new Pattern { Id = Guid.NewGuid(), PatternName = "name", SyntaxString = "2" };
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "name",
                SyntaxString = "$a$b",
                Components = []
            };
            parent.Components = [
                    new() {ParentPattern = parent, PlaceholderName = "a", ChildPattern = child1 },
                    new() {ParentPattern = parent, PlaceholderName = "b", ChildPattern = child2 }
                ];

            var patterns = new List<Pattern> { parent, child1, child2 };

            // Act
            var (ResolvedRegexes, _) = new PatternCompiler().CompilePatterns(patterns);

            // Assert: Both placeholders replaced with named groups
            Assert.Equal("(?<a>1)(?<b>2)", ResolvedRegexes[parent.Id]);
        }

        [Fact]
        public void CompilePatterns_PlaceholderWithRegexSpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var child = new Pattern { Id = Guid.NewGuid(), PatternName = "name", SyntaxString = @"\d+" };
            var parent = new Pattern
            {
                PatternName = "name",
                Id = Guid.NewGuid(),
                SyntaxString = @"$user_input",
                Components = []
            };
            parent.Components = [new() { ParentPattern = parent, PlaceholderName = "user_input", ChildPattern = child }];
            var patterns = new List<Pattern> { parent, child };

            // Act
            var (ResolvedRegexes, _) = new PatternCompiler().CompilePatterns(patterns);

            // Assert: Valid placeholder name without special characters
            Assert.Equal(@"(?<user_input>\d+)", ResolvedRegexes[parent.Id]);
        }

        [Fact]
        public void CompilePatterns_PlaceholderOrder_LongestFirst()
        {
            // Arrange
            var childIp = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "childIp",
                SyntaxString = "a",
                Components = []
            };
            var childIpAddress = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "childIpAddress",
                SyntaxString = "b",
                Components = []
            };
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "parent",
                SyntaxString = "$ip$ipaddress",
                Components = []
            };
            parent.Components.Add(new PatternComponent { Id = Guid.NewGuid(), PlaceholderName = "ip", ChildPattern = childIp, ParentPattern = parent });
            parent.Components.Add(new PatternComponent { Id = Guid.NewGuid(), PlaceholderName = "ipaddress", ChildPattern = childIpAddress, ParentPattern = parent });
            var patterns = new List<Pattern> { parent, childIp, childIpAddress };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            Assert.Equal(SortedPatterns, [parent, childIp, childIpAddress]);
            Assert.Equal("(?<ip>a)(?<ipaddress>b)", ResolvedRegexes[parent.Id]);
        }
    }

}