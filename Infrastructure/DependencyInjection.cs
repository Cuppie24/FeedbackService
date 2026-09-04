using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configu)
    {
        return services;
    }
}