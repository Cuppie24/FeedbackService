using Application.EntityServices.User;
using Dapper;
using MySqlConnector;

namespace Infrastructure.EntityServices.User;

public class UserRepo(MySqlDataSource dataSource) : IUserRepo
{
    private const string SelectUser =
        "SELECT UserId AS Id, UserName AS Username, PW AS PasswordHash FROM refers.m_user";

    public async Task<Domain.Entities.User?> GetAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Domain.Entities.User>(
            $"{SelectUser} WHERE UserId = @id",
            new { id });
    }

    public async Task<Domain.Entities.User?> GetUserByUsername(string username)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Domain.Entities.User>(
            $"{SelectUser} WHERE UserName = @username",
            new { username });
    }
}
