using consoleAppTest.structs;

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
                PatternName = "patternName",

                SyntaxString = @"\d+",
                Components = []
            };
            var parentPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "patternName",
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

            // Assert
            Assert.Equal(@"\d+", ResolvedRegexes[parentPattern.Id]);
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
                PatternName = "patternName",
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
                PatternName = "patternName",
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

            // Assert
            Assert.Equal("C", ResolvedRegexes[patternA.Id]);
        }

        [Fact]
        public void CompilePatterns_CircularReference_ThrowsException()
        {
            // Arrange
            var patternA = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
                SyntaxString = "$B",
                Components = []
            };
            var patternB = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
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
        public void CompilePatterns_PlaceholderOrder_LongestFirst()
        {
            // Arrange
            var childIp = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
                SyntaxString = "a",
                Components = []
            };
            var childIpAddress = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
                SyntaxString = "b",
                Components = []
            };
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
                SyntaxString = "$ip$ipaddress",
                Components = []
            };
            parent.Components.Add(new PatternComponent { PlaceholderName = "ip", ChildPattern = childIp, ParentPattern = parent });
            parent.Components.Add(new PatternComponent { PlaceholderName = "ipaddress", ChildPattern = childIpAddress, ParentPattern = parent });
            var patterns = new List<Pattern> { parent, childIp, childIpAddress };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert
            Assert.Equal("ab", ResolvedRegexes[parent.Id]);
        }

        [Fact]
        public void CompilePatterns_PlaceholderWithSpecialCharacters_ReplacesCorrectly()
        {
            // Arrange
            var child = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
                SyntaxString = "b",
                Components = new List<PatternComponent>()
            };
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "",
                SyntaxString = "$a+",
                Components = new List<PatternComponent>()
            };
            parent.Components.Add(new PatternComponent { PlaceholderName = "a+", ChildPattern = child, ParentPattern = parent });
            var patterns = new List<Pattern> { parent, child };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert
            Assert.Equal("b", ResolvedRegexes[parent.Id]);
        }

        [Fact]
        public void CompilePatterns_PlaceholderWithoutComponent_LeavesAsIs()
        {
            // Arrange
            var parent = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "patternName",
                SyntaxString = "$unknown",
                Components = []
            };
            var patterns = new List<Pattern> { parent };

            // Act
            var (ResolvedRegexes, SortedPatterns) = new PatternCompiler().CompilePatterns(patterns);

            // Assert
            Assert.Equal("$unknown", ResolvedRegexes[parent.Id]);
        }
    }
}
