using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Users.GetUser;

internal sealed class GetUserQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory) : IQueryHandler<GetUserQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
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

        var user = await connection.QueryFirstOrDefaultAsync<UserResponse>(sql, new { request.UserId });

        return user ?? Result.Failure<UserResponse>(UserErrors.NotFound);
    }
}