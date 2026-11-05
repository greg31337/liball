namespace uspto;

internal class PatentsViewLocalRowQueryRequest
{
    internal PatentsViewLocalTableInfo Table = new PatentsViewLocalTableInfo();
    internal SearchRequest Search = new SearchRequest();
    internal UsptoLimits Limits = new UsptoLimits();
    internal int MaximumScannedRows = UsptoDefaults.MaximumResults;
}

internal class PatentsViewLocalRowQueryResult
{
    internal PatentsViewDerivedTableRecord[] Rows = new PatentsViewDerivedTableRecord[0];
    internal SearchResponseMetadata Metadata = new SearchResponseMetadata();
    internal int ScannedRowCount = 0;
    internal int MatchedRowCount = 0;
    internal long NextOffset = 0L;
    internal bool HasNextOffset = false;
    internal bool ReachedScanLimit = false;
    internal bool ReachedResultLimit = false;
    internal bool IsComplete = false;
}

internal static class PatentsViewLocalRowQuery
{
    internal static PatentsViewLocalRowQueryResult Execute(PatentsViewLocalRowQueryRequest request)
    {
        ValidateRequest(request);
        global::System.Threading.CancellationToken cancellationToken = request.Search.Context.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        using (global::System.IO.FileStream input = new global::System.IO.FileStream(request.Table.FilePath ?? string.Empty, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
        {
            return ScanStream(request, input, cancellationToken);
        }
    }

    internal static PatentsViewLocalRowQueryResult ScanStream(PatentsViewLocalRowQueryRequest request, global::System.IO.Stream input, global::System.Threading.CancellationToken cancellationToken)
    {
        if (input == null)
        {
            throw new ValidationException("A local table input stream is required.", "input", "REQUIRED", null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        global::System.Collections.Generic.List<PatentsViewDerivedTableRecord> matches = new global::System.Collections.Generic.List<PatentsViewDerivedTableRecord>();
        PatentsViewLocalRowQueryResult result = new PatentsViewLocalRowQueryResult();
        bool reachedEnd = false;
        DelimitedReader reader = new DelimitedReader(input, request.Table.Delimiter, true, request.Table.FilePath, request.Limits, cancellationToken);
        PatentsViewDerivedTableSchema? schema = null;
        while (result.ScannedRowCount < request.MaximumScannedRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DelimitedRow? row = reader.ReadRow();
            if (row == null)
            {
                reachedEnd = true;
                break;
            }

            result.ScannedRowCount += 1;
            if (schema == null)
            {
                schema = PatentsViewDerivedTableParser.InspectSchema(row, request.Table.TableIdentifier, request.Table.Family);
            }

            PatentsViewDerivedTableRecord record = CreateRecord(row, schema, request.Table);
            if (MatchesSearch(record, request.Search))
            {
                matches.Add(record);
            }
        }

        if (!reachedEnd && result.ScannedRowCount >= request.MaximumScannedRows && reader.Reader.Peek() < 0)
        {
            reachedEnd = true;
        }

        result.ReachedScanLimit = !reachedEnd && result.ScannedRowCount >= request.MaximumScannedRows;
        result.MatchedRowCount = matches.Count;
        cancellationToken.ThrowIfCancellationRequested();
        matches.Sort((left, right) => CompareRecords(left, right, request.Search.Sorts));
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Search.UseCursorPage)
        {
            ValidateUniqueCursorKeys(matches, request.Search.Sorts);
            ApplyCursorPage(result, matches, request);
        }
        else
        {
            ApplyOffsetPage(result, matches, request);
        }

        result.Metadata.Request = request.Search;
        result.Metadata.ReturnedCount = result.Rows.Length;
        result.Metadata.Provenance = DataProvenance.LocalMirror;
        if (result.Rows.Length > 0)
        {
            result.Metadata.FieldSources = ProvenanceLabeler.CopyFieldSources(result.Rows[0].Provenance.FieldSources);
        }

        if (reachedEnd)
        {
            result.Metadata.Total = matches.Count;
            result.Metadata.HasTotal = true;
        }

        result.IsComplete = reachedEnd && !HasMoreKnownMatches(result, matches.Count, request.Search.UseCursorPage);
        return result;
    }

    internal static bool MatchesSearch(PatentsViewDerivedTableRecord record, SearchRequest request)
    {
        int filterIndex;
        for (filterIndex = 0; filterIndex < request.Filters.Length; filterIndex += 1)
        {
            if (!PatentsViewLocalPredicateEvaluator.Matches(record, request.Filters[filterIndex]))
            {
                return false;
            }
        }

        int rangeIndex;
        for (rangeIndex = 0; rangeIndex < request.Ranges.Length; rangeIndex += 1)
        {
            if (!PatentsViewLocalPredicateEvaluator.MatchesRange(record, request.Ranges[rangeIndex]))
            {
                return false;
            }
        }

        return true;
    }

    internal static PatentsViewDerivedTableRecord CreateRecord(DelimitedRow row, PatentsViewDerivedTableSchema schema, PatentsViewLocalTableInfo table)
    {
        PatentsViewDerivedTableRecord result = new PatentsViewDerivedTableRecord();
        result.Schema = schema;
        result.RowNumber = row.RowNumber;
        result.StartLineNumber = row.StartLineNumber;
        result.EndLineNumber = row.EndLineNumber;
        result.StartOffset = row.StartOffset;
        result.HasMissingColumns = row.HasMissingColumns || row.Values.Length < schema.Headers.Length;
        result.HasExtraColumns = row.HasExtraColumns || row.Values.Length > schema.Headers.Length;
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

        result.Provenance = CopyProvenance(table.Provenance, table.TableIdentifier);
        for (index = 0; index < schema.Headers.Length; index += 1)
        {
            ProvenanceLabeler.AddLocalPatentField(result.Provenance, schema.Headers[index], table.Provenance);
        }

        return result;
    }

    internal static PatentDataProvenance CopyProvenance(PatentDataProvenance source, string tableIdentifier)
    {
        PatentDataProvenance result = ProvenanceLabeler.CopyPatent(source);
        result.Source = DataProvenance.LocalMirror;
        result.DatasetIdentifier = tableIdentifier;
        result.FromCache = false;
        result.IsAuthoritative = false;
        return result;
    }

    internal static int CompareRecords(PatentsViewDerivedTableRecord left, PatentsViewDerivedTableRecord right, SearchSort[] sorts)
    {
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            string? leftValue = left.ReadValue(sorts[index].Field ?? string.Empty);
            string? rightValue = right.ReadValue(sorts[index].Field ?? string.Empty);
            int comparison = CompareFieldValues(leftValue, rightValue);
            if (sorts[index].Direction == SortDirection.Descending)
            {
                comparison = -comparison;
            }

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return left.RowNumber.CompareTo(right.RowNumber);
    }

    internal static int CompareFieldValues(string? left, string? right)
    {
        bool leftMissing = PatentsViewLocalPredicateEvaluator.IsMissing(left);
        bool rightMissing = PatentsViewLocalPredicateEvaluator.IsMissing(right);
        if (leftMissing && rightMissing)
        {
            return 0;
        }

        if (leftMissing)
        {
            return -1;
        }

        if (rightMissing)
        {
            return 1;
        }

        return PatentsViewLocalPredicateEvaluator.CompareValues(left ?? string.Empty, right ?? string.Empty);
    }

    internal static int CompareRecordToCursor(PatentsViewDerivedTableRecord record, string[] afterValues, SearchSort[] sorts)
    {
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            string? value = record.ReadValue(sorts[index].Field ?? string.Empty);
            int comparison = CompareFieldValues(value, afterValues[index]);
            if (sorts[index].Direction == SortDirection.Descending)
            {
                comparison = -comparison;
            }

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    internal static void ApplyOffsetPage(PatentsViewLocalRowQueryResult result, global::System.Collections.Generic.List<PatentsViewDerivedTableRecord> matches, PatentsViewLocalRowQueryRequest request)
    {
        long requestedOffset = request.Search.OffsetPage.Offset;
        int start = matches.Count;
        if (requestedOffset < matches.Count)
        {
            start = (int)requestedOffset;
        }

        int pageLimit = EffectivePageLimit(request.Search.OffsetPage.Limit, request.Search.MaximumResults, request.Limits.MaximumResults);
        int available = matches.Count - start;
        int count = global::System.Math.Min(pageLimit, available);
        result.Rows = ProjectRows(matches, start, count, request.Search.FieldSelection.Fields);
        result.Metadata.OffsetPage.Offset = requestedOffset;
        result.Metadata.OffsetPage.Limit = request.Search.OffsetPage.Limit;
        result.Metadata.HasOffsetPage = true;
        result.NextOffset = requestedOffset + count;
        result.HasNextOffset = count > 0 && start + count < matches.Count;
        result.ReachedResultLimit = count == pageLimit && pageLimit < request.Search.OffsetPage.Limit && start + count < matches.Count;
    }

    internal static void ApplyCursorPage(PatentsViewLocalRowQueryResult result, global::System.Collections.Generic.List<PatentsViewDerivedTableRecord> matches, PatentsViewLocalRowQueryRequest request)
    {
        int start = 0;
        if (request.Search.CursorPage.AfterValues.Length != 0)
        {
            start = matches.Count;
            int index;
            for (index = 0; index < matches.Count; index += 1)
            {
                if (CompareRecordToCursor(matches[index], request.Search.CursorPage.AfterValues, request.Search.Sorts) > 0)
                {
                    start = index;
                    break;
                }
            }
        }

        int pageLimit = EffectivePageLimit(request.Search.CursorPage.Size, request.Search.MaximumResults, request.Limits.MaximumResults);
        int available = matches.Count - start;
        int count = global::System.Math.Min(pageLimit, available);
        result.Rows = ProjectRows(matches, start, count, request.Search.FieldSelection.Fields);
        bool hasNext = count > 0 && start + count < matches.Count;
        if (hasNext)
        {
            PatentsViewDerivedTableRecord last = matches[start + count - 1];
            result.Metadata.NextCursorPage.AfterValues = ReadSortValues(last, request.Search.Sorts);
            result.Metadata.NextCursorPage.Size = request.Search.CursorPage.Size;
            result.Metadata.HasNextCursorPage = true;
        }

        result.ReachedResultLimit = count == pageLimit && pageLimit < request.Search.CursorPage.Size && start + count < matches.Count;
    }

    internal static int EffectivePageLimit(int requested, int requestMaximum, int configuredMaximum)
    {
        int result = requested;
        if (requestMaximum < result)
        {
            result = requestMaximum;
        }

        if (configuredMaximum < result)
        {
            result = configuredMaximum;
        }

        return result;
    }

    internal static string[] ReadSortValues(PatentsViewDerivedTableRecord record, SearchSort[] sorts)
    {
        string[] result = new string[sorts.Length];
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            string? value = record.ReadValue(sorts[index].Field ?? string.Empty);
            if (PatentsViewLocalPredicateEvaluator.IsMissing(value))
            {
                throw new ValidationException("A local cursor sort field contains a missing value.", "Sorts.Field", "MISSING_CURSOR_VALUE", null);
            }

            result[index] = value ?? string.Empty;
        }

        return result;
    }

    internal static void ValidateUniqueCursorKeys(global::System.Collections.Generic.List<PatentsViewDerivedTableRecord> matches, SearchSort[] sorts)
    {
        int index;
        for (index = 1; index < matches.Count; index += 1)
        {
            if (CompareSortKeys(matches[index - 1], matches[index], sorts) == 0)
            {
                throw new ValidationException("Local cursor sort values must uniquely identify each matching row.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
            }
        }
    }

    internal static int CompareSortKeys(PatentsViewDerivedTableRecord left, PatentsViewDerivedTableRecord right, SearchSort[] sorts)
    {
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            int comparison = CompareFieldValues(left.ReadValue(sorts[index].Field ?? string.Empty), right.ReadValue(sorts[index].Field ?? string.Empty));
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    internal static PatentsViewDerivedTableRecord[] ProjectRows(global::System.Collections.Generic.List<PatentsViewDerivedTableRecord> matches, int start, int count, string[] fields)
    {
        PatentsViewDerivedTableRecord[] result = new PatentsViewDerivedTableRecord[count];
        int index;
        for (index = 0; index < count; index += 1)
        {
            result[index] = ProjectRow(matches[start + index], fields);
        }

        return result;
    }

    internal static PatentsViewDerivedTableRecord ProjectRow(PatentsViewDerivedTableRecord source, string[] fields)
    {
        PatentsViewDerivedTableRecord result = new PatentsViewDerivedTableRecord();
        result.RowNumber = source.RowNumber;
        result.StartLineNumber = source.StartLineNumber;
        result.EndLineNumber = source.EndLineNumber;
        result.StartOffset = source.StartOffset;
        result.HasMissingColumns = source.HasMissingColumns;
        result.HasExtraColumns = source.HasExtraColumns;
        result.Provenance = CopyProvenance(source.Provenance, source.Schema.TableIdentifier);
        if (fields.Length == 0)
        {
            result.Schema = source.Schema;
            int allIndex;
            for (allIndex = 0; allIndex < source.Schema.Headers.Length; allIndex += 1)
            {
                string header = source.Schema.Headers[allIndex];
                result.Fields.Add(header, source.ReadValue(header) ?? string.Empty);
            }

            result.ExtraValues = new string[source.ExtraValues.Length];
            global::System.Array.Copy(source.ExtraValues, result.ExtraValues, source.ExtraValues.Length);
            return result;
        }

        result.Provenance.FieldSources = new FieldDataProvenance[0];
        result.Schema = CreateProjectedSchema(source.Schema, fields);
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            result.Fields.Add(fields[index], source.ReadValue(fields[index]) ?? string.Empty);
            ProvenanceLabeler.AddLocalPatentField(result.Provenance, fields[index], source.Provenance);
        }

        return result;
    }

    internal static PatentsViewDerivedTableSchema CreateProjectedSchema(PatentsViewDerivedTableSchema source, string[] fields)
    {
        PatentsViewDerivedTableSchema result = new PatentsViewDerivedTableSchema();
        result.Family = source.Family;
        result.TableIdentifier = source.TableIdentifier;
        result.DetectionReason = "field-selection";
        result.Headers = new string[fields.Length];
        result.NormalizedHeaders = new string[fields.Length];
        result.ColumnCount = fields.Length;
        global::System.Text.StringBuilder fingerprint = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            result.Headers[index] = fields[index];
            result.NormalizedHeaders[index] = PatentsViewGrantBulkParser.NormalizeName(fields[index]);
            if (index > 0)
            {
                fingerprint.Append('|');
            }

            fingerprint.Append(result.NormalizedHeaders[index]);
        }

        result.Fingerprint = fingerprint.ToString();
        return result;
    }

    internal static bool HasMoreKnownMatches(PatentsViewLocalRowQueryResult result, int matchedCount, bool cursorMode)
    {
        if (cursorMode)
        {
            return result.Metadata.HasNextCursorPage;
        }

        return result.HasNextOffset && result.NextOffset < matchedCount;
    }

    internal static void ValidateRequest(PatentsViewLocalRowQueryRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A PatentsView local row query is required.", "request", "REQUIRED", null);
        }

        if (request.Table == null || global::System.String.IsNullOrWhiteSpace(request.Table.FilePath))
        {
            throw new ValidationException("A local table file is required.", "Table.FilePath", "REQUIRED", null);
        }

        if (!global::System.IO.File.Exists(request.Table.FilePath))
        {
            throw new ValidationException("The local table file does not exist.", "Table.FilePath", "FILE_NOT_FOUND", null);
        }

        if (request.Table.Delimiter != ',' && request.Table.Delimiter != '\t')
        {
            throw new ValidationException("A local table delimiter must be a comma or tab.", "Table.Delimiter", "UNSUPPORTED_DELIMITER", null);
        }

        PatentsViewDerivedTableParser.ValidateInspectableFamily(request.Table.Family);
        if (request.Search == null)
        {
            throw new ValidationException("A local structured search request is required.", "Search", "REQUIRED", null);
        }

        if (request.Limits == null)
        {
            throw new ValidationException("Local query limits are required.", "Limits", "REQUIRED", null);
        }

        if (request.MaximumScannedRows <= 0)
        {
            throw new ValidationException("The local scan limit must be positive.", "MaximumScannedRows", "SCAN_LIMIT_OUT_OF_RANGE", null);
        }

        PatentsViewQueryWriter.ValidateRequestShape(request.Search);
        PatentsViewQueryWriter.ValidateFields(request.Search.FieldSelection);
        PatentsViewQueryWriter.ValidateSorts(request.Search.Sorts);
        if (request.Search.Sorts.Length == 0)
        {
            throw new ValidationException("Local queries require deterministic sorting.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }

        if (request.Search.Context == null)
        {
            throw new ValidationException("A local query context is required.", "Context", "REQUIRED", null);
        }

        if (request.Search.MaximumResults <= 0 || request.Limits.MaximumResults <= 0)
        {
            throw new ValidationException("The local result limit must be positive.", "MaximumResults", "RESULT_LIMIT_OUT_OF_RANGE", null);
        }

        int filterIndex;
        for (filterIndex = 0; filterIndex < request.Search.Filters.Length; filterIndex += 1)
        {
            PatentsViewQueryWriter.WriteQuery(request.Search.Filters[filterIndex]);
        }

        int rangeIndex;
        for (rangeIndex = 0; rangeIndex < request.Search.Ranges.Length; rangeIndex += 1)
        {
            PatentsViewQueryWriter.ValidateRange(request.Search.Ranges[rangeIndex]);
        }

        if (request.Search.UseCursorPage)
        {
            PatentsViewQueryWriter.ValidateCursor(request.Search.CursorPage, request.Search.Sorts);
        }
        else
        {
            if (request.Search.OffsetPage == null)
            {
                throw new ValidationException("Local offset-page coordinates are required.", "OffsetPage", "REQUIRED", null);
            }

            if (request.Search.OffsetPage.Offset < 0L)
            {
                throw new ValidationException("The local result offset cannot be negative.", "OffsetPage.Offset", "OFFSET_OUT_OF_RANGE", null);
            }

            if (request.Search.OffsetPage.Limit <= 0)
            {
                throw new ValidationException("The local offset-page limit must be positive.", "OffsetPage.Limit", "PAGE_SIZE_OUT_OF_RANGE", null);
            }
        }
    }
}
