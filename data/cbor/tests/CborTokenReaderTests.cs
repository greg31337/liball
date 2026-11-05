using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenReaderTests
{
    [Test]
    public void DefiniteMajorTypesExposeTheirSemanticPayloads()
    {
        CborToken unsigned = ReadOne([0x1b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]).Token!;
        CborToken negative = ReadOne([0x3b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]).Token!;
        CborToken bytes = ReadOne([0x43, 0x00, 0x80, 0xff]).Token!;
        CborToken text = ReadOne([0x63, 0xe2, 0x82, 0xac]).Token!;
        CborToken array = ReadOne([0x98, 0x18]).Token!;
        CborToken map = ReadOne([0xb9, 0x01, 0x00]).Token!;
        CborToken tag = ReadOne([0xdb, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]).Token!;

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(unsigned.IntegerValue, Is.EqualTo(new BigInteger(ulong.MaxValue)));
            Assert.That(negative.Kind, Is.EqualTo(CborTokenKind.NegativeInteger));
            Assert.That(negative.IntegerValue, Is.EqualTo(BigInteger.Parse("-18446744073709551616")));
            Assert.That(bytes.Kind, Is.EqualTo(CborTokenKind.ByteString));
            Assert.That(bytes.ByteStringValue, Is.EqualTo(new byte[] { 0x00, 0x80, 0xff }));
            Assert.That(bytes.Length, Is.EqualTo(3));
            Assert.That(bytes.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(text.Kind, Is.EqualTo(CborTokenKind.TextString));
            Assert.That(text.TextStringValue, Is.EqualTo("€"));
            Assert.That(text.Length, Is.EqualTo(3));
            Assert.That(text.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(array.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(array.Length, Is.EqualTo(24));
            Assert.That(array.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(map.Kind, Is.EqualTo(CborTokenKind.StartMap));
            Assert.That(map.Length, Is.EqualTo(256));
            Assert.That(map.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(tag.Kind, Is.EqualTo(CborTokenKind.Tag));
            Assert.That(tag.TagNumber, Is.EqualTo(ulong.MaxValue));
        });
    }

    [Test]
    public void InlineAndExtendedSimpleValuesCoverEveryLegalForm()
    {
        CborTokenReader reader = new CborTokenReader(new CborDecodeOptions());

        for (int value = 0; value <= 23; value++)
        {
            CborTokenReadResult result = reader.ReadToken([(byte)(0xe0 | value)]);
            if (result.Status != CborReadStatus.Done
                || result.Token?.Kind != CborTokenKind.SimpleValue
                || result.Token.SimpleValue != value
                || result.Consumed != 1)
            {
                Assert.Fail($"Inline simple value {value} was not tokenized.");
            }
        }

        CborTokenReadResult firstExtended = reader.ReadToken([0xf8, 0x20]);
        CborTokenReadResult lastExtended = reader.ReadToken([0xf8, 0xff]);
        CborTokenReadResult invalidExtended = reader.ReadToken([0xf8, 0x1f]);

        Assert.Multiple(() =>
        {
            Assert.That(firstExtended.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(firstExtended.Token?.SimpleValue, Is.EqualTo(32));
            Assert.That(firstExtended.Consumed, Is.EqualTo(2));
            Assert.That(lastExtended.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(lastExtended.Token?.SimpleValue, Is.EqualTo(255));
            Assert.That(invalidExtended.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(invalidExtended.ErrorCode, Is.EqualTo("CBOR_INVALID_SIMPLE_VALUE_ENCODING"));
        });
    }

    [Test]
    public void EveryFloatFormPreservesItsExactWidthAndBits()
    {
        CborTokenReadResult halfResult = ReadOne([0xf9, 0x7d, 0x55]);
        CborTokenReadResult singleResult = ReadOne([0xfa, 0x7f, 0xa1, 0x23, 0x45]);
        CborTokenReadResult doubleResult = ReadOne(
            [0xfb, 0xff, 0xf8, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc]);
        CborToken half = halfResult.Token!;
        CborToken single = singleResult.Token!;
        CborToken doubleToken = doubleResult.Token!;

        Assert.Multiple(() =>
        {
            Assert.That(half.Kind, Is.EqualTo(CborTokenKind.Float));
            Assert.That(half.FloatWidth, Is.EqualTo(CborFloatWidth.Half));
            Assert.That(half.FloatValue?.RawBits, Is.EqualTo(0x7d55));
            Assert.That(half.FloatValue?.IsSignalingNaN, Is.True);
            Assert.That(halfResult.Consumed, Is.EqualTo(3));
            Assert.That(single.FloatWidth, Is.EqualTo(CborFloatWidth.Single));
            Assert.That(single.FloatValue?.RawBits, Is.EqualTo(0x7fa12345));
            Assert.That(single.FloatValue?.IsSignalingNaN, Is.True);
            Assert.That(singleResult.Consumed, Is.EqualTo(5));
            Assert.That(doubleToken.FloatWidth, Is.EqualTo(CborFloatWidth.Double));
            Assert.That(doubleToken.FloatValue?.RawBits, Is.EqualTo(0xfff8123456789abc));
            Assert.That(doubleToken.FloatValue?.IsSignalingNaN, Is.False);
            Assert.That(doubleResult.Consumed, Is.EqualTo(9));
        });
    }

    [Test]
    public void ConsumptionRemainingAndRepresentationTrackExactSourceRanges()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        CborTokenReader reader = new CborTokenReader(options);

        CborTokenReadResult integer = reader.ReadToken([0x18, 0x18, 0xaa]);
        CborTokenReadResult bytes = reader.ReadToken([0x42, 0x10, 0x20, 0xbb]);
        CborTokenReadResult floating = reader.ReadToken([0xfa, 0x3f, 0xc0, 0x00, 0x00, 0xcc]);

        Assert.Multiple(() =>
        {
            Assert.That(integer.Consumed, Is.EqualTo(2));
            Assert.That(integer.Remaining, Is.EqualTo(1));
            Assert.That(integer.Token?.Representation?.HeadArgumentWidth, Is.EqualTo(1));
            Assert.That(integer.Token?.Representation?.HasSourceRange, Is.True);
            Assert.That(integer.Token?.Representation?.SourceOffset, Is.Zero);
            Assert.That(integer.Token?.Representation?.SourceLength, Is.EqualTo(2));
            Assert.That(bytes.Consumed, Is.EqualTo(3));
            Assert.That(bytes.Remaining, Is.EqualTo(1));
            Assert.That(bytes.Token?.Representation?.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(bytes.Token?.Representation?.SourceOffset, Is.EqualTo(2));
            Assert.That(bytes.Token?.Representation?.SourceLength, Is.EqualTo(3));
            Assert.That(floating.Consumed, Is.EqualTo(5));
            Assert.That(floating.Remaining, Is.EqualTo(1));
            Assert.That(floating.Token?.Representation?.FloatWidth, Is.EqualTo(CborFloatWidth.Single));
            Assert.That(floating.Token?.Representation?.FloatBits, Is.EqualTo(0x3fc00000));
            Assert.That(floating.Token?.Representation?.HasFloatBits, Is.True);
            Assert.That(floating.Token?.Representation?.SourceOffset, Is.EqualTo(5));
            Assert.That(floating.Token?.Representation?.SourceLength, Is.EqualTo(5));
        });

        reader.ResetReader();
        CborTokenReadResult reset = reader.ReadToken([0x00]);
        Assert.That(reset.Token?.Representation?.SourceOffset, Is.Zero);
    }

    [Test]
    public void IncompleteHeadsAndStringPayloadsAreBufferedAsConsumedInput()
    {
        byte[][] inputs =
        [
            [],
            [0x1a],
            [0x1a, 0x00],
            [0x1a, 0x00, 0x00],
            [0x1a, 0x00, 0x00, 0x00],
            [0x44],
            [0x44, 0x01],
            [0x44, 0x01, 0x02],
            [0x44, 0x01, 0x02, 0x03],
            [0x63, 0x61, 0x62]
        ];

        foreach (byte[] input in inputs)
        {
            CborTokenReadResult result = ReadOne(input);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(result.Token, Is.Null);
                Assert.That(result.Consumed, Is.EqualTo(input.Length));
                Assert.That(result.Remaining, Is.Zero);
                Assert.That(result.ErrorCode, Is.Null);
            });
        }
    }

    [Test]
    public void InvalidTextLengthsSimplePoliciesAndIndefiniteFormsReportErrors()
    {
        CborTokenReadResult invalidUtf8 = ReadOne([0x63, 0xe2, 0x28, 0xa1]);
        CborTokenReadResult oversizedLength = ReadOne(
            [0x5b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);
        CborTokenReadResult indefinite = ReadOne([0x9f]);
        CborDecodeOptions strictSimpleOptions = new CborDecodeOptions
        {
            AllowUnknownSimpleValues = false
        };
        CborTokenReader strictSimpleReader = new CborTokenReader(strictSimpleOptions);
        CborTokenReadResult unknownInline = strictSimpleReader.ReadToken([0xe0]);
        CborTokenReadResult known = strictSimpleReader.ReadToken([0xf4]);
        CborTokenReadResult unknownExtended = strictSimpleReader.ReadToken([0xf8, 0x20]);

        Assert.Multiple(() =>
        {
            Assert.That(invalidUtf8.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(invalidUtf8.ErrorCode, Is.EqualTo("CBOR_UTF8_INVALID_CONTINUATION"));
            Assert.That(invalidUtf8.ErrorOffset, Is.EqualTo(2));
            Assert.That(oversizedLength.ErrorCode, Is.EqualTo("CBOR_MAX_STRING_BYTES"));
            Assert.That(indefinite.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(indefinite.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(indefinite.Token?.LengthKind, Is.EqualTo(CborLengthKind.Indefinite));
            Assert.That(unknownInline.ErrorCode, Is.EqualTo("CBOR_UNKNOWN_SIMPLE_VALUE"));
            Assert.That(known.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(known.Token?.SimpleValue, Is.EqualTo(CborSimpleValues.False));
            Assert.That(unknownExtended.ErrorCode, Is.EqualTo("CBOR_UNKNOWN_SIMPLE_VALUE"));
        });
    }

    [Test]
    public void InvalidUtf8CanBeDecodedWithReplacementWhenConfigured()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            RejectInvalidUtf8 = false
        };
        CborTokenReader reader = new CborTokenReader(options);

        CborTokenReadResult result = reader.ReadToken([0x63, 0xe2, 0x28, 0xa1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(4));
            Assert.That(result.Token?.Kind, Is.EqualTo(CborTokenKind.TextString));
            Assert.That(result.Token?.TextStringValue, Is.EqualTo("�(�"));
        });
    }

    private static CborTokenReadResult ReadOne(byte[] source)
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        return new CborTokenReader(options).ReadToken(source);
    }
}
