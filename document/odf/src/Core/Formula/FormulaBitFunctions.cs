namespace odf;

internal static class FormulaBitFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult first = ReadBitInteger(arguments[0]);
        OdfFormulaResult second = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        if (first.Error != OdfFormulaError.None)
        {
            return first;
        }
        if (second.Error != OdfFormulaError.None)
        {
            return second;
        }
        long left = (long)first.NumericValue;
        long right = (long)global::System.Math.Truncate(second.NumericValue);
        if (name == OdfFormulaNames.BITAND
            || name == OdfFormulaNames.BITOR
            || name == OdfFormulaNames.BITXOR)
        {
            OdfFormulaResult rightValue = ReadBitInteger(arguments[1]);
            if (rightValue.Error != OdfFormulaError.None)
            {
                return rightValue;
            }
            long value = (long)rightValue.NumericValue;
            if (name == OdfFormulaNames.BITAND)
            {
                return FormulaValueModel.CreateNumber(left & value);
            }
            if (name == OdfFormulaNames.BITOR)
            {
                return FormulaValueModel.CreateNumber(left | value);
            }
            return FormulaValueModel.CreateNumber(left ^ value);
        }
        if (right < -53 || right > 53)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        bool leftShift = name == OdfFormulaNames.BITLSHIFT;
        if (right < 0)
        {
            leftShift = !leftShift;
            right = -right;
        }
        if (!leftShift)
        {
            return FormulaValueModel.CreateNumber(left >> (int)right);
        }
        if (right > 48
            || left > 281474976710655L >> (int)right)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(left << (int)right);
    }

    static OdfFormulaResult ReadBitInteger(OdfFormulaResult value)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        double integer = global::System.Math.Truncate(number.NumericValue);
        if (integer < 0 || integer > 281474976710655.0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(integer);
    }
}
