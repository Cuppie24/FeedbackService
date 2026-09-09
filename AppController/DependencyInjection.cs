using System.Text;
using AppController.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppController;

public static class DependencyInjection
{
    public const string CorsPolicyName = "FeedbackCors";

    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors:AllowedOrigins is required");

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });
        return services;
    }
    
    
    public static IServiceCollection AddControllerServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthNService, AuthNService>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        return services;
    }
    
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwtOptions = config.GetSection("Jwt");
        ValidateJwtOptions(jwtOptions);
        
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidation>();
        services.AddOptionsWithValidateOnStart<JwtOptions>()
            .BindConfiguration("Jwt");
        
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