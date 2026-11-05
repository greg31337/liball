using asn1;

namespace asn1.Tests;

public class RandomizedGrammarRoundTripTests
{
    private const int SchemaSeed = 236031;
    private const int ValueSeed = 236079;

    [Test]
    public void FixedSeedSchemasSurviveParsePrintAndReparse()
    {
        System.Random grammar = new System.Random(SchemaSeed);

        int index = 0;
        while (index < 20)
        {
            string source = GenerateSchema(grammar, index);
            AsnCompilationResult parsed = Compile(source, index);
            string printed = new AsnModuleTextPrinter().PrintModules(
                parsed.Modules);
            AsnCompilationResult reparsed = Compile(printed, index + 1000);

            Assert.Multiple(() =>
            {
                Assert.That(parsed.ParserResults.Single().Complete, Is.True,
                    Context(index, source, parsed.Diagnostics));
                Assert.That(reparsed.ParserResults.Single().Complete, Is.True,
                    Context(index, printed, reparsed.Diagnostics));
                Assert.That(CatalogSignature(reparsed.ReflectionCatalog),
                    Is.EqualTo(CatalogSignature(parsed.ReflectionCatalog)),
                    Context(index, printed, reparsed.Diagnostics));
                Assert.That(new AsnModuleTextPrinter().PrintModules(
                        reparsed.Modules),
                    Is.EqualTo(printed),
                    "seed=" + SchemaSeed + ", case=" + index);
            });
            index = index + 1;
        }
    }

    [Test]
    public void FixedSeedValuesRoundTripAcrossEveryApplicableRule()
    {
        System.Random grammar = new System.Random(SchemaSeed);
        System.Random values = new System.Random(ValueSeed);
        AsnEncodingRules[] rules = ApplicableRules();

        int schemaIndex = 0;
        while (schemaIndex < 12)
        {
            string source = GenerateSchema(grammar, schemaIndex);
            AsnCompilationResult compiled = Compile(source, schemaIndex);
            AsnReflectionCatalog catalog = RequireCatalog(compiled);
            AsnTypeDescriptor type = RequirePacketType(catalog);
            AsnRuntimeValue expected = GenerateValue(type, values);

            int ruleIndex = 0;
            while (ruleIndex < rules.Length)
            {
                AsnEncodingRules rule = rules[ruleIndex];
                IAsnCodec codec = Codec(catalog, rule);
                AsnEncodeResult encoded = Encode(codec, expected);
                AsnDecodeResult decoded = Decode(codec, encoded.Data);

                Assert.Multiple(() =>
                {
                    Assert.That(encoded.Success, Is.True,
                        RuntimeContext(schemaIndex, rule, encoded.Diagnostics));
                    Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done),
                        RuntimeContext(schemaIndex, rule, decoded.Diagnostics));
                    Assert.That(decoded.ConsumedCount,
                        Is.EqualTo(encoded.Data.Length),
                        "seed=" + ValueSeed + ", schema=" + schemaIndex
                            + ", rule=" + rule);
                    Assert.That(RuntimeValuesEqual(
                            expected,
                            decoded.Value,
                            type),
                        Is.True,
                        "seed=" + ValueSeed + ", schema=" + schemaIndex
                            + ", rule=" + rule);
                });
                ruleIndex = ruleIndex + 1;
            }
            schemaIndex = schemaIndex + 1;
        }
    }

    [Test]
    public void CanonicalRulesAreIdempotentForFixedSeedValues()
    {
        System.Random grammar = new System.Random(SchemaSeed);
        System.Random values = new System.Random(ValueSeed);
        AsnEncodingRules[] rules = CanonicalRules();

        int schemaIndex = 0;
        while (schemaIndex < 12)
        {
            AsnCompilationResult compiled = Compile(
                GenerateSchema(grammar, schemaIndex),
                schemaIndex);
            AsnReflectionCatalog catalog = RequireCatalog(compiled);
            AsnTypeDescriptor type = RequirePacketType(catalog);
            AsnRuntimeValue expected = GenerateValue(type, values);

            int ruleIndex = 0;
            while (ruleIndex < rules.Length)
            {
                AsnEncodingRules rule = rules[ruleIndex];
                IAsnCodec codec = Codec(catalog, rule);
                AsnEncodeResult first = Encode(codec, expected);
                AsnDecodeResult decoded = Decode(codec, first.Data);
                AsnEncodeResult second = Encode(
                    codec,
                    decoded.Value ?? throw new AssertionException(
                        RuntimeContext(schemaIndex, rule, decoded.Diagnostics)));
                AsnCanonicalityResult canonical = codec.CheckCanonicality(
                    new AsnDecodeRequest
                    {
                        TypeName = PacketName(catalog),
                        Data = first.Data
                    });

                Assert.Multiple(() =>
                {
                    Assert.That(first.Success, Is.True,
                        RuntimeContext(schemaIndex, rule, first.Diagnostics));
                    Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done),
                        RuntimeContext(schemaIndex, rule, decoded.Diagnostics));
                    Assert.That(second.Success, Is.True,
                        RuntimeContext(schemaIndex, rule, second.Diagnostics));
                    Assert.That(second.Data, Is.EqualTo(first.Data),
                        "seed=" + ValueSeed + ", schema=" + schemaIndex
                            + ", rule=" + rule);
                    Assert.That(canonical.Status,
                        Is.EqualTo(AsnCanonicalityStatus.Canonical),
                        RuntimeContext(schemaIndex, rule, canonical.Diagnostics));
                });
                ruleIndex = ruleIndex + 1;
            }
            schemaIndex = schemaIndex + 1;
        }
    }

    [Test]
    public void DeterministicMalformedMutationsNeverReproduceTheSourceValue()
    {
        System.Random grammar = new System.Random(SchemaSeed);
        System.Random values = new System.Random(ValueSeed);
        AsnEncodingRules[] rules = ApplicableRules();

        int schemaIndex = 0;
        while (schemaIndex < 8)
        {
            AsnCompilationResult compiled = Compile(
                GenerateSchema(grammar, schemaIndex),
                schemaIndex);
            AsnReflectionCatalog catalog = RequireCatalog(compiled);
            AsnTypeDescriptor type = RequirePacketType(catalog);
            AsnRuntimeValue expected = GenerateValue(type, values);

            int ruleIndex = 0;
            while (ruleIndex < rules.Length)
            {
                AsnEncodingRules rule = rules[ruleIndex];
                IAsnCodec codec = Codec(catalog, rule);
                AsnEncodeResult encoded = Encode(codec, expected);
                Assert.That(encoded.Success, Is.True,
                    RuntimeContext(schemaIndex, rule, encoded.Diagnostics));
                byte[][] mutations = Mutations(encoded.Data);

                int mutationIndex = 0;
                while (mutationIndex < mutations.Length)
                {
                    AsnDecodeResult decoded = new AsnDecodeResult();
                    Assert.DoesNotThrow(() =>
                    {
                        decoded = Decode(codec, mutations[mutationIndex]);
                    }, "seed=" + ValueSeed + ", schema=" + schemaIndex
                        + ", rule=" + rule + ", mutation=" + mutationIndex);
                    bool reproduced = decoded.Status == AsnDecodeStatus.Done
                        && decoded.ConsumedCount == mutations[mutationIndex].Length
                        && RuntimeValuesEqual(expected, decoded.Value, type);
                    Assert.That(reproduced, Is.False,
                        "seed=" + ValueSeed + ", schema=" + schemaIndex
                            + ", rule=" + rule + ", mutation=" + mutationIndex);
                    mutationIndex = mutationIndex + 1;
                }
                ruleIndex = ruleIndex + 1;
            }
            schemaIndex = schemaIndex + 1;
        }
    }

    private static string GenerateSchema(System.Random random, int index)
    {
        string[] grammar =
        [
            "BOOLEAN",
            "INTEGER",
            "BIT STRING",
            "OCTET STRING",
            "UTF8String",
            "PrintableString",
            "SEQUENCE OF BOOLEAN",
            "SEQUENCE OF INTEGER"
        ];
        int count = random.Next(2, 7);
        System.Text.StringBuilder source = new System.Text.StringBuilder();
        source.Append("Randomized");
        source.Append(index);
        source.AppendLine(" DEFINITIONS AUTOMATIC TAGS ::= BEGIN");
        source.AppendLine("Packet ::= SEQUENCE {");
        int fieldIndex = 0;
        while (fieldIndex < count)
        {
            source.Append("  field");
            source.Append(fieldIndex);
            source.Append(' ');
            source.Append(grammar[random.Next(grammar.Length)]);
            if (fieldIndex > 0 && random.Next(3) == 0)
            {
                source.Append(" OPTIONAL");
            }
            source.AppendLine(fieldIndex + 1 == count ? "" : ",");
            fieldIndex = fieldIndex + 1;
        }
        source.AppendLine("}");
        source.AppendLine("END");
        return source.ToString();
    }

    private static AsnCompilationResult Compile(string source, int index)
    {
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    new AsnSourceUnit
                    {
                        Name = "randomized-" + index + ".asn",
                        Text = source
                    }
                ],
                CompilerOptions = new AsnCompilerOptions
                {
                    EncodingRules = AsnEncodingRules.Ber
                }
            });
        Assert.That(result.Success, Is.True,
            Context(index, source, result.Diagnostics));
        return result;
    }

    private static AsnReflectionCatalog RequireCatalog(
        AsnCompilationResult result)
    {
        return result.ReflectionCatalog
            ?? throw new AssertionException("The reflection catalog is missing.");
    }

    private static AsnTypeDescriptor RequirePacketType(
        AsnReflectionCatalog catalog)
    {
        return catalog.Assignments.Single(item => item.Name == "Packet").Type
            ?? throw new AssertionException("Packet has no reflected type.");
    }

    private static string PacketName(AsnReflectionCatalog catalog)
    {
        return catalog.Assignments.Single(item => item.Name == "Packet")
            .QualifiedName;
    }

    private static IAsnCodec Codec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules)
    {
        return new CodecProviderImplementation().CreateCodec(
            catalog,
            new AsnCodecOptions
            {
                EncodingRules = rules,
                UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
                ValidateValues = true
            });
    }

    private static AsnEncodeResult Encode(
        IAsnCodec codec,
        AsnRuntimeValue value)
    {
        ConfiguredCodec configured = (ConfiguredCodec)codec;
        return codec.Encode(new AsnEncodeRequest
        {
            TypeName = PacketName(configured.Catalog),
            Value = value
        });
    }

    private static AsnDecodeResult Decode(IAsnCodec codec, byte[] data)
    {
        ConfiguredCodec configured = (ConfiguredCodec)codec;
        return codec.Decode(new AsnDecodeRequest
        {
            TypeName = PacketName(configured.Catalog),
            Data = data
        });
    }

    private static AsnRuntimeValue GenerateValue(
        AsnTypeDescriptor type,
        System.Random random)
    {
        if (type.Kind == AsnTypeKind.Boolean)
        {
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = random.Next(2) == 1
            };
        }
        if (type.Kind == AsnTypeKind.Integer)
        {
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = random.Next(-1000, 1001)
            };
        }
        if (type.Kind == AsnTypeKind.BitString)
        {
            int bitLength = random.Next(0, 33);
            byte[] bytes = RandomBytes(random, (bitLength + 7) / 8);
            if (bitLength % 8 != 0 && bytes.Length > 0)
            {
                bytes[bytes.Length - 1] = (byte)(bytes[bytes.Length - 1]
                    & 255 << (8 - bitLength % 8));
            }
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = bytes,
                    BitLength = bitLength
                }
            };
        }
        if (type.Kind == AsnTypeKind.OctetString)
        {
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = RandomBytes(random, random.Next(0, 9))
            };
        }
        if (IsTextKind(type.Kind))
        {
            string alphabet = type.Kind == AsnTypeKind.PrintableString
                ? "ABCDE01234 "
                : "Abc09-_";
            int length = random.Next(0, 9);
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            int index = 0;
            while (index < length)
            {
                text.Append(alphabet[random.Next(alphabet.Length)]);
                index = index + 1;
            }
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = text.ToString()
            };
        }
        if (type.Kind == AsnTypeKind.SequenceOf)
        {
            AsnTypeDescriptor element = type.ElementType
                ?? throw new AssertionException("Collection element is missing.");
            int count = random.Next(0, 5);
            AsnRuntimeValue[] items = new AsnRuntimeValue[count];
            int index = 0;
            while (index < count)
            {
                items[index] = GenerateValue(element, random);
                index = index + 1;
            }
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = items
            };
        }
        if (type.Kind == AsnTypeKind.Sequence)
        {
            AsnRuntimeFieldValue[] fields =
                new AsnRuntimeFieldValue[type.Fields.Length];
            int index = 0;
            while (index < fields.Length)
            {
                AsnFieldDescriptor descriptor = type.Fields[index];
                bool present = !descriptor.Optional || random.Next(2) == 1;
                fields[index] = new AsnRuntimeFieldValue
                {
                    Name = descriptor.Name,
                    Present = present,
                    Value = present
                        ? GenerateValue(
                            descriptor.Type ?? throw new AssertionException(
                                "Field type is missing."),
                            random)
                        : null
                };
                index = index + 1;
            }
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = fields
            };
        }

        throw new AssertionException("Unsupported generated type " + type.Kind);
    }

    private static bool RuntimeValuesEqual(
        AsnRuntimeValue expected,
        AsnRuntimeValue? actual,
        AsnTypeDescriptor type)
    {
        if (actual == null || expected.Kind != actual.Kind)
        {
            return false;
        }
        if (type.Kind == AsnTypeKind.Boolean)
        {
            return expected.HasBooleanValue == actual.HasBooleanValue
                && expected.BooleanValue == actual.BooleanValue;
        }
        if (type.Kind == AsnTypeKind.Integer)
        {
            return expected.HasIntegerValue == actual.HasIntegerValue
                && expected.IntegerValue == actual.IntegerValue;
        }
        if (type.Kind == AsnTypeKind.BitString)
        {
            return expected.BitStringValue != null
                && actual.BitStringValue != null
                && expected.BitStringValue.BitLength
                    == actual.BitStringValue.BitLength
                && expected.BitStringValue.Bytes.SequenceEqual(
                    actual.BitStringValue.Bytes);
        }
        if (type.Kind == AsnTypeKind.OctetString)
        {
            return expected.Octets.SequenceEqual(actual.Octets);
        }
        if (IsTextKind(type.Kind))
        {
            return expected.TextValue == actual.TextValue;
        }
        if (type.Kind == AsnTypeKind.SequenceOf)
        {
            if (expected.Items.Length != actual.Items.Length
                || type.ElementType == null)
            {
                return false;
            }
            int index = 0;
            while (index < expected.Items.Length)
            {
                if (!RuntimeValuesEqual(
                    expected.Items[index],
                    actual.Items[index],
                    type.ElementType))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        if (type.Kind == AsnTypeKind.Sequence)
        {
            int index = 0;
            while (index < type.Fields.Length)
            {
                AsnFieldDescriptor descriptor = type.Fields[index];
                AsnRuntimeFieldValue? left = FindField(
                    expected,
                    descriptor.Name);
                AsnRuntimeFieldValue? right = FindField(
                    actual,
                    descriptor.Name);
                bool leftPresent = left != null && left.Present;
                bool rightPresent = right != null && right.Present;
                if (leftPresent != rightPresent)
                {
                    return false;
                }
                if (leftPresent && (left?.Value == null || right?.Value == null
                    || descriptor.Type == null
                    || !RuntimeValuesEqual(
                        left.Value,
                        right.Value,
                        descriptor.Type)))
                {
                    return false;
                }
                index = index + 1;
            }
            return true;
        }
        return false;
    }

    private static AsnRuntimeFieldValue? FindField(
        AsnRuntimeValue value,
        string name)
    {
        return value.Fields.FirstOrDefault(item => item.Name == name);
    }

    private static byte[] RandomBytes(System.Random random, int count)
    {
        byte[] result = new byte[count];
        random.NextBytes(result);
        return result;
    }

    private static bool IsTextKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.PrintableString;
    }

    private static byte[][] Mutations(byte[] data)
    {
        byte[] truncated = new byte[data.Length - 1];
        System.Buffer.BlockCopy(data, 0, truncated, 0, truncated.Length);
        byte[] appended = new byte[data.Length + 1];
        System.Buffer.BlockCopy(data, 0, appended, 0, data.Length);
        appended[appended.Length - 1] = 0xff;
        byte[] corrupted = (byte[])data.Clone();
        corrupted[0] = (byte)(corrupted[0] ^ 0xff);
        return [truncated, appended, corrupted];
    }

    private static string CatalogSignature(AsnReflectionCatalog? catalog)
    {
        if (catalog == null)
        {
            return "missing";
        }
        return string.Join("|", catalog.Assignments.Select(item =>
            item.Name + "=" + TypeSignature(item.Type)));
    }

    private static string TypeSignature(AsnTypeDescriptor? type)
    {
        if (type == null)
        {
            return "null";
        }
        if (type.Kind == AsnTypeKind.SequenceOf)
        {
            return "SequenceOf(" + TypeSignature(type.ElementType) + ")";
        }
        if (type.Kind == AsnTypeKind.Sequence)
        {
            return "Sequence(" + string.Join(",", type.Fields.Select(field =>
                field.Name + ":" + TypeSignature(field.Type)
                    + (field.Optional ? "?" : ""))) + ")";
        }
        return type.Kind.ToString();
    }

    private static AsnEncodingRules[] ApplicableRules()
    {
        return
        [
            AsnEncodingRules.Ber,
            AsnEncodingRules.Der,
            AsnEncodingRules.Cer,
            AsnEncodingRules.Aper,
            AsnEncodingRules.Uper,
            AsnEncodingRules.CanonicalAlignedPer,
            AsnEncodingRules.CanonicalUnalignedPer,
            AsnEncodingRules.Oer,
            AsnEncodingRules.Coer,
            AsnEncodingRules.BasicXer,
            AsnEncodingRules.CanonicalXer,
            AsnEncodingRules.ExtendedXer
        ];
    }

    private static AsnEncodingRules[] CanonicalRules()
    {
        return
        [
            AsnEncodingRules.Der,
            AsnEncodingRules.Cer,
            AsnEncodingRules.CanonicalAlignedPer,
            AsnEncodingRules.CanonicalUnalignedPer,
            AsnEncodingRules.Coer,
            AsnEncodingRules.CanonicalXer
        ];
    }

    private static string Context(
        int index,
        string source,
        AsnDiagnostic[] diagnostics)
    {
        return "seed=" + SchemaSeed + ", case=" + index + "\n"
            + source + "\n" + Format(diagnostics);
    }

    private static string RuntimeContext(
        int index,
        AsnEncodingRules rules,
        AsnDiagnostic[] diagnostics)
    {
        return "schema-seed=" + SchemaSeed + ", value-seed=" + ValueSeed
            + ", case=" + index + ", rule=" + rules + ": "
            + Format(diagnostics);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(
            " | ",
            diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
