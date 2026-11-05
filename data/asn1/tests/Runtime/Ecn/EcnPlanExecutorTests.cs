using asn1;

namespace asn1.Tests;

public class EcnPlanExecutorTests
{
    [Test]
    public void ExecutesUsableX692AnnexDExamplesAtEveryBitSplit()
    {
        EcnFixture fixture = Compile(
            "X692AnnexD ENCODING-DEFINITIONS ::= BEGIN\n"
                + "booleanEncoding #BOOLEAN ::= { ENCODING-SPACE, SIZE 1, "
                + "MULTIPLE OF bit, TRUE-PATTERN bits:'1'B, "
                + "FALSE-PATTERN bits:'0'B }\n"
                + "evenIntegerEncoding #INT ::= { ENCODING-SPACE, "
                + "SIZE 16, MAPPING TRANSFORMS "
                + "{{ INT-TO-INT divide:2 }} }\n"
                + "integerRightAlignedEncoding #INT ::= { ENCODING-SPACE, "
                + "SIZE 16, ALIGNED TO NEXT octet }\n"
                + "binaryFileEncoding #OCTETS ::= { ALIGNED TO NEXT octet, "
                + "ENCODING-SPACE, SIZE 4, MULTIPLE OF octet, "
                + "VALUE-PADDING, JUSTIFIED right, PADDING one, "
                + "UNUSED BITS 16 }\n"
                + "passwordEncoding #CHARS ::= { ALIGNED TO NEXT octet, "
                + "TERMINATION '00'H }\n"
                + "END");

        AssertEveryBitSplit(
            fixture,
            "booleanEncoding",
            EcnClassCategory.Boolean,
            BooleanValue(true),
            [0x80],
            1);
        AssertEveryBitSplit(
            fixture,
            "evenIntegerEncoding",
            EcnClassCategory.Integer,
            IntegerValue(84),
            [0x00, 0x2a],
            16);
        AssertEveryBitSplit(
            fixture,
            "integerRightAlignedEncoding",
            EcnClassCategory.Integer,
            IntegerValue(513),
            [0x02, 0x01],
            16);
        AssertEveryBitSplit(
            fixture,
            "binaryFileEncoding",
            EcnClassCategory.OctetString,
            OctetStringValue([0xab, 0xcd]),
            [0xff, 0xff, 0xab, 0xcd],
            32);
        AssertEveryBitSplit(
            fixture,
            "passwordEncoding",
            EcnClassCategory.CharacterString,
            CharacterStringValue("ECN"),
            [0x45, 0x43, 0x4e, 0x00],
            32);
    }

    [Test]
    public void StreamsNormativeDeterminantsAndAlignmentAtEveryBitSplit()
    {
        EcnFixture fixture = Compile(
            "X692StructuredExamples ENCODING-DEFINITIONS ::= BEGIN\n"
                + "threeBits #BOOL ::= { ENCODING-SPACE, SIZE 3, "
                + "TRUE-PATTERN '1'B, FALSE-PATTERN '0'B }\n"
                + "alignedOctet #INT ::= { ALIGNED TO NEXT octet, "
                + "PADDING one, ENCODING-SPACE, SIZE 8, "
                + "SIGNEDNESS unsigned }\n"
                + "#AlignedPair ::= #CONCATENATION { "
                + "prefix #BOOL, value #INT }\n"
                + "alignedPair #AlignedPair ::= { ENCODE STRUCTURE { "
                + "prefix USE threeBits, value USE alignedOctet } }\n"
                + "octet #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "#Packet ::= #CONCATENATION { "
                + "length #INT, value #OCTETS }\n"
                + "sized {< REFERENCE : determinant >} #OCTETS ::= { "
                + "ENCODING-SPACE DETERMINED BY field-to-be-set "
                + "USING determinant }\n"
                + "packet #Packet ::= { ENCODE STRUCTURE { "
                + "value USE sized {< length >} } }\n"
                + "END");
        AsnRuntimeValue pair = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Field("prefix", BooleanValue(true)),
                Field("value", IntegerValue(170))
            ]
        };
        AsnRuntimeValue packet = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Field("length", IntegerValue(0)),
                Field("value", OctetStringValue([0xde, 0xad]))
            ]
        };

        AssertStructuredEveryBitSplit(
            Plan(fixture, "alignedPair"),
            pair,
            () => AlignedPairBindings(fixture),
            [0x3f, 0xaa],
            16,
            decoded =>
            {
                Assert.That(decoded.Fields[0].Value?.BooleanValue, Is.True);
                Assert.That(decoded.Fields[1].Value?.IntegerValue,
                    Is.EqualTo(new System.Numerics.BigInteger(170)));
            });
        AssertStructuredEveryBitSplit(
            Plan(fixture, "packet"),
            packet,
            () => PacketBindings(fixture),
            [0x10, 0xde, 0xad],
            24,
            decoded =>
            {
                Assert.That(decoded.Fields[0].Value?.IntegerValue,
                    Is.EqualTo(new System.Numerics.BigInteger(16)));
                Assert.That(decoded.Fields[1].Value?.Octets,
                    Is.EqualTo(new byte[] { 0xde, 0xad }));
            });
        EcnDecodeMachine invalidPadding = new EcnDecodeMachine(
            Plan(fixture, "alignedPair"),
            EcnClassCategory.Unknown,
            [],
            new AsnResourceLimits());
        invalidPadding.ConfigureBindings(AlignedPairBindings(fixture));
        EcnPlanExecutionResult rejectedPadding =
            invalidPadding.SupplyInputBits([0x20, 0xaa], 0, 16, true);

        Assert.That(rejectedPadding.ErrorCode, Is.EqualTo("ASNECN0210"));
    }

    [Test]
    public void RejectsCyclesAmbiguityNonReversiblePlansAndResourceFailures()
    {
        EcnFixture invalid = Compile(
            "X692InvalidExecutions ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Cycle ::= #CONCATENATION { a #INT, b #INT }\n"
                + "reader {< REFERENCE : determinant >} #OPTIONAL ::= { "
                + "PRESENCE DETERMINED BY field-to-be-used "
                + "USING determinant }\n"
                + "cyclic #Cycle ::= { ENCODE STRUCTURE { "
                + "a USE reader {< b >}, b USE reader {< a >} } }\n"
                + "nonReversible #INT ::= { USE #INT MAPPING TRANSFORMS "
                + "{{ INT-TO-BOOL TRUE-IS {0} }} }\n"
                + "END");
        EcnFixture fixture = Compile(
            "X692ExecutionFailures ENCODING-DEFINITIONS ::= BEGIN\n"
                + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "outer #INT ::= { }\n"
                + "#Choice ::= #ALTERNATIVES { first #INT, second #INT }\n"
                + "choice #Choice ::= { }\n"
                + "#Items ::= #REPETITION { item #INT }\n"
                + "items #Items ::= { }\n"
                + "END");
        EcnPlanExecutor normal = new EcnPlanExecutor(
            new AsnResourceLimits());
        EcnPlanExecutionBinding[] alternatives =
        [
            new EcnPlanExecutionBinding
            {
                Path = "first",
                Category = EcnClassCategory.Integer,
                BitCount = 8,
                Selected = true
            },
            new EcnPlanExecutionBinding
            {
                Path = "second",
                Category = EcnClassCategory.Integer,
                BitCount = 8,
                Selected = true
            }
        ];
        EcnPlanExecutionResult ambiguousEncode = normal.EncodePlan(
            Request(
                Plan(fixture, "choice"),
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields =
                    [
                        Field("first", IntegerValue(1)),
                        Field("second", IntegerValue(2))
                    ]
                },
                alternatives));
        EcnPlanExecutionResult ambiguousDecode = normal.DecodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "choice"),
                Bindings = alternatives,
                Data = [0x00],
                BitCount = 8,
                FinalBlock = true
            });

        AsnResourceLimits collectionLimits = new AsnResourceLimits
        {
            MaximumCollectionElements = 1
        };
        EcnPlanExecutionBinding repetition = new EcnPlanExecutionBinding
        {
            Path = "item",
            Category = EcnClassCategory.Integer,
            Properties = Definition(fixture, "scalar").Properties,
            BitCount = 8,
            RepetitionCount = 2
        };
        EcnPlanExecutionResult collectionEncode = new EcnPlanExecutor(
            collectionLimits).EncodePlan(Request(
                Plan(fixture, "items"),
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [IntegerValue(1), IntegerValue(2)]
                },
                [repetition]));
        EcnPlanExecutionResult collectionDecode = new EcnPlanExecutor(
            collectionLimits).DecodePlan(new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "items"),
                Bindings = [repetition],
                Data = [1, 2],
                BitCount = 16,
                FinalBlock = true
            });

        AsnResourceLimits depthLimits = new AsnResourceLimits
        {
            MaximumNestingDepth = 1
        };
        EcnPlanExecutionBinding nested = new EcnPlanExecutionBinding
        {
            Path = "$value",
            Category = EcnClassCategory.Integer,
            Properties = Definition(fixture, "scalar").Properties,
            Plan = Plan(fixture, "scalar"),
            BitCount = 8
        };
        EcnPlanExecutionResult depthEncode = new EcnPlanExecutor(
            depthLimits).EncodePlan(Request(
                Plan(fixture, "outer"),
                IntegerValue(1),
                [nested]));
        EcnPlanExecutionResult depthDecode = new EcnPlanExecutor(
            depthLimits).DecodePlan(new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "outer"),
                Category = EcnClassCategory.Integer,
                Bindings = [nested],
                Data = [1],
                BitCount = 8,
                FinalBlock = true
            });
        EcnPlanExecutionResult rawLimit = new EcnPlanExecutor(
            new AsnResourceLimits { MaximumOpenTypeBytes = 1 }).DecodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "outer"),
                Data = [0xde, 0xad],
                BitCount = 16,
                FinalBlock = true
            });

        string[] invalidCodes = invalid.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(invalidCodes, Does.Contain("ASNECN0039"));
            Assert.That(invalidCodes, Does.Contain("ASNECN0049"));
            Assert.That(Definition(invalid, "cyclic").Plan, Is.Null);
            Assert.That(Definition(invalid, "nonReversible").Plan, Is.Null);
            Assert.That(ambiguousEncode.ErrorCode,
                Is.EqualTo("ASNECN0203"));
            Assert.That(ambiguousDecode.ErrorCode,
                Is.EqualTo("ASNECN0203"));
            Assert.That(collectionEncode.ErrorCode,
                Is.EqualTo("ASNR0002"));
            Assert.That(collectionEncode.LimitExceeded, Is.True);
            Assert.That(collectionDecode.ErrorCode,
                Is.EqualTo("ASNR0002"));
            Assert.That(collectionDecode.LimitExceeded, Is.True);
            Assert.That(depthEncode.ErrorCode, Is.EqualTo("ASNR0001"));
            Assert.That(depthEncode.LimitExceeded, Is.True);
            Assert.That(depthDecode.ErrorCode, Is.EqualTo("ASNR0001"));
            Assert.That(depthDecode.LimitExceeded, Is.True);
            Assert.That(rawLimit.ErrorCode, Is.EqualTo("ASNR0007"));
            Assert.That(rawLimit.LimitExceeded, Is.True);
        });
    }

    [Test]
    public void StreamsPrimitivePlansAcrossNonOctetChunks()
    {
        EcnFixture fixture = Compile(
            "PrimitiveExecution ENCODING-DEFINITIONS ::= BEGIN\n"
                + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "END");
        EcnEncodingObjectDefinition scalar = Definition(fixture, "scalar");
        EcnEncodeMachine encoder = new EcnEncodeMachine(
            Plan(fixture, "scalar"),
            EcnClassCategory.Integer,
            scalar.Properties,
            new AsnResourceLimits());
        EcnPlanExecutionResult prepared = encoder.BeginEncoding(
            IntegerValue(42),
            []);
        EcnPlanExecutionResult first = encoder.TakeOutputBits(3);
        EcnPlanExecutionResult second = encoder.TakeOutputBits(5);

        EcnDecodeMachine decoder = new EcnDecodeMachine(
            Plan(fixture, "scalar"),
            EcnClassCategory.Integer,
            scalar.Properties,
            new AsnResourceLimits());
        EcnPlanExecutionResult waiting = decoder.SupplyInputBits(
            first.Data,
            0,
            first.BitCount,
            false);
        EcnPlanExecutionResult decoded = decoder.SupplyInputBits(
            second.Data,
            0,
            second.BitCount,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(prepared.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(prepared.BitCount, Is.EqualTo(8));
            Assert.That(first.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(first.Data, Is.EqualTo(new byte[] { 0x20 }));
            Assert.That(second.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(second.Data, Is.EqualTo(new byte[] { 0x50 }));
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(waiting.PendingPath, Is.EqualTo("$value"));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(decoded.ConsumedBitCount, Is.EqualTo(8));
        });
    }

    [Test]
    public void UsesDecodedAndComputedDeterminantsForStructuredFields()
    {
        EcnFixture fixture = Compile(
            "DeterminantExecution ENCODING-DEFINITIONS ::= BEGIN\n"
                + "octet #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "#Packet ::= #CONCATENATION { length #INT, value #OCTETS }\n"
                + "sized {< REFERENCE : determinant >} #OCTETS ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-set USING determinant }\n"
                + "packet #Packet ::= { ENCODE STRUCTURE {\n"
                + "  value USE sized {< length >} } }\n"
                + "END");
        EcnEncodingProperty[] integerProperties = Definition(
            fixture,
            "octet").Properties;
        EcnPlanExecutionBinding[] bindings =
        [
            new EcnPlanExecutionBinding
            {
                Path = "length",
                Category = EcnClassCategory.Integer,
                Properties = integerProperties,
                BitCount = 8
            },
            new EcnPlanExecutionBinding
            {
                Path = "value",
                Category = EcnClassCategory.OctetString
            }
        ];
        AsnRuntimeValue packetValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Field("length", IntegerValue(0)),
                Field("value", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OctetString,
                    Octets = [0xde, 0xad]
                })
            ]
        };
        EcnPlanExecutor executor = new EcnPlanExecutor(
            new AsnResourceLimits());
        EcnPlanExecutionResult encoded = executor.EncodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "packet"),
                Value = packetValue,
                Bindings = bindings
            });

        EcnDecodeMachine decoder = new EcnDecodeMachine(
            Plan(fixture, "packet"),
            EcnClassCategory.Unknown,
            [],
            new AsnResourceLimits());
        decoder.ConfigureBindings(bindings);
        EcnPlanExecutionResult partial = decoder.SupplyInputBits(
            encoded.Data,
            0,
            16,
            false);
        EcnPlanExecutionResult decoded = decoder.SupplyInputBits(
            [encoded.Data[2]],
            0,
            8,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x10, 0xde, 0xad }));
            Assert.That(partial.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(partial.PendingPath, Is.EqualTo("value"));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields[0].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(16)));
            Assert.That(decoded.Value?.Fields[1].Value?.Octets,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
        });
    }

    [Test]
    public void ExecutesSelectedAlternativesAndBoundedRepetitions()
    {
        EcnFixture fixture = Compile(
            "ConstructedExecution ENCODING-DEFINITIONS ::= BEGIN\n"
                + "octet #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "#Choice ::= #ALTERNATIVES { number #INT, data #OCTETS }\n"
                + "choice #Choice ::= { }\n"
                + "#Items ::= #REPETITION { item #INT }\n"
                + "items #Items ::= { }\n"
                + "END");
        EcnEncodingProperty[] integerProperties = Definition(
            fixture,
            "octet").Properties;
        EcnPlanExecutionBinding[] choiceBindings =
        [
            new EcnPlanExecutionBinding
            {
                Path = "number",
                Category = EcnClassCategory.Integer,
                Properties = integerProperties,
                BitCount = 8
            },
            new EcnPlanExecutionBinding
            {
                Path = "data",
                Category = EcnClassCategory.OctetString,
                BitCount = 8,
                Selected = true
            }
        ];
        AsnRuntimeValue choiceValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields =
            [
                Field("data", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OctetString,
                    Octets = [0xa5]
                })
            ]
        };
        EcnPlanExecutionBinding itemBinding = new EcnPlanExecutionBinding
        {
            Path = "item",
            Category = EcnClassCategory.Integer,
            Properties = integerProperties,
            BitCount = 8,
            RepetitionCount = 2
        };
        EcnPlanExecutor executor = new EcnPlanExecutor(
            new AsnResourceLimits());
        EcnPlanExecutionResult choiceEncoded = executor.EncodePlan(
            Request(Plan(fixture, "choice"), choiceValue, choiceBindings));
        EcnPlanExecutionResult choiceDecoded = executor.DecodePlan(
            DecodeRequest(
                Plan(fixture, "choice"),
                choiceEncoded,
                choiceBindings));
        EcnPlanExecutionResult itemsEncoded = executor.EncodePlan(
            Request(
                Plan(fixture, "items"),
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [IntegerValue(3), IntegerValue(7)]
                },
                [itemBinding]));
        EcnPlanExecutionResult itemsDecoded = executor.DecodePlan(
            DecodeRequest(
                Plan(fixture, "items"),
                itemsEncoded,
                [itemBinding]));

        Assert.Multiple(() =>
        {
            Assert.That(choiceEncoded.Data,
                Is.EqualTo(new byte[] { 0xa5 }));
            Assert.That(choiceDecoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Choice));
            Assert.That(choiceDecoded.Value?.Fields[0].Name,
                Is.EqualTo("data"));
            Assert.That(itemsEncoded.Data,
                Is.EqualTo(new byte[] { 3, 7 }));
            Assert.That(itemsDecoded.Value?.Items.Select(item =>
                item.IntegerValue), Is.EqualTo(new[]
                {
                    new System.Numerics.BigInteger(3),
                    new System.Numerics.BigInteger(7)
                }));
        });
    }

    [Test]
    public void ExecutesNestedReplacementAndStandardEncodingPlans()
    {
        EcnFixture fixture = Compile(
            "NestedExecution ENCODING-DEFINITIONS ::= BEGIN\n"
                + "inner #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "standard #INT ::= { USE-SET DER }\n"
                + "#Wrapped {< #Element >} ::= #CONCATENATION { value #Element }\n"
                + "wrappedEncoding {< #Element >} #Wrapped {< #Element >} ::= { }\n"
                + "replacement #OCTETS ::= { REPETITION-ENCODING {\n"
                + "  REPLACE STRUCTURE WITH #Wrapped ENCODED BY wrappedEncoding } }\n"
                + "END");
        EcnEncodingObjectDefinition inner = Definition(fixture, "inner");
        EcnPlanExecutionBinding nested = new EcnPlanExecutionBinding
        {
            Path = "$value",
            Category = EcnClassCategory.Integer,
            Properties = inner.Properties,
            Plan = Plan(fixture, "inner"),
            BitCount = 8
        };
        EcnPlanExecutor executor = new EcnPlanExecutor(
            new AsnResourceLimits());
        EcnPlanExecutionResult standard = executor.EncodePlan(
            Request(
                Plan(fixture, "standard"),
                IntegerValue(9),
                [nested]));
        EcnPlanExecutionResult replacement = executor.EncodePlan(
            Request(
                Plan(fixture, "replacement"),
                IntegerValue(11),
                [nested]));
        EcnDecodeMachine replacementDecoder = new EcnDecodeMachine(
            Plan(fixture, "replacement"),
            EcnClassCategory.OctetString,
            [],
            new AsnResourceLimits());
        replacementDecoder.ConfigureBindings([nested]);
        EcnPlanExecutionResult replacementPending =
            replacementDecoder.SupplyInputBits([0x00], 0, 4, false);
        EcnPlanExecutionResult decoded =
            replacementDecoder.SupplyInputBits([0xb0], 0, 4, true);

        Assert.Multiple(() =>
        {
            Assert.That(Plan(fixture, "standard").Steps.Any(step =>
                step.Kind == EcnPlanStepKind.StandardEncoding), Is.True);
            Assert.That(Plan(fixture, "replacement").Steps.Any(step =>
                step.Kind == EcnPlanStepKind.Replacement), Is.True);
            Assert.That(standard.Data, Is.EqualTo(new byte[] { 9 }));
            Assert.That(replacement.Data, Is.EqualTo(new byte[] { 11 }));
            Assert.That(replacementPending.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(replacementPending.PendingPath,
                Is.EqualTo("$value"));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(11)));
        });
    }

    [Test]
    public void AppliesReversibleTransformPipelinesSymmetrically()
    {
        EcnFixture fixture = Compile(
            "TransformExecution ENCODING-DEFINITIONS ::= BEGIN\n"
                + "mapped #INT ::= { USE #INT MAPPING TRANSFORMS "
                + "{{ INT-TO-INT add:1 }, { INT-TO-INT multiply:2 }} }\n"
                + "END");
        EcnPlanExecutor executor = new EcnPlanExecutor(
            new AsnResourceLimits());
        EcnPlanExecutionResult encoded = executor.EncodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "mapped"),
                Category = EcnClassCategory.Integer,
                Value = IntegerValue(3)
            });
        EcnPlanExecutionBinding root = new EcnPlanExecutionBinding
        {
            Path = "$value",
            Category = EcnClassCategory.Integer,
            BitCount = encoded.BitCount
        };
        EcnPlanExecutionResult decoded = executor.DecodePlan(
            DecodeRequest(Plan(fixture, "mapped"), encoded, [root]));

        Assert.Multiple(() =>
        {
            Assert.That(Plan(fixture, "mapped").Steps.Count(step =>
                step.Kind == EcnPlanStepKind.Transformation), Is.EqualTo(2));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
        });
    }

    [Test]
    public void PausesForControlsAndRejectsFinalPartialFields()
    {
        EcnFixture fixture = Compile(
            "ControlExecution ENCODING-DEFINITIONS ::= BEGIN\n"
                + "conditional {< REFERENCE : enabled >} #CONDITIONAL-INT ::= { IF enabled }\n"
                + "fixed #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                + "END");
        EcnPlanExecutor executor = new EcnPlanExecutor(
            new AsnResourceLimits());
        EcnPlanExecutionBinding enabled = new EcnPlanExecutionBinding
        {
            Path = "enabled",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = false
            }
        };
        EcnPlanExecutionResult skipped = executor.EncodePlan(
            new EcnPlanExecutionRequest
            {
                Plan = Plan(fixture, "conditional"),
                Category = EcnClassCategory.Integer,
                Value = IntegerValue(7),
                Bindings = [enabled]
            });
        EcnDecodeMachine waiting = new EcnDecodeMachine(
            Plan(fixture, "conditional"),
            EcnClassCategory.Integer,
            [],
            new AsnResourceLimits());
        EcnPlanExecutionResult controlPending = waiting.DecodeAvailable();
        EcnDecodeMachine partial = new EcnDecodeMachine(
            Plan(fixture, "fixed"),
            EcnClassCategory.Integer,
            Definition(fixture, "fixed").Properties,
            new AsnResourceLimits());
        EcnPlanExecutionResult truncated = partial.SupplyInputBits(
            [0xa0],
            0,
            3,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.BitCount, Is.EqualTo(0));
            Assert.That(controlPending.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(controlPending.PendingPath, Is.EqualTo("enabled"));
            Assert.That(truncated.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(truncated.ErrorCode, Is.EqualTo("ASNECN0207"));
            Assert.That(truncated.PendingPath, Is.EqualTo("$value"));
        });
    }

    private static EcnPlanExecutionRequest Request(
        EcnExecutablePlan plan,
        AsnRuntimeValue value,
        EcnPlanExecutionBinding[] bindings)
    {
        return new EcnPlanExecutionRequest
        {
            Plan = plan,
            Value = value,
            Bindings = bindings
        };
    }

    private static EcnPlanExecutionRequest DecodeRequest(
        EcnExecutablePlan plan,
        EcnPlanExecutionResult encoded,
        EcnPlanExecutionBinding[] bindings)
    {
        return new EcnPlanExecutionRequest
        {
            Plan = plan,
            Bindings = bindings,
            Data = encoded.Data,
            BitCount = encoded.BitCount,
            FinalBlock = true
        };
    }

    private static void AssertEveryBitSplit(
        EcnFixture fixture,
        string name,
        EcnClassCategory category,
        AsnRuntimeValue value,
        byte[] expectedData,
        int expectedBitCount)
    {
        EcnExecutablePlan plan = Plan(fixture, name);
        EcnEncodingProperty[] properties = Definition(
            fixture,
            name).Properties;
        int split = 0;
        while (split <= expectedBitCount)
        {
            EcnEncodeMachine encoder = new EcnEncodeMachine(
                plan,
                category,
                properties,
                new AsnResourceLimits());
            EcnPlanExecutionResult prepared = encoder.BeginEncoding(
                value,
                []);
            EcnPlanExecutionResult first = encoder.TakeOutputBits(split);
            EcnPlanExecutionResult second = encoder.TakeOutputBits(
                expectedBitCount - split);
            EcnBitField combined = new EcnBitFieldOperations().AppendFields(
                new EcnBitFieldOperations().CreateFromBytes(
                    first.Data,
                    first.BitCount),
                new EcnBitFieldOperations().CreateFromBytes(
                    second.Data,
                    second.BitCount));

            EcnDecodeMachine decoder = new EcnDecodeMachine(
                plan,
                category,
                properties,
                new AsnResourceLimits());
            EcnPlanExecutionResult decoded;
            if (split == expectedBitCount)
            {
                decoded = decoder.SupplyInputBits(
                    first.Data,
                    0,
                    first.BitCount,
                    true);
            }
            else
            {
                EcnPlanExecutionResult waiting = decoder.SupplyInputBits(
                    first.Data,
                    0,
                    first.BitCount,
                    false);
                Assert.That(
                    waiting.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    name + " decoder split " + split);
                decoded = decoder.SupplyInputBits(
                    second.Data,
                    0,
                    second.BitCount,
                    true);
            }

            Assert.Multiple(() =>
            {
                Assert.That(
                    prepared.Status,
                    Is.EqualTo(AsnDecodeStatus.Done),
                    name + " encoder split " + split);
                Assert.That(
                    prepared.BitCount,
                    Is.EqualTo(expectedBitCount),
                    name + " bit count at split " + split);
                Assert.That(
                    combined.Data,
                    Is.EqualTo(expectedData),
                    name + " encoded data at split " + split);
                Assert.That(
                    combined.BitCount,
                    Is.EqualTo(expectedBitCount),
                    name + " combined bit count at split " + split);
                Assert.That(
                    decoded.Status,
                    Is.EqualTo(AsnDecodeStatus.Done),
                    name + " decoder result at split " + split + ": "
                        + decoded.ErrorCode + " " + decoded.ErrorMessage);
                AssertRuntimeValue(value, decoded.Value, name, split);
            });
            split = split + 1;
        }
    }

    private static void AssertStructuredEveryBitSplit(
        EcnExecutablePlan plan,
        AsnRuntimeValue value,
        System.Func<EcnPlanExecutionBinding[]> createBindings,
        byte[] expectedData,
        int expectedBitCount,
        System.Action<AsnRuntimeValue> verifyValue)
    {
        int split = 0;
        while (split <= expectedBitCount)
        {
            EcnEncodeMachine encoder = new EcnEncodeMachine(
                plan,
                EcnClassCategory.Unknown,
                [],
                new AsnResourceLimits());
            EcnPlanExecutionResult prepared = encoder.BeginEncoding(
                value,
                createBindings());
            EcnPlanExecutionResult first = encoder.TakeOutputBits(split);
            EcnPlanExecutionResult second = encoder.TakeOutputBits(
                expectedBitCount - split);
            EcnBitFieldOperations bits = new EcnBitFieldOperations();
            EcnBitField combined = bits.AppendFields(
                bits.CreateFromBytes(first.Data, first.BitCount),
                bits.CreateFromBytes(second.Data, second.BitCount));

            EcnDecodeMachine decoder = new EcnDecodeMachine(
                plan,
                EcnClassCategory.Unknown,
                [],
                new AsnResourceLimits());
            decoder.ConfigureBindings(createBindings());
            EcnPlanExecutionResult decoded;
            if (split == expectedBitCount)
            {
                decoded = decoder.SupplyInputBits(
                    first.Data,
                    0,
                    first.BitCount,
                    true);
            }
            else
            {
                EcnPlanExecutionResult waiting = decoder.SupplyInputBits(
                    first.Data,
                    0,
                    first.BitCount,
                    false);
                Assert.That(
                    waiting.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    plan.ObjectName + " decoder split " + split);
                decoded = decoder.SupplyInputBits(
                    second.Data,
                    0,
                    second.BitCount,
                    true);
            }

            Assert.Multiple(() =>
            {
                Assert.That(prepared.Status,
                    Is.EqualTo(AsnDecodeStatus.Done));
                Assert.That(combined.Data,
                    Is.EqualTo(expectedData),
                    plan.ObjectName + " encoder split " + split);
                Assert.That(combined.BitCount,
                    Is.EqualTo(expectedBitCount));
                Assert.That(
                    decoded.Status,
                    Is.EqualTo(AsnDecodeStatus.Done),
                    plan.ObjectName + " decoder split " + split + ": "
                        + decoded.ErrorCode + " " + decoded.ErrorMessage);
                Assert.That(decoded.ConsumedBitCount,
                    Is.EqualTo(expectedBitCount));
                Assert.That(decoded.Value, Is.Not.Null);
                if (decoded.Value != null)
                {
                    verifyValue(decoded.Value);
                }
            });
            split = split + 1;
        }
    }

    private static EcnPlanExecutionBinding[] AlignedPairBindings(
        EcnFixture fixture)
    {
        EcnEncodingObjectDefinition prefix = Definition(fixture, "threeBits");
        EcnEncodingObjectDefinition value = Definition(fixture, "alignedOctet");
        return
        [
            new EcnPlanExecutionBinding
            {
                Path = "prefix",
                Category = EcnClassCategory.Boolean,
                Properties = prefix.Properties,
                Plan = Plan(fixture, "threeBits")
            },
            new EcnPlanExecutionBinding
            {
                Path = "value",
                Category = EcnClassCategory.Integer,
                Properties = value.Properties,
                Plan = Plan(fixture, "alignedOctet")
            }
        ];
    }

    private static EcnPlanExecutionBinding[] PacketBindings(
        EcnFixture fixture)
    {
        return
        [
            new EcnPlanExecutionBinding
            {
                Path = "length",
                Category = EcnClassCategory.Integer,
                Properties = Definition(fixture, "octet").Properties,
                BitCount = 8
            },
            new EcnPlanExecutionBinding
            {
                Path = "value",
                Category = EcnClassCategory.OctetString
            }
        ];
    }

    private static void AssertRuntimeValue(
        AsnRuntimeValue expected,
        AsnRuntimeValue? actual,
        string name,
        int split)
    {
        Assert.That(actual, Is.Not.Null, name + " value at split " + split);
        Assert.That(
            actual?.Kind,
            Is.EqualTo(expected.Kind),
            name + " kind at split " + split);
        if (expected.Kind == AsnRuntimeValueKind.Boolean)
        {
            Assert.That(
                actual?.BooleanValue,
                Is.EqualTo(expected.BooleanValue),
                name + " boolean at split " + split);
        }
        else if (expected.Kind == AsnRuntimeValueKind.Integer)
        {
            Assert.That(
                actual?.IntegerValue,
                Is.EqualTo(expected.IntegerValue),
                name + " integer at split " + split);
        }
        else if (expected.Kind == AsnRuntimeValueKind.OctetString)
        {
            Assert.That(
                actual?.Octets,
                Is.EqualTo(expected.Octets),
                name + " octets at split " + split);
        }
        else if (expected.Kind == AsnRuntimeValueKind.CharacterString)
        {
            Assert.That(
                actual?.TextValue,
                Is.EqualTo(expected.TextValue),
                name + " text at split " + split);
        }
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

    private static AsnRuntimeValue OctetStringValue(byte[] value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = value
        };
    }

    private static AsnRuntimeValue CharacterStringValue(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnRuntimeValue IntegerValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnRuntimeFieldValue Field(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
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
            "plan-execution.edm",
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
        Assert.That(session.GetDiagnostics(), Is.Empty);
        EcnModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnSymbolTable table = new EcnSymbolTableBuilder(session.Diagnostics)
            .BuildTable(module, []);
        return new EcnFixture
        {
            Diagnostics = session.Diagnostics,
            Table = table
        };
    }

    private class EcnFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal EcnSymbolTable Table = null!;
    }
}
