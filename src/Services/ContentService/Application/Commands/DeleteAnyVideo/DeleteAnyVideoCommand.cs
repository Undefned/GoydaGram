using MediatR;

namespace ContentService.Application.Commands.DeleteAnyVideo;

public class DeleteAnyVideoCommand : IRequest<DeleteAnyVideoResult>
{
    public Guid VideoId { get; set; }
    public Guid UserId { get; set; } // Для проверки прав доступа
    
    public DeleteAnyVideoCommand(Guid videoId, Guid userId)
    {
        VideoId = videoId;
        UserId = userId;
    }
}

