namespace uspto;

internal static class OfficeActionQueryWriter
{
    internal const string ApplicationNumberField = "app_id";
    internal const int MaximumPredicateDepth = 64;

    internal static string NormalizeApplicationNumber(string? identifier)
    {
        if (identifier == null)
        {
            throw new ValidationException("An Office Action application identifier is required.", "identifier", "REQUIRED", null);
        }

        PatentApplicationNumberResult application = PatentIdentifierParser.ParseApplicationNumber(identifier);
        if (!application.IsValid)
        {
            throw new ValidationException("The Office Action application identifier is invalid.", "identifier", application.FailureCode, null);
        }

        return application.CanonicalNumber;
    }

    internal static string EscapeLuceneLiteral(string? value)
    {
        ValidateLiteral(value, "value");
        string literal = value ?? string.Empty;
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(literal.Length);
        int index;
        for (index = 0; index < literal.Length; index += 1)
        {
            char character = literal[index];
            if (IsReservedLuceneCharacter(character) || global::System.Char.IsWhiteSpace(character))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    internal static string WriteText(string? value)
    {
        return EscapeLuceneLiteral(value);
    }

    internal static string WriteEquality(string? field, string? value)
    {
        ValidateField(field, "field");
        string safeField = field ?? string.Empty;
        return safeField + ":\"" + EscapeQuotedLiteral(value, "value") + "\"";
    }

    internal static string WriteApplicationCriteria(string? identifier)
    {
        return WriteEquality(ApplicationNumberField, NormalizeApplicationNumber(identifier));
    }

    internal static string WriteComparison(string? field, SearchOperator searchOperator, string? value)
    {
        ValidateField(field, "field");
        ValidateLiteral(value, "value");
        string safeField = field ?? string.Empty;
        string bound = EscapeLuceneLiteral(value);
        switch (searchOperator)
        {
            case SearchOperator.LessThan:
                return safeField + ":{* TO " + bound + "}";
            case SearchOperator.LessThanOrEqual:
                return safeField + ":[* TO " + bound + "]";
            case SearchOperator.GreaterThan:
                return safeField + ":{" + bound + " TO *}";
            case SearchOperator.GreaterThanOrEqual:
                return safeField + ":[" + bound + " TO *]";
            default:
                throw new ValidationException("The search operator is not a Lucene comparison operator.", "searchOperator", "UNSUPPORTED_SEARCH_OPERATOR", null);
        }
    }

    internal static string WriteRange(SearchRange range)
    {
        if (range == null)
        {
            throw new ValidationException("An Office Action range is required.", "range", "REQUIRED", null);
        }

        ValidateField(range.Field, "range.Field");
        if (range.LowerValue == null && range.UpperValue == null)
        {
            throw new ValidationException("An Office Action range requires at least one bound.", "range", "RANGE_BOUND_REQUIRED", null);
        }

        string lower = "*";
        string upper = "*";
        if (range.LowerValue != null)
        {
            lower = EscapeLuceneLiteral(range.LowerValue);
        }

        if (range.UpperValue != null)
        {
            upper = EscapeLuceneLiteral(range.UpperValue);
        }

        string opening = "{";
        if (range.IncludeLower)
        {
            opening = "[";
        }

        string closing = "}";
        if (range.IncludeUpper)
        {
            closing = "]";
        }

        return range.Field + ":" + opening + lower + " TO " + upper + closing;
    }

    internal static string WritePredicate(SearchFilter filter)
    {
        if (filter == null)
        {
            throw new ValidationException("An Office Action predicate is required.", "filter", "REQUIRED", null);
        }

        return WritePredicateNode(filter, 0);
    }

    internal static string WriteCriteria(SearchRequest request)
    {
        ValidateSearchRequest(request);
        global::System.Collections.Generic.List<string> fragments = new global::System.Collections.Generic.List<string>();
        int structuredCount = request.Filters.Length + request.Ranges.Length;
        if (request.Query != null)
        {
            string raw = request.Query;
            if (structuredCount != 0)
            {
                raw = "(" + raw + ")";
            }

            fragments.Add(raw);
        }

        int index;
        for (index = 0; index < request.Filters.Length; index += 1)
        {
            SearchFilter filter = request.Filters[index];
            if (filter == null)
            {
                throw new ValidationException("Office Action predicates cannot contain null values.", "Filters", "REQUIRED", null);
            }

            fragments.Add(WritePredicateNode(filter, 0));
        }

        for (index = 0; index < request.Ranges.Length; index += 1)
        {
            SearchRange range = request.Ranges[index];
            if (range == null)
            {
                throw new ValidationException("Office Action ranges cannot contain null values.", "Ranges", "REQUIRED", null);
            }

            fragments.Add(WriteRange(range));
        }

        if (fragments.Count == 0)
        {
            return "*:*";
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

    internal static global::System.Collections.Generic.Dictionary<string, string> WriteFormFields(SearchRequest request, UsptoLimits limits)
    {
        ValidateSearchRequest(request);
        ValidateLimits(limits);
        ValidatePage(request);
        global::System.Collections.Generic.Dictionary<string, string> fields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
        fields.Add("criteria", WriteCriteria(request));
        fields.Add("start", request.OffsetPage.Offset.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        fields.Add("rows", request.OffsetPage.Limit.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        ValidateFormSize(fields, limits.MaximumRequestBytes);
        return fields;
    }

    internal static global::System.Net.Http.HttpRequestMessage CreateFormRequest(global::System.Uri baseUri, string datasetName, string datasetVersion, SearchRequest request, UsptoLimits limits)
    {
        ValidatePathSegment(datasetName, "datasetName");
        ValidatePathSegment(datasetVersion, "datasetVersion");
        global::System.Collections.Generic.Dictionary<string, string> fields = WriteFormFields(request, limits);
        return RequestBuilder.CreateFormPostRequest(baseUri, new string[] { "api", "v1", "patent", "oa", datasetName, datasetVersion, "records" }, new global::System.Collections.Generic.Dictionary<string, string?>(), fields);
    }

    internal static string WritePredicateNode(SearchFilter filter, int depth)
    {
        if (depth >= MaximumPredicateDepth)
        {
            throw new ValidationException("The Office Action predicate tree exceeds the supported depth.", "filter", "PREDICATE_DEPTH_EXCEEDED", null);
        }

        switch (filter.Operator)
        {
            case SearchOperator.Equal:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                return WriteEquality(filter.Field, filter.Value);
            case SearchOperator.NotEqual:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                return "(NOT " + WriteEquality(filter.Field, filter.Value) + ")";
            case SearchOperator.LessThan:
            case SearchOperator.LessThanOrEqual:
            case SearchOperator.GreaterThan:
            case SearchOperator.GreaterThanOrEqual:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                return WriteComparison(filter.Field, filter.Operator, filter.Value);
            case SearchOperator.Contains:
                ValidateLeaf(filter);
                ValidateSingleValue(filter);
                return (filter.Field ?? string.Empty) + ":*" + EscapeLuceneLiteral(filter.Value) + "*";
            case SearchOperator.In:
                ValidateLeaf(filter);
                ValidateSetValues(filter);
                return WriteSetPredicate(filter);
            case SearchOperator.And:
                return WriteLogicalPredicate(filter, "AND", 2, depth);
            case SearchOperator.Or:
                return WriteLogicalPredicate(filter, "OR", 2, depth);
            case SearchOperator.Not:
                return WriteLogicalPredicate(filter, "NOT", 1, depth);
            default:
                throw new ValidationException("The Office Action predicate has no supported operator.", "filter.Operator", "UNSUPPORTED_SEARCH_OPERATOR", null);
        }
    }

    internal static string WriteSetPredicate(SearchFilter filter)
    {
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder("(");
        int index;
        for (index = 0; index < filter.Values.Length; index += 1)
        {
            if (index != 0)
            {
                builder.Append(" OR ");
            }

            builder.Append(WriteEquality(filter.Field, filter.Values[index]));
        }

        builder.Append(')');
        return builder.ToString();
    }

    internal static string WriteLogicalPredicate(SearchFilter filter, string logicalOperator, int minimumChildren, int depth)
    {
        if (filter.Field != null || filter.Value != null || filter.Values == null || filter.Values.Length != 0 || filter.Children == null)
        {
            throw new ValidationException("The logical Office Action predicate has an invalid shape.", "filter", "INVALID_PREDICATE_SHAPE", null);
        }

        if (filter.Children.Length < minimumChildren || logicalOperator == "NOT" && filter.Children.Length != 1)
        {
            throw new ValidationException("The logical Office Action predicate has an invalid child count.", "filter.Children", "INVALID_PREDICATE_CHILDREN", null);
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder("(");
        if (logicalOperator == "NOT")
        {
            if (filter.Children[0] == null)
            {
                throw new ValidationException("Office Action predicate children cannot contain null values.", "filter.Children", "REQUIRED", null);
            }

            builder.Append("NOT ");
            builder.Append(WritePredicateNode(filter.Children[0], depth + 1));
        }
        else
        {
            int index;
            for (index = 0; index < filter.Children.Length; index += 1)
            {
                if (filter.Children[index] == null)
                {
                    throw new ValidationException("Office Action predicate children cannot contain null values.", "filter.Children", "REQUIRED", null);
                }

                if (index != 0)
                {
                    builder.Append(' ');
                    builder.Append(logicalOperator);
                    builder.Append(' ');
                }

                builder.Append(WritePredicateNode(filter.Children[index], depth + 1));
            }
        }

        builder.Append(')');
        return builder.ToString();
    }

    internal static void ValidateSearchRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("An Office Action search request is required.", "request", "REQUIRED", null);
        }

        if (request.Filters == null)
        {
            throw new ValidationException("Office Action predicates are required.", "Filters", "REQUIRED", null);
        }

        if (request.Ranges == null)
        {
            throw new ValidationException("Office Action ranges are required.", "Ranges", "REQUIRED", null);
        }

        if (request.Sorts == null || request.Sorts.Length != 0)
        {
            throw new ValidationException("The Office Action DSAPI does not support caller-defined sorting.", "Sorts", "UNSUPPORTED_SEARCH_OPTION", null);
        }

        if (request.FieldSelection == null || request.FieldSelection.Fields == null || request.FieldSelection.Fields.Length != 0)
        {
            throw new ValidationException("The Office Action DSAPI does not support response field selection.", "FieldSelection", "UNSUPPORTED_SEARCH_OPTION", null);
        }

        if (request.Facets == null || request.Facets.Length != 0)
        {
            throw new ValidationException("The Office Action DSAPI does not support facets.", "Facets", "UNSUPPORTED_SEARCH_OPTION", null);
        }

        if (request.Query != null)
        {
            if (global::System.String.IsNullOrWhiteSpace(request.Query))
            {
                throw new ValidationException("The Office Action raw criteria cannot be blank.", "Query", "INVALID_QUERY_LITERAL", null);
            }

            int index;
            for (index = 0; index < request.Query.Length; index += 1)
            {
                if (global::System.Char.IsControl(request.Query[index]))
                {
                    throw new ValidationException("The Office Action raw criteria contains a control character.", "Query", "INVALID_QUERY_LITERAL", null);
                }
            }
        }
    }

    internal static void ValidatePage(SearchRequest request)
    {
        if (request.UseCursorPage)
        {
            throw new ValidationException("Office Action searches require offset pagination.", "UseCursorPage", "UNSUPPORTED_PAGE_MODE", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("Office Action offset pagination is required.", "OffsetPage", "REQUIRED", null);
        }

        if (request.OffsetPage.Offset < 0L || request.OffsetPage.Offset > global::System.Int32.MaxValue)
        {
            throw new ValidationException("The Office Action start value must fit a nonnegative 32-bit integer.", "OffsetPage.Offset", "INVALID_OFFSET", null);
        }

        if (request.OffsetPage.Limit <= 0)
        {
            throw new ValidationException("The Office Action rows value must be positive.", "OffsetPage.Limit", "INVALID_LIMIT", null);
        }
    }

    internal static void ValidateLimits(UsptoLimits limits)
    {
        if (limits == null)
        {
            throw new ValidationException("Office Action request limits are required.", "limits", "REQUIRED", null);
        }

        if (limits.MaximumRequestBytes <= 0L)
        {
            throw new ValidationException("The maximum request body length must be positive.", "MaximumRequestBytes", "INVALID_LIMIT", null);
        }
    }

    internal static void ValidateFormSize(global::System.Collections.Generic.Dictionary<string, string> fields, long maximumBytes)
    {
        global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<string, string>> pairs = CreateOrderedPairs(fields);
        using (global::System.Net.Http.FormUrlEncodedContent content = new global::System.Net.Http.FormUrlEncodedContent(pairs))
        {
            byte[] body = content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            if (body.LongLength > maximumBytes)
            {
                throw new ValidationException("The Office Action form exceeds the configured request-size limit.", "MaximumRequestBytes", "REQUEST_BODY_TOO_LARGE", null);
            }
        }
    }

    internal static global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<string, string>> CreateOrderedPairs(global::System.Collections.Generic.Dictionary<string, string> fields)
    {
        string[] keys = new string[fields.Count];
        fields.Keys.CopyTo(keys, 0);
        global::System.Array.Sort(keys, global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<string, string>> pairs = new global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<string, string>>();
        int index;
        for (index = 0; index < keys.Length; index += 1)
        {
            pairs.Add(new global::System.Collections.Generic.KeyValuePair<string, string>(keys[index], fields[keys[index]]));
        }

        return pairs;
    }

    internal static void ValidateLeaf(SearchFilter filter)
    {
        ValidateField(filter.Field, "filter.Field");
        if (filter.Children == null || filter.Children.Length != 0)
        {
            throw new ValidationException("An Office Action leaf predicate cannot contain children.", "filter.Children", "INVALID_PREDICATE_SHAPE", null);
        }
    }

    internal static void ValidateSingleValue(SearchFilter filter)
    {
        if (filter.Value == null || filter.Value.Length == 0 || filter.Values == null || filter.Values.Length != 0)
        {
            throw new ValidationException("The Office Action predicate requires one value.", "filter.Value", "INVALID_PREDICATE_SHAPE", null);
        }

        ValidateLiteral(filter.Value, "filter.Value");
    }

    internal static void ValidateSetValues(SearchFilter filter)
    {
        if (filter.Value != null || filter.Values == null || filter.Values.Length == 0)
        {
            throw new ValidationException("The Office Action set predicate requires one or more values.", "filter.Values", "INVALID_PREDICATE_SHAPE", null);
        }

        int index;
        for (index = 0; index < filter.Values.Length; index += 1)
        {
            ValidateLiteral(filter.Values[index], "filter.Values");
        }
    }

    internal static void ValidateField(string? field, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(field))
        {
            throw new ValidationException("An Office Action field name is required.", fieldName, "REQUIRED", null);
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
                throw new ValidationException("The Office Action field name contains unsupported characters.", fieldName, "INVALID_FIELD_NAME", null);
            }

            previousWasDot = character == '.';
        }

        if (previousWasDot)
        {
            throw new ValidationException("The Office Action field name contains an empty path segment.", fieldName, "INVALID_FIELD_NAME", null);
        }
    }

    internal static void ValidateLiteral(string? value, string fieldName)
    {
        if (value == null || value.Length == 0)
        {
            throw new ValidationException("An Office Action query value is required.", fieldName, "REQUIRED", null);
        }

        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            if (global::System.Char.IsControl(value[index]))
            {
                throw new ValidationException("The Office Action query value contains a control character.", fieldName, "INVALID_QUERY_LITERAL", null);
            }
        }
    }

    internal static string EscapeQuotedLiteral(string? value, string fieldName)
    {
        ValidateLiteral(value, fieldName);
        string literal = value ?? string.Empty;
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(literal.Length);
        int index;
        for (index = 0; index < literal.Length; index += 1)
        {
            char character = literal[index];
            if (character == '\\' || character == '"')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    internal static void ValidatePathSegment(string value, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException("An Office Action dataset path segment is required.", fieldName, "REQUIRED", null);
        }

        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            bool isLetter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isLetter && !isDigit && character != '_' && character != '-')
            {
                throw new ValidationException("The Office Action dataset path segment contains unsupported characters.", fieldName, "INVALID_PATH_SEGMENT", null);
            }
        }
    }

    internal static bool IsReservedLuceneCharacter(char character)
    {
        return character == '+' || character == '-' || character == '&' || character == '|' || character == '!' || character == '(' || character == ')' || character == '{' || character == '}' || character == '[' || character == ']' || character == '^' || character == '"' || character == '~' || character == '*' || character == '?' || character == ':' || character == '\\' || character == '/';
    }
}
