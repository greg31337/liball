using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace odf;

internal class PackageEncryptionResult
{
    internal byte[] EncryptedBytes = [];
    internal byte[] InitializationVector = [];
    internal byte[] Checksum = [];
    internal int CompressedSize;
    internal long UncompressedSize;
}

internal static class PackageEncryption
{
    internal static PackageEncryptionResult EncryptBlowfishEntry(
        byte[] uncompressedData,
        SecurityBuffer key,
        string checksumType,
        OdfVersion version)
    {
        ValidateBlowfishVersion(version);
        PackageEncryptionResult result = new PackageEncryptionResult();
        result.InitializationVector = RandomNumberGenerator.GetBytes(8);
        result.UncompressedSize = uncompressedData.Length;
        using (SecurityBuffer compressed = CompressEntry(uncompressedData))
        using (BlowfishCipher cipher = new BlowfishCipher(key))
        {
            result.CompressedSize = compressed.GetLength();
            result.Checksum = PackageChecksum.CreateChecksum(
                compressed.GetOperationBytes(),
                checksumType);
            result.EncryptedBytes = cipher.EncryptCfb8(
                compressed.GetOperationBytes(),
                result.InitializationVector);
        }
        return result;
    }

    internal static byte[] DecryptBlowfishEntry(
        byte[] encryptedData,
        SecurityBuffer key,
        byte[] initializationVector,
        string checksumType,
        byte[] expectedChecksum,
        long expectedUncompressedSize,
        long maximumExpandedSize,
        OdfVersion version)
    {
        ValidateBlowfishVersion(version);
        using (BlowfishCipher cipher = new BlowfishCipher(key))
        {
            byte[] decrypted = cipher.DecryptCfb8(
                encryptedData,
                initializationVector);
            using (SecurityBuffer compressed = new SecurityBuffer(decrypted))
            {
                SecurityBuffer.ClearBytes(decrypted);
                if (!PackageChecksum.VerifyChecksum(
                    compressed.GetOperationBytes(),
                    checksumType,
                    expectedChecksum))
                {
                    throw new OdfSecurityException(
                        "The decrypted ODF entry checksum does not match.");
                }
                byte[] result = InflateEntry(
                    compressed.GetOperationBytes(),
                    maximumExpandedSize);
                if (expectedUncompressedSize >= 0
                    && result.LongLength != expectedUncompressedSize)
                {
                    SecurityBuffer.ClearBytes(result);
                    throw new OdfSecurityException(
                        "The decrypted ODF entry size does not match its manifest size.");
                }
                return result;
            }
        }
    }

    internal static PackageEncryptionResult EncryptAesEntry(
        byte[] uncompressedData,
        SecurityBuffer key,
        OdfEncryptionMethod encryptionMethod,
        string checksumType,
        OdfVersion version)
    {
        ValidateAesVersion(version);
        PackageEncryptionResult result = new PackageEncryptionResult();
        result.InitializationVector = AesCbcCipher.CreateInitializationVector();
        result.UncompressedSize = uncompressedData.Length;
        using (SecurityBuffer compressed = CompressEntry(uncompressedData))
        {
            result.CompressedSize = compressed.GetLength();
            result.Checksum = PackageChecksum.CreateChecksum(
                compressed.GetOperationBytes(),
                checksumType);
            result.EncryptedBytes = AesCbcCipher.EncryptPadded(
                compressed,
                key,
                result.InitializationVector,
                encryptionMethod);
        }
        return result;
    }

    internal static byte[] DecryptAesEntry(
        byte[] encryptedData,
        SecurityBuffer key,
        byte[] initializationVector,
        OdfEncryptionMethod encryptionMethod,
        string checksumType,
        byte[] expectedChecksum,
        long expectedUncompressedSize,
        long maximumExpandedSize,
        OdfVersion version)
    {
        ValidateAesVersion(version);
        using (SecurityBuffer compressed = AesCbcCipher.DecryptPadded(
            encryptedData,
            key,
            initializationVector,
            encryptionMethod))
        {
            if (!PackageChecksum.VerifyChecksum(
                compressed.GetOperationBytes(),
                checksumType,
                expectedChecksum))
            {
                throw new OdfSecurityException("The decrypted ODF entry checksum does not match.");
            }
            byte[] result = InflateEntry(
                compressed.GetOperationBytes(),
                maximumExpandedSize);
            if (expectedUncompressedSize >= 0
                && result.LongLength != expectedUncompressedSize)
            {
                SecurityBuffer.ClearBytes(result);
                throw new OdfSecurityException("The decrypted ODF entry size does not match its manifest size.");
            }
            return result;
        }
    }

    static SecurityBuffer CompressEntry(byte[] data)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (DeflateStream compressor = new DeflateStream(
                output,
                CompressionLevel.SmallestSize,
                true))
            {
                compressor.Write(data, 0, data.Length);
            }
            byte[] compressed = output.ToArray();
            SecurityBuffer result = new SecurityBuffer(compressed);
            SecurityBuffer.ClearBytes(compressed);
            return result;
        }
    }

    static byte[] InflateEntry(byte[] compressed, long maximumExpandedSize)
    {
        if (maximumExpandedSize < 0)
        {
            throw new OdfSecurityException("The maximum expanded entry size cannot be negative.");
        }
        using (MemoryStream input = new MemoryStream(compressed, false))
        using (DeflateStream inflater = new DeflateStream(
            input,
            CompressionMode.Decompress,
            false))
        using (MemoryStream output = new MemoryStream())
        using (SecurityBuffer buffer = new SecurityBuffer(new byte[8192]))
        {
            byte[] bufferBytes = buffer.GetOperationBytes();
            int count = inflater.Read(bufferBytes, 0, bufferBytes.Length);
            while (count != 0)
            {
                if (output.Length + count > maximumExpandedSize)
                {
                    throw new OdfSecurityException("The decrypted ODF entry exceeds its expansion limit.");
                }
                output.Write(bufferBytes, 0, count);
                count = inflater.Read(bufferBytes, 0, bufferBytes.Length);
            }
            return output.ToArray();
        }
    }

    static void ValidateAesVersion(OdfVersion version)
    {
        if (version != OdfVersion.Version12
            && version != OdfVersion.Version13
            && version != OdfVersion.Version14)
        {
            throw new OdfSecurityException("AES-CBC package encryption requires ODF 1.2 or later.");
        }
    }

    static void ValidateBlowfishVersion(OdfVersion version)
    {
        if (version != OdfVersion.Version10
            && version != OdfVersion.Version11
            && version != OdfVersion.Version12
            && version != OdfVersion.Version13
            && version != OdfVersion.Version14)
        {
            throw new OdfSecurityException(
                "Blowfish package encryption requires a supported ODF version.");
        }
    }
}
