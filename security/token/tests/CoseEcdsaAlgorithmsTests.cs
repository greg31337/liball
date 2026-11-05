using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CoseEcdsaAlgorithmsTests
{
    [TestCase(CoseAlgorithms.EcdsaP256Sha256, "P-256", 32)]
    [TestCase(CoseAlgorithms.EcdsaP384Sha384, "P-384", 48)]
    [TestCase(CoseAlgorithms.EcdsaP521Sha512, "P-521", 66)]
    public void FullySpecifiedAlgorithmsInteroperateWithBuiltInRawFormat(
        int algorithm,
        string curve,
        int componentWidth)
    {
        using ECDsa ecdsa = CreateCurve(curve);
        byte[] data = System.Text.Encoding.ASCII.GetBytes(
            "COSE ECDSA interoperability");
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ecdsa,
            algorithm,
            true).Key;

        TokenBytesResult signature = CoseEcdsaAlgorithms.Sign(
            data,
            key,
            algorithm);
        byte[] builtInSignature = ecdsa.SignData(
            data,
            Hash(algorithm),
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        TokenResult libraryVerification = CoseEcdsaAlgorithms.Verify(
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
            Assert.That(signature.Bytes,
                Has.Length.EqualTo(componentWidth * 2));
            Assert.That(libraryVerification.Success, Is.True);
            Assert.That(libraryVerification.Error, Is.Null);
            Assert.That(builtInVerification, Is.True);
        });
    }

    [Test]
    public void PublicOnlyKeyCanVerifyButCannotSign()
    {
        using ECDsa privateEcdsa =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = new byte[] { 2, 4, 6, 8 };
        TokenKey privateKey = EcdsaKeyImporter.ImportCose(
            privateEcdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        TokenBytesResult signature = CoseEcdsaAlgorithms.Sign(
            data,
            privateKey,
            CoseAlgorithms.EcdsaP256Sha256);
        using ECDsa publicEcdsa = ECDsa.Create(
            privateEcdsa.ExportParameters(false));
        TokenKey publicKey = EcdsaKeyImporter.ImportCose(
            publicEcdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            false).Key;

        TokenResult verified = CoseEcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            publicKey,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenBytesResult signing = CoseEcdsaAlgorithms.Sign(
            data,
            publicKey,
            CoseAlgorithms.EcdsaP256Sha256);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Success, Is.True);
            AssertBytesFailure(signing, "missing_private_key");
        });
    }

    [Test]
    public void SpanOverloadsUseOnlySelectedSlices()
    {
        using ECDsa ecdsa =
            ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP384Sha384,
            true).Key;
        byte[] data = new byte[] { 99, 1, 2, 3, 88 };

        TokenBytesResult signature = CoseEcdsaAlgorithms.Sign(
            data.AsSpan(1, 3),
            key,
            CoseAlgorithms.EcdsaP384Sha384,
            false);
        byte[] wrapped = new byte[signature.Bytes.Length + 2];
        signature.Bytes.CopyTo(wrapped, 1);
        TokenResult selected = CoseEcdsaAlgorithms.Verify(
            data.AsSpan(1, 3),
            wrapped.AsSpan(1, signature.Bytes.Length),
            key,
            CoseAlgorithms.EcdsaP384Sha384,
            false);
        TokenResult full = CoseEcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            key,
            CoseAlgorithms.EcdsaP384Sha384);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Success, Is.True);
            AssertFailure(full, "signature_mismatch");
        });
    }

    [Test]
    public void AlteredInputsAndIncorrectRawLengthsAreRejected()
    {
        using ECDsa ecdsa =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        byte[] data = new byte[] { 10, 20, 30, 40 };
        TokenBytesResult signed = CoseEcdsaAlgorithms.Sign(
            data,
            key,
            CoseAlgorithms.EcdsaP256Sha256);
        byte[] alteredData = data.ToArray();
        alteredData[0] ^= 1;
        byte[] alteredSignature = signed.Bytes.ToArray();
        alteredSignature[0] ^= 1;

        TokenResult wrongData = CoseEcdsaAlgorithms.Verify(
            alteredData,
            signed.Bytes,
            key,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenResult wrongSignature = CoseEcdsaAlgorithms.Verify(
            data,
            alteredSignature,
            key,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenResult shortSignature = CoseEcdsaAlgorithms.Verify(
            data,
            signed.Bytes[..^1],
            key,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenResult longSignature = CoseEcdsaAlgorithms.Verify(
            data,
            new byte[signed.Bytes.Length + 1],
            key,
            CoseAlgorithms.EcdsaP256Sha256);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongData, "signature_mismatch");
            AssertFailure(wrongSignature, "signature_mismatch");
            AssertFailure(shortSignature, "signature_mismatch");
            AssertFailure(longSignature, "signature_mismatch");
        });
    }

    [TestCase(
        CoseAlgorithms.EcdsaSha256,
        CoseAlgorithms.EcdsaP256Sha256,
        "P-256")]
    [TestCase(
        CoseAlgorithms.EcdsaSha384,
        CoseAlgorithms.EcdsaP384Sha384,
        "P-384")]
    [TestCase(
        CoseAlgorithms.EcdsaSha512,
        CoseAlgorithms.EcdsaP521Sha512,
        "P-521")]
    public void DeprecatedAlgorithmsRequireExplicitCompatibility(
        int deprecated,
        int current,
        string curve)
    {
        using ECDsa ecdsa = CreateCurve(curve);
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ecdsa,
            current,
            true).Key;
        byte[] data = new byte[] { 1, 3, 5 };

        TokenBytesResult conservative = CoseEcdsaAlgorithms.Sign(
            data,
            key,
            deprecated);
        TokenBytesResult compatible = CoseEcdsaAlgorithms.Sign(
            data,
            key,
            deprecated,
            true);
        TokenResult verified = CoseEcdsaAlgorithms.Verify(
            data,
            compatible.Bytes,
            key,
            deprecated,
            true);

        Assert.Multiple(() =>
        {
            AssertBytesFailure(conservative, "compatibility_required");
            Assert.That(compatible.Success, Is.True);
            Assert.That(verified.Success, Is.True);
        });
    }

    [Test]
    public void RequestedAlgorithmMustMatchCurveAndKeyMetadata()
    {
        using ECDsa p256 =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa p384 =
            ECDsa.Create(ECCurve.NamedCurves.nistP384);
        TokenKey wrongCurve = EcdsaKeyImporter.Import(p256, true).Key;
        TokenKey wrongJose = EcdsaKeyImporter.ImportJose(
            p384,
            JoseAlgorithms.EcdsaSha384,
            true).Key;
        TokenKey wrongCose = EcdsaKeyImporter.ImportCose(
            p384,
            CoseAlgorithms.EcdsaP384Sha384,
            true).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    wrongCurve,
                    CoseAlgorithms.EcdsaP384Sha384),
                "algorithm_curve_mismatch");
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    wrongJose,
                    CoseAlgorithms.EcdsaP256Sha256),
                "algorithm_curve_mismatch");
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    wrongCose,
                    CoseAlgorithms.EcdsaP256Sha256),
                "algorithm_curve_mismatch");
        });
    }

    [Test]
    public void OperationsAndUseMetadataAreEnforced()
    {
        using ECDsa ecdsa =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] data = new byte[] { 1, 3, 5, 7 };
        TokenKey unrestricted = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        TokenBytesResult signature = CoseEcdsaAlgorithms.Sign(
            data,
            unrestricted,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenKey verifyOnly = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        verifyOnly.KeyOperations =
            new string[] { TokenKeyOperations.Verify };
        TokenKey signOnly = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        signOnly.CoseKeyOperations =
            new int[] { CoseKeyLabels.OperationSign };
        TokenKey encryptionUse = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;
        encryptionUse.Use = TokenKeyUses.Encryption;

        TokenResult allowedVerify = CoseEcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            verifyOnly,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenBytesResult disallowedSign = CoseEcdsaAlgorithms.Sign(
            data,
            verifyOnly,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenResult disallowedVerify = CoseEcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            signOnly,
            CoseAlgorithms.EcdsaP256Sha256);
        TokenBytesResult disallowedUse = CoseEcdsaAlgorithms.Sign(
            data,
            encryptionUse,
            CoseAlgorithms.EcdsaP256Sha256);

        Assert.Multiple(() =>
        {
            Assert.That(allowedVerify.Success, Is.True);
            AssertBytesFailure(disallowedSign, "key_operation_not_permitted");
            AssertFailure(disallowedVerify, "key_operation_not_permitted");
            AssertBytesFailure(
                disallowedUse,
                "key_use_algorithm_mismatch");
        });
    }

    [Test]
    public void Secp256k1MatchesPlatformAvailability()
    {
        if (!CryptoAlgorithmAvailability.IsEs256KAvailable())
        {
            using ECDsa p256 =
                ECDsa.Create(ECCurve.NamedCurves.nistP256);
            TokenKey unavailableKey =
                EcdsaKeyImporter.Import(p256, true).Key;
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    unavailableKey,
                    CoseAlgorithms.EcdsaSecp256k1Sha256),
                "unsupported_algorithm");
            return;
        }

        ECCurve curve = ECCurve.CreateFromValue("1.3.132.0.10");
        using ECDsa ecdsa = ECDsa.Create(curve);
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaSecp256k1Sha256,
            true).Key;
        byte[] data = new byte[] { 9, 8, 7 };

        TokenBytesResult signature = CoseEcdsaAlgorithms.Sign(
            data,
            key,
            CoseAlgorithms.EcdsaSecp256k1Sha256);
        TokenResult verified = CoseEcdsaAlgorithms.Verify(
            data,
            signature.Bytes,
            key,
            CoseAlgorithms.EcdsaSecp256k1Sha256);

        Assert.Multiple(() =>
        {
            Assert.That(signature.Success, Is.True);
            Assert.That(signature.Bytes, Has.Length.EqualTo(64));
            Assert.That(verified.Success, Is.True);
        });
    }

    [Test]
    public void UnsupportedAndNonEcdsaAlgorithmsReturnStableFailures()
    {
        using ECDsa ecdsa =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.Import(ecdsa, true).Key;

        Assert.Multiple(() =>
        {
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    key,
                    999999),
                "unknown_algorithm");
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    key,
                    CoseAlgorithms.RsaSha256),
                "invalid_algorithm");
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    key,
                    CoseAlgorithms.EcdsaBrainpoolP256r1Sha256),
                "unsupported_algorithm");
            AssertBytesFailure(
                CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    key,
                    CoseAlgorithms.RsaSha1),
                "unsupported_algorithm");
        });
    }

    [Test]
    public void NullReferenceArgumentsAreProgrammerErrors()
    {
        using ECDsa ecdsa =
            ECDsa.Create(ECCurve.NamedCurves.nistP256);
        TokenKey key = EcdsaKeyImporter.ImportCose(
            ecdsa,
            CoseAlgorithms.EcdsaP256Sha256,
            true).Key;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => CoseEcdsaAlgorithms.Sign(
                    null,
                    key,
                    CoseAlgorithms.EcdsaP256Sha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEcdsaAlgorithms.Sign(
                    Array.Empty<byte>(),
                    null,
                    CoseAlgorithms.EcdsaP256Sha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEcdsaAlgorithms.Verify(
                    null,
                    new byte[64],
                    key,
                    CoseAlgorithms.EcdsaP256Sha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEcdsaAlgorithms.Verify(
                    Array.Empty<byte>(),
                    null,
                    key,
                    CoseAlgorithms.EcdsaP256Sha256),
                Throws.ArgumentNullException);
            Assert.That(
                () => CoseEcdsaAlgorithms.Verify(
                    Array.Empty<byte>(),
                    new byte[64],
                    null,
                    CoseAlgorithms.EcdsaP256Sha256),
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

    static HashAlgorithmName Hash(int algorithm)
    {
        if (algorithm == CoseAlgorithms.EcdsaP256Sha256)
        {
            return HashAlgorithmName.SHA256;
        }

        if (algorithm == CoseAlgorithms.EcdsaP384Sha384)
        {
            return HashAlgorithmName.SHA384;
        }

        return HashAlgorithmName.SHA512;
    }

    static void AssertBytesFailure(
        TokenBytesResult result,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Bytes, Is.Null);
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
    }
}
