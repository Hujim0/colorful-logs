using System.ComponentModel.DataAnnotations;

namespace consoleAppTest.structs
{
    public class IndexedValue
    {
        [Key]
        public Guid Id { get; set; }
        public required string Value { get; set; }
        public required Pattern Pattern { get; set; }

        public virtual List<TagInstance> TagInstances { get; set; } = [];

    }
}