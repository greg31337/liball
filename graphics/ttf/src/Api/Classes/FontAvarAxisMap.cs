namespace ttf;

/// <summary>Represents one <c>avar</c> axis segment map.</summary>
public class FontAvarAxisMap : IFontAvarAxisMap
{
    SfntAvarSegmentMap map;

    internal FontAvarAxisMap(SfntAvarSegmentMap map)
    {
        this.map = map;
    }

    /// <summary>Gets the mapped axis tag.</summary>
    /// <returns>The big-endian axis tag.</returns>
    public int GetAxisTag() { return map.GetAxisTag(); }

    /// <summary>Gets the number of mapping control points.</summary>
    /// <returns>The control-point count.</returns>
    public int GetValueMapCount() { return map.GetValueMapCount(); }

    /// <summary>Gets one mapping control point.</summary>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The control point.</returns>
    public FontAvarValueMap GetValueMap(int index)
    {
        return new FontAvarValueMap(map.GetValueMap(index));
    }

    /// <summary>Reports whether the required identity anchors are present.</summary>
    /// <returns>True when the segment map is active; otherwise false.</returns>
    public bool HasRequiredMappings() { return map.HasRequiredMappings(); }

    /// <summary>Maps one normalized coordinate through the segment map.</summary>
    /// <param name="value">A finite normalized coordinate from -1 through 1.</param>
    /// <returns>The mapped normalized coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is non-finite or outside the normalized range.
    /// </exception>
    public double Map(double value)
    {
        if (!double.IsFinite(value) || value < -1 || value > 1)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (!map.HasRequiredMappings() || map.GetValueMapCount() == 0)
        {
            return value;
        }

        int endIndex = 0;
        while (endIndex < map.GetValueMapCount() &&
            map.GetValueMap(endIndex).GetFromCoordinate() < value)
        {
            endIndex += 1;
        }
        if (endIndex >= map.GetValueMapCount())
        {
            endIndex = map.GetValueMapCount() - 1;
        }
        SfntAvarAxisValueMap end = map.GetValueMap(endIndex);
        if (end.GetFromCoordinate() == value || endIndex == 0)
        {
            return end.GetToCoordinate();
        }
        SfntAvarAxisValueMap start = map.GetValueMap(endIndex - 1);
        double ratio = (value - start.GetFromCoordinate()) /
            (end.GetFromCoordinate() - start.GetFromCoordinate());
        return start.GetToCoordinate() + ratio *
            (end.GetToCoordinate() - start.GetToCoordinate());
    }
}
