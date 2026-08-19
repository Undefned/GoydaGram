using MediatR;

namespace ContentService.Application.Commands.UnblockVideo;

public record UnblockVideoResult(
    bool Success,
    string Message
);