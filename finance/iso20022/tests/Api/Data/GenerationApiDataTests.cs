using iso20022;

namespace iso20022.Tests;

public class GenerationApiDataTests
{
    [Test]
    public void GenerationPlanCarriesExactSourcesSelectionSettingsAndPreviousOwnership()
    {
        RepositorySnapshot repository = new RepositorySnapshot
        {
            Identifier = "repository-2026",
            Edition = "2026"
        };
        SchemaArtifact schema = new SchemaArtifact
        {
            SourceName = "pacs.008.001.10.xsd",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"
        };
        MessageIdentifier identifier = new MessageIdentifier
        {
            Value = "pacs.008.001.10",
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10"
        };
        GenerationManifest previous = new GenerationManifest
        {
            ArtifactEdition = "2025",
            RootNamespace = "Bank.Messages"
        };
        GenerationOptions options = new GenerationOptions
        {
            RootNamespace = "Bank.Messages",
            OutputRootPath = "/generated",
            ArtifactEdition = "2026",
            Profile = "cbpr-plus",
            IncludeDeprecated = true,
            IncludeDocumentation = false,
            EmitSourceMappings = true,
            EmitManifest = true,
            Indentation = "  ",
            NewLine = "\r\n"
        };
        options.MessageIdentifiers.Add(identifier);
        options.NamespaceUris.Add(schema.NamespaceUri);
        GenerationPlan plan = new GenerationPlan
        {
            Repository = repository,
            Options = options,
            PreviousManifest = previous
        };
        plan.Schemas.Add(schema);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Repository, Is.SameAs(repository));
            Assert.That(plan.Schemas.Single(), Is.SameAs(schema));
            Assert.That(plan.Options, Is.SameAs(options));
            Assert.That(plan.PreviousManifest, Is.SameAs(previous));
            Assert.That(plan.Options.MessageIdentifiers.Single(), Is.SameAs(identifier));
            Assert.That(plan.Options.NamespaceUris.Single(), Is.EqualTo(schema.NamespaceUri));
            Assert.That(plan.Options.RootNamespace, Is.EqualTo("Bank.Messages"));
            Assert.That(plan.Options.OutputRootPath, Is.EqualTo("/generated"));
            Assert.That(plan.Options.ArtifactEdition, Is.EqualTo("2026"));
            Assert.That(plan.Options.Profile, Is.EqualTo("cbpr-plus"));
            Assert.That(plan.Options.IncludeDeprecated, Is.True);
            Assert.That(plan.Options.IncludeDocumentation, Is.False);
            Assert.That(plan.Options.EmitSourceMappings, Is.True);
            Assert.That(plan.Options.EmitManifest, Is.True);
            Assert.That(plan.Options.Indentation, Is.EqualTo("  "));
            Assert.That(plan.Options.NewLine, Is.EqualTo("\r\n"));
        });
    }

    [Test]
    public void GeneratedFilesManifestAndSymbolsRetainExactMappingsAndHashes()
    {
        ArtifactManifest sourceArtifact = new ArtifactManifest
        {
            OriginUri = "https://example.test/repository.ecore",
            HashAlgorithm = "SHA-256"
        };
        GenerationSymbol symbol = new GenerationSymbol
        {
            SourceIdentifier = "_component-1",
            SourceType = "MessageComponent",
            RepositoryKind = RepositoryEntityKind.MessageComponent,
            SourceName = "repository.ecore",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10",
            XmlName = "CreditTransferTransaction58",
            CSharpNamespace = "Bank.Messages.Pacs",
            CSharpName = "CreditTransferTransaction58_abcd",
            ShapeIdentifier = "shape_abcd",
            OutputPath = "Pacs/CreditTransferTransaction58_abcd.cs",
            Order = 7,
            HasCollision = true
        };
        GeneratedFile file = new GeneratedFile
        {
            Kind = GenerationFileKind.CSharp,
            RelativePath = symbol.OutputPath,
            MediaType = "text/x-csharp",
            NamespaceName = symbol.CSharpNamespace,
            Content = "namespace Bank.Messages.Pacs;"u8.ToArray(),
            HashAlgorithm = "SHA-256",
            IsUnchanged = true
        };
        file.Hash.Value = new byte[] { 1, 2, 3 };
        file.SourceIdentifiers.Add(symbol.SourceIdentifier);
        GenerationManifest manifest = new GenerationManifest
        {
            GeneratorIdentifier = "iso20022",
            GeneratorVersion = "1.0.0",
            ArtifactEdition = "2026",
            Profile = "cbpr-plus",
            RootNamespace = "Bank.Messages",
            HashAlgorithm = "SHA-256"
        };
        manifest.SourceArtifacts.Add(sourceArtifact);
        manifest.Symbols.Add(symbol);
        manifest.Files.Add(file);
        manifest.SettingsHash.Value = new byte[] { 4 };
        manifest.OutputHash.Value = new byte[] { 5 };
        manifest.ManifestHash.Value = new byte[] { 6 };
        GenerationResult result = new GenerationResult
        {
            Success = true,
            Manifest = manifest
        };
        result.Files.Add(file);

        Assert.Multiple(() =>
        {
            Assert.That(file.Kind, Is.EqualTo(GenerationFileKind.CSharp));
            Assert.That(file.Content, Is.EqualTo("namespace Bank.Messages.Pacs;"u8.ToArray()));
            Assert.That(file.Hash.Value, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(file.SourceIdentifiers.Single(), Is.EqualTo("_component-1"));
            Assert.That(file.IsUnchanged, Is.True);
            Assert.That(manifest.FormatVersion, Is.EqualTo("1"));
            Assert.That(manifest.SourceArtifacts.Single(), Is.SameAs(sourceArtifact));
            Assert.That(manifest.Symbols.Single(), Is.SameAs(symbol));
            Assert.That(manifest.Files.Single(), Is.SameAs(file));
            Assert.That(manifest.SettingsHash.Value, Is.EqualTo(new byte[] { 4 }));
            Assert.That(manifest.OutputHash.Value, Is.EqualTo(new byte[] { 5 }));
            Assert.That(manifest.ManifestHash.Value, Is.EqualTo(new byte[] { 6 }));
            Assert.That(symbol.RepositoryKind,
                Is.EqualTo(RepositoryEntityKind.MessageComponent));
            Assert.That(symbol.CSharpName,
                Is.EqualTo("CreditTransferTransaction58_abcd"));
            Assert.That(symbol.HasCollision, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Files.Single(), Is.SameAs(file));
            Assert.That(result.Manifest, Is.SameAs(manifest));
        });
    }

    [Test]
    public void GenerationDataDefaultsAreIndependentAndDeterministic()
    {
        GenerationPlan firstPlan = new GenerationPlan();
        GenerationPlan secondPlan = new GenerationPlan();
        GenerationResult firstResult = new GenerationResult();
        GenerationResult secondResult = new GenerationResult();
        GenerationManifest firstManifest = new GenerationManifest();
        GenerationManifest secondManifest = new GenerationManifest();
        GeneratedFile firstFile = new GeneratedFile();
        GeneratedFile secondFile = new GeneratedFile();

        firstPlan.Schemas.Add(new SchemaArtifact());
        firstPlan.Options.MessageIdentifiers.Add(new MessageIdentifier());
        firstPlan.Options.Limits.MaxCollectionCount = 2;
        firstResult.Files.Add(firstFile);
        firstResult.DiagnosticCodes.Add("generation.test");
        firstManifest.Symbols.Add(new GenerationSymbol());
        firstManifest.SourceArtifacts.Add(new ArtifactManifest());
        firstFile.SourceIdentifiers.Add("source-1");
        firstFile.Hash.Value = new byte[] { 1 };

        Assert.Multiple(() =>
        {
            Assert.That(secondPlan.Repository, Is.Null);
            Assert.That(secondPlan.PreviousManifest, Is.Null);
            Assert.That(secondPlan.Schemas, Is.Empty);
            Assert.That(secondPlan.Options.MessageIdentifiers, Is.Empty);
            Assert.That(secondPlan.Options.NamespaceUris, Is.Empty);
            Assert.That(secondPlan.Options.Limits.MaxCollectionCount,
                Is.EqualTo(Iso20022Limits.MaxCollectionCount));
            Assert.That(secondPlan.Options.RootNamespace, Is.EqualTo("Iso20022.Generated"));
            Assert.That(secondPlan.Options.IncludeDeprecated, Is.False);
            Assert.That(secondPlan.Options.IncludeDocumentation, Is.True);
            Assert.That(secondPlan.Options.EmitSourceMappings, Is.True);
            Assert.That(secondPlan.Options.EmitManifest, Is.True);
            Assert.That(secondPlan.Options.Indentation, Is.EqualTo("    "));
            Assert.That(secondPlan.Options.NewLine, Is.EqualTo("\n"));
            Assert.That(secondResult.Files, Is.Empty);
            Assert.That(secondResult.DiagnosticCodes, Is.Empty);
            Assert.That(secondResult.DiagnosticMessages, Is.Empty);
            Assert.That(secondResult.DiagnosticLocations, Is.Empty);
            Assert.That(secondResult.DiagnosticSourceIdentifiers, Is.Empty);
            Assert.That(secondManifest.FormatVersion, Is.EqualTo("1"));
            Assert.That(secondManifest.Symbols, Is.Empty);
            Assert.That(secondManifest.SourceArtifacts, Is.Empty);
            Assert.That(secondManifest.Files, Is.Empty);
            Assert.That(secondFile.Content, Is.Empty);
            Assert.That(secondFile.Hash.Value, Is.Empty);
            Assert.That(secondFile.SourceIdentifiers, Is.Empty);
            Assert.That(firstPlan.Options, Is.Not.SameAs(secondPlan.Options));
            Assert.That(firstPlan.Options.Limits, Is.Not.SameAs(secondPlan.Options.Limits));
            Assert.That(firstResult.Plan, Is.Not.SameAs(secondResult.Plan));
            Assert.That(firstManifest.Options, Is.Not.SameAs(secondManifest.Options));
            Assert.That(firstFile.Hash, Is.Not.SameAs(secondFile.Hash));
        });
    }

    [Test]
    public void GeneratorContractsUseOnePlanAndDistinctExceptionlessOperations()
    {
        GenerationPlan plan = new GenerationPlan
        {
            Repository = new RepositorySnapshot { Identifier = "repository-2026" }
        };
        CapturingGenerator implementation = new CapturingGenerator();
        IXsdGenerator xsd = implementation;
        IJsonSchemaGenerator json = implementation;
        ICSharpModelGenerator csharp = implementation;

        GenerationResult xsdResult = xsd.GenerateXsd(plan);
        GenerationResult jsonResult = json.GenerateJsonSchema(plan);
        GenerationResult csharpResult = csharp.GenerateCSharpModels(plan);
        GenerationResult invalid = xsd.GenerateXsd(null);

        Assert.Multiple(() =>
        {
            Assert.That(implementation.Plans, Has.Count.EqualTo(4));
            Assert.That(implementation.Plans[0], Is.SameAs(plan));
            Assert.That(implementation.Plans[1], Is.SameAs(plan));
            Assert.That(implementation.Plans[2], Is.SameAs(plan));
            Assert.That(implementation.Plans[3], Is.Null);
            Assert.That(xsdResult.Success, Is.True);
            Assert.That(xsdResult.Plan, Is.SameAs(plan));
            Assert.That(xsdResult.Files.Single().Kind, Is.EqualTo(GenerationFileKind.Xsd));
            Assert.That(jsonResult.Files.Single().Kind,
                Is.EqualTo(GenerationFileKind.JsonSchema));
            Assert.That(csharpResult.Files.Single().Kind,
                Is.EqualTo(GenerationFileKind.CSharp));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCode, Is.EqualTo("generation.plan.required"));
        });
    }

    private class CapturingGenerator : IXsdGenerator, IJsonSchemaGenerator, ICSharpModelGenerator
    {
        public List<GenerationPlan?> Plans = new List<GenerationPlan?>();

        public GenerationResult GenerateXsd(GenerationPlan? plan)
        {
            return CaptureGeneration(plan, GenerationFileKind.Xsd);
        }

        public GenerationResult GenerateJsonSchema(GenerationPlan? plan)
        {
            return CaptureGeneration(plan, GenerationFileKind.JsonSchema);
        }

        public GenerationResult GenerateCSharpModels(GenerationPlan? plan)
        {
            return CaptureGeneration(plan, GenerationFileKind.CSharp);
        }

        private GenerationResult CaptureGeneration(
            GenerationPlan? plan,
            GenerationFileKind kind)
        {
            Plans.Add(plan);
            GenerationResult result = new GenerationResult();
            if (plan == null)
            {
                result.ErrorCode = "generation.plan.required";
                result.ErrorMessage = "A generation plan is required.";
                return result;
            }
            result.Success = true;
            result.Plan = plan;
            result.Files.Add(new GeneratedFile { Kind = kind });
            return result;
        }
    }
}
