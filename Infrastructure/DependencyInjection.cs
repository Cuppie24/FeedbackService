using Application.EntityServices.RefreshToken;
using Application.EntityServices.User;
using Infrastructure.Database;
using Infrastructure.EntityServices.RefreshToken;
using Infrastructure.EntityServices.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(9, 2, 0))));

        services.AddScoped<IUserRepo, UserRepo>();
        services.AddScoped<IRefreshTokenRepo, RefreshTokenRepo>();

        return services;
    }
}
