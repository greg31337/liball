using System.Collections.Generic;

namespace iso20022;

internal class SchemaCompiler
{
    Dictionary<string, SchemaCompilationCacheEntry> cache =
        new Dictionary<string, SchemaCompilationCacheEntry>();

    internal SchemaCompiler()
    {
    }

    internal SchemaCompilationResult CompileClosure(
        SchemaCompilationResult? imported,
        SchemaRegistryOptions? options)
    {
        SchemaCompilationResult? validation = ValidateCompilation(imported, options);
        if (validation != null)
        {
            return validation;
        }

        SchemaCompilationResult source = imported ?? new SchemaCompilationResult();
        SchemaRegistryOptions policy = options ?? new SchemaRegistryOptions();
        SchemaCompilationResult result = CopyImportResult(source);
        if (!source.Success)
        {
            return result;
        }

        SchemaArtifact root = source.RootSchema ?? new SchemaArtifact();
        string key = SchemaCompilationKeyBuilder.Build(root);
        if (policy.CacheCompiledSchemas)
        {
            lock (cache)
            {
                if (cache.ContainsKey(key))
                {
                    result.CompiledSchemas = cache[key].CompiledSchemas;
                    result.ReusedCachedCompilation = true;
                    return result;
                }
            }
        }

        SchemaSetCompilation.Compile(result, policy);
        if (result.Success && result.CompiledSchemas != null &&
            policy.CacheCompiledSchemas)
        {
            lock (cache)
            {
                if (cache.ContainsKey(key))
                {
                    result.CompiledSchemas = cache[key].CompiledSchemas;
                    result.ReusedCachedCompilation = true;
                }
                else if (cache.Count < policy.Limits.MaxCollectionCount)
                {
                    SchemaCompilationCacheEntry entry = new SchemaCompilationCacheEntry();
                    entry.CompiledSchemas = result.CompiledSchemas;
                    int index = 0;
                    while (index < result.Schemas.Count)
                    {
                        entry.SchemaKeys.Add(
                            SchemaCompilationKeyBuilder.Build(result.Schemas[index]));
                        index++;
                    }

                    cache.Add(key, entry);
                }
            }
        }

        return result;
    }

    internal bool Invalidate(SchemaArtifact schema)
    {
        string key = SchemaCompilationKeyBuilder.Build(schema);
        List<string> removals = new List<string>();
        lock (cache)
        {
            foreach (string cachedKey in cache.Keys)
            {
                SchemaCompilationCacheEntry entry = cache[cachedKey];
                if (SchemaDependencyResolver.ContainsText(entry.SchemaKeys, key))
                {
                    removals.Add(cachedKey);
                }
            }

            int index = 0;
            while (index < removals.Count)
            {
                cache.Remove(removals[index]);
                index++;
            }
        }

        return removals.Count > 0;
    }

    internal static SchemaCompilationResult? ValidateCompilation(
        SchemaCompilationResult? imported,
        SchemaRegistryOptions? options)
    {
        if (imported == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.compile.request_missing",
                "A resolved schema import result is required for compilation.");
        }

        if (options == null || options.Limits == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.compile.options_missing",
                "Schema compilation options and resource limits are required.");
        }

        if (!imported.Success)
        {
            return null;
        }

        if (imported.RootSchema == null || imported.Schemas == null ||
            imported.Schemas.Count == 0)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.compile.closure_missing",
                "A successful schema import must contain a root and local closure.");
        }

        if (options.LocalRootPath == null || options.Limits.MaxSchemaCount <= 0 ||
            options.Limits.MaxTotalBytes <= 0 ||
            options.Limits.MaxDiagnosticCount <= 0 ||
            options.Limits.MaxCollectionCount <= 0)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.compile.limits_invalid",
                "Schema compilation limits and the local root must be valid.");
        }

        if (imported.Schemas.Count > options.Limits.MaxSchemaCount)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.compile.schema_limit",
                "The resolved XSD closure exceeds the configured schema limit.");
        }

        long totalBytes = 0;
        int index = 0;
        while (index < imported.Schemas.Count)
        {
            SchemaArtifact? schema = imported.Schemas[index];
            if (schema == null || schema.Content == null || schema.BaseUri == null ||
                schema.NamespaceUri == null || schema.Version == null ||
                schema.Profile == null)
            {
                return SchemaImporter.CreateSchemaFailure(
                    "schema.compile.closure_invalid",
                    "The resolved XSD closure contains an invalid schema artifact.");
            }

            totalBytes = totalBytes + schema.Content.LongLength;
            if (totalBytes > options.Limits.MaxTotalBytes)
            {
                return SchemaImporter.CreateSchemaFailure(
                    "schema.compile.byte_limit",
                    "The resolved XSD closure exceeds the configured total byte limit.");
            }

            index++;
        }

        return null;
    }

    internal static SchemaCompilationResult CopyImportResult(
        SchemaCompilationResult source)
    {
        SchemaCompilationResult result = new SchemaCompilationResult();
        result.Success = source.Success;
        result.Found = source.Found;
        result.RootSchema = source.RootSchema;
        result.Registered = source.Registered;
        result.ReplacedExistingSchema = source.ReplacedExistingSchema;
        result.InvalidatedCachedCompilation = source.InvalidatedCachedCompilation;
        result.ErrorCode = source.ErrorCode;
        result.ErrorMessage = source.ErrorMessage;
        int index = 0;
        while (index < source.Schemas.Count)
        {
            result.Schemas.Add(source.Schemas[index]);
            index++;
        }

        index = 0;
        while (index < source.DiagnosticCodes.Count)
        {
            result.DiagnosticCodes.Add(source.DiagnosticCodes[index]);
            result.DiagnosticMessages.Add(source.DiagnosticMessages[index]);
            result.DiagnosticLocations.Add(source.DiagnosticLocations[index]);
            result.DiagnosticPaths.Add(source.DiagnosticPaths[index]);
            result.DiagnosticSchemaUris.Add(source.DiagnosticSchemaUris[index]);
            index++;
        }

        return result;
    }
}
