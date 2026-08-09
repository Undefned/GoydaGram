namespace ContentService.Application.Interfaces;

public interface IVideoProcessingQueue
{
    ValueTask EnqueueAsync(Guid videoId);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}