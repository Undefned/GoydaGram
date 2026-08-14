using MediatR;

namespace ContentService.Application.Commands.DeleteVideo;

public class DeleteVideoCommand : IRequest<DeleteVideoResult>
{
    public Guid VideoId { get; set; }
    public Guid UserId { get; set; } // Для проверки прав доступа
    
    public DeleteVideoCommand(Guid videoId, Guid userId)
    {
        VideoId = videoId;
        UserId = userId;
    }
}

