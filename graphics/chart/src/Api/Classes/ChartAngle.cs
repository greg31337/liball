namespace chart;
/// <summary>Represents a finite angle expressed in degrees.</summary>
public class ChartAngle : IChartAngle
{
    internal double Degrees;

    /// <summary>Initializes an angle from degrees.</summary>
    /// <param name = "degrees">The finite angle in degrees.</param>
    public ChartAngle(double degrees)
    {
        ValidateDegrees(degrees);
        Degrees = degrees;
    }

    /// <summary>Converts this angle to radians.</summary>
    /// <returns>The angle in radians.</returns>
    public double ToRadians()
    {
        return Degrees * System.Math.PI / 180.0;
    }

    /// <summary>Creates an equivalent angle normalized to the range from zero inclusive to 360 exclusive.</summary>
    /// <returns>The normalized angle.</returns>
    public ChartAngle Normalize()
    {
        double normalized = Degrees % 360.0;
        if (normalized < 0.0)
        {
            normalized = normalized + 360.0;
        }

        return new ChartAngle(normalized);
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A new angle with the same value.</returns>
    public ChartAngle Copy()
    {
        return new ChartAngle(Degrees);
    }

    /// <summary>Returns the angle in degrees.</summary>
    /// <returns>The current angle in degrees.</returns>
    public double GetDegrees()
    {
        return Degrees;
    }

    /// <summary>Replaces the angle in degrees.</summary>
    /// <param name = "value">The finite angle in degrees.</param>
    public void SetDegrees(double value)
    {
        ValidateDegrees(value);
        Degrees = value;
    }

    internal static void ValidateDegrees(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
    }
}
