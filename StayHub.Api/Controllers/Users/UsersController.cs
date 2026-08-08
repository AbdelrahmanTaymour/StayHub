using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

namespace StayHub.Api.Controllers.Users;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetLoggedInUser(CancellationToken cancellationToken)
    {
        var query = new GetLoggedInUserQuery();

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.FirstName, request.LastName, request.Email, request.Password);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetUser), new { id = result.Value }, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> LogIn(LogInUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LogInUserCommand(request.Email, request.Password);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccessTokenResponse>> RefreshToken(
        RefreshAccessTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshAccessTokenCommand(request.RefreshToken);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> LogOut(LogOutUserRequest request, CancellationToken cancellationToken)
    {
        var command = new LogOutUserCommand(request.RefreshToken);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPut("{id:guid}/name")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateName(Guid id, UpdateUserNameRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserNameCommand(id, request.FirstName, request.LastName);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateProfile(
        Guid id,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserProfileCommand(id, request.AvatarUrl, request.Bio, request.PhoneNumber);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // ---- Sessions ----

    [HttpGet("{id:guid}/sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSessionResponse>>> GetSessions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetUserSessionsQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var command = new RevokeUserSessionCommand(sessionId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }
}