using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Twitter.Repo.Abstractions;
using Twitter.Repo;
using WorkerService;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Logging;
using Divergic.Logging.Xunit;

namespace WorkerServiceTests
{
    public class TweetMonitorTests
    {
        private readonly IConfigurationRoot config;
        private readonly ServiceCollection services;
        private readonly ServiceProvider serviceProvider;
        private readonly ITestOutputHelper output;
        private readonly ICacheLogger<TweetMonitor> tweetMonLogger;
        private readonly ICacheLogger<TweetRepository> tweetRepoLogger;
        private readonly ICacheLogger<DefaultBackgroundTaskQueue> bgTaskQueueLogger;

        public TweetMonitorTests(ITestOutputHelper output)
        {
            config = new ConfigurationBuilder()
                .AddUserSecrets<TweetMonitorTests>()
                .Build();

            services = new ServiceCollection();


            this.output = output;

            tweetMonLogger = output.BuildLoggerFor<TweetMonitor>();
            tweetRepoLogger = output.BuildLoggerFor<TweetRepository>(LogLevel.Information);
            bgTaskQueueLogger = output.BuildLoggerFor<DefaultBackgroundTaskQueue>();

            services.AddSingleton(tweetRepoLogger);


            services.AddHttpClient<ITweetRepository, TweetRepository>("twitter", _ =>
            {
                _.Timeout = TimeSpan.FromSeconds(3);
                _.BaseAddress = new Uri("https://api.twitter.com");
                _.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GetSection("ApiToken").Value}");
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler((provider, request) => GetRetryPolicy(provider));

            serviceProvider = services.BuildServiceProvider();

        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider policyServiceProvider)
        {
            var retryCodes = new[] {
                System.Net.HttpStatusCode.NotFound,
                System.Net.HttpStatusCode.TooManyRequests};

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => retryCodes.Any(_=>_ == msg.StatusCode))
                .WaitAndRetryAsync(6, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        var logger = policyServiceProvider.GetService<ICacheLogger<TweetRepository>>();
                        logger.LogWarning($"Polly retry {retryCount} triggered. Retrying Twitter in {timeSpan.TotalSeconds} seconds.");
                    });
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void TweetMonitor_Ctor_Nominal()
        {

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var tweetRepo = new TweetRepository(tweetRepoLogger, httpClientFactory);

            CancellationTokenSource cancellationTokenSource = new();

            var bgTaskQueue = new DefaultBackgroundTaskQueue(3,
                bgTaskQueueLogger);

            var hostAppLifetime = Mock.Of<IHostApplicationLifetime>();
            Mock.Get(hostAppLifetime)
                .Setup(_ => _.ApplicationStopping)
                .Returns(CancellationToken.None);

            var target = new TweetMonitor(bgTaskQueue,
                tweetMonLogger,
                hostAppLifetime,
                tweetRepo);

            Assert.NotNull(target);

        }


        [Fact]
        [Trait("Category", "Integration")]
        public void TweetMonitor_StartMonitor_Nominal()
        {

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var tweetRepo = new TweetRepository(tweetRepoLogger, httpClientFactory);

            CancellationTokenSource cancellationTokenSource = new();

            var bgTaskQueue = new DefaultBackgroundTaskQueue(3,
                bgTaskQueueLogger);

            var hostAppLifetime = Mock.Of<IHostApplicationLifetime>();
            Mock.Get(hostAppLifetime)
                .Setup(_ => _.ApplicationStopping)
                .Returns(CancellationToken.None);

            var target = new TweetMonitor(bgTaskQueue,
                tweetMonLogger,
                hostAppLifetime,
                tweetRepo);

            target.StartMonitor();

            Assert.NotNull(bgTaskQueue.Queue);
            Assert.Equal(1,bgTaskQueue.Queue.Reader.Count);
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async void TweetMonitor_BgTask_Exec_Single()
        {

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var tweetRepo = new TweetRepository(tweetRepoLogger, httpClientFactory);

            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.CancelAfter(3000);

            var bgTaskQueue = new DefaultBackgroundTaskQueue(1,
                bgTaskQueueLogger);

            var hostAppLifetime = Mock.Of<IHostApplicationLifetime>();
            Mock.Get(hostAppLifetime)
                .Setup(_ => _.ApplicationStopping)
                .Returns(CancellationToken.None);

            var target = new TweetMonitor(bgTaskQueue,
                tweetMonLogger,
                hostAppLifetime,
                tweetRepo);

            target.StartMonitor();

            Assert.False(tweetRepo.Tweets.Any());

            await (await bgTaskQueue.DequeueAsync(cancellationTokenSource.Token)).Invoke(cancellationTokenSource.Token);

            Assert.True(tweetRepo.Tweets.Any());
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async void TweetMonitor_BgTask_Exec_Chain()
        {

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var tweetRepo = new TweetRepository(tweetRepoLogger, httpClientFactory);

            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.CancelAfter(90000);

            var bgTaskQueue = new DefaultBackgroundTaskQueue(3,
                bgTaskQueueLogger);


            var hostAppLifetime = Mock.Of<IHostApplicationLifetime>();
            Mock.Get(hostAppLifetime)
                .Setup(_ => _.ApplicationStopping)
                .Returns(CancellationToken.None);

            var target = new TweetMonitor(bgTaskQueue,
                tweetMonLogger,
                hostAppLifetime,
                tweetRepo);

            target.StartMonitor();

            Assert.False(tweetRepo.Tweets.Any());

            await (await bgTaskQueue.DequeueAsync(cancellationTokenSource.Token)).Invoke(cancellationTokenSource.Token);

            Assert.True(tweetRepo.Tweets.Any());
        }
    }
}