using MediatR;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Exceptions;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.UnblockVideo;

public class UnblockVideoCommandHandler(
    IVideoRepository videoRepository,
    ICacheService cacheService)
    : IRequestHandler<UnblockVideoCommand, UnblockVideoResult>
{
    public async Task<UnblockVideoResult> Handle(UnblockVideoCommand command, CancellationToken cancellationToken)
    {
        var video = await videoRepository.GetByIdAsync(command.VideoId);
        if (video == null)
            throw new NotFoundException("Video", command.VideoId);

        if (video.Status != VideoStatus.Blocked)
            throw new ValidationException("Video is not blocked");

        video.Unblock();
        await videoRepository.UpdateAsync(video);
        await videoRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // Инвалидируем кеш
        await cacheService.RemoveAsync($"video:{command.VideoId}");
        await cacheService.RemoveAsync("trending:videos");

        return new UnblockVideoResult(
            true,
            $"Video {command.VideoId} unblocked successfully"
        );
    }
}