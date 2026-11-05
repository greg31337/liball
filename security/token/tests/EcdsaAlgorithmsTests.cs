using System.Security.Cryptography;
using token;

namespace token.Tests;

public class EcdsaAlgorithmsTests
{
    [TestCase(JoseAlgorithms.EcdsaSha256, "P-256", 32)]
    [TestCase(JoseAlgorithms.EcdsaSha384, "P-384", 48)]
    [TestCase(JoseAlgorithms.EcdsaSha512, "P-521", 66)]
    public void SignAndVerifyInteroperateWithBuiltInRawFormat(
        string algorithm,
        string curve,
        int componentWidth)
    {
        using ECDsa ecdsa = CreateCurve(curve);
        byte[] data = System.Text.Encoding.ASCII.GetBytes(
            "The quick brown fox jumps over the lazy dog");
        TokenKey key = EcdsaKeyImporter.ImportJose(ecdsa, algorithm, true).Key;

        TokenBytesResult signature = EcdsaAlgorithms.Sign(data, key, algorithm);
        byte[] builtInSignature = ecdsa.SignData(
            data,
            Hash(algorithm),
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        TokenResult libraryVerification = EcdsaAlgorithms.Verify(
            data,
            builtInSignature,
            key,
            algorithm);
        bool builtInVerification = ecdsa.VerifyData(
            data,
            signature.Bytes,
            Hash(algorithm),
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        Assert.Multiple(() =>
        {
            Assert.That(signature.Success, Is.True);
            Assert.That(signature.Error, Is.Null);
            Assert.That(signature.Bytes, Has.Length.EqualTo(componentWidth * 2));
            Assert.That(libraryVerification.Success, Is.True);
            Assert.That(libraryVerification.Error, Is.Null);
            Assert.That(builtInVerification, Is.True);
        });
    }

    [Test]
    public void PublicOnlyKeyCanVerifyButCannotSign()
    {
        using ECDsa privateEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = new byte[] { 2, 4, 6, 8 };
        TokenKey privateKey = EcdsaKeyImporter.ImportJose(
            privateEcdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        TokenBytesResult signature = EcdsaAlgorithms.Sign(
            data,
            privateKey,
            JoseAlgorithms.EcdsaSha256);
        using ECDsa publicEcdsa = ECDsa.Create(privateEcdsa.ExportParameters(false));
        TokenKey publicKey = EcdsaKeyImporter.ImportJose(
            publicEcdsa,
            JoseAlgorithms.EcdsaSha256,
            false).Key;

        TokenResult verified = EcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            publicKey,
            JoseAlgorithms.EcdsaSha256);
        TokenBytesResult signing = EcdsaAlgorithms.Sign(
            data,
            publicKey,
            JoseAlgorithms.EcdsaSha256);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Success, Is.True);
            AssertBytesFailure(signing, "missing_private_key");
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedDataAndSignatureSlices()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha384,
            true).Key;
        byte[] data = new byte[] { 99, 1, 2, 3, 88 };

        TokenBytesResult signature = EcdsaAlgorithms.Sign(
            data.AsSpan(1, 3),
            key,
            JoseAlgorithms.EcdsaSha384);
        byte[] wrapped = new byte[signature.Bytes.Length + 2];
        signature.Bytes.CopyTo(wrapped, 1);
        TokenResult verified = EcdsaAlgorithms.Verify(
            data.AsSpan(1, 3),
            wrapped.AsSpan(1, signature.Bytes.Length),
            key,
            JoseAlgorithms.EcdsaSha384);
        TokenResult fullData = EcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            key,
            JoseAlgorithms.EcdsaSha384);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Success, Is.True);
            AssertFailure(fullData, "signature_mismatch");
        });
    }

    [Test]
    public void VerifyRejectsAlteredDataSignatureAndIncorrectRawLengths()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        byte[] data = new byte[] { 10, 20, 30, 40 };
        TokenBytesResult signed = EcdsaAlgorithms.Sign(
            data,
            key,
            JoseAlgorithms.EcdsaSha256);
        byte[] alteredData = (byte[])data.Clone();
        alteredData[0] ^= 1;
        byte[] alteredSignature = (byte[])signed.Bytes.Clone();
        alteredSignature[alteredSignature.Length - 1] ^= 1;

        TokenResult wrongData = EcdsaAlgorithms.Verify(
            alteredData,
            signed.Bytes,
            key,
            JoseAlgorithms.EcdsaSha256);
        TokenResult wrongSignature = EcdsaAlgorithms.Verify(
            data,
            alteredSignature,
            key,
            JoseAlgorithms.EcdsaSha256);
        TokenResult shortSignature = EcdsaAlgorithms.Verify(
            data,
            signed.Bytes[..^1],
            key,
            JoseAlgorithms.EcdsaSha256);
        TokenResult longSignature = EcdsaAlgorithms.Verify(
            data,
            new byte[signed.Bytes.Length + 1],
            key,
            JoseAlgorithms.EcdsaSha256);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongData, "signature_mismatch");
            AssertFailure(wrongSignature, "signature_mismatch");
            AssertFailure(shortSignature, "signature_mismatch");
            AssertFailure(longSignature, "signature_mismatch");
        });
    }

    [Test]
    public void VerifyRejectsDerEncodingInsteadOfJoseRawEncoding()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        byte[] data = new byte[] { 1, 3, 3, 7 };
        byte[] der = ecdsa.SignData(
            data,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);

        TokenResult result = EcdsaAlgorithms.Verify(
            data,
            der,
            key,
            JoseAlgorithms.EcdsaSha256);

        AssertFailure(result, "signature_mismatch");
    }

    [Test]
    public void RequestedAlgorithmMustMatchNativeCurveAndKeyMetadata()
    {
        using ECDsa p256 = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa p384 = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey wrongCurve = EcdsaKeyImporter.Import(p256, true).Key;
        TokenKey wrongJoseAlgorithm = EcdsaKeyImporter.ImportJose(
            p384,
            JoseAlgorithms.EcdsaSha384,
            true).Key;
        TokenKey wrongCoseAlgorithm = EcdsaKeyImporter.ImportCose(
            p384,
            CoseAlgorithms.EcdsaP384Sha384,
            true).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(
                    new byte[0],
                    wrongCurve,
                    JoseAlgorithms.EcdsaSha384),
                "algorithm_curve_mismatch");
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(
                    new byte[0],
                    wrongJoseAlgorithm,
                    JoseAlgorithms.EcdsaSha256),
                "algorithm_curve_mismatch");
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(
                    new byte[0],
                    wrongCoseAlgorithm,
                    JoseAlgorithms.EcdsaSha256),
                "algorithm_curve_mismatch");
        });
    }

    [Test]
    public void OperationsAndUseMetadataAreEnforcedForEachAction()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = new byte[] { 1, 3, 5, 7 };
        TokenKey unrestricted = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        TokenBytesResult signature = EcdsaAlgorithms.Sign(
            data,
            unrestricted,
            JoseAlgorithms.EcdsaSha256);
        TokenKey verifyOnly = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        verifyOnly.KeyOperations = new string[] { TokenKeyOperations.Verify };
        TokenKey signOnly = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        signOnly.CoseKeyOperations = new int[] { CoseKeyLabels.OperationSign };
        TokenKey encryptionUse = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;
        encryptionUse.Use = TokenKeyUses.Encryption;

        TokenResult allowedVerify = EcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            verifyOnly,
            JoseAlgorithms.EcdsaSha256);
        TokenBytesResult disallowedSign = EcdsaAlgorithms.Sign(
            data,
            verifyOnly,
            JoseAlgorithms.EcdsaSha256);
        TokenResult disallowedVerify = EcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            signOnly,
            JoseAlgorithms.EcdsaSha256);
        TokenBytesResult disallowedUse = EcdsaAlgorithms.Sign(
            data,
            encryptionUse,
            JoseAlgorithms.EcdsaSha256);

        Assert.Multiple(() =>
        {
            Assert.That(allowedVerify.Success, Is.True);
            AssertBytesFailure(disallowedSign, "key_operation_not_permitted");
            AssertFailure(disallowedVerify, "key_operation_not_permitted");
            AssertBytesFailure(disallowedUse, "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void WrongKeyTypesAndUnsupportedAlgorithmsReturnStableFailures()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(ecdsa, true).Key;
        TokenKey wrongType = EcdsaKeyImporter.Import(ecdsa, true).Key;
        wrongType.KeyType = TokenKeyTypes.Rsa;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(new byte[0], wrongType, JoseAlgorithms.EcdsaSha256),
                "invalid_key_type");
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(new byte[0], key, "es256"),
                "unknown_algorithm");
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(
                    new byte[0],
                    key,
                    JoseAlgorithms.EcdsaSecp256k1Sha256),
                CryptoAlgorithmAvailability.IsEs256KAvailable()
                    ? "invalid_algorithm"
                    : "unsupported_algorithm");
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(new byte[0], key, JoseAlgorithms.RsaSha256),
                "invalid_algorithm");
            AssertBytesFailure(
                EcdsaAlgorithms.Sign(new byte[0], key, JoseAlgorithms.RsaSha1),
                "prohibited_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportJose(
            ecdsa,
            JoseAlgorithms.EcdsaSha256,
            true).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => EcdsaAlgorithms.Sign(null, key, JoseAlgorithms.EcdsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => EcdsaAlgorithms.Sign(new byte[0], null, JoseAlgorithms.EcdsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => EcdsaAlgorithms.Sign(new byte[0], key, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => EcdsaAlgorithms.Verify(null, new byte[64], key, JoseAlgorithms.EcdsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => EcdsaAlgorithms.Verify(new byte[0], null, key, JoseAlgorithms.EcdsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => EcdsaAlgorithms.Verify(new byte[0], new byte[64], null, JoseAlgorithms.EcdsaSha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => EcdsaAlgorithms.Verify(new byte[0], new byte[64], key, null),
                Throws.ArgumentNullException);
        });
    }

    static ECDsa CreateCurve(string curve)
    {
        if (curve == "P-256")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP256);
        }

        if (curve == "P-384")
        {
            return ECDsa.Create(ECCurve.NamedCurves.nistP384);
        }

        return ECDsa.Create(ECCurve.NamedCurves.nistP521);
    }

    static HashAlgorithmName Hash(string algorithm)
    {
        if (algorithm == JoseAlgorithms.EcdsaSha256)
        {
            return HashAlgorithmName.SHA256;
        }

        if (algorithm == JoseAlgorithms.EcdsaSha384)
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
