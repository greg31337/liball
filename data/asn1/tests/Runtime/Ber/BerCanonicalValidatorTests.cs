using asn1;

namespace asn1.Tests;

public class BerCanonicalValidatorTests
{
    [Test]
    public void CanonicalDerAndCerValuesPassWithoutViolationOffsets()
    {
        AsnCanonicalityResult der = Validate(
            [0x30, 0x06, 0x01, 0x01, 0xff, 0x02, 0x01, 0x01],
            AsnEncodingRules.Der);
        AsnCanonicalityResult cer = Validate(
            [0x30, 0x80, 0x01, 0x01, 0xff, 0x00, 0x00],
            AsnEncodingRules.Cer);
        byte[] canonicalUtf8 = Write(
            new BerPrimitiveCodec(new AsnResourceLimits()).EncodePrimitive(
                AsnTypeKind.Utf8String,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = new string('A', 999) + "😀"
                },
                AsnEncodingRules.Cer).Tlv,
            AsnEncodingRules.Cer);
        AsnCanonicalityResult utf8 = Validate(
            canonicalUtf8,
            AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            Assert.That(der.Status, Is.EqualTo(AsnCanonicalityStatus.Canonical));
            Assert.That(der.HasViolationOffset, Is.False);
            Assert.That(cer.Status, Is.EqualTo(AsnCanonicalityStatus.Canonical));
            Assert.That(cer.HasViolationOffset, Is.False);
            Assert.That(utf8.Status, Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    [Test]
    public void HeaderViolationsReportTheirExactOriginalInputOffsets()
    {
        BerCanonicalValidator validator = CreateValidator();
        byte[] highTag = [0xaa, 0xbb, 0x1f, 0x80, 0x02, 0x01, 0x01];
        AsnCanonicalityResult identifier = validator.ValidateInput(
            highTag,
            2,
            5,
            AsnEncodingRules.Der);
        AsnCanonicalityResult length = Validate(
            [0x02, 0x81, 0x01, 0x01],
            AsnEncodingRules.Der);
        AsnCanonicalityResult derIndefinite = Validate(
            [0x30, 0x80, 0x02, 0x01, 0x01, 0x00, 0x00],
            AsnEncodingRules.Der);
        AsnCanonicalityResult cerDefinite = Validate(
            [0x30, 0x03, 0x02, 0x01, 0x01],
            AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            AssertViolation(identifier, 2, "ASNCAN0002");
            AssertViolation(length, 1, "ASNCAN0003");
            AssertViolation(derIndefinite, 1, "ASNCAN0003");
            AssertViolation(cerDefinite, 1, "ASNCAN0003");
        });
    }

    [Test]
    public void ScalarPaddingAndTimeViolationsAreDetectedFromInputBytes()
    {
        AsnCanonicalityResult boolean = Validate(
            [0x01, 0x01, 0x01],
            AsnEncodingRules.Der);
        AsnCanonicalityResult integer = Validate(
            [0x02, 0x02, 0x00, 0x01],
            AsnEncodingRules.Der);
        AsnCanonicalityResult padding = Validate(
            [0x03, 0x02, 0x03, 0x07],
            AsnEncodingRules.Der);
        AsnCanonicalityResult oid = Validate(
            [0x06, 0x02, 0x80, 0x00],
            AsnEncodingRules.Der);
        AsnCanonicalityResult real = Validate(
            [0x09, 0x04, 0x81, 0x00, 0x00, 0x01],
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult time = new BerUsefulTypeCodec(
            new AsnResourceLimits()).EncodeUsefulType(
                AsnTypeKind.GeneralizedTime,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Time,
                    TimeValue = new AsnTimeValue
                    {
                        Kind = AsnTimeKind.GeneralizedTime,
                        Text = "20260828153045.1200-0700"
                    }
                },
                AsnEncodingRules.Ber);
        AsnCanonicalityResult nonCanonicalTime = Validate(
            Write(time.Tlv, AsnEncodingRules.Ber),
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            AssertViolation(boolean, 2, "ASNCAN0005");
            AssertViolation(integer, 2, "ASNCAN0005");
            AssertViolation(padding, 3, "ASNCAN0006");
            AssertViolation(oid, 2, "ASNCAN0005");
            AssertViolation(real, 3, "ASNCAN0005");
            Assert.That(nonCanonicalTime.Status,
                Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
            Assert.That(nonCanonicalTime.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0005"));
        });
    }

    [Test]
    public void CerEnforcesPrimitiveThresholdsAndCanonicalSegmentation()
    {
        byte[] longPrimitive = new byte[1005];
        longPrimitive[0] = 0x04;
        longPrimitive[1] = 0x82;
        longPrimitive[2] = 0x03;
        longPrimitive[3] = 0xe9;
        AsnCanonicalityResult primitive = Validate(
            longPrimitive,
            AsnEncodingRules.Cer);

        BerTlv badSegments = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = AsnUniversalTags.OctetString
            },
            Constructed = true,
            IndefiniteLength = true,
            Children =
            [
                OctetFragment(999),
                OctetFragment(2)
            ]
        };
        AsnCanonicalityResult segmented = Validate(
            Write(badSegments, AsnEncodingRules.Cer),
            AsnEncodingRules.Cer);
        BerTlv shortConstructed = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = AsnUniversalTags.OctetString
            },
            Constructed = true,
            IndefiniteLength = true,
            Children = [OctetFragment(10)]
        };
        AsnCanonicalityResult shortValue = Validate(
            Write(shortConstructed, AsnEncodingRules.Cer),
            AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            AssertViolation(primitive, 0, "ASNCAN0007");
            Assert.That(segmented.Status,
                Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
            Assert.That(segmented.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0007"));
            AssertViolation(shortValue, 0, "ASNCAN0007");
        });
    }

    [Test]
    public void SetOrderingAndEarliestNestedViolationAreStable()
    {
        AsnCanonicalityResult order = Validate(
            [0x31, 0x06, 0x02, 0x01, 0x02, 0x02, 0x01, 0x01],
            AsnEncodingRules.Der);
        AsnCanonicalityResult earliest = Validate(
            [0x30, 0x05, 0x01, 0x01, 0x01, 0x05, 0x00],
            AsnEncodingRules.Cer);
        AsnCanonicalityResult malformed = Validate(
            [0x05, 0x01, 0x00],
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            AssertViolation(order, 5, "ASNCAN0008");
            AssertViolation(earliest, 1, "ASNCAN0003");
            Assert.That(malformed.Status,
                Is.EqualTo(AsnCanonicalityStatus.Invalid));
            Assert.That(malformed.HasViolationOffset, Is.True);
        });
    }

    private static BerCanonicalValidator CreateValidator()
    {
        return new BerCanonicalValidator(new AsnResourceLimits());
    }

    private static AsnCanonicalityResult Validate(
        byte[] data,
        AsnEncodingRules rules)
    {
        return CreateValidator().ValidateInput(data, 0, data.Length, rules);
    }

    private static byte[] Write(BerTlv? tlv, AsnEncodingRules rules)
    {
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = rules
            });
        Assert.That(written.Success, Is.True);
        return written.Data;
    }

    private static BerTlv OctetFragment(int count)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = AsnUniversalTags.OctetString
            },
            Content = new byte[count]
        };
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
}
