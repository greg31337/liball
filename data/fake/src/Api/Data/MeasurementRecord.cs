namespace fake;

/// <summary>Represents a correlated synthetic scientific measurement fixture.</summary>
/// <remarks>The value is generated for software tests and does not claim calibration, observation, or measurement.</remarks>
public class MeasurementRecord
{
    /// <summary>The physical quantity correlated with <c>Unit</c>.</summary>
    public string Quantity = string.Empty;

    /// <summary>The synthetic decimal magnitude before applying <c>SiPrefix</c>.</summary>
    public decimal Value;

    /// <summary>The SI prefix name correlated with its symbol and exponent.</summary>
    public string SiPrefix = string.Empty;

    /// <summary>The SI prefix symbol.</summary>
    public string PrefixSymbol = string.Empty;

    /// <summary>The base-ten exponent represented by <c>SiPrefix</c>.</summary>
    public int PrefixExponent;

    /// <summary>The unit name correlated with <c>Quantity</c>.</summary>
    public string Unit = string.Empty;

    /// <summary>The unit symbol.</summary>
    public string UnitSymbol = string.Empty;

    /// <summary>The invariant formatted value, prefix symbol, and unit symbol.</summary>
    public string Formatted = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the value claims a measurement, always <c>false</c>.</summary>
    public bool IsMeasured;

    internal MeasurementRecord()
    {
    }
}
