using System;

namespace html;
/// <summary>Describes one mapped location in normalized and original HTML input.</summary>
/// <remarks>
/// <para>Character offsets are zero-based, while line and column numbers are one-based. A position can retain both the offset after HTML input normalization and the corresponding offset in the caller's original text.</para>
/// <para>Use <see cref = "IsKnown"/> before reading coordinates from a position returned by parsing. Unknown positions report <c>-1</c> for character offsets, <c>0</c> for line and column, and no byte offset.</para>
/// </remarks>
public class HtmlSourcePosition : IHtmlSourcePosition
{
    internal int normalizedCharacterOffset;
    internal int characterOffset;
    internal int line;
    internal int column;
    internal bool hasByteOffset;
    internal long byteOffset;
    internal HtmlSourcePosition(int normalizedCharacterOffset, int characterOffset, int line, int column, bool hasByteOffset, long byteOffset)
    {
        this.normalizedCharacterOffset = normalizedCharacterOffset;
        this.characterOffset = characterOffset;
        this.line = line;
        this.column = column;
        this.hasByteOffset = hasByteOffset;
        this.byteOffset = byteOffset;
    }

    /// <summary>Creates a source position whose normalized and original character offsets are equal.</summary>
    /// <param name = "characterOffset">The zero-based character offset.</param>
    /// <param name = "line">The one-based line number.</param>
    /// <param name = "column">The one-based column number.</param>
    /// <returns>A known source position using the same value for normalized and original character offsets.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an offset is negative or a line or column is not positive.</exception>
    internal static HtmlSourcePosition Create(int characterOffset, int line, int column)
    {
        return CreateMapped(characterOffset, characterOffset, line, column);
    }

    /// <summary>Creates a source position with distinct normalized and original character offsets.</summary>
    /// <param name = "normalizedCharacterOffset">The zero-based offset after input normalization.</param>
    /// <param name = "characterOffset">The zero-based original character offset.</param>
    /// <param name = "line">The one-based source line.</param>
    /// <param name = "column">The one-based source column.</param>
    /// <returns>A known source position containing separate normalized and original character offsets.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an offset is negative or a line or column is not positive.</exception>
    internal static HtmlSourcePosition CreateMapped(int normalizedCharacterOffset, int characterOffset, int line, int column)
    {
        ValidateKnown(normalizedCharacterOffset, characterOffset, line, column);
        return new HtmlSourcePosition(normalizedCharacterOffset, characterOffset, line, column, false, -1);
    }

    /// <summary>Creates a source position that also records its original byte offset.</summary>
    /// <param name = "characterOffset">The zero-based original character offset.</param>
    /// <param name = "line">The one-based source line.</param>
    /// <param name = "column">The one-based source column.</param>
    /// <param name = "byteOffset">The zero-based offset in the original byte input.</param>
    /// <returns>A known source position that includes the original byte offset.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an offset is negative or a line or column is not positive.</exception>
    internal static HtmlSourcePosition CreateWithByteOffset(int characterOffset, int line, int column, long byteOffset)
    {
        return CreateMappedWithByteOffset(characterOffset, characterOffset, line, column, byteOffset);
    }

    /// <summary>Creates a mapped source position that also records its original byte offset.</summary>
    /// <param name = "normalizedCharacterOffset">The zero-based offset after input normalization.</param>
    /// <param name = "characterOffset">The zero-based original character offset.</param>
    /// <param name = "line">The one-based source line.</param>
    /// <param name = "column">The one-based source column.</param>
    /// <param name = "byteOffset">The zero-based offset in the original byte input.</param>
    /// <returns>A known source position containing normalized, original-character, and original-byte offsets.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    internal static HtmlSourcePosition CreateMappedWithByteOffset(int normalizedCharacterOffset, int characterOffset, int line, int column, long byteOffset)
    {
        ValidateKnown(normalizedCharacterOffset, characterOffset, line, column);
        if (byteOffset < 0)
        {
            throw new ArgumentOutOfRangeException("byteOffset", byteOffset, "A byte offset cannot be negative.");
        }

        return new HtmlSourcePosition(normalizedCharacterOffset, characterOffset, line, column, true, byteOffset);
    }

    /// <summary>Creates a value that explicitly represents an unknown source location.</summary>
    /// <returns>An explicitly unknown source position.</returns>
    internal static HtmlSourcePosition CreateUnknown()
    {
        return new HtmlSourcePosition(-1, -1, 0, 0, false, -1);
    }

    /// <summary>Reports whether source location information is available.</summary>
    /// <returns><c>true</c> when source location information is available; otherwise, <c>false</c>.</returns>
    public bool IsKnown()
    {
        return this.normalizedCharacterOffset >= 0;
    }

    /// <summary>Gets the zero-based offset after HTML input normalization.</summary>
    /// <returns>The normalized character offset, or <c>-1</c> when the position is unknown.</returns>
    public int GetNormalizedCharacterOffset()
    {
        return this.normalizedCharacterOffset;
    }

    /// <summary>Gets the zero-based offset in the caller's original character input.</summary>
    /// <returns>The original character offset, or <c>-1</c> when the position is unknown.</returns>
    public int GetCharacterOffset()
    {
        return this.characterOffset;
    }

    /// <summary>Gets the one-based source line containing this position.</summary>
    /// <returns>The line number, or <c>0</c> when the position is unknown.</returns>
    public int GetLine()
    {
        return this.line;
    }

    /// <summary>Gets the one-based source column containing this position.</summary>
    /// <returns>The column number, or <c>0</c> when the position is unknown.</returns>
    public int GetColumn()
    {
        return this.column;
    }

    /// <summary>Reports whether this position maps to an offset in the original byte input.</summary>
    /// <returns><c>true</c> when the source position has an original byte offset; otherwise, <c>false</c>.</returns>
    public bool HasByteOffset()
    {
        return this.hasByteOffset;
    }

    /// <summary>Gets the zero-based offset in the caller's original byte input.</summary>
    /// <returns>The original byte offset, or <c>-1</c> when byte mapping is unavailable.</returns>
    public long GetByteOffset()
    {
        return this.byteOffset;
    }

    internal static void ValidateKnown(int normalizedCharacterOffset, int characterOffset, int line, int column)
    {
        if (normalizedCharacterOffset < 0)
        {
            throw new ArgumentOutOfRangeException("normalizedCharacterOffset", normalizedCharacterOffset, "A normalized character offset cannot be negative.");
        }

        if (characterOffset < 0)
        {
            throw new ArgumentOutOfRangeException("characterOffset", characterOffset, "A character offset cannot be negative.");
        }

        if (line <= 0)
        {
            throw new ArgumentOutOfRangeException("line", line, "A source line is one-based.");
        }

        if (column <= 0)
        {
            throw new ArgumentOutOfRangeException("column", column, "A source column is one-based.");
        }
    }
}
