using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace iso20022;

internal static class GenerationManifestEmitter
{
    internal static void AddSchemaMappings(
        XsdGenerationContext context,
        GeneratedFile file,
        GenerationManifest manifest)
    {
        if (!context.Options.EmitSourceMappings)
        {
            return;
        }

        GenerationModel model = CreateSchemaMappingModel(context);
        GenerationShapeIdentifierBuilder shapeBuilder = new GenerationShapeIdentifierBuilder();
        int index = 0;
        while (index < context.Names.Count)
        {
            XsdGenerationName name = context.Names[index];
            GenerationConcept? concept = FindMappingConcept(model, name.Kind, name.Identifier);
            if (concept != null)
            {
                GenerationSymbol symbol = CreateBaseMapping(model, concept, shapeBuilder);
                symbol.XmlName = FindXmlName(concept, name.Name);
                symbol.OutputPath = file.RelativePath;
                manifest.Symbols.Add(symbol);
            }
            index++;
        }
        index = 0;
        while (index < model.Concepts.Count)
        {
            GenerationConcept concept = model.Concepts[index];
            if (!ManifestContainsConcept(manifest.Symbols, model, concept))
            {
                GenerationSymbol symbol = CreateBaseMapping(model, concept, shapeBuilder);
                symbol.XmlName = FindXmlName(concept, concept.Name);
                symbol.OutputPath = file.RelativePath;
                manifest.Symbols.Add(symbol);
            }
            index++;
        }
    }

    internal static void AddCSharpMappings(
        CSharpGenerationContext context,
        List<GeneratedFile> files,
        GenerationManifest manifest)
    {
        if (!context.Plan.Options.EmitSourceMappings)
        {
            return;
        }

        int index = 0;
        while (index < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry entry = context.Symbols.Symbols[index];
            GenerationSymbol symbol = CreateBaseMapping(
                context.Model,
                entry.Concept,
                new GenerationShapeIdentifierBuilder());
            symbol.XmlName = FindXmlName(entry.Concept, entry.Concept.Name);
            symbol.CSharpNamespace = entry.NamespaceName;
            symbol.CSharpName = entry.SymbolName;
            symbol.ShapeIdentifier = entry.ShapeIdentifier;
            symbol.OutputPath = FindCSharpOutputPath(files, entry);
            symbol.HasCollision = entry.HasCollision;
            manifest.Symbols.Add(symbol);
            if (entry.Concept.Kind == GenerationConceptKind.Choice)
            {
                GenerationSymbolEntry discriminator = new GenerationSymbolEntry();
                discriminator.Concept = entry.Concept;
                discriminator.NamespaceName = entry.NamespaceName;
                discriminator.SymbolName = CSharpModelEmitter.FindChoiceKindName(context, entry);
                GenerationSymbol kindSymbol = CreateBaseMapping(
                    context.Model,
                    entry.Concept,
                    new GenerationShapeIdentifierBuilder());
                kindSymbol.SourceType = "ChoiceDiscriminator";
                kindSymbol.XmlName = symbol.XmlName;
                kindSymbol.CSharpNamespace = discriminator.NamespaceName;
                kindSymbol.CSharpName = discriminator.SymbolName;
                kindSymbol.ShapeIdentifier = "shape_" + GenerationStableHash.CreateHex(
                    entry.ShapeIdentifier + "\nchoice-discriminator");
                kindSymbol.OutputPath = FindCSharpOutputPath(files, discriminator);
                kindSymbol.HasCollision = entry.HasCollision;
                manifest.Symbols.Add(kindSymbol);
            }
            index++;
        }
        AddCSharpMemberMappings(context, files, manifest);
        AddUnmappedCSharpConcepts(context, manifest);
    }

    internal static void AddCSharpMemberMappings(
        CSharpGenerationContext context,
        List<GeneratedFile> files,
        GenerationManifest manifest)
    {
        int symbolIndex = 0;
        while (symbolIndex < context.Symbols.Symbols.Count)
        {
            GenerationSymbolEntry owner = context.Symbols.Symbols[symbolIndex];
            List<string> usedNames = new List<string>();
            usedNames.Add(owner.SymbolName);
            if (owner.Concept.MessageDefinition != null)
            {
                MessageDefinitionMetadata message = owner.Concept.MessageDefinition;
                int index = 0;
                while (index < message.BuildingBlocks.Count)
                {
                    string identifier = message.BuildingBlocks[index].TargetIdentifier;
                    GenerationConcept? member = FindConceptInContext(
                        context, owner.Concept.SourceOrdinal,
                        GenerationConceptKind.MessageBuildingBlock, identifier);
                    if (member != null && member.MessageBuildingBlock != null)
                    {
                        string name = CSharpModelEmitter.CreateUniqueMemberName(
                            usedNames,
                            CSharpModelEmitter.FindMemberCandidate(
                                member.MessageBuildingBlock.XmlTag,
                                member.MessageBuildingBlock.Name),
                            member.Identifier);
                        AddCSharpMemberMapping(
                            context, files, manifest, owner, member, name);
                    }
                    index++;
                }
            }
            else if (owner.Concept.MessageComponent != null)
            {
                MessageComponentDefinition component = owner.Concept.MessageComponent;
                int index = 0;
                while (index < component.MessageElements.Count)
                {
                    string identifier = component.MessageElements[index].TargetIdentifier;
                    GenerationConcept? member = FindConceptInContext(
                        context, owner.Concept.SourceOrdinal,
                        GenerationConceptKind.MessageElement, identifier);
                    if (member != null && member.MessageElement != null)
                    {
                        string name = CSharpModelEmitter.CreateUniqueMemberName(
                            usedNames,
                            CSharpModelEmitter.FindMemberCandidate(
                                member.MessageElement.XmlTag,
                                member.MessageElement.Name),
                            member.Identifier);
                        AddCSharpMemberMapping(
                            context, files, manifest, owner, member, name);
                    }
                    index++;
                }
            }
            else if (owner.Concept.Choice != null)
            {
                usedNames.Add("Choice");
                ChoiceDefinition choice = owner.Concept.Choice;
                int index = 0;
                while (index < choice.MessageElements.Count)
                {
                    string identifier = choice.MessageElements[index].TargetIdentifier;
                    GenerationConcept? member = FindConceptInContext(
                        context, owner.Concept.SourceOrdinal,
                        GenerationConceptKind.MessageElement, identifier);
                    if (member != null && member.MessageElement != null)
                    {
                        string name = CSharpModelEmitter.CreateUniqueMemberName(
                            usedNames,
                            CSharpModelEmitter.FindMemberCandidate(
                                member.MessageElement.XmlTag,
                                member.MessageElement.Name),
                            member.Identifier);
                        AddCSharpMemberMapping(
                            context, files, manifest, owner, member, name);
                    }
                    index++;
                }
            }
            else if (owner.Concept.CodeSet != null &&
                CSharpModelEmitter.CodeSetIsClosed(owner.Concept.CodeSet))
            {
                List<CodeValueDefinition> codes =
                    CSharpModelEmitter.CollectClosedCodeValues(owner.Concept.CodeSet);
                usedNames.Add("Unknown");
                int index = 0;
                while (index < codes.Count)
                {
                    CodeValueDefinition code = codes[index];
                    GenerationConcept? member = FindConceptInContext(
                        context, owner.Concept.SourceOrdinal,
                        GenerationConceptKind.CodeValue, code.Identifier);
                    if (member != null)
                    {
                        string candidate = code.Name;
                        if (candidate.Length == 0)
                        {
                            candidate = code.Value;
                        }
                        string name = CSharpModelEmitter.CreateUniqueMemberName(
                            usedNames, candidate, code.Identifier + "|" + code.Value);
                        AddCSharpMemberMapping(
                            context, files, manifest, owner, member, name);
                    }
                    index++;
                }
            }
            symbolIndex++;
        }
    }

    internal static void AddCSharpMemberMapping(
        CSharpGenerationContext context,
        List<GeneratedFile> files,
        GenerationManifest manifest,
        GenerationSymbolEntry owner,
        GenerationConcept member,
        string memberName)
    {
        GenerationSymbol symbol = CreateBaseMapping(
            context.Model,
            member,
            new GenerationShapeIdentifierBuilder());
        symbol.XmlName = FindXmlName(member, member.Name);
        symbol.CSharpNamespace = owner.NamespaceName;
        symbol.CSharpName = memberName;
        symbol.OutputPath = FindCSharpOutputPath(files, owner);
        manifest.Symbols.Add(symbol);
    }

    internal static void AddUnmappedCSharpConcepts(
        CSharpGenerationContext context,
        GenerationManifest manifest)
    {
        int index = 0;
        while (index < context.Concepts.Count)
        {
            GenerationConcept concept = context.Concepts[index];
            if (!ManifestContainsConcept(manifest.Symbols, context.Model, concept))
            {
                GenerationSymbol symbol = CreateBaseMapping(
                    context.Model,
                    concept,
                    new GenerationShapeIdentifierBuilder());
                symbol.XmlName = FindXmlName(concept, concept.Name);
                manifest.Symbols.Add(symbol);
            }
            index++;
        }
    }

    internal static GenerationConcept? FindConceptInContext(
        CSharpGenerationContext context,
        int sourceOrdinal,
        GenerationConceptKind kind,
        string identifier)
    {
        string identity = CSharpModelEmitter.CreateGenerationIdentity(
            sourceOrdinal,
            kind,
            identifier);
        if (context.ConceptsByIdentity.ContainsKey(identity))
        {
            return context.ConceptsByIdentity[identity];
        }
        return null;
    }

    internal static bool ManifestContainsConcept(
        List<GenerationSymbol> symbols,
        GenerationModel model,
        GenerationConcept concept)
    {
        string sourceType = concept.Kind.ToString();
        string sourceName = string.Empty;
        GenerationSource? source = FindGenerationSource(model, concept.SourceOrdinal);
        if (source != null)
        {
            sourceName = source.Identifier;
        }
        int index = 0;
        while (index < symbols.Count)
        {
            if (symbols[index].SourceIdentifier == concept.Identifier &&
                symbols[index].SourceType == sourceType &&
                symbols[index].SourceName == sourceName)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void CompleteManifest(GenerationResult result)
    {
        GenerationManifest manifest = result.Manifest;
        NormalizeSourceHashes(manifest.SourceArtifacts);
        SortSourceArtifacts(manifest.SourceArtifacts);
        SortManifestSymbols(manifest.Symbols);
        SortGeneratedFiles(manifest.Files);
        SetSymbolOrder(manifest.Symbols);
        NormalizeFileHashes(manifest.Files);

        byte[] settings = CreateSettingsContent(manifest);
        SetBinaryHash(manifest.SettingsHash, SHA256.HashData(settings));
        byte[] output = CreateOutputIdentityContent(manifest.Files);
        SetBinaryHash(manifest.OutputHash, SHA256.HashData(output));
        byte[] canonical = CreateManifestContent(manifest, false);
        SetBinaryHash(manifest.ManifestHash, SHA256.HashData(canonical));

        if (manifest.Options.EmitManifest)
        {
            byte[] content = CreateManifestContent(manifest, true);
            GeneratedFile file = new GeneratedFile();
            file.Kind = GenerationFileKind.Manifest;
            file.RelativePath = CreateManifestPath(manifest.GeneratorIdentifier);
            file.MediaType = Iso20022MediaTypes.Json;
            file.Content = content;
            file.HashAlgorithm = manifest.HashAlgorithm;
            SetBinaryHash(file.Hash, SHA256.HashData(content));
            result.Files.Add(file);
        }
        SortGeneratedFiles(result.Files);
    }

    internal static GenerationModel CreateSchemaMappingModel(XsdGenerationContext context)
    {
        GenerationModel model = new GenerationModel();
        int sourceOrdinal = CSharpModelEmitter.FindOrAddGenerationSource(model, context.Unit);
        GenerationSourceKind sourceKind = GenerationSourceKind.Repository;
        if (context.Unit.Schema != null)
        {
            sourceKind = GenerationSourceKind.Xsd;
        }
        GenerationModelNormalizer normalizer = new GenerationModelNormalizer();
        normalizer.AppendMessageDefinitions(model, context.Messages, sourceOrdinal, sourceKind);
        normalizer.AppendMessageBuildingBlocks(
            model, context.BuildingBlocks, sourceOrdinal, sourceKind);
        normalizer.AppendMessageComponents(model, context.Components, sourceOrdinal, sourceKind);
        normalizer.AppendMessageElements(model, context.Elements, sourceOrdinal, sourceKind);
        normalizer.AppendChoices(model, context.Choices, sourceOrdinal, sourceKind);
        normalizer.AppendDataTypes(model, context.DataTypes, sourceOrdinal, sourceKind);
        normalizer.AppendCodeSets(model, context.CodeSets, sourceOrdinal, sourceKind);
        normalizer.AppendExternalSchemas(
            model, context.ExternalSchemas, sourceOrdinal, sourceKind,
            context.Unit.Schema != null);
        return model;
    }

    internal static GenerationConcept? FindMappingConcept(
        GenerationModel model,
        GenerationConceptKind kind,
        string identifier)
    {
        int index = 0;
        while (index < model.Concepts.Count)
        {
            GenerationConcept concept = model.Concepts[index];
            if (concept.Kind == kind && concept.Identifier == identifier)
            {
                return concept;
            }
            index++;
        }
        return null;
    }

    internal static GenerationSymbol CreateBaseMapping(
        GenerationModel model,
        GenerationConcept concept,
        GenerationShapeIdentifierBuilder shapeBuilder)
    {
        GenerationSymbol symbol = new GenerationSymbol();
        symbol.SourceIdentifier = concept.Identifier;
        symbol.SourceType = concept.Kind.ToString();
        symbol.RepositoryKind = FindRepositoryKind(concept.Kind);
        GenerationSource? source = FindGenerationSource(model, concept.SourceOrdinal);
        if (source != null)
        {
            symbol.SourceName = source.Identifier;
            symbol.NamespaceUri = FindConceptNamespace(source, concept);
        }
        symbol.ShapeIdentifier = shapeBuilder.CreateShapeIdentifier(concept);
        return symbol;
    }

    internal static GenerationSource? FindGenerationSource(GenerationModel model, int ordinal)
    {
        int index = 0;
        while (index < model.Sources.Count)
        {
            if (model.Sources[index].Ordinal == ordinal)
            {
                return model.Sources[index];
            }
            index++;
        }
        return null;
    }

    internal static string FindConceptNamespace(
        GenerationSource source,
        GenerationConcept concept)
    {
        if (concept.MessageDefinition != null &&
            concept.MessageDefinition.NamespaceUri.Length > 0)
        {
            return concept.MessageDefinition.NamespaceUri;
        }
        return source.NamespaceUri;
    }

    internal static string FindXmlName(GenerationConcept concept, string fallback)
    {
        if (concept.MessageDefinition != null)
        {
            if (concept.MessageDefinition.RootElement.Length > 0)
            {
                return concept.MessageDefinition.RootElement;
            }
            if (concept.MessageDefinition.XmlTag.Length > 0)
            {
                return concept.MessageDefinition.XmlTag;
            }
            if (concept.MessageDefinition.XmlName.Length > 0)
            {
                return concept.MessageDefinition.XmlName;
            }
        }
        if (concept.MessageBuildingBlock != null &&
            concept.MessageBuildingBlock.XmlTag.Length > 0)
        {
            return concept.MessageBuildingBlock.XmlTag;
        }
        if (concept.MessageElement != null && concept.MessageElement.XmlTag.Length > 0)
        {
            return concept.MessageElement.XmlTag;
        }
        if (concept.CodeValue != null && concept.CodeValue.Value.Length > 0)
        {
            return concept.CodeValue.Value;
        }
        return fallback;
    }

    internal static RepositoryEntityKind FindRepositoryKind(GenerationConceptKind kind)
    {
        if (kind == GenerationConceptKind.MessageDefinition)
        {
            return RepositoryEntityKind.MessageDefinition;
        }
        if (kind == GenerationConceptKind.MessageBuildingBlock)
        {
            return RepositoryEntityKind.MessageBuildingBlock;
        }
        if (kind == GenerationConceptKind.MessageComponent)
        {
            return RepositoryEntityKind.MessageComponent;
        }
        if (kind == GenerationConceptKind.MessageElement)
        {
            return RepositoryEntityKind.MessageElement;
        }
        if (kind == GenerationConceptKind.Choice)
        {
            return RepositoryEntityKind.Choice;
        }
        if (kind == GenerationConceptKind.DataType)
        {
            return RepositoryEntityKind.DataType;
        }
        if (kind == GenerationConceptKind.CodeSet)
        {
            return RepositoryEntityKind.CodeSet;
        }
        if (kind == GenerationConceptKind.CodeValue)
        {
            return RepositoryEntityKind.CodeValue;
        }
        if (kind == GenerationConceptKind.ExternalSchema)
        {
            return RepositoryEntityKind.ExternalSchema;
        }
        return RepositoryEntityKind.Unknown;
    }

    internal static string FindCSharpOutputPath(
        List<GeneratedFile> files,
        GenerationSymbolEntry entry)
    {
        string typeName = entry.SymbolName;
        if (typeName.Length > 0 && typeName[0] == '@')
        {
            typeName = typeName.Substring(1);
        }
        string suffix = "/" + typeName + ".cs";
        int index = 0;
        while (index < files.Count)
        {
            GeneratedFile file = files[index];
            if (file.Kind == GenerationFileKind.CSharp &&
                file.NamespaceName == entry.NamespaceName &&
                file.RelativePath.EndsWith(suffix, StringComparison.Ordinal))
            {
                return file.RelativePath;
            }
            index++;
        }
        return string.Empty;
    }

    internal static void NormalizeSourceHashes(List<ArtifactManifest> artifacts)
    {
        int index = 0;
        while (index < artifacts.Count)
        {
            ArtifactManifest artifact = artifacts[index];
            if (artifact.Hash.Value.Length > 0)
            {
                if (artifact.HashAlgorithm.Length == 0)
                {
                    artifact.HashAlgorithm = "SHA-256";
                }
                SetBinaryHash(artifact.Hash, artifact.Hash.Value);
            }
            index++;
        }
    }

    internal static void NormalizeFileHashes(List<GeneratedFile> files)
    {
        int index = 0;
        while (index < files.Count)
        {
            GeneratedFile file = files[index];
            if (file.Hash.Value.Length == 0)
            {
                file.Hash.Value = SHA256.HashData(file.Content);
            }
            if (file.HashAlgorithm.Length == 0)
            {
                file.HashAlgorithm = "SHA-256";
            }
            SetBinaryHash(file.Hash, file.Hash.Value);
            SortTextValues(file.SourceIdentifiers);
            index++;
        }
    }

    internal static void SetBinaryHash(IsoBinaryValue value, byte[] hash)
    {
        value.Value = hash;
        value.Encoding = BinaryEncodingKind.Hexadecimal;
        value.LexicalValue = Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static byte[] CreateSettingsContent(GenerationManifest manifest)
    {
        MemoryStream stream = new MemoryStream();
        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
        {
            WriteSettingsObject(writer, manifest);
        }
        return stream.ToArray();
    }

    internal static byte[] CreateOutputIdentityContent(List<GeneratedFile> files)
    {
        MemoryStream stream = new MemoryStream();
        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            int index = 0;
            while (index < files.Count)
            {
                WriteFileIdentity(writer, files[index]);
                index++;
            }
            writer.WriteEndArray();
        }
        return stream.ToArray();
    }

    internal static byte[] CreateManifestContent(
        GenerationManifest manifest,
        bool includeManifestHash)
    {
        MemoryStream stream = new MemoryStream();
        JsonWriterOptions options = new JsonWriterOptions();
        options.Indented = true;
        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, options))
        {
            writer.WriteStartObject();
            writer.WriteString("formatVersion", manifest.FormatVersion);
            writer.WriteString("generatorIdentifier", manifest.GeneratorIdentifier);
            writer.WriteString("generatorVersion", manifest.GeneratorVersion);
            writer.WriteString("artifactEdition", manifest.ArtifactEdition);
            writer.WriteString("profile", manifest.Profile);
            writer.WriteString("rootNamespace", manifest.RootNamespace);
            writer.WriteString("hashAlgorithm", manifest.HashAlgorithm);
            writer.WriteString("settingsHash", manifest.SettingsHash.LexicalValue);
            writer.WriteString("outputHash", manifest.OutputHash.LexicalValue);
            if (includeManifestHash)
            {
                writer.WriteString("manifestHash", manifest.ManifestHash.LexicalValue);
            }
            WriteSettingsProperty(writer, manifest);
            WriteSourceArtifacts(writer, manifest.SourceArtifacts);
            WriteSymbols(writer, manifest.Symbols);
            WriteFiles(writer, manifest.Files);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static void WriteSettingsProperty(
        Utf8JsonWriter writer,
        GenerationManifest manifest)
    {
        writer.WritePropertyName("settings");
        WriteSettingsObject(writer, manifest);
    }

    internal static void WriteSettingsObject(Utf8JsonWriter writer, GenerationManifest manifest)
    {
        GenerationOptions options = manifest.Options;
        writer.WriteStartObject();
        writer.WriteString("generatorIdentifier", manifest.GeneratorIdentifier);
        writer.WriteString("generatorVersion", manifest.GeneratorVersion);
        writer.WriteString("rootNamespace", options.RootNamespace);
        writer.WriteString("outputRootPath", options.OutputRootPath);
        writer.WriteString("artifactEdition", options.ArtifactEdition);
        writer.WriteString("profile", options.Profile);
        writer.WriteBoolean("includeDeprecated", options.IncludeDeprecated);
        writer.WriteBoolean("includeDocumentation", options.IncludeDocumentation);
        writer.WriteBoolean("emitSourceMappings", options.EmitSourceMappings);
        writer.WriteBoolean("emitManifest", options.EmitManifest);
        writer.WriteString("indentation", options.Indentation);
        writer.WriteString("newLine", options.NewLine);
        WriteMessageIdentifiers(writer, options.MessageIdentifiers);
        WriteNamespaceUris(writer, options.NamespaceUris);
        WriteLimits(writer, options.Limits);
        writer.WriteEndObject();
    }

    internal static void WriteMessageIdentifiers(
        Utf8JsonWriter writer,
        List<MessageIdentifier> identifiers)
    {
        List<MessageIdentifier> values = new List<MessageIdentifier>();
        values.AddRange(identifiers);
        SortMessageIdentifiers(values);
        writer.WritePropertyName("messageIdentifiers");
        writer.WriteStartArray();
        int index = 0;
        while (index < values.Count)
        {
            MessageIdentifier identifier = values[index];
            writer.WriteStartObject();
            writer.WriteString("value", identifier.Value);
            writer.WriteString("businessArea", identifier.BusinessArea);
            writer.WriteString("messageFunctionality", identifier.MessageFunctionality);
            writer.WriteString("variant", identifier.Variant);
            writer.WriteString("version", identifier.Version);
            writer.WriteEndObject();
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void SortMessageIdentifiers(List<MessageIdentifier> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            MessageIdentifier value = values[index];
            int destination = index;
            while (destination > 0 &&
                CompareMessageIdentifiers(value, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }

    internal static int CompareMessageIdentifiers(
        MessageIdentifier left,
        MessageIdentifier right)
    {
        int comparison = string.CompareOrdinal(left.Value, right.Value);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.BusinessArea, right.BusinessArea);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(
            left.MessageFunctionality, right.MessageFunctionality);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Variant, right.Variant);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Version, right.Version);
    }

    internal static void WriteNamespaceUris(Utf8JsonWriter writer, List<string> namespaceUris)
    {
        List<string> values = new List<string>();
        values.AddRange(namespaceUris);
        SortTextValues(values);
        writer.WritePropertyName("namespaceUris");
        WriteTextArray(writer, values);
    }

    internal static void WriteLimits(Utf8JsonWriter writer, ResourceLimits limits)
    {
        writer.WritePropertyName("limits");
        writer.WriteStartObject();
        writer.WriteNumber("maxDepth", limits.MaxDepth);
        writer.WriteNumber("maxAttributesPerElement", limits.MaxAttributesPerElement);
        writer.WriteNumber("maxTextLength", limits.MaxTextLength);
        writer.WriteNumber("maxElementCount", limits.MaxElementCount);
        writer.WriteNumber("maxCollectionCount", limits.MaxCollectionCount);
        writer.WriteNumber("maxTotalBytes", limits.MaxTotalBytes);
        writer.WriteNumber("maxSchemaCount", limits.MaxSchemaCount);
        writer.WriteNumber("maxDiagnosticCount", limits.MaxDiagnosticCount);
        writer.WriteEndObject();
    }

    internal static void WriteSourceArtifacts(
        Utf8JsonWriter writer,
        List<ArtifactManifest> artifacts)
    {
        writer.WritePropertyName("sourceArtifacts");
        writer.WriteStartArray();
        int index = 0;
        while (index < artifacts.Count)
        {
            ArtifactManifest artifact = artifacts[index];
            writer.WriteStartObject();
            writer.WriteString("kind", artifact.Coordinate.Kind.ToString());
            writer.WriteString("edition", artifact.Coordinate.Edition);
            writer.WriteString("identifier", artifact.Coordinate.Identifier);
            writer.WriteString("version", artifact.Coordinate.Version);
            writer.WriteString("originUri", artifact.OriginUri);
            writer.WriteString("retrievedAt", FindDateTimeLexical(artifact.RetrievedAt));
            writer.WriteString("publicationDate", FindPartialDateLexical(
                artifact.PublicationDate));
            writer.WriteString("hashAlgorithm", artifact.HashAlgorithm);
            writer.WriteString("hash", artifact.Hash.LexicalValue);
            writer.WriteString("mediaType", artifact.MediaType);
            writer.WriteString("licenseNotice", artifact.LicenseNotice);
            writer.WriteBoolean("isApproved", artifact.IsApproved);
            writer.WriteEndObject();
            index++;
        }
        writer.WriteEndArray();
    }

    internal static string FindDateTimeLexical(IsoDateTimeValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        return value.LexicalValue;
    }

    internal static string FindPartialDateLexical(IsoPartialDateValue? value)
    {
        if (value == null)
        {
            return string.Empty;
        }
        return value.LexicalValue;
    }

    internal static void WriteSymbols(Utf8JsonWriter writer, List<GenerationSymbol> symbols)
    {
        writer.WritePropertyName("symbols");
        writer.WriteStartArray();
        int index = 0;
        while (index < symbols.Count)
        {
            GenerationSymbol symbol = symbols[index];
            writer.WriteStartObject();
            writer.WriteString("sourceIdentifier", symbol.SourceIdentifier);
            writer.WriteString("sourceType", symbol.SourceType);
            writer.WriteString("repositoryKind", symbol.RepositoryKind.ToString());
            writer.WriteString("sourceName", symbol.SourceName);
            writer.WriteString("namespaceUri", symbol.NamespaceUri);
            writer.WriteString("xmlName", symbol.XmlName);
            writer.WriteString("cSharpNamespace", symbol.CSharpNamespace);
            writer.WriteString("cSharpName", symbol.CSharpName);
            writer.WriteString("shapeIdentifier", symbol.ShapeIdentifier);
            writer.WriteString("outputPath", symbol.OutputPath);
            writer.WriteNumber("order", symbol.Order);
            writer.WriteBoolean("hasCollision", symbol.HasCollision);
            writer.WriteEndObject();
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void WriteFiles(Utf8JsonWriter writer, List<GeneratedFile> files)
    {
        writer.WritePropertyName("files");
        writer.WriteStartArray();
        int index = 0;
        while (index < files.Count)
        {
            WriteFileIdentity(writer, files[index]);
            index++;
        }
        writer.WriteEndArray();
    }

    internal static void WriteFileIdentity(Utf8JsonWriter writer, GeneratedFile file)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", file.Kind.ToString());
        writer.WriteString("relativePath", file.RelativePath);
        writer.WriteString("mediaType", file.MediaType);
        writer.WriteString("namespaceName", file.NamespaceName);
        writer.WriteString("hashAlgorithm", file.HashAlgorithm);
        writer.WriteString("hash", file.Hash.LexicalValue);
        writer.WritePropertyName("sourceIdentifiers");
        WriteTextArray(writer, file.SourceIdentifiers);
        writer.WriteEndObject();
    }

    internal static void WriteTextArray(Utf8JsonWriter writer, List<string> values)
    {
        writer.WriteStartArray();
        int index = 0;
        while (index < values.Count)
        {
            writer.WriteStringValue(values[index]);
            index++;
        }
        writer.WriteEndArray();
    }

    internal static string CreateManifestPath(string generatorIdentifier)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < generatorIdentifier.Length)
        {
            char current = generatorIdentifier[index];
            if ((current >= 'a' && current <= 'z') ||
                (current >= 'A' && current <= 'Z') ||
                (current >= '0' && current <= '9') || current == '.' ||
                current == '-' || current == '_')
            {
                value.Append(current);
            }
            else
            {
                value.Append('_');
            }
            index++;
        }
        return "Generation/" + value + ".manifest.json";
    }

    internal static void SortSourceArtifacts(List<ArtifactManifest> artifacts)
    {
        int index = 1;
        while (index < artifacts.Count)
        {
            ArtifactManifest value = artifacts[index];
            int destination = index;
            while (destination > 0 &&
                CompareSourceArtifacts(value, artifacts[destination - 1]) < 0)
            {
                artifacts[destination] = artifacts[destination - 1];
                destination--;
            }
            artifacts[destination] = value;
            index++;
        }
    }

    internal static int CompareSourceArtifacts(ArtifactManifest left, ArtifactManifest right)
    {
        int comparison = string.CompareOrdinal(left.OriginUri, right.OriginUri);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Coordinate.Identifier,
            right.Coordinate.Identifier);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.Coordinate.Edition, right.Coordinate.Edition);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.Coordinate.Version, right.Coordinate.Version);
    }

    internal static void SortManifestSymbols(List<GenerationSymbol> symbols)
    {
        symbols.Sort((left, right) => CompareManifestSymbols(left, right));
    }

    internal static int CompareManifestSymbols(GenerationSymbol left, GenerationSymbol right)
    {
        int comparison = string.CompareOrdinal(left.CSharpNamespace, right.CSharpNamespace);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.CSharpName, right.CSharpName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.NamespaceUri, right.NamespaceUri);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.XmlName, right.XmlName);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = string.CompareOrdinal(left.SourceIdentifier, right.SourceIdentifier);
        if (comparison != 0)
        {
            return comparison;
        }
        return string.CompareOrdinal(left.ShapeIdentifier, right.ShapeIdentifier);
    }

    internal static void SetSymbolOrder(List<GenerationSymbol> symbols)
    {
        int index = 0;
        while (index < symbols.Count)
        {
            symbols[index].Order = index;
            index++;
        }
    }

    internal static void SortGeneratedFiles(List<GeneratedFile> files)
    {
        files.Sort((left, right) => string.CompareOrdinal(
            left.RelativePath,
            right.RelativePath));
    }

    internal static void SortTextValues(List<string> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            string value = values[index];
            int destination = index;
            while (destination > 0 &&
                string.CompareOrdinal(value, values[destination - 1]) < 0)
            {
                values[destination] = values[destination - 1];
                destination--;
            }
            values[destination] = value;
            index++;
        }
    }
}
