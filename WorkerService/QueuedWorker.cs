using Twitter.Repo;

namespace WorkerService
{
    public class QueuedWorker : BackgroundService
    {
        private readonly ILogger<QueuedWorker> logger;
        private readonly IBackgroundTaskQueue taskQueue;
        private readonly IConfiguration config;
        private int asyncBatchSize;
        public QueuedWorker(ILogger<QueuedWorker> logger,
            IBackgroundTaskQueue taskQueue,
            IConfiguration config)
        {
            (this.logger,
                this.taskQueue,
                this.config ) = 
                (logger,
                    taskQueue,
                    config);

            // async batch size allows env config driven scaling to ingest multiple streams at once
            if (!int.TryParse(config["AsyncBatchSize"], out var asyncBatchSize))
            {
                asyncBatchSize = 1;
            }

            this.asyncBatchSize = asyncBatchSize;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<Task> taskBatch = new();

            while (!stoppingToken.IsCancellationRequested &&
                taskBatch.Count <= asyncBatchSize)
            {
                try
                {
                    Func<CancellationToken, ValueTask>? workItem = await taskQueue.DequeueAsync(stoppingToken);

                    taskBatch.Add(workItem(stoppingToken).AsTask());

                    if(taskBatch.Count == asyncBatchSize)
                    {
                        Task.WaitAll(taskBatch.ToArray(), stoppingToken);
                    }
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
            logger.LogInformation($"{nameof(QueuedWorker)} is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}