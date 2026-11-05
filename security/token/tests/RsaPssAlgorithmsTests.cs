using System.Security.Cryptography;
using token;

namespace token.Tests;

public class RsaPssAlgorithmsTests
{
    RSA rsa;

    [OneTimeSetUp]
    public void CreateKey()
    {
        rsa = RSA.Create(2048);
    }

    [OneTimeTearDown]
    public void DisposeKey()
    {
        rsa.Dispose();
    }

    [TestCase(JoseAlgorithms.RsaPssSha256)]
    [TestCase(JoseAlgorithms.RsaPssSha384)]
    [TestCase(JoseAlgorithms.RsaPssSha512)]
    public void SignAndVerifyInteroperateWithBuiltInPss(string algorithm)
    {
        byte[] data = System.Text.Encoding.ASCII.GetBytes(
            "The quick brown fox jumps over the lazy dog");
        TokenKey key = RsaKeyImporterOperations.ImportJose(rsa, algorithm, true).Key;

        TokenBytesResult signature = RsaAlgorithms.Sign(data, key, algorithm);
        byte[] builtInSignature = rsa.SignData(
            data,
            Hash(algorithm),
            RSASignaturePadding.Pss);
        TokenResult libraryVerification = RsaAlgorithms.Verify(
            data,
            builtInSignature,
            key,
            algorithm);
        bool builtInVerification = rsa.VerifyData(
            data,
            signature.Bytes,
            Hash(algorithm),
            RSASignaturePadding.Pss);

        Assert.Multiple(() =>
        {
            Assert.That(signature.Success, Is.True);
            Assert.That(signature.Error, Is.Null);
            Assert.That(signature.Bytes, Has.Length.EqualTo(rsa.KeySize / 8));
            Assert.That(libraryVerification.Success, Is.True);
            Assert.That(libraryVerification.Error, Is.Null);
            Assert.That(builtInVerification, Is.True);
        });
    }

    [Test]
    public void PssSignaturesAreRandomizedAndRemainVerifiable()
    {
        byte[] data = new byte[] { 1, 2, 3, 4 };
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPssSha256,
            true).Key;

        TokenBytesResult first = RsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.RsaPssSha256);
        TokenBytesResult second = RsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.RsaPssSha256);

        Assert.Multiple(() =>
        {
            Assert.That(first.Bytes, Is.Not.EqualTo(second.Bytes));
            Assert.That(RsaAlgorithms.Verify(
                data,
                first.Bytes,
                key,
                JoseAlgorithms.RsaPssSha256).Success, Is.True);
            Assert.That(RsaAlgorithms.Verify(
                data,
                second.Bytes,
                key,
                JoseAlgorithms.RsaPssSha256).Success, Is.True);
        });
    }

    [Test]
    public void VerificationRejectsAlteredInputWrongHashAndMalformedSignature()
    {
        byte[] data = new byte[] { 10, 20, 30, 40 };
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        TokenBytesResult signed = RsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.RsaPssSha256);
        byte[] alteredData = (byte[])data.Clone();
        alteredData[1] ^= 1;
        byte[] alteredSignature = (byte[])signed.Bytes.Clone();
        alteredSignature[0] ^= 1;

        TokenResult wrongData = RsaAlgorithms.Verify(
            alteredData,
            signed.Bytes,
            key,
            JoseAlgorithms.RsaPssSha256);
        TokenResult wrongSignature = RsaAlgorithms.Verify(
            data,
            alteredSignature,
            key,
            JoseAlgorithms.RsaPssSha256);
        TokenResult wrongHash = RsaAlgorithms.Verify(
            data,
            signed.Bytes,
            key,
            JoseAlgorithms.RsaPssSha384);
        TokenResult wrongLength = RsaAlgorithms.Verify(
            data,
            signed.Bytes[..^1],
            key,
            JoseAlgorithms.RsaPssSha256);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongData, "signature_mismatch");
            AssertFailure(wrongSignature, "signature_mismatch");
            AssertFailure(wrongHash, "signature_mismatch");
            AssertFailure(wrongLength, "signature_mismatch");
        });
    }

    [Test]
    public void PssAndPkcs1SignaturesCannotBeInterchanged()
    {
        byte[] data = new byte[] { 5, 6, 7, 8 };
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        TokenBytesResult pss = RsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.RsaPssSha256);
        TokenBytesResult pkcs1 = RsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.RsaSha256);

        TokenResult pssAsPkcs1 = RsaAlgorithms.Verify(
            data,
            pss.Bytes,
            key,
            JoseAlgorithms.RsaSha256);
        TokenResult pkcs1AsPss = RsaAlgorithms.Verify(
            data,
            pkcs1.Bytes,
            key,
            JoseAlgorithms.RsaPssSha256);

        Assert.Multiple(() =>
        {
            AssertFailure(pssAsPkcs1, "signature_mismatch");
            AssertFailure(pkcs1AsPss, "signature_mismatch");
        });
    }

    [Test]
    public void PublicOnlyPssKeyCanVerifyButCannotSign()
    {
        byte[] data = new byte[] { 8, 6, 4, 2 };
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPssSha512,
            true).Key;
        TokenBytesResult signature = RsaAlgorithms.Sign(
            data,
            privateKey,
            JoseAlgorithms.RsaPssSha512);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(rsa.ExportParameters(false));
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaPssSha512,
            false).Key;

        TokenResult verified = RsaAlgorithms.Verify(
            data,
            signature.Bytes,
            publicKey,
            JoseAlgorithms.RsaPssSha512);
        TokenBytesResult signing = RsaAlgorithms.Sign(
            data,
            publicKey,
            JoseAlgorithms.RsaPssSha512);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Success, Is.True);
            AssertBytesFailure(signing, "missing_private_key");
        });
    }

    [Test]
    public void PssKeyMetadataIsBoundToAlgorithmAndOperation()
    {
        byte[] data = new byte[] { 3, 1, 4, 1, 5 };
        TokenKey mismatched = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPssSha384,
            true).Key;
        TokenKey verifyOnly = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaPssSha256,
            true).Key;
        verifyOnly.KeyOperations = new string[] { TokenKeyOperations.Verify };

        TokenBytesResult mismatch = RsaAlgorithms.Sign(
            data,
            mismatched,
            JoseAlgorithms.RsaPssSha256);
        TokenBytesResult operation = RsaAlgorithms.Sign(
            data,
            verifyOnly,
            JoseAlgorithms.RsaPssSha256);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(mismatch, "conflicting_key_algorithm");
            AssertBytesFailure(operation, "key_operation_not_permitted");
        });
    }

    static HashAlgorithmName Hash(string algorithm)
    {
        if (algorithm == JoseAlgorithms.RsaPssSha256)
        {
            return HashAlgorithmName.SHA256;
        }

        if (algorithm == JoseAlgorithms.RsaPssSha384)
        {
            return HashAlgorithmName.SHA384;
        }

        return HashAlgorithmName.SHA512;
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Position, Is.EqualTo(-1));
        });
    }
}
