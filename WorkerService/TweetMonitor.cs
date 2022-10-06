using Microsoft.Extensions.DependencyInjection;
using Twitter.Repo;
using Twitter.Repo.Abstractions;

namespace WorkerService;

public class TweetMonitor
{
    public IBackgroundTaskQueue TaskQueue { private set; get; }
    private readonly ILogger<TweetMonitor> logger;
    public TweetRepository TweetRepository { private set; get; }
    private CancellationToken cancellationToken;

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
        // check the Twitter status and exit if we have fatals
        var isUp = await TweetRepository.CheckStatus(cancellationToken);
        if (!isUp)
        {
            logger.LogCritical($"Unable to reach the data provider stream.");
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.Cancel();
            cancellationToken = cancellationTokenSource.Token;
        }
        else
        {
            logger.LogInformation($"{nameof(MonitorAsync)} is starting.");
            await MonitorAsync();
        }
    }

    /// <summary>
    /// Continuously queues the operations while not cancelled and the queue is not full.  
    /// </summary>
    /// <returns></returns>
    private async ValueTask MonitorAsync()
    {
        var isQueueFull = false;
        while (!cancellationToken.IsCancellationRequested &&
            !isQueueFull)
        {
            // Enqueue a background work item
            isQueueFull = await TaskQueue.QueueBackgroundWorkItemAsync(BuildWorkItemAsync);
            string message = string.Empty;// $"Queue items: {((DefaultBackgroundTaskQueue)TaskQueue).Queue.Reader.Count}.";

            if (!isQueueFull)
            {
                logger.LogInformation($"Queued a new operation. {message}");
            }
            else
            {
                logger.LogInformation($"Queue full. {message}");
            }
        }
    }

    /// <summary>
    /// The working operation that hits the data provider.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async ValueTask BuildWorkItemAsync(CancellationToken stoppingToken)
    {
        var guid = Guid.NewGuid();

        logger.LogInformation($"Queued work item {guid} is starting.");

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

            logger.LogInformation($"Queued work item {guid} is done. ");
        }

    }
}