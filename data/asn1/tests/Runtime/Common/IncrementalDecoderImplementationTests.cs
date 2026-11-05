using asn1;

namespace asn1.Tests;

public class IncrementalDecoderImplementationTests
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
    public void SelectsConfiguredRuleAndLeavesFollowingValueUnconsumed(
        AsnEncodingRules rules)
    {
        AsnCodecOptions options = Options(rules);
        ConfiguredCodec codec = Codec(BooleanCatalog(), options);
        byte[] first = EncodeBoolean(codec, true);
        byte[] second = EncodeBoolean(codec, false);
        byte[] input = new byte[first.Length + second.Length + 2];
        input[0] = 0xaa;
        System.Buffer.BlockCopy(first, 0, input, 1, first.Length);
        System.Buffer.BlockCopy(
            second,
            0,
            input,
            1 + first.Length,
            second.Length);
        input[input.Length - 1] = 0xbb;
        AsnIncrementalDecoder decoder = Decoder(
            codec,
            "Schema.Flag",
            options);

        AsnIncrementalResult empty = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = input,
                Offset = 1,
                HasCount = true,
                Count = 0
            });
        AsnIncrementalResult firstResult = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = input,
                Offset = 1,
                HasCount = true,
                Count = first.Length + second.Length,
                IsFinalBlock = true
            });
        AsnIncrementalResult terminal = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = input,
                Offset = 1 + first.Length,
                HasCount = true,
                Count = second.Length,
                IsFinalBlock = true
            });
        AsnIncrementalResult repeated = decoder.Complete();

        decoder.Reset();
        AsnIncrementalResult secondResult = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = input,
                Offset = 1 + first.Length,
                HasCount = true,
                Count = second.Length,
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(firstResult.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(firstResult.ConsumedCount, Is.EqualTo(first.Length));
            Assert.That(firstResult.Value?.BooleanValue, Is.True);
            Assert.That(terminal.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(terminal.ConsumedCount, Is.Zero);
            Assert.That(terminal.Diagnostics.Single().Code,
                Is.EqualTo("ASNCODEC0009"));
            Assert.That(repeated.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Value?.BooleanValue, Is.True);
            Assert.That(secondResult.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(secondResult.ConsumedCount,
                Is.EqualTo(second.Length));
            Assert.That(secondResult.Value?.BooleanValue, Is.False);
        });
    }

    [Test]
    public void EcnPrefixesRemainPendingAndResetDecodesLeftoverValue()
    {
        AsnCompilationResult compiled = new EcnCompilerImplementation()
            .CompileEcn(
                new AsnSourceUnit
                {
                    Name = "incremental.edm",
                    Text = "Incremental ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "scalar #INT ::= { ENCODING-SPACE, SIZE 16 }\n"
                        + "END"
                },
                [IntegerModule()]);
        AsnReflectionCatalog catalog = compiled.ReflectionCatalog
            ?? throw new AssertionException("ECN reflection was not built.");
        AsnCodecOptions options = Options(AsnEncodingRules.Ecn);
        ConfiguredCodec codec = Codec(catalog, options);
        byte[] first = EncodeInteger(codec, 42);
        byte[] second = EncodeInteger(codec, 7);
        byte[] combined = Join(first, second);
        AsnIncrementalDecoder decoder = Decoder(
            codec,
            "Schema.scalar",
            options);

        AsnIncrementalResult prefix = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = combined,
                HasCount = true,
                Count = 1
            });
        AsnIncrementalResult firstResult = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = combined,
                Offset = 1,
                HasCount = true,
                Count = combined.Length - 1,
                IsFinalBlock = true
            });
        decoder.Reset();
        AsnIncrementalResult secondResult = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = combined,
                Offset = first.Length,
                HasCount = true,
                Count = second.Length,
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Success, Is.True);
            Assert.That(first, Has.Length.EqualTo(2));
            Assert.That(prefix.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(prefix.ConsumedCount, Is.EqualTo(1));
            Assert.That(firstResult.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(firstResult.ConsumedCount, Is.EqualTo(1));
            Assert.That(firstResult.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(secondResult.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(secondResult.ConsumedCount,
                Is.EqualTo(second.Length));
            Assert.That(secondResult.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
        });
    }

    [Test]
    public void FinalAndExplicitCompletionMakeIncompleteInputTerminal()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        ConfiguredCodec codec = Codec(BooleanCatalog(), options);
        AsnIncrementalDecoder finalDecoder = Decoder(
            codec,
            "Schema.Flag",
            options);
        AsnIncrementalResult final = finalDecoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x01],
                IsFinalBlock = true
            });
        AsnIncrementalResult repeated = finalDecoder.Complete();

        AsnIncrementalDecoder completedDecoder = Decoder(
            codec,
            "Schema.Flag",
            options);
        AsnIncrementalResult waiting = completedDecoder.Consume(
            new AsnIncrementalInput { Data = [0x01] });
        AsnIncrementalResult completed = completedDecoder.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(final.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(final.ConsumedCount, Is.EqualTo(1));
            Assert.That(final.Diagnostics.Single().Code,
                Is.EqualTo("ASNCODEC0008"));
            Assert.That(repeated.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Diagnostics.Single().Code,
                Is.EqualTo("ASNCODEC0008"));
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(completed.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(completed.ConsumedCount, Is.Zero);
            Assert.That(completed.Diagnostics.Single().Code,
                Is.EqualTo("ASNCODEC0008"));
        });
    }

    [Test]
    public void ResetStartsAnIndependentAllocationBudget()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumTotalAllocationBytes = 100
        };
        ConfiguredCodec codec = Codec(BooleanCatalog(), options);
        byte[] encoded = EncodeBoolean(codec, true);
        IncrementalDecoderImplementation implementation =
            new IncrementalDecoderImplementation(
                codec,
                "Schema.Flag",
                options);
        AsnIncrementalDecoder decoder = new AsnIncrementalDecoder(
            implementation);

        AsnIncrementalResult first = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = encoded,
                IsFinalBlock = true
            });
        long firstAllocation = implementation.Resources.AllocatedBytes;
        decoder.Reset();
        long resetAllocation = implementation.Resources.AllocatedBytes;
        AsnIncrementalResult second = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = encoded,
                IsFinalBlock = true
            });
        long secondAllocation = implementation.Resources.AllocatedBytes;

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Has.Length.EqualTo(3));
            Assert.That(first.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(first.ConsumedCount, Is.EqualTo(3));
            Assert.That(firstAllocation, Is.EqualTo(3));
            Assert.That(resetAllocation, Is.Zero);
            Assert.That(second.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(second.ConsumedCount, Is.EqualTo(3));
            Assert.That(secondAllocation, Is.EqualTo(3));
        });
    }

    [Test]
    public void StopsBeforeByteThatWouldExceedAllocationBudget()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumTotalAllocationBytes = 2
        };
        ConfiguredCodec codec = Codec(BooleanCatalog(), options);
        AsnIncrementalDecoder decoder = Decoder(
            codec,
            "Schema.Flag",
            options);

        AsnIncrementalResult result = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x01, 0x01, 0xff],
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0005"));
            Assert.That(result.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void ValidatesConstructionAndInputRanges()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        ConfiguredCodec codec = Codec(BooleanCatalog(), options);
        IncrementalDecoderImplementation decoder =
            new IncrementalDecoderImplementation(
                codec,
                "Schema.Flag",
                options);

        Assert.Multiple(() =>
        {
            Assert.That(() => new IncrementalDecoderImplementation(
                null!,
                "Schema.Flag",
                options), Throws.ArgumentNullException);
            Assert.That(() => new IncrementalDecoderImplementation(
                codec,
                null!,
                options), Throws.ArgumentNullException);
            Assert.That(() => new IncrementalDecoderImplementation(
                codec,
                "Schema.Flag",
                null!), Throws.ArgumentNullException);
            Assert.That(() => decoder.Consume(null!),
                Throws.ArgumentNullException);
            Assert.That(() => decoder.Consume(new AsnIncrementalInput
            {
                Data = null!
            }), Throws.ArgumentException);
            Assert.That(() => decoder.Consume(new AsnIncrementalInput
            {
                Data = [1],
                Offset = 2
            }), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static AsnIncrementalDecoder Decoder(
        ConfiguredCodec codec,
        string typeName,
        AsnCodecOptions options)
    {
        return new AsnIncrementalDecoder(
            new IncrementalDecoderImplementation(
                codec,
                typeName,
                options));
    }

    private static ConfiguredCodec Codec(
        AsnReflectionCatalog catalog,
        AsnCodecOptions options)
    {
        return (ConfiguredCodec)new CodecProviderImplementation()
            .CreateCodec(catalog, options);
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

    private static byte[] EncodeBoolean(
        ConfiguredCodec codec,
        bool value)
    {
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Flag",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = value
            }
        });
        Assert.That(encoded.Success, Is.True);
        return encoded.Data;
    }

    private static byte[] EncodeInteger(
        ConfiguredCodec codec,
        int value)
    {
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.scalar",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
        });
        Assert.That(encoded.Success, Is.True);
        return encoded.Data;
    }

    private static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, result, 0, first.Length);
        System.Buffer.BlockCopy(
            second,
            0,
            result,
            first.Length,
            second.Length);
        return result;
    }
}
