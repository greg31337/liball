namespace uspto;

internal class OdpSearchRequestParts
{
    internal string? RawQuery = null;
    internal global::System.Collections.Generic.List<SearchFilter> ExactFilters = new global::System.Collections.Generic.List<SearchFilter>();
    internal global::System.Collections.Generic.List<SearchFilter> QueryPredicates = new global::System.Collections.Generic.List<SearchFilter>();
    internal global::System.Collections.Generic.List<SearchRange> ExactRanges = new global::System.Collections.Generic.List<SearchRange>();
    internal global::System.Collections.Generic.List<SearchRange> QueryRanges = new global::System.Collections.Generic.List<SearchRange>();
}

internal static class OdpSearchRequestWriter
{
    internal static global::System.Collections.Generic.Dictionary<string, string?> WriteGetQuery(SearchRequest request, UsptoLimits limits)
    {
        ValidateRequest(request, limits, true);
        OdpSearchRequestParts parts = CreateParts(request);
        bool useExactFilter = parts.ExactFilters.Count == 1 && CanUseGetExactFilter(parts.ExactFilters[0]);
        bool useExactRange = parts.ExactRanges.Count == 1 && CanUseGetRangeFilter(parts.ExactRanges[0]);
        string queryText = BuildCombinedQuery(parts, !useExactFilter, !useExactRange);
        global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
        if (queryText.Length != 0)
        {
            query.Add("q", queryText);
        }

        if (useExactFilter)
        {
            query.Add("filters", OdpQueryWriter.WriteExactFilter(parts.ExactFilters[0]));
        }

        if (useExactRange)
        {
            query.Add("rangeFilters", OdpQueryWriter.WriteRangeFilter(parts.ExactRanges[0]));
        }

        string sorts = OdpQueryWriter.WriteSorts(request.Sorts);
        if (sorts.Length != 0)
        {
            query.Add("sort", sorts);
        }

        string fields = OdpQueryWriter.WriteSelectedFields(request.FieldSelection);
        if (fields.Length != 0)
        {
            query.Add("fields", fields);
        }

        string facets = OdpQueryWriter.WriteFacets(request.Facets);
        if (facets.Length != 0)
        {
            query.Add("facets", facets);
        }

        query.Add("offset", request.OffsetPage.Offset.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        query.Add("limit", request.OffsetPage.Limit.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        ValidateGetQuerySize(query, limits.MaximumUriCharacters);
        return query;
    }

    internal static string WritePostBody(SearchRequest request, UsptoLimits limits)
    {
        ValidateRequest(request, limits, false);
        OdpSearchRequestParts parts = CreateParts(request);
        string queryText = BuildCombinedQuery(parts, false, false);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            if (queryText.Length != 0)
            {
                writer.WriteString("q", queryText);
            }

            WritePostFilters(writer, parts.ExactFilters);
            WritePostRanges(writer, parts.ExactRanges);
            WritePostSorts(writer, request.Sorts);
            WritePostFields(writer, request.FieldSelection);
            writer.WritePropertyName("pagination");
            writer.WriteStartObject();
            writer.WriteNumber("offset", request.OffsetPage.Offset);
            writer.WriteNumber("limit", request.OffsetPage.Limit);
            writer.WriteEndObject();
            WritePostFacets(writer, request.Facets);
            writer.WriteEndObject();
            writer.Flush();
        }

        long bodyBytes = output.Length;
        if (bodyBytes > limits.MaximumRequestBytes)
        {
            throw new ValidationException("The ODP search body exceeds the configured request-size limit.", "MaximumRequestBytes", "REQUEST_BODY_TOO_LARGE", null);
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WriteExportBody(SearchRequest request, ExportFormat format, UsptoLimits limits)
    {
        ValidateRequest(request, limits, false);
        if (format != ExportFormat.Json && format != ExportFormat.Csv)
        {
            throw new ValidationException("The ODP export format must be JSON or CSV.", "ExportFormat", "UNSUPPORTED_FORMAT", null);
        }

        OdpSearchRequestParts parts = CreateParts(request);
        string queryText = BuildCombinedQuery(parts, false, false);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            if (queryText.Length != 0)
            {
                writer.WriteString("q", queryText);
            }

            WritePostFilters(writer, parts.ExactFilters);
            WritePostRanges(writer, parts.ExactRanges);
            WritePostSorts(writer, request.Sorts);
            WritePostFields(writer, request.FieldSelection);
            writer.WritePropertyName("pagination");
            writer.WriteStartObject();
            writer.WriteNumber("offset", request.OffsetPage.Offset);
            writer.WriteNumber("limit", request.OffsetPage.Limit);
            writer.WriteEndObject();
            if (format == ExportFormat.Csv)
            {
                writer.WriteString("format", "csv");
            }
            else
            {
                writer.WriteString("format", "json");
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        if (output.Length > limits.MaximumRequestBytes)
        {
            throw new ValidationException("The ODP export body exceeds the configured request-size limit.", "MaximumRequestBytes", "REQUEST_BODY_TOO_LARGE", null);
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static OdpSearchRequestParts CreateParts(SearchRequest request)
    {
        OdpSearchRequestParts parts = new OdpSearchRequestParts();
        parts.RawQuery = request.Query;
        int filterIndex;
        for (filterIndex = 0; filterIndex < request.Filters.Length; filterIndex += 1)
        {
            SearchFilter filter = request.Filters[filterIndex];
            if (filter == null)
            {
                throw new ValidationException("ODP search filters cannot contain null values.", "Filters", "REQUIRED", null);
            }

            OdpQueryWriter.WritePredicate(filter);
            if (IsExactFilter(filter))
            {
                parts.ExactFilters.Add(filter);
            }
            else
            {
                parts.QueryPredicates.Add(filter);
            }
        }

        int rangeIndex;
        for (rangeIndex = 0; rangeIndex < request.Ranges.Length; rangeIndex += 1)
        {
            SearchRange range = request.Ranges[rangeIndex];
            if (range == null)
            {
                throw new ValidationException("ODP search ranges cannot contain null values.", "Ranges", "REQUIRED", null);
            }

            OdpQueryWriter.WriteRangeQuery(range);
            if (IsExactRange(range))
            {
                parts.ExactRanges.Add(range);
            }
            else
            {
                parts.QueryRanges.Add(range);
            }
        }

        return parts;
    }

    internal static string BuildCombinedQuery(OdpSearchRequestParts parts, bool includeExactFilters, bool includeExactRanges)
    {
        global::System.Collections.Generic.List<string> fragments = new global::System.Collections.Generic.List<string>();
        int structuredCount = parts.QueryPredicates.Count + parts.QueryRanges.Count;
        if (includeExactFilters)
        {
            structuredCount += parts.ExactFilters.Count;
        }

        if (includeExactRanges)
        {
            structuredCount += parts.ExactRanges.Count;
        }

        if (parts.RawQuery != null)
        {
            string raw = parts.RawQuery;
            if (structuredCount != 0)
            {
                raw = "(" + raw + ")";
            }

            fragments.Add(raw);
        }

        int index;
        for (index = 0; index < parts.QueryPredicates.Count; index += 1)
        {
            fragments.Add(OdpQueryWriter.WritePredicate(parts.QueryPredicates[index]));
        }

        if (includeExactFilters)
        {
            for (index = 0; index < parts.ExactFilters.Count; index += 1)
            {
                fragments.Add(OdpQueryWriter.WritePredicate(parts.ExactFilters[index]));
            }
        }

        for (index = 0; index < parts.QueryRanges.Count; index += 1)
        {
            fragments.Add(OdpQueryWriter.WriteRangeQuery(parts.QueryRanges[index]));
        }

        if (includeExactRanges)
        {
            for (index = 0; index < parts.ExactRanges.Count; index += 1)
            {
                fragments.Add(OdpQueryWriter.WriteRangeQuery(parts.ExactRanges[index]));
            }
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        for (index = 0; index < fragments.Count; index += 1)
        {
            if (index != 0)
            {
                builder.Append(" AND ");
            }

            builder.Append(fragments[index]);
        }

        return builder.ToString();
    }

    internal static void WritePostFilters(global::System.Text.Json.Utf8JsonWriter writer, global::System.Collections.Generic.List<SearchFilter> filters)
    {
        if (filters.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("filters");
        writer.WriteStartArray();
        int index;
        for (index = 0; index < filters.Count; index += 1)
        {
            SearchFilter filter = filters[index];
            writer.WriteStartObject();
            writer.WriteString("name", filter.Field);
            writer.WritePropertyName("value");
            writer.WriteStartArray();
            if (filter.Operator == SearchOperator.Equal)
            {
                writer.WriteStringValue(filter.Value);
            }
            else
            {
                int valueIndex;
                for (valueIndex = 0; valueIndex < filter.Values.Length; valueIndex += 1)
                {
                    writer.WriteStringValue(filter.Values[valueIndex]);
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    internal static void WritePostRanges(global::System.Text.Json.Utf8JsonWriter writer, global::System.Collections.Generic.List<SearchRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return;
        }

        writer.WritePropertyName("rangeFilters");
        writer.WriteStartArray();
        int index;
        for (index = 0; index < ranges.Count; index += 1)
        {
            SearchRange range = ranges[index];
            writer.WriteStartObject();
            writer.WriteString("field", range.Field);
            writer.WriteString("valueFrom", range.LowerValue);
            writer.WriteString("valueTo", range.UpperValue);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    internal static void WritePostSorts(global::System.Text.Json.Utf8JsonWriter writer, SearchSort[] sorts)
    {
        OdpQueryWriter.WriteSorts(sorts);
        if (sorts.Length == 0)
        {
            return;
        }

        writer.WritePropertyName("sort");
        writer.WriteStartArray();
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            writer.WriteStartObject();
            writer.WriteString("field", sorts[index].Field);
            string direction = "asc";
            if (sorts[index].Direction == SortDirection.Descending)
            {
                direction = "desc";
            }

            writer.WriteString("order", direction);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    internal static void WritePostFields(global::System.Text.Json.Utf8JsonWriter writer, SearchFieldSelection selection)
    {
        OdpQueryWriter.WriteSelectedFields(selection);
        if (selection.Fields.Length == 0)
        {
            return;
        }

        writer.WritePropertyName("fields");
        writer.WriteStartArray();
        int index;
        for (index = 0; index < selection.Fields.Length; index += 1)
        {
            writer.WriteStringValue(selection.Fields[index]);
        }

        writer.WriteEndArray();
    }

    internal static void WritePostFacets(global::System.Text.Json.Utf8JsonWriter writer, SearchFacetRequest[] facets)
    {
        OdpQueryWriter.WriteFacets(facets);
        if (facets.Length == 0)
        {
            return;
        }

        writer.WritePropertyName("facets");
        writer.WriteStartArray();
        int index;
        for (index = 0; index < facets.Length; index += 1)
        {
            writer.WriteStringValue(facets[index].Field);
        }

        writer.WriteEndArray();
    }

    internal static void ValidateRequest(SearchRequest request, UsptoLimits limits, bool isGet)
    {
        if (request == null)
        {
            throw new ValidationException("An ODP search request is required.", "request", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("ODP request limits are required.", "limits", "REQUIRED", null);
        }

        if (isGet && limits.MaximumUriCharacters <= 0)
        {
            throw new ValidationException("The maximum URI length must be positive.", "MaximumUriCharacters", "INVALID_LIMIT", null);
        }

        if (!isGet && limits.MaximumRequestBytes <= 0L)
        {
            throw new ValidationException("The maximum request body length must be positive.", "MaximumRequestBytes", "INVALID_LIMIT", null);
        }

        if (request.UseCursorPage)
        {
            throw new ValidationException("ODP searches require offset pagination.", "UseCursorPage", "UNSUPPORTED_PAGE_MODE", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("ODP offset pagination is required.", "OffsetPage", "REQUIRED", null);
        }

        if (request.OffsetPage.Offset < 0L || request.OffsetPage.Offset > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The ODP result offset must fit a nonnegative 32-bit integer.", "OffsetPage.Offset", "INVALID_OFFSET", null);
        }

        if (request.OffsetPage.Limit <= 0)
        {
            throw new ValidationException("The ODP page limit must be positive.", "OffsetPage.Limit", "INVALID_LIMIT", null);
        }

        if (request.Filters == null)
        {
            throw new ValidationException("ODP search filters are required.", "Filters", "REQUIRED", null);
        }

        if (request.Ranges == null)
        {
            throw new ValidationException("ODP search ranges are required.", "Ranges", "REQUIRED", null);
        }

        if (request.Sorts == null)
        {
            throw new ValidationException("ODP search sorts are required.", "Sorts", "REQUIRED", null);
        }

        if (request.FieldSelection == null)
        {
            throw new ValidationException("An ODP field selection is required.", "FieldSelection", "REQUIRED", null);
        }

        if (request.Facets == null)
        {
            throw new ValidationException("ODP search facets are required.", "Facets", "REQUIRED", null);
        }

        if (request.Query != null)
        {
            if (global::System.String.IsNullOrWhiteSpace(request.Query))
            {
                throw new ValidationException("The ODP raw query cannot be blank.", "Query", "INVALID_QUERY_LITERAL", null);
            }

            int queryIndex;
            for (queryIndex = 0; queryIndex < request.Query.Length; queryIndex += 1)
            {
                if (global::System.Char.IsControl(request.Query[queryIndex]))
                {
                    throw new ValidationException("The ODP raw query contains a control character.", "Query", "INVALID_QUERY_LITERAL", null);
                }
            }
        }

    }

    internal static void ValidateGetQuerySize(global::System.Collections.Generic.Dictionary<string, string?> query, int maximumCharacters)
    {
        string[] keys = new string[query.Count];
        query.Keys.CopyTo(keys, 0);
        global::System.Array.Sort(keys, global::System.StringComparer.Ordinal);
        long characters = 0L;
        int index;
        for (index = 0; index < keys.Length; index += 1)
        {
            characters += 1L;
            characters += global::System.Uri.EscapeDataString(keys[index]).Length;
            string? value = query[keys[index]];
            if (value != null)
            {
                characters += 1L;
                characters += global::System.Uri.EscapeDataString(value).Length;
            }
        }

        if (characters > maximumCharacters)
        {
            throw new ValidationException("The encoded ODP query exceeds the configured URI-size limit.", "MaximumUriCharacters", "REQUEST_URI_TOO_LARGE", null);
        }
    }

    internal static void ValidateGetRequestUri(global::System.Uri uri, UsptoLimits limits)
    {
        if (uri == null)
        {
            throw new ValidationException("An encoded ODP request URI is required.", "uri", "REQUIRED", null);
        }

        if (!uri.IsAbsoluteUri)
        {
            throw new ValidationException("The encoded ODP request URI must be absolute.", "uri", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("ODP request limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumUriCharacters <= 0)
        {
            throw new ValidationException("The maximum URI length must be positive.", "MaximumUriCharacters", "INVALID_LIMIT", null);
        }

        if (uri.AbsoluteUri.Length > limits.MaximumUriCharacters)
        {
            throw new ValidationException("The encoded ODP request URI exceeds the configured size limit.", "MaximumUriCharacters", "REQUEST_URI_TOO_LARGE", null);
        }
    }

    internal static bool IsExactFilter(SearchFilter filter)
    {
        return filter.Operator == SearchOperator.Equal || filter.Operator == SearchOperator.In;
    }

    internal static bool IsExactRange(SearchRange range)
    {
        return range.IncludeLower && range.IncludeUpper && range.LowerValue != null && range.UpperValue != null;
    }

    internal static bool CanUseGetExactFilter(SearchFilter filter)
    {
        if (filter.Operator == SearchOperator.Equal)
        {
            return filter.Value != null && filter.Value.IndexOf(',') < 0;
        }

        int index;
        for (index = 0; index < filter.Values.Length; index += 1)
        {
            if (filter.Values[index].IndexOf(',') >= 0)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool CanUseGetRangeFilter(SearchRange range)
    {
        if (range.LowerValue == null || range.UpperValue == null)
        {
            return false;
        }

        return range.LowerValue.IndexOf(':') < 0 && range.UpperValue.IndexOf(':') < 0 && !global::System.Char.IsWhiteSpace(range.LowerValue[0]) && !global::System.Char.IsWhiteSpace(range.LowerValue[range.LowerValue.Length - 1]) && !global::System.Char.IsWhiteSpace(range.UpperValue[0]) && !global::System.Char.IsWhiteSpace(range.UpperValue[range.UpperValue.Length - 1]);
    }
}
