using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using iso20022;

namespace iso20022.Tests;

public class JsonSchemaGeneratorTests
{
    private const string MessageNamespace =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";

    [Test]
    public void CleanWorktreeRegenerationIsStableAcrossRepeatedAndShuffledRepositoryInput()
    {
        CleanWorktreeRegenerationAssertions.AssertDeterministic(
            "json-schema",
            CreateRepositoryPlan,
            plan => new JsonSchemaGenerator().GenerateJsonSchema(plan));
    }

    [Test]
    public void RepositoryModelGeneratesIsoDraft202012Schema()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        IJsonSchemaGenerator generator = new JsonSchemaGenerator();

        GenerationResult result = generator.GenerateJsonSchema(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Files, Has.Count.EqualTo(2));
        GeneratedFile file = result.Files.Single(value =>
            value.Kind == GenerationFileKind.JsonSchema);
        using JsonDocument document = JsonDocument.Parse(file.Content);
        JsonElement root = document.RootElement;
        JsonElement definitions = root.GetProperty("$defs");
        JsonElement rootDefinition = definitions.GetProperty("_Document");
        JsonElement message = definitions.GetProperty("CustomerCreditTransferV10");
        JsonElement component = definitions.GetProperty("PaymentInstruction");
        Assert.Multiple(() =>
        {
            Assert.That(file.Kind, Is.EqualTo(GenerationFileKind.JsonSchema));
            Assert.That(file.RelativePath, Is.EqualTo(
                "JsonSchemas/pacs.008.001.10-CustomerCreditTransferV10.schema.json"));
            Assert.That(file.MediaType, Is.EqualTo("application/schema+json"));
            Assert.That(file.MediaType, Is.EqualTo(Iso20022MediaTypes.JsonSchema));
            Assert.That(file.NamespaceName, Is.EqualTo(
                "urn:iso:std:iso:20022:tech:json:pacs.008.001.10"));
            Assert.That(file.Hash.Value, Has.Length.EqualTo(32));
            Assert.That(file.SourceIdentifiers, Is.EqualTo(new[] { "message-1" }));
            Assert.That(result.Manifest.GeneratorIdentifier,
                Is.EqualTo("iso20022.json-schema-2020-12"));
            Assert.That(result.Manifest.Files.Single(), Is.SameAs(file));
            Assert.That(root.GetProperty("$schema").GetString(),
                Is.EqualTo("https://json-schema.org/draft/2020-12/schema"));
            Assert.That(root.GetProperty("$id").GetString(), Is.EqualTo(
                "urn:iso:std:iso:20022:tech:json:pacs.008.001.10"));
            Assert.That(root.GetProperty("additionalProperties").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("required")[0].GetString(), Is.EqualTo("Document"));
            Assert.That(root.GetProperty("properties").GetProperty("Document")
                .GetProperty("$ref").GetString(), Is.EqualTo("#_Document"));
            Assert.That(rootDefinition.GetProperty("required")[0].GetString(),
                Is.EqualTo("FIToFICstmrCdtTrf"));
            Assert.That(rootDefinition.GetProperty("properties")
                .GetProperty("FIToFICstmrCdtTrf").GetProperty("$ref").GetString(),
                Is.EqualTo("#CustomerCreditTransferV10"));
            Assert.That(message.GetProperty("required")[0].GetString(),
                Is.EqualTo("PmtInf"));
            Assert.That(message.GetProperty("properties").GetProperty("PmtInf")
                .GetProperty("$ref").GetString(), Is.EqualTo("#PaymentInstruction"));
            Assert.That(component.GetProperty("additionalProperties").GetBoolean(), Is.False);
            Assert.That(component.GetProperty("required").EnumerateArray()
                .Select(value => value.GetString()),
                Is.EqualTo(new[] { "Nm", "Sts", "Acct" }));
            Assert.That(definitions.TryGetProperty("UnusedText", out _), Is.False);
        });
    }

    [Test]
    public void GeneratedDefinitionsApplyCardinalityChoiceWildcardAndLexicalRules()
    {
        GenerationResult result = new JsonSchemaGenerator().GenerateJsonSchema(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        using JsonDocument document = JsonDocument.Parse(result.Files.Single(value =>
            value.Kind == GenerationFileKind.JsonSchema).Content);
        JsonElement definitions = document.RootElement.GetProperty("$defs");
        JsonElement properties = definitions.GetProperty("PaymentInstruction")
            .GetProperty("properties");
        JsonElement status = properties.GetProperty("Sts");
        JsonElement statusArray = status.GetProperty("anyOf")[1];
        JsonElement choice = definitions.GetProperty("AccountChoice");
        JsonElement amount = definitions.GetProperty("ActiveCurrencyAndAmount");
        JsonElement dateTime = definitions.GetProperty("ISODateTime");
        JsonElement binary = definitions.GetProperty("Max16Binary");
        JsonElement text = definitions.GetProperty("Max35Text");
        JsonElement code = definitions.GetProperty("PaymentStatusCode");
        JsonElement currency = definitions.GetProperty("ActiveCurrencyCode");
        JsonElement external = definitions.GetProperty("SupplementaryDataEnvelope1");
        Assert.Multiple(() =>
        {
            Assert.That(status.GetProperty("anyOf")[0].GetProperty("$ref").GetString(),
                Is.EqualTo("#PaymentStatusCode"));
            Assert.That(statusArray.GetProperty("type").GetString(), Is.EqualTo("array"));
            Assert.That(statusArray.GetProperty("minItems").GetInt32(), Is.EqualTo(1));
            Assert.That(statusArray.GetProperty("maxItems").GetInt32(), Is.EqualTo(3));
            Assert.That(statusArray.GetProperty("items").GetProperty("$ref").GetString(),
                Is.EqualTo("#PaymentStatusCode"));
            Assert.That(choice.GetProperty("minProperties").GetInt32(), Is.EqualTo(1));
            Assert.That(choice.GetProperty("maxProperties").GetInt32(), Is.EqualTo(1));
            Assert.That(choice.GetProperty("properties").EnumerateObject()
                .Select(value => value.Name), Is.EqualTo(new[] { "IBAN", "BIC" }));
            Assert.That(external.EnumerateObject().Select(value => value.Name),
                Does.Not.Contain("type"));
            Assert.That(amount.GetProperty("type").GetString(), Is.EqualTo("object"));
            Assert.That(amount.GetProperty("required").EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { "amt", "Ccy" }));
            Assert.That(amount.GetProperty("properties").GetProperty("Ccy")
                .GetProperty("$ref").GetString(), Is.EqualTo("#ActiveCurrencyCode"));
            Assert.That(amount.GetProperty("additionalProperties").GetBoolean(), Is.False);
            Assert.That(dateTime.GetProperty("pattern").GetString(),
                Does.Contain("24:00:00"));
            Assert.That(dateTime.TryGetProperty("format", out _), Is.False);
            Assert.That(binary.GetProperty("minLength").GetInt32(), Is.EqualTo(16));
            Assert.That(binary.GetProperty("maxLength").GetInt32(), Is.EqualTo(16));
            Assert.That(text.GetProperty("maxLength").GetInt32(), Is.EqualTo(35));
            Assert.That(text.GetProperty("pattern").GetString(), Is.EqualTo(".*\\S.*"));
            Assert.That(code.GetProperty("enum").EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { "ACCP", "RJCT" }));
            Assert.That(currency.GetProperty("pattern").GetString(),
                Is.EqualTo("^[A-Z]{3}$"));
            Assert.That(properties.GetProperty("AcctSvcr").GetProperty("type").GetString(),
                Is.EqualTo("string"));
        });
    }

    [Test]
    public void IsoCardinalityRulesDistinguishRequiredOptionalAndRepeatingMembers()
    {
        GenerationResult result = new JsonSchemaGenerator().GenerateJsonSchema(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        using JsonDocument document = JsonDocument.Parse(result.Files.Single(value =>
            value.Kind == GenerationFileKind.JsonSchema).Content);
        JsonElement component = document.RootElement.GetProperty("$defs")
            .GetProperty("PaymentInstruction");
        JsonElement properties = component.GetProperty("properties");
        JsonElement bounded = properties.GetProperty("Sts").GetProperty("anyOf")[1];
        JsonElement unbounded = properties.GetProperty("AddtlInf").GetProperty("anyOf")[1];

        Assert.Multiple(() =>
        {
            Assert.That(component.GetProperty("required").EnumerateArray()
                .Select(value => value.GetString()),
                Is.EqualTo(new[] { "Nm", "Sts", "Acct" }));
            Assert.That(properties.GetProperty("Nm").GetProperty("$ref").GetString(),
                Is.EqualTo("#Max35Text"));
            Assert.That(properties.GetProperty("CreDtTm").GetProperty("$ref").GetString(),
                Is.EqualTo("#ISODateTime"));
            Assert.That(bounded.GetProperty("minItems").GetInt32(), Is.EqualTo(1));
            Assert.That(bounded.GetProperty("maxItems").GetInt32(), Is.EqualTo(3));
            Assert.That(unbounded.GetProperty("minItems").GetInt32(), Is.EqualTo(1));
            Assert.That(unbounded.TryGetProperty("maxItems", out _), Is.False);
            Assert.That(unbounded.GetProperty("items").GetProperty("$ref").GetString(),
                Is.EqualTo("#Max35Text"));
        });
    }

    [Test]
    public void IsoContentRulesMapChoiceWildcardCodesDatesAndBinaryValues()
    {
        GenerationResult result = new JsonSchemaGenerator().GenerateJsonSchema(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        using JsonDocument document = JsonDocument.Parse(result.Files.Single(value =>
            value.Kind == GenerationFileKind.JsonSchema).Content);
        JsonElement definitions = document.RootElement.GetProperty("$defs");
        JsonElement componentProperties = definitions.GetProperty("PaymentInstruction")
            .GetProperty("properties");
        JsonElement choice = definitions.GetProperty("AccountChoice");
        JsonElement wildcard = definitions.GetProperty("SupplementaryDataEnvelope1");
        JsonElement code = definitions.GetProperty("PaymentStatusCode");
        JsonElement date = definitions.GetProperty("ISODate");
        JsonElement dateTime = definitions.GetProperty("ISODateTime");
        JsonElement binary = definitions.GetProperty("Max16Binary");
        string datePattern = date.GetProperty("pattern").GetString()!;
        string dateTimePattern = dateTime.GetProperty("pattern").GetString()!;

        Assert.Multiple(() =>
        {
            Assert.That(choice.GetProperty("additionalProperties").GetBoolean(), Is.False);
            Assert.That(choice.GetProperty("minProperties").GetInt32(), Is.EqualTo(1));
            Assert.That(choice.GetProperty("maxProperties").GetInt32(), Is.EqualTo(1));
            Assert.That(choice.GetProperty("properties").GetProperty("IBAN")
                .GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(choice.GetProperty("properties").GetProperty("BIC")
                .GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(componentProperties.GetProperty("SplmtryData")
                .GetProperty("$ref").GetString(),
                Is.EqualTo("#SupplementaryDataEnvelope1"));
            Assert.That(wildcard.TryGetProperty("type", out _), Is.False);
            Assert.That(wildcard.TryGetProperty("additionalProperties", out _), Is.False);
            Assert.That(code.GetProperty("enum").EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { "ACCP", "RJCT" }));
            Assert.That(date.GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(Regex.IsMatch("2026-08-31Z", datePattern), Is.True);
            Assert.That(Regex.IsMatch("2026-13-31", datePattern), Is.False);
            Assert.That(Regex.IsMatch("2026-08-31T24:00:00Z", dateTimePattern), Is.True);
            Assert.That(Regex.IsMatch("2026-08-31T24:00:01Z", dateTimePattern), Is.False);
            Assert.That(date.TryGetProperty("format", out _), Is.False);
            Assert.That(binary.GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(binary.GetProperty("minLength").GetInt32(), Is.EqualTo(16));
            Assert.That(binary.GetProperty("maxLength").GetInt32(), Is.EqualTo(16));
        });
    }

    [Test]
    public void GenerationIsDeterministicAndHonorsTextStyleAndDocumentationOption()
    {
        GenerationPlan firstPlan = CreateRepositoryPlan(false);
        firstPlan.Options.Indentation = "\t";
        firstPlan.Options.NewLine = "\r\n";
        GenerationPlan shuffledPlan = CreateRepositoryPlan(true);
        shuffledPlan.Options.Indentation = "\t";
        shuffledPlan.Options.NewLine = "\r\n";
        GenerationResult first = new JsonSchemaGenerator().GenerateJsonSchema(firstPlan);
        GenerationResult shuffled = new JsonSchemaGenerator().GenerateJsonSchema(shuffledPlan);
        GenerationPlan undocumentedPlan = CreateRepositoryPlan(false);
        undocumentedPlan.Options.IncludeDocumentation = false;
        GenerationResult undocumented = new JsonSchemaGenerator().GenerateJsonSchema(
            undocumentedPlan);

        Assert.That(first.Success, Is.True, first.ErrorMessage);
        Assert.That(shuffled.Success, Is.True, shuffled.ErrorMessage);
        Assert.That(undocumented.Success, Is.True, undocumented.ErrorMessage);
        string styled = Encoding.UTF8.GetString(first.Files.Single(value =>
            value.Kind == GenerationFileKind.JsonSchema).Content);
        using JsonDocument undocumentedDocument = JsonDocument.Parse(
            undocumented.Files.Single(value =>
                value.Kind == GenerationFileKind.JsonSchema).Content);
        JsonElement component = undocumentedDocument.RootElement.GetProperty("$defs")
            .GetProperty("PaymentInstruction");
        Assert.Multiple(() =>
        {
            Assert.That(shuffled.Files.Single(value =>
                    value.Kind == GenerationFileKind.JsonSchema).Content,
                Is.EqualTo(first.Files.Single(value =>
                    value.Kind == GenerationFileKind.JsonSchema).Content));
            Assert.That(styled, Does.Contain("\r\n\t\"$id\""));
            Assert.That(styled.Replace("\r\n", string.Empty, StringComparison.Ordinal),
                Does.Not.Contain("\n"));
            Assert.That(component.TryGetProperty("description", out _), Is.False);
            Assert.That(component.GetProperty("properties").GetProperty("Nm")
                .TryGetProperty("description", out _), Is.False);
        });
    }

    [Test]
    public void InvalidSelectionAndResourceLimitsReturnStableErrors()
    {
        IJsonSchemaGenerator generator = new JsonSchemaGenerator();
        GenerationResult missing = generator.GenerateJsonSchema(null);
        GenerationResult noSources = generator.GenerateJsonSchema(new GenerationPlan());
        GenerationPlan selectionPlan = CreateRepositoryPlan(false);
        selectionPlan.Options.MessageIdentifiers.Add(new MessageIdentifier
        {
            Value = "pacs.999.001.01"
        });
        GenerationResult empty = generator.GenerateJsonSchema(selectionPlan);
        GenerationPlan boundedPlan = CreateRepositoryPlan(false);
        boundedPlan.Options.Limits.MaxTotalBytes = 32;
        GenerationResult bounded = generator.GenerateJsonSchema(boundedPlan);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.ErrorCode, Is.EqualTo("generation.plan.required"));
            Assert.That(noSources.Success, Is.False);
            Assert.That(noSources.ErrorCode, Is.EqualTo("generation.sources.required"));
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.ErrorCode,
                Is.EqualTo("generation.json_schema.selection_empty"));
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.ErrorCode,
                Is.EqualTo("generation.json_schema.byte_limit"));
        });
    }

    [Test]
    public void SchemaAndModelLimitsReturnTheirSpecificFailures()
    {
        GenerationPlan schemaPlan = CreateRepositoryPlan(false);
        MessageDefinitionMetadata second = CreateMessageDefinition();
        second.Identifier = "message-2";
        second.Name = "FinancialInstitutionCreditTransferV10";
        second.XmlName = second.Name;
        second.XmlTag = "FICdtTrf";
        second.NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.10";
        second.MessageIdentifier = new MessageIdentifier
        {
            Value = "pacs.009.001.10",
            BusinessArea = "pacs",
            MessageFunctionality = "009",
            Variant = "001",
            Version = "10"
        };
        schemaPlan.Repository?.MessageDefinitions.Add(second);
        schemaPlan.Options.Limits.MaxSchemaCount = 1;

        GenerationPlan modelPlan = CreateRepositoryPlan(false);
        modelPlan.Options.Limits.MaxCollectionCount = 1;

        GenerationResult schemas = new JsonSchemaGenerator().GenerateJsonSchema(schemaPlan);
        GenerationResult model = new JsonSchemaGenerator().GenerateJsonSchema(modelPlan);

        Assert.Multiple(() =>
        {
            Assert.That(schemas.ErrorCode,
                Is.EqualTo("generation.json_schema.schema_limit"));
            Assert.That(model.ErrorCode,
                Is.EqualTo("generation.json_schema.model_limit"));
        });
    }

    [Test]
    public void IncrementalOutputFailureIsPropagatedWithoutReplacingUserFile()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-json-schema-collision-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(
                root,
                "JsonSchemas",
                "pacs.008.001.10-CustomerCreditTransferV10.schema.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "user-owned", Encoding.UTF8);
            GenerationPlan plan = CreateRepositoryPlan(false);
            plan.Options.OutputRootPath = root;

            GenerationResult result = new JsonSchemaGenerator().GenerateJsonSchema(plan);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode,
                    Is.EqualTo("generation.incremental.unowned_collision"));
                Assert.That(File.ReadAllText(path, Encoding.UTF8), Is.EqualTo("user-owned"));
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Test]
    public void FileMetadataAndStemFallbacksRemainDeterministic()
    {
        JsonSchemaGenerator generator = new JsonSchemaGenerator();
        MessageDefinitionMetadata message = CreateMessageDefinition();
        XsdGenerationUnit messageUnit = new XsdGenerationUnit
        {
            Identity = "message-identity",
            Message = message,
            FileStem = "fallback"
        };
        XsdGenerationContext messageContext = new XsdGenerationContext
        {
            Unit = messageUnit
        };
        List<GeneratedFile> existing = new List<GeneratedFile>
        {
            new GeneratedFile
            {
                RelativePath =
                    "JsonSchemas/pacs.008.001.10-CustomerCreditTransferV10.schema.json"
            }
        };
        GeneratedFile collision = generator.CreateJsonSchemaFile(
            messageUnit,
            messageContext,
            new byte[] { 1, 2, 3 },
            existing);

        SchemaArtifact schema = new SchemaArtifact
        {
            SourceName = "payments schema.xsd"
        };
        schema.MessageDefinitions.Add(message);
        XsdGenerationUnit schemaUnit = new XsdGenerationUnit
        {
            Identity = "schema-identity",
            Schema = schema,
            FileStem = "schema-fallback"
        };
        XsdGenerationContext schemaContext = new XsdGenerationContext
        {
            Unit = schemaUnit
        };
        GeneratedFile schemaFile = generator.CreateJsonSchemaFile(
            schemaUnit,
            schemaContext,
            new byte[] { 4 },
            new List<GeneratedFile>());
        string schemaStem = generator.FindJsonSchemaFileStem(schemaUnit, schemaContext);
        schemaContext.Messages.Add(message);
        string contextMessageStem = generator.FindJsonSchemaFileStem(
            schemaUnit, schemaContext);

        MessageDefinitionMetadata identifierOnly = CreateMessageDefinition();
        identifierOnly.Identifier = string.Empty;
        identifierOnly.Name = string.Empty;
        identifierOnly.XmlName = string.Empty;
        identifierOnly.XmlTag = string.Empty;
        messageUnit.Message = identifierOnly;
        identifierOnly.MessageIdentifier = new MessageIdentifier();
        identifierOnly.Name = "Named Schema";
        string nameStem = generator.FindJsonSchemaFileStem(messageUnit, messageContext);
        messageUnit.Message = null;
        messageContext.Messages.Clear();
        SchemaArtifact unnamedSchema = new SchemaArtifact { SourceName = ".xsd" };
        messageUnit.Schema = unnamedSchema;
        messageUnit.FileStem = "fallback schema";
        string fallbackStem = generator.FindJsonSchemaFileStem(messageUnit, messageContext);

        Assert.Multiple(() =>
        {
            Assert.That(collision.RelativePath, Does.StartWith(
                "JsonSchemas/pacs.008.001.10-CustomerCreditTransferV10_"));
            Assert.That(collision.SourceIdentifiers, Is.EqualTo(new[] { "message-1" }));
            Assert.That(schemaFile.SourceIdentifiers, Is.EqualTo(new[] { "message-1" }));
            Assert.That(nameStem, Is.EqualTo("NamedSchema"));
            Assert.That(contextMessageStem,
                Is.EqualTo("pacs.008.001.10-CustomerCreditTransferV10"));
            Assert.That(schemaStem, Is.EqualTo("payments_schema"));
            Assert.That(fallbackStem, Is.EqualTo("fallback_schema"));
            Assert.That(generator.MakeJsonFileStemSafe(string.Empty), Is.EqualTo("schema"));
            Assert.That(generator.MakeJsonFileStemSafe("aZ-_.09 /"), Is.EqualTo("aZ-_.09__"));
        });
    }

    private static GenerationPlan CreateRepositoryPlan(bool shuffled)
    {
        RepositorySnapshot repository = new RepositorySnapshot
        {
            Identifier = "repository-2026",
            Edition = "2026"
        };
        repository.Provenance.Artifact.OriginUri = "file:///repository-2026.ecore";
        MessageDefinitionMetadata message = CreateMessageDefinition();
        repository.MessageDefinitions.Add(message);
        MessageBuildingBlockDefinition block = new MessageBuildingBlockDefinition
        {
            Identifier = "block-1",
            Name = "PaymentInformation",
            XmlTag = "PmtInf",
            Definition = "Payment information.",
            Multiplicity = CreateMultiplicity(1, true, 1),
            ComplexType = CreateReference(
                "component-1", RepositoryEntityKind.MessageComponent)
        };
        block.MemberType = block.ComplexType;
        repository.MessageBuildingBlocks.Add(block);

        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = "component-1",
            Name = "PaymentInstruction",
            Definition = "The payment instruction."
        };
        AddElementReference(component, "element-name", 0);
        AddElementReference(component, "element-status", 1);
        AddElementReference(component, "element-account", 2);
        AddElementReference(component, "element-supplementary", 3);
        AddElementReference(component, "element-date-time", 4);
        AddElementReference(component, "element-amount", 5);
        AddElementReference(component, "element-binary", 6);
        AddElementReference(component, "element-servicer", 7);
        AddElementReference(component, "element-additional", 8);
        AddElementReference(component, "element-date", 9);
        if (shuffled)
        {
            component.MessageElements.Reverse();
        }
        MessageComponentDefinition unusedComponent = new MessageComponentDefinition
        {
            Identifier = "component-unused",
            Name = "UnusedComponent"
        };
        if (shuffled)
        {
            repository.MessageComponents.Add(unusedComponent);
            repository.MessageComponents.Add(component);
        }
        else
        {
            repository.MessageComponents.Add(component);
            repository.MessageComponents.Add(unusedComponent);
        }

        repository.MessageElements.Add(CreateElement(
            "element-name", "Name", "Nm", "type-text", RepositoryEntityKind.DataType,
            CreateMultiplicity(1, true, 1), MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-status", "Status", "Sts", "codes-status", RepositoryEntityKind.CodeSet,
            CreateMultiplicity(1, true, 3), MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-account", "Account", "Acct", "choice-account", RepositoryEntityKind.Choice,
            CreateMultiplicity(1, true, 1), MessageElementKind.AssociationEnd, true));
        repository.MessageElements.Add(CreateElement(
            "element-supplementary", "SupplementaryData", "SplmtryData", "external-envelope",
            RepositoryEntityKind.ExternalSchema, CreateMultiplicity(0, true, 1),
            MessageElementKind.AssociationEnd, true));
        repository.MessageElements.Add(CreateElement(
            "element-date-time", "CreationDateTime", "CreDtTm", "type-date-time",
            RepositoryEntityKind.DataType, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-amount", "InterbankSettlementAmount", "IntrBkSttlmAmt", "type-amount",
            RepositoryEntityKind.DataType, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-binary", "SignatureData", "SgntrData", "type-binary",
            RepositoryEntityKind.DataType, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-servicer", "AccountServicer", "AcctSvcr", "component-unused",
            RepositoryEntityKind.MessageComponent, CreateMultiplicity(0, true, 1),
            MessageElementKind.AssociationEnd, false));
        repository.MessageElements.Add(CreateElement(
            "element-additional", "AdditionalInformation", "AddtlInf", "type-text",
            RepositoryEntityKind.DataType, CreateMultiplicity(0, false, 0),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-date", "SettlementDate", "SttlmDt", "type-date",
            RepositoryEntityKind.DataType, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "choice-iban", "Iban", "IBAN", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(0, true, 1), MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "choice-bic", "Bic", "BIC", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(0, true, 1), MessageElementKind.Attribute, true));
        if (shuffled)
        {
            repository.MessageElements.Reverse();
        }

        ChoiceDefinition choice = new ChoiceDefinition
        {
            Identifier = "choice-account",
            Name = "AccountChoice",
            ChoiceType = "ChoiceComponent",
            Definition = "Choice of account identifiers."
        };
        RepositoryReference ibanChoice = CreateReference(
            "choice-iban", RepositoryEntityKind.MessageElement);
        ibanChoice.Position = 0;
        RepositoryReference bicChoice = CreateReference(
            "choice-bic", RepositoryEntityKind.MessageElement);
        bicChoice.Position = 1;
        if (shuffled)
        {
            choice.MessageElements.Add(bicChoice);
            choice.MessageElements.Add(ibanChoice);
        }
        else
        {
            choice.MessageElements.Add(ibanChoice);
            choice.MessageElements.Add(bicChoice);
        }
        repository.Choices.Add(choice);

        DataTypeDefinition text = new DataTypeDefinition
        {
            Identifier = "type-text",
            Name = "Max35Text",
            Kind = DataTypeKind.String,
            Definition = "Text with a maximum length."
        };
        DataTypeFacet maxLength = IntegerFacet("maxLength", 35);
        DataTypeFacet pattern = new DataTypeFacet
        {
            Name = "pattern",
            Value = ".*\\S.*"
        };
        if (shuffled)
        {
            text.Facets.Add(pattern);
            text.Facets.Add(maxLength);
        }
        else
        {
            text.Facets.Add(maxLength);
            text.Facets.Add(pattern);
        }
        DataTypeDefinition dateTime = new DataTypeDefinition
        {
            Identifier = "type-date-time",
            Name = "ISODateTime",
            Kind = DataTypeKind.DateTime
        };
        DataTypeDefinition date = new DataTypeDefinition
        {
            Identifier = "type-date",
            Name = "ISODate",
            Kind = DataTypeKind.Date
        };
        DataTypeDefinition amount = new DataTypeDefinition
        {
            Identifier = "type-amount",
            Name = "ActiveCurrencyAndAmount",
            Kind = DataTypeKind.Amount,
            CurrencyIdentifierSet = CreateReference(
                "codes-currency", RepositoryEntityKind.CodeSet)
        };
        amount.Facets.Add(IntegerFacet("totalDigits", 18));
        amount.Facets.Add(IntegerFacet("fractionDigits", 5));
        amount.Facets.Add(new DataTypeFacet
        {
            Name = "minInclusive",
            Value = "0"
        });
        DataTypeDefinition binary = new DataTypeDefinition
        {
            Identifier = "type-binary",
            Name = "Max16Binary",
            Kind = DataTypeKind.Binary,
            BinaryEncoding = BinaryEncodingKind.Base64
        };
        binary.Facets.Add(IntegerFacet("length", 16));
        DataTypeDefinition unusedText = new DataTypeDefinition
        {
            Identifier = "type-unused",
            Name = "UnusedText",
            Kind = DataTypeKind.String
        };
        if (shuffled)
        {
            repository.DataTypes.Add(unusedText);
            repository.DataTypes.Add(binary);
            repository.DataTypes.Add(amount);
            repository.DataTypes.Add(date);
            repository.DataTypes.Add(dateTime);
            repository.DataTypes.Add(text);
        }
        else
        {
            repository.DataTypes.Add(text);
            repository.DataTypes.Add(dateTime);
            repository.DataTypes.Add(date);
            repository.DataTypes.Add(amount);
            repository.DataTypes.Add(binary);
            repository.DataTypes.Add(unusedText);
        }

        CodeSetDefinition statuses = new CodeSetDefinition
        {
            Identifier = "codes-status",
            Name = "PaymentStatusCode",
            Kind = CodeSetKind.Internal
        };
        CodeValueDefinition accepted = new CodeValueDefinition
        {
            Identifier = "code-accepted",
            Value = "ACCP"
        };
        CodeValueDefinition rejected = new CodeValueDefinition
        {
            Identifier = "code-rejected",
            Value = "RJCT"
        };
        if (shuffled)
        {
            statuses.Codes.Add(rejected);
            statuses.Codes.Add(accepted);
        }
        else
        {
            statuses.Codes.Add(accepted);
            statuses.Codes.Add(rejected);
        }
        CodeSetDefinition currencies = new CodeSetDefinition
        {
            Identifier = "codes-currency",
            Name = "ActiveCurrencyCode",
            Kind = CodeSetKind.External
        };
        currencies.Facets.Add(new DataTypeFacet
        {
            Name = "pattern",
            Value = "^[A-Z]{3}$"
        });
        if (shuffled)
        {
            repository.CodeSets.Add(currencies);
            repository.CodeSets.Add(statuses);
        }
        else
        {
            repository.CodeSets.Add(statuses);
            repository.CodeSets.Add(currencies);
        }
        repository.ExternalSchemas.Add(new ExternalSchemaDefinition
        {
            Identifier = "external-envelope",
            Name = "SupplementaryDataEnvelope1",
            Kind = DataTypeKind.ExternalSchema,
            Definition = "Open supplementary content.",
            NamespaceMode = "##other",
            ProcessContents = "LAX"
        });
        return new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026-06-26",
                IncludeDocumentation = true
            }
        };
    }

    private static MessageDefinitionMetadata CreateMessageDefinition()
    {
        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = "message-1",
            Name = "CustomerCreditTransferV10",
            Definition = "Transfers funds between financial institutions.",
            NamespaceUri = MessageNamespace,
            RootElement = "Document",
            XmlName = "CustomerCreditTransferV10",
            XmlTag = "FIToFICstmrCdtTrf",
            MessageIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10",
                BusinessArea = "pacs",
                MessageFunctionality = "008",
                Variant = "001",
                Version = "10"
            }
        };
        message.BuildingBlocks.Add(CreateReference(
            "block-1", RepositoryEntityKind.MessageBuildingBlock));
        return message;
    }

    private static void AddElementReference(
        MessageComponentDefinition component,
        string identifier,
        int index)
    {
        RepositoryReference reference = CreateReference(
            identifier, RepositoryEntityKind.MessageElement);
        reference.Position = index;
        component.MessageElements.Add(reference);
    }

    private static MessageElementDefinition CreateElement(
        string identifier,
        string name,
        string xmlTag,
        string targetIdentifier,
        RepositoryEntityKind targetKind,
        MultiplicityDefinition multiplicity,
        MessageElementKind kind,
        bool isComposite)
    {
        RepositoryReference type = CreateReference(targetIdentifier, targetKind);
        MessageElementDefinition element = new MessageElementDefinition
        {
            Identifier = identifier,
            Name = name,
            XmlTag = xmlTag,
            Definition = name + " definition.",
            Kind = kind,
            IsComposite = isComposite,
            MemberType = type,
            Multiplicity = multiplicity
        };
        if (targetKind == RepositoryEntityKind.DataType ||
            targetKind == RepositoryEntityKind.CodeSet)
        {
            element.SimpleType = type;
        }
        else
        {
            element.ComplexType = type;
        }
        return element;
    }

    private static RepositoryReference CreateReference(
        string targetIdentifier,
        RepositoryEntityKind targetKind)
    {
        return new RepositoryReference
        {
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            RawValue = targetIdentifier
        };
    }

    private static MultiplicityDefinition CreateMultiplicity(
        int minimum,
        bool hasMaximum,
        int maximum)
    {
        return new MultiplicityDefinition
        {
            MinimumOccurs = minimum,
            HasMaximumOccurs = hasMaximum,
            MaximumOccurs = maximum
        };
    }

    private static DataTypeFacet IntegerFacet(string name, int value)
    {
        return new DataTypeFacet
        {
            Name = name,
            Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            HasIntegerValue = true,
            IntegerValue = value
        };
    }
}
