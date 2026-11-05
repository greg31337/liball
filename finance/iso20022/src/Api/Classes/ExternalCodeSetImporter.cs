using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace iso20022;

/// <summary>Normalizes and cross-checks official external-code artifacts and catalogues.</summary>
public class ExternalCodeSetImporter : IExternalCodeSetImporter
{
    /// <summary>Initializes an external-code set importer.</summary>
    public ExternalCodeSetImporter()
    {
    }

    /// <summary>Imports one raw or archived official JSON external-code artifact.</summary>
    /// <param name="input">The source content stream.</param><param name="request">The import provenance and limits.</param><returns>The normalized import result.</returns>
    public ExternalCodeImportResult ImportJsonStream(
        Stream? input,
        ExternalCodeImportRequest? request)
    {
        if (input == null)
        {
            return CreateImportFailure(
                "external-code.input_missing",
                "An external-code input stream is required.",
                "JSON");
        }

        if (request == null)
        {
            return CreateImportFailure(
                "external-code.request_missing",
                "An external-code import request is required.",
                "JSON");
        }

        ExternalCodeImportResult? validation = ValidateJsonRequest(request);
        if (validation != null)
        {
            return validation;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        ExternalCodeArtifactOpenResult opened = ExternalCodeArtifactOpener.OpenJsonArtifact(
            input,
            request);
        if (!opened.Success)
        {
            ExternalCodeImportResult failure = CreateImportFailure(
                opened.ErrorCode,
                opened.ErrorMessage,
                "JSON");
            failure.Artifact = opened.Artifact;
            return failure;
        }

        if (opened.Content.LongLength > request.Limits.MaxTotalBytes)
        {
            ExternalCodeImportResult failure = CreateImportFailure(
                "external-code.json.size",
                "The expanded JSON content exceeds the configured total-byte limit.",
                "JSON");
            failure.Artifact = opened.Artifact;
            failure.SourceEntryName = opened.EntryName;
            return failure;
        }

        byte[] jsonBytes = RemoveUtf8Preamble(opened.Content);
        JsonDocumentOptions options = new JsonDocumentOptions();
        options.MaxDepth = request.Limits.MaxDepth;
        options.CommentHandling = JsonCommentHandling.Disallow;
        options.AllowTrailingCommas = false;
        ExternalCodeImportResult result = new ExternalCodeImportResult();
        result.Artifact = opened.Artifact;
        result.SourceEntryName = opened.EntryName;
        result.SourceFormat = "JSON";
        using (JsonDocument document = JsonDocument.Parse(jsonBytes, options))
        {
            ExternalCodeCatalog? catalog = ParseJsonCatalog(
                document.RootElement,
                request,
                opened,
                result);
            if (catalog == null)
            {
                return result;
            }

            result.Success = true;
            result.Catalog = catalog;
            return result;
        }
    }

    /// <summary>Imports one raw or archived official XSD external-code artifact.</summary>
    /// <param name="input">The source content stream.</param><param name="request">The import provenance and limits.</param><returns>The normalized import result.</returns>
    public ExternalCodeImportResult ImportXsdStream(
        Stream? input,
        ExternalCodeImportRequest? request)
    {
        if (input == null)
        {
            return CreateImportFailure(
                "external-code.input_missing",
                "An external-code input stream is required.",
                "XSD");
        }

        if (request == null)
        {
            return CreateImportFailure(
                "external-code.request_missing",
                "An external-code import request is required.",
                "XSD");
        }

        ExternalCodeImportResult? validation = ValidateXsdRequest(request);
        if (validation != null)
        {
            return validation;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        ExternalCodeArtifactOpenResult opened = ExternalCodeArtifactOpener.OpenXsdArtifact(
            input,
            request);
        if (!opened.Success)
        {
            ExternalCodeImportResult failure = CreateImportFailure(
                opened.ErrorCode,
                opened.ErrorMessage,
                "XSD");
            failure.Artifact = opened.Artifact;
            return failure;
        }

        if (opened.Content.LongLength > request.Limits.MaxTotalBytes)
        {
            ExternalCodeImportResult failure = CreateImportFailure(
                "external-code.xsd.size",
                "The expanded XSD content exceeds the configured total-byte limit.",
                "XSD");
            failure.Artifact = opened.Artifact;
            failure.SourceEntryName = opened.EntryName;
            return failure;
        }

        ExternalCodeImportResult result = new ExternalCodeImportResult();
        result.Artifact = opened.Artifact;
        result.SourceEntryName = opened.EntryName;
        result.SourceFormat = "XSD";
        ExternalCodeCatalog? catalog = ExternalCodeXsdReader.ReadCatalog(
            opened.Content,
            request,
            opened,
            result);
        if (catalog == null)
        {
            return result;
        }

        result.Success = true;
        result.Catalog = catalog;
        return result;
    }

    /// <summary>Imports one raw or archived official XLSX external-code artifact.</summary>
    /// <param name="input">The source content stream.</param><param name="request">The import provenance and limits.</param><returns>The normalized import result.</returns>
    public ExternalCodeImportResult ImportXlsxStream(
        Stream? input,
        ExternalCodeImportRequest? request)
    {
        if (input == null)
        {
            return CreateImportFailure(
                "external-code.input_missing",
                "An external-code input stream is required.",
                "XLSX");
        }

        if (request == null)
        {
            return CreateImportFailure(
                "external-code.request_missing",
                "An external-code import request is required.",
                "XLSX");
        }

        ExternalCodeImportResult? validation = ValidateXlsxRequest(request);
        if (validation != null)
        {
            return validation;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        ExternalCodeArtifactOpenResult opened = ExternalCodeArtifactOpener.OpenXlsxArtifact(
            input,
            request);
        if (!opened.Success)
        {
            ExternalCodeImportResult failure = CreateImportFailure(
                opened.ErrorCode,
                opened.ErrorMessage,
                "XLSX");
            failure.Artifact = opened.Artifact;
            return failure;
        }

        ExternalCodeImportResult result = new ExternalCodeImportResult();
        result.Artifact = opened.Artifact;
        result.SourceEntryName = opened.EntryName;
        result.SourceFormat = "XLSX";
        ExternalCodeCatalog? catalog = ExternalCodeXlsxReader.ReadWorkbookCatalog(
            opened.Content,
            request,
            opened,
            result);
        if (catalog == null)
        {
            return result;
        }

        result.Success = true;
        result.Catalog = catalog;
        return result;
    }

    /// <summary>Cross-checks normalized JSON, XSD, and XLSX catalogues without selecting a preferred format.</summary>
    /// <param name="request">The three normalized catalogues and comparison limits.</param><returns>The deterministic comparison result.</returns>
    public ExternalCodeCatalogComparisonResult CompareCatalogs(
        ExternalCodeCatalogComparisonRequest? request)
    {
        if (request == null)
        {
            return ExternalCodeCatalogComparer.CreateComparisonFailure(
                "external-code.compare.request_missing",
                "An external-code catalogue comparison request is required.");
        }

        return ExternalCodeCatalogComparer.CompareCatalogs(request);
    }

    internal static ExternalCodeCatalog? ParseJsonCatalog(
        JsonElement root,
        ExternalCodeImportRequest request,
        ExternalCodeArtifactOpenResult opened,
        ExternalCodeImportResult result)
    {
        if (root.ValueKind != JsonValueKind.Object || ObjectHasDuplicateNames(root))
        {
            SetImportError(
                result,
                "external-code.json.root",
                "The external-code JSON root must be an object with unique member names.");
            return null;
        }

        JsonElement schemaMember = FindJsonMember(root, "$schema");
        string? schema = ReadJsonString(schemaMember);
        if (schema == null || schema.Length == 0 || schema.Length > request.Limits.MaxTextLength)
        {
            SetImportError(
                result,
                "external-code.json.schema",
                "The external-code JSON must declare a bounded $schema string.");
            return null;
        }

        JsonElement rootTypeMember = FindJsonMember(root, "type");
        string? rootType = ReadJsonString(rootTypeMember);
        if (rootType != "object")
        {
            SetImportError(
                result,
                "external-code.json.root_type",
                "The external-code JSON schema root type must be object.");
            return null;
        }

        JsonElement definitions = FindJsonMember(root, "definitions");
        if (definitions.ValueKind != JsonValueKind.Object || ObjectHasDuplicateNames(definitions))
        {
            SetImportError(
                result,
                "external-code.json.definitions",
                "The external-code JSON must contain an object of unique definitions.");
            return null;
        }

        int definitionCount = CountObjectMembers(definitions);
        if (definitionCount > request.Limits.MaxSchemaCount ||
            definitionCount > request.Limits.MaxCollectionCount ||
            definitionCount > request.Limits.MaxElementCount)
        {
            SetImportError(
                result,
                "external-code.json.definition_limit",
                "The external-code JSON exceeds the configured definition-count limit.");
            return null;
        }

        string edition = ResolveEdition(request, opened.EntryName);
        if (edition.Length == 0)
        {
            SetImportError(
                result,
                "external-code.json.edition_missing",
                "An external-code edition must be supplied by the request, artifact coordinate, or official file name.");
            return null;
        }

        ExternalCodeCatalog catalog = new ExternalCodeCatalog();
        catalog.Artifact = opened.Artifact;
        catalog.Edition = edition;
        catalog.PublicationDate = opened.Artifact.PublicationDate;
        catalog.SchemaIdentifier = ReadSchemaIdentifier(root, schema);
        if (catalog.SchemaIdentifier.Length > request.Limits.MaxTextLength)
        {
            SetImportError(
                result,
                "external-code.json.schema_identifier",
                "The external-code JSON schema identifier exceeds the configured text limit.");
            return null;
        }

        catalog.SchemaVersion = ReadSchemaVersion(schema);
        catalog.SourceFormat = "JSON";
        int totalElements = definitionCount;
        List<string> identifiers = new List<string>();
        foreach (JsonProperty definition in definitions.EnumerateObject())
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            if (definition.Name.Length == 0 ||
                definition.Name.Length > request.Limits.MaxTextLength ||
                identifiers.Contains(definition.Name))
            {
                SetImportError(
                    result,
                    "external-code.json.definition_identifier",
                    "An external-code definition identifier is empty, duplicated, or too long.");
                return null;
            }

            identifiers.Add(definition.Name);
            ExternalCodeSetParseResult parsed = ParseJsonCodeSet(
                definition.Name,
                definition.Value,
                request,
                result);
            if (!parsed.Success)
            {
                return null;
            }

            if (totalElements > request.Limits.MaxElementCount - parsed.CodeCount)
            {
                SetImportError(
                    result,
                    "external-code.json.code_limit",
                    "The external-code JSON exceeds the configured total code-count limit.");
                return null;
            }

            totalElements = totalElements + parsed.CodeCount;
            catalog.CodeSets.Add(parsed.CodeSet);
        }

        AddDiagnostic(
            result,
            request,
            "external-code.json.lifecycle_unavailable",
            "The official JSON schema does not publish code names, registration status, or activation and deactivation dates; those normalized fields remain absent.");
        return catalog;
    }

    internal static ExternalCodeSetParseResult ParseJsonCodeSet(
        string identifier,
        JsonElement definition,
        ExternalCodeImportRequest request,
        ExternalCodeImportResult result)
    {
        ExternalCodeSetParseResult parsed = new ExternalCodeSetParseResult();
        if (definition.ValueKind != JsonValueKind.Object || ObjectHasDuplicateNames(definition))
        {
            SetImportError(
                result,
                "external-code.json.definition_shape",
                "Every external-code definition must be an object with unique member names.");
            return parsed;
        }

        string? type = ReadJsonString(FindJsonMember(definition, "type"));
        if (type != "string")
        {
            SetImportError(
                result,
                "external-code.json.definition_type",
                "Every external-code definition must have JSON Schema type string.");
            return parsed;
        }

        string? description = ReadJsonString(FindJsonMember(definition, "description"));
        if (description == null || description.Length > request.Limits.MaxTextLength)
        {
            SetImportError(
                result,
                "external-code.json.description",
                "Every external-code definition must have a bounded description string.");
            return parsed;
        }

        ExternalCodeIntegerFacetResult minimum = ReadIntegerFacet(
            definition,
            "minLength",
            result);
        ExternalCodeIntegerFacetResult maximum = ReadIntegerFacet(
            definition,
            "maxLength",
            result);
        if (!minimum.Success || !maximum.Success)
        {
            return parsed;
        }

        if (minimum.Present && maximum.Present && minimum.Value > maximum.Value)
        {
            SetImportError(
                result,
                "external-code.json.length_range",
                "An external-code minimum length exceeds its maximum length.");
            return parsed;
        }

        ExternalCodeSet codeSet = new ExternalCodeSet();
        codeSet.Identifier = identifier;
        codeSet.Name = identifier;
        codeSet.Definition = ExtractSetDefinition(description);
        codeSet.HasMinimumLength = minimum.Present;
        codeSet.MinimumLength = minimum.Value;
        codeSet.HasMaximumLength = maximum.Present;
        codeSet.MaximumLength = maximum.Value;
        JsonElement patternMember = FindJsonMember(definition, "pattern");
        if (patternMember.ValueKind != JsonValueKind.Undefined)
        {
            string? pattern = ReadJsonString(patternMember);
            if (pattern == null || pattern.Length > request.Limits.MaxTextLength)
            {
                SetImportError(
                    result,
                    "external-code.json.pattern",
                    "An external-code pattern must be a bounded string.");
                return parsed;
            }

            codeSet.Pattern = pattern;
        }

        JsonElement values = FindJsonMember(definition, "enum");
        if (values.ValueKind == JsonValueKind.Undefined)
        {
            AddDiagnostic(
                result,
                request,
                "external-code.json.values_unavailable",
                "The JSON schema does not publish values for " + identifier + ".");
            parsed.Success = true;
            parsed.CodeSet = codeSet;
            return parsed;
        }

        if (values.ValueKind != JsonValueKind.Array ||
            values.GetArrayLength() > request.Limits.MaxCollectionCount)
        {
            SetImportError(
                result,
                "external-code.json.values",
                "An external-code enum must be a bounded array.");
            return parsed;
        }

        List<string> seenValues = new List<string>();
        foreach (JsonElement item in values.EnumerateArray())
        {
            string? code = ReadJsonString(item);
            if (code == null || code.Length == 0 ||
                code.Length > request.Limits.MaxTextLength || seenValues.Contains(code))
            {
                SetImportError(
                    result,
                    "external-code.json.code",
                    "An external-code value is empty, duplicated, non-string, or too long.");
                return parsed;
            }

            seenValues.Add(code);
            ExternalCodeValue value = new ExternalCodeValue();
            value.Value = code;
            value.Definition = ExtractCodeDefinition(description, code);
            if (value.Definition.Length == 0)
            {
                AddDiagnostic(
                    result,
                    request,
                    "external-code.json.code_definition_unavailable",
                    "The JSON description does not define code " + code + " in " + identifier + ".");
            }

            codeSet.Values.Add(value);
        }

        parsed.Success = true;
        parsed.CodeSet = codeSet;
        parsed.CodeCount = codeSet.Values.Count;
        return parsed;
    }

    internal static ExternalCodeIntegerFacetResult ReadIntegerFacet(
        JsonElement definition,
        string name,
        ExternalCodeImportResult result)
    {
        ExternalCodeIntegerFacetResult facet = new ExternalCodeIntegerFacetResult();
        JsonElement member = FindJsonMember(definition, name);
        if (member.ValueKind == JsonValueKind.Undefined)
        {
            return facet;
        }

        if (member.ValueKind != JsonValueKind.Number)
        {
            facet.Success = false;
            SetImportError(
                result,
                "external-code.json.length_facet",
                "External-code length facets must be non-negative integers.");
            return facet;
        }

        IntegerLexicalResult integer = IntegerLexical.ParseInt32(member.GetRawText().AsSpan());
        if (!integer.Success || integer.Value < 0)
        {
            facet.Success = false;
            SetImportError(
                result,
                "external-code.json.length_facet",
                "External-code length facets must be non-negative integers.");
            return facet;
        }

        facet.Present = true;
        facet.Value = integer.Value;
        return facet;
    }

    internal static JsonElement FindJsonMember(JsonElement owner, string name)
    {
        if (owner.ValueKind != JsonValueKind.Object)
        {
            return default(JsonElement);
        }

        foreach (JsonProperty property in owner.EnumerateObject())
        {
            if (property.Name == name)
            {
                return property.Value;
            }
        }

        return default(JsonElement);
    }

    internal static bool ObjectHasDuplicateNames(JsonElement value)
    {
        List<string> names = new List<string>();
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (names.Contains(property.Name))
            {
                return true;
            }

            names.Add(property.Name);
        }

        return false;
    }

    internal static int CountObjectMembers(JsonElement value)
    {
        int count = 0;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            count++;
        }

        return count;
    }

    internal static string? ReadJsonString(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    internal static string ExtractSetDefinition(string description)
    {
        int bullet = description.IndexOf("\n*`", StringComparison.Ordinal);
        if (bullet < 0 && description.StartsWith("*`", StringComparison.Ordinal))
        {
            bullet = 0;
        }

        string definition = bullet < 0 ? description : description.Substring(0, bullet);
        return definition.Trim();
    }

    internal static string ExtractCodeDefinition(string description, string code)
    {
        string marker = "*`" + code + "`-";
        int start = description.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        start = start + marker.Length;
        int end = description.IndexOf("\n*`", start, StringComparison.Ordinal);
        if (end < 0)
        {
            end = description.Length;
        }

        return description.Substring(start, end - start).Trim();
    }

    internal static string ReadSchemaIdentifier(JsonElement root, string fallback)
    {
        JsonElement properties = FindJsonMember(root, "properties");
        JsonElement identifierProperty = FindJsonMember(properties, "$id");
        string? identifier = ReadJsonString(FindJsonMember(identifierProperty, "default"));
        if (identifier == null || identifier.Length == 0)
        {
            return fallback;
        }

        return identifier;
    }

    internal static string ReadSchemaVersion(string schema)
    {
        if (schema.IndexOf("draft-04", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "draft-04";
        }

        return schema;
    }

    internal static string ResolveEdition(
        ExternalCodeImportRequest request,
        string entryName)
    {
        if (request.Edition.Length > 0)
        {
            return request.Edition;
        }

        if (request.Artifact.Coordinate.Edition.Length > 0)
        {
            return request.Artifact.Coordinate.Edition;
        }

        string fileName = Path.GetFileName(entryName);
        if (fileName.Length >= 6 && fileName[0] >= '1' && fileName[0] <= '4' &&
            (fileName[1] == 'Q' || fileName[1] == 'q'))
        {
            IntegerLexicalResult year = IntegerLexical.ParseInt32(
                fileName.AsSpan().Slice(2, 4));
            if (year.Success)
            {
                return IntegerLexical.FormatInt32(year.Value) + "-Q" + fileName[0];
            }
        }

        return string.Empty;
    }

    internal static byte[] RemoveUtf8Preamble(byte[] bytes)
    {
        if (bytes.Length < 3 || bytes[0] != 239 || bytes[1] != 187 || bytes[2] != 191)
        {
            return bytes;
        }

        byte[] result = new byte[bytes.Length - 3];
        Array.Copy(bytes, 3, result, 0, result.Length);
        return result;
    }

    internal static ExternalCodeImportResult? ValidateJsonRequest(
        ExternalCodeImportRequest request)
    {
        if (request.SourceName == null || request.ArchiveEntryName == null ||
            request.Edition == null || request.Artifact == null ||
            request.Artifact.Coordinate == null || request.Artifact.Hash == null ||
            request.Artifact.Hash.Value == null || request.Limits == null)
        {
            return CreateImportFailure(
                "external-code.request_invalid",
                "The external-code request contains a missing required value.",
                "JSON");
        }

        if (request.MaxArtifactBytes <= 0 || request.MaxArchiveEntryBytes <= 0 ||
            request.MaxArchiveEntryCount <= 0 || request.MaxArchiveExpandedBytes <= 0 ||
            request.MaxArchiveCompressionRatio <= 0 ||
            request.MaxArchiveEntryBytes > request.MaxArchiveExpandedBytes ||
            request.Limits.MaxDepth <= 0 || request.Limits.MaxTextLength <= 0 ||
            request.Limits.MaxElementCount <= 0 || request.Limits.MaxCollectionCount <= 0 ||
            request.Limits.MaxTotalBytes <= 0 || request.Limits.MaxSchemaCount <= 0 ||
            request.Limits.MaxDiagnosticCount <= 0)
        {
            return CreateImportFailure(
                "external-code.limits_invalid",
                "All external-code import limits must be positive and internally consistent.",
                "JSON");
        }

        if (request.Artifact.Hash.Value.Length > 0 &&
            (request.Artifact.Hash.Value.Length != 32 ||
            !ArtifactHashAlgorithmIsSha256(request.Artifact.HashAlgorithm)))
        {
            return CreateImportFailure(
                "external-code.artifact.hash_invalid",
                "A supplied external-code artifact hash must be a 32-byte SHA-256 value.",
                "JSON");
        }

        return null;
    }

    internal static ExternalCodeImportResult? ValidateXsdRequest(
        ExternalCodeImportRequest request)
    {
        if (request.SourceName == null || request.ArchiveEntryName == null ||
            request.Edition == null || request.Artifact == null ||
            request.Artifact.Coordinate == null || request.Artifact.Hash == null ||
            request.Artifact.Hash.Value == null || request.Limits == null)
        {
            return CreateImportFailure(
                "external-code.request_invalid",
                "The external-code request contains a missing required value.",
                "XSD");
        }

        if (request.MaxArtifactBytes <= 0 || request.MaxArchiveEntryBytes <= 0 ||
            request.MaxArchiveEntryCount <= 0 || request.MaxArchiveExpandedBytes <= 0 ||
            request.MaxArchiveCompressionRatio <= 0 ||
            request.MaxArchiveEntryBytes > request.MaxArchiveExpandedBytes ||
            request.Limits.MaxDepth <= 0 || request.Limits.MaxAttributesPerElement <= 0 ||
            request.Limits.MaxTextLength <= 0 || request.Limits.MaxElementCount <= 0 ||
            request.Limits.MaxCollectionCount <= 0 || request.Limits.MaxTotalBytes <= 0 ||
            request.Limits.MaxSchemaCount <= 0 || request.Limits.MaxDiagnosticCount <= 0)
        {
            return CreateImportFailure(
                "external-code.limits_invalid",
                "All external-code import limits must be positive and internally consistent.",
                "XSD");
        }

        if (request.Artifact.Hash.Value.Length > 0 &&
            (request.Artifact.Hash.Value.Length != 32 ||
            !ArtifactHashAlgorithmIsSha256(request.Artifact.HashAlgorithm)))
        {
            return CreateImportFailure(
                "external-code.artifact.hash_invalid",
                "A supplied external-code artifact hash must be a 32-byte SHA-256 value.",
                "XSD");
        }

        return null;
    }

    internal static ExternalCodeImportResult? ValidateXlsxRequest(
        ExternalCodeImportRequest request)
    {
        if (request.SourceName == null || request.ArchiveEntryName == null ||
            request.Edition == null || request.Artifact == null ||
            request.Artifact.Coordinate == null || request.Artifact.Hash == null ||
            request.Artifact.Hash.Value == null || request.Limits == null)
        {
            return CreateImportFailure(
                "external-code.request_invalid",
                "The external-code request contains a missing required value.",
                "XLSX");
        }

        if (request.MaxArtifactBytes <= 0 || request.MaxArchiveEntryBytes <= 0 ||
            request.MaxArchiveEntryCount <= 0 || request.MaxArchiveExpandedBytes <= 0 ||
            request.MaxArchiveCompressionRatio <= 0 ||
            request.MaxArchiveEntryBytes > request.MaxArchiveExpandedBytes ||
            request.Limits.MaxDepth <= 0 || request.Limits.MaxAttributesPerElement <= 0 ||
            request.Limits.MaxTextLength <= 0 || request.Limits.MaxElementCount <= 0 ||
            request.Limits.MaxCollectionCount <= 0 || request.Limits.MaxTotalBytes <= 0 ||
            request.Limits.MaxSchemaCount <= 0 || request.Limits.MaxDiagnosticCount <= 0)
        {
            return CreateImportFailure(
                "external-code.limits_invalid",
                "All external-code import limits must be positive and internally consistent.",
                "XLSX");
        }

        if (request.Artifact.Hash.Value.Length > 0 &&
            (request.Artifact.Hash.Value.Length != 32 ||
            !ArtifactHashAlgorithmIsSha256(request.Artifact.HashAlgorithm)))
        {
            return CreateImportFailure(
                "external-code.artifact.hash_invalid",
                "A supplied external-code artifact hash must be a 32-byte SHA-256 value.",
                "XLSX");
        }

        return null;
    }

    internal static bool ArtifactHashAlgorithmIsSha256(string? value)
    {
        return value != null &&
            (string.Equals(value, "SHA-256", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "SHA256", StringComparison.OrdinalIgnoreCase));
    }

    internal static void AddDiagnostic(
        ExternalCodeImportResult result,
        ExternalCodeImportRequest request,
        string code,
        string message)
    {
        if (result.DiagnosticCodes.Count >= request.Limits.MaxDiagnosticCount)
        {
            return;
        }

        result.DiagnosticCodes.Add(code);
        result.DiagnosticMessages.Add(message);
    }

    internal static void SetImportError(
        ExternalCodeImportResult result,
        string errorCode,
        string errorMessage)
    {
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
    }

    internal static ExternalCodeImportResult CreateImportFailure(
        string errorCode,
        string errorMessage,
        string sourceFormat)
    {
        ExternalCodeImportResult result = new ExternalCodeImportResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.SourceFormat = sourceFormat;
        return result;
    }
}
