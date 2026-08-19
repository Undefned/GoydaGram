using MediatR;
using ContentService.Application.Interfaces;
using ContentService.Domain.Enums;
using ContentService.Domain.Exceptions;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.BlockVideo;

public class BlockVideoCommandHandler(
    IVideoRepository videoRepository,
    ICacheService cacheService)
    : IRequestHandler<BlockVideoCommand, BlockVideoResult>
{
    public async Task<BlockVideoResult> Handle(BlockVideoCommand command, CancellationToken cancellationToken)
    {
        var video = await videoRepository.GetByIdAsync(command.VideoId);
        if (video == null)
            throw new NotFoundException("Video", command.VideoId);

        if (video.Status == VideoStatus.Blocked)
            throw new ValidationException("Video is already blocked");

        video.Block(command.Reason);
        await videoRepository.UpdateAsync(video);
        await videoRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        // Инвалидируем кеш
        await cacheService.RemoveAsync($"video:{command.VideoId}");
        await cacheService.RemoveAsync("trending:videos");

        return new BlockVideoResult(
            true,
            $"Video {command.VideoId} blocked successfully",
            video.BlockedAt ?? DateTime.UtcNow
        );
    }
}