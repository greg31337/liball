namespace unicode;

/// <summary>
/// Describes scalar-level metadata useful to a PDF text layout engine.
/// </summary>
public class UnicodePdfScalarLayout
{
    /// <summary>The scalar value.</summary>
    public int CodePoint;
    /// <summary>The scalar's UTF-16 start index.</summary>
    public int Utf16Index;
    /// <summary>The scalar's UTF-16 length.</summary>
    public int Utf16Length;
    /// <summary>The resolved layout script.</summary>
    public UnicodeScript Script;
    /// <summary>The resolved simple layout direction.</summary>
    public UnicodePdfBidiDirection Direction;
    /// <summary>The paired-bracket role.</summary>
    public UnicodeBidiPairedBracketType PairedBracketType;
    /// <summary>The paired bracket, or the input scalar when none is assigned.</summary>
    public int PairedBracketCodePoint;
    /// <summary>Whether the Unicode bidi-mirrored property is assigned.</summary>
    public bool IsBidiMirrored;
    /// <summary>The bidi mirror, or the input scalar when none is assigned.</summary>
    public int MirrorCodePoint;
    /// <summary>The scalar's PDF width classification.</summary>
    public UnicodePdfTextWidth Width;

    /// <summary>
    /// Initializes scalar-level PDF layout metadata.
    /// </summary>
    /// <param name="codePoint">The Unicode scalar.</param>
    /// <param name="utf16Index">The scalar's UTF-16 start index.</param>
    /// <param name="utf16Length">The scalar's UTF-16 length.</param>
    /// <param name="script">The resolved script.</param>
    /// <param name="direction">The resolved layout direction.</param>
    /// <param name="pairedBracketType">The paired-bracket role.</param>
    /// <param name="pairedBracketCodePoint">The paired bracket.</param>
    /// <param name="isBidiMirrored">Whether the scalar is mirrored.</param>
    /// <param name="mirrorCodePoint">The mirrored scalar.</param>
    /// <param name="width">The width classification.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a scalar, index, or length is invalid.</exception>
    public UnicodePdfScalarLayout(
        int codePoint,
        int utf16Index,
        int utf16Length,
        UnicodeScript script,
        UnicodePdfBidiDirection direction,
        UnicodeBidiPairedBracketType pairedBracketType,
        int pairedBracketCodePoint,
        bool isBidiMirrored,
        int mirrorCodePoint,
        UnicodePdfTextWidth width)
    {
        if (!UnicodeScalar.Instance.IsValid(codePoint))
        {
            throw new System.ArgumentOutOfRangeException("codePoint");
        }

        if (utf16Index < 0)
        {
            throw new System.ArgumentOutOfRangeException("utf16Index");
        }

        if (utf16Length < 1 || utf16Length > 2)
        {
            throw new System.ArgumentOutOfRangeException("utf16Length");
        }

        if (!UnicodeScalar.Instance.IsValid(pairedBracketCodePoint))
        {
            throw new System.ArgumentOutOfRangeException("pairedBracketCodePoint");
        }

        if (!UnicodeScalar.Instance.IsValid(mirrorCodePoint))
        {
            throw new System.ArgumentOutOfRangeException("mirrorCodePoint");
        }

        CodePoint = codePoint;
        Utf16Index = utf16Index;
        Utf16Length = utf16Length;
        Script = script;
        Direction = direction;
        PairedBracketType = pairedBracketType;
        PairedBracketCodePoint = pairedBracketCodePoint;
        IsBidiMirrored = isBidiMirrored;
        MirrorCodePoint = mirrorCodePoint;
        Width = width;
    }
}
