using asn1;

namespace asn1.Tests;

public class TranscodeImplementationTests
{
    [Test]
    public void TranscodesOneNormalizedValueAcrossRuleFamilies()
    {
        AsnTypeDescriptor number = Type(AsnTypeKind.Integer, "Number");
        number.Constraints = [Bounds(0, 255)];
        AsnReflectionCatalog catalog = Catalog(number);

        AsnTranscodeResult per = Codec(
            catalog,
            AsnEncodingRules.Ber).Transcode(Request(
                "Schema.Number",
                [0x02, 0x01, 0x2a],
                AsnEncodingRules.CanonicalUnalignedPer));
        AsnTranscodeResult oer = Codec(
            catalog,
            AsnEncodingRules.CanonicalUnalignedPer).Transcode(Request(
                "Schema.Number",
                per.Data,
                AsnEncodingRules.Coer));
        AsnTranscodeResult xer = Codec(
            catalog,
            AsnEncodingRules.Coer).Transcode(Request(
                "Schema.Number",
                oer.Data,
                AsnEncodingRules.CanonicalXer));

        Assert.Multiple(() =>
        {
            AssertSuccessful(
                per,
                AsnEncodingRules.Ber,
                AsnEncodingRules.CanonicalUnalignedPer);
            Assert.That(per.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(per.Data, Is.EqualTo(new byte[] { 0x2a }));
            AssertSuccessful(
                oer,
                AsnEncodingRules.CanonicalUnalignedPer,
                AsnEncodingRules.Coer);
            Assert.That(oer.Data, Is.EqualTo(new byte[] { 0x2a }));
            AssertSuccessful(
                xer,
                AsnEncodingRules.Coer,
                AsnEncodingRules.CanonicalXer);
            Assert.That(System.Text.Encoding.UTF8.GetString(xer.Data),
                Is.EqualTo("<Number>42</Number>"));
        });
    }

    [Test]
    public void SameRuleTransferRetainsUnknownExtensionBytes()
    {
        AsnReflectionCatalog catalog = Catalog(ExtensibleSequence());
        byte[] source =
        [
            0x30, 0x0a,
            0x80, 0x01, 0x01,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        ];

        AsnTranscodeResult result = Codec(
            catalog,
            AsnEncodingRules.Ber).Transcode(Request(
                "Schema.Container",
                source,
                AsnEncodingRules.Ber));

        Assert.Multiple(() =>
        {
            AssertSuccessful(
                result,
                AsnEncodingRules.Ber,
                AsnEncodingRules.Ber);
            Assert.That(result.Data, Is.EqualTo(source));
            Assert.That(result.Data, Is.Not.SameAs(source));
            Assert.That(result.Value?.UnknownExtensions, Has.Length.EqualTo(1));
            Assert.That(result.Value?.UnknownExtensions[0].BinaryData,
                Is.EqualTo(new byte[] { 0x89, 0x81, 0x01, 0x7f }));
        });
    }

    [Test]
    public void LossyUnknownExtensionTransferFailsBeforeProducingOutput()
    {
        AsnReflectionCatalog catalog = Catalog(ExtensibleSequence());
        byte[] source =
        [
            0x30, 0x0a,
            0x80, 0x01, 0x01,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        ];

        AsnTranscodeResult result = Codec(
            catalog,
            AsnEncodingRules.Ber).Transcode(Request(
                "Schema.Container",
                source,
                AsnEncodingRules.Oer));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNTRANS0006"));
            Assert.That(result.Diagnostics.Single().ValuePath,
                Does.StartWith("$.<extension["));
        });
    }

    [Test]
    public void UnresolvedOpenTransferReportsThePathWithoutProducingOutput()
    {
        AsnReflectionCatalog catalog = Catalog(
            Type(AsnTypeKind.OpenType, "Open"));

        AsnTranscodeResult result = Codec(
            catalog,
            AsnEncodingRules.Ber).Transcode(Request(
                "Schema.Open",
                [0x02, 0x81, 0x01, 0x2a],
                AsnEncodingRules.Coer));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value?.OpenTypeValue?.Unresolved, Is.True);
            Assert.That(result.Data, Is.Empty);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNTRANS0005"));
            Assert.That(result.Diagnostics.Single().ValuePath,
                Is.EqualTo("$"));
            Assert.That(result.Diagnostics.Single().Message,
                Does.Contain("without loss"));
        });
    }

    [Test]
    public void InvalidSourceAndTargetFailuresDoNotExposeOutput()
    {
        AsnReflectionCatalog catalog = Catalog(
            Type(AsnTypeKind.Boolean, "Flag"));
        IAsnCodec codec = Codec(catalog, AsnEncodingRules.Ber);
        AsnTranscodeResult invalidSource = codec.Transcode(Request(
            "Schema.Flag",
            [0x01, 0x01],
            AsnEncodingRules.Oer));
        AsnTranscodeRequest unsupportedRequest = Request(
            "Schema.Flag",
            [0x01, 0x01, 0xff],
            AsnEncodingRules.Oer);
        unsupportedRequest.TargetOptions = new AsnCodecOptions
        {
            EncodingRules = (AsnEncodingRules)99
        };
        AsnTranscodeResult unsupported = codec.Transcode(
            unsupportedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(invalidSource.Success, Is.False);
            Assert.That(invalidSource.Data, Is.Empty);
            Assert.That(invalidSource.Diagnostics, Is.Not.Empty);
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Data, Is.Empty);
            Assert.That(unsupported.Diagnostics.Single().Code,
                Is.EqualTo("ASNTRANS0002"));
        });
    }

    private static void AssertSuccessful(
        AsnTranscodeResult result,
        AsnEncodingRules source,
        AsnEncodingRules target)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.SourceEncodingRules, Is.EqualTo(source));
        Assert.That(result.TargetEncodingRules, Is.EqualTo(target));
        Assert.That(result.Value, Is.Not.Null);
        Assert.That(result.WrittenCount, Is.EqualTo(result.Data.Length));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    private static IAsnCodec Codec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules)
    {
        return new CodecProviderImplementation().CreateCodec(
            catalog,
            Options(rules));
    }

    private static AsnTranscodeRequest Request(
        string typeName,
        byte[] data,
        AsnEncodingRules targetRules)
    {
        return new AsnTranscodeRequest
        {
            TypeName = typeName,
            Data = data,
            TargetOptions = Options(targetRules)
        };
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

    private static AsnTypeDescriptor ExtensibleSequence()
    {
        AsnFieldDescriptor id = Field("id", 0);
        AsnFieldDescriptor later = Field("later", 2);
        later.Extension = true;
        return new AsnTypeDescriptor
        {
            StableId = "type.Container",
            Name = "Container",
            GeneratedName = "Container",
            Kind = AsnTypeKind.Sequence,
            Extensible = true,
            Fields = [id, later]
        };
    }

    private static AsnFieldDescriptor Field(string name, int tag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = Type(AsnTypeKind.Integer, name + "Type"),
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Implicit,
                    Number = tag
                }
            ]
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
}
