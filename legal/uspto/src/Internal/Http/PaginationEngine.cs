namespace uspto;

internal class OffsetPageResult
{
    internal object[] Records = new object[0];
    internal string[] RecordIdentifiers = new string[0];
    internal SearchResponseMetadata Metadata = new SearchResponseMetadata();
}

internal class OffsetPaginationResult
{
    internal object[] Records = new object[0];
    internal int PagesRead = 0;
    internal long Total = 0L;
    internal bool HasTotal = false;
    internal long NextOffset = 0L;
    internal bool Completed = false;
    internal bool EndedOnEmptyPage = false;
    internal bool ReachedPageLimit = false;
    internal bool ReachedResultLimit = false;
}

internal interface IOffsetPageSource
{
    OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken);
}

internal class CursorPageResult
{
    internal object[] Records = new object[0];
    internal string[] RecordIdentifiers = new string[0];
    internal SearchResponseMetadata Metadata = new SearchResponseMetadata();
}

internal class CursorPaginationResult
{
    internal object[] Records = new object[0];
    internal int PagesRead = 0;
    internal long Total = 0L;
    internal bool HasTotal = false;
    internal CursorPage NextCursorPage = new CursorPage();
    internal bool HasNextCursorPage = false;
    internal bool Completed = false;
    internal bool EndedOnEmptyPage = false;
    internal bool ReachedPageLimit = false;
    internal bool ReachedResultLimit = false;
}

internal interface ICursorPageSource
{
    CursorPageResult FetchCursorPage(string[] afterValues, int size, global::System.Threading.CancellationToken cancellationToken);
}

internal static class PaginationEngine
{
    internal static OffsetPaginationResult AggregateOffsetPages(IOffsetPageSource source, SearchRequest request, UsptoLimits limits, string stableSortField)
    {
        ValidateOffsetRequest(source, request, limits, stableSortField);
        global::System.Threading.CancellationToken cancellationToken = request.Context.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        int maximumPages = request.MaximumPages;
        if (limits.MaximumPages < maximumPages)
        {
            maximumPages = limits.MaximumPages;
        }

        int maximumResults = request.MaximumResults;
        if (limits.MaximumResults < maximumResults)
        {
            maximumResults = limits.MaximumResults;
        }

        global::System.Collections.Generic.List<object> records = new global::System.Collections.Generic.List<object>();
        global::System.Collections.Generic.HashSet<string> pageSignatures = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.HashSet<string> recordIdentifiers = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        OffsetPaginationResult result = new OffsetPaginationResult();
        long offset = request.OffsetPage.Offset;
        result.NextOffset = offset;
        while (result.PagesRead < maximumPages && records.Count < maximumResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OffsetPageResult page = source.FetchOffsetPage(offset, request.OffsetPage.Limit, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOffsetPage(page, offset, request.OffsetPage.Limit);
            result.PagesRead += 1;
            string signature = BuildPageSignature(page.RecordIdentifiers);
            if (page.Records.Length != 0 && !pageSignatures.Add(signature))
            {
                throw new ValidationException("Offset pagination returned a duplicate result page.", "RecordIdentifiers", "DUPLICATE_PAGE", null);
            }

            foreach (string recordIdentifier in page.RecordIdentifiers)
            {
                if (!recordIdentifiers.Add(recordIdentifier))
                {
                    throw new ValidationException("Offset pagination returned a duplicate record.", "RecordIdentifiers", "DUPLICATE_RECORD", null);
                }
            }

            if (page.Metadata.HasTotal)
            {
                if (!result.HasTotal)
                {
                    result.Total = page.Metadata.Total;
                    result.HasTotal = true;
                }
                else if (result.Total != page.Metadata.Total)
                {
                    throw new ValidationException("The service total changed between offset pages.", "Total", "TOTAL_MISMATCH", null);
                }
            }

            if (page.Records.Length == 0)
            {
                if (result.HasTotal && offset < result.Total)
                {
                    throw new ValidationException("Offset pagination ended before reaching the service-provided total.", "Total", "TOTAL_COUNT_MISMATCH", null);
                }

                result.Completed = true;
                result.EndedOnEmptyPage = true;
                break;
            }

            if (offset > global::System.Int64.MaxValue - page.Records.Length)
            {
                throw new ValidationException("The next result offset exceeds the supported signed range.", "Offset", "OFFSET_OVERFLOW", null);
            }

            long pageEndOffset = offset + page.Records.Length;
            if (result.HasTotal && pageEndOffset > result.Total)
            {
                throw new ValidationException("A returned page extends beyond the service-provided total.", "Total", "TOTAL_COUNT_MISMATCH", null);
            }

            int remaining = maximumResults - records.Count;
            int acceptedCount = page.Records.Length;
            if (acceptedCount > remaining)
            {
                acceptedCount = remaining;
            }

            for (int recordIndex = 0; recordIndex < acceptedCount; recordIndex += 1)
            {
                records.Add(page.Records[recordIndex]);
            }

            if (acceptedCount < page.Records.Length)
            {
                result.NextOffset = offset + acceptedCount;
                result.ReachedResultLimit = true;
                break;
            }

            offset = pageEndOffset;
            result.NextOffset = offset;
            if (result.HasTotal && offset >= result.Total)
            {
                result.Completed = true;
                break;
            }

            if (records.Count >= maximumResults)
            {
                result.ReachedResultLimit = true;
                break;
            }
        }

        if (!result.Completed && !result.ReachedResultLimit && result.PagesRead >= maximumPages)
        {
            result.ReachedPageLimit = true;
        }

        object[] copiedRecords = new object[records.Count];
        records.CopyTo(copiedRecords, 0);
        result.Records = copiedRecords;
        return result;
    }

    internal static CursorPaginationResult AggregateCursorPages(ICursorPageSource source, SearchRequest request, UsptoLimits limits, string stableSortField)
    {
        ValidateCursorRequest(source, request, limits, stableSortField);
        global::System.Threading.CancellationToken cancellationToken = request.Context.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        int maximumPages = request.MaximumPages;
        if (limits.MaximumPages < maximumPages)
        {
            maximumPages = limits.MaximumPages;
        }

        int maximumResults = request.MaximumResults;
        if (limits.MaximumResults < maximumResults)
        {
            maximumResults = limits.MaximumResults;
        }

        global::System.Collections.Generic.List<object> records = new global::System.Collections.Generic.List<object>();
        global::System.Collections.Generic.HashSet<string> pageSignatures = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.HashSet<string> cursorSignatures = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.HashSet<string> recordIdentifiers = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        string[] afterValues = CopyValues(request.CursorPage.AfterValues);
        bool startedAtBeginning = afterValues.Length == 0;
        if (!startedAtBeginning)
        {
            cursorSignatures.Add(BuildPageSignature(afterValues));
        }

        CursorPaginationResult result = new CursorPaginationResult();
        result.NextCursorPage.AfterValues = CopyValues(afterValues);
        result.NextCursorPage.Size = request.CursorPage.Size;
        while (result.PagesRead < maximumPages && records.Count < maximumResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int requestSize = request.CursorPage.Size;
            int remaining = maximumResults - records.Count;
            if (remaining < requestSize)
            {
                requestSize = remaining;
            }

            CursorPageResult page = source.FetchCursorPage(CopyValues(afterValues), requestSize, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            ValidateCursorPage(page, requestSize, request.Sorts.Length);
            result.PagesRead += 1;
            string pageSignature = BuildPageSignature(page.RecordIdentifiers);
            if (page.Records.Length != 0 && !pageSignatures.Add(pageSignature))
            {
                throw new ValidationException("Cursor pagination returned a duplicate result page.", "RecordIdentifiers", "DUPLICATE_PAGE", null);
            }

            foreach (string recordIdentifier in page.RecordIdentifiers)
            {
                if (!recordIdentifiers.Add(recordIdentifier))
                {
                    throw new ValidationException("Cursor pagination returned a duplicate record.", "RecordIdentifiers", "DUPLICATE_RECORD", null);
                }
            }

            if (page.Metadata.HasTotal)
            {
                if (!result.HasTotal)
                {
                    result.Total = page.Metadata.Total;
                    result.HasTotal = true;
                }
                else if (result.Total != page.Metadata.Total)
                {
                    throw new ValidationException("The service total changed between cursor pages.", "Total", "TOTAL_MISMATCH", null);
                }
            }

            if (page.Records.Length == 0)
            {
                if (startedAtBeginning && result.HasTotal && records.Count < result.Total)
                {
                    throw new ValidationException("Cursor pagination ended before reaching the service-provided total.", "Total", "TOTAL_COUNT_MISMATCH", null);
                }

                result.Completed = true;
                result.EndedOnEmptyPage = true;
                result.HasNextCursorPage = false;
                break;
            }

            foreach (object record in page.Records)
            {
                records.Add(record);
            }

            if (startedAtBeginning && result.HasTotal && records.Count > result.Total)
            {
                throw new ValidationException("Cursor pages contain more records than the service-provided total.", "Total", "TOTAL_COUNT_MISMATCH", null);
            }

            if (startedAtBeginning && result.HasTotal && records.Count == result.Total)
            {
                result.Completed = true;
                result.HasNextCursorPage = false;
                break;
            }

            if (!page.Metadata.HasNextCursorPage)
            {
                if (startedAtBeginning && result.HasTotal && records.Count < result.Total)
                {
                    throw new ValidationException("Cursor pagination ended before reaching the service-provided total.", "Total", "TOTAL_COUNT_MISMATCH", null);
                }

                result.Completed = true;
                result.HasNextCursorPage = false;
                break;
            }

            string[] nextValues = CopyValues(page.Metadata.NextCursorPage.AfterValues);
            string cursorSignature = BuildPageSignature(nextValues);
            if (!cursorSignatures.Add(cursorSignature))
            {
                throw new ValidationException("PatentsView returned a repeated cursor.", "AfterValues", "DUPLICATE_CURSOR", null);
            }

            afterValues = nextValues;
            result.NextCursorPage.AfterValues = CopyValues(afterValues);
            result.NextCursorPage.Size = request.CursorPage.Size;
            result.HasNextCursorPage = true;
            if (records.Count >= maximumResults)
            {
                result.ReachedResultLimit = true;
                break;
            }
        }

        if (!result.Completed && !result.ReachedResultLimit && result.PagesRead >= maximumPages)
        {
            result.ReachedPageLimit = true;
        }

        object[] copiedRecords = new object[records.Count];
        records.CopyTo(copiedRecords, 0);
        result.Records = copiedRecords;
        return result;
    }

    internal static void ValidateOffsetRequest(IOffsetPageSource source, SearchRequest request, UsptoLimits limits, string stableSortField)
    {
        if (source == null)
        {
            throw new ValidationException("An offset-page source is required.", "source", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("A search request is required.", "request", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Pagination limits are required.", "limits", "REQUIRED", null);
        }

        if (request.UseCursorPage)
        {
            throw new ValidationException("Offset pagination cannot process a cursor-page request.", "UseCursorPage", "OFFSET_PAGE_REQUIRED", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("Offset-page coordinates are required.", "OffsetPage", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A request context is required.", "Context", "REQUIRED", null);
        }

        if (request.OffsetPage.Offset < 0L)
        {
            throw new ValidationException("The result offset cannot be negative.", "Offset", "OFFSET_OUT_OF_RANGE", null);
        }

        if (request.OffsetPage.Limit <= 0)
        {
            throw new ValidationException("The offset-page limit must be positive.", "Limit", "PAGE_SIZE_OUT_OF_RANGE", null);
        }

        if (request.MaximumPages <= 0 || limits.MaximumPages <= 0)
        {
            throw new ValidationException("The pagination page limit must be positive.", "MaximumPages", "PAGE_LIMIT_OUT_OF_RANGE", null);
        }

        if (request.MaximumResults <= 0 || limits.MaximumResults <= 0)
        {
            throw new ValidationException("The pagination result limit must be positive.", "MaximumResults", "RESULT_LIMIT_OUT_OF_RANGE", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(stableSortField))
        {
            throw new ValidationException("A stable tie-breaker sort field is required.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }

        if (request.Sorts == null || request.Sorts.Length == 0)
        {
            throw new ValidationException("Offset aggregation requires deterministic sorting.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> sortFields = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        foreach (SearchSort sort in request.Sorts)
        {
            if (sort == null || global::System.String.IsNullOrWhiteSpace(sort.Field))
            {
                throw new ValidationException("Every pagination sort must name a field.", "Sorts", "INVALID_SORT", null);
            }

            if (sort.Direction != SortDirection.Ascending && sort.Direction != SortDirection.Descending)
            {
                throw new ValidationException("Every pagination sort must use a supported direction.", "Sorts", "INVALID_SORT", null);
            }

            if (!sortFields.Add(sort.Field))
            {
                throw new ValidationException("Pagination sort fields cannot be repeated.", "Sorts", "DUPLICATE_SORT", null);
            }
        }

        SearchSort finalSort = request.Sorts[request.Sorts.Length - 1];
        if (!global::System.String.Equals(finalSort.Field, stableSortField, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The final pagination sort must be the stable tie-breaker field.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }
    }

    internal static void ValidateOffsetPage(OffsetPageResult page, long requestedOffset, int requestedLimit)
    {
        if (page == null)
        {
            throw new ValidationException("The offset-page source returned no page.", "page", "MISSING_PAGE", null);
        }

        if (page.Records == null || page.RecordIdentifiers == null || page.Metadata == null)
        {
            throw new ValidationException("The offset page is missing records, identifiers, or metadata.", "page", "INVALID_PAGE", null);
        }

        if (page.Metadata.ReturnedCount != page.Records.Length || page.RecordIdentifiers.Length != page.Records.Length)
        {
            throw new ValidationException("The offset-page count does not match its records and identifiers.", "ReturnedCount", "PAGE_COUNT_MISMATCH", null);
        }

        if (page.Records.Length > requestedLimit)
        {
            throw new ValidationException("The offset page contains more records than requested.", "ReturnedCount", "PAGE_SIZE_EXCEEDED", null);
        }

        if (page.Metadata.HasOffsetPage && (page.Metadata.OffsetPage.Offset != requestedOffset || page.Metadata.OffsetPage.Limit != requestedLimit))
        {
            throw new ValidationException("The returned offset-page coordinates do not match the request.", "OffsetPage", "PAGE_OFFSET_MISMATCH", null);
        }

        if (page.Metadata.HasTotal && page.Metadata.Total < 0L)
        {
            throw new ValidationException("The service-provided result total cannot be negative.", "Total", "TOTAL_OUT_OF_RANGE", null);
        }

        global::System.Collections.Generic.HashSet<string> identifiers = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        foreach (string identifier in page.RecordIdentifiers)
        {
            if (global::System.String.IsNullOrWhiteSpace(identifier))
            {
                throw new ValidationException("Every paged record requires a stable identifier.", "RecordIdentifiers", "MISSING_RECORD_IDENTIFIER", null);
            }

            if (!identifiers.Add(identifier))
            {
                throw new ValidationException("An offset page contains duplicate record identifiers.", "RecordIdentifiers", "DUPLICATE_RECORD", null);
            }
        }
    }

    internal static void ValidateCursorRequest(ICursorPageSource source, SearchRequest request, UsptoLimits limits, string stableSortField)
    {
        if (source == null)
        {
            throw new ValidationException("A cursor-page source is required.", "source", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("A search request is required.", "request", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("Pagination limits are required.", "limits", "REQUIRED", null);
        }

        if (!request.UseCursorPage)
        {
            throw new ValidationException("Cursor pagination requires a cursor-page request.", "UseCursorPage", "CURSOR_PAGE_REQUIRED", null);
        }

        if (request.CursorPage == null)
        {
            throw new ValidationException("Cursor-page coordinates are required.", "CursorPage", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A request context is required.", "Context", "REQUIRED", null);
        }

        if (request.CursorPage.Size <= 0 || request.CursorPage.Size > UsptoDefaults.PatentsViewMaximumPageSize)
        {
            throw new ValidationException("The PatentsView cursor-page size must be between one and 1,000 records.", "Size", "PAGE_SIZE_OUT_OF_RANGE", null);
        }

        if (request.MaximumPages <= 0 || limits.MaximumPages <= 0)
        {
            throw new ValidationException("The pagination page limit must be positive.", "MaximumPages", "PAGE_LIMIT_OUT_OF_RANGE", null);
        }

        if (request.MaximumResults <= 0 || limits.MaximumResults <= 0)
        {
            throw new ValidationException("The pagination result limit must be positive.", "MaximumResults", "RESULT_LIMIT_OUT_OF_RANGE", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(stableSortField) || request.Sorts == null || request.Sorts.Length == 0)
        {
            throw new ValidationException("PatentsView cursor pagination requires deterministic sorting.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> sortFields = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        foreach (SearchSort sort in request.Sorts)
        {
            if (sort == null || global::System.String.IsNullOrWhiteSpace(sort.Field))
            {
                throw new ValidationException("Every cursor sort must name a field.", "Sorts", "INVALID_SORT", null);
            }

            if (sort.Direction != SortDirection.Ascending && sort.Direction != SortDirection.Descending)
            {
                throw new ValidationException("Every cursor sort must use a supported direction.", "Sorts", "INVALID_SORT", null);
            }

            if (!sortFields.Add(sort.Field))
            {
                throw new ValidationException("Cursor sort fields cannot be repeated.", "Sorts", "DUPLICATE_SORT", null);
            }
        }

        SearchSort finalSort = request.Sorts[request.Sorts.Length - 1];
        if (!global::System.String.Equals(finalSort.Field, stableSortField, global::System.StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The final cursor sort must be the stable tie-breaker field.", "Sorts", "DETERMINISTIC_SORT_REQUIRED", null);
        }

        ValidateAfterValues(request.CursorPage.AfterValues, request.Sorts.Length);
    }

    internal static void ValidateCursorPage(CursorPageResult page, int requestedSize, int sortCount)
    {
        if (page == null)
        {
            throw new ValidationException("The cursor-page source returned no page.", "page", "MISSING_PAGE", null);
        }

        if (page.Records == null || page.RecordIdentifiers == null || page.Metadata == null)
        {
            throw new ValidationException("The cursor page is missing records, identifiers, or metadata.", "page", "INVALID_PAGE", null);
        }

        if (page.Metadata.ReturnedCount != page.Records.Length || page.RecordIdentifiers.Length != page.Records.Length)
        {
            throw new ValidationException("The cursor-page count does not match its records and identifiers.", "ReturnedCount", "PAGE_COUNT_MISMATCH", null);
        }

        if (page.Records.Length > requestedSize || page.Records.Length > UsptoDefaults.PatentsViewMaximumPageSize)
        {
            throw new ValidationException("The cursor page contains more records than requested or supported.", "ReturnedCount", "PAGE_SIZE_EXCEEDED", null);
        }

        if (page.Metadata.HasTotal && page.Metadata.Total < 0L)
        {
            throw new ValidationException("The service-provided result total cannot be negative.", "Total", "TOTAL_OUT_OF_RANGE", null);
        }

        global::System.Collections.Generic.HashSet<string> identifiers = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        foreach (string identifier in page.RecordIdentifiers)
        {
            if (global::System.String.IsNullOrWhiteSpace(identifier))
            {
                throw new ValidationException("Every cursor-paged record requires a stable identifier.", "RecordIdentifiers", "MISSING_RECORD_IDENTIFIER", null);
            }

            if (!identifiers.Add(identifier))
            {
                throw new ValidationException("A cursor page contains duplicate record identifiers.", "RecordIdentifiers", "DUPLICATE_RECORD", null);
            }
        }

        if (page.Metadata.HasNextCursorPage)
        {
            if (page.Metadata.NextCursorPage == null)
            {
                throw new ValidationException("The next cursor coordinates are missing.", "NextCursorPage", "INVALID_CURSOR", null);
            }

            ValidateAfterValues(page.Metadata.NextCursorPage.AfterValues, sortCount);
            if (page.Metadata.NextCursorPage.AfterValues.Length == 0)
            {
                throw new ValidationException("A following cursor cannot be empty.", "AfterValues", "INVALID_CURSOR", null);
            }
        }
    }

    internal static void ValidateAfterValues(string[] afterValues, int sortCount)
    {
        if (afterValues == null)
        {
            throw new ValidationException("Cursor values are required.", "AfterValues", "REQUIRED", null);
        }

        if (afterValues.Length != 0 && afterValues.Length != sortCount)
        {
            throw new ValidationException("Cursor values must match the number and order of sort fields.", "AfterValues", "CURSOR_SORT_MISMATCH", null);
        }

        foreach (string value in afterValues)
        {
            if (global::System.String.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException("Cursor sort values cannot be blank.", "AfterValues", "INVALID_CURSOR", null);
            }
        }
    }

    internal static string[] CopyValues(string[] values)
    {
        string[] copy = new string[values.Length];
        global::System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static string BuildPageSignature(string[] identifiers)
    {
        global::System.Text.StringBuilder signature = new global::System.Text.StringBuilder();
        foreach (string identifier in identifiers)
        {
            signature.Append(identifier.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            signature.Append(':');
            signature.Append(identifier);
            signature.Append(';');
        }

        return signature.ToString();
    }
}
