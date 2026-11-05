namespace barcode;

internal class GaloisField256
{
    internal const int FieldSize = 256;
    internal const int MultiplicativeOrder = 255;
    internal const int PrimitivePolynomial = 301;
    internal const int GeneratorBase = 1;

    internal static readonly GaloisField256 DataMatrix = new GaloisField256();

    int[] ExponentTable;
    int[] LogarithmTable;
    GaloisPolynomial256 Zero;
    GaloisPolynomial256 One;

    internal GaloisField256()
    {
        ExponentTable = new int[FieldSize];
        LogarithmTable = new int[FieldSize];
        int index = 0;
        while (index < FieldSize)
        {
            LogarithmTable[index] = -1;
            index = index + 1;
        }

        int value = 1;
        index = 0;
        while (index < FieldSize)
        {
            ExponentTable[index] = value;
            value = value * 2;
            if (value >= FieldSize)
            {
                value = value ^ PrimitivePolynomial;
                value = value & (FieldSize - 1);
            }

            index = index + 1;
        }

        index = 0;
        while (index < MultiplicativeOrder)
        {
            LogarithmTable[ExponentTable[index]] = index;
            index = index + 1;
        }

        Zero = new GaloisPolynomial256(new int[] { 0 });
        One = new GaloisPolynomial256(new int[] { 1 });
    }

    internal static int AddOrSubtract(int left, int right)
    {
        ValidateElement(left);
        ValidateElement(right);
        return left ^ right;
    }

    internal int Exponent(int exponent)
    {
        if (exponent < 0 || exponent >= FieldSize)
        {
            throw new BarcodeValidationException("GF(256) exponent must be from zero through 255.");
        }

        return ExponentTable[exponent];
    }

    internal int Logarithm(int element)
    {
        ValidateElement(element);
        if (element == 0)
        {
            throw new BarcodeValidationException("Zero has no logarithm in GF(256).");
        }

        return LogarithmTable[element];
    }

    internal int Inverse(int element)
    {
        ValidateElement(element);
        if (element == 0)
        {
            throw new BarcodeValidationException("Zero has no multiplicative inverse in GF(256).");
        }

        return ExponentTable[MultiplicativeOrder - LogarithmTable[element]];
    }

    internal int Multiply(int left, int right)
    {
        ValidateElement(left);
        ValidateElement(right);
        if (left == 0 || right == 0)
        {
            return 0;
        }

        int logarithm = LogarithmTable[left] + LogarithmTable[right];
        return ExponentTable[logarithm % MultiplicativeOrder];
    }

    internal int DivideElements(int dividend, int divisor)
    {
        ValidateElement(dividend);
        ValidateElement(divisor);
        if (divisor == 0)
        {
            throw new BarcodeValidationException("GF(256) division by zero is not defined.");
        }

        if (dividend == 0)
        {
            return 0;
        }

        int logarithm = LogarithmTable[dividend] - LogarithmTable[divisor];
        if (logarithm < 0)
        {
            logarithm = logarithm + MultiplicativeOrder;
        }

        return ExponentTable[logarithm];
    }

    internal int Power(int element, int exponent)
    {
        ValidateElement(element);
        if (exponent < 0)
        {
            throw new BarcodeValidationException("GF(256) power exponent must not be negative.");
        }

        if (exponent == 0)
        {
            return 1;
        }

        if (element == 0)
        {
            return 0;
        }

        long product = (long)LogarithmTable[element] * (long)exponent;
        int reduced = (int)(product % MultiplicativeOrder);
        return ExponentTable[reduced];
    }

    internal GaloisPolynomial256 CreateZeroPolynomial()
    {
        return Zero;
    }

    internal GaloisPolynomial256 CreateOnePolynomial()
    {
        return One;
    }

    internal GaloisPolynomial256 BuildMonomial(int degree, int coefficient)
    {
        if (degree < 0)
        {
            throw new BarcodeValidationException("GF(256) monomial degree must not be negative.");
        }

        ValidateElement(coefficient);
        if (coefficient == 0)
        {
            return Zero;
        }

        int[] coefficients = new int[CheckedMath.Add(degree, 1)];
        coefficients[0] = coefficient;
        return new GaloisPolynomial256(coefficients);
    }

    internal int[] CopyExponentTable()
    {
        return CopyTable(ExponentTable);
    }

    internal int[] CopyLogarithmTable()
    {
        return CopyTable(LogarithmTable);
    }

    internal static void ValidateElement(int element)
    {
        if (element < 0 || element >= FieldSize)
        {
            throw new BarcodeValidationException("GF(256) element must be from zero through 255.");
        }
    }

    internal static int[] CopyTable(int[] table)
    {
        int[] copy = new int[table.Length];
        int index = 0;
        while (index < table.Length)
        {
            copy[index] = table[index];
            index = index + 1;
        }

        return copy;
    }
}
