using Twitter.Repo;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly TweetRepository twitterSampleRepo;
        private readonly IConfiguration config;

        public Worker(ILogger<Worker> logger,
            IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //await Task.Delay(1000, stoppingToken);

                using IServiceScope scope = serviceProvider.CreateScope();
                TweetRepository sampleRepo =
                    scope.ServiceProvider.GetRequiredService<TweetRepository>();

                await sampleRepo.GetSampleStreamAsync(stoppingToken);
            }
        }
    }
}