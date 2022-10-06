using Logic;
using Polly.Extensions.Http;
using Polly;
using Twitter.Repo;
using Twitter.Repo.Abstractions;
using WorkerService;
using Repo.Entities;

var builder = WebApplication.CreateBuilder(args);

// add Twitter httpClient for DI
builder.Services.AddHttpClient<ITweetRepository, TweetRepository>("twitter", _ =>
{
    if (!int.TryParse(builder.Configuration["Twitter:Timeout"], out var timeout))
    {
        timeout = 3;
    }
    _.Timeout = TimeSpan.FromSeconds(timeout);
    _.BaseAddress = new Uri("https://api.twitter.com");
    _.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration.GetSection("Twitter:ApiToken").Value}");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddPolicyHandler((provider, request) => GetNominalRetryPolicy(provider));

// add monitor for DI
builder.Services.AddSingleton<TweetMonitor>();

// add queuedworker as hosted service
builder.Services.AddHostedService(_=>_.GetRequiredService<QueuedWorker>());

// add queuedworker for DI
builder.Services.AddSingleton<QueuedWorker>();

// add bgTaskQueue for DI
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ =>
{
   return new DefaultBackgroundTaskQueue(1);
}); 

// add TweetRepo for DI
builder.Services.AddSingleton<TweetRepository>();

// add EndpointsExplorer for DI
builder.Services.AddEndpointsApiExplorer();

// add SwaggerGen
builder.Services.AddSwaggerGen();

// build the app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// build a servicelocator instance to handle some bootstrap ops
var serviceProvider = builder.Services.BuildServiceProvider();

// init the monitor and start consuming the Twitter stream
var monitor = serviceProvider.GetService<TweetMonitor>()!;
monitor.StartMonitor();

// start the queuedWorker
QueuedWorker worker = serviceProvider
     .GetServices<IHostedService>()
     .OfType<QueuedWorker>()
     .FirstOrDefault()!;

await worker.StartAsync(CancellationToken.None);

// setup single API Endpoint
// This is the only owner of statistic invocations. Other mechanisms may be considered that involve persistence that enable this endpoint to read those sources and abandon calulating the stats itself.
app.MapGet("/stats", () =>
{
    var tweetRepo = monitor.TweetRepository;
    var tweetTexts = tweetRepo.Tweets.Select(_ => _?.data?.text).ToArray();
    var topTenHashtags = TwitterStatsProcessor.GetTop(tweetTexts!, @"\#\w+");
    var topTenMentions = TwitterStatsProcessor.GetTop(tweetTexts!, @"\@\w+");

    var statResult = new Statistic()
    {
        Count = tweetRepo.Tweets.Count,
        TopTenHashtags = topTenHashtags.ToArray(),
        TopTenMentions = topTenMentions.ToArray(),
        AsOf = DateTime.UtcNow
    };

    return statResult;
})
.WithName("GetStats");

// run the app
app.Run();

// encapsulate the Polly Retry Policy setup 
IAsyncPolicy<HttpResponseMessage> GetNominalRetryPolicy(IServiceProvider policyServiceProvider)
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
                var logger = policyServiceProvider.GetService<ILogger<TweetRepository>>()!;
                logger.LogWarning($"Polly retry {retryCount} triggered by {result.Result.StatusCode}: {result.Result.ReasonPhrase}. \nRetrying Twitter in {timeSpan.TotalSeconds} seconds.");
            });
}

