namespace ttf;

/// <summary>Defines per-operation bounds used while shaping text.</summary>
public interface ITextShapingLimits
{
    /// <summary>Gets the maximum source UTF-16 length.</summary>
    /// <returns>The positive code-unit limit.</returns>
    int GetMaximumUtf16CodeUnits();

    /// <summary>Sets the maximum source UTF-16 length.</summary>
    /// <param name="value">The positive code-unit limit.</param>
    void SetMaximumUtf16CodeUnits(int value);

    /// <summary>Gets the maximum decoded or normalized rune count.</summary>
    /// <returns>The positive rune limit.</returns>
    int GetMaximumRunes();

    /// <summary>Sets the maximum decoded or normalized rune count.</summary>
    /// <param name="value">The positive rune limit.</param>
    void SetMaximumRunes(int value);

    /// <summary>Gets the maximum source cluster count.</summary>
    /// <returns>The positive cluster limit.</returns>
    int GetMaximumClusters();

    /// <summary>Sets the maximum source cluster count.</summary>
    /// <param name="value">The positive cluster limit.</param>
    void SetMaximumClusters(int value);

    /// <summary>Gets the maximum shaping run count.</summary>
    /// <returns>The positive run limit.</returns>
    int GetMaximumRuns();

    /// <summary>Sets the maximum shaping run count.</summary>
    /// <param name="value">The positive run limit.</param>
    void SetMaximumRuns(int value);

    /// <summary>Gets the maximum intermediate glyph count.</summary>
    /// <returns>The positive glyph limit.</returns>
    int GetMaximumGlyphs();

    /// <summary>Sets the maximum intermediate glyph count.</summary>
    /// <param name="value">The positive glyph limit.</param>
    void SetMaximumGlyphs(int value);

    /// <summary>Gets the maximum OpenType layout operation count.</summary>
    /// <returns>The positive operation limit.</returns>
    int GetMaximumLayoutOperations();

    /// <summary>Sets the maximum OpenType layout operation count.</summary>
    /// <param name="value">The positive operation limit.</param>
    void SetMaximumLayoutOperations(int value);
}
