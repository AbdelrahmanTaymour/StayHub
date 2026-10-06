using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Users.ForgotPassword;
using StayHub.Application.Users.GetLoggedInUser;
using StayHub.Application.Users.GetOwnerProfile;
using StayHub.Application.Users.GetUser;
using StayHub.Application.Users.GetUserSessions;
using StayHub.Application.Users.LogInUser;
using StayHub.Application.Users.LogOutUser;
using StayHub.Application.Users.RefreshAccessToken;
using StayHub.Application.Users.RegisterUser;
using StayHub.Application.Users.RevokeUserSession;
using StayHub.Application.Users.UpdateUserAvatar;
using StayHub.Application.Users.UpdateUserName;
using StayHub.Application.Users.UpdateUserProfile;

namespace StayHub.Api.Endpoints.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("users").WithTags("Users").RequireAuthorization();

        group.MapGet("me", GetLoggedInUser)
            .HasPermission(Permissions.UserRead)
            .Produces<LoggedInUserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("{id:guid}", GetUser)
            .HasPermission(Permissions.UserRead)
            .WithName(nameof(GetUser))
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("{ownerId:guid}/profile", GetOwnerProfile)
            .WithName(nameof(GetOwnerProfile))
            .Produces<UserProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPost("register", Register)
            .AllowAnonymous()
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("login", LogIn)
            .AllowAnonymous()
            .Produces<AccessTokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("refresh-token", RefreshToken)
            .AllowAnonymous()
            .Produces<AccessTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("logout", LogOut)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("forgot-password", ForgotPassword)
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        group.MapPut("profile", UpdateProfile)
            .HasPermission(Permissions.UserUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("profile/name", UpdateName)
            .HasPermission(Permissions.UserUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("profile/avatar", UpdateProfileAvatar)
            .DisableAntiforgery()
            .HasPermission(Permissions.UserUpdate)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Sessions ----

        group.MapGet("{id:guid}/sessions", GetSessions)
            .HasPermission(Permissions.UserManageSessions)
            .Produces<IReadOnlyList<UserSessionResponse>>();

        group.MapDelete("sessions/{sessionId:guid}", RevokeSession)
            .HasPermission(Permissions.UserManageSessions)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return builder;
    }

    private static async Task<IResult> GetLoggedInUser(ISender sender, IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLoggedInUserQuery(userContext), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetUser(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserQuery(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetOwnerProfile(Guid ownerId, ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOwnerProfileQuery(ownerId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> Register(
        RegisterUserRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.FirstName, request.LastName, request.Email, request.Password);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetUser), new { id = result.Value });
    }

    private static async Task<IResult> LogIn(
        LogInUserRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogInUserCommand(request.Email, request.Password), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> RefreshToken(
        RefreshAccessTokenRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> LogOut(
        LogOutUserRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogOutUserCommand(request.RefreshToken), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> ForgotPassword(
        ForgotPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> UpdateName(
        UpdateUserNameRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserNameCommand(request.FirstName, request.LastName);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> UpdateProfile(
        UpdateUserProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserProfileCommand(request.Bio, request.PhoneNumber);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> UpdateProfileAvatar(
        IFormFile file,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();

        var command = new UpdateUserAvatarCommand(stream, file.FileName, file.ContentType);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetSessions(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserSessionsQuery(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> RevokeSession(
        Guid sessionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeUserSessionCommand(sessionId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}