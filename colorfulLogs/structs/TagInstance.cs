using System.ComponentModel.DataAnnotations;

namespace colorfulLogs.structs
{
    public class TagInstance
    {
        [Key]
        public Guid Id { get; set; }
        public required IndexedLine IndexedLine { get; set; }
        public required IndexedValue IndexedValue { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }
}
