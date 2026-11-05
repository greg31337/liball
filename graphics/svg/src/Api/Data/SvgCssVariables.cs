namespace svg;

/// <summary>Represents one source-preserving custom-property token.</summary>
public class SvgCssStoredToken
{
    /// <summary>The token category.</summary>
    public SvgCssStoredTokenKind Kind;
    /// <summary>The exact raw token text.</summary>
    public string Text;
    /// <summary>The token starting source offset.</summary>
    public int StartOffset;
    /// <summary>The exclusive token ending source offset.</summary>
    public int EndOffset;

    /// <summary>Creates a source-preserving stored token.</summary>
    /// <param name="kind">The token category.</param>
    /// <param name="text">The raw token text.</param>
    /// <param name="startOffset">The starting offset.</param>
    /// <param name="endOffset">The ending offset.</param>
    public SvgCssStoredToken(SvgCssStoredTokenKind kind, string text, int startOffset, int endOffset)
    {
        if (text == null || text.Length == 0 || startOffset < 0 || endOffset <= startOffset) throw new System.ArgumentOutOfRangeException("text");
        Kind = kind;
        Text = text;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }
}

/// <summary>Stores one custom property's exact component-value token stream.</summary>
public class SvgCustomPropertyValue
{
    /// <summary>The case-sensitive custom-property name.</summary>
    public string Name;
    /// <summary>The exact authored component-value text.</summary>
    public string RawText;
    /// <summary>The source-preserving token snapshot.</summary>
    public System.Collections.Generic.List<SvgCssStoredToken> Tokens;
    /// <summary>Whether the mutable stored value is valid.</summary>
    public bool IsValid;

    /// <summary>Creates a custom-property value and copies its tokens.</summary>
    /// <param name="name">The custom-property name.</param>
    /// <param name="rawText">The exact raw value.</param>
    /// <param name="tokens">The stored tokens.</param>
    public SvgCustomPropertyValue(string name, string rawText, System.Collections.Generic.List<SvgCssStoredToken> tokens)
    {
        if (name == null || rawText == null || tokens == null) throw new System.ArgumentNullException("name");
        Name = name;
        RawText = rawText;
        Tokens = new System.Collections.Generic.List<SvgCssStoredToken>(tokens);
        IsValid = true;
    }
}

/// <summary>Contains a custom-property storage operation result.</summary>
public class SvgCustomPropertyStoreResult
{
    /// <summary>Whether storage succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgCssVariableError Error;
    /// <summary>The stored value.</summary>
    public SvgCustomPropertyValue? Value;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;

    /// <summary>Creates a custom-property storage result.</summary>
    /// <param name="success">Whether storage succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The stored value.</param>
    /// <param name="errorOffset">The failure offset.</param>
    public SvgCustomPropertyStoreResult(bool success, SvgCssVariableError error, SvgCustomPropertyValue? value, int errorOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a bounded custom-property substitution result.</summary>
public class SvgCssVariableResolveResult
{
    /// <summary>Whether every variable resolved.</summary>
    public bool Success;
    /// <summary>The stable failure kind.</summary>
    public SvgCssVariableError Error;
    /// <summary>The substituted component-value text.</summary>
    public string Text;
    /// <summary>The unique referenced custom-property names in encounter order.</summary>
    public System.Collections.Generic.List<string> Dependencies;
    /// <summary>The exact failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The resulting stored-token count.</summary>
    public int TokenCount;

    /// <summary>Creates a variable resolution result and copies dependencies.</summary>
    /// <param name="success">Whether resolution succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="text">The resolved text.</param>
    /// <param name="dependencies">The referenced names.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="tokenCount">The expanded token count.</param>
    public SvgCssVariableResolveResult(bool success, SvgCssVariableError error, string text, System.Collections.Generic.List<string> dependencies, int errorOffset, int tokenCount)
    {
        Success = success;
        Error = error;
        Text = text;
        Dependencies = new System.Collections.Generic.List<string>(dependencies);
        ErrorOffset = errorOffset;
        TokenCount = tokenCount;
    }
}
