using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace iso20022;

/// <summary>Generates deterministic XML Schema 1.0 files from repository and normalized XSD concepts.</summary>
public class XsdGenerator : IXsdGenerator
{
    /// <summary>Initializes a deterministic logical-model-to-XSD generator.</summary>
    public XsdGenerator()
    {
    }

    /// <summary>Generates the selected XML Schema files and their ownership manifest.</summary>
    /// <param name="plan">The exact sources, selection, settings, and previous ownership state.</param><returns>The exception-less deterministic generation result.</returns>
    public GenerationResult GenerateXsd(GenerationPlan? plan)
    {
        if (plan == null)
        {
            return CreateGenerationFailure(
                null,
                "generation.plan.required",
                "A generation plan is required.");
        }
        GenerationResult? invalid = ValidateGenerationPlan(plan);
        if (invalid != null)
        {
            return invalid;
        }

        GenerationPlan selectedPlan = plan;
        GenerationResult result = new GenerationResult();
        result.Plan = selectedPlan;
        InitializeManifest(result, selectedPlan);
        List<XsdGenerationUnit> units = BuildGenerationUnits(selectedPlan);
        if (units.Count == 0)
        {
            return CreateGenerationFailure(
                selectedPlan,
                "generation.xsd.selection_empty",
                "The generation plan did not select any repository message or XSD artifact.");
        }
        if (units.Count > selectedPlan.Options.Limits.MaxSchemaCount)
        {
            return CreateGenerationFailure(
                selectedPlan,
                "generation.xsd.schema_limit",
                "The selected output exceeds the configured schema-count limit.");
        }

        SortGenerationUnits(units);
        long totalBytes = 0;
        int index = 0;
        while (index < units.Count)
        {
            selectedPlan.Options.CancellationToken.ThrowIfCancellationRequested();
            XsdGenerationUnit unit = units[index];
            if (unit.NamespaceUri.Length > 0 &&
                !UriLexical.IsNamespaceUri(unit.NamespaceUri.AsSpan()))
            {
                return CreateGenerationFailure(
                    selectedPlan,
                    "generation.xsd.namespace_invalid",
                    "A selected target namespace is not a valid namespace URI.");
            }
            XsdGenerationContext context = XsdLogicalModelEmitter.CreateContext(
                unit,
                selectedPlan.Options,
                result);
            long itemCount = XsdLogicalModelEmitter.CountGenerationItems(context);
            if (itemCount > selectedPlan.Options.Limits.MaxCollectionCount ||
                itemCount > selectedPlan.Options.Limits.MaxElementCount)
            {
                return CreateGenerationFailure(
                    selectedPlan,
                    "generation.xsd.model_limit",
                    "A selected logical model exceeds the configured generation limits.");
            }
            byte[] content = XsdLogicalModelEmitter.EmitSchema(context);
            totalBytes += content.Length;
            if (content.Length > selectedPlan.Options.Limits.MaxTotalBytes ||
                totalBytes > selectedPlan.Options.Limits.MaxTotalBytes)
            {
                return CreateGenerationFailure(
                    selectedPlan,
                    "generation.xsd.byte_limit",
                    "The generated output exceeds the configured total-byte limit.");
            }

            GeneratedFile file = CreateGeneratedFile(unit, content, result.Files);
            result.Files.Add(file);
            result.Manifest.Files.Add(file);
            GenerationManifestEmitter.AddSchemaMappings(context, file, result.Manifest);
            index++;
        }

        GenerationManifestEmitter.CompleteManifest(result);
        if (!IncrementalGenerationWriter.ApplyOutput(result))
        {
            return result;
        }
        result.Success = true;
        return result;
    }

    internal GenerationResult? ValidateGenerationPlan(GenerationPlan plan)
    {
        if (plan.Options == null || plan.Schemas == null ||
            plan.Options.MessageIdentifiers == null || plan.Options.NamespaceUris == null ||
            plan.Options.Limits == null || plan.Options.Indentation == null ||
            plan.Options.NewLine == null || plan.Options.ArtifactEdition == null ||
            plan.Options.Profile == null || plan.Options.RootNamespace == null ||
            plan.Options.OutputRootPath == null)
        {
            return CreateGenerationFailure(
                plan,
                "generation.plan.invalid",
                "The generation plan contains a missing required value.");
        }
        ResourceLimits limits = plan.Options.Limits;
        if (limits.MaxDepth <= 0 || limits.MaxAttributesPerElement <= 0 ||
            limits.MaxTextLength <= 0 || limits.MaxElementCount <= 0 ||
            limits.MaxCollectionCount <= 0 || limits.MaxTotalBytes <= 0 ||
            limits.MaxSchemaCount <= 0 || limits.MaxDiagnosticCount <= 0)
        {
            return CreateGenerationFailure(
                plan,
                "generation.limits.invalid",
                "All generation limits must be positive.");
        }
        if (plan.Repository == null && plan.Schemas.Count == 0)
        {
            return CreateGenerationFailure(
                plan,
                "generation.sources.required",
                "A repository snapshot or normalized XSD artifact is required.");
        }
        if (plan.Schemas.Count > limits.MaxCollectionCount ||
            plan.Options.MessageIdentifiers.Count > limits.MaxCollectionCount ||
            plan.Options.NamespaceUris.Count > limits.MaxCollectionCount)
        {
            return CreateGenerationFailure(
                plan,
                "generation.collection.limit",
                "A generation input collection exceeds the configured collection limit.");
        }
        return null;
    }

    internal void InitializeManifest(GenerationResult result, GenerationPlan plan)
    {
        result.Manifest.ArtifactEdition = plan.Options.ArtifactEdition;
        result.Manifest.Profile = plan.Options.Profile;
        result.Manifest.RootNamespace = plan.Options.RootNamespace;
        result.Manifest.Options = plan.Options;
        result.Manifest.GeneratorIdentifier = "iso20022.xsd";
        result.Manifest.GeneratorVersion = "1.0.0";
        result.Manifest.HashAlgorithm = "SHA-256";
        if (plan.Repository != null)
        {
            result.Manifest.SourceArtifacts.Add(plan.Repository.Provenance.Artifact);
        }
        int index = 0;
        while (index < plan.Schemas.Count)
        {
            if (plan.Schemas[index].Artifact.Hash.Value.Length == 0 &&
                plan.Schemas[index].Content.Length > 0)
            {
                GenerationManifestEmitter.SetBinaryHash(
                    plan.Schemas[index].Artifact.Hash,
                    SHA256.HashData(plan.Schemas[index].Content));
                plan.Schemas[index].Artifact.HashAlgorithm = "SHA-256";
            }
            result.Manifest.SourceArtifacts.Add(plan.Schemas[index].Artifact);
            index++;
        }
    }

    internal List<XsdGenerationUnit> BuildGenerationUnits(GenerationPlan plan)
    {
        List<XsdGenerationUnit> units = new List<XsdGenerationUnit>();
        int schemaIndex = 0;
        while (schemaIndex < plan.Schemas.Count)
        {
            SchemaArtifact schema = plan.Schemas[schemaIndex];
            if (SchemaIsSelected(schema, plan.Options))
            {
                XsdGenerationUnit unit = new XsdGenerationUnit();
                unit.Identity = "xsd|" + schema.NamespaceUri + "|" + schema.Version + "|" +
                    schema.Profile + "|" + schema.SourceName;
                unit.NamespaceUri = schema.NamespaceUri;
                unit.FileStem = FindSchemaFileStem(schema);
                unit.Schema = schema;
                units.Add(unit);
            }
            schemaIndex++;
        }

        if (plan.Repository != null)
        {
            int messageIndex = 0;
            while (messageIndex < plan.Repository.MessageDefinitions.Count)
            {
                MessageDefinitionMetadata message = plan.Repository.MessageDefinitions[messageIndex];
                if (MessageIsSelected(message, plan.Options) &&
                    !SchemaNamespaceExists(units, message.NamespaceUri))
                {
                    XsdGenerationUnit unit = new XsdGenerationUnit();
                    unit.Identity = "repository|" + plan.Repository.Identifier + "|" +
                        FindMessageIdentity(message);
                    unit.NamespaceUri = message.NamespaceUri;
                    unit.FileStem = FindMessageFileStem(message);
                    unit.Repository = plan.Repository;
                    unit.Message = message;
                    units.Add(unit);
                }
                messageIndex++;
            }
        }
        return units;
    }

    internal bool SchemaIsSelected(SchemaArtifact schema, GenerationOptions options)
    {
        if (!NamespaceIsSelected(schema.NamespaceUri, options.NamespaceUris))
        {
            return false;
        }
        if (options.MessageIdentifiers.Count == 0)
        {
            return true;
        }
        if (schema.MessageIdentifier != null &&
            MessageIdentifierIsSelected(schema.MessageIdentifier, options.MessageIdentifiers))
        {
            return true;
        }
        int index = 0;
        while (index < schema.MessageDefinitions.Count)
        {
            if (MessageIdentifierIsSelected(
                schema.MessageDefinitions[index].MessageIdentifier,
                options.MessageIdentifiers))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal bool MessageIsSelected(MessageDefinitionMetadata message, GenerationOptions options)
    {
        if (!options.IncludeDeprecated && message.Registration.IsDeprecated)
        {
            return false;
        }
        if (!NamespaceIsSelected(message.NamespaceUri, options.NamespaceUris))
        {
            return false;
        }
        return MessageIdentifierIsSelected(message.MessageIdentifier, options.MessageIdentifiers);
    }

    internal bool NamespaceIsSelected(string namespaceUri, List<string> selected)
    {
        if (selected.Count == 0)
        {
            return true;
        }
        int index = 0;
        while (index < selected.Count)
        {
            if (selected[index] == namespaceUri)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal bool MessageIdentifierIsSelected(
        MessageIdentifier identifier,
        List<MessageIdentifier> selected)
    {
        if (selected.Count == 0)
        {
            return true;
        }
        string value = FindIdentifierValue(identifier);
        int index = 0;
        while (index < selected.Count)
        {
            if (FindIdentifierValue(selected[index]) == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal string FindIdentifierValue(MessageIdentifier identifier)
    {
        if (identifier.Value.Length > 0)
        {
            return identifier.Value;
        }
        return identifier.BusinessArea + "." + identifier.MessageFunctionality + "." +
            identifier.Variant + "." + identifier.Version;
    }

    internal string FindMessageIdentity(MessageDefinitionMetadata message)
    {
        string value = FindIdentifierValue(message.MessageIdentifier);
        if (value != "...")
        {
            return value;
        }
        if (message.Identifier.Length > 0)
        {
            return message.Identifier;
        }
        return message.Name;
    }

    internal string FindMessageFileStem(MessageDefinitionMetadata message)
    {
        string value = FindMessageIdentity(message);
        return CreateSafeFileStem(value);
    }

    internal string FindSchemaFileStem(SchemaArtifact schema)
    {
        string value = Path.GetFileNameWithoutExtension(schema.SourceName);
        if (value.Length == 0 && schema.MessageIdentifier != null)
        {
            value = FindIdentifierValue(schema.MessageIdentifier);
        }
        if (value.Length == 0)
        {
            value = "schema_" + GenerationStableHash.CreateHex(schema.NamespaceUri).Substring(0, 16);
        }
        return CreateSafeFileStem(value);
    }

    internal string CreateSafeFileStem(string value)
    {
        GenerationIdentifierNamer namer = new GenerationIdentifierNamer();
        return namer.CreateTypeName(value);
    }

    internal bool SchemaNamespaceExists(List<XsdGenerationUnit> units, string namespaceUri)
    {
        int index = 0;
        while (index < units.Count)
        {
            if (units[index].Schema != null && units[index].NamespaceUri == namespaceUri)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal void SortGenerationUnits(List<XsdGenerationUnit> units)
    {
        int index = 1;
        while (index < units.Count)
        {
            XsdGenerationUnit value = units[index];
            int destination = index;
            while (destination > 0 &&
                string.CompareOrdinal(value.Identity, units[destination - 1].Identity) < 0)
            {
                units[destination] = units[destination - 1];
                destination--;
            }
            units[destination] = value;
            index++;
        }
    }

    internal GeneratedFile CreateGeneratedFile(
        XsdGenerationUnit unit,
        byte[] content,
        List<GeneratedFile> existing)
    {
        string path = "Schemas/" + unit.FileStem + ".xsd";
        if (OutputPathExists(existing, path))
        {
            path = "Schemas/" + unit.FileStem + "_" +
                GenerationStableHash.CreateHex(unit.Identity).Substring(0, 16) + ".xsd";
        }
        GeneratedFile file = new GeneratedFile();
        file.Kind = GenerationFileKind.Xsd;
        file.RelativePath = path;
        file.MediaType = Iso20022MediaTypes.XmlSchema;
        file.NamespaceName = unit.NamespaceUri;
        file.Content = content;
        file.Hash.Value = SHA256.HashData(content);
        file.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        file.HashAlgorithm = "SHA-256";
        if (unit.Message != null)
        {
            file.SourceIdentifiers.Add(unit.Message.Identifier);
        }
        if (unit.Schema != null)
        {
            int index = 0;
            while (index < unit.Schema.MessageDefinitions.Count)
            {
                file.SourceIdentifiers.Add(unit.Schema.MessageDefinitions[index].Identifier);
                index++;
            }
        }
        return file;
    }

    internal bool OutputPathExists(List<GeneratedFile> files, string path)
    {
        int index = 0;
        while (index < files.Count)
        {
            if (files[index].RelativePath == path)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal GenerationResult CreateGenerationFailure(
        GenerationPlan? plan,
        string errorCode,
        string errorMessage)
    {
        GenerationResult result = new GenerationResult();
        if (plan != null)
        {
            result.Plan = plan;
        }
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
