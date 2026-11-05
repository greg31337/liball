namespace ttf;

/// <summary>Represents one caller-selected feature value over a UTF-16 range.</summary>
public class FontLayoutFeatureSetting : IFontLayoutFeatureSetting
{
    int tag;
    int value;
    int start;
    int end;

    /// <summary>Creates one feature selection.</summary>
    /// <param name="tag">The big-endian feature tag.</param>
    /// <param name="value">The nonnegative feature value.</param>
    /// <param name="start">The inclusive UTF-16 start offset.</param>
    /// <param name="end">The exclusive UTF-16 end offset.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is negative or the range is invalid.
    /// </exception>
    public FontLayoutFeatureSetting(int tag, int value, int start, int end)
    {
        SfntUserFeature validated = new SfntUserFeature(tag, value, start, end);
        this.tag = validated.GetFeatureTag();
        this.value = validated.GetValue();
        this.start = validated.GetStart();
        this.end = validated.GetEnd();
    }

    /// <summary>Gets the feature tag.</summary>
    /// <returns>The big-endian feature tag.</returns>
    public int GetTag() { return tag; }

    /// <summary>Gets the nonnegative feature value.</summary>
    /// <returns>The feature value.</returns>
    public int GetValue() { return value; }

    /// <summary>Gets the inclusive UTF-16 start offset.</summary>
    /// <returns>The start offset.</returns>
    public int GetStart() { return start; }

    /// <summary>Gets the exclusive UTF-16 end offset.</summary>
    /// <returns>The end offset.</returns>
    public int GetEnd() { return end; }

    internal SfntUserFeature CreateInternal()
    {
        return new SfntUserFeature(tag, value, start, end);
    }
}
