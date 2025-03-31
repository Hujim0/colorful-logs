using colorfulLogs.structs;
using System.Collections.Generic;

namespace colorfulLogs.patterns
{
    public class VeeamLogPatterns
    {
        public static Pattern CreateTimestampPattern()
        {
            return new Pattern
            {
                PatternName = "Log Timestamp",
                SyntaxString = @"\[\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}\]",
                Components = new List<PatternComponent>()
            };
        }

        public static Pattern CreateProcessIdPattern()
        {
            return new Pattern
            {
                PatternName = "Process ID",
                SyntaxString = @"<\d+>",
                Components = new List<PatternComponent>()
            };
        }

        public static Pattern CreateSeverityLevelPattern()
        {
            return new Pattern
            {
                PatternName = "Severity Level",
                SyntaxString = @"Info|Error|Warning|Debug",
                Components = new List<PatternComponent>()
            };
        }

        public static Pattern CreateModuleTagPattern()
        {
            return new Pattern
            {
                PatternName = "Module Tag",
                SyntaxString = @"\[\w+\]",
                Components = new List<PatternComponent>()
            };
        }

        public static Pattern CreateKeyValuePairPattern()
        {
            var keyPattern = new Pattern
            {
                PatternName = "Key",
                SyntaxString = @"\w+",
                Components = new List<PatternComponent>()
            };

            var valuePattern = new Pattern
            {
                PatternName = "Value",
                SyntaxString = @"'[^']*'|\d+|\[[^\]]*\]|yes|no",
                Components = new List<PatternComponent>()
            };

            var kvPattern = new Pattern
            {
                PatternName = "Key-Value Pair",
                SyntaxString = @"$key:\s*$value",
                Components = new List<PatternComponent>()
            };

            kvPattern.Components.AddRange(new[]
            {
                new PatternComponent { ParentPattern = kvPattern, PlaceholderName = "key", ChildPattern = keyPattern },
                new PatternComponent { ParentPattern = kvPattern, PlaceholderName = "value", ChildPattern = valuePattern }
            });

            return kvPattern;
        }

        public static Pattern CreateParametersPattern(Pattern keyValuePair)
        {
            var pattern = new Pattern
            {
                PatternName = "Parameters List",
                SyntaxString = @"\[\s*$kvp(,\s*$kvp)*\s*\]\.?",
                Components = new List<PatternComponent>()
            };

            pattern.Components.Add(
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "kvp", ChildPattern = keyValuePair }
            );

            return pattern;
        }

        public static Pattern CreateExceptionPattern()
        {
            return new Pattern
            {
                PatternName = "Exception Type",
                SyntaxString = @"\b\w+(\.\w+)+\b(?=Exception)",
                Components = new List<PatternComponent>()
            };
        }

        public static Pattern CreateStackTracePattern()
        {
            return new Pattern
            {
                PatternName = "Stack Trace Line",
                SyntaxString = @"at\s+\S+\.\S+\(.*\)(\s+in\s+\S+:\d+)?",
                Components = new List<PatternComponent>()
            };
        }

        public static Pattern CreateLogEntryPattern(
            Pattern timestamp,
            Pattern processId,
            Pattern severity,
            Pattern messageContent)
        {
            var pattern = new Pattern
            {
                PatternName = "Log Entry",
                SyntaxString = @"$timestamp $processId $severity $message",
                Components = new List<PatternComponent>()
            };

            pattern.Components.AddRange(new[]
            {
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "timestamp", ChildPattern = timestamp },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "processId", ChildPattern = processId },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "severity", ChildPattern = severity },
                new PatternComponent { ParentPattern = pattern, PlaceholderName = "message", ChildPattern = messageContent }
            });

            return pattern;
        }

        public static List<Pattern> GetLogPatterns()
        {
            var patterns = new List<Pattern>();

            // Atomic patterns
            var timestamp = CreateTimestampPattern();
            var processId = CreateProcessIdPattern();
            var severity = CreateSeverityLevelPattern();
            var moduleTag = CreateModuleTagPattern();
            var kvPair = CreateKeyValuePairPattern();
            var parameters = CreateParametersPattern(kvPair);
            var exception = CreateExceptionPattern();
            var stackTrace = CreateStackTracePattern();

            // Composite patterns
            var sshConnectionPattern = new Pattern
            {
                PatternName = "SSH Connection Message",
                SyntaxString = @"$module Creating new connection $params",
                Components = new List<PatternComponent>()
            };

            sshConnectionPattern.Components.AddRange(new[]
            {
                new PatternComponent { ParentPattern = sshConnectionPattern, PlaceholderName = "module", ChildPattern = moduleTag },
                new PatternComponent { ParentPattern = sshConnectionPattern, PlaceholderName = "params", ChildPattern = parameters }
            });

            var errorMessagePattern = new Pattern
            {
                PatternName = "Error Message",
                SyntaxString = @"$exception(.*\n\s*$stackTrace)*",
                Components = new List<PatternComponent>()
            };

            errorMessagePattern.Components.AddRange(new[]
            {
                new PatternComponent { ParentPattern = errorMessagePattern, PlaceholderName = "exception", ChildPattern = exception },
                new PatternComponent { ParentPattern = errorMessagePattern, PlaceholderName = "stackTrace", ChildPattern = stackTrace }
            });

            var logEntry = CreateLogEntryPattern(timestamp, processId, severity,
                new Pattern { PatternName = "Message", SyntaxString = ".*", Components = new List<PatternComponent>() });

            patterns.AddRange(new[]
            {
                timestamp,
                processId,
                severity,
                moduleTag,
                kvPair,
                parameters,
                exception,
                stackTrace,
                sshConnectionPattern,
                errorMessagePattern,
                logEntry
            });

            return patterns;
        }
    }
}