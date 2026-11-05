namespace barcode;

internal class Gs1UnbracketedParseResult
{
    internal bool Success;
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
    internal bool RequiresLeadingFunctionOne;
}

internal class Gs1UnbracketedBuildResult
{
    internal bool Success;
    internal string Text = "";
    internal byte[] Bytes = new byte[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
    internal bool RequiresLeadingFunctionOne;
}

internal class Gs1UnbracketedToken
{
    internal string Identifier = "";
    internal string Value = "";
    internal int IdentifierOffset;
    internal int ValueOffset;
}

internal static class Gs1UnbracketedSyntax
{
    internal const int GroupSeparator = 29;
    internal const char FunctionOnePlaceholder = '^';

    internal static Gs1UnbracketedParseResult ParseUnbracketedElementString(
        string? source)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (source == null)
        {
            AddSyntaxIssue(
                issues,
                "GS1_UNBRACKETED_NULL",
                "The unbracketed GS1 element string must not be null.",
                -1,
                "a caret-prefixed GS1 element string",
                "null");
            return CreateParseResult(
                new Gs1Element[0],
                issues,
                false);
        }

        if (source.Length == 0
            || source[0] != FunctionOnePlaceholder)
        {
            AddSyntaxIssue(
                issues,
                "GS1_UNBRACKETED_FNC1_FIRST_REQUIRED",
                "An unbracketed GS1 element string must begin with the caret placeholder for the first-position FNC1 symbol character.",
                0,
                "'^' followed by GS1 Application Identifier data",
                source.Length == 0 ? "empty" : ReadObservedText(source, 0));
            return CreateParseResult(
                new Gs1Element[0],
                issues,
                false);
        }

        int separatorOffset = FindTextCharacter(
            source,
            (char)GroupSeparator);
        if (separatorOffset >= 0)
        {
            AddSyntaxIssue(
                issues,
                "GS1_UNBRACKETED_ASCII_GS_FORBIDDEN",
                "ASCII GS is not an FNC1 placeholder in an unbracketed GS1 element string.",
                separatorOffset,
                "a caret placeholder at each FNC1 position",
                "ASCII GS");
            return CreateParseResult(
                new Gs1Element[0],
                issues,
                true);
        }

        char[] transformed = source.ToCharArray();
        int index = 0;
        while (index < transformed.Length)
        {
            if (transformed[index] == FunctionOnePlaceholder)
            {
                transformed[index] = (char)GroupSeparator;
            }

            index = index + 1;
        }

        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(
                new string(transformed));
        AddParsedIssues(issues, parsed.Issues);
        SortSyntaxIssues(issues);
        return CreateParseResult(parsed.Elements, issues, true);
    }

    internal static Gs1UnbracketedParseResult ParseBarcodeMessageBytes(
        byte[]? source)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (source == null)
        {
            AddSyntaxIssue(
                issues,
                "GS1_BARCODE_MESSAGE_NULL",
                "The GS1 barcode message must not be null.",
                -1,
                "GS1 barcode message bytes",
                "null");
            return CreateParseResult(
                new Gs1Element[0],
                issues,
                true);
        }

        if (source.Length > 0 && source[0] == GroupSeparator)
        {
            AddSyntaxIssue(
                issues,
                "GS1_BARCODE_MESSAGE_LEADING_GS",
                "The first-position FNC1 symbol character is supplied by the barcode carrier and must not be represented by a leading ASCII GS byte.",
                0,
                "Application Identifier data without a leading ASCII GS byte",
                "ASCII GS");
        }

        string text = ConvertBytesToText(source);
        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(text);
        AddParsedIssues(issues, parsed.Issues);
        SortSyntaxIssues(issues);
        return CreateParseResult(parsed.Elements, issues, true);
    }

    internal static Gs1UnbracketedBuildResult BuildUnbracketedElementString(
        Gs1Element[]? elements)
    {
        return BuildRepresentation(elements, true);
    }

    internal static Gs1UnbracketedBuildResult BuildBarcodeMessage(
        Gs1Element[]? elements)
    {
        return BuildRepresentation(elements, false);
    }

    static Gs1UnbracketedBuildResult BuildRepresentation(
        Gs1Element[]? elements,
        bool caretRepresentation)
    {
        global::System.Collections.Generic.List<Gs1UnbracketedToken> tokens =
            new global::System.Collections.Generic.List<Gs1UnbracketedToken>();
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (elements == null || elements.Length == 0)
        {
            AddSyntaxIssue(
                issues,
                caretRepresentation
                    ? "GS1_UNBRACKETED_ELEMENTS_MISSING"
                    : "GS1_BARCODE_MESSAGE_ELEMENTS_MISSING",
                "At least one GS1 element is required.",
                -1,
                "one or more GS1 elements",
                elements == null ? "null" : "empty");
            return CreateBuildResult("", issues, true);
        }

        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        if (caretRepresentation)
        {
            text.Append(FunctionOnePlaceholder);
        }

        int index = 0;
        while (index < elements.Length)
        {
            Gs1Element element = elements[index];
            if (element == null)
            {
                AddSyntaxIssue(
                    issues,
                    caretRepresentation
                        ? "GS1_UNBRACKETED_ELEMENT_NULL"
                        : "GS1_BARCODE_MESSAGE_ELEMENT_NULL",
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
            Gs1AiDefinition? definition =
                Gs1AiResolver.ResolveDefinition(identifier);
            if (definition == null)
            {
                AddSyntaxIssue(
                    issues,
                    caretRepresentation
                        ? "GS1_UNBRACKETED_BUILD_AI_INVALID"
                        : "GS1_BARCODE_MESSAGE_BUILD_AI_INVALID",
                    "The GS1 element does not contain a registered Application Identifier.",
                    text.Length,
                    "a registered two- through four-digit Application Identifier",
                    identifier);
                index = index + 1;
                continue;
            }

            string value = element.Value == null ? "" : element.Value;
            Gs1UnbracketedToken token = new Gs1UnbracketedToken();
            token.Identifier = identifier;
            token.Value = value;
            token.IdentifierOffset = text.Length;
            text.Append(identifier);
            token.ValueOffset = text.Length;
            text.Append(value);
            tokens.Add(token);

            int caretOffset = FindTextCharacter(
                value,
                FunctionOnePlaceholder);
            int groupSeparatorOffset = FindTextCharacter(
                value,
                (char)GroupSeparator);
            if (caretRepresentation && caretOffset >= 0)
            {
                AddSyntaxIssue(
                    issues,
                    "GS1_UNBRACKETED_RESERVED_CARET",
                    "A caret in element data would be interpreted as an FNC1 placeholder.",
                    token.ValueOffset + caretOffset,
                    "element data without a caret",
                    "caret");
            }

            if (caretRepresentation && groupSeparatorOffset >= 0)
            {
                AddSyntaxIssue(
                    issues,
                    "GS1_UNBRACKETED_ASCII_GS_FORBIDDEN",
                    "ASCII GS is not permitted in caret-based unbracketed element data.",
                    token.ValueOffset + groupSeparatorOffset,
                    "element data without ASCII GS",
                    "ASCII GS");
            }

            if (!caretRepresentation && groupSeparatorOffset >= 0)
            {
                AddSyntaxIssue(
                    issues,
                    "GS1_BARCODE_MESSAGE_RESERVED_GS",
                    "ASCII GS in element data would be interpreted as an element separator.",
                    token.ValueOffset + groupSeparatorOffset,
                    "element data without ASCII GS",
                    "ASCII GS");
            }

            if (!definition.PredefinedLength
                && index < elements.Length - 1)
            {
                text.Append(
                    caretRepresentation
                        ? FunctionOnePlaceholder
                        : (char)GroupSeparator);
            }

            index = index + 1;
        }

        if (issues.Count == 0)
        {
            ValidateBuildTokens(tokens, issues);
        }

        SortSyntaxIssues(issues);
        return CreateBuildResult(text.ToString(), issues, true);
    }

    static void ValidateBuildTokens(
        global::System.Collections.Generic.List<Gs1UnbracketedToken> tokens,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            Gs1UnbracketedToken token = tokens[index];
            Gs1ElementStringParseResult parsed =
                Gs1ElementStringParser.ParseElementString(
                    token.Identifier + token.Value);
            int issueIndex = 0;
            while (issueIndex < parsed.Issues.Length)
            {
                BarcodeValidationIssue issue = parsed.Issues[issueIndex];
                issue.Offset = MapBuildOffset(token, issue.Offset);
                issues.Add(issue);
                issueIndex = issueIndex + 1;
            }

            index = index + 1;
        }
    }

    static int MapBuildOffset(
        Gs1UnbracketedToken token,
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

    static int FindTextCharacter(string source, char target)
    {
        int index = 0;
        while (index < source.Length)
        {
            if (source[index] == target)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static string ConvertBytesToText(byte[] source)
    {
        char[] characters = new char[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            characters[index] = (char)source[index];
            index = index + 1;
        }

        return new string(characters);
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

    static string ReadObservedText(string source, int offset)
    {
        int length = source.Length - offset;
        if (length > 8)
        {
            length = 8;
        }

        return source.Substring(offset, length);
    }

    static void AddParsedIssues(
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        BarcodeValidationIssue[] parsedIssues)
    {
        int index = 0;
        while (index < parsedIssues.Length)
        {
            issues.Add(parsedIssues[index]);
            index = index + 1;
        }
    }

    static void AddSyntaxIssue(
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
            "Message",
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static void SortSyntaxIssues(
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

    static Gs1UnbracketedParseResult CreateParseResult(
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues,
        bool requiresLeadingFunctionOne)
    {
        Gs1UnbracketedParseResult result =
            new Gs1UnbracketedParseResult();
        result.Success = issues.Count == 0;
        result.Elements = elements;
        result.Issues = issues.ToArray();
        result.RequiresLeadingFunctionOne = requiresLeadingFunctionOne;
        return result;
    }

    static Gs1UnbracketedBuildResult CreateBuildResult(
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
