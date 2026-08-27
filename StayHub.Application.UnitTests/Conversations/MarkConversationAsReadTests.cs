using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Conversations.MarkConversationAsRead;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.UnitTests.Conversations;

public class MarkConversationAsReadTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IConversationRepository _conversationRepositoryMock = Substitute.For<IConversationRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly MarkConversationAsReadCommandHandler _handler;
    private readonly IMessageRepository _messageRepositoryMock = Substitute.For<IMessageRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public MarkConversationAsReadTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new MarkConversationAsReadCommandHandler(
            _conversationRepositoryMock,
            _messageRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static Message CreateUnreadMessage(Guid conversationId, Guid senderId) =>
        Message.Send(conversationId, senderId, new MessageBody("Hi"), DateTime.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenConversationNotFound()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        _conversationRepositoryMock.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        // Act
        var result = await _handler.Handle(new MarkConversationAsReadCommand(conversationId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotParticipant()
    {
        // Arrange
        var conversation = ConversationData.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());
        _conversationRepositoryMock.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());

        // Act
        var result = await _handler.Handle(new MarkConversationAsReadCommand(conversation.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_MarkAllUnreadMessagesAsReadAndSaveChanges_WhenUnreadMessagesExist()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var guestId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var conversation = ConversationData.Start(apartmentId, guestId, ownerId);
        var unreadMessage = CreateUnreadMessage(conversation.Id, ownerId);
        _conversationRepositoryMock.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        _userContextMock.UserId.Returns(guestId);
        _messageRepositoryMock
            .GetUnreadForRecipientAsync(conversation.Id, guestId, Arg.Any<CancellationToken>())
            .Returns([unreadMessage]);

        // Act
        var result = await _handler.Handle(new MarkConversationAsReadCommand(conversation.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        unreadMessage.ReadOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithoutSavingChanges_WhenNoUnreadMessages()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var guestId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var conversation = ConversationData.Start(apartmentId, guestId, ownerId);
        _conversationRepositoryMock.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        _userContextMock.UserId.Returns(guestId);
        _messageRepositoryMock
            .GetUnreadForRecipientAsync(conversation.Id, guestId, Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _handler.Handle(new MarkConversationAsReadCommand(conversation.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}