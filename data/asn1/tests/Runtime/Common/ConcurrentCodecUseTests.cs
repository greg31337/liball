using asn1;

namespace asn1.Tests;

public class ConcurrentCodecUseTests
{
    [Test]
    public void ConfiguredCodecSnapshotsCatalogCollectionsForConcurrentUse()
    {
        AsnReflectionCatalog catalog = BooleanCatalog();
        IAsnCodec codec = Codec(catalog, AsnEncodingRules.Der);
        catalog.Assignments = [];
        catalog.Modules = [];
        System.Threading.Tasks.Task<AsnDecodeResult>[] tasks =
            new System.Threading.Tasks.Task<AsnDecodeResult>[64];

        int index = 0;
        while (index < tasks.Length)
        {
            bool expected = index % 2 == 0;
            tasks[index] = System.Threading.Tasks.Task.Run(() =>
            {
                AsnEncodeResult encoded = codec.Encode(
                    new AsnEncodeRequest
                    {
                        TypeName = "Schema.Flag",
                        Value = Boolean(expected)
                    });
                return codec.Decode(new AsnDecodeRequest
                {
                    TypeName = "Schema.Flag",
                    Data = encoded.Data
                });
            });
            index = index + 1;
        }

        System.Threading.Tasks.Task.WaitAll(tasks);
        index = 0;
        while (index < tasks.Length)
        {
            Assert.That(tasks[index].Result.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(tasks[index].Result.Value?.BooleanValue,
                Is.EqualTo(index % 2 == 0));
            index = index + 1;
        }
    }

    [Test]
    public void CompiledEcnPlanSnapshotSupportsConcurrentExecution()
    {
        AsnCompilationResult compiled = new EcnCompilerImplementation()
            .CompileEcn(
                new AsnSourceUnit
                {
                    Name = "concurrent.edm",
                    Text = "Concurrent ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                        + "END"
                },
                [IntegerModule()]);
        AsnReflectionCatalog catalog = compiled.ReflectionCatalog
            ?? throw new AssertionException("ECN reflection was not built.");
        IAsnCodec codec = Codec(catalog, AsnEncodingRules.Ecn);
        catalog.Assignments = [];
        catalog.EcnCodecPlans = [];
        System.Threading.Tasks.Task<AsnDecodeResult>[] tasks =
            new System.Threading.Tasks.Task<AsnDecodeResult>[32];

        int index = 0;
        while (index < tasks.Length)
        {
            int expected = index;
            tasks[index] = System.Threading.Tasks.Task.Run(() =>
            {
                AsnEncodeResult encoded = codec.Encode(
                    new AsnEncodeRequest
                    {
                        TypeName = "Schema.scalar",
                        Value = Integer(expected)
                    });
                return codec.Decode(new AsnDecodeRequest
                {
                    TypeName = "Schema.scalar",
                    Data = encoded.Data
                });
            });
            index = index + 1;
        }

        System.Threading.Tasks.Task.WaitAll(tasks);
        index = 0;
        while (index < tasks.Length)
        {
            Assert.That(tasks[index].Result.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(tasks[index].Result.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(index)));
            index = index + 1;
        }
    }

    [Test]
    public void PerInstructionRegistryIsAnImmutableLookupSnapshot()
    {
        MutableRegistry registry = new MutableRegistry();
        AsnPerInstructionRegistration registration =
            new AsnPerInstructionRegistration
            {
                Keyword = "APPLICATION-RULE",
                HandlerId = "handler.original",
                SupportsUnaligned = true,
                Reversible = true,
                ConflictingKeywords = ["OTHER"]
            };
        registry.Registrations = [registration];
        ConfiguredCodec codec = (ConfiguredCodec)new CodecProviderImplementation()
            .CreateCodec(
                BooleanCatalog(),
                new AsnCodecOptions
                {
                    EncodingRules = AsnEncodingRules.Uper,
                    PerInstructionRegistry = registry
                });
        registration.Keyword = "MUTATED";
        registration.HandlerId = "handler.mutated";
        registration.ConflictingKeywords[0] = "MUTATED";
        registry.Registrations = [];
        IPerInstructionRegistry snapshot = codec.Options.PerInstructionRegistry
            ?? throw new AssertionException("Registry was not snapshotted.");
        System.Threading.Tasks.Task<IPerInstructionHandler?>[] tasks =
            new System.Threading.Tasks.Task<IPerInstructionHandler?>[32];

        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = System.Threading.Tasks.Task.Run(() =>
                snapshot.ResolveHandler("APPLICATION-RULE"));
            index = index + 1;
        }
        System.Threading.Tasks.Task.WaitAll(tasks);
        AsnPerInstructionRegistration captured =
            snapshot.GetRegistrations().Single();

        Assert.Multiple(() =>
        {
            Assert.That(captured.Keyword, Is.EqualTo("APPLICATION-RULE"));
            Assert.That(captured.HandlerId, Is.EqualTo("handler.original"));
            Assert.That(captured.ConflictingKeywords,
                Is.EqualTo(new[] { "OTHER" }));
            Assert.That(tasks.All(task => task.Result == registry.Handler),
                Is.True);
            Assert.That(() => snapshot.RegisterHandler(
                new AsnPerInstructionRegistration(),
                registry.Handler), Throws.InvalidOperationException);
        });
    }

    [Test]
    public void CodecFactoriesIsolateIncrementalAndStreamOperationState()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        IAsnCodec codec = Codec(BooleanCatalog(), AsnEncodingRules.Ber);
        IAsnIncrementalDecoder first = codec.CreateIncrementalDecoder(
            "Schema.Flag",
            options);
        IAsnIncrementalDecoder second = codec.CreateIncrementalDecoder(
            "Schema.Flag",
            options);
        AsnIncrementalResult pending = first.Consume(
            new AsnIncrementalInput { Data = [0x01] });
        AsnIncrementalResult secondDone = second.Consume(
            new AsnIncrementalInput
            {
                Data = [0x01, 0x01, 0x00],
                IsFinalBlock = true
            });
        AsnIncrementalResult firstDone = first.Consume(
            new AsnIncrementalInput
            {
                Data = [0x01, 0xff],
                IsFinalBlock = true
            });
        IAsnStreamingCodec firstStream = codec.CreateStreamingCodec();
        IAsnStreamingCodec secondStream = codec.CreateStreamingCodec();
        System.Threading.Tasks.Task<AsnDecodeResult> streamTrue =
            System.Threading.Tasks.Task.Run(() => firstStream.DecodeStream(
                new MemoryStream([0x01, 0x01, 0xff]),
                "Schema.Flag",
                options));
        System.Threading.Tasks.Task<AsnDecodeResult> streamFalse =
            System.Threading.Tasks.Task.Run(() => secondStream.DecodeStream(
                new MemoryStream([0x01, 0x01, 0x00]),
                "Schema.Flag",
                options));
        System.Threading.Tasks.Task.WaitAll(streamTrue, streamFalse);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(pending.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(secondDone.Value?.BooleanValue, Is.False);
            Assert.That(firstDone.Value?.BooleanValue, Is.True);
            Assert.That(firstStream, Is.Not.SameAs(secondStream));
            Assert.That(streamTrue.Result.Value?.BooleanValue, Is.True);
            Assert.That(streamFalse.Result.Value?.BooleanValue, Is.False);
        });
    }

    private static IAsnCodec Codec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules)
    {
        return new CodecProviderImplementation().CreateCodec(
            catalog,
            Options(rules));
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

    private class MutableRegistry :
        IPerInstructionRegistry,
        IPerInstructionHandler
    {
        public AsnPerInstructionRegistration[] Registrations = [];
        public IPerInstructionHandler Handler;

        public MutableRegistry()
        {
            this.Handler = this;
        }

        public void RegisterHandler(
            AsnPerInstructionRegistration registration,
            IPerInstructionHandler handler)
        {
            this.Registrations = [registration];
            this.Handler = handler;
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            return keyword == "APPLICATION-RULE"
                ? this.Handler
                : null;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            return this.Registrations;
        }

        public AsnDiagnostic[] ValidateInstruction(
            AsnEncodingInstruction instruction,
            AsnType type)
        {
            return [];
        }

        public AsnEncodingPlanStep[] ApplyInstruction(
            AsnEncodingInstruction instruction,
            AsnType type,
            AsnEncodingPlan plan)
        {
            return [];
        }
    }
}
