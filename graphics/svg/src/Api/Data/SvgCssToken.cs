namespace svg;

/// <summary>Represents one source-preserving CSS lexical token.</summary>
public class SvgCssToken
{
    /// <summary>The token kind.</summary>
    public SvgCssTokenKind Kind;
    /// <summary>The exact authored token text.</summary>
    public string RawText;
    /// <summary>The decoded token payload without structural delimiters.</summary>
    public string Value;
    /// <summary>The parsed numeric value for number, percentage, and dimension tokens.</summary>
    public double NumberValue;
    /// <summary>The authored numeric representation subtype.</summary>
    public SvgCssNumberType NumberType;
    /// <summary>The decoded unit for a dimension token.</summary>
    public string Unit;
    /// <summary>The contextual subtype for a hash token.</summary>
    public SvgCssHashType HashType;
    /// <summary>The inclusive first scalar of a Unicode range.</summary>
    public int UnicodeRangeStart;
    /// <summary>The inclusive last scalar of a Unicode range.</summary>
    public int UnicodeRangeEnd;
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

    /// <summary>Creates a source-preserving CSS token.</summary>
    /// <param name="kind">The token kind.</param>
    /// <param name="rawText">The exact authored text.</param>
    /// <param name="value">The decoded payload.</param>
    /// <param name="startOffset">The starting source offset.</param>
    /// <param name="endOffset">The ending source offset.</param>
    /// <param name="startLine">The starting line.</param>
    /// <param name="startColumn">The starting column.</param>
    /// <param name="endLine">The ending line.</param>
    /// <param name="endColumn">The ending column.</param>
    public SvgCssToken(SvgCssTokenKind kind, string rawText, string value, int startOffset, int endOffset, int startLine, int startColumn, int endLine, int endColumn)
    {
        Kind = kind;
        RawText = rawText;
        Value = value;
        NumberValue = 0;
        NumberType = SvgCssNumberType.Number;
        Unit = "";
        HashType = SvgCssHashType.Unrestricted;
        UnicodeRangeStart = 0;
        UnicodeRangeEnd = 0;
        StartOffset = startOffset;
        EndOffset = endOffset;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}

/// <summary>Contains a bounded CSS tokenization result.</summary>
public class SvgCssTokenizeResult
{
    /// <summary>Whether the complete source was tokenized within its limits.</summary>
    public bool Success;
    /// <summary>The stable tokenizer failure.</summary>
    public SvgCssTokenizeError Error;
    /// <summary>The source-preserving token prefix.</summary>
    public System.Collections.Generic.List<SvgCssToken> Tokens;
    /// <summary>The number of recoverable CSS parse errors represented by bad tokens or replacement characters.</summary>
    public int ParseErrorCount;
    /// <summary>The exact fatal failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The one-based fatal failure line, or zero.</summary>
    public int ErrorLine;
    /// <summary>The one-based fatal failure column, or zero.</summary>
    public int ErrorColumn;
    /// <summary>The stable human-readable fatal failure message.</summary>
    public string Message;

    /// <summary>Creates a CSS tokenization result and copies its tokens.</summary>
    /// <param name="success">Whether the complete source was tokenized.</param>
    /// <param name="error">The fatal failure.</param>
    /// <param name="tokens">The token prefix.</param>
    /// <param name="parseErrorCount">The recoverable parse-error count.</param>
    /// <param name="errorOffset">The fatal failure offset.</param>
    /// <param name="errorLine">The fatal failure line.</param>
    /// <param name="errorColumn">The fatal failure column.</param>
    /// <param name="message">The fatal failure message.</param>
    public SvgCssTokenizeResult(bool success, SvgCssTokenizeError error, System.Collections.Generic.List<SvgCssToken> tokens, int parseErrorCount, int errorOffset, int errorLine, int errorColumn, string message)
    {
        Success = success;
        Error = error;
        Tokens = new System.Collections.Generic.List<SvgCssToken>(tokens);
        ParseErrorCount = parseErrorCount;
        ErrorOffset = errorOffset;
        ErrorLine = errorLine;
        ErrorColumn = errorColumn;
        Message = message;
    }
}
