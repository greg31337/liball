using asn1;

namespace asn1.Tests;

public class OpenTypeRuntimeCodecTests
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
    public void ResolvedTablesDelegatePayloadsForEveryEncodingFamily(
        AsnEncodingRules rules)
    {
        RecordingCodec codec = new RecordingCodec();
        AsnRuntimeValue decodedValue = IntegerRuntimeValue(42);
        codec.DecodeResult = new AsnDecodeResult
        {
            Status = AsnDecodeStatus.Done,
            Value = decodedValue,
            ConsumedCount = 3
        };
        codec.EncodeResult = new AsnEncodeResult
        {
            Success = true,
            Data = [0xaa, 0xbb, 0xcc],
            WrittenCount = 2
        };
        AsnObjectSetRegistry registry = new AsnObjectSetRegistry();
        OpenTypeRuntimeCodec runtime = new OpenTypeRuntimeCodec(codec, registry);
        AsnInformationObjectSet objectSet = CreateObjectSet(true, 7, "Payload");
        AsnCodecOptions options = CreateOptions(rules, AsnUnknownDataPolicy.Reject);
        AsnRuntimeValue key = IntegerRuntimeValue(7);
        byte[] payload = [0x01, 0x02, 0x03];

        AsnDecodeResult decoded = runtime.DecodePayload(
            objectSet,
            key,
            payload,
            options);
        AsnOpenTypeValue open = decoded.Value?.OpenTypeValue
            ?? throw new InvalidOperationException();
        payload[0] = 0xff;

        Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
        Assert.That(decoded.ConsumedCount, Is.EqualTo(3));
        Assert.That(open.Resolved, Is.True);
        Assert.That(open.Unresolved, Is.False);
        Assert.That(open.TypeName, Is.EqualTo("Payload"));
        Assert.That(open.Value, Is.SameAs(decodedValue));
        Assert.That(open.DispatchKey, Is.SameAs(key));
        Assert.That(open.EncodingRules, Is.EqualTo(rules));
        Assert.That(open.OriginalData, Is.EqualTo(new byte[] { 0x01, 0x02, 0x03 }));
        Assert.That(open.IsValid(), Is.True);
        Assert.That(codec.LastDecodeRequest?.TypeName, Is.EqualTo("Payload"));
        Assert.That(codec.LastDecodeRequest?.Options, Is.SameAs(options));

        AsnEncodeResult encoded = runtime.EncodePayload(objectSet, open, options);

        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0xaa, 0xbb }));
        Assert.That(encoded.WrittenCount, Is.EqualTo(2));
        Assert.That(codec.LastEncodeRequest?.TypeName, Is.EqualTo("Payload"));
        Assert.That(codec.LastEncodeRequest?.Value, Is.SameAs(decodedValue));
        Assert.That(codec.LastEncodeRequest?.Options, Is.SameAs(options));
    }

    [Test]
    public void CodecRegistryOverridesCatalogRegistrationForDispatch()
    {
        RecordingCodec codec = new RecordingCodec
        {
            DecodeResult = new AsnDecodeResult
            {
                Status = AsnDecodeStatus.Done,
                Value = IntegerRuntimeValue(9),
                ConsumedCount = 1
            }
        };
        AsnObjectSetRegistry catalogRegistry = new AsnObjectSetRegistry();
        AsnObjectSetRegistry codecRegistry = new AsnObjectSetRegistry();
        OpenTypeRuntimeCodec runtime = new OpenTypeRuntimeCodec(codec, catalogRegistry);
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            StableId = "Catalog.Dynamic",
            Extensible = true
        };
        codecRegistry.RegisterObject(
            objectSet,
            new AsnInformationObject { StableId = "Catalog.dynamic" },
            CreateDispatch(9, "DynamicPayload"));
        AsnCodecOptions catalogOptions = CreateOptions(
            AsnEncodingRules.Ber,
            AsnUnknownDataPolicy.Preserve);
        AsnCodecOptions codecOptions = CreateOptions(
            AsnEncodingRules.Ber,
            AsnUnknownDataPolicy.Preserve);
        codecOptions.ObjectSetRegistry = codecRegistry;

        AsnDecodeResult catalogResult = runtime.DecodePayload(
            objectSet,
            IntegerRuntimeValue(9),
            [0x09],
            catalogOptions);
        AsnDecodeResult codecResult = runtime.DecodePayload(
            objectSet,
            IntegerRuntimeValue(9),
            [0x09],
            codecOptions);

        Assert.That(catalogResult.Value?.OpenTypeValue?.Unresolved, Is.True);
        Assert.That(codecResult.Value?.OpenTypeValue?.Resolved, Is.True);
        Assert.That(codec.LastDecodeRequest?.TypeName, Is.EqualTo("DynamicPayload"));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(1));
    }

    [Test]
    public void UnknownExtensiblePayloadsFollowRejectPreserveAndSkipPolicies()
    {
        RecordingCodec codec = new RecordingCodec();
        OpenTypeRuntimeCodec runtime = new OpenTypeRuntimeCodec(
            codec,
            new AsnObjectSetRegistry());
        AsnInformationObjectSet extensible = new AsnInformationObjectSet
        {
            StableId = "Catalog.Extensible",
            Extensible = true
        };
        AsnRuntimeValue key = IntegerRuntimeValue(99);
        byte[] source = [0xde, 0xad];
        AsnCodecOptions preserveOptions = CreateOptions(
            AsnEncodingRules.Uper,
            AsnUnknownDataPolicy.Preserve);

        AsnDecodeResult preserved = runtime.DecodePayload(
            extensible,
            key,
            source,
            preserveOptions);
        AsnOpenTypeValue preservedOpen = preserved.Value?.OpenTypeValue
            ?? throw new InvalidOperationException();
        source[0] = 0x00;
        Assert.That(preserved.Status, Is.EqualTo(AsnDecodeStatus.Done));
        Assert.That(preservedOpen.Unresolved, Is.True);
        Assert.That(preservedOpen.HasOriginalData, Is.True);
        Assert.That(preservedOpen.OriginalData, Is.EqualTo(new byte[] { 0xde, 0xad }));
        Assert.That(preservedOpen.IsValid(), Is.True);

        AsnEncodeResult reencoded = runtime.EncodePayload(
            extensible,
            preservedOpen,
            preserveOptions);
        Assert.That(reencoded.Success, Is.True);
        Assert.That(reencoded.Data, Is.EqualTo(new byte[] { 0xde, 0xad }));
        reencoded.Data[0] = 0x00;
        Assert.That(preservedOpen.OriginalData[0], Is.EqualTo(0xde));

        AsnCodecOptions differentRules = CreateOptions(
            AsnEncodingRules.Oer,
            AsnUnknownDataPolicy.Preserve);
        Assert.That(runtime.EncodePayload(extensible, preservedOpen, differentRules)
            .Diagnostics.Single().Code, Is.EqualTo("ASNE0007"));

        AsnCodecOptions skipOptions = CreateOptions(
            AsnEncodingRules.Uper,
            AsnUnknownDataPolicy.Skip);
        AsnOpenTypeValue skipped = runtime.DecodePayload(
            extensible,
            key,
            [0xbe, 0xef],
            skipOptions).Value?.OpenTypeValue ?? throw new InvalidOperationException();
        Assert.That(skipped.Unresolved, Is.True);
        Assert.That(skipped.HasOriginalData, Is.False);
        Assert.That(skipped.OriginalData, Is.Empty);
        Assert.That(skipped.IsValid(), Is.True);
        Assert.That(runtime.EncodePayload(extensible, skipped, skipOptions).Success,
            Is.False);

        AsnCodecOptions rejectOptions = CreateOptions(
            AsnEncodingRules.Uper,
            AsnUnknownDataPolicy.Reject);
        Assert.That(runtime.DecodePayload(extensible, key, [0x01], rejectOptions)
            .Diagnostics.Single().Code, Is.EqualTo("ASND0001"));

        AsnInformationObjectSet closed = new AsnInformationObjectSet();
        Assert.That(runtime.DecodePayload(closed, key, [0x01], preserveOptions)
            .Status, Is.EqualTo(AsnDecodeStatus.Error));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(0));
        Assert.That(codec.EncodeCallCount, Is.EqualTo(0));
    }

    [Test]
    public void PayloadFailuresAndLimitsReturnStableDiagnostics()
    {
        RecordingCodec codec = new RecordingCodec
        {
            DecodeResult = new AsnDecodeResult
            {
                Status = AsnDecodeStatus.Done,
                Value = IntegerRuntimeValue(1),
                ConsumedCount = 1
            },
            EncodeResult = new AsnEncodeResult
            {
                Success = true,
                Data = [0x01, 0x02],
                WrittenCount = 2
            }
        };
        OpenTypeRuntimeCodec runtime = new OpenTypeRuntimeCodec(
            codec,
            new AsnObjectSetRegistry());
        AsnInformationObjectSet objectSet = CreateObjectSet(true, 1, "Payload");
        AsnRuntimeValue key = IntegerRuntimeValue(1);
        AsnCodecOptions options = CreateOptions(
            AsnEncodingRules.Ber,
            AsnUnknownDataPolicy.Reject);

        Assert.That(runtime.DecodePayload(objectSet, key, [0x01, 0x02], options)
            .Diagnostics.Single().Code, Is.EqualTo("ASND0002"));

        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = "Payload",
            TypeName = "Payload",
            Value = IntegerRuntimeValue(1),
            DispatchKey = key
        };
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 1
        };
        AsnEncodeResult limitedEncode = runtime.EncodePayload(objectSet, open, options);
        Assert.That(limitedEncode.Success, Is.False);
        Assert.That(limitedEncode.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Resource));
        Assert.That(limitedEncode.Diagnostics.Single().Code, Is.EqualTo("ASNR0001"));

        int callsBeforeLimitedDecode = codec.DecodeCallCount;
        AsnDecodeResult limitedDecode = runtime.DecodePayload(
            objectSet,
            key,
            [0x01, 0x02],
            options);
        Assert.That(limitedDecode.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Resource));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(callsBeforeLimitedDecode));

        options.ResourceLimits = null;
        open.ResolvedTypeId = "OtherPayload";
        open.TypeName = "OtherPayload";
        Assert.That(runtime.EncodePayload(objectSet, open, options)
            .Diagnostics.Single().Code, Is.EqualTo("ASNE0003"));
        open.ResolvedTypeId = "Payload";
        open.TypeName = "Payload";
        codec.EncodeResult = new AsnEncodeResult();
        Assert.That(runtime.EncodePayload(objectSet, open, options)
            .Diagnostics.Single().Code, Is.EqualTo("ASNE0005"));

        open.Unresolved = true;
        Assert.That(runtime.EncodePayload(objectSet, open, options)
            .Diagnostics.Single().Code, Is.EqualTo("ASNE0001"));
        open.Unresolved = false;

        AsnCodecOptions ecnOptions = CreateOptions(
            AsnEncodingRules.Ecn,
            AsnUnknownDataPolicy.Reject);
        Assert.That(runtime.DecodePayload(objectSet, key, [0x01], ecnOptions)
            .Diagnostics.Single().Code, Is.EqualTo("ASND0004"));
        Assert.That(runtime.EncodePayload(objectSet, open, ecnOptions)
            .Diagnostics.Single().Code, Is.EqualTo("ASNE0004"));
    }

    [Test]
    public void ObjectTableFanoutIsBoundedBeforeDispatchLookup()
    {
        RecordingCodec codec = new RecordingCodec();
        OpenTypeRuntimeCodec runtime = new OpenTypeRuntimeCodec(
            codec,
            new AsnObjectSetRegistry());
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            DispatchEntries =
            [
                CreateDispatch(1, "FirstPayload"),
                CreateDispatch(2, "SecondPayload")
            ]
        };
        AsnCodecOptions options = CreateOptions(
            AsnEncodingRules.Ber,
            AsnUnknownDataPolicy.Reject);
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumCollectionElements = 1
        };

        AsnDecodeResult result = runtime.DecodePayload(
            objectSet,
            IntegerRuntimeValue(1),
            [0x01],
            options);

        Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
        Assert.That(result.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Resource));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("ASNR0011"));
        Assert.That(codec.DecodeCallCount, Is.EqualTo(0));
    }

    private static AsnInformationObjectSet CreateObjectSet(
        bool extensible,
        int key,
        string typeName)
    {
        return new AsnInformationObjectSet
        {
            StableId = "Catalog.Set",
            Extensible = extensible,
            DispatchEntries = [CreateDispatch(key, typeName)]
        };
    }

    private static AsnOpenTypeDispatchEntry CreateDispatch(int key, string typeName)
    {
        return new AsnOpenTypeDispatchEntry
        {
            StableId = "dispatch:" + key.ToString(),
            Key = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = key
            },
            Type = new AsnType { Name = typeName, Kind = AsnTypeKind.Integer }
        };
    }

    private static AsnRuntimeValue IntegerRuntimeValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnCodecOptions CreateOptions(
        AsnEncodingRules rules,
        AsnUnknownDataPolicy policy)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = policy
        };
    }

    private class RecordingCodec : IAsnCodec
    {
        public AsnDecodeResult DecodeResult = new AsnDecodeResult();
        public AsnEncodeResult EncodeResult = new AsnEncodeResult();
        public AsnDecodeRequest? LastDecodeRequest;
        public AsnEncodeRequest? LastEncodeRequest;
        public int DecodeCallCount;
        public int EncodeCallCount;

        public AsnEncodeResult Encode(AsnEncodeRequest request)
        {
            this.LastEncodeRequest = request;
            this.EncodeCallCount = this.EncodeCallCount + 1;
            return this.EncodeResult;
        }

        public AsnDecodeResult Decode(AsnDecodeRequest request)
        {
            this.LastDecodeRequest = request;
            this.DecodeCallCount = this.DecodeCallCount + 1;
            return this.DecodeResult;
        }

        public AsnDecodeResult TryDecodeSpan(
            ReadOnlySpan<byte> input,
            string typeName,
            AsnCodecOptions options)
        {
            return this.DecodeResult;
        }

        public AsnEncodeResult EncodeToSpan(
            AsnEncodeRequest request,
            Span<byte> destination)
        {
            return this.EncodeResult;
        }

        public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)
        {
            return new AsnCanonicalityResult();
        }

        public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
        {
            return new AsnTranscodeResult();
        }

        public IAsnStreamingCodec CreateStreamingCodec()
        {
            throw new NotSupportedException();
        }

        public IAsnIncrementalDecoder CreateIncrementalDecoder(
            string typeName,
            AsnCodecOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
