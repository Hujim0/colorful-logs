
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace colorfulLogs.structs
{
    public class IndexedLine
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        public required DataSource Source { get; set; }
        public required string LineText { get; set; }
        public ulong LineNumber { get; set; }

        public virtual List<TagInstance> TagInstances { get; set; } = [];

    }
}
