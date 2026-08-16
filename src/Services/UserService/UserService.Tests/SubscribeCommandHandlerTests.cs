using Moq;
using UserService.Application.Commands.Subscribe;
using UserService.Application.Events;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Interfaces;
using Xunit;

namespace UserService.Tests.Application.Commands;

public class SubscribeCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private SubscribeCommandHandler CreateHandler()
    {
        _userRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        return new SubscribeCommandHandler(_userRepository.Object, _eventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidSubscription_AddsSubscriptionAndPublishesEvent()
    {
        var follower = User.Create("follower", "f@user.com", "hash");
        var followee = User.Create("followee", "e@user.com", "hash");

        _userRepository.Setup(r => r.GetByIdAsync(follower.Id)).ReturnsAsync(follower);
        _userRepository.Setup(r => r.GetByIdAsync(followee.Id)).ReturnsAsync(followee);
        _userRepository.Setup(r => r.SubscriptionExistsAsync(follower.Id, followee.Id)).ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new SubscribeCommand(follower.Id, followee.Id), CancellationToken.None);

        Assert.True(result.Success);
        _userRepository.Verify(r => r.AddSubscriptionAsync(follower.Id, followee.Id), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<UserSubscribedEvent>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadySubscribed_ThrowsValidationException()
    {
        var follower = User.Create("follower", "f@user.com", "hash");
        var followee = User.Create("followee", "e@user.com", "hash");

        _userRepository.Setup(r => r.GetByIdAsync(follower.Id)).ReturnsAsync(follower);
        _userRepository.Setup(r => r.GetByIdAsync(followee.Id)).ReturnsAsync(followee);
        _userRepository.Setup(r => r.SubscriptionExistsAsync(follower.Id, followee.Id)).ReturnsAsync(true);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new SubscribeCommand(follower.Id, followee.Id), CancellationToken.None));

        _userRepository.Verify(r => r.AddSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SelfSubscribe_ThrowsDomainException()
    {
        // Regression guard for the "Cannot subscribe to yourself" rule in User.RegisterSubscriptionTo
        var user = User.Create("solo", "solo@user.com", "hash");

        _userRepository.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepository.Setup(r => r.SubscriptionExistsAsync(user.Id, user.Id)).ReturnsAsync(false);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new SubscribeCommand(user.Id, user.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FollowerNotFound_ThrowsNotFoundException()
    {
        var followeeId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new SubscribeCommand(Guid.NewGuid(), followeeId), CancellationToken.None));
    }
}
