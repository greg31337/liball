using System;
using System.Security.Cryptography;

namespace odf;

internal static class Pbkdf2Derivation
{
    const int DefaultIterationCount = 1024;
    const int DefaultEntryKeySize = 16;
    const int Sha1Size = 20;
    const int Sha256Size = 32;
    const int MaximumBlowfishKeySize = 56;

    internal static SecurityBuffer CreateStartKey(
        string password,
        OdfVersion version,
        OdfStartKeyGeneration settings)
    {
        ValidateVersion(version);
        string algorithmName = settings.Name;
        bool useSha256 = false;
        int digestSize = Sha1Size;

        if (algorithmName.Length != 0 && !IsSha1Name(algorithmName))
        {
            if (IsSha256Name(algorithmName) && !IsLegacyVersion(version))
            {
                useSha256 = true;
                digestSize = Sha256Size;
            }
            else
            {
                throw new OdfSecurityException("The ODF start-key algorithm is not supported for this version.");
            }
        }

        int keySize = ResolveStartKeySize(settings, digestSize, version);
        using (SecurityBuffer passwordBytes = SecurityBuffer.CreateUtf8(password))
        {
            byte[] digest;
            if (useSha256)
            {
                digest = SHA256.HashData(passwordBytes.GetOperationBytes());
            }
            else
            {
                digest = SHA1.HashData(passwordBytes.GetOperationBytes());
            }

            byte[] selected = new byte[keySize];
            Array.Copy(digest, selected, keySize);
            SecurityBuffer.ClearBytes(digest);
            SecurityBuffer result = new SecurityBuffer(selected);
            SecurityBuffer.ClearBytes(selected);
            return result;
        }
    }

    internal static SecurityBuffer DeriveEntryKey(
        SecurityBuffer startKey,
        OdfVersion version,
        OdfKeyDerivation settings,
        OdfEncryptionMethod encryptionMethod)
    {
        ValidateVersion(version);
        if (settings.IsPgp
            || (settings.Name != OdfAlgorithmIdentifiers.Pbkdf2Name
                && settings.Name != OdfAlgorithmIdentifiers.Pbkdf2))
        {
            throw new OdfSecurityException("The ODF entry key does not use supported PBKDF2 derivation.");
        }
        if (!settings.SaltIsValid || settings.Salt.Length == 0)
        {
            throw new OdfSecurityException("PBKDF2 requires a valid non-empty salt.");
        }

        int iterationCount = ResolveIterationCount(settings);
        int keySize = ResolveEntryKeySize(settings);
        ValidateEncryptionKeySize(version, encryptionMethod, keySize);
        return DeriveHmacSha1(
            startKey.GetOperationBytes(),
            settings.Salt,
            iterationCount,
            keySize);
    }

    internal static SecurityBuffer DeriveHmacSha1(
        byte[] input,
        byte[] salt,
        int iterationCount,
        int keySize)
    {
        if (iterationCount <= 0)
        {
            throw new OdfSecurityException("PBKDF2 iteration count must be positive.");
        }
        if (keySize <= 0)
        {
            throw new OdfSecurityException("PBKDF2 key size must be positive.");
        }

        byte[] derived = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            iterationCount,
            HashAlgorithmName.SHA1,
            keySize);
        SecurityBuffer result = new SecurityBuffer(derived);
        SecurityBuffer.ClearBytes(derived);
        return result;
    }

    static void ValidateVersion(OdfVersion version)
    {
        if (version == OdfVersion.Unknown || version == OdfVersion.Future)
        {
            throw new OdfSecurityException("Key derivation requires a supported ODF version.");
        }
    }

    static bool IsLegacyVersion(OdfVersion version)
    {
        return version == OdfVersion.Version10 || version == OdfVersion.Version11;
    }

    static bool IsSha1Name(string algorithmName)
    {
        return algorithmName == OdfAlgorithmIdentifiers.Sha1Name
            || algorithmName == OdfAlgorithmIdentifiers.Sha1;
    }

    static bool IsSha256Name(string algorithmName)
    {
        return algorithmName == OdfAlgorithmIdentifiers.Sha256Odf;
    }

    static int ResolveStartKeySize(
        OdfStartKeyGeneration settings,
        int digestSize,
        OdfVersion version)
    {
        int keySize = Sha1Size;
        if (settings.HasKeySize)
        {
            if (!settings.KeySizeIsValid || settings.KeySize <= 0)
            {
                throw new OdfSecurityException("The ODF start-key size is invalid.");
            }
            keySize = settings.KeySize;
        }
        if (keySize > digestSize)
        {
            throw new OdfSecurityException("The ODF start-key size exceeds the digest size.");
        }
        if (IsLegacyVersion(version) && keySize != Sha1Size)
        {
            throw new OdfSecurityException("ODF 1.0 and 1.1 require a 20-byte SHA-1 start key.");
        }
        return keySize;
    }

    static int ResolveIterationCount(OdfKeyDerivation settings)
    {
        if (!settings.HasIterationCount)
        {
            return DefaultIterationCount;
        }
        if (!settings.IterationCountIsValid || settings.IterationCount <= 0)
        {
            throw new OdfSecurityException("The PBKDF2 iteration count is invalid.");
        }
        return settings.IterationCount;
    }

    static int ResolveEntryKeySize(OdfKeyDerivation settings)
    {
        if (!settings.HasKeySize)
        {
            return DefaultEntryKeySize;
        }
        if (!settings.KeySizeIsValid || settings.KeySize <= 0)
        {
            throw new OdfSecurityException("The PBKDF2 key size is invalid.");
        }
        return settings.KeySize;
    }

    static void ValidateEncryptionKeySize(
        OdfVersion version,
        OdfEncryptionMethod encryptionMethod,
        int keySize)
    {
        if (IsLegacyVersion(version))
        {
            if (encryptionMethod != OdfEncryptionMethod.BlowfishCfb
                || keySize != DefaultEntryKeySize)
            {
                throw new OdfSecurityException("ODF 1.0 and 1.1 require a 16-byte Blowfish entry key.");
            }
            return;
        }
        if (encryptionMethod == OdfEncryptionMethod.BlowfishCfb)
        {
            if (keySize < 4 || keySize > MaximumBlowfishKeySize)
            {
                throw new OdfSecurityException("Blowfish keys must contain between 4 and 56 bytes.");
            }
            return;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes128Cbc && keySize == 16)
        {
            return;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes192Cbc && keySize == 24)
        {
            return;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes256Cbc && keySize == 32)
        {
            return;
        }
        throw new OdfSecurityException("The derived key size does not match the ODF encryption method.");
    }
}
