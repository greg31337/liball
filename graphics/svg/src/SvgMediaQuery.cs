namespace svg;

internal class SvgMediaQueryParserState
{
    string text;
    System.Collections.Generic.List<SvgCssToken> tokens;
    SvgSourceUri source;
    int baseOffset;
    int baseLine;
    int baseColumn;
    int maximumQueries;
    int maximumFeatures;
    int featureCount;
    bool limitExceeded;
    System.Collections.Generic.List<SvgDiagnostic> diagnostics;

    internal SvgMediaQueryParserState(string value, System.Collections.Generic.List<SvgCssToken> queryTokens, SvgSourceUri sourceUri, int offset, int line, int column, int maxQueries, int maxFeatures)
    {
        text = value;
        tokens = queryTokens;
        source = sourceUri;
        baseOffset = offset;
        baseLine = line;
        baseColumn = column;
        maximumQueries = maxQueries;
        maximumFeatures = maxFeatures;
        featureCount = 0;
        limitExceeded = false;
        diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }

    internal SvgMediaQueryParseResult Parse()
    {
        System.Collections.Generic.List<SvgMediaQuery> queries = new System.Collections.Generic.List<SvgMediaQuery>();
        if (tokens.Count == 0)
        {
            AddError("A media query list cannot be empty.", null);
        }
        else
        {
            int start = 0;
            int depth = 0;
            int index = 0;
            while (index < tokens.Count)
            {
                SvgCssToken token = tokens[index];
                if (token.Kind == SvgCssTokenKind.OpenParenthesis || token.Kind == SvgCssTokenKind.Function) depth = depth + 1;
                else if (token.Kind == SvgCssTokenKind.CloseParenthesis) depth = depth - 1;
                if (depth < 0)
                {
                    AddError("A media query contains an unmatched closing parenthesis.", TokenSpan(token));
                    depth = 0;
                }
                if (token.Kind == SvgCssTokenKind.Comma && depth == 0)
                {
                    if (queries.Count >= maximumQueries)
                    {
                        AddLimit("The media query count exceeds MaxRules.", TokenSpan(token));
                        break;
                    }
                    queries.Add(ParseQuery(start, index));
                    if (limitExceeded) break;
                    start = index + 1;
                }
                index = index + 1;
            }
            if (limitExceeded) return new SvgMediaQueryParseResult(false, null, diagnostics, tokens[index].StartOffset);
            if (depth != 0) AddError("A media query contains an unclosed parenthesis.", TokenSpan(tokens[tokens.Count - 1]));
            if (queries.Count >= maximumQueries)
            {
                AddLimit("The media query count exceeds MaxRules.", TokenSpan(tokens[tokens.Count - 1]));
                return new SvgMediaQueryParseResult(false, null, diagnostics, tokens[tokens.Count - 1].StartOffset);
            }
            queries.Add(ParseQuery(start, tokens.Count));
            if (limitExceeded) return new SvgMediaQueryParseResult(false, null, diagnostics, tokens[tokens.Count - 1].StartOffset);
        }
        SvgMediaQueryList list = new SvgMediaQueryList(queries, text, diagnostics);
        return new SvgMediaQueryParseResult(true, list, diagnostics, -1);
    }

    SvgMediaQuery ParseQuery(int start, int end)
    {
        SvgMediaQuery query = new SvgMediaQuery();
        if (start >= end)
        {
            AddError("A media query cannot be empty.", null);
            return query;
        }
        query.RawText = text.Substring(tokens[start].StartOffset, tokens[end - 1].EndOffset - tokens[start].StartOffset);
        query.Source = Span(tokens[start], tokens[end - 1]);
        int position = start;
        bool hasMediaType = false;
        if (IsIdent(position, end, "not"))
        {
            query.Modifier = SvgMediaQueryModifier.Not;
            position = position + 1;
        }
        else if (IsIdent(position, end, "only"))
        {
            query.Modifier = SvgMediaQueryModifier.Only;
            position = position + 1;
        }
        if (position < end && tokens[position].Kind == SvgCssTokenKind.Ident && !AsciiEquals(tokens[position].Value, "and"))
        {
            query.MediaType = LowerAscii(tokens[position].Value);
            hasMediaType = true;
            position = position + 1;
        }
        else if (query.Modifier == SvgMediaQueryModifier.Only)
        {
            return InvalidQuery(query, "The only modifier requires a media type.");
        }
        if (position == end)
        {
            query.IsValid = hasMediaType;
            if (!query.IsValid) AddError("A media query requires a media type or feature.", query.Source);
            return query;
        }
        if (hasMediaType)
        {
            if (!IsIdent(position, end, "and")) return InvalidQuery(query, "A media type must be followed by and before a feature.");
            position = position + 1;
        }
        while (position < end)
        {
            if (tokens[position].Kind != SvgCssTokenKind.OpenParenthesis) return InvalidQuery(query, "A media feature must be enclosed in parentheses.");
            int close = MatchingClose(position, end);
            if (close < 0) return InvalidQuery(query, "A media feature has an unclosed parenthesis.");
            SvgMediaFeature feature = ParseFeature(position + 1, close);
            if (featureCount >= maximumFeatures)
            {
                AddLimit("The media feature count exceeds MaxDeclarations.", feature.Source);
                return query;
            }
            query.Features.Add(feature);
            featureCount = featureCount + 1;
            position = close + 1;
            if (position < end)
            {
                if (!IsIdent(position, end, "and")) return InvalidQuery(query, "Media features must be joined with and.");
                position = position + 1;
                if (position == end) return InvalidQuery(query, "A trailing and requires another media feature.");
            }
        }
        query.IsValid = query.Features.Count > 0 && AllFeaturesValid(query.Features);
        return query;
    }

    SvgMediaFeature ParseFeature(int start, int end)
    {
        SvgMediaFeature feature = new SvgMediaFeature();
        if (start >= end)
        {
            AddError("A media feature cannot be empty.", null);
            return feature;
        }
        feature.Source = Span(tokens[start], tokens[end - 1]);
        int colon = FindKind(start, end, SvgCssTokenKind.Colon);
        if (colon >= 0)
        {
            if (colon != start + 1 || tokens[start].Kind != SvgCssTokenKind.Ident || colon + 1 >= end) return InvalidFeature(feature, "A media feature name and value are required around the colon.");
            SetFeatureName(feature, tokens[start].Value, true);
            if (!ParseFeatureValue(feature, colon + 1, end)) return InvalidFeature(feature, "The media feature value is invalid.");
            feature.ValueText = Slice(tokens[colon + 1], tokens[end - 1]);
            feature.IsValid = true;
            return feature;
        }
        if (end == start + 1 && tokens[start].Kind == SvgCssTokenKind.Ident)
        {
            SetFeatureName(feature, tokens[start].Value, false);
            feature.Comparison = SvgMediaComparison.Boolean;
            feature.IsValid = true;
            return feature;
        }
        return ParseRangeFeature(feature, start, end);
    }

    SvgMediaFeature ParseRangeFeature(SvgMediaFeature feature, int start, int end)
    {
        int featureIndex = -1;
        if (tokens[start].Kind == SvgCssTokenKind.Ident) featureIndex = start;
        else if (tokens[end - 1].Kind == SvgCssTokenKind.Ident) featureIndex = end - 1;
        if (featureIndex < 0) return InvalidFeature(feature, "A range media feature requires one feature name.");
        int operatorStart = start + 1;
        int operatorEnd = end - 2;
        int valueIndex = featureIndex == start ? end - 1 : start;
        if (operatorStart > operatorEnd || operatorEnd - operatorStart > 1) return InvalidFeature(feature, "A media range comparison operator is invalid.");
        string comparisonText = "";
        int index = operatorStart;
        while (index <= operatorEnd)
        {
            if (tokens[index].Kind != SvgCssTokenKind.Delimiter) return InvalidFeature(feature, "A media range comparison operator is invalid.");
            comparisonText = comparisonText + tokens[index].Value;
            index = index + 1;
        }
        SetFeatureName(feature, tokens[featureIndex].Value, false);
        SvgMediaComparison comparison = RangeComparison(comparisonText, featureIndex == start);
        if (comparison < SvgMediaComparison.Equal || comparison > SvgMediaComparison.MaximumExclusive) return InvalidFeature(feature, "A media range comparison operator is invalid.");
        feature.Comparison = comparison;
        if (!ParseFeatureValue(feature, valueIndex, valueIndex + 1)) return InvalidFeature(feature, "The media range value is invalid.");
        feature.ValueText = tokens[valueIndex].RawText;
        feature.IsValid = true;
        return feature;
    }

    static SvgMediaComparison RangeComparison(string value, bool featureFirst)
    {
        if (value == "=") return SvgMediaComparison.Equal;
        if (featureFirst)
        {
            if (value == ">=") return SvgMediaComparison.MinimumInclusive;
            if (value == ">") return SvgMediaComparison.MinimumExclusive;
            if (value == "<=") return SvgMediaComparison.MaximumInclusive;
            if (value == "<") return SvgMediaComparison.MaximumExclusive;
        }
        else
        {
            if (value == "<=") return SvgMediaComparison.MinimumInclusive;
            if (value == "<") return SvgMediaComparison.MinimumExclusive;
            if (value == ">=") return SvgMediaComparison.MaximumInclusive;
            if (value == ">") return SvgMediaComparison.MaximumExclusive;
        }
        return SvgMediaComparison.Boolean;
    }

    void SetFeatureName(SvgMediaFeature feature, string authoredName, bool legacyRange)
    {
        string name = LowerAscii(authoredName);
        if (legacyRange && name.StartsWith("min-", System.StringComparison.Ordinal))
        {
            feature.Comparison = SvgMediaComparison.MinimumInclusive;
            name = name.Substring(4);
        }
        else if (legacyRange && name.StartsWith("max-", System.StringComparison.Ordinal))
        {
            feature.Comparison = SvgMediaComparison.MaximumInclusive;
            name = name.Substring(4);
        }
        else feature.Comparison = SvgMediaComparison.Equal;
        feature.Name = name;
        if (name == "width") feature.Kind = SvgMediaFeatureKind.Width;
        else if (name == "height") feature.Kind = SvgMediaFeatureKind.Height;
        else if (name == "orientation") feature.Kind = SvgMediaFeatureKind.Orientation;
        else if (name == "resolution") feature.Kind = SvgMediaFeatureKind.Resolution;
        else if (name == "color") feature.Kind = SvgMediaFeatureKind.Color;
        else if (name == "monochrome") feature.Kind = SvgMediaFeatureKind.Monochrome;
        else if (name == "prefers-reduced-motion") feature.Kind = SvgMediaFeatureKind.ReducedMotion;
        else feature.Kind = SvgMediaFeatureKind.Unknown;
    }

    bool ParseFeatureValue(SvgMediaFeature feature, int start, int end)
    {
        if (feature.Kind == SvgMediaFeatureKind.Unknown) return start < end;
        if (end != start + 1) return false;
        SvgCssToken token = tokens[start];
        if (feature.Kind == SvgMediaFeatureKind.Width || feature.Kind == SvgMediaFeatureKind.Height)
        {
            if (token.Kind == SvgCssTokenKind.Number && token.NumberValue == 0)
            {
                feature.NumericValue = 0;
                feature.Unit = "px";
                return true;
            }
            if (token.Kind != SvgCssTokenKind.Dimension || token.NumberValue < 0 || !LengthUnit(token.Unit)) return false;
            feature.NumericValue = token.NumberValue;
            feature.Unit = LowerAscii(token.Unit);
            return true;
        }
        if (feature.Kind == SvgMediaFeatureKind.Resolution)
        {
            if (token.Kind == SvgCssTokenKind.Number && token.NumberValue == 0)
            {
                feature.NumericValue = 0;
                feature.Unit = "dpi";
                return true;
            }
            string unit = LowerAscii(token.Unit);
            if (token.Kind != SvgCssTokenKind.Dimension || token.NumberValue < 0 || unit != "dpi" && unit != "dpcm" && unit != "dppx" && unit != "x") return false;
            feature.NumericValue = token.NumberValue;
            feature.Unit = unit == "x" ? "dppx" : unit;
            return true;
        }
        if (feature.Kind == SvgMediaFeatureKind.Color || feature.Kind == SvgMediaFeatureKind.Monochrome)
        {
            if (token.Kind != SvgCssTokenKind.Number || token.NumberType != SvgCssNumberType.Integer || token.NumberValue < 0) return false;
            feature.NumericValue = token.NumberValue;
            return true;
        }
        if (token.Kind != SvgCssTokenKind.Ident) return false;
        feature.Keyword = LowerAscii(token.Value);
        if (feature.Kind == SvgMediaFeatureKind.Orientation) return feature.Keyword == "portrait" || feature.Keyword == "landscape";
        return feature.Keyword == "reduce" || feature.Keyword == "no-preference";
    }

    static bool LengthUnit(string unitText)
    {
        string unit = LowerAscii(unitText);
        return unit == "px" || unit == "in" || unit == "cm" || unit == "mm" || unit == "q" || unit == "pt" || unit == "pc" || unit == "em" || unit == "rem" || unit == "vw" || unit == "vh" || unit == "vmin" || unit == "vmax";
    }

    SvgMediaQuery InvalidQuery(SvgMediaQuery query, string message)
    {
        query.IsValid = false;
        AddError(message, query.Source);
        return query;
    }

    SvgMediaFeature InvalidFeature(SvgMediaFeature feature, string message)
    {
        feature.IsValid = false;
        AddError(message, feature.Source);
        return feature;
    }

    void AddError(string message, SvgSourceSpan? span)
    {
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, message, span));
    }

    void AddLimit(string message, SvgSourceSpan? span)
    {
        limitExceeded = true;
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, message, span));
    }

    int MatchingClose(int open, int end)
    {
        int depth = 0;
        int index = open;
        while (index < end)
        {
            SvgCssTokenKind kind = tokens[index].Kind;
            if (kind == SvgCssTokenKind.OpenParenthesis || kind == SvgCssTokenKind.Function) depth = depth + 1;
            else if (kind == SvgCssTokenKind.CloseParenthesis)
            {
                depth = depth - 1;
                if (depth == 0) return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int FindKind(int start, int end, SvgCssTokenKind kind)
    {
        int index = start;
        while (index < end)
        {
            if (tokens[index].Kind == kind) return index;
            index = index + 1;
        }
        return -1;
    }

    bool IsIdent(int index, int end, string value)
    {
        return index < end && tokens[index].Kind == SvgCssTokenKind.Ident && AsciiEquals(tokens[index].Value, value);
    }

    SvgSourceSpan TokenSpan(SvgCssToken token)
    {
        return Span(token, token);
    }

    SvgSourceSpan Span(SvgCssToken first, SvgCssToken last)
    {
        int startLine = baseLine + first.StartLine - 1;
        int startColumn = first.StartLine == 1 ? baseColumn + first.StartColumn - 1 : first.StartColumn;
        int endLine = baseLine + last.EndLine - 1;
        int endColumn = last.EndLine == 1 ? baseColumn + last.EndColumn - 1 : last.EndColumn;
        return new SvgSourceSpan(source, baseOffset + first.StartOffset, startLine, startColumn, baseOffset + last.EndOffset, endLine, endColumn);
    }

    string Slice(SvgCssToken first, SvgCssToken last)
    {
        return text.Substring(first.StartOffset, last.EndOffset - first.StartOffset);
    }

    static bool AllFeaturesValid(System.Collections.Generic.List<SvgMediaFeature> features)
    {
        int index = 0;
        while (index < features.Count)
        {
            if (!features[index].IsValid) return false;
            index = index + 1;
        }
        return true;
    }

    static string LowerAscii(string value)
    {
        return value.ToLowerInvariant();
    }

    static bool AsciiEquals(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }
}
