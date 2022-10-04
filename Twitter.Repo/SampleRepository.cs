using System.Net.Http;
using System.Text.Json;
using Twitter.Repo.Abstractions;
using Twitter.Repo.Models;

namespace Twitter.Repo
{
    public class SampleRepository : ISampleRepository
    {
        private HttpClient httpClient;

        public SampleRepository(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient("twitter");
        }
        public List<Tweet> Tweets { private set; get; } =new List<Tweet>();
        public async Task GetSampleStreamAsync(CancellationToken cancellationToken)
        {
            using var response = await httpClient.GetAsync("/2/tweets/sample/stream",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            var stream = new StreamReader(await response.Content.ReadAsStreamAsync());
            var line = await stream.ReadLineAsync();
            while (!string.IsNullOrEmpty(line))
            {
                var tweet = JsonSerializer.Deserialize<Tweet>(line);
                Tweets.Add(tweet);
                line = await stream.ReadLineAsync();
            }

                   
        
        }
    }
}