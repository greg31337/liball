using fix;
using System.Xml;
using System.Xml.Schema;

namespace fix.Tests;

[TestFixture]
public class SbeSchemaGeneratorTests
{
    [Test]
    public void WarningDiagnosticsDoNotPreventSchemaEmission()
    {
        List<GenerationDiagnostic> diagnostics =
        [
            new GenerationDiagnostic { Severity = ValidationSeverity.Warning, Code = "warning" }
        ];

        Assert.That(SbeSchemaGenerator.HasSbeGeneratorErrors(diagnostics), Is.False);
    }

    [Test]
    public void GeneratedSchemaValidatesAgainstBundledSbeXsd()
    {
        GenerationResult generated = new SbeSchemaGenerator().GenerateSbeSchema(CreateDictionary(), new SbeGenerationOptions
        {
            Package = "venue.sbe",
            SchemaId = 42,
            Version = 0,
            FirstTemplateId = 100,
            Scenario = "base"
        });
        string xsdPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../specs/sbe/resources/sbe.xsd"));
        Assert.That(File.Exists(xsdPath), Is.True, xsdPath);
        XmlSchemaSet schemas = new XmlSchemaSet();
        schemas.Add("http://fixprotocol.io/2016/sbe", xsdPath);
        List<string> issues = [];
        XmlReaderSettings settings = new XmlReaderSettings
        {
            Schemas = schemas,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
        };
        settings.ValidationEventHandler += (_, eventArgs) => issues.Add(eventArgs.Severity + ": " + eventArgs.Message);

        using StringReader text = new StringReader(generated.Files.Single().Content);
        using XmlReader reader = XmlReader.Create(text, settings);
        while (reader.Read())
        {
        }

        Assert.Multiple(() =>
        {
            Assert.That(generated.Success, Is.True, string.Join(",", generated.Diagnostics.Select(item => item.Code)));
            Assert.That(issues, Is.Empty, string.Join(Environment.NewLine, issues));
        });
    }

    [Test]
    public void GeneratorEmitsSelectedTypesEnumsCompositesGroupsAndVariableData()
    {
        FixDictionary dictionary = CreateDictionary();
        SbeGenerationOptions options = new SbeGenerationOptions
        {
            Package = "venue.sbe",
            SchemaId = 42,
            Version = 0,
            FirstTemplateId = 100,
            LittleEndian = false,
            EmitSemanticTypes = true,
            Scenario = "base"
        };

        GenerationResult result = new SbeSchemaGenerator().GenerateSbeSchema(dictionary, options);
        string content = result.Files.Single().Content;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, string.Join(",", result.Diagnostics.Select(item => item.Code)));
            Assert.That(result.Files, Has.Count.EqualTo(1));
            Assert.That(result.Files[0].Path, Is.EqualTo("Schemas/SbeSchema.xml"));
            Assert.That(result.Files[0].ContentHash, Has.Length.EqualTo(64));
            Assert.That(result.Manifest, Does.Contain(result.Files[0].ContentHash));
            Assert.That(content, Does.Contain("package=\"venue.sbe\" id=\"42\" version=\"0\" byteOrder=\"bigEndian\""));
            Assert.That(content, Does.Contain("<composite name=\"messageHeader\">"));
            Assert.That(content, Does.Contain("<composite name=\"groupSizeEncoding\">"));
            Assert.That(content, Does.Contain("<enum name=\"SideCode\" encodingType=\"char\""));
            Assert.That(content, Does.Contain("<composite name=\"Price\" semanticType=\"Price\">"));
            Assert.That(content, Does.Contain("<composite name=\"data\" semanticType=\"data\">"));
            Assert.That(content, Does.Not.Contain("<composite name=\"OrderCore\""));
            Assert.That(content, Does.Contain("<data name=\"Account\" id=\"1\" type=\"String\""));
            Assert.That(content, Does.Contain("<group name=\"Items\" id=\"100\" dimensionType=\"groupSizeEncoding\">"));
            Assert.That(content, Does.Contain("<data name=\"RawData\" id=\"3\" type=\"data\""));
            Assert.That(content, Does.Contain("<sbe:message name=\"ExecutionReport\" id=\"100\" semanticType=\"8\">"));
            Assert.That(content, Does.Contain("<sbe:message name=\"Order\" id=\"101\" semanticType=\"D\">"));
        });
    }

    [Test]
    public void SchemaAndManifestAreDeterministicAcrossInsertionOrderAndScenarioFallback()
    {
        FixDictionary firstDictionary = CreateDictionary();
        AddScenarioMessage(firstDictionary);
        FixDictionary secondDictionary = CreateDictionary();
        AddScenarioMessage(secondDictionary);
        ReverseDictionary(secondDictionary);
        SbeGenerationOptions options = new SbeGenerationOptions
        {
            Package = "venue.sbe",
            SchemaId = 42,
            Version = 0,
            FirstTemplateId = 100,
            Scenario = "algo"
        };

        GenerationResult first = new SbeSchemaGenerator().GenerateSbeSchema(firstDictionary, options);
        GenerationResult second = new SbeSchemaGenerator().GenerateSbeSchema(secondDictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, string.Join(",", first.Diagnostics.Select(item => item.Code)));
            Assert.That(second.Success, Is.True, string.Join(",", second.Diagnostics.Select(item => item.Code)));
            Assert.That(second.Files.Single().Content, Is.EqualTo(first.Files.Single().Content));
            Assert.That(second.Manifest, Is.EqualTo(first.Manifest));
            Assert.That(first.Files.Single().Content, Does.Contain("<sbe:message name=\"AlgorithmicOrder\""));
            Assert.That(first.Files.Single().Content, Does.Not.Contain("<sbe:message name=\"Order\""));
            Assert.That(first.Files.Single().Content, Does.Contain("<sbe:message name=\"ExecutionReport\""));
        });
    }

    [Test]
    public void ConditionalAndRecursiveLayoutsAreRejectedWithExplicitDiagnostics()
    {
        FixDictionary conditional = CreateDictionary();
        conditional.Messages[0].Members[0].Presence = FixPresence.Conditional;
        conditional.Messages[0].Members[0].PresenceRule = "Side == 1";
        FixDictionary recursive = CreateRecursiveDictionary();
        SbeSchemaGenerator generator = new SbeSchemaGenerator();

        GenerationResult conditionalResult = generator.GenerateSbeSchema(conditional, new SbeGenerationOptions());
        GenerationResult recursiveResult = generator.GenerateSbeSchema(recursive, new SbeGenerationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(conditionalResult.Success, Is.False);
            Assert.That(conditionalResult.Files, Is.Empty);
            Assert.That(conditionalResult.Diagnostics.Select(item => item.Code), Does.Contain("unsupported-sbe-conditional-layout"));
            Assert.That(recursiveResult.Success, Is.False);
            Assert.That(recursiveResult.Files, Is.Empty);
            Assert.That(recursiveResult.Diagnostics.Select(item => item.Code), Does.Contain("recursive-sbe-layout"));
        });
    }

    [Test]
    public void AmbiguousMappingsAndUnsupportedPrecisionAreRejected()
    {
        FixDictionary ambiguous = CreateDictionary();
        ambiguous.Datatypes.Add(new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String });
        ambiguous.Fields.Add(new FixFieldDefinition { Tag = 1, Name = "DuplicateAccount", DatatypeName = "String", FieldType = FixFieldType.String });
        GenerationResult ambiguousResult = new SbeSchemaGenerator().GenerateSbeSchema(ambiguous, new SbeGenerationOptions());

        SbeMappingPlan precisionPlan = CreateUnsupportedPrecisionPlan();
        SbeSchemaValidation.ValidateSchemaPlan(precisionPlan);

        Assert.Multiple(() =>
        {
            Assert.That(ambiguousResult.Success, Is.False);
            Assert.That(ambiguousResult.Diagnostics.Select(item => item.Code), Does.Contain("ambiguous-sbe-type-name"));
            Assert.That(ambiguousResult.Diagnostics.Select(item => item.Code), Does.Contain("ambiguous-sbe-field-id"));
            Assert.That(precisionPlan.Diagnostics.Select(item => item.Code), Does.Contain("unsupported-sbe-precision"));
        });
    }

    [Test]
    public void UnsupportedPlanConstructsProduceExplicitDiagnostics()
    {
        SbeMappingPlan plan = new SbeMappingPlan { SchemaId = 1, Version = 3 };
        plan.Types.Add(new SbeTypeMapping { Name = "BadPrimitive", Kind = "type", PrimitiveType = "decimal128" });
        plan.Types.Add(new SbeTypeMapping { Name = "EmptyEnum", Kind = "enum", PrimitiveType = "uint8" });
        plan.Types.Add(new SbeTypeMapping { Name = "UnsupportedSet", Kind = "set", PrimitiveType = "uint8" });
        plan.Types.Add(new SbeTypeMapping
        {
            Name = "RecursiveType",
            Kind = "composite",
            Members = [new SbeMemberMapping { Name = "self", Kind = "type", TypeName = "RecursiveType" }]
        });
        plan.Groups.Add(new SbeGroupMapping
        {
            Id = 1,
            Name = "BadGroup",
            DimensionType = "MissingDimension",
            SinceVersion = 4
        });
        plan.Messages.Add(new SbeMessageMapping
        {
            TemplateId = 1,
            Name = "BadMessage",
            SinceVersion = 2,
            DeprecatedVersion = 1,
            Members =
            [
                new SbeMemberMapping
                {
                    Id = 1,
                    Name = "ConditionalMystery",
                    Kind = "mystery",
                    Conditional = true,
                    SinceVersion = 4
                }
            ]
        });

        SbeSchemaValidation.ValidateSchemaPlan(plan);

        Assert.That(plan.Diagnostics.Select(item => item.Code), Is.SupersetOf(new[]
        {
            "unsupported-sbe-primitive",
            "empty-sbe-enum",
            "unsupported-sbe-type-kind",
            "recursive-sbe-type",
            "undefined-sbe-dimension-type",
            "unsupported-sbe-conditional-layout",
            "unsupported-sbe-member-kind",
            "invalid-sbe-since-version",
            "invalid-sbe-version-range"
        }));
    }

    [Test]
    public void MissingInputsAndOutOfRangeIdsReturnFailuresWithoutFiles()
    {
        SbeSchemaGenerator generator = new SbeSchemaGenerator();
        GenerationResult missingDictionary = generator.GenerateSbeSchema(null!, new SbeGenerationOptions());
        GenerationResult missingOptions = generator.GenerateSbeSchema(CreateDictionary(), null!);
        SbeGenerationOptions invalidOptions = new SbeGenerationOptions { SchemaId = 65536 };
        GenerationResult invalid = generator.GenerateSbeSchema(CreateDictionary(), invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(missingDictionary.Diagnostics.Single().Code, Is.EqualTo("missing-sbe-dictionary"));
            Assert.That(missingOptions.Diagnostics.Single().Code, Is.EqualTo("missing-sbe-options"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Files, Is.Empty);
            Assert.That(invalid.Diagnostics.Select(item => item.Code), Does.Contain("invalid-sbe-schema-id"));
        });
    }

    private static FixDictionary CreateDictionary()
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
                    Codes =
                    [
                        new FixCodeDefinition { Name = "Buy", Value = "1" },
                        new FixCodeDefinition { Name = "Sell", Value = "2" }
                    ]
                }
            ],
            Fields =
            [
                new FixFieldDefinition { Tag = 1, Name = "Account", DatatypeName = "String", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 3, Name = "RawData", DatatypeName = "data", FieldType = FixFieldType.Data },
                new FixFieldDefinition { Tag = 17, Name = "ExecID", DatatypeName = "String", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 44, Name = "Price", DatatypeName = "Price", FieldType = FixFieldType.Price },
                new FixFieldDefinition { Tag = 54, Name = "Side", CodeSetName = "SideCode", FieldType = FixFieldType.Char },
                new FixFieldDefinition { Tag = 100, Name = "NoItems", DatatypeName = "NumInGroup", FieldType = FixFieldType.NumInGroup }
            ],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "OrderCore",
                    Members =
                    [
                        new FixMemberDefinition { Name = "Account", Kind = FixMemberKind.Field, ReferenceId = 1, Position = 0 },
                        new FixMemberDefinition { Name = "Price", Kind = FixMemberKind.Field, ReferenceId = 44, Position = 1 }
                    ]
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
                },
                new FixMessageDefinition
                {
                    Id = 31,
                    Name = "ExecutionReport",
                    MsgType = "8",
                    Scenario = "base",
                    Members = [new FixMemberDefinition { Name = "ExecID", Kind = FixMemberKind.Field, ReferenceId = 17, Position = 0 }]
                }
            ]
        };
    }

    private static void AddScenarioMessage(FixDictionary dictionary)
    {
        dictionary.Messages.Add(new FixMessageDefinition
        {
            Id = 32,
            Name = "AlgorithmicOrder",
            MsgType = "D",
            Scenario = "algo",
            Members = [new FixMemberDefinition { Name = "Side", Kind = FixMemberKind.Field, ReferenceId = 54, Position = 0 }]
        });
    }

    private static FixDictionary CreateRecursiveDictionary()
    {
        return new FixDictionary
        {
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 10,
                    Name = "First",
                    Members = [new FixMemberDefinition { Name = "Second", Kind = FixMemberKind.Component, ReferenceId = 20, Position = 0 }]
                },
                new FixComponentDefinition
                {
                    Id = 20,
                    Name = "Second",
                    Members = [new FixMemberDefinition { Name = "First", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 0 }]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "RecursiveMessage",
                    MsgType = "R",
                    Scenario = "base",
                    Members = [new FixMemberDefinition { Name = "First", Kind = FixMemberKind.Component, ReferenceId = 10, Position = 0 }]
                }
            ]
        };
    }

    private static SbeMappingPlan CreateUnsupportedPrecisionPlan()
    {
        SbeMappingPlan plan = new SbeMappingPlan { SchemaId = 1, Version = 1 };
        SbeTypeMapping decimalType = new SbeTypeMapping
        {
            Name = "DecimalValue",
            Kind = "composite",
            Members =
            [
                new SbeMemberMapping { Name = "mantissa", Kind = "type", TypeName = "int64" },
                new SbeMemberMapping { Name = "exponent", Kind = "type", TypeName = "int8", Presence = "constant", ConstantValue = "-129" }
            ]
        };
        plan.Types.Add(decimalType);
        plan.Fields.Add(new SbeFieldMapping { SourceId = 1, Id = 1, Name = "Value", TypeName = "DecimalValue" });
        plan.Messages.Add(new SbeMessageMapping
        {
            SourceId = 1,
            TemplateId = 1,
            Name = "PrecisionMessage",
            MessageType = "P",
            Members = [new SbeMemberMapping { Id = 1, Name = "Value", Kind = "field", TypeName = "DecimalValue" }]
        });
        return plan;
    }

    private static void ReverseDictionary(FixDictionary dictionary)
    {
        dictionary.Datatypes.Reverse();
        dictionary.CodeSets.Reverse();
        dictionary.CodeSets[0].Codes.Reverse();
        dictionary.Fields.Reverse();
        dictionary.Components.Reverse();
        for (int index = 0; index < dictionary.Components.Count; index++) dictionary.Components[index].Members.Reverse();
        dictionary.Groups.Reverse();
        for (int index = 0; index < dictionary.Groups.Count; index++) dictionary.Groups[index].Members.Reverse();
        dictionary.Messages.Reverse();
        for (int index = 0; index < dictionary.Messages.Count; index++) dictionary.Messages[index].Members.Reverse();
    }
}
