using System;
using System.Security.Cryptography;

namespace argon2;

internal static class RandomSaltGenerator
{
    internal static byte[] CreateSalt(int length)
    {
        if (length < Argon2Limits.MinimumSaltLength || length > Array.MaxLength)
        {
            throw new Argon2ParameterException("Random salt length is outside the supported range.");
        }

        byte[] salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }
}
