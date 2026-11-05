namespace svg;

/// <summary>Contains a non-throwing scalar-unit conversion result.</summary>
public class SvgUnitConversionResult
{
    /// <summary>Indicates whether conversion produced a finite value.</summary>
    public bool Success;

    /// <summary>The conversion failure kind.</summary>
    public SvgUnitConversionError Error;

    /// <summary>The converted finite value, or zero after failure.</summary>
    public double Value;

    /// <summary>Creates a scalar-unit conversion result.</summary>
    /// <param name="success">Whether conversion succeeded.</param>
    /// <param name="error">The conversion failure kind.</param>
    /// <param name="value">The converted value, or zero.</param>
    public SvgUnitConversionResult(bool success, SvgUnitConversionError error, double value)
    {
        Success = success;
        Error = error;
        Value = value;
    }
}

/// <summary>Represents a finite typed angle.</summary>
public class SvgAngle
{
    /// <summary>The finite numeric component.</summary>
    public double Value;

    /// <summary>The angle unit.</summary>
    public SvgAngleUnit Unit;

    /// <summary>Creates a finite typed angle.</summary>
    /// <param name="value">The finite numeric component.</param>
    /// <param name="unit">The angle unit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a non-finite value or unknown unit.</exception>
    public SvgAngle(double value, SvgAngleUnit unit)
    {
        if (!SvgFiniteMath.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        if (!SvgScalarUnitNames.IsAngleUnit(unit))
        {
            throw new System.ArgumentOutOfRangeException("unit");
        }
        Value = SvgFiniteMath.NormalizeZero(value);
        Unit = unit;
    }

    /// <summary>Converts this angle's numeric value to another angle unit.</summary>
    /// <param name="targetUnit">The target angle unit.</param>
    /// <returns>The converted finite numeric value.</returns>
    internal SvgUnitConversionResult ConvertTo(SvgAngleUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertAngle(this, targetUnit);
    }

    /// <summary>Serializes the angle with shortest invariant number formatting.</summary>
    /// <returns>The canonical typed angle text.</returns>
    internal string ToText()
    {
        return SvgScalarUnitText.Number(Value) + SvgScalarUnitNames.AngleText(Unit);
    }
}

/// <summary>Represents a finite typed time.</summary>
public class SvgTime
{
    /// <summary>The finite numeric component.</summary>
    public double Value;

    /// <summary>The time unit.</summary>
    public SvgTimeUnit Unit;

    /// <summary>Creates a finite typed time.</summary>
    /// <param name="value">The finite numeric component.</param>
    /// <param name="unit">The time unit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a non-finite value or unknown unit.</exception>
    public SvgTime(double value, SvgTimeUnit unit)
    {
        if (!SvgFiniteMath.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        if (!SvgScalarUnitNames.IsTimeUnit(unit))
        {
            throw new System.ArgumentOutOfRangeException("unit");
        }
        Value = SvgFiniteMath.NormalizeZero(value);
        Unit = unit;
    }

    /// <summary>Converts this time's numeric value to another time unit.</summary>
    /// <param name="targetUnit">The target time unit.</param>
    /// <returns>The converted finite numeric value.</returns>
    internal SvgUnitConversionResult ConvertTo(SvgTimeUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertTime(this, targetUnit);
    }

    /// <summary>Serializes the time with shortest invariant number formatting.</summary>
    /// <returns>The canonical typed time text.</returns>
    internal string ToText()
    {
        return SvgScalarUnitText.Number(Value) + SvgScalarUnitNames.TimeText(Unit);
    }
}

/// <summary>Represents a finite nonnegative frequency.</summary>
public class SvgFrequency
{
    /// <summary>The finite nonnegative numeric component.</summary>
    public double Value;

    /// <summary>The frequency unit.</summary>
    public SvgFrequencyUnit Unit;

    /// <summary>Creates a finite nonnegative frequency.</summary>
    /// <param name="value">The finite nonnegative numeric component.</param>
    /// <param name="unit">The frequency unit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a negative or non-finite value or unknown unit.</exception>
    public SvgFrequency(double value, SvgFrequencyUnit unit)
    {
        if (!SvgFiniteMath.IsFinite(value) || value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        if (!SvgScalarUnitNames.IsFrequencyUnit(unit))
        {
            throw new System.ArgumentOutOfRangeException("unit");
        }
        Value = SvgFiniteMath.NormalizeZero(value);
        Unit = unit;
    }

    /// <summary>Converts this frequency's numeric value to another frequency unit.</summary>
    /// <param name="targetUnit">The target frequency unit.</param>
    /// <returns>The converted finite numeric value.</returns>
    internal SvgUnitConversionResult ConvertTo(SvgFrequencyUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertFrequency(this, targetUnit);
    }

    /// <summary>Serializes the frequency with shortest invariant number formatting.</summary>
    /// <returns>The canonical typed frequency text.</returns>
    internal string ToText()
    {
        return SvgScalarUnitText.Number(Value) + SvgScalarUnitNames.FrequencyText(Unit);
    }
}

/// <summary>Represents a finite strictly positive resolution.</summary>
public class SvgResolution
{
    /// <summary>The finite strictly positive numeric component.</summary>
    public double Value;

    /// <summary>The resolution unit.</summary>
    public SvgResolutionUnit Unit;

    /// <summary>Creates a finite strictly positive resolution.</summary>
    /// <param name="value">The finite strictly positive numeric component.</param>
    /// <param name="unit">The resolution unit.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for a nonpositive or non-finite value or unknown unit.</exception>
    public SvgResolution(double value, SvgResolutionUnit unit)
    {
        if (!SvgFiniteMath.IsFinite(value) || value <= 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        if (!SvgScalarUnitNames.IsResolutionUnit(unit))
        {
            throw new System.ArgumentOutOfRangeException("unit");
        }
        Value = value;
        Unit = unit;
    }

    /// <summary>Converts this resolution's numeric value to another resolution unit.</summary>
    /// <param name="targetUnit">The target resolution unit.</param>
    /// <returns>The converted finite numeric value.</returns>
    internal SvgUnitConversionResult ConvertTo(SvgResolutionUnit targetUnit)
    {
        return SvgScalarUnitConverter.ConvertResolution(this, targetUnit);
    }

    /// <summary>Serializes the resolution with shortest invariant number formatting.</summary>
    /// <returns>The canonical typed resolution text.</returns>
    internal string ToText()
    {
        return SvgScalarUnitText.Number(Value) + SvgScalarUnitNames.ResolutionText(Unit);
    }
}

/// <summary>Represents a finite ratio with strictly positive components.</summary>
public class SvgRatio
{
    /// <summary>The finite strictly positive numerator.</summary>
    public double Numerator;

    /// <summary>The finite strictly positive denominator.</summary>
    public double Denominator;

    /// <summary>Creates a finite ratio.</summary>
    /// <param name="numerator">The strictly positive numerator.</param>
    /// <param name="denominator">The strictly positive denominator.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a component is nonpositive or non-finite.</exception>
    public SvgRatio(double numerator, double denominator)
    {
        if (!SvgFiniteMath.IsFinite(numerator) || numerator <= 0)
        {
            throw new System.ArgumentOutOfRangeException("numerator");
        }
        if (!SvgFiniteMath.IsFinite(denominator) || denominator <= 0)
        {
            throw new System.ArgumentOutOfRangeException("denominator");
        }
        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>Evaluates the numerator divided by the denominator.</summary>
    /// <returns>The finite ratio value or a stable failure.</returns>
    internal SvgUnitConversionResult ToValue()
    {
        if (!SvgFiniteMath.IsFinite(Numerator) || Numerator <= 0 || !SvgFiniteMath.IsFinite(Denominator) || Denominator <= 0)
        {
            return SvgScalarUnitConverter.Failure(SvgUnitConversionError.InvalidArgument);
        }
        double result = Numerator / Denominator;
        if (!SvgFiniteMath.IsFinite(result))
        {
            return SvgScalarUnitConverter.Failure(SvgUnitConversionError.NonFiniteResult);
        }
        return SvgScalarUnitConverter.Success(result);
    }

    /// <summary>Serializes the ratio in compact numerator-slash-denominator form.</summary>
    /// <returns>The canonical ratio text.</returns>
    internal string ToText()
    {
        return SvgScalarUnitText.Number(Numerator) + "/" + SvgScalarUnitText.Number(Denominator);
    }
}

/// <summary>Represents a finite nonnegative SVG pathLength value.</summary>
public class SvgPathLength
{
    /// <summary>The finite nonnegative author path length.</summary>
    public double Value;

    /// <summary>Creates a finite nonnegative pathLength value.</summary>
    /// <param name="value">The finite nonnegative author path length.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the value is negative or non-finite.</exception>
    public SvgPathLength(double value)
    {
        if (!SvgFiniteMath.IsFinite(value) || value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
        Value = SvgFiniteMath.NormalizeZero(value);
    }

    /// <summary>Computes actual path length divided by this author pathLength.</summary>
    /// <param name="actualLength">The finite nonnegative measured path length.</param>
    /// <returns>The finite path-length scale or a stable failure.</returns>
    internal SvgUnitConversionResult GetScale(double actualLength)
    {
        if (!SvgFiniteMath.IsFinite(Value) || Value < 0 || !SvgFiniteMath.IsFinite(actualLength) || actualLength < 0)
        {
            return SvgScalarUnitConverter.Failure(SvgUnitConversionError.InvalidArgument);
        }
        if (Value == 0)
        {
            return SvgScalarUnitConverter.Failure(SvgUnitConversionError.ZeroDivisor);
        }
        double result = actualLength / Value;
        if (!SvgFiniteMath.IsFinite(result))
        {
            return SvgScalarUnitConverter.Failure(SvgUnitConversionError.NonFiniteResult);
        }
        return SvgScalarUnitConverter.Success(result);
    }

    /// <summary>Serializes the pathLength with shortest invariant number formatting.</summary>
    /// <returns>The canonical pathLength text.</returns>
    internal string ToText()
    {
        return SvgScalarUnitText.Number(Value);
    }
}
