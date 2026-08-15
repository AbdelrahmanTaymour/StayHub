using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Users.GetLoggedInUser;
using StayHub.Application.Users.GetUser;
using StayHub.Application.Users.GetUserSessions;
using StayHub.Application.Users.LogInUser;
using StayHub.Application.Users.LogOutUser;
using StayHub.Application.Users.RefreshAccessToken;
using StayHub.Application.Users.RegisterUser;
using StayHub.Application.Users.RevokeUserSession;
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
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("{id:guid}", GetUser)
            .HasPermission(Permissions.UserRead)
            .WithName(nameof(GetUser))
            .Produces<UserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

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

        group.MapPut("{id:guid}/name", UpdateName)
            .HasPermission(Permissions.UserUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("{id:guid}/profile", UpdateProfile)
            .HasPermission(Permissions.UserUpdate)
            .Produces(StatusCodes.Status204NoContent)
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

    private static async Task<IResult> UpdateName(
        Guid id,
        UpdateUserNameRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserNameCommand(id, request.FirstName, request.LastName);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> UpdateProfile(
        Guid id,
        UpdateUserProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserProfileCommand(id, request.AvatarUrl, request.Bio, request.PhoneNumber);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
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