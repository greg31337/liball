using System;
using System.Security.Cryptography;

namespace odf;

internal static class AesCbcCipher
{
    const int BlockSize = 16;

    internal static byte[] CreateInitializationVector()
    {
        return RandomNumberGenerator.GetBytes(BlockSize);
    }

    internal static byte[] EncryptPadded(
        SecurityBuffer plaintext,
        SecurityBuffer key,
        byte[] initializationVector,
        OdfEncryptionMethod encryptionMethod)
    {
        ValidateParameters(key, initializationVector, encryptionMethod);
        byte[] input = plaintext.GetOperationBytes();
        int paddingLength = BlockSize - input.Length % BlockSize;
        byte[] padded = new byte[input.Length + paddingLength];
        Array.Copy(input, padded, input.Length);
        if (paddingLength > 1)
        {
            RandomNumberGenerator.Fill(
                padded.AsSpan(input.Length, paddingLength - 1));
        }
        padded[padded.Length - 1] = (byte)paddingLength;

        byte[] encrypted;
        using (Aes aes = Aes.Create())
        {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = key.GetOperationBytes();
            aes.IV = initializationVector;
            using (ICryptoTransform transform = aes.CreateEncryptor())
            {
                encrypted = transform.TransformFinalBlock(
                    padded,
                    0,
                    padded.Length);
            }
        }
        SecurityBuffer.ClearBytes(padded);
        return encrypted;
    }

    internal static SecurityBuffer DecryptPadded(
        byte[] ciphertext,
        SecurityBuffer key,
        byte[] initializationVector,
        OdfEncryptionMethod encryptionMethod)
    {
        ValidateParameters(key, initializationVector, encryptionMethod);
        if (ciphertext.Length == 0 || ciphertext.Length % BlockSize != 0)
        {
            throw new OdfSecurityException("AES-CBC ciphertext must contain complete 16-byte blocks.");
        }

        byte[] padded;
        using (Aes aes = Aes.Create())
        {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = key.GetOperationBytes();
            aes.IV = initializationVector;
            using (ICryptoTransform transform = aes.CreateDecryptor())
            {
                padded = transform.TransformFinalBlock(
                    ciphertext,
                    0,
                    ciphertext.Length);
            }
        }

        int paddingLength = padded[padded.Length - 1];
        if (paddingLength <= 0
            || paddingLength > BlockSize
            || paddingLength > padded.Length)
        {
            SecurityBuffer.ClearBytes(padded);
            throw new OdfSecurityException("AES-CBC ciphertext has invalid XML Encryption padding.");
        }

        int resultLength = padded.Length - paddingLength;
        byte[] unpadded = new byte[resultLength];
        Array.Copy(padded, unpadded, resultLength);
        SecurityBuffer.ClearBytes(padded);
        SecurityBuffer result = new SecurityBuffer(unpadded);
        SecurityBuffer.ClearBytes(unpadded);
        return result;
    }

    static void ValidateParameters(
        SecurityBuffer key,
        byte[] initializationVector,
        OdfEncryptionMethod encryptionMethod)
    {
        if (initializationVector.Length != BlockSize)
        {
            throw new OdfSecurityException("ODF AES-CBC requires a 16-byte initialization vector.");
        }
        int requiredKeySize = GetRequiredKeySize(encryptionMethod);
        if (key.GetLength() != requiredKeySize)
        {
            throw new OdfSecurityException("The AES key size does not match the ODF encryption method.");
        }
    }

    static int GetRequiredKeySize(OdfEncryptionMethod encryptionMethod)
    {
        if (encryptionMethod == OdfEncryptionMethod.Aes128Cbc)
        {
            return 16;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes192Cbc)
        {
            return 24;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes256Cbc)
        {
            return 32;
        }
        throw new OdfSecurityException("The requested ODF encryption method is not AES-CBC.");
    }
}
