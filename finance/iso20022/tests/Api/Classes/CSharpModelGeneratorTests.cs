using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using iso20022;

namespace iso20022.Tests;

public class CSharpModelGeneratorTests
{
    private const string MessageNamespace =
        "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";
    private static RepositorySnapshot? bundledRepository;

    [Test]
    public void CleanWorktreeRegenerationIsStableAcrossRepeatedAndShuffledRepositoryInput()
    {
        CleanWorktreeRegenerationAssertions.AssertDeterministic(
            "csharp",
            CreateRepositoryPlan,
            plan => new CSharpModelGenerator().GenerateCSharpModels(plan));
    }

    [Test]
    public void ManifestRecordsExactMappingsSettingsSourcesAndDeterministicHashes()
    {
        GenerationPlan firstPlan = CreateRepositoryPlan(false);
        GenerationPlan shuffledPlan = CreateRepositoryPlan(true);
        byte[] sourceHash = SHA256.HashData(Encoding.UTF8.GetBytes("repository-xmi"));
        ConfigureSourceArtifact(firstPlan, sourceHash);
        ConfigureSourceArtifact(shuffledPlan, sourceHash);

        GenerationResult first = new CSharpModelGenerator().GenerateCSharpModels(firstPlan);
        GenerationResult shuffled = new CSharpModelGenerator().GenerateCSharpModels(
            shuffledPlan);

        Assert.That(first.Success, Is.True, first.ErrorMessage);
        Assert.That(shuffled.Success, Is.True, shuffled.ErrorMessage);
        GeneratedFile manifestFile = first.Files.Single(file =>
            file.Kind == GenerationFileKind.Manifest);
        GeneratedFile shuffledManifest = shuffled.Files.Single(file =>
            file.Kind == GenerationFileKind.Manifest);
        using JsonDocument document = JsonDocument.Parse(manifestFile.Content);
        JsonElement root = document.RootElement;
        GenerationSymbol message = first.Manifest.Symbols.Single(symbol =>
            symbol.SourceIdentifier == "message-1" &&
            symbol.SourceType == "MessageDefinition");
        GenerationSymbol block = first.Manifest.Symbols.Single(symbol =>
            symbol.SourceIdentifier == "block-1" &&
            symbol.SourceType == "MessageBuildingBlock");
        string[] generatedTypePaths = first.Files
            .Where(file => file.Kind == GenerationFileKind.CSharp)
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] mappedTypePaths = first.Manifest.Symbols
            .Where(symbol => symbol.OutputPath.Length > 0)
            .Select(symbol => symbol.OutputPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(manifestFile.RelativePath,
                Is.EqualTo("Generation/iso20022.csharp-models.manifest.json"));
            Assert.That(manifestFile.MediaType, Is.EqualTo(Iso20022MediaTypes.Json));
            Assert.That(manifestFile.Hash.Value,
                Is.EqualTo(SHA256.HashData(manifestFile.Content)));
            Assert.That(shuffledManifest.Content, Is.EqualTo(manifestFile.Content));
            Assert.That(first.Manifest.GeneratorVersion, Is.EqualTo("1.0.0"));
            Assert.That(first.Manifest.SettingsHash.Value, Has.Length.EqualTo(32));
            Assert.That(first.Manifest.OutputHash.Value, Has.Length.EqualTo(32));
            Assert.That(first.Manifest.ManifestHash.Value, Has.Length.EqualTo(32));
            Assert.That(root.GetProperty("settingsHash").GetString(),
                Is.EqualTo(first.Manifest.SettingsHash.LexicalValue));
            Assert.That(root.GetProperty("outputHash").GetString(),
                Is.EqualTo(first.Manifest.OutputHash.LexicalValue));
            Assert.That(root.GetProperty("manifestHash").GetString(),
                Is.EqualTo(first.Manifest.ManifestHash.LexicalValue));
            Assert.That(root.GetProperty("sourceArtifacts")[0].GetProperty("hash")
                .GetString(), Is.EqualTo(Convert.ToHexString(sourceHash).ToLowerInvariant()));
            Assert.That(root.GetProperty("settings").GetProperty("artifactEdition")
                .GetString(), Is.EqualTo("2026-06-26"));
            Assert.That(message.RepositoryKind,
                Is.EqualTo(RepositoryEntityKind.MessageDefinition));
            Assert.That(message.SourceName, Is.EqualTo("repository-2026"));
            Assert.That(message.NamespaceUri, Is.EqualTo(MessageNamespace));
            Assert.That(message.XmlName, Is.EqualTo("Document"));
            Assert.That(message.CSharpNamespace,
                Is.EqualTo("Contoso.Generated.Messages.Pacs"));
            Assert.That(message.CSharpName, Is.EqualTo("CustomerCreditTransferV10"));
            Assert.That(message.ShapeIdentifier, Does.StartWith("shape_"));
            Assert.That(message.OutputPath,
                Does.EndWith("/CustomerCreditTransferV10.cs"));
            Assert.That(block.XmlName, Is.EqualTo("PmtInf"));
            Assert.That(block.CSharpName, Is.EqualTo("PaymentInformation"));
            Assert.That(block.OutputPath, Is.EqualTo(message.OutputPath));
            Assert.That(first.Manifest.Symbols.Select(symbol => symbol.SourceIdentifier),
                Does.Contain("element-name"));
            Assert.That(first.Manifest.Symbols.Select(symbol => symbol.SourceIdentifier),
                Does.Contain("code-accepted"));
            Assert.That(mappedTypePaths, Is.EqualTo(generatedTypePaths));
            Assert.That(first.Manifest.Symbols.Select(symbol => symbol.Order),
                Is.EqualTo(Enumerable.Range(0, first.Manifest.Symbols.Count)));
            Assert.That(first.Manifest.Files, Has.All.Matches<GeneratedFile>(file =>
                file.Hash.LexicalValue.Length == 64));
        });
    }

    [Test]
    public void ManifestFileCanBeOmittedWithoutLosingInMemoryHashes()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        plan.Options.EmitManifest = false;

        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Files, Has.None.Matches<GeneratedFile>(file =>
                file.Kind == GenerationFileKind.Manifest));
            Assert.That(result.Manifest.SettingsHash.Value, Has.Length.EqualTo(32));
            Assert.That(result.Manifest.OutputHash.Value, Has.Length.EqualTo(32));
            Assert.That(result.Manifest.ManifestHash.Value, Has.Length.EqualTo(32));
            Assert.That(result.Manifest.Symbols, Is.Not.Empty);
        });
    }

    [Test]
    public void RepositoryModelGeneratesCompilableInstantiableDataTypes()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        ICSharpModelGenerator generator = new CSharpModelGenerator();

        GenerationResult result = generator.GenerateCSharpModels(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10");
        Type componentType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.PaymentInstruction");
        Type textType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Types.Max35Text");
        Type binaryType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Types.Max16Binary");
        Type wildcardType = FindGeneratedType(
            assembly,
            "Contoso.Generated.External.SupplementaryDataEnvelope1");
        object root = Activator.CreateInstance(rootType)!;
        object component = rootType.GetField("PaymentInformation")?.GetValue(root)!;
        object text = componentType.GetField("Name")?.GetValue(component)!;
        object binary = Activator.CreateInstance(binaryType)!;
        object wildcard = Activator.CreateInstance(wildcardType)!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Files, Is.Not.Empty);
            Assert.That(result.Files.Where(file => file.Kind == GenerationFileKind.CSharp),
                Is.Not.Empty);
            Assert.That(result.Files.Where(file => file.Kind == GenerationFileKind.Metadata),
                Is.Not.Empty);
            Assert.That(result.Files.Where(file => file.Kind != GenerationFileKind.Manifest),
                Has.All.Matches<GeneratedFile>(file =>
                    file.MediaType == Iso20022MediaTypes.CSharp));
            Assert.That(result.Files.Select(file => file.RelativePath),
                Is.EqualTo(result.Files.Select(file => file.RelativePath)
                    .OrderBy(path => path, StringComparer.Ordinal)));
            Assert.That(result.Files.Select(file => file.RelativePath),
                Does.Contain(
                    "Api/Data/Generated/2026-06-26/Contoso/Generated/Messages/Pacs/CustomerCreditTransferV10.cs"));
            Assert.That(result.Files.Select(file => file.RelativePath),
                Does.Not.Contain(
                    "Api/Data/Generated/2026-06-26/Contoso/Generated/Types/UnusedText.cs"));
            Assert.That(result.Files.Select(file => file.RelativePath),
                Does.Contain(
                    "Api/Enums/Generated/2026-06-26/Contoso/Generated/Codes/PaymentStatusCode.cs"));
            Assert.That(result.Files.Select(file => file.RelativePath),
                Does.Contain(
                    "Api/Enums/Generated/2026-06-26/Contoso/Generated/Components/AccountChoiceKind.cs"));
            Assert.That(result.Manifest.GeneratorIdentifier,
                Is.EqualTo("iso20022.csharp-models"));
            Assert.That(result.Manifest.Files,
                Is.EqualTo(result.Files.Where(file =>
                    file.Kind != GenerationFileKind.Manifest)));
            Assert.That(root, Is.Not.Null);
            Assert.That(component, Is.TypeOf(componentType));
            Assert.That(text, Is.TypeOf(textType));
            Assert.That(textType.GetField("Value")?.GetValue(text), Is.TypeOf<IsoTextValue>());
            Assert.That(binaryType.GetField("Value")?.GetValue(binary),
                Is.TypeOf<IsoBinaryValue>());
            Assert.That(wildcardType.GetField("Value")?.GetValue(wildcard),
                Is.TypeOf<RawXmlFragment>());
            Assert.That(result.Files, Has.All.Matches<GeneratedFile>(file =>
                file.Hash.Value.Length == 32 && file.HashAlgorithm == "SHA-256"));
        });
    }

    [Test]
    public void AllDomainModelsCompileInstantiateAndMatchEveryManifestSymbol()
    {
        GenerationPlan plan = CreateAllDomainRepositoryPlan();

        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type[] publicTypes = assembly.GetExportedTypes()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        GenerationSymbol[] typeSymbols = result.Manifest.Symbols
            .Where(symbol => IsGeneratedTypeSymbol(symbol) &&
                symbol.CSharpNamespace.Length > 0 && symbol.CSharpName.Length > 0 &&
                symbol.OutputPath.Length > 0)
            .ToArray();
        string[] expectedTypeNames = typeSymbols
            .Select(symbol => symbol.CSharpNamespace + "." + symbol.CSharpName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Dictionary<string, Type> typeByName = publicTypes.ToDictionary(
            type => type.FullName!,
            StringComparer.Ordinal);
        Dictionary<string, Type> typeByOutputPath = new Dictionary<string, Type>(
            StringComparer.Ordinal);
        List<string> missingMappings = new List<string>();
        int symbolIndex = 0;
        while (symbolIndex < typeSymbols.Length)
        {
            GenerationSymbol symbol = typeSymbols[symbolIndex];
            string fullName = symbol.CSharpNamespace + "." + symbol.CSharpName;
            if (!typeByName.TryGetValue(fullName, out Type? type))
            {
                missingMappings.Add(fullName);
            }
            else if (symbol.OutputPath.Length == 0)
            {
                missingMappings.Add(fullName + " has no output path");
            }
            else
            {
                typeByOutputPath[symbol.OutputPath] = type;
            }
            symbolIndex++;
        }
        GenerationSymbol[] memberSymbols = result.Manifest.Symbols
            .Where(symbol => IsGeneratedMemberSymbol(symbol) &&
                symbol.CSharpNamespace.Length > 0 && symbol.CSharpName.Length > 0)
            .ToArray();
        symbolIndex = 0;
        while (symbolIndex < memberSymbols.Length)
        {
            GenerationSymbol symbol = memberSymbols[symbolIndex];
            if (!typeByOutputPath.TryGetValue(symbol.OutputPath, out Type? owner) ||
                owner.GetField(symbol.CSharpName) == null)
            {
                missingMappings.Add(symbol.SourceType + " " + symbol.SourceIdentifier +
                    " -> " + symbol.OutputPath + "#" + symbol.CSharpName);
            }
            symbolIndex++;
        }
        List<string> failedInstances = new List<string>();
        int typeIndex = 0;
        while (typeIndex < publicTypes.Length)
        {
            Type type = publicTypes[typeIndex];
            try
            {
                object instance;
                if (type.IsEnum)
                {
                    instance = Enum.ToObject(type, 0);
                }
                else
                {
                    instance = Activator.CreateInstance(type)!;
                }
                if (instance == null)
                {
                    failedInstances.Add(type.FullName ?? type.Name);
                }
            }
            catch (Exception exception)
            {
                failedInstances.Add((type.FullName ?? type.Name) + ": " +
                    exception.GetType().Name);
            }
            typeIndex++;
        }
        string[] selectedAreas = plan.Options.MessageIdentifiers
            .Select(identifier => identifier.BusinessArea)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(area => area, StringComparer.Ordinal)
            .ToArray();
        string[] generatedAreas = result.Manifest.Symbols
            .Where(symbol => symbol.SourceType == "MessageDefinition")
            .Select(symbol => symbol.NamespaceUri.Split(':').Last().Split('.')[0])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(area => area, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(selectedAreas, Has.Length.EqualTo(35));
            Assert.That(generatedAreas, Is.EqualTo(selectedAreas));
            Assert.That(publicTypes.Select(type => type.FullName),
                Is.EqualTo(expectedTypeNames));
            Assert.That(publicTypes, Is.Not.Empty);
            Assert.That(failedInstances, Is.Empty);
            Assert.That(missingMappings, Is.Empty);
            Assert.That(typeSymbols.Select(symbol => symbol.OutputPath)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal),
                Is.EqualTo(result.Files
                    .Where(file => file.Kind == GenerationFileKind.CSharp)
                    .Select(file => file.RelativePath)
                    .OrderBy(path => path, StringComparer.Ordinal)));
            Assert.That(result.Files.Count(file => file.Kind == GenerationFileKind.CSharp),
                Is.EqualTo(publicTypes.Length));
        });
    }

    [Test]
    public void TypedCodecsParseAndWriteEveryPrimitiveSchemaConstruct()
    {
        GenerationPlan plan = CreateReaderPlan();
        RepositorySnapshot repository = plan.Repository!;
        MessageComponentDefinition component = repository.MessageComponents.Single(
            value => value.Identifier == "component-1");
        List<TypedCodecConstructCase> cases = CreateTypedCodecConstructCases();
        int index = 0;
        while (index < cases.Count)
        {
            TypedCodecConstructCase item = cases[index];
            string identifier = "construct-" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            string typeIdentifier = "construct-type-" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            AddElementReference(component, identifier, component.MessageElements.Count);
            repository.MessageElements.Add(CreateElement(
                identifier,
                item.Name,
                item.XmlName,
                typeIdentifier,
                RepositoryEntityKind.DataType,
                CreateMultiplicity(0, true, 1),
                MessageElementKind.Attribute,
                true));
            repository.DataTypes.Add(new DataTypeDefinition
            {
                Identifier = typeIdentifier,
                Name = item.Name + "Type",
                Kind = item.Kind,
                BinaryEncoding = item.BinaryEncoding,
                SchemaTypeName = item.SchemaTypeName
            });
            index++;
        }

        GenerationResult generated = new CSharpModelGenerator().GenerateCSharpModels(plan);
        Assert.That(generated.Success, Is.True, generated.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(generated.Files);
        Type codec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        StringBuilder content = new StringBuilder();
        content.Append("<Nm>Alice</Nm><Sts>ACCP</Sts>");
        index = 0;
        while (index < cases.Count)
        {
            TypedCodecConstructCase item = cases[index];
            content.Append("<").Append(item.XmlName).Append(">")
                .Append(item.LexicalValue).Append("</").Append(item.XmlName).Append(">");
            index++;
        }
        string xml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            content + "</PmtInf></Document>";

        object model = ReadGeneratedDocument(assembly, xml);
        string written = WriteGeneratedDocument(
            codec,
            model,
            "pacs.008.001.10",
            MessageNamespace);
        object reparsed = ReadGeneratedDocument(assembly, written);
        object payment = FindField(reparsed.GetType(), "PaymentInformation")
            .GetValue(reparsed)!;

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(xml));
            Assert.That(payment, Is.Not.Null);
            Assert.That(cases.Select(value => value.Kind).Distinct(), Is.EquivalentTo(
                Enum.GetValues<DataTypeKind>().Where(value =>
                    value != DataTypeKind.Unknown &&
                    value != DataTypeKind.String &&
                    value != DataTypeKind.Amount &&
                    value != DataTypeKind.CodeSet &&
                    value != DataTypeKind.ExternalSchema)));
        });
    }

    [Test]
    public void BusinessAreaVersionRepresentativesExposeTypedReadAndWriteApis()
    {
        RepositorySnapshot repository = GetBundledRepository();
        MessageDefinitionMetadata[] representatives = repository.MessageDefinitions
            .Where(message =>
                message.MessageIdentifier.BusinessArea.Length > 0 &&
                message.MessageIdentifier.Version.Length > 0)
            .GroupBy(message => new
            {
                message.MessageIdentifier.BusinessArea,
                message.MessageIdentifier.Version
            })
            .Select(group => group.OrderBy(message =>
                    message.MessageIdentifier.Value,
                    StringComparer.Ordinal)
                .First())
            .OrderBy(message => message.MessageIdentifier.BusinessArea,
                StringComparer.Ordinal)
            .ThenBy(message => message.MessageIdentifier.Version,
                StringComparer.Ordinal)
            .ToArray();
        GenerationPlan plan = new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026-06",
                IncludeDeprecated = true,
                IncludeDocumentation = false,
                RootNamespace = "VersionFamilies.Generated"
            }
        };
        int index = 0;
        while (index < representatives.Length)
        {
            plan.Options.MessageIdentifiers.Add(representatives[index].MessageIdentifier);
            index++;
        }

        GenerationResult generated = new CSharpModelGenerator().GenerateCSharpModels(plan);
        Assert.That(generated.Success, Is.True, generated.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(generated.Files);
        Type[] generatedTypes = assembly.GetTypes();
        List<string> missingApis = new List<string>();
        index = 0;
        while (index < representatives.Length)
        {
            MessageDefinitionMetadata message = representatives[index];
            GenerationSymbol? symbol = generated.Manifest.Symbols.FirstOrDefault(candidate =>
                candidate.SourceType == "MessageDefinition" &&
                candidate.SourceIdentifier == message.Identifier);
            if (symbol == null)
            {
                missingApis.Add(message.MessageIdentifier.Value + ":model");
                index++;
                continue;
            }
            Type? codec = generatedTypes.SingleOrDefault(type =>
                type.Namespace == symbol.CSharpNamespace &&
                type.Name.StartsWith(symbol.CSharpName + "Codec", StringComparison.Ordinal));
            MethodInfo? reader = codec?.GetMethod(
                "ReadModel",
                BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo? writer = codec?.GetMethod(
                "WriteDocument",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (reader == null || reader.ReturnType.FullName !=
                symbol.CSharpNamespace + "." + symbol.CSharpName)
            {
                missingApis.Add(message.MessageIdentifier.Value + ":read");
            }
            if (writer == null || writer.GetParameters().Length != 4)
            {
                missingApis.Add(message.MessageIdentifier.Value + ":write");
            }
            index++;
        }
        string[] repositoryFamilies = repository.MessageDefinitions
            .Where(message =>
                message.MessageIdentifier.BusinessArea.Length > 0 &&
                message.MessageIdentifier.Version.Length > 0)
            .Select(message => message.MessageIdentifier.BusinessArea + "." +
                message.MessageIdentifier.Version)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] testedFamilies = representatives
            .Select(message => message.MessageIdentifier.BusinessArea + "." +
                message.MessageIdentifier.Version)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(representatives, Is.Not.Empty);
            Assert.That(testedFamilies, Is.EqualTo(repositoryFamilies));
            Assert.That(testedFamilies.Select(value => value.Split('.')[0]).Distinct()
                .ToArray(), Has.Length.EqualTo(35));
            Assert.That(missingApis, Is.Empty);
        });
    }

    [Test]
    public void FullCorpusResolvesEveryMessageNamespaceSchemaModelCodecAndVersion()
    {
        RepositorySnapshot repository = GetBundledRepository();
        GenerationPlan plan = new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026-06",
                IncludeDeprecated = true,
                IncludeDocumentation = false,
                RootNamespace = "Corpus.Generated"
            }
        };
        XsdGenerator planner = new XsdGenerator();
        List<XsdGenerationUnit> units = planner.BuildGenerationUnits(plan);
        planner.SortGenerationUnits(units);
        GenerationResult generation = new GenerationResult { Plan = plan };
        CSharpGenerationContext context = CSharpModelEmitter.CreateCSharpContext(
            plan,
            units,
            generation);
        List<CSharpRegistryEntry> entries =
            CSharpMetadataEmitter.CollectRegistryEntries(context);
        Dictionary<string, XsdGenerationUnit> unitsBySource = units.ToDictionary(
            unit => unit.Message?.Identifier ?? string.Empty,
            StringComparer.Ordinal);
        Dictionary<string, CSharpRegistryEntry> entriesBySource = entries.ToDictionary(
            entry => entry.SourceIdentifier,
            StringComparer.Ordinal);
        NamespaceService namespaceService = new NamespaceService();
        List<string> failures = new List<string>();
        int index = 0;
        while (index < repository.MessageDefinitions.Count)
        {
            MessageDefinitionMetadata message = repository.MessageDefinitions[index];
            if (!unitsBySource.TryGetValue(message.Identifier, out XsdGenerationUnit? unit))
            {
                failures.Add(message.Identifier + ":schema");
                index++;
                continue;
            }
            if (!entriesBySource.TryGetValue(
                message.Identifier,
                out CSharpRegistryEntry? entry))
            {
                failures.Add(message.Identifier + ":registry");
                index++;
                continue;
            }
            NamespaceServiceResult parsed = namespaceService.ParseNamespace(
                message.NamespaceUri,
                NamespaceMode.Strict);
            MessageIdentifier? identified = parsed.Descriptor?.MessageIdentifier;
            if (!parsed.Success || parsed.Descriptor == null ||
                parsed.Descriptor.Kind == NamespaceKind.Unknown ||
                parsed.Descriptor.Kind == NamespaceKind.Repository ||
                identified == null ||
                identified.Value != message.MessageIdentifier.Value)
            {
                failures.Add(message.MessageIdentifier.Value + ":namespace");
            }
            if (unit.NamespaceUri != message.NamespaceUri ||
                unit.Message?.Identifier != message.Identifier)
            {
                failures.Add(message.MessageIdentifier.Value + ":schema");
            }
            if (entry.MessageIdentifier != message.MessageIdentifier.Value ||
                entry.NamespaceUri != message.NamespaceUri ||
                entry.ModelTypeName.Length == 0)
            {
                failures.Add(message.MessageIdentifier.Value + ":model");
            }
            if (entry.CodecTypeName.Length == 0 ||
                entry.CodecTypeName == entry.ModelTypeName)
            {
                failures.Add(message.MessageIdentifier.Value + ":codec");
            }
            if (identified?.Version != message.MessageIdentifier.Version ||
                identified?.Version.Length == 0)
            {
                failures.Add(message.MessageIdentifier.Value + ":version");
            }
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(repository.MessageDefinitions, Has.Count.EqualTo(3329));
            Assert.That(units, Has.Count.EqualTo(repository.MessageDefinitions.Count));
            Assert.That(entries, Has.Count.EqualTo(repository.MessageDefinitions.Count));
            Assert.That(unitsBySource.Keys, Is.EquivalentTo(
                repository.MessageDefinitions.Select(message => message.Identifier)));
            Assert.That(entriesBySource.Keys, Is.EquivalentTo(
                repository.MessageDefinitions.Select(message => message.Identifier)));
            Assert.That(repository.MessageDefinitions
                .Select(message => message.NamespaceUri)
                .Distinct(StringComparer.Ordinal)
                .Count(),
                Is.EqualTo(repository.MessageDefinitions.Count));
            Assert.That(entries.Select(entry => entry.ModelTypeName)
                .Distinct(StringComparer.Ordinal)
                .Count(),
                Is.EqualTo(entries.Count));
            Assert.That(entries.Select(entry => entry.CodecTypeName)
                .Distinct(StringComparer.Ordinal)
                .Count(),
                Is.EqualTo(entries.Count));
            Assert.That(failures, Is.Empty);
            Assert.That(generation.DiagnosticCodes, Is.Empty);
        });
    }

    [Test]
    public void ApprovedSnapshotMatchesPinnedAllMessageModelCodecAndRegistryBaseline()
    {
        RepositorySnapshot repository = GetBundledRepository();
        GenerationPlan plan = new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026-06",
                EmitManifest = false,
                EmitSourceMappings = false,
                IncludeDeprecated = true,
                IncludeDocumentation = false,
                RootNamespace = "Iso20022.Baseline"
            }
        };
        plan.Options.Limits.MaxCollectionCount = 1000000;
        plan.Options.Limits.MaxElementCount = 2000000;
        plan.Options.Limits.MaxSchemaCount = 10000;
        plan.Options.Limits.MaxTotalBytes = int.MaxValue;

        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        HashSet<string> messageSources = repository.MessageDefinitions
            .Select(message => message.Identifier)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> generatedModelSources = result.Files
            .Where(file => file.Kind == GenerationFileKind.CSharp &&
                file.NamespaceName.StartsWith(
                    "Iso20022.Baseline.Messages.",
                    StringComparison.Ordinal))
            .SelectMany(file => file.SourceIdentifiers)
            .Where(messageSources.Contains)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> generatedCodecSources = result.Files
            .Where(file => file.Kind == GenerationFileKind.Metadata)
            .SelectMany(file => file.SourceIdentifiers)
            .Where(messageSources.Contains)
            .ToHashSet(StringComparer.Ordinal);
        GeneratedFile registry = result.Files.Single(file =>
            file.RelativePath.EndsWith(
                "/Iso20022GeneratedRegistry.cs",
                StringComparison.Ordinal));
        string registryText = Encoding.UTF8.GetString(registry.Content);
        List<string> missingRegistryIdentifiers = repository.MessageDefinitions
            .Select(message => message.MessageIdentifier.Value)
            .Where(identifier => identifier.Length > 0 &&
                !registryText.Contains("\"" + identifier + "\"", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identifier => identifier, StringComparer.Ordinal)
            .ToList();
        string[] archivedSources = repository.MessageDefinitions
            .GroupBy(message => new
            {
                message.MessageIdentifier.BusinessArea,
                message.MessageIdentifier.MessageFunctionality,
                message.MessageIdentifier.Variant
            })
            .SelectMany(group => group.OrderByDescending(message =>
                    message.MessageIdentifier.Version,
                    StringComparer.Ordinal)
                .Skip(1))
            .Select(message => message.Identifier)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(repository.Provenance.Artifact.IsApproved, Is.True);
            Assert.That(repository.Provenance.Artifact.Hash.LexicalValue, Is.EqualTo(
                "9a9d060dd94b6858a4ef4836ca36a9a67308e96b69d7d184ff97f28295b96039"));
            Assert.That(messageSources, Has.Count.EqualTo(3329));
            Assert.That(archivedSources, Is.Not.Empty);
            Assert.That(generatedModelSources, Is.EquivalentTo(messageSources));
            Assert.That(generatedCodecSources, Is.EquivalentTo(messageSources));
            Assert.That(archivedSources, Is.SubsetOf(generatedModelSources));
            Assert.That(missingRegistryIdentifiers, Is.Empty);
            Assert.That(result.DiagnosticCodes, Is.Empty);
            Assert.That(result.Files.Count(file =>
                file.Kind == GenerationFileKind.CSharp), Is.EqualTo(9132));
            Assert.That(result.Files.Count(file =>
                file.Kind == GenerationFileKind.Metadata), Is.EqualTo(8593));
            Assert.That(result.Manifest.OutputHash.LexicalValue, Is.EqualTo(
                "aac764490304f9b828c3bfed657301d32e6d7c826bb5dfe476da7cc62c1610b1"));
            Assert.That(result.Manifest.ManifestHash.LexicalValue, Is.EqualTo(
                "6a081b29252152e0a9b0b4c2cc5c59ac5fb87b2abb8c5a3f0f9c81241c5a5b86"));
            Assert.That(registry.Hash.LexicalValue, Is.EqualTo(
                "6ec76b124b490d75dd1b2232ac98bf9d1d28132f51d3ddca5877ee88754bd512"));
        });
    }

    [Test]
    public void GeneratedFieldsRepresentRequiredOptionalRepeatingChoiceAndCodeValues()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type componentType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.PaymentInstruction");
        Type choiceType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.AccountChoice");
        Type choiceKindType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.AccountChoiceKind");
        Type statusType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Codes.PaymentStatusCode");
        Type currencyType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Codes.ExternalCurrencyCode");
        object component = Activator.CreateInstance(componentType)!;
        object choice = Activator.CreateInstance(choiceType)!;
        FieldInfo statuses = FindField(componentType, "Statuses");
        FieldInfo optionalStatus = FindField(componentType, "OptionalStatus");
        FieldInfo statusPresence = FindField(componentType, "HasOptionalStatus");
        FieldInfo account = FindField(componentType, "Account");
        FieldInfo currency = FindField(componentType, "Currency");
        FieldInfo servicer = FindField(componentType, "AccountServicer");
        Assert.Multiple(() =>
        {
            Assert.That(statuses.FieldType.IsGenericType, Is.True);
            Assert.That(statuses.FieldType.GetGenericTypeDefinition(),
                Is.EqualTo(typeof(List<>)));
            Assert.That(statuses.FieldType.GenericTypeArguments.Single(), Is.EqualTo(statusType));
            Assert.That(statuses.GetValue(component), Is.Not.Null);
            Assert.That(optionalStatus.FieldType, Is.EqualTo(statusType));
            Assert.That(optionalStatus.GetValue(component)?.ToString(), Is.EqualTo("Unknown"));
            Assert.That(statusPresence.FieldType, Is.EqualTo(typeof(bool)));
            Assert.That(statusPresence.GetValue(component), Is.EqualTo(false));
            Assert.That(account.FieldType, Is.EqualTo(choiceType));
            Assert.That(account.GetValue(component), Is.Null);
            Assert.That(currency.FieldType, Is.EqualTo(currencyType));
            Assert.That(currency.GetValue(component), Is.Null);
            Assert.That(servicer.FieldType, Is.EqualTo(typeof(IsoTextValue)));
            Assert.That(servicer.GetValue(component), Is.Null);
            Assert.That(choiceType.GetField("Choice")?.FieldType, Is.EqualTo(choiceKindType));
            Assert.That(choiceType.GetField("Choice")?.GetValue(choice)?.ToString(),
                Is.EqualTo("None"));
            Assert.That(choiceType.GetField("Iban")?.GetValue(choice), Is.Null);
            Assert.That(choiceType.GetField("Bic")?.GetValue(choice), Is.Null);
            Assert.That(Enum.GetNames(statusType),
                Is.EqualTo(new[] { "Unknown", "Accepted", "Rejected" }));
            Assert.That(Convert.ToInt32(Enum.Parse(statusType, "Accepted")), Is.EqualTo(1));
            Assert.That(Convert.ToInt32(Enum.Parse(statusType, "Rejected")), Is.EqualTo(2));
            Assert.That(currencyType.GetField("Value")?.FieldType,
                Is.EqualTo(typeof(IsoCodeValue)));
        });
    }

    [Test]
    public void GenerationIsStableAcrossRepositoryOrderingAndHonorsTextOptions()
    {
        GenerationPlan firstPlan = CreateRepositoryPlan(false);
        firstPlan.Options.Indentation = "\t";
        firstPlan.Options.NewLine = "\r\n";
        GenerationPlan shuffledPlan = CreateRepositoryPlan(true);
        shuffledPlan.Options.Indentation = "\t";
        shuffledPlan.Options.NewLine = "\r\n";
        GenerationResult first = new CSharpModelGenerator().GenerateCSharpModels(firstPlan);
        GenerationResult shuffled = new CSharpModelGenerator().GenerateCSharpModels(shuffledPlan);

        Assert.That(first.Success, Is.True, first.ErrorMessage);
        Assert.That(shuffled.Success, Is.True, shuffled.ErrorMessage);
        Assert.Multiple(() =>
        {
            Assert.That(shuffled.Files.Select(file => file.RelativePath),
                Is.EqualTo(first.Files.Select(file => file.RelativePath)));
            Assert.That(shuffled.Files.Select(file => file.Content),
                Is.EqualTo(first.Files.Select(file => file.Content)));
            Assert.That(first.Files.Where(file => file.Kind != GenerationFileKind.Manifest),
                Has.All.Matches<GeneratedFile>(file =>
                Encoding.UTF8.GetString(file.Content).Contains("\r\n", StringComparison.Ordinal)));
            Assert.That(first.Files, Has.Some.Matches<GeneratedFile>(file =>
                Encoding.UTF8.GetString(file.Content).Contains(
                    "\r\n\tpublic", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void NormalizedSchemaGeneratesRootAndComponentModelsInMessagePartition()
    {
        SchemaArtifact schema = new SchemaArtifact
        {
            SourceName = "pacs.008.001.10.xsd",
            NamespaceUri = MessageNamespace,
            RootElement = "Document",
            MessageIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10",
                BusinessArea = "pacs",
                MessageFunctionality = "008",
                Variant = "001",
                Version = "10"
            }
        };
        MessageComponentDefinition component = new MessageComponentDefinition
        {
            Identifier = "DocumentType",
            Name = "DocumentType"
        };
        AddElementReference(component, "document-name", 0);
        schema.MessageComponents.Add(component);
        schema.MessageElements.Add(CreateElement(
            "document-name", "Name", "Nm", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(1, true, 1), MessageElementKind.Attribute, true));
        schema.MessageDefinitions.Add(new MessageDefinitionMetadata
        {
            Identifier = "Document",
            Name = "Document",
            NamespaceUri = MessageNamespace,
            RootElement = "Document",
            XmlName = "DocumentType",
            MessageIdentifier = schema.MessageIdentifier
        });
        GenerationPlan plan = new GenerationPlan();
        plan.Schemas.Add(schema);
        plan.Options.RootNamespace = "contoso.generated";
        plan.Options.ArtifactEdition = "2026-06-26";

        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.Pacs00800110.Document");
        Type componentType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.Pacs00800110.DocumentType");
        object root = Activator.CreateInstance(rootType)!;
        Assert.Multiple(() =>
        {
            Assert.That(rootType.GetField("Value")?.FieldType, Is.EqualTo(componentType));
            Assert.That(rootType.GetField("Value")?.GetValue(root), Is.TypeOf(componentType));
            Assert.That(componentType.GetField("Name")?.FieldType,
                Is.EqualTo(typeof(IsoTextValue)));
            Assert.That(result.Files.Where(file => file.Kind == GenerationFileKind.CSharp)
                .Select(file => file.NamespaceName).Distinct(),
                Is.EqualTo(new[] { "Contoso.Generated.Messages.Pacs.Pacs00800110" }));
        });
    }

    [Test]
    public void InternalMetadataAndCodecsExposeExactMemberAndCodeMappings()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type componentType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.PaymentInstruction");
        Type metadataType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.PaymentInstructionMetadata");
        Type codecType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.PaymentInstructionCodec");
        Type statusType = FindGeneratedType(
            assembly,
            "Contoso.Generated.Codes.PaymentStatusCode");
        Type statusCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Codes.PaymentStatusCodeCodec");
        string[] memberNames = ReadStaticField<string[]>(metadataType, "MemberNames");
        string[] xmlNames = ReadStaticField<string[]>(metadataType, "XmlNames");
        string[] typeNames = ReadStaticField<string[]>(metadataType, "TypeNames");
        int[] minimum = ReadStaticField<int[]>(metadataType, "MinimumOccurs");
        int[] maximum = ReadStaticField<int[]>(metadataType, "MaximumOccurs");
        bool[] hasMaximum = ReadStaticField<bool[]>(metadataType, "HasMaximumOccurs");
        object created = InvokeInternal(codecType, "CreateModel", Array.Empty<object>())!;
        object accepted = InvokeInternal(
            statusCodec, "ReadCode", new object[] { "ACCP" })!;
        object unknown = InvokeInternal(
            statusCodec, "ReadCode", new object[] { "NOPE" })!;
        object rejected = Enum.Parse(statusType, "Rejected");
        Assert.Multiple(() =>
        {
            Assert.That(result.Files.Where(file => file.Kind == GenerationFileKind.Metadata),
                Is.Not.Empty);
            Assert.That(result.Files.Where(file => file.Kind == GenerationFileKind.Metadata)
                .Select(file => file.RelativePath), Does.Contain(
                    "Api/Internal/Generated/2026-06-26/Contoso/Generated/Components/PaymentInstruction.Metadata.cs"));
            Assert.That(memberNames, Is.EqualTo(new[]
            {
                "Name", "Statuses", "OptionalStatus", "Account", "Currency",
                "SignatureData", "SupplementaryData", "AccountServicer"
            }));
            Assert.That(xmlNames, Is.EqualTo(new[]
            {
                "Nm", "Sts", "OptnlSts", "Acct", "Ccy", "SgntrData",
                "SplmtryData", "AcctSvcr"
            }));
            Assert.That(typeNames[0],
                Is.EqualTo("global::Contoso.Generated.Types.Max35Text"));
            Assert.That(minimum, Is.EqualTo(new[] { 1, 1, 0, 0, 0, 0, 0, 0 }));
            Assert.That(maximum, Is.EqualTo(new[] { 1, 3, 1, 1, 1, 1, 1, 1 }));
            Assert.That(hasMaximum, Is.All.EqualTo(true));
            Assert.That(created, Is.TypeOf(componentType));
            Assert.That(InvokeInternal(
                codecType, "FindMemberIndex", new object[] { "Sts" }), Is.EqualTo(1));
            Assert.That(InvokeInternal(
                codecType, "FindMemberIndex", new object[] { "Missing" }), Is.EqualTo(-1));
            Assert.That(InvokeInternal(
                codecType, "FindMemberXmlName", new object[] { 6 }),
                Is.EqualTo("SplmtryData"));
            Assert.That(InvokeInternal(
                codecType, "FindMemberXmlName", new object[] { 99 }),
                Is.EqualTo(string.Empty));
            Assert.That(accepted.ToString(), Is.EqualTo("Accepted"));
            Assert.That(unknown.ToString(), Is.EqualTo("Unknown"));
            Assert.That(InvokeInternal(
                statusCodec, "WriteCode", new[] { rejected }), Is.EqualTo("RJCT"));
            Assert.That(InvokeInternal(
                statusCodec, "WriteCode", new[] { unknown }), Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void GeneratedStructuralValidationChecksRequiredMultiplicityAndOrderMetadata()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        Type metadata = FindGeneratedType(
            assembly,
            "Contoso.Generated.Components.PaymentInstructionMetadata");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;

        ValidationReport missingMultiplicity = ValidateGeneratedModel(registry, model);
        object payment = AddRequiredGeneratedStatus(model);
        ValidationReport valid = ValidateGeneratedModel(registry, model);
        FindField(model.GetType(), "PaymentInformation").SetValue(model, null);
        ValidationReport missingRequired = ValidateGeneratedModel(registry, model);

        Assert.Multiple(() =>
        {
            Assert.That(ReadStaticField<int[]>(metadata, "Order"),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(ReadStaticField<string[]>(metadata, "XmlNames"),
                Is.EqualTo(new[]
                {
                    "Nm", "Sts", "OptnlSts", "Acct", "Ccy", "SgntrData",
                    "SplmtryData", "AcctSvcr"
                }));
            Assert.That(missingMultiplicity.Success, Is.True);
            Assert.That(missingMultiplicity.Valid, Is.False);
            Assert.That(missingMultiplicity.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.minimum_occurs"));
            Assert.That(missingMultiplicity.Diagnostics.Single().Path.Value,
                Is.EqualTo("/Document/PmtInf/Sts"));
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(valid.Diagnostics, Is.Empty);
            Assert.That(missingRequired.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.required_missing"));
            Assert.That(missingRequired.Diagnostics.Single().Path.Value,
                Is.EqualTo("/Document/PmtInf"));
            Assert.That(payment, Is.Not.Null);
        });
    }

    [Test]
    public void GeneratedStructuralValidationChecksMaximumMultiplicityAndChoices()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        FieldInfo statusesField = FindField(payment.GetType(), "Statuses");
        System.Collections.IList statuses =
            (System.Collections.IList)statusesField.GetValue(payment)!;
        Type statusType = statusesField.FieldType.GetGenericArguments()[0];
        statuses.Add(Enum.Parse(statusType, "Rejected"));
        statuses.Add(Enum.Parse(statusType, "Accepted"));
        statuses.Add(Enum.Parse(statusType, "Rejected"));

        ValidationReport excessive = ValidateGeneratedModel(registry, model);

        statuses.Clear();
        statuses.Add(Enum.Parse(statusType, "Accepted"));
        FieldInfo accountField = FindField(payment.GetType(), "Account");
        object account = Activator.CreateInstance(accountField.FieldType)!;
        accountField.SetValue(payment, account);
        FieldInfo ibanField = FindField(account.GetType(), "Iban");
        FieldInfo bicField = FindField(account.GetType(), "Bic");
        ibanField.SetValue(account, Activator.CreateInstance(ibanField.FieldType));
        bicField.SetValue(account, Activator.CreateInstance(bicField.FieldType));
        FieldInfo discriminator = FindField(account.GetType(), "Choice");
        discriminator.SetValue(
            account,
            Enum.Parse(discriminator.FieldType, "Iban"));

        ValidationReport invalidChoice = ValidateGeneratedModel(registry, model);

        Assert.Multiple(() =>
        {
            Assert.That(excessive.Success, Is.True);
            Assert.That(excessive.Valid, Is.False);
            Assert.That(excessive.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.maximum_occurs"));
            Assert.That(excessive.Diagnostics.Single().Path.Value,
                Is.EqualTo("/Document/PmtInf/Sts"));
            Assert.That(invalidChoice.Success, Is.True);
            Assert.That(invalidChoice.Valid, Is.False);
            Assert.That(invalidChoice.Diagnostics.Select(item => item.Code),
                Is.EqualTo(new[]
                {
                    "validation.structural.choice_discriminator",
                    "validation.structural.choice_cardinality"
                }));
            Assert.That(invalidChoice.Diagnostics.Select(item => item.Path.Value),
                Is.EqualTo(new[]
                {
                    "/Document/PmtInf/Acct/BIC",
                    "/Document/PmtInf/Acct"
                }));
        });
    }

    [Test]
    public void GeneratedStructuralValidationChecksVersionCoherenceAndPipelineDispatch()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);

        ValidationReport wrongNamespace = (ValidationReport)InvokeInternal(
            registry,
            "ValidateMessageModel",
            new object[]
            {
                model,
                "pacs.008.001.10",
                "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.09",
                new ValidationPolicy()
            })!;
        ValidationReport wrongIdentifier = (ValidationReport)InvokeInternal(
            registry,
            "ValidateMessageModel",
            new object[]
            {
                model,
                "pacs.009.001.10",
                MessageNamespace,
                new ValidationPolicy()
            })!;
        ValidationReport wrongType = (ValidationReport)InvokeInternal(
            registry,
            "ValidateMessageModel",
            new object[]
            {
                new object(),
                "pacs.008.001.10",
                MessageNamespace,
                new ValidationPolicy()
            })!;
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace
        };
        request.Policy.Stages.Add(ValidationStage.Structural);
        ValidationReport valid = new MessageValidator().ValidateMessage(request);
        ((System.Collections.IList)FindField(payment.GetType(), "Statuses")
            .GetValue(payment)!).Clear();
        ValidationReport invalid = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(wrongNamespace.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.namespace_mismatch"));
            Assert.That(wrongIdentifier.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.identifier_mismatch"));
            Assert.That(wrongType.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.model_type_mismatch"));
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(valid.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.Structural }));
            Assert.That(invalid.Success, Is.True, invalid.ErrorMessage);
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.Diagnostics.Single().Code,
                Is.EqualTo("validation.structural.minimum_occurs"));
            Assert.That(invalid.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.Structural));
        });
    }

    [Test]
    public void GeneratedStructuralFieldConstraintMutationsFailThroughPublicValidation()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateGeneratedFieldMutationPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        Dictionary<string, ValidationReport> reports =
            new Dictionary<string, ValidationReport>();

        reports.Add("validation.structural.required_missing",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                FindField(model.GetType(), "PaymentInformation").SetValue(model, null);
            }));
        reports.Add("validation.structural.collection_missing",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                FindField(payment.GetType(), "Statuses").SetValue(payment, null);
            }));
        reports.Add("validation.structural.collection_limit",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                System.Collections.IList statuses =
                    GetGeneratedCollection(payment, "Statuses");
                Type statusType = statuses.GetType().GetGenericArguments()[0];
                statuses.Add(Enum.Parse(statusType, "Rejected"));
                policy.Limits.MaxCollectionCount = 1;
            }));
        reports.Add("validation.structural.minimum_occurs",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                GetGeneratedCollection(payment, "Statuses").Clear();
            }));
        reports.Add("validation.structural.maximum_occurs",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                System.Collections.IList statuses =
                    GetGeneratedCollection(payment, "Statuses");
                Type statusType = statuses.GetType().GetGenericArguments()[0];
                statuses.Add(Enum.Parse(statusType, "Rejected"));
                statuses.Add(Enum.Parse(statusType, "Accepted"));
                statuses.Add(Enum.Parse(statusType, "Rejected"));
            }));
        reports.Add("validation.structural.null_item",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                GetGeneratedCollection(payment, "SignatureData").Add(null);
            }));
        reports.Add("validation.structural.choice_discriminator",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                object account = AddGeneratedAccount(payment);
                FieldInfo iban = FindField(account.GetType(), "Iban");
                iban.SetValue(account, Activator.CreateInstance(iban.FieldType));
                FieldInfo choice = FindField(account.GetType(), "Choice");
                choice.SetValue(account, Enum.Parse(choice.FieldType, "Bic"));
            }));
        reports.Add("validation.structural.choice_cardinality",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                AddGeneratedAccount(payment);
            }));
        reports.Add("validation.structural.simple_content_missing",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                object name = FindField(payment.GetType(), "Name").GetValue(payment)!;
                FindField(name.GetType(), "Value").SetValue(name, null);
            }));
        reports.Add("validation.structural.amount_missing",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                AddGeneratedAmount(payment).Amount = null!;
            }));
        reports.Add("validation.structural.currency_missing",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                IsoAmountValue amount = AddGeneratedAmount(payment);
                SetDecimalValue(amount.Amount, 1m, 0, "1");
            }));
        reports.Add("validation.structural.raw_content_missing",
            ValidateGeneratedStructuralMutation(registry, (model, payment, policy) =>
            {
                FieldInfo supplementary =
                    FindField(payment.GetType(), "SupplementaryData");
                object wrapper = Activator.CreateInstance(supplementary.FieldType)!;
                supplementary.SetValue(payment, wrapper);
                FindField(wrapper.GetType(), "Value").SetValue(wrapper, null);
            }));

        AssertGeneratedMutationReports(reports, ValidationStage.Structural);
    }

    [Test]
    public void GeneratedPrimitiveFieldConstraintMutationsFailThroughPublicValidation()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateGeneratedFieldMutationPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        Dictionary<string, ValidationReport> reports =
            new Dictionary<string, ValidationReport>();

        reports.Add("validation.primitive.length",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetTextValue(FindGeneratedTextValue(payment, "Name"), "ABCD");
            }));
        reports.Add("validation.primitive.minimum_length",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetTextValue(FindGeneratedTextValue(payment, "Name"), "A");
            }));
        reports.Add("validation.primitive.maximum_length",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetTextValue(FindGeneratedTextValue(payment, "Name"), "ABCDEF");
            }));
        reports.Add("validation.primitive.pattern",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetTextValue(FindGeneratedTextValue(payment, "Name"), "AbC");
            }));
        reports.Add("validation.primitive.enumeration",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetTextValue(FindGeneratedTextValue(payment, "Name"), "DEF");
            }));
        reports.Add("validation.primitive.white_space",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetTextValue(FindGeneratedTextValue(payment, "Name"), " ABC");
            }));
        reports.Add("validation.primitive.total_digits",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetGeneratedAmount(payment, 1234.56m, 2, "1234.56");
            }));
        reports.Add("validation.primitive.fraction_digits",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetGeneratedAmount(payment, 12.345m, 3, "12.345");
            }));
        reports.Add("validation.primitive.minimum_inclusive",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetGeneratedAmount(payment, -1m, 0, "-1");
            }));
        reports.Add("validation.primitive.minimum_exclusive",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetGeneratedAmount(payment, 0m, 0, "0");
            }));
        reports.Add("validation.primitive.maximum_inclusive",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetGeneratedAmount(payment, 999.5m, 1, "999.5");
            }));
        reports.Add("validation.primitive.maximum_exclusive",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                SetGeneratedAmount(payment, 1000m, 0, "1000");
            }));
        reports.Add("validation.generated.collection_limit",
            ValidateGeneratedPrimitiveMutation(registry, (model, payment, policy) =>
            {
                System.Collections.IList statuses =
                    GetGeneratedCollection(payment, "Statuses");
                Type statusType = statuses.GetType().GetGenericArguments()[0];
                statuses.Add(Enum.Parse(statusType, "Rejected"));
                policy.Limits.MaxCollectionCount = 1;
            }));
        ValidationReport binaryLength = ValidateGeneratedPrimitiveMutation(
            registry, (model, payment, policy) =>
            {
                SetGeneratedBinary(payment, new byte[] { 1, 2, 3 });
            });
        ValidationReport binaryMinimumLength = ValidateGeneratedPrimitiveMutation(
            registry, (model, payment, policy) =>
            {
                SetGeneratedBinary(payment, Array.Empty<byte>());
            });
        ValidationReport binaryMaximumLength = ValidateGeneratedPrimitiveMutation(
            registry, (model, payment, policy) =>
            {
                SetGeneratedBinary(payment, new byte[] { 1, 2, 3, 4 });
            });

        AssertGeneratedMutationReports(reports, ValidationStage.PrimitiveFacet);
        AssertGeneratedMutationReport(
            binaryLength, "validation.primitive.length", ValidationStage.PrimitiveFacet);
        AssertGeneratedMutationReport(binaryMinimumLength,
            "validation.primitive.minimum_length", ValidationStage.PrimitiveFacet);
        AssertGeneratedMutationReport(binaryMaximumLength,
            "validation.primitive.maximum_length", ValidationStage.PrimitiveFacet);
    }

    [Test]
    public void GeneratedPrimitiveFacetValidationChecksTextNumericAndProvenance()
    {
        GenerationPlan plan = CreatePrimitiveValidationPlan();
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        IsoTextValue text = FindGeneratedTextValue(payment, "Name");
        IsoAmountValue amount = AddGeneratedAmount(payment);
        SetTextValue(text, "ABC");
        SetDecimalValue(amount.Amount, 123.45m, 2, "123.45");

        ValidationReport valid = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        SetTextValue(text, "ABCDEF");
        ValidationReport length = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        SetTextValue(text, "AbC");
        ValidationReport pattern = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        SetTextValue(text, "DEF");
        ValidationReport enumeration = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        SetTextValue(text, " ABC");
        ValidationReport whitespace = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        SetTextValue(text, "ABC");
        SetDecimalValue(amount.Amount, 12.345m, 3, "12.345");
        ValidationReport fraction = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        SetDecimalValue(amount.Amount, -1m, 0, "-1");
        ValidationReport minimum = ValidateGeneratedStage(
            registry, "ValidateMessagePrimitiveFacets", model);
        ValidationDiagnostic provenance = minimum.Diagnostics.Single(item =>
            item.Code == "validation.primitive.minimum_inclusive");
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace
        };
        request.Policy.Stages.Add(ValidationStage.PrimitiveFacet);
        ValidationReport pipeline = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(length.Diagnostics.Select(item => item.Code),
                Does.Contain("validation.primitive.maximum_length"));
            Assert.That(pattern.Diagnostics.Select(item => item.Code),
                Does.Contain("validation.primitive.pattern"));
            Assert.That(enumeration.Diagnostics.Select(item => item.Code),
                Does.Contain("validation.primitive.enumeration"));
            Assert.That(whitespace.Diagnostics.Select(item => item.Code),
                Does.Contain("validation.primitive.white_space"));
            Assert.That(fraction.Diagnostics.Select(item => item.Code),
                Does.Contain("validation.primitive.fraction_digits"));
            Assert.That(provenance.Stage, Is.EqualTo(ValidationStage.PrimitiveFacet));
            Assert.That(provenance.Path.Value, Is.EqualTo("/Document/PmtInf/Amt"));
            Assert.That(provenance.SourceIdentifier, Is.EqualTo("type-amount"));
            Assert.That(provenance.SourceArtifact, Is.Not.Null);
            Assert.That(provenance.SourceArtifact!.Coordinate.Kind,
                Is.EqualTo(ArtifactKind.Repository));
            Assert.That(provenance.SourceArtifact.Coordinate.Identifier,
                Is.EqualTo("iso20022-repository"));
            Assert.That(provenance.SourceArtifact.Coordinate.Edition,
                Is.EqualTo("2026-06-26"));
            Assert.That(provenance.SourceArtifact.Hash.Value,
                Is.EqualTo(plan.Repository!.Provenance.Artifact.Hash.Value));
            Assert.That(pipeline.Success, Is.True, pipeline.ErrorMessage);
            Assert.That(pipeline.Valid, Is.False);
            Assert.That(pipeline.Diagnostics.Select(item => item.Code),
                Does.Contain("validation.primitive.minimum_inclusive"));
            Assert.That(pipeline.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.PrimitiveFacet }));
        });
    }

    [Test]
    public void GeneratedClosedCodeValidationRejectsUnknownValuesAndRunsInPipeline()
    {
        GenerationPlan plan = CreatePrimitiveValidationPlan();
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        IsoTextValue text = FindGeneratedTextValue(payment, "Name");
        SetTextValue(text, "ABC");
        IsoAmountValue amount = AddGeneratedAmount(payment);
        SetDecimalValue(amount.Amount, 123.45m, 2, "123.45");
        ValidationReport valid = ValidateGeneratedStage(
            registry, "ValidateMessageInternalCodes", model);
        FieldInfo statusesField = FindField(payment.GetType(), "Statuses");
        System.Collections.IList statuses =
            (System.Collections.IList)statusesField.GetValue(payment)!;
        Type statusType = statusesField.FieldType.GetGenericArguments()[0];
        statuses.Clear();
        statuses.Add(Enum.Parse(statusType, "Unknown"));
        ValidationReport invalid = ValidateGeneratedStage(
            registry, "ValidateMessageInternalCodes", model);
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace
        };
        request.Policy.Stages.Add(ValidationStage.InternalCode);
        ValidationReport pipeline = new MessageValidator().ValidateMessage(request);
        ValidationDiagnostic diagnostic = invalid.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True, valid.ErrorMessage);
            Assert.That(valid.Valid, Is.True);
            Assert.That(invalid.Success, Is.True);
            Assert.That(invalid.Valid, Is.False);
            Assert.That(diagnostic.Code,
                Is.EqualTo("validation.internal_code.unregistered"));
            Assert.That(diagnostic.Stage, Is.EqualTo(ValidationStage.InternalCode));
            Assert.That(diagnostic.Path.Value, Is.EqualTo("/Document/PmtInf/Sts"));
            Assert.That(diagnostic.SourceIdentifier, Is.EqualTo("codes-status"));
            Assert.That(diagnostic.SourceArtifact, Is.Not.Null);
            Assert.That(diagnostic.SourceArtifact!.Coordinate.Identifier,
                Is.EqualTo("iso20022-repository"));
            Assert.That(pipeline.Success, Is.True, pipeline.ErrorMessage);
            Assert.That(pipeline.Valid, Is.False);
            Assert.That(pipeline.Diagnostics.Single().Code,
                Is.EqualTo("validation.internal_code.unregistered"));
            Assert.That(pipeline.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.InternalCode }));
        });
    }

    [Test]
    public void GeneratedExternalCodeValidationSelectsPinnedAndLatestEditions()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        IsoCodeValue code = AddGeneratedExternalCode(payment, "USD");
        ExternalCodeCatalog first = CreateValidationExternalCatalog("2026-Q1");
        ExternalCodeCatalog latestCatalog =
            CreateValidationExternalCatalog("2026-Q2");
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { latestCatalog, first });
        ValidationRequest pinnedRequest = CreateExternalValidationRequest(
            model, provider, "2026-Q1", false);
        ValidationRequest latestRequest = CreateExternalValidationRequest(
            model, provider, string.Empty, true);
        ValidationRequest missingProvider = CreateExternalValidationRequest(
            model, null, string.Empty, true);
        ValidationRequest unavailableRequest = CreateExternalValidationRequest(
            model, provider, "2026-Q9", false);

        ValidationReport pinned = new MessageValidator().ValidateMessage(pinnedRequest);
        ValidationReport latest = new MessageValidator().ValidateMessage(latestRequest);
        ValidationReport missing =
            new MessageValidator().ValidateMessage(missingProvider);
        ValidationReport unavailable =
            new MessageValidator().ValidateMessage(unavailableRequest);
        code.Value = string.Empty;
        code.LexicalValue = string.Empty;
        ValidationReport missingValue =
            new MessageValidator().ValidateMessage(pinnedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(pinned.Success, Is.True, pinned.ErrorMessage);
            Assert.That(pinned.Valid, Is.True);
            Assert.That(pinned.ExternalCodeEdition, Is.EqualTo("2026-Q1"));
            Assert.That(pinned.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.ExternalCode }));
            Assert.That(latest.Success, Is.True, latest.ErrorMessage);
            Assert.That(latest.Valid, Is.True);
            Assert.That(latest.ExternalCodeEdition, Is.EqualTo("2026-Q2"));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Valid, Is.False);
            Assert.That(missing.ErrorCode,
                Is.EqualTo("validation.external_code.provider_missing"));
            Assert.That(missing.Diagnostics.Single().Stage,
                Is.EqualTo(ValidationStage.ExternalCode));
            Assert.That(unavailable.Success, Is.False);
            Assert.That(unavailable.Valid, Is.False);
            Assert.That(unavailable.ErrorCode,
                Is.EqualTo("validation.external_code.edition_unavailable"));
            Assert.That(unavailable.Diagnostics.Single().Code,
                Is.EqualTo("validation.external_code.edition_unavailable"));
            Assert.That(missingValue.Success, Is.True, missingValue.ErrorMessage);
            Assert.That(missingValue.Valid, Is.False);
            Assert.That(missingValue.Diagnostics.Single().Code,
                Is.EqualTo("validation.external_code.value_missing"));
            Assert.That(missingValue.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.ExternalCode }));
        });
    }

    [Test]
    public void GeneratedExternalCodeValidationAppliesActivityAndInactivePolicies()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        IsoCodeValue code = AddGeneratedExternalCode(payment, "USD");
        ExternalCodeCatalog catalog = CreateValidationExternalCatalog("2026-Q2");
        ExternalCodeValue usd = catalog.CodeSets[0].Values.Single(value =>
            value.Value == "USD");
        usd.ActivationDate = CreateValidationDate("2026-01-01");
        usd.DeactivationDate = CreateValidationDate("2027-01-01");
        catalog.CodeSets[0].Values.Add(new ExternalCodeValue
        {
            Value = "OLD",
            Name = "Obsolete currency",
            RegistrationStatus = "Obsolete"
        });
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { catalog });
        ValidationRequest request = CreateExternalValidationRequest(
            model, provider, "2026-Q2", false);
        request.Context.EffectiveAt = CreateValidationDateTime("2025-12-31");
        ValidationReport before = new MessageValidator().ValidateMessage(request);
        request.Context.EffectiveAt = CreateValidationDateTime("2026-01-01");
        ValidationReport start = new MessageValidator().ValidateMessage(request);
        request.Context.EffectiveAt = CreateValidationDateTime("2027-01-01");
        ValidationReport end = new MessageValidator().ValidateMessage(request);
        code.Value = "OLD";
        code.LexicalValue = "OLD";
        request.Context.EffectiveAt = null;
        ValidationReport inactive = new MessageValidator().ValidateMessage(request);
        request.Policy.IncludeInactiveExternalCodes = true;
        ValidationReport inactiveIncluded =
            new MessageValidator().ValidateMessage(request);
        ValidationDiagnostic beforeDiagnostic = before.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(before.Success, Is.True, before.ErrorMessage);
            Assert.That(before.Valid, Is.False);
            Assert.That(beforeDiagnostic.Code,
                Is.EqualTo("validation.external_code.not_found"));
            Assert.That(beforeDiagnostic.Path.Value,
                Is.EqualTo("/Document/PmtInf/Ccy"));
            Assert.That(beforeDiagnostic.SourceIdentifier,
                Is.EqualTo("codes-currency"));
            Assert.That(beforeDiagnostic.SourceArtifact, Is.SameAs(catalog.Artifact));
            Assert.That(before.ExternalCodeEdition, Is.EqualTo("2026-Q2"));
            Assert.That(start.Success, Is.True, start.ErrorMessage);
            Assert.That(start.Valid, Is.True);
            Assert.That(end.Success, Is.True, end.ErrorMessage);
            Assert.That(end.Valid, Is.False);
            Assert.That(inactive.Valid, Is.False);
            Assert.That(inactive.Diagnostics.Single().Code,
                Is.EqualTo("validation.external_code.not_found"));
            Assert.That(inactiveIncluded.Success, Is.True,
                inactiveIncluded.ErrorMessage);
            Assert.That(inactiveIncluded.Valid, Is.True);
            Assert.That(inactiveIncluded.ExternalCodeEdition,
                Is.EqualTo("2026-Q2"));
        });
    }

    [Test]
    public void GeneratedRepositoryConstraintValidationDispatchesApplicableOwnersWithProvenance()
    {
        GenerationPlan plan = CreateRepositoryConstraintValidationPlan();
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        AddRequiredGeneratedStatus(model);
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace
        };
        request.Policy.Stages.Add(ValidationStage.RepositoryConstraint);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.True);
            Assert.That(report.WarningCount, Is.EqualTo(9));
            Assert.That(report.ErrorCount, Is.EqualTo(0));
            Assert.That(report.CompletedStages,
                Is.EqualTo(new[] { ValidationStage.RepositoryConstraint }));
            Assert.That(report.Diagnostics.Select(item => item.RuleIdentifier),
                Is.EqualTo(new[]
                {
                    "constraint-repository",
                    "constraint-message",
                    "constraint-block",
                    "constraint-component",
                    "constraint-element-name",
                    "constraint-type-text",
                    "constraint-element-statuses",
                    "constraint-code-set",
                    "constraint-code-value"
                }));
            Assert.That(report.Diagnostics.Select(item => item.Path.Value),
                Is.EqualTo(new[]
                {
                    "/Document",
                    "/Document",
                    "/Document/PmtInf",
                    "/Document/PmtInf",
                    "/Document/PmtInf/Nm",
                    "/Document/PmtInf/Nm",
                    "/Document/PmtInf/Sts",
                    "/Document/PmtInf/Sts",
                    "/Document/PmtInf/Sts"
                }));
            Assert.That(report.Diagnostics.Select(item => item.Severity),
                Has.All.EqualTo(ValidationSeverity.Warning));
            Assert.That(report.Diagnostics.Select(item => item.Kind),
                Has.All.EqualTo(DiagnosticKind.UnsupportedConstraint));
            Assert.That(report.Diagnostics.Select(item => item.Stage),
                Has.All.EqualTo(ValidationStage.RepositoryConstraint));
            Assert.That(report.Diagnostics.Select(item => item.SourceArtifact),
                Has.All.Not.Null);
            Assert.That(report.Diagnostics.Select(item =>
                    item.SourceArtifact!.Coordinate.Identifier),
                Has.All.EqualTo("iso20022-repository"));
            Assert.That(report.Diagnostics.Select(item => item.RuleIdentifier),
                Does.Not.Contain("constraint-unused"));
            Assert.That(report.Diagnostics.Select(item => item.RuleIdentifier),
                Does.Not.Contain("constraint-choice"));
        });
    }

    [Test]
    public void GeneratedRepositoryConstraintWarningsHonorStrictFailFastPolicy()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryConstraintValidationPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        AddRequiredGeneratedStatus(model);
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace,
            Policy = new ValidationPolicy
            {
                TreatWarningsAsErrors = true,
                FailFast = true
            }
        };
        request.Policy.Stages.Add(ValidationStage.RepositoryConstraint);

        ValidationReport report = new MessageValidator().ValidateMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(report.Success, Is.True, report.ErrorMessage);
            Assert.That(report.Valid, Is.False);
            Assert.That(report.StoppedEarly, Is.True);
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.Diagnostics.Single().RuleIdentifier,
                Is.EqualTo("constraint-repository"));
            Assert.That(report.Diagnostics.Single().ExpectedConstraint,
                Is.EqualTo("self.isConsistent()"));
            Assert.That(report.Diagnostics.Single().Message,
                Does.Contain("OCL"));
        });
    }

    [Test]
    public void GeneratedReadersHandleSequencesChoicesNilAttributesAndSimpleContent()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateReaderPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        string xml =
            "<Document xmlns=\"" + MessageNamespace + "\" " +
            "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
            "<PmtInf>" +
            "<Nm>Alice</Nm>" +
            "<Sts>ACCP</Sts><Sts>RJCT</Sts>" +
            "<OptnlSts>ACCP</OptnlSts>" +
            "<Acct><IBAN>GB82WEST12345698765432</IBAN></Acct>" +
            "<Ccy>USD</Ccy>" +
            "<SgntrData xsi:nil=\"true\"/>" +
            "<AcctSvcr>Example Bank</AcctSvcr>" +
            "<Amt Ccy=\"USD\">12.30</Amt>" +
            "</PmtInf></Document>";
        object root;
        using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(
            new StringReader(xml)))
        {
            root = InvokeInternal(rootCodec, "ReadModel", new object[] { reader })!;
        }
        object payment = FindField(root.GetType(), "PaymentInformation").GetValue(root)!;
        object name = FindField(payment.GetType(), "Name").GetValue(payment)!;
        IsoTextValue nameValue = (IsoTextValue)FindField(name.GetType(), "Value")
            .GetValue(name)!;
        System.Collections.IList statuses = (System.Collections.IList)FindField(
            payment.GetType(), "Statuses").GetValue(payment)!;
        object account = FindField(payment.GetType(), "Account").GetValue(payment)!;
        IsoTextValue ibanValue = (IsoTextValue)FindField(account.GetType(), "Iban")
            .GetValue(account)!;
        object currency = FindField(payment.GetType(), "Currency").GetValue(payment)!;
        IsoCodeValue currencyValue = (IsoCodeValue)FindField(currency.GetType(), "Value")
            .GetValue(currency)!;
        object amount = FindField(payment.GetType(), "Amount").GetValue(payment)!;
        IsoAmountValue amountValue = (IsoAmountValue)FindField(amount.GetType(), "Value")
            .GetValue(amount)!;

        Assert.Multiple(() =>
        {
            Assert.That(nameValue.Value, Is.EqualTo("Alice"));
            Assert.That(nameValue.LexicalValue, Is.EqualTo("Alice"));
            Assert.That(statuses.Cast<object>().Select(value => value.ToString()),
                Is.EqualTo(new[] { "Accepted", "Rejected" }));
            Assert.That(FindField(payment.GetType(), "OptionalStatus").GetValue(payment)
                ?.ToString(), Is.EqualTo("Accepted"));
            Assert.That(FindField(payment.GetType(), "HasOptionalStatus").GetValue(payment),
                Is.EqualTo(true));
            Assert.That(FindField(account.GetType(), "Choice").GetValue(account)?.ToString(),
                Is.EqualTo("Iban"));
            Assert.That(ibanValue.Value, Is.EqualTo("GB82WEST12345698765432"));
            Assert.That(currencyValue.Value, Is.EqualTo("USD"));
            Assert.That(currencyValue.CodeSetName, Is.EqualTo("ExternalCurrencyCode"));
            Assert.That(FindField(payment.GetType(), "SignatureData").GetValue(payment),
                Is.Null);
            Assert.That(((IsoTextValue)FindField(payment.GetType(), "AccountServicer")
                .GetValue(payment)!).Value, Is.EqualTo("Example Bank"));
            Assert.That(amountValue.Currency, Is.EqualTo("USD"));
            Assert.That(amountValue.Amount.Value, Is.EqualTo(12.30m));
            Assert.That(amountValue.Amount.Scale, Is.EqualTo(2));
            Assert.That(amountValue.Amount.LexicalValue, Is.EqualTo("12.30"));
        });
    }

    [TestCase("<Sts>ACCP</Sts><Nm>Alice</Nm>", "missing or out of order")]
    [TestCase("<Nm>Alice</Nm>", "Required element 'Sts'")]
    [TestCase("<Nm>Alice</Nm><Sts>ACCP</Sts><Unknown/>", "Unexpected element")]
    public void GeneratedReadersRejectMissingUnknownAndOutOfOrderContent(
        string content,
        string expectedMessage)
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        string xml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            content + "</PmtInf></Document>";

        TargetInvocationException? exception;
        using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(
            new StringReader(xml)))
        {
            exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeInternal(rootCodec, "ReadModel", new object[] { reader }));
        }

        Assert.That(exception?.InnerException, Is.TypeOf<System.Xml.XmlException>());
        Assert.That(exception?.InnerException?.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public void GeneratedWritersEmitExactOrderedXmlWithStableNamespaceAssignment()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateReaderPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        object model = ReadGeneratedDocument(assembly,
            "<Document xmlns=\"" + MessageNamespace + "\" " +
            "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
            "<PmtInf><Nm>Alice</Nm><Sts>ACCP</Sts><Sts>RJCT</Sts>" +
            "<OptnlSts>ACCP</OptnlSts>" +
            "<Acct><IBAN>GB82WEST12345698765432</IBAN></Acct>" +
            "<Ccy>USD</Ccy><SgntrData xsi:nil=\"true\"/>" +
            "<AcctSvcr>Example Bank</AcctSvcr>" +
            "<Amt Ccy=\"USD\">12.30</Amt></PmtInf></Document>");

        string first = WriteGeneratedDocument(
            rootCodec, model, "pacs.008.001.10", MessageNamespace);
        string second = WriteGeneratedDocument(
            rootCodec, model, "pacs.008.001.10", MessageNamespace);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(
                "<Document xmlns=\"" + MessageNamespace + "\">" +
                "<PmtInf><Nm>Alice</Nm><Sts>ACCP</Sts><Sts>RJCT</Sts>" +
                "<OptnlSts>ACCP</OptnlSts>" +
                "<Acct><IBAN>GB82WEST12345698765432</IBAN></Acct>" +
                "<Ccy>USD</Ccy><AcctSvcr>Example Bank</AcctSvcr>" +
                "<Amt Ccy=\"USD\">12.30</Amt></PmtInf></Document>"));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.Not.Contain("xsi:nil"));
            Assert.That(first, Does.Not.Contain("p1:"));
        });
    }

    [Test]
    public void GeneratedWritersGuardVersionModelRequiredMultiplicityAndChoiceState()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateReaderPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        string xml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            "<Nm>Alice</Nm><Sts>ACCP</Sts>" +
            "<Acct><IBAN>GB82WEST12345698765432</IBAN></Acct>" +
            "<Amt Ccy=\"USD\">12.30</Amt>" +
            "</PmtInf></Document>";
        object model = ReadGeneratedDocument(assembly, xml);

        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.11",
            MessageNamespace, typeof(ArgumentException), "message identifier");
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            "urn:wrong-version", typeof(ArgumentException), "namespace");
        AssertGeneratedWriteFailure(rootCodec, null, "pacs.008.001.10",
            MessageNamespace, typeof(InvalidOperationException), "model is required");

        object payment = FindField(model.GetType(), "PaymentInformation").GetValue(model)!;
        object name = FindField(payment.GetType(), "Name").GetValue(payment)!;
        FindField(payment.GetType(), "Name").SetValue(payment, null);
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            MessageNamespace, typeof(InvalidOperationException), "Required element 'Nm'");
        FindField(payment.GetType(), "Name").SetValue(payment, name);

        System.Collections.IList statuses = (System.Collections.IList)FindField(
            payment.GetType(), "Statuses").GetValue(payment)!;
        object accepted = statuses[0]!;
        statuses.Clear();
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            MessageNamespace, typeof(InvalidOperationException), "minimum multiplicity");
        statuses.Add(accepted);
        statuses.Add(accepted);
        statuses.Add(accepted);
        statuses.Add(accepted);
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            MessageNamespace, typeof(InvalidOperationException), "maximum multiplicity");
        while (statuses.Count > 1)
        {
            statuses.RemoveAt(statuses.Count - 1);
        }

        object amount = FindField(payment.GetType(), "Amount").GetValue(payment)!;
        IsoAmountValue amountValue = (IsoAmountValue)FindField(amount.GetType(), "Value")
            .GetValue(amount)!;
        amountValue.Currency = string.Empty;
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            MessageNamespace, typeof(InvalidOperationException), "currency attribute");
        amountValue.Currency = "USD";

        object account = FindField(payment.GetType(), "Account").GetValue(payment)!;
        Type choiceKind = FindField(account.GetType(), "Choice").FieldType;
        FindField(account.GetType(), "Choice").SetValue(
            account, Enum.Parse(choiceKind, "Bic"));
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            MessageNamespace, typeof(InvalidOperationException), "choice discriminator");
    }

    [Test]
    public void GeneratedWildcardCodecsPreserveRawMarkupAndNamespaceClosure()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateReaderPlan());
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type rootCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        string xml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            "<Nm>Alice</Nm><Sts>ACCP</Sts>" +
            "<SplmtryData xmlns:ext=\"urn:extension\" xmlns:meta=\"urn:meta\">" +
            "<ext:Extension meta:id=\"17\" plain=\"yes\">before" +
            "<!--inside--><![CDATA[value<]]><Local>main</Local>" +
            "<ext:Child meta:code=\"A\"/></ext:Extension>" +
            "</SplmtryData></PmtInf></Document>";
        object model = ReadGeneratedDocument(assembly, xml);
        object payment = FindField(model.GetType(), "PaymentInformation").GetValue(model)!;
        object supplementary = FindField(payment.GetType(), "SupplementaryData")
            .GetValue(payment)!;
        RawXmlFragment fragment = (RawXmlFragment)FindField(
            supplementary.GetType(), "Value").GetValue(supplementary)!;

        string first = WriteGeneratedDocument(
            rootCodec, model, "pacs.008.001.10", MessageNamespace);
        object repeatedModel = ReadGeneratedDocument(assembly, first);
        string repeated = WriteGeneratedDocument(
            rootCodec, repeatedModel, "pacs.008.001.10", MessageNamespace);
        byte[] preservedBytes = fragment.Utf8Bytes;
        fragment.Xml = string.Empty;
        string byteBacked = WriteGeneratedDocument(
            rootCodec, model, "pacs.008.001.10", MessageNamespace);

        System.Xml.XmlDocument output = new System.Xml.XmlDocument
        {
            PreserveWhitespace = true
        };
        output.LoadXml(first);
        System.Xml.XmlElement extension = (System.Xml.XmlElement)output
            .GetElementsByTagName("Extension", "urn:extension")[0]!;
        System.Xml.XmlElement local = (System.Xml.XmlElement)extension
            .GetElementsByTagName("Local", MessageNamespace)[0]!;
        Assert.Multiple(() =>
        {
            Assert.That(fragment.RootName?.Prefix, Is.EqualTo("ext"));
            Assert.That(fragment.RootName?.LocalName, Is.EqualTo("Extension"));
            Assert.That(fragment.RootName?.NamespaceUri, Is.EqualTo("urn:extension"));
            Assert.That(fragment.NamespaceDeclarations.Select(value => value.Prefix),
                Does.Contain("ext").And.Contain("meta"));
            Assert.That(fragment.NamespaceDeclarations.Single(value =>
                value.Prefix == "ext").NamespaceUri, Is.EqualTo("urn:extension"));
            Assert.That(Encoding.UTF8.GetString(preservedBytes),
                Does.Contain("<ext:Extension"));
            Assert.That(first, Does.Contain("xmlns:ext=\"urn:extension\""));
            Assert.That(first, Does.Contain("xmlns:meta=\"urn:meta\""));
            Assert.That(first, Does.Contain("meta:id=\"17\""));
            Assert.That(first, Does.Contain("<!--inside-->"));
            Assert.That(first, Does.Contain("<![CDATA[value<]]>"));
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(byteBacked, Is.EqualTo(first));
            Assert.That(extension.Prefix, Is.EqualTo("ext"));
            Assert.That(extension.GetAttribute("id", "urn:meta"), Is.EqualTo("17"));
            Assert.That(local.NamespaceURI, Is.EqualTo(MessageNamespace));
        });

        fragment.Xml = Encoding.UTF8.GetString(preservedBytes);
        fragment.RootName!.LocalName = "Different";
        AssertGeneratedWriteFailure(rootCodec, model, "pacs.008.001.10",
            MessageNamespace, typeof(System.Xml.XmlException), "declared root name");
    }

    [Test]
    public void GeneratedRegistryCreatesOnlyExactSelectedMessageModels()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        MessageDefinitionMetadata second = new MessageDefinitionMetadata
        {
            Identifier = "message-2",
            Name = "FinancialInstitutionCreditTransferV10",
            NamespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.10",
            RootElement = "Document",
            MessageIdentifier = new MessageIdentifier
            {
                Value = "pacs.009.001.10",
                BusinessArea = "pacs",
                MessageFunctionality = "009",
                Variant = "001",
                Version = "10"
            }
        };
        plan.Repository?.MessageDefinitions.Add(second);

        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        object first = InvokeInternal(
            registry, "CreateMessage", new object[] { "pacs.008.001.10" })!;
        object secondModel = InvokeInternal(
            registry, "CreateMessage", new object[] { "pacs.009.001.10" })!;
        object byNamespace = InvokeInternal(
            registry,
            "CreateMessageByNamespace",
            new object[] { "urn:iso:std:iso:20022:tech:xsd:pacs.009.001.10" })!;
        Assert.Multiple(() =>
        {
            Assert.That(first.GetType().FullName,
                Is.EqualTo("Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10"));
            Assert.That(secondModel.GetType().FullName, Is.EqualTo(
                "Contoso.Generated.Messages.Pacs.FinancialInstitutionCreditTransferV10"));
            Assert.That(byNamespace.GetType(), Is.EqualTo(secondModel.GetType()));
            Assert.That(InvokeInternal(
                registry, "CreateMessage", new object[] { "pacs.999.001.01" }), Is.Null);
            Assert.That(InvokeInternal(
                registry,
                "CreateMessageByNamespace",
                new object[] { "urn:example:unknown" }), Is.Null);
            Assert.That(InvokeInternal(
                registry, "FindModelName", new object[] { "pacs.008.001.10" }),
                Is.EqualTo(
                    "global::Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10"));
            Assert.That(InvokeInternal(
                registry, "FindMessageNamespace", new object[] { "pacs.009.001.10" }),
                Is.EqualTo(second.NamespaceUri));
            Assert.That(result.Files.Where(file => file.Kind == GenerationFileKind.Metadata)
                .Select(file => file.RelativePath), Does.Contain(
                    "Api/Internal/Generated/2026-06-26/Contoso/Generated/Internal/Iso20022GeneratedRegistry.cs"));
        });
    }

    [Test]
    public void GeneratedDispatchUsesExactPairsWithGenericFallbackAndCastDiagnostics()
    {
        GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(
            CreateRepositoryPlan(false));
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(result.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        string modelName =
            "global::Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10";
        string exactXml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            "<Nm>Alice</Nm><Sts>ACCP</Sts></PmtInf></Document>";
        object exact = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            exactXml, "pacs.008.001.10", MessageNamespace
        })!;
        object exactModel = ReadInternalField<object>(exact, "Model");
        object castModel = InvokeInternal(registry, "CastMessageModel", new object[]
        {
            exact, modelName
        })!;
        string unknownXml = File.ReadAllText(
            ForwardCompatibilityTests.GetFixturePath(
                "FutureMessageVersion.xml"));
        string unknownIdentifier = "pacs.008.001.99";
        string unknownNamespace =
            "urn:iso:std:iso:20022:tech:xsd:" + unknownIdentifier;
        object unknown = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            unknownXml,
            unknownIdentifier,
            unknownNamespace
        })!;
        object wrongNamespace = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            "<Document xmlns=\"urn:wrong\"><PmtInf/></Document>",
            "pacs.008.001.10",
            "urn:wrong"
        })!;
        object wrongIdentifier = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            exactXml,
            "pacs.777.001.01",
            MessageNamespace
        })!;
        object wrongRoot = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            "<Wrong xmlns=\"" + MessageNamespace + "\"><PmtInf/></Wrong>",
            "pacs.008.001.10",
            MessageNamespace
        })!;
        TargetInvocationException? fallbackCast = Assert.Throws<TargetInvocationException>(
            () => InvokeInternal(registry, "CastMessageModel", new object[]
            {
                unknown, modelName
            }));
        TargetInvocationException? typeCast = Assert.Throws<TargetInvocationException>(
            () => InvokeInternal(registry, "CastMessageModel", new object[]
            {
                exact, "global::Contoso.Generated.Messages.Pacs.OtherModel"
            }));
        MessageDocument? unknownDocument =
            ReadInternalField<MessageDocument?>(unknown, "GenericDocument");
        MessageWriteResult unknownWritten =
            new XmlMessageWriter().WriteGenericString(
                unknownDocument,
                new MessageWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(ReadInternalField<bool>(exact, "IsTyped"), Is.True);
            Assert.That(exactModel.GetType().FullName, Is.EqualTo(
                "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10"));
            Assert.That(castModel, Is.SameAs(exactModel));
            Assert.That(ReadInternalField<string>(exact, "ModelTypeName"),
                Is.EqualTo(modelName));
            Assert.That(ReadInternalField<string>(exact, "DiagnosticCode"), Is.Empty);
            Assert.That(ReadInternalField<MessageDocument?>(exact, "GenericDocument"),
                Is.Null);
            Assert.That(ReadInternalField<bool>(unknown, "IsTyped"), Is.False);
            Assert.That(ReadInternalField<object?>(unknown, "Model"), Is.Null);
            Assert.That(unknownDocument, Is.Not.Null);
            Assert.That(unknownDocument?.Payloads.Single().Identifier?.Value,
                Is.EqualTo(unknownIdentifier));
            Assert.That(ReadInternalField<string>(unknown, "DiagnosticCode"),
                Is.EqualTo("generation.codec.generic_fallback"));
            Assert.That(unknownWritten.Success,
                Is.True,
                unknownWritten.ErrorMessage);
            Assert.That(unknownWritten.Text,
                Does.Contain("future:Version99Data")
                    .And.Contain("unavailable generated model content"));
            Assert.That(ReadInternalField<string>(wrongNamespace, "DiagnosticCode"),
                Is.EqualTo("generation.codec.namespace_mismatch"));
            Assert.That(ReadInternalField<string>(wrongIdentifier, "DiagnosticCode"),
                Is.EqualTo("generation.codec.identifier_mismatch"));
            Assert.That(ReadInternalField<string>(wrongRoot, "DiagnosticCode"),
                Is.EqualTo("generation.codec.root_mismatch"));
            Assert.That(fallbackCast?.InnerException, Is.TypeOf<InvalidCastException>());
            Assert.That(fallbackCast?.InnerException?.Message,
                Does.Contain("generation.codec.cast_generic_fallback")
                    .And.Contain("generation.codec.generic_fallback"));
            Assert.That(typeCast?.InnerException, Is.TypeOf<InvalidCastException>());
            Assert.That(typeCast?.InnerException?.Message,
                Does.Contain("generation.codec.type_mismatch")
                    .And.Contain("OtherModel")
                    .And.Contain("CustomerCreditTransferV10"));
        });
    }

    [Test]
    public void GeneratedTypedJsonRoundTripsChoicesBinaryAmountsAndReportsWildcardLoss()
    {
        GenerationResult generated = new CSharpModelGenerator().GenerateCSharpModels(
            CreateJsonReaderPlan());
        Assert.That(generated.Success, Is.True, generated.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(generated.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        string xml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            "<Nm>Alice</Nm><Sts>ACCP</Sts>" +
            "<Acct><IBAN>GB82WEST12345698765432</IBAN></Acct>" +
            "<SgntrData>AQI=</SgntrData>" +
            "<SplmtryData xmlns:ext=\"urn:extension\"><ext:Extension>" +
            "value</ext:Extension></SplmtryData>" +
            "<Amt Ccy=\"USD\">12.30</Amt>" +
            "<CreDt>2026-08-31</CreDt><CutOffTm>12:34:56.120Z</CutOffTm>" +
            "<CreDtTm>2026-08-31T12:34:56.120Z</CreDtTm>" +
            "</PmtInf></Document>";
        object exact = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            xml, "pacs.008.001.10", MessageNamespace
        })!;

        MessageJsonResult json = (MessageJsonResult)InvokeInternal(
            registry,
            "WriteMessageJson",
            new object[] { exact })!;
        using JsonDocument parsed = JsonDocument.Parse(json.Json);
        JsonElement payment = parsed.RootElement.GetProperty("Document")
            .GetProperty("PmtInf");
        object restored = InvokeInternal(registry, "ReadMessageJson", new object[]
        {
            json.Json, "pacs.008.001.10", MessageNamespace
        })!;
        object restoredModel = ReadInternalField<object>(restored, "Model");
        object restoredPayment = FindField(
            restoredModel.GetType(),
            "PaymentInformation").GetValue(restoredModel)!;
        object restoredAccount = FindField(
            restoredPayment.GetType(),
            "Account").GetValue(restoredPayment)!;
        object restoredBinary = FindField(
            restoredPayment.GetType(),
            "SignatureData").GetValue(restoredPayment)!;
        IsoBinaryValue binary = (IsoBinaryValue)FindField(
            restoredBinary.GetType(),
            "Value").GetValue(restoredBinary)!;
        object restoredDate = FindField(
            restoredPayment.GetType(),
            "CreationDate").GetValue(restoredPayment)!;
        IsoDateValue date = (IsoDateValue)FindField(
            restoredDate.GetType(),
            "Value").GetValue(restoredDate)!;
        object restoredTime = FindField(
            restoredPayment.GetType(),
            "CutOffTime").GetValue(restoredPayment)!;
        IsoTimeValue time = (IsoTimeValue)FindField(
            restoredTime.GetType(),
            "Value").GetValue(restoredTime)!;
        object restoredDateTime = FindField(
            restoredPayment.GetType(),
            "CreationDateTime").GetValue(restoredPayment)!;
        IsoDateTimeValue dateTime = (IsoDateTimeValue)FindField(
            restoredDateTime.GetType(),
            "Value").GetValue(restoredDateTime)!;
        object invalidIdentity = InvokeInternal(
            registry,
            "ReadMessageJson",
            new object[]
            {
                json.Json.Replace(
                    "tech:json:pacs.008.001.10",
                    "tech:json:pacs.009.001.10",
                    StringComparison.Ordinal),
                "pacs.008.001.10",
                MessageNamespace
            })!;

        Assert.Multiple(() =>
        {
            Assert.That(json.Success, Is.True, json.ErrorMessage);
            Assert.That(json.SchemaIdentifier, Is.EqualTo(
                "urn:iso:std:iso:20022:tech:json:pacs.008.001.10"));
            Assert.That(json.NamespaceUri, Is.EqualTo(MessageNamespace));
            Assert.That(payment.GetProperty("Acct").GetProperty("IBAN").GetString(),
                Is.EqualTo("GB82WEST12345698765432"));
            Assert.That(payment.GetProperty("SgntrData").GetString(),
                Is.EqualTo("AQI="));
            Assert.That(payment.GetProperty("Amt").GetProperty("@Ccy").GetString(),
                Is.EqualTo("USD"));
            Assert.That(payment.GetProperty("Amt").GetProperty("$value").GetString(),
                Is.EqualTo("12.30"));
            Assert.That(payment.GetProperty("CreDt").GetString(),
                Is.EqualTo("2026-08-31"));
            Assert.That(payment.GetProperty("CutOffTm").GetString(),
                Is.EqualTo("12:34:56.120Z"));
            Assert.That(payment.GetProperty("CreDtTm").GetString(),
                Is.EqualTo("2026-08-31T12:34:56.120Z"));
            Assert.That(json.IsLossless, Is.False);
            Assert.That(json.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_namespace_loss"));
            Assert.That(ReadInternalField<bool>(restored, "IsTyped"), Is.True);
            Assert.That(FindField(restoredAccount.GetType(), "Choice")
                .GetValue(restoredAccount)?.ToString(), Is.EqualTo("Iban"));
            Assert.That(binary.Value, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(binary.LexicalValue, Is.EqualTo("AQI="));
            Assert.That(date.LexicalValue, Is.EqualTo("2026-08-31"));
            Assert.That(time.LexicalValue, Is.EqualTo("12:34:56.120Z"));
            Assert.That(time.FractionalSecondDigits, Is.EqualTo("120"));
            Assert.That(dateTime.LexicalValue,
                Is.EqualTo("2026-08-31T12:34:56.120Z"));
            Assert.That(ReadInternalField<string>(invalidIdentity, "DiagnosticCode"),
                Is.EqualTo("json.schema_namespace_mismatch"));
        });
    }

    [Test]
    public void GeneratedTypedXmlJsonXmlRoundTripRetainsPrecisionAndExactVersion()
    {
        GenerationResult generated = new CSharpModelGenerator().GenerateCSharpModels(
            CreateJsonReaderPlan());
        Assert.That(generated.Success, Is.True, generated.ErrorMessage);
        Assembly assembly = CompileGeneratedAssembly(generated.Files);
        Type registry = FindGeneratedType(
            assembly,
            "Contoso.Generated.Internal.Iso20022GeneratedRegistry");
        const string preciseAmount = "123456789012345678.1200";
        string xml = "<Document xmlns=\"" + MessageNamespace + "\"><PmtInf>" +
            "<Nm>Alice</Nm><Sts>ACCP</Sts>" +
            "<SplmtryData xmlns:ext=\"urn:extension\" xmlns:meta=\"urn:metadata\">" +
            "<ext:Extension meta:id=\"007\"><ext:Value>opaque-value</ext:Value>" +
            "</ext:Extension></SplmtryData><Amt Ccy=\"USD\">" + preciseAmount +
            "</Amt></PmtInf></Document>";
        object first = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            xml, "pacs.008.001.10", MessageNamespace
        })!;
        string typedXml = (string)InvokeInternal(
            registry,
            "WriteTypedMessageXml",
            new object[] { first })!;
        object second = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            typedXml, "pacs.008.001.10", MessageNamespace
        })!;
        MessageJsonResult json = (MessageJsonResult)InvokeInternal(
            registry,
            "WriteMessageJson",
            new object[] { second })!;
        object restored = InvokeInternal(registry, "ReadMessageJson", new object[]
        {
            json.Json, "pacs.008.001.10", MessageNamespace
        })!;
        string restoredXml = (string)InvokeInternal(
            registry,
            "WriteTypedMessageXml",
            new object[] { restored })!;
        object reread = InvokeInternal(registry, "ReadMessageText", new object[]
        {
            restoredXml, "pacs.008.001.10", MessageNamespace
        })!;
        object wrongVersion = InvokeInternal(registry, "ReadMessageJson", new object[]
        {
            json.Json, "pacs.008.001.09", MessageNamespace
        })!;
        string olderNamespace =
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.09";
        object wrongNamespace = InvokeInternal(
            registry,
            "ReadMessageJson",
            new object[] { json.Json, "pacs.008.001.10", olderNamespace })!;
        object restoredModel = ReadInternalField<object>(restored, "Model");
        object restoredPayment = FindField(
            restoredModel.GetType(),
            "PaymentInformation").GetValue(restoredModel)!;
        object restoredAmount = FindField(
            restoredPayment.GetType(),
            "Amount").GetValue(restoredPayment)!;
        IsoAmountValue amount = (IsoAmountValue)FindField(
            restoredAmount.GetType(),
            "Value").GetValue(restoredAmount)!;
        System.Xml.XmlDocument typedDocument = new System.Xml.XmlDocument();
        typedDocument.LoadXml(typedXml);
        System.Xml.XmlDocument restoredDocument = new System.Xml.XmlDocument();
        restoredDocument.LoadXml(restoredXml);
        System.Xml.XmlElement typedExtension = (System.Xml.XmlElement)typedDocument
            .GetElementsByTagName("Extension", "urn:extension")[0]!;
        System.Xml.XmlElement finalExtension = (System.Xml.XmlElement)restoredDocument
            .GetElementsByTagName("Extension", MessageNamespace)[0]!;
        System.Xml.XmlElement finalAmount = (System.Xml.XmlElement)restoredDocument
            .GetElementsByTagName("Amt", MessageNamespace)[0]!;

        Assert.Multiple(() =>
        {
            Assert.That(ReadInternalField<bool>(first, "IsTyped"), Is.True);
            Assert.That(ReadInternalField<bool>(second, "IsTyped"), Is.True);
            Assert.That(ReadInternalField<bool>(restored, "IsTyped"), Is.True);
            Assert.That(ReadInternalField<bool>(reread, "IsTyped"), Is.True);
            Assert.That(ReadInternalField<string>(restored, "MessageIdentifier"),
                Is.EqualTo("pacs.008.001.10"));
            Assert.That(ReadInternalField<string>(restored, "NamespaceUri"),
                Is.EqualTo(MessageNamespace));
            Assert.That(typedDocument.DocumentElement?.NamespaceURI,
                Is.EqualTo(MessageNamespace));
            Assert.That(restoredDocument.DocumentElement?.NamespaceURI,
                Is.EqualTo(MessageNamespace));
            Assert.That(typedExtension.GetAttribute("id", "urn:metadata"),
                Is.EqualTo("007"));
            Assert.That(typedExtension.InnerText, Is.EqualTo("opaque-value"));
            Assert.That(finalExtension.NamespaceURI, Is.EqualTo(MessageNamespace));
            Assert.That(finalExtension.GetAttribute("id"), Is.EqualTo("007"));
            Assert.That(finalExtension.InnerText, Is.EqualTo("opaque-value"));
            Assert.That(finalAmount.GetAttribute("Ccy"), Is.EqualTo("USD"));
            Assert.That(finalAmount.InnerText, Is.EqualTo(preciseAmount));
            Assert.That(amount.Amount.LexicalValue, Is.EqualTo(preciseAmount));
            Assert.That(amount.Amount.Scale, Is.EqualTo(4));
            Assert.That(json.IsLossless, Is.False);
            Assert.That(json.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_namespace_loss"));
            Assert.That(json.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_attribute_namespace_loss"));
            Assert.That(ReadInternalField<string>(wrongVersion, "DiagnosticCode"),
                Is.EqualTo("generation.codec.identifier_mismatch"));
            Assert.That(ReadInternalField<string>(wrongNamespace, "DiagnosticCode"),
                Is.EqualTo("generation.codec.namespace_mismatch"));
        });
    }

    [Test]
    public void InvalidSelectionAndResourceLimitsReturnStableErrors()
    {
        ICSharpModelGenerator generator = new CSharpModelGenerator();
        GenerationResult missing = generator.GenerateCSharpModels(null);
        GenerationResult noSources = generator.GenerateCSharpModels(new GenerationPlan());
        GenerationPlan selectionPlan = CreateRepositoryPlan(false);
        selectionPlan.Options.MessageIdentifiers.Add(new MessageIdentifier
        {
            Value = "pacs.999.001.01"
        });
        GenerationResult empty = generator.GenerateCSharpModels(selectionPlan);
        GenerationPlan boundedPlan = CreateRepositoryPlan(false);
        boundedPlan.Options.Limits.MaxTotalBytes = 32;
        GenerationResult bounded = generator.GenerateCSharpModels(boundedPlan);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.ErrorCode, Is.EqualTo("generation.plan.required"));
            Assert.That(noSources.Success, Is.False);
            Assert.That(noSources.ErrorCode, Is.EqualTo("generation.sources.required"));
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.ErrorCode, Is.EqualTo("generation.csharp.selection_empty"));
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.ErrorCode, Is.EqualTo("generation.csharp.byte_limit"));
        });
    }

    [Test]
    public void EmptyModelModelLimitAndFileLimitReturnSpecificFailures()
    {
        GenerationPlan emptyPlan = new GenerationPlan();
        emptyPlan.Schemas.Add(new SchemaArtifact
        {
            SourceName = "empty.xsd",
            NamespaceUri = "urn:example:empty"
        });

        GenerationPlan modelPlan = CreateRepositoryPlan(false);
        modelPlan.Options.Limits.MaxCollectionCount = 1;

        GenerationPlan filePlan = CreateRepositoryPlan(false);
        XsdGenerator planner = new XsdGenerator();
        List<XsdGenerationUnit> units = planner.BuildGenerationUnits(filePlan);
        CSharpGenerationContext context = CSharpModelEmitter.CreateCSharpContext(
            filePlan,
            units,
            new GenerationResult());
        filePlan.Options.Limits.MaxCollectionCount = context.Symbols.Symbols.Count;
        filePlan.Options.Limits.MaxElementCount = context.Symbols.Symbols.Count;

        GenerationResult empty = new CSharpModelGenerator().GenerateCSharpModels(emptyPlan);
        GenerationResult model = new CSharpModelGenerator().GenerateCSharpModels(modelPlan);
        GenerationResult files = new CSharpModelGenerator().GenerateCSharpModels(filePlan);

        Assert.Multiple(() =>
        {
            Assert.That(empty.ErrorCode, Is.EqualTo("generation.csharp.model_empty"));
            Assert.That(model.ErrorCode, Is.EqualTo("generation.csharp.model_limit"));
            Assert.That(files.ErrorCode, Is.EqualTo("generation.csharp.file_limit"));
        });
    }

    [Test]
    public void IncrementalCSharpCollisionIsReportedAndUserFileIsPreserved()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-csharp-collision-" + Guid.NewGuid().ToString("N"));
        try
        {
            GenerationPlan plan = CreateRepositoryPlan(false);
            plan.Options.OutputRootPath = root;
            GenerationResult preview = new CSharpModelGenerator().GenerateCSharpModels(plan);
            Assert.That(preview.Success, Is.True, preview.ErrorMessage);
            GeneratedFile owned = preview.Files.First();
            Directory.Delete(root, true);
            string collision = Path.Combine(
                root,
                owned.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(collision)!);
            File.WriteAllText(collision, "user-owned", Encoding.UTF8);

            GenerationResult result = new CSharpModelGenerator().GenerateCSharpModels(plan);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode,
                    Is.EqualTo("generation.incremental.unowned_collision"));
                Assert.That(File.ReadAllText(collision, Encoding.UTF8), Is.EqualTo("user-owned"));
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
    public void GeneratedFileOmitsAnAbsentSourceIdentifier()
    {
        CSharpGeneratedSource source = new CSharpGeneratedSource
        {
            Kind = GenerationFileKind.CSharp,
            RelativePath = "Api/Data/Generated/Empty.cs",
            NamespaceName = "Example.Generated",
            Content = "source"u8.ToArray()
        };

        GeneratedFile file = new CSharpModelGenerator().CreateCSharpGeneratedFile(source);

        Assert.Multiple(() =>
        {
            Assert.That(file.SourceIdentifiers, Is.Empty);
            Assert.That(file.Content, Is.EqualTo(source.Content));
            Assert.That(file.Hash.Value, Has.Length.EqualTo(32));
        });
    }

    private static void ConfigureSourceArtifact(GenerationPlan plan, byte[] sourceHash)
    {
        ArtifactManifest artifact = plan.Repository!.Provenance.Artifact;
        artifact.Coordinate.Kind = ArtifactKind.Repository;
        artifact.Coordinate.Identifier = "iso20022-repository";
        artifact.Coordinate.Edition = "2026-06-26";
        artifact.Coordinate.Version = "2013-ecore";
        artifact.MediaType = Iso20022MediaTypes.Xml;
        artifact.Hash.Value = sourceHash;
        artifact.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        artifact.HashAlgorithm = "SHA-256";
        artifact.IsApproved = true;
    }

    private static Assembly CompileGeneratedAssembly(List<GeneratedFile> files)
    {
        List<SyntaxTree> trees = new List<SyntaxTree>();
        int index = 0;
        while (index < files.Count)
        {
            if (files[index].Kind == GenerationFileKind.CSharp ||
                files[index].Kind == GenerationFileKind.Metadata)
            {
                trees.Add(CSharpSyntaxTree.ParseText(
                    Encoding.UTF8.GetString(files[index].Content),
                    path: files[index].RelativePath));
            }
            index++;
        }
        List<MetadataReference> references = CreateCompilationReferences();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratedIso20022Models",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream output = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(output);
        string errors = string.Join(
            Environment.NewLine,
            emitted.Diagnostics.Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage())
                .Distinct(StringComparer.Ordinal)
                .Take(100));
        Assert.That(emitted.Success, Is.True, errors);
        return Assembly.Load(output.ToArray());
    }

    private static List<MetadataReference> CreateCompilationReferences()
    {
        List<MetadataReference> references = new List<MetadataReference>();
        string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ??
            string.Empty;
        foreach (string path in trusted.Split(Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries))
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }
        references.Add(MetadataReference.CreateFromFile(typeof(GenerationPlan).Assembly.Location));
        return references;
    }

    private static Type FindGeneratedType(Assembly assembly, string name)
    {
        Type? type = assembly.GetType(name);
        Assert.That(type, Is.Not.Null, "Missing generated type " + name);
        return type!;
    }

    private static FieldInfo FindField(Type type, string name)
    {
        FieldInfo? field = type.GetField(name);
        Assert.That(field, Is.Not.Null, "Missing generated field " + name);
        return field!;
    }

    private static T ReadStaticField<T>(Type type, string name)
    {
        FieldInfo? field = type.GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(field, Is.Not.Null, "Missing generated metadata field " + name);
        return (T)field!.GetValue(null)!;
    }

    private static T ReadInternalField<T>(object target, string name)
    {
        FieldInfo? field = target.GetType().GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "Missing generated result field " + name);
        return (T)field!.GetValue(target)!;
    }

    private static object? InvokeInternal(Type type, string name, object[] arguments)
    {
        MethodInfo? method = type.GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "Missing generated internal method " + name);
        return method!.Invoke(null, arguments);
    }

    private static ValidationReport ValidateGeneratedModel(Type registry, object model)
    {
        return (ValidationReport)InvokeInternal(
            registry,
            "ValidateMessageModel",
            new object[]
            {
                model,
                "pacs.008.001.10",
                MessageNamespace,
                new ValidationPolicy()
            })!;
    }

    private static ValidationReport ValidateGeneratedStage(
        Type registry,
        string method,
        object model)
    {
        ValidationPolicy policy = new ValidationPolicy
        {
            RedactOffendingValues = false
        };
        return (ValidationReport)InvokeInternal(
            registry,
            method,
            new object[]
            {
                model,
                "pacs.008.001.10",
                MessageNamespace,
                policy
            })!;
    }

    private static object AddRequiredGeneratedStatus(object model)
    {
        object payment = FindField(model.GetType(), "PaymentInformation")
            .GetValue(model)!;
        FieldInfo statusesField = FindField(payment.GetType(), "Statuses");
        System.Collections.IList statuses =
            (System.Collections.IList)statusesField.GetValue(payment)!;
        Type statusType = statusesField.FieldType.GetGenericArguments()[0];
        statuses.Add(Enum.Parse(statusType, "Accepted"));
        return payment;
    }

    private static System.Collections.IList GetGeneratedCollection(
        object owner,
        string fieldName)
    {
        return (System.Collections.IList)FindField(owner.GetType(), fieldName)
            .GetValue(owner)!;
    }

    private static object AddGeneratedAccount(object payment)
    {
        FieldInfo accountField = FindField(payment.GetType(), "Account");
        object account = Activator.CreateInstance(accountField.FieldType)!;
        accountField.SetValue(payment, account);
        return account;
    }

    private static IsoTextValue FindGeneratedTextValue(object owner, string fieldName)
    {
        object wrapper = FindField(owner.GetType(), fieldName).GetValue(owner)!;
        return (IsoTextValue)FindField(wrapper.GetType(), "Value").GetValue(wrapper)!;
    }

    private static IsoAmountValue AddGeneratedAmount(object payment)
    {
        FieldInfo amountField = FindField(payment.GetType(), "Amount");
        object wrapper = Activator.CreateInstance(amountField.FieldType)!;
        amountField.SetValue(payment, wrapper);
        return (IsoAmountValue)FindField(wrapper.GetType(), "Value").GetValue(wrapper)!;
    }

    private static void SetGeneratedAmount(
        object payment,
        decimal number,
        int scale,
        string lexicalValue)
    {
        object wrapper = FindField(payment.GetType(), "Amount").GetValue(payment)!;
        IsoAmountValue amount = (IsoAmountValue)FindField(wrapper.GetType(), "Value")
            .GetValue(wrapper)!;
        SetDecimalValue(amount.Amount, number, scale, lexicalValue);
    }

    private static IsoBinaryValue AddGeneratedBinary(object payment, byte[] bytes)
    {
        System.Collections.IList values =
            GetGeneratedCollection(payment, "SignatureData");
        Type itemType = values.GetType().GetGenericArguments()[0];
        object wrapper = Activator.CreateInstance(itemType)!;
        values.Add(wrapper);
        IsoBinaryValue value = (IsoBinaryValue)FindField(wrapper.GetType(), "Value")
            .GetValue(wrapper)!;
        value.Value = bytes;
        value.LexicalValue = Convert.ToBase64String(bytes);
        return value;
    }

    private static void SetGeneratedBinary(object payment, byte[] bytes)
    {
        object wrapper = GetGeneratedCollection(payment, "SignatureData")[0]!;
        IsoBinaryValue value = (IsoBinaryValue)FindField(wrapper.GetType(), "Value")
            .GetValue(wrapper)!;
        value.Value = bytes;
        value.LexicalValue = Convert.ToBase64String(bytes);
    }

    private static ValidationReport ValidateGeneratedStructuralMutation(
        Type registry,
        Action<object, object, ValidationPolicy> mutation)
    {
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        ValidationPolicy policy = new ValidationPolicy();
        mutation(model, payment, policy);
        return ValidateGeneratedPublicStage(model, policy, ValidationStage.Structural);
    }

    private static ValidationReport ValidateGeneratedPrimitiveMutation(
        Type registry,
        Action<object, object, ValidationPolicy> mutation)
    {
        object model = InvokeInternal(
            registry,
            "CreateMessage",
            new object[] { "pacs.008.001.10" })!;
        object payment = AddRequiredGeneratedStatus(model);
        SetTextValue(FindGeneratedTextValue(payment, "Name"), "ABC");
        IsoAmountValue amount = AddGeneratedAmount(payment);
        SetDecimalValue(amount.Amount, 123.45m, 2, "123.45");
        amount.Currency = "USD";
        AddGeneratedBinary(payment, new byte[] { 1, 2 });
        ValidationPolicy policy = new ValidationPolicy();
        mutation(model, payment, policy);
        return ValidateGeneratedPublicStage(
            model, policy, ValidationStage.PrimitiveFacet);
    }

    private static ValidationReport ValidateGeneratedPublicStage(
        object model,
        ValidationPolicy policy,
        ValidationStage stage)
    {
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace,
            Policy = policy
        };
        request.Policy.Stages.Add(stage);
        return new MessageValidator().ValidateMessage(request);
    }

    private static void AssertGeneratedMutationReports(
        Dictionary<string, ValidationReport> reports,
        ValidationStage stage)
    {
        Assert.Multiple(() =>
        {
            foreach (KeyValuePair<string, ValidationReport> item in reports)
            {
                AssertGeneratedMutationReport(item.Value, item.Key, stage);
            }
        });
    }

    private static void AssertGeneratedMutationReport(
        ValidationReport report,
        string expectedCode,
        ValidationStage stage)
    {
        bool resourceFailure = expectedCode.EndsWith(
            "collection_limit", StringComparison.Ordinal);
        Assert.That(report.Success, Is.EqualTo(!resourceFailure),
            expectedCode + ": " + report.ErrorMessage);
        Assert.That(report.Valid, Is.False, expectedCode);
        Assert.That(report.Diagnostics.Select(value => value.Code),
            Does.Contain(expectedCode), expectedCode);
        Assert.That(report.CompletedStages,
            Is.EqualTo(new[] { stage }), expectedCode);
    }

    private static IsoCodeValue AddGeneratedExternalCode(object payment, string code)
    {
        FieldInfo currencyField = FindField(payment.GetType(), "Currency");
        object wrapper = Activator.CreateInstance(currencyField.FieldType)!;
        currencyField.SetValue(payment, wrapper);
        IsoCodeValue value = (IsoCodeValue)FindField(wrapper.GetType(), "Value")
            .GetValue(wrapper)!;
        value.Value = code;
        value.LexicalValue = code;
        value.CodeSetName = "ExternalCurrencyCode";
        return value;
    }

    private static ExternalCodeCatalog CreateValidationExternalCatalog(string edition)
    {
        ExternalCodeCatalog catalog = new ExternalCodeCatalog
        {
            Edition = edition,
            SourceFormat = "XSD",
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.ExternalCodeSet,
                    Edition = edition,
                    Identifier = "external-codes",
                    Version = "1"
                },
                OriginUri = "https://example.test/external-codes/" + edition,
                IsApproved = true
            }
        };
        ExternalCodeSet codeSet = new ExternalCodeSet
        {
            Identifier = "codes-currency",
            Name = "ExternalCurrencyCode",
            RegistrationStatus = "Registered"
        };
        codeSet.Values.Add(new ExternalCodeValue
        {
            Value = "USD",
            Name = "US Dollar",
            RegistrationStatus = "Registered"
        });
        catalog.CodeSets.Add(codeSet);
        return catalog;
    }

    private static ValidationRequest CreateExternalValidationRequest(
        object model,
        ICodeSetProvider? provider,
        string edition,
        bool selectLatest)
    {
        ValidationRequest request = new ValidationRequest
        {
            Model = model,
            ExpectedIdentifier = new MessageIdentifier
            {
                Value = "pacs.008.001.10"
            },
            ExpectedNamespaceUri = MessageNamespace,
            CodeSetProvider = provider
        };
        request.Policy.Stages.Add(ValidationStage.ExternalCode);
        request.Policy.ExternalCodeEdition = edition;
        request.Policy.SelectLatestExternalCodeEdition = selectLatest;
        return request;
    }

    private static IsoDateValue CreateValidationDate(string value)
    {
        string[] parts = value.Split('-');
        return new IsoDateValue
        {
            Year = int.Parse(parts[0]),
            Month = int.Parse(parts[1]),
            Day = int.Parse(parts[2]),
            LexicalValue = value
        };
    }

    private static IsoDateTimeValue CreateValidationDateTime(string value)
    {
        return new IsoDateTimeValue
        {
            Date = CreateValidationDate(value),
            LexicalValue = value + "T00:00:00Z"
        };
    }

    private static void SetTextValue(IsoTextValue value, string text)
    {
        value.Value = text;
        value.LexicalValue = text;
    }

    private static void SetDecimalValue(
        IsoDecimalValue value,
        decimal number,
        int scale,
        string lexical)
    {
        value.Value = number;
        value.Scale = scale;
        value.LexicalValue = lexical;
        value.IsNegativeZero = false;
    }

    private static object ReadGeneratedDocument(Assembly assembly, string xml)
    {
        Type rootCodec = FindGeneratedType(
            assembly,
            "Contoso.Generated.Messages.Pacs.CustomerCreditTransferV10Codec");
        using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(
            new StringReader(xml));
        return InvokeInternal(rootCodec, "ReadModel", new object[] { reader })!;
    }

    private static string WriteGeneratedDocument(
        Type rootCodec,
        object model,
        string identifier,
        string namespaceUri)
    {
        StringBuilder output = new StringBuilder();
        System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = false,
            NamespaceHandling = System.Xml.NamespaceHandling.OmitDuplicates
        };
        using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(output, settings))
        {
            InvokeInternal(rootCodec, "WriteDocument",
                new object[] { writer, model, identifier, namespaceUri });
        }
        return output.ToString();
    }

    private static void AssertGeneratedWriteFailure(
        Type rootCodec,
        object? model,
        string identifier,
        string namespaceUri,
        Type exceptionType,
        string message)
    {
        StringBuilder output = new StringBuilder();
        System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            ConformanceLevel = System.Xml.ConformanceLevel.Fragment
        };
        TargetInvocationException? exception;
        using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(output, settings))
        {
            exception = Assert.Throws<TargetInvocationException>(() =>
                InvokeInternal(rootCodec, "WriteDocument",
                    new object?[] { writer, model, identifier, namespaceUri }!));
        }
        Assert.Multiple(() =>
        {
            Assert.That(exception?.InnerException?.GetType(), Is.EqualTo(exceptionType));
            Assert.That(exception?.InnerException?.Message, Does.Contain(message)
                .IgnoreCase);
            Assert.That(output.ToString(), Is.Empty,
                "Writer validation must complete before XML output begins.");
        });
    }

    private static bool IsGeneratedTypeSymbol(GenerationSymbol symbol)
    {
        return symbol.SourceType == "MessageDefinition" ||
            symbol.SourceType == "MessageComponent" ||
            symbol.SourceType == "Choice" ||
            symbol.SourceType == "ChoiceDiscriminator" ||
            symbol.SourceType == "DataType" ||
            symbol.SourceType == "CodeSet" ||
            symbol.SourceType == "ExternalSchema";
    }

    private static bool IsGeneratedMemberSymbol(GenerationSymbol symbol)
    {
        return symbol.SourceType == "MessageBuildingBlock" ||
            symbol.SourceType == "MessageElement" ||
            symbol.SourceType == "CodeValue";
    }

    private static GenerationPlan CreateAllDomainRepositoryPlan()
    {
        RepositorySnapshot repository = GetBundledRepository();
        GenerationPlan plan = new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                ArtifactEdition = "2026-06",
                IncludeDeprecated = true,
                IncludeDocumentation = false,
                RootNamespace = "AllDomains.Generated"
            }
        };
        List<string> selectedAreas = new List<string>();
        int areaIndex = 0;
        while (areaIndex < repository.BusinessAreas.Count)
        {
            string area = repository.BusinessAreas[areaIndex].Code;
            MessageDefinitionMetadata? message = repository.MessageDefinitions.FirstOrDefault(
                candidate => candidate.MessageIdentifier.BusinessArea == area);
            if (area.Length > 0 && message != null && !selectedAreas.Contains(area))
            {
                plan.Options.MessageIdentifiers.Add(message.MessageIdentifier);
                selectedAreas.Add(area);
            }
            areaIndex++;
        }
        return plan;
    }

    private static RepositorySnapshot GetBundledRepository()
    {
        if (bundledRepository != null)
        {
            return bundledRepository;
        }
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/eRepository_ISO20022_20260626.zip"));
        RepositoryImportResult imported = new RepositoryImporter().ImportZipFile(
            archivePath,
            new RepositoryImportOptions
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.Repository,
                    Edition = "2026-06",
                    Identifier = "iso20022-e-repository",
                    Version = "20260626"
                },
                IsApproved = true
            });
        Assert.That(imported.Success, Is.True, imported.ErrorMessage);
        Assert.That(imported.Snapshot, Is.Not.Null, imported.ErrorMessage);
        bundledRepository = imported.Snapshot!;
        return bundledRepository;
    }

    private static GenerationPlan CreateRepositoryPlan(bool shuffled)
    {
        RepositorySnapshot repository = new RepositorySnapshot
        {
            Identifier = "repository-2026",
            Edition = "2026"
        };
        MessageDefinitionMetadata message = new MessageDefinitionMetadata
        {
            Identifier = "message-1",
            Name = "CustomerCreditTransferV10",
            Definition = "Transfers funds between financial institutions.",
            NamespaceUri = MessageNamespace,
            RootElement = "Document",
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
        repository.MessageDefinitions.Add(message);
        MessageBuildingBlockDefinition block = new MessageBuildingBlockDefinition
        {
            Identifier = "block-1",
            Name = "PaymentInformation",
            XmlTag = "PmtInf",
            Definition = "Payment information.",
            Multiplicity = CreateMultiplicity(1, true, 1),
            MessageDefinition = CreateReference(
                "message-1", RepositoryEntityKind.MessageDefinition),
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
        AddElementReference(component, "element-statuses", 1);
        AddElementReference(component, "element-optional-status", 2);
        AddElementReference(component, "element-account", 3);
        AddElementReference(component, "element-currency", 4);
        AddElementReference(component, "element-binary", 5);
        AddElementReference(component, "element-supplementary", 6);
        AddElementReference(component, "element-servicer", 7);
        MessageComponentDefinition accountServicer = new MessageComponentDefinition
        {
            Identifier = "component-servicer",
            Name = "AccountServicerComponent"
        };
        MessageComponentDefinition unusedComponent = new MessageComponentDefinition
        {
            Identifier = "component-unused",
            Name = "UnusedComponent"
        };
        if (shuffled)
        {
            repository.MessageComponents.Add(unusedComponent);
            repository.MessageComponents.Add(accountServicer);
            repository.MessageComponents.Add(component);
        }
        else
        {
            repository.MessageComponents.Add(component);
            repository.MessageComponents.Add(accountServicer);
            repository.MessageComponents.Add(unusedComponent);
        }

        repository.MessageElements.Add(CreateElement(
            "element-name", "Name", "Nm", "type-text", RepositoryEntityKind.DataType,
            CreateMultiplicity(1, true, 1), MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-statuses", "Statuses", "Sts", "codes-status",
            RepositoryEntityKind.CodeSet, CreateMultiplicity(1, true, 3),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-optional-status", "OptionalStatus", "OptnlSts", "codes-status",
            RepositoryEntityKind.CodeSet, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-account", "Account", "Acct", "choice-account", RepositoryEntityKind.Choice,
            CreateMultiplicity(0, true, 1), MessageElementKind.AssociationEnd, true));
        repository.MessageElements.Add(CreateElement(
            "element-currency", "Currency", "Ccy", "codes-currency",
            RepositoryEntityKind.CodeSet, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-binary", "SignatureData", "SgntrData", "type-binary",
            RepositoryEntityKind.DataType, CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "element-supplementary", "SupplementaryData", "SplmtryData", "external-envelope",
            RepositoryEntityKind.ExternalSchema, CreateMultiplicity(0, true, 1),
            MessageElementKind.AssociationEnd, true));
        repository.MessageElements.Add(CreateElement(
            "element-servicer", "AccountServicer", "AcctSvcr", "component-servicer",
            RepositoryEntityKind.MessageComponent, CreateMultiplicity(0, true, 1),
            MessageElementKind.AssociationEnd, false));
        repository.MessageElements.Add(CreateElement(
            "choice-iban", "Iban", "IBAN", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(1, true, 1), MessageElementKind.Attribute, true));
        repository.MessageElements.Add(CreateElement(
            "choice-bic", "Bic", "BIC", "xs:string", RepositoryEntityKind.Unknown,
            CreateMultiplicity(1, true, 1), MessageElementKind.Attribute, true));

        ChoiceDefinition choice = new ChoiceDefinition
        {
            Identifier = "choice-account",
            Name = "AccountChoice",
            ChoiceType = "ChoiceComponent",
            Definition = "Choice of account identifiers."
        };
        choice.MessageElements.Add(CreateReference(
            "choice-iban", RepositoryEntityKind.MessageElement));
        choice.MessageElements.Add(CreateReference(
            "choice-bic", RepositoryEntityKind.MessageElement));
        repository.Choices.Add(choice);

        DataTypeDefinition text = new DataTypeDefinition
        {
            Identifier = "type-text",
            Name = "Max35Text",
            Kind = DataTypeKind.String,
            Definition = "Text with a maximum length."
        };
        DataTypeDefinition binary = new DataTypeDefinition
        {
            Identifier = "type-binary",
            Name = "Max16Binary",
            Kind = DataTypeKind.Binary
        };
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
            repository.DataTypes.Add(text);
        }
        else
        {
            repository.DataTypes.Add(text);
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
            Name = "ExternalCurrencyCode",
            Kind = CodeSetKind.External
        };
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
            Kind = DataTypeKind.ExternalSchema
        });
        return new GenerationPlan
        {
            Repository = repository,
            Options = new GenerationOptions
            {
                RootNamespace = "contoso.generated",
                ArtifactEdition = "2026-06-26",
                IncludeDocumentation = true
            }
        };
    }

    private static GenerationPlan CreateReaderPlan()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        RepositorySnapshot repository = plan.Repository!;
        MessageComponentDefinition component = repository.MessageComponents.Single(
            value => value.Identifier == "component-1");
        AddElementReference(component, "element-amount", component.MessageElements.Count);
        repository.MessageElements.Add(CreateElement(
            "element-amount",
            "Amount",
            "Amt",
            "type-amount",
            RepositoryEntityKind.DataType,
            CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute,
            true));
        repository.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "type-amount",
            Name = "ActiveCurrencyAndAmount",
            Kind = DataTypeKind.Amount
        });
        return plan;
    }

    private static GenerationPlan CreatePrimitiveValidationPlan()
    {
        GenerationPlan plan = CreateReaderPlan();
        RepositorySnapshot repository = plan.Repository!;
        ConfigureSourceArtifact(
            plan,
            SHA256.HashData(Encoding.UTF8.GetBytes("primitive-validation-repository")));
        DataTypeDefinition text = repository.DataTypes.Single(value =>
            value.Identifier == "type-text");
        text.Facets.Add(CreateIntegerFacet("maxLength", 5));
        text.Facets.Add(new DataTypeFacet
        {
            Name = "pattern",
            Value = "^[A-Z]+$"
        });
        text.Facets.Add(new DataTypeFacet
        {
            Name = "enumeration",
            EnumerationValues = { "ABC", "XYZ" }
        });
        text.Facets.Add(new DataTypeFacet
        {
            Name = "whiteSpace",
            Value = "collapse"
        });
        DataTypeDefinition amount = repository.DataTypes.Single(value =>
            value.Identifier == "type-amount");
        amount.Facets.Add(CreateIntegerFacet("totalDigits", 5));
        amount.Facets.Add(CreateIntegerFacet("fractionDigits", 2));
        amount.Facets.Add(new DataTypeFacet
        {
            Name = "minInclusive",
            Value = "0"
        });
        amount.Facets.Add(new DataTypeFacet
        {
            Name = "maxExclusive",
            Value = "1000"
        });
        return plan;
    }

    private static GenerationPlan CreateGeneratedFieldMutationPlan()
    {
        GenerationPlan plan = CreatePrimitiveValidationPlan();
        RepositorySnapshot repository = plan.Repository!;
        DataTypeDefinition text = repository.DataTypes.Single(value =>
            value.Identifier == "type-text");
        text.Facets.Add(CreateIntegerFacet("length", 3));
        text.Facets.Add(CreateIntegerFacet("minLength", 2));
        DataTypeDefinition amount = repository.DataTypes.Single(value =>
            value.Identifier == "type-amount");
        amount.Facets.Add(new DataTypeFacet
        {
            Name = "minExclusive",
            Value = "0"
        });
        amount.Facets.Add(new DataTypeFacet
        {
            Name = "maxInclusive",
            Value = "999"
        });
        DataTypeDefinition binary = repository.DataTypes.Single(value =>
            value.Identifier == "type-binary");
        binary.Facets.Add(CreateIntegerFacet("length", 2));
        binary.Facets.Add(CreateIntegerFacet("minLength", 1));
        binary.Facets.Add(CreateIntegerFacet("maxLength", 3));
        MessageElementDefinition signature = repository.MessageElements.Single(value =>
            value.Identifier == "element-binary");
        signature.Multiplicity = CreateMultiplicity(0, true, 3);
        return plan;
    }

    private static GenerationPlan CreateRepositoryConstraintValidationPlan()
    {
        GenerationPlan plan = CreateRepositoryPlan(false);
        ConfigureSourceArtifact(
            plan,
            SHA256.HashData(Encoding.UTF8.GetBytes("constraint-validation-repository")));
        RepositorySnapshot repository = plan.Repository!;
        repository.Constraints.Add(CreateRepositoryConstraint(
            "constraint-repository",
            "RepositoryConsistency",
            "The repository-wide message state must be consistent.",
            "self.isConsistent()",
            "OCL"));
        repository.MessageDefinitions.Single(value =>
            value.Identifier == "message-1").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-message",
                    "MessageConsistency",
                    "The message must be internally consistent.",
                    string.Empty,
                    string.Empty));
        repository.MessageBuildingBlocks.Single(value =>
            value.Identifier == "block-1").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-block",
                    "BlockConsistency",
                    "The payment block must be internally consistent.",
                    string.Empty,
                    string.Empty));
        repository.MessageComponents.Single(value =>
            value.Identifier == "component-1").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-component",
                    "ComponentConsistency",
                    "The payment component must be internally consistent.",
                    string.Empty,
                    string.Empty));
        repository.MessageElements.Single(value =>
            value.Identifier == "element-name").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-element-name",
                    "NameConsistency",
                    "The name member must be internally consistent.",
                    string.Empty,
                    string.Empty));
        repository.DataTypes.Single(value =>
            value.Identifier == "type-text").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-type-text",
                    "TextConsistency",
                    "The text value must be internally consistent.",
                    string.Empty,
                    string.Empty));
        repository.MessageElements.Single(value =>
            value.Identifier == "element-statuses").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-element-statuses",
                    "StatusMemberConsistency",
                    "The status member must be internally consistent.",
                    string.Empty,
                    string.Empty));
        CodeSetDefinition codeSet = repository.CodeSets.Single(value =>
            value.Identifier == "codes-status");
        codeSet.Registration.Constraints.Add(CreateRepositoryConstraint(
            "constraint-code-set",
            "CodeSetConsistency",
            "The code set value must be internally consistent.",
            string.Empty,
            string.Empty));
        codeSet.Codes.Single(value =>
            value.Identifier == "code-accepted").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-code-value",
                    "AcceptedCodeConsistency",
                    "The accepted code must be internally consistent.",
                    string.Empty,
                    string.Empty));
        repository.MessageComponents.Single(value =>
            value.Identifier == "component-unused").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-unused",
                    "UnusedConsistency",
                    "An unused component constraint.",
                    string.Empty,
                    string.Empty));
        repository.Choices.Single(value =>
            value.Identifier == "choice-account").Registration.Constraints.Add(
                CreateRepositoryConstraint(
                    "constraint-choice",
                    "AccountChoiceConsistency",
                    "The optional account choice must be internally consistent.",
                    string.Empty,
                    string.Empty));
        return plan;
    }

    private static ConstraintDefinition CreateRepositoryConstraint(
        string identifier,
        string name,
        string definition,
        string expression,
        string expressionLanguage)
    {
        return new ConstraintDefinition
        {
            Identifier = identifier,
            Name = name,
            Definition = definition,
            Expression = expression,
            ExpressionLanguage = expressionLanguage
        };
    }

    private static DataTypeFacet CreateIntegerFacet(string name, int value)
    {
        return new DataTypeFacet
        {
            Name = name,
            Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static List<TypedCodecConstructCase> CreateTypedCodecConstructCases()
    {
        return new List<TypedCodecConstructCase>
        {
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Text,
                Name = "TextConstruct",
                XmlName = "Txt",
                LexicalValue = "alpha"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Identifier,
                Name = "IdentifierConstruct",
                XmlName = "Id",
                LexicalValue = "ID-2026-08"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Boolean,
                Name = "BooleanConstruct",
                XmlName = "Bool",
                LexicalValue = "1"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Indicator,
                Name = "IndicatorConstruct",
                XmlName = "Ind",
                LexicalValue = "false"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Decimal,
                Name = "DecimalConstruct",
                XmlName = "Dec",
                LexicalValue = "-0.00"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Quantity,
                Name = "QuantityConstruct",
                XmlName = "Qty",
                LexicalValue = "123.450"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Rate,
                Name = "RateConstruct",
                XmlName = "Rate",
                LexicalValue = "0.0150"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Binary,
                Name = "HexBinaryConstruct",
                XmlName = "Hex",
                LexicalValue = "0A0B",
                BinaryEncoding = BinaryEncodingKind.Hexadecimal
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Date,
                Name = "DateConstruct",
                XmlName = "Dt",
                LexicalValue = "2026-08-31-00:00"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.DateTime,
                Name = "DateTimeConstruct",
                XmlName = "DtTm",
                LexicalValue = "2026-08-31T12:34:56.120Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Day,
                Name = "DayConstruct",
                XmlName = "Day",
                LexicalValue = "---31Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Duration,
                Name = "DurationConstruct",
                XmlName = "Dur",
                LexicalValue = "-P1Y2M3DT4H5M6.70S"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Month,
                Name = "MonthConstruct",
                XmlName = "Mnth",
                LexicalValue = "--08Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.MonthDay,
                Name = "MonthDayConstruct",
                XmlName = "MnthDay",
                LexicalValue = "--08-31Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Time,
                Name = "TimeConstruct",
                XmlName = "Tm",
                LexicalValue = "12:34:56.120Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Year,
                Name = "YearConstruct",
                XmlName = "Yr",
                LexicalValue = "2026Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.YearMonth,
                Name = "YearMonthConstruct",
                XmlName = "YrMnth",
                LexicalValue = "2026-08Z"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.Schema,
                Name = "SchemaBooleanConstruct",
                XmlName = "SchmaBool",
                LexicalValue = "true",
                SchemaTypeName = "boolean"
            },
            new TypedCodecConstructCase
            {
                Kind = DataTypeKind.UserDefined,
                Name = "UserDefinedConstruct",
                XmlName = "Usr",
                LexicalValue = "custom-value"
            }
        };
    }

    private static GenerationPlan CreateJsonReaderPlan()
    {
        GenerationPlan plan = CreateReaderPlan();
        RepositorySnapshot repository = plan.Repository!;
        MessageComponentDefinition component = repository.MessageComponents.Single(
            value => value.Identifier == "component-1");
        AddElementReference(component, "element-date", component.MessageElements.Count);
        AddElementReference(component, "element-time", component.MessageElements.Count);
        AddElementReference(
            component,
            "element-date-time",
            component.MessageElements.Count);
        repository.MessageElements.Add(CreateElement(
            "element-date",
            "CreationDate",
            "CreDt",
            "type-date",
            RepositoryEntityKind.DataType,
            CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute,
            true));
        repository.MessageElements.Add(CreateElement(
            "element-time",
            "CutOffTime",
            "CutOffTm",
            "type-time",
            RepositoryEntityKind.DataType,
            CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute,
            true));
        repository.MessageElements.Add(CreateElement(
            "element-date-time",
            "CreationDateTime",
            "CreDtTm",
            "type-date-time",
            RepositoryEntityKind.DataType,
            CreateMultiplicity(0, true, 1),
            MessageElementKind.Attribute,
            true));
        repository.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "type-date",
            Name = "ISODate",
            Kind = DataTypeKind.Date
        });
        repository.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "type-time",
            Name = "ISOTime",
            Kind = DataTypeKind.Time
        });
        repository.DataTypes.Add(new DataTypeDefinition
        {
            Identifier = "type-date-time",
            Name = "ISODateTime",
            Kind = DataTypeKind.DateTime
        });
        return plan;
    }

    private static void AddElementReference(
        MessageComponentDefinition component,
        string identifier,
        int position)
    {
        RepositoryReference reference = CreateReference(
            identifier, RepositoryEntityKind.MessageElement);
        reference.Position = position;
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

    private class TypedCodecConstructCase
    {
        internal DataTypeKind Kind;
        internal string Name = string.Empty;
        internal string XmlName = string.Empty;
        internal string LexicalValue = string.Empty;
        internal BinaryEncodingKind BinaryEncoding;
        internal string SchemaTypeName = string.Empty;
    }
}
