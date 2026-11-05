namespace ttf;

/// <summary>Represents one MATH value and optional adjustment.</summary>
public class FontMathValue : IFontMathValue
{
    SfntMathValue value;
    internal FontMathValue(SfntMathValue value) { this.value = value; }
    /// <summary>Gets the design-unit value.</summary><returns>The value.</returns>
    public int GetValue() { return value.GetValue(); }
    /// <summary>Reports whether an adjustment exists.</summary><returns>True when present.</returns>
    public bool HasAdjustment() { return value.HasAdjustment(); }
    /// <summary>Reports whether the adjustment is device data.</summary><returns>True for device data.</returns>
    public bool IsDeviceAdjustment()
    {
        SfntDeviceOrVariationIndex? adjustment = value.GetAdjustment();
        return adjustment != null && adjustment.IsDevice();
    }
    /// <summary>Reports whether the adjustment is a variation index.</summary><returns>True for a variation index.</returns>
    public bool IsVariationAdjustment()
    {
        SfntDeviceOrVariationIndex? adjustment = value.GetAdjustment();
        return adjustment != null && adjustment.IsVariationIndex();
    }
}
