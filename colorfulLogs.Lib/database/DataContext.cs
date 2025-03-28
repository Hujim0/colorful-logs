using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using colorfulLogs.structs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace colorfulLogs.database
{
    public class DataContext(string databaseName) : DbContext
    {
        public static readonly object DbLock = new();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlite($"Data Source = {databaseName}");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Pattern -> PatternComponent relationships
            modelBuilder.Entity<Pattern>(entity =>
            {
                // Parent pattern's components
                entity.HasMany(p => p.Components)
                    .WithOne(c => c.ParentPattern)
                    .HasForeignKey(c => c.ParentPatternId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Pattern's indexed values
                entity.HasMany(p => p.IndexedValues)
                    .WithOne(v => v.Pattern)
                    .HasForeignKey(v => v.PatternId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PatternComponent -> ChildPattern relationship
            modelBuilder.Entity<PatternComponent>(entity =>
            {
                entity.HasOne(c => c.ChildPattern)
                    .WithMany()
                    .HasForeignKey(c => c.ChildPatternId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure TagInstance relationships
            modelBuilder.Entity<TagInstance>(entity =>
            {
                entity.HasOne(t => t.IndexedLine)
                    .WithMany(l => l.TagInstances)
                    .HasForeignKey("IndexedLineId")  // Shadow property
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.IndexedValue)
                    .WithMany(v => v.TagInstances)
                    .HasForeignKey("IndexedValueId")  // Shadow property
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure DataSource -> IndexedLine relationship
            modelBuilder.Entity<IndexedLine>(entity =>
            {
                entity.HasOne(l => l.Source)
                    .WithMany(s => s.IndexedLines)
                    .HasForeignKey(l => l.SourceId);
            });

            // Configure composite index for IndexedValue
            modelBuilder.Entity<IndexedValue>()
                .HasIndex(iv => new { iv.PatternId, iv.Value });

            // // Configure LocalFile -> DataSource relationship
            // modelBuilder.Entity<LocalFile>(entity =>
            // {
            //     entity.HasOne(f => f.dataSource)
            //         .WithMany(l => l.)
            //         .HasForeignKey()  // Shadow property
            //         .IsRequired();
            // });

            // Configure Sequential GUIDs for all entities
            modelBuilder.Entity<DataSource>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

            modelBuilder.Entity<IndexedLine>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

            modelBuilder.Entity<IndexedValue>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

            modelBuilder.Entity<TagInstance>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

            modelBuilder.Entity<Pattern>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

            modelBuilder.Entity<PatternComponent>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

            modelBuilder.Entity<LocalFile>()
                .Property(b => b.Id)
                .HasValueGenerator<SequentialGuidValueGenerator>();

        }

        public DbSet<DataSource> DataSources { get; set; }
        public DbSet<IndexedLine> IndexedLines { get; set; }
        public DbSet<IndexedValue> IndexedValues { get; set; }
        public DbSet<TagInstance> TagInstances { get; set; }
        public DbSet<Pattern> Patterns { get; set; }
        public DbSet<PatternComponent> PatternComponents { get; set; }
    }
}
