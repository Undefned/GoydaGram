namespace ContentService.Application.Commands.DeleteVideo;

public class DeleteVideoResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    
    public DeleteVideoResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}