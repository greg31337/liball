using asn1;

namespace asn1.Tests;

public class BerDerConformanceVectorTests
{
    private const string Rfc5280Certificate =
        "MIICcTCCAdqgAwIBAgIBEjANBgkqhkiG9w0BAQUFADBDMRMwEQYKCZImiZPy"
        + "LGQBGRYDY29tMRcwFQYKCZImiZPyLGQBGRYHZXhhbXBsZTETMBEGA1UEAxMK"
        + "RXhhbXBsZSBDQTAeFw0wNDA5MTUxMTQ4MjFaFw0wNTAzMTUxMTQ4MjFaMEMx"
        + "EzARBgoJkiaJk/IsZAEZFgNjb20xFzAVBgoJkiaJk/IsZAEZFgdleGFtcGxl"
        + "MRMwEQYDVQQDEwpFbmQgRW50aXR5MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCB"
        + "iQKBgQDhauQDMJcCPPQQ87UeTX8Ue/b10HjppIrwo3Xs7bZWln+ImYWa8j5o"
        + "d4frntGfwLQX3KuJI6QdfhYjTE+oTfUxuHyq4xpJCfRLJtsnZzCCEgFK6Rq2"
        + "wQxTi2z8L3pD7DM2fjKye9WqzwEUxhLsE/ItFHqLIVgUE0xGo5ryFpX/IwID"
        + "AQABo3UwczAhBgNVHREEGjAYgRZlbmQuZW50aXR5QGV4YW1wbGUuY29tMB0G"
        + "A1UdDgQWBBQXe5Iw/0TWZuGQECJsFk/AjkHdbTAfBgNVHSMEGDAWgBQIaK+F"
        + "M8g5Snr4gpOOcGpKIIQsMjAOBgNVHQ8BAf8EBAMCBsAwDQYJKoZIhvcNAQEF"
        + "BQADgYEAACAoNFtoMgG7CjYOrXHFlRrhBM+urcdiFKQbNjHA4gw92R7AANwQ"
        + "oLqFb0HLYnq3TGOBJl7SgEVeM+dwRTs5OyZKnDvyJjZpCHm7+5ZDd0thi6Gr"
        + "kWTg8zdhPBqjpMmKsr9z1E3kWORi6rwgdJKGDs6EYHbpc7vHhdORRepiXc0=";

    private const string Rfc8017PublicKey =
        "MIGJAoGBAOFq5AMwlwI89BDztR5NfxR79vXQeOmkivCjdezttlaWf4iZhZry"
        + "Pmh3h+ue0Z/AtBfcq4kjpB1+FiNMT6hN9TG4fKrjGkkJ9Esm2ydnMIISAUrp"
        + "GrbBDFOLbPwvekPsMzZ+MrJ71arPARTGEuwT8i0UeoshWBQTTEajmvIWlf8jA"
        + "gMBAAE=";

    [Test]
    public void Rfc5280AppendixCertificateAndRfc8017RsaKeysAreCanonicalDer()
    {
        byte[] certificate = Convert.FromBase64String(Rfc5280Certificate);
        byte[] publicKey = Convert.FromBase64String(Rfc8017PublicKey);
        byte[] privateKey =
        [
            0x30, 0x1d,
            0x02, 0x01, 0x00,
            0x02, 0x02, 0x0c, 0xa1,
            0x02, 0x01, 0x11,
            0x02, 0x02, 0x0a, 0xc1,
            0x02, 0x01, 0x3d,
            0x02, 0x01, 0x35,
            0x02, 0x01, 0x35,
            0x02, 0x01, 0x31,
            0x02, 0x01, 0x26
        ];

        AsnCanonicalityResult certificateResult = Validate(certificate);
        AsnCanonicalityResult publicResult = Validate(publicKey);
        AsnCanonicalityResult privateResult = Validate(privateKey);

        Assert.Multiple(() =>
        {
            Assert.That(certificate, Has.Length.EqualTo(629));
            Assert.That(certificateResult.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
            Assert.That(publicResult.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
            Assert.That(privateResult.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    [Test]
    public void Rfc5280NestedViolationReportsItsOriginalCertificateOffset()
    {
        byte[] certificate = Convert.FromBase64String(Rfc5280Certificate);
        Assert.That(certificate[475], Is.EqualTo(0xff));
        certificate[475] = 0x01;

        AsnCanonicalityResult result = Validate(certificate);

        AssertViolation(result, 475, "ASNCAN0005");
    }

    [Test]
    public void GeneratedDerEdgesCoverHeadersScalarsTimesPaddingAndOrder()
    {
        byte[][] values =
        [
            [0x1f, 0x02, 0x01, 0x00],
            [0x02, 0x81, 0x01, 0x01],
            [0x01, 0x01, 0x01],
            [0x02, 0x02, 0x00, 0x01],
            [0x03, 0x02, 0x03, 0x07],
            [0x06, 0x02, 0x80, 0x00],
            [0x17, 0x0b, 0x30, 0x34, 0x30, 0x39, 0x31, 0x35, 0x31,
                0x31, 0x34, 0x38, 0x5a],
            [0x31, 0x06, 0x02, 0x01, 0x02, 0x02, 0x01, 0x01]
        ];
        int[] offsets = [0, 1, 2, 2, 3, 2, 12, 5];
        string[] codes =
        [
            "ASNCAN0002",
            "ASNCAN0003",
            "ASNCAN0005",
            "ASNCAN0005",
            "ASNCAN0006",
            "ASNCAN0005",
            "ASNCAN0005",
            "ASNCAN0008"
        ];

        int index = 0;
        while (index < values.Length)
        {
            AssertViolation(
                Validate(values[index]),
                offsets[index],
                codes[index]);
            index++;
        }
    }

    [Test]
    public void DerRejectsEndOfContentsAndPrimitiveConstructedUniversalTypes()
    {
        AsnCanonicalityResult endOfContents = Validate([0x00, 0x00]);
        AsnCanonicalityResult sequence = Validate([0x10, 0x00]);
        AsnCanonicalityResult nestedEndOfContents = Validate(
            [0x30, 0x02, 0x00, 0x00]);

        Assert.Multiple(() =>
        {
            AssertInvalid(endOfContents, 0);
            AssertInvalid(sequence, 0);
            AssertInvalid(nestedEndOfContents, 2);
        });
    }

    [Test]
    public void Rfc5280DefaultVersionIsOmittedByDerSchemaEncoding()
    {
        AsnFieldDescriptor version = new AsnFieldDescriptor
        {
            Name = "version",
            Type = new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Explicit,
                    Number = 0
                },
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.Universal,
                    Mode = AsnTaggingMode.Implicit,
                    Number = AsnUniversalTags.Integer,
                    Universal = true
                }
            ],
            HasDefaultValue = true,
            DefaultValue = IntegerDefault(0)
        };
        AsnTypeDescriptor certificate = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                version,
                new AsnFieldDescriptor
                {
                    Name = "serialNumber",
                    Type = new AsnTypeDescriptor
                    {
                        Kind = AsnTypeKind.Integer
                    }
                }
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("version", IntegerValue(0)),
                Present("serialNumber", IntegerValue(17))
            ]
        };

        BerSchemaEncodeResult encoded = new BerSchemaEncoder(
            new AsnReflectionCatalog()).EncodeTlv(
                certificate,
                value,
                new AsnCodecOptions
                {
                    EncodingRules = AsnEncodingRules.Der
                });
        byte[] data = Write(encoded);

        Assert.That(data,
            Is.EqualTo(new byte[] { 0x30, 0x03, 0x02, 0x01, 0x11 }));
    }

    private static AsnCanonicalityResult Validate(byte[] data)
    {
        return new BerCanonicalValidator(new AsnResourceLimits())
            .ValidateInput(data, 0, data.Length, AsnEncodingRules.Der);
    }

    private static void AssertViolation(
        AsnCanonicalityResult result,
        int offset,
        string code)
    {
        Assert.That(result.Status,
            Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
        Assert.That(result.HasViolationOffset, Is.True);
        Assert.That(result.ViolationOffset, Is.EqualTo(offset));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
    }

    private static void AssertInvalid(AsnCanonicalityResult result, int offset)
    {
        Assert.That(result.Status, Is.EqualTo(AsnCanonicalityStatus.Invalid));
        Assert.That(result.HasViolationOffset, Is.True);
        Assert.That(result.ViolationOffset, Is.EqualTo(offset));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("ASNCAN0010"));
    }

    private static AsnValueDescriptor IntegerDefault(int value)
    {
        return new AsnValueDescriptor
        {
            Kind = AsnValueKind.Integer,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
        };
    }

    private static AsnRuntimeValue IntegerValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnRuntimeFieldValue Present(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    private static byte[] Write(BerSchemaEncodeResult result)
    {
        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = result.Tlv,
                EncodingRules = AsnEncodingRules.Der
            });
        Assert.That(written.Success, Is.True, Format(written.Diagnostics));
        return written.Data;
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item =>
            item.Code + ":" + item.Message));
    }
}
