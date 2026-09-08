using System.Security.Cryptography;
using System.Text;

namespace Application.Crypto;

public class CryptoService : ICryptoService
{
    public string Md5Hash(string text)
    {
        var hasher = MD5.Create();
        var bytes = hasher.ComputeHash(Encoding.Default.GetBytes(text));
        var result = new StringBuilder();
        foreach (var b in bytes)
            result.Append(b.ToString("x2"));
        return result.ToString();
    }

    public string HS256Hash(string text, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Base64UrlEncode(bytes);
    }
    
    public string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public byte[] Base64UrlDecode(string input)
    {
        string padded = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}