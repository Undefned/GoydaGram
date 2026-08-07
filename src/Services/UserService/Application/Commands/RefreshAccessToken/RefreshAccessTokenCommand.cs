using MediatR;

namespace UserService.Application.Commands.RefreshAccessToken;

public record RefreshAccessTokenCommand(string RefreshToken) : IRequest<RefreshAccessTokenResult>;

public record RefreshAccessTokenResult(
    string AccessToken,
    string RefreshToken
);
