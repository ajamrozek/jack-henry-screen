using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading;
using Twitter.Repo.Abstractions;
using Twitter.Repo.Models;

namespace Twitter.Repo;

public class TweetRepository : ITweetRepository
{
    private const string SampleStreamRequestUri = "/2/tweets/sample/stream";
    private readonly ILogger<TweetRepository> logger;
    private HttpClient httpClient;

    public TweetRepository(ILogger<TweetRepository> logger, 
        IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient("twitter");
        this.logger = logger;
    }
    public List<Tweet> Tweets { private set; get; } = new List<Tweet>();

    public async Task<bool> CheckStatus(CancellationToken cancellationToken)
    {
        // http call to Twitter stream headers
        using var response = await httpClient.GetAsync(SampleStreamRequestUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var acceptibleStatusCodes = new[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.TooManyRequests
        };
        var isUp = acceptibleStatusCodes.Contains(response.StatusCode);

        logger.Log(isUp ? LogLevel.Information : LogLevel.Critical, $"Twitter API is {(isUp ? "UP" : $"DOWN ({response.StatusCode})")}."  );

        return isUp;
    }

    public async Task GetSampleStreamAsync(CancellationToken cancellationToken)
    {
        // log
        var logHeader = $"{nameof(GetSampleStreamAsync)}";
        logger.LogInformation($"{logHeader} Starting. "); 

        // http call to Twitter stream headers
        using var response = await httpClient.GetAsync("/2/tweets/sample/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        
        // ensure success
        response.EnsureSuccessStatusCode();

        // log
        logger.LogInformation($"Twitter Sample Stream Headers succeeded. RateLimitRemaining: {response.Headers.GetValues("x-rate-limit-remaining").Single()}");

        // parse content stream
        var stream = new StreamReader(await response.Content.ReadAsStreamAsync());
        
        // init line
        var line = await stream.ReadLineAsync();

        // read to completion
        while (!string.IsNullOrEmpty(line))
        {
            // log line
            logger.LogTrace($"Tweet line read from stream: {line}");

            // deserialize line
            var tweet = JsonSerializer.Deserialize<Tweet>(line);

            // add to underlying collection
            Tweets.Add(tweet);

            // read next line
            line = await stream.ReadLineAsync();
        }

        // log
        logger.LogInformation($"{logHeader} Ended. ");

    }
}