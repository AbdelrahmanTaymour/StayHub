using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Conversations.SendMessage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;

namespace StayHub.Application.UnitTests.Conversations;

public class SendMessageTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IConversationRepository _conversationRepositoryMock = Substitute.For<IConversationRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly SendMessageCommandHandler _handler;
    private readonly IMessageRepository _messageRepositoryMock = Substitute.For<IMessageRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public SendMessageTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new SendMessageCommandHandler(
            _conversationRepositoryMock,
            _messageRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenConversationNotFound()
    {
        // Arrange
        var conversationId = Guid.CreateVersion7();
        _conversationRepositoryMock.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        // Act
        var result = await _handler.Handle(new SendMessageCommand(conversationId, "Hello"), default);

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
        var result = await _handler.Handle(new SendMessageCommand(conversation.Id, "Hello"), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MessageErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_SendMessageRegisterAndSaveChanges_WhenCallerIsGuest()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var guestId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var conversation = ConversationData.Start(apartmentId, guestId, ownerId);
        _conversationRepositoryMock.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(new SendMessageCommand(conversation.Id, "Hello"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _messageRepositoryMock.Received(1).Add(Arg.Is<Message>(m =>
            m.Id == result.Value && m.SenderId == guestId && m.Body.Message == "Hello"));
        conversation.LastMessageOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SendMessage_WhenCallerIsOwner()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var guestId = Guid.CreateVersion7();
        var ownerId = Guid.CreateVersion7();
        var conversation = ConversationData.Start(apartmentId, guestId, ownerId);
        _conversationRepositoryMock.GetByIdAsync(conversation.Id, Arg.Any<CancellationToken>()).Returns(conversation);
        _userContextMock.UserId.Returns(ownerId);

        // Act
        var result = await _handler.Handle(new SendMessageCommand(conversation.Id, "Sure, happy to help!"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}