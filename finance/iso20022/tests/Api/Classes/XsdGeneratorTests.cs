using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using iso20022;

namespace iso20022.Tests;

public class XsdGeneratorTests
{
    private const string MessageNamespace =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";

    [Test]
    public void CleanWorktreeRegenerationIsStableAcrossRepeatedAndShuffledRepositoryInput()
    {
        CleanWorktreeRegenerationAssertions.AssertDeterministic(
            "xsd",
            CreateRepositoryPlan,
            plan => new XsdGenerator().GenerateXsd(plan));
    }

    [Test]
    public void IncrementalGenerationReusesReplacesAndRemovesOnlyOwnedFiles()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-generation-" + Guid.NewGuid().ToString("N"));
        try
        {
            GenerationPlan firstPlan = CreateIncrementalPlan(root, false);
            GenerationResult first = new XsdGenerator().GenerateXsd(firstPlan);
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            string firstSchema = Path.Combine(root, "Schemas", "Pacs00800110.xsd");
            string unrelated = Path.Combine(root, "user-notes.txt");
            File.WriteAllText(unrelated, "keep this user file", Encoding.UTF8);

            GenerationPlan changedPlan = CreateIncrementalPlan(root, true);
            changedPlan.PreviousManifest = first.Manifest;
            GenerationResult changed = new XsdGenerator().GenerateXsd(changedPlan);
            Assert.That(changed.Success, Is.True, changed.ErrorMessage);
            string changedSchema = Path.Combine(root, "Schemas", "Pacs00900110.xsd");

            GenerationPlan repeatedPlan = CreateIncrementalPlan(root, true);
            repeatedPlan.PreviousManifest = changed.Manifest;
            GenerationResult repeated = new XsdGenerator().GenerateXsd(repeatedPlan);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(firstSchema), Is.False);
                Assert.That(File.Exists(changedSchema), Is.True);
                Assert.That(File.ReadAllText(unrelated, Encoding.UTF8),
                    Is.EqualTo("keep this user file"));
                Assert.That(repeated.Success, Is.True, repeated.ErrorMessage);
                Assert.That(repeated.Files, Has.All.Matches<GeneratedFile>(file =>
                    file.IsUnchanged));
                Assert.That(repeated.Files.Select(file =>
                        File.ReadAllBytes(Path.Combine(
                            root,
                            file.RelativePath.Replace('/', Path.DirectorySeparatorChar)))),
                    Is.EqualTo(repeated.Files.Select(file => file.Content)));
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
    public void IncrementalGenerationRejectsUnownedAndModifiedFileCollisions()
    {
        string unownedRoot = Path.Combine(
            Path.GetTempPath(),
            "iso20022-unowned-" + Guid.NewGuid().ToString("N"));
        string modifiedRoot = Path.Combine(
            Path.GetTempPath(),
            "iso20022-modified-" + Guid.NewGuid().ToString("N"));
        try
        {
            string unownedSchema = Path.Combine(
                unownedRoot, "Schemas", "Pacs00800110.xsd");
            Directory.CreateDirectory(Path.GetDirectoryName(unownedSchema)!);
            File.WriteAllText(unownedSchema, "user schema", Encoding.UTF8);
            GenerationResult unowned = new XsdGenerator().GenerateXsd(
                CreateIncrementalPlan(unownedRoot, false));

            GenerationPlan firstPlan = CreateIncrementalPlan(modifiedRoot, false);
            GenerationResult first = new XsdGenerator().GenerateXsd(firstPlan);
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            string modifiedSchema = Path.Combine(
                modifiedRoot, "Schemas", "Pacs00800110.xsd");
            File.WriteAllText(modifiedSchema, "user modification", Encoding.UTF8);
            GenerationPlan repeatedPlan = CreateIncrementalPlan(modifiedRoot, false);
            repeatedPlan.PreviousManifest = first.Manifest;
            GenerationResult modified = new XsdGenerator().GenerateXsd(repeatedPlan);

            Assert.Multiple(() =>
            {
                Assert.That(unowned.Success, Is.False);
                Assert.That(unowned.ErrorCode,
                    Is.EqualTo("generation.incremental.unowned_collision"));
                Assert.That(File.ReadAllText(unownedSchema, Encoding.UTF8),
                    Is.EqualTo("user schema"));
                Assert.That(modified.Success, Is.False);
                Assert.That(modified.ErrorCode,
                    Is.EqualTo("generation.incremental.owned_file_changed"));
                Assert.That(File.ReadAllText(modifiedSchema, Encoding.UTF8),
                    Is.EqualTo("user modification"));
            });
        }
        finally
        {
            if (Directory.Exists(unownedRoot))
            {
                Directory.Delete(unownedRoot, true);
            }
            if (Directory.Exists(modifiedRoot))
            {
                Directory.Delete(modifiedRoot, true);
            }
        }
    }

    [Test]
    public void IncrementalGenerationRejectsUnsafePreviousOwnedPaths()
    {
        string parent = Path.Combine(
            Path.GetTempPath(),
            "iso20022-traversal-" + Guid.NewGuid().ToString("N"));
        string root = Path.Combine(parent, "generated");
        string outside = Path.Combine(parent, "outside.txt");
        try
        {
            Directory.CreateDirectory(parent);
            File.WriteAllText(outside, "outside user file", Encoding.UTF8);
            GenerationPlan firstPlan = CreateIncrementalPlan(root, false);
            GenerationResult first = new XsdGenerator().GenerateXsd(firstPlan);
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            first.Manifest.Files.Add(new GeneratedFile
            {
                RelativePath = "../outside.txt",
                HashAlgorithm = "SHA-256"
            });
            GenerationPlan repeatedPlan = CreateIncrementalPlan(root, false);
            repeatedPlan.PreviousManifest = first.Manifest;

            GenerationResult result = new XsdGenerator().GenerateXsd(repeatedPlan);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode,
                    Is.EqualTo("generation.incremental.owned_path_invalid"));
                Assert.That(File.ReadAllText(outside, Encoding.UTF8),
                    Is.EqualTo("outside user file"));
            });
        }
        finally
        {
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, true);
            }
        }
    }

    [Test]
    public void RepositoryModelGeneratesCompilableSchemaForAllLogicalConstructs()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        IXsdGenerator generator = new XsdGenerator();

        GenerationResult result = generator.GenerateXsd(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Files, Has.Count.EqualTo(2));
        GeneratedFile file = result.Files.Single(value => value.Kind == GenerationFileKind.Xsd);
        string schemaText = Encoding.UTF8.GetString(file.Content);
        XmlSchemaSet compiled = CompileSchema(file.Content);
        Assert.Multiple(() =>
        {
            Assert.That(file.Kind, Is.EqualTo(GenerationFileKind.Xsd));
            Assert.That(file.RelativePath, Is.EqualTo("Schemas/Pacs00800110.xsd"));
            Assert.That(file.MediaType, Is.EqualTo(Iso20022MediaTypes.XmlSchema));
            Assert.That(file.NamespaceName, Is.EqualTo(MessageNamespace));
            Assert.That(file.Hash.Value, Has.Length.EqualTo(32));
            Assert.That(file.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(file.SourceIdentifiers, Is.EqualTo(new[] { "message-1" }));
            Assert.That(result.Manifest.Files.Single(), Is.SameAs(file));
            Assert.That(result.Manifest.SourceArtifacts.Single(),
                Is.SameAs(plan.Repository?.Provenance.Artifact));
            Assert.That(compiled.IsCompiled, Is.True);
            Assert.That(schemaText, Does.Contain("name=\"CustomerCreditTransferV10\""));
            Assert.That(schemaText, Does.Contain("name=\"PaymentInstruction\""));
            Assert.That(schemaText, Does.Not.Contain("name=\"UnusedComponent\""));
            Assert.That(schemaText, Does.Not.Contain("name=\"UnusedText\""));
            Assert.That(schemaText, Does.Contain("name=\"AccountChoice\""));
            Assert.That(schemaText, Does.Contain("name=\"PaymentStatusCode\""));
            Assert.That(schemaText, Does.Contain("value=\"ACCP\""));
            Assert.That(schemaText, Does.Contain("value=\"RJCT\""));
            Assert.That(schemaText, Does.Contain("maxOccurs=\"unbounded\""));
            Assert.That(schemaText, Does.Contain("<xs:choice>"));
            Assert.That(schemaText, Does.Contain("name=\"RoutingCode\""));
            Assert.That(schemaText, Does.Contain("name=\"ClearingCode\""));
            Assert.That(schemaText, Does.Contain("<xs:any namespace=\"##other\""));
            Assert.That(schemaText, Does.Contain("processContents=\"lax\""));
            Assert.That(schemaText, Does.Contain("Payment &amp; settlement details."));
        });
    }

    [Test]
    public void GeneratedSchemaEnforcesFacetsCodesChoicesOrderAndMultiplicity()
    {
        GenerationResult result = new XsdGenerator().GenerateXsd(CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        byte[] schema = result.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Content;
        string valid = CreateMessageXml("Invoice", "ACCP", true, 2);
        string badLength = CreateMessageXml(new string('x', 36), "ACCP", true, 1);
        string badCode = CreateMessageXml("Invoice", "NOPE", true, 1);
        string missingChoice = CreateMessageXml("Invoice", "ACCP", false, 1);
        string tooManyStatuses = CreateMessageXml("Invoice", "ACCP", true, 4);
        string oneLegacyAlternative = valid.Replace(
            "</PmtInf>",
            "<RoutingCode>RT01</RoutingCode></PmtInf>",
            StringComparison.Ordinal);
        string bothLegacyAlternatives = valid.Replace(
            "</PmtInf>",
            "<RoutingCode>RT01</RoutingCode><ClearingCode>CL01</ClearingCode></PmtInf>",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(ValidateMessage(schema, valid), Is.Empty);
            Assert.That(ValidateMessage(schema, badLength), Is.Not.Empty);
            Assert.That(ValidateMessage(schema, badCode), Is.Not.Empty);
            Assert.That(ValidateMessage(schema, missingChoice), Is.Not.Empty);
            Assert.That(ValidateMessage(schema, tooManyStatuses), Is.Not.Empty);
            Assert.That(ValidateMessage(schema, oneLegacyAlternative), Is.Empty);
            Assert.That(ValidateMessage(schema, bothLegacyAlternatives), Is.Not.Empty);
        });
    }

    [Test]
    public void GenerationIsStableAcrossUnorderedTypesFacetsAndCodes()
    {
        IXsdGenerator generator = new XsdGenerator();
        GenerationResult first = generator.GenerateXsd(CreateRepositoryPlan(false));
        GenerationResult shuffled = generator.GenerateXsd(CreateRepositoryPlan(true));
        GenerationResult repeated = generator.GenerateXsd(CreateRepositoryPlan(false));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(shuffled.Success, Is.True, shuffled.ErrorMessage);
            Assert.That(repeated.Success, Is.True, repeated.ErrorMessage);
            Assert.That(shuffled.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Content,
                Is.EqualTo(first.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Content));
            Assert.That(repeated.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Content,
                Is.EqualTo(first.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Content));
            Assert.That(shuffled.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Hash.Value,
                Is.EqualTo(first.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Hash.Value));
        });
    }

    [Test]
    public void NormalizedSchemaGenerationPreservesNamespaceIncludesImportsAndRootType()
    {
        SchemaArtifact schema = CreateNormalizedSchemaArtifact();
        GenerationPlan plan = new GenerationPlan();
        plan.Schemas.Add(schema);
        plan.Options.IncludeDocumentation = false;

        GenerationResult result = new XsdGenerator().GenerateXsd(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        XDocument document = XDocument.Parse(Encoding.UTF8.GetString(
            result.Files.Single(value => value.Kind == GenerationFileKind.Xsd).Content));
        XNamespace xs = Iso20022Namespaces.XmlSchema;
        XElement root = document.Root!;
        XElement[] includes = root.Elements(xs + "include").ToArray();
        XElement[] imports = root.Elements(xs + "import").ToArray();
        XElement global = root.Elements(xs + "element").Single();
        Assert.Multiple(() =>
        {
            Assert.That(root.Attribute("targetNamespace")?.Value,
                Is.EqualTo("urn:example:payments"));
            Assert.That(root.Attribute("elementFormDefault")?.Value, Is.EqualTo("qualified"));
            Assert.That(includes.Select(value => value.Attribute("schemaLocation")?.Value),
                Is.EqualTo(new[] { "common-a.xsd", "common-b.xsd" }));
            Assert.That(imports.Select(value => value.Attribute("namespace")?.Value),
                Is.EqualTo(new[] { "urn:example:a", "urn:example:z" }));
            Assert.That(global.Attribute("name")?.Value, Is.EqualTo("Document"));
            Assert.That(global.Attribute("type")?.Value, Is.EqualTo("tns:DocumentType"));
            Assert.That(result.Files.Single(value => value.Kind == GenerationFileKind.Xsd).RelativePath,
                Is.EqualTo("Schemas/Payments.xsd"));
        });
    }

    [Test]
    public void SelectionDeprecationAndResourceLimitsReturnStableOutcomes()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        MessageDefinitionMetadata second = CreateMessageDefinition(
            "message-2",
            "pacs.009.001.10",
            "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.10");
        second.Registration.IsDeprecated = true;
        plan.Repository?.MessageDefinitions.Add(second);
        plan.Options.NamespaceUris.Add(MessageNamespace);
        GenerationResult selected = new XsdGenerator().GenerateXsd(plan);
        plan.Options.NamespaceUris.Clear();
        plan.Options.MessageIdentifiers.Add(new MessageIdentifier
        {
            Value = "pacs.009.001.10"
        });
        GenerationResult deprecated = new XsdGenerator().GenerateXsd(plan);
        plan.Options.IncludeDeprecated = true;
        GenerationResult included = new XsdGenerator().GenerateXsd(plan);
        plan.Options.Limits.MaxTotalBytes = 16;
        GenerationResult bounded = new XsdGenerator().GenerateXsd(plan);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Success, Is.True, selected.ErrorMessage);
            Assert.That(selected.Files, Has.Count.EqualTo(2));
            Assert.That(selected.Files.Single(value => value.Kind == GenerationFileKind.Xsd)
                .NamespaceName, Is.EqualTo(MessageNamespace));
            Assert.That(deprecated.Success, Is.False);
            Assert.That(deprecated.ErrorCode, Is.EqualTo("generation.xsd.selection_empty"));
            Assert.That(included.Success, Is.True, included.ErrorMessage);
            Assert.That(included.Files.Single(value => value.Kind == GenerationFileKind.Xsd)
                .NamespaceName,
                Is.EqualTo(second.NamespaceUri));
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.ErrorCode, Is.EqualTo("generation.xsd.byte_limit"));
        });
    }

    [Test]
    public void InvalidPlansReturnExceptionlessErrors()
    {
        IXsdGenerator generator = new XsdGenerator();
        GenerationResult missingPlan = generator.GenerateXsd(null);
        GenerationResult missingSources = generator.GenerateXsd(new GenerationPlan());
        GenerationPlan invalidLimitsPlan = CreateRepositoryPlan(false);
        invalidLimitsPlan.Options.Limits.MaxSchemaCount = 0;
        GenerationResult invalidLimits = generator.GenerateXsd(invalidLimitsPlan);

        Assert.Multiple(() =>
        {
            Assert.That(missingPlan.Success, Is.False);
            Assert.That(missingPlan.ErrorCode, Is.EqualTo("generation.plan.required"));
            Assert.That(missingSources.Success, Is.False);
            Assert.That(missingSources.ErrorCode, Is.EqualTo("generation.sources.required"));
            Assert.That(invalidLimits.Success, Is.False);
            Assert.That(invalidLimits.ErrorCode, Is.EqualTo("generation.limits.invalid"));
        });
    }

    [Test]
    public void PlanValidationCoversEveryMissingValueAndCollectionLimit()
    {
        XsdGenerator generator = new XsdGenerator();
        List<GenerationPlan> missingValues = new List<GenerationPlan>();
        GenerationPlan missingOptions = CreateRepositoryPlan(false);
        missingOptions.Options = null!;
        missingValues.Add(missingOptions);
        GenerationPlan missingSchemas = CreateRepositoryPlan(false);
        missingSchemas.Schemas = null!;
        missingValues.Add(missingSchemas);
        GenerationPlan missingIdentifiers = CreateRepositoryPlan(false);
        missingIdentifiers.Options.MessageIdentifiers = null!;
        missingValues.Add(missingIdentifiers);
        GenerationPlan missingNamespaces = CreateRepositoryPlan(false);
        missingNamespaces.Options.NamespaceUris = null!;
        missingValues.Add(missingNamespaces);
        GenerationPlan missingLimits = CreateRepositoryPlan(false);
        missingLimits.Options.Limits = null!;
        missingValues.Add(missingLimits);
        GenerationPlan missingIndentation = CreateRepositoryPlan(false);
        missingIndentation.Options.Indentation = null!;
        missingValues.Add(missingIndentation);
        GenerationPlan missingNewLine = CreateRepositoryPlan(false);
        missingNewLine.Options.NewLine = null!;
        missingValues.Add(missingNewLine);
        GenerationPlan missingEdition = CreateRepositoryPlan(false);
        missingEdition.Options.ArtifactEdition = null!;
        missingValues.Add(missingEdition);
        GenerationPlan missingProfile = CreateRepositoryPlan(false);
        missingProfile.Options.Profile = null!;
        missingValues.Add(missingProfile);
        GenerationPlan missingRootNamespace = CreateRepositoryPlan(false);
        missingRootNamespace.Options.RootNamespace = null!;
        missingValues.Add(missingRootNamespace);
        GenerationPlan missingOutputRoot = CreateRepositoryPlan(false);
        missingOutputRoot.Options.OutputRootPath = null!;
        missingValues.Add(missingOutputRoot);

        GenerationPlan schemaLimit = CreateRepositoryPlan(false);
        schemaLimit.Schemas.Add(CreateNormalizedSchemaArtifact());
        schemaLimit.Schemas.Add(CreateNormalizedSchemaArtifact());
        schemaLimit.Options.Limits.MaxCollectionCount = 1;
        GenerationPlan identifierLimit = CreateRepositoryPlan(false);
        identifierLimit.Options.MessageIdentifiers.Add(new MessageIdentifier());
        identifierLimit.Options.MessageIdentifiers.Add(new MessageIdentifier());
        identifierLimit.Options.Limits.MaxCollectionCount = 1;
        GenerationPlan namespaceLimit = CreateRepositoryPlan(false);
        namespaceLimit.Options.NamespaceUris.Add("urn:one");
        namespaceLimit.Options.NamespaceUris.Add("urn:two");
        namespaceLimit.Options.Limits.MaxCollectionCount = 1;

        Assert.Multiple(() =>
        {
            Assert.That(missingValues.Select(generator.ValidateGenerationPlan)
                .Select(result => result?.ErrorCode),
                Is.All.EqualTo("generation.plan.invalid"));
            Assert.That(generator.ValidateGenerationPlan(schemaLimit)?.ErrorCode,
                Is.EqualTo("generation.collection.limit"));
            Assert.That(generator.ValidateGenerationPlan(identifierLimit)?.ErrorCode,
                Is.EqualTo("generation.collection.limit"));
            Assert.That(generator.ValidateGenerationPlan(namespaceLimit)?.ErrorCode,
                Is.EqualTo("generation.collection.limit"));
        });
    }

    [Test]
    public void GenerationReportsSchemaNamespaceAndModelLimits()
    {
        GenerationPlan schemaPlan = CreateRepositoryPlan(false);
        MessageDefinitionMetadata second = CreateMessageDefinition(
            "message-2",
            "pacs.009.001.10",
            "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.10");
        schemaPlan.Repository?.MessageDefinitions.Add(second);
        schemaPlan.Options.Limits.MaxSchemaCount = 1;

        GenerationPlan namespacePlan = CreateRepositoryPlan(false);
        namespacePlan.Repository!.MessageDefinitions[0].NamespaceUri = "not a namespace";

        GenerationPlan modelPlan = CreateRepositoryPlan(false);
        modelPlan.Options.Limits.MaxCollectionCount = 1;

        GenerationPlan emptyNamespacePlan = new GenerationPlan();
        emptyNamespacePlan.Schemas.Add(new SchemaArtifact
        {
            SourceName = "no-namespace.xsd"
        });

        GenerationResult schemas = new XsdGenerator().GenerateXsd(schemaPlan);
        GenerationResult namespaceResult = new XsdGenerator().GenerateXsd(namespacePlan);
        GenerationResult model = new XsdGenerator().GenerateXsd(modelPlan);
        GenerationResult emptyNamespace = new XsdGenerator().GenerateXsd(
            emptyNamespacePlan);

        Assert.Multiple(() =>
        {
            Assert.That(schemas.ErrorCode, Is.EqualTo("generation.xsd.schema_limit"));
            Assert.That(namespaceResult.ErrorCode,
                Is.EqualTo("generation.xsd.namespace_invalid"));
            Assert.That(model.ErrorCode, Is.EqualTo("generation.xsd.model_limit"));
            Assert.That(emptyNamespace.Success, Is.True, emptyNamespace.ErrorMessage);
        });
    }

    [Test]
    public void ManifestInitializationHashesNonemptySchemaContentOnce()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        SchemaArtifact unhashed = CreateNormalizedSchemaArtifact();
        unhashed.Content = "schema"u8.ToArray();
        SchemaArtifact alreadyHashed = CreateNormalizedSchemaArtifact();
        alreadyHashed.Content = "other"u8.ToArray();
        alreadyHashed.Artifact.Hash.Value = new byte[] { 1 };
        plan.Schemas.Add(unhashed);
        plan.Schemas.Add(alreadyHashed);
        GenerationResult result = new GenerationResult();

        new XsdGenerator().InitializeManifest(result, plan);

        Assert.Multiple(() =>
        {
            Assert.That(unhashed.Artifact.Hash.Value, Has.Length.EqualTo(32));
            Assert.That(unhashed.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(alreadyHashed.Artifact.Hash.Value, Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.Manifest.SourceArtifacts, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void SelectionIdentityNamingAndOrderingHelpersCoverAllOutcomes()
    {
        XsdGenerator generator = new XsdGenerator();
        GenerationOptions options = new GenerationOptions();
        SchemaArtifact schema = CreateNormalizedSchemaArtifact();
        schema.MessageIdentifier = new MessageIdentifier { Value = "pacs.008.001.10" };
        MessageDefinitionMetadata definition = CreateMessageDefinition(
            "message-1", "pacs.009.001.10", "urn:message");
        schema.MessageDefinitions.Add(definition);

        options.NamespaceUris.Add("urn:different");
        bool wrongNamespace = generator.SchemaIsSelected(schema, options);
        options.NamespaceUris.Clear();
        bool noIdentifiers = generator.SchemaIsSelected(schema, options);
        options.MessageIdentifiers.Add(new MessageIdentifier { Value = "pacs.008.001.10" });
        bool schemaIdentifier = generator.SchemaIsSelected(schema, options);
        options.MessageIdentifiers[0].Value = "pacs.009.001.10";
        schema.MessageIdentifier.Value = "pacs.010.001.10";
        bool definitionIdentifier = generator.SchemaIsSelected(schema, options);
        options.MessageIdentifiers[0].Value = "pacs.011.001.10";
        bool noIdentifier = generator.SchemaIsSelected(schema, options);
        schema.MessageIdentifier = null;
        bool absentSchemaIdentifier = generator.SchemaIsSelected(schema, options);

        MessageDefinitionMetadata message = CreateMessageDefinition(
            "message", "pacs.008.001.10", "urn:message");
        message.Registration.IsDeprecated = true;
        bool deprecated = generator.MessageIsSelected(message, new GenerationOptions());
        GenerationOptions messageOptions = new GenerationOptions
        {
            IncludeDeprecated = true
        };
        messageOptions.NamespaceUris.Add("urn:different");
        bool messageWrongNamespace = generator.MessageIsSelected(message, messageOptions);

        MessageIdentifier components = new MessageIdentifier
        {
            BusinessArea = "camt",
            MessageFunctionality = "053",
            Variant = "001",
            Version = "10"
        };
        MessageDefinitionMetadata identity = new MessageDefinitionMetadata
        {
            Identifier = "repository-id",
            Name = "Repository Name",
            MessageIdentifier = new MessageIdentifier()
        };
        string repositoryIdentity = generator.FindMessageIdentity(identity);
        identity.Identifier = string.Empty;
        string nameIdentity = generator.FindMessageIdentity(identity);

        SchemaArtifact identifierStemSchema = new SchemaArtifact
        {
            SourceName = ".xsd",
            NamespaceUri = "urn:identifier",
            MessageIdentifier = components
        };
        SchemaArtifact hashStemSchema = new SchemaArtifact
        {
            SourceName = ".xsd",
            NamespaceUri = "urn:hash"
        };

        List<XsdGenerationUnit> units = new List<XsdGenerationUnit>
        {
            new XsdGenerationUnit { Identity = "b", NamespaceUri = "urn:b" },
            new XsdGenerationUnit
            {
                Identity = "a",
                NamespaceUri = "urn:a",
                Schema = new SchemaArtifact()
            },
            new XsdGenerationUnit { Identity = "c", NamespaceUri = "urn:c" }
        };
        bool schemaNamespace = generator.SchemaNamespaceExists(units, "urn:a");
        bool nonSchemaNamespace = generator.SchemaNamespaceExists(units, "urn:b");
        bool absentNamespace = generator.SchemaNamespaceExists(units, "urn:missing");
        generator.SortGenerationUnits(units);

        Assert.Multiple(() =>
        {
            Assert.That(wrongNamespace, Is.False);
            Assert.That(noIdentifiers, Is.True);
            Assert.That(schemaIdentifier, Is.True);
            Assert.That(definitionIdentifier, Is.True);
            Assert.That(noIdentifier, Is.False);
            Assert.That(absentSchemaIdentifier, Is.False);
            Assert.That(deprecated, Is.False);
            Assert.That(messageWrongNamespace, Is.False);
            Assert.That(generator.NamespaceIsSelected("urn:a", new List<string>()), Is.True);
            Assert.That(generator.NamespaceIsSelected(
                "urn:a", new List<string> { "urn:b", "urn:a" }), Is.True);
            Assert.That(generator.NamespaceIsSelected(
                "urn:a", new List<string> { "urn:b" }), Is.False);
            Assert.That(generator.MessageIdentifierIsSelected(
                components, new List<MessageIdentifier>()), Is.True);
            Assert.That(generator.MessageIdentifierIsSelected(
                components, new List<MessageIdentifier> { new MessageIdentifier { Value = "camt.053.001.10" } }), Is.True);
            Assert.That(generator.MessageIdentifierIsSelected(
                components, new List<MessageIdentifier> { new MessageIdentifier { Value = "pacs.008.001.10" } }), Is.False);
            Assert.That(generator.FindIdentifierValue(components), Is.EqualTo("camt.053.001.10"));
            Assert.That(repositoryIdentity, Is.EqualTo("repository-id"));
            Assert.That(nameIdentity, Is.EqualTo("Repository Name"));
            Assert.That(generator.FindSchemaFileStem(identifierStemSchema),
                Is.EqualTo("Camt05300110"));
            Assert.That(generator.FindSchemaFileStem(hashStemSchema), Does.StartWith("Schema"));
            Assert.That(schemaNamespace, Is.True);
            Assert.That(nonSchemaNamespace, Is.False);
            Assert.That(absentNamespace, Is.False);
            Assert.That(units.Select(unit => unit.Identity), Is.EqualTo(new[] { "a", "b", "c" }));
        });
    }

    [Test]
    public void GeneratedFileCollisionAndPathLookupAreDeterministic()
    {
        XsdGenerator generator = new XsdGenerator();
        MessageDefinitionMetadata message = CreateMessageDefinition(
            "message", "pacs.008.001.10", MessageNamespace);
        SchemaArtifact schema = new SchemaArtifact();
        schema.MessageDefinitions.Add(message);
        XsdGenerationUnit unit = new XsdGenerationUnit
        {
            Identity = "unit",
            NamespaceUri = MessageNamespace,
            FileStem = "Document",
            Schema = schema,
            Message = message
        };
        List<GeneratedFile> existing = new List<GeneratedFile>
        {
            new GeneratedFile { RelativePath = "Schemas/Document.xsd" },
            new GeneratedFile { RelativePath = "Schemas/Other.xsd" }
        };

        GeneratedFile file = generator.CreateGeneratedFile(
            unit, new byte[] { 1, 2 }, existing);
        GenerationResult withoutPlan = generator.CreateGenerationFailure(
            null, "code", "message");

        Assert.Multiple(() =>
        {
            Assert.That(file.RelativePath, Does.StartWith("Schemas/Document_"));
            Assert.That(file.SourceIdentifiers, Is.EqualTo(new[] { "message", "message" }));
            Assert.That(generator.OutputPathExists(existing, "Schemas/Other.xsd"), Is.True);
            Assert.That(generator.OutputPathExists(existing, "Schemas/Missing.xsd"), Is.False);
            Assert.That(withoutPlan.ErrorCode, Is.EqualTo("code"));
        });
    }

    private static GenerationPlan CreateIncrementalPlan(string root, bool changed)
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        plan.Options.OutputRootPath = root;
        if (changed)
        {
            MessageDefinitionMetadata message = plan.Repository!.MessageDefinitions.Single();
            message.MessageIdentifier.Value = "pacs.009.001.10";
            message.MessageIdentifier.MessageFunctionality = "009";
            message.NamespaceUri =
                "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.10";
        }
        return plan;
    }

    private static GenerationPlan CreateRepositoryPlan(bool shuffled)
    {
        RepositorySnapshot repository = new RepositorySnapshot
        {
            Identifier = "repository-2026",
            Edition = "2026"
        };
        repository.Provenance.Artifact.OriginUri = "file:///repository-2026.ecore";
        MessageDefinitionMetadata message = CreateMessageDefinition(
            "message-1",
            "pacs.008.001.10",
            MessageNamespace);
        message.Name = "CustomerCreditTransfer";
        message.Definition = "Payment & settlement details.";
        message.XmlName = "CustomerCreditTransferV10";
        message.BuildingBlocks.Add(CreateReference(
            "message-1", RepositoryEntityKind.MessageDefinition, "BuildingBlocks", 0,
            "block-1", RepositoryEntityKind.MessageBuildingBlock));
        repository.MessageDefinitions.Add(message);
        MessageBuildingBlockDefinition block = new MessageBuildingBlockDefinition
        {
            Identifier = "block-1",
            Name = "PaymentInformation",
            XmlTag = "PmtInf",
            Definition = "Payment information.",
            Multiplicity = CreateMultiplicity(1, true, 1),
            ComplexType = CreateReference("block-1", RepositoryEntityKind.MessageBuildingBlock,
                "ComplexType", 0, "component-1", RepositoryEntityKind.MessageComponent)
        };
        block.MemberType = block.ComplexType;
        repository.MessageBuildingBlocks.Add(block);

        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = "component-1",
            Name = "PaymentInstruction",
            Definition = "The payment instruction."
        };
        AddComponentElement(component, "element-name", 0);
        AddComponentElement(component, "element-status", 1);
        AddComponentElement(component, "element-account", 2);
        AddComponentElement(component, "element-supplementary", 3);
        AddComponentElement(component, "element-routing", 4);
        AddComponentElement(component, "element-clearing", 5);
        MessageComponentDefinition unused = new MessageComponentDefinition
        {
            Identifier = "component-unused",
            Name = "UnusedComponent"
        };
        if (shuffled)
        {
            repository.MessageComponents.Add(unused);
            repository.MessageComponents.Add(component);
        }
        else
        {
            repository.MessageComponents.Add(component);
            repository.MessageComponents.Add(unused);
        }

        repository.MessageElements.Add(CreateElement(
            "element-name", "Name", "Nm", "type-text", RepositoryEntityKind.DataType,
            CreateMultiplicity(1, true, 1)));
        repository.MessageElements.Add(CreateElement(
            "element-status", "Status", "Sts", "codes-status", RepositoryEntityKind.CodeSet,
            CreateMultiplicity(1, true, 3)));
        repository.MessageElements.Add(CreateElement(
            "element-account", "Account", "Acct", "choice-account", RepositoryEntityKind.Choice,
            CreateMultiplicity(1, true, 1)));
        repository.MessageElements.Add(CreateElement(
            "element-supplementary", "SupplementaryData", "SplmtryData", "external-envelope",
            RepositoryEntityKind.ExternalSchema, CreateMultiplicity(0, true, 1)));
        repository.MessageElements.Add(CreateElement(
            "element-routing", "RoutingCode", "RoutingCode", "xs:string",
            RepositoryEntityKind.Unknown, CreateMultiplicity(0, true, 1)));
        repository.MessageElements.Add(CreateElement(
            "element-clearing", "ClearingCode", "ClearingCode", "xs:string",
            RepositoryEntityKind.Unknown, CreateMultiplicity(0, true, 1)));
        repository.MessageElements.Add(CreateElement(
            "choice-iban", "Iban", "IBAN", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(1, true, 1)));
        repository.MessageElements.Add(CreateElement(
            "choice-bic", "Bic", "BIC", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(1, true, 1)));

        ChoiceDefinition choice = new ChoiceDefinition
        {
            Identifier = "choice-account",
            Name = "AccountChoice",
            ChoiceType = "ChoiceComponent"
        };
        choice.MessageElements.Add(CreateReference(
            choice.Identifier, RepositoryEntityKind.Choice, "MessageElements", 0,
            "choice-iban", RepositoryEntityKind.MessageElement));
        choice.MessageElements.Add(CreateReference(
            choice.Identifier, RepositoryEntityKind.Choice, "MessageElements", 1,
            "choice-bic", RepositoryEntityKind.MessageElement));
        repository.Choices.Add(choice);
        ChoiceDefinition legacyChoice = new ChoiceDefinition
        {
            Identifier = "choice-routing-xor",
            Name = "RoutingChoice",
            ChoiceType = "Xor"
        };
        legacyChoice.ImpactedMessageElements.Add(CreateReference(
            legacyChoice.Identifier, RepositoryEntityKind.Choice,
            "ImpactedMessageElements", 0, "element-routing",
            RepositoryEntityKind.MessageElement));
        legacyChoice.ImpactedMessageElements.Add(CreateReference(
            legacyChoice.Identifier, RepositoryEntityKind.Choice,
            "ImpactedMessageElements", 1, "element-clearing",
            RepositoryEntityKind.MessageElement));
        repository.Choices.Add(legacyChoice);
        component.Choices.Add(CreateReference(
            component.Identifier, RepositoryEntityKind.MessageComponent,
            "Choices", 0, legacyChoice.Identifier, RepositoryEntityKind.Choice));

        DataTypeDefinition text = new DataTypeDefinition
        {
            Identifier = "type-text",
            Name = "Max35Text",
            Kind = DataTypeKind.String,
            Definition = "Text with a maximum length."
        };
        DataTypeFacet maxLength = new DataTypeFacet
        {
            Name = "maxLength",
            Value = "35",
            HasIntegerValue = true,
            IntegerValue = 35
        };
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
        DataTypeDefinition unusedType = new DataTypeDefinition
        {
            Identifier = "type-unused",
            Name = "UnusedText",
            Kind = DataTypeKind.String
        };
        if (shuffled)
        {
            repository.DataTypes.Add(unusedType);
            repository.DataTypes.Add(text);
        }
        else
        {
            repository.DataTypes.Add(text);
            repository.DataTypes.Add(unusedType);
        }

        CodeSetDefinition codes = new CodeSetDefinition
        {
            Identifier = "codes-status",
            Name = "PaymentStatusCode",
            Kind = CodeSetKind.Internal
        };
        CodeValueDefinition accepted = new CodeValueDefinition
        {
            Identifier = "code-accepted",
            Value = "ACCP",
            Name = "Accepted"
        };
        CodeValueDefinition rejected = new CodeValueDefinition
        {
            Identifier = "code-rejected",
            Value = "RJCT",
            Name = "Rejected"
        };
        if (shuffled)
        {
            codes.Codes.Add(rejected);
            codes.Codes.Add(accepted);
        }
        else
        {
            codes.Codes.Add(accepted);
            codes.Codes.Add(rejected);
        }
        repository.CodeSets.Add(codes);
        repository.ExternalSchemas.Add(new ExternalSchemaDefinition
        {
            Identifier = "external-envelope",
            Name = "SupplementaryEnvelope",
            Kind = DataTypeKind.ExternalSchema,
            NamespaceMode = "##other",
            ProcessContents = "LAX"
        });
        return new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026",
                IncludeDocumentation = true
            }
        };
    }

    private static MessageDefinitionMetadata CreateMessageDefinition(
        string identifier,
        string messageIdentifier,
        string namespaceUri)
    {
        string[] parts = messageIdentifier.Split('.');
        return new MessageDefinitionMetadata
        {
            Identifier = identifier,
            Name = identifier,
            NamespaceUri = namespaceUri,
            RootElement = "Document",
            XmlName = "DocumentType",
            MessageIdentifier = new MessageIdentifier
            {
                Value = messageIdentifier,
                BusinessArea = parts[0],
                MessageFunctionality = parts[1],
                Variant = parts[2],
                Version = parts[3]
            }
        };
    }

    private static MessageElementDefinition CreateElement(
        string identifier,
        string name,
        string xmlTag,
        string targetIdentifier,
        RepositoryEntityKind targetKind,
        MultiplicityDefinition multiplicity)
    {
        RepositoryReference type = CreateReference(
            identifier, RepositoryEntityKind.MessageElement, "MemberType", 0,
            targetIdentifier, targetKind);
        MessageElementDefinition element = new MessageElementDefinition
        {
            Identifier = identifier,
            Name = name,
            XmlTag = xmlTag,
            MemberType = type,
            Multiplicity = multiplicity
        };
        if (targetKind == RepositoryEntityKind.DataType ||
            targetKind == RepositoryEntityKind.CodeSet)
        {
            element.SimpleType = type;
        }
        else if (targetKind != RepositoryEntityKind.Unknown)
        {
            element.ComplexType = type;
        }
        return element;
    }

    private static void AddComponentElement(
        MessageComponentDefinition component,
        string identifier,
        int position)
    {
        component.MessageElements.Add(CreateReference(
            component.Identifier,
            RepositoryEntityKind.MessageComponent,
            "MessageElements",
            position,
            identifier,
            RepositoryEntityKind.MessageElement));
    }

    private static RepositoryReference CreateReference(
        string sourceIdentifier,
        RepositoryEntityKind sourceKind,
        string propertyName,
        int position,
        string targetIdentifier,
        RepositoryEntityKind targetKind)
    {
        return new RepositoryReference
        {
            SourceIdentifier = sourceIdentifier,
            SourceKind = sourceKind,
            PropertyName = propertyName,
            Position = position,
            TargetIdentifier = targetIdentifier,
            TargetKind = targetKind,
            IsResolved = true
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

    private static SchemaArtifact CreateNormalizedSchemaArtifact()
    {
        SchemaArtifact schema = new SchemaArtifact
        {
            SourceName = "payments.xsd",
            NamespaceUri = "urn:example:payments",
            RootElement = "Document"
        };
        schema.IncludeLocations.Add("common-b.xsd");
        schema.IncludeLocations.Add("common-a.xsd");
        schema.ImportNamespaceUris.Add("urn:example:z");
        schema.ImportLocations.Add("z.xsd");
        schema.ImportNamespaceUris.Add("urn:example:a");
        schema.ImportLocations.Add("a.xsd");
        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = "DocumentType",
            Name = "DocumentType"
        };
        component.MessageElements.Add(CreateReference(
            component.Identifier, RepositoryEntityKind.MessageComponent,
            "MessageElements", 0, "document-name", RepositoryEntityKind.MessageElement));
        schema.MessageComponents.Add(component);
        schema.MessageElements.Add(CreateElement(
            "document-name", "Name", "Nm", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(1, true, 1)));
        schema.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            Identifier = "Document",
            Name = "Document",
            NamespaceUri = schema.NamespaceUri,
            RootElement = "Document",
            XmlName = "DocumentType"
        });
        return schema;
    }

    private static XmlSchemaSet CompileSchema(byte[] content)
    {
        XmlSchemaSet schemas = new XmlSchemaSet();
        using MemoryStream input = new MemoryStream(content);
        using XmlReader reader = XmlReader.Create(input);
        schemas.Add(null, reader);
        schemas.Compile();
        return schemas;
    }

    private static List<string> ValidateMessage(byte[] schema, string xml)
    {
        XmlSchemaSet schemas = CompileSchema(schema);
        List<string> errors = new List<string>();
        XmlReaderSettings settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemas
        };
        settings.ValidationEventHandler += (_, args) => errors.Add(args.Message);
        using StringReader input = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(input, settings);
        while (reader.Read())
        {
        }
        return errors;
    }

    private static string CreateMessageXml(
        string name,
        string status,
        bool includeChoice,
        int statusCount)
    {
        StringBuilder xml = new StringBuilder();
        xml.Append("<Document xmlns=\"");
        xml.Append(MessageNamespace);
        xml.Append("\" xmlns:ext=\"urn:example:extension\"><PmtInf><Nm>");
        xml.Append(name);
        xml.Append("</Nm>");
        int index = 0;
        while (index < statusCount)
        {
            xml.Append("<Sts>");
            xml.Append(status);
            xml.Append("</Sts>");
            index++;
        }
        if (includeChoice)
        {
            xml.Append("<Acct><IBAN>DE123</IBAN></Acct>");
        }
        xml.Append("<SplmtryData><ext:Extension/></SplmtryData></PmtInf></Document>");
        return xml.ToString();
    }
}
