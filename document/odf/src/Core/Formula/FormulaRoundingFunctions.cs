namespace odf;

internal static class FormulaRoundingFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.CEILING
            || name == OdfFormulaNames.FLOOR)
        {
            return RoundToMultiple(name, arguments);
        }
        if (name == OdfFormulaNames.INT)
        {
            OdfFormulaResult value = Number(arguments[0]);
            if (value.Error != OdfFormulaError.None)
            {
                return value;
            }
            return FormulaValueModel.CreateNumber(
                global::System.Math.Floor(value.NumericValue));
        }
        if (name == OdfFormulaNames.MROUND)
        {
            return RoundToNearestMultiple(arguments);
        }
        return RoundDecimal(name, arguments);
    }

    static OdfFormulaResult RoundToMultiple(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = Number(arguments[0]);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        OdfFormulaResult significance;
        if (arguments.Length < 2
            || arguments[1].Kind == OdfValueKind.Empty)
        {
            significance = FormulaValueModel.CreateNumber(
                number.NumericValue < 0 ? -1 : 1);
        }
        else
        {
            significance = Number(arguments[1]);
        }
        if (significance.Error != OdfFormulaError.None)
        {
            return significance;
        }
        OdfFormulaResult mode = arguments.Length > 2
            ? Number(arguments[2])
            : FormulaValueModel.CreateNumber(0);
        if (mode.Error != OdfFormulaError.None)
        {
            return mode;
        }
        double value = number.NumericValue;
        double multiple = significance.NumericValue;
        if (value == 0 || multiple == 0)
        {
            return FormulaValueModel.CreateNumber(0);
        }
        if ((value < 0 && multiple > 0)
            || (value > 0 && multiple < 0))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double magnitude = global::System.Math.Abs(multiple);
        double result;
        if (mode.NumericValue != 0)
        {
            double absolute = global::System.Math.Abs(value) / magnitude;
            double rounded = name == OdfFormulaNames.CEILING
                ? global::System.Math.Ceiling(absolute)
                : global::System.Math.Floor(absolute);
            result = value < 0 ? -rounded * magnitude : rounded * magnitude;
        }
        else if (name == OdfFormulaNames.CEILING)
        {
            result = global::System.Math.Ceiling(value / magnitude)
                * magnitude;
        }
        else
        {
            result = global::System.Math.Floor(value / magnitude)
                * magnitude;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult RoundToNearestMultiple(
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = Number(arguments[0]);
        OdfFormulaResult significance = Number(arguments[1]);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        if (significance.Error != OdfFormulaError.None)
        {
            return significance;
        }
        double value = number.NumericValue;
        double magnitude = global::System.Math.Abs(
            significance.NumericValue);
        if (magnitude == 0)
        {
            return FormulaValueModel.CreateNumber(0);
        }
        double lower = global::System.Math.Floor(value / magnitude)
            * magnitude;
        double upper = lower + magnitude;
        double lowerDistance = global::System.Math.Abs(value - lower);
        double upperDistance = global::System.Math.Abs(upper - value);
        return FormulaValueModel.CreateNumber(
            upperDistance <= lowerDistance ? upper : lower);
    }

    static OdfFormulaResult RoundDecimal(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = Number(arguments[0]);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        OdfFormulaResult digits = arguments.Length > 1
            ? Number(arguments[1])
            : FormulaValueModel.CreateNumber(0);
        if (digits.Error != OdfFormulaError.None)
        {
            return digits;
        }
        double places = global::System.Math.Truncate(digits.NumericValue);
        if (places > 308)
        {
            return number;
        }
        if (places < -308)
        {
            if (name == OdfFormulaNames.ROUNDUP
                && number.NumericValue != 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            return FormulaValueModel.CreateNumber(0);
        }
        double scale = places >= 0
            ? global::System.Math.Pow(10, places)
            : global::System.Math.Pow(10, -places);
        double shifted = places >= 0
            ? number.NumericValue * scale
            : number.NumericValue / scale;
        if (!double.IsFinite(shifted))
        {
            return number;
        }
        double rounded;
        if (name == OdfFormulaNames.ROUND)
        {
            rounded = RoundHalfAwayFromZero(shifted);
        }
        else if (name == OdfFormulaNames.ROUNDUP)
        {
            double magnitude = global::System.Math.Ceiling(
                global::System.Math.Abs(shifted));
            rounded = shifted < 0 ? -magnitude : magnitude;
        }
        else
        {
            rounded = global::System.Math.Truncate(shifted);
        }
        double result = places >= 0
            ? rounded / scale
            : rounded * scale;
        return FormulaValueModel.CreateNumber(result);
    }

    static double RoundHalfAwayFromZero(double value)
    {
        double magnitude = global::System.Math.Floor(
            global::System.Math.Abs(value) + 0.5);
        return value < 0 ? -magnitude : magnitude;
    }

    static OdfFormulaResult Number(OdfFormulaResult value)
    {
        return FormulaValueModel.ConvertToNumber(value);
    }
}
