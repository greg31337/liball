namespace barcode;

internal class Gs1HriResult
{
    internal bool Success;
    internal string CompactText = "";
    internal string[] Lines = new string[0];
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
}

internal static class Gs1HriFormatter
{
    internal static Gs1HriResult GenerateHri(
        Gs1Element[]? elements,
        bool includeDataTitles,
        bool optimizeElementOrder)
    {
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (elements == null || elements.Length == 0)
        {
            AddHriIssue(
                issues,
                "GS1_HRI_ELEMENTS_MISSING",
                "At least one GS1 element is required to generate HRI.",
                -1,
                "one or more GS1 elements",
                elements == null ? "null" : "empty");
            return CreateHriResult(
                "",
                new string[0],
                new Gs1Element[0],
                issues);
        }

        Gs1Element[] orderedElements = optimizeElementOrder
            ? OrderElements(elements)
            : CopyElements(elements);
        global::System.Collections.Generic.List<string> lines =
            new global::System.Collections.Generic.List<string>();
        global::System.Text.StringBuilder compactText =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < orderedElements.Length)
        {
            Gs1Element element = orderedElements[index];
            if (element == null)
            {
                AddHriIssue(
                    issues,
                    "GS1_HRI_ELEMENT_NULL",
                    "A GS1 HRI element must not be null.",
                    -1,
                    "a GS1 element",
                    "null");
                index = index + 1;
                continue;
            }

            string identifier = element.ApplicationIdentifier == null
                ? ""
                : element.ApplicationIdentifier;
            if (!IsDisplayIdentifier(identifier))
            {
                AddHriIssue(
                    issues,
                    "GS1_HRI_AI_INVALID",
                    "A GS1 HRI element must contain a two- through four-digit Application Identifier.",
                    element.SourceOffset,
                    "two through four digits",
                    identifier);
                index = index + 1;
                continue;
            }

            string value = element.Value == null ? "" : element.Value;
            int invalidValueOffset = FindInvalidDisplayCharacter(value);
            if (value.Length == 0 || invalidValueOffset >= 0)
            {
                int issueOffset = element.SourceOffset + identifier.Length;
                if (invalidValueOffset >= 0)
                {
                    issueOffset = issueOffset + invalidValueOffset;
                }

                AddHriIssue(
                    issues,
                    value.Length == 0
                        ? "GS1_HRI_VALUE_EMPTY"
                        : "GS1_HRI_VALUE_NOT_DISPLAYABLE",
                    value.Length == 0
                        ? "A GS1 HRI element value must not be empty."
                        : "A GS1 HRI element value must contain printable ISO/IEC 646 characters.",
                    issueOffset,
                    value.Length == 0
                        ? "a non-empty element value"
                        : "printable ISO/IEC 646 characters",
                    value.Length == 0 ? "empty" : ReadObservedCharacter(value, invalidValueOffset));
                index = index + 1;
                continue;
            }

            Gs1AiDefinition? definition =
                Gs1AiResolver.ResolveDefinition(identifier);
            global::System.Text.StringBuilder line =
                new global::System.Text.StringBuilder();
            if (includeDataTitles
                && definition != null
                && definition.Title.Length > 0)
            {
                line.Append(definition.Title);
                line.Append(' ');
            }

            line.Append('(');
            line.Append(identifier);
            line.Append(") ");
            line.Append(value);
            lines.Add(line.ToString());

            compactText.Append('(');
            compactText.Append(identifier);
            compactText.Append(')');
            compactText.Append(value);
            index = index + 1;
        }

        return CreateHriResult(
            compactText.ToString(),
            lines.ToArray(),
            orderedElements,
            issues);
    }

    internal static Gs1Element[] OrderElements(
        Gs1Element[]? elements)
    {
        Gs1Element[] ordered = CopyElements(elements);
        int index = 1;
        while (index < ordered.Length)
        {
            Gs1Element current = ordered[index];
            int currentRank = GetElementOrderRank(current);
            int previousIndex = index - 1;
            while (previousIndex >= 0
                && GetElementOrderRank(ordered[previousIndex]) > currentRank)
            {
                ordered[previousIndex + 1] = ordered[previousIndex];
                previousIndex = previousIndex - 1;
            }

            ordered[previousIndex + 1] = current;
            index = index + 1;
        }

        return ordered;
    }

    internal static bool IsVariableLengthElement(Gs1Element element)
    {
        if (element == null)
        {
            return false;
        }

        Gs1AiDefinition? definition =
            Gs1AiResolver.ResolveDefinition(element.ApplicationIdentifier);
        return definition == null
            ? element.VariableLength
            : !definition.PredefinedLength;
    }

    static Gs1Element[] CopyElements(Gs1Element[]? elements)
    {
        if (elements == null)
        {
            return new Gs1Element[0];
        }

        Gs1Element[] copy = new Gs1Element[elements.Length];
        int index = 0;
        while (index < elements.Length)
        {
            copy[index] = elements[index];
            index = index + 1;
        }

        return copy;
    }

    static int GetElementOrderRank(Gs1Element element)
    {
        if (element == null)
        {
            return 3;
        }

        Gs1AiDefinition? definition =
            Gs1AiResolver.ResolveDefinition(element.ApplicationIdentifier);
        bool predefinedLength = !IsVariableLengthElement(element);
        if (predefinedLength
            && definition != null
            && definition.DigitalLinkPrimaryKey)
        {
            return 0;
        }

        return predefinedLength ? 1 : 2;
    }

    static bool IsDisplayIdentifier(string identifier)
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

    static int FindInvalidDisplayCharacter(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < ' ' || character > '~')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static string ReadObservedCharacter(string value, int offset)
    {
        if (offset < 0 || offset >= value.Length)
        {
            return "empty";
        }

        return ((int)value[offset]).ToString();
    }

    static void AddHriIssue(
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
            "HumanReadableInterpretation",
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static Gs1HriResult CreateHriResult(
        string compactText,
        string[] lines,
        Gs1Element[] elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1HriResult result = new Gs1HriResult();
        result.Success = issues.Count == 0;
        result.CompactText = compactText;
        result.Lines = lines;
        result.Elements = elements;
        result.Issues = issues.ToArray();
        return result;
    }
}
