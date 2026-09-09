using Microsoft.Extensions.Options;

namespace AppController.Authentication;

public class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string IssuerSigningKey { get; set; } = null!;
    public int ExpireMinutes { get; set; }
    public int RefreshExpireMinutes { get; set; }
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
        if(string.IsNullOrEmpty(options.IssuerSigningKey))
            return ValidateOptionsResult.Fail("IssuerSigningKey is required.");
        if(string.IsNullOrEmpty(options.RefreshCookieName))
            return ValidateOptionsResult.Fail("RefreshCookieName is required.");
        if(string.IsNullOrEmpty(options.CookieName))
            return ValidateOptionsResult.Fail("CookieName is required.");
        if(options.ExpireMinutes <= 0)
            return ValidateOptionsResult.Fail("ExpireMinutes must be greater than zero.");
        if(options.RefreshExpireMinutes <= 0)
            return ValidateOptionsResult.Fail("RefreshExpiresMinutes must be greater than zero.");
        return ValidateOptionsResult.Success;
    }
}
