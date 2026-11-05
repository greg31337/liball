using System;

namespace geo;

internal class DoubleDouble
{
    internal double High;
    internal double Low;

    internal DoubleDouble(double high, double low)
    {
        if (!double.IsFinite(high) || !double.IsFinite(low))
        {
            throw new InvalidGeometryException("Double-double components must be finite.");
        }
        double[] normalized = ExpansionArithmetic.CreateTwoSum(high, low);
        if (normalized.Length == 2)
        {
            Low = normalized[0];
            High = normalized[1];
        }
        else
        {
            High = normalized[0];
        }
    }

    internal DoubleDouble AddValue(DoubleDouble addition)
    {
        ValidateOperand(addition);
        double[] first = [Low, High];
        double[] second = [addition.Low, addition.High];
        return FromExpansion(ExpansionArithmetic.AddExpansions(first, second));
    }

    internal DoubleDouble SubtractValue(DoubleDouble subtraction)
    {
        ValidateOperand(subtraction);
        double[] first = [Low, High];
        double[] second = [subtraction.Low, subtraction.High];
        return FromExpansion(ExpansionArithmetic.SubtractExpansions(first, second));
    }

    internal DoubleDouble MultiplyValue(DoubleDouble multiplier)
    {
        ValidateOperand(multiplier);
        double[] first = [Low, High];
        double[] second = [multiplier.Low, multiplier.High];
        return FromExpansion(ExpansionArithmetic.MultiplyExpansions(first, second));
    }

    internal DoubleDouble DivideValue(DoubleDouble divisor)
    {
        ValidateOperand(divisor);
        if (divisor.High == 0.0 && divisor.Low == 0.0)
        {
            throw new InvalidGeometryException("A double-double value cannot be divided by zero.");
        }
        double quotientHigh = High / divisor.High;
        if (!double.IsFinite(quotientHigh))
        {
            throw new InvalidGeometryException("Double-double quotient is outside the finite range.");
        }
        DoubleDouble estimate = new DoubleDouble(quotientHigh, 0.0);
        DoubleDouble remainder = SubtractValue(divisor.MultiplyValue(estimate));
        double quotientLow = (remainder.High + remainder.Low) / divisor.High;
        return estimate.AddValue(new DoubleDouble(quotientLow, 0.0));
    }

    internal DoubleDouble NegateValue()
    {
        return new DoubleDouble(-High, -Low);
    }

    internal int CompareToZero()
    {
        if (High > 0.0) return 1;
        if (High < 0.0) return -1;
        if (Low > 0.0) return 1;
        if (Low < 0.0) return -1;
        return 0;
    }

    internal double ToDoubleValue()
    {
        return High + Low;
    }

    internal static DoubleDouble FromExpansion(double[] expansion)
    {
        double[] compressed = ExpansionArithmetic.CompressExpansion(expansion);
        int last = compressed.Length - 1;
        double high = compressed[last];
        double low = 0.0;
        int index = 0;
        while (index < last)
        {
            low += compressed[index];
            index++;
        }
        return new DoubleDouble(high, low);
    }

    internal static void ValidateOperand(DoubleDouble value)
    {
        if (value == null)
        {
            throw new InvalidGeometryException("Double-double operand is null.");
        }
    }
}
