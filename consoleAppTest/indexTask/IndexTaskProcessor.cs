using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace consoleAppTest.indexTask
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
    }

    public class PriorityTaskScheduler : IDisposable
    {
        private readonly BlockingCollection<IndexTask> _highPriorityQueue = new BlockingCollection<IndexTask>();
        private readonly BlockingCollection<IndexTask> _lowPriorityQueue = new BlockingCollection<IndexTask>();
        private readonly List<Thread> _highPriorityWorkers;
        private readonly List<Thread> _lowPriorityWorkers;
        private bool _disposed;

        public PriorityTaskScheduler(
            int maxHighPriorityThreads = 4,
            int maxLowPriorityThreads = 2,
            int highPriorityQueueCapacity = 100)
        {
            // Initialize queues with capacity limits
            _highPriorityQueue = new BlockingCollection<IndexTask>(highPriorityQueueCapacity);

            // Create worker threads
            _highPriorityWorkers = CreateWorkerThreads(_highPriorityQueue, maxHighPriorityThreads, "HighPriWorker");
            _lowPriorityWorkers = CreateWorkerThreads(_lowPriorityQueue, maxLowPriorityThreads, "LowPriWorker");
        }

        public void QueueTask(IndexTask task)
        {
            if (task.Priority == TaskPriority.High)
            {
                if (!_highPriorityQueue.TryAdd(task))
                {
                    // If high priority queue is full, cancel low priority tasks
                    CancelLowPriorityTasks(1);
                    _highPriorityQueue.Add(task);
                }
            }
            else
            {
                _lowPriorityQueue.Add(task);
            }
        }

        private List<Thread> CreateWorkerThreads(
            BlockingCollection<IndexTask> queue,
            int threadCount,
            string threadNamePrefix)
        {
            var workers = new List<Thread>();

            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(() =>
                {
                    foreach (var task in queue.GetConsumingEnumerable())
                    {
                        try
                        {
                            if (task.CancellationToken.IsCancellationRequested)
                                continue;

                            task.Work?.Invoke();
                        }
                        catch (OperationCanceledException)
                        {
                            // Task cancellation requested
                        }
                    }
                })
                {
                    Name = $"{threadNamePrefix}_{i}",
                    IsBackground = true
                };

                thread.Start();
                workers.Add(thread);
            }

            return workers;
        }

        private void CancelLowPriorityTasks(int count)
        {

            int canceledTasks = 0;
            // Drain low priority queue and cancel tasks
            while (_lowPriorityQueue.TryTake(out var task) && canceledTasks < count)
            {
                task.CancellationToken = new CancellationToken(true);
                canceledTasks++;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_disposed) return;

            _highPriorityQueue.CompleteAdding();
            _lowPriorityQueue.CompleteAdding();

            foreach (var thread in _highPriorityWorkers.Concat(_lowPriorityWorkers))
            {
                thread.Join();
            }

            _highPriorityQueue.Dispose();
            _lowPriorityQueue.Dispose();
            _disposed = true;
        }
    }
}