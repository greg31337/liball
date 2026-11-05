namespace ttf;

/// <summary>Represents one <c>avar</c> segment-map control point.</summary>
public class FontAvarValueMap : IFontAvarValueMap
{
    SfntAvarAxisValueMap valueMap;

    internal FontAvarValueMap(SfntAvarAxisValueMap valueMap)
    {
        this.valueMap = valueMap;
    }

    /// <summary>Gets the input normalized coordinate.</summary>
    /// <returns>The input coordinate.</returns>
    public double GetFromCoordinate() { return valueMap.GetFromCoordinate(); }

    /// <summary>Gets the output normalized coordinate.</summary>
    /// <returns>The output coordinate.</returns>
    public double GetToCoordinate() { return valueMap.GetToCoordinate(); }
}
