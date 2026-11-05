using asn1;

namespace asn1.Tests;

public class ValueAccessTests
{
    [Test]
    public void NormalizeValueOrdersStructuredFieldsAndMaterializesDefaults()
    {
        AsnTypeDescriptor integerType = IntegerType("type.integer", "Counter");
        AsnTypeDescriptor booleanType = BooleanType();
        AsnTypeDescriptor textType = TextType();
        AsnTypeDescriptor recordType = new AsnTypeDescriptor
        {
            StableId = "type.record",
            Name = "Record",
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new AsnFieldDescriptor
                {
                    StableId = "field.count",
                    Name = "count",
                    Index = 0,
                    Type = integerType
                },
                new AsnFieldDescriptor
                {
                    StableId = "field.enabled",
                    Name = "enabled",
                    Index = 1,
                    Type = booleanType,
                    HasDefaultValue = true,
                    DefaultValue = new AsnValueDescriptor
                    {
                        Kind = AsnValueKind.Boolean,
                        Value = new AsnValue
                        {
                            Kind = AsnValueKind.Boolean,
                            TextValue = "FALSE"
                        }
                    }
                },
                new AsnFieldDescriptor
                {
                    StableId = "field.note",
                    Name = "note",
                    Index = 2,
                    Type = textType,
                    Optional = true
                }
            ]
        };
        AsnRuntimeValue input = new AsnRuntimeValue
        {
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "note",
                    Present = true,
                    Value = TextValue("ready")
                },
                new AsnRuntimeFieldValue
                {
                    Name = "count",
                    Present = true,
                    Value = IntegerValue(12)
                }
            ]
        };

        AsnRuntimeValue normalized = new ValueAccess().NormalizeValue(
            input,
            recordType,
            EmptyCatalog());

        Assert.Multiple(() =>
        {
            Assert.That(normalized.Kind, Is.EqualTo(AsnRuntimeValueKind.Sequence));
            Assert.That(normalized.Fields, Has.Length.EqualTo(3));
            Assert.That(normalized.Fields[0].Name, Is.EqualTo("count"));
            Assert.That(normalized.Fields[0].Present, Is.True);
            Assert.That(normalized.Fields[1].Name, Is.EqualTo("enabled"));
            Assert.That(normalized.Fields[1].Present, Is.False);
            Assert.That(normalized.Fields[1].Value?.HasBooleanValue, Is.True);
            Assert.That(normalized.Fields[1].Value?.BooleanValue, Is.False);
            Assert.That(normalized.Fields[2].Name, Is.EqualTo("note"));
            Assert.That(normalized.Fields[2].Present, Is.True);
            Assert.That(normalized.Value?.NamedValues.Select(item => item.Name),
                Is.EqualTo(new string[] { "count", "note" }));
        });
    }

    [Test]
    public void ChoiceConversionRequiresAndPreservesExactlyOneSelection()
    {
        AsnTypeDescriptor choiceType = new AsnTypeDescriptor
        {
            StableId = "type.choice",
            Name = "Result",
            Kind = AsnTypeKind.Choice,
            Fields =
            [
                new AsnFieldDescriptor
                {
                    Name = "number",
                    Type = IntegerType("type.number", "Number")
                },
                new AsnFieldDescriptor
                {
                    Name = "message",
                    Type = TextType()
                }
            ]
        };
        AsnValue schemaValue = new AsnValue
        {
            Kind = AsnValueKind.Choice,
            NamedValues =
            [
                new AsnNamedValue
                {
                    Name = "message",
                    Value = new AsnValue
                    {
                        Kind = AsnValueKind.CharacterString,
                        TextValue = "done"
                    }
                }
            ]
        };
        ValueAccess access = new ValueAccess();

        AsnRuntimeValue runtime = access.FromSchemaValue(
            schemaValue,
            choiceType,
            EmptyCatalog());
        AsnValue convertedBack = access.ToSchemaValue(
            runtime,
            choiceType,
            EmptyCatalog());
        AsnRuntimeValue invalid = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "number",
                    Present = true,
                    Value = IntegerValue(1)
                },
                new AsnRuntimeFieldValue
                {
                    Name = "message",
                    Present = true,
                    Value = TextValue("two")
                }
            ]
        };

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Fields, Has.Length.EqualTo(1));
            Assert.That(runtime.Fields[0].Name, Is.EqualTo("message"));
            Assert.That(runtime.Fields[0].Value?.TextValue, Is.EqualTo("done"));
            Assert.That(convertedBack.NamedValues, Has.Length.EqualTo(1));
            Assert.That(convertedBack.NamedValues[0].Name, Is.EqualTo("message"));
            Assert.That(() => access.NormalizeValue(
                invalid,
                choiceType,
                EmptyCatalog()), Throws.InvalidOperationException);
        });
    }

    [Test]
    public void CollectionConversionUsesTheElementDescriptorForEveryItem()
    {
        AsnTypeDescriptor listType = new AsnTypeDescriptor
        {
            StableId = "type.list",
            Name = "Numbers",
            Kind = AsnTypeKind.SequenceOf,
            ElementType = IntegerType("type.item", "Item")
        };
        AsnValue schemaValue = new AsnValue
        {
            Kind = AsnValueKind.SequenceOf,
            Items =
            [
                SchemaIntegerValue(2),
                SchemaIntegerValue(3)
            ]
        };
        ValueAccess access = new ValueAccess();

        AsnRuntimeValue runtime = access.FromSchemaValue(
            schemaValue,
            listType,
            EmptyCatalog());
        AsnValue convertedBack = access.ToSchemaValue(
            runtime,
            listType,
            EmptyCatalog());
        AsnRuntimeValue wrongItem = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items =
            [
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Boolean,
                    HasBooleanValue = true,
                    BooleanValue = true
                }
            ]
        };
        AsnValue wrongSchemaItem = new AsnValue
        {
            Kind = AsnValueKind.Boolean,
            TextValue = "TRUE"
        };

        Assert.Multiple(() =>
        {
            Assert.That(runtime.Items.Select(item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 2, 3 }));
            Assert.That(convertedBack.Items.Select(item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 2, 3 }));
            Assert.That(() => access.NormalizeValue(
                wrongItem,
                listType,
                EmptyCatalog()), Throws.InvalidOperationException);
            Assert.That(() => access.FromSchemaValue(
                wrongSchemaItem,
                listType.ElementType ?? new AsnTypeDescriptor(),
                EmptyCatalog()), Throws.InvalidOperationException);
        });
    }

    [Test]
    public void OpenTypeConversionResolvesDescriptorIdentityAndPreservesPayload()
    {
        AsnTypeDescriptor payloadType = IntegerType(
            "type.catalog.payload",
            "Payload");
        AsnTypeDescriptor openType = new AsnTypeDescriptor
        {
            StableId = "type.open",
            Name = "Content",
            Kind = AsnTypeKind.OpenType,
            ObjectTables =
            [
                new AsnObjectTableDescriptor
                {
                    Entries =
                    [
                        new AsnObjectTableEntryDescriptor { Type = payloadType }
                    ]
                }
            ]
        };
        AsnRuntimeValue input = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Resolved = true,
                TypeName = "Payload",
                Value = new AsnRuntimeValue
                {
                    HasIntegerValue = true,
                    IntegerValue = 77
                },
                DispatchKey = IntegerValue(5),
                EncodingRules = AsnEncodingRules.Der,
                HasEncodingRules = true,
                HasOriginalData = true,
                OriginalData = [0x02, 0x01, 0x4d]
            }
        };
        ValueAccess access = new ValueAccess();

        AsnRuntimeValue normalized = access.NormalizeValue(
            input,
            openType,
            EmptyCatalog());
        AsnValue schemaValue = access.ToSchemaValue(
            normalized,
            openType,
            EmptyCatalog());
        AsnRuntimeValue roundTrip = access.FromSchemaValue(
            schemaValue,
            openType,
            EmptyCatalog());
        AsnRuntimeValue unresolved = access.NormalizeValue(
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = new AsnOpenTypeValue
                {
                    Unresolved = true,
                    EncodingRules = AsnEncodingRules.Oer,
                    HasEncodingRules = true,
                    HasOriginalData = true,
                    OriginalData = [0xde, 0xad]
                }
            },
            openType,
            EmptyCatalog());
        AsnOpenTypeValue normalizedOpen = normalized.OpenTypeValue
            ?? new AsnOpenTypeValue();
        AsnOpenTypeValue roundTripOpen = roundTrip.OpenTypeValue
            ?? new AsnOpenTypeValue();
        AsnOpenTypeValue unresolvedOpen = unresolved.OpenTypeValue
            ?? new AsnOpenTypeValue();

        Assert.Multiple(() =>
        {
            Assert.That(normalizedOpen.ResolvedTypeId,
                Is.EqualTo("type.catalog.payload"));
            Assert.That(normalizedOpen.TypeName, Is.EqualTo("Payload"));
            Assert.That(normalizedOpen.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Integer));
            Assert.That(normalizedOpen.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(77)));
            Assert.That(roundTripOpen.OriginalData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x4d }));
            Assert.That(roundTripOpen.DispatchKey?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(5)));
            Assert.That(unresolvedOpen.Unresolved, Is.True);
            Assert.That(unresolvedOpen.OriginalData,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
        });
    }

    [Test]
    public void StructuredConversionRejectsMissingAndDuplicateRequiredFields()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new AsnFieldDescriptor
                {
                    Name = "id",
                    Type = IntegerType("type.id", "Id")
                }
            ]
        };
        AsnRuntimeValue missing = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence
        };
        AsnRuntimeValue duplicate = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                new AsnRuntimeFieldValue
                {
                    Name = "id",
                    Present = true,
                    Value = IntegerValue(1)
                },
                new AsnRuntimeFieldValue
                {
                    Name = "id",
                    Present = true,
                    Value = IntegerValue(2)
                }
            ]
        };
        ValueAccess access = new ValueAccess();

        Assert.Multiple(() =>
        {
            Assert.That(() => access.NormalizeValue(
                missing,
                type,
                EmptyCatalog()), Throws.InvalidOperationException);
            Assert.That(() => access.NormalizeValue(
                duplicate,
                type,
                EmptyCatalog()), Throws.InvalidOperationException);
        });
    }

    private static AsnTypeDescriptor IntegerType(string stableId, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = stableId,
            Name = name,
            Kind = AsnTypeKind.Integer
        };
    }

    private static AsnTypeDescriptor BooleanType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Boolean };
    }

    private static AsnTypeDescriptor TextType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String };
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

    private static AsnRuntimeValue TextValue(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnValue SchemaIntegerValue(int value)
    {
        return new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnReflectionCatalog EmptyCatalog()
    {
        return new AsnReflectionCatalog();
    }
}
