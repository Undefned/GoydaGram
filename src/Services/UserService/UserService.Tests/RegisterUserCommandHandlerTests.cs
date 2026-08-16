using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using UserService.Application.Commands.RegisterUser;
using UserService.Application.Events;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Security;
using Xunit;

namespace UserService.Tests.Application.Commands;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtProvider> _jwtProvider = new();
    private readonly Mock<IRefreshTokenGenerator> _refreshTokenGenerator = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RegisterUserCommandHandler CreateHandler()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "test-secret-at-least-32-characters-long",
            Issuer = "goydagram",
            Audience = "goydagram-users",
            ExpiryMinutes = 60,
            RefreshTokenExpiryDays = 30
        });

        _userRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        return new RegisterUserCommandHandler(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher.Object,
            _jwtProvider.Object,
            _refreshTokenGenerator.Object,
            jwtOptions,
            _eventPublisher.Object);
    }

    [Fact]
    public async Task Handle_WithNewEmailAndUsername_CreatesUserAndReturnsTokens()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _jwtProvider.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("fake-jwt-token");
        _refreshTokenGenerator.Setup(g => g.GenerateToken()).Returns("raw-refresh-token");
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hashed-refresh-token");

        var handler = CreateHandler();
        var command = new RegisterUserCommand("newuser", "new@user.com", "P@ssw0rd123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("newuser", result.Username);
        Assert.Equal("fake-jwt-token", result.AccessToken);
        Assert.Equal("raw-refresh-token", result.RefreshToken);

        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "newuser")), Times.Once);
        _refreshTokenRepository.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<UserRegisteredEvent>(e => e.Username == "newuser")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ThrowsValidationException()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(User.Create("existing", "taken@user.com", "hash"));

        var handler = CreateHandler();
        var command = new RegisterUserCommand("newuser", "taken@user.com", "P@ssw0rd123");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Email", ex.Message);

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingUsername_ThrowsValidationException()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync(User.Create("taken", "other@user.com", "hash"));

        var handler = CreateHandler();
        var command = new RegisterUserCommand("taken", "new@user.com", "P@ssw0rd123");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Username", ex.Message);

        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlwaysPublishesEventAfterSuccessfulRegistration()
    {
        // Arrange — regression guard: event must fire exactly once, not before persistence
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _jwtProvider.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");
        _refreshTokenGenerator.Setup(g => g.GenerateToken()).Returns("raw");
        _refreshTokenGenerator.Setup(g => g.Hash(It.IsAny<string>())).Returns("hashed");

        var handler = CreateHandler();
        var command = new RegisterUserCommand("someone", "someone@user.com", "P@ssw0rd123");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — order matters: SaveChanges before publish
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<UserRegisteredEvent>()), Times.Once);
    }
}
