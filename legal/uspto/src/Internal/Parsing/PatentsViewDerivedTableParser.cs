namespace uspto;

internal class PatentsViewDerivedTableSchema
{
    internal DatasetFamily Family = DatasetFamily.Unknown;
    internal string TableIdentifier = string.Empty;
    internal string DetectionReason = string.Empty;
    internal bool DetectedFromHint = false;
    internal bool DetectedFromHeaders = false;
    internal string[] Headers = new string[0];
    internal string[] NormalizedHeaders = new string[0];
    internal string Fingerprint = string.Empty;
    internal int ColumnCount = 0;
}

internal class PatentsViewDerivedTableRecord
{
    internal PatentsViewDerivedTableSchema Schema = new PatentsViewDerivedTableSchema();
    internal global::System.Collections.Generic.Dictionary<string, string> Fields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal string[] ExtraValues = new string[0];
    internal long RowNumber = 0L;
    internal long StartLineNumber = 0L;
    internal long EndLineNumber = 0L;
    internal long StartOffset = 0L;
    internal bool HasMissingColumns = false;
    internal bool HasExtraColumns = false;
    internal PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    internal string? ReadValue(string columnName)
    {
        if (columnName == null)
        {
            throw new ValidationException("A PatentsView derived-table column name is required.", "columnName", "REQUIRED", null);
        }

        if (Fields.ContainsKey(columnName))
        {
            return Fields[columnName];
        }

        string normalized = PatentsViewGrantBulkParser.NormalizeName(columnName);
        int index;
        for (index = 0; index < Schema.NormalizedHeaders.Length; index += 1)
        {
            if (Schema.NormalizedHeaders[index].Equals(normalized, global::System.StringComparison.Ordinal))
            {
                return Fields[Schema.Headers[index]];
            }
        }

        return null;
    }
}

internal static class PatentsViewDerivedTableParser
{
    internal static PatentsViewDerivedTableRecord ParseRow(DelimitedRow row, string tableHint, DatasetFamily family, PatentDataProvenance provenance)
    {
        ValidateInputs(row, family, provenance);
        PatentsViewDerivedTableSchema schema = DiscoverSchema(row, tableHint, family);
        PatentsViewDerivedTableRecord result = new PatentsViewDerivedTableRecord();
        result.Schema = schema;
        result.RowNumber = row.RowNumber;
        result.StartLineNumber = row.StartLineNumber;
        result.EndLineNumber = row.EndLineNumber;
        result.StartOffset = row.StartOffset;
        result.HasMissingColumns = row.HasMissingColumns || row.Values.Length < row.Headers.Length;
        result.HasExtraColumns = row.HasExtraColumns || row.Values.Length > row.Headers.Length;
        int index;
        for (index = 0; index < schema.Headers.Length; index += 1)
        {
            string value = string.Empty;
            if (index < row.Values.Length)
            {
                value = row.Values[index];
            }

            result.Fields.Add(schema.Headers[index], value);
        }

        if (row.Values.Length > schema.Headers.Length)
        {
            result.ExtraValues = new string[row.Values.Length - schema.Headers.Length];
            global::System.Array.Copy(row.Values, schema.Headers.Length, result.ExtraValues, 0, result.ExtraValues.Length);
        }

        result.Provenance = PatentsViewGrantBulkParser.CreateProvenance(provenance, schema.TableIdentifier);
        return result;
    }

    internal static PatentsViewDerivedTableSchema DiscoverSchema(DelimitedRow row, string tableHint, DatasetFamily family)
    {
        ValidateFamily(family);
        return InspectSchema(row, tableHint, family);
    }

    internal static PatentsViewDerivedTableSchema InspectSchema(DelimitedRow row, string tableHint, DatasetFamily family)
    {
        if (row == null)
        {
            throw new ValidationException("A PatentsView derived-table row is required for schema discovery.", "row", "REQUIRED", null);
        }

        ValidateInspectableFamily(family);
        if (row.Headers == null || row.Headers.Length == 0)
        {
            throw new ValidationException("PatentsView derived-table headers are required.", "Headers", "REQUIRED", null);
        }

        PatentsViewDerivedTableSchema result = new PatentsViewDerivedTableSchema();
        result.Family = family;
        result.Headers = new string[row.Headers.Length];
        result.NormalizedHeaders = new string[row.Headers.Length];
        result.ColumnCount = row.Headers.Length;
        global::System.Collections.Generic.HashSet<string> seen = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Text.StringBuilder fingerprint = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < row.Headers.Length; index += 1)
        {
            string header = row.Headers[index].Trim();
            string normalized = PatentsViewGrantBulkParser.NormalizeName(header);
            if (normalized.Length == 0)
            {
                throw SchemaFailure("PatentsView derived-table headers cannot be blank.", tableHint, "header[" + index + "]", row);
            }

            if (!seen.Add(normalized))
            {
                throw SchemaFailure("PatentsView derived-table headers must be unique after normalization.", tableHint, header, row);
            }

            result.Headers[index] = header;
            result.NormalizedHeaders[index] = normalized;
            if (index > 0)
            {
                fingerprint.Append('|');
            }

            fingerprint.Append(normalized);
        }

        result.Fingerprint = fingerprint.ToString();
        string? hinted = ReadTableIdentifier(tableHint);
        if (hinted != null)
        {
            result.TableIdentifier = hinted;
            result.DetectionReason = "table-hint";
            result.DetectedFromHint = true;
            return result;
        }

        string? discovered = DiscoverTableFromHeaders(result.NormalizedHeaders);
        if (discovered != null)
        {
            result.TableIdentifier = discovered;
            result.DetectionReason = "headers";
            result.DetectedFromHeaders = true;
            return result;
        }

        result.TableIdentifier = "table";
        result.DetectionReason = "generic";
        return result;
    }

    internal static string? ReadTableIdentifier(string tableHint)
    {
        if (global::System.String.IsNullOrWhiteSpace(tableHint))
        {
            return null;
        }

        string fileName = global::System.IO.Path.GetFileName(tableHint.Trim());
        string lower = fileName.ToLowerInvariant();
        if (lower.EndsWith(".gz", global::System.StringComparison.Ordinal))
        {
            fileName = fileName.Substring(0, fileName.Length - 3);
            lower = fileName.ToLowerInvariant();
        }
        else if (lower.EndsWith(".zip", global::System.StringComparison.Ordinal))
        {
            fileName = fileName.Substring(0, fileName.Length - 4);
            lower = fileName.ToLowerInvariant();
        }

        if (lower.EndsWith(".tsv", global::System.StringComparison.Ordinal) || lower.EndsWith(".csv", global::System.StringComparison.Ordinal) || lower.EndsWith(".txt", global::System.StringComparison.Ordinal))
        {
            fileName = fileName.Substring(0, fileName.Length - 4);
        }

        string normalized = PatentsViewGrantBulkParser.NormalizeName(fileName);
        if (normalized.Equals("pvannual", global::System.StringComparison.Ordinal) || normalized.Equals("pvannualized", global::System.StringComparison.Ordinal) || normalized.Equals("patentsviewannual", global::System.StringComparison.Ordinal) || normalized.Equals("patentsviewannualized", global::System.StringComparison.Ordinal) || normalized.Equals("patentsviewannualizedtables", global::System.StringComparison.Ordinal) || normalized.Equals("annual", global::System.StringComparison.Ordinal) || normalized.Equals("annualized", global::System.StringComparison.Ordinal) || normalized.Equals("pvsorted", global::System.StringComparison.Ordinal) || normalized.Equals("patentsviewsorted", global::System.StringComparison.Ordinal) || normalized.Equals("patentsviewsortedtables", global::System.StringComparison.Ordinal) || normalized.Equals("sorted", global::System.StringComparison.Ordinal))
        {
            return null;
        }

        return fileName;
    }

    internal static string? DiscoverTableFromHeaders(string[] headers)
    {
        if (ContainsHeader(headers, "sourceentityid") && ContainsHeader(headers, "persistententityid"))
        {
            return "entity_crosswalk";
        }

        if (ContainsHeader(headers, "documentnumber") && (ContainsHeader(headers, "publicationtitle") || ContainsHeader(headers, "publicationdate") || ContainsHeader(headers, "publicationyear") || ContainsHeader(headers, "publicationtype")))
        {
            return "publication";
        }

        if (ContainsHeader(headers, "patentid") && (ContainsHeader(headers, "patenttitle") || ContainsHeader(headers, "patentdate") || ContainsHeader(headers, "patentyear") || ContainsHeader(headers, "patenttype")))
        {
            return "patent";
        }

        if (ContainsHeader(headers, "citationpatentid"))
        {
            return "us_patent_citation";
        }

        if (ContainsHeader(headers, "citationdocumentnumber"))
        {
            return "us_application_citation";
        }

        if (ContainsHeader(headers, "citationcountry"))
        {
            return "foreign_citation";
        }

        if (ContainsHeader(headers, "referencetext"))
        {
            return "other_reference";
        }

        if (ContainsHeader(headers, "claimtext"))
        {
            return "claim";
        }

        if (ContainsHeader(headers, "summarytext"))
        {
            return "brief_summary";
        }

        if (ContainsHeader(headers, "descriptiontext"))
        {
            return "description";
        }

        if (ContainsHeader(headers, "drawdesctext") || ContainsHeader(headers, "drawingdescription"))
        {
            return "drawing_description";
        }

        if (ContainsHeader(headers, "inventorid"))
        {
            return "inventor";
        }

        if (ContainsHeader(headers, "assigneeid"))
        {
            return "assignee";
        }

        if (ContainsHeader(headers, "applicantid") || ContainsHeader(headers, "uspartynamefirst"))
        {
            return "applicant";
        }

        if (ContainsHeader(headers, "locationid"))
        {
            return "location";
        }

        if (ContainsHeader(headers, "cpcid"))
        {
            return "cpc";
        }

        if (ContainsHeader(headers, "ipcid"))
        {
            return "ipc";
        }

        if (ContainsHeader(headers, "uspcid"))
        {
            return "uspc";
        }

        if (ContainsHeader(headers, "wipoid"))
        {
            return "wipo";
        }

        if (ContainsHeader(headers, "patentid"))
        {
            return "patent_relation";
        }

        if (ContainsHeader(headers, "documentnumber"))
        {
            return "publication_relation";
        }

        return null;
    }

    internal static bool ContainsHeader(string[] headers, string expected)
    {
        int index;
        for (index = 0; index < headers.Length; index += 1)
        {
            if (headers[index].Equals(expected, global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidateInputs(DelimitedRow row, DatasetFamily family, PatentDataProvenance provenance)
    {
        if (row == null)
        {
            throw new ValidationException("A PatentsView derived-table row is required.", "row", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("PatentsView derived-table provenance is required.", "provenance", "REQUIRED", null);
        }

        if (row.Values == null)
        {
            throw new ValidationException("PatentsView derived-table values are required.", "Values", "REQUIRED", null);
        }

        ValidateFamily(family);
    }

    internal static void ValidateFamily(DatasetFamily family)
    {
        if (family != DatasetFamily.PatentsViewAnnualized && family != DatasetFamily.PatentsViewSorted)
        {
            throw new ValidationException("The derived-table family must be PatentsView annualized or sorted.", "family", "UNSUPPORTED_DATASET_FAMILY", null);
        }
    }

    internal static void ValidateInspectableFamily(DatasetFamily family)
    {
        if (family != DatasetFamily.Unknown && family != DatasetFamily.PatentsViewGrant && family != DatasetFamily.PatentsViewPreGrant && family != DatasetFamily.PatentsViewAnnualized && family != DatasetFamily.PatentsViewSorted)
        {
            throw new ValidationException("Schema inspection supports only PatentsView dataset families.", "family", "UNSUPPORTED_DATASET_FAMILY", null);
        }
    }

    internal static ParseException SchemaFailure(string message, string sourceName, string path, DelimitedRow row)
    {
        return new ParseException(message, DocumentFormat.Text, sourceName, path, row.StartOffset, true, (int)row.StartLineNumber, row.StartLineNumber > 0L, null);
    }
}
