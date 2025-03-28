using System.ComponentModel.DataAnnotations;

namespace colorfulLogs.structs
{
    public class Pattern
    {
        [Key]
        public Guid Id { get; set; }
        public required string PatternName { get; set; } = "";

        // Regex template with placeholders (e.g., "$ipaddress:[0-9]{1,4}")
        public required string SyntaxString { get; set; }

        // Sub-patterns used in this pattern
        public virtual List<PatternComponent> Components { get; set; } = [];

        public virtual List<IndexedValue> IndexedValues { get; set; } = [];
    }
}
