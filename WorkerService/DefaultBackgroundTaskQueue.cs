using System.Threading.Channels;

namespace WorkerService;

public class DefaultBackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ILogger<DefaultBackgroundTaskQueue> logger;

    public Channel<Func<CancellationToken, ValueTask>> Queue { private set; get; }

    public DefaultBackgroundTaskQueue(int capacity, 
        ILogger<DefaultBackgroundTaskQueue> logger)
    {
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        Queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        this.logger = logger;
    }

    public ValueTask<bool> QueueBackgroundWorkItemAsync(
        Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem is null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        return new ValueTask<bool>(!Queue.Writer.TryWrite(workItem));        
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, ValueTask>? workItem = await Queue.Reader.ReadAsync(cancellationToken);
         
        return workItem;
    }
}