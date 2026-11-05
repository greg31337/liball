namespace ttf;

/// <summary>Represents one effective feature-value range.</summary>
public class FontLayoutFeatureRange : IFontLayoutFeatureRange
{
    SfntLayoutFeatureRange range;

    internal FontLayoutFeatureRange(SfntLayoutFeatureRange range)
    {
        this.range = range;
    }

    /// <summary>Gets the inclusive UTF-16 start offset.</summary>
    /// <returns>The start offset.</returns>
    public int GetStart() { return range.GetStart(); }

    /// <summary>Gets the exclusive UTF-16 end offset.</summary>
    /// <returns>The end offset.</returns>
    public int GetEnd() { return range.GetEnd(); }

    /// <summary>Gets the selected feature value.</summary>
    /// <returns>The nonzero feature value.</returns>
    public int GetValue() { return range.GetValue(); }
}
