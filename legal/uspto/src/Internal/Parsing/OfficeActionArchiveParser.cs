namespace uspto;

internal class OfficeActionArchiveRecord
{
    internal long RecordNumber = 0L;
    internal long StartLineNumber = 0L;
    internal DocumentFormat Format = DocumentFormat.Unknown;
    internal string DatasetIdentifier = string.Empty;
    internal string Generation = string.Empty;
    internal string RawSource = string.Empty;
    internal string NormalizedJson = string.Empty;
    internal global::System.Collections.Generic.Dictionary<string, string> RawFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal OfficeActionTextRecord? ActionText = null;
    internal OfficeActionCitation? Citation = null;
    internal OfficeActionRejection? Rejection = null;
    internal OfficeActionEnrichedCitation? EnrichedCitation = null;
    internal PatentDataProvenance Provenance = new PatentDataProvenance();
}

internal class OfficeActionArchiveJsonSelection
{
    internal global::System.Text.Json.JsonElement Element = default(global::System.Text.Json.JsonElement);
    internal string Path = "$";
    internal string? DatasetMarker = null;
}

internal static class OfficeActionArchiveParser
{
    internal static OfficeActionArchiveRecord ParseJsonRecord(JsonRecord record, string? datasetHint, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("An Office Action archive JSON record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Office Action archive provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawJson))
        {
            throw new ValidationException("The Office Action archive JSON record must retain its raw source.", "RawJson", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(record.RawJson))
        {
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ArchiveFailure("An Office Action archive record must be a JSON object.", DocumentFormat.Json, CreateJsonPath(record), record.StartLineNumber);
            }

            OfficeActionArchiveJsonSelection selection = SelectJsonRecord(document.RootElement, CreateJsonPath(record));
            global::System.Collections.Generic.Dictionary<string, string> rawFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
            CaptureJsonFields(selection.Element, string.Empty, rawFields);
            string normalizedJson = NormalizeJsonObject(selection.Element);
            string? marker = selection.DatasetMarker;
            if (marker == null)
            {
                marker = ReadDatasetMarker(document.RootElement);
            }

            string dataset = ResolveDatasetIdentifier(datasetHint, marker, normalizedJson, DocumentFormat.Json, selection.Path, record.StartLineNumber);
            return ParseNormalizedRecord(normalizedJson, dataset, DocumentFormat.Json, record.RawJson, rawFields, record.RecordNumber, record.StartLineNumber, provenance);
        }
    }

    internal static OfficeActionArchiveRecord ParseDelimitedRow(DelimitedRow row, string? datasetHint, PatentDataProvenance provenance)
    {
        if (row == null)
        {
            throw new ValidationException("An Office Action archive delimited row is required.", "row", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Office Action archive provenance is required.", "provenance", "REQUIRED", null);
        }

        if (row.HasMissingColumns || row.HasExtraColumns || row.Headers.Length == 0)
        {
            throw ArchiveFailure("The Office Action archive row does not match its header schema.", DocumentFormat.Csv, "row[" + row.RowNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", row.StartLineNumber);
        }

        global::System.Collections.Generic.Dictionary<string, string> rawFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
        string? marker = null;
        int index;
        for (index = 0; index < row.Headers.Length; index += 1)
        {
            string value = string.Empty;
            if (index < row.Values.Length)
            {
                value = row.Values[index];
            }

            rawFields[row.Headers[index]] = value;
            if (IsDatasetMarkerName(row.Headers[index]) && !global::System.String.IsNullOrWhiteSpace(value))
            {
                marker = value.Trim();
            }
        }

        string normalizedJson = NormalizeDelimitedObject(row);
        string path = "row[" + row.RowNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
        string dataset = ResolveDatasetIdentifier(datasetHint, marker, normalizedJson, DocumentFormat.Csv, path, row.StartLineNumber);
        return ParseNormalizedRecord(normalizedJson, dataset, DocumentFormat.Csv, normalizedJson, rawFields, row.RowNumber, row.StartLineNumber, provenance);
    }

    internal static OfficeActionArchiveRecord ParseNormalizedRecord(string normalizedJson, string dataset, DocumentFormat format, string rawSource, global::System.Collections.Generic.Dictionary<string, string> rawFields, long recordNumber, long startLineNumber, PatentDataProvenance provenance)
    {
        PatentDataProvenance recordProvenance = OdpJsonParser.CopyProvenance(provenance);
        recordProvenance.Source = DataProvenance.Bulk;
        recordProvenance.DatasetIdentifier = dataset;
        recordProvenance.IsAuthoritative = true;

        OfficeActionArchiveRecord result = new OfficeActionArchiveRecord();
        result.RecordNumber = recordNumber;
        result.StartLineNumber = startLineNumber;
        result.Format = format;
        result.DatasetIdentifier = dataset;
        result.Generation = ResolveGeneration(dataset);
        result.RawSource = rawSource;
        result.NormalizedJson = normalizedJson;
        result.RawFields = rawFields;
        result.Provenance = recordProvenance;

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(normalizedJson))
        {
            if (dataset.Equals("oa_actions", global::System.StringComparison.Ordinal))
            {
                result.ActionText = OfficeActionTextParser.ParseRecord(document.RootElement, "$", recordProvenance);
            }
            else if (dataset.Equals("oa_citations", global::System.StringComparison.Ordinal))
            {
                result.Citation = OfficeActionCitationParser.ParseCitation(document.RootElement, "$", recordProvenance);
            }
            else if (dataset.Equals("oa_rejections", global::System.StringComparison.Ordinal))
            {
                result.Rejection = OfficeActionRejectionParser.ParseRejection(document.RootElement, "$", recordProvenance);
            }
            else if (dataset.Equals("enriched_cited_reference_metadata", global::System.StringComparison.Ordinal))
            {
                result.EnrichedCitation = OfficeActionEnrichedCitationParser.ParseEnrichedCitation(document.RootElement, "$", recordProvenance);
            }
            else
            {
                throw ArchiveFailure("The Office Action archive record generation is unsupported.", format, "$", startLineNumber);
            }
        }

        return result;
    }

    internal static OfficeActionArchiveJsonSelection SelectJsonRecord(global::System.Text.Json.JsonElement root, string path)
    {
        OfficeActionArchiveJsonSelection result = new OfficeActionArchiveJsonSelection();
        result.Element = root;
        result.Path = path;
        result.DatasetMarker = ReadDatasetMarker(root);
        foreach (global::System.Text.Json.JsonProperty property in root.EnumerateObject())
        {
            string normalized = NormalizeName(property.Name);
            bool wrapper = normalized.Equals("record", global::System.StringComparison.Ordinal)
                || normalized.Equals("doc", global::System.StringComparison.Ordinal)
                || normalized.Equals("document", global::System.StringComparison.Ordinal)
                || normalized.Equals("source", global::System.StringComparison.Ordinal)
                || normalized.Equals("fields", global::System.StringComparison.Ordinal);
            if (wrapper && property.Value.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                result.Element = property.Value;
                result.Path = path + "." + property.Name;
                string? nestedMarker = ReadDatasetMarker(property.Value);
                if (nestedMarker != null)
                {
                    result.DatasetMarker = nestedMarker;
                }

                return result;
            }
        }

        return result;
    }

    internal static string NormalizeJsonObject(global::System.Text.Json.JsonElement element)
    {
        global::System.Collections.Generic.Dictionary<string, string> canonicalNames = CreateCanonicalNames();
        global::System.Collections.Generic.Dictionary<string, bool> written = new global::System.Collections.Generic.Dictionary<string, bool>(global::System.StringComparer.Ordinal);
        using (global::System.IO.MemoryStream output = new global::System.IO.MemoryStream())
        {
            using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
            {
                writer.WriteStartObject();
                foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
                {
                    if (IsDatasetMarkerName(property.Name))
                    {
                        continue;
                    }

                    if (NormalizeName(property.Name).Equals("sections", global::System.StringComparison.Ordinal) && property.Value.ValueKind == global::System.Text.Json.JsonValueKind.Object)
                    {
                        WriteSectionProperties(writer, property.Value, canonicalNames, written);
                    }
                    else
                    {
                        string canonical = ResolveCanonicalName(property.Name, canonicalNames);
                        EnsureUniqueCanonicalName(canonical, written, DocumentFormat.Json, "$.", 0L);
                        writer.WritePropertyName(canonical);
                        property.Value.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
        }
    }

    internal static void WriteSectionProperties(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonElement sections, global::System.Collections.Generic.Dictionary<string, string> canonicalNames, global::System.Collections.Generic.Dictionary<string, bool> written)
    {
        foreach (global::System.Text.Json.JsonProperty section in sections.EnumerateObject())
        {
            string sourceName = "sections." + section.Name;
            string canonical = ResolveCanonicalName(sourceName, canonicalNames);
            EnsureUniqueCanonicalName(canonical, written, DocumentFormat.Json, "$.sections.", 0L);
            writer.WritePropertyName(canonical);
            section.Value.WriteTo(writer);
        }
    }

    internal static string NormalizeDelimitedObject(DelimitedRow row)
    {
        global::System.Collections.Generic.Dictionary<string, string> canonicalNames = CreateCanonicalNames();
        global::System.Collections.Generic.Dictionary<string, bool> written = new global::System.Collections.Generic.Dictionary<string, bool>(global::System.StringComparer.Ordinal);
        using (global::System.IO.MemoryStream output = new global::System.IO.MemoryStream())
        {
            using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
            {
                writer.WriteStartObject();
                int index;
                for (index = 0; index < row.Headers.Length; index += 1)
                {
                    if (IsDatasetMarkerName(row.Headers[index]))
                    {
                        continue;
                    }

                    string value = row.Values[index];
                    if (value.Length == 0)
                    {
                        continue;
                    }

                    string canonical = ResolveCanonicalName(row.Headers[index], canonicalNames);
                    EnsureUniqueCanonicalName(canonical, written, DocumentFormat.Csv, "row[" + row.RowNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "].", row.StartLineNumber);
                    writer.WritePropertyName(canonical);
                    WriteDelimitedValue(writer, canonical, value);
                }

                writer.WriteEndObject();
            }

            return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
        }
    }

    internal static void WriteDelimitedValue(global::System.Text.Json.Utf8JsonWriter writer, string canonicalName, string value)
    {
        if (IsBooleanField(canonicalName))
        {
            string normalized = value.Trim();
            if (normalized.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("YES", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("T", global::System.StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteBooleanValue(true);
                return;
            }

            if (normalized.Equals("N", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("NO", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("F", global::System.StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteBooleanValue(false);
                return;
            }
        }

        if (IsArrayField(canonicalName) && value.Length >= 2 && value[0] == '[' && value[value.Length - 1] == ']')
        {
            using (global::System.Text.Json.JsonDocument array = global::System.Text.Json.JsonDocument.Parse(value))
            {
                if (array.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Array)
                {
                    throw ArchiveFailure("The Office Action archive array field is invalid.", DocumentFormat.Csv, canonicalName, 0L);
                }

                array.RootElement.WriteTo(writer);
                return;
            }
        }

        writer.WriteStringValue(value);
    }

    internal static string ResolveDatasetIdentifier(string? hint, string? marker, string normalizedJson, DocumentFormat format, string path, long lineNumber)
    {
        string? dataset = NormalizeDatasetIdentifier(hint);
        if (dataset == null)
        {
            dataset = NormalizeDatasetIdentifier(marker);
        }

        if (dataset != null)
        {
            return dataset;
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(normalizedJson))
        {
            global::System.Text.Json.JsonElement element = document.RootElement;
            if (HasAnyField(element, new string[] { "citedDocumentIdentifier", "publicationNumber", "nplIndicator", "passageLocationText", "qualitySummaryText" }))
            {
                return "enriched_cited_reference_metadata";
            }

            if (HasAnyField(element, new string[] { "claimNumberArrayDocument", "hasRej101", "hasRej102", "hasRej103", "hasRej112", "cite103Max", "allowedClaimIndicator" }))
            {
                return "oa_rejections";
            }

            if (HasAnyField(element, new string[] { "referenceIdentifier", "parsedReferenceIdentifier", "officeActionCitationReferenceIndicator" }))
            {
                return "oa_citations";
            }

            if (HasAnyField(element, new string[] { "bodyText", "inventionTitle", "sections.summaryText", "sections.officeActionIdentifier", "applicationStatusNumber" }))
            {
                return "oa_actions";
            }
        }

        throw ArchiveFailure("The Office Action archive record generation cannot be determined.", format, path, lineNumber);
    }

    internal static string? NormalizeDatasetIdentifier(string? value)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = NormalizeName(value ?? string.Empty);
        if (normalized.Contains("enriched", global::System.StringComparison.Ordinal) && (normalized.Contains("citation", global::System.StringComparison.Ordinal) || normalized.Contains("reference", global::System.StringComparison.Ordinal)))
        {
            return "enriched_cited_reference_metadata";
        }

        if (normalized.Contains("rejection", global::System.StringComparison.Ordinal))
        {
            return "oa_rejections";
        }

        if (normalized.Contains("citation", global::System.StringComparison.Ordinal))
        {
            return "oa_citations";
        }

        if (normalized.Contains("oaactions", global::System.StringComparison.Ordinal) || normalized.Contains("actiontext", global::System.StringComparison.Ordinal) || normalized.Contains("officeaction", global::System.StringComparison.Ordinal))
        {
            return "oa_actions";
        }

        return null;
    }

    internal static string ResolveGeneration(string dataset)
    {
        if (dataset.Equals("oa_actions", global::System.StringComparison.Ordinal))
        {
            return "action-text";
        }

        if (dataset.Equals("oa_citations", global::System.StringComparison.Ordinal))
        {
            return "citation";
        }

        if (dataset.Equals("oa_rejections", global::System.StringComparison.Ordinal))
        {
            return "rejection";
        }

        return "enriched-citation";
    }

    internal static global::System.Collections.Generic.Dictionary<string, string> CreateCanonicalNames()
    {
        global::System.Collections.Generic.Dictionary<string, string> result = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
        AddCanonicalFields(result, OfficeActionTextParser.KnownRecordFields());
        AddCanonicalFields(result, OfficeActionCitationParser.KnownCitationFields());
        AddCanonicalFields(result, OfficeActionRejectionParser.KnownRejectionFields());
        AddCanonicalFields(result, OfficeActionEnrichedCitationParser.KnownEnrichedFields());
        result[NormalizeName("applicationNumber")] = "patentApplicationNumber";
        result[NormalizeName("applicationNumberText")] = "patentApplicationNumber";
        result[NormalizeName("officeActionBodyText")] = "bodyText";
        result[NormalizeName("section101Text")] = "sections.section101RejectionText";
        result[NormalizeName("section102Text")] = "sections.section102RejectionText";
        result[NormalizeName("section103Text")] = "sections.section103RejectionText";
        result[NormalizeName("section112Text")] = "sections.section112RejectionText";
        return result;
    }

    internal static void AddCanonicalFields(global::System.Collections.Generic.Dictionary<string, string> destination, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            destination[NormalizeName(names[index])] = names[index];
        }
    }

    internal static string ResolveCanonicalName(string sourceName, global::System.Collections.Generic.Dictionary<string, string> canonicalNames)
    {
        string normalized = NormalizeName(sourceName);
        if (canonicalNames.ContainsKey(normalized))
        {
            return canonicalNames[normalized];
        }

        return sourceName;
    }

    internal static void EnsureUniqueCanonicalName(string name, global::System.Collections.Generic.Dictionary<string, bool> written, DocumentFormat format, string pathPrefix, long lineNumber)
    {
        if (written.ContainsKey(name))
        {
            throw ArchiveFailure("The Office Action archive record contains duplicate aliases for one field.", format, pathPrefix + name, lineNumber);
        }

        written.Add(name, true);
    }

    internal static bool HasAnyField(global::System.Text.Json.JsonElement element, string[] names)
    {
        foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            int index;
            for (index = 0; index < names.Length; index += 1)
            {
                if (property.Name.Equals(names[index], global::System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool IsBooleanField(string name)
    {
        return name.EndsWith("Indicator", global::System.StringComparison.Ordinal)
            || name.Equals("hasRej101", global::System.StringComparison.Ordinal)
            || name.Equals("hasRej102", global::System.StringComparison.Ordinal)
            || name.Equals("hasRej103", global::System.StringComparison.Ordinal)
            || name.Equals("hasRej112", global::System.StringComparison.Ordinal)
            || name.Equals("hasRejDP", global::System.StringComparison.Ordinal);
    }

    internal static bool IsArrayField(string name)
    {
        return name.Equals("claimNumberArrayDocument", global::System.StringComparison.Ordinal)
            || name.Equals("passageLocationText", global::System.StringComparison.Ordinal)
            || name.Equals("patentApplicationNumber", global::System.StringComparison.Ordinal)
            || name.Equals("patentNumber", global::System.StringComparison.Ordinal)
            || name.Equals("bodyText", global::System.StringComparison.Ordinal)
            || name.StartsWith("sections.", global::System.StringComparison.Ordinal);
    }

    internal static string? ReadDatasetMarker(global::System.Text.Json.JsonElement element)
    {
        foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            if (IsDatasetMarkerName(property.Name) && property.Value.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    internal static bool IsDatasetMarkerName(string name)
    {
        string normalized = NormalizeName(name);
        return normalized.Equals("dataset", global::System.StringComparison.Ordinal)
            || normalized.Equals("datasetname", global::System.StringComparison.Ordinal)
            || normalized.Equals("recordtype", global::System.StringComparison.Ordinal)
            || normalized.Equals("generation", global::System.StringComparison.Ordinal);
    }

    internal static void CaptureJsonFields(global::System.Text.Json.JsonElement element, string prefix, global::System.Collections.Generic.Dictionary<string, string> destination)
    {
        foreach (global::System.Text.Json.JsonProperty property in element.EnumerateObject())
        {
            string name = prefix + property.Name;
            if (property.Value.ValueKind == global::System.Text.Json.JsonValueKind.Object)
            {
                CaptureJsonFields(property.Value, name + ".", destination);
            }
            else
            {
                destination[name] = property.Value.GetRawText();
            }
        }
    }

    internal static string NormalizeName(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (global::System.Char.IsLetterOrDigit(character))
            {
                result.Append(global::System.Char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }

    internal static string CreateJsonPath(JsonRecord record)
    {
        if (record.RecordNumber > 0L)
        {
            return "$[" + record.RecordNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
        }

        return "$";
    }

    internal static ParseException ArchiveFailure(string message, DocumentFormat format, string path, long lineNumber)
    {
        bool hasLine = lineNumber > 0L && lineNumber <= global::System.Int32.MaxValue;
        int boundedLine = 0;
        if (hasLine)
        {
            boundedLine = (int)lineNumber;
        }

        return new ParseException(message, format, "Office Action weekly archive", path, 0L, false, boundedLine, hasLine, null);
    }
}
