using asn1;

namespace asn1.Tests;

public class UnknownExtensionVersionRoundTripTests
{
    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.ExtendedXer)]
    public void OlderAndNewerSchemasRoundTripInBothDirections(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor older = MessageType(false);
        AsnTypeDescriptor newer = MessageType(true);
        ConfiguredCodec olderCodec = Codec(older, rules);
        ConfiguredCodec newerCodec = Codec(newer, rules);
        AsnCodecOptions options = Options(rules);

        AsnEncodeResult newBytes = Encode(
            newerCodec,
            NewerValue(),
            options);
        AsnDecodeResult preserved = Decode(
            olderCodec,
            newBytes.Data,
            options);
        AsnEncodeResult preservedBytes = Encode(
            olderCodec,
            RequireValue(preserved),
            options);
        AsnDecodeResult recovered = Decode(
            newerCodec,
            preservedBytes.Data,
            options);

        AsnEncodeResult oldBytes = Encode(
            olderCodec,
            OlderValue(),
            options);
        AsnDecodeResult upgraded = Decode(
            newerCodec,
            oldBytes.Data,
            options);
        AsnEncodeResult upgradedBytes = Encode(
            newerCodec,
            RequireValue(upgraded),
            options);
        AsnDecodeResult downgraded = Decode(
            olderCodec,
            upgradedBytes.Data,
            options);

        AsnRuntimeValue preservedValue = RequireValue(preserved);
        AsnRuntimeValue recoveredFuture = RequireValue(recovered).Fields
            .Single(field => field.Name == "future").Value
            ?? throw new AssertionException("The newer field was not recovered.");
        Assert.Multiple(() =>
        {
            Assert.That(newBytes.Success, Is.True, Format(newBytes));
            Assert.That(preserved.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(preserved));
            Assert.That(
                preservedValue.UnknownExtensions.Length
                    + preservedValue.UnknownFields.Length,
                Is.EqualTo(1));
            Assert.That(preservedBytes.Success, Is.True, Format(preservedBytes));
            Assert.That(preservedBytes.Data, Is.EqualTo(newBytes.Data));
            Assert.That(recovered.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(recovered));
            Assert.That(recoveredFuture.TextValue, Is.EqualTo("future-value"));
            Assert.That(oldBytes.Success, Is.True, Format(oldBytes));
            Assert.That(upgraded.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(upgraded));
            Assert.That(upgradedBytes.Data, Is.EqualTo(oldBytes.Data));
            Assert.That(downgraded.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(downgraded));
            Assert.That(RequireValue(downgraded).Fields.Single().Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
        });
    }

    [TestCase(AsnEncodingRules.Ber, AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.Uper, AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Oer, AsnEncodingRules.ExtendedXer)]
    [TestCase(AsnEncodingRules.ExtendedXer, AsnEncodingRules.Ber)]
    public void CrossRuleOpaqueTransferIsExplicitlyRejected(
        AsnEncodingRules sourceRules,
        AsnEncodingRules targetRules)
    {
        AsnTypeDescriptor older = MessageType(false);
        AsnTypeDescriptor newer = MessageType(true);
        AsnEncodeResult source = Encode(
            Codec(newer, sourceRules),
            NewerValue(),
            Options(sourceRules));
        AsnDecodeResult preserved = Decode(
            Codec(older, sourceRules),
            source.Data,
            Options(sourceRules));

        AsnEncodeResult transferred = Encode(
            Codec(older, targetRules),
            RequireValue(preserved),
            Options(targetRules));

        Assert.Multiple(() =>
        {
            Assert.That(source.Success, Is.True, Format(source));
            Assert.That(preserved.Status, Is.EqualTo(AsnDecodeStatus.Done),
                Format(preserved));
            Assert.That(transferred.Success, Is.False);
            Assert.That(transferred.Diagnostics, Is.Not.Empty);
        });
    }

    private static AsnTypeDescriptor MessageType(bool newer)
    {
        AsnFieldDescriptor root = Field(
            "root",
            Type(AsnTypeKind.Integer, "Root"),
            0,
            false);
        AsnTypeDescriptor message = Type(AsnTypeKind.Sequence, "Message");
        message.Extensible = true;
        message.RootFields = [root];
        if (!newer)
        {
            message.Fields = [root];
            return message;
        }

        AsnFieldDescriptor future = Field(
            "future",
            Type(AsnTypeKind.Utf8String, "Future"),
            1,
            true);
        message.Fields = [root, future];
        message.ExtensionFields = [future];
        return message;
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int index,
        bool extension)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Index = index,
            Extension = extension,
            Optional = extension,
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Implicit,
                    Number = index
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

    private static ConfiguredCodec Codec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules)
    {
        AsnReflectionCatalog catalog = new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment.Message",
                    Name = "Message",
                    QualifiedName = "Schema.Message",
                    GeneratedName = "Message",
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
        return (ConfiguredCodec)new CodecProviderImplementation().CreateCodec(
            catalog,
            Options(rules));
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = false
        };
    }

    private static AsnRuntimeValue OlderValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("root", Integer(7))]
        };
    }

    private static AsnRuntimeValue NewerValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("root", Integer(7)),
                Present("future", Text("future-value"))
            ]
        };
    }

    private static AsnRuntimeFieldValue Present(
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

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnEncodeResult Encode(
        ConfiguredCodec codec,
        AsnRuntimeValue value,
        AsnCodecOptions options)
    {
        return codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Message",
            Value = value,
            Options = options
        });
    }

    private static AsnDecodeResult Decode(
        ConfiguredCodec codec,
        byte[] data,
        AsnCodecOptions options)
    {
        return codec.Decode(new AsnDecodeRequest
        {
            TypeName = "Schema.Message",
            Data = data,
            Options = options
        });
    }

    private static AsnRuntimeValue RequireValue(AsnDecodeResult result)
    {
        return result.Value ?? throw new AssertionException(Format(result));
    }

    private static string Format(AsnEncodeResult result)
    {
        return string.Join(
            " | ",
            result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }

    private static string Format(AsnDecodeResult result)
    {
        return string.Join(
            " | ",
            result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
