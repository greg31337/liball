namespace uspto;

internal static class PatentsViewQueryWriter
{
    internal const int MaximumPredicateDepth = 64;

    internal static string WriteQuery(SearchFilter filter)
    {
        if (filter == null)
        {
            throw new ValidationException("A PatentsView query predicate is required.", "filter", "REQUIRED", null);
        }

        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteFilterNode(writer, filter, 0);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WriteText(string? field, string? value)
    {
        ValidateField(field, "field");
        ValidateValue(value, "value");
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteTextNode(writer, field ?? string.Empty, value ?? string.Empty);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WriteSearchQuery(SearchRequest request)
    {
        ValidateRequestShape(request);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteSearchRoot(writer, request);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WriteFields(SearchFieldSelection selection)
    {
        ValidateFields(selection);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteFieldsValue(writer, selection);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WriteSorts(SearchSort[] sorts)
    {
        ValidateSorts(sorts);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteSortsValue(writer, sorts);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WriteOptions(CursorPage page, SearchSort[] sorts)
    {
        ValidateSorts(sorts);
        ValidateCursor(page, sorts);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteOptionsValue(writer, page);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static global::System.Collections.Generic.Dictionary<string, string?> WriteGetQuery(SearchRequest request, UsptoLimits limits)
    {
        ValidateRequest(request, limits, true);
        global::System.Collections.Generic.Dictionary<string, string?> query = new global::System.Collections.Generic.Dictionary<string, string?>();
        query.Add("q", WriteSearchQuery(request));
        if (request.FieldSelection.Fields.Length != 0)
        {
            query.Add("f", WriteFields(request.FieldSelection));
        }

        if (request.Sorts.Length != 0)
        {
            query.Add("s", WriteSorts(request.Sorts));
        }

        query.Add("o", WriteOptions(request.CursorPage, request.Sorts));
        ValidateGetSize(query, limits.MaximumUriCharacters);
        return query;
    }

    internal static global::System.Collections.Generic.Dictionary<string, string?> WritePatentGetQuery(PatentsViewQueryRequest request, UsptoLimits limits)
    {
        ValidateTypedRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = WriteGetQuery(request.Search, limits);
        query["o"] = WritePatentOptions(request.Search.CursorPage, request.Search.Sorts, request.PadPatentIdentifiers, request.ExcludeWithdrawnPatents);
        ValidateGetSize(query, limits.MaximumUriCharacters);
        return query;
    }

    internal static global::System.Collections.Generic.Dictionary<string, string?> WritePatentLinkedGetQuery(PatentsViewQueryRequest request, UsptoLimits limits)
    {
        ValidateTypedRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = WriteGetQuery(request.Search, limits);
        query["o"] = WritePatentLinkedOptions(request.Search.CursorPage, request.Search.Sorts, request.PadPatentIdentifiers);
        ValidateGetSize(query, limits.MaximumUriCharacters);
        return query;
    }

    internal static string WritePostBody(SearchRequest request, UsptoLimits limits)
    {
        ValidateRequest(request, limits, false);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("q");
            WriteSearchRoot(writer, request);
            if (request.FieldSelection.Fields.Length != 0)
            {
                writer.WritePropertyName("f");
                WriteFieldsValue(writer, request.FieldSelection);
            }

            if (request.Sorts.Length != 0)
            {
                writer.WritePropertyName("s");
                WriteSortsValue(writer, request.Sorts);
            }

            writer.WritePropertyName("o");
            WriteOptionsValue(writer, request.CursorPage);
            writer.WriteEndObject();
            writer.Flush();
        }

        if (output.Length > limits.MaximumRequestBytes)
        {
            throw new ValidationException("The PatentsView search body exceeds the configured request-size limit.", "MaximumRequestBytes", "REQUEST_BODY_TOO_LARGE", null);
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WritePatentPostBody(PatentsViewQueryRequest request, UsptoLimits limits)
    {
        ValidateTypedRequest(request);
        ValidateRequest(request.Search, limits, false);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("q");
            WriteSearchRoot(writer, request.Search);
            if (request.Search.FieldSelection.Fields.Length != 0)
            {
                writer.WritePropertyName("f");
                WriteFieldsValue(writer, request.Search.FieldSelection);
            }

            if (request.Search.Sorts.Length != 0)
            {
                writer.WritePropertyName("s");
                WriteSortsValue(writer, request.Search.Sorts);
            }

            writer.WritePropertyName("o");
            WritePatentOptionsValue(writer, request.Search.CursorPage, request.PadPatentIdentifiers, request.ExcludeWithdrawnPatents);
            writer.WriteEndObject();
            writer.Flush();
        }

        if (output.Length > limits.MaximumRequestBytes)
        {
            throw new ValidationException("The PatentsView search body exceeds the configured request-size limit.", "MaximumRequestBytes", "REQUEST_BODY_TOO_LARGE", null);
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WritePatentLinkedPostBody(PatentsViewQueryRequest request, UsptoLimits limits)
    {
        ValidateTypedRequest(request);
        ValidateRequest(request.Search, limits, false);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("q");
            WriteSearchRoot(writer, request.Search);
            if (request.Search.FieldSelection.Fields.Length != 0)
            {
                writer.WritePropertyName("f");
                WriteFieldsValue(writer, request.Search.FieldSelection);
            }

            if (request.Search.Sorts.Length != 0)
            {
                writer.WritePropertyName("s");
                WriteSortsValue(writer, request.Search.Sorts);
            }

            writer.WritePropertyName("o");
            WritePatentLinkedOptionsValue(writer, request.Search.CursorPage, request.PadPatentIdentifiers);
            writer.WriteEndObject();
            writer.Flush();
        }

        if (output.Length > limits.MaximumRequestBytes)
        {
            throw new ValidationException("The PatentsView search body exceeds the configured request-size limit.", "MaximumRequestBytes", "REQUEST_BODY_TOO_LARGE", null);
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static void WriteFilterNode(global::System.Text.Json.Utf8JsonWriter writer, SearchFilter filter, int depth)
    {
        if (filter == null)
        {
            throw new ValidationException("A PatentsView query predicate is required.", "filter", "REQUIRED", null);
        }

        if (depth >= MaximumPredicateDepth)
        {
            throw new ValidationException("The PatentsView query tree exceeds the supported depth.", "filter", "PREDICATE_DEPTH_EXCEEDED", null);
        }

        switch (filter.Operator)
        {
            case SearchOperator.Equal:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteComparisonNode(writer, "_eq", filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.NotEqual:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteComparisonNode(writer, "_neq", filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.LessThan:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteComparisonNode(writer, "_lt", filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.LessThanOrEqual:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteComparisonNode(writer, "_lte", filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.GreaterThan:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteComparisonNode(writer, "_gt", filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.GreaterThanOrEqual:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteComparisonNode(writer, "_gte", filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.Contains:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                WriteTextNode(writer, filter.Field ?? string.Empty, filter.Value ?? string.Empty);
                break;
            case SearchOperator.In:
                ValidateLeaf(filter);
                ValidateSetValues(filter);
                WriteArrayNode(writer, filter);
                break;
            case SearchOperator.And:
                WriteLogicalNode(writer, filter, "_and", 2, depth);
                break;
            case SearchOperator.Or:
                WriteLogicalNode(writer, filter, "_or", 2, depth);
                break;
            case SearchOperator.Not:
                WriteLogicalNode(writer, filter, "_not", 1, depth);
                break;
            default:
                throw new ValidationException("The PatentsView predicate has no supported operator.", "filter.Operator", "UNSUPPORTED_SEARCH_OPERATOR", null);
        }
    }

    internal static void WriteComparisonNode(global::System.Text.Json.Utf8JsonWriter writer, string operatorName, string field, string value)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(operatorName);
        writer.WriteStartObject();
        writer.WriteString(field, value);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    internal static void WriteTextNode(global::System.Text.Json.Utf8JsonWriter writer, string field, string value)
    {
        WriteComparisonNode(writer, "_text_all", field, value);
    }

    internal static void WriteArrayNode(global::System.Text.Json.Utf8JsonWriter writer, SearchFilter filter)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(filter.Field ?? string.Empty);
        writer.WriteStartArray();
        int index;
        for (index = 0; index < filter.Values.Length; index += 1)
        {
            writer.WriteStringValue(filter.Values[index]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WriteLogicalNode(global::System.Text.Json.Utf8JsonWriter writer, SearchFilter filter, string operatorName, int minimumChildren, int depth)
    {
        ValidateLogical(filter, operatorName, minimumChildren);
        writer.WriteStartObject();
        writer.WritePropertyName(operatorName);
        if (operatorName == "_not")
        {
            WriteFilterNode(writer, filter.Children[0], depth + 1);
        }
        else
        {
            writer.WriteStartArray();
            int index;
            for (index = 0; index < filter.Children.Length; index += 1)
            {
                WriteFilterNode(writer, filter.Children[index], depth + 1);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    internal static void WriteSearchRoot(global::System.Text.Json.Utf8JsonWriter writer, SearchRequest request)
    {
        int clauseCount = request.Filters.Length;
        int rangeIndex;
        for (rangeIndex = 0; rangeIndex < request.Ranges.Length; rangeIndex += 1)
        {
            SearchRange range = request.Ranges[rangeIndex];
            if (range == null)
            {
                throw new ValidationException("PatentsView ranges cannot contain null values.", "Ranges", "REQUIRED", null);
            }

            ValidateRange(range);
            clauseCount += 1;
        }

        if (clauseCount == 0)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        if (clauseCount == 1)
        {
            if (request.Filters.Length == 1)
            {
                WriteFilterNode(writer, request.Filters[0], 0);
            }
            else
            {
                WriteRangeNode(writer, request.Ranges[0]);
            }

            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("_and");
        writer.WriteStartArray();
        int filterIndex;
        for (filterIndex = 0; filterIndex < request.Filters.Length; filterIndex += 1)
        {
            WriteFilterNode(writer, request.Filters[filterIndex], 0);
        }

        for (rangeIndex = 0; rangeIndex < request.Ranges.Length; rangeIndex += 1)
        {
            WriteRangeNode(writer, request.Ranges[rangeIndex]);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WriteRangeNode(global::System.Text.Json.Utf8JsonWriter writer, SearchRange range)
    {
        ValidateRange(range);
        bool hasLower = range.LowerValue != null;
        bool hasUpper = range.UpperValue != null;
        if (hasLower && hasUpper)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("_and");
            writer.WriteStartArray();
            string lowerOperator = "_gt";
            if (range.IncludeLower)
            {
                lowerOperator = "_gte";
            }

            WriteComparisonNode(writer, lowerOperator, range.Field ?? string.Empty, range.LowerValue ?? string.Empty);
            string upperOperator = "_lt";
            if (range.IncludeUpper)
            {
                upperOperator = "_lte";
            }

            WriteComparisonNode(writer, upperOperator, range.Field ?? string.Empty, range.UpperValue ?? string.Empty);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        else if (hasLower)
        {
            string lowerOperator = "_gt";
            if (range.IncludeLower)
            {
                lowerOperator = "_gte";
            }

            WriteComparisonNode(writer, lowerOperator, range.Field ?? string.Empty, range.LowerValue ?? string.Empty);
        }
        else
        {
            string upperOperator = "_lt";
            if (range.IncludeUpper)
            {
                upperOperator = "_lte";
            }

            WriteComparisonNode(writer, upperOperator, range.Field ?? string.Empty, range.UpperValue ?? string.Empty);
        }
    }

    internal static void WriteFieldsValue(global::System.Text.Json.Utf8JsonWriter writer, SearchFieldSelection selection)
    {
        writer.WriteStartArray();
        int index;
        for (index = 0; index < selection.Fields.Length; index += 1)
        {
            writer.WriteStringValue(selection.Fields[index]);
        }

        writer.WriteEndArray();
    }

    internal static void WriteSortsValue(global::System.Text.Json.Utf8JsonWriter writer, SearchSort[] sorts)
    {
        writer.WriteStartArray();
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            writer.WriteStartObject();
            string direction = "asc";
            if (sorts[index].Direction == SortDirection.Descending)
            {
                direction = "desc";
            }

            writer.WriteString(sorts[index].Field ?? string.Empty, direction);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    internal static void WriteOptionsValue(global::System.Text.Json.Utf8JsonWriter writer, CursorPage page)
    {
        writer.WriteStartObject();
        writer.WriteNumber("size", page.Size);
        if (page.AfterValues.Length == 1)
        {
            writer.WriteString("after", page.AfterValues[0]);
        }
        else if (page.AfterValues.Length > 1)
        {
            writer.WritePropertyName("after");
            writer.WriteStartArray();
            int index;
            for (index = 0; index < page.AfterValues.Length; index += 1)
            {
                writer.WriteStringValue(page.AfterValues[index]);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    internal static string WritePatentOptions(CursorPage page, SearchSort[] sorts, bool padPatentIdentifiers, bool excludeWithdrawnPatents)
    {
        ValidateSorts(sorts);
        ValidateCursor(page, sorts);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WritePatentOptionsValue(writer, page, padPatentIdentifiers, excludeWithdrawnPatents);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static string WritePatentLinkedOptions(CursorPage page, SearchSort[] sorts, bool padPatentIdentifiers)
    {
        ValidateSorts(sorts);
        ValidateCursor(page, sorts);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WritePatentLinkedOptionsValue(writer, page, padPatentIdentifiers);
            writer.Flush();
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static void WritePatentOptionsValue(global::System.Text.Json.Utf8JsonWriter writer, CursorPage page, bool padPatentIdentifiers, bool excludeWithdrawnPatents)
    {
        writer.WriteStartObject();
        writer.WriteNumber("size", page.Size);
        if (page.AfterValues.Length == 1)
        {
            writer.WriteString("after", page.AfterValues[0]);
        }
        else if (page.AfterValues.Length > 1)
        {
            writer.WritePropertyName("after");
            writer.WriteStartArray();
            int index;
            for (index = 0; index < page.AfterValues.Length; index += 1)
            {
                writer.WriteStringValue(page.AfterValues[index]);
            }

            writer.WriteEndArray();
        }

        writer.WriteBoolean("pad_patent_id", padPatentIdentifiers);
        writer.WriteBoolean("exclude_withdrawn", excludeWithdrawnPatents);
        writer.WriteEndObject();
    }

    internal static void WritePatentLinkedOptionsValue(global::System.Text.Json.Utf8JsonWriter writer, CursorPage page, bool padPatentIdentifiers)
    {
        writer.WriteStartObject();
        writer.WriteNumber("size", page.Size);
        if (page.AfterValues.Length == 1)
        {
            writer.WriteString("after", page.AfterValues[0]);
        }
        else if (page.AfterValues.Length > 1)
        {
            writer.WritePropertyName("after");
            writer.WriteStartArray();
            int index;
            for (index = 0; index < page.AfterValues.Length; index += 1)
            {
                writer.WriteStringValue(page.AfterValues[index]);
            }

            writer.WriteEndArray();
        }

        writer.WriteBoolean("pad_patent_id", padPatentIdentifiers);
        writer.WriteEndObject();
    }

    internal static void ValidateTypedRequest(PatentsViewQueryRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A typed PatentsView query request is required.", "request", "REQUIRED", null);
        }

        if (request.Search == null)
        {
            throw new ValidationException("A PatentsView search request is required.", "Search", "REQUIRED", null);
        }
    }

    internal static void ValidateRequest(SearchRequest request, UsptoLimits limits, bool isGet)
    {
        ValidateRequestShape(request);
        if (limits == null)
        {
            throw new ValidationException("PatentsView request limits are required.", "limits", "REQUIRED", null);
        }

        if (isGet && limits.MaximumUriCharacters <= 0)
        {
            throw new ValidationException("The maximum URI length must be positive.", "MaximumUriCharacters", "INVALID_LIMIT", null);
        }

        if (!isGet && limits.MaximumRequestBytes <= 0L)
        {
            throw new ValidationException("The maximum request body length must be positive.", "MaximumRequestBytes", "INVALID_LIMIT", null);
        }

        if (!request.UseCursorPage)
        {
            throw new ValidationException("PatentsView searches require cursor pagination.", "UseCursorPage", "CURSOR_PAGE_REQUIRED", null);
        }

        ValidateCursor(request.CursorPage, request.Sorts);
        ValidateFields(request.FieldSelection);
        ValidateSorts(request.Sorts);
    }

    internal static void ValidateRequestShape(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A PatentsView search request is required.", "request", "REQUIRED", null);
        }

        if (request.Query != null)
        {
            throw new ValidationException("PatentsView searches require structured query predicates.", "Query", "UNSUPPORTED_SEARCH_OPTION", null);
        }

        if (request.Filters == null)
        {
            throw new ValidationException("PatentsView query predicates are required.", "Filters", "REQUIRED", null);
        }

        if (request.Ranges == null)
        {
            throw new ValidationException("PatentsView ranges are required.", "Ranges", "REQUIRED", null);
        }

        if (request.Sorts == null)
        {
            throw new ValidationException("PatentsView sorts are required.", "Sorts", "REQUIRED", null);
        }

        if (request.FieldSelection == null)
        {
            throw new ValidationException("A PatentsView field selection is required.", "FieldSelection", "REQUIRED", null);
        }

        if (request.Facets == null || request.Facets.Length != 0)
        {
            throw new ValidationException("The PatentsView PatentSearch API does not support facets.", "Facets", "UNSUPPORTED_SEARCH_OPTION", null);
        }

        int index;
        for (index = 0; index < request.Filters.Length; index += 1)
        {
            if (request.Filters[index] == null)
            {
                throw new ValidationException("PatentsView predicates cannot contain null values.", "Filters", "REQUIRED", null);
            }
        }
    }

    internal static void ValidateCursor(CursorPage page, SearchSort[] sorts)
    {
        if (page == null)
        {
            throw new ValidationException("PatentsView cursor coordinates are required.", "CursorPage", "REQUIRED", null);
        }

        if (sorts == null)
        {
            throw new ValidationException("PatentsView sorts are required.", "Sorts", "REQUIRED", null);
        }

        if (page.Size <= 0 || page.Size > UsptoDefaults.PatentsViewMaximumPageSize)
        {
            throw new ValidationException("The PatentsView page size must be between one and 1,000 records.", "CursorPage.Size", "PAGE_SIZE_OUT_OF_RANGE", null);
        }

        if (page.AfterValues == null)
        {
            throw new ValidationException("PatentsView cursor values are required.", "CursorPage.AfterValues", "REQUIRED", null);
        }

        if (page.AfterValues.Length != 0 && page.AfterValues.Length != sorts.Length)
        {
            throw new ValidationException("PatentsView cursor values must match the number and order of sort fields.", "CursorPage.AfterValues", "CURSOR_SORT_MISMATCH", null);
        }

        int index;
        for (index = 0; index < page.AfterValues.Length; index += 1)
        {
            if (global::System.String.IsNullOrWhiteSpace(page.AfterValues[index]))
            {
                throw new ValidationException("PatentsView cursor values cannot be blank.", "CursorPage.AfterValues", "INVALID_CURSOR", null);
            }

            ValidateValue(page.AfterValues[index], "CursorPage.AfterValues");
        }
    }

    internal static void ValidateFields(SearchFieldSelection selection)
    {
        if (selection == null)
        {
            throw new ValidationException("A PatentsView field selection is required.", "FieldSelection", "REQUIRED", null);
        }

        if (selection.Fields == null)
        {
            throw new ValidationException("PatentsView selected fields are required.", "FieldSelection.Fields", "REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> seen = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < selection.Fields.Length; index += 1)
        {
            ValidateField(selection.Fields[index], "FieldSelection.Fields");
            if (!seen.Add(selection.Fields[index]))
            {
                throw new ValidationException("PatentsView selected fields cannot be repeated.", "FieldSelection.Fields", "DUPLICATE_FIELD", null);
            }
        }
    }

    internal static void ValidateSorts(SearchSort[] sorts)
    {
        if (sorts == null)
        {
            throw new ValidationException("PatentsView sorts are required.", "Sorts", "REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> seen = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            SearchSort sort = sorts[index];
            if (sort == null)
            {
                throw new ValidationException("PatentsView sorts cannot contain null values.", "Sorts", "REQUIRED", null);
            }

            ValidateField(sort.Field, "Sorts.Field");
            if (sort.Direction != SortDirection.Ascending && sort.Direction != SortDirection.Descending)
            {
                throw new ValidationException("The PatentsView sort direction is invalid.", "Sorts.Direction", "INVALID_SORT", null);
            }

            if (!seen.Add(sort.Field ?? string.Empty))
            {
                throw new ValidationException("PatentsView sort fields cannot be repeated.", "Sorts.Field", "DUPLICATE_SORT", null);
            }
        }
    }

    internal static void ValidateLeaf(SearchFilter filter)
    {
        ValidateField(filter.Field, "filter.Field");
        if (filter.Children == null || filter.Children.Length != 0)
        {
            throw new ValidationException("A PatentsView leaf predicate cannot contain children.", "filter.Children", "INVALID_PREDICATE_SHAPE", null);
        }
    }

    internal static void ValidateSingleValue(SearchFilter filter)
    {
        if (filter.Value == null || filter.Values == null || filter.Values.Length != 0)
        {
            throw new ValidationException("The PatentsView predicate requires one value.", "filter.Value", "INVALID_PREDICATE_SHAPE", null);
        }

        ValidateValue(filter.Value, "filter.Value");
    }

    internal static void ValidateSetValues(SearchFilter filter)
    {
        if (filter.Value != null || filter.Values == null || filter.Values.Length == 0)
        {
            throw new ValidationException("The PatentsView set predicate requires one or more values.", "filter.Values", "INVALID_PREDICATE_SHAPE", null);
        }

        int index;
        for (index = 0; index < filter.Values.Length; index += 1)
        {
            ValidateValue(filter.Values[index], "filter.Values");
        }
    }

    internal static void ValidateLogical(SearchFilter filter, string operatorName, int minimumChildren)
    {
        if (filter.Field != null || filter.Value != null || filter.Values == null || filter.Values.Length != 0 || filter.Children == null)
        {
            throw new ValidationException("The logical PatentsView predicate has an invalid shape.", "filter", "INVALID_PREDICATE_SHAPE", null);
        }

        if (filter.Children.Length < minimumChildren || operatorName == "_not" && filter.Children.Length != 1)
        {
            throw new ValidationException("The logical PatentsView predicate has an invalid child count.", "filter.Children", "INVALID_PREDICATE_CHILDREN", null);
        }

        int index;
        for (index = 0; index < filter.Children.Length; index += 1)
        {
            if (filter.Children[index] == null)
            {
                throw new ValidationException("PatentsView predicate children cannot contain null values.", "filter.Children", "REQUIRED", null);
            }
        }
    }

    internal static void ValidateRange(SearchRange range)
    {
        if (range == null)
        {
            throw new ValidationException("A PatentsView range is required.", "range", "REQUIRED", null);
        }

        ValidateField(range.Field, "range.Field");
        if (range.LowerValue == null && range.UpperValue == null)
        {
            throw new ValidationException("A PatentsView range requires at least one bound.", "range", "RANGE_BOUND_REQUIRED", null);
        }

        if (range.LowerValue != null)
        {
            ValidateValue(range.LowerValue, "range.LowerValue");
        }

        if (range.UpperValue != null)
        {
            ValidateValue(range.UpperValue, "range.UpperValue");
        }
    }

    internal static void ValidateField(string? field, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(field))
        {
            throw new ValidationException("A PatentsView field name is required.", fieldName, "REQUIRED", null);
        }

        bool previousWasDot = true;
        int index;
        for (index = 0; index < field.Length; index += 1)
        {
            char character = field[index];
            bool isLetter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            bool allowed = isLetter || isDigit || character == '_' || character == '.';
            if (!allowed || character == '.' && previousWasDot)
            {
                throw new ValidationException("The PatentsView field name contains unsupported characters.", fieldName, "INVALID_FIELD_NAME", null);
            }

            previousWasDot = character == '.';
        }

        if (previousWasDot)
        {
            throw new ValidationException("The PatentsView field name contains an empty path segment.", fieldName, "INVALID_FIELD_NAME", null);
        }
    }

    internal static void ValidateValue(string? value, string fieldName)
    {
        if (value == null || value.Length == 0)
        {
            throw new ValidationException("A PatentsView query value is required.", fieldName, "REQUIRED", null);
        }

        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            if (global::System.Char.IsControl(value[index]))
            {
                throw new ValidationException("The PatentsView query value contains a control character.", fieldName, "INVALID_QUERY_VALUE", null);
            }
        }
    }

    internal static void ValidateGetSize(global::System.Collections.Generic.Dictionary<string, string?> query, int maximumCharacters)
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
            throw new ValidationException("The encoded PatentsView query exceeds the configured URI-size limit.", "MaximumUriCharacters", "REQUEST_URI_TOO_LARGE", null);
        }
    }

    internal static void ValidateGetRequestUri(global::System.Uri? uri, UsptoLimits limits)
    {
        if (uri == null)
        {
            throw new ValidationException("An encoded PatentsView request URI is required.", "uri", "REQUIRED", null);
        }

        if (!uri.IsAbsoluteUri)
        {
            throw new ValidationException("The encoded PatentsView request URI must be absolute.", "uri", "ABSOLUTE_URI_REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("PatentsView request limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumUriCharacters <= 0)
        {
            throw new ValidationException("The maximum URI length must be positive.", "MaximumUriCharacters", "INVALID_LIMIT", null);
        }

        if (uri.OriginalString.Length > limits.MaximumUriCharacters)
        {
            throw new ValidationException("The encoded PatentsView request URI exceeds the configured size limit.", "MaximumUriCharacters", "REQUEST_URI_TOO_LARGE", null);
        }
    }
}
