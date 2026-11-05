using System.Security.Cryptography;
using System.Text;

namespace iso20022;

internal static class GenerationStableHash
{
    internal static string CreateHex(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);
        return System.Convert.ToHexString(hash).ToLowerInvariant();
    }
}
