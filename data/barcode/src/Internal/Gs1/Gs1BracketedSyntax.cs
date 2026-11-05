namespace barcode;

internal class Gs1BracketedParseResult
{
    internal bool Success;
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
}

internal class Gs1BracketedBuildResult
{
    internal bool Success;
    internal string Text = "";
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
}

internal class Gs1BracketedToken
{
    internal string Identifier = "";
    internal string Value = "";
    internal int OpeningOffset;
    internal int IdentifierOffset;
    internal int ValueOffset;
}

internal class Gs1BracketScanResult
{
    internal int NextOffset;
    internal bool Error;
}

internal static class Gs1BracketedSyntax
{
    internal static Gs1BracketedParseResult ParseBracketed(
        string? source)
    {
        global::System.Collections.Generic.List<Gs1BracketedToken> tokens =
            new global::System.Collections.Generic.List<Gs1BracketedToken>();
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (source == null)
        {
            AddBracketIssue(
                issues,
                "GS1_BRACKETED_NULL",
                "The bracketed GS1 text must not be null.",
                -1,
                "a non-null bracketed GS1 message",
                "null");
            return CreateParseResult(new Gs1Element[0], issues);
        }

        if (source.Length == 0)
        {
            AddBracketIssue(
                issues,
                "GS1_BRACKETED_EMPTY",
                "The bracketed GS1 text must contain at least one element.",
                0,
                "an element beginning with '(AI)'",
                "empty");
            return CreateParseResult(new Gs1Element[0], issues);
        }

        int offset = 0;
        while (offset < source.Length)
        {
            if (source[offset] != '(')
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_MISSING",
                    "A bracketed GS1 element must begin with an Application Identifier in parentheses.",
                    offset,
                    "'(AI)'",
                    ReadObservedSource(source, offset));
                break;
            }

            int closingOffset = source.IndexOf(')', offset + 1);
            if (closingOffset < 0)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_UNTERMINATED",
                    "The parenthesized Application Identifier is not terminated.",
                    offset,
                    "a closing ')' after the Application Identifier",
                    source.Substring(offset));
                break;
            }

            string identifier = source.Substring(
                offset + 1,
                closingOffset - offset - 1);
            int invalidDigitOffset = FindInvalidAiDigit(identifier);
            if (invalidDigitOffset >= 0)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_NON_NUMERIC",
                    "A bracketed Application Identifier must contain only digits.",
                    offset + 1 + invalidDigitOffset,
                    "two through four digits",
                    identifier);
                break;
            }

            if (identifier.Length < 2 || identifier.Length > 4)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_LENGTH_INVALID",
                    "A bracketed Application Identifier must contain two through four digits.",
                    offset + 1,
                    "two through four digits",
                    identifier);
                break;
            }

            if (Gs1AiResolver.ResolveDefinition(identifier) == null)
            {
                AddBracketIssue(
                    issues,
                    "GS1_UNKNOWN_APPLICATION_IDENTIFIER",
                    "The bracketed Application Identifier is not registered in the GS1 dictionary.",
                    offset + 1,
                    "a registered GS1 Application Identifier",
                    identifier);
                break;
            }

            int valueOffset = closingOffset + 1;
            Gs1BracketScanResult scan = ScanBracketedValue(
                source,
                valueOffset,
                issues);
            int valueLength = scan.NextOffset - valueOffset;
            Gs1BracketedToken token = new Gs1BracketedToken();
            token.Identifier = identifier;
            token.Value = source.Substring(valueOffset, valueLength);
            token.OpeningOffset = offset;
            token.IdentifierOffset = offset + 1;
            token.ValueOffset = valueOffset;
            tokens.Add(token);
            if (scan.Error)
            {
                break;
            }

            offset = scan.NextOffset;
        }

        Gs1Element[] elements = ParseTokenElements(tokens, issues);
        SortBracketIssues(issues);
        return CreateParseResult(elements, issues);
    }

    internal static Gs1BracketedBuildResult BuildBracketed(
        Gs1Element[]? elements)
    {
        global::System.Collections.Generic.List<Gs1BracketedToken> tokens =
            new global::System.Collections.Generic.List<Gs1BracketedToken>();
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (elements == null || elements.Length == 0)
        {
            AddBracketIssue(
                issues,
                "GS1_BRACKETED_ELEMENTS_MISSING",
                "At least one GS1 element is required to build bracketed text.",
                -1,
                "one or more GS1 elements",
                elements == null ? "null" : "empty");
            return CreateBuildResult("", issues);
        }

        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_ELEMENT_NULL",
                    "A GS1 element must not be null.",
                    text.Length,
                    "a GS1 element",
                    "null");
                index = index + 1;
                continue;
            }

            string identifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            int openingOffset = text.Length;
            int invalidDigitOffset = FindInvalidAiDigit(identifier);
            if (identifier.Length < 2
                || identifier.Length > 4
                || invalidDigitOffset >= 0
                || Gs1AiResolver.ResolveDefinition(identifier) == null)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_BUILD_AI_INVALID",
                    "The GS1 element does not contain a registered two- through four-digit Application Identifier.",
                    openingOffset,
                    "a registered two- through four-digit Application Identifier",
                    identifier);
                index = index + 1;
                continue;
            }

            string value = element.Value == null ? "" : element.Value;
            int ambiguousOffset = FindAmbiguousParenthesis(value);
            if (ambiguousOffset >= 0)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AMBIGUOUS_DATA",
                    "The element data contains a numeric parenthesis sequence that would be interpreted as another Application Identifier.",
                    openingOffset + identifier.Length + 2 + ambiguousOffset,
                    "literal parentheses that do not begin with a digit",
                    value.Substring(ambiguousOffset));
            }

            text.Append('(');
            text.Append(identifier);
            text.Append(')');
            int valueOffset = text.Length;
            text.Append(value);

            Gs1BracketedToken token = new Gs1BracketedToken();
            token.Identifier = identifier;
            token.Value = value;
            token.OpeningOffset = openingOffset;
            token.IdentifierOffset = openingOffset + 1;
            token.ValueOffset = valueOffset;
            tokens.Add(token);
            index = index + 1;
        }

        if (issues.Count == 0)
        {
            ParseTokenElements(tokens, issues);
        }

        SortBracketIssues(issues);
        return CreateBuildResult(text.ToString(), issues);
    }

    static Gs1BracketScanResult ScanBracketedValue(
        string source,
        int valueOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1BracketScanResult result = new Gs1BracketScanResult();
        int offset = valueOffset;
        while (offset < source.Length)
        {
            if (source[offset] != '(')
            {
                offset = offset + 1;
                continue;
            }

            int contentOffset = offset + 1;
            if (contentOffset >= source.Length
                || source[contentOffset] < '0'
                || source[contentOffset] > '9')
            {
                offset = offset + 1;
                continue;
            }

            int digitEnd = contentOffset;
            while (digitEnd < source.Length
                && source[digitEnd] >= '0'
                && source[digitEnd] <= '9')
            {
                digitEnd = digitEnd + 1;
            }

            if (digitEnd >= source.Length)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_UNTERMINATED",
                    "The numeric parenthesis sequence in the element data is not terminated.",
                    offset,
                    "a closing ')' after the Application Identifier",
                    source.Substring(offset));
                result.NextOffset = offset;
                result.Error = true;
                return result;
            }

            if (source[digitEnd] != ')')
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_NON_NUMERIC",
                    "A numeric parenthesis sequence contains a non-digit Application Identifier character.",
                    digitEnd,
                    "digits followed by ')'",
                    ReadObservedSource(source, offset));
                result.NextOffset = offset;
                result.Error = true;
                return result;
            }

            int identifierLength = digitEnd - contentOffset;
            if (identifierLength < 2 || identifierLength > 4)
            {
                AddBracketIssue(
                    issues,
                    "GS1_BRACKETED_AI_LENGTH_INVALID",
                    "A numeric parenthesis sequence must contain a two- through four-digit Application Identifier.",
                    contentOffset,
                    "two through four digits",
                    source.Substring(contentOffset, identifierLength));
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

    static Gs1Element[] ParseTokenElements(
        global::System.Collections.Generic.List<Gs1BracketedToken> tokens,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        global::System.Collections.Generic.List<Gs1Element> elements =
            new global::System.Collections.Generic.List<Gs1Element>();
        int index = 0;
        while (index < tokens.Count)
        {
            Gs1BracketedToken token = tokens[index];
            Gs1ElementStringParseResult parsed =
                Gs1ElementStringParser.ParseElementString(
                    token.Identifier + token.Value);
            if (parsed.Elements.Length > 0)
            {
                Gs1Element element = parsed.Elements[0];
                element.SourceOffset = token.IdentifierOffset;
                element.SourceLength = token.Identifier.Length
                    + 1
                    + token.Value.Length;
                elements.Add(element);
            }

            int issueIndex = 0;
            while (issueIndex < parsed.Issues.Length)
            {
                BarcodeValidationIssue issue = parsed.Issues[issueIndex];
                issue.Offset = MapTokenOffset(token, issue.Offset);
                issues.Add(issue);
                issueIndex = issueIndex + 1;
            }

            index = index + 1;
        }

        return elements.ToArray();
    }

    static int MapTokenOffset(
        Gs1BracketedToken token,
        int tokenOffset)
    {
        if (tokenOffset < 0)
        {
            return tokenOffset;
        }

        if (tokenOffset < token.Identifier.Length)
        {
            return token.IdentifierOffset + tokenOffset;
        }

        return token.ValueOffset + tokenOffset - token.Identifier.Length;
    }

    static int FindInvalidAiDigit(string identifier)
    {
        int index = 0;
        while (index < identifier.Length)
        {
            char character = identifier[index];
            if (character < '0' || character > '9')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static int FindAmbiguousParenthesis(string value)
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

    static string ReadObservedSource(string source, int offset)
    {
        int length = source.Length - offset;
        if (length > 8)
        {
            length = 8;
        }

        return source.Substring(offset, length);
    }

    static void AddBracketIssue(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        string code,
        string message,
        int offset,
        string expected,
        string observed)
    {
        BarcodeValidationIssue issue = ValidationIssueFactory.Create(
            BarcodeValidationSeverity.Error,
            code,
            message,
            Gs1ElementStringParser.StandardName,
            "ApplicationIdentifier",
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static void SortBracketIssues(
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
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1BracketedParseResult result = new Gs1BracketedParseResult();
        result.Success = issues.Count == 0;
        result.Elements = elements;
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
}
