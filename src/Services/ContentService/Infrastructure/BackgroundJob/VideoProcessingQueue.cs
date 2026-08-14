using System.Threading.Channels;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.BackgroundJobs;

// Singleton — очередь одна на весь процесс, шарится между HTTP-запросами (Enqueue)
// и фоновым воркером (Dequeue). Channel<T> потокобезопасен сам по себе, доп. блокировок не нужно.
public class VideoProcessingQueue : IVideoProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid videoId)
        => _channel.Writer.WriteAsync(videoId);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}