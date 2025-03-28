using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace colorfulLogs.structs
{
    public class PatternComponent
    {
        [Key]
        public Guid Id { get; set; }

        // Parent pattern containing the placeholder
        public Guid ParentPatternId { get; set; }
        [ForeignKey(nameof(ParentPatternId))]
        public virtual required Pattern ParentPattern { get; set; }

        // Child pattern referenced by the placeholder
        public Guid ChildPatternId { get; set; }
        [ForeignKey(nameof(ChildPatternId))]
        public virtual required Pattern ChildPattern { get; set; }

        // Placeholder name (e.g., "ipaddress" in "$ipaddress")
        public required string PlaceholderName { get; set; }

    }
}
