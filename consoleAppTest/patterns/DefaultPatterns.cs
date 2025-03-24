using consoleAppTest.structs;

namespace consoleAppTest.patterns
{
    public class DefaultPatterns
    {
        public static Pattern CreateTimestampPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Apache Timestamp",
                SyntaxString = @"\[\d{2}/\w{3}/\d{4}:\d{2}:\d{2}:\d{2} [+-]\d{4}\]",
                Components = []
            };
        }

        public static Pattern CreateHttpVersionPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP Version",
                SyntaxString = @"HTTP/\d\.\d",
                Components = []
            };
        }

        public static Pattern CreateStatusCodePattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP Status Code",
                SyntaxString = @"\b\d{3}\b",
                Components = []
            };
        }
        public static Pattern CreateUserAgentPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "User Agent",
                SyntaxString = "\"[^\"]+\"",
                Components = []
            };
        }

        public static Pattern CreateFullRequestPattern(
            Pattern method,
            Pattern resource,
            Pattern httpVersion)
        {
            var pattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Full HTTP Request",
                SyntaxString = @"$http_method $resource $http_version",
                Components = []
            };

            pattern.Components.AddRange(
                new PatternComponent
                {
                    ParentPattern = pattern,
                    PlaceholderName = "http_method",
                    ChildPattern = method
                },
                new PatternComponent
                {
                    ParentPattern = pattern,
                    PlaceholderName = "resource",
                    ChildPattern = resource
                },
                new PatternComponent
                {
                    ParentPattern = pattern,
                    PlaceholderName = "http_version",
                    ChildPattern = httpVersion
                });

            return pattern;
        }

        public static Pattern CreateLogEntryPattern(
            Pattern ip,
            Pattern timestamp,
            Pattern request,
            Pattern statusCode,
            Pattern userAgent)
        {
            var pattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Apache Log Entry",
                SyntaxString = "$ip - - $timestamp \"$request\" $status_code \\d+ \"-\" $user_agent",
                Components = []
            };

            pattern.Components.AddRange(
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "ip", ChildPattern = ip },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "timestamp", ChildPattern = timestamp },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "request", ChildPattern = request },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "status_code", ChildPattern = statusCode },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "user_agent", ChildPattern = userAgent }
            );

            return pattern;
        }

        public static Pattern CreateResourcePattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Resource Path",
                SyntaxString = @"/([^/^\ ]/?)*",
                Components = []
            };
        }

        public static Pattern CreateAddressPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "IPv4 Address",
                SyntaxString = @"(?:\d{1,3}\.){3}\d{1,3}",
                Components = []
            };
        }

        public static Pattern CreateHttpMethodPattern()
        {
            var getPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP GET",
                SyntaxString = "GET",
                Components = []
            };
            var postPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP POST",
                SyntaxString = "POST",
                Components = []
            };
            var headPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP HEAD",
                SyntaxString = "HEAD",
                Components = []
            };
            var optionsPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP OPTIONS",
                SyntaxString = "OPTIONS",
                Components = []
            };

            var putPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP PUT",
                SyntaxString = "PUT",
                Components = [],
            };
            var deletePattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP DELETE",
                SyntaxString = "DELETE",
                Components = [],
            };
            var connectPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP CONNECT",
                SyntaxString = "CONNECT",
                Components = [],
            };
            var tracePattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP TRACE",
                SyntaxString = "TRACE",
                Components = [],
            };
            var patchPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP PATCH",
                SyntaxString = "PATCH",
                Components = [],
            };

            var httpMethodPattern = new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "HTTP Method",
                SyntaxString = "$http_get|$http_post|$http_head|$http_options|$http_put|$http_delete|$http_connect|$http_trace|$http_patch",
                Components = [],

            };

            httpMethodPattern.Components =
            [
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_get", ChildPattern = getPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_post", ChildPattern = postPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_head", ChildPattern = headPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_options", ChildPattern = optionsPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_put", ChildPattern =  putPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_delete", ChildPattern =  deletePattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_connect", ChildPattern =  connectPattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_trace", ChildPattern =  tracePattern },
                new() { ParentPattern = httpMethodPattern, PlaceholderName = "http_patch", ChildPattern =  patchPattern },

            ];

            return httpMethodPattern;
        }

        public static Pattern CreatePortPattern()
        {
            return new Pattern
            {
                Id = Guid.NewGuid(),
                PatternName = "Port Number",
                SyntaxString = @"\b\d{1,5}\b",
                Components = []
            };
        }

        public static Pattern CreateAddressPortPattern(Pattern address, Pattern port)
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

        public static Pattern CreateUrlWithIpAndPortPattern(Pattern address, Pattern port, Pattern resource)
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

        public static List<Pattern> GetLogPatterns()
        {
            var patterns = new List<Pattern>();

            // Atomic patterns
            var ipPattern = CreateAddressPattern();
            var timestampPattern = CreateTimestampPattern();
            var httpVersionPattern = CreateHttpVersionPattern();
            var statusCodePattern = CreateStatusCodePattern();
            var userAgentPattern = CreateUserAgentPattern();
            var resourcePattern = CreateResourcePattern();

            // Composite patterns
            var httpMethodPattern = CreateHttpMethodPattern();

            var fullRequestPattern = CreateFullRequestPattern(
                httpMethodPattern,
                resourcePattern,
                httpVersionPattern);

            var logEntryPattern = CreateLogEntryPattern(
                ipPattern,
                timestampPattern,
                fullRequestPattern,
                statusCodePattern,
                userAgentPattern);

            patterns.AddRange([
                ipPattern,
                timestampPattern,
                httpMethodPattern,
                fullRequestPattern,
                logEntryPattern
            ]);

            return patterns;
        }
    }
}
