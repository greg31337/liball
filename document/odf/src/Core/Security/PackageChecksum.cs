using System;
using System.Security.Cryptography;

namespace odf;

internal static class PackageChecksum
{
    const int PrefixLength = 1024;

    internal static byte[] CreateChecksum(
        byte[] compressedPlaintext,
        string algorithmName)
    {
        int length = compressedPlaintext.Length;
        bool usesPrefix = false;
        if (algorithmName == OdfAlgorithmIdentifiers.Sha1OneKilobyteName
            || algorithmName == OdfAlgorithmIdentifiers.Sha1OneKilobyte
            || algorithmName == OdfAlgorithmIdentifiers.Sha256OneKilobyte)
        {
            usesPrefix = true;
            if (length > PrefixLength)
            {
                length = PrefixLength;
            }
        }

        if (algorithmName == OdfAlgorithmIdentifiers.Sha1OneKilobyteName
            || algorithmName == OdfAlgorithmIdentifiers.Sha1OneKilobyte
            || algorithmName == OdfAlgorithmIdentifiers.Sha1)
        {
            return SHA1.HashData(compressedPlaintext.AsSpan(0, length));
        }
        if (algorithmName == OdfAlgorithmIdentifiers.Sha256OneKilobyte
            || algorithmName == OdfAlgorithmIdentifiers.Sha256)
        {
            return SHA256.HashData(compressedPlaintext.AsSpan(0, length));
        }
        if (!usesPrefix && algorithmName == OdfAlgorithmIdentifiers.Sha384)
        {
            return SHA384.HashData(compressedPlaintext);
        }
        if (!usesPrefix && algorithmName == OdfAlgorithmIdentifiers.Sha512)
        {
            return SHA512.HashData(compressedPlaintext);
        }
        throw new OdfSecurityException("The ODF encryption checksum algorithm is not supported.");
    }

    internal static bool VerifyChecksum(
        byte[] compressedPlaintext,
        string algorithmName,
        byte[] expectedChecksum)
    {
        byte[] actualChecksum = CreateChecksum(
            compressedPlaintext,
            algorithmName);
        bool matches = actualChecksum.Length == expectedChecksum.Length
            && CryptographicOperations.FixedTimeEquals(
                actualChecksum,
                expectedChecksum);
        SecurityBuffer.ClearBytes(actualChecksum);
        return matches;
    }
}
