using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborKnownTagValidationTests
{
    [Test]
    public void DateTimeTagsValidateRfc3339SyntaxAndEpochRange()
    {
        CborValidator validator = new CborValidator();
        CborValidationResult valid = validator.ValidateValue(
            Tag(0, Text("1996-12-19T16:39:57.123456789-08:00")),
            CborValidationLevel.TagValidity);
        CborValidationResult leapSecond = validator.ValidateValue(
            Tag(0, Text("1990-12-31T23:59:60Z")),
            CborValidationLevel.TagValidity);
        CborValidationResult invalidDate = validator.ValidateValue(
            Tag(0, Text("2023-02-29T12:00:00Z")),
            CborValidationLevel.TagValidity);
        CborValidationResult invalidEpoch = validator.ValidateValue(
            Tag(1, Float(double.PositiveInfinity)),
            CborValidationLevel.TagValidity);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(leapSecond.IsValid, Is.True);
            AssertIssue(invalidDate, "CBOR_DATE_TIME_SYNTAX");
            AssertIssue(invalidEpoch, "CBOR_EPOCH_DATE_TIME_RANGE");
        });
    }

    [Test]
    public void GenericDecodeValidatesKnownTagsUnlessPreservationIsRequested()
    {
        byte[] invalidBignum = Convert.FromHexString("C201");
        CborDecoder decoder = new CborDecoder();

        CborReadResult validated = decoder.Decode(
            invalidBignum,
            new CborDecodeOptions());
        CborReadResult preserved = decoder.Decode(
            invalidBignum,
            new CborDecodeOptions { TagPolicy = CborTagPolicy.Preserve });

        Assert.Multiple(() =>
        {
            Assert.That(validated.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(validated.ErrorCode, Is.EqualTo("CBOR_BIGNUM_TYPE"));
            Assert.That(preserved.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(preserved.Value?.Kind, Is.EqualTo(CborValueKind.Tag));
        });
    }

    [Test]
    public void DeterministicValidationRequiresPreferredBignums()
    {
        CborValue small = Tag(2, Bytes([0x01]));
        CborValue leadingZero = Tag(
            2,
            Bytes([0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));
        CborValue preferred = Tag(
            2,
            Bytes([0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));
        CborValidator validator = new CborValidator();

        CborValidationResult tagValid = validator.ValidateValue(
            small,
            CborValidationLevel.TagValidity);
        CborValidationResult smallDeterministic = validator.ValidateValue(
            small,
            CborValidationLevel.Deterministic);
        CborValidationResult zeroDeterministic = validator.ValidateValue(
            leadingZero,
            CborValidationLevel.Deterministic);
        CborValidationResult preferredDeterministic = validator.ValidateValue(
            preferred,
            CborValidationLevel.Deterministic);

        Assert.Multiple(() =>
        {
            Assert.That(tagValid.IsValid, Is.True);
            AssertIssue(smallDeterministic, "CBOR_BIGNUM_NOT_PREFERRED");
            AssertIssue(zeroDeterministic, "CBOR_BIGNUM_NOT_PREFERRED");
            Assert.That(preferredDeterministic.IsValid, Is.True);
        });
    }

    [TestCase(CborTagNumbers.DecimalFraction)]
    [TestCase(CborTagNumbers.Bigfloat)]
    public void DecimalAndBigfloatRequireExponentMantissaShape(ulong tagNumber)
    {
        CborValue bignumMantissa = Tag(
            CborTagNumbers.PositiveBignum,
            Bytes([0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]));
        CborValue valid = Tag(tagNumber, Array([Integer(-2), bignumMantissa]));
        CborValue wrongCount = Tag(tagNumber, Array([Integer(0)]));
        CborValue wrongExponent = Tag(tagNumber, Array([Text("zero"), Integer(1)]));
        CborValue wrongMantissa = Tag(tagNumber, Array([Integer(0), Float(1.0)]));
        CborValidator validator = new CborValidator();

        CborValidationResult validResult = validator.ValidateValue(
            valid,
            CborValidationLevel.TagValidity);
        CborValidationResult countResult = validator.ValidateValue(
            wrongCount,
            CborValidationLevel.TagValidity);
        CborValidationResult exponentResult = validator.ValidateValue(
            wrongExponent,
            CborValidationLevel.TagValidity);
        CborValidationResult mantissaResult = validator.ValidateValue(
            wrongMantissa,
            CborValidationLevel.TagValidity);

        Assert.Multiple(() =>
        {
            Assert.That(validResult.IsValid, Is.True);
            AssertIssue(countResult, "CBOR_BIG_NUMBER_PAIR_TYPE");
            AssertIssue(exponentResult, "CBOR_BIG_NUMBER_PAIR_TYPE");
            AssertIssue(mantissaResult, "CBOR_BIG_NUMBER_PAIR_TYPE");
        });
    }

    [Test]
    public void EncodedCborRequiresExactlyOneWellFormedItem()
    {
        CborValidator validator = new CborValidator();
        CborValidationResult valid = validator.ValidateEncoding(
            Convert.FromHexString("D8184100"),
            CborValidationLevel.TagValidity);
        CborValidationResult empty = validator.ValidateEncoding(
            Convert.FromHexString("D81840"),
            CborValidationLevel.TagValidity);
        CborValidationResult twoItems = validator.ValidateEncoding(
            Convert.FromHexString("D818420001"),
            CborValidationLevel.TagValidity);
        CborValidationResult malformed = validator.ValidateEncoding(
            Convert.FromHexString("D818411C"),
            CborValidationLevel.TagValidity);

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            AssertIssue(empty, "CBOR_EMBEDDED_CBOR_ITEM_COUNT");
            AssertIssue(twoItems, "CBOR_EMBEDDED_CBOR_ITEM_COUNT");
            AssertIssue(malformed, "CBOR_RESERVED_ADDITIONAL_INFORMATION");
        });
    }

    [Test]
    public void EmbeddedCborHonorsRecursiveDepthLimit()
    {
        byte[] nested = Convert.FromHexString("D81844D8184100");
        CborValidator validator = new CborValidator();
        CborValidationResult rejected = validator.ValidateEncodingWithOptions(
            nested,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.TagValidity,
                MaxEmbeddedDepth = 1
            });
        CborValidationResult accepted = validator.ValidateEncodingWithOptions(
            nested,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.TagValidity,
                MaxEmbeddedDepth = 2
            });

        Assert.Multiple(() =>
        {
            AssertIssue(rejected, "CBOR_MAX_EMBEDDED_DEPTH");
            Assert.That(accepted.IsValid, Is.True);
        });
    }

    [Test]
    public void EncodingAndTextHintsValidateTheirDefinedContentSemantics()
    {
        CborValidator validator = new CborValidator();
        CborValidationResult recursiveHint = validator.ValidateValue(
            Tag(21, Array([Bytes([0x01]), Integer(2)])),
            CborValidationLevel.TagValidity);
        CborValidationResult invalidBase64Url = validator.ValidateValue(
            Tag(33, Text("AB")),
            CborValidationLevel.TagValidity);
        CborValidationResult invalidBase64 = validator.ValidateValue(
            Tag(34, Text("AB==")),
            CborValidationLevel.TagValidity);
        CborValidationResult invalidUri = validator.ValidateValue(
            Tag(32, Text("https://example.com/a b")),
            CborValidationLevel.TagValidity);

        Assert.Multiple(() =>
        {
            Assert.That(recursiveHint.IsValid, Is.True);
            AssertIssue(invalidBase64Url, "CBOR_BASE64URL_TEXT_INVALID");
            AssertIssue(invalidBase64, "CBOR_BASE64_TEXT_INVALID");
            AssertIssue(invalidUri, "CBOR_URI_TEXT_INVALID");
        });
    }

    private static CborValue Tag(ulong tagNumber, CborValue content)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = tagNumber,
            TaggedValue = content
        };
    }

    private static CborValue Array(CborValue[] items)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [.. items]
        };
    }

    private static CborValue Bytes(byte[] value)
    {
        return new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = value
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = value < 0
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Float(double value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Double,
            FloatValue = new CborFloatValue
            {
                Value = value,
                Width = CborFloatWidth.Double
            }
        };
    }

    private static void AssertIssue(CborValidationResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain(code));
        });
    }
}
