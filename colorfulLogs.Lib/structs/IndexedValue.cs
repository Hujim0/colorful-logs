using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace colorfulLogs.structs
{
    [Index(nameof(Pattern), nameof(Value))]
    public class IndexedValue
    {
        [Key]
        public Guid Id { get; set; }
        public required string Value { get; set; }


        public Guid PatternId { get; set; }

        [ForeignKey(nameof(PatternId))]
        public required Pattern Pattern { get; set; }

        public virtual List<TagInstance> TagInstances { get; set; } = [];

    }
}
