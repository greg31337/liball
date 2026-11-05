namespace odf;

internal static class OpenPgpKeyMaterial
{
    const int MinimumBlowfishKeySize = 4;
    const int MaximumBlowfishKeySize = 56;

    internal static SecurityBuffer AcquireEntryKey(
        IOdfOpenPgpKeyProvider provider,
        OdfEncryptedKey encryptedKey,
        OdfEncryptionMethod encryptionMethod)
    {
        if (provider == null)
        {
            throw new OdfSecurityException("An OpenPGP key provider is required.");
        }
        if (encryptedKey == null || !encryptedKey.IsValid)
        {
            throw new OdfSecurityException("Valid OpenPGP encrypted-key metadata is required.");
        }
        ValidateEncryptionMethod(encryptionMethod);

        byte[] keyBytes = provider.GetDecryptedKey(encryptedKey);
        if (keyBytes == null)
        {
            throw new OdfSecurityException("The OpenPGP key provider returned no key material.");
        }
        if (!HasValidKeySize(encryptionMethod, keyBytes.Length))
        {
            SecurityBuffer.ClearBytes(keyBytes);
            throw new OdfSecurityException("The supplied OpenPGP entry key size does not match the ODF encryption method.");
        }

        SecurityBuffer result = new SecurityBuffer(keyBytes);
        SecurityBuffer.ClearBytes(keyBytes);
        return result;
    }

    static void ValidateEncryptionMethod(OdfEncryptionMethod encryptionMethod)
    {
        if (encryptionMethod != OdfEncryptionMethod.BlowfishCfb
            && encryptionMethod != OdfEncryptionMethod.Aes128Cbc
            && encryptionMethod != OdfEncryptionMethod.Aes192Cbc
            && encryptionMethod != OdfEncryptionMethod.Aes256Cbc)
        {
            throw new OdfSecurityException("The ODF encryption method cannot use supplied OpenPGP key material.");
        }
    }

    static bool HasValidKeySize(
        OdfEncryptionMethod encryptionMethod,
        int keySize)
    {
        if (encryptionMethod == OdfEncryptionMethod.BlowfishCfb)
        {
            return keySize >= MinimumBlowfishKeySize
                && keySize <= MaximumBlowfishKeySize;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes128Cbc)
        {
            return keySize == 16;
        }
        if (encryptionMethod == OdfEncryptionMethod.Aes192Cbc)
        {
            return keySize == 24;
        }
        return keySize == 32;
    }
}
