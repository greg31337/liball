using System.Reflection;
using System.Security.Cryptography;
using token;

namespace token.Tests;

public class CryptoAlgorithmAvailabilityTests
{
    [Test]
    public void Es256KAvailabilityMatchesARealPlatformOperation()
    {
        bool expected = ProbeEs256KWithPlatform();

        Assert.Multiple(() =>
        {
            Assert.That(CryptoAlgorithmAvailability.IsEs256KAvailable(), Is.EqualTo(expected));
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
                    JoseAlgorithms.EcdsaSecp256k1Sha256),
                Is.EqualTo(expected));
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(
                    CoseAlgorithms.EcdsaSecp256k1Sha256),
                Is.EqualTo(expected));
        });
    }

    [Test]
    public void EdwardsAvailabilityMatchesPublicRuntimePrimitives()
    {
        bool ed25519 = HasPublicEdwardsPrimitive("Ed25519");
        bool ed448 = HasPublicEdwardsPrimitive("Ed448");

        Assert.Multiple(() =>
        {
            Assert.That(CryptoAlgorithmAvailability.IsEd25519Available(), Is.EqualTo(ed25519));
            Assert.That(CryptoAlgorithmAvailability.IsEd448Available(), Is.EqualTo(ed448));
            Assert.That(CryptoAlgorithmAvailability.IsEdDsaAvailable(), Is.EqualTo(ed25519 || ed448));
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(JoseAlgorithms.Ed25519),
                Is.EqualTo(ed25519));
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(JoseAlgorithms.Ed448),
                Is.EqualTo(ed448));
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(JoseAlgorithms.EdDsa),
                Is.EqualTo(ed25519 || ed448));
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(CoseAlgorithms.Ed25519),
                Is.EqualTo(ed25519));
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(CoseAlgorithms.Ed448),
                Is.EqualTo(ed448));
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(CoseAlgorithms.EdDsa),
                Is.EqualTo(ed25519 || ed448));
        });
    }

    [Test]
    public void GeneralChecksRespectRegistryImplementationAndSecurityMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
                    JoseAlgorithms.HmacSha256),
                Is.True);
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(
                    CoseAlgorithms.RsaPssSha256),
                Is.True);
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
                    JoseAlgorithms.MlDsa65),
                Is.False);
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(
                    CoseAlgorithms.MlDsa87),
                Is.False);
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(
                    JoseAlgorithms.RsaSha1),
                Is.False);
            Assert.That(
                CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable("unknown"),
                Is.False);
            Assert.That(
                CryptoAlgorithmAvailability.IsCoseAlgorithmAvailable(999999),
                Is.False);
        });
    }

    [Test]
    public void CapabilityResultsDistinguishUnsupportedUnknownAndProhibitedAlgorithms()
    {
        TokenResult supportedJose = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            JoseAlgorithms.HmacSha256);
        TokenResult supportedCose = CryptoAlgorithmAvailability.CheckCoseAlgorithm(
            CoseAlgorithms.RsaPssSha256);
        TokenResult unsupportedJose = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            JoseAlgorithms.MlDsa65);
        TokenResult unsupportedCose = CryptoAlgorithmAvailability.CheckCoseAlgorithm(
            CoseAlgorithms.MlDsa87);
        TokenResult prohibited = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            JoseAlgorithms.RsaSha1);
        TokenResult unknownJose = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            "unknown");
        TokenResult unknownCose = CryptoAlgorithmAvailability.CheckCoseAlgorithm(
            999999);

        Assert.Multiple(() =>
        {
            Assert.That(supportedJose.Success, Is.True);
            Assert.That(supportedJose.Error, Is.Null);
            Assert.That(supportedCose.Success, Is.True);
            Assert.That(supportedCose.Error, Is.Null);
            AssertFailure(unsupportedJose, "unsupported_algorithm");
            AssertFailure(unsupportedCose, "unsupported_algorithm");
            AssertFailure(prohibited, "prohibited_algorithm");
            AssertFailure(unknownJose, "unknown_algorithm");
            AssertFailure(unknownCose, "unknown_algorithm");
        });
    }

    [Test]
    public void OptionalAlgorithmResultsMatchRuntimeAvailability()
    {
        TokenResult jose = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            JoseAlgorithms.EcdsaSecp256k1Sha256);
        TokenResult cose = CryptoAlgorithmAvailability.CheckCoseAlgorithm(
            CoseAlgorithms.EcdsaSecp256k1Sha256);
        bool available = CryptoAlgorithmAvailability.IsEs256KAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(jose.Success, Is.EqualTo(available));
            Assert.That(cose.Success, Is.EqualTo(available));
            if (!available)
            {
                AssertFailure(jose, "unsupported_algorithm");
                AssertFailure(cose, "unsupported_algorithm");
            }
        });
    }

    [Test]
    public void AvailabilityIsCachedAndNullJoseNameIsRejected()
    {
        bool first = CryptoAlgorithmAvailability.IsEs256KAvailable();
        bool ed25519 = CryptoAlgorithmAvailability.IsEd25519Available();

        Assert.Multiple(() =>
        {
            Assert.That(CryptoAlgorithmAvailability.IsEs256KAvailable(), Is.EqualTo(first));
            Assert.That(CryptoAlgorithmAvailability.IsEd25519Available(),
                Is.EqualTo(ed25519));
            Assert.That(
                () => CryptoAlgorithmAvailability.IsJoseAlgorithmAvailable(null),
                Throws.ArgumentNullException);
            Assert.That(
                () => CryptoAlgorithmAvailability.CheckJoseAlgorithm(null),
                Throws.ArgumentNullException);
        });
    }

    static void AssertFailure(TokenResult result, string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(-1));
    }

    static bool ProbeEs256KWithPlatform()
    {
        try
        {
            ECCurve curve = ECCurve.CreateFromValue("1.3.132.0.10");
            using ECDsa ecdsa = ECDsa.Create(curve);
            byte[] data = new byte[] { 69, 83, 50, 53, 54, 75 };
            byte[] signature = ecdsa.SignData(
                data,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return ecdsa.KeySize == 256 && ecdsa.VerifyData(
                data,
                signature,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    static bool HasPublicEdwardsPrimitive(string name)
    {
        Type type = Type.GetType(
            "System.Security.Cryptography." + name
            + ", System.Security.Cryptography.Algorithms",
            false);
        type ??= Type.GetType(
            "System.Security.Cryptography." + name
            + ", System.Security.Cryptography",
            false);
        if (type == null || !type.IsPublic)
        {
            return false;
        }

        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
        return methods.Any(method => method.Name == "Sign")
            && methods.Any(method => method.Name == "Verify");
    }
}
