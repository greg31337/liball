using System.Security.Cryptography;
using System.Text;

namespace dns.Tests.Internal.Security;

public class DnssecSignatureAdaptersTests
{
    [Test]
    public void VerifiesTheRfc5702RsaSha256Example()
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.RsaSha256,
            Convert.FromBase64String(
                "AwEAAcFcGsaxxdgiuuGmCkVImy4h99CqT7jwY3pexPGcnUFt" +
                "R2Fh36BponcwtkZ4cAgtvd4Qs8PkxUdp6p/DlUmObdk="));
        DnsResourceRecordSet set = AddressSet(
            Name("www", "example", "net"),
            [192, 0, 2, 91]);
        DnsSignatureData rrsig = Signature(
            DnsDnssecAlgorithm.RsaSha256,
            9033,
            Name("example", "net"));
        rrsig.Signature = Convert.FromBase64String(
            "kRCOH6u7l0QGy9qpC9l1sLncJcOKFLJ7GhiUOibu4teYp5VE" +
            "9RncriShZNz85mwlMgNEacFYK/lPtPiVYP4bwg==");
        DnsEncodeResult signed =
            CanonicalDnsData.BuildRrsigSignedData(set, rrsig);

        DnsSignatureAdapterResult result =
            DnssecSignatureAdapters.VerifySignature(
                key,
                signed.Bytes,
                rrsig.Signature);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Supported, Is.True);
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [TestCase(DnsDnssecAlgorithm.RsaSha1, "SHA1")]
    [TestCase(DnsDnssecAlgorithm.RsaNsec3Sha1, "SHA1")]
    [TestCase(DnsDnssecAlgorithm.RsaSha256, "SHA256")]
    [TestCase(DnsDnssecAlgorithm.RsaSha512, "SHA512")]
    public void VerifiesRsaAlgorithmsAndRejectsAlteredData(
        DnsDnssecAlgorithm algorithm,
        string hashName)
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        RSAParameters parameters = rsa.ExportParameters(false);
        byte[] data = Encoding.ASCII.GetBytes("dnssec-rsa-adapter");
        HashAlgorithmName hash = new HashAlgorithmName(hashName);
        byte[] signature = rsa.SignData(
            data,
            hash,
            RSASignaturePadding.Pkcs1);
        DnsKeyData key = Key(algorithm, RsaDnskey(parameters));

        DnsSignatureAdapterResult valid =
            DnssecSignatureAdapters.VerifySignature(key, data, signature);
        data[0] = (byte)(data[0] ^ 1);
        DnsSignatureAdapterResult altered =
            DnssecSignatureAdapters.VerifySignature(key, data, signature);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Supported, Is.True);
            Assert.That(valid.Valid, Is.True);
            Assert.That(altered.Success, Is.True);
            Assert.That(altered.Valid, Is.False);
        });
    }

    [TestCase(DnsDnssecAlgorithm.EcdsaP256Sha256, "SHA256", 64)]
    [TestCase(DnsDnssecAlgorithm.EcdsaP384Sha384, "SHA384", 96)]
    public void VerifiesEcdsaAlgorithmsAndRejectsAlteredSignatures(
        DnsDnssecAlgorithm algorithm,
        string hashName,
        int publicKeyLength)
    {
        ECCurve curve = ECCurve.NamedCurves.nistP256;
        if (algorithm == DnsDnssecAlgorithm.EcdsaP384Sha384)
        {
            curve = ECCurve.NamedCurves.nistP384;
        }

        using ECDsa ecdsa = ECDsa.Create(curve);
        ECParameters parameters = ecdsa.ExportParameters(false);
        byte[] data = Encoding.ASCII.GetBytes("dnssec-ecdsa-adapter");
        byte[] signature = ecdsa.SignData(
            data,
            new HashAlgorithmName(hashName),
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        DnsKeyData key = Key(algorithm, EcdsaDnskey(parameters));
        DnsSignatureAdapterResult valid =
            DnssecSignatureAdapters.VerifySignature(key, data, signature);
        signature[0] = (byte)(signature[0] ^ 1);
        DnsSignatureAdapterResult altered =
            DnssecSignatureAdapters.VerifySignature(key, data, signature);

        Assert.Multiple(() =>
        {
            Assert.That(key.PublicKey, Has.Length.EqualTo(publicKeyLength));
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Supported, Is.True);
            Assert.That(valid.Valid, Is.True);
            Assert.That(altered.Success, Is.True);
            Assert.That(altered.Valid, Is.False);
        });
    }

    [Test]
    public void ReportsEd25519AsUnavailableOnTheCurrentDotnetApi()
    {
        DnsKeyData key = Key(
            DnsDnssecAlgorithm.Ed25519,
            Convert.FromBase64String(
                "l02Woi0iS8Aa25FQkUd9RMzZHJpBoRQwAQEX1SxZJA4="));

        DnsSignatureAdapterResult result =
            DnssecSignatureAdapters.VerifySignature(
                key,
                [1, 2, 3],
                new byte[64]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Supported, Is.False);
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnsupportedAlgorithm));
        });
    }

    [Test]
    public void RejectsMalformedKeysWithoutInvokingTheProvider()
    {
        DnsKeyData rsa = Key(
            DnsDnssecAlgorithm.RsaSha256,
            RsaDnskey(new RSAParameters
            {
                Exponent = [2],
                Modulus = CreateOddBytes(64)
            }));
        DnsKeyData ecdsa = Key(
            DnsDnssecAlgorithm.EcdsaP256Sha256,
            new byte[64]);

        DnsSignatureAdapterResult badRsa =
            DnssecSignatureAdapters.VerifySignature(
                rsa,
                [1],
                new byte[64]);
        DnsSignatureAdapterResult badEcdsa =
            DnssecSignatureAdapters.VerifySignature(
                ecdsa,
                [1],
                new byte[64]);

        Assert.Multiple(() =>
        {
            Assert.That(badRsa.Success, Is.False);
            Assert.That(badRsa.Supported, Is.True);
            Assert.That(badRsa.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
            Assert.That(badEcdsa.Success, Is.False);
            Assert.That(badEcdsa.Supported, Is.True);
            Assert.That(badEcdsa.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        });
    }

    static DnsKeyData Key(
        DnsDnssecAlgorithm algorithm,
        byte[] publicKey)
    {
        DnsKeyData key = new DnsKeyData();
        key.Flags = 256;
        key.Protocol = 3;
        key.Algorithm = algorithm;
        key.AlgorithmValue = (int)algorithm;
        key.PublicKey = publicKey;
        return key;
    }

    static byte[] RsaDnskey(RSAParameters parameters)
    {
        byte[] exponent = parameters.Exponent ?? [];
        byte[] modulus = parameters.Modulus ?? [];
        byte[] result = new byte[1 + exponent.Length + modulus.Length];
        result[0] = (byte)exponent.Length;
        Array.Copy(exponent, 0, result, 1, exponent.Length);
        Array.Copy(
            modulus,
            0,
            result,
            1 + exponent.Length,
            modulus.Length);
        return result;
    }

    static byte[] EcdsaDnskey(ECParameters parameters)
    {
        byte[] x = parameters.Q.X ?? [];
        byte[] y = parameters.Q.Y ?? [];
        byte[] result = new byte[x.Length + y.Length];
        Array.Copy(x, 0, result, 0, x.Length);
        Array.Copy(y, 0, result, x.Length, y.Length);
        return result;
    }

    static byte[] CreateOddBytes(int length)
    {
        byte[] result = new byte[length];
        result[0] = 1;
        result[length - 1] = 1;
        return result;
    }

    static DnsResourceRecordSet AddressSet(
        DnsName owner,
        byte[] address)
    {
        DnsAddressData addressData = new DnsAddressData();
        addressData.Address = address;
        DnsRecordData data = new DnsRecordData();
        data.Address = addressData;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner;
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 3600;
        record.Data = data;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = owner;
        set.RecordType = DnsRecordType.A;
        set.RecordTypeValue = 1;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 3600;
        set.Records.Add(record);
        return set;
    }

    static DnsSignatureData Signature(
        DnsDnssecAlgorithm algorithm,
        int keyTag,
        DnsName signerName)
    {
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = DnsRecordType.A;
        signature.TypeCoveredValue = 1;
        signature.Algorithm = algorithm;
        signature.AlgorithmValue = (int)algorithm;
        signature.Labels = 3;
        signature.OriginalTimeToLive = 3600;
        signature.SignatureExpiration = 1893456000;
        signature.SignatureInception = 946684800;
        signature.KeyTag = keyTag;
        signature.SignerName = signerName;
        return signature;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName name = new DnsName();
        name.IsAbsolute = true;
        int index = 0;
        while (index < labels.Length)
        {
            name.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return name;
    }
}
