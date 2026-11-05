using System;
using System.Collections.Generic;
using System.IO;

namespace iso20022;

/// <summary>Registers, selects, and compiles bounded local schemas by exact registry keys.</summary>
public class SchemaRegistry : ISchemaRegistry
{
    internal Dictionary<string, SchemaArtifact> Schemas =
        new Dictionary<string, SchemaArtifact>();
    internal SchemaCompiler Compiler = new SchemaCompiler();

    /// <summary>Initializes an empty local schema registry and compilation cache.</summary>
    public SchemaRegistry()
    {
    }

    /// <summary>Imports and registers one raw XSD stream through the ordinary registry path.</summary>
    /// <param name="input">The raw XSD stream.</param><param name="schema">The exact identity and artifact provenance.</param><param name="options">The import, registry, resolution, compilation, and cache policy.</param><returns>The import and registration result.</returns>
    public SchemaCompilationResult ImportRegisterSchemaStream(
        Stream? input,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        SchemaCompilationResult imported = new SchemaImporter().ImportSchemaStream(
            input,
            schema,
            options);
        return RegisterImportedSchema(imported, options);
    }

    /// <summary>Imports and registers one local XSD file through the ordinary registry path.</summary>
    /// <param name="path">The local XSD file path.</param><param name="schema">The exact identity and artifact provenance.</param><param name="options">The import, registry, resolution, compilation, and cache policy.</param><returns>The import and registration result.</returns>
    public SchemaCompilationResult ImportRegisterSchemaFile(
        string? path,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        SchemaCompilationResult imported = new SchemaImporter().ImportSchemaFile(
            path,
            schema,
            options);
        return RegisterImportedSchema(imported, options);
    }

    /// <summary>Imports and registers one explicitly named XSD entry from a bounded local ZIP stream.</summary>
    /// <param name="input">The ZIP artifact stream.</param><param name="schema">The exact identity, selected entry name, and artifact provenance.</param><param name="options">The import, registry, resolution, compilation, and cache policy.</param><returns>The import and registration result.</returns>
    public SchemaCompilationResult ImportRegisterSchemaArchive(
        Stream? input,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        if (input == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.archive.input_missing",
                "A local ZIP artifact stream is required.");
        }

        if (schema == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.archive.schema_missing",
                "Schema identity and an explicit XSD entry name are required.");
        }

        if (options == null || options.Limits == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.archive.options_missing",
                "Schema archive import options and resource limits are required.");
        }

        if (schema.SourceName == null || schema.SourceName.Length == 0)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.archive.entry_missing",
                "An explicit XSD archive entry name is required.");
        }

        RepositoryArtifactOpenOptions openOptions = CreateArchiveOptions(
            schema,
            options);
        RepositoryArtifactOpenResult opened =
            RepositoryArtifactOpener.OpenArtifact(input, openOptions);
        if (!opened.Success || opened.Content == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                opened.ErrorCode,
                opened.ErrorMessage);
        }

        SchemaArtifact entrySchema = SchemaImporter.CloneSchemaIdentity(schema);
        entrySchema.SourceName = opened.EntryName;
        SchemaCompilationResult imported;
        using (Stream content = opened.Content)
        {
            imported = new SchemaImporter().ImportSchemaStream(
                content,
                entrySchema,
                options);
        }

        return RegisterImportedSchema(imported, options);
    }

    /// <summary>Registers one schema and applies explicit collision, precedence, replacement, and cache policy.</summary>
    /// <param name="schema">The normalized schema artifact to register.</param><param name="options">The registry and compilation policy.</param><returns>The registration result and affected provenance.</returns>
    public SchemaCompilationResult RegisterSchema(
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        SchemaCompilationResult? validation = ValidateRegistration(schema, options);
        if (validation != null)
        {
            return validation;
        }

        SchemaArtifact candidate = schema ?? new SchemaArtifact();
        SchemaRegistryOptions policy = options ?? new SchemaRegistryOptions();
        policy.CancellationToken.ThrowIfCancellationRequested();
        string key = SchemaCompilationKeyBuilder.Build(candidate);
        lock (Schemas)
        {
            if (Schemas.ContainsKey(key))
            {
                SchemaArtifact existing = Schemas[key];
                if (object.ReferenceEquals(existing, candidate))
                {
                    return CreateSelectedResult(existing);
                }

                if (candidate.Precedence == existing.Precedence)
                {
                    return CreateSelectedFailure(
                        existing,
                        "schema.registry.precedence_collision",
                        "The exact schema key already has a different artifact at the same precedence.");
                }

                if (candidate.Precedence < existing.Precedence)
                {
                    return CreateSelectedFailure(
                        existing,
                        "schema.registry.precedence_rejected",
                        "A lower-precedence schema cannot replace the exact registered key.");
                }

                if (!policy.AllowReplacement)
                {
                    return CreateSelectedFailure(
                        existing,
                        "schema.registry.replacement_disabled",
                        "Replacing an exact schema key requires explicit replacement policy.");
                }

                Schemas[key] = candidate;
                bool invalidated = Compiler.Invalidate(candidate);
                return CompleteRegistration(candidate, policy, true, invalidated);
            }

            if (Schemas.Count >= policy.Limits.MaxCollectionCount ||
                Schemas.Count >= policy.Limits.MaxSchemaCount)
            {
                return SchemaImporter.CreateSchemaFailure(
                    "schema.registry.limit",
                    "The local schema registry exceeds the configured collection limit.");
            }

            Schemas.Add(key, candidate);
            return CompleteRegistration(candidate, policy, false, false);
        }
    }

    /// <summary>Looks up one exact namespace, message identifier, version, and profile key.</summary>
    /// <param name="selector">The exact registry key fields to select.</param><param name="options">The bounded lookup policy.</param><returns>The lookup result and selected provenance.</returns>
    public SchemaCompilationResult LookupSchema(
        SchemaArtifact? selector,
        SchemaRegistryOptions? options)
    {
        SchemaCompilationResult? validation = ValidateSelector(selector, options);
        if (validation != null)
        {
            return validation;
        }

        SchemaArtifact keySource = selector ?? new SchemaArtifact();
        SchemaRegistryOptions policy = options ?? new SchemaRegistryOptions();
        policy.CancellationToken.ThrowIfCancellationRequested();
        string key = SchemaCompilationKeyBuilder.Build(keySource);
        lock (Schemas)
        {
            if (!Schemas.ContainsKey(key))
            {
                SchemaCompilationResult missing = new SchemaCompilationResult();
                missing.Success = true;
                return missing;
            }

            return CreateSelectedResult(Schemas[key]);
        }
    }

    /// <summary>Compiles or reuses the bounded local closure for one exact registry key.</summary>
    /// <param name="selector">The exact registry key fields to compile.</param><param name="options">The local resolution, compilation, and cache policy.</param><returns>The compilation result, diagnostics, closure, and provenance.</returns>
    public SchemaCompilationResult CompileSchema(
        SchemaArtifact? selector,
        SchemaRegistryOptions? options)
    {
        SchemaCompilationResult? validation = ValidateSelector(selector, options);
        if (validation != null)
        {
            return validation;
        }

        SchemaArtifact keySource = selector ?? new SchemaArtifact();
        SchemaRegistryOptions policy = options ?? new SchemaRegistryOptions();
        policy.CancellationToken.ThrowIfCancellationRequested();
        string key = SchemaCompilationKeyBuilder.Build(keySource);
        lock (Schemas)
        {
            if (!Schemas.ContainsKey(key))
            {
                SchemaCompilationResult missing = new SchemaCompilationResult();
                missing.Success = true;
                return missing;
            }

            return CompileRegisteredSchema(Schemas[key], policy);
        }
    }

    internal SchemaCompilationResult CompleteRegistration(
        SchemaArtifact schema,
        SchemaRegistryOptions options,
        bool replaced,
        bool invalidated)
    {
        if (!options.CompileOnRegistration)
        {
            SchemaCompilationResult result = CreateSelectedResult(schema);
            result.Registered = true;
            result.ReplacedExistingSchema = replaced;
            result.InvalidatedCachedCompilation = invalidated;
            return result;
        }

        SchemaCompilationResult compiled = CompileRegisteredSchema(schema, options);
        compiled.Registered = true;
        compiled.ReplacedExistingSchema = replaced;
        compiled.InvalidatedCachedCompilation = invalidated;
        return compiled;
    }

    internal SchemaCompilationResult RegisterImportedSchema(
        SchemaCompilationResult imported,
        SchemaRegistryOptions? options)
    {
        if (!imported.Success || imported.RootSchema == null)
        {
            return imported;
        }

        SchemaCompilationResult registered = RegisterSchema(imported.RootSchema, options);
        if (registered.Success && registered.Registered &&
            options != null && !options.CompileOnRegistration)
        {
            registered.Schemas.Clear();
            int schemaIndex = 0;
            while (schemaIndex < imported.Schemas.Count)
            {
                registered.Schemas.Add(imported.Schemas[schemaIndex]);
                schemaIndex++;
            }

            int diagnosticIndex = 0;
            while (diagnosticIndex < imported.DiagnosticCodes.Count &&
                registered.DiagnosticCodes.Count <
                options.Limits.MaxDiagnosticCount)
            {
                registered.DiagnosticCodes.Add(
                    imported.DiagnosticCodes[diagnosticIndex]);
                registered.DiagnosticMessages.Add(
                    imported.DiagnosticMessages[diagnosticIndex]);
                registered.DiagnosticLocations.Add(
                    imported.DiagnosticLocations[diagnosticIndex]);
                registered.DiagnosticPaths.Add(
                    imported.DiagnosticPaths[diagnosticIndex]);
                registered.DiagnosticSchemaUris.Add(
                    imported.DiagnosticSchemaUris[diagnosticIndex]);
                diagnosticIndex++;
            }
        }

        return registered;
    }

    internal static RepositoryArtifactOpenOptions CreateArchiveOptions(
        SchemaArtifact schema,
        SchemaRegistryOptions options)
    {
        RepositoryArtifactOpenOptions openOptions =
            new RepositoryArtifactOpenOptions();
        openOptions.MaxArtifactBytes = options.Limits.MaxTotalBytes;
        openOptions.MaxArchiveEntryCount = options.Limits.MaxCollectionCount;
        openOptions.MaxArchiveEntryBytes = options.Limits.MaxTotalBytes;
        openOptions.MaxArchiveExpandedBytes = options.Limits.MaxTotalBytes;
        openOptions.EntryName = schema.SourceName;
        openOptions.Coordinate = schema.Artifact.Coordinate;
        openOptions.OriginUri = schema.Artifact.OriginUri;
        openOptions.RetrievedAt = schema.Artifact.RetrievedAt;
        openOptions.PublicationDate = schema.Artifact.PublicationDate;
        openOptions.LicenseNotice = schema.Artifact.LicenseNotice;
        openOptions.IsApproved = schema.Artifact.IsApproved;
        return openOptions;
    }

    internal SchemaCompilationResult CompileRegisteredSchema(
        SchemaArtifact selected,
        SchemaRegistryOptions options)
    {
        long availableCount = options.LocalSchemas.Count + (long)Schemas.Count - 1;
        if (availableCount > options.Limits.MaxCollectionCount)
        {
            return CreateSelectedFailure(
                selected,
                "schema.registry.local_schema_limit",
                "The available local schema candidates exceed the configured collection limit.");
        }

        if (options.RequireApprovedArtifacts && !selected.Artifact.IsApproved)
        {
            return CreateSelectedFailure(
                selected,
                "schema.registry.unapproved",
                "Compilation requires approved schema artifacts.");
        }

        SchemaRegistryOptions importOptions = CreateImportOptions(selected, options);
        SchemaImporter importer = new SchemaImporter();
        SchemaCompilationResult imported;
        SchemaArtifact request = SchemaImporter.CloneSchemaIdentity(selected);
        using (MemoryStream input = new MemoryStream(selected.Content, false))
        {
            imported = importer.ImportSchemaStream(input, request, importOptions);
        }

        if (!imported.Success)
        {
            imported.Found = true;
            imported.RootSchema = selected;
            if (imported.Schemas.Count == 0)
            {
                imported.Schemas.Add(selected);
            }

            return imported;
        }

        if (options.RequireApprovedArtifacts)
        {
            int approvalIndex = 0;
            while (approvalIndex < imported.Schemas.Count)
            {
                if (!imported.Schemas[approvalIndex].Artifact.IsApproved)
                {
                    imported.Success = false;
                    imported.Found = true;
                    imported.ErrorCode = "schema.registry.unapproved";
                    imported.ErrorMessage =
                        "Compilation requires an entirely approved schema closure.";
                    return imported;
                }

                approvalIndex++;
            }
        }

        return Compiler.CompileClosure(imported, importOptions);
    }

    internal SchemaRegistryOptions CreateImportOptions(
        SchemaArtifact selected,
        SchemaRegistryOptions source)
    {
        SchemaRegistryOptions options = new SchemaRegistryOptions();
        options.LocalRootPath = source.LocalRootPath;
        options.Limits = source.Limits;
        options.ResolveIncludes = source.ResolveIncludes;
        options.ResolveImports = source.ResolveImports;
        options.AllowRepositoryFallback = source.AllowRepositoryFallback;
        options.AllowReplacement = source.AllowReplacement;
        options.RequireApprovedArtifacts = source.RequireApprovedArtifacts;
        options.CompileOnRegistration = source.CompileOnRegistration;
        options.CacheCompiledSchemas = source.CacheCompiledSchemas;
        options.CancellationToken = source.CancellationToken;
        int index = 0;
        while (index < source.LocalSchemas.Count)
        {
            options.LocalSchemas.Add(source.LocalSchemas[index]);
            index++;
        }

        foreach (SchemaArtifact available in Schemas.Values)
        {
            if (!object.ReferenceEquals(available, selected))
            {
                options.LocalSchemas.Add(available);
            }
        }

        return options;
    }

    internal static SchemaCompilationResult? ValidateRegistration(
        SchemaArtifact? schema,
        SchemaRegistryOptions? options)
    {
        if (schema == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.schema_missing",
                "A normalized schema artifact is required for registration.");
        }

        if (options == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.options_missing",
                "Schema registry options are required.");
        }

        SchemaCompilationResult? requestValidation =
            SchemaImporter.ValidateSchemaRequest(schema, options);
        if (requestValidation != null)
        {
            return requestValidation;
        }

        SchemaCompilationResult? selectorValidation = ValidateKey(schema);
        if (selectorValidation != null)
        {
            return selectorValidation;
        }

        if (schema.Content.Length == 0 || schema.SourceName.Length == 0 ||
            schema.BaseUri.Length == 0 ||
            !UriLexical.IsAbsoluteUri(schema.BaseUri.AsSpan()))
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.schema_invalid",
                "A registered schema requires content, a source name, and an absolute base URI.");
        }

        if (schema.Artifact.OriginUri == null ||
            schema.Artifact.OriginUri.Length == 0 ||
            !UriLexical.IsAbsoluteUri(schema.Artifact.OriginUri.AsSpan()) ||
            schema.Artifact.Coordinate.Edition == null ||
            schema.Artifact.Coordinate.Identifier == null ||
            schema.Artifact.Coordinate.Version == null ||
            schema.Artifact.HashAlgorithm == null ||
            schema.Artifact.MediaType == null ||
            schema.Artifact.LicenseNotice == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.provenance_invalid",
                "A registered schema requires complete artifact provenance values.");
        }

        if (options.RequireApprovedArtifacts && !schema.Artifact.IsApproved)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.unapproved",
                "Registration requires an approved schema artifact.");
        }

        return null;
    }

    internal static SchemaCompilationResult? ValidateSelector(
        SchemaArtifact? selector,
        SchemaRegistryOptions? options)
    {
        if (selector == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.selector_missing",
                "An exact schema registry selector is required.");
        }

        if (options == null || options.Limits == null ||
            options.LocalSchemas == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.options_missing",
                "Schema registry options and resource limits are required.");
        }

        if (options.Limits.MaxSchemaCount <= 0 ||
            options.Limits.MaxCollectionCount <= 0 ||
            options.Limits.MaxTotalBytes <= 0 ||
            options.Limits.MaxDiagnosticCount <= 0)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.limits_invalid",
                "Schema registry resource limits must be positive.");
        }

        if (options.LocalSchemas.Count > options.Limits.MaxCollectionCount)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.local_schema_limit",
                "The caller-supplied local schema candidates exceed the configured collection limit.");
        }

        return ValidateKey(selector);
    }

    internal static SchemaCompilationResult? ValidateKey(SchemaArtifact schema)
    {
        if (schema.NamespaceUri == null || schema.Version == null ||
            schema.Profile == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.key_invalid",
                "Namespace, version, and profile registry key values cannot be missing.");
        }

        if (schema.NamespaceUri.Length > 0 &&
            !UriLexical.IsNamespaceUri(schema.NamespaceUri.AsSpan()))
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.key_invalid",
                "The exact schema namespace key is not a valid URI reference.");
        }

        if (schema.MessageIdentifier != null &&
            MessageIdentifierLexical.FormatMessageIdentifier(
                schema.MessageIdentifier) == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.registry.key_invalid",
                "The exact schema message identifier key is invalid.");
        }

        return null;
    }

    internal static SchemaCompilationResult CreateSelectedResult(SchemaArtifact schema)
    {
        SchemaCompilationResult result = new SchemaCompilationResult();
        result.Success = true;
        result.Found = true;
        result.RootSchema = schema;
        result.Schemas.Add(schema);
        return result;
    }

    internal static SchemaCompilationResult CreateSelectedFailure(
        SchemaArtifact schema,
        string errorCode,
        string errorMessage)
    {
        SchemaCompilationResult result = CreateSelectedResult(schema);
        result.Success = false;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        return result;
    }
}
