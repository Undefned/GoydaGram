namespace ContentService.Application.Commands.DeleteAnyVideo;

public class DeleteAnyVideoResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    
    public DeleteAnyVideoResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}