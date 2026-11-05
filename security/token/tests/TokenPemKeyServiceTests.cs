using System.Security.Cryptography;
using token;

namespace token.Tests;

public class TokenPemKeyServiceTests
{
    [Test]
    public void RsaPemPrivateAndPublicKeysRoundTripAndPerformOperations()
    {
        ITokenPemKeyService service = new TokenPemKeyService();
        using RSA source = RSA.Create(2048);
        TokenKeyResult privateResult = service.ImportRsaPrivateKeyPem(source.ExportPkcs8PrivateKeyPem());
        TokenTextResult privatePem = service.ExportRsaPrivateKeyPem(privateResult.Key!);
        TokenTextResult publicPem = service.ExportRsaPublicKeyPem(privateResult.Key!);
        TokenKeyResult publicResult = service.ImportRsaPublicKeyPem(publicPem.Text!);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("RSA PEM round trip");
        byte[] signature = privateResult.Key!.Rsa!.SignData(
            data,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);

        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Success, Is.True);
            Assert.That(privateResult.Key.OwnsNativeKey, Is.True);
            Assert.That(privateResult.Key.NativePrivateMaterial, Is.True);
            Assert.That(privatePem.Text, Does.StartWith("-----BEGIN PRIVATE KEY-----"));
            Assert.That(publicPem.Text, Does.StartWith("-----BEGIN PUBLIC KEY-----"));
            Assert.That(publicResult.Success, Is.True);
            Assert.That(publicResult.Key!.NativePrivateMaterial, Is.False);
            Assert.That(
                publicResult.Key.Rsa!.VerifyData(
                    data,
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss),
                Is.True);
        });

        service.ReleaseImportedKey(privateResult.Key);
        service.ReleaseImportedKey(publicResult.Key!);
        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Key.Rsa, Is.Null);
            Assert.That(privateResult.Key.OwnsNativeKey, Is.False);
            Assert.That(publicResult.Key!.Rsa, Is.Null);
        });
    }

    [Test]
    public void EcdsaPemPrivateAndPublicKeysRoundTripAndPerformOperations()
    {
        ITokenPemKeyService service = new TokenPemKeyService();
        using ECDsa source = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKeyResult privateResult = service.ImportEcdsaPrivateKeyPem(source.ExportPkcs8PrivateKeyPem());
        TokenTextResult privatePem = service.ExportEcdsaPrivateKeyPem(privateResult.Key!);
        TokenTextResult publicPem = service.ExportEcdsaPublicKeyPem(privateResult.Key!);
        TokenKeyResult publicResult = service.ImportEcdsaPublicKeyPem(publicPem.Text!);
        byte[] data = System.Text.Encoding.UTF8.GetBytes("ECDSA PEM round trip");
        byte[] signature = privateResult.Key!.Ecdsa!.SignData(data, HashAlgorithmName.SHA384);

        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Success, Is.True);
            Assert.That(privateResult.Key.KeyType, Is.EqualTo(TokenKeyTypes.EllipticCurve));
            Assert.That(privateResult.Key.Curve, Is.EqualTo("P-384"));
            Assert.That(privatePem.Text, Does.StartWith("-----BEGIN PRIVATE KEY-----"));
            Assert.That(publicPem.Text, Does.StartWith("-----BEGIN PUBLIC KEY-----"));
            Assert.That(publicResult.Success, Is.True);
            Assert.That(publicResult.Key!.Ecdsa!.VerifyData(data, signature, HashAlgorithmName.SHA384), Is.True);
        });

        service.ReleaseImportedKey(privateResult.Key);
        service.ReleaseImportedKey(publicResult.Key!);
        Assert.Multiple(() =>
        {
            Assert.That(privateResult.Key.Ecdsa, Is.Null);
            Assert.That(publicResult.Key!.Ecdsa, Is.Null);
        });
    }

    [Test]
    public void PemServiceImportsTraditionalRsaAndEcPrivateKeyLabels()
    {
        ITokenPemKeyService service = new TokenPemKeyService();
        using RSA rsa = RSA.Create(2048);
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        TokenKeyResult rsaResult = service.ImportRsaPrivateKeyPem(rsa.ExportRSAPrivateKeyPem());
        TokenKeyResult ecdsaResult = service.ImportEcdsaPrivateKeyPem(ecdsa.ExportECPrivateKeyPem());

        Assert.Multiple(() =>
        {
            Assert.That(rsaResult.Success, Is.True);
            Assert.That(rsaResult.Key!.NativePrivateMaterial, Is.True);
            Assert.That(ecdsaResult.Success, Is.True);
            Assert.That(ecdsaResult.Key!.NativePrivateMaterial, Is.True);
        });

        service.ReleaseImportedKey(rsaResult.Key!);
        service.ReleaseImportedKey(ecdsaResult.Key!);
    }

    [Test]
    public void PemServiceReturnsStableFailuresForMalformedAndWrongKeyBlocks()
    {
        ITokenPemKeyService service = new TokenPemKeyService();
        using RSA rsa = RSA.Create(2048);

        TokenKeyResult malformed = service.ImportRsaPublicKeyPem("not PEM");
        TokenKeyResult wrongPublicType = service.ImportRsaPublicKeyPem(rsa.ExportPkcs8PrivateKeyPem());
        TokenKeyResult wrongPrivateType = service.ImportEcdsaPrivateKeyPem(rsa.ExportSubjectPublicKeyInfoPem());
        TokenKeyResult publicKey = service.ImportRsaPublicKeyPem(rsa.ExportSubjectPublicKeyInfoPem());
        TokenTextResult privateExport = service.ExportRsaPrivateKeyPem(publicKey.Key!);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Error!.Code, Is.EqualTo("invalid_pem"));
            Assert.That(wrongPublicType.Error!.Code, Is.EqualTo("invalid_pem_key_type"));
            Assert.That(wrongPrivateType.Error!.Code, Is.EqualTo("invalid_pem_key_type"));
            Assert.That(privateExport.Error!.Code, Is.EqualTo("missing_private_key"));
        });

        service.ReleaseImportedKey(publicKey.Key!);
    }

    [Test]
    public void PemServiceRejectsNullArguments()
    {
        ITokenPemKeyService service = new TokenPemKeyService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ImportRsaPublicKeyPem(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ImportRsaPrivateKeyPem(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ImportEcdsaPublicKeyPem(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ImportEcdsaPrivateKeyPem(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ExportRsaPublicKeyPem(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ExportEcdsaPublicKeyPem(null!), Throws.ArgumentNullException);
            Assert.That(() => service.ReleaseImportedKey(null!), Throws.ArgumentNullException);
        });
    }
}
