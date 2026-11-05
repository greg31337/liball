using System;
using System.IO;

namespace iso20022;

internal static class SchemaDependencyResolver
{
    internal static void ResolveClosure(
        SchemaImporter importer,
        SchemaCompilationResult result,
        SchemaRegistryOptions options)
    {
        SchemaArtifact? root = result.RootSchema;
        if (root == null ||
            ((!options.ResolveIncludes || root.IncludeLocations.Count == 0) &&
            (!options.ResolveImports || root.ImportLocations.Count == 0)))
        {
            return;
        }

        SchemaResolutionState state = new SchemaResolutionState();
        state.Options = options;
        state.Result = result;
        SchemaLocationResolution rootPath = NormalizeRootPath(options.LocalRootPath);
        if (!rootPath.Success)
        {
            SetResolutionFailure(state, rootPath.ErrorCode, rootPath.ErrorMessage);
            return;
        }

        state.RootPath = rootPath.Path;
        if (!PrepareLocalSchemas(state))
        {
            return;
        }

        SchemaLocationResolution rootUri = NormalizeRootUri(root, state.RootPath);
        if (!rootUri.Success)
        {
            SetResolutionFailure(state, rootUri.ErrorCode, rootUri.ErrorMessage);
            return;
        }

        root.BaseUri = rootUri.NormalizedUri;
        state.ActiveUris.Add(rootUri.NormalizedUri);
        ResolveSchemaDependencies(importer, root, state);
        if (state.Success)
        {
            state.ActiveUris.RemoveAt(state.ActiveUris.Count - 1);
            state.VisitedUris.Add(rootUri.NormalizedUri);
        }
    }

    internal static bool PrepareLocalSchemas(SchemaResolutionState state)
    {
        int index = 0;
        while (index < state.Options.LocalSchemas.Count)
        {
            SchemaArtifact? schema = state.Options.LocalSchemas[index];
            if (schema == null || schema.BaseUri == null || schema.SourceName == null ||
                schema.NamespaceUri == null || schema.Content == null)
            {
                SetResolutionFailure(
                    state,
                    "schema.resolve.local_schema_invalid",
                    "A caller-supplied local schema has invalid identity or content.");
                return false;
            }

            SchemaLocationResolution normalized = NormalizeRootUri(schema, state.RootPath);
            if (!normalized.Success)
            {
                SetResolutionFailure(
                    state,
                    "schema.resolve.local_schema_invalid",
                    normalized.ErrorMessage);
                return false;
            }

            state.LocalSchemas.Add(schema);
            state.LocalSchemaUris.Add(normalized.NormalizedUri);
            index++;
        }

        return true;
    }

    internal static void ResolveSchemaDependencies(
        SchemaImporter importer,
        SchemaArtifact parent,
        SchemaResolutionState state)
    {
        if (state.Options.ResolveIncludes)
        {
            int includeIndex = 0;
            while (state.Success && includeIndex < parent.IncludeLocations.Count)
            {
                ResolveDependency(
                    importer,
                    parent,
                    parent.IncludeLocations[includeIndex],
                    parent.NamespaceUri,
                    true,
                    state);
                includeIndex++;
            }
        }

        if (state.Options.ResolveImports)
        {
            int importIndex = 0;
            while (state.Success && importIndex < parent.ImportLocations.Count)
            {
                string expectedNamespace = string.Empty;
                if (importIndex < parent.ImportNamespaceUris.Count)
                {
                    expectedNamespace = parent.ImportNamespaceUris[importIndex];
                }

                ResolveDependency(
                    importer,
                    parent,
                    parent.ImportLocations[importIndex],
                    expectedNamespace,
                    false,
                    state);
                importIndex++;
            }
        }
    }

    internal static void ResolveDependency(
        SchemaImporter importer,
        SchemaArtifact parent,
        string location,
        string expectedNamespace,
        bool include,
        SchemaResolutionState state)
    {
        state.Options.CancellationToken.ThrowIfCancellationRequested();
        if (include && location.Length == 0)
        {
            SetResolutionFailure(
                state,
                "schema.resolve.include_location_missing",
                "An XSD include declaration requires a local schema location.");
            return;
        }

        SchemaLocationResolution normalized = new SchemaLocationResolution();
        if (location.Length > 0)
        {
            normalized = NormalizeDependencyUri(parent, location, state.RootPath);
            if (!normalized.Success)
            {
                SetResolutionFailure(state, normalized.ErrorCode, normalized.ErrorMessage);
                return;
            }
        }

        SchemaCandidateSelection selected = SelectLocalCandidate(
            normalized.NormalizedUri,
            expectedNamespace,
            location.Length == 0,
            state);
        if (!selected.Success)
        {
            SetResolutionFailure(state, selected.ErrorCode, selected.ErrorMessage);
            return;
        }

        string dependencyUri = normalized.NormalizedUri;
        if (selected.Found)
        {
            dependencyUri = selected.NormalizedUri;
            if (selected.CandidateCount > 1)
            {
                SchemaImporter.AddSchemaDiagnostic(
                    state.Result,
                    state.Options,
                    "schema.resolve.precedence_selected",
                    "The highest-precedence local schema was selected from multiple candidates.",
                    new SourceLocation());
            }
        }

        if (dependencyUri.Length == 0)
        {
            SetResolutionFailure(
                state,
                "schema.resolve.import_not_found",
                "No local schema matches the namespace-only XSD import.");
            return;
        }

        if (ContainsText(state.ActiveUris, dependencyUri))
        {
            SetResolutionFailure(
                state,
                "schema.resolve.cycle",
                "The local XSD include or import closure contains a cycle.");
            return;
        }

        if (ContainsText(state.VisitedUris, dependencyUri))
        {
            return;
        }

        if (state.Result.Schemas.Count >= state.Options.Limits.MaxSchemaCount)
        {
            SetResolutionFailure(
                state,
                "schema.resolve.schema_limit",
                "The local XSD dependency closure exceeds the configured schema limit.");
            return;
        }

        state.ActiveUris.Add(dependencyUri);
        SchemaCompilationResult imported = ImportDependencySchema(
            importer,
            parent,
            selected.Schema,
            dependencyUri,
            expectedNamespace,
            state);
        if (!imported.Success || imported.RootSchema == null)
        {
            SetResolutionFailure(state, imported.ErrorCode, imported.ErrorMessage);
            return;
        }

        SchemaArtifact dependency = imported.RootSchema;
        if (expectedNamespace.Length > 0 && dependency.NamespaceUri != expectedNamespace)
        {
            SetResolutionFailure(
                state,
                "schema.resolve.namespace_mismatch",
                "A resolved local schema does not match its include or import namespace.");
            return;
        }

        AppendDependencyDiagnostics(imported, state);
        state.Result.Schemas.Add(dependency);
        ResolveSchemaDependencies(importer, dependency, state);
        if (state.Success)
        {
            state.ActiveUris.RemoveAt(state.ActiveUris.Count - 1);
            state.VisitedUris.Add(dependencyUri);
        }
    }

    internal static SchemaCompilationResult ImportDependencySchema(
        SchemaImporter importer,
        SchemaArtifact parent,
        SchemaArtifact? selected,
        string dependencyUri,
        string expectedNamespace,
        SchemaResolutionState state)
    {
        SchemaArtifact source;
        if (selected != null)
        {
            source = SchemaImporter.CloneSchemaIdentity(selected);
            source.BaseUri = dependencyUri;
            source.IsEntryPoint = false;
            using (MemoryStream input = new MemoryStream(selected.Content, false))
            {
                return importer.ImportSchemaDocument(input, source, state.Options);
            }
        }

        string path = new Uri(dependencyUri, UriKind.Absolute).LocalPath;
        if (!File.Exists(path))
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.resolve.file_not_found",
                "A referenced local XSD file does not exist.");
        }

        source = CreateDependencyIdentity(parent, path, dependencyUri, expectedNamespace);
        using (FileStream input = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return importer.ImportSchemaDocument(input, source, state.Options);
        }
    }

    internal static SchemaArtifact CreateDependencyIdentity(
        SchemaArtifact parent,
        string path,
        string uri,
        string expectedNamespace)
    {
        SchemaArtifact schema = new SchemaArtifact();
        schema.Artifact.Coordinate.Kind = ArtifactKind.Schema;
        schema.Artifact.Coordinate.Edition = parent.Artifact.Coordinate.Edition;
        schema.Artifact.Coordinate.Identifier = Path.GetFileNameWithoutExtension(path);
        schema.Artifact.Coordinate.Version = parent.Artifact.Coordinate.Version;
        schema.Artifact.OriginUri = uri;
        schema.Artifact.IsApproved = parent.Artifact.IsApproved;
        schema.SourceName = Path.GetFileName(path);
        schema.BaseUri = uri;
        schema.NamespaceUri = expectedNamespace;
        schema.Version = parent.Version;
        schema.Profile = parent.Profile;
        schema.Precedence = parent.Precedence;
        return schema;
    }

    internal static SchemaCandidateSelection SelectLocalCandidate(
        string normalizedUri,
        string expectedNamespace,
        bool namespaceOnly,
        SchemaResolutionState state)
    {
        SchemaCandidateSelection selection = new SchemaCandidateSelection();
        int highestPrecedence = int.MinValue;
        int highestCount = 0;
        int index = 0;
        while (index < state.LocalSchemas.Count)
        {
            SchemaArtifact candidate = state.LocalSchemas[index];
            bool matches = namespaceOnly
                ? candidate.NamespaceUri == expectedNamespace
                : state.LocalSchemaUris[index] == normalizedUri;
            if (matches)
            {
                selection.CandidateCount++;
                if (candidate.Precedence > highestPrecedence)
                {
                    selection.Schema = candidate;
                    selection.NormalizedUri = state.LocalSchemaUris[index];
                    highestPrecedence = candidate.Precedence;
                    highestCount = 1;
                }
                else if (candidate.Precedence == highestPrecedence &&
                    !object.ReferenceEquals(selection.Schema, candidate))
                {
                    highestCount++;
                }
            }

            index++;
        }

        if (highestCount > 1)
        {
            selection.ErrorCode = "schema.resolve.precedence_collision";
            selection.ErrorMessage =
                "Multiple local schemas have the same highest precedence for one dependency.";
            return selection;
        }

        selection.Success = true;
        selection.Found = selection.Schema != null;
        return selection;
    }

    internal static SchemaLocationResolution NormalizeRootPath(string rootPath)
    {
        SchemaLocationResolution result = new SchemaLocationResolution();
        if (rootPath.Length == 0 || !Path.IsPathFullyQualified(rootPath))
        {
            result.ErrorCode = "schema.resolve.root_invalid";
            result.ErrorMessage =
                "Local schema resolution requires a caller-selected absolute root path.";
            return result;
        }

        result.Success = true;
        result.Path = Path.GetFullPath(rootPath);
        return result;
    }

    internal static SchemaLocationResolution NormalizeRootUri(
        SchemaArtifact schema,
        string rootPath)
    {
        string value = schema.BaseUri;
        if (value.Length == 0)
        {
            value = schema.SourceName;
        }

        return NormalizeSchemaLocation(string.Empty, value, rootPath);
    }

    internal static SchemaLocationResolution NormalizeDependencyUri(
        SchemaArtifact parent,
        string location,
        string rootPath)
    {
        return NormalizeSchemaLocation(parent.BaseUri, location, rootPath);
    }

    internal static SchemaLocationResolution NormalizeSchemaLocation(
        string baseUri,
        string location,
        string rootPath)
    {
        SchemaLocationResolution result = new SchemaLocationResolution();
        if (location.Length == 0 || location.IndexOf('?') >= 0 || location.IndexOf('#') >= 0)
        {
            result.ErrorCode = "schema.resolve.location_invalid";
            result.ErrorMessage = "A schema location is empty or contains a query or fragment.";
            return result;
        }

        string path;
        if (UriLexical.IsAbsoluteUri(location.AsSpan()))
        {
            if (!location.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                result.ErrorCode = "schema.resolve.local_only";
                result.ErrorMessage = "Schema dependencies must resolve to local file URIs.";
                return result;
            }

            path = new Uri(location, UriKind.Absolute).LocalPath;
        }
        else if (Path.IsPathFullyQualified(location))
        {
            path = location;
        }
        else if (baseUri.Length > 0)
        {
            if (!baseUri.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                !UriLexical.IsAbsoluteUri(baseUri.AsSpan()))
            {
                result.ErrorCode = "schema.resolve.local_only";
                result.ErrorMessage = "A schema with dependencies must have a local file base URI.";
                return result;
            }

            Uri combined = new Uri(new Uri(baseUri, UriKind.Absolute), location);
            if (combined.Scheme != "file")
            {
                result.ErrorCode = "schema.resolve.local_only";
                result.ErrorMessage = "Schema dependencies must resolve to local files.";
                return result;
            }

            path = combined.LocalPath;
        }
        else
        {
            path = Path.Combine(rootPath, location);
        }

        string normalizedPath = Path.GetFullPath(path);
        if (!PathIsUnderRoot(normalizedPath, rootPath))
        {
            result.ErrorCode = "schema.resolve.traversal";
            result.ErrorMessage = "A schema dependency resolves outside the caller-selected root.";
            return result;
        }

        result.Success = true;
        result.Path = normalizedPath;
        result.NormalizedUri = new Uri(normalizedPath).AbsoluteUri;
        return result;
    }

    internal static bool PathIsUnderRoot(string path, string rootPath)
    {
        if (path == rootPath)
        {
            return true;
        }

        string prefix = rootPath;
        if (!prefix.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            prefix = prefix + Path.DirectorySeparatorChar;
        }

        return path.StartsWith(prefix, StringComparison.Ordinal);
    }

    internal static bool ContainsText(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static void AppendDependencyDiagnostics(
        SchemaCompilationResult imported,
        SchemaResolutionState state)
    {
        int index = 0;
        while (index < imported.DiagnosticCodes.Count &&
            state.Result.DiagnosticCodes.Count < state.Options.Limits.MaxDiagnosticCount)
        {
            state.Result.DiagnosticCodes.Add(imported.DiagnosticCodes[index]);
            state.Result.DiagnosticMessages.Add(imported.DiagnosticMessages[index]);
            state.Result.DiagnosticLocations.Add(imported.DiagnosticLocations[index]);
            state.Result.DiagnosticPaths.Add(imported.DiagnosticPaths[index]);
            state.Result.DiagnosticSchemaUris.Add(imported.DiagnosticSchemaUris[index]);
            index++;
        }
    }

    internal static void SetResolutionFailure(
        SchemaResolutionState state,
        string errorCode,
        string errorMessage)
    {
        state.Success = false;
        state.Result.Success = false;
        state.Result.Found = false;
        state.Result.ErrorCode = errorCode;
        state.Result.ErrorMessage = errorMessage;
    }
}
