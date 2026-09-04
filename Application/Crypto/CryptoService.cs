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

    public string HmacSha256Hash(string text, string key)
    {
        throw new NotImplementedException();
    }
}