
using System.ComponentModel.DataAnnotations;

namespace colorfulLogs.structs
{
    public class IndexedLine
    {
        [Key]
        public Guid Id { get; set; }
        public DataSource? Source { get; set; }
        public required string LineText { get; set; }
        public ulong LineNumber { get; set; }

        public virtual List<TagInstance> TagInstances { get; set; } = [];

    }
}
