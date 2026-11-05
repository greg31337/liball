using asn1;

namespace asn1.Tests;

public class BerSchemaDecoderTests
{
    [Test]
    public void RequestDecodingResolvesTypesAndAccountsForTrailingData()
    {
        AsnTypeDescriptor integer = IntegerType("type.number", "Number");
        AsnReflectionCatalog catalog = CatalogWithType(
            "Module.Number",
            integer);
        BerSchemaDecoder decoder = new BerSchemaDecoder(catalog);

        AsnDecodeResult result = decoder.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Number",
            Data = [0x02, 0x01, 0x2a, 0x05, 0x00],
            Options = Options(AsnUnknownDataPolicy.Reject)
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.ConsumedCount, Is.EqualTo(3));
            Assert.That(result.Value?.Kind, Is.EqualTo(AsnRuntimeValueKind.Integer));
            Assert.That(result.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ExplicitTypeTagsAreUnwrappedBeforeValueDecoding()
    {
        AsnTypeDescriptor integer = IntegerType("type.explicit", "Explicit");
        integer.Tags =
        [
            new AsnTagDescriptor
            {
                Class = AsnTagClass.ContextSpecific,
                Mode = AsnTaggingMode.Explicit,
                Number = 5
            }
        ];
        BerTlv wrapped = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = 5
            },
            Constructed = true,
            Children = [UniversalPrimitive(2, [9])]
        };

        BerSchemaDecodeResult result = EmptyDecoder().DecodeTlv(
            integer,
            wrapped,
            Options(AsnUnknownDataPolicy.Reject));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(9)));
        });
    }

    [Test]
    public void SequenceDecodingRestoresImplicitTagsAndValidatesDefaults()
    {
        AsnFieldDescriptor required = Field(
            "id",
            IntegerType("type.id", "Id"),
            0);
        AsnFieldDescriptor optional = Field(
            "note",
            StringType("type.note", "Note"),
            1);
        optional.Optional = true;
        AsnFieldDescriptor defaulted = Field(
            "count",
            IntegerType("type.count", "Count"),
            2);
        defaulted.HasDefaultValue = true;
        defaulted.DefaultValue = IntegerDefault(3);
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields = [required, optional, defaulted]
        };
        BerSchemaDecoder decoder = EmptyDecoder();

        BerSchemaDecodeResult explicitDefault = decoder.DecodeTlv(
            sequence,
            Constructed(16, [ContextPrimitive(0, [5]), ContextPrimitive(2, [3])]),
            Options(AsnUnknownDataPolicy.Reject));
        BerSchemaDecodeResult omittedDefault = decoder.DecodeTlv(
            sequence,
            Constructed(16, [ContextPrimitive(0, [6])]),
            Options(AsnUnknownDataPolicy.Reject));

        AsnRuntimeFieldValue[] explicitFields = explicitDefault.Value?.Fields
            ?? throw new InvalidOperationException();
        AsnRuntimeFieldValue[] omittedFields = omittedDefault.Value?.Fields
            ?? throw new InvalidOperationException();
        Assert.Multiple(() =>
        {
            Assert.That(explicitFields[0].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(5)));
            Assert.That(explicitFields[1].Present, Is.False);
            Assert.That(explicitFields[2].Present, Is.True);
            Assert.That(explicitFields[2].PresentDefault, Is.True);
            Assert.That(omittedFields[2].Present, Is.False);
            Assert.That(omittedFields[2].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
        });
    }

    [Test]
    public void SequencePreservesUnknownExtensionsBetweenKnownAdditions()
    {
        AsnFieldDescriptor first = Field(
            "first",
            IntegerType("type.first", "First"),
            1);
        first.Extension = true;
        AsnFieldDescriptor second = Field(
            "second",
            IntegerType("type.second", "Second"),
            2);
        second.Extension = true;
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Extensible = true,
            Fields = [first, second]
        };
        BerTlv unknown = ContextPrimitive(9, [0x7f]);
        unknown.EncodedData = [0x89, 0x01, 0x7f];

        BerSchemaDecodeResult result = EmptyDecoder().DecodeTlv(
            sequence,
            Constructed(16, [unknown, ContextPrimitive(2, [8])]),
            Options(AsnUnknownDataPolicy.Preserve));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value?.Fields[0].Present, Is.False);
            Assert.That(result.Value?.Fields[1].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(8)));
            Assert.That(result.Value?.UnknownExtensions.Single().Index,
                Is.EqualTo(0));
            Assert.That(result.Value?.UnknownExtensions.Single().BinaryData,
                Is.EqualTo(new byte[] { 0x89, 0x01, 0x7f }));
        });
    }

    [Test]
    public void SetDispatchesByTagAndReportsAmbiguousFields()
    {
        AsnFieldDescriptor id = Field(
            "id",
            IntegerType("type.id", "Id"),
            0);
        AsnFieldDescriptor text = Field(
            "text",
            StringType("type.text", "Text"),
            1);
        AsnTypeDescriptor set = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Fields = [id, text]
        };
        BerSchemaDecoder decoder = EmptyDecoder();
        BerSchemaDecodeResult decoded = decoder.DecodeTlv(
            set,
            Constructed(17,
            [
                ContextPrimitive(1, System.Text.Encoding.UTF8.GetBytes("ok")),
                ContextPrimitive(0, [7])
            ]),
            Options(AsnUnknownDataPolicy.Reject));

        AsnFieldDescriptor duplicateTag = Field(
            "other",
            IntegerType("type.other", "Other"),
            0);
        set.Fields = [id, duplicateTag];
        BerSchemaDecodeResult ambiguous = decoder.DecodeTlv(
            set,
            Constructed(17, [ContextPrimitive(0, [1])]),
            Options(AsnUnknownDataPolicy.Reject));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Value?.Fields[0].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(decoded.Value?.Fields[1].Value?.TextValue, Is.EqualTo("ok"));
            Assert.That(ambiguous.Success, Is.False);
            Assert.That(ambiguous.Diagnostics.Single().Code,
                Is.EqualTo("ASND0102"));
        });
    }

    [Test]
    public void ChoiceDispatchesByTagAndReportsMissingOrAmbiguousAlternatives()
    {
        AsnFieldDescriptor integer = Field(
            "number",
            IntegerType("type.number", "Number"),
            0);
        AsnFieldDescriptor text = Field(
            "text",
            StringType("type.text", "Text"),
            1);
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Fields = [integer, text]
        };
        BerSchemaDecoder decoder = EmptyDecoder();

        BerSchemaDecodeResult selected = decoder.DecodeTlv(
            choice,
            ContextPrimitive(1, System.Text.Encoding.UTF8.GetBytes("chosen")),
            Options(AsnUnknownDataPolicy.Reject));
        BerSchemaDecodeResult missing = decoder.DecodeTlv(
            choice,
            ContextPrimitive(9, [0]),
            Options(AsnUnknownDataPolicy.Reject));
        choice.Fields = [integer, Field(
            "other",
            IntegerType("type.other", "Other"),
            0)];
        BerSchemaDecodeResult ambiguous = decoder.DecodeTlv(
            choice,
            ContextPrimitive(0, [1]),
            Options(AsnUnknownDataPolicy.Reject));

        Assert.Multiple(() =>
        {
            Assert.That(selected.Value?.Fields.Single().Name, Is.EqualTo("text"));
            Assert.That(selected.Value?.Fields.Single().Value?.TextValue,
                Is.EqualTo("chosen"));
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("ASND0095"));
            Assert.That(ambiguous.Diagnostics.Single().Code,
                Is.EqualTo("ASND0102"));
        });
    }

    [Test]
    public void CollectionsDecodeEveryElementAndRequiredFieldsAreEnforced()
    {
        AsnTypeDescriptor list = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = IntegerType("type.item", "Item")
        };
        BerSchemaDecoder decoder = EmptyDecoder();
        BerSchemaDecodeResult collection = decoder.DecodeTlv(
            list,
            Constructed(16, [UniversalPrimitive(2, [1]), UniversalPrimitive(2, [2])]),
            Options(AsnUnknownDataPolicy.Reject));
        AsnTypeDescriptor requiredSequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                Field("required", IntegerType("type.required", "Required"), 0)
            ]
        };
        BerSchemaDecodeResult missing = decoder.DecodeTlv(
            requiredSequence,
            Constructed(16, []),
            Options(AsnUnknownDataPolicy.Reject));

        Assert.Multiple(() =>
        {
            Assert.That(collection.Value?.Items.Select(item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 2 }));
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("ASND0098"));
        });
    }

    [Test]
    public void OpenTypeTablesResolveByTagAndApplyUnknownPolicies()
    {
        AsnTypeDescriptor integer = IntegerType("type.payload", "Payload");
        AsnTypeDescriptor open = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OpenType,
            ObjectTables =
            [
                new AsnObjectTableDescriptor
                {
                    Entries =
                    [
                        new AsnObjectTableEntryDescriptor
                        {
                            Key = IntegerDefault(7),
                            Type = integer
                        }
                    ]
                }
            ]
        };
        BerTlv payload = UniversalPrimitive(2, [0x2a]);
        payload.EncodedData = [0x02, 0x01, 0x2a];
        BerSchemaDecoder decoder = EmptyDecoder();

        BerSchemaDecodeResult resolved = decoder.DecodeTlv(
            open,
            payload,
            Options(AsnUnknownDataPolicy.Reject));
        BerSchemaDecodeResult rejected = decoder.DecodeTlv(
            open,
            UniversalPrimitive(12, [0x41]),
            Options(AsnUnknownDataPolicy.Reject));
        BerSchemaDecodeResult preserved = decoder.DecodeTlv(
            open,
            UniversalPrimitive(12, [0x41]),
            Options(AsnUnknownDataPolicy.Preserve));
        BerSchemaDecodeResult skipped = decoder.DecodeTlv(
            open,
            UniversalPrimitive(12, [0x41]),
            Options(AsnUnknownDataPolicy.Skip));

        AsnOpenTypeValue resolvedOpen = resolved.Value?.OpenTypeValue
            ?? throw new InvalidOperationException();
        AsnOpenTypeValue preservedOpen = preserved.Value?.OpenTypeValue
            ?? throw new InvalidOperationException();
        AsnOpenTypeValue skippedOpen = skipped.Value?.OpenTypeValue
            ?? throw new InvalidOperationException();
        Assert.Multiple(() =>
        {
            Assert.That(resolvedOpen.Resolved, Is.True);
            Assert.That(resolvedOpen.ResolvedTypeId, Is.EqualTo("type.payload"));
            Assert.That(resolvedOpen.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(resolvedOpen.DispatchKey?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(resolvedOpen.OriginalData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x2a }));
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("ASND0097"));
            Assert.That(preservedOpen.Unresolved, Is.True);
            Assert.That(preservedOpen.HasOriginalData, Is.True);
            Assert.That(preservedOpen.OriginalData,
                Is.EqualTo(new byte[] { 0x0c, 0x01, 0x41 }));
            Assert.That(skippedOpen.Unresolved, Is.True);
            Assert.That(skippedOpen.HasOriginalData, Is.False);
        });
    }

    private static BerSchemaDecoder EmptyDecoder()
    {
        return new BerSchemaDecoder(new AsnReflectionCatalog());
    }

    private static AsnCodecOptions Options(AsnUnknownDataPolicy policy)
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Ber,
            UnknownDataPolicy = policy
        };
    }

    private static AsnReflectionCatalog CatalogWithType(
        string qualifiedName,
        AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Name = type.Name,
                    QualifiedName = qualifiedName,
                    Type = type
                }
            ]
        };
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

    private static AsnTypeDescriptor StringType(string stableId, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = stableId,
            Name = name,
            Kind = AsnTypeKind.Utf8String
        };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int contextTag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Implicit,
                    Number = contextTag
                }
            ]
        };
    }

    private static AsnValueDescriptor IntegerDefault(int value)
    {
        return new AsnValueDescriptor
        {
            Kind = AsnValueKind.Integer,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
        };
    }

    private static BerTlv Constructed(int tag, BerTlv[] children)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tag
            },
            Constructed = true,
            Children = children
        };
    }

    private static BerTlv ContextPrimitive(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = tag
            },
            Content = content
        };
    }

    private static BerTlv UniversalPrimitive(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tag
            },
            Content = content
        };
    }
}
