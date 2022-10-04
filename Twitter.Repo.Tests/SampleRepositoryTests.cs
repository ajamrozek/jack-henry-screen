using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using Twitter.Repo.Abstractions;
using Xunit.Abstractions;

namespace Twitter.Repo.Tests
{
    public class SampleRepositoryTests
    {
        private IServiceCollection services;
        private ServiceProvider serviceProvider;
        private IHttpClientFactory httpClientFactory;
        IConfiguration config;

        private readonly ITestOutputHelper output;

        public SampleRepositoryTests(ITestOutputHelper output)
        {
            config = new ConfigurationBuilder()
            .AddUserSecrets<SampleRepositoryTests>()
            .Build();

            services = new ServiceCollection();
            services.AddHttpClient<ITweetRepository, TweetRepository>("twitter",_=>
            {
                _.Timeout = TimeSpan.FromSeconds(3);
                _.BaseAddress = new Uri("https://api.twitter.com");
                _.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GetSection("ApiToken").Value}");
            });
                
            serviceProvider = services.BuildServiceProvider();
            this.output = output;
        }

        [Fact]
        public async void GetSampleStream_Nominal()
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            using var logger = output.BuildLoggerFor<TweetRepository>();

            var target = new TweetRepository(logger, httpClientFactory);

            CancellationTokenSource cancellationTokenSource = new();
            
            await target.GetSampleStreamAsync(cancellationTokenSource.Token);

            Assert.NotEmpty(target.Tweets);
        }
    }
}