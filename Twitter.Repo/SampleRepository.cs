using Microsoft.Extensions.Logging;
using System.Text.Json;
using Twitter.Repo.Abstractions;
using Twitter.Repo.Models;

namespace Twitter.Repo;

public class TweetRepository : ITweetRepository
{
    private readonly ILogger<TweetRepository> logger;
    private HttpClient httpClient;

    public TweetRepository(ILogger<TweetRepository> logger, 
        IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient("twitter");
        this.logger = logger;
    }
    public List<Tweet> Tweets { private set; get; } = new List<Tweet>();

    public async Task GetSampleStreamAsync(CancellationToken cancellationToken)
    {
        var logHeader = $"{nameof(GetSampleStreamAsync)}";
        logger.LogInformation($"{logHeader} Starting. "); 
        using var response = await httpClient.GetAsync("/2/tweets/sample/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Twitter Sample Stream Headers succeeded.");

        var stream = new StreamReader(await response.Content.ReadAsStreamAsync());
        var line = await stream.ReadLineAsync();
        while (!string.IsNullOrEmpty(line))
        {
            logger.LogInformation($"Tweet line read from stream: {line}");
            var tweet = JsonSerializer.Deserialize<Tweet>(line);
            Tweets.Add(tweet);
            line = await stream.ReadLineAsync();
        }

        logger.LogInformation($"{logHeader} Ended. ");

    }
}