using asn1;

namespace asn1.Tests;

public class ConfiguredCodecOneShotTests
{
    [Test]
    public void ByteArrayAndSpanOperationsReportExactCounts()
    {
        IAsnCodec codec = Codec(
            Catalog(Type(AsnTypeKind.Boolean, "Flag")),
            Options(AsnEncodingRules.Ber));
        AsnEncodeRequest request = new AsnEncodeRequest
        {
            TypeName = "Schema.Flag",
            Value = Boolean(true)
        };
        AsnEncodeResult buffered = codec.Encode(request);
        byte[] destination = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc];
        AsnEncodeResult spanned = codec.EncodeToSpan(
            request,
            destination);
        byte[] smallDestination = [0xcc, 0xcc];
        AsnEncodeResult tooSmall = codec.EncodeToSpan(
            request,
            smallDestination);
        AsnDecodeResult ranged = codec.Decode(new AsnDecodeRequest
        {
            TypeName = "Schema.Flag",
            Data = [0xaa, 0x01, 0x01, 0xff, 0xbb],
            Offset = 1,
            HasCount = true,
            Count = 4
        });

        Assert.Multiple(() =>
        {
            Assert.That(buffered.Success, Is.True);
            Assert.That(buffered.Data,
                Is.EqualTo(new byte[] { 0x01, 0x01, 0xff }));
            Assert.That(buffered.WrittenCount,
                Is.EqualTo(buffered.Data.Length));
            Assert.That(spanned.Success, Is.True);
            Assert.That(spanned.WrittenCount, Is.EqualTo(3));
            Assert.That(spanned.StreamProgress?.WrittenCount, Is.EqualTo(3));
            Assert.That(spanned.StreamProgress?.ExpectedCount, Is.EqualTo(3));
            Assert.That(spanned.StreamProgress?.Complete, Is.True);
            Assert.That(destination,
                Is.EqualTo(new byte[] { 0x01, 0x01, 0xff, 0xcc, 0xcc }));
            Assert.That(tooSmall.Success, Is.False);
            Assert.That(tooSmall.WrittenCount, Is.Zero);
            Assert.That(tooSmall.StreamProgress?.ExpectedCount, Is.EqualTo(3));
            Assert.That(tooSmall.StreamProgress?.Complete, Is.False);
            Assert.That(smallDestination,
                Is.EqualTo(new byte[] { 0xcc, 0xcc }));
            Assert.That(ranged.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(ranged.ConsumedCount, Is.EqualTo(3));
            Assert.That(ranged.Value?.BooleanValue, Is.True);
        });
    }

    [Test]
    public void XerUtf8BuffersUseExactByteCounts()
    {
        AsnTypeDescriptor greeting = Type(
            AsnTypeKind.Utf8String,
            "Greeting");
        IAsnCodec codec = Codec(
            Catalog(greeting),
            Options(AsnEncodingRules.BasicXer));
        string text = "café🙂";
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Greeting",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = text
            }
        });
        byte[] surrounded = new byte[encoded.Data.Length + 2];
        surrounded[0] = 0xaa;
        System.Buffer.BlockCopy(
            encoded.Data,
            0,
            surrounded,
            1,
            encoded.Data.Length);
        surrounded[surrounded.Length - 1] = 0xbb;
        AsnDecodeResult decoded = codec.Decode(new AsnDecodeRequest
        {
            TypeName = "Schema.Greeting",
            Data = surrounded,
            Offset = 1,
            HasCount = true,
            Count = encoded.Data.Length
        });
        AsnDecodeResult partial = codec.TryDecodeSpan(
            new System.ReadOnlySpan<byte>(
                encoded.Data,
                0,
                encoded.Data.Length - 1),
            "Schema.Greeting",
            Options(AsnEncodingRules.BasicXer));
        AsnDecodeResult complete = codec.TryDecodeSpan(
            encoded.Data,
            "Schema.Greeting",
            Options(AsnEncodingRules.BasicXer));

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.WrittenCount,
                Is.EqualTo(encoded.Data.Length));
            Assert.That(encoded.WrittenCount,
                Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(
                    System.Text.Encoding.UTF8.GetString(encoded.Data))));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.ConsumedCount,
                Is.EqualTo(encoded.Data.Length));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo(text));
            Assert.That(partial.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(partial.ConsumedCount,
                Is.EqualTo(encoded.Data.Length - 1));
            Assert.That(complete.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(complete.ConsumedCount,
                Is.EqualTo(encoded.Data.Length));
        });
    }

    [Test]
    public void TryDecodeDistinguishesIncompleteInputAndEnforcesLimits()
    {
        AsnTypeDescriptor integer = Type(
            AsnTypeKind.Integer,
            "Number");
        integer.Constraints = [Bounds(0, 65535)];
        IAsnCodec per = Codec(
            Catalog(integer),
            Options(AsnEncodingRules.Aper));
        IAsnCodec oer = Codec(
            Catalog(integer),
            Options(AsnEncodingRules.Oer));
        AsnDecodeResult incompletePer = per.TryDecodeSpan(
            [0x2a],
            "Schema.Number",
            Options(AsnEncodingRules.Aper));
        AsnDecodeResult incompleteOer = oer.TryDecodeSpan(
            [0x2a],
            "Schema.Number",
            Options(AsnEncodingRules.Oer));
        AsnCodecOptions limitedOptions = Options(AsnEncodingRules.Ber);
        limitedOptions.ResourceLimits = new AsnResourceLimits
        {
            MaximumTotalAllocationBytes = 2
        };
        IAsnCodec limited = Codec(
            Catalog(Type(AsnTypeKind.Boolean, "Flag")),
            limitedOptions);
        AsnDecodeResult rejected = limited.TryDecodeSpan(
            [0x01, 0x01, 0xff],
            "Schema.Flag",
            limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(incompletePer.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(incompletePer.ConsumedCount, Is.EqualTo(1));
            Assert.That(incompletePer.Diagnostics, Is.Empty);
            Assert.That(incompleteOer.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(incompleteOer.ConsumedCount, Is.EqualTo(1));
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.ConsumedCount, Is.Zero);
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0005"));
            Assert.That(rejected.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void OneShotOptionsControlLookupValidationUnknownsAndRuleMode()
    {
        AsnTypeDescriptor small = Type(AsnTypeKind.Integer, "Small");
        small.Constraints = [Bounds(0, 10)];
        IAsnCodec uncheckedCodec = Codec(
            Catalog(small),
            new AsnCodecOptions
            {
                EncodingRules = AsnEncodingRules.Ber,
                UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
                ValidateValues = false
            });
        AsnEncodeRequest invalidValue = new AsnEncodeRequest
        {
            TypeName = "Schema.Small",
            Value = Integer(11)
        };
        AsnEncodeResult uncheckedResult = uncheckedCodec.Encode(invalidValue);
        invalidValue.Options = Options(AsnEncodingRules.Ber);
        AsnEncodeResult validated = uncheckedCodec.Encode(invalidValue);
        AsnEncodeResult missing = uncheckedCodec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Missing",
            Value = Integer(1)
        });

        AsnTypeDescriptor open = Type(AsnTypeKind.OpenType, "Open");
        AsnCodecOptions preserveOptions = Options(AsnEncodingRules.Ber);
        preserveOptions.UnknownDataPolicy = AsnUnknownDataPolicy.Preserve;
        AsnDecodeResult preserved = Codec(
            Catalog(open),
            preserveOptions).Decode(new AsnDecodeRequest
            {
                TypeName = "Schema.Open",
                Data = [0x02, 0x01, 0x2a]
            });
        AsnDecodeResult rejected = Codec(
            Catalog(open),
            Options(AsnEncodingRules.Ber)).Decode(new AsnDecodeRequest
            {
                TypeName = "Schema.Open",
                Data = [0x02, 0x01, 0x2a]
            });

        IAsnCodec der = Codec(
            Catalog(Type(AsnTypeKind.Boolean, "Flag")),
            Options(AsnEncodingRules.Der));
        AsnCodecOptions attemptedBer = Options(AsnEncodingRules.Ber);
        attemptedBer.UseIndefiniteLengths = true;
        AsnEncodeResult canonicalMode = der.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Flag",
            Value = Boolean(true),
            Options = attemptedBer
        });

        Assert.Multiple(() =>
        {
            Assert.That(uncheckedResult.Success, Is.True);
            Assert.That(validated.Success, Is.False);
            Assert.That(validated.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Constraint));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics, Is.Not.Empty);
            Assert.That(preserved.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(preserved.Value?.OpenTypeValue?.Unresolved, Is.True);
            Assert.That(preserved.Value?.OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x2a }));
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(canonicalMode.Success, Is.False);
            Assert.That(canonicalMode.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0075"));
        });
    }

    [Test]
    public void PerAdapterPreservesPendingDecodeStatus()
    {
        PerSchemaCodec codec = new PerSchemaCodec(
            Catalog(Type(AsnTypeKind.Integer, "Number")),
            AsnEncodingRules.Aper);
        AsnDecodeResult result = codec.CopyPublicDecodeFailure(
            new PerSchemaResult
            {
                Status = AsnDecodeStatus.NeedMoreData
            },
            2,
            new PerBitReader(new AsnResourceLimits()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static IAsnCodec Codec(
        AsnReflectionCatalog catalog,
        AsnCodecOptions options)
    {
        return new CodecProviderImplementation().CreateCodec(
            catalog,
            options);
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = true
        };
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment." + type.Name,
                    Name = type.Name,
                    QualifiedName = "Schema." + type.Name,
                    GeneratedName = type.Name,
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnTypeDescriptor Type(
        AsnTypeKind kind,
        string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            GeneratedName = name,
            Kind = kind
        };
    }

    private static AsnConstraintDescriptor Bounds(int lower, int upper)
    {
        return new AsnConstraintDescriptor
        {
            DomainKind = AsnTypeKind.Integer,
            Kind = AsnConstraintKind.ValueRange,
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
}
