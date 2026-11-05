using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborObjectMapperBuiltInTests
{
    private enum SignedChoice
    {
        Below = -2,
        Above = 7
    }

    private enum UnsignedChoice : ulong
    {
        Maximum = ulong.MaxValue
    }

    [Test]
    public void RoundTripsEveryIntegralScalarAndEnums()
    {
        object[] values =
        [
            sbyte.MinValue,
            byte.MaxValue,
            short.MinValue,
            ushort.MaxValue,
            int.MinValue,
            uint.MaxValue,
            long.MinValue,
            ulong.MaxValue,
            'λ'
        ];
        Type[] types =
        [
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(char)
        ];
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < values.Length)
            {
                CborValue encoded = mapper.MapToValue(values[index], types[index], options);
                object? decoded = mapper.MapFromValue(encoded, types[index], options);
                Assert.That(decoded, Is.EqualTo(values[index]), types[index].Name);
                index++;
            }

            CborValue signed = mapper.MapToValue(
                SignedChoice.Below,
                typeof(SignedChoice),
                options);
            CborValue unsigned = mapper.MapToValue(
                UnsignedChoice.Maximum,
                typeof(UnsignedChoice),
                options);
            Assert.That(
                mapper.MapFromValue(signed, typeof(SignedChoice), options),
                Is.EqualTo(SignedChoice.Below));
            Assert.That(
                mapper.MapFromValue(unsigned, typeof(UnsignedChoice), options),
                Is.EqualTo(UnsignedChoice.Maximum));
            Assert.That(unsigned.IntegerValue, Is.EqualTo(new BigInteger(ulong.MaxValue)));
        });
    }

    [Test]
    public void MapsArbitraryBigIntegersThroughStandardBignumTags()
    {
        BigInteger positive = (BigInteger.One << 100) + 123;
        BigInteger negative = -positive;
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();

        CborValue positiveValue = mapper.MapToValue(positive, typeof(BigInteger), options);
        CborValue negativeValue = mapper.MapToValue(negative, typeof(BigInteger), options);
        object? positiveRoundTrip = mapper.MapFromValue(
            positiveValue,
            typeof(BigInteger),
            options);
        object? negativeRoundTrip = mapper.MapFromValue(
            negativeValue,
            typeof(BigInteger),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(positiveValue.TagNumber, Is.EqualTo(CborTagNumbers.PositiveBignum));
            Assert.That(negativeValue.TagNumber, Is.EqualTo(CborTagNumbers.NegativeBignum));
            Assert.That(positiveValue.TaggedValue?.Kind,
                Is.EqualTo(CborValueKind.ByteString));
            Assert.That(negativeValue.TaggedValue?.Kind,
                Is.EqualTo(CborValueKind.ByteString));
            Assert.That(positiveRoundTrip, Is.EqualTo(positive));
            Assert.That(negativeRoundTrip, Is.EqualTo(negative));
        });
    }

    [Test]
    public void PreservesFloatingPointWidthsAndSpecialValues()
    {
        Half half = (Half)(-1.5);
        float single = -0.0f;
        double doubleValue = double.NaN;
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();

        CborValue encodedHalf = mapper.MapToValue(half, typeof(Half), options);
        CborValue encodedSingle = mapper.MapToValue(single, typeof(float), options);
        CborValue encodedDouble = mapper.MapToValue(doubleValue, typeof(double), options);

        Assert.Multiple(() =>
        {
            Assert.That(encodedHalf.FloatWidth, Is.EqualTo(CborFloatWidth.Half));
            Assert.That(encodedSingle.FloatWidth, Is.EqualTo(CborFloatWidth.Single));
            Assert.That(encodedDouble.FloatWidth, Is.EqualTo(CborFloatWidth.Double));
            Assert.That(
                mapper.MapFromValue(encodedHalf, typeof(Half), options),
                Is.EqualTo(half));
            Assert.That(
                BitConverter.SingleToUInt32Bits(
                    (float)mapper.MapFromValue(encodedSingle, typeof(float), options)!),
                Is.EqualTo(0x80000000U));
            Assert.That(
                double.IsNaN((double)mapper.MapFromValue(
                    encodedDouble,
                    typeof(double),
                    options)!),
                Is.True);
        });
    }

    [Test]
    public void DecimalFractionsRoundTripExactlyThroughTagFour()
    {
        decimal[] values =
        [
            decimal.MinValue,
            -123.4500m,
            0.0000000000000000000000000001m
        ];
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < values.Length)
            {
                CborValue encoded = mapper.MapToValue(values[index], typeof(decimal), options);
                decimal decoded = (decimal)mapper.MapFromValue(
                    encoded,
                    typeof(decimal),
                    options)!;
                Assert.That(encoded.TagNumber, Is.EqualTo(CborTagNumbers.DecimalFraction));
                Assert.That(encoded.TaggedValue?.ArrayItems, Has.Count.EqualTo(2));
                Assert.That(decimal.GetBits(decoded), Is.EqualTo(decimal.GetBits(values[index])));
                index++;
            }
        });
    }

    [Test]
    public void MapsTextBytesByteMemoryBooleansAndNullableValues()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        byte[] source = [1, 2, 3];
        Memory<byte> memory = new Memory<byte>(source);
        ReadOnlyMemory<byte> readOnly = new ReadOnlyMemory<byte>(source);
        Type nullableIntegerType = Type.GetType(
            "System.Nullable`1[System.Int32]",
            true)!;

        CborValue text = mapper.MapToValue("hello 😀", typeof(string), options);
        CborValue bytes = mapper.MapToValue(source, typeof(byte[]), options);
        CborValue mutableMemory = mapper.MapToValue(memory, typeof(Memory<byte>), options);
        CborValue readOnlyMemory = mapper.MapToValue(
            readOnly,
            typeof(ReadOnlyMemory<byte>),
            options);
        CborValue boolean = mapper.MapToValue(true, typeof(bool), options);
        CborValue nullText = mapper.MapToValue(null!, typeof(string), options);
        CborValue nullInteger = mapper.MapToValue(null!, nullableIntegerType, options);
        CborValue nullableInteger = mapper.MapToValue(42, nullableIntegerType, options);
        source[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(text.TextStringValue, Is.EqualTo("hello 😀"));
            Assert.That(mapper.MapFromValue(text, typeof(string), options),
                Is.EqualTo("hello 😀"));
            Assert.That(bytes.ByteStringValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(mutableMemory.ByteStringValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(readOnlyMemory.ByteStringValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(
                ((Memory<byte>)mapper.MapFromValue(
                    mutableMemory,
                    typeof(Memory<byte>),
                    options)!).ToArray(),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(
                ((ReadOnlyMemory<byte>)mapper.MapFromValue(
                    readOnlyMemory,
                    typeof(ReadOnlyMemory<byte>),
                    options)!).ToArray(),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(mapper.MapFromValue(boolean, typeof(bool), options), Is.True);
            Assert.That(nullText.SimpleValue, Is.EqualTo(CborSimpleValues.Null));
            Assert.That(nullInteger.SimpleValue, Is.EqualTo(CborSimpleValues.Null));
            Assert.That(mapper.MapFromValue(nullText, typeof(string), options), Is.Null);
            Assert.That(mapper.MapFromValue(nullInteger, nullableIntegerType, options), Is.Null);
            Assert.That(mapper.MapFromValue(nullableInteger, nullableIntegerType, options),
                Is.EqualTo(42));
        });
    }

    [Test]
    public void MapsDateTimeUriAndGuidThroughStandardTags()
    {
        DateTimeOffset instant = new DateTimeOffset(
            2024, 3, 4, 5, 6, 7, TimeSpan.FromHours(2)).AddTicks(1234567);
        DateTime utc = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Uri uri = new Uri("https://example.com/a?b=c");
        Guid identifier = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();

        CborValue encodedOffset = mapper.MapToValue(
            instant,
            typeof(DateTimeOffset),
            options);
        CborValue encodedDate = mapper.MapToValue(utc, typeof(DateTime), options);
        CborValue encodedUri = mapper.MapToValue(uri, typeof(Uri), options);
        CborValue encodedGuid = mapper.MapToValue(identifier, typeof(Guid), options);

        Assert.Multiple(() =>
        {
            Assert.That(encodedOffset.TagNumber, Is.EqualTo(CborTagNumbers.DateTimeString));
            Assert.That(encodedDate.TagNumber, Is.EqualTo(CborTagNumbers.DateTimeString));
            Assert.That(encodedUri.TagNumber, Is.EqualTo(CborTagNumbers.Uri));
            Assert.That(encodedGuid.TagNumber, Is.EqualTo(CborTagNumbers.Uuid));
            Assert.That(encodedGuid.TaggedValue?.ByteStringValue,
                Is.EqualTo(Convert.FromHexString("00112233445566778899AABBCCDDEEFF")));
            Assert.That(
                mapper.MapFromValue(encodedOffset, typeof(DateTimeOffset), options),
                Is.EqualTo(instant));
            Assert.That(
                mapper.MapFromValue(encodedDate, typeof(DateTime), options),
                Is.EqualTo(utc));
            Assert.That(
                ((Uri)mapper.MapFromValue(encodedUri, typeof(Uri), options)!).OriginalString,
                Is.EqualTo(uri.OriginalString));
            Assert.That(
                mapper.MapFromValue(encodedGuid, typeof(Guid), options),
                Is.EqualTo(identifier));
        });
    }

    [Test]
    public void NumericConversionsAreRangeCheckedAndNarrowingIsExplicit()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions strict = new CborMappingOptions();
        CborMappingOptions narrowing = new CborMappingOptions { AllowIntegerNarrowing = true };
        CborValue tooLarge = Integer(256);
        CborValue wholeFloat = Float(42.0);
        CborValue fractionalFloat = Float(42.5);
        BigInteger unsafeValue = (BigInteger.One << 60) + BigInteger.One;
        CborValue unsafeInteger = Integer(unsafeValue);

        CborMappingException? overflow = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(tooLarge, typeof(byte), strict));
        CborMappingException? requiresInteger = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(wholeFloat, typeof(int), strict));
        CborMappingException? fractional = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(fractionalFloat, typeof(int), narrowing));
        CborMappingException? precision = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(unsafeInteger, typeof(double), strict));

        Assert.Multiple(() =>
        {
            AssertException(overflow, "CBOR_MAPPING_INTEGER_OVERFLOW");
            AssertException(requiresInteger, "CBOR_MAPPING_INTEGER_REQUIRED");
            AssertException(fractional, "CBOR_MAPPING_INTEGER_REQUIRED");
            AssertException(precision, "CBOR_MAPPING_INTEGER_PRECISION_LOSS");
            Assert.That(mapper.MapFromValue(wholeFloat, typeof(int), narrowing),
                Is.EqualTo(42));
            Assert.That(mapper.MapFromValue(unsafeInteger, typeof(double), narrowing),
                Is.EqualTo((double)unsafeValue));
        });
    }

    [Test]
    public void RejectsNullMismatchesUnsupportedTypesAndInvalidLimits()
    {
        CborObjectMapper mapper = new CborObjectMapper();
        CborMappingOptions options = new CborMappingOptions();
        CborValue nullValue = Simple(CborSimpleValues.Null);
        CborValue invalidGuid = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.Uuid,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.ByteString,
                ByteStringValue = [1]
            }
        };

        CborMappingException? nullNative = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(null!, typeof(int), options));
        CborMappingException? nullTarget = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(nullValue, typeof(int), options));
        CborMappingException? mismatch = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue("1", typeof(int), options));
        CborMappingException? unsupported = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(TimeSpan.Zero, typeof(TimeSpan), options));
        CborMappingException? invalidText = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue("\ud800", typeof(string), options));
        CborMappingException? guid = Assert.Throws<CborMappingException>(() =>
            mapper.MapFromValue(invalidGuid, typeof(Guid), options));
        CborMappingException? missingOptions = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(1, typeof(int), null!));
        CborMappingException? invalidDepth = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(
                1,
                typeof(int),
                new CborMappingOptions { MaxNestingDepth = -1 }));
        CborMappingException? noItems = Assert.Throws<CborMappingException>(() =>
            mapper.MapToValue(
                1,
                typeof(int),
                new CborMappingOptions { MaxTotalItems = 0 }));

        Assert.Multiple(() =>
        {
            AssertException(nullNative, "CBOR_MAPPING_NULL_NOT_ALLOWED");
            AssertException(nullTarget, "CBOR_MAPPING_NULL_NOT_ALLOWED");
            AssertException(mismatch, "CBOR_MAPPING_RUNTIME_TYPE_MISMATCH");
            AssertException(unsupported, "CBOR_MAPPING_TYPE_UNSUPPORTED");
            AssertException(invalidText, "CBOR_MAPPING_TEXT_INVALID_UTF16");
            AssertException(guid, "CBOR_MAPPING_UUID_REQUIRED");
            AssertException(missingOptions, "CBOR_MAPPING_OPTIONS_MISSING");
            AssertException(invalidDepth, "CBOR_MAPPING_MAX_NESTING_DEPTH_INVALID");
            AssertException(noItems, "CBOR_MAPPING_MAX_TOTAL_ITEMS");
        });
    }

    private static CborValue Integer(BigInteger value)
    {
        return new CborValue
        {
            Kind = value < BigInteger.Zero
                ? CborValueKind.NegativeInteger
                : CborValueKind.UnsignedInteger,
            IntegerValue = value
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
                Width = CborFloatWidth.Double,
                RawBits = BitConverter.DoubleToUInt64Bits(value)
            }
        };
    }

    private static CborValue Simple(byte value)
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = value
        };
    }

    private static void AssertException(CborMappingException? exception, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(exception?.ErrorCode, Is.EqualTo(code));
            Assert.That(exception?.ErrorOffset, Is.EqualTo(-1));
            Assert.That(exception?.ErrorPath, Is.EqualTo("$"));
        });
    }
}
