using System.Collections;
using Twitter.Repo;
using Twitter.Repo.Abstractions;

namespace WorkerService;

public class TweetMonitor
{
    public IBackgroundTaskQueue TaskQueue { private set; get; }
    private readonly ILogger<TweetMonitor> logger;
    public TweetRepository TweetRepository { private set; get; }
    private readonly CancellationToken cancellationToken;

    public TweetMonitor(
        IBackgroundTaskQueue taskQueue,
        ILogger<TweetMonitor> logger,
        IHostApplicationLifetime applicationLifetime,
        TweetRepository tweetRepository)
    {
        this.TaskQueue = taskQueue;
        this.logger = logger;
        this.TweetRepository = tweetRepository;
        cancellationToken = applicationLifetime.ApplicationStopping;
    }

    public async void StartMonitor()
    {
        logger.LogInformation($"{nameof(MonitorAsync)} is starting.");

        await MonitorAsync();
    }

    private async ValueTask MonitorAsync()
    {
        var isQueueFull = false;
        while (!cancellationToken.IsCancellationRequested &&
            !isQueueFull)
        {
            // Enqueue a background work item
            isQueueFull = await TaskQueue.QueueBackgroundWorkItemAsync(BuildWorkItemAsync);
        }
    }

    private async ValueTask BuildWorkItemAsync(CancellationToken stoppingToken)
    {
        var guid = Guid.NewGuid();

        logger.LogInformation("Queued work item {Guid} is starting.", guid);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                // call the provider
                await TweetRepository.GetSampleStreamAsync(stoppingToken);

                logger.LogInformation($"TweetRepo.Tweets: {TweetRepository.Tweets.Count}");
                
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if the provider call is cancelled
            }

            logger.LogInformation("Queued work item {Guid} is done. ", guid);
        }

    }
}