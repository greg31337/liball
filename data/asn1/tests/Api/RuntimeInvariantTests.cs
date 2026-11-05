using System.Numerics;
using asn1;

namespace asn1.Tests;

public class RuntimeInvariantTests
{
    [Test]
    public void NullReferencesAndInvalidNestedTagsAreRejectedWithoutThrowing()
    {
        AsnTag invalidTag = new AsnTag
        {
            Class = (AsnTagClass)99,
            Number = 1
        };
        AsnRealValue real = new AsnRealValue
        {
            Kind = AsnRealKind.PositiveInfinity,
            DecimalText = null!
        };
        AsnUnknownExtension extension = new AsnUnknownExtension
        {
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.Ber,
            HasBinaryData = true,
            Tag = invalidTag
        };
        AsnUnknownField field = new AsnUnknownField
        {
            Name = null!,
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.Ber,
            HasBinaryData = true
        };
        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "type:Payload",
            Value = new AsnRuntimeValue(),
            OriginalTag = invalidTag
        };

        Assert.That(real.IsValid(), Is.False);
        Assert.That(extension.IsValid(), Is.False);
        Assert.That(field.IsValid(), Is.False);
        field.Name = "future";
        field.Tag = invalidTag;
        Assert.That(field.IsValid(), Is.False);
        Assert.That(open.IsValid(), Is.False);
    }

    [Test]
    public void ArbitrarySizeIntegersArePreservedAcrossPublicModels()
    {
        BigInteger hugePositive = BigInteger.Parse("340282366920938463463374607431768211507");
        BigInteger hugeNegative = -hugePositive;
        AsnValue schemaValue = new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = hugePositive
        };
        AsnRuntimeValue runtimeValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = hugeNegative
        };
        AsnConstraintElement bounds = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.ValueRange,
            HasLowerBound = true,
            LowerBound = hugeNegative,
            HasUpperBound = true,
            UpperBound = hugePositive
        };
        AsnTag tag = new AsnTag { Class = AsnTagClass.ContextSpecific, Number = hugePositive };
        AsnRealValue real = new AsnRealValue
        {
            Kind = AsnRealKind.FiniteBinary,
            HasBinaryValue = true,
            Mantissa = hugeNegative,
            Radix = 2,
            Exponent = hugePositive
        };

        Assert.That(schemaValue.IntegerValue, Is.EqualTo(hugePositive));
        Assert.That(runtimeValue.IntegerValue, Is.EqualTo(hugeNegative));
        Assert.That(bounds.LowerBound, Is.EqualTo(hugeNegative));
        Assert.That(bounds.UpperBound, Is.EqualTo(hugePositive));
        Assert.That(tag.Number, Is.EqualTo(hugePositive));
        Assert.That(tag.IsValid(), Is.True);
        Assert.That(real.Exponent, Is.EqualTo(hugePositive));
        Assert.That(real.IsValid(), Is.True);
    }

    [Test]
    public void BitStringRequiresMinimalBytesAndZeroUnusedBits()
    {
        AsnBitStringValue empty = new AsnBitStringValue();
        AsnBitStringValue oneBit = new AsnBitStringValue { Bytes = [0x80], BitLength = 1 };
        AsnBitStringValue nineBits = new AsnBitStringValue { Bytes = [0xff, 0x80], BitLength = 9 };
        AsnBitStringValue negative = new AsnBitStringValue { BitLength = -1 };
        AsnBitStringValue excessByte = new AsnBitStringValue { Bytes = [0x00], BitLength = 0 };
        AsnBitStringValue nonzeroUnusedBits = new AsnBitStringValue { Bytes = [0x81], BitLength = 1 };

        Assert.That(empty.IsValid(), Is.True);
        Assert.That(oneBit.IsValid(), Is.True);
        Assert.That(nineBits.IsValid(), Is.True);
        Assert.That(negative.IsValid(), Is.False);
        Assert.That(excessByte.IsValid(), Is.False);
        Assert.That(nonzeroUnusedBits.IsValid(), Is.False);
        Assert.That(new AsnBitStringValue { Bytes = null! }.IsValid(), Is.False);
    }

    [Test]
    public void ObjectIdentifierEnforcesArcInvariantsWithoutSizeLimits()
    {
        BigInteger hugeArc = BigInteger.Parse("999999999999999999999999999999999999999");
        AsnObjectIdentifierValue absolute = new AsnObjectIdentifierValue { Arcs = [2, hugeArc] };
        AsnObjectIdentifierValue relative = new AsnObjectIdentifierValue { Relative = true, Arcs = [hugeArc] };
        AsnObjectIdentifierValue badFirstArc = new AsnObjectIdentifierValue { Arcs = [3, 0] };
        AsnObjectIdentifierValue badSecondArc = new AsnObjectIdentifierValue { Arcs = [1, 40] };
        AsnObjectIdentifierValue negativeArc = new AsnObjectIdentifierValue { Relative = true, Arcs = [-1] };
        AsnObjectIdentifierValue tooShort = new AsnObjectIdentifierValue { Arcs = [1] };

        Assert.That(absolute.IsValid(), Is.True);
        Assert.That(relative.IsValid(), Is.True);
        Assert.That(absolute.Arcs[1], Is.EqualTo(hugeArc));
        Assert.That(badFirstArc.IsValid(), Is.False);
        Assert.That(badSecondArc.IsValid(), Is.False);
        Assert.That(negativeArc.IsValid(), Is.False);
        Assert.That(tooShort.IsValid(), Is.False);
        Assert.That(new AsnObjectIdentifierValue { Arcs = null! }.IsValid(), Is.False);
    }

    [Test]
    public void RealKindsRequireExactlyTheirFinitePayloadForm()
    {
        AsnRealValue decimalValue = new AsnRealValue
        {
            Kind = AsnRealKind.FiniteDecimal,
            HasDecimalValue = true,
            DecimalText = "-1.25E+3"
        };
        AsnRealValue binaryValue = new AsnRealValue
        {
            Kind = AsnRealKind.FiniteBinary,
            HasBinaryValue = true,
            Mantissa = -5,
            Radix = 16,
            Exponent = 7
        };
        AsnRealValue specialValue = new AsnRealValue { Kind = AsnRealKind.NotANumber };
        AsnRealValue emptyDecimal = new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, HasDecimalValue = true };
        AsnRealValue zeroMantissa = new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Radix = 2 };
        AsnRealValue badRadix = new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Mantissa = 1, Radix = 10 };
        AsnRealValue specialWithPayload = new AsnRealValue
        {
            Kind = AsnRealKind.PositiveInfinity,
            HasDecimalValue = true,
            DecimalText = "1"
        };
        AsnRealValue unknownKind = new AsnRealValue { Kind = (AsnRealKind)99 };

        Assert.That(decimalValue.IsValid(), Is.True);
        Assert.That(binaryValue.IsValid(), Is.True);
        Assert.That(specialValue.IsValid(), Is.True);
        Assert.That(emptyDecimal.IsValid(), Is.False);
        Assert.That(zeroMantissa.IsValid(), Is.False);
        Assert.That(badRadix.IsValid(), Is.False);
        Assert.That(specialWithPayload.IsValid(), Is.False);
        Assert.That(unknownKind.IsValid(), Is.False);
    }

    [Test]
    public void RealNormalizationRejectsEveryContradictoryPayloadComponent()
    {
        AsnRealValue validDecimal = new AsnRealValue
        {
            Kind = AsnRealKind.FiniteDecimal,
            HasDecimalValue = true,
            DecimalText = "1",
            DecimalCoefficient = 1,
            Normalized = true
        };
        AsnRealValue validBinary = new AsnRealValue
        {
            Kind = AsnRealKind.FiniteBinary,
            HasBinaryValue = true,
            Mantissa = 3,
            Radix = 2,
            Normalized = true
        };
        AsnRealValue validSpecial = new AsnRealValue
        {
            Kind = AsnRealKind.MinusZero,
            Normalized = true
        };
        AsnRealValue[] invalid =
        [
            new AsnRealValue { Kind = (AsnRealKind)(-1) },
            new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, DecimalText = "1" },
            new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, HasDecimalValue = true, HasBinaryValue = true, DecimalText = "1" },
            new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, HasDecimalValue = true, DecimalText = "1", Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, HasDecimalValue = true, DecimalText = "1", DecimalCoefficient = 1, Mantissa = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, HasDecimalValue = true, DecimalText = "1", DecimalCoefficient = 1, Radix = 2, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteDecimal, HasDecimalValue = true, DecimalText = "1", DecimalCoefficient = 1, Exponent = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasDecimalValue = true, HasBinaryValue = true, Mantissa = 1, Radix = 2 },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, Mantissa = 1, Radix = 2 },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Radix = 2 },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Mantissa = 1, Radix = 8, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Mantissa = 2, Radix = 2, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Mantissa = 1, Radix = 2, DecimalText = "1", Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Mantissa = 1, Radix = 2, DecimalCoefficient = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.FiniteBinary, HasBinaryValue = true, Mantissa = 1, Radix = 2, DecimalExponent = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, HasBinaryValue = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, DecimalText = "1", Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, DecimalCoefficient = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, DecimalExponent = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, Mantissa = 1, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, Radix = 2, Normalized = true },
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity, Exponent = 1, Normalized = true }
        ];

        Assert.That(validDecimal.IsValid(), Is.True);
        Assert.That(validBinary.IsValid(), Is.True);
        Assert.That(validSpecial.IsValid(), Is.True);
        Assert.That(invalid.All(value => !value.IsValid()), Is.True);
    }

    [Test]
    public void TimeMetadataEnforcesOffsetAndPrecisionBounds()
    {
        AsnTimeValue utc = new AsnTimeValue
        {
            Kind = AsnTimeKind.DateTime,
            Text = "20260828T120000-0700",
            HasUtcOffsetMinutes = true,
            UtcOffsetMinutes = -420,
            HasFractionalDigits = true,
            FractionalDigits = 0
        };
        AsnTimeValue maximumOffset = new AsnTimeValue
        {
            Kind = AsnTimeKind.Time,
            Text = "12:00+14:00",
            HasUtcOffsetMinutes = true,
            UtcOffsetMinutes = 840
        };
        AsnTimeValue emptyText = new AsnTimeValue { Kind = AsnTimeKind.Date };
        AsnTimeValue excessiveOffset = new AsnTimeValue
        {
            Kind = AsnTimeKind.Time,
            Text = "12:00",
            HasUtcOffsetMinutes = true,
            UtcOffsetMinutes = 841
        };
        AsnTimeValue negativePrecision = new AsnTimeValue
        {
            Kind = AsnTimeKind.Time,
            Text = "12:00",
            HasFractionalDigits = true,
            FractionalDigits = -1
        };
        AsnTimeValue durationWithOffset = new AsnTimeValue
        {
            Kind = AsnTimeKind.Duration,
            Text = "P1D",
            HasUtcOffsetMinutes = true
        };

        Assert.That(utc.IsValid(), Is.True);
        Assert.That(maximumOffset.IsValid(), Is.True);
        Assert.That(emptyText.IsValid(), Is.False);
        Assert.That(excessiveOffset.IsValid(), Is.False);
        Assert.That(negativePrecision.IsValid(), Is.False);
        Assert.That(durationWithOffset.IsValid(), Is.False);
        Assert.That(new AsnTimeValue { Kind = (AsnTimeKind)(-1), Text = "x" }.IsValid(), Is.False);
        Assert.That(new AsnTimeValue { Kind = (AsnTimeKind)99, Text = "x" }.IsValid(), Is.False);
        Assert.That(new AsnTimeValue { Kind = AsnTimeKind.Time, Text = null! }.IsValid(), Is.False);
        Assert.That(new AsnTimeValue
        {
            Kind = AsnTimeKind.Time,
            Text = "12:00",
            HasUtcOffsetMinutes = true,
            UtcOffsetMinutes = -841
        }.IsValid(), Is.False);
    }

    [Test]
    public void UnknownDataRequiresExactlyOneRuleSpecificPayload()
    {
        AsnUnknownExtension binaryExtension = new AsnUnknownExtension
        {
            Index = 0,
            EncodingRules = AsnEncodingRules.Uper,
            HasEncodingRules = true,
            HasBinaryData = true,
            BinaryData = [0x80]
        };
        AsnUnknownExtension textExtension = new AsnUnknownExtension
        {
            Index = 1,
            EncodingRules = AsnEncodingRules.ExtendedXer,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "<extension />"
        };
        AsnUnknownExtension noPayload = new AsnUnknownExtension { Index = 2 };
        AsnUnknownExtension bothPayloads = new AsnUnknownExtension
        {
            Index = 3,
            HasBinaryData = true,
            HasTextData = true
        };
        AsnUnknownExtension negativeIndex = new AsnUnknownExtension { Index = -1, HasBinaryData = true };
        AsnUnknownField field = new AsnUnknownField
        {
            Name = "future",
            EncodingRules = AsnEncodingRules.ExtendedXer,
            HasEncodingRules = true,
            HasTextData = true,
            TextData = "value"
        };

        Assert.That(binaryExtension.IsValid(), Is.True);
        Assert.That(textExtension.IsValid(), Is.True);
        Assert.That(noPayload.IsValid(), Is.False);
        Assert.That(bothPayloads.IsValid(), Is.False);
        Assert.That(negativeIndex.IsValid(), Is.False);
        Assert.That(field.IsValid(), Is.True);
    }

    [Test]
    public void UnknownDataRejectsEveryInvalidLocationAndPayloadCombination()
    {
        AsnUnknownExtension absent = new AsnUnknownExtension
        {
            Index = 0,
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.Ber,
            HasEncodedIndex = true
        };
        AsnUnknownExtension basicXer = new AsnUnknownExtension
        {
            Index = 0,
            Present = true,
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.BasicXer,
            HasTextData = true
        };
        AsnUnknownExtension canonicalXer = new AsnUnknownExtension
        {
            Index = 0,
            Present = true,
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.CanonicalXer,
            HasTextData = true
        };
        AsnUnknownExtension[] invalidExtensions =
        [
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, BinaryData = null! },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, TextData = null! },
            new AsnUnknownExtension { Index = 0, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = (AsnEncodingRules)(-1), HasBinaryData = true },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = (AsnEncodingRules)99, HasBinaryData = true },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasEncodedIndex = true, EncodedIndex = -1 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasEncodedLocation = true, EncodedOffset = -1 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasEncodedLocation = true, EncodedLength = -1 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, BinaryData = [1], HasEncodedLocation = true, EncodedLength = 2 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasEncodedBitLocation = true, EncodedBitOffset = -1 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasEncodedBitLocation = true, EncodedBitLength = -1 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasAlignmentPaddingBitCount = true, AlignmentPaddingBitCount = -1 },
            new AsnUnknownExtension { Index = 0, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasAlignmentPaddingBitCount = true, AlignmentPaddingBitCount = 8 },
            new AsnUnknownExtension { Index = 0, Present = true, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber },
            new AsnUnknownExtension { Index = 0, Present = true, HasEncodingRules = true, EncodingRules = AsnEncodingRules.Ber, HasBinaryData = true, HasTextData = true }
        ];
        Assert.That(absent.IsValid(), Is.True);
        Assert.That(basicXer.IsValid(), Is.True);
        Assert.That(canonicalXer.IsValid(), Is.True);
        Assert.That(invalidExtensions.All(value => !value.IsValid()), Is.True);

        AsnUnknownField field = ValidBinaryField();
        field.BinaryData = null!;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.TextData = null!;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.XmlPrefix = null!;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.XmlNamespaceUri = null!;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.HasEncodingRules = false;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.EncodingRules = (AsnEncodingRules)(-1);
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.EncodingRules = (AsnEncodingRules)99;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.HasTextData = true;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.HasBinaryData = false;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.EncodingRules = AsnEncodingRules.BasicXer;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.HasEncodedIndex = true;
        field.EncodedIndex = -1;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.HasEncodedLocation = true;
        field.EncodedOffset = -1;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.HasEncodedLocation = true;
        field.EncodedLength = -1;
        Assert.That(field.IsValid(), Is.False);
        field = ValidBinaryField();
        field.BinaryData = [1];
        field.HasEncodedLocation = true;
        field.EncodedLength = 2;
        Assert.That(field.IsValid(), Is.False);
    }

    [Test]
    public void OpenTypesDistinguishResolvedUnresolvedAndUninitializedValues()
    {
        AsnRuntimeValue decoded = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = 42
        };
        AsnOpenTypeValue resolved = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "type:Catalog.Payload",
            TypeName = "Catalog.Payload",
            Value = decoded,
            DispatchKey = decoded,
            EncodingRules = AsnEncodingRules.Der,
            HasEncodingRules = true,
            HasOriginalData = true,
            OriginalData = [0x02, 0x01, 0x2a]
        };
        AsnOpenTypeValue legacyResolved = new AsnOpenTypeValue
        {
            Resolved = true,
            TypeName = "Catalog.Payload",
            Value = decoded
        };
        AsnOpenTypeValue unresolved = new AsnOpenTypeValue
        {
            Unresolved = true,
            DispatchKey = decoded,
            EncodingRules = AsnEncodingRules.Uper,
            HasEncodingRules = true,
            HasOriginalData = true,
            OriginalData = []
        };
        AsnOpenTypeValue ambiguous = new AsnOpenTypeValue
        {
            Resolved = true,
            Unresolved = true,
            TypeName = "Catalog.Payload",
            Value = decoded
        };
        AsnOpenTypeValue unresolvedWithValue = new AsnOpenTypeValue
        {
            Unresolved = true,
            Value = decoded,
            HasEncodingRules = true,
            HasOriginalData = true
        };
        AsnOpenTypeValue skippedUnresolved = new AsnOpenTypeValue
        {
            Unresolved = true
        };
        AsnOpenTypeValue unmarkedPayload = new AsnOpenTypeValue
        {
            Unresolved = true,
            OriginalData = [0x00]
        };
        AsnOpenTypeValue payloadWithoutRules = new AsnOpenTypeValue
        {
            Unresolved = true,
            HasOriginalData = true,
            OriginalData = [0x00]
        };
        AsnOpenTypeValue invalidRules = new AsnOpenTypeValue
        {
            Unresolved = true,
            EncodingRules = (AsnEncodingRules)99,
            HasEncodingRules = true,
            HasOriginalData = true
        };

        Assert.That(resolved.IsValid(), Is.True);
        Assert.That(resolved.GetResolvedTypeIdentity(),
            Is.EqualTo("type:Catalog.Payload"));
        Assert.That(legacyResolved.IsValid(), Is.True);
        Assert.That(legacyResolved.GetResolvedTypeIdentity(),
            Is.EqualTo("Catalog.Payload"));
        Assert.That(unresolved.IsValid(), Is.True);
        Assert.That(new AsnOpenTypeValue().IsValid(), Is.False);
        Assert.That(ambiguous.IsValid(), Is.False);
        Assert.That(unresolvedWithValue.IsValid(), Is.False);
        Assert.That(skippedUnresolved.IsValid(), Is.True);
        Assert.That(unmarkedPayload.IsValid(), Is.False);
        Assert.That(payloadWithoutRules.IsValid(), Is.False);
        Assert.That(invalidRules.IsValid(), Is.False);
    }

    [Test]
    public void OpenTypesRejectEveryMalformedIdentityPayloadAndLocation()
    {
        AsnRuntimeValue value = new AsnRuntimeValue();
        AsnOpenTypeValue[] invalid =
        [
            new AsnOpenTypeValue { Resolved = true, ResolvedTypeId = null!, Value = value },
            new AsnOpenTypeValue { Resolved = true, ResolvedTypeId = "T", TypeName = null!, Value = value },
            new AsnOpenTypeValue { Unresolved = true, OriginalData = null! },
            new AsnOpenTypeValue { Resolved = true, ResolvedTypeId = "T", Value = value, HasEncodingRules = true },
            new AsnOpenTypeValue { Resolved = true, ResolvedTypeId = "T", Value = value, HasOriginalData = true },
            new AsnOpenTypeValue { Unresolved = true, HasEncodedLocation = true, EncodedOffset = -1 },
            new AsnOpenTypeValue { Unresolved = true, HasEncodedLocation = true, EncodedLength = -1 },
            new AsnOpenTypeValue { Unresolved = true, HasEncodingRules = true, HasOriginalData = true, OriginalData = [1], HasEncodedLocation = true, EncodedLength = 2 },
            new AsnOpenTypeValue { Unresolved = true, HasEncodingRules = true, HasOriginalData = true, EncodingRules = (AsnEncodingRules)(-1) },
            new AsnOpenTypeValue { Resolved = true, Value = value },
            new AsnOpenTypeValue { Resolved = true, ResolvedTypeId = "T" },
            new AsnOpenTypeValue { Unresolved = true, ResolvedTypeId = "T" },
            new AsnOpenTypeValue { Unresolved = true, TypeName = "T" }
        ];

        Assert.That(invalid.All(open => !open.IsValid()), Is.True);
        Assert.That(new AsnOpenTypeValue
        {
            Unresolved = true,
            OriginalTag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = 1
            }
        }.IsValid(), Is.True);
        Assert.That(new AsnOpenTypeValue
        {
            Unresolved = true,
            HasEncodedLocation = true
        }.IsValid(), Is.True);
        Assert.That(new AsnOpenTypeValue
        {
            Unresolved = true,
            HasEncodingRules = true,
            HasOriginalData = true,
            HasEncodedLocation = true
        }.IsValid(), Is.True);
        Assert.That(new AsnOpenTypeValue
        {
            ResolvedTypeId = null!,
            TypeName = "legacy"
        }.GetResolvedTypeIdentity(), Is.EqualTo("legacy"));
        Assert.That(new AsnOpenTypeValue
        {
            ResolvedTypeId = null!,
            TypeName = null!
        }.GetResolvedTypeIdentity(), Is.Empty);
    }

    private static AsnUnknownField ValidBinaryField()
    {
        return new AsnUnknownField
        {
            Name = "future",
            HasEncodingRules = true,
            EncodingRules = AsnEncodingRules.Ber,
            HasBinaryData = true
        };
    }
}
