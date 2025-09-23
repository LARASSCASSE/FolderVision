using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FolderVision.Core
{
    public class ThreadManager
    {
        private readonly int _maxConcurrency;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentQueue<Task> _taskQueue;

        public ThreadManager(int maxConcurrency = Environment.ProcessorCount)
        {
            _maxConcurrency = maxConcurrency;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _taskQueue = new ConcurrentQueue<Task>();
        }

        public async Task ExecuteAsync(Func<Task> taskFunc)
        {
            throw new NotImplementedException();
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> taskFunc)
        {
            throw new NotImplementedException();
        }

        public void SetMaxConcurrency(int maxConcurrency)
        {
            throw new NotImplementedException();
        }

        public int ActiveTasks { get; private set; }
        public int QueuedTasks => _taskQueue.Count;

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}