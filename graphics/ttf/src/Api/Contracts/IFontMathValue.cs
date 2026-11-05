namespace ttf;

/// <summary>Defines one MATH value and optional adjustment.</summary>
public interface IFontMathValue
{
    /// <summary>Gets the design-unit value.</summary><returns>The value.</returns>
    int GetValue();
    /// <summary>Reports whether an adjustment exists.</summary><returns>True when present.</returns>
    bool HasAdjustment();
    /// <summary>Reports whether the adjustment is device data.</summary><returns>True for device data.</returns>
    bool IsDeviceAdjustment();
    /// <summary>Reports whether the adjustment is a variation index.</summary><returns>True for a variation index.</returns>
    bool IsVariationAdjustment();
}
