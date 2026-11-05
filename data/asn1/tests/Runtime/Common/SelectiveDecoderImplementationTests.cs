using asn1;

namespace asn1.Tests;

public class SelectiveDecoderImplementationTests
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
    public void SelectsAndSkipsNestedReflectedFields(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor type = OuterType();
        AsnCodecOptions options = Options(rules);
        ConfiguredCodec codec = Configured(type, options);
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Outer",
            Value = OuterValue()
        });
        SelectiveDecoderImplementation selective =
            new SelectiveDecoderImplementation(codec);
        AsnDecodeRequest request = new AsnDecodeRequest
        {
            TypeName = "Schema.Outer",
            Data = encoded.Data,
            Options = options
        };

        AsnDecodeResult selected = selective.DecodeSelectedField(
            request,
            ["payload", "flag"]);
        AsnDecodeResult skipped = selective.SkipSelectedField(
            request,
            ["payload", "flag"]);
        AsnRuntimeValue skippedRoot = skipped.Value
            ?? throw new AssertionException("Skipped root was not returned.");
        AsnRuntimeValue skippedPayload = skippedRoot.Fields.Single(
            field => field.Name == "payload").Value
            ?? throw new AssertionException("Skipped payload was not returned.");
        AsnRuntimeFieldValue skippedFlag = skippedPayload.Fields.Single(
            field => field.Name == "flag");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(selected.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(selected.ConsumedCount,
                Is.EqualTo(encoded.Data.Length));
            Assert.That(selected.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Boolean));
            Assert.That(selected.Value?.BooleanValue, Is.True);
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.ConsumedCount,
                Is.EqualTo(encoded.Data.Length));
            Assert.That(skippedFlag.Present, Is.False);
            Assert.That(skippedFlag.Value, Is.Null);
            Assert.That(skippedRoot.Fields.Single(
                field => field.Name == "tail").Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(9)));
        });
    }

    [Test]
    public void SelectsAndSkipsCollectionItemsByIndex()
    {
        AsnTypeDescriptor integer = Type(
            AsnTypeKind.Integer,
            "Item");
        AsnTypeDescriptor collection = Type(
            AsnTypeKind.SequenceOf,
            "Numbers");
        collection.ElementType = integer;
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        ConfiguredCodec codec = Configured(collection, options);
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema.Numbers",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = [Integer(1), Integer(2), Integer(3)]
            }
        });
        SelectiveDecoderImplementation selective =
            new SelectiveDecoderImplementation(codec);
        AsnDecodeRequest request = new AsnDecodeRequest
        {
            TypeName = "Schema.Numbers",
            Data = encoded.Data,
            Options = options
        };

        AsnDecodeResult selected = selective.DecodeSelectedField(
            request,
            ["[1]"]);
        AsnDecodeResult skipped = selective.SkipSelectedField(
            request,
            ["1"]);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(selected.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.Value?.Items, Has.Length.EqualTo(2));
            Assert.That(skipped.Value?.Items[0].IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(skipped.Value?.Items[1].IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
        });
    }

    [Test]
    public void PreservesOpaqueExtensionDataInsideSelectedValue()
    {
        AsnTypeDescriptor payload = ExtensiblePayload();
        AsnFieldDescriptor payloadField = Field(
            "payload",
            payload,
            0);
        AsnTypeDescriptor outer = Type(AsnTypeKind.Sequence, "Outer");
        outer.Fields = [payloadField];
        outer.RootFields = [payloadField];
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        options.UnknownDataPolicy = AsnUnknownDataPolicy.Preserve;
        ConfiguredCodec codec = Configured(outer, options);
        SelectiveDecoderImplementation selective =
            new SelectiveDecoderImplementation(codec);
        byte[] input =
        [
            0x30, 0x0c,
            0x30, 0x0a,
            0x80, 0x01, 0x01,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        ];

        AsnDecodeResult selected = selective.DecodeSelectedField(
            new AsnDecodeRequest
            {
                TypeName = "Schema.Outer",
                Data = input,
                Options = options
            },
            ["payload"]);
        AsnUnknownExtension unknown = selected.Value?.UnknownExtensions.Single()
            ?? throw new AssertionException("Unknown extension was not preserved.");

        Assert.Multiple(() =>
        {
            Assert.That(selected.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(unknown.Index, Is.EqualTo(0));
            Assert.That(unknown.HasBinaryData, Is.True);
            Assert.That(unknown.BinaryData,
                Is.EqualTo(new byte[] { 0x89, 0x81, 0x01, 0x7f }));
            Assert.That(unknown.EncodingRules,
                Is.EqualTo(AsnEncodingRules.Ber));
        });
    }

    [Test]
    public void RejectsTrailingInputInvalidPathsAndNoncanonicalData()
    {
        AsnTypeDescriptor flag = Type(AsnTypeKind.Boolean, "Flag");
        AsnCodecOptions berOptions = Options(AsnEncodingRules.Ber);
        SelectiveDecoderImplementation ber =
            new SelectiveDecoderImplementation(
                Configured(flag, berOptions));
        AsnDecodeResult trailing = ber.DecodeSelectedField(
            new AsnDecodeRequest
            {
                TypeName = "Schema.Flag",
                Data = [0x01, 0x01, 0xff, 0x00],
                Options = berOptions
            },
            []);
        AsnDecodeResult missing = ber.DecodeSelectedField(
            new AsnDecodeRequest
            {
                TypeName = "Schema.Flag",
                Data = [0x01, 0x01, 0xff],
                Options = berOptions
            },
            ["missing"]);

        AsnCodecOptions derOptions = Options(AsnEncodingRules.Der);
        SelectiveDecoderImplementation der =
            new SelectiveDecoderImplementation(
                Configured(flag, derOptions));
        AsnDecodeResult noncanonical = der.DecodeSelectedField(
            new AsnDecodeRequest
            {
                TypeName = "Schema.Flag",
                Data = [0x01, 0x01, 0x01],
                Options = derOptions
            },
            []);

        Assert.Multiple(() =>
        {
            Assert.That(trailing.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(trailing.ConsumedCount, Is.EqualTo(3));
            Assert.That(trailing.Diagnostics.Single().Code,
                Is.EqualTo("ASNSELECT0002"));
            Assert.That(missing.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(missing.Diagnostics.Single().Code,
                Is.EqualTo("ASNSELECT0001"));
            Assert.That(missing.Diagnostics.Single().ValuePath,
                Is.EqualTo("$.missing"));
            Assert.That(noncanonical.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(noncanonical.Diagnostics.Single().Code,
                Is.Not.EqualTo("ASNSELECT0001"));
            Assert.That(noncanonical.Diagnostics.Single().Code,
                Is.Not.EqualTo("ASNSELECT0002"));
        });
    }

    [Test]
    public void SelectsAndSkipsEcnRootValues()
    {
        AsnCompilationResult compiled = new EcnCompilerImplementation()
            .CompileEcn(
                new AsnSourceUnit
                {
                    Name = "selective.edm",
                    Text = "Selective ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                        + "END"
                },
                [IntegerModule()]);
        AsnReflectionCatalog catalog = compiled.ReflectionCatalog
            ?? throw new AssertionException("ECN reflection was not built.");
        AsnCodecOptions options = Options(AsnEncodingRules.Ecn);
        ConfiguredCodec codec = (ConfiguredCodec)new CodecProviderImplementation()
            .CreateCodec(catalog, options);
        SelectiveDecoderImplementation selective =
            new SelectiveDecoderImplementation(codec);
        AsnDecodeRequest request = new AsnDecodeRequest
        {
            TypeName = "Schema.scalar",
            Data = [42],
            Options = options
        };

        AsnDecodeResult selected = selective.DecodeSelectedField(request, []);
        AsnDecodeResult skipped = selective.SkipSelectedField(request, []);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Success, Is.True);
            Assert.That(selected.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(selected.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.Value, Is.Null);
            Assert.That(skipped.ConsumedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ValidatesRequiredOperationInputs()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        ConfiguredCodec codec = Configured(
            Type(AsnTypeKind.Boolean, "Flag"),
            options);
        SelectiveDecoderImplementation selective =
            new SelectiveDecoderImplementation(codec);

        Assert.Multiple(() =>
        {
            Assert.That(() => new SelectiveDecoderImplementation(null!),
                Throws.ArgumentNullException);
            Assert.That(() => selective.DecodeSelectedField(null!, []),
                Throws.ArgumentNullException);
            Assert.That(() => selective.DecodeSelectedField(
                new AsnDecodeRequest
                {
                    TypeName = "Schema.Flag",
                    Data = null!
                },
                []), Throws.ArgumentException);
            Assert.That(() => selective.DecodeSelectedField(
                new AsnDecodeRequest
                {
                    TypeName = "Schema.Flag",
                    Data = [1],
                    Offset = 2
                },
                []), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => selective.DecodeSelectedField(
                new AsnDecodeRequest
                {
                    TypeName = "Schema.Flag",
                    Data = [0x01, 0x01, 0xff]
                },
                null!), Throws.ArgumentNullException);
        });
    }

    private static ConfiguredCodec Configured(
        AsnTypeDescriptor type,
        AsnCodecOptions options)
    {
        AsnReflectionCatalog catalog = new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment." + type.Name,
                    Name = type.Name,
                    QualifiedName = "Schema." + type.Name,
                    GeneratedName = type.GeneratedName,
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
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

    private static AsnTypeDescriptor OuterType()
    {
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor count = Field(
            "count",
            Type(AsnTypeKind.Integer, "Count"),
            1);
        AsnTypeDescriptor payload = Type(
            AsnTypeKind.Sequence,
            "Payload");
        payload.Fields = [flag, count];
        payload.RootFields = [flag, count];
        AsnFieldDescriptor payloadField = Field("payload", payload, 0);
        AsnFieldDescriptor tail = Field(
            "tail",
            Type(AsnTypeKind.Integer, "Tail"),
            1);
        AsnTypeDescriptor outer = Type(AsnTypeKind.Sequence, "Outer");
        outer.Fields = [payloadField, tail];
        outer.RootFields = [payloadField, tail];
        return outer;
    }

    private static AsnTypeDescriptor ExtensiblePayload()
    {
        AsnFieldDescriptor id = ContextField("id", 0);
        AsnFieldDescriptor later = ContextField("later", 2);
        later.Extension = true;
        AsnTypeDescriptor payload = Type(
            AsnTypeKind.Sequence,
            "Payload");
        payload.Extensible = true;
        payload.Fields = [id, later];
        payload.RootFields = [id];
        payload.ExtensionFields = [later];
        return payload;
    }

    private static AsnFieldDescriptor ContextField(string name, int tag)
    {
        AsnFieldDescriptor field = Field(
            name,
            Type(AsnTypeKind.Integer, name),
            tag);
        field.Tags =
        [
            new AsnTagDescriptor
            {
                Class = AsnTagClass.ContextSpecific,
                Mode = AsnTaggingMode.Implicit,
                Number = tag
            }
        ];
        return field;
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int index)
    {
        return new AsnFieldDescriptor
        {
            StableId = "field." + name,
            Name = name,
            GeneratedName = name,
            Index = index,
            Type = type
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

    private static AsnRuntimeValue OuterValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("payload", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("flag", Boolean(true)),
                        Present("count", Integer(4))
                    ]
                }),
                Present("tail", Integer(9))
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
}
