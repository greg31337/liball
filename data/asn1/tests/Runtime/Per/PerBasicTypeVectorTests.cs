using asn1;

namespace asn1.Tests;

public class PerBasicTypeVectorTests
{
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    public void EveryPrimitiveSchemaFamilyRoundTripsDeterministically(
        AsnEncodingRules rules)
    {
        AssertRoundTrip(
            Type(AsnTypeKind.Boolean, "BooleanValue"),
            Boolean(true),
            rules);
        AssertRoundTrip(
            Type(AsnTypeKind.Null, "NullValue"),
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null },
            rules);

        AsnTypeDescriptor integer = Type(
            AsnTypeKind.Integer,
            "IntegerValue");
        integer.Constraints = [Bounds(AsnTypeKind.Integer, -2, 5, false)];
        AssertRoundTrip(integer, Integer(3), rules);
        AssertRoundTrip(
            Type(AsnTypeKind.Real, "RealValue"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = new AsnRealValue
                {
                    Kind = AsnRealKind.FiniteBinary,
                    HasBinaryValue = true,
                    Mantissa = 3,
                    Radix = 2,
                    Exponent = 2
                }
            },
            rules);

        AsnTypeDescriptor enumeration = Type(
            AsnTypeKind.Enumerated,
            "EnumeratedValue");
        enumeration.Extensible = true;
        enumeration.RootNamedValues = [Named("ready", 0), Named("busy", 1)];
        enumeration.ExtensionNamedValues = [Named("retired", 2)];
        enumeration.NamedValues =
        [
            enumeration.RootNamedValues[0],
            enumeration.RootNamedValues[1],
            enumeration.ExtensionNamedValues[0]
        ];
        AssertRoundTrip(
            enumeration,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Enumerated,
                TextValue = "retired"
            },
            rules);

        AsnTypeDescriptor bits = Type(
            AsnTypeKind.BitString,
            "BitStringValue");
        bits.Constraints = [Bounds(AsnTypeKind.BitString, 5, 5, false)];
        AssertRoundTrip(
            bits,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = [0xa8],
                    BitLength = 5
                }
            },
            rules);

        AsnTypeDescriptor octets = Type(
            AsnTypeKind.OctetString,
            "OctetStringValue");
        octets.Constraints = [Bounds(AsnTypeKind.OctetString, 2, 2, false)];
        AssertRoundTrip(octets, Octets(0xca, 0xfe), rules);

        AssertRoundTrip(
            Type(AsnTypeKind.ObjectIdentifier, "ObjectIdentifierValue"),
            ObjectIdentifier(false, 1, 2, 840, 113549),
            rules);
        AssertRoundTrip(
            Type(
                AsnTypeKind.RelativeObjectIdentifier,
                "RelativeObjectIdentifierValue"),
            ObjectIdentifier(true, 840, 113549),
            rules);
        AssertRoundTrip(
            Type(
                AsnTypeKind.ObjectIdentifierIri,
                "ObjectIdentifierIriValue"),
            Text(AsnRuntimeValueKind.ObjectIdentifierIri, "/iso/example"),
            rules);
        AssertRoundTrip(
            Type(
                AsnTypeKind.RelativeObjectIdentifierIri,
                "RelativeObjectIdentifierIriValue"),
            Text(AsnRuntimeValueKind.RelativeObjectIdentifierIri, "example/item"),
            rules);

        AsnTypeKind[] knownStrings =
        [
            AsnTypeKind.NumericString,
            AsnTypeKind.PrintableString,
            AsnTypeKind.Ia5String,
            AsnTypeKind.VisibleString,
            AsnTypeKind.BmpString,
            AsnTypeKind.UniversalString
        ];
        int knownIndex = 0;
        while (knownIndex < knownStrings.Length)
        {
            AsnTypeDescriptor textType = Type(
                knownStrings[knownIndex],
                "KnownString" + knownIndex);
            textType.Constraints =
                [Bounds(knownStrings[knownIndex], 1, 1, false)];
            string text = knownStrings[knownIndex] == AsnTypeKind.NumericString
                ? "9"
                : "A";
            AssertRoundTrip(
                textType,
                Text(AsnRuntimeValueKind.CharacterString, text),
                rules);
            knownIndex = knownIndex + 1;
        }

        AsnTypeKind[] baseEncodedStrings =
        [
            AsnTypeKind.ObjectDescriptor,
            AsnTypeKind.Utf8String,
            AsnTypeKind.TeletexString,
            AsnTypeKind.VideotexString,
            AsnTypeKind.GraphicString,
            AsnTypeKind.GeneralString
        ];
        int baseIndex = 0;
        while (baseIndex < baseEncodedStrings.Length)
        {
            AssertRoundTrip(
                Type(baseEncodedStrings[baseIndex], "BaseString" + baseIndex),
                Text(AsnRuntimeValueKind.CharacterString, "A"),
                rules);
            baseIndex = baseIndex + 1;
        }

        AsnTypeKind[] timeKinds =
        [
            AsnTypeKind.UtcTime,
            AsnTypeKind.GeneralizedTime,
            AsnTypeKind.Time,
            AsnTypeKind.Date,
            AsnTypeKind.TimeOfDay,
            AsnTypeKind.DateTime,
            AsnTypeKind.Duration
        ];
        int timeIndex = 0;
        while (timeIndex < timeKinds.Length)
        {
            AssertRoundTrip(
                Type(timeKinds[timeIndex], "TimeValue" + timeIndex),
                Time(timeKinds[timeIndex]),
                rules);
            timeIndex = timeIndex + 1;
        }
    }

    [Test]
    public void PrimitiveBoundaryVectorsHaveExactAlignedAndUnalignedBits()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Boundary");
        integer.Constraints = [Bounds(AsnTypeKind.Integer, -2, 5, false)];
        AssertExact(integer, Integer(3), AsnEncodingRules.Aper, [0xa0], 3);
        AssertExact(integer, Integer(3), AsnEncodingRules.Uper, [0xa0], 3);

        AsnTypeDescriptor extended = Type(AsnTypeKind.Integer, "Extended");
        extended.Constraints = [Bounds(AsnTypeKind.Integer, 0, 3, true)];
        AssertExact(
            extended,
            Integer(4),
            AsnEncodingRules.Aper,
            [0x80, 0x01, 0x04],
            24);
        AssertExact(
            extended,
            Integer(4),
            AsnEncodingRules.Uper,
            [0x80, 0x82, 0x00],
            17);

        AsnTypeDescriptor ia5 = Type(AsnTypeKind.Ia5String, "Alphabet");
        ia5.Constraints = [Bounds(AsnTypeKind.Ia5String, 2, 2, false)];
        AsnRuntimeValue text = Text(
            AsnRuntimeValueKind.CharacterString,
            "AZ");
        AssertExact(
            ia5,
            text,
            AsnEncodingRules.Aper,
            [0x41, 0x5a],
            16);
        AssertExact(
            ia5,
            text,
            AsnEncodingRules.Uper,
            [0x83, 0x68],
            14);

        AsnTypeDescriptor universal = Type(
            AsnTypeKind.UniversalString,
            "UniversalAlphabet");
        universal.Constraints =
            [Bounds(AsnTypeKind.UniversalString, 1, 1, false)];
        AsnRuntimeValue scalar = Text(
            AsnRuntimeValueKind.CharacterString,
            "😀");
        AssertExact(
            universal,
            scalar,
            AsnEncodingRules.Aper,
            [0x00, 0x01, 0xf6, 0x00],
            32);
        AssertExact(
            universal,
            scalar,
            AsnEncodingRules.Uper,
            [0x0f, 0xb0, 0x00],
            21);

        AsnTypeDescriptor descriptor = Type(
            AsnTypeKind.ObjectDescriptor,
            "Descriptor");
        AsnRuntimeValue descriptorText = Text(
            AsnRuntimeValueKind.CharacterString,
            "A");
        AssertExact(
            descriptor,
            descriptorText,
            AsnEncodingRules.Aper,
            [0x01, 0x41],
            16);
        AssertExact(
            descriptor,
            descriptorText,
            AsnEncodingRules.Uper,
            [0x01, 0x41],
            16);
    }

    private static void AssertRoundTrip(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnEncodingRules rules)
    {
        AsnReflectionCatalog catalog = Catalog(type);
        AsnCodecOptions options = Options(rules);
        PerSchemaCodec codec = new PerSchemaCodec(catalog, rules);
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module." + type.Name,
            Value = value,
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module." + type.Name,
            Data = encoded.Data,
            Options = options
        });
        AsnEncodeResult repeated = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module." + type.Name,
            Value = decoded.Value,
            Options = options
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success,
                Is.True,
                type.Kind + ": " + string.Join(
                    " | ",
                    encoded.Diagnostics.Select(item => item.Message)));
            Assert.That(decoded.Status,
                Is.EqualTo(AsnDecodeStatus.Done),
                type.Kind.ToString());
            Assert.That(repeated.Success, Is.True, type.Kind.ToString());
            Assert.That(repeated.Data,
                Is.EqualTo(encoded.Data),
                type.Kind.ToString());
        });
    }

    private static void AssertExact(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        byte[] expected,
        int expectedBitCount)
    {
        PerSchemaCodec codec = new PerSchemaCodec(Catalog(type), rules);
        PerBitWriter writer = new PerBitWriter(new AsnResourceLimits());
        PerSchemaResult encoded = codec.EncodeType(
            type,
            value,
            writer,
            Options(rules),
            0);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = new PerBitReader(new AsnResourceLimits());
        reader.AppendInputBits(expected, 0, expectedBitCount, true);
        PerSchemaResult decoded = codec.DecodeType(
            type,
            reader,
            Options(rules),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data, Is.EqualTo(expected));
            Assert.That(output.WrittenBitCount, Is.EqualTo(expectedBitCount));
            Assert.That(decoded.Success, Is.True);
            Assert.That(reader.GetAvailableBitCount(), Is.Zero);
        });
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Name = type.Name,
                    QualifiedName = "Module." + type.Name,
                    Type = type
                }
            ]
        };
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = false
        };
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            Kind = kind
        };
    }

    private static AsnConstraintDescriptor Bounds(
        AsnTypeKind kind,
        int lower,
        int upper,
        bool extensible)
    {
        return new AsnConstraintDescriptor
        {
            DomainKind = kind,
            Extensible = extensible,
            RootIntervals =
            [
                new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = lower,
                    HasUpperBound = true,
                    UpperBound = upper
                }
            ]
        };
    }

    private static AsnValueDescriptor Named(string name, int value)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Kind = AsnValueKind.Integer,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
        };
    }

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
        };
    }

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnRuntimeValue Octets(params byte[] value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = value
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(
        bool relative,
        params int[] arcs)
    {
        System.Numerics.BigInteger[] values =
            new System.Numerics.BigInteger[arcs.Length];
        int index = 0;
        while (index < arcs.Length)
        {
            values[index] = arcs[index];
            index = index + 1;
        }

        return new AsnRuntimeValue
        {
            Kind = relative
                ? AsnRuntimeValueKind.RelativeObjectIdentifier
                : AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Relative = relative,
                Arcs = values
            }
        };
    }

    private static AsnRuntimeValue Text(
        AsnRuntimeValueKind kind,
        string value)
    {
        return new AsnRuntimeValue
        {
            Kind = kind,
            TextValue = value
        };
    }

    private static AsnRuntimeValue Time(AsnTypeKind kind)
    {
        string text = kind == AsnTypeKind.UtcTime
            ? "260829120000Z"
            : kind == AsnTypeKind.GeneralizedTime
                ? "20260829120000Z"
                : kind == AsnTypeKind.Date
                    ? "2026-08-29"
                    : kind == AsnTypeKind.TimeOfDay
                        ? "12:00:00"
                        : kind == AsnTypeKind.DateTime
                            ? "2026-08-29T12:00:00"
                            : kind == AsnTypeKind.Duration
                                ? "P1D"
                                : "12:00:00";
        AsnTimeKind timeKind = kind == AsnTypeKind.UtcTime
            ? AsnTimeKind.UtcTime
            : kind == AsnTypeKind.GeneralizedTime
                ? AsnTimeKind.GeneralizedTime
                : kind == AsnTypeKind.Date
                    ? AsnTimeKind.Date
                    : kind == AsnTypeKind.TimeOfDay
                        ? AsnTimeKind.TimeOfDay
                        : kind == AsnTypeKind.DateTime
                            ? AsnTimeKind.DateTime
                            : kind == AsnTypeKind.Duration
                                ? AsnTimeKind.Duration
                                : AsnTimeKind.Time;
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = timeKind,
                Text = text
            }
        };
    }
}
