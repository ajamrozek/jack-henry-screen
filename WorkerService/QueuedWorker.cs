using Twitter.Repo;

namespace WorkerService
{
    public class QueuedWorker : BackgroundService
    {
        private readonly ILogger<QueuedWorker> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IBackgroundTaskQueue taskQueue;

        public QueuedWorker(ILogger<QueuedWorker> logger,
            IServiceProvider serviceProvider,
            IBackgroundTaskQueue taskQueue)
        {
            (this.logger,
                this.serviceProvider,
                this.taskQueue) = 
                (logger,
                    serviceProvider,
                    taskQueue);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Func<CancellationToken, ValueTask>? workItem = await taskQueue.DequeueAsync(stoppingToken);

                    await workItem(stoppingToken);

                    stoppingToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken).Token;

                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred executing task work item.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(
                $"{nameof(QueuedWorker)} is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}