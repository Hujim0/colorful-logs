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

        //used primarily for tests, because database creates those automatically
        public static void GenerateNewGuids(Pattern pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var processedIds = new HashSet<Guid>();
            GenerateNewGuidsRecursive(pattern, processedIds);
        }

        private static void GenerateNewGuidsRecursive(Pattern pattern, HashSet<Guid> processedIds)
        {
            if (processedIds.Contains(pattern.Id))
                return;

            // Save the old Id to check if it's a new or existing entry
            var oldId = pattern.Id;

            // Generate a new Guid for the current pattern
            pattern.Id = Guid.NewGuid();

            // Add the new Id to the processed set to prevent cycles
            processedIds.Add(pattern.Id);

            // Update all components' ParentPatternId to the new Id
            foreach (var component in pattern.Components)
            {
                component.ParentPatternId = pattern.Id;
            }

            // Process each child pattern and update the component's ChildPatternId
            foreach (var component in pattern.Components.ToList()) // Use ToList to avoid modification issues during iteration
            {
                var childPattern = component.ChildPattern;

                // Recursively generate new Guids for the child pattern
                GenerateNewGuidsRecursive(childPattern, processedIds);

                // Update the component's ChildPatternId to the child's new Id
                component.ChildPatternId = childPattern.Id;
            }
        }
    }
}
