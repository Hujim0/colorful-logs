using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using consoleAppTest.structs;

namespace consoleAppTest.patterns
{
    public class DefaultPatterns
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

        private static Pattern CreateResourcePattern()
        {
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


        public static List<Pattern> GetDefaultPatterns()
        {
            var address = CreateAddressPattern();
            var port = CreatePortPattern();
            var addressPort = CreateAddressPortPattern(address, port);
            var resource = CreateResourcePattern();

            var patterns = new List<Pattern> { address, port, addressPort, resource };

            return patterns;
        }
    }
}