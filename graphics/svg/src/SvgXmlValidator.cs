namespace svg;

/// <summary>Maintains state for deterministic XML 1.0 validation and normalization.</summary>
internal class SvgXmlValidationState
{
    string source;
    System.Collections.Generic.List<SvgXmlToken> tokens;
    int maximumNameCharacters;
    SvgXmlValidationError error;
    int errorOffset;
    string message;
    bool sawDeclaration;

    /// <summary>Creates XML validation state.</summary>
    /// <param name="sourceText">The exact decoded source.</param>
    /// <param name="sourceTokens">The ordered lexical tokens.</param>
    /// <param name="maxNameCharacters">The XML name bound.</param>
    internal SvgXmlValidationState(string sourceText, System.Collections.Generic.List<SvgXmlToken> sourceTokens, int maxNameCharacters)
    {
        source = sourceText;
        tokens = sourceTokens;
        maximumNameCharacters = maxNameCharacters;
        error = SvgXmlValidationError.None;
        errorOffset = -1;
        message = "";
        sawDeclaration = false;
    }

    /// <summary>Validates the source and populates normalized fields.</summary>
    /// <returns>The earliest deterministic validation result.</returns>
    internal SvgXmlValidationResult Validate()
    {
        ValidateSourceCharacters();
        int index = 0;
        while (index < tokens.Count)
        {
            ValidateToken(tokens[index]);
            index = index + 1;
        }
        if (error == SvgXmlValidationError.None)
        {
            return new SvgXmlValidationResult(true, SvgXmlValidationError.None, -1, 0, 0, "");
        }
        int[] location = new int[] { 1, 1 };
        FindLocation(errorOffset, location);
        return new SvgXmlValidationResult(false, error, errorOffset, location[0], location[1], message);
    }

    void ValidateSourceCharacters()
    {
        int index = 0;
        while (index < source.Length)
        {
            int width = CodePointWidth(source, index);
            int codePoint = ReadCodePoint(source, index);
            if (width == 0 || !IsXmlCharacter(codePoint))
            {
                SetError(SvgXmlValidationError.InvalidCharacter, index, "The source contains a character forbidden by XML 1.0.");
                if (width == 0) width = 1;
            }
            index = index + width;
        }
    }

    void ValidateToken(SvgXmlToken token)
    {
        switch (token.Kind)
        {
            case SvgXmlTokenKind.Declaration:
                ValidateDeclaration(token);
                break;
            case SvgXmlTokenKind.StartElement:
            case SvgXmlTokenKind.EmptyElement:
                ValidateName(token.QualifiedName, token.StartOffset + 1);
                ValidateAttributes(token.Attributes);
                token.NormalizedValue = "";
                break;
            case SvgXmlTokenKind.EndElement:
                ValidateName(token.QualifiedName, token.StartOffset + 2);
                token.NormalizedValue = "";
                break;
            case SvgXmlTokenKind.Text:
                ValidateCharacterData(token);
                break;
            case SvgXmlTokenKind.Reference:
                ValidateTopReference(token);
                break;
            case SvgXmlTokenKind.CData:
                token.NormalizedValue = NormalizeLineEndings(token.Value);
                break;
            case SvgXmlTokenKind.Comment:
                ValidateComment(token);
                break;
            case SvgXmlTokenKind.ProcessingInstruction:
                ValidateProcessingInstruction(token);
                break;
            case SvgXmlTokenKind.Doctype:
                ValidateDoctype(token);
                break;
            default:
                SetError(SvgXmlValidationError.InvalidArgument, token.StartOffset, "The XML token kind is invalid.");
                break;
        }
    }

    void ValidateDeclaration(SvgXmlToken token)
    {
        if (sawDeclaration || token.StartOffset != 0)
        {
            SetError(SvgXmlValidationError.InvalidDeclaration, token.StartOffset, "The XML declaration must occur exactly once at the start of the source.");
        }
        sawDeclaration = true;
        ValidateAttributes(token.Attributes);
        if (token.Attributes.Count == 0)
        {
            SetError(SvgXmlValidationError.InvalidDeclaration, token.StartOffset + 5, "The XML declaration requires version='1.0'.");
            token.NormalizedValue = "";
            return;
        }
        SvgXmlAttributeToken version = token.Attributes[0];
        if (version.QualifiedName != "version" || version.Value != "1.0")
        {
            SetError(SvgXmlValidationError.InvalidDeclaration, version.StartOffset, "The XML declaration must begin with version='1.0'.");
        }
        int expectedIndex = 1;
        if (expectedIndex < token.Attributes.Count && token.Attributes[expectedIndex].QualifiedName == "encoding")
        {
            SvgXmlAttributeToken encoding = token.Attributes[expectedIndex];
            if (!IsEncodingName(encoding.Value))
            {
                SetError(SvgXmlValidationError.InvalidDeclaration, AttributeValueOffset(encoding), "The XML declaration encoding name is invalid.");
            }
            expectedIndex = expectedIndex + 1;
        }
        if (expectedIndex < token.Attributes.Count && token.Attributes[expectedIndex].QualifiedName == "standalone")
        {
            SvgXmlAttributeToken standalone = token.Attributes[expectedIndex];
            if (standalone.Value != "yes" && standalone.Value != "no")
            {
                SetError(SvgXmlValidationError.InvalidDeclaration, AttributeValueOffset(standalone), "The XML declaration standalone value must be 'yes' or 'no'.");
            }
            expectedIndex = expectedIndex + 1;
        }
        if (expectedIndex < token.Attributes.Count)
        {
            SetError(SvgXmlValidationError.InvalidDeclaration, token.Attributes[expectedIndex].StartOffset, "The XML declaration contains an unexpected or out-of-order pseudo-attribute.");
        }
        int attributeIndex = 0;
        while (attributeIndex < token.Attributes.Count)
        {
            SvgXmlAttributeToken attribute = token.Attributes[attributeIndex];
            if (attribute.References.Count != 0)
            {
                SetError(SvgXmlValidationError.InvalidDeclaration, attribute.References[0].StartOffset, "References are not allowed in XML declaration values.");
            }
            attributeIndex = attributeIndex + 1;
        }
        token.NormalizedValue = "";
    }

    void ValidateAttributes(System.Collections.Generic.List<SvgXmlAttributeToken> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SvgXmlAttributeToken attribute = attributes[index];
            ValidateName(attribute.QualifiedName, attribute.StartOffset);
            int referenceIndex = 0;
            while (referenceIndex < attribute.References.Count)
            {
                DecodeReference(attribute.References[referenceIndex]);
                referenceIndex = referenceIndex + 1;
            }
            attribute.NormalizedValue = NormalizeAttributeValue(attribute);
            index = index + 1;
        }
    }

    void ValidateCharacterData(SvgXmlToken token)
    {
        int close = token.RawText.IndexOf("]]>", System.StringComparison.Ordinal);
        if (close >= 0)
        {
            SetError(SvgXmlValidationError.InvalidCharacterData, token.StartOffset + close, "Character data must not contain the CDATA closing sequence.");
        }
        token.NormalizedValue = NormalizeLineEndings(token.RawText);
    }

    void ValidateTopReference(SvgXmlToken token)
    {
        DecodeReference(token.Reference);
        token.NormalizedValue = (token.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).DecodedValue;
    }

    void ValidateComment(SvgXmlToken token)
    {
        int doubleHyphen = token.Value.IndexOf("--", System.StringComparison.Ordinal);
        if (doubleHyphen >= 0)
        {
            SetError(SvgXmlValidationError.InvalidComment, token.StartOffset + 4 + doubleHyphen, "XML comments must not contain a double hyphen.");
        }
        else if (token.Value.EndsWith("-", System.StringComparison.Ordinal))
        {
            SetError(SvgXmlValidationError.InvalidComment, token.StartOffset + 3 + token.Value.Length, "XML comment content must not end with a hyphen.");
        }
        token.NormalizedValue = NormalizeLineEndings(token.Value);
    }

    void ValidateProcessingInstruction(SvgXmlToken token)
    {
        ValidateName(token.QualifiedName, token.StartOffset + 2);
        if (string.Equals(token.QualifiedName, "xml", System.StringComparison.OrdinalIgnoreCase))
        {
            SetError(SvgXmlValidationError.InvalidProcessingInstruction, token.StartOffset + 2, "The processing-instruction target XML is reserved.");
        }
        token.NormalizedValue = NormalizeLineEndings(token.Value);
    }

    void ValidateDoctype(SvgXmlToken token)
    {
        int index = 0;
        if (token.Value.Length == 0 || !IsXmlWhitespace(token.Value[0]))
        {
            SetError(SvgXmlValidationError.InvalidDoctype, token.StartOffset + 9, "The document type declaration requires separating whitespace and a root name.");
            token.NormalizedValue = NormalizeLineEndings(token.Value);
            return;
        }
        while (index < token.Value.Length && IsXmlWhitespace(token.Value[index])) index = index + 1;
        int nameStart = index;
        while (index < token.Value.Length && !IsXmlWhitespace(token.Value[index]) && token.Value[index] != '[') index = index + 1;
        if (index == nameStart)
        {
            SetError(SvgXmlValidationError.InvalidDoctype, token.StartOffset + 9 + nameStart, "The document type declaration requires a root name.");
        }
        else
        {
            string name = token.Value.Substring(nameStart, index - nameStart);
            ValidateName(name, token.StartOffset + 9 + nameStart);
        }
        token.NormalizedValue = NormalizeLineEndings(token.Value);
    }

    void DecodeReference(SvgXmlReferenceToken? reference)
    {
        string value = (reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value;
        if (value == "lt") reference.DecodedValue = "<";
        else if (value == "gt") reference.DecodedValue = ">";
        else if (value == "amp") reference.DecodedValue = "&";
        else if (value == "apos") reference.DecodedValue = "'";
        else if (value == "quot") reference.DecodedValue = "\"";
        else if (value.Length > 0 && value[0] == '#') DecodeCharacterReference(reference);
        else
        {
            reference.DecodedValue = null;
            SetError(SvgXmlValidationError.InvalidReference, reference.StartOffset + 1, "Only predefined XML entities are decoded; declared and external entities are not expanded.");
        }
    }

    void DecodeCharacterReference(SvgXmlReferenceToken reference)
    {
        string value = reference.Value;
        int numberBase = 10;
        int index = 1;
        if (index < value.Length && value[index] == 'x')
        {
            numberBase = 16;
            index = index + 1;
        }
        int digitStart = index;
        int codePoint = 0;
        while (index < value.Length)
        {
            int digit = DigitValue(value[index], numberBase);
            if (digit < 0)
            {
                reference.DecodedValue = null;
                SetError(SvgXmlValidationError.InvalidReference, reference.StartOffset + 1 + index, "The XML character reference contains an invalid digit.");
                return;
            }
            if (codePoint > (1114111 - digit) / numberBase)
            {
                reference.DecodedValue = null;
                SetError(SvgXmlValidationError.InvalidReference, reference.StartOffset + 1 + index, "The XML character reference exceeds the Unicode range.");
                return;
            }
            codePoint = codePoint * numberBase + digit;
            index = index + 1;
        }
        if (digitStart == value.Length || !IsXmlCharacter(codePoint))
        {
            reference.DecodedValue = null;
            SetError(SvgXmlValidationError.InvalidReference, reference.StartOffset + 1 + digitStart, "The XML character reference does not identify an allowed XML 1.0 character.");
            return;
        }
        reference.DecodedValue = System.Char.ConvertFromUtf32(codePoint);
    }

    void ValidateName(string value, int startOffset)
    {
        if (value.Length > maximumNameCharacters)
        {
            SetError(SvgXmlValidationError.NameTooLong, startOffset + maximumNameCharacters, "An XML name exceeded MaxNameCharacters.");
        }
        int index = 0;
        while (index < value.Length)
        {
            int width = CodePointWidth(value, index);
            int codePoint = ReadCodePoint(value, index);
            bool valid = index == 0 ? IsNameStartCharacter(codePoint) : IsNameCharacter(codePoint);
            if (width == 0 || !valid)
            {
                SetError(SvgXmlValidationError.InvalidName, startOffset + index, "The XML name does not follow the XML 1.0 Name production.");
                return;
            }
            index = index + width;
        }
    }

    string NormalizeAttributeValue(SvgXmlAttributeToken attribute)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(attribute.Value.Length);
        int referenceIndex = 0;
        int index = 0;
        int valueOffset = AttributeValueOffset(attribute);
        while (index < attribute.Value.Length)
        {
            if (referenceIndex < attribute.References.Count && valueOffset + index == attribute.References[referenceIndex].StartOffset)
            {
                SvgXmlReferenceToken reference = attribute.References[referenceIndex];
                if (reference.DecodedValue != null) builder.Append(reference.DecodedValue);
                index = index + reference.RawText.Length;
                referenceIndex = referenceIndex + 1;
            }
            else
            {
                char current = attribute.Value[index];
                if (current == '\r')
                {
                    if (index + 1 < attribute.Value.Length && attribute.Value[index + 1] == '\n') index = index + 1;
                    builder.Append(' ');
                }
                else if (current == '\n' || current == '\t' || current == ' ') builder.Append(' ');
                else builder.Append(current);
                index = index + 1;
            }
        }
        return builder.ToString();
    }

    static string NormalizeLineEndings(string value)
    {
        if (value.IndexOf('\r') < 0) return value;
        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r')
            {
                if (index + 1 < value.Length && value[index + 1] == '\n') index = index + 1;
                builder.Append('\n');
            }
            else builder.Append(value[index]);
            index = index + 1;
        }
        return builder.ToString();
    }

    static int AttributeValueOffset(SvgXmlAttributeToken attribute)
    {
        int quote = attribute.RawText.IndexOf(attribute.Quote);
        return attribute.StartOffset + quote + 1;
    }

    static bool IsEncodingName(string value)
    {
        if (value.Length == 0 || !IsAsciiLetter(value[0])) return false;
        int index = 1;
        while (index < value.Length)
        {
            char current = value[index];
            if (!IsAsciiLetter(current) && !IsAsciiDigit(current) && current != '.' && current != '_' && current != '-') return false;
            index = index + 1;
        }
        return true;
    }

    static int DigitValue(char value, int numberBase)
    {
        if (value >= '0' && value <= '9')
        {
            int digit = value - '0';
            return digit < numberBase ? digit : -1;
        }
        if (numberBase == 16 && value >= 'a' && value <= 'f') return value - 'a' + 10;
        if (numberBase == 16 && value >= 'A' && value <= 'F') return value - 'A' + 10;
        return -1;
    }

    static int CodePointWidth(string value, int index)
    {
        char first = value[index];
        if (first >= 55296 && first <= 56319)
        {
            if (index + 1 < value.Length && value[index + 1] >= 56320 && value[index + 1] <= 57343) return 2;
            return 0;
        }
        if (first >= 56320 && first <= 57343) return 0;
        return 1;
    }

    static int ReadCodePoint(string value, int index)
    {
        int width = CodePointWidth(value, index);
        if (width == 0) return -1;
        if (width == 1) return value[index];
        return 65536 + (value[index] - 55296) * 1024 + value[index + 1] - 56320;
    }

    static bool IsXmlCharacter(int value)
    {
        return value == 9 || value == 10 || value == 13 || value >= 32 && value <= 55295 || value >= 57344 && value <= 65533 || value >= 65536 && value <= 1114111;
    }

    static bool IsNameStartCharacter(int value)
    {
        return value == ':' || value == '_' || value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' ||
            value >= 192 && value <= 214 || value >= 216 && value <= 246 || value >= 248 && value <= 767 ||
            value >= 880 && value <= 893 || value >= 895 && value <= 8191 || value >= 8204 && value <= 8205 ||
            value >= 8304 && value <= 8591 || value >= 11264 && value <= 12271 || value >= 12289 && value <= 55295 ||
            value >= 63744 && value <= 64975 || value >= 65008 && value <= 65533 || value >= 65536 && value <= 983039;
    }

    static bool IsNameCharacter(int value)
    {
        return IsNameStartCharacter(value) || value == '-' || value == '.' || value >= '0' && value <= '9' || value == 183 ||
            value >= 768 && value <= 879 || value >= 8255 && value <= 8256;
    }

    static bool IsXmlWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    void SetError(SvgXmlValidationError value, int offset, string errorMessage)
    {
        if (error != SvgXmlValidationError.None && offset >= errorOffset) return;
        error = value;
        errorOffset = offset;
        message = errorMessage;
    }

    void FindLocation(int offset, int[] location)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < offset && index < source.Length)
        {
            char current = source[index];
            index = index + 1;
            if (current == '\r')
            {
                line = line + 1;
                column = 1;
            }
            else if (current == '\n')
            {
                if (index < 2 || source[index - 2] != '\r')
                {
                    line = line + 1;
                    column = 1;
                }
            }
            else column = column + 1;
        }
        location[0] = line;
        location[1] = column;
    }
}
