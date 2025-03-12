using System.ComponentModel.DataAnnotations;

namespace consoleAppTest.structs
{
    public class TagInstance
    {
        [Key]
        public Guid Id { get; set; }
        public required IndexedLine Line { get; set; }
        public required IndexedValue Value { get; set; }
        public int ValuePos { get; set; }
        public int ValueLength { get; set; }
    }
}