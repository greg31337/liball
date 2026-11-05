namespace odf;

internal static class OdfUnitConverter
{
    internal static OdfUnitValue ConvertLength(
        OdfUnitValue? value,
        OdfUnitKind targetUnit,
        OdfUnitConversionContext? context)
    {
        OdfUnitValue result = NewResult(value, targetUnit);
        if (value == null || !value.IsValid)
        {
            return result;
        }

        double points = ToPoints(value.NumericValue, value.UnitKind, context);
        if (!double.IsFinite(points))
        {
            return result;
        }
        double converted = FromPoints(points, targetUnit, context);
        if (!double.IsFinite(converted))
        {
            return result;
        }

        result.NumericValue = converted;
        result.IsValid = true;
        return result;
    }

    internal static OdfUnitValue ConvertAngle(OdfUnitValue? value, OdfUnitKind targetUnit)
    {
        OdfUnitValue result = NewResult(value, targetUnit);
        if (value == null || !value.IsValid)
        {
            return result;
        }

        double degrees = ToDegrees(value.NumericValue, value.UnitKind);
        if (!double.IsFinite(degrees))
        {
            return result;
        }
        double converted = FromDegrees(degrees, targetUnit);
        if (!double.IsFinite(converted))
        {
            return result;
        }

        result.NumericValue = converted;
        result.IsValid = true;
        return result;
    }

    static OdfUnitValue NewResult(OdfUnitValue? value, OdfUnitKind targetUnit)
    {
        OdfUnitValue result = new OdfUnitValue();
        if (value != null)
        {
            result.LexicalValue = value.LexicalValue;
        }
        result.UnitKind = targetUnit;
        return result;
    }

    static double ToPoints(double value, OdfUnitKind unit, OdfUnitConversionContext? context)
    {
        if (unit == OdfUnitKind.Millimetre)
        {
            return value * 72 / 25.4;
        }
        if (unit == OdfUnitKind.Centimetre)
        {
            return value * 72 / 2.54;
        }
        if (unit == OdfUnitKind.Inch)
        {
            return value * 72;
        }
        if (unit == OdfUnitKind.Point)
        {
            return value;
        }
        if (unit == OdfUnitKind.Pica)
        {
            return value * 12;
        }
        if (unit == OdfUnitKind.Pixel && context != null && IsPositiveFinite(context.PixelsPerInch))
        {
            return value * 72 / context.PixelsPerInch;
        }
        if (unit == OdfUnitKind.Em && context != null && IsPositiveFinite(context.EmSizeInPoints))
        {
            return value * context.EmSizeInPoints;
        }
        if (unit == OdfUnitKind.Ex && context != null && IsPositiveFinite(context.ExSizeInPoints))
        {
            return value * context.ExSizeInPoints;
        }
        if (unit == OdfUnitKind.Percentage && context != null && IsNonNegativeFinite(context.PercentageBasisInPoints))
        {
            return value * context.PercentageBasisInPoints / 100;
        }
        return double.NaN;
    }

    static double FromPoints(double value, OdfUnitKind unit, OdfUnitConversionContext? context)
    {
        if (unit == OdfUnitKind.Millimetre)
        {
            return value * 25.4 / 72;
        }
        if (unit == OdfUnitKind.Centimetre)
        {
            return value * 2.54 / 72;
        }
        if (unit == OdfUnitKind.Inch)
        {
            return value / 72;
        }
        if (unit == OdfUnitKind.Point)
        {
            return value;
        }
        if (unit == OdfUnitKind.Pica)
        {
            return value / 12;
        }
        if (unit == OdfUnitKind.Pixel && context != null && IsPositiveFinite(context.PixelsPerInch))
        {
            return value * context.PixelsPerInch / 72;
        }
        if (unit == OdfUnitKind.Em && context != null && IsPositiveFinite(context.EmSizeInPoints))
        {
            return value / context.EmSizeInPoints;
        }
        if (unit == OdfUnitKind.Ex && context != null && IsPositiveFinite(context.ExSizeInPoints))
        {
            return value / context.ExSizeInPoints;
        }
        if (unit == OdfUnitKind.Percentage && context != null && IsPositiveFinite(context.PercentageBasisInPoints))
        {
            return value * 100 / context.PercentageBasisInPoints;
        }
        return double.NaN;
    }

    static double ToDegrees(double value, OdfUnitKind unit)
    {
        if (unit == OdfUnitKind.Unitless || unit == OdfUnitKind.Degree)
        {
            return value;
        }
        if (unit == OdfUnitKind.Grad)
        {
            return value * 0.9;
        }
        if (unit == OdfUnitKind.Radian)
        {
            return value * 180 / global::System.Math.PI;
        }
        return double.NaN;
    }

    static double FromDegrees(double value, OdfUnitKind unit)
    {
        if (unit == OdfUnitKind.Unitless || unit == OdfUnitKind.Degree)
        {
            return value;
        }
        if (unit == OdfUnitKind.Grad)
        {
            return value / 0.9;
        }
        if (unit == OdfUnitKind.Radian)
        {
            return value * global::System.Math.PI / 180;
        }
        return double.NaN;
    }

    static bool IsPositiveFinite(double value)
    {
        return value > 0 && double.IsFinite(value);
    }

    static bool IsNonNegativeFinite(double value)
    {
        return value >= 0 && double.IsFinite(value);
    }
}
