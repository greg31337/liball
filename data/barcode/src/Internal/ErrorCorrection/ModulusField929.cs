namespace barcode;

internal class ModulusField929
{
    internal const int Modulus = 929;
    internal const int Generator = 3;
    internal const int MultiplicativeOrder = 928;

    internal static readonly ModulusField929 Pdf417 = new ModulusField929();

    int[] ExponentTable;
    int[] LogarithmTable;
    ModulusPolynomial929 Zero;
    ModulusPolynomial929 One;

    internal ModulusField929()
    {
        ExponentTable = new int[Modulus];
        LogarithmTable = new int[Modulus];
        int index = 0;
        while (index < Modulus)
        {
            LogarithmTable[index] = -1;
            index = index + 1;
        }

        int value = 1;
        index = 0;
        while (index < Modulus)
        {
            ExponentTable[index] = value;
            value = (value * Generator) % Modulus;
            index = index + 1;
        }

        index = 0;
        while (index < MultiplicativeOrder)
        {
            LogarithmTable[ExponentTable[index]] = index;
            index = index + 1;
        }

        Zero = new ModulusPolynomial929(new int[] { 0 });
        One = new ModulusPolynomial929(new int[] { 1 });
    }

    internal static int Add(int left, int right)
    {
        ValidateElement(left);
        ValidateElement(right);
        return (left + right) % Modulus;
    }

    internal static int Subtract(int left, int right)
    {
        ValidateElement(left);
        ValidateElement(right);
        return (Modulus + left - right) % Modulus;
    }

    internal static int Negate(int element)
    {
        ValidateElement(element);
        if (element == 0)
        {
            return 0;
        }

        return Modulus - element;
    }

    internal int Exponent(int exponent)
    {
        if (exponent < 0 || exponent >= Modulus)
        {
            throw new BarcodeValidationException("Modulo-929 exponent must be from zero through 928.");
        }

        return ExponentTable[exponent];
    }

    internal int Logarithm(int element)
    {
        ValidateElement(element);
        if (element == 0)
        {
            throw new BarcodeValidationException("Zero has no logarithm in the modulo-929 field.");
        }

        return LogarithmTable[element];
    }

    internal int Inverse(int element)
    {
        ValidateElement(element);
        if (element == 0)
        {
            throw new BarcodeValidationException("Zero has no multiplicative inverse in the modulo-929 field.");
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
            throw new BarcodeValidationException("Modulo-929 division by zero is not defined.");
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
            throw new BarcodeValidationException("Modulo-929 power exponent must not be negative.");
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

    internal ModulusPolynomial929 CreateZeroPolynomial()
    {
        return Zero;
    }

    internal ModulusPolynomial929 CreateOnePolynomial()
    {
        return One;
    }

    internal ModulusPolynomial929 BuildMonomial(int degree, int coefficient)
    {
        if (degree < 0)
        {
            throw new BarcodeValidationException("Modulo-929 monomial degree must not be negative.");
        }

        ValidateElement(coefficient);
        if (coefficient == 0)
        {
            return Zero;
        }

        int[] coefficients = new int[CheckedMath.Add(degree, 1)];
        coefficients[0] = coefficient;
        return new ModulusPolynomial929(coefficients);
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
        if (element < 0 || element >= Modulus)
        {
            throw new BarcodeValidationException("Modulo-929 element must be from zero through 928.");
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
