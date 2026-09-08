namespace Application.Crypto;

public interface ICryptoService
{
    string Md5Hash(string text);
    
    /// <returns>Computed hash in Base64-URL format</returns>
    string HS256Hash(string text, string key);

    string Base64UrlEncode(byte[] input);

    byte[] Base64UrlDecode(string input);
}