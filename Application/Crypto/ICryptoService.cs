namespace Application.Crypto;

public interface ICryptoService
{
    string Md5Hash(string text);
    
    /// <returns>Computed hash in Base64-URL format</returns>
    string HmacSha256Hash(string text, string key);
}