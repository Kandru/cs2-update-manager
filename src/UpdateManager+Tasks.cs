using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;

namespace UpdateManager
{
    public partial class UpdateManager : BasePlugin
    {
        private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private readonly ConcurrentQueue<Func<Task>> _updateQueue = new();
        private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

        private async Task ProcessUpdateQueueAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await _queueSemaphore.WaitAsync();
                try
                {
                    if (_updateQueue.TryDequeue(out var challengeTask))
                    {
                        await challengeTask();
                    }
                }
                finally
                {
                    _queueSemaphore.Release();
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(Localizer["core.tasks.stopped"]);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                // Add a small delay to prevent tight loop
                await Task.Delay(100);
            }
        }

        public void EnqueueUpdateTask(Func<Task> challengeTask)
        {
            _updateQueue.Enqueue(challengeTask);
        }
    }
}
