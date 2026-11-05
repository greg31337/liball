using System;
using System.IO;
using System.Security.Cryptography;

namespace iso20022;

/// <summary>Imports bounded local XSD content into normalized schema artifacts.</summary>
public class SchemaImporter : ISchemaImporter
{
    /// <summary>Initializes a local schema importer.</summary>
    public SchemaImporter()
    {
    }

    /// <summary>Imports one XSD stream and its bounded local dependency closure.</summary>
    /// <param name="input">The encoded XSD stream.</param><param name="schema">The source identity, provenance, and registry key.</param><param name="options">The local resolution and resource policy.</param><returns>The normalized schema import result.</returns>
    public SchemaCompilationResult ImportSchemaStream(
        Stream? input,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        if (input == null)
        {
            return CreateSchemaFailure(
                "schema.input_missing",
                "An XSD input stream is required.");
        }

        if (schema == null)
        {
            return CreateSchemaFailure(
                "schema.request_missing",
                "Schema source identity and provenance are required.");
        }

        if (options == null)
        {
            return CreateSchemaFailure(
                "schema.options_missing",
                "Schema import options are required.");
        }

        SchemaCompilationResult? validation = ValidateSchemaRequest(schema, options);
        if (validation != null)
        {
            return validation;
        }

        SchemaCompilationResult result = ImportSchemaDocument(input, schema, options);
        if (result.Success)
        {
            SchemaDependencyResolver.ResolveClosure(this, result, options);
        }

        return result;
    }

    internal SchemaCompilationResult ImportSchemaDocument(
        Stream input,
        SchemaArtifact source,
        SchemaRegistryOptions policy)
    {
        policy.CancellationToken.ThrowIfCancellationRequested();
        BoundedArtifactCopyResult copied = CopySchemaInput(
            input,
            policy.Limits.MaxTotalBytes,
            policy);
        if (!copied.Success)
        {
            return CreateSchemaFailure(copied.ErrorCode, copied.ErrorMessage);
        }

        byte[] hash = SHA256.HashData(copied.Bytes);
        if (source.Artifact.Hash.Value.Length > 0 &&
            !CryptographicOperations.FixedTimeEquals(source.Artifact.Hash.Value, hash))
        {
            return CreateSchemaFailure(
                "schema.artifact_hash_mismatch",
                "The XSD content does not match the supplied SHA-256 hash.");
        }

        SchemaArtifact imported = CreateImportedSchema(source, copied.Bytes, hash);
        SchemaCompilationResult result = new SchemaCompilationResult();
        SchemaXsdReadState state = new SchemaXsdReadState();
        state.Schema = imported;
        state.Options = policy;
        state.Result = result;
        SchemaXsdReader.ReadSchema(copied.Bytes, state);
        if (!state.Success)
        {
            return result;
        }

        ApplyRepositoryFallback(imported, policy, result);
        SchemaCompilationResult? identity = FinalizeSchemaIdentity(imported, source, state);
        if (identity != null)
        {
            return identity;
        }

        result.Success = true;
        result.Found = true;
        result.RootSchema = imported;
        result.Schemas.Add(imported);
        return result;
    }

    /// <summary>Imports one XSD file and its bounded local dependency closure.</summary>
    /// <param name="path">The caller-selected XSD file path.</param><param name="schema">The source identity, provenance, and registry key.</param><param name="options">The local resolution and resource policy.</param><returns>The normalized schema import result.</returns>
    public SchemaCompilationResult ImportSchemaFile(
        string? path,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        if (path == null || path.Length == 0)
        {
            return CreateSchemaFailure(
                "schema.path_missing",
                "An XSD file path is required.");
        }

        if (schema == null)
        {
            return CreateSchemaFailure(
                "schema.request_missing",
                "Schema source identity and provenance are required.");
        }

        if (options == null)
        {
            return CreateSchemaFailure(
                "schema.options_missing",
                "Schema import options are required.");
        }

        SchemaCompilationResult? validation = ValidateSchemaRequest(schema, options);
        if (validation != null)
        {
            return validation;
        }

        SchemaArtifact fileSchema = CloneSchemaIdentity(schema);
        if (fileSchema.SourceName.Length == 0)
        {
            fileSchema.SourceName = Path.GetFileName(path);
        }

        if (fileSchema.BaseUri.Length == 0)
        {
            fileSchema.BaseUri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
        }

        using (FileStream input = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return ImportSchemaStream(input, fileSchema, options);
        }
    }

    internal static SchemaCompilationResult? ValidateSchemaRequest(
        SchemaArtifact schema,
        SchemaRegistryOptions options)
    {
        if (schema.Artifact == null || schema.Artifact.Coordinate == null ||
            schema.Artifact.Hash == null || schema.Artifact.Hash.Value == null ||
            schema.SourceName == null || schema.BaseUri == null ||
            schema.NamespaceUri == null || schema.Version == null || schema.Profile == null ||
            schema.RootElement == null || schema.Content == null ||
            schema.IncludeLocations == null || schema.ImportNamespaceUris == null ||
            schema.ImportLocations == null || schema.GlobalElements == null ||
            schema.GlobalTypes == null || schema.DataTypes == null ||
            schema.MessageComponents == null || schema.MessageElements == null ||
            schema.Choices == null || schema.ExternalSchemas == null ||
            schema.MessageDefinitions == null || schema.RepositoryFallbackDefinitions == null ||
            options.LocalRootPath == null || options.Limits == null ||
            options.LocalSchemas == null)
        {
            return CreateSchemaFailure(
                "schema.request_invalid",
                "The schema request contains a missing required value.");
        }

        if (schema.Artifact.Coordinate.Kind != ArtifactKind.Schema)
        {
            return CreateSchemaFailure(
                "schema.artifact_kind_invalid",
                "The schema artifact coordinate must use the schema kind.");
        }

        if (options.Limits.MaxDepth <= 0 || options.Limits.MaxAttributesPerElement <= 0 ||
            options.Limits.MaxTextLength <= 0 || options.Limits.MaxElementCount <= 0 ||
            options.Limits.MaxCollectionCount <= 0 || options.Limits.MaxTotalBytes <= 0 ||
            options.Limits.MaxSchemaCount <= 0 || options.Limits.MaxDiagnosticCount <= 0)
        {
            return CreateSchemaFailure(
                "schema.limits_invalid",
                "All schema import limits must be positive.");
        }

        if (schema.Artifact.Hash.Value.Length > 0 &&
            (schema.Artifact.Hash.Value.Length != 32 ||
            !FileArtifactStore.IsSha256(schema.Artifact.HashAlgorithm)))
        {
            return CreateSchemaFailure(
                "schema.artifact_hash_invalid",
                "A supplied schema artifact hash must be a 32-byte SHA-256 value.");
        }

        if (schema.MessageIdentifier != null &&
            MessageIdentifierLexical.FormatMessageIdentifier(schema.MessageIdentifier) == null)
        {
            return CreateSchemaFailure(
                "schema.message_identifier_invalid",
                "The schema message identifier is not valid.");
        }

        return null;
    }

    internal static BoundedArtifactCopyResult CopySchemaInput(
        Stream input,
        int maximumBytes,
        SchemaRegistryOptions options)
    {
        BoundedArtifactCopyResult result = new BoundedArtifactCopyResult();
        MemoryStream output = new MemoryStream();
        byte[] buffer = new byte[81920];
        long total = 0;
        int read = input.Read(buffer, 0, buffer.Length);
        while (read > 0)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            if (total > maximumBytes - read)
            {
                output.Dispose();
                result.ErrorCode = "schema.input_limit";
                result.ErrorMessage = "The XSD input exceeds the configured total-byte limit.";
                return result;
            }

            output.Write(buffer, 0, read);
            total = total + read;
            read = input.Read(buffer, 0, buffer.Length);
        }

        result.Success = true;
        result.Bytes = output.ToArray();
        output.Dispose();
        return result;
    }

    internal static SchemaArtifact CreateImportedSchema(
        SchemaArtifact source,
        byte[] content,
        byte[] hash)
    {
        SchemaArtifact result = CloneSchemaIdentity(source);
        result.Artifact = FileArtifactStore.CloneManifestWithHash(source.Artifact, hash);
        result.Artifact.MediaType = Iso20022MediaTypes.XmlSchema;
        result.Content = content;
        return result;
    }

    internal static SchemaArtifact CloneSchemaIdentity(SchemaArtifact source)
    {
        SchemaArtifact result = new SchemaArtifact();
        result.Artifact = source.Artifact;
        result.SourceName = source.SourceName;
        result.BaseUri = source.BaseUri;
        result.NamespaceUri = source.NamespaceUri;
        result.MessageIdentifier = source.MessageIdentifier;
        result.Version = source.Version;
        result.Profile = source.Profile;
        result.RootElement = source.RootElement;
        result.Precedence = source.Precedence;
        result.IsEntryPoint = source.IsEntryPoint;
        int index = 0;
        while (index < source.RepositoryFallbackDefinitions.Count)
        {
            result.RepositoryFallbackDefinitions.Add(source.RepositoryFallbackDefinitions[index]);
            index++;
        }

        return result;
    }

    internal static SchemaCompilationResult? FinalizeSchemaIdentity(
        SchemaArtifact imported,
        SchemaArtifact source,
        SchemaXsdReadState state)
    {
        if (source.NamespaceUri.Length > 0 && imported.NamespaceUri != source.NamespaceUri)
        {
            return CreateSchemaFailure(
                "schema.namespace_mismatch",
                "The XSD target namespace does not match the requested registry namespace.");
        }

        if (imported.Version.Length == 0)
        {
            imported.Version = state.ParsedVersion;
        }

        if (imported.MessageIdentifier == null && imported.NamespaceUri.Length > 0)
        {
            NamespaceServiceResult parsed = new NamespaceService().ParseNamespace(
                imported.NamespaceUri,
                NamespaceMode.Strict);
            if (parsed.Success && parsed.Descriptor != null)
            {
                imported.MessageIdentifier = parsed.Descriptor.MessageIdentifier;
            }
        }

        UpdateQualifiedSchemaNamespaces(imported);

        int index = 0;
        while (index < imported.MessageDefinitions.Count)
        {
            MessageDefinitionMetadata definition = imported.MessageDefinitions[index];
            if (imported.MessageIdentifier != null)
            {
                definition.MessageIdentifier = imported.MessageIdentifier;
            }

            definition.NamespaceUri = imported.NamespaceUri;
            index++;
        }

        return null;
    }

    internal static void UpdateQualifiedSchemaNamespaces(SchemaArtifact schema)
    {
        int index = 0;
        while (index < schema.GlobalTypes.Count)
        {
            schema.GlobalTypes[index].NamespaceUri = schema.NamespaceUri;
            index++;
        }

        index = 0;
        while (index < schema.GlobalElements.Count)
        {
            schema.GlobalElements[index].NamespaceUri = schema.NamespaceUri;
            index++;
        }
    }

    internal static void ApplyRepositoryFallback(
        SchemaArtifact schema,
        SchemaRegistryOptions options,
        SchemaCompilationResult result)
    {
        if (!options.AllowRepositoryFallback || schema.RepositoryFallbackDefinitions.Count == 0)
        {
            return;
        }

        bool applied = false;
        int index = 0;
        while (index < schema.RepositoryFallbackDefinitions.Count)
        {
            ExternalSchemaDefinition? fallback = schema.RepositoryFallbackDefinitions[index];
            if (fallback != null)
            {
                applied = ApplyOneRepositoryFallback(schema, fallback) || applied;
            }

            index++;
        }

        if (applied)
        {
            AddSchemaDiagnostic(
                result,
                options,
                "schema.repository_fallback_applied",
                "Repository metadata filled semantic details absent from the XSD.",
                new SourceLocation());
        }
    }

    internal static bool ApplyOneRepositoryFallback(
        SchemaArtifact schema,
        ExternalSchemaDefinition fallback)
    {
        bool applied = false;
        if (schema.NamespaceUri.Length == 0 && fallback.NamespaceUris != null &&
            fallback.NamespaceUris.Count == 1 && fallback.NamespaceUris[0] != null &&
            fallback.NamespaceUris[0].Length > 0)
        {
            schema.NamespaceUri = fallback.NamespaceUris[0];
            applied = true;
        }

        if (fallback.Name == null || fallback.Name.Length == 0)
        {
            return applied;
        }

        DataTypeDefinition? matching = FindSchemaDataType(schema, fallback.Name);
        if (matching != null)
        {
            if (matching.Definition.Length == 0 && fallback.Definition != null &&
                fallback.Definition.Length > 0)
            {
                matching.Definition = fallback.Definition;
                applied = true;
            }

            return applied;
        }

        DataTypeDefinition dataType = new DataTypeDefinition();
        dataType.Identifier = fallback.Identifier;
        dataType.Kind = DataTypeKind.ExternalSchema;
        dataType.SourceTypeName = fallback.SourceTypeName;
        dataType.Name = fallback.Name;
        dataType.Definition = fallback.Definition;
        schema.DataTypes.Add(dataType);
        QualifiedNameData name = new QualifiedNameData();
        name.NamespaceUri = schema.NamespaceUri;
        name.LocalName = fallback.Name;
        schema.GlobalTypes.Add(name);
        return true;
    }

    internal static DataTypeDefinition? FindSchemaDataType(
        SchemaArtifact schema,
        string name)
    {
        int index = 0;
        while (index < schema.DataTypes.Count)
        {
            if (schema.DataTypes[index].Name == name)
            {
                return schema.DataTypes[index];
            }

            index++;
        }

        return null;
    }

    internal static void AddSchemaDiagnostic(
        SchemaCompilationResult result,
        SchemaRegistryOptions options,
        string code,
        string message,
        SourceLocation location)
    {
        if (result.DiagnosticCodes.Count >= options.Limits.MaxDiagnosticCount)
        {
            return;
        }

        result.DiagnosticCodes.Add(code);
        result.DiagnosticMessages.Add(message);
        result.DiagnosticLocations.Add(location);
        result.DiagnosticPaths.Add(new XmlPathData());
        result.DiagnosticSchemaUris.Add(string.Empty);
    }

    internal static SchemaCompilationResult CreateSchemaFailure(
        string errorCode,
        string errorMessage)
    {
        SchemaCompilationResult result = new SchemaCompilationResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
