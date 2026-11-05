using System;

namespace avif;

internal class ColorVector
{
    internal double First;
    internal double Second;
    internal double Third;

    internal ColorVector(double first, double second, double third)
    {
        First = first;
        Second = second;
        Third = third;
    }
}

internal class ColorMatrix
{
    internal double[] Values;

    internal ColorMatrix(double[] values)
    {
        Values = (double[])values.Clone();
    }

    internal ColorVector Transform(ColorVector? value)
    {
        return new ColorVector(
            Values[0] * (value ?? throw new InvalidOperationException("Invalid internal state.")).First
                + Values[1] * value.Second
                + Values[2] * value.Third,
            Values[3] * value.First
                + Values[4] * value.Second
                + Values[5] * value.Third,
            Values[6] * value.First
                + Values[7] * value.Second
                + Values[8] * value.Third);
    }

    internal static ColorMatrix Identity()
    {
        return new ColorMatrix(new double[]
        {
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        });
    }

    internal static ColorMatrix Diagonal(
        double first,
        double second,
        double third)
    {
        return new ColorMatrix(new double[]
        {
            first, 0, 0,
            0, second, 0,
            0, 0, third
        });
    }

    internal static ColorMatrix Multiply(
        ColorMatrix? first,
        ColorMatrix second)
    {
        double[] result = new double[9];
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 3)
            {
                int offset = row * 3 + column;
                result[offset] = (first ?? throw new InvalidOperationException("Invalid internal state.")).Values[row * 3]
                    * second.Values[column]
                    + first.Values[row * 3 + 1]
                    * second.Values[3 + column]
                    + first.Values[row * 3 + 2]
                    * second.Values[6 + column];
                column++;
            }
            row++;
        }
        return new ColorMatrix(result);
    }

    internal ColorMatrix? Invert()
    {
        double a = Values[0];
        double b = Values[1];
        double c = Values[2];
        double d = Values[3];
        double e = Values[4];
        double f = Values[5];
        double g = Values[6];
        double h = Values[7];
        double i = Values[8];
        double determinant = a * (e * i - f * h)
            - b * (d * i - f * g)
            + c * (d * h - e * g);
        if (System.Math.Abs(determinant) < 1e-15)
        {
            return null;
        }
        double inverse = 1 / determinant;
        return new ColorMatrix(new double[]
        {
            (e * i - f * h) * inverse,
            (c * h - b * i) * inverse,
            (b * f - c * e) * inverse,
            (f * g - d * i) * inverse,
            (a * i - c * g) * inverse,
            (c * d - a * f) * inverse,
            (d * h - e * g) * inverse,
            (b * g - a * h) * inverse,
            (a * e - b * d) * inverse
        });
    }
}
