using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Realtime;
using StayHub.Application.Conversations.StartConversation;
using StayHub.Application.UnitTests.Apartments;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Bookings;
using StayHub.Domain.Conversations;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Conversations;

public class StartConversationTests
{
    private const string InitialMessage = "Is the apartment pet-friendly?";
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IBookingRepository _bookingRepositoryMock = Substitute.For<IBookingRepository>();
    private readonly IConversationRepository _conversationRepositoryMock = Substitute.For<IConversationRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly StartConversationCommandHandler _handler;
    private readonly IMessageRepository _messageRepositoryMock = Substitute.For<IMessageRepository>();

    private readonly IRealtimeConversationNotifier _realtimeConversationNotifierMock =
        Substitute.For<IRealtimeConversationNotifier>();

    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();

    public StartConversationTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _userRepositoryMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        _handler = new StartConversationCommandHandler(
            _userRepositoryMock,
            _apartmentRepositoryMock,
            _bookingRepositoryMock,
            _conversationRepositoryMock,
            _messageRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _realtimeConversationNotifierMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenRecipientDoesNotExist()
    {
        // Arrange
        var initiatorId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(initiatorId);
        _userRepositoryMock.ExistsAsync(recipientId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(recipientId, null, null, InitialMessage), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.RecipientNotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsTheRecipient()
    {
        // Arrange
        var sameUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(sameUserId);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(sameUserId, null, null, InitialMessage), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ConversationErrors.CannotMessageSelf);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentIdGivenButNotFound()
    {
        // Arrange
        var initiatorId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();
        var apartmentId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(initiatorId);
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(recipientId, apartmentId, null, InitialMessage), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_CreateConversationWithNoApartmentOrBooking_WhenNeitherIsGiven()
    {
        // Arrange
        var initiatorId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(initiatorId);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(initiatorId, recipientId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(recipientId, null, null, InitialMessage), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _conversationRepositoryMock.Received(1).Add(Arg.Is<Conversation>(c =>
            c.Id == result.Value &&
            c.InitiatorId == initiatorId &&
            c.RecipientId == recipientId &&
            c.ApartmentId == null &&
            c.BookingId == null));
        await _apartmentRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReuseExistingConversation_WhenOneAlreadyExistsBetweenParticipants()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var existingConversation = ConversationData.Start(guestId, apartment.OwnerId, apartment.Id);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns(existingConversation);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(apartment.OwnerId, apartment.Id, null, InitialMessage), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingConversation.Id);
        _conversationRepositoryMock.DidNotReceive().Add(Arg.Any<Conversation>());
    }

    [Fact]
    public async Task Handle_Should_AttachApartmentToExistingConversation_WhenReusingWithNewApartmentContext()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var existingConversation = ConversationData.Start(guestId, apartment.OwnerId, apartmentId: null);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns(existingConversation);
        _userContextMock.UserId.Returns(guestId);

        // Act
        await _handler.Handle(
            new StartConversationCommand(apartment.OwnerId, apartment.Id, null, InitialMessage), default);

        // Assert
        existingConversation.ApartmentId.Should().Be(apartment.Id);
    }

    [Fact]
    public async Task Handle_Should_RegisterMessageOnExistingConversation_WhenReusing()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        var guestId = Guid.CreateVersion7();
        var existingConversation = ConversationData.Start(guestId, apartment.OwnerId, apartment.Id);
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _conversationRepositoryMock
            .GetBetweenParticipantsAsync(guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns(existingConversation);
        _userContextMock.UserId.Returns(guestId);

        // Act
        await _handler.Handle(
            new StartConversationCommand(apartment.OwnerId, apartment.Id, null, InitialMessage), default);

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
            .GetBetweenParticipantsAsync(guestId, apartment.OwnerId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);
        _userContextMock.UserId.Returns(guestId);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(apartment.OwnerId, apartment.Id, null, InitialMessage), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _conversationRepositoryMock.Received(1).Add(Arg.Is<Conversation>(c =>
            c.Id == result.Value && c.ApartmentId == apartment.Id &&
            c.InitiatorId == guestId && c.RecipientId == apartment.OwnerId));
        _messageRepositoryMock.Received(1).Add(Arg.Is<Message>(m =>
            m.ConversationId == result.Value && m.SenderId == guestId && m.Body.Message == InitialMessage));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnBookingNotFound_WhenBookingIdGivenButNotFound()
    {
        // Arrange
        var initiatorId = Guid.CreateVersion7();
        var recipientId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(initiatorId);
        _bookingRepositoryMock.GetByIdAsync(bookingId, Arg.Any<CancellationToken>()).Returns((Booking?)null);

        // Act
        var result = await _handler.Handle(
            new StartConversationCommand(recipientId, null, bookingId, InitialMessage), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(BookingErrors.NotFound);
    }
}