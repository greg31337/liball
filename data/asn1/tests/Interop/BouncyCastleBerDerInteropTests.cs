using asn1;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;

namespace asn1.Tests;

public class BouncyCastleBerDerInteropTests
{
    // RFC 5280 Appendix C.1 end-entity certificate.
    private const string Rfc5280CertificateBase64 =
        "MIICcTCCAdqgAwIBAgIBEjANBgkqhkiG9w0BAQUFADBDMRMwEQYKCZImiZPyLGQBGRYDY29tMRcwFQYKCZImiZPyLGQBGRYHZXhhbXBsZTETMBEGA1UEAxMKRXhhbXBsZSBDQTAeFw0wNDA5MTUxMTQ4MjFaFw0wNTAzMTUxMTQ4MjFaMEMxEzARBgoJkiaJk/IsZAEZFgNjb20xFzAVBgoJkiaJk/IsZAEZFgdleGFtcGxlMRMwEQYDVQQDEwpFbmQgRW50aXR5MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDhauQDMJcCPPQQ87UeTX8Ue/b10HjppIrwo3Xs7bZWln+ImYWa8j5od4frntGfwLQX3KuJI6QdfhYjTE+oTfUxuHyq4xpJCfRLJtsnZzCCEgFK6Rq2wQxTi2z8L3pD7DM2fjKye9WqzwEUxhLsE/ItFHqLIVgUE0xGo5ryFpX/IwIDAQABo3UwczAhBgNVHREEGjAYgRZlbmQuZW50aXR5QGV4YW1wbGUuY29tMB0GA1UdDgQWBBQXe5Iw/0TWZuGQECJsFk/AjkHdbTAfBgNVHSMEGDAWgBQIaK+FM8g5Snr4gpOOcGpKIIQsMjAOBgNVHQ8BAf8EBAMCBsAwDQYJKoZIhvcNAQEFBQADgYEAACAoNFtoMgG7CjYOrXHFlRrhBM+urcdiFKQbNjHA4gw92R7AANwQoLqFb0HLYnq3TGOBJl7SgEVeM+dwRTs5OyZKnDvyJjZpCHm7+5ZDd0thi6GrkWTg8zdhPBqjpMmKsr9z1E3kWORi6rwgdJKGDs6EYHbpc7vHhdORRepiXc0=";

    // OpenSSL 3.6.3: crl2pkcs7 -nocrl -certfile rfc5280.pem -outform DER.
    private const string OpenSslCmsSignedDataBase64 =
        "MIICoAYJKoZIhvcNAQcCoIICkTCCAo0CAQExADALBgkqhkiG9w0BBwGgggJ1MIICcTCCAdqgAwIBAgIBEjANBgkqhkiG9w0BAQUFADBDMRMwEQYKCZImiZPyLGQBGRYDY29tMRcwFQYKCZImiZPyLGQBGRYHZXhhbXBsZTETMBEGA1UEAxMKRXhhbXBsZSBDQTAeFw0wNDA5MTUxMTQ4MjFaFw0wNTAzMTUxMTQ4MjFaMEMxEzARBgoJkiaJk/IsZAEZFgNjb20xFzAVBgoJkiaJk/IsZAEZFgdleGFtcGxlMRMwEQYDVQQDEwpFbmQgRW50aXR5MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDhauQDMJcCPPQQ87UeTX8Ue/b10HjppIrwo3Xs7bZWln+ImYWa8j5od4frntGfwLQX3KuJI6QdfhYjTE+oTfUxuHyq4xpJCfRLJtsnZzCCEgFK6Rq2wQxTi2z8L3pD7DM2fjKye9WqzwEUxhLsE/ItFHqLIVgUE0xGo5ryFpX/IwIDAQABo3UwczAhBgNVHREEGjAYgRZlbmQuZW50aXR5QGV4YW1wbGUuY29tMB0GA1UdDgQWBBQXe5Iw/0TWZuGQECJsFk/AjkHdbTAfBgNVHSMEGDAWgBQIaK+FM8g5Snr4gpOOcGpKIIQsMjAOBgNVHQ8BAf8EBAMCBsAwDQYJKoZIhvcNAQEFBQADgYEAACAoNFtoMgG7CjYOrXHFlRrhBM+urcdiFKQbNjHA4gw92R7AANwQoLqFb0HLYnq3TGOBJl7SgEVeM+dwRTs5OyZKnDvyJjZpCHm7+5ZDd0thi6GrkWTg8zdhPBqjpMmKsr9z1E3kWORi6rwgdJKGDs6EYHbpc7vHhdORRepiXc0xAA==";

    [Test]
    public void Rfc5280CertificateMatchesBouncyCastleCanonicalDer()
    {
        byte[] fixture = Convert.FromBase64String(Rfc5280CertificateBase64);
        X509Certificate certificate = new X509CertificateParser()
            .ReadCertificate(fixture);
        byte[] bouncyCastleDer = Asn1Object.FromByteArray(fixture)
            .GetDerEncoded();
        BerReadResult read = ReadBer(fixture);
        BerTlv root = read.Tlv ?? throw new InvalidOperationException();
        BerWriteResult written = WriteDer(root);
        AsnCanonicalityResult canonical = ValidateDer(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(certificate.GetEncoded(), Is.EqualTo(fixture));
            Assert.That(bouncyCastleDer, Is.EqualTo(fixture));
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(read.ConsumedCount, Is.EqualTo(fixture.Length));
            Assert.That(root.Children, Has.Length.EqualTo(3));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Data, Is.EqualTo(bouncyCastleDer));
            Assert.That(canonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    [Test]
    public void OpenSslCmsSignedDataMatchesBouncyCastleCanonicalDer()
    {
        byte[] fixture = Convert.FromBase64String(OpenSslCmsSignedDataBase64);
        CmsSignedData cms = new CmsSignedData(new MemoryStream(fixture));
        byte[] bouncyCastleDer = cms.GetEncoded("DER");
        BerReadResult read = ReadBer(fixture);
        BerTlv root = read.Tlv ?? throw new InvalidOperationException();
        BerWriteResult written = WriteDer(root);
        AsnCanonicalityResult canonical = ValidateDer(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(cms.SignedContentType.Id,
                Is.EqualTo("1.2.840.113549.1.7.1"));
            Assert.That(bouncyCastleDer, Is.EqualTo(fixture));
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(read.ConsumedCount, Is.EqualTo(fixture.Length));
            Assert.That(root.Children, Has.Length.EqualTo(2));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Data, Is.EqualTo(bouncyCastleDer));
            Assert.That(canonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    [Test]
    public void SchemaLessBerTranscodingMatchesBouncyCastleCanonicalDer()
    {
        byte[] ber =
        [
            0x30, 0x80,
            0x24, 0x80,
            0x04, 0x01, 0x41,
            0x04, 0x01, 0x42,
            0x00, 0x00,
            0x31, 0x80,
            0x02, 0x01, 0x02,
            0x02, 0x01, 0x01,
            0x00, 0x00,
            0x00, 0x00
        ];
        byte[] bouncyCastleDer = Asn1Object.FromByteArray(ber)
            .GetDerEncoded();
        AsnEncodeResult transcoded = new BerCanonicalTranscoder(
            null,
            new AsnResourceLimits()).TranscodeTlv(
                ber,
                0,
                ber.Length,
                AsnEncodingRules.Der);
        AsnCanonicalityResult canonical = ValidateDer(transcoded.Data);

        Assert.Multiple(() =>
        {
            Assert.That(transcoded.Success, Is.True);
            Assert.That(transcoded.Data, Is.EqualTo(bouncyCastleDer));
            Assert.That(transcoded.Data, Is.EqualTo(new byte[]
            {
                0x30, 0x0c,
                0x04, 0x02, 0x41, 0x42,
                0x31, 0x06,
                0x02, 0x01, 0x01,
                0x02, 0x01, 0x02
            }));
            Assert.That(canonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    private static BerReadResult ReadBer(byte[] data)
    {
        return new BerTlvReaderImplementation(new AsnResourceLimits()).Read(
            new BerReadRequest
            {
                Data = data,
                EncodingRules = AsnEncodingRules.Ber,
                PreserveEncodedData = true
            });
    }

    private static BerWriteResult WriteDer(BerTlv tlv)
    {
        return new BerTlvWriterImplementation(new AsnResourceLimits()).Write(
            new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = AsnEncodingRules.Der
            });
    }

    private static AsnCanonicalityResult ValidateDer(byte[] data)
    {
        return new BerCanonicalValidator(new AsnResourceLimits())
            .ValidateInput(data, 0, data.Length, AsnEncodingRules.Der);
    }
}
