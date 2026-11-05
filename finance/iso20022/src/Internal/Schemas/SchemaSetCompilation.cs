using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace iso20022;

internal static class SchemaSetCompilation
{
    internal static void Compile(SchemaCompilationResult result, SchemaRegistryOptions options)
    {
        SchemaSetCompilationState state = new SchemaSetCompilationState();
        state.Result = result;
        state.Options = options;
        ParseSchemas(state);
        if (state.Success)
        {
            WireDependencies(state);
        }

        if (state.Success)
        {
            CompileSchemaSet(state);
        }

        if (!state.Success)
        {
            result.Success = false;
            result.Found = false;
            if (state.DiagnosticLimitExceeded)
            {
                result.ErrorCode = "schema.compile.diagnostic_limit";
                result.ErrorMessage =
                    "XSD compilation exceeded the configured diagnostic limit.";
            }
            else if (result.ErrorCode.Length == 0)
            {
                result.ErrorCode = "schema.compile.invalid";
                result.ErrorMessage = "The local XSD closure did not compile as XSD 1.0.";
            }
        }
    }

    internal static void ParseSchemas(SchemaSetCompilationState state)
    {
        int index = 0;
        while (state.Success && index < state.Result.Schemas.Count)
        {
            state.Options.CancellationToken.ThrowIfCancellationRequested();
            SchemaArtifact artifact = state.Result.Schemas[index];
            SchemaCompilationUnit unit = new SchemaCompilationUnit();
            unit.Artifact = artifact;
            XmlReaderSettings settings = CreateReaderSettings(artifact.Content.LongLength);
            using (MemoryStream input = new MemoryStream(artifact.Content, false))
            using (XmlReader reader = XmlReader.Create(input, settings, artifact.BaseUri))
            {
                unit.Schema = XmlSchema.Read(
                    reader,
                    (sender, arguments) =>
                    {
                        RecordDiagnostic(state, arguments);
                    });
            }

            if (unit.Schema == null)
            {
                state.Success = false;
            }
            else
            {
                state.Units.Add(unit);
            }

            index++;
        }
    }

    internal static XmlReaderSettings CreateReaderSettings(long maximumCharacters)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.CheckCharacters = true;
        settings.CloseInput = true;
        settings.ConformanceLevel = ConformanceLevel.Document;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = true;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = maximumCharacters;
        return settings;
    }

    internal static void WireDependencies(SchemaSetCompilationState state)
    {
        int unitIndex = 0;
        while (state.Success && unitIndex < state.Units.Count)
        {
            SchemaCompilationUnit unit = state.Units[unitIndex];
            XmlSchema schema = unit.Schema ?? new XmlSchema();
            int externalIndex = 0;
            while (state.Success && externalIndex < schema.Includes.Count)
            {
                XmlSchemaExternal external = (XmlSchemaExternal)schema.Includes[externalIndex];
                SchemaCompilationUnit? dependency = FindDependency(unit, external, state);
                if (dependency == null || dependency.Schema == null)
                {
                    state.Success = false;
                    state.Result.ErrorCode = "schema.compile.dependency_unresolved";
                    state.Result.ErrorMessage =
                        "An XSD include or import is absent from the resolved local closure.";
                }
                else
                {
                    external.Schema = dependency.Schema;
                }

                externalIndex++;
            }

            unitIndex++;
        }
    }

    internal static SchemaCompilationUnit? FindDependency(
        SchemaCompilationUnit parent,
        XmlSchemaExternal external,
        SchemaSetCompilationState state)
    {
        string location = external.SchemaLocation ?? string.Empty;
        if (location.Length > 0)
        {
            SchemaLocationResolution normalized =
                SchemaDependencyResolver.NormalizeDependencyUri(
                    parent.Artifact,
                    location,
                    state.Options.LocalRootPath);
            if (!normalized.Success)
            {
                return null;
            }

            int locationIndex = 0;
            while (locationIndex < state.Units.Count)
            {
                SchemaCompilationUnit candidate = state.Units[locationIndex];
                if (candidate.Artifact.BaseUri == normalized.NormalizedUri)
                {
                    return candidate;
                }

                locationIndex++;
            }

            return null;
        }

        XmlSchemaImport? schemaImport = external as XmlSchemaImport;
        if (schemaImport == null)
        {
            return null;
        }

        string expectedNamespace = schemaImport.Namespace ?? string.Empty;
        SchemaCompilationUnit? selected = null;
        int highestPrecedence = int.MinValue;
        int highestCount = 0;
        int index = 0;
        while (index < state.Units.Count)
        {
            SchemaCompilationUnit candidate = state.Units[index];
            if (candidate.Artifact.NamespaceUri == expectedNamespace &&
                !object.ReferenceEquals(candidate, parent))
            {
                if (candidate.Artifact.Precedence > highestPrecedence)
                {
                    selected = candidate;
                    highestPrecedence = candidate.Artifact.Precedence;
                    highestCount = 1;
                }
                else if (candidate.Artifact.Precedence == highestPrecedence)
                {
                    highestCount++;
                }
            }

            index++;
        }

        if (highestCount != 1)
        {
            return null;
        }

        return selected;
    }

    internal static void CompileSchemaSet(SchemaSetCompilationState state)
    {
        XmlSchemaSet schemas = new XmlSchemaSet();
        schemas.XmlResolver = null;
        schemas.ValidationEventHandler += (sender, arguments) =>
        {
            RecordDiagnostic(state, arguments);
        };
        XmlSchema? root = state.Units[0].Schema;
        if (root == null)
        {
            state.Success = false;
            return;
        }

        schemas.Add(root);
        if (state.Success)
        {
            schemas.Compile();
        }

        if (state.Success)
        {
            state.Result.CompiledSchemas = schemas;
            state.Result.Success = true;
            state.Result.Found = true;
        }
    }

    internal static void RecordDiagnostic(
        SchemaSetCompilationState state,
        ValidationEventArgs arguments)
    {
        if (state.Result.DiagnosticCodes.Count >=
            state.Options.Limits.MaxDiagnosticCount)
        {
            state.DiagnosticLimitExceeded = true;
            state.Success = false;
            return;
        }

        string code = "schema.compile.warning";
        if (arguments.Severity == XmlSeverityType.Error)
        {
            code = "schema.compile.error";
            state.Success = false;
        }

        SourceLocation location = new SourceLocation();
        location.SourceName = arguments.Exception.SourceUri;
        if (arguments.Exception.LineNumber > 0)
        {
            location.HasLineInformation = true;
            location.LineNumber = arguments.Exception.LineNumber;
            location.ColumnNumber = arguments.Exception.LinePosition;
        }

        state.Result.DiagnosticCodes.Add(code);
        state.Result.DiagnosticMessages.Add(arguments.Message);
        state.Result.DiagnosticLocations.Add(location);
        state.Result.DiagnosticPaths.Add(new XmlPathData());
        state.Result.DiagnosticSchemaUris.Add(
            arguments.Exception.SourceUri ?? string.Empty);
    }
}
