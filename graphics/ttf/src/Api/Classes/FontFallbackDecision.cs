namespace ttf;

/// <summary>Describes one immutable fallback decision reported during shaping.</summary>
public class FontFallbackDecision : IFontFallbackDecision
{
    FontFallbackDecisionKind kind;
    int utf16Start;
    int utf16Length;
    string script;
    string requestedFamilyName;
    string resolvedFamilyName;
    FontFace? face;
    FontInstance? instance;
    int codePoint;
    int tableTag;

    internal FontFallbackDecision(
        FontFallbackDecisionKind kind,
        int utf16Start,
        int utf16Length,
        string script,
        string requestedFamilyName,
        string resolvedFamilyName,
        FontFace? face,
        FontInstance? instance,
        int codePoint,
        int tableTag)
    {
        this.kind = kind;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.script = script;
        this.requestedFamilyName = requestedFamilyName;
        this.resolvedFamilyName = resolvedFamilyName;
        this.face = face;
        this.instance = instance;
        this.codePoint = codePoint;
        this.tableTag = tableTag;
    }

    /// <summary>Gets the machine-readable decision kind.</summary>
    /// <returns>The fallback decision kind.</returns>
    public FontFallbackDecisionKind GetKind() { return kind; }

    /// <summary>Gets the affected source UTF-16 start.</summary>
    /// <returns>The zero-based source offset.</returns>
    public int GetUtf16Start() { return utf16Start; }

    /// <summary>Gets the affected source UTF-16 length.</summary>
    /// <returns>The nonnegative source length.</returns>
    public int GetUtf16Length() { return utf16Length; }

    /// <summary>Gets the canonical script associated with the decision.</summary>
    /// <returns>The four-letter script code, or an empty string.</returns>
    public string GetScript() { return script; }

    /// <summary>Gets the caller-requested family name.</summary>
    /// <returns>The concrete or generic requested family name.</returns>
    public string GetRequestedFamilyName() { return requestedFamilyName; }

    /// <summary>Gets the selected configured family name.</summary>
    /// <returns>The selected family name, or an empty string.</returns>
    public string GetResolvedFamilyName() { return resolvedFamilyName; }

    /// <summary>Gets the selected face when one applies.</summary>
    /// <returns>The selected face, or null.</returns>
    public FontFace? GetFace() { return face; }

    /// <summary>Gets the selected instance when one applies.</summary>
    /// <returns>The selected instance, or null.</returns>
    public FontInstance? GetInstance() { return instance; }

    /// <summary>Gets the first missing Unicode scalar.</summary>
    /// <returns>The Unicode scalar, or <c>-1</c>.</returns>
    public int GetCodePoint() { return codePoint; }

    /// <summary>Gets the unsupported shaping table tag.</summary>
    /// <returns>The big-endian table tag, or zero.</returns>
    public int GetTableTag() { return tableTag; }
}
