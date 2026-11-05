using System.Security.Cryptography;
using token;

namespace token.Tests;

public class RsaAlgorithmsTests
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

    [TestCase(JoseAlgorithms.RsaSha256)]
    [TestCase(JoseAlgorithms.RsaSha384)]
    [TestCase(JoseAlgorithms.RsaSha512)]
    public void SignAndVerifyMatchBuiltInPkcs1Vectors(string algorithm)
    {
        byte[] data = System.Text.Encoding.ASCII.GetBytes(
            "The quick brown fox jumps over the lazy dog");
        TokenKey key = RsaKeyImporterOperations.ImportJose(rsa, algorithm, true).Key;
        byte[] expected = rsa.SignData(
            data,
            Hash(algorithm),
            RSASignaturePadding.Pkcs1);

        TokenBytesResult signature = RsaAlgorithms.Sign(data, key, algorithm);
        TokenResult verification = RsaAlgorithms.Verify(data, expected, key, algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(signature.Success, Is.True);
            Assert.That(signature.Error, Is.Null);
            Assert.That(signature.Bytes, Is.EqualTo(expected));
            Assert.That(signature.Bytes, Has.Length.EqualTo(rsa.KeySize / 8));
            Assert.That(verification.Success, Is.True);
            Assert.That(verification.Error, Is.Null);
        });
    }

    [Test]
    public void PublicOnlyKeyCanVerifyButCannotSign()
    {
        byte[] data = new byte[] { 2, 4, 6, 8 };
        TokenKey privateKey = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        TokenBytesResult signature = RsaAlgorithms.Sign(
            data,
            privateKey,
            JoseAlgorithms.RsaSha256);
        using RSA publicRsa = RSA.Create();
        publicRsa.ImportParameters(rsa.ExportParameters(false));
        TokenKey publicKey = RsaKeyImporterOperations.ImportJose(
            publicRsa,
            JoseAlgorithms.RsaSha256,
            false).Key;

        TokenResult verified = RsaAlgorithms.Verify(
            data,
            signature.Bytes,
            publicKey,
            JoseAlgorithms.RsaSha256);
        TokenBytesResult signing = RsaAlgorithms.Sign(
            data,
            publicKey,
            JoseAlgorithms.RsaSha256);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Success, Is.True);
            AssertBytesFailure(signing, "missing_private_key");
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedDataAndSignatureSlices()
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha384,
            true).Key;
        byte[] data = new byte[] { 99, 1, 2, 3, 88 };

        TokenBytesResult sliced = RsaAlgorithms.Sign(
            data.AsSpan(1, 3),
            key,
            JoseAlgorithms.RsaSha384);
        TokenBytesResult exact = RsaAlgorithms.Sign(
            new byte[] { 1, 2, 3 },
            key,
            JoseAlgorithms.RsaSha384);
        byte[] wrapped = new byte[sliced.Bytes.Length + 2];
        sliced.Bytes.CopyTo(wrapped, 1);
        TokenResult verified = RsaAlgorithms.Verify(
            data.AsSpan(1, 3),
            wrapped.AsSpan(1, sliced.Bytes.Length),
            key,
            JoseAlgorithms.RsaSha384);

        Assert.Multiple(() =>
        {
            Assert.That(sliced.Bytes, Is.EqualTo(exact.Bytes));
            Assert.That(verified.Success, Is.True);
        });
    }

    [Test]
    public void VerifyRejectsAlteredDataSignatureHashAndLength()
    {
        byte[] data = new byte[] { 10, 20, 30, 40 };
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;
        TokenBytesResult signed = RsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.RsaSha256);
        byte[] alteredData = (byte[])data.Clone();
        alteredData[0] ^= 1;
        byte[] alteredSignature = (byte[])signed.Bytes.Clone();
        alteredSignature[alteredSignature.Length - 1] ^= 1;
        byte[] shortSignature = signed.Bytes[..^1];

        TokenResult wrongData = RsaAlgorithms.Verify(
            alteredData,
            signed.Bytes,
            key,
            JoseAlgorithms.RsaSha256);
        TokenResult wrongSignature = RsaAlgorithms.Verify(
            data,
            alteredSignature,
            key,
            JoseAlgorithms.RsaSha256);
        TokenResult wrongHash = RsaAlgorithms.Verify(
            data,
            signed.Bytes,
            key,
            JoseAlgorithms.RsaSha384);
        TokenResult wrongLength = RsaAlgorithms.Verify(
            data,
            shortSignature,
            key,
            JoseAlgorithms.RsaSha256);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongData, "signature_mismatch");
            AssertFailure(wrongSignature, "signature_mismatch");
            AssertFailure(wrongHash, "signature_mismatch");
            AssertFailure(wrongLength, "signature_mismatch");
        });
    }

    [Test]
    public void OperationsAndUseMetadataAreEnforcedForEachAction()
    {
        byte[] data = new byte[] { 1, 3, 5, 7 };
        TokenKey unrestricted = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        TokenBytesResult signature = RsaAlgorithms.Sign(
            data,
            unrestricted,
            JoseAlgorithms.RsaSha256);
        TokenKey verifyOnly = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        verifyOnly.KeyOperations = new string[] { TokenKeyOperations.Verify };
        TokenKey signOnly = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        signOnly.CoseKeyOperations = new int[] { CoseKeyLabels.OperationSign };
        TokenKey encryptionUse = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;
        encryptionUse.Use = TokenKeyUses.Encryption;

        TokenResult allowedVerify = RsaAlgorithms.Verify(
            data,
            signature.Bytes,
            verifyOnly,
            JoseAlgorithms.RsaSha256);
        TokenBytesResult disallowedSign = RsaAlgorithms.Sign(
            data,
            verifyOnly,
            JoseAlgorithms.RsaSha256);
        TokenResult disallowedVerify = RsaAlgorithms.Verify(
            data,
            signature.Bytes,
            signOnly,
            JoseAlgorithms.RsaSha256);
        TokenBytesResult disallowedUse = RsaAlgorithms.Sign(
            data,
            encryptionUse,
            JoseAlgorithms.RsaSha256);

        Assert.Multiple(() =>
        {
            Assert.That(allowedVerify.Success, Is.True);
            AssertBytesFailure(disallowedSign, "key_operation_not_permitted");
            AssertFailure(disallowedVerify, "key_operation_not_permitted");
            AssertBytesFailure(disallowedUse, "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void SigningRejectsWeakWrongTypeAndMismatchedKeys()
    {
        using RSA weakRsa = RSA.Create(1024);
        TokenKey weak = new TokenKey();
        weak.KeyType = TokenKeyTypes.Rsa;
        weak.Rsa = weakRsa;
        weak.NativePrivateMaterial = true;
        TokenKey wrongType = RsaKeyImporterOperations.Import(rsa, true).Key;
        wrongType.KeyType = TokenKeyTypes.Symmetric;
        TokenKey mismatched = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha384,
            true).Key;
        TokenKey coseMismatched = RsaKeyImporterOperations.ImportCose(
            rsa,
            CoseAlgorithms.RsaSha384,
            true).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], weak, JoseAlgorithms.RsaSha256),
                "weak_key");
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], wrongType, JoseAlgorithms.RsaSha256),
                "invalid_key_type");
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], mismatched, JoseAlgorithms.RsaSha256),
                "conflicting_key_algorithm");
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], coseMismatched, JoseAlgorithms.RsaSha256),
                "conflicting_key_algorithm");
        });
    }

    [Test]
    public void UnsupportedAlgorithmNamesReturnStableFailures()
    {
        TokenKey key = RsaKeyImporterOperations.Import(rsa, true).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], key, "rs256"),
                "unknown_algorithm");
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], key, JoseAlgorithms.RsaSha1),
                "prohibited_algorithm");
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], key, JoseAlgorithms.RsaOaep),
                "invalid_algorithm");
            AssertBytesFailure(
                RsaAlgorithms.Sign(new byte[0], key, JoseAlgorithms.HmacSha256),
                "invalid_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        TokenKey key = RsaKeyImporterOperations.ImportJose(
            rsa,
            JoseAlgorithms.RsaSha256,
            true).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RsaAlgorithms.Sign(null, key, JoseAlgorithms.RsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaAlgorithms.Sign(new byte[0], null, JoseAlgorithms.RsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaAlgorithms.Sign(new byte[0], key, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaAlgorithms.Verify(null, new byte[256], key, JoseAlgorithms.RsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaAlgorithms.Verify(new byte[0], null, key, JoseAlgorithms.RsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaAlgorithms.Verify(new byte[0], new byte[256], null, JoseAlgorithms.RsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => RsaAlgorithms.Verify(new byte[0], new byte[256], key, null),
                Throws.ArgumentNullException);
        });
    }

    static HashAlgorithmName Hash(string algorithm)
    {
        if (algorithm == JoseAlgorithms.RsaSha256)
        {
            return HashAlgorithmName.SHA256;
        }

        if (algorithm == JoseAlgorithms.RsaSha384)
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
