namespace fake;

/// <summary>Represents synthetic product dimensions expressed in centimeters.</summary>
public class ProductDimensions
{
    /// <summary>The product length in centimeters.</summary>
    public decimal Length;

    /// <summary>The product width in centimeters.</summary>
    public decimal Width;

    /// <summary>The product height in centimeters.</summary>
    public decimal Height;

    /// <summary>The rectangular volume in cubic centimeters.</summary>
    public decimal Volume;

    /// <summary>The unit used by the length, width, and height fields.</summary>
    public string Unit = string.Empty;

    internal ProductDimensions()
    {
    }
}
