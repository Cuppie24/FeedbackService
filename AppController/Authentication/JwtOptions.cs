using Microsoft.Extensions.Options;

namespace AppController.Authentication;

public abstract class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string IssuerKey { get; set; } = null!;
    public int ExpiresInMinutes { get; set; }
    public int RefreshExpiresInMinutes { get; set; }
    public string CookieName { get; set; } = null!;
    public string RefreshCookieName { get; set; } = null!;
}

public class JwtOptionsValidation : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if(string.IsNullOrEmpty(options.Issuer))
            return ValidateOptionsResult.Fail("Issuer is required.");
        if(string.IsNullOrEmpty(options.Audience))
            return ValidateOptionsResult.Fail("Audience is required.");
        if(string.IsNullOrEmpty(options.IssuerKey))
            return ValidateOptionsResult.Fail("IssuerKey is required.");
        if(string.IsNullOrEmpty(options.RefreshCookieName))
            return ValidateOptionsResult.Fail("RefreshCookieName is required.");
        if(string.IsNullOrEmpty(options.CookieName))
            return ValidateOptionsResult.Fail("CookieName is required.");
        if(options.ExpiresInMinutes <= 0)
            return ValidateOptionsResult.Fail("ExpiresInMinutes must be greater than zero.");
        if(options.RefreshExpiresInMinutes <= 0)
            return ValidateOptionsResult.Fail("RefreshExpiresInMinutes must be greater than zero.");
        return ValidateOptionsResult.Success;
    }
}