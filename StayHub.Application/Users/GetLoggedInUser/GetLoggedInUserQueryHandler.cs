using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Application.Users.GetUser;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetLoggedInUser;

internal sealed class GetLoggedInUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService) : IQueryHandler<GetLoggedInUserQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetLoggedInUserQuery request, CancellationToken cancellationToken)
    {
        var userId = userContext.UserId;

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               u.id AS Id,
                               u.first_name AS FirstName,
                               u.last_name AS LastName,
                               u.email AS Email,
                               p.avatar_key AS AvatarKey,
                               p.bio AS Bio,
                               p.phone_number AS PhoneNumber
                           FROM users u
                           LEFT JOIN user_profiles p ON p.user_id = u.id
                           WHERE u.id = @UserId
                           """;

        var row = await connection.QueryFirstOrDefaultAsync<UserRow>(
            sql,
            new { UserId = userId });

        if (row is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound);
        }

        return await ToUserResponseAsync(row, cancellationToken);
    }

    private async Task<UserResponse> ToUserResponseAsync(
        UserRow row,
        CancellationToken cancellationToken)
    {
        var avatarUrl = string.IsNullOrWhiteSpace(row.AvatarKey)
            ? null
            : await fileStorageService.GeneratePresignedUrlAsync(row.AvatarKey, cancellationToken);

        return new UserResponse
        {
            Id = row.Id,
            FirstName = row.FirstName,
            LastName = row.LastName,
            Email = row.Email,
            AvatarUrl = avatarUrl,
            Bio = row.Bio,
            PhoneNumber = row.PhoneNumber
        };
    }

    internal sealed class UserRow
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? AvatarKey { get; init; }
        public string? Bio { get; init; }
        public string? PhoneNumber { get; init; }
    }
}