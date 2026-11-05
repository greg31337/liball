namespace qrcode;

internal class GaloisField256
{
    internal const int PrimitivePolynomial = 0x11D;
    internal const int ElementCount = 256;
    internal const int MultiplicativeOrder = 255;

    static readonly int[] ExponentTable = CreateExponentTable();
    static readonly int[] LogarithmTable = CreateLogarithmTable();

    internal static int Add(int left, int right)
    {
        ValidateElement(left, "left");
        ValidateElement(right, "right");
        return left ^ right;
    }

    internal static int Multiply(int left, int right)
    {
        ValidateElement(left, "left");
        ValidateElement(right, "right");
        if (left == 0 || right == 0)
        {
            return 0;
        }

        return ExponentTable[LogarithmTable[left] + LogarithmTable[right]];
    }

    internal static int Divide(int dividend, int divisor)
    {
        ValidateElement(dividend, "dividend");
        ValidateElement(divisor, "divisor");
        if (divisor == 0)
        {
            throw new QrInputException("Division by the zero field element is undefined.", "divisor");
        }

        if (dividend == 0)
        {
            return 0;
        }

        return GetExponent(LogarithmTable[dividend] - LogarithmTable[divisor]);
    }

    internal static int Inverse(int value)
    {
        ValidateElement(value, "value");
        if (value == 0)
        {
            throw new QrInputException("The zero field element has no multiplicative inverse.", "value");
        }

        return ExponentTable[MultiplicativeOrder - LogarithmTable[value]];
    }

    internal static int GetExponent(int exponent)
    {
        int normalizedExponent = exponent % MultiplicativeOrder;
        if (normalizedExponent < 0)
        {
            normalizedExponent += MultiplicativeOrder;
        }

        return ExponentTable[normalizedExponent];
    }

    internal static int GetLogarithm(int value)
    {
        ValidateElement(value, "value");
        if (value == 0)
        {
            throw new QrInputException("The zero field element has no logarithm.", "value");
        }

        return LogarithmTable[value];
    }

    static int[] CreateExponentTable()
    {
        int[] table = new int[MultiplicativeOrder * 2];
        int value = 1;
        for (int exponent = 0; exponent < MultiplicativeOrder; exponent++)
        {
            table[exponent] = value;
            value <<= 1;
            if ((value & ElementCount) != 0)
            {
                value ^= PrimitivePolynomial;
            }
        }

        for (int exponent = MultiplicativeOrder; exponent < table.Length; exponent++)
        {
            table[exponent] = table[exponent - MultiplicativeOrder];
        }

        return table;
    }

    static int[] CreateLogarithmTable()
    {
        int[] table = new int[ElementCount];
        table[0] = -1;
        for (int exponent = 0; exponent < MultiplicativeOrder; exponent++)
        {
            table[ExponentTable[exponent]] = exponent;
        }

        return table;
    }

    static void ValidateElement(int value, string inputName)
    {
        if (value < 0 || value >= ElementCount)
        {
            throw new QrInputException(
                "A GF(256) element must be between zero and 255.",
                inputName);
        }
    }
}
