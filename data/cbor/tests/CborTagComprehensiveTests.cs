using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTagComprehensiveTests
{
    [Test]
    public void DateTimeTypedConversionsCoverFractionalAndNegativeEpochs()
    {
        ICborTagHandler handler = Handler(CborTagNumbers.EpochDateTime);
        DateTimeOffset fractional = DateTimeOffset.UnixEpoch.AddTicks(12_500_000);
        DateTimeOffset negative = DateTimeOffset.UnixEpoch.AddSeconds(-1);

        CborTagResult fractionalContent = handler.EncodeTag(
            CborTagNumbers.EpochDateTime,
            fractional);
        CborTagResult fractionalDecoded = handler.DecodeTag(
            CborTagNumbers.EpochDateTime,
            fractionalContent.Value!);
        CborTagResult negativeContent = handler.EncodeTag(
            CborTagNumbers.EpochDateTime,
            negative);
        CborTagResult negativeDecoded = handler.DecodeTag(
            CborTagNumbers.EpochDateTime,
            negativeContent.Value!);

        Assert.Multiple(() =>
        {
            Assert.That(fractionalContent.Value?.Kind, Is.EqualTo(CborValueKind.Float));
            Assert.That(fractionalContent.Value?.FloatValue?.Value, Is.EqualTo(1.25));
            Assert.That(fractionalDecoded.NativeValue, Is.EqualTo(fractional));
            Assert.That(negativeContent.Value?.Kind,
                Is.EqualTo(CborValueKind.NegativeInteger));
            Assert.That(negativeContent.Value?.IntegerValue,
                Is.EqualTo(new BigInteger(-1)));
            Assert.That(negativeDecoded.NativeValue, Is.EqualTo(negative));
        });
    }

    [Test]
    public void DateTimeTypedConversionReportsRuntimeRepresentationLimits()
    {
        ICborTagHandler handler = Handler(CborTagNumbers.DateTimeString);
        CborTagResult leapSecond = handler.DecodeTag(
            CborTagNumbers.DateTimeString,
            Text("1990-12-31T23:59:60Z"));
        CborTagResult wideOffset = handler.DecodeTag(
            CborTagNumbers.DateTimeString,
            Text("2024-01-01T00:00:00+15:00"));

        Assert.Multiple(() =>
        {
            AssertFailure(leapSecond, "CBOR_DATE_TIME_NATIVE_RANGE");
            AssertFailure(wideOffset, "CBOR_DATE_TIME_NATIVE_RANGE");
        });
    }

    [TestCase(CborTagNumbers.DecimalFraction, false)]
    [TestCase(CborTagNumbers.DecimalFraction, true)]
    [TestCase(CborTagNumbers.Bigfloat, false)]
    [TestCase(CborTagNumbers.Bigfloat, true)]
    public void TypedBigNumberPairsWithLargeMantissasSerializeExactly(
        ulong tagNumber,
        bool negative)
    {
        BigInteger mantissa = BigInteger.One << 90;
        if (negative)
        {
            mantissa = -mantissa;
        }
        BigInteger[] expected = [new BigInteger(-3), mantissa];
        ICborTagHandler handler = Handler(tagNumber);
        CborTagResult content = handler.EncodeTag(tagNumber, expected);
        CborValue tagged = Tag(tagNumber, content.Value!);

        byte[] encoding = new CborEncoder().Encode(tagged, new CborEncodeOptions());
        CborReadResult decoded = new CborDecoder().Decode(
            encoding,
            new CborDecodeOptions());
        CborTagResult converted = handler.DecodeTag(
            tagNumber,
            decoded.Value!.TaggedValue!);

        Assert.Multiple(() =>
        {
            Assert.That(content.IsValid, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(
                decoded.Value?.TaggedValue?.ArrayItems?[1].Kind,
                Is.EqualTo(CborValueKind.Tag));
            Assert.That(
                decoded.Value?.TaggedValue?.ArrayItems?[1].TagNumber,
                Is.EqualTo(negative
                    ? CborTagNumbers.NegativeBignum
                    : CborTagNumbers.PositiveBignum));
            Assert.That(converted.IsValid, Is.True);
            Assert.That(converted.NativeValue, Is.EqualTo(expected));
        });
    }

    [TestCase(CborTagNumbers.DecimalFraction)]
    [TestCase(CborTagNumbers.Bigfloat)]
    public void TypedBigNumberPairsRejectExponentsOutsideBasicIntegerRange(
        ulong tagNumber)
    {
        CborTagResult result = Handler(tagNumber).EncodeTag(
            tagNumber,
            new BigInteger[] { BigInteger.One << 90, BigInteger.One });

        AssertFailure(result, "CBOR_BIG_NUMBER_PAIR_NATIVE_TYPE");
    }

    [Test]
    public void InvalidTypedInputsReturnSpecificTagDiagnostics()
    {
        Assert.Multiple(() =>
        {
            AssertFailure(
                Handler(0).EncodeTag(0, "not a date"),
                "CBOR_DATE_TIME_NATIVE_TYPE");
            AssertFailure(
                Handler(2).EncodeTag(2, "not a number"),
                "CBOR_BIGNUM_NATIVE_TYPE");
            AssertFailure(
                Handler(2).EncodeTag(2, new BigInteger(-1)),
                "CBOR_POSITIVE_BIGNUM_SIGN");
            AssertFailure(
                Handler(3).EncodeTag(3, BigInteger.Zero),
                "CBOR_NEGATIVE_BIGNUM_SIGN");
            AssertFailure(
                Handler(4).EncodeTag(4, new BigInteger[] { BigInteger.One }),
                "CBOR_BIG_NUMBER_PAIR_NATIVE_TYPE");
            AssertFailure(
                Handler(24).EncodeTag(24, new object()),
                "CBOR_EMBEDDED_CBOR_NATIVE_TYPE");
            AssertFailure(
                Handler(21).EncodeTag(21, "not bytes"),
                "CBOR_ENCODING_HINT_NATIVE_TYPE");
            AssertFailure(
                Handler(32).EncodeTag(32, "not a URI object"),
                "CBOR_URI_NATIVE_TYPE");
            AssertFailure(
                Handler(33).EncodeTag(33, "not bytes"),
                "CBOR_BASE_TEXT_NATIVE_TYPE");
            AssertFailure(
                Handler(35).EncodeTag(35, "not a regex"),
                "CBOR_REGULAR_EXPRESSION_NATIVE_TYPE");
            AssertFailure(
                Handler(36).EncodeTag(36, new object()),
                "CBOR_MIME_NATIVE_TYPE");
        });
    }

    [Test]
    public void EveryTagPolicyHandlesKnownValidAndInvalidTagsAsDefined()
    {
        byte[] valid = Convert.FromHexString("C24101");
        byte[] invalid = Convert.FromHexString("C201");

        CborReadResult validPreserved = Decode(valid, CborTagPolicy.Preserve);
        CborReadResult validValidated = Decode(valid, CborTagPolicy.ValidateKnown);
        CborReadResult validRejectedUnknown = Decode(valid, CborTagPolicy.RejectUnknown);
        CborReadResult validForbidden = Decode(valid, CborTagPolicy.Forbid);
        CborReadResult invalidPreserved = Decode(invalid, CborTagPolicy.Preserve);
        CborReadResult invalidValidated = Decode(invalid, CborTagPolicy.ValidateKnown);
        CborReadResult invalidRejectedUnknown = Decode(
            invalid,
            CborTagPolicy.RejectUnknown);
        CborReadResult invalidForbidden = Decode(invalid, CborTagPolicy.Forbid);

        Assert.Multiple(() =>
        {
            AssertDone(validPreserved);
            AssertDone(validValidated);
            AssertDone(validRejectedUnknown);
            AssertFailure(validForbidden, "CBOR_TAG_FORBIDDEN");
            AssertDone(invalidPreserved);
            AssertFailure(invalidValidated, "CBOR_BIGNUM_TYPE");
            AssertFailure(invalidRejectedUnknown, "CBOR_BIGNUM_TYPE");
            AssertFailure(invalidForbidden, "CBOR_TAG_FORBIDDEN");
        });
    }

    [Test]
    public void NestedUnknownTagsRespectAllPoliciesAndRetainTheirOffset()
    {
        byte[] source = Convert.FromHexString("81D86400");

        CborReadResult preserved = Decode(source, CborTagPolicy.Preserve);
        CborReadResult validated = Decode(source, CborTagPolicy.ValidateKnown);
        CborReadResult rejected = Decode(source, CborTagPolicy.RejectUnknown);
        CborReadResult forbidden = Decode(source, CborTagPolicy.Forbid);

        Assert.Multiple(() =>
        {
            AssertDone(preserved);
            AssertDone(validated);
            AssertFailure(rejected, "CBOR_UNKNOWN_TAG");
            Assert.That(rejected.ErrorOffset, Is.EqualTo(1));
            AssertFailure(forbidden, "CBOR_TAG_FORBIDDEN");
            Assert.That(forbidden.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void CustomRegistrationOverridesABuiltInDuringDecode()
    {
        AcceptingHandler handler = new AcceptingHandler();
        CborDecodeOptions options = new CborDecodeOptions
        {
            TagPolicy = CborTagPolicy.ValidateKnown,
            TagRegistrations =
            [
                new CborTagRegistration
                {
                    TagNumber = CborTagNumbers.PositiveBignum,
                    Name = "application bignum",
                    Handler = handler,
                    ValidateContent = true
                }
            ]
        };

        CborReadResult result = new CborDecoder().Decode(
            Convert.FromHexString("C201"),
            options);

        Assert.Multiple(() =>
        {
            AssertDone(result);
            Assert.That(handler.ValidationCalls, Is.EqualTo(1));
            Assert.That(handler.LastTagNumber,
                Is.EqualTo(CborTagNumbers.PositiveBignum));
            Assert.That(handler.LastContent?.Kind,
                Is.EqualTo(CborValueKind.UnsignedInteger));
        });
    }

    [Test]
    public void ValidationCanBeDisabledForACustomRegistrationWithoutAHandler()
    {
        CborReadResult result = new CborDecoder().Decode(
            Convert.FromHexString("D86400"),
            new CborDecodeOptions
            {
                TagPolicy = CborTagPolicy.RejectUnknown,
                TagRegistrations =
                [
                    new CborTagRegistration
                    {
                        TagNumber = 100,
                        Handler = null,
                        ValidateContent = false
                    }
                ]
            });

        AssertDone(result);
    }

    [Test]
    public void SerializationHintTagsPreserveTheirNestedWireOrder()
    {
        CborValue value = Tag(
            CborTagNumbers.ExpectedBase64Url,
            Tag(
                CborTagNumbers.ExpectedBase64,
                Tag(CborTagNumbers.ExpectedBase16, Bytes([0xfb, 0xff]))));

        byte[] encoded = new CborEncoder().Encode(value, new CborEncodeOptions());
        CborReadResult decoded = new CborDecoder().Decode(
            encoded,
            new CborDecodeOptions());
        CborValue outer = decoded.Value!;
        CborValue middle = outer.TaggedValue!;
        CborValue inner = middle.TaggedValue!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString("D5D6D742FBFF")));
            Assert.That(outer.TagNumber, Is.EqualTo(CborTagNumbers.ExpectedBase64Url));
            Assert.That(middle.TagNumber, Is.EqualTo(CborTagNumbers.ExpectedBase64));
            Assert.That(inner.TagNumber, Is.EqualTo(CborTagNumbers.ExpectedBase16));
            Assert.That(inner.TaggedValue?.ByteStringValue,
                Is.EqualTo(new byte[] { 0xfb, 0xff }));
            Assert.That(Handler(21).DecodeTag(21, middle).NativeValue,
                Is.SameAs(middle));
        });
    }

    [Test]
    public void EmbeddedCborTypedConversionRetainsNestedEncodedItems()
    {
        ICborTagHandler handler = Handler(CborTagNumbers.EncodedCbor);
        CborTagResult innerBytes = handler.EncodeTag(
            CborTagNumbers.EncodedCbor,
            Integer(7));
        CborValue innerTag = Tag(CborTagNumbers.EncodedCbor, innerBytes.Value!);
        CborTagResult outerBytes = handler.EncodeTag(
            CborTagNumbers.EncodedCbor,
            innerTag);
        CborTagResult decoded = handler.DecodeTag(
            CborTagNumbers.EncodedCbor,
            outerBytes.Value!);
        CborValue decodedTag = (CborValue)decoded.NativeValue!;

        Assert.Multiple(() =>
        {
            Assert.That(outerBytes.Value?.ByteStringValue,
                Is.EqualTo(Convert.FromHexString("D8184107")));
            Assert.That(decoded.IsValid, Is.True);
            Assert.That(decodedTag.TagNumber, Is.EqualTo(CborTagNumbers.EncodedCbor));
            Assert.That(decodedTag.TaggedValue?.ByteStringValue,
                Is.EqualTo(new byte[] { 0x07 }));
        });
    }

    [Test]
    public void EmbeddedValidationUsesCustomRegistrationsRecursively()
    {
        RejectingHandler handler = new RejectingHandler();
        CborValidationResult result = new CborValidator().ValidateEncodingWithOptions(
            Convert.FromHexString("D81843D86400"),
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.TagValidity,
                TagRegistrations =
                [
                    new CborTagRegistration
                    {
                        TagNumber = 100,
                        Handler = handler
                    }
                ]
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_RECURSIVE_CUSTOM_TAG_INVALID"));
            Assert.That(handler.ValidationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void EmbeddedTypedConversionRejectsEmptyTrailingAndMalformedData()
    {
        ICborTagHandler handler = Handler(CborTagNumbers.EncodedCbor);

        CborTagResult empty = handler.DecodeTag(
            CborTagNumbers.EncodedCbor,
            Bytes([]));
        CborTagResult trailing = handler.DecodeTag(
            CborTagNumbers.EncodedCbor,
            Bytes([0x00, 0x01]));
        CborTagResult malformed = handler.DecodeTag(
            CborTagNumbers.EncodedCbor,
            Bytes([0x1c]));

        Assert.Multiple(() =>
        {
            AssertFailure(empty, "CBOR_UNEXPECTED_END_OF_DATA");
            AssertFailure(trailing, "CBOR_TRAILING_DATA");
            AssertFailure(malformed, "CBOR_RESERVED_ADDITIONAL_INFORMATION");
        });
    }

    private static ICborTagHandler Handler(ulong tagNumber)
    {
        return new CborTagRegistry().FindTag(tagNumber)!.Handler!;
    }

    private static CborReadResult Decode(byte[] source, CborTagPolicy policy)
    {
        return new CborDecoder().Decode(
            source,
            new CborDecodeOptions
            {
                TagPolicy = policy,
                RetainSourceRanges = true
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

    private static void AssertDone(CborReadResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.ErrorCode, Is.Null);
        });
    }

    private static void AssertFailure(CborReadResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(code));
        });
    }

    private static void AssertFailure(CborTagResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(code));
        });
    }

    private class AcceptingHandler : ICborTagHandler
    {
        public int ValidationCalls;
        public ulong LastTagNumber;
        public CborValue? LastContent;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls++;
            LastTagNumber = tagNumber;
            LastContent = content;
            return new CborTagResult { Handled = true, IsValid = true };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }

    private class RejectingHandler : ICborTagHandler
    {
        public int ValidationCalls;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls++;
            return new CborTagResult
            {
                Handled = true,
                IsValid = false,
                ErrorCode = "CBOR_RECURSIVE_CUSTOM_TAG_INVALID"
            };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }
}
