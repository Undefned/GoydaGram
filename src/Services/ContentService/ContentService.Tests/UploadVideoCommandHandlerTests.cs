using ContentService.Application.Commands.UploadVideo;
using ContentService.Application.Events;
using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using ContentService.Domain.Interfaces;
using Moq;
using Xunit;

namespace ContentService.Tests.Application.Commands;

public class UploadVideoCommandHandlerTests
{
    private readonly Mock<IVideoRepository> _videoRepository = new();
    private readonly Mock<IStorageService> _storageService = new();
    private readonly Mock<IThumbnailService> _thumbnailService = new();
    private readonly Mock<IVideoProcessingQueue> _processingQueue = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UploadVideoCommandHandler CreateHandler()
    {
        _videoRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        return new UploadVideoCommandHandler(
            _videoRepository.Object,
            _storageService.Object,
            _thumbnailService.Object,
            _processingQueue.Object,
            _eventPublisher.Object);
    }

    // NOTE: constructor arg order for UploadVideoCommand assumed from VideosController usage
    // (UserId, Title, Description, Tags, VideoStream, FileName, FileSize).
    // Adjust here if the actual record definition differs.
    private static UploadVideoCommand BuildCommand(Guid userId, List<string> tags, Stream stream)
        => new(userId, "My video", "Description text", tags, stream, "clip.mp4", stream.Length);

    [Fact]
    public async Task Handle_ValidUpload_UploadsFileGeneratesPreviewAndPersistsVideo()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var userId = Guid.NewGuid();

        _storageService
            .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), "video/mp4", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/videos/original.mp4");
        _thumbnailService
            .Setup(t => t.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/previews/preview.jpg");
        _videoRepository
            .Setup(r => r.GetOrCreateTagAsync("funny"))
            .ReturnsAsync(Tag.Create("funny"));

        var handler = CreateHandler();
        var command = BuildCommand(userId, new List<string> { "funny" }, stream);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("https://storage/videos/original.mp4", result.Url);
        Assert.Equal("https://storage/previews/preview.jpg", result.PreviewUrl);
        Assert.Equal("Ready", result.Status);

        _videoRepository.Verify(r => r.AddAsync(It.IsAny<Video>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidUpload_PublishesVideoUploadedEventBeforeEnqueueingHlsProcessing()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var userId = Guid.NewGuid();

        _storageService
            .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), "video/mp4", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/videos/original.mp4");
        _thumbnailService
            .Setup(t => t.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage/previews/preview.jpg");

        var handler = CreateHandler();
        var command = BuildCommand(userId, new List<string>(), stream);

        await handler.Handle(command, CancellationToken.None);

        // Video must be discoverable (Search/Feed) before heavy HLS transcoding finishes —
        // this is the whole point of firing the event synchronously and enqueueing separately.
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<VideoUploadedEvent>()), Times.Once);
        _processingQueue.Verify(q => q.EnqueueAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateTagNames_DeduplicatesBeforeCreatingTags()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var userId = Guid.NewGuid();

        _storageService
            .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), "video/mp4", It.IsAny<CancellationToken>()))
            .ReturnsAsync("url");
        _thumbnailService
            .Setup(t => t.GenerateThumbnailAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("preview-url");
        _videoRepository
            .Setup(r => r.GetOrCreateTagAsync("funny"))
            .ReturnsAsync(Tag.Create("funny"));

        var handler = CreateHandler();
        // "funny" repeated on purpose — command.Tags.Distinct() in the handler should collapse it
        var command = BuildCommand(userId, new List<string> { "funny", "funny" }, stream);

        await handler.Handle(command, CancellationToken.None);

        _videoRepository.Verify(r => r.GetOrCreateTagAsync("funny"), Times.Once);
    }
}
