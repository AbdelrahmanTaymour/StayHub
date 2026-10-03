using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetLoggedInUser;

internal sealed class GetLoggedInUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService) : IQueryHandler<GetLoggedInUserQuery, LoggedInUserResponse>
{
    public async Task<Result<LoggedInUserResponse>> Handle(GetLoggedInUserQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               u.id AS Id,
                               u.first_name AS FirstName,
                               u.last_name AS LastName,
                               u.email AS Email,
                               r.name AS Role,
                               p.avatar_key AS AvatarKey,
                               p.bio AS Bio,
                               p.phone_number AS PhoneNumber
                           FROM users u
                           INNER JOIN user_roles ur ON ur.user_id = u.id
                           INNER JOIN roles r ON r.id = ur.role_id
                           LEFT JOIN user_profiles p ON p.user_id = u.id
                           WHERE u.id = @UserId
                           """;

        var row = await connection.QueryFirstOrDefaultAsync<UserRow>(
            sql,
            new { userContext.UserId });

        if (row is null)
        {
            return Result.Failure<LoggedInUserResponse>(UserErrors.NotFound);
        }

        return await ToUserResponseAsync(row, cancellationToken);
    }

    private async Task<LoggedInUserResponse> ToUserResponseAsync(
        UserRow row,
        CancellationToken cancellationToken)
    {
        var avatarUrl = string.IsNullOrWhiteSpace(row.AvatarKey)
            ? null
            : await fileStorageService.GeneratePresignedUrlAsync(row.AvatarKey, cancellationToken);

        return new LoggedInUserResponse
        {
            Id = row.Id,
            FirstName = row.FirstName,
            LastName = row.LastName,
            Email = row.Email,
            Role = row.Role,
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
        public string Role { get; init; } = string.Empty;
        public string? AvatarKey { get; init; }
        public string? Bio { get; init; }
        public string? PhoneNumber { get; init; }
    }
}