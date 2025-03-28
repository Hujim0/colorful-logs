using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using colorfulLogs.database;
using colorfulLogs.patterns;
using colorfulLogs.structs;

namespace colorfulLogs.Tests
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

            Pattern.GenerateNewGuids(address);
            Pattern.GenerateNewGuids(port);
            Pattern.GenerateNewGuids(addressPort);
            Pattern.GenerateNewGuids(resource);

            var patterns = new List<Pattern> { address, port, addressPort, resource };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var startIndex = "Server at ".Length;
            var length = "192.168.1.1:8080".Length;

            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "Server at 192.168.1.1:8080",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var addressMatches = results.Where(iv => iv.Pattern.PatternName == address.PatternName).ToList();
            var portMatches = results.Where(iv => iv.Pattern.PatternName == port.PatternName).ToList();
            var addressPortMatches = results.Where(iv => iv.Pattern.PatternName == addressPort.PatternName).ToList();

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

            Pattern.GenerateNewGuids(address);
            Pattern.GenerateNewGuids(port);
            Pattern.GenerateNewGuids(resource);
            Pattern.GenerateNewGuids(urlPattern);

            var patterns = new List<Pattern> { address, port, urlPattern };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "Request to https://10.0.0.1:8080/api/v1/users",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var urlMatches = results.Where(iv => iv.Pattern.PatternName == urlPattern.PatternName).ToList();
            var resourceMatches = results.Where(iv => iv.Pattern.PatternName == resource.PatternName).ToList();

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

            Pattern.GenerateNewGuids(address);
            Pattern.GenerateNewGuids(port);
            Pattern.GenerateNewGuids(addressPort);

            var patterns = new List<Pattern> { address, port, addressPort };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "Connections: 192.168.0.1:80, 10.0.0.1:443",
                LineNumber = 1
            };

            int startFirstIp = "Connections: ".Length;
            int startSecondIp = "Connections: 192.168.0.1:80, ".Length;


            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var addressPortMatches = results.Where(iv => iv.Pattern.PatternName == addressPort.PatternName).ToList();
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

            Pattern.GenerateNewGuids(address);
            Pattern.GenerateNewGuids(port);
            Pattern.GenerateNewGuids(resource);
            Pattern.GenerateNewGuids(urlPattern);

            //removed port from it
            var patterns = new List<Pattern> { address, urlPattern };
            var matcher = new PatternMatcher(patterns, new PatternCompiler());

            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "Request #3422 to https://10.0.0.1:8080/api/v1/users",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var urlMatches = results.Where(iv => iv.Pattern.PatternName == urlPattern.PatternName).ToList();
            var resourceMatches = results.Where(iv => iv.Pattern.PatternName == resource.PatternName).ToList();
            var portMatches = results.Where(iv => iv.Pattern.PatternName == port.PatternName).ToList();


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
            var httpMethodPattern = DefaultPatterns.CreateHttpMethodPattern();

            Pattern.GenerateNewGuids(httpMethodPattern);

            var matcher = new PatternMatcher([httpMethodPattern], new PatternCompiler());
            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "GET /api/data HTTP/1.1",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var matches = results.Where(m => m.Pattern.PatternName == httpMethodPattern.PatternName).ToList();
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

            Pattern.GenerateNewGuids(addressPattern);
            Pattern.GenerateNewGuids(portPattern);
            Pattern.GenerateNewGuids(addressPortPattern);

            // Only include composite patterns, not standalone port
            var matcher = new PatternMatcher([addressPattern, addressPortPattern], new PatternCompiler());
            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "Invalid port: 8080",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            // Assert
            var portMatches = results.Where(m => m.Pattern.PatternName == portPattern.PatternName).ToList();
            Assert.Empty(portMatches); // Port shouldn't match when not part of address:port

            // Verify address:port pattern doesn't match invalid format
            var addressPortMatches = results.Where(m => m.Pattern.PatternName == addressPortPattern.PatternName).ToList();
            Assert.Empty(addressPortMatches);
        }

        [Fact]
        public void NginxPatternTests()
        {
            // Atomic patterns
            var ipPattern = DefaultPatterns.CreateAddressPattern();
            var timestampPattern = DefaultPatterns.CreateTimestampPattern();
            var httpVersionPattern = DefaultPatterns.CreateHttpVersionPattern();
            var statusCodePattern = DefaultPatterns.CreateStatusCodePattern();
            var userAgentPattern = DefaultPatterns.CreateUserAgentPattern();
            var resourcePattern = DefaultPatterns.CreateResourcePattern();

            Pattern.GenerateNewGuids(ipPattern);
            Pattern.GenerateNewGuids(timestampPattern);
            Pattern.GenerateNewGuids(httpVersionPattern);
            Pattern.GenerateNewGuids(statusCodePattern);
            Pattern.GenerateNewGuids(userAgentPattern);
            Pattern.GenerateNewGuids(resourcePattern);

            // Composite patterns
            var httpMethodPattern = DefaultPatterns.CreateHttpMethodPattern();
            Pattern.GenerateNewGuids(httpMethodPattern);

            var fullRequestPattern = DefaultPatterns.CreateFullRequestPattern(
                httpMethodPattern,
                resourcePattern,
                httpVersionPattern);
            Pattern.GenerateNewGuids(fullRequestPattern);

            var logEntryPattern = DefaultPatterns.CreateLogEntryPattern(
                ipPattern,
                timestampPattern,
                fullRequestPattern,
                statusCodePattern,
                userAgentPattern);

            Pattern.GenerateNewGuids(logEntryPattern);

            // Only include composite patterns, not standalone port
            var matcher = new PatternMatcher([
                ipPattern,
                timestampPattern,
                httpMethodPattern,
                fullRequestPattern,
                logEntryPattern
            ], new PatternCompiler());

            var line = new IndexedLine
            {
                Source = new DataSource { Name = "tests" },
                LineText = "159.89.16.205 - - [24/Mar/2025:07:17:41 +0300] \"GET /query?q=SHOW+DIAGNOSTICS HTTP/1.1\" 404 162 \"-\" \"Go-http-client/1.1\"",
                LineNumber = 1
            };

            // Act
            var results = matcher.ProcessLine(line);

            var logEntryMatches = results.Where(m => m.Pattern.PatternName == logEntryPattern.PatternName).ToList();
            var ipMatches = results.Where(m => m.Pattern.PatternName == ipPattern.PatternName).ToList();
            var timestampMatches = results.Where(m => m.Pattern.PatternName == timestampPattern.PatternName).ToList();
            var httpMethodMatches = results.Where(m => m.Pattern.PatternName == httpMethodPattern.PatternName).ToList();
            var resourceMatches = results.Where(m => m.Pattern.PatternName == resourcePattern.PatternName).ToList();
            var httpVersionMatches = results.Where(m => m.Pattern.PatternName == httpVersionPattern.PatternName).ToList();
            var statusCodeMatches = results.Where(m => m.Pattern.PatternName == statusCodePattern.PatternName).ToList();
            var userAgentMatches = results.Where(m => m.Pattern.PatternName == userAgentPattern.PatternName).ToList();


            Assert.Single(logEntryMatches);
            Assert.Single(ipMatches);
            Assert.Single(timestampMatches);
            Assert.Single(httpMethodMatches);
            Assert.Single(resourceMatches);
            Assert.Single(httpVersionMatches);
            Assert.Single(statusCodeMatches);
            Assert.Single(userAgentMatches);
        }
    }
}
