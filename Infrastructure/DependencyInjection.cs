using Application.EntityServices.Feedback;
using Application.EntityServices.RefreshToken;
using Application.EntityServices.User;
using Dapper;
using Infrastructure.EntityServices.Feedback;
using Infrastructure.EntityServices.RefreshToken;
using Infrastructure.EntityServices.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var dbConnectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(dbConnectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required");

        // Dapper maps snake_case columns (user_id, is_revoked, created_at) onto
        // PascalCase entity properties. Columns whose name differs beyond casing
        // (m_user.PW, m_user.UserId -> User.Id) are still aliased explicitly in SQL.
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddMySqlDataSource(dbConnectionString);

        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();
        services.AddScoped<IFeedbackRepo, FeedbackRepo>();

        return services;
    }
}
