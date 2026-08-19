using MediatR;

namespace ContentService.Application.Commands.BlockVideo;
public record BlockVideoResult(
    bool Success,
    string Message,
    DateTime BlockedAt
);