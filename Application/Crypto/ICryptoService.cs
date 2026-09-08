namespace Application.Crypto;

public interface ICryptoService
{
    
    /// <returns>Computed hash in Base64-URL format</returns>
    string Hs256Hash(string text, string key);

    string Base64UrlEncode(byte[] input);

    byte[] Base64UrlDecode(string input);
}