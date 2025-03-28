using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using colorfulLogs.database;
using colorfulLogs.patterns;
using colorfulLogs.structs;
using Microsoft.EntityFrameworkCore;

namespace colorfulLogs.Lib.indexTask
{
    public enum TaskPriority
    {
        High,
        Low
    }
    public class IndexTask
    {
        public TaskPriority Priority { get; set; }
        public required Action Work { get; set; }
        public CancellationToken CancellationToken { get; set; }


        public static IndexTask CreateDefault(TaskPriority priority, List<IndexedLine> indexedLines, PatternMatcher patternMatcher)
        {
            void work()
            {
                DataSource source = indexedLines.First().Source;
                lock (DataContext.DbLock) // Serialize writes
                {
                    // Save IndexedLines first

                    using var lineContext = new DataContext("UserData");
                    lineContext.DataSources.Attach(source);

                    lineContext.IndexedLines.AddRange(indexedLines);
                    lineContext.SaveChanges();
                }

                List<IndexedValue> allValues = [];
                List<TagInstance> allTagInstances = [];

                foreach (var indexedLine in indexedLines)
                {
                    Console.WriteLine($"Line {indexedLine.LineNumber}: {indexedLine.LineText}");

                    var values = patternMatcher.ProcessLine(indexedLine);
                    allValues.AddRange(values);

                    foreach (var value in values)
                    {
                        allTagInstances.AddRange(value.TagInstances);
                    }
                }

                // Save IndexedValues
                if (allValues.Count > 0 && allTagInstances.Count > 0)
                {
                    lock (DataContext.DbLock) // Serialize writes
                    {
                        using var valueContext = new DataContext("UserData");
                        valueContext.DataSources.Attach(source);

                        var uniquePatterns = allValues.Select(v => v.Pattern).DistinctBy(p => p.Id).ToList();

                        foreach (var pattern in uniquePatterns)
                        {
                            valueContext.Attach(pattern);
                            valueContext.Entry(pattern).State = EntityState.Unchanged;
                        }
                        valueContext.IndexedValues.AddRange(allValues);

                        foreach (var tagInstance in allTagInstances)
                        {
                            valueContext.Attach(tagInstance);
                        }

                        valueContext.TagInstances.AddRange(allTagInstances);
                        valueContext.SaveChanges();
                    }
                }
            }

            return new IndexTask
            {
                Priority = priority,
                Work = work
            };
        }
    }
}