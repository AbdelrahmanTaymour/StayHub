using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Conversations.StartConversation;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Conversations;

namespace StayHub.Application.UnitTests.Conversations;

public class StartConversationTests
{
    private const string InitialMessage = "Is the apartment pet-friendly?";
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IConversationRepository _conversationRepositoryMock = Substitute.For<IConversationRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly StartConversationCommandHandler _handler;
    private readonly IMessageRepository _messageRepositoryMock = Substitute.For<IMessageRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public StartConversationTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new StartConversationCommandHandler(
            _apartmentRepositoryMock,
            _conversationRepositoryMock,
            _messageRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(new StartConversationCommand(apartmentId, InitialMessage), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsTheApartmentOwner()
    {
        // Arrange — a host "messaging themselves" about their own listing;
        // surfaces Conversation.Start's own CannotMessageSelf guard through
        // the handler unchanged.
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(apartment.Id, apartment.OwnerId, apartment.OwnerId,
                Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        _userContextMock.UserId.Returns(apartment.OwnerId);

        // Act
        var result = await _handler.Handle(new StartConversationCommand(apartment.Id, InitialMessage), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.CannotMessageSelf);
    }

    [Fact]
    public async Task Handle_Should_ReuseExistingConversation_WhenOneAlreadyExistsBetweenParticipants()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var existingConversation = ConversationData.Start(apartment.Id, guestId, apartment.OwnerId);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(apartment.Id, guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns(existingConversation);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(new StartConversationCommand(apartment.Id, InitialMessage), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingConversation.Id);
        _conversationRepositoryMock.DidNotReceive().Add(Arg.Any<Conversation>());
    }

    [Fact]
    public async Task Handle_Should_RegisterMessageOnExistingConversation_WhenReusing()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var existingConversation = ConversationData.Start(apartment.Id, guestId, apartment.OwnerId);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(apartment.Id, guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns(existingConversation);
        _userContextMock.UserId.Returns(guestId);

        // Act
        await _handler.Handle(new StartConversationCommand(apartment.Id, InitialMessage), default);

        // Assert
        existingConversation.LastMessageOnUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Handle_Should_CreateNewConversationAndSendMessage_WhenNoneExists()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(apartment.Id, guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(new StartConversationCommand(apartment.Id, InitialMessage), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _conversationRepositoryMock.Received(1).Add(Arg.Is<Conversation>(c =>
            c.Id == result.Value && c.ApartmentId == apartment.Id &&
            c.GuestId == guestId && c.OwnerId == apartment.OwnerId));
        _messageRepositoryMock.Received(1).Add(Arg.Is<Message>(m =>
            m.ConversationId == result.Value && m.SenderId == guestId && m.Body.Message == InitialMessage));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}