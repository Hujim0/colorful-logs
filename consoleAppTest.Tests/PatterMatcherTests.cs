using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using consoleAppTest.database;
using consoleAppTest.structs;

namespace consoleAppTest.Tests
{
    public class PatternMatcherTests
    {
        private static Pattern CreateAddressPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "IPv4 Address",
                SyntaxString = @"\b(?:\d{1,3}\.){3}\d{1,3}\b",
                Components = []
            };
        }

        private static Pattern CreatePortPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Port Number",
                SyntaxString = @"\b\d{1,5}\b",
                Components = []
            };
        }

        private static Pattern CreateAddressPortPattern(Pattern address, Pattern port)
        {
            var pattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Address with Port",
                SyntaxString = @"$address:$port",
                Components = [],
            };
            var addressComponent = new PatternComponent
            {
                ParentPattern = pattern,
                PlaceholderName = "address",
                ChildPattern = address
            };
            var portComponent = new PatternComponent
            {
                ParentPattern = pattern,
                PlaceholderName = "port",
                ChildPattern = port
            };
            pattern.Components.AddRange(addressComponent, portComponent);

            return pattern;
        }

        private static Pattern CreateResourcePattern() {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Resource Path",
                SyntaxString = @"\/([a-zA-z0-9]+\/?)*\b",
                Components = []
            };
        }

        private static Pattern CreateUrlPattern(Pattern address, Pattern port, Pattern resource)
        {
            var pattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "API Endpoint",
                SyntaxString = @"https://$address:$port$resource",
                Components = []
            };
            pattern.Components.AddRange(
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "address", ChildPattern = address },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "port", ChildPattern = port },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "resource", ChildPattern = resource });
            return pattern;
        }

        [Fact]
        public void ShouldMatchNestedAddressPortPattern()
        {
            // Arrange
            var address = CreateAddressPattern();
            var port = CreatePortPattern();
            var addressPort = CreateAddressPortPattern(address, port);
            var resource = CreateResourcePattern();

            var patterns = new List<Pattern> { address, port, addressPort, resource };
            var matcher = new PatternMatcher(patterns);

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
            Assert.Equal(line.LineText[computedStartIndex..(computedStartIndex+computedLength)], mainAddressPort.Value);
            Assert.Equal(line.LineText.Substring(computedStartIndex,computedLength), mainAddressPort.Value);

        }

        [Fact]
        public void ShouldMatchComplexUrlPattern()
        {
            // Arrange
            var address = CreateAddressPattern();
            var port = CreatePortPattern();
            var resource = CreateResourcePattern();

            var urlPattern = CreateUrlPattern(address, port, resource);
            var patterns = new List<Pattern> { address, port, resource, urlPattern };
            var matcher = new PatternMatcher(patterns);

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
            Assert.Equal("Request to https://10.0.0.1:8080".Length, resourceMatch.TagInstances.First().StartIndex);
            Assert.Equal("/api/v1/users".Length, resourceMatch.TagInstances.First().Length);
        }

        [Fact]
        public void ShouldHandleMultipleMatchesInSingleLine()
        {
            // Arrange
            var address = CreateAddressPattern();
            var port = CreatePortPattern();
            var addressPort = CreateAddressPortPattern(address, port);
            var patterns = new List<Pattern> { address, port, addressPort };
            var matcher = new PatternMatcher(patterns);

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
    }
}