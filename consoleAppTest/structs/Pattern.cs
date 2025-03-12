using System.ComponentModel.DataAnnotations;

namespace consoleAppTest.structs
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

        public string GetResolvedRegex()
        {
            string regex = SyntaxString;
            foreach (var component in Components)
            {
                string childRegex = component.ChildPattern.GetResolvedRegex();
                regex = regex.Replace($"${component.PlaceholderName}", childRegex);
            }
            return regex;
        }
    }
}