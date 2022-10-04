using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Twitter.Repo.Abstractions;

namespace Twitter.Repo.Tests
{
    public class SampleRepositoryTests
    {
        private IServiceCollection services;
        private ServiceProvider serviceProvider;
        private IHttpClientFactory httpClientFactory;
        IConfiguration config; 

        public SampleRepositoryTests()
        {
            config = new ConfigurationBuilder()
            .AddUserSecrets<SampleRepositoryTests>()
            .Build();

            services = new ServiceCollection();
            services.AddHttpClient<ISampleRepository, SampleRepository>("twitter",_=>
            {
                _.Timeout = TimeSpan.FromSeconds(3);
                _.BaseAddress = new Uri("https://api.twitter.com");
                _.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GetSection("ApiToken").Value}");
            });
                
            serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async void GetSampleStream_Nominal()
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var target = new Twitter.Repo.SampleRepository(httpClientFactory);

            CancellationTokenSource cancellationTokenSource = new();
            await target.GetSampleStreamAsync(cancellationTokenSource.Token);

            Assert.NotEmpty(target.Tweets);
        }
    }
}