using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace consoleAppTest.structs
{
    enum DataSourceOrigin { File, ByLineIncoming }
    public class DataSource
    {
        [Key]
        public Guid Id { get; set; }
        public required string Name { get; set; }

        public virtual List<IndexedLine> IndexedLines { get; set; } = [];

    }
}