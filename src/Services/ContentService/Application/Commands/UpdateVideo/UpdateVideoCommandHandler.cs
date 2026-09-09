using MediatR;
using ContentService.Domain.Interfaces;

namespace ContentService.Application.Commands.UpdateVideo;

public class UpdateVideoCommandHandler(IVideoRepository videoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateVideoCommand, UpdateVideoResult>
{
    public async Task<UpdateVideoResult> Handle(UpdateVideoCommand command, CancellationToken cancellationToken)
    {
        var video = await videoRepository.GetByIdAsync(command.VideoId);
        if (video is null)
            return new UpdateVideoResult(false, "Video not found");

        if (video.UserId != command.RequestingUserId)
            return new UpdateVideoResult(false, "You don't have permission to edit this video");

        video.UpdateDetails(command.Title, command.Description);

        if (command.Tags is not null)
        {
            video.ClearTags();
            foreach (var tagName in command.Tags)
            {
                var tag = await videoRepository.GetOrCreateTagAsync(tagName);
                video.AddTag(tag);
            }
        }

        await videoRepository.UpdateAsync(video);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateVideoResult(true, "Video updated successfully");
    }
}