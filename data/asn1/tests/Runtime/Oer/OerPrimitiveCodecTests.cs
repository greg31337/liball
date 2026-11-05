using asn1;

namespace asn1.Tests;

public class OerPrimitiveCodecTests
{
    [Test]
    public void BooleanAndNullUseTheirSingleCanonicalForms()
    {
        OerPrimitiveResult encodedTrue = Codec().EncodePrimitive(
            Type(AsnTypeKind.Boolean),
            BooleanValue(true),
            true);
        OerPrimitiveResult basicTrue = Codec().DecodePrimitive(
            Type(AsnTypeKind.Boolean),
            [0x01],
            0,
            1,
            false);
        OerPrimitiveResult canonicalTrue = Codec().DecodePrimitive(
            Type(AsnTypeKind.Boolean),
            [0x01],
            0,
            1,
            true);
        OerPrimitiveResult encodedNull = Codec().EncodePrimitive(
            Type(AsnTypeKind.Null),
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null },
            true);
        OerPrimitiveResult decodedNull = Codec().DecodePrimitive(
            Type(AsnTypeKind.Null),
            [0xaa],
            0,
            1,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encodedTrue.Data, Is.EqualTo(new byte[] { 0xff }));
            Assert.That(basicTrue.Success, Is.True);
            Assert.That(basicTrue.Value?.BooleanValue, Is.True);
            Assert.That(basicTrue.Minimal, Is.False);
            Assert.That(canonicalTrue.ErrorCode, Is.EqualTo("ASNO0023"));
            Assert.That(encodedNull.Data, Is.Empty);
            Assert.That(decodedNull.Success, Is.True);
            Assert.That(decodedNull.ConsumedCount, Is.Zero);
        });
    }

    [Test]
    public void IntegerDispatchUsesTheConstraintDerivedCodec()
    {
        AsnTypeDescriptor type = IntegerType(0, 65535);
        OerPrimitiveResult encoded = Codec().EncodePrimitive(
            type,
            IntegerValue(0x1234, false),
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            type,
            [0x12, 0x34, 0xaa],
            0,
            3,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(0x1234)));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void EnumeratedUsesShortAndSignedLongForms()
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Enumerated);
        OerPrimitiveResult shortForm = Codec().EncodePrimitive(
            type,
            IntegerValue(127, true),
            true);
        OerPrimitiveResult positiveLong = Codec().EncodePrimitive(
            type,
            IntegerValue(128, true),
            true);
        OerPrimitiveResult negativeLong = Codec().EncodePrimitive(
            type,
            IntegerValue(-1, true),
            true);
        OerPrimitiveResult basicLongSmall = Codec().DecodePrimitive(
            type,
            [0x81, 0x01],
            0,
            2,
            false);
        OerPrimitiveResult canonicalLongSmall = Codec().DecodePrimitive(
            type,
            [0x81, 0x01],
            0,
            2,
            true);
        OerPrimitiveResult canonicalRedundant = Codec().DecodePrimitive(
            type,
            [0x82, 0xff, 0xff],
            0,
            3,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(shortForm.Data, Is.EqualTo(new byte[] { 0x7f }));
            Assert.That(positiveLong.Data,
                Is.EqualTo(new byte[] { 0x82, 0x00, 0x80 }));
            Assert.That(negativeLong.Data,
                Is.EqualTo(new byte[] { 0x81, 0xff }));
            Assert.That(basicLongSmall.Success, Is.True);
            Assert.That(basicLongSmall.Value?.IntegerValue,
                Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(basicLongSmall.Minimal, Is.False);
            Assert.That(canonicalLongSmall.ErrorCode, Is.EqualTo("ASNO0025"));
            Assert.That(canonicalRedundant.ErrorCode, Is.EqualTo("ASNO0025"));
        });
    }

    [Test]
    public void RealUsesLengthPrefixedDerContentsOctets()
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Real);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Kind = AsnRealKind.FiniteBinary,
                HasBinaryValue = true,
                Mantissa = 3,
                Radix = 2,
                Exponent = 1
            }
        };
        OerPrimitiveResult encoded = Codec().EncodePrimitive(
            type,
            value,
            false);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);
        OerPrimitiveResult nonDer = Codec().DecodePrimitive(
            type,
            [0x05, 0x01, (byte)'3', (byte)'.', (byte)'0', (byte)'0'],
            0,
            6,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x03, 0x80, 0x01, 0x03 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.RealValue?.Mantissa,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(decoded.Value?.RealValue?.Exponent,
                Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(nonDer.ErrorCode, Is.EqualTo("ASNO0026"));
        });
    }

    [Test]
    public void VariableAndFixedBitStringsUseDifferentFraming()
    {
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = [0xa5, 0xf0],
                BitLength = 12
            }
        };
        AsnTypeDescriptor variableType = Type(AsnTypeKind.BitString);
        AsnTypeDescriptor fixedType = FixedType(AsnTypeKind.BitString, 12);
        OerPrimitiveResult variable = Codec().EncodePrimitive(
            variableType,
            value,
            true);
        OerPrimitiveResult fixedValue = Codec().EncodePrimitive(
            fixedType,
            value,
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            fixedType,
            fixedValue.Data,
            0,
            fixedValue.Data.Length,
            true);
        OerPrimitiveResult badPadding = Codec().DecodePrimitive(
            variableType,
            [0x02, 0x04, 0xff],
            0,
            3,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(variable.Data,
                Is.EqualTo(new byte[] { 0x03, 0x04, 0xa5, 0xf0 }));
            Assert.That(fixedValue.Data,
                Is.EqualTo(new byte[] { 0xa5, 0xf0 }));
            Assert.That(decoded.Value?.BitStringValue?.BitLength,
                Is.EqualTo(12));
            Assert.That(decoded.Value?.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0xa5, 0xf0 }));
            Assert.That(badPadding.ErrorCode, Is.EqualTo("ASNO0031"));
        });
    }

    [Test]
    public void VariableAndFixedOctetStringsUseDifferentFraming()
    {
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = [0x01, 0x02, 0x03]
        };
        AsnTypeDescriptor variableType = Type(AsnTypeKind.OctetString);
        AsnTypeDescriptor fixedType = FixedType(AsnTypeKind.OctetString, 3);
        OerPrimitiveResult variable = Codec().EncodePrimitive(
            variableType,
            value,
            true);
        OerPrimitiveResult fixedValue = Codec().EncodePrimitive(
            fixedType,
            value,
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            variableType,
            [0x03, 0x01, 0x02, 0x03, 0xaa],
            0,
            5,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(variable.Data,
                Is.EqualTo(new byte[] { 0x03, 0x01, 0x02, 0x03 }));
            Assert.That(fixedValue.Data,
                Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
            Assert.That(decoded.Value?.Octets,
                Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void ObjectIdentifiersReuseBerContentsWithOerLengths()
    {
        AsnTypeDescriptor absoluteType = Type(AsnTypeKind.ObjectIdentifier);
        AsnRuntimeValue absolute = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Arcs = [1, 2, 840, 113549]
            }
        };
        AsnTypeDescriptor relativeType = Type(
            AsnTypeKind.RelativeObjectIdentifier);
        AsnRuntimeValue relative = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.RelativeObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Relative = true,
                Arcs = [840, 113549]
            }
        };
        OerPrimitiveResult absoluteEncoded = Codec().EncodePrimitive(
            absoluteType,
            absolute,
            true);
        OerPrimitiveResult relativeEncoded = Codec().EncodePrimitive(
            relativeType,
            relative,
            true);
        OerPrimitiveResult absoluteDecoded = Codec().DecodePrimitive(
            absoluteType,
            absoluteEncoded.Data,
            0,
            absoluteEncoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(absoluteEncoded.Data,
                Is.EqualTo(new byte[]
                {
                    0x06, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d
                }));
            Assert.That(relativeEncoded.Data,
                Is.EqualTo(new byte[]
                {
                    0x05, 0x86, 0x48, 0x86, 0xf7, 0x0d
                }));
            Assert.That(absoluteDecoded.Value?.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[]
                {
                    1, 2, 840, 113549
                }));
        });
    }

    [Test]
    public void CharacterStringsAndIrisUseTheirSpecifiedOctets()
    {
        AsnRuntimeValue utf8 = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = "Aé"
        };
        AsnRuntimeValue ia5 = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = "ABC"
        };
        AsnRuntimeValue iri = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifierIri,
            TextValue = "/iso/1"
        };
        OerPrimitiveResult utf8Encoded = Codec().EncodePrimitive(
            Type(AsnTypeKind.Utf8String),
            utf8,
            true);
        OerPrimitiveResult ia5Encoded = Codec().EncodePrimitive(
            FixedType(AsnTypeKind.Ia5String, 3),
            ia5,
            true);
        OerPrimitiveResult iriEncoded = Codec().EncodePrimitive(
            Type(AsnTypeKind.ObjectIdentifierIri),
            iri,
            true);
        OerPrimitiveResult iriDecoded = Codec().DecodePrimitive(
            Type(AsnTypeKind.ObjectIdentifierIri),
            iriEncoded.Data,
            0,
            iriEncoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(utf8Encoded.Data,
                Is.EqualTo(new byte[] { 0x03, 0x41, 0xc3, 0xa9 }));
            Assert.That(ia5Encoded.Data,
                Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
            Assert.That(iriEncoded.Data[0], Is.EqualTo(6));
            Assert.That(iriDecoded.Value?.TextValue, Is.EqualTo("/iso/1"));
            Assert.That(iriDecoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.ObjectIdentifierIri));
        });
    }

    [Test]
    public void DateAndTimeOfDayUseOptimizedComponentEncodings()
    {
        AsnRuntimeValue date = TimeValue(
            AsnTimeKind.Date,
            "2024-02-29");
        AsnRuntimeValue clock = TimeValue(
            AsnTimeKind.TimeOfDay,
            "12:34:56");
        OerPrimitiveResult dateEncoded = Codec().EncodePrimitive(
            Type(AsnTypeKind.Date),
            date,
            true);
        OerPrimitiveResult clockEncoded = Codec().EncodePrimitive(
            Type(AsnTypeKind.TimeOfDay),
            clock,
            true);
        OerPrimitiveResult dateDecoded = Codec().DecodePrimitive(
            Type(AsnTypeKind.Date),
            dateEncoded.Data,
            0,
            dateEncoded.Data.Length,
            true);
        OerPrimitiveResult clockDecoded = Codec().DecodePrimitive(
            Type(AsnTypeKind.TimeOfDay),
            clockEncoded.Data,
            0,
            clockEncoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(dateEncoded.Data,
                Is.EqualTo(new byte[] { 0x02, 0x07, 0xe8, 0x02, 0x1d }));
            Assert.That(clockEncoded.Data,
                Is.EqualTo(new byte[] { 0x0c, 0x22, 0x38 }));
            Assert.That(dateDecoded.Value?.TimeValue?.Text,
                Is.EqualTo("2024-02-29"));
            Assert.That(clockDecoded.Value?.TimeValue?.Text,
                Is.EqualTo("12:34:56"));
        });
    }

    [Test]
    public void DateTimeConcatenatesOptimizedDateAndClockComponents()
    {
        AsnRuntimeValue value = TimeValue(
            AsnTimeKind.DateTime,
            "2024-02-29T12:34:56");
        AsnTypeDescriptor type = Type(AsnTypeKind.DateTime);
        OerPrimitiveResult encoded = Codec().EncodePrimitive(
            type,
            value,
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[]
                {
                    0x02, 0x07, 0xe8, 0x02, 0x1d, 0x0c, 0x22, 0x38
                }));
            Assert.That(decoded.Value?.TimeValue?.Text,
                Is.EqualTo("2024-02-29T12:34:56"));
        });
    }

    [Test]
    public void DurationUsesPresenceBitsAndNonnegativeComponents()
    {
        AsnRuntimeValue value = TimeValue(
            AsnTimeKind.Duration,
            "P1Y2M3DT4H5M6.007S");
        value.TimeValue!.HasFractionalDigits = true;
        value.TimeValue.FractionalDigits = 3;
        AsnTypeDescriptor type = Type(AsnTypeKind.Duration);
        OerPrimitiveResult encoded = Codec().EncodePrimitive(
            type,
            value,
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[]
                {
                    0xdf,
                    0x01, 0x01,
                    0x01, 0x02,
                    0x01, 0x03,
                    0x01, 0x04,
                    0x01, 0x05,
                    0x01, 0x06,
                    0x01, 0x03,
                    0x01, 0x07
                }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.TimeValue?.Text,
                Is.EqualTo("P1Y2M3DT4H5M6.007S"));
            Assert.That(decoded.Value?.TimeValue?.FractionalDigits,
                Is.EqualTo(3));
        });
    }

    [Test]
    public void UsefulTimesUseLengthPrefixedCharacterContents()
    {
        AsnRuntimeValue value = TimeValue(
            AsnTimeKind.UtcTime,
            "991231235959Z");
        AsnTypeDescriptor type = Type(AsnTypeKind.UtcTime);
        OerPrimitiveResult encoded = Codec().EncodePrimitive(
            type,
            value,
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data[0], Is.EqualTo(13));
            Assert.That(System.Text.Encoding.ASCII.GetString(
                encoded.Data,
                1,
                encoded.Data.Length - 1), Is.EqualTo("991231235959Z"));
            Assert.That(decoded.Value?.TimeValue?.Text,
                Is.EqualTo("991231235959Z"));
        });
    }

    [Test]
    public void TimePropertySettingsSelectFractionAndDifferenceComponents()
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Time);
        type.Resolution = new ResolvedTypeSyntax();
        type.Resolution.EncodingConstraints.Oer.TimeSettings =
        [
            new TimePropertySettings
            {
                Basic = TimeBasicSettingKind.Time,
                Time = TimePrecisionSettingKind.HourMinuteSecond,
                FractionalDigits = 3,
                Zone = TimeZoneSettingKind.LocalDifference
            }
        ];
        AsnRuntimeValue value = TimeValue(
            AsnTimeKind.Time,
            "12:34:56.007+01:30");
        value.TimeValue!.HasFractionalDigits = true;
        value.TimeValue.FractionalDigits = 3;
        value.TimeValue.HasUtcOffsetMinutes = true;
        value.TimeValue.UtcOffsetMinutes = 90;

        OerPrimitiveResult encoded = Codec().EncodePrimitive(
            type,
            value,
            true);
        OerPrimitiveResult decoded = Codec().DecodePrimitive(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[]
                {
                    0x0c, 0x22, 0x38, 0x01, 0x07, 0x00, 0x5a
                }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.TimeValue?.Kind,
                Is.EqualTo(AsnTimeKind.Time));
            Assert.That(decoded.Value?.TimeValue?.Text,
                Is.EqualTo("12:34:56.007+01:30"));
            Assert.That(decoded.Value?.TimeValue?.UtcOffsetMinutes,
                Is.EqualTo(90));
        });
    }

    [Test]
    public void TruncatedFramingAndResourceLimitsReturnFailures()
    {
        OerPrimitiveResult truncated = Codec().DecodePrimitive(
            Type(AsnTypeKind.OctetString),
            [0x03, 0x01],
            0,
            2,
            false);
        OerPrimitiveCodec limited = new OerPrimitiveCodec(
            new AsnResourceLimits
            {
                MaximumOpenTypeBytes = 2,
                MaximumTotalAllocationBytes = 100
            });
        OerPrimitiveResult resource = limited.EncodePrimitive(
            Type(AsnTypeKind.OctetString),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = [0x01, 0x02, 0x03]
            },
            false);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(resource.ErrorCode, Is.EqualTo("ASNR0020"));
            Assert.That(resource.LimitExceeded, Is.True);
        });
    }

    private static OerPrimitiveCodec Codec()
    {
        return new OerPrimitiveCodec(new AsnResourceLimits
        {
            MaximumLengthOctets = 9
        });
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    private static AsnTypeDescriptor FixedType(AsnTypeKind kind, int size)
    {
        return new AsnTypeDescriptor
        {
            Kind = kind,
            Constraints = [new AsnConstraintDescriptor
            {
                Root = new AsnConstraintElement
                {
                    Kind = AsnConstraintKind.Size,
                    HasLowerBound = true,
                    LowerBound = size,
                    HasUpperBound = true,
                    UpperBound = size
                }
            }]
        };
    }

    private static AsnTypeDescriptor IntegerType(int lower, int upper)
    {
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Constraints = [new AsnConstraintDescriptor
            {
                DomainKind = AsnTypeKind.Integer,
                RootIntervals = [new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = lower,
                    HasUpperBound = true,
                    UpperBound = upper
                }]
            }]
        };
    }

    private static AsnRuntimeValue BooleanValue(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
        };
    }

    private static AsnRuntimeValue IntegerValue(int value, bool enumerated)
    {
        return new AsnRuntimeValue
        {
            Kind = enumerated
                ? AsnRuntimeValueKind.Enumerated
                : AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnRuntimeValue TimeValue(AsnTimeKind kind, string text)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = kind,
                Text = text
            }
        };
    }
}
