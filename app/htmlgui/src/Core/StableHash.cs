using System;
using System.Security.Cryptography;
using System.Text;

namespace htmlgui;

internal static class StableHash
{
    internal static string HashText(string? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static string HashBytes(byte[] value)
    {
        Guard.ArgumentNotNull(value, "value");
        byte[] hash = SHA256.HashData(value);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static string HashParts(string[] parts)
    {
        Guard.ArgumentNotNull(parts, "parts");
        StringBuilder material = new StringBuilder();
        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index];
            if (part == null)
            {
                throw new ArgumentException("Hash parts cannot contain null.", "parts");
            }

            material.Append(InvariantFormatter.FormatInt32(part.Length));
            material.Append(':');
            material.Append(part);
            material.Append(';');
            index++;
        }

        return HashText(material.ToString());
    }

    internal static string ShortToken(string? value, int length)
    {
        Guard.ArgumentInRange(length, 1, 64, "length");
        string hash = HashText(value);
        return hash.Substring(0, length);
    }
}
