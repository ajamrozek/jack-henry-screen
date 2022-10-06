using System.Threading.Channels;

namespace WorkerService;

public class DefaultBackgroundTaskQueue : IBackgroundTaskQueue
{
    public Channel<Func<CancellationToken, ValueTask>> Queue { private set; get; }

    public DefaultBackgroundTaskQueue()
    {
        BoundedChannelOptions options = new(1)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        Queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
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