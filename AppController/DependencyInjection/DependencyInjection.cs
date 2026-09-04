using System.Text;
using AppController.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AppController.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtOptions = config.GetSection("Jwt");
        ValidateJwtOptions(jwtOptions);
        
        var key = Encoding.UTF8.GetBytes(jwtOptions["IssuerSigningKey"]!);
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions["Issuer"],
                    ValidAudience = jwtOptions["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token) &&
                            context.Request.Cookies.TryGetValue(jwtOptions["CookieName"]!, out var cookieToken))
                        {
                            context.Token = cookieToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();
        return services;
    }

    private static void ValidateJwtOptions(IConfigurationSection jwtOptions)
    {
        if (string.IsNullOrWhiteSpace(jwtOptions["IssuerSigningKey"])
            || string.IsNullOrWhiteSpace(jwtOptions["Issuer"])
            || string.IsNullOrWhiteSpace(jwtOptions["Audience"])
            || string.IsNullOrWhiteSpace(jwtOptions["ExpireMinutes"])
            || string.IsNullOrWhiteSpace(jwtOptions["CookieName"])
            || string.IsNullOrWhiteSpace(jwtOptions["RefreshCookieName"]))
            throw new InvalidOperationException("Jwt options are required");
    }
}