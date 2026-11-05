using asn1;

namespace asn1.Tests;

public class EcnIntegratedPlanCodecTests
{
    [Test]
    public void ValidatesNormalizedConstraintsAndCanDisableValidation()
    {
        EcnFixture fixture = Compile(
            "ConstraintIntegration ENCODING-DEFINITIONS ::= BEGIN\n"
                + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "END");
        EcnEncodingObjectDefinition scalar = Definition(fixture, "scalar");
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Name = "scalar",
            Kind = AsnTypeKind.Integer,
            Constraints =
            [
                new AsnConstraintDescriptor
                {
                    StableId = "scalar/range",
                    Kind = AsnConstraintKind.ValueRange,
                    DomainKind = AsnTypeKind.Integer,
                    RootIntervals =
                    [
                        new AsnIntervalDescriptor
                        {
                            HasLowerBound = true,
                            LowerBound = 0,
                            HasUpperBound = true,
                            UpperBound = 10
                        }
                    ]
                }
            ]
        };
        EcnIntegratedPlanCodec validating = new EcnIntegratedPlanCodec(
            Plan(fixture, "scalar"),
            EcnClassCategory.Integer,
            scalar.Properties,
            type,
            Options(AsnUnknownDataPolicy.Preserve, 32, true));
        EcnIntegratedPlanCodec uncheckedCodec = new EcnIntegratedPlanCodec(
            Plan(fixture, "scalar"),
            EcnClassCategory.Integer,
            scalar.Properties,
            type,
            Options(AsnUnknownDataPolicy.Preserve, 32, false));

        AsnEncodeResult valid = validating.EncodeIntegrated(Integer(7), []);
        AsnDecodeResult decoded = validating.DecodeIntegrated([7], 8, []);
        AsnEncodeResult invalid = validating.EncodeIntegrated(Integer(11), []);
        AsnEncodeResult uncheckedResult = uncheckedCodec.EncodeIntegrated(
            Integer(11),
            []);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Data, Is.EqualTo(new byte[] { 7 }));
            Assert.That(valid.StreamProgress?.WrittenCount, Is.EqualTo(8));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(decoded.StreamProgress?.ConsumedCount, Is.EqualTo(8));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics.Single().Code,
                Is.EqualTo("ASNC0031"));
            Assert.That(invalid.Diagnostics.Single().ValuePath,
                Is.EqualTo("$"));
            Assert.That(uncheckedResult.Success, Is.True);
            Assert.That(uncheckedResult.Data,
                Is.EqualTo(new byte[] { 11 }));
        });
    }

    [Test]
    public void AppliesUnknownDataPoliciesAndOpenTypeResourceLimits()
    {
        EcnFixture fixture = Compile(
            "OpenIntegration ENCODING-DEFINITIONS ::= BEGIN\n"
                + "open #OPEN-TYPE ::= { }\n"
                + "END");
        EcnEncodingObjectDefinition open = Definition(fixture, "open");
        AsnRuntimeValue unresolved = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = AsnEncodingRules.Ecn,
                HasOriginalData = true,
                OriginalData = [0xde, 0xad]
            }
        };
        EcnIntegratedPlanCodec preserve = Codec(
            fixture,
            open,
            AsnUnknownDataPolicy.Preserve,
            8);
        EcnIntegratedPlanCodec reject = Codec(
            fixture,
            open,
            AsnUnknownDataPolicy.Reject,
            8);
        EcnIntegratedPlanCodec skip = Codec(
            fixture,
            open,
            AsnUnknownDataPolicy.Skip,
            8);
        EcnIntegratedPlanCodec limited = Codec(
            fixture,
            open,
            AsnUnknownDataPolicy.Preserve,
            1);

        AsnEncodeResult preserved = preserve.EncodeIntegrated(unresolved, []);
        AsnDecodeResult decoded = preserve.DecodeIntegrated(
            preserved.Data,
            16,
            []);
        AsnEncodeResult rejected = reject.EncodeIntegrated(unresolved, []);
        AsnDecodeResult rejectedDecode = reject.DecodeIntegrated(
            [0xde, 0xad],
            16,
            []);
        AsnEncodeResult skipped = skip.EncodeIntegrated(unresolved, []);
        AsnEncodeResult limit = limited.EncodeIntegrated(unresolved, []);

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Success, Is.True);
            Assert.That(preserved.Data,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.OpenTypeValue?.Unresolved, Is.True);
            Assert.That(decoded.Value?.OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNECN0301"));
            Assert.That(rejectedDecode.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejectedDecode.Diagnostics.Single().Code,
                Is.EqualTo("ASNECN0301"));
            Assert.That(skipped.Success, Is.True);
            Assert.That(skipped.Data, Is.Empty);
            Assert.That(limit.Success, Is.False);
            Assert.That(limit.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0006"));
            Assert.That(limit.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void DerivesStructuredExecutionBindingsFromReflectionFields()
    {
        EcnFixture fixture = Compile(
            "ReflectionBindings ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { length #INT, payload #OCTETS }\n"
                + "packet #Packet ::= { }\n"
                + "END");
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Name = "packet",
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new AsnFieldDescriptor
                {
                    Name = "length",
                    Type = new AsnTypeDescriptor
                    {
                        Kind = AsnTypeKind.Integer
                    }
                },
                new AsnFieldDescriptor
                {
                    Name = "payload",
                    Optional = true,
                    Type = new AsnTypeDescriptor
                    {
                        Kind = AsnTypeKind.OctetString
                    }
                }
            ]
        };

        EcnPlanExecutionBinding[] bindings = new EcnReflectionBindingBuilder()
            .BuildBindings(Plan(fixture, "packet"), type);

        Assert.Multiple(() =>
        {
            Assert.That(bindings.Select(value => value.Path),
                Is.EqualTo(new[] { "length", "payload" }));
            Assert.That(bindings[0].Category,
                Is.EqualTo(EcnClassCategory.Integer));
            Assert.That(bindings[0].Present, Is.True);
            Assert.That(bindings[1].Category,
                Is.EqualTo(EcnClassCategory.OctetString));
            Assert.That(bindings[1].Present, Is.True);
        });
    }

    [Test]
    public void ReportsExecutionSourcePathStepBitOffsetAndDeclarations()
    {
        EcnFixture fixture = Compile(
            "ExecutionDiagnostics ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { length #INT, payload #OCTETS }\n"
                + "packet #Packet ::= { }\n"
                + "END");
        EcnEncodingObjectDefinition packet = Definition(fixture, "packet");
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Name = "packet",
            Kind = AsnTypeKind.Sequence,
            SourceSpan = Span("schema.asn", 10, 40)
        };
        EcnIntegratedPlanCodec codec = new EcnIntegratedPlanCodec(
            Plan(fixture, "packet"),
            EcnClassCategory.Concatenation,
            packet.Properties,
            type,
            Options(AsnUnknownDataPolicy.Preserve, 32, true));
        EcnPlanExecutionBinding[] bindings =
        [
            new EcnPlanExecutionBinding
            {
                Path = "length",
                Category = EcnClassCategory.Integer,
                BitCount = 8
            },
            new EcnPlanExecutionBinding
            {
                Path = "payload",
                Category = EcnClassCategory.OctetString,
                BitCount = 8
            }
        ];

        AsnDecodeResult result = codec.DecodeIntegrated([1], 8, bindings);
        AsnDiagnostic diagnostic = result.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo("ASNECN0207"));
            Assert.That(diagnostic.Span?.Start.SourceName,
                Is.EqualTo("integrated-runtime.edm"));
            Assert.That(diagnostic.ValuePath, Is.EqualTo("payload"));
            Assert.That(diagnostic.PlanStepId,
                Does.StartWith(Plan(fixture, "packet").StableId + "/step/"));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(8));
            Assert.That(diagnostic.RelatedLocations.Select(value =>
                value.Message), Does.Contain(
                    "The affected ASN.1 type is declared here."));
            Assert.That(diagnostic.RelatedLocations.Select(value =>
                value.Message), Does.Contain(
                    "The ECN encoding object is declared here."));
        });
    }

    private static EcnIntegratedPlanCodec Codec(
        EcnFixture fixture,
        EcnEncodingObjectDefinition definition,
        AsnUnknownDataPolicy policy,
        int maximumOpenTypeBytes)
    {
        return new EcnIntegratedPlanCodec(
            Plan(fixture, "open"),
            EcnClassCategory.OpenType,
            definition.Properties,
            null,
            Options(policy, maximumOpenTypeBytes, true));
    }

    private static AsnCodecOptions Options(
        AsnUnknownDataPolicy policy,
        int maximumOpenTypeBytes,
        bool validateValues)
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Ecn,
            UnknownDataPolicy = policy,
            ValidateValues = validateValues,
            ResourceLimits = new AsnResourceLimits
            {
                MaximumOpenTypeBytes = maximumOpenTypeBytes
            }
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

    private static AsnSourceSpan Span(
        string sourceName,
        int start,
        int end)
    {
        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = sourceName,
                Offset = start
            },
            End = new AsnSourcePosition
            {
                SourceName = sourceName,
                Offset = end
            }
        };
    }

    private static EcnExecutablePlan Plan(EcnFixture fixture, string name)
    {
        return Definition(fixture, name).Plan
            ?? throw new AssertionException(
                "The executable plan for '" + name + "' is missing.");
    }

    private static EcnEncodingObjectDefinition Definition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition
            ?? throw new AssertionException(
                "The encoding object '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "integrated-runtime.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }
            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                throw new AssertionException(
                    "Final ECN input requested more data.");
            }
            if (result.Token != null)
            {
                tokens.Add(result.Token);
            }
        }
        EcnParseResult parsed = new EcnSyntaxParser(tokens.ToArray())
            .ParseModuleSyntax();
        Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
        EcnModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnSymbolTable table = new EcnSymbolTableBuilder(session.Diagnostics)
            .BuildTable(module, []);
        Assert.That(session.GetDiagnostics(), Is.Empty);
        return new EcnFixture
        {
            Table = table
        };
    }

    private class EcnFixture
    {
        internal EcnSymbolTable Table = null!;
    }
}
