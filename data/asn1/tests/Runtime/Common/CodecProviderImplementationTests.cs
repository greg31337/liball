using asn1;

namespace asn1.Tests;

public class CodecProviderImplementationTests
{
    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Der)]
    [TestCase(AsnEncodingRules.Cer)]
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    [TestCase(AsnEncodingRules.ExtendedXer)]
    public void SelectsRuleSpecificSchemaCodecAndRoundTrips(
        AsnEncodingRules rules)
    {
        IAsnCodec codec = Provider().CreateCodec(
            BooleanCatalog(),
            Options(rules));
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Flag",
            Value = Boolean(true)
        });
        AsnDecodeResult decoded = codec.Decode(new AsnDecodeRequest
        {
            TypeName = "Schema.Flag",
            Data = encoded.Data
        });

        Assert.Multiple(() =>
        {
            Assert.That(codec, Is.TypeOf<ConfiguredCodec>());
            Assert.That(((ConfiguredCodec)codec).Rules, Is.EqualTo(rules));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.WrittenCount, Is.GreaterThan(0));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Boolean));
            Assert.That(decoded.Value?.BooleanValue, Is.True);
        });
    }

    [Test]
    public void SelectsCompiledEcnPlanByRequestedSchemaType()
    {
        AsnCompilationResult compiled = new EcnCompilerImplementation()
            .CompileEcn(
                new AsnSourceUnit
                {
                    Name = "provider.edm",
                    Text = "Provider ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                        + "END"
                },
                [IntegerModule()]);
        AsnReflectionCatalog catalog = compiled.ReflectionCatalog
            ?? throw new AssertionException("ECN reflection was not built.");
        IAsnCodec codec = Provider().CreateCodec(
            catalog,
            Options(AsnEncodingRules.Ecn));
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.scalar",
            Value = Integer(42)
        });
        AsnDecodeResult decoded = codec.Decode(new AsnDecodeRequest
        {
            TypeName = "Schema.scalar",
            Data = encoded.Data
        });

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Success, Is.True);
            Assert.That(catalog.EcnCodecPlans, Has.Length.EqualTo(1));
            Assert.That(catalog.EcnCodecPlans[0].Name,
                Is.EqualTo("scalar"));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 42 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
        });
    }

    [Test]
    public void SnapshotsConfigurationAndKeepsTheSelectedRule()
    {
        AsnCodecOptions configured = Options(AsnEncodingRules.Der);
        configured.ResourceLimits = new AsnResourceLimits
        {
            MaximumNestingDepth = 23
        };
        ConfiguredCodec codec = (ConfiguredCodec)Provider().CreateCodec(
            BooleanCatalog(),
            configured);

        configured.EncodingRules = AsnEncodingRules.BasicXer;
        configured.ResourceLimits.MaximumNestingDepth = 1;
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Flag",
            Value = Boolean(true),
            Options = Options(AsnEncodingRules.ExtendedXer)
        });

        Assert.Multiple(() =>
        {
            Assert.That(codec.Rules, Is.EqualTo(AsnEncodingRules.Der));
            Assert.That(codec.Options.ResourceLimits?.MaximumNestingDepth,
                Is.EqualTo(23));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x01, 0x01, 0xff }));
        });
    }

    [Test]
    public void RejectsMissingInputsAndUnknownEncodingRules()
    {
        CodecProviderImplementation provider =
            new CodecProviderImplementation();
        AsnReflectionCatalog catalog = BooleanCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(() => provider.CreateCodec(null!, Options(
                AsnEncodingRules.Ber)), Throws.ArgumentNullException);
            Assert.That(() => provider.CreateCodec(catalog, null!),
                Throws.ArgumentNullException);
            Assert.That(() => provider.CreateCodec(catalog, Options(
                (AsnEncodingRules)99)), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static AsnCodecProvider Provider()
    {
        return new AsnCodecProvider(new CodecProviderImplementation());
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

    private static AsnReflectionCatalog BooleanCatalog()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            StableId = "type.Flag",
            Name = "Flag",
            GeneratedName = "Flag",
            Kind = AsnTypeKind.Boolean
        };
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment.Flag",
                    Name = "Flag",
                    QualifiedName = "Schema.Flag",
                    GeneratedName = "Flag",
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnModule IntegerModule()
    {
        return new AsnModule
        {
            Name = "Schema",
            Assignments =
            [
                new AsnAssignment
                {
                    Name = "scalar",
                    Kind = AsnAssignmentKind.Type,
                    Type = new AsnType
                    {
                        Name = "scalar",
                        Kind = AsnTypeKind.Integer
                    }
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
