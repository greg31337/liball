namespace uspto;

internal static class OdpQueryWriter
{
    internal const int MaximumPredicateDepth = 64;

    internal static string EscapeSimplifiedQueryLiteral(string? value)
    {
        ValidateLiteral(value, "value");
        string literal = value ?? string.Empty;
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(literal.Length);
        int index;
        for (index = 0; index < literal.Length; index += 1)
        {
            char character = literal[index];
            if (IsReservedQueryCharacter(character) || global::System.Char.IsWhiteSpace(character))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    internal static string WriteText(string? value)
    {
        return EscapeSimplifiedQueryLiteral(value);
    }

    internal static string WriteEquality(string? field, string? value)
    {
        ValidateField(field, "field");
        string safeField = field ?? string.Empty;
        return safeField + ":\"" + EscapeQuotedLiteral(value, "value") + "\"";
    }

    internal static string WriteComparison(string? field, SearchOperator searchOperator, string? value)
    {
        ValidateField(field, "field");
        string safeField = field ?? string.Empty;
        string comparison;
        switch (searchOperator)
        {
            case SearchOperator.LessThan:
                comparison = "<";
                break;
            case SearchOperator.LessThanOrEqual:
                comparison = "<=";
                break;
            case SearchOperator.GreaterThan:
                comparison = ">";
                break;
            case SearchOperator.GreaterThanOrEqual:
                comparison = ">=";
                break;
            default:
                throw new ValidationException("The search operator is not an ODP comparison operator.", "searchOperator", "UNSUPPORTED_SEARCH_OPERATOR", null);
        }

        return safeField + ":" + comparison + EscapeSimplifiedQueryLiteral(value);
    }

    internal static string WritePredicate(SearchFilter filter)
    {
        if (filter == null)
        {
            throw new ValidationException("A search predicate is required.", "filter", "REQUIRED", null);
        }

        return WritePredicateNode(filter, 0);
    }

    internal static string WritePredicates(SearchFilter[] filters)
    {
        if (filters == null)
        {
            throw new ValidationException("Search predicates are required.", "filters", "REQUIRED", null);
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < filters.Length; index += 1)
        {
            if (filters[index] == null)
            {
                throw new ValidationException("Search predicates cannot contain null values.", "filters", "REQUIRED", null);
            }

            if (index != 0)
            {
                builder.Append(" AND ");
            }

            builder.Append(WritePredicateNode(filters[index], 0));
        }

        return builder.ToString();
    }

    internal static string WriteExactFilter(SearchFilter filter)
    {
        if (filter == null)
        {
            throw new ValidationException("An ODP exact filter is required.", "filter", "REQUIRED", null);
        }

        ValidateField(filter.Field, "filter.Field");
        ValidateNoChildren(filter);
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        builder.Append(filter.Field);
        builder.Append(' ');
        if (filter.Operator == SearchOperator.Equal)
        {
            ValidateSingleValueShape(filter);
            builder.Append(ValidateFilterValue(filter.Value, "filter.Value"));
        }
        else if (filter.Operator == SearchOperator.In)
        {
            ValidateSetValueShape(filter);
            int index;
            for (index = 0; index < filter.Values.Length; index += 1)
            {
                if (index != 0)
                {
                    builder.Append(',');
                }

                builder.Append(ValidateFilterValue(filter.Values[index], "filter.Values"));
            }
        }
        else
        {
            throw new ValidationException("ODP exact filters support only equality and set membership.", "filter.Operator", "UNSUPPORTED_FILTER_OPERATOR", null);
        }

        return builder.ToString();
    }

    internal static string WriteRangeQuery(SearchRange range)
    {
        ValidateRange(range);
        string lower = "*";
        string upper = "*";
        if (range.LowerValue != null)
        {
            lower = EscapeSimplifiedQueryLiteral(range.LowerValue);
        }

        if (range.UpperValue != null)
        {
            upper = EscapeSimplifiedQueryLiteral(range.UpperValue);
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

    internal static string WriteRangeFilter(SearchRange range)
    {
        ValidateRange(range);
        if (!range.IncludeLower || !range.IncludeUpper)
        {
            throw new ValidationException("ODP range filters cannot represent exclusive bounds.", "range", "UNSUPPORTED_RANGE_INCLUSIVITY", null);
        }

        if (range.LowerValue == null || range.UpperValue == null)
        {
            throw new ValidationException("ODP range filters require both bounds.", "range", "RANGE_BOUND_REQUIRED", null);
        }

        string lower = ValidateRangeFilterValue(range.LowerValue, "range.LowerValue");
        string upper = ValidateRangeFilterValue(range.UpperValue, "range.UpperValue");
        return range.Field + " " + lower + ":" + upper;
    }

    internal static string WriteSelectedFields(SearchFieldSelection selection)
    {
        if (selection == null)
        {
            throw new ValidationException("An ODP field selection is required.", "selection", "REQUIRED", null);
        }

        return WriteFieldList(selection.Fields, "selection.Fields");
    }

    internal static string WriteFacets(SearchFacetRequest[] facets)
    {
        if (facets == null)
        {
            throw new ValidationException("ODP facet requests are required.", "facets", "REQUIRED", null);
        }

        string[] fields = new string[facets.Length];
        int index;
        for (index = 0; index < facets.Length; index += 1)
        {
            SearchFacetRequest facet = facets[index];
            if (facet == null)
            {
                throw new ValidationException("ODP facet requests cannot contain null values.", "facets", "REQUIRED", null);
            }

            if (facet.HasLimit || facet.Prefix != null || facet.IncludeMissing)
            {
                throw new ValidationException("The ODP facet request contains unsupported options.", "facets", "UNSUPPORTED_FACET_OPTION", null);
            }

            ValidateField(facet.Field, "facets.Field");
            fields[index] = facet.Field ?? string.Empty;
        }

        return WriteFieldList(fields, "facets.Field");
    }

    internal static string WriteSorts(SearchSort[] sorts)
    {
        if (sorts == null)
        {
            throw new ValidationException("ODP sort requests are required.", "sorts", "REQUIRED", null);
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        global::System.Collections.Generic.HashSet<string> seen = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < sorts.Length; index += 1)
        {
            SearchSort sort = sorts[index];
            if (sort == null)
            {
                throw new ValidationException("ODP sort requests cannot contain null values.", "sorts", "REQUIRED", null);
            }

            ValidateField(sort.Field, "sorts.Field");
            string sortField = sort.Field ?? string.Empty;
            if (!seen.Add(sortField))
            {
                throw new ValidationException("ODP sort fields must be unique.", "sorts.Field", "DUPLICATE_FIELD", null);
            }

            string direction;
            switch (sort.Direction)
            {
                case SortDirection.Ascending:
                    direction = "asc";
                    break;
                case SortDirection.Descending:
                    direction = "desc";
                    break;
                default:
                    throw new ValidationException("The ODP sort direction is invalid.", "sorts.Direction", "INVALID_SORT_DIRECTION", null);
            }

            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append(sortField);
            builder.Append(' ');
            builder.Append(direction);
        }

        return builder.ToString();
    }

    internal static string WritePredicateNode(SearchFilter filter, int depth)
    {
        if (depth >= MaximumPredicateDepth)
        {
            throw new ValidationException("The ODP predicate tree exceeds the supported depth.", "filter", "PREDICATE_DEPTH_EXCEEDED", null);
        }

        switch (filter.Operator)
        {
            case SearchOperator.Equal:
                ValidateLeaf(filter);
                ValidateSingleValueShape(filter);
                return WriteEquality(filter.Field, filter.Value);
            case SearchOperator.NotEqual:
                ValidateLeaf(filter);
                ValidateSingleValueShape(filter);
                return "(NOT " + WriteEquality(filter.Field, filter.Value) + ")";
            case SearchOperator.LessThan:
            case SearchOperator.LessThanOrEqual:
            case SearchOperator.GreaterThan:
            case SearchOperator.GreaterThanOrEqual:
                ValidateLeaf(filter);
                ValidateSingleValueShape(filter);
                return WriteComparison(filter.Field, filter.Operator, filter.Value);
            case SearchOperator.Contains:
                ValidateLeaf(filter);
                ValidateSingleValueShape(filter);
                return (filter.Field ?? string.Empty) + ":*" + EscapeSimplifiedQueryLiteral(filter.Value) + "*";
            case SearchOperator.In:
                ValidateLeaf(filter);
                ValidateSetValueShape(filter);
                return WriteSetPredicate(filter);
            case SearchOperator.And:
                return WriteLogicalPredicate(filter, "AND", 2, depth);
            case SearchOperator.Or:
                return WriteLogicalPredicate(filter, "OR", 2, depth);
            case SearchOperator.Not:
                return WriteLogicalPredicate(filter, "NOT", 1, depth);
            default:
                throw new ValidationException("The search predicate has no supported operator.", "filter.Operator", "UNSUPPORTED_SEARCH_OPERATOR", null);
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
            throw new ValidationException("The logical search predicate has an invalid shape.", "filter", "INVALID_PREDICATE_SHAPE", null);
        }

        if (filter.Children.Length < minimumChildren || (logicalOperator == "NOT" && filter.Children.Length != 1))
        {
            throw new ValidationException("The logical search predicate has an invalid child count.", "filter.Children", "INVALID_PREDICATE_CHILDREN", null);
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder("(");
        if (logicalOperator == "NOT")
        {
            if (filter.Children[0] == null)
            {
                throw new ValidationException("Search predicate children cannot contain null values.", "filter.Children", "REQUIRED", null);
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
                    throw new ValidationException("Search predicate children cannot contain null values.", "filter.Children", "REQUIRED", null);
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

    internal static void ValidateLeaf(SearchFilter filter)
    {
        ValidateField(filter.Field, "filter.Field");
        ValidateNoChildren(filter);
    }

    internal static void ValidateNoChildren(SearchFilter filter)
    {
        if (filter.Children == null || filter.Children.Length != 0)
        {
            throw new ValidationException("A leaf search predicate cannot contain children.", "filter.Children", "INVALID_PREDICATE_SHAPE", null);
        }
    }

    internal static void ValidateSingleValueShape(SearchFilter filter)
    {
        if (filter.Value == null || filter.Value.Length == 0 || filter.Values == null || filter.Values.Length != 0)
        {
            throw new ValidationException("The search predicate requires one value.", "filter.Value", "INVALID_PREDICATE_SHAPE", null);
        }

        ValidateLiteral(filter.Value, "filter.Value");
    }

    internal static void ValidateSetValueShape(SearchFilter filter)
    {
        if (filter.Value != null || filter.Values == null || filter.Values.Length == 0)
        {
            throw new ValidationException("The set search predicate requires one or more values.", "filter.Values", "INVALID_PREDICATE_SHAPE", null);
        }

        int index;
        for (index = 0; index < filter.Values.Length; index += 1)
        {
            ValidateLiteral(filter.Values[index], "filter.Values");
        }
    }

    internal static void ValidateRange(SearchRange range)
    {
        if (range == null)
        {
            throw new ValidationException("An ODP range is required.", "range", "REQUIRED", null);
        }

        ValidateField(range.Field, "range.Field");
        if (range.LowerValue == null && range.UpperValue == null)
        {
            throw new ValidationException("An ODP range requires at least one bound.", "range", "RANGE_BOUND_REQUIRED", null);
        }

        if (range.LowerValue != null)
        {
            ValidateLiteral(range.LowerValue, "range.LowerValue");
        }

        if (range.UpperValue != null)
        {
            ValidateLiteral(range.UpperValue, "range.UpperValue");
        }
    }

    internal static string WriteFieldList(string[] fields, string fieldName)
    {
        if (fields == null)
        {
            throw new ValidationException("ODP fields are required.", fieldName, "REQUIRED", null);
        }

        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        global::System.Collections.Generic.HashSet<string> seen = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            ValidateField(fields[index], fieldName);
            if (!seen.Add(fields[index]))
            {
                throw new ValidationException("ODP fields must be unique.", fieldName, "DUPLICATE_FIELD", null);
            }

            if (index != 0)
            {
                builder.Append(',');
            }

            builder.Append(fields[index]);
        }

        return builder.ToString();
    }

    internal static void ValidateField(string? field, string fieldName)
    {
        if (global::System.String.IsNullOrWhiteSpace(field))
        {
            throw new ValidationException("An ODP field name is required.", fieldName, "REQUIRED", null);
        }

        int index;
        bool previousWasDot = true;
        for (index = 0; index < field.Length; index += 1)
        {
            char character = field[index];
            bool isLetter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            bool isAllowed = isLetter || isDigit || character == '_' || character == '.';
            if (!isAllowed || character == '.' && previousWasDot)
            {
                throw new ValidationException("The ODP field name contains unsupported characters.", fieldName, "INVALID_FIELD_NAME", null);
            }

            previousWasDot = character == '.';
        }

        if (previousWasDot)
        {
            throw new ValidationException("The ODP field name contains an empty path segment.", fieldName, "INVALID_FIELD_NAME", null);
        }
    }

    internal static void ValidateLiteral(string? value, string fieldName)
    {
        if (value == null || value.Length == 0)
        {
            throw new ValidationException("An ODP query value is required.", fieldName, "REQUIRED", null);
        }

        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            if (global::System.Char.IsControl(value[index]))
            {
                throw new ValidationException("The ODP query value contains a control character.", fieldName, "INVALID_QUERY_LITERAL", null);
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

    internal static string ValidateFilterValue(string? value, string fieldName)
    {
        ValidateLiteral(value, fieldName);
        string safeValue = value ?? string.Empty;
        if (safeValue.IndexOf(',') >= 0)
        {
            throw new ValidationException("An ODP exact-filter value cannot contain a comma.", fieldName, "INVALID_FILTER_VALUE", null);
        }

        return safeValue;
    }

    internal static string ValidateRangeFilterValue(string value, string fieldName)
    {
        ValidateLiteral(value, fieldName);
        if (value.IndexOf(':') >= 0 || global::System.Char.IsWhiteSpace(value[0]) || global::System.Char.IsWhiteSpace(value[value.Length - 1]))
        {
            throw new ValidationException("An ODP range-filter bound contains an unsupported delimiter.", fieldName, "INVALID_RANGE_VALUE", null);
        }

        return value;
    }

    internal static bool IsReservedQueryCharacter(char character)
    {
        return character == '+' || character == '-' || character == '=' || character == '&' || character == '|' || character == '>' || character == '<' || character == '!' || character == '(' || character == ')' || character == '{' || character == '}' || character == '[' || character == ']' || character == '^' || character == '"' || character == '~' || character == '*' || character == '?' || character == ':' || character == '\\' || character == '/';
    }
}
