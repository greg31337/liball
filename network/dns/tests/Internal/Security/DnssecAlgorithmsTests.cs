using System.Text;

namespace dns.Tests.Internal.Security;

public class DnssecAlgorithmsTests
{
    [Test]
    public void DecodesRfc5702RsaKeyAndCalculatesItsKeyTag()
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.RsaSha256,
            Convert.FromBase64String(
                "AwEAAcFcGsaxxdgiuuGmCkVImy4h99CqT7jwY3pexPGcnUFt" +
                "R2Fh36BponcwtkZ4cAgtvd4Qs8PkxUdp6p/DlUmObdk="));

        DnsPublicKeyResult decoded = DnssecAlgorithms.DecodePublicKey(key);
        DnsDnssecIntegerResult keyTag =
            DnssecAlgorithms.CalculateKeyTag(key);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Rsa, Is.True);
            Assert.That(decoded.Exponent,
                Is.EqualTo(new byte[] { 1, 0, 1 }));
            Assert.That(decoded.Modulus, Has.Length.EqualTo(64));
            Assert.That(decoded.Modulus[0], Is.EqualTo(0xc1));
            Assert.That(decoded.Modulus[63], Is.EqualTo(0xd9));
            Assert.That(keyTag.Success, Is.True);
            Assert.That(keyTag.Value, Is.EqualTo(9033));
        });
    }

    [Test]
    public void UsesTheLegacyRsaMd5ModulusKeyTagRule()
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.RsaMd5,
            [1, 3, 0x11, 0x22, 0x33, 0x44]);

        DnsDnssecIntegerResult result =
            DnssecAlgorithms.CalculateKeyTag(key);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(0x2233));
        });
    }

    [Test]
    public void DecodesFixedWidthEcdsaCoordinatesAndRejectsWrongLength()
    {
        byte[] publicKey = new byte[64];
        int index = 0;
        while (index < publicKey.Length)
        {
            publicKey[index] = (byte)index;
            index = index + 1;
        }

        DnsPublicKeyResult decoded = DnssecAlgorithms.DecodePublicKey(
            Key(DnsDnssecAlgorithm.EcdsaP256Sha256, publicKey));
        DnsPublicKeyResult malformed = DnssecAlgorithms.DecodePublicKey(
            Key(DnsDnssecAlgorithm.EcdsaP256Sha256, new byte[63]));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Ecdsa, Is.True);
            Assert.That(decoded.X, Is.EqualTo(publicKey[..32]));
            Assert.That(decoded.Y, Is.EqualTo(publicKey[32..]));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void CreatesTheRfc8080Ed25519Sha256DelegationDigest()
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.Ed25519,
            Convert.FromBase64String(
                "l02Woi0iS8Aa25FQkUd9RMzZHJpBoRQwAQEX1SxZJA4="));
        key.Flags = 257;

        DnsPublicKeyResult decoded = DnssecAlgorithms.DecodePublicKey(key);
        DnsDelegationDigestResult result =
            DnssecAlgorithms.CreateDelegationDigest(
                Name("example", "com"),
                key,
                DnsDigestAlgorithm.Sha256,
                2);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Edwards, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.DelegationSigner, Is.Not.Null);
            Assert.That(result.DelegationSigner!.KeyTag, Is.EqualTo(3613));
            Assert.That(result.DelegationSigner.Algorithm,
                Is.EqualTo(DnsDnssecAlgorithm.Ed25519));
            Assert.That(result.DelegationSigner.DigestAlgorithm,
                Is.EqualTo(DnsDigestAlgorithm.Sha256));
            Assert.That(result.DelegationSigner.Digest, Is.EqualTo(Hex(
                "3aa5ab37efce57f737fc1627013fee07b" +
                "df241bd10f3b1964ab55c78e79a304b")));
        });
    }

    [TestCase(DnsDigestAlgorithm.Sha1, 20)]
    [TestCase(DnsDigestAlgorithm.Sha256, 32)]
    [TestCase(DnsDigestAlgorithm.Sha384, 48)]
    public void GeneratesEverySupportedDelegationDigest(
        DnsDigestAlgorithm algorithm,
        int expectedLength)
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.Ed25519,
            Convert.FromBase64String(
                "l02Woi0iS8Aa25FQkUd9RMzZHJpBoRQwAQEX1SxZJA4="));
        key.Flags = 257;

        DnsDelegationDigestResult result =
            DnssecAlgorithms.CreateDelegationDigest(
                Name("example", "com"),
                key,
                algorithm,
                (int)algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DelegationSigner!.Digest,
                Has.Length.EqualTo(expectedLength));
        });
    }

    [TestCase(
        DnsDigestAlgorithm.Sha1,
        "b2c63605467c4a40942b47a953e9c0d38f81083a")]
    [TestCase(
        DnsDigestAlgorithm.Sha384,
        "89389da437fca8372e67359dfc0dd4428fa2615df6e31bc5" +
        "501677dd068514fea5c4efaf82188530a8a1645d9d3ef884")]
    public void MatchesIndependentDelegationDigestFixtures(
        DnsDigestAlgorithm algorithm,
        string expectedDigest)
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.Ed25519,
            Convert.FromBase64String(
                "l02Woi0iS8Aa25FQkUd9RMzZHJpBoRQwAQEX1SxZJA4="));
        key.Flags = 257;

        DnsDelegationDigestResult result =
            DnssecAlgorithms.CreateDelegationDigest(
                Name("example", "com"),
                key,
                algorithm,
                (int)algorithm);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DelegationSigner, Is.Not.Null);
            Assert.That(result.DelegationSigner!.KeyTag, Is.EqualTo(3613));
            Assert.That(result.DelegationSigner.Digest,
                Is.EqualTo(Hex(expectedDigest)));
        });
    }

    [Test]
    public void ChecksSignatureTimesAcrossSerialWraparoundAndClockSkew()
    {
        DnsSignatureData signature = new DnsSignatureData();
        signature.SignatureInception = uint.MaxValue - 10;
        signature.SignatureExpiration = 20;

        DnsSignatureTimeResult valid =
            DnssecAlgorithms.CheckSignatureTime(signature, 5, 0);
        DnsSignatureTimeResult early =
            DnssecAlgorithms.CheckSignatureTime(
                signature,
                uint.MaxValue - 20,
                0);
        DnsSignatureTimeResult expired =
            DnssecAlgorithms.CheckSignatureTime(signature, 30, 0);
        DnsSignatureData skewedSignature = new DnsSignatureData();
        skewedSignature.SignatureInception = 100;
        skewedSignature.SignatureExpiration = 200;
        DnsSignatureTimeResult skewed =
            DnssecAlgorithms.CheckSignatureTime(
                skewedSignature,
                95,
                5);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success && valid.Valid, Is.True);
            Assert.That(early.Success && early.NotYetValid, Is.True);
            Assert.That(expired.Success && expired.Expired, Is.True);
            Assert.That(skewed.Success && skewed.Valid, Is.True);
        });
    }

    [Test]
    public void ResolvesExplicitAndDefaultAlgorithmPolicies()
    {
        DnsSecurityPolicy policy = new DnsSecurityPolicy();
        policy.DefaultAlgorithmPolicy = DnsAlgorithmPolicy.Forbidden;
        policy.DefaultDigestAlgorithmPolicy =
            DnsAlgorithmPolicy.Unsupported;
        policy.AlgorithmValues.Add(8);
        policy.AlgorithmPolicies.Add(DnsAlgorithmPolicy.Allowed);
        policy.DigestAlgorithmValues.Add(2);
        policy.DigestAlgorithmPolicies.Add(
            DnsAlgorithmPolicy.ValidateOnly);

        DnsAlgorithmPolicyResult algorithm =
            DnssecAlgorithms.ResolveAlgorithmPolicy(policy, 8);
        DnsAlgorithmPolicyResult defaultAlgorithm =
            DnssecAlgorithms.ResolveAlgorithmPolicy(policy, 15);
        DnsAlgorithmPolicyResult digest =
            DnssecAlgorithms.ResolveDigestPolicy(policy, 2);
        policy.AlgorithmValues.Add(15);
        DnsAlgorithmPolicyResult misaligned =
            DnssecAlgorithms.ResolveAlgorithmPolicy(policy, 15);

        Assert.Multiple(() =>
        {
            Assert.That(algorithm.Success, Is.True);
            Assert.That(algorithm.Policy,
                Is.EqualTo(DnsAlgorithmPolicy.Allowed));
            Assert.That(defaultAlgorithm.Policy,
                Is.EqualTo(DnsAlgorithmPolicy.Forbidden));
            Assert.That(digest.Policy,
                Is.EqualTo(DnsAlgorithmPolicy.ValidateOnly));
            Assert.That(misaligned.Success, Is.False);
        });
    }

    static DnsKeyData Key(
        DnsDnssecAlgorithm algorithm,
        byte[] publicKey)
    {
        DnsKeyData result = new DnsKeyData();
        result.Flags = 256;
        result.Protocol = 3;
        result.Algorithm = algorithm;
        result.AlgorithmValue = (int)algorithm;
        result.PublicKey = publicKey;
        return result;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        int index = 0;
        while (index < labels.Length)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return result;
    }

    static byte[] Hex(string text)
    {
        byte[] result = new byte[text.Length / 2];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Convert.ToByte(
                text.Substring(index * 2, 2),
                16);
            index = index + 1;
        }

        return result;
    }
}
