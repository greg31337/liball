namespace svg;

/// <summary>Represents a source-preserving XML character or entity reference.</summary>
public class SvgXmlReferenceToken
{
    /// <summary>The exact text including ampersand and semicolon.</summary>
    public string RawText;
    /// <summary>The unexpanded reference body.</summary>
    public string Value;
    /// <summary>The decoded XML character, or <c>null</c> before well-formedness validation.</summary>
    public string? DecodedValue;
    /// <summary>The inclusive UTF-16 source offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive UTF-16 source offset.</summary>
    public int EndOffset;
    /// <summary>The one-based starting line.</summary>
    public int StartLine;
    /// <summary>The one-based starting column.</summary>
    public int StartColumn;
    /// <summary>The one-based exclusive ending line.</summary>
    public int EndLine;
    /// <summary>The one-based exclusive ending column.</summary>
    public int EndColumn;

    /// <summary>Creates a source-preserving reference token.</summary>
    /// <param name="rawText">The exact reference text.</param>
    /// <param name="value">The unexpanded reference body.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    /// <param name="startLine">The starting line.</param>
    /// <param name="startColumn">The starting column.</param>
    /// <param name="endLine">The ending line.</param>
    /// <param name="endColumn">The ending column.</param>
    public SvgXmlReferenceToken(string rawText, string value, int startOffset, int endOffset, int startLine, int startColumn, int endLine, int endColumn)
    {
        RawText = rawText;
        Value = value;
        DecodedValue = null;
        StartOffset = startOffset;
        EndOffset = endOffset;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}

/// <summary>Represents one source-preserving qualified XML attribute.</summary>
public class SvgXmlAttributeToken
{
    /// <summary>The exact attribute assignment without preceding whitespace.</summary>
    public string RawText;
    /// <summary>The exact qualified name.</summary>
    public string QualifiedName;
    /// <summary>The prefix before the first colon, or an empty string.</summary>
    public string Prefix;
    /// <summary>The local-name portion.</summary>
    public string LocalName;
    /// <summary>The resolved namespace URI, or <c>null</c> before namespace processing.</summary>
    public string? NamespaceUri;
    /// <summary>The exact unexpanded value inside quotes.</summary>
    public string Value;
    /// <summary>The decoded and XML-normalized value, or <c>null</c> before well-formedness validation.</summary>
    public string? NormalizedValue;
    /// <summary>The authored quote character.</summary>
    public char Quote;
    /// <summary>The references contained in the value.</summary>
    public System.Collections.Generic.List<SvgXmlReferenceToken> References;
    /// <summary>The inclusive UTF-16 source offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive UTF-16 source offset.</summary>
    public int EndOffset;
    /// <summary>The one-based starting line.</summary>
    public int StartLine;
    /// <summary>The one-based starting column.</summary>
    public int StartColumn;
    /// <summary>The one-based exclusive ending line.</summary>
    public int EndLine;
    /// <summary>The one-based exclusive ending column.</summary>
    public int EndColumn;

    /// <summary>Creates a source-preserving attribute token.</summary>
    /// <param name="rawText">The exact assignment text.</param>
    /// <param name="qualifiedName">The qualified name.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="localName">The local name.</param>
    /// <param name="value">The unexpanded value.</param>
    /// <param name="quote">The quote character.</param>
    /// <param name="references">The value references.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    /// <param name="startLine">The starting line.</param>
    /// <param name="startColumn">The starting column.</param>
    /// <param name="endLine">The ending line.</param>
    /// <param name="endColumn">The ending column.</param>
    public SvgXmlAttributeToken(string rawText, string qualifiedName, string prefix, string localName, string value, char quote, System.Collections.Generic.List<SvgXmlReferenceToken> references, int startOffset, int endOffset, int startLine, int startColumn, int endLine, int endColumn)
    {
        RawText = rawText;
        QualifiedName = qualifiedName;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = null;
        Value = value;
        NormalizedValue = null;
        Quote = quote;
        References = new System.Collections.Generic.List<SvgXmlReferenceToken>(references);
        StartOffset = startOffset;
        EndOffset = endOffset;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}

/// <summary>Represents one top-level source-preserving XML lexical token.</summary>
public class SvgXmlToken
{
    /// <summary>The lexical token kind.</summary>
    public SvgXmlTokenKind Kind;
    /// <summary>The exact raw token text.</summary>
    public string RawText;
    /// <summary>The preserved content payload with delimiters removed where applicable.</summary>
    public string Value;
    /// <summary>The decoded and XML-normalized payload, or <c>null</c> before well-formedness validation.</summary>
    public string? NormalizedValue;
    /// <summary>The exact element, declaration, or processing-instruction qualified name.</summary>
    public string QualifiedName;
    /// <summary>The name prefix, or an empty string.</summary>
    public string Prefix;
    /// <summary>The local-name portion, or an empty string.</summary>
    public string LocalName;
    /// <summary>The resolved element namespace URI, or <c>null</c> before namespace processing.</summary>
    public string? NamespaceUri;
    /// <summary>The ordered source-preserving attributes.</summary>
    public System.Collections.Generic.List<SvgXmlAttributeToken> Attributes;
    /// <summary>The reference payload when <see cref="SvgXmlTokenKind.Reference"/> applies.</summary>
    public SvgXmlReferenceToken? Reference;
    /// <summary>The inclusive UTF-16 source offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive UTF-16 source offset.</summary>
    public int EndOffset;
    /// <summary>The one-based starting line.</summary>
    public int StartLine;
    /// <summary>The one-based starting column.</summary>
    public int StartColumn;
    /// <summary>The one-based exclusive ending line.</summary>
    public int EndLine;
    /// <summary>The one-based exclusive ending column.</summary>
    public int EndColumn;

    /// <summary>Creates one XML lexical token and copies its attributes.</summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="rawText">The exact raw text.</param>
    /// <param name="value">The preserved payload.</param>
    /// <param name="qualifiedName">The qualified name.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="localName">The local name.</param>
    /// <param name="attributes">The attributes.</param>
    /// <param name="reference">The reference payload.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    /// <param name="startLine">The starting line.</param>
    /// <param name="startColumn">The starting column.</param>
    /// <param name="endLine">The ending line.</param>
    /// <param name="endColumn">The ending column.</param>
    public SvgXmlToken(SvgXmlTokenKind kind, string rawText, string value, string qualifiedName, string prefix, string localName, System.Collections.Generic.List<SvgXmlAttributeToken> attributes, SvgXmlReferenceToken? reference, int startOffset, int endOffset, int startLine, int startColumn, int endLine, int endColumn)
    {
        Kind = kind;
        RawText = rawText;
        Value = value;
        NormalizedValue = null;
        QualifiedName = qualifiedName;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = null;
        Attributes = new System.Collections.Generic.List<SvgXmlAttributeToken>(attributes);
        Reference = reference;
        StartOffset = startOffset;
        EndOffset = endOffset;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}

/// <summary>Contains a bounded XML lexical tokenization result.</summary>
public class SvgXmlTokenizeResult
{
    /// <summary>Whether the complete decoded source was tokenized.</summary>
    public bool Success;
    /// <summary>The stable lexical failure.</summary>
    public SvgXmlTokenizeError Error;
    /// <summary>The successfully parsed top-level token prefix.</summary>
    public System.Collections.Generic.List<SvgXmlToken> Tokens;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The one-based failure line, or zero.</summary>
    public int ErrorLine;
    /// <summary>The one-based failure column, or zero.</summary>
    public int ErrorColumn;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;

    /// <summary>Creates a tokenization result and copies its token list.</summary>
    /// <param name="success">Whether tokenization succeeded.</param>
    /// <param name="error">The lexical failure.</param>
    /// <param name="tokens">The parsed token prefix.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="errorLine">The failure line.</param>
    /// <param name="errorColumn">The failure column.</param>
    /// <param name="message">The failure message.</param>
    public SvgXmlTokenizeResult(bool success, SvgXmlTokenizeError error, System.Collections.Generic.List<SvgXmlToken> tokens, int errorOffset, int errorLine, int errorColumn, string message)
    {
        Success = success;
        Error = error;
        Tokens = new System.Collections.Generic.List<SvgXmlToken>(tokens);
        ErrorOffset = errorOffset;
        ErrorLine = errorLine;
        ErrorColumn = errorColumn;
        Message = message;
    }
}
