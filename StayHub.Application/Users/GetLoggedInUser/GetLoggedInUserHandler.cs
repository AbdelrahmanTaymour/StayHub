using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Users.GetUser;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetLoggedInUser;

internal sealed class GetLoggedInUserHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext) : IQueryHandler<GetLoggedInQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetLoggedInQuery request, CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               u.id AS Id,
                               u.first_name AS FirstName,
                               u.last_name AS LastName,
                               u.email AS Email,
                               p.avatar_url AS AvatarUrl,
                               p.bio AS Bio,
                               p.phone_number AS PhoneNumber
                           FROM users u
                           LEFT JOIN user_profiles p ON p.user_id = u.id
                           WHERE u.id = @UserId
                           """;

        var user = await connection.QueryFirstOrDefaultAsync<UserResponse>(sql, new { userContext.UserId });

        return user ?? Result.Failure<UserResponse>(UserErrors.NotFound);
    }
}