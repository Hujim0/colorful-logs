using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace colorfulLogs.structs
{
    public class LocalFile
    {
        public Guid Id { get; set; }
        public required string Path { get; set; }
        public byte[] LastHash { get; set; } = new byte[20];
        public long LastLength { get; set; } = 0;
        public required DataSource dataSource;
        public string PendingLine { get; set; } = "";
        public long LastLineNumber;
    }
}
