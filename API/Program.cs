using Logic;
using Polly.Extensions.Http;
using Polly;
using Twitter.Repo;
using Twitter.Repo.Abstractions;
using WorkerService;

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
    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
    {
        queueCapacity = 3;
    }

    return new DefaultBackgroundTaskQueue(queueCapacity);
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
var monitor = serviceProvider.GetService<TweetMonitor>();
monitor.StartMonitor();

// start the queuedWorker
QueuedWorker worker = serviceProvider
     .GetServices<IHostedService>()
     .OfType<QueuedWorker>()
     .FirstOrDefault();

await worker.StartAsync(CancellationToken.None);

// setup single API Endpoint
app.MapGet("/stats", () =>
{
    var tweetRepo = monitor.TweetRepository;
    var topTenHashtags = TwitterStatsProcessor.GetTopTenHashTags(tweetRepo.Tweets
        .Select(_ => _?.data?.text)
        .ToArray());

    return new { count = tweetRepo.Tweets.Count, topTenHashtags };
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
                var logger = policyServiceProvider.GetService<ILogger<TweetRepository>>();
                logger.LogWarning($"Polly retry {retryCount} triggered by {result.Result.StatusCode}: {result.Result.ReasonPhrase}. \nRetrying Twitter in {timeSpan.TotalSeconds} seconds.");
            });
}

