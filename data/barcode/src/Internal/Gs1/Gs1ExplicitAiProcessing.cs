namespace barcode;

internal class Gs1ExplicitValueScan
{
    internal int NextOffset;
    internal bool Error;
}

internal static class Gs1ExplicitAiProcessing
{
    internal static Gs1BracketedParseResult ParseBracketedAllowUnknown(
        string? source)
    {
        return ParseExplicit(source, false, true);
    }

    internal static Gs1BracketedParseResult ParseHri(
        string? source,
        bool allowUnknown)
    {
        return ParseExplicit(source, true, allowUnknown);
    }

    internal static Gs1BracketedBuildResult BuildBracketedAllowUnknown(
        Gs1Element[]? elements)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        if (elements == null || elements.Length == 0)
        {
            AddIssue(
                issues,
                "GS1_BRACKETED_ELEMENTS_MISSING",
                "At least one GS1 element is required to build bracketed text.",
                "Elements",
                -1,
                "one or more GS1 elements",
                elements == null ? "null" : "empty");
            return CreateBuildResult("", issues);
        }

        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null)
            {
                AddIssue(
                    issues,
                    "GS1_BRACKETED_ELEMENT_NULL",
                    "A GS1 element must not be null.",
                    "Elements",
                    text.Length,
                    "a GS1 element",
                    "null");
                index = index + 1;
                continue;
            }

            string identifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            if (!IsIdentifier(identifier))
            {
                AddIssue(
                    issues,
                    "GS1_BRACKETED_BUILD_AI_INVALID",
                    "The GS1 element does not contain a two- through four-digit Application Identifier.",
                    "ApplicationIdentifier",
                    text.Length,
                    "two through four digits",
                    identifier);
                index = index + 1;
                continue;
            }

            string value = element.Value == null ? "" : element.Value;
            int ambiguousOffset = FindNumericParenthesis(value);
            if (ambiguousOffset >= 0)
            {
                AddIssue(
                    issues,
                    "GS1_BRACKETED_AMBIGUOUS_DATA",
                    "The element data contains a numeric parenthesis sequence that would be interpreted as another Application Identifier.",
                    identifier,
                    text.Length + identifier.Length + 2 + ambiguousOffset,
                    "literal parentheses that do not begin with a digit",
                    value.Substring(ambiguousOffset));
            }

            ValidateUnknownValue(
                identifier,
                value,
                text.Length + identifier.Length + 2,
                issues);
            text.Append('(');
            text.Append(identifier);
            text.Append(')');
            text.Append(value);
            index = index + 1;
        }

        return CreateBuildResult(text.ToString(), issues);
    }

    internal static Gs1UnbracketedBuildResult BuildUnbracketedAllowUnknown(
        Gs1Element[]? elements,
        bool caretRepresentation)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        if (elements == null || elements.Length == 0)
        {
            AddIssue(
                issues,
                caretRepresentation
                    ? "GS1_UNBRACKETED_ELEMENTS_MISSING"
                    : "GS1_BARCODE_MESSAGE_ELEMENTS_MISSING",
                "At least one GS1 element is required.",
                "Elements",
                -1,
                "one or more GS1 elements",
                elements == null ? "null" : "empty");
            return CreateUnbracketedBuildResult(
                "",
                issues,
                true);
        }

        if (caretRepresentation)
        {
            text.Append(Gs1UnbracketedSyntax.FunctionOnePlaceholder);
        }

        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null)
            {
                AddIssue(
                    issues,
                    "GS1_PROCESS_ELEMENT_NULL",
                    "A GS1 element must not be null.",
                    "Elements",
                    text.Length,
                    "a GS1 element",
                    "null");
                index = index + 1;
                continue;
            }

            string identifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            if (!IsIdentifier(identifier))
            {
                AddIssue(
                    issues,
                    "GS1_PROCESS_AI_INVALID",
                    "The GS1 element does not contain a two- through four-digit Application Identifier.",
                    "ApplicationIdentifier",
                    text.Length,
                    "two through four digits",
                    identifier);
                index = index + 1;
                continue;
            }

            string value = element.Value == null ? "" : element.Value;
            ValidateTransportValue(
                identifier,
                value,
                text.Length + identifier.Length,
                caretRepresentation,
                issues);
            text.Append(identifier);
            text.Append(value);
            if (IsVariableLength(element) && index < elements.Length - 1)
            {
                text.Append(
                    caretRepresentation
                        ? Gs1UnbracketedSyntax.FunctionOnePlaceholder
                        : (char)Gs1UnbracketedSyntax.GroupSeparator);
            }

            index = index + 1;
        }

        return CreateUnbracketedBuildResult(
            text.ToString(),
            issues,
            true);
    }

    static Gs1BracketedParseResult ParseExplicit(
        string? source,
        bool hri,
        bool allowUnknown)
    {
        global::System.Collections.Generic.List<Gs1Element> elements =
            new global::System.Collections.Generic.List<Gs1Element>();
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (source == null || source.Length == 0)
        {
            AddIssue(
                issues,
                source == null
                    ? "GS1_EXPLICIT_NULL"
                    : "GS1_EXPLICIT_EMPTY",
                "The GS1 text must not be null or empty.",
                "Message",
                source == null ? -1 : 0,
                "one or more parenthesized GS1 elements",
                source == null ? "null" : "empty");
            return CreateParseResult(elements, issues);
        }

        int offset = 0;
        while (offset < source.Length)
        {
            offset = SkipHriPadding(source, offset, hri);
            if (offset >= source.Length)
            {
                break;
            }

            int openingOffset = FindOpeningOffset(source, offset, hri);
            if (openingOffset < 0)
            {
                AddIssue(
                    issues,
                    "GS1_EXPLICIT_AI_MISSING",
                    "A GS1 element must contain a parenthesized Application Identifier.",
                    "ApplicationIdentifier",
                    offset,
                    "'(AI)'",
                    ReadObserved(source, offset));
                break;
            }

            int closingOffset = source.IndexOf(')', openingOffset + 1);
            if (closingOffset < 0)
            {
                AddIssue(
                    issues,
                    "GS1_EXPLICIT_AI_UNTERMINATED",
                    "The parenthesized Application Identifier is not terminated.",
                    "ApplicationIdentifier",
                    openingOffset,
                    "a closing ')'",
                    source.Substring(openingOffset));
                break;
            }

            string identifier = source.Substring(
                openingOffset + 1,
                closingOffset - openingOffset - 1);
            if (!IsIdentifier(identifier))
            {
                AddIssue(
                    issues,
                    "GS1_EXPLICIT_AI_INVALID",
                    "A parenthesized Application Identifier must contain two through four digits.",
                    "ApplicationIdentifier",
                    openingOffset + 1,
                    "two through four digits",
                    identifier);
                break;
            }

            int valueOffset = closingOffset + 1;
            if (hri && valueOffset < source.Length
                && source[valueOffset] == ' ')
            {
                valueOffset = valueOffset + 1;
            }

            Gs1ExplicitValueScan scan = ScanValue(
                source,
                valueOffset,
                hri,
                issues);
            int nextOffset = scan.NextOffset;
            int valueEnd = scan.NextOffset;
            while (hri && valueEnd > valueOffset
                && (source[valueEnd - 1] == '\r'
                    || source[valueEnd - 1] == '\n'))
            {
                valueEnd = valueEnd - 1;
            }

            string value = source.Substring(
                valueOffset,
                valueEnd - valueOffset);
            Gs1AiDefinition? definition =
                Gs1AiResolver.ResolveDefinition(identifier);
            if (definition == null && !allowUnknown)
            {
                AddIssue(
                    issues,
                    "GS1_UNKNOWN_APPLICATION_IDENTIFIER",
                    "The Application Identifier is not registered in the GS1 dictionary.",
                    "ApplicationIdentifier",
                    openingOffset + 1,
                    "a registered GS1 Application Identifier",
                    identifier);
            }
            else if (definition == null)
            {
                Gs1Element unknown = new Gs1Element();
                unknown.ApplicationIdentifier = identifier;
                unknown.Value = value;
                unknown.Components = new string[] { value };
                unknown.Definition = null;
                unknown.SourceOffset = openingOffset + 1;
                unknown.SourceLength = nextOffset - openingOffset;
                unknown.VariableLength = true;
                elements.Add(unknown);
                ValidateUnknownValue(
                    identifier,
                    value,
                    valueOffset,
                    issues);
            }
            else
            {
                AddKnownElement(
                    elements,
                    issues,
                    identifier,
                    value,
                    openingOffset + 1,
                    valueOffset,
                    nextOffset - openingOffset);
            }

            offset = nextOffset;
            if (scan.Error)
            {
                break;
            }
        }

        SortIssues(issues);
        return CreateParseResult(elements, issues);
    }

    static void AddKnownElement(
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string identifier,
        string value,
        int identifierOffset,
        int valueOffset,
        int sourceLength)
    {
        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(identifier + value);
        if (parsed.Elements.Length > 0)
        {
            Gs1Element element = parsed.Elements[0];
            element.SourceOffset = identifierOffset;
            element.SourceLength = sourceLength;
            elements.Add(element);
        }

        int issueIndex = 0;
        while (issueIndex < parsed.Issues.Length)
        {
            BarcodeValidationIssue issue = parsed.Issues[issueIndex];
            if (issue.Offset >= 0)
            {
                issue.Offset = issue.Offset < identifier.Length
                    ? identifierOffset + issue.Offset
                    : valueOffset + issue.Offset - identifier.Length;
            }

            issues.Add(issue);
            issueIndex = issueIndex + 1;
        }
    }

    static void ValidateUnknownValue(
        string identifier,
        string value,
        int valueOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        if (Gs1AiResolver.ResolveDefinition(identifier) != null)
        {
            return;
        }

        if (value.Length == 0)
        {
            AddIssue(
                issues,
                "GS1_UNKNOWN_AI_VALUE_EMPTY",
                "An unknown Application Identifier must have a non-empty value.",
                identifier,
                valueOffset,
                "a non-empty printable value",
                "empty");
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 32 || character > 126)
            {
                AddIssue(
                    issues,
                    "GS1_UNKNOWN_AI_VALUE_INVALID",
                    "An unknown Application Identifier value must contain printable ASCII characters.",
                    identifier,
                    valueOffset + index,
                    "printable ASCII characters",
                    ((int)character).ToString());
                return;
            }

            index = index + 1;
        }
    }

    static void ValidateTransportValue(
        string identifier,
        string value,
        int valueOffset,
        bool caretRepresentation,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        ValidateUnknownValue(identifier, value, valueOffset, issues);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if ((caretRepresentation
                    && character == Gs1UnbracketedSyntax.FunctionOnePlaceholder)
                || character == (char)Gs1UnbracketedSyntax.GroupSeparator)
            {
                AddIssue(
                    issues,
                    "GS1_PROCESS_RESERVED_SEPARATOR",
                    "The element value contains a reserved separator character.",
                    identifier,
                    valueOffset + index,
                    "element data without separator characters",
                    ((int)character).ToString());
                return;
            }

            index = index + 1;
        }
    }

    static bool IsVariableLength(Gs1Element element)
    {
        Gs1AiDefinition? definition =
            Gs1AiResolver.ResolveDefinition(element.ApplicationIdentifier);
        return definition == null
            ? element.VariableLength
            : !definition.PredefinedLength;
    }

    static bool IsIdentifier(string identifier)
    {
        if (identifier.Length < 2 || identifier.Length > 4)
        {
            return false;
        }

        int index = 0;
        while (index < identifier.Length)
        {
            if (identifier[index] < '0' || identifier[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int SkipHriPadding(string source, int offset, bool hri)
    {
        if (!hri)
        {
            return offset;
        }

        while (offset < source.Length
            && (source[offset] == '\r' || source[offset] == '\n'))
        {
            offset = offset + 1;
        }

        return offset;
    }

    static int FindOpeningOffset(string source, int offset, bool hri)
    {
        if (source[offset] == '(')
        {
            return offset;
        }

        if (!hri)
        {
            return -1;
        }

        int openingOffset = source.IndexOf('(', offset);
        int newlineOffset = source.IndexOf('\n', offset);
        if (openingOffset < 0
            || (newlineOffset >= 0 && newlineOffset < openingOffset))
        {
            return -1;
        }

        return openingOffset;
    }

    static Gs1ExplicitValueScan ScanValue(
        string source,
        int valueOffset,
        bool hri,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1ExplicitValueScan result = new Gs1ExplicitValueScan();
        int offset = valueOffset;
        while (offset < source.Length)
        {
            if (hri && (source[offset] == '\r' || source[offset] == '\n'))
            {
                result.NextOffset = offset;
                return result;
            }

            if (source[offset] != '('
                || offset + 1 >= source.Length
                || source[offset + 1] < '0'
                || source[offset + 1] > '9')
            {
                offset = offset + 1;
                continue;
            }

            int digitEnd = offset + 1;
            while (digitEnd < source.Length
                && source[digitEnd] >= '0'
                && source[digitEnd] <= '9')
            {
                digitEnd = digitEnd + 1;
            }

            if (digitEnd >= source.Length)
            {
                AddIssue(
                    issues,
                    "GS1_EXPLICIT_AI_UNTERMINATED",
                    "The numeric parenthesis sequence in the element data is not terminated.",
                    "ApplicationIdentifier",
                    offset,
                    "a closing ')' after the Application Identifier",
                    source.Substring(offset));
                result.NextOffset = offset;
                result.Error = true;
                return result;
            }

            if (source[digitEnd] != ')')
            {
                AddIssue(
                    issues,
                    "GS1_EXPLICIT_AI_INVALID",
                    "A numeric parenthesis sequence contains an invalid Application Identifier.",
                    "ApplicationIdentifier",
                    digitEnd,
                    "two through four digits followed by ')'",
                    ReadObserved(source, offset));
                result.NextOffset = offset;
                result.Error = true;
                return result;
            }

            int identifierLength = digitEnd - offset - 1;
            if (identifierLength < 2 || identifierLength > 4)
            {
                AddIssue(
                    issues,
                    "GS1_EXPLICIT_AI_INVALID",
                    "A numeric parenthesis sequence must contain a two- through four-digit Application Identifier.",
                    "ApplicationIdentifier",
                    offset + 1,
                    "two through four digits",
                    source.Substring(offset + 1, identifierLength));
                result.NextOffset = offset;
                result.Error = true;
                return result;
            }

            result.NextOffset = offset;
            return result;
        }

        result.NextOffset = source.Length;
        return result;
    }

    static int FindNumericParenthesis(string value)
    {
        int index = 0;
        while (index < value.Length - 1)
        {
            if (value[index] == '('
                && value[index + 1] >= '0'
                && value[index + 1] <= '9')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static string ReadObserved(string source, int offset)
    {
        int length = source.Length - offset;
        if (length > 8)
        {
            length = 8;
        }

        return source.Substring(offset, length);
    }

    static byte[] ConvertTextToBytes(string source)
    {
        byte[] bytes = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            bytes[index] = (byte)source[index];
            index = index + 1;
        }

        return bytes;
    }

    static void AddIssue(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string code,
        string message,
        string field,
        int offset,
        string expected,
        string observed)
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Error,
            code,
            message,
            Gs1ElementStringParser.StandardName,
            field,
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static void SortIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 1;
        while (index < issues.Count)
        {
            BarcodeValidationIssue current = issues[index];
            int previousIndex = index - 1;
            while (previousIndex >= 0
                && issues[previousIndex].Offset > current.Offset)
            {
                issues[previousIndex + 1] = issues[previousIndex];
                previousIndex = previousIndex - 1;
            }

            issues[previousIndex + 1] = current;
            index = index + 1;
        }
    }

    static Gs1BracketedParseResult CreateParseResult(
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1BracketedParseResult result = new Gs1BracketedParseResult();
        result.Success = issues.Count == 0;
        result.Elements = elements.ToArray();
        result.Issues = issues.ToArray();
        return result;
    }

    static Gs1BracketedBuildResult CreateBuildResult(
        string text,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1BracketedBuildResult result = new Gs1BracketedBuildResult();
        result.Success = issues.Count == 0;
        result.Text = text;
        result.Issues = issues.ToArray();
        return result;
    }

    static Gs1UnbracketedBuildResult CreateUnbracketedBuildResult(
        string text,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        bool requiresLeadingFunctionOne)
    {
        Gs1UnbracketedBuildResult result =
            new Gs1UnbracketedBuildResult();
        result.Success = issues.Count == 0;
        result.Text = text;
        result.Bytes = issues.Count == 0
            ? ConvertTextToBytes(text)
            : new byte[0];
        result.Issues = issues.ToArray();
        result.RequiresLeadingFunctionOne = requiresLeadingFunctionOne;
        return result;
    }
}
