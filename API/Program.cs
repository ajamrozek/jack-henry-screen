using Logic;
using Polly.Extensions.Http;
using Polly;
using Twitter.Repo;
using Twitter.Repo.Abstractions;
using WorkerService;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ITweetRepository, TweetRepository>("twitter", _ =>
{
    _.Timeout = TimeSpan.FromSeconds(3);
    _.BaseAddress = new Uri("https://api.twitter.com");
    _.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration.GetSection("ApiToken").Value}");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddPolicyHandler((provider, request) => GetRetryPolicy(provider));

builder.Services.AddSingleton<TweetMonitor>();
builder.Services.AddHostedService(_=>_.GetRequiredService<QueuedWorker>());

builder.Services.AddSingleton<QueuedWorker>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ =>
{
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
    {
        queueCapacity = 3;
    }

    var logger = _.GetService<ILogger<DefaultBackgroundTaskQueue>>();

    return new DefaultBackgroundTaskQueue(queueCapacity, logger);
}); 
builder.Services.AddSingleton<TweetRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
var serviceProvider = builder.Services.BuildServiceProvider();
TweetMonitor monitor = serviceProvider.GetService<TweetMonitor>();

app.MapGet("/stats", () =>
{
    var tweetRepo = monitor.TweetRepository;
    var topTenHashtags = TwitterStatsProcessor.GetTopTenHashTags(tweetRepo.Tweets
        .Select(_=>_?.data?.text)
        .ToArray());

    return new { count = tweetRepo.Tweets.Count, topTenHashtags };
})
.WithName("GetStats");

monitor.StartMonitor();

QueuedWorker worker = serviceProvider
     .GetServices<IHostedService>()
     .OfType<QueuedWorker>()
     .FirstOrDefault();

await worker.StartAsync(CancellationToken.None);

app.Run();

IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider policyServiceProvider)
{
    var retryCodes = new[] {
                System.Net.HttpStatusCode.NotFound,
                System.Net.HttpStatusCode.TooManyRequests};

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => retryCodes.Any(_ => _ == msg.StatusCode))
        .WaitAndRetryAsync(6,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (result, timeSpan, retryCount, context) =>
            {
                var logger = policyServiceProvider.GetService<ILogger<TweetRepository>>();
                logger.LogWarning($"Polly retry {retryCount} triggered. Retrying Twitter in {timeSpan.TotalSeconds} seconds.");
            });
}
