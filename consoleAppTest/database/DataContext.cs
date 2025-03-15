using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using consoleAppTest.structs;
using Microsoft.EntityFrameworkCore;

namespace consoleAppTest.database
{
    public class DataContext(string databaseName) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlite($"Data Source = {databaseName}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DataSource>().Property(b => b.Id).HasDefaultValue(new Guid());
            modelBuilder.Entity<IndexedLine>().Property(b => b.Id).HasDefaultValue(new Guid());
            modelBuilder.Entity<IndexedValue>().Property(b => b.Id).HasDefaultValue(new Guid());
            modelBuilder.Entity<TagInstance>().Property(b => b.Id).HasDefaultValue(new Guid());
            modelBuilder.Entity<Pattern>().Property(b => b.Id).HasDefaultValue(new Guid());
        }

        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<IndexedLine> IndexedLines { get; set; }
        public DbSet<IndexedValue> IndexedValues { get; set; }
        public DbSet<TagInstance> TagInstances { get; set; }
        public DbSet<Pattern> Patterns { get; set; }
    }
}