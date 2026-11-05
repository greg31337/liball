namespace ttf;

/// <summary>Defines positive resource bounds for one shaping operation.</summary>
public class TextShapingLimits : ITextShapingLimits
{
    int maximumUtf16CodeUnits;
    int maximumRunes;
    int maximumClusters;
    int maximumRuns;
    int maximumGlyphs;
    int maximumLayoutOperations;

    /// <summary>Creates production defaults with finite limits.</summary>
    public TextShapingLimits()
    {
        maximumUtf16CodeUnits = 1048576;
        maximumRunes = 65535;
        maximumClusters = 65535;
        maximumRuns = 65535;
        maximumGlyphs = 65535;
        maximumLayoutOperations = 5000000;
    }

    /// <summary>Gets the maximum source UTF-16 length.</summary>
    /// <returns>The positive code-unit limit.</returns>
    public int GetMaximumUtf16CodeUnits()
    {
        return maximumUtf16CodeUnits;
    }

    /// <summary>Sets the maximum source UTF-16 length.</summary>
    /// <param name="value">The positive code-unit limit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is not positive.</exception>
    public void SetMaximumUtf16CodeUnits(int value)
    {
        ValidatePositive(value);
        maximumUtf16CodeUnits = value;
    }

    /// <summary>Gets the maximum decoded or normalized rune count.</summary>
    /// <returns>The positive rune limit.</returns>
    public int GetMaximumRunes()
    {
        return maximumRunes;
    }

    /// <summary>Sets the maximum decoded or normalized rune count.</summary>
    /// <param name="value">The positive rune limit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is not positive.</exception>
    public void SetMaximumRunes(int value)
    {
        ValidatePositive(value);
        maximumRunes = value;
    }

    /// <summary>Gets the maximum source cluster count.</summary>
    /// <returns>The positive cluster limit.</returns>
    public int GetMaximumClusters()
    {
        return maximumClusters;
    }

    /// <summary>Sets the maximum source cluster count.</summary>
    /// <param name="value">The positive cluster limit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is not positive.</exception>
    public void SetMaximumClusters(int value)
    {
        ValidatePositive(value);
        maximumClusters = value;
    }

    /// <summary>Gets the maximum shaping run count.</summary>
    /// <returns>The positive run limit.</returns>
    public int GetMaximumRuns()
    {
        return maximumRuns;
    }

    /// <summary>Sets the maximum shaping run count.</summary>
    /// <param name="value">The positive run limit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is not positive.</exception>
    public void SetMaximumRuns(int value)
    {
        ValidatePositive(value);
        maximumRuns = value;
    }

    /// <summary>Gets the maximum intermediate glyph count.</summary>
    /// <returns>The positive glyph limit.</returns>
    public int GetMaximumGlyphs()
    {
        return maximumGlyphs;
    }

    /// <summary>Sets the maximum intermediate glyph count.</summary>
    /// <param name="value">The positive glyph limit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is not positive.</exception>
    public void SetMaximumGlyphs(int value)
    {
        ValidatePositive(value);
        maximumGlyphs = value;
    }

    /// <summary>Gets the maximum OpenType layout operation count.</summary>
    /// <returns>The positive operation limit.</returns>
    public int GetMaximumLayoutOperations()
    {
        return maximumLayoutOperations;
    }

    /// <summary>Sets the maximum OpenType layout operation count.</summary>
    /// <param name="value">The positive operation limit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is not positive.</exception>
    public void SetMaximumLayoutOperations(int value)
    {
        ValidatePositive(value);
        maximumLayoutOperations = value;
    }

    internal TextShapingLimits Copy()
    {
        TextShapingLimits copy = new TextShapingLimits();
        copy.maximumUtf16CodeUnits = maximumUtf16CodeUnits;
        copy.maximumRunes = maximumRunes;
        copy.maximumClusters = maximumClusters;
        copy.maximumRuns = maximumRuns;
        copy.maximumGlyphs = maximumGlyphs;
        copy.maximumLayoutOperations = maximumLayoutOperations;
        return copy;
    }

    static void ValidatePositive(int value)
    {
        if (value <= 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }
}
