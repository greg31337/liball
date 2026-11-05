using System.Numerics;
using System.Text.RegularExpressions;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborBuiltInTagTests
{
    [Test]
    public void RegistryExposesEveryBuiltInHandlerAndAllowsOverrides()
    {
        ulong[] tagNumbers =
        [
            0, 1, 2, 3, 4, 5, 21, 22, 23, 24,
            32, 33, 34, 35, 36, 55799
        ];
        CborTagRegistry registry = new CborTagRegistry();

        Assert.Multiple(() =>
        {
            foreach (ulong tagNumber in tagNumbers)
            {
                CborTagRegistration? registration = registry.FindTag(tagNumber);
                Assert.That(registration, Is.Not.Null, $"tag {tagNumber}");
                Assert.That(registration?.Handler, Is.Not.Null, $"tag {tagNumber}");
                Assert.That(registration?.ValidateContent, Is.True, $"tag {tagNumber}");
            }
            Assert.That(registry.FindTag(100), Is.Null);
        });

        TestTagHandler custom = new TestTagHandler();
        registry.AddTag(new CborTagRegistration
        {
            TagNumber = CborTagNumbers.DateTimeString,
            Handler = custom
        });

        Assert.That(
            registry.FindTag(CborTagNumbers.DateTimeString)?.Handler,
            Is.SameAs(custom));
    }

    [Test]
    public void DateTimeHandlersRoundTripStringAndEpochRepresentations()
    {
        DateTimeOffset instant = new DateTimeOffset(
            2024, 3, 4, 5, 6, 7, TimeSpan.Zero);
        ICborTagHandler stringHandler = Handler(CborTagNumbers.DateTimeString);
        ICborTagHandler epochHandler = Handler(CborTagNumbers.EpochDateTime);

        CborTagResult stringContent = stringHandler.EncodeTag(
            CborTagNumbers.DateTimeString,
            instant);
        CborTagResult stringDecoded = stringHandler.DecodeTag(
            CborTagNumbers.DateTimeString,
            stringContent.Value!);
        CborTagResult epochContent = epochHandler.EncodeTag(
            CborTagNumbers.EpochDateTime,
            instant);
        CborTagResult epochDecoded = epochHandler.DecodeTag(
            CborTagNumbers.EpochDateTime,
            epochContent.Value!);

        Assert.Multiple(() =>
        {
            Assert.That(stringContent.IsValid, Is.True);
            Assert.That(stringContent.Value?.TextStringValue,
                Is.EqualTo("2024-03-04T05:06:07Z"));
            Assert.That(stringDecoded.NativeValue, Is.EqualTo(instant));
            Assert.That(epochContent.Value?.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
            Assert.That(epochDecoded.NativeValue, Is.EqualTo(instant));
        });
    }

    [Test]
    public void BignumHandlersRoundTripBothSignsExactly()
    {
        BigInteger positive = (BigInteger.One << 100) + 123;
        BigInteger negative = -positive;
        ICborTagHandler positiveHandler = Handler(CborTagNumbers.PositiveBignum);
        ICborTagHandler negativeHandler = Handler(CborTagNumbers.NegativeBignum);

        CborTagResult positiveContent = positiveHandler.EncodeTag(
            CborTagNumbers.PositiveBignum,
            positive);
        CborTagResult negativeContent = negativeHandler.EncodeTag(
            CborTagNumbers.NegativeBignum,
            negative);
        CborTagResult positiveDecoded = positiveHandler.DecodeTag(
            CborTagNumbers.PositiveBignum,
            positiveContent.Value!);
        CborTagResult negativeDecoded = negativeHandler.DecodeTag(
            CborTagNumbers.NegativeBignum,
            negativeContent.Value!);

        Assert.Multiple(() =>
        {
            Assert.That(positiveContent.Value?.Kind,
                Is.EqualTo(CborValueKind.ByteString));
            Assert.That(negativeContent.Value?.Kind,
                Is.EqualTo(CborValueKind.ByteString));
            Assert.That(positiveDecoded.NativeValue, Is.EqualTo(positive));
            Assert.That(negativeDecoded.NativeValue, Is.EqualTo(negative));
        });
    }

    [Test]
    public void BuiltInRegistrationsPlugIntoGenericDecodeValidation()
    {
        CborTagRegistration registration = new CborTagRegistry().FindTag(
            CborTagNumbers.PositiveBignum)!;
        CborDecodeOptions options = new CborDecodeOptions
        {
            TagRegistrations = [registration]
        };
        CborDecoder decoder = new CborDecoder();

        CborReadResult valid = decoder.Decode(
            Convert.FromHexString("C24101"),
            options);
        CborReadResult invalid = decoder.Decode(
            Convert.FromHexString("C201"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(valid.Value?.TagNumber,
                Is.EqualTo(CborTagNumbers.PositiveBignum));
            Assert.That(invalid.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(invalid.ErrorCode, Is.EqualTo("CBOR_BIGNUM_TYPE"));
        });
    }

    [TestCase(CborTagNumbers.DecimalFraction)]
    [TestCase(CborTagNumbers.Bigfloat)]
    public void ExponentMantissaHandlersPreserveExactIntegerPairs(ulong tagNumber)
    {
        BigInteger[] pair = [new BigInteger(-3), BigInteger.One << 90];
        ICborTagHandler handler = Handler(tagNumber);

        CborTagResult content = handler.EncodeTag(tagNumber, pair);
        CborTagResult decoded = handler.DecodeTag(tagNumber, content.Value!);

        Assert.Multiple(() =>
        {
            Assert.That(content.IsValid, Is.True);
            Assert.That(content.Value?.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(content.Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(decoded.NativeValue, Is.EqualTo(pair));
        });
    }

    [Test]
    public void EncodedCborHandlerRoundTripsOneGenericValue()
    {
        ICborTagHandler handler = Handler(CborTagNumbers.EncodedCbor);
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [Integer(1), Text("two")]
        };

        CborTagResult content = handler.EncodeTag(CborTagNumbers.EncodedCbor, value);
        CborTagResult decoded = handler.DecodeTag(
            CborTagNumbers.EncodedCbor,
            content.Value!);

        Assert.Multiple(() =>
        {
            Assert.That(content.Value?.ByteStringValue,
                Is.EqualTo(Convert.FromHexString("82016374776F")));
            Assert.That(decoded.IsValid, Is.True);
            Assert.That(decoded.NativeValue, Is.TypeOf<CborValue>());
            Assert.That(((CborValue)decoded.NativeValue!).ArrayItems?[1].TextStringValue,
                Is.EqualTo("two"));
        });
    }

    [Test]
    public void EncodingHintHandlersConvertByteStringsToRequestedText()
    {
        byte[] bytes = [0xfb, 0xff];
        CborValue content = new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = bytes
        };

        CborTagResult base64Url = Handler(21).DecodeTag(21, content);
        CborTagResult base64 = Handler(22).DecodeTag(22, content);
        CborTagResult base16 = Handler(23).DecodeTag(23, content);

        Assert.Multiple(() =>
        {
            Assert.That(base64Url.NativeValue, Is.EqualTo("-_8"));
            Assert.That(base64.NativeValue, Is.EqualTo("+/8="));
            Assert.That(base16.NativeValue, Is.EqualTo("FBFF"));
            Assert.That(Handler(21).EncodeTag(21, bytes).Value?.ByteStringValue,
                Is.EqualTo(bytes));
        });
    }

    [Test]
    public void UriBaseTextRegexAndMimeHandlersUseNaturalRuntimeValues()
    {
        byte[] bytes = [0xfb, 0xff];
        Uri uri = new Uri("https://example.com/a?b=c");
        Regex expression = new Regex("^[a-z]+$");
        ICborTagHandler hints = Handler(CborTagNumbers.Uri);

        CborTagResult uriContent = hints.EncodeTag(32, uri);
        CborTagResult uriDecoded = hints.DecodeTag(32, uriContent.Value!);
        CborTagResult urlContent = hints.EncodeTag(33, bytes);
        CborTagResult urlDecoded = hints.DecodeTag(33, urlContent.Value!);
        CborTagResult base64Content = hints.EncodeTag(34, bytes);
        CborTagResult base64Decoded = hints.DecodeTag(34, base64Content.Value!);
        CborTagResult regexContent = hints.EncodeTag(35, expression);
        CborTagResult regexDecoded = hints.DecodeTag(35, regexContent.Value!);
        CborTagResult mimeContent = hints.EncodeTag(36, "Subject: hello\r\n\r\nbody");
        CborTagResult mimeDecoded = hints.DecodeTag(36, mimeContent.Value!);

        Assert.Multiple(() =>
        {
            Assert.That(((Uri)uriDecoded.NativeValue!).OriginalString,
                Is.EqualTo(uri.OriginalString));
            Assert.That(urlContent.Value?.TextStringValue, Is.EqualTo("-_8"));
            Assert.That(urlDecoded.NativeValue, Is.EqualTo(bytes));
            Assert.That(base64Content.Value?.TextStringValue, Is.EqualTo("+/8="));
            Assert.That(base64Decoded.NativeValue, Is.EqualTo(bytes));
            Assert.That(((Regex)regexDecoded.NativeValue!).ToString(),
                Is.EqualTo(expression.ToString()));
            Assert.That(mimeDecoded.NativeValue,
                Is.EqualTo("Subject: hello\r\n\r\nbody"));
        });
    }

    [Test]
    public void SelfDescribedHandlerPassesGenericValuesThrough()
    {
        ICborTagHandler handler = Handler(CborTagNumbers.SelfDescribedCbor);
        CborValue value = Integer(42);

        CborTagResult encoded = handler.EncodeTag(
            CborTagNumbers.SelfDescribedCbor,
            value);
        CborTagResult decoded = handler.DecodeTag(
            CborTagNumbers.SelfDescribedCbor,
            value);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Value, Is.SameAs(value));
            Assert.That(decoded.NativeValue, Is.SameAs(value));
        });
    }

    private static ICborTagHandler Handler(ulong tagNumber)
    {
        return new CborTagRegistry().FindTag(tagNumber)!.Handler!;
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

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private class TestTagHandler : ICborTagHandler
    {
        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult { Handled = true, IsValid = true };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult { Handled = true, IsValid = true };
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult { Handled = true, IsValid = true };
        }
    }
}
