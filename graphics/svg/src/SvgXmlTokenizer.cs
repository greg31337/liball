namespace svg;

/// <summary>Splits one lexical qualified name without applying XML name validity rules.</summary>
internal class SvgXmlNameParts
{
    /// <summary>The exact qualified name.</summary>
    public string QualifiedName;
    /// <summary>The prefix before the first colon.</summary>
    public string Prefix;
    /// <summary>The local-name portion.</summary>
    public string LocalName;

    /// <summary>Creates qualified-name parts.</summary>
    /// <param name="qualifiedName">The exact lexical name.</param>
    internal SvgXmlNameParts(string qualifiedName)
    {
        QualifiedName = qualifiedName;
        int colon = qualifiedName.IndexOf(':');
        Prefix = colon < 0 ? "" : qualifiedName.Substring(0, colon);
        LocalName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1);
    }
}

/// <summary>Maintains position and bounds for source-preserving XML tokenization.</summary>
internal class SvgXmlTokenizerState
{
    string text;
    int maximumTokens;
    int maximumTokenCharacters;
    int tokenCount;
    int position;
    int line;
    int column;
    SvgXmlTokenizeError error;
    int errorOffset;
    int errorLine;
    int errorColumn;
    string message;
    System.Collections.Generic.List<SvgXmlToken> tokens;

    /// <summary>Creates tokenizer state.</summary>
    /// <param name="value">The decoded source.</param>
    /// <param name="maxTokens">The total token bound.</param>
    /// <param name="maxTokenCharacters">The raw token bound.</param>
    internal SvgXmlTokenizerState(string value, int maxTokens, int maxTokenCharacters)
    {
        text = value;
        maximumTokens = maxTokens;
        maximumTokenCharacters = maxTokenCharacters;
        tokenCount = 0;
        position = 0;
        line = 1;
        column = 1;
        error = SvgXmlTokenizeError.None;
        errorOffset = -1;
        errorLine = 0;
        errorColumn = 0;
        message = "";
        tokens = new System.Collections.Generic.List<SvgXmlToken>();
    }

    /// <summary>Tokenizes the complete state source.</summary>
    /// <returns>A complete result or token prefix and failure.</returns>
    internal SvgXmlTokenizeResult Tokenize()
    {
        while (position < text.Length && error == SvgXmlTokenizeError.None)
        {
            if (text[position] == '<') ParseMarkup();
            else if (text[position] == '&') ParseTopReference();
            else ParseText();
        }
        if (error != SvgXmlTokenizeError.None) return new SvgXmlTokenizeResult(false, error, tokens, errorOffset, errorLine, errorColumn, message);
        return new SvgXmlTokenizeResult(true, SvgXmlTokenizeError.None, tokens, -1, 0, 0, "");
    }

    void ParseMarkup()
    {
        if (Starts("<?xml") && (position + 5 >= text.Length || IsWhitespace(text[position + 5]) || text[position + 5] == '?')) ParseDeclaration();
        else if (Starts("<?")) ParseProcessingInstruction();
        else if (Starts("<!--")) ParseDelimited(SvgXmlTokenKind.Comment, 4, "-->");
        else if (Starts("<![CDATA[")) ParseDelimited(SvgXmlTokenKind.CData, 9, "]]>");
        else if (Starts("<!DOCTYPE")) ParseDoctype();
        else if (Starts("</")) ParseEndElement();
        else if (Starts("<!")) SetError(SvgXmlTokenizeError.InvalidMarkup, position, "The XML declaration markup is not recognized.");
        else ParseStartElement();
    }

    void ParseText()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        while (position < text.Length && text[position] != '<' && text[position] != '&') Advance();
        if (!CheckLength(start)) return;
        AddToken(SvgXmlTokenKind.Text, start, startLine, startColumn, text.Substring(start, position - start), "", "", "", new System.Collections.Generic.List<SvgXmlAttributeToken>(), null);
    }

    void ParseTopReference()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        SvgXmlReferenceToken? reference = ParseReference(false);
        if (reference == null) return;
        AddToken(SvgXmlTokenKind.Reference, start, startLine, startColumn, reference.RawText, reference.Value, "", "", new System.Collections.Generic.List<SvgXmlAttributeToken>(), reference);
    }

    void ParseDeclaration()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        AdvanceCount(5);
        System.Collections.Generic.List<SvgXmlAttributeToken> attributes = new System.Collections.Generic.List<SvgXmlAttributeToken>();
        while (error == SvgXmlTokenizeError.None)
        {
            bool hadWhitespace = SkipWhitespace();
            if (Starts("?>"))
            {
                AdvanceCount(2);
                if (!CheckLength(start)) return;
                AddToken(SvgXmlTokenKind.Declaration, start, startLine, startColumn, text.Substring(start, position - start), "", "xml", "", attributes, null);
                return;
            }
            if (position >= text.Length)
            {
                SetError(SvgXmlTokenizeError.UnexpectedEnd, position, "The XML declaration is not closed.");
                return;
            }
            if (!hadWhitespace)
            {
                SetError(SvgXmlTokenizeError.InvalidAttribute, position, "XML declaration attributes require separating whitespace.");
                return;
            }
            SvgXmlAttributeToken? attribute = ParseAttribute();
            if (attribute == null) return;
            attributes.Add(attribute);
        }
    }

    void ParseProcessingInstruction()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        AdvanceCount(2);
        int nameStart = position;
        string name = ReadName();
        if (name.Length == 0)
        {
            SetError(SvgXmlTokenizeError.ExpectedName, nameStart, "A processing-instruction target is required.");
            return;
        }
        int valueStart = position;
        if (position < text.Length && IsWhitespace(text[position]))
        {
            SkipWhitespace();
            valueStart = position;
        }
        while (position < text.Length && !Starts("?>"))
        {
            Advance();
            if (!CheckLength(start)) return;
        }
        if (position >= text.Length)
        {
            SetError(SvgXmlTokenizeError.UnexpectedEnd, position, "The processing instruction is not closed.");
            return;
        }
        string value = text.Substring(valueStart, position - valueStart);
        AdvanceCount(2);
        SvgXmlNameParts parts = new SvgXmlNameParts(name);
        AddToken(SvgXmlTokenKind.ProcessingInstruction, start, startLine, startColumn, text.Substring(start, position - start), value, parts.QualifiedName, parts.Prefix, new System.Collections.Generic.List<SvgXmlAttributeToken>(), null);
    }

    void ParseDelimited(SvgXmlTokenKind kind, int openerLength, string close)
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        AdvanceCount(openerLength);
        int valueStart = position;
        while (position < text.Length && !Starts(close))
        {
            Advance();
            if (!CheckLength(start)) return;
        }
        if (position >= text.Length)
        {
            SetError(SvgXmlTokenizeError.UnexpectedEnd, position, kind == SvgXmlTokenKind.Comment ? "The XML comment is not closed." : "The CDATA section is not closed.");
            return;
        }
        string value = text.Substring(valueStart, position - valueStart);
        AdvanceCount(close.Length);
        if (!CheckLength(start)) return;
        AddToken(kind, start, startLine, startColumn, text.Substring(start, position - start), value, "", "", new System.Collections.Generic.List<SvgXmlAttributeToken>(), null);
    }

    void ParseDoctype()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        AdvanceCount(9);
        int valueStart = position;
        int subsetDepth = 0;
        char quote = '\0';
        while (position < text.Length)
        {
            char current = text[position];
            if (quote != '\0')
            {
                if (current == quote) quote = '\0';
                Advance();
            }
            else if (current == '\'' || current == '"')
            {
                quote = current;
                Advance();
            }
            else if (current == '[')
            {
                subsetDepth = subsetDepth + 1;
                Advance();
            }
            else if (current == ']' && subsetDepth > 0)
            {
                subsetDepth = subsetDepth - 1;
                Advance();
            }
            else if (current == '>' && subsetDepth == 0)
            {
                string value = text.Substring(valueStart, position - valueStart);
                Advance();
                if (!CheckLength(start)) return;
                AddToken(SvgXmlTokenKind.Doctype, start, startLine, startColumn, text.Substring(start, position - start), value, "", "", new System.Collections.Generic.List<SvgXmlAttributeToken>(), null);
                return;
            }
            else Advance();
            if (!CheckLength(start)) return;
        }
        SetError(SvgXmlTokenizeError.UnexpectedEnd, position, "The document type declaration is not closed.");
    }

    void ParseEndElement()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        AdvanceCount(2);
        int nameStart = position;
        string name = ReadName();
        if (name.Length == 0)
        {
            SetError(SvgXmlTokenizeError.ExpectedName, nameStart, "An end-element qualified name is required.");
            return;
        }
        SkipWhitespace();
        if (position >= text.Length)
        {
            SetError(SvgXmlTokenizeError.UnexpectedEnd, position, "The end-element tag is not closed.");
            return;
        }
        if (text[position] != '>')
        {
            SetError(SvgXmlTokenizeError.InvalidMarkup, position, "Unexpected text follows the end-element name.");
            return;
        }
        Advance();
        if (!CheckLength(start)) return;
        SvgXmlNameParts parts = new SvgXmlNameParts(name);
        AddToken(SvgXmlTokenKind.EndElement, start, startLine, startColumn, text.Substring(start, position - start), "", parts.QualifiedName, parts.Prefix, new System.Collections.Generic.List<SvgXmlAttributeToken>(), null);
    }

    void ParseStartElement()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return;
        Advance();
        int nameStart = position;
        string name = ReadName();
        if (name.Length == 0)
        {
            SetError(SvgXmlTokenizeError.ExpectedName, nameStart, "A start-element qualified name is required.");
            return;
        }
        System.Collections.Generic.List<SvgXmlAttributeToken> attributes = new System.Collections.Generic.List<SvgXmlAttributeToken>();
        SvgXmlTokenKind kind = SvgXmlTokenKind.StartElement;
        while (error == SvgXmlTokenizeError.None)
        {
            bool hadWhitespace = SkipWhitespace();
            if (Starts("/>"))
            {
                kind = SvgXmlTokenKind.EmptyElement;
                AdvanceCount(2);
                break;
            }
            if (position < text.Length && text[position] == '>')
            {
                Advance();
                break;
            }
            if (position >= text.Length)
            {
                SetError(SvgXmlTokenizeError.UnexpectedEnd, position, "The start-element tag is not closed.");
                return;
            }
            if (!hadWhitespace)
            {
                SetError(SvgXmlTokenizeError.InvalidAttribute, position, "XML attributes require separating whitespace.");
                return;
            }
            SvgXmlAttributeToken? attribute = ParseAttribute();
            if (attribute == null) return;
            attributes.Add(attribute);
            if (!CheckLength(start)) return;
        }
        if (error != SvgXmlTokenizeError.None) return;
        if (!CheckLength(start)) return;
        SvgXmlNameParts parts = new SvgXmlNameParts(name);
        AddToken(kind, start, startLine, startColumn, text.Substring(start, position - start), "", parts.QualifiedName, parts.Prefix, attributes, null);
    }

    SvgXmlAttributeToken? ParseAttribute()
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (!ReserveToken(start)) return null;
        string name = ReadName();
        if (name.Length == 0)
        {
            SetError(SvgXmlTokenizeError.ExpectedName, start, "An attribute qualified name is required.");
            return null;
        }
        SkipWhitespace();
        if (position >= text.Length || text[position] != '=')
        {
            SetError(SvgXmlTokenizeError.InvalidAttribute, position, "An attribute equals sign is required.");
            return null;
        }
        Advance();
        SkipWhitespace();
        if (position >= text.Length || text[position] != '\'' && text[position] != '"')
        {
            SetError(SvgXmlTokenizeError.InvalidAttribute, position, "An attribute value must be quoted.");
            return null;
        }
        char quote = text[position];
        Advance();
        int valueStart = position;
        System.Collections.Generic.List<SvgXmlReferenceToken> references = new System.Collections.Generic.List<SvgXmlReferenceToken>();
        while (position < text.Length && text[position] != quote)
        {
            if (text[position] == '<')
            {
                SetError(SvgXmlTokenizeError.InvalidAttribute, position, "A less-than sign is not allowed in an attribute value.");
                return null;
            }
            if (text[position] == '&')
            {
                SvgXmlReferenceToken? reference = ParseReference(true);
                if (reference == null) return null;
                references.Add(reference);
            }
            else Advance();
            if (!CheckLength(start)) return null;
        }
        if (position >= text.Length)
        {
            SetError(SvgXmlTokenizeError.UnexpectedEnd, position, "The quoted attribute value is not closed.");
            return null;
        }
        string value = text.Substring(valueStart, position - valueStart);
        Advance();
        if (!CheckLength(start)) return null;
        SvgXmlNameParts parts = new SvgXmlNameParts(name);
        return new SvgXmlAttributeToken(text.Substring(start, position - start), parts.QualifiedName, parts.Prefix, parts.LocalName, value, quote, references, start, position, startLine, startColumn, line, column);
    }

    SvgXmlReferenceToken? ParseReference(bool nested)
    {
        int start = position;
        int startLine = line;
        int startColumn = column;
        if (nested && !ReserveToken(start)) return null;
        Advance();
        int valueStart = position;
        while (position < text.Length && text[position] != ';')
        {
            char current = text[position];
            if (current == '<' || current == '&' || current == '\'' || current == '"' || IsWhitespace(current))
            {
                SetError(SvgXmlTokenizeError.InvalidReference, position, "The XML reference is malformed.");
                return null;
            }
            Advance();
            if (!CheckLength(start)) return null;
        }
        if (position >= text.Length || position == valueStart)
        {
            SetError(SvgXmlTokenizeError.InvalidReference, position, "The XML reference is not terminated.");
            return null;
        }
        string value = text.Substring(valueStart, position - valueStart);
        Advance();
        if (!CheckLength(start)) return null;
        return new SvgXmlReferenceToken(text.Substring(start, position - start), value, start, position, startLine, startColumn, line, column);
    }

    string ReadName()
    {
        int start = position;
        while (position < text.Length && !IsNameDelimiter(text[position])) Advance();
        return text.Substring(start, position - start);
    }

    bool SkipWhitespace()
    {
        int start = position;
        while (position < text.Length && IsWhitespace(text[position])) Advance();
        return position > start;
    }

    void AddToken(SvgXmlTokenKind kind, int start, int startLine, int startColumn, string rawText, string value, string qualifiedName, string prefix, System.Collections.Generic.List<SvgXmlAttributeToken> attributes, SvgXmlReferenceToken? reference)
    {
        string localName = "";
        if (qualifiedName.Length != 0) localName = new SvgXmlNameParts(qualifiedName).LocalName;
        tokens.Add(new SvgXmlToken(kind, rawText, value, qualifiedName, prefix, localName, attributes, reference, start, position, startLine, startColumn, line, column));
    }

    bool ReserveToken(int offset)
    {
        if (tokenCount >= maximumTokens)
        {
            SetError(SvgXmlTokenizeError.TokenLimitExceeded, offset, "The configured XML token count was exceeded.");
            return false;
        }
        tokenCount = tokenCount + 1;
        return true;
    }

    bool CheckLength(int start)
    {
        if (position - start <= maximumTokenCharacters) return true;
        SetError(SvgXmlTokenizeError.TokenTooLong, start + maximumTokenCharacters, "An XML token exceeded MaxXmlTokenCharacters.");
        return false;
    }

    bool Starts(string value)
    {
        if (position + value.Length > text.Length) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (text[position + index] != value[index]) return false;
            index = index + 1;
        }
        return true;
    }

    void AdvanceCount(int count)
    {
        int index = 0;
        while (index < count && position < text.Length)
        {
            Advance();
            index = index + 1;
        }
    }

    void Advance()
    {
        char current = text[position];
        position = position + 1;
        if (current == '\r')
        {
            line = line + 1;
            column = 1;
        }
        else if (current == '\n')
        {
            if (position < 2 || text[position - 2] != '\r')
            {
                line = line + 1;
                column = 1;
            }
        }
        else column = column + 1;
    }

    void SetError(SvgXmlTokenizeError value, int offset, string errorMessage)
    {
        if (error != SvgXmlTokenizeError.None) return;
        error = value;
        errorOffset = offset;
        errorLine = 1;
        errorColumn = 1;
        int index = 0;
        while (index < offset && index < text.Length)
        {
            char current = text[index];
            index = index + 1;
            if (current == '\r')
            {
                errorLine = errorLine + 1;
                errorColumn = 1;
            }
            else if (current == '\n')
            {
                if (index < 2 || text[index - 2] != '\r')
                {
                    errorLine = errorLine + 1;
                    errorColumn = 1;
                }
            }
            else errorColumn = errorColumn + 1;
        }
        message = errorMessage;
    }

    static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static bool IsNameDelimiter(char value)
    {
        return IsWhitespace(value) || value == '/' || value == '>' || value == '=' || value == '?' || value == '!' || value == '\'' || value == '"' || value == '<';
    }
}
