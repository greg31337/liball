using token;

namespace token.Tests;

public class UnsupportedAlgorithmResultsTests
{
    [Test]
    public void SignatureOperationsReturnUnsupportedForKnownUnavailableAlgorithms()
    {
        TokenKey key = new TokenKey();
        TokenBytesResult hmac = HmacAlgorithms.Sign(
            Array.Empty<byte>(),
            key,
            JoseAlgorithms.MlDsa65);
        TokenBytesResult rsa = RsaAlgorithms.Sign(
            Array.Empty<byte>(),
            key,
            JoseAlgorithms.MlDsa65);
        TokenBytesResult ecdsa = EcdsaAlgorithms.Sign(
            Array.Empty<byte>(),
            key,
            JoseAlgorithms.MlDsa65);

        Assert.Multiple(() =>
        {
            AssertError(hmac.Error);
            AssertError(rsa.Error);
            AssertError(ecdsa.Error);
        });
    }

    [Test]
    public void SymmetricEncryptionOperationsReturnUnsupportedForKnownUnavailableAlgorithms()
    {
        TokenKey key = new TokenKey();
        TokenBytesResult aesKeyWrap = AesKeyWrap.Wrap(
            new byte[16],
            key,
            JoseAlgorithms.MlDsa65);
        AesGcmKeyWrapResult aesGcmKeyWrap = AesGcmKeyWrap.Wrap(
            new byte[16],
            key,
            JoseAlgorithms.MlDsa65);
        Pbes2KeyWrapResult pbes2 = Pbes2AlgorithmsOperations.Wrap(
            new byte[16],
            new byte[] { 1 },
            JoseAlgorithms.MlDsa65);
        DirectKeyManagementResult direct = DirectKeyManagement.Encrypt(
            key,
            JoseAlgorithms.MlDsa65);
        AesGcmContentEncryptionResult aesGcm = AesGcmContentEncryption.Encrypt(
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            key,
            JoseAlgorithms.MlDsa65);
        AesCbcHmacContentEncryptionResult aesCbc =
            AesCbcHmacContentEncryption.Encrypt(
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                key,
                JoseAlgorithms.MlDsa65);

        Assert.Multiple(() =>
        {
            AssertError(aesKeyWrap.Error);
            AssertError(aesGcmKeyWrap.Error);
            AssertError(pbes2.Error);
            AssertError(direct.Error);
            AssertError(aesGcm.Error);
            AssertError(aesCbc.Error);
        });
    }

    [Test]
    public void AsymmetricKeyManagementReturnsUnsupportedForEitherAlgorithmSlot()
    {
        TokenKey key = new TokenKey();
        RsaKeyManagementResult rsaKeyManagement = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.MlDsa65,
            JoseAlgorithms.Aes128Gcm);
        RsaKeyManagementResult rsaContentEncryption = RsaKeyManagement.Encrypt(
            key,
            JoseAlgorithms.RsaOaep,
            JoseAlgorithms.MlDsa65);
        EcdhKeyManagementResult ecdhKeyManagement = EcdhAlgorithms.Encrypt(
            key,
            JoseAlgorithms.MlDsa65,
            JoseAlgorithms.Aes128Gcm);
        EcdhKeyManagementResult ecdhContentEncryption = EcdhAlgorithms.Encrypt(
            key,
            JoseAlgorithms.EcdhEs,
            JoseAlgorithms.MlDsa65);

        Assert.Multiple(() =>
        {
            AssertError(rsaKeyManagement.Error);
            AssertError(rsaContentEncryption.Error);
            AssertError(ecdhKeyManagement.Error);
            AssertError(ecdhContentEncryption.Error);
        });
    }

    static void AssertError(TokenError error)
    {
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Code, Is.EqualTo("unsupported_algorithm"));
        Assert.That(error.Position, Is.EqualTo(-1));
    }
}
