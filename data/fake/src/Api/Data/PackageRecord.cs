using System;

namespace fake;

/// <summary>Represents a synthetic package fixture and its physical measurements.</summary>
public class PackageRecord
{
    /// <summary>The package-identifier-shaped value.</summary>
    public string Identifier = string.Empty;

    /// <summary>The generic package type.</summary>
    public string Type = string.Empty;

    /// <summary>The rectangular package dimensions in centimeters.</summary>
    public ProductDimensions Dimensions = new ProductDimensions();

    /// <summary>The package weight in kilograms.</summary>
    public decimal Weight;

    /// <summary>Whether the package is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    internal PackageRecord()
    {
    }
}
