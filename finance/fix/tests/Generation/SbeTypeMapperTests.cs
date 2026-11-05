using fix;

namespace fix.Tests;

[TestFixture]
public class SbeTypeMapperTests
{
    [Test]
    public void OrchestraMappingCarriesPhysicalTypesConstantsNullsIdsVersionsAndByteOrder()
    {
        OrchestraRepository repository = CreateOrchestraRepository();
        SbeGenerationOptions options = new SbeGenerationOptions
        {
            Package = "venue.sbe",
            SchemaId = 42,
            Version = 12,
            FirstTemplateId = 100,
            LittleEndian = true,
            EmitSemanticTypes = true,
            Scenario = "base"
        };

        SbeMappingPlan plan = SbeTypeMapper.MapOrchestraRepository(repository, options);
        SbeTypeMapping price = plan.Types.Single(item => item.Name == "Price");
        SbeTypeMapping data = plan.Types.Single(item => item.Name == "data");
        SbeTypeMapping side = plan.Types.Single(item => item.Name == "SideCode");
        SbeTypeMapping component = plan.Types.Single(item => item.Name == "OrderCore");
        SbeFieldMapping priceField = plan.Fields.Single(item => item.Name == "PriceField");
        SbeFieldMapping sideField = plan.Fields.Single(item => item.Name == "Side");
        SbeGroupMapping group = plan.Groups.Single();
        SbeMessageMapping message = plan.Messages.Single();

        Assert.Multiple(() =>
        {
            Assert.That(plan.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False, string.Join(",", plan.Diagnostics.Select(item => item.Code)));
            Assert.That(plan.Package, Is.EqualTo("venue.sbe"));
            Assert.That(plan.SchemaId, Is.EqualTo(42));
            Assert.That(plan.Version, Is.EqualTo(12));
            Assert.That(plan.ByteOrder, Is.EqualTo("bigEndian"));
            Assert.That(price.Kind, Is.EqualTo("composite"));
            Assert.That(price.Members.Select(item => item.TypeName), Is.EqualTo(new[] { "int64", "int8" }));
            Assert.That(price.Members[1].ConstantValue, Is.EqualTo("-4"));
            Assert.That(price.MinimumValue, Is.EqualTo("-100"));
            Assert.That(price.MaximumValue, Is.EqualTo("100"));
            Assert.That(price.NullValue, Is.EqualTo("-9223372036854775808"));
            Assert.That(price.SinceVersion, Is.EqualTo(7));
            Assert.That(price.DeprecatedVersion, Is.EqualTo(9));
            Assert.That(data.VariableLength, Is.True);
            Assert.That(data.PrimitiveType, Is.EqualTo("uint8"));
            Assert.That(side.Kind, Is.EqualTo("enum"));
            Assert.That(side.PrimitiveType, Is.EqualTo("char"));
            Assert.That(side.Values.Select(item => item.Value), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(component.Kind, Is.EqualTo("composite"));
            Assert.That(priceField.Presence, Is.EqualTo("constant"));
            Assert.That(priceField.ConstantValue, Is.EqualTo("123.45"));
            Assert.That(sideField.SourceId, Is.EqualTo(54));
            Assert.That(sideField.Id, Is.EqualTo(154));
            Assert.That(sideField.NullValue, Is.EqualTo("0"));
            Assert.That(group.Id, Is.EqualTo(453));
            Assert.That(group.DimensionType, Is.EqualTo("venueGroupSize"));
            Assert.That(group.Members.Single().Kind, Is.EqualTo("data"));
            Assert.That(message.SourceId, Is.EqualTo(100));
            Assert.That(message.TemplateId, Is.EqualTo(77));
            Assert.That(message.SemanticType, Is.EqualTo("D"));
            Assert.That(message.SinceVersion, Is.EqualTo(4));
            Assert.That(message.Members.Single(item => item.Kind == "group").Id, Is.EqualTo(453));
            Assert.That(message.Members.Single(item => item.Name == "Side").Id, Is.EqualTo(154));
        });
    }

    [Test]
    public void CanonicalDictionaryMappingProvidesPortableFallbacksForEveryLayoutKind()
    {
        FixDictionary dictionary = CreateCanonicalDictionary();
        SbeGenerationOptions options = new SbeGenerationOptions
        {
            Package = "canonical.sbe",
            SchemaId = 7,
            Version = 2,
            FirstTemplateId = 10,
            LittleEndian = false,
            EmitSemanticTypes = false,
            Scenario = "base"
        };

        SbeMappingPlan plan = SbeTypeMapper.MapDictionaryTypes(dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False, string.Join(",", plan.Diagnostics.Select(item => item.Code)));
            Assert.That(plan.ByteOrder, Is.EqualTo("bigEndian"));
            Assert.That(plan.Types.Single(item => item.Name == "Int").PrimitiveType, Is.EqualTo("int32"));
            Assert.That(plan.Types.Single(item => item.Name == "Price").Kind, Is.EqualTo("composite"));
            Assert.That(plan.Types.Single(item => item.Name == "String").VariableLength, Is.True);
            Assert.That(plan.Types.Single(item => item.Name == "SideCode").Kind, Is.EqualTo("enum"));
            Assert.That(plan.Types.Single(item => item.Name == "OrderCore").Kind, Is.EqualTo("composite"));
            Assert.That(plan.Fields.Single(item => item.Name == "RawData").Kind, Is.EqualTo("data"));
            Assert.That(plan.Groups.Single().Id, Is.EqualTo(100));
            Assert.That(plan.Messages.Single().TemplateId, Is.EqualTo(10));
            Assert.That(plan.Messages.Single().Members.Select(item => item.Kind), Is.EqualTo(new[] { "component", "group", "field" }));
            Assert.That(plan.Types, Is.All.Matches<SbeTypeMapping>(item => item.SemanticType.Length == 0));
            Assert.That(plan.Fields, Is.All.Matches<SbeFieldMapping>(item => item.SemanticType.Length == 0));
            Assert.That(plan.Messages.Single().SemanticType, Is.Empty);
        });
    }

    [Test]
    public void EveryCanonicalFieldTypeMapsToItsSupportedSbeShape()
    {
        FixDictionary dictionary = new FixDictionary();
        foreach (FixFieldType fieldType in Enum.GetValues<FixFieldType>())
        {
            dictionary.Datatypes.Add(new FixDatatypeDefinition { Name = fieldType.ToString(), FieldType = fieldType });
        }
        dictionary.Datatypes.Add(new FixDatatypeDefinition { Name = "IntAlias", FieldType = FixFieldType.Unknown, BaseTypeName = "Int" });

        SbeMappingPlan plan = SbeTypeMapper.MapDictionaryTypes(dictionary, new SbeGenerationOptions());
        Dictionary<FixFieldType, string> scalarPrimitives = new Dictionary<FixFieldType, string>
        {
            [FixFieldType.Unknown] = "char",
            [FixFieldType.Int] = "int32",
            [FixFieldType.SeqNum] = "uint32",
            [FixFieldType.NumInGroup] = "uint16",
            [FixFieldType.Length] = "uint32",
            [FixFieldType.TagNum] = "uint32",
            [FixFieldType.Char] = "char",
            [FixFieldType.Boolean] = "uint8",
            [FixFieldType.DayOfMonth] = "int32",
            [FixFieldType.LocalMktDate] = "uint32",
            [FixFieldType.UtcDateOnly] = "uint32",
            [FixFieldType.UtcTimeOnly] = "uint64",
            [FixFieldType.UtcTimestamp] = "uint64"
        };
        FixFieldType[] decimalTypes =
        [
            FixFieldType.Float,
            FixFieldType.Qty,
            FixFieldType.Price,
            FixFieldType.PriceOffset,
            FixFieldType.Amt,
            FixFieldType.Percentage
        ];
        FixFieldType[] textTypes =
        [
            FixFieldType.String,
            FixFieldType.MultipleCharValue,
            FixFieldType.MultipleStringValue,
            FixFieldType.Currency,
            FixFieldType.Country,
            FixFieldType.Exchange,
            FixFieldType.TzTimeOnly,
            FixFieldType.TzTimestamp
        ];

        Assert.Multiple(() =>
        {
            Assert.That(plan.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False, string.Join(",", plan.Diagnostics.Select(item => item.Code)));
            foreach (KeyValuePair<FixFieldType, string> expected in scalarPrimitives)
            {
                Assert.That(plan.Types.Single(item => item.Name == expected.Key.ToString()).PrimitiveType, Is.EqualTo(expected.Value), expected.Key.ToString());
            }
            foreach (FixFieldType fieldType in decimalTypes)
            {
                SbeTypeMapping mapping = plan.Types.Single(item => item.Name == fieldType.ToString());
                Assert.That(mapping.Kind, Is.EqualTo("composite"), fieldType.ToString());
                Assert.That(mapping.Members.Select(item => item.Name + ":" + item.TypeName), Is.EqualTo(new[] { "mantissa:int64", "exponent:int8" }), fieldType.ToString());
                Assert.That(mapping.Members[1].ConstantValue, Is.EqualTo("0"), fieldType.ToString());
            }
            foreach (FixFieldType fieldType in textTypes)
            {
                SbeTypeMapping mapping = plan.Types.Single(item => item.Name == fieldType.ToString());
                Assert.That(mapping.PrimitiveType, Is.EqualTo("char"), fieldType.ToString());
                Assert.That(mapping.VariableLength, Is.True, fieldType.ToString());
            }
            foreach (FixFieldType fieldType in new[] { FixFieldType.Data, FixFieldType.XmlData })
            {
                SbeTypeMapping mapping = plan.Types.Single(item => item.Name == fieldType.ToString());
                Assert.That(mapping.PrimitiveType, Is.EqualTo("uint8"), fieldType.ToString());
                Assert.That(mapping.VariableLength, Is.True, fieldType.ToString());
            }
            Assert.That(plan.Types.Single(item => item.Name == "MonthYear").Members.Select(item => item.Name + ":" + item.TypeName), Is.EqualTo(new[] { "year:uint16", "month:uint8", "day:uint8", "week:uint8" }));
            Assert.That(plan.Types.Single(item => item.Name == "IntAlias").ReferenceType, Is.EqualTo("Int"));
        });
    }

    [Test]
    public void MappingIsIndependentOfOrchestraDefinitionInsertionOrder()
    {
        OrchestraRepository firstRepository = CreateOrchestraRepository();
        OrchestraRepository secondRepository = CreateOrchestraRepository();
        ReverseOrchestraRepository(secondRepository);
        SbeGenerationOptions options = new SbeGenerationOptions { Package = "venue.sbe", SchemaId = 42, FirstTemplateId = 100 };

        SbeMappingPlan first = SbeTypeMapper.MapOrchestraRepository(firstRepository, options);
        SbeMappingPlan second = SbeTypeMapper.MapOrchestraRepository(secondRepository, options);

        Assert.Multiple(() =>
        {
            Assert.That(MappingText(second), Is.EqualTo(MappingText(first)));
            Assert.That(secondRepository.Datatypes[0].Name, Is.EqualTo("MonthYear"));
            Assert.That(secondRepository.Fields[0].Name, Is.EqualTo("NoPartyIDs"));
        });
    }

    [Test]
    public void ExplicitIdentifiersAndVersionsRemainStableAcrossRepeatedMappings()
    {
        OrchestraRepository firstRepository = CreateOrchestraRepository();
        AddVersionedEncodings(firstRepository);
        OrchestraRepository secondRepository = CreateOrchestraRepository();
        AddVersionedEncodings(secondRepository);
        ReverseOrchestraRepository(secondRepository);
        SbeGenerationOptions options = new SbeGenerationOptions
        {
            Package = "venue.sbe",
            SchemaId = 42,
            Version = 12,
            FirstTemplateId = 100,
            Scenario = "base"
        };

        SbeMappingPlan first = SbeTypeMapper.MapOrchestraRepository(firstRepository, options);
        SbeMappingPlan second = SbeTypeMapper.MapOrchestraRepository(secondRepository, options);
        SbeSchemaValidation.ValidateSchemaPlan(first);
        SbeSchemaValidation.ValidateSchemaPlan(second);
        SbeMessageMapping message = first.Messages.Single();
        string schema = SbeSchemaEmitter.EmitSchema(first);

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False, string.Join(",", first.Diagnostics.Select(item => item.Code)));
            Assert.That(second.Diagnostics.Exists(item => item.Severity == ValidationSeverity.Error || item.Severity == ValidationSeverity.Fatal), Is.False, string.Join(",", second.Diagnostics.Select(item => item.Code)));
            Assert.That(MappingText(second), Is.EqualTo(MappingText(first)));
            Assert.That(first.SchemaId, Is.EqualTo(42));
            Assert.That(first.Version, Is.EqualTo(12));
            Assert.That(first.Fields.Single(item => item.Name == "Side").Id, Is.EqualTo(154));
            Assert.That(first.Groups.Single().Id, Is.EqualTo(453));
            Assert.That(message.TemplateId, Is.EqualTo(77));
            Assert.That(message.SinceVersion, Is.EqualTo(6));
            Assert.That(message.DeprecatedVersion, Is.EqualTo(12));
            Assert.That(message.Members.Single(item => item.Name == "Side").Id, Is.EqualTo(254));
            Assert.That(message.Members.Single(item => item.Name == "Side").SinceVersion, Is.EqualTo(7));
            Assert.That(first.Components.Single().SinceVersion, Is.EqualTo(2));
            Assert.That(first.Groups.Single().SinceVersion, Is.EqualTo(4));
            Assert.That(first.Types.Single(item => item.Name == "SideCode").Values.Single(item => item.Name == "Buy").SinceVersion, Is.EqualTo(1));
            Assert.That(schema, Does.Contain("<sbe:message name=\"NewOrderSingle\" id=\"77\" semanticType=\"D\" sinceVersion=\"6\" deprecated=\"12\">"));
            Assert.That(schema, Does.Contain("<field name=\"Side\" id=\"254\" type=\"SideCode\" semanticType=\"Char\" sinceVersion=\"7\" deprecated=\"11\" />"));
            Assert.That(schema, Does.Contain("<validValue name=\"Buy\" sinceVersion=\"1\" deprecated=\"8\">1</validValue>"));
        });
    }

    [Test]
    public void InvalidAndAmbiguousPhysicalMappingsProduceExplicitDiagnostics()
    {
        OrchestraRepository repository = CreateOrchestraRepository();
        repository.EncodingStandards[0].Encoding!.Attributes["byteOrder"] = "middleEndian";
        repository.Datatypes[0].Mappings.Add(new OrchestraDatatypeMapping { Standard = "SBE", Base = "int32" });
        repository.Fields[0].Id = "bad";
        OrchestraMessageDefinition duplicate = new OrchestraMessageDefinition
        {
            Id = "101",
            Name = "DuplicateTemplate",
            MessageType = "8",
            Scenario = "base",
            Encodings = [Encoding("id", "77")]
        };
        repository.Messages.Add(duplicate);

        SbeMappingPlan plan = SbeTypeMapper.MapOrchestraRepository(repository, new SbeGenerationOptions { FirstTemplateId = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("invalid-sbe-byte-order"));
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("ambiguous-sbe-datatype-mapping"));
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("invalid-sbe-field-id"));
            Assert.That(plan.Diagnostics.Select(item => item.Code), Does.Contain("duplicate-sbe-template-id"));
        });
    }

    private static OrchestraRepository CreateOrchestraRepository()
    {
        OrchestraEncoding standardEncoding = Encoding("byteOrder", "bigEndian");
        OrchestraRepository repository = new OrchestraRepository
        {
            EncodingStandards =
            [
                new OrchestraEncodingStandard { Name = "SBE", Encoding = standardEncoding }
            ],
            Datatypes =
            [
                new OrchestraDatatypeDefinition
                {
                    Name = "Price",
                    BaseType = "Decimal",
                    Pedigree = new OrchestraPedigree { AddedExtensionPack = "7", DeprecatedExtensionPack = "9" },
                    Mappings =
                    [
                        new OrchestraDatatypeMapping
                        {
                            Standard = "SBE",
                            Builtin = true,
                            HasBuiltin = true,
                            Base = "int64",
                            Parameter = "scale=4",
                            MinimumInclusive = "-100",
                            MaximumInclusive = "100",
                            Encoding = Encoding("nullValue", "-9223372036854775808")
                        }
                    ]
                },
                Datatype("Char", "char"),
                Datatype("NumInGroup", "uint16"),
                new OrchestraDatatypeDefinition
                {
                    Name = "data",
                    Mappings =
                    [
                        new OrchestraDatatypeMapping { Standard = "SBE", Base = "uint8", Encoding = Encoding("variableLength", "true") }
                    ]
                },
                new OrchestraDatatypeDefinition { Name = "MonthYear" }
            ],
            CodeSets =
            [
                new OrchestraCodeSetDefinition
                {
                    Id = "1",
                    Name = "SideCode",
                    Type = "Char",
                    Codes =
                    [
                        new OrchestraCodeDefinition { Id = "1", Name = "Buy", Value = "1", Sort = "1" },
                        new OrchestraCodeDefinition { Id = "2", Name = "Sell", Value = "2", Sort = "2" }
                    ]
                }
            ],
            Fields =
            [
                new OrchestraFieldDefinition
                {
                    Id = "44",
                    Name = "PriceField",
                    Type = "Price",
                    Presence = FixPresence.Constant,
                    Value = "123.45"
                },
                new OrchestraFieldDefinition
                {
                    Id = "54",
                    Name = "Side",
                    Type = "Char",
                    CodeSet = "SideCode",
                    Encodings = [Encoding("id", "154", "nullValue", "0")]
                },
                new OrchestraFieldDefinition
                {
                    Id = "96",
                    Name = "RawData",
                    Type = "data",
                    Encodings = [Encoding("sinceVersion", "3")]
                },
                new OrchestraFieldDefinition { Id = "453", Name = "NoPartyIDs", Type = "NumInGroup" }
            ],
            Components =
            [
                new OrchestraComponentDefinition
                {
                    Id = "1000",
                    Name = "OrderCore",
                    Members = [Member("fieldRef", "44", "PriceField", 0)]
                }
            ],
            Groups =
            [
                new OrchestraGroupDefinition
                {
                    Id = "2000",
                    Name = "Payloads",
                    CountMember = Member("numInGroup", "453", "NoPartyIDs", 0),
                    Members = [Member("fieldRef", "96", "RawData", 0)],
                    Encodings = [Encoding("dimensionType", "venueGroupSize")]
                }
            ],
            Messages =
            [
                new OrchestraMessageDefinition
                {
                    Id = "100",
                    Name = "NewOrderSingle",
                    MessageType = "D",
                    Scenario = "base",
                    Pedigree = new OrchestraPedigree { AddedExtensionPack = "4" },
                    Members =
                    [
                        Member("componentRef", "1000", "OrderCore", 0),
                        Member("groupRef", "2000", "Payloads", 1),
                        Member("fieldRef", "54", "Side", 2)
                    ],
                    Encodings = [Encoding("id", "77")]
                }
            ]
        };
        return repository;
    }

    private static OrchestraDatatypeDefinition Datatype(string name, string primitive)
    {
        return new OrchestraDatatypeDefinition
        {
            Name = name,
            Mappings = [new OrchestraDatatypeMapping { Standard = "SBE", Base = primitive }]
        };
    }

    private static OrchestraMemberDefinition Member(string kind, string id, string name, int position)
    {
        return new OrchestraMemberDefinition
        {
            Kind = kind,
            ReferenceId = id,
            InstanceName = name,
            Position = position,
            Presence = FixPresence.Required
        };
    }

    private static OrchestraEncoding Encoding(string firstName, string firstValue)
    {
        return new OrchestraEncoding
        {
            Standard = "SBE",
            Attributes = new Dictionary<string, string> { [firstName] = firstValue }
        };
    }

    private static OrchestraEncoding Encoding(string firstName, string firstValue, string secondName, string secondValue)
    {
        return new OrchestraEncoding
        {
            Standard = "SBE",
            Attributes = new Dictionary<string, string> { [firstName] = firstValue, [secondName] = secondValue }
        };
    }

    private static FixDictionary CreateCanonicalDictionary()
    {
        return new FixDictionary
        {
            Datatypes =
            [
                new FixDatatypeDefinition { Name = "Int", FieldType = FixFieldType.Int },
                new FixDatatypeDefinition { Name = "NumInGroup", FieldType = FixFieldType.NumInGroup },
                new FixDatatypeDefinition { Name = "Price", FieldType = FixFieldType.Price },
                new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String },
                new FixDatatypeDefinition { Name = "data", FieldType = FixFieldType.Data }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Id = 1,
                    Name = "SideCode",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "1" }]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 1, Name = "Account", DatatypeName = "String", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 2, Name = "Count", DatatypeName = "Int", FieldType = FixFieldType.Int },
                new FixFieldDefinition { Tag = 3, Name = "RawData", DatatypeName = "data", FieldType = FixFieldType.Data },
                new FixFieldDefinition { Tag = 54, Name = "Side", CodeSetName = "SideCode", FieldType = FixFieldType.Char },
                new FixFieldDefinition { Tag = 100, Name = "NoItems", DatatypeName = "NumInGroup", FieldType = FixFieldType.NumInGroup }
            ],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "OrderCore",
                    Members = [new FixMemberDefinition { Name = "Count", Kind = FixMemberKind.Field, ReferenceId = 2, Position = 0 }]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 20,
                    Name = "Items",
                    CountTag = 100,
                    DelimiterTag = 3,
                    Members = [new FixMemberDefinition { Name = "RawData", Kind = FixMemberKind.Field, ReferenceId = 3, Position = 0 }]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 30,
                    Name = "Order",
                    MsgType = "D",
                    Scenario = "base",
                    Members =
                    [
                        new FixMemberDefinition { Name = "OrderCore", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 0 },
                        new FixMemberDefinition { Name = "Items", Kind = FixMemberKind.Group, ReferenceId = 20, Position = 1 },
                        new FixMemberDefinition { Name = "Side", Kind = FixMemberKind.Field, ReferenceId = 54, Position = 2 }
                    ]
                }
            ]
        };
    }

    private static void ReverseOrchestraRepository(OrchestraRepository repository)
    {
        repository.Datatypes.Reverse();
        for (int index = 0; index < repository.Datatypes.Count; index++) repository.Datatypes[index].Mappings.Reverse();
        repository.CodeSets.Reverse();
        for (int index = 0; index < repository.CodeSets.Count; index++) repository.CodeSets[index].Codes.Reverse();
        repository.Fields.Reverse();
        repository.Components.Reverse();
        for (int index = 0; index < repository.Components.Count; index++) repository.Components[index].Members.Reverse();
        repository.Groups.Reverse();
        for (int index = 0; index < repository.Groups.Count; index++) repository.Groups[index].Members.Reverse();
        repository.Messages.Reverse();
        for (int index = 0; index < repository.Messages.Count; index++) repository.Messages[index].Members.Reverse();
    }

    private static void AddVersionedEncodings(OrchestraRepository repository)
    {
        repository.CodeSets[0].Codes[0].Pedigree = new OrchestraPedigree { AddedVersion = "1", DeprecatedVersion = "8" };
        repository.Fields[0].Pedigree = new OrchestraPedigree { AddedVersion = "2", DeprecatedVersion = "11" };
        repository.Components[0].Encodings.Add(Encoding("sinceVersion", "2", "deprecated", "11"));
        repository.Components[0].Members[0].Pedigree = new OrchestraPedigree { AddedVersion = "3", DeprecatedVersion = "10" };
        repository.Groups[0].Encodings[0].Attributes["dimensionType"] = "groupSizeEncoding";
        repository.Groups[0].Encodings[0].Attributes["sinceVersion"] = "4";
        repository.Groups[0].Encodings[0].Attributes["deprecated"] = "11";
        repository.Groups[0].Members[0].Encodings.Add(Encoding("sinceVersion", "5", "deprecated", "10"));
        repository.Messages[0].Encodings[0].Attributes["sinceVersion"] = "6";
        repository.Messages[0].Encodings[0].Attributes["deprecated"] = "12";
        repository.Messages[0].Members[2].Encodings.Add(new OrchestraEncoding
        {
            Standard = "SBE",
            Attributes = new Dictionary<string, string>
            {
                ["id"] = "254",
                ["sinceVersion"] = "7",
                ["deprecated"] = "11"
            }
        });
    }

    private static string MappingText(SbeMappingPlan plan)
    {
        List<string> values = [plan.Package, plan.SchemaId.ToString(), plan.Version.ToString(), plan.ByteOrder];
        values.AddRange(plan.Types.Select(item => "T:" + item.Name + ":" + item.Kind + ":" + item.PrimitiveType + ":" + item.SemanticType + ":" + item.Length + ":" + item.VariableLength + ":" + string.Join(",", item.Values.Select(value => value.Name + "=" + value.Value)) + ":" + string.Join(",", item.Members.Select(member => member.Name + "=" + member.TypeName))));
        values.AddRange(plan.Fields.Select(item => "F:" + item.Id + ":" + item.Name + ":" + item.TypeName + ":" + item.Kind));
        values.AddRange(plan.Components.Select(item => "C:" + item.Id + ":" + item.Name + ":" + string.Join(",", item.Members.Select(member => member.Name))));
        values.AddRange(plan.Groups.Select(item => "G:" + item.Id + ":" + item.Name + ":" + string.Join(",", item.Members.Select(member => member.Name))));
        values.AddRange(plan.Messages.Select(item => "M:" + item.TemplateId + ":" + item.Name + ":" + string.Join(",", item.Members.Select(member => member.Name))));
        values.AddRange(plan.Diagnostics.Select(item => "D:" + item.Code + ":" + item.Source));
        return string.Join("\n", values);
    }
}
