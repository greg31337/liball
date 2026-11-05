namespace barcode;

internal class Gs1ElementStringParseResult
{
    internal Gs1Element[] Elements = new Gs1Element[0];
    internal BarcodeValidationIssue[] Issues = new BarcodeValidationIssue[0];
    internal bool Success;
}

internal class Gs1AiRecognition
{
    internal Gs1AiDefinition? Definition;
    internal string Identifier = "";
    internal int Length;
}

internal static class Gs1ElementStringParser
{
    internal const int GroupSeparator = 29;
    internal const string StandardName =
        BarcodeStandards.Gs1SyntaxDictionarySnapshot;
    internal const string Cset82Characters = "!\"%&'()*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

    internal static Gs1ElementStringParseResult ParseElementString(
        string? source)
    {
        global::System.Collections.Generic.List<Gs1Element> elements =
            new global::System.Collections.Generic.List<Gs1Element>();
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues =
            new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        if (source == null)
        {
            AddIssue(
                issues,
                "GS1_NULL_MESSAGE",
                "The GS1 element string must not be null.",
                "Message",
                -1,
                "a non-null GS1 element string",
                "null");
            return CreateResult(elements, issues);
        }

        int offset = 0;
        if (source.Length > 0 && source[0] == (char)GroupSeparator)
        {
            offset = 1;
        }

        if (offset == source.Length)
        {
            AddIssue(
                issues,
                "GS1_EMPTY_MESSAGE",
                "The GS1 element string does not contain an element.",
                "Message",
                offset,
                "at least one GS1 element",
                "empty");
            return CreateResult(elements, issues);
        }

        while (offset < source.Length)
        {
            if (source[offset] == (char)GroupSeparator)
            {
                AddIssue(
                    issues,
                    "GS1_EMPTY_ELEMENT",
                    "A group separator is not preceded by variable-length element data.",
                    "Message",
                    offset,
                    "an Application Identifier",
                    "group separator");
                offset = offset + 1;
                continue;
            }

            Gs1AiRecognition recognition = RecognizeApplicationIdentifier(
                source,
                offset);
            if (recognition.Definition == null)
            {
                AddIssue(
                    issues,
                    "GS1_UNKNOWN_APPLICATION_IDENTIFIER",
                    "The element string does not begin with a known GS1 Application Identifier.",
                    "ApplicationIdentifier",
                    offset,
                    "a registered two-, three-, or four-digit Application Identifier",
                    ReadObservedIdentifier(source, offset));
                break;
            }

            Gs1AiDefinition definition = recognition.Definition;
            int minimumLength = CalculateMinimumLength(definition);
            int maximumLength = CalculateMaximumLength(definition);
            bool variableLength = !definition.PredefinedLength;
            int valueOffset = offset + recognition.Length;
            int availableLength = source.Length - valueOffset;
            int separatorOffset = FindSeparatorOffset(
                source,
                valueOffset,
                maximumLength);
            int valueLength;
            bool separatorConsumed = false;
            if (separatorOffset >= 0)
            {
                valueLength = separatorOffset - valueOffset;
                separatorConsumed = true;
            }
            else if (availableLength > maximumLength)
            {
                valueLength = maximumLength;
            }
            else
            {
                valueLength = availableLength;
            }

            if (valueLength < minimumLength)
            {
                AddIssue(
                    issues,
                    "GS1_DATA_TOO_SHORT",
                    "The element data is shorter than the Application Identifier permits.",
                    recognition.Identifier,
                    valueOffset,
                    $"{minimumLength} through {maximumLength} characters",
                    $"{valueLength} characters");
            }

            string value = source.Substring(valueOffset, valueLength);
            string[] components = SliceAndValidateComponents(
                definition,
                recognition.Identifier,
                value,
                valueOffset,
                issues);
            int sourceLength = recognition.Length + valueLength;
            if (separatorConsumed)
            {
                sourceLength = sourceLength + 1;
                if (definition.PredefinedLength)
                {
                    AddIssue(
                        issues,
                        "GS1_UNEXPECTED_SEPARATOR",
                        "A fixed-length element must not be terminated by a group separator.",
                        recognition.Identifier,
                        separatorOffset,
                        "the next Application Identifier",
                        "group separator");
                }
            }
            else if (!definition.PredefinedLength
                && availableLength > maximumLength)
            {
                AddIssue(
                    issues,
                    "GS1_MISSING_SEPARATOR",
                    "A non-predefined-length element followed by another element must be terminated by a group separator.",
                    recognition.Identifier,
                    valueOffset + valueLength,
                    "a group separator before the next Application Identifier",
                    CharacterDescription(source[valueOffset + valueLength]));
            }

            Gs1Element element = new Gs1Element();
            element.ApplicationIdentifier = recognition.Identifier;
            element.Value = value;
            element.Components = components;
            element.Definition = CreatePublicDefinition(
                definition,
                recognition.Identifier);
            element.SourceOffset = offset;
            element.SourceLength = sourceLength;
            element.VariableLength = variableLength;
            elements.Add(element);
            offset = offset + sourceLength;
        }

        return CreateResult(elements, issues);
    }

    internal static Gs1ElementStringParseResult ParseElementBytes(
        byte[]? source)
    {
        if (source == null)
        {
            return ParseElementString(null);
        }

        char[] characters = new char[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            characters[index] = (char)source[index];
            index = index + 1;
        }

        return ParseElementString(new string(characters));
    }

    static Gs1AiRecognition RecognizeApplicationIdentifier(
        string source,
        int offset)
    {
        Gs1AiRecognition result = new Gs1AiRecognition();
        int nodeIndex = 0;
        int length = 0;
        while (length < 4 && offset + length < source.Length)
        {
            char character = source[offset + length];
            if (character < '0' || character > '9')
            {
                break;
            }

            int childIndex = Gs1AiTable.TrieChildIndices[
                (nodeIndex * Gs1AiTable.TrieRadix) + character - '0'];
            if (childIndex < 0)
            {
                break;
            }

            nodeIndex = childIndex;
            length = length + 1;
            int definitionIndex = Gs1AiTable.TrieDefinitionIndices[nodeIndex];
            if (definitionIndex >= 0)
            {
                result.Definition = Gs1AiTable.Definitions[definitionIndex];
                result.Identifier = source.Substring(offset, length);
                result.Length = length;
            }
        }

        return result;
    }

    static string ReadObservedIdentifier(string source, int offset)
    {
        int length = source.Length - offset;
        if (length > 4)
        {
            length = 4;
        }

        return source.Substring(offset, length);
    }

    static int CalculateMinimumLength(Gs1AiDefinition definition)
    {
        int length = 0;
        int index = 0;
        while (index < definition.Components.Length)
        {
            Gs1AiComponentDefinition component = definition.Components[index];
            if (!component.Optional)
            {
                length = length + component.MinimumLength;
            }

            index = index + 1;
        }

        return length;
    }

    static int CalculateMaximumLength(Gs1AiDefinition definition)
    {
        int length = 0;
        int index = 0;
        while (index < definition.Components.Length)
        {
            length = length + definition.Components[index].MaximumLength;
            index = index + 1;
        }

        return length;
    }

    static int FindSeparatorOffset(
        string source,
        int valueOffset,
        int maximumLength)
    {
        int endOffset = valueOffset + maximumLength;
        if (endOffset > source.Length)
        {
            endOffset = source.Length;
        }

        int offset = valueOffset;
        while (offset < endOffset)
        {
            if (source[offset] == (char)GroupSeparator)
            {
                return offset;
            }

            offset = offset + 1;
        }

        if (offset < source.Length
            && offset == valueOffset + maximumLength
            && source[offset] == (char)GroupSeparator)
        {
            return offset;
        }

        return -1;
    }

    static string[] SliceAndValidateComponents(
        Gs1AiDefinition definition,
        string identifier,
        string value,
        int valueOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        global::System.Collections.Generic.List<string> values =
            new global::System.Collections.Generic.List<string>();
        int componentOffset = 0;
        int componentIndex = 0;
        while (componentIndex < definition.Components.Length)
        {
            Gs1AiComponentDefinition component =
                definition.Components[componentIndex];
            int remainingLength = value.Length - componentOffset;
            if (remainingLength == 0 && component.Optional)
            {
                componentIndex = componentIndex + 1;
                continue;
            }

            int componentLength;
            if (component.MinimumLength == component.MaximumLength)
            {
                componentLength = component.MaximumLength;
                if (componentLength > remainingLength)
                {
                    componentLength = remainingLength;
                }
            }
            else
            {
                componentLength = remainingLength;
                if (componentLength > component.MaximumLength)
                {
                    componentLength = component.MaximumLength;
                }
            }

            if (componentLength < component.MinimumLength)
            {
                AddIssue(
                    issues,
                    "GS1_COMPONENT_TOO_SHORT",
                    "The element data does not contain a complete component.",
                    identifier,
                    valueOffset + componentOffset,
                    $"{component.MinimumLength} through {component.MaximumLength} characters for component {componentIndex + 1}",
                    $"{componentLength} characters");
            }

            string componentValue = value.Substring(
                componentOffset,
                componentLength);
            values.Add(componentValue);
            int issueCount = issues.Count;
            ValidateCharacterSet(
                component.CharacterSet,
                identifier,
                componentValue,
                valueOffset + componentOffset,
                issues);
            if (issues.Count == issueCount
                && componentLength >= component.MinimumLength)
            {
                Gs1ComponentValidator.ValidateComponent(
                    component,
                    identifier,
                    componentValue,
                    valueOffset + componentOffset,
                    issues);
            }

            componentOffset = componentOffset + componentLength;
            componentIndex = componentIndex + 1;
        }

        if (componentOffset < value.Length)
        {
            AddIssue(
                issues,
                "GS1_DATA_TOO_LONG",
                "The element data is longer than its component definitions permit.",
                identifier,
                valueOffset + componentOffset,
                $"at most {componentOffset} characters",
                $"{value.Length} characters");
        }

        return values.ToArray();
    }

    static void ValidateCharacterSet(
        string characterSet,
        string identifier,
        string value,
        int sourceOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        if (characterSet == "Z")
        {
            ValidateBase64Url(identifier, value, sourceOffset, issues);
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool valid = IsCharacterPermitted(characterSet, character);
            if (!valid)
            {
                AddIssue(
                    issues,
                    CharacterSetIssueCode(characterSet),
                    CharacterSetIssueMessage(characterSet),
                    identifier,
                    sourceOffset + index,
                    CharacterSetExpectation(characterSet),
                    $"character {CharacterDescription(character)}");
                return;
            }

            index = index + 1;
        }

    }

    static bool IsCharacterPermitted(string characterSet, char character)
    {
        if (characterSet == "N")
        {
            return character >= '0' && character <= '9';
        }

        if (characterSet == "X")
        {
            return Cset82Characters.IndexOf(character) >= 0;
        }

        if (characterSet == "Y")
        {
            return character == '#'
                || character == '-'
                || character == '/'
                || (character >= '0' && character <= '9')
                || (character >= 'A' && character <= 'Z');
        }

        if (characterSet == "Z")
        {
            return character == '-'
                || character == '_'
                || (character >= '0' && character <= '9')
                || (character >= 'A' && character <= 'Z')
                || (character >= 'a' && character <= 'z');
        }

        return false;
    }

    static string CharacterSetIssueCode(string characterSet)
    {
        if (characterSet == "N")
        {
            return "GS1_INVALID_NUMERIC_CHARACTER";
        }

        if (characterSet == "X")
        {
            return "GS1_INVALID_CSET82_CHARACTER";
        }

        if (characterSet == "Y")
        {
            return "GS1_INVALID_CSET39_CHARACTER";
        }

        return "GS1_INVALID_BASE64URL_CHARACTER";
    }

    static string CharacterSetIssueMessage(string characterSet)
    {
        if (characterSet == "N")
        {
            return "Numeric GS1 element data contains a non-digit character.";
        }

        if (characterSet == "X")
        {
            return "GS1 CSET 82 element data contains a character outside the permitted repertoire.";
        }

        if (characterSet == "Y")
        {
            return "GS1 CSET 39 element data contains a character outside the permitted repertoire.";
        }

        return "GS1 base64url element data contains a character outside the permitted repertoire.";
    }

    static string CharacterSetExpectation(string characterSet)
    {
        if (characterSet == "N")
        {
            return "digits 0 through 9";
        }

        if (characterSet == "X")
        {
            return "a GS1 CSET 82 character";
        }

        if (characterSet == "Y")
        {
            return "a GS1 CSET 39 character";
        }

        return "a base64url character or trailing padding";
    }

    static string CharacterDescription(char character)
    {
        if (character < ' ' || character > '~')
        {
            return $"U+{((int)character):X4}";
        }

        return $"'{character}'";
    }

    static void ValidateBase64Url(
        string identifier,
        string value,
        int sourceOffset,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        int paddingLength = 0;
        while (paddingLength < value.Length
            && value[value.Length - paddingLength - 1] == '=')
        {
            paddingLength = paddingLength + 1;
        }

        int dataLength = value.Length - paddingLength;
        if (paddingLength > 2
            || (paddingLength > 0 && value.Length % 3 != 0))
        {
            AddIssue(
                issues,
                "GS1_INVALID_BASE64URL_PADDING",
                "GS1 base64url padding has an invalid length.",
                identifier,
                sourceOffset + dataLength,
                "one or two trailing '=' characters with a component length divisible by three",
                $"{paddingLength} padding characters in {value.Length} characters");
            return;
        }

        int index = 0;
        while (index < dataLength)
        {
            char character = value[index];
            if (!IsCharacterPermitted("Z", character))
            {
                AddIssue(
                    issues,
                    "GS1_INVALID_BASE64URL_CHARACTER",
                    "GS1 base64url element data contains a character outside the permitted repertoire.",
                    identifier,
                    sourceOffset + index,
                    "a base64url character or trailing padding",
                    $"character {CharacterDescription(character)}");
                return;
            }

            index = index + 1;
        }
    }

    static Gs1ApplicationIdentifierInfo CreatePublicDefinition(
        Gs1AiDefinition definition,
        string identifier)
    {
        return Gs1AiInfoFactory.Create(definition, identifier);
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
            StandardName,
            field,
            offset,
            expected,
            observed);
        issues.Add(issue);
    }

    static Gs1ElementStringParseResult CreateResult(
        global::System.Collections.Generic.List<Gs1Element> elements,
        global::System.Collections.Generic.List<BarcodeValidationIssue> issues)
    {
        Gs1ElementStringParseResult result =
            new Gs1ElementStringParseResult();
        result.Elements = elements.ToArray();
        result.Issues = issues.ToArray();
        result.Success = issues.Count == 0;
        return result;
    }
}
