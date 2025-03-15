using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace consoleAppTest.structs
{
    [Index(nameof(Pattern), nameof(Value))]
    public class IndexedValue
    {
        [Key]
        public Guid Id { get; set; }
        public required string Value { get; set; }
        public required Pattern Pattern { get; set; }
        public virtual List<TagInstance> TagInstances { get; set; } = [];

    }
}