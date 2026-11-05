namespace pptx;

internal static class PptxUnitsCore
{
    internal const long EmusPerInch = 914400L;
    internal const long EmusPerPoint = 12700L;
    internal const double CentimetersPerInch = 2.54;
    internal const int AngleUnitsPerDegree = 60000;
    internal const int PercentageUnitsPerPercent = 1000;
    internal const int PercentageUnitsPerWhole = 100000;
    internal static long InchesToEmu(double inches)
    {
        return RoundToLong(inches * EmusPerInch);
    }
    internal static double EmuToInches(long emu)
    {
        return (double)emu / (double)EmusPerInch;
    }
    internal static long PointsToEmu(double points)
    {
        return RoundToLong(points * EmusPerPoint);
    }
    internal static double EmuToPoints(long emu)
    {
        return (double)emu / (double)EmusPerPoint;
    }
    internal static long CentimetersToEmu(double centimeters)
    {
        return InchesToEmu(centimeters / CentimetersPerInch);
    }
    internal static double EmuToCentimeters(long emu)
    {
        return EmuToInches(emu) * CentimetersPerInch;
    }
    internal static long PixelsToEmu(double pixels, double dpi)
    {
        ValidateDpi(dpi);
        return InchesToEmu(pixels / dpi);
    }
    internal static double EmuToPixels(long emu, double dpi)
    {
        ValidateDpi(dpi);
        return EmuToInches(emu) * dpi;
    }
    internal static int EmuToRoundedPixels(long emu, double dpi)
    {
        return RoundToInt(EmuToPixels(emu, dpi));
    }
    internal static int DegreesToAngleUnits(double degrees)
    {
        return RoundToInt(degrees * AngleUnitsPerDegree);
    }
    internal static double AngleUnitsToDegrees(int angleUnits)
    {
        return (double)angleUnits / (double)AngleUnitsPerDegree;
    }
    internal static int PercentToPercentageUnits(double percent)
    {
        return RoundToInt(percent * PercentageUnitsPerPercent);
    }
    internal static double PercentageUnitsToPercent(int percentageUnits)
    {
        return (double)percentageUnits / (double)PercentageUnitsPerPercent;
    }
    internal static int FractionToPercentageUnits(double fraction)
    {
        return RoundToInt(fraction * PercentageUnitsPerWhole);
    }
    internal static double PercentageUnitsToFraction(int percentageUnits)
    {
        return (double)percentageUnits / (double)PercentageUnitsPerWhole;
    }
    internal static long RoundToLong(double value)
    {
        ValidateFinite(value, "value");
        double rounded = System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        if (rounded >= 9223372036854775808.0 || rounded < -9223372036854775808.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return (long)rounded;
    }
    internal static int RoundToInt(double value)
    {
        ValidateFinite(value, "value");
        double rounded = System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        if (rounded > 2147483647.0 || rounded < -2147483648.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return (int)rounded;
    }

    internal static void ValidateDpi(double dpi)
    {
        ValidateFinite(dpi, "dpi");
        if (dpi <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("dpi");
        }
    }

    internal static void ValidateFinite(double value, string parameterName)
    {
        if (double.IsNaN(value))
        {
            throw new System.ArgumentException("Value must be finite.", parameterName);
        }

        if (double.IsInfinity(value))
        {
            throw new System.ArgumentException("Value must be finite.", parameterName);
        }
    }
}
