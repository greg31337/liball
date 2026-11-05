using System.Globalization;
using System.Numerics;

namespace odf;

internal class FormulaComplexData
{
    internal Complex Value;
    internal string Suffix = "i";
    internal OdfFormulaResult? Error;
}

internal static class FormulaComplexFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.COMPLEX)
        {
            return Create(arguments);
        }
        if (name == OdfFormulaNames.IMPRODUCT
            || name == OdfFormulaNames.IMSUM)
        {
            return CombineValues(name, arguments);
        }
        FormulaComplexData first = Read(arguments[0]);
        if (first.Error != null)
        {
            return first.Error;
        }
        if (name == OdfFormulaNames.IMABS)
        {
            return FormulaValueModel.CreateNumber(first.Value.Magnitude);
        }
        if (name == OdfFormulaNames.IMAGINARY)
        {
            return FormulaValueModel.CreateNumber(first.Value.Imaginary);
        }
        if (name == OdfFormulaNames.IMARGUMENT)
        {
            if (first.Value == Complex.Zero)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.DivisionByZero);
            }
            return FormulaValueModel.CreateNumber(first.Value.Phase);
        }
        if (name == OdfFormulaNames.IMREAL)
        {
            return FormulaValueModel.CreateNumber(first.Value.Real);
        }
        if (name == OdfFormulaNames.IMDIV
            || name == OdfFormulaNames.IMPOWER
            || name == OdfFormulaNames.IMSUB)
        {
            return EvaluateBinary(name, first, arguments[1]);
        }
        Complex value = first.Value;
        if (name == OdfFormulaNames.IMCONJUGATE)
        {
            value = Complex.Conjugate(value);
        }
        else if (name == OdfFormulaNames.IMCOS)
        {
            value = Complex.Cos(value);
        }
        else if (name == OdfFormulaNames.IMCOSH)
        {
            value = Complex.Cosh(value);
        }
        else if (name == OdfFormulaNames.IMCOT)
        {
            value = Complex.One / Complex.Tan(value);
        }
        else if (name == OdfFormulaNames.IMCSC)
        {
            value = Complex.One / Complex.Sin(value);
        }
        else if (name == OdfFormulaNames.IMCSCH)
        {
            value = Complex.One / Complex.Sinh(value);
        }
        else if (name == OdfFormulaNames.IMEXP)
        {
            value = Complex.Exp(value);
        }
        else if (name == OdfFormulaNames.IMLN)
        {
            value = Complex.Log(value);
        }
        else if (name == OdfFormulaNames.IMLOG10)
        {
            value = Complex.Log10(value);
        }
        else if (name == OdfFormulaNames.IMLOG2)
        {
            value = Complex.Log(value) / global::System.Math.Log(2);
        }
        else if (name == OdfFormulaNames.IMSIN)
        {
            value = Complex.Sin(value);
        }
        else if (name == OdfFormulaNames.IMSINH)
        {
            value = Complex.Sinh(value);
        }
        else if (name == OdfFormulaNames.IMSEC)
        {
            value = Complex.One / Complex.Cos(value);
        }
        else if (name == OdfFormulaNames.IMSECH)
        {
            value = Complex.One / Complex.Cosh(value);
        }
        else if (name == OdfFormulaNames.IMSQRT)
        {
            value = Complex.Sqrt(value);
        }
        else if (name == OdfFormulaNames.IMTAN)
        {
            value = Complex.Tan(value);
        }
        else
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.UnsupportedFunction);
        }
        return CreateResult(value, first.Suffix);
    }

    static OdfFormulaResult Create(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult real = FormulaValueModel.ConvertToNumber(arguments[0]);
        OdfFormulaResult imaginary = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        if (real.Error != OdfFormulaError.None)
        {
            return real;
        }
        if (imaginary.Error != OdfFormulaError.None)
        {
            return imaginary;
        }
        string suffix = "i";
        if (arguments.Length > 2)
        {
            OdfFormulaResult suffixValue = FormulaValueModel.ConvertToText(
                arguments[2]);
            if (suffixValue.Error != OdfFormulaError.None)
            {
                return suffixValue;
            }
            suffix = suffixValue.StringValue;
        }
        if (suffix != "i" && suffix != "j")
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return CreateResult(
            new Complex(real.NumericValue, imaginary.NumericValue),
            suffix);
    }

    static OdfFormulaResult EvaluateBinary(
        string name,
        FormulaComplexData first,
        OdfFormulaResult secondValue)
    {
        FormulaComplexData second = Read(secondValue);
        if (second.Error != null)
        {
            return second.Error;
        }
        Complex result;
        if (name == OdfFormulaNames.IMDIV)
        {
            if (second.Value == Complex.Zero)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.DivisionByZero);
            }
            result = first.Value / second.Value;
        }
        else if (name == OdfFormulaNames.IMPOWER)
        {
            result = Complex.Pow(first.Value, second.Value);
        }
        else
        {
            result = first.Value - second.Value;
        }
        return CreateResult(result, first.Suffix);
    }

    static OdfFormulaResult CombineValues(
        string name,
        OdfFormulaResult[] arguments)
    {
        Complex result = name == OdfFormulaNames.IMPRODUCT
            ? Complex.One
            : Complex.Zero;
        string suffix = "i";
        int argument = 0;
        while (argument < arguments.Length)
        {
            OdfFormulaResult current = arguments[argument];
            int count = current.Kind == OdfValueKind.Array
                || current.Kind == OdfValueKind.Reference
                ? current.Values.Length
                : 1;
            int index = 0;
            while (index < count)
            {
                OdfFormulaResult item = count == 1
                    && current.Kind != OdfValueKind.Array
                    && current.Kind != OdfValueKind.Reference
                    ? current
                    : current.Values[index];
                FormulaComplexData parsed = Read(item);
                if (parsed.Error != null)
                {
                    return parsed.Error;
                }
                suffix = parsed.Suffix;
                result = name == OdfFormulaNames.IMPRODUCT
                    ? result * parsed.Value
                    : result + parsed.Value;
                index = index + 1;
            }
            argument = argument + 1;
        }
        return CreateResult(result, suffix);
    }

    static FormulaComplexData Read(OdfFormulaResult input)
    {
        FormulaComplexData result = new FormulaComplexData();
        if (input.Error != OdfFormulaError.None)
        {
            result.Error = input;
            return result;
        }
        if (input.Kind != OdfValueKind.String
            && input.Kind != OdfValueKind.Complex)
        {
            OdfFormulaResult number = FormulaValueModel.ConvertToNumber(input);
            if (number.Error != OdfFormulaError.None)
            {
                result.Error = number;
                return result;
            }
            result.Value = new Complex(number.NumericValue, 0);
            return result;
        }
        string text = input.StringValue.Trim();
        bool hasSuffix = text.EndsWith("i",
            global::System.StringComparison.OrdinalIgnoreCase)
            || text.EndsWith("j",
                global::System.StringComparison.OrdinalIgnoreCase);
        if (hasSuffix)
        {
            result.Suffix = text.Substring(text.Length - 1, 1).ToLowerInvariant();
            text = text.Substring(0, text.Length - 1);
        }
        int separator = FindImaginarySeparator(text);
        double real = 0;
        double imaginary = 0;
        if (hasSuffix && separator > 0)
        {
            OdfLexicalValue realValue = OdfLexicalParser.ParseFloat(
                text.Substring(0, separator));
            OdfLexicalValue imaginaryValue = ParseCoefficient(
                text.Substring(separator));
            if (!realValue.IsValid || !imaginaryValue.IsValid)
            {
                result.Error = FormulaValueModel.CreateError(
                    OdfFormulaError.Value);
                return result;
            }
            real = realValue.FloatValue;
            imaginary = imaginaryValue.FloatValue;
        }
        else if (hasSuffix)
        {
            OdfLexicalValue imaginaryValue = ParseCoefficient(text);
            if (!imaginaryValue.IsValid)
            {
                result.Error = FormulaValueModel.CreateError(
                    OdfFormulaError.Value);
                return result;
            }
            imaginary = imaginaryValue.FloatValue;
        }
        else
        {
            OdfLexicalValue realValue = OdfLexicalParser.ParseFloat(text);
            if (!realValue.IsValid)
            {
                result.Error = FormulaValueModel.CreateError(
                    OdfFormulaError.Value);
                return result;
            }
            real = realValue.FloatValue;
        }
        result.Value = new Complex(real, imaginary);
        return result;
    }

    static OdfLexicalValue ParseCoefficient(string text)
    {
        if (text.Length == 0 || text == "+")
        {
            return OdfLexicalParser.ParseFloat("1");
        }
        if (text == "-")
        {
            return OdfLexicalParser.ParseFloat("-1");
        }
        return OdfLexicalParser.ParseFloat(text);
    }

    static int FindImaginarySeparator(string text)
    {
        int index = 1;
        while (index < text.Length)
        {
            if ((text[index] == '+' || text[index] == '-')
                && text[index - 1] != 'e'
                && text[index - 1] != 'E')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static OdfFormulaResult CreateResult(Complex value, string suffix)
    {
        if (!double.IsFinite(value.Real)
            || !double.IsFinite(value.Imaginary))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double real = global::System.Math.Abs(value.Real) < 0.00000000000001
            ? 0
            : value.Real;
        double imaginary = global::System.Math.Abs(value.Imaginary)
            < 0.00000000000001
            ? 0
            : value.Imaginary;
        string text = "";
        if (real != 0 || imaginary == 0)
        {
            text = real.ToString("R", CultureInfo.InvariantCulture);
        }
        if (imaginary != 0)
        {
            if (real != 0 && imaginary > 0)
            {
                text = text + "+";
            }
            if (imaginary == -1)
            {
                text = text + "-";
            }
            else if (imaginary != 1)
            {
                text = text + imaginary.ToString(
                    "R",
                    CultureInfo.InvariantCulture);
            }
            text = text + suffix;
        }
        return FormulaValueModel.CreateComplex(text);
    }
}
