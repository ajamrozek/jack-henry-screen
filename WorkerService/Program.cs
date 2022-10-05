using WorkerService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<QueuedWorker>();
    })
    .Build();

await host.RunAsync();
