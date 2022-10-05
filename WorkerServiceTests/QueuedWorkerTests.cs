using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Twitter.Repo.Abstractions;
using Twitter.Repo;
using WorkerService;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Hosting;

namespace WorkerServiceTests
{
    public class QueuedWorkerTests
    {
        private readonly IConfigurationRoot config;
        private readonly ServiceCollection services;
        private readonly ServiceProvider serviceProvider;
        private readonly ITestOutputHelper output;

        public QueuedWorkerTests(ITestOutputHelper output)
        {
            config = new ConfigurationBuilder()
                .AddUserSecrets<TweetMonitorTests>()
                .Build();

            services = new ServiceCollection();
            services.AddHttpClient<ITweetRepository, TweetRepository>("twitter", _ =>
            {
                _.Timeout = TimeSpan.FromSeconds(3);
                _.BaseAddress = new Uri("https://api.twitter.com");
                _.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GetSection("ApiToken").Value}");
            });

            serviceProvider = services.BuildServiceProvider();
            this.output = output;
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void QueuedWorker_Ctor_Nominal()
        {
            using var bgTaskQueueLogger = output.BuildLoggerFor<DefaultBackgroundTaskQueue>();
            var bgTaskQueue = new DefaultBackgroundTaskQueue(3,
                bgTaskQueueLogger);

            using var queuedWorkerLogger = output.BuildLoggerFor<QueuedWorker>();
            
            var target = new QueuedWorker(queuedWorkerLogger,
                serviceProvider,
                bgTaskQueue);

            Assert.NotNull(target);
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async void QueuedWorker_Dequeue_Nominal()
        {

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            using var queuedWorkerLogger = output.BuildLoggerFor<QueuedWorker>();
            using var bgTaskQueueLogger = output.BuildLoggerFor<DefaultBackgroundTaskQueue>();


            CancellationTokenSource cancellationTokenSource = new();

            var bgTaskQueue = new DefaultBackgroundTaskQueue(3,
                bgTaskQueueLogger);


            var target = new QueuedWorker(queuedWorkerLogger,
                serviceProvider,
                bgTaskQueue);

            await target.StartAsync(cancellationTokenSource.Token);

        }
    }
}