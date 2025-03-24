using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using consoleAppTest.database;
using consoleAppTest.patterns;
using consoleAppTest.structs;

namespace consoleAppTest.Tests
{
    public class PatternMatcherTests
    {

        [Fact]
        public void ShouldMatchNestedAddressPortPattern()
        {
            // Arrange
            var address = DefaultPatterns.CreateAddressPattern();
            var port = DefaultPatterns.CreatePortPattern();
            var addressPort = DefaultPatterns.CreateAddressPortPattern(address, port);
            var resource = DefaultPatterns.CreateResourcePattern();

            var patterns = new List<Pattern> { address, port, addressPort, resource };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var startIndex = "Server at ".Length;
            var length = "192.168.1.1:8080".Length;

            var line = new IndexedLine
            {
                LineText = "Server at 192.168.1.1:8080",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var addressMatches = results.Where(iv => iv.Pattern.Id == address.Id).ToList();
            var portMatches = results.Where(iv => iv.Pattern.Id == port.Id).ToList();
            var addressPortMatches = results.Where(iv => iv.Pattern.Id == addressPort.Id).ToList();

            // Verify base components
            Assert.Single(addressMatches);
            Assert.Single(portMatches);
            Assert.Single(addressPortMatches);

            var mainAddressPort = addressPortMatches.First();
            Assert.Equal("192.168.1.1:8080", mainAddressPort.Value);

            int computedStartIndex = mainAddressPort.TagInstances.First().StartIndex;
            int computedLength = mainAddressPort.TagInstances.First().Length;

            Assert.Equal(startIndex, computedStartIndex);
            Assert.Equal("192.168.1.1:8080".Length, computedLength);

            Assert.Equal("Server at 192.168.1.1:8080", line.LineText);
            Assert.Equal(line.LineText[computedStartIndex..(computedStartIndex + computedLength)], mainAddressPort.Value);
            Assert.Equal(line.LineText.Substring(computedStartIndex, computedLength), mainAddressPort.Value);

        }

        [Fact]
        public void ShouldMatchComplexUrlPattern()
        {
            // Arrange
            var address = DefaultPatterns.CreateAddressPattern();
            var port = DefaultPatterns.CreatePortPattern();
            var resource = DefaultPatterns.CreateResourcePattern();

            var urlPattern = DefaultPatterns.CreateUrlWithIpAndPortPattern(address, port, resource);
            var patterns = new List<Pattern> { address, port, urlPattern };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var line = new IndexedLine
            {
                LineText = "Request to https://10.0.0.1:8080/api/v1/users",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var urlMatches = results.Where(iv => iv.Pattern.Id == urlPattern.Id).ToList();
            var resourceMatches = results.Where(iv => iv.Pattern.Id == resource.Id).ToList();

            // Verify full URL match
            Assert.Single(urlMatches);

            var urlMatch = urlMatches.First();
            Assert.Equal("https://10.0.0.1:8080/api/v1/users", urlMatch.Value);
            Assert.Equal("Request to ".Length, urlMatch.TagInstances.First().StartIndex);
            Assert.Equal("https://10.0.0.1:8080/api/v1/users".Length, urlMatch.TagInstances.First().Length);

            // Verify nested resources
            Assert.Single(resourceMatches);
            var resourceMatch = resourceMatches.First();
            Assert.Equal("/api/v1/users", resourceMatch.Value);
            Assert.Equal("/api/v1/users".Length, resourceMatch.TagInstances.First().Length);
            Assert.Equal("Request to https://10.0.0.1:8080".Length, resourceMatch.TagInstances.First().StartIndex);
        }

        [Fact]
        public void ShouldHandleMultipleMatchesInSingleLine()
        {
            // Arrange
            var address = DefaultPatterns.CreateAddressPattern();
            var port = DefaultPatterns.CreatePortPattern();
            var addressPort = DefaultPatterns.CreateAddressPortPattern(address, port);
            var patterns = new List<Pattern> { address, port, addressPort };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var line = new IndexedLine
            {
                LineText = "Connections: 192.168.0.1:80, 10.0.0.1:443",
                LineNumber = 1
            };

            int startFirstIp = "Connections: ".Length;
            int startSecondIp = "Connections: 192.168.0.1:80, ".Length;


            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var addressPortMatches = results.Where(iv => iv.Pattern.Id == addressPort.Id).ToList();
            Assert.Equal(2, addressPortMatches.Count);

            var firstMatch = addressPortMatches[0];
            Assert.Equal("192.168.0.1:80", firstMatch.Value);
            Assert.Equal("192.168.0.1:80".Length, firstMatch.TagInstances.First().Length);
            Assert.Equal(startFirstIp, firstMatch.TagInstances.First().StartIndex);

            var secondMatch = addressPortMatches[1];
            Assert.Equal("10.0.0.1:443", secondMatch.Value);
            Assert.Equal("10.0.0.1:443".Length, secondMatch.TagInstances.First().Length);
            Assert.Equal(startSecondIp, secondMatch.TagInstances.First().StartIndex);
        }

        [Fact]
        public void ShouldNotMatchNotAddedPatterns()
        {
            // Arrange
            var address = DefaultPatterns.CreateAddressPattern();
            var port = DefaultPatterns.CreatePortPattern();
            var resource = DefaultPatterns.CreateResourcePattern();

            var urlPattern = DefaultPatterns.CreateUrlWithIpAndPortPattern(address, port, resource);

            //removed port from it
            var patterns = new List<Pattern> { address, urlPattern };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var line = new IndexedLine
            {
                LineText = "Request #3422 to https://10.0.0.1:8080/api/v1/users",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var urlMatches = results.Where(iv => iv.Pattern.Id == urlPattern.Id).ToList();
            var resourceMatches = results.Where(iv => iv.Pattern.Id == resource.Id).ToList();
            var portMatches = results.Where(iv => iv.Pattern.Id == port.Id).ToList();


            // Verify full URL match
            Assert.Single(urlMatches);
            Assert.Single(resourceMatches);
            //should not match "#3422"
            Assert.Single(portMatches);
        }
        // New test for HTTP methods
        [Fact]
        public void ShouldMatchHttpMethods()
        {
            // Arrange
            var getPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP GET",
                SyntaxString = @"\bGET\b",
                Components = []
            };
            var postPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP POST",
                SyntaxString = @"\bPOST\b",
                Components = []
            };
            var headPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP HEAD",
                SyntaxString = @"\bHEAD\b",
                Components = []
            };
            var optionsPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP OPTIONS",
                SyntaxString = @"\bOPTIONS\b",
                Components = []
            };

            var httpMethodPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP Method",
                SyntaxString = "$http_get|$http_post|$http_head|$http_options",
                Components = [],
            };
            httpMethodPattern.Components =
            [
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_get", ChildPattern = getPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_post", ChildPattern = postPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_head", ChildPattern = headPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_options", ChildPattern = optionsPattern }
            ];

            var matcher = new PatternMatcher([httpMethodPattern], new PatternCompiler());
            var line = new IndexedLine
            {
                LineText = "GET /api/data HTTP/1.1",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var matches = results.Where(m => m.Pattern.Id == httpMethodPattern.Id).ToList();
            Assert.Single(matches);
            Assert.Equal("GET", matches[0].Value);
            Assert.Equal(0, matches[0].TagInstances[0].StartIndex);
            Assert.Equal(3, matches[0].TagInstances[0].Length);
        }

        // New test to ensure ports aren't matched standalone
        [Fact]
        public void ShouldNotMatchStandalonePort()
        {
            // Arrange
            var addressPattern = DefaultPatterns.CreateAddressPattern();
            var portPattern = DefaultPatterns.CreatePortPattern();
            var addressPortPattern = DefaultPatterns.CreateAddressPortPattern(addressPattern, portPattern);

            // Only include composite patterns, not standalone port
            var matcher = new PatternMatcher([addressPattern, addressPortPattern], new PatternCompiler());
            var line = new IndexedLine
            {
                LineText = "Invalid port: 8080",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var portMatches = results.Where(m => m.Pattern.Id == portPattern.Id).ToList();
            Assert.Empty(portMatches); // Port shouldn't match when not part of address:port

            // Verify address:port pattern doesn't match invalid format
            var addressPortMatches = results.Where(m => m.Pattern.Id == addressPortPattern.Id).ToList();
            Assert.Empty(addressPortMatches);
        }
    }
}

