using System.Text;

namespace odf;

internal static class FormulaRepresentationFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.ARABIC)
        {
            return Arabic(arguments[0]);
        }
        if (name == OdfFormulaNames.ROMAN)
        {
            return Roman(arguments);
        }
        if (name == OdfFormulaNames.BASE)
        {
            return Base(arguments);
        }
        if (name == OdfFormulaNames.DECIMAL)
        {
            return Decimal(arguments);
        }
        return Engineering(name, arguments);
    }

    static OdfFormulaResult Arabic(OdfFormulaResult value)
    {
        OdfFormulaResult textValue = FormulaValueModel.ConvertToText(value);
        if (textValue.Error != OdfFormulaError.None)
        {
            return textValue;
        }
        string text = textValue.StringValue.Trim().ToUpperInvariant();
        if (text.Length == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        long total = 0;
        int previous = 0;
        int index = text.Length - 1;
        while (index >= 0)
        {
            int valueAtIndex = RomanDigit(text[index]);
            if (valueAtIndex == 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            total = valueAtIndex < previous
                ? total - valueAtIndex
                : total + valueAtIndex;
            if (valueAtIndex > previous)
            {
                previous = valueAtIndex;
            }
            index = index - 1;
        }
        return FormulaValueModel.CreateNumber(total);
    }

    static OdfFormulaResult Roman(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(arguments[0]);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        int value = (int)global::System.Math.Truncate(number.NumericValue);
        int form = 0;
        if (arguments.Length > 1)
        {
            OdfFormulaResult formValue = FormulaValueModel.ConvertToNumber(
                arguments[1]);
            if (formValue.Error != OdfFormulaError.None)
            {
                return formValue;
            }
            form = (int)global::System.Math.Truncate(formValue.NumericValue);
        }
        if (value < 0 || value > 3999 || form < 0 || form > 4)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (value == 0)
        {
            return FormulaValueModel.CreateText("");
        }
        int[] values =
            [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
        string[] digits =
            ["M", "CM", "D", "CD", "C", "XC", "L", "XL",
                "X", "IX", "V", "IV", "I"];
        if (form == 1)
        {
            values = [1000, 900, 500, 450, 400, 100, 90, 50, 45, 40,
                10, 9, 5, 4, 1];
            digits = ["M", "CM", "D", "LD", "CD", "C", "XC", "L",
                "VL", "XL", "X", "IX", "V", "IV", "I"];
        }
        else if (form == 2)
        {
            values = [1000, 990, 900, 500, 490, 400, 100, 99, 90, 50,
                49, 40, 10, 9, 5, 4, 1];
            digits = ["M", "XM", "CM", "D", "XD", "CD", "C", "IC",
                "XC", "L", "IL", "XL", "X", "IX", "V", "IV", "I"];
        }
        else if (form == 3)
        {
            values = [1000, 995, 990, 900, 500, 495, 490, 400, 100, 99,
                95, 90, 50, 49, 45, 40, 10, 9, 5, 4, 1];
            digits = ["M", "VM", "XM", "CM", "D", "VD", "XD", "CD",
                "C", "IC", "VC", "XC", "L", "IL", "VL", "XL", "X",
                "IX", "V", "IV", "I"];
        }
        else if (form == 4)
        {
            values = [1000, 999, 995, 990, 900, 500, 499, 495, 490, 400,
                100, 99, 95, 90, 50, 49, 45, 40, 10, 9, 5, 4, 1];
            digits = ["M", "IM", "VM", "XM", "CM", "D", "ID", "VD",
                "XD", "CD", "C", "IC", "VC", "XC", "L", "IL", "VL",
                "XL", "X", "IX", "V", "IV", "I"];
        }
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            while (value >= values[index])
            {
                result.Append(digits[index]);
                value = value - values[index];
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateText(result.ToString());
    }

    static OdfFormulaResult Base(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(arguments[0]);
        OdfFormulaResult radixValue = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        if (radixValue.Error != OdfFormulaError.None)
        {
            return radixValue;
        }
        long value = (long)global::System.Math.Truncate(number.NumericValue);
        int radix = (int)global::System.Math.Truncate(radixValue.NumericValue);
        int minimum = 0;
        if (arguments.Length > 2)
        {
            OdfFormulaResult minimumValue = FormulaValueModel.ConvertToNumber(
                arguments[2]);
            if (minimumValue.Error != OdfFormulaError.None)
            {
                return minimumValue;
            }
            minimum = (int)global::System.Math.Truncate(
                minimumValue.NumericValue);
        }
        if (value < 0 || radix < 2 || radix > 36
            || minimum < 0 || minimum > 255)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateText(
            FormatRadix(value, radix, minimum));
    }

    static OdfFormulaResult Decimal(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult text = FormulaValueModel.ConvertToText(arguments[0]);
        OdfFormulaResult radixValue = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        if (text.Error != OdfFormulaError.None)
        {
            return text;
        }
        if (radixValue.Error != OdfFormulaError.None)
        {
            return radixValue;
        }
        int radix = (int)global::System.Math.Truncate(radixValue.NumericValue);
        if (radix < 2 || radix > 36)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        OdfFormulaResult parsed = ParseUnsigned(text.StringValue, radix, 255);
        return parsed.Error == OdfFormulaError.None
            ? FormulaValueModel.CreateNumber(parsed.NumericValue)
            : parsed;
    }

    static OdfFormulaResult Engineering(
        string name,
        OdfFormulaResult[] arguments)
    {
        int sourceRadix = SourceRadix(name);
        int targetRadix = TargetRadix(name);
        int sourceBits = sourceRadix == 2
            ? 10
            : (sourceRadix == 8 ? 30 : (sourceRadix == 16 ? 40 : 0));
        int targetBits = targetRadix == 2
            ? 10
            : (targetRadix == 8 ? 30 : (targetRadix == 16 ? 40 : 0));
        long value;
        if (sourceRadix == 10)
        {
            OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
                arguments[0]);
            if (number.Error != OdfFormulaError.None)
            {
                return number;
            }
            value = (long)global::System.Math.Truncate(number.NumericValue);
        }
        else
        {
            OdfFormulaResult text = FormulaValueModel.ConvertToText(arguments[0]);
            if (text.Error != OdfFormulaError.None)
            {
                return text;
            }
            OdfFormulaResult parsed = ParseUnsigned(
                text.StringValue,
                sourceRadix,
                10);
            if (parsed.Error != OdfFormulaError.None)
            {
                return parsed;
            }
            value = (long)parsed.NumericValue;
            long signBit = 1L << (sourceBits - 1);
            long fullRange = 1L << sourceBits;
            string trimmed = text.StringValue.Trim();
            if (trimmed.Length == 10 && (value & signBit) != 0)
            {
                value = value - fullRange;
            }
        }
        if (targetRadix == 10)
        {
            return FormulaValueModel.CreateNumber(value);
        }
        long minimum = -(1L << (targetBits - 1));
        long maximum = (1L << (targetBits - 1)) - 1;
        if (value < minimum || value > maximum)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        int digits = 0;
        if (arguments.Length > 1)
        {
            OdfFormulaResult digitsValue = FormulaValueModel.ConvertToNumber(
                arguments[1]);
            if (digitsValue.Error != OdfFormulaError.None)
            {
                return digitsValue;
            }
            digits = (int)global::System.Math.Truncate(digitsValue.NumericValue);
            if (digits < 1 || digits > 10)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
        }
        if (value < 0)
        {
            value = value + (1L << targetBits);
            digits = 10;
        }
        string result = FormatRadix(value, targetRadix, digits);
        if (result.Length > 10
            || (digits > 0 && result.Length > digits))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateText(result);
    }

    static OdfFormulaResult ParseUnsigned(
        string input,
        int radix,
        int maximumLength)
    {
        string text = input.Trim().ToUpperInvariant();
        if (text.Length == 0 || text.Length > maximumLength)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = DigitValue(text[index]);
            if (digit < 0 || digit >= radix)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            if (value > (long.MaxValue - digit) / radix)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            value = value * radix + digit;
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(value);
    }

    static string FormatRadix(long value, int radix, int minimum)
    {
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        StringBuilder result = new StringBuilder();
        do
        {
            result.Insert(0, digits[(int)(value % radix)]);
            value = value / radix;
        }
        while (value > 0);
        while (result.Length < minimum)
        {
            result.Insert(0, '0');
        }
        return result.ToString();
    }

    static int SourceRadix(string name)
    {
        if (name.StartsWith("BIN", global::System.StringComparison.Ordinal))
        {
            return 2;
        }
        if (name.StartsWith("OCT", global::System.StringComparison.Ordinal))
        {
            return 8;
        }
        if (name.StartsWith("HEX", global::System.StringComparison.Ordinal))
        {
            return 16;
        }
        return 10;
    }

    static int TargetRadix(string name)
    {
        if (name.EndsWith("BIN", global::System.StringComparison.Ordinal))
        {
            return 2;
        }
        if (name.EndsWith("OCT", global::System.StringComparison.Ordinal))
        {
            return 8;
        }
        if (name.EndsWith("HEX", global::System.StringComparison.Ordinal))
        {
            return 16;
        }
        return 10;
    }

    static int DigitValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A' + 10;
        }
        return -1;
    }

    static int RomanDigit(char value)
    {
        if (value == 'I')
        {
            return 1;
        }
        if (value == 'V')
        {
            return 5;
        }
        if (value == 'X')
        {
            return 10;
        }
        if (value == 'L')
        {
            return 50;
        }
        if (value == 'C')
        {
            return 100;
        }
        if (value == 'D')
        {
            return 500;
        }
        return value == 'M' ? 1000 : 0;
    }
}
