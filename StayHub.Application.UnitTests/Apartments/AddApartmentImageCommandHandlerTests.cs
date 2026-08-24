using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Storage;
using StayHub.Application.Apartments.AddApartmentImage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.UnitTests.Apartments;

public class AddApartmentImageCommandHandlerTests
{
    private const string UploadedUrl = "https://cdn.stayhub.dev/uploaded.png";
    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IApartmentRepository _apartmentRepositoryMock = Substitute.For<IApartmentRepository>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
    private readonly IFileStorageService _fileStorageServiceMock = Substitute.For<IFileStorageService>();

    private readonly AddApartmentImageCommandHandler _handler;
    private readonly IApartmentImageRepository _imageRepositoryMock = Substitute.For<IApartmentImageRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public AddApartmentImageCommandHandlerTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);
        _fileStorageServiceMock
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(UploadedUrl);

        _handler = new AddApartmentImageCommandHandler(
            _apartmentRepositoryMock,
            _imageRepositoryMock,
            _fileStorageServiceMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static AddApartmentImageCommand CommandFor(Guid apartmentId) => new(
        ApartmentId: apartmentId,
        FileContent: new MemoryStream([1, 2, 3]),
        FileName: "photo.png",
        ContentType: "image/png",
        IsPrimary: false);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenApartmentNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        _apartmentRepositoryMock.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        // Act
        var result = await _handler.Handle(CommandFor(apartmentId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
        await _fileStorageServiceMock.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
        await _fileStorageServiceMock.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UploadFileCreateImageAndSaveChanges_WhenValid()
    {
        // Arrange
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _imageRepositoryMock.CountByApartmentId(apartment.Id, Arg.Any<CancellationToken>()).Returns(2);

        // Act
        var result = await _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _imageRepositoryMock.Received(1).Add(Arg.Is<ApartmentImage>(i =>
            i.Id == result.Value && i.Url.Value == UploadedUrl && i.DisplayOrder == 2));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteUploadedFileAndRethrow_WhenSaveChangesFails()
    {
        // Arrange — the compensating-action pattern: if the DB commit fails
        // after the file already landed in storage, the handler cleans up
        // the now-orphaned file rather than leaving it dangling.
        var apartment = ApartmentData.Create();
        _apartmentRepositoryMock.GetByIdAsync(apartment.Id, Arg.Any<CancellationToken>()).Returns(apartment);
        _userContextMock.UserId.Returns(apartment.OwnerId);
        _unitOfWorkMock.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        // Act
        var act = () => _handler.Handle(CommandFor(apartment.Id), default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _fileStorageServiceMock.Received(1).DeleteAsync(UploadedUrl, Arg.Any<CancellationToken>());
    }
}