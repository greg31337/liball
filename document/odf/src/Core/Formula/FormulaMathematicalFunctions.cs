using System.Collections.Generic;

namespace odf;

internal class FormulaNumberSequence
{
    internal double[] Values = [];
    internal OdfFormulaResult? Error;
}

internal static class FormulaMathematicalFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (name == OdfFormulaNames.PI)
        {
            return FormulaValueModel.CreateNumber(global::System.Math.PI);
        }
        if (name == OdfFormulaNames.RAND)
        {
            return DeterministicRandom(context);
        }
        if (name == OdfFormulaNames.CONVERT)
        {
            return FormulaUnitConversionFunctions.Convert(arguments);
        }
        if (name == OdfFormulaNames.EUROCONVERT)
        {
            return EuroConvert(arguments);
        }
        if (name == OdfFormulaNames.SUMIF)
        {
            return SumIf(arguments);
        }
        if (name == OdfFormulaNames.SUMIFS)
        {
            return SumIfs(arguments);
        }
        if (name == OdfFormulaNames.SUBTOTAL)
        {
            return Subtotal(arguments);
        }
        if (name == OdfFormulaNames.SUMPRODUCT)
        {
            return SumProduct(arguments);
        }
        if (name == OdfFormulaNames.SUMX2MY2
            || name == OdfFormulaNames.SUMX2PY2
            || name == OdfFormulaNames.SUMXMY2)
        {
            return SumPair(name, arguments);
        }
        if (name == OdfFormulaNames.SERIESSUM)
        {
            return SeriesSum(arguments);
        }
        if (name == OdfFormulaNames.BESSELI
            || name == OdfFormulaNames.BESSELJ
            || name == OdfFormulaNames.BESSELK
            || name == OdfFormulaNames.BESSELY)
        {
            return Bessel(name, arguments);
        }
        if (name == OdfFormulaNames.COMBIN
            || name == OdfFormulaNames.COMBINA)
        {
            return Combination(name, arguments);
        }
        if (name == OdfFormulaNames.GCD
            || name == OdfFormulaNames.LCM)
        {
            return CommonMultiple(name, arguments);
        }
        if (name == OdfFormulaNames.MULTINOMIAL)
        {
            return Multinomial(arguments);
        }
        if (name == OdfFormulaNames.PRODUCT
            || name == OdfFormulaNames.SUM
            || name == OdfFormulaNames.SUMSQ)
        {
            return CalculateSequence(name, arguments);
        }
        if (name == OdfFormulaNames.RANDBETWEEN)
        {
            return RandomBetween(arguments, context);
        }
        if (name == OdfFormulaNames.DELTA
            || name == OdfFormulaNames.GESTEP)
        {
            return ComparisonFunction(name, arguments);
        }
        if (name == OdfFormulaNames.ERF)
        {
            return ErrorFunction(arguments);
        }
        if (name == OdfFormulaNames.ERFC)
        {
            OdfFormulaResult number = Number(arguments[0]);
            return number.Error == OdfFormulaError.None
                ? FormulaValueModel.CreateNumber(
                    1 - Erf(number.NumericValue))
                : number;
        }
        if (name == OdfFormulaNames.FACT
            || name == OdfFormulaNames.FACTDOUBLE)
        {
            return FactorialFunction(name, arguments[0]);
        }
        if (name == OdfFormulaNames.GAMMA
            || name == OdfFormulaNames.GAMMALN)
        {
            return GammaFunction(name, arguments[0]);
        }
        if (name == OdfFormulaNames.LOG)
        {
            return Logarithm(arguments);
        }
        if (name == OdfFormulaNames.MOD
            || name == OdfFormulaNames.POWER
            || name == OdfFormulaNames.QUOTIENT
            || name == OdfFormulaNames.ATAN2)
        {
            return Binary(name, arguments);
        }
        return Unary(name, arguments[0]);
    }

    static OdfFormulaResult Unary(string name, OdfFormulaResult argument)
    {
        OdfFormulaResult number = Number(argument);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        double value = number.NumericValue;
        double result;
        if (name == OdfFormulaNames.ABS)
        {
            result = global::System.Math.Abs(value);
        }
        else if (name == OdfFormulaNames.ACOS)
        {
            result = global::System.Math.Acos(value);
        }
        else if (name == OdfFormulaNames.ACOSH)
        {
            result = global::System.Math.Acosh(value);
        }
        else if (name == OdfFormulaNames.ACOT)
        {
            result = value == 0
                ? global::System.Math.PI / 2
                : global::System.Math.Atan(1 / value)
                    + (value < 0 ? global::System.Math.PI : 0);
        }
        else if (name == OdfFormulaNames.ACOTH)
        {
            result = 0.5 * global::System.Math.Log((value + 1) / (value - 1));
        }
        else if (name == OdfFormulaNames.ASIN)
        {
            result = global::System.Math.Asin(value);
        }
        else if (name == OdfFormulaNames.ASINH)
        {
            result = global::System.Math.Asinh(value);
        }
        else if (name == OdfFormulaNames.ATAN)
        {
            result = global::System.Math.Atan(value);
        }
        else if (name == OdfFormulaNames.ATANH)
        {
            result = global::System.Math.Atanh(value);
        }
        else if (name == OdfFormulaNames.COS)
        {
            result = global::System.Math.Cos(value);
        }
        else if (name == OdfFormulaNames.COSH)
        {
            result = global::System.Math.Cosh(value);
        }
        else if (name == OdfFormulaNames.COT)
        {
            result = 1 / global::System.Math.Tan(value);
        }
        else if (name == OdfFormulaNames.COTH)
        {
            result = 1 / global::System.Math.Tanh(value);
        }
        else if (name == OdfFormulaNames.CSC)
        {
            result = 1 / global::System.Math.Sin(value);
        }
        else if (name == OdfFormulaNames.CSCH)
        {
            result = 1 / global::System.Math.Sinh(value);
        }
        else if (name == OdfFormulaNames.DEGREES)
        {
            result = value * 180 / global::System.Math.PI;
        }
        else if (name == OdfFormulaNames.EVEN)
        {
            result = RoundAway(value, 2);
        }
        else if (name == OdfFormulaNames.EXP)
        {
            result = global::System.Math.Exp(value);
        }
        else if (name == OdfFormulaNames.LN)
        {
            result = global::System.Math.Log(value);
        }
        else if (name == OdfFormulaNames.LOG10)
        {
            result = global::System.Math.Log10(value);
        }
        else if (name == OdfFormulaNames.ODD)
        {
            double magnitude = global::System.Math.Ceiling(
                global::System.Math.Abs(value));
            if (((long)magnitude) % 2 == 0)
            {
                magnitude = magnitude + 1;
            }
            result = value < 0 ? -magnitude : magnitude;
        }
        else if (name == OdfFormulaNames.RADIANS)
        {
            result = value * global::System.Math.PI / 180;
        }
        else if (name == OdfFormulaNames.SEC)
        {
            result = 1 / global::System.Math.Cos(value);
        }
        else if (name == OdfFormulaNames.SECH)
        {
            result = 1 / global::System.Math.Cosh(value);
        }
        else if (name == OdfFormulaNames.SIGN)
        {
            result = value < 0 ? -1 : (value > 0 ? 1 : 0);
        }
        else if (name == OdfFormulaNames.SIN)
        {
            result = global::System.Math.Sin(value);
        }
        else if (name == OdfFormulaNames.SINH)
        {
            result = global::System.Math.Sinh(value);
        }
        else if (name == OdfFormulaNames.SQRT)
        {
            result = global::System.Math.Sqrt(value);
        }
        else if (name == OdfFormulaNames.SQRTPI)
        {
            result = global::System.Math.Sqrt(value * global::System.Math.PI);
        }
        else if (name == OdfFormulaNames.TAN)
        {
            result = global::System.Math.Tan(value);
        }
        else if (name == OdfFormulaNames.TANH)
        {
            result = global::System.Math.Tanh(value);
        }
        else
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.UnsupportedFunction);
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult Binary(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult first = Number(arguments[0]);
        OdfFormulaResult second = Number(arguments[1]);
        if (first.Error != OdfFormulaError.None)
        {
            return first;
        }
        if (second.Error != OdfFormulaError.None)
        {
            return second;
        }
        double left = first.NumericValue;
        double right = second.NumericValue;
        if ((name == OdfFormulaNames.MOD
            || name == OdfFormulaNames.QUOTIENT) && right == 0)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        if (name == OdfFormulaNames.ATAN2)
        {
            if (left == 0 && right == 0)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.DivisionByZero);
            }
            return FormulaValueModel.CreateNumber(
                global::System.Math.Atan2(right, left));
        }
        if (name == OdfFormulaNames.MOD)
        {
            return FormulaValueModel.CreateNumber(
                left - right * global::System.Math.Floor(left / right));
        }
        if (name == OdfFormulaNames.QUOTIENT)
        {
            return FormulaValueModel.CreateNumber(
                global::System.Math.Truncate(left / right));
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Pow(left, right));
    }

    static OdfFormulaResult CalculateSequence(
        string name,
        OdfFormulaResult[] arguments)
    {
        FormulaNumberSequence sequence = ReadSequence(arguments);
        if (sequence.Error != null)
        {
            return sequence.Error;
        }
        double result = name == OdfFormulaNames.PRODUCT ? 1 : 0;
        int index = 0;
        while (index < sequence.Values.Length)
        {
            double value = sequence.Values[index];
            if (name == OdfFormulaNames.PRODUCT)
            {
                result = result * value;
            }
            else if (name == OdfFormulaNames.SUMSQ)
            {
                result = result + value * value;
            }
            else
            {
                result = result + value;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult Combination(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult nValue = Number(arguments[0]);
        OdfFormulaResult rValue = Number(arguments[1]);
        if (nValue.Error != OdfFormulaError.None)
        {
            return nValue;
        }
        if (rValue.Error != OdfFormulaError.None)
        {
            return rValue;
        }
        long n = (long)global::System.Math.Truncate(nValue.NumericValue);
        long r = (long)global::System.Math.Truncate(rValue.NumericValue);
        if (name == OdfFormulaNames.COMBINA)
        {
            n = n + r - 1;
        }
        if (n < 0 || r < 0 || r > n)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        r = global::System.Math.Min(r, n - r);
        double result = 1;
        long index = 1;
        while (index <= r)
        {
            result = result * (n - r + index) / index;
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult CommonMultiple(
        string name,
        OdfFormulaResult[] arguments)
    {
        FormulaNumberSequence sequence = ReadSequence(arguments);
        if (sequence.Error != null)
        {
            return sequence.Error;
        }
        long result = name == OdfFormulaNames.GCD ? 0 : 1;
        int index = 0;
        while (index < sequence.Values.Length)
        {
            long value = (long)global::System.Math.Truncate(
                sequence.Values[index]);
            if (value < 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            long gcd = GreatestCommonDivisor(result, value);
            result = name == OdfFormulaNames.GCD
                ? gcd
                : (result == 0 || value == 0 ? 0 : result / gcd * value);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult Multinomial(OdfFormulaResult[] arguments)
    {
        FormulaNumberSequence sequence = ReadSequence(arguments);
        if (sequence.Error != null)
        {
            return sequence.Error;
        }
        long total = 0;
        double denominator = 1;
        int index = 0;
        while (index < sequence.Values.Length)
        {
            long value = (long)global::System.Math.Truncate(
                sequence.Values[index]);
            if (value < 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            total = total + value;
            denominator = denominator * Factorial(value);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(Factorial(total) / denominator);
    }

    static OdfFormulaResult FactorialFunction(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult number = Number(argument);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        long value = (long)global::System.Math.Truncate(number.NumericValue);
        if (value < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        if (name == OdfFormulaNames.FACT)
        {
            return FormulaValueModel.CreateNumber(Factorial(value));
        }
        double result = 1;
        long current = value < 2 ? 1 : value;
        while (current > 1)
        {
            result = result * current;
            current = current - 2;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult GammaFunction(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult number = Number(argument);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        double gamma = Gamma(number.NumericValue);
        return name == OdfFormulaNames.GAMMALN
            ? FormulaValueModel.CreateNumber(global::System.Math.Log(gamma))
            : FormulaValueModel.CreateNumber(gamma);
    }

    static OdfFormulaResult Logarithm(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult number = Number(arguments[0]);
        OdfFormulaResult baseValue = arguments.Length > 1
            ? Number(arguments[1])
            : FormulaValueModel.CreateNumber(10);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        if (baseValue.Error != OdfFormulaError.None)
        {
            return baseValue;
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Log(number.NumericValue, baseValue.NumericValue));
    }

    static OdfFormulaResult ComparisonFunction(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult first = Number(arguments[0]);
        OdfFormulaResult second = arguments.Length > 1
            ? Number(arguments[1])
            : FormulaValueModel.CreateNumber(0);
        if (first.Error != OdfFormulaError.None)
        {
            return first;
        }
        if (second.Error != OdfFormulaError.None)
        {
            return second;
        }
        bool result = name == OdfFormulaNames.DELTA
            ? first.NumericValue == second.NumericValue
            : first.NumericValue >= second.NumericValue;
        return FormulaValueModel.CreateNumber(result ? 1 : 0);
    }

    static OdfFormulaResult ErrorFunction(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult lower = Number(arguments[0]);
        OdfFormulaResult upper = arguments.Length > 1
            ? Number(arguments[1])
            : FormulaValueModel.CreateNumber(0);
        if (lower.Error != OdfFormulaError.None)
        {
            return lower;
        }
        if (upper.Error != OdfFormulaError.None)
        {
            return upper;
        }
        double result = arguments.Length > 1
            ? Erf(upper.NumericValue) - Erf(lower.NumericValue)
            : Erf(lower.NumericValue);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult RandomBetween(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult lowerValue = Number(arguments[0]);
        OdfFormulaResult upperValue = Number(arguments[1]);
        if (lowerValue.Error != OdfFormulaError.None)
        {
            return lowerValue;
        }
        if (upperValue.Error != OdfFormulaError.None)
        {
            return upperValue;
        }
        long lower = (long)global::System.Math.Ceiling(lowerValue.NumericValue);
        long upper = (long)global::System.Math.Floor(upperValue.NumericValue);
        if (lower > upper)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        OdfFormulaResult randomValue = DeterministicRandom(context);
        if (randomValue.Error != OdfFormulaError.None)
        {
            return randomValue;
        }
        double random = randomValue.NumericValue;
        return FormulaValueModel.CreateNumber(
            lower + global::System.Math.Floor(random * (upper - lower + 1)));
    }

    static OdfFormulaResult DeterministicRandom(
        FormulaFunctionContext context)
    {
        if (context.RandomValue < 0 || context.RandomValue >= 1)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.NotAvailable);
        }
        return FormulaValueModel.CreateNumber(context.RandomValue);
    }

    static OdfFormulaResult SeriesSum(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult x = Number(arguments[0]);
        OdfFormulaResult power = Number(arguments[1]);
        OdfFormulaResult step = Number(arguments[2]);
        if (x.Error != OdfFormulaError.None)
        {
            return x;
        }
        if (power.Error != OdfFormulaError.None)
        {
            return power;
        }
        if (step.Error != OdfFormulaError.None)
        {
            return step;
        }
        FormulaNumberSequence coefficients = ReadSequence([arguments[3]]);
        if (coefficients.Error != null)
        {
            return coefficients.Error;
        }
        double sum = 0;
        int index = 0;
        while (index < coefficients.Values.Length)
        {
            sum = sum + coefficients.Values[index]
                * global::System.Math.Pow(
                    x.NumericValue,
                    power.NumericValue + index * step.NumericValue);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(sum);
    }

    static OdfFormulaResult SumProduct(OdfFormulaResult[] arguments)
    {
        int length = arguments[0].Values.Length;
        if (length == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int argument = 0;
        while (argument < arguments.Length)
        {
            if (arguments[argument].Values.Length != length)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            argument = argument + 1;
        }
        double sum = 0;
        int index = 0;
        while (index < length)
        {
            double product = 1;
            argument = 0;
            while (argument < arguments.Length)
            {
                OdfFormulaResult number = Number(
                    arguments[argument].Values[index]);
                if (number.Error != OdfFormulaError.None)
                {
                    return number;
                }
                product = product * number.NumericValue;
                argument = argument + 1;
            }
            sum = sum + product;
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(sum);
    }

    static OdfFormulaResult SumPair(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult first = arguments[0];
        OdfFormulaResult second = arguments[1];
        if (first.Values.Length == 0
            || first.Values.Length != second.Values.Length)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double sum = 0;
        int index = 0;
        while (index < first.Values.Length)
        {
            OdfFormulaResult a = Number(first.Values[index]);
            OdfFormulaResult b = Number(second.Values[index]);
            if (a.Error != OdfFormulaError.None)
            {
                return a;
            }
            if (b.Error != OdfFormulaError.None)
            {
                return b;
            }
            if (name == OdfFormulaNames.SUMX2MY2)
            {
                sum = sum + a.NumericValue * a.NumericValue
                    - b.NumericValue * b.NumericValue;
            }
            else if (name == OdfFormulaNames.SUMX2PY2)
            {
                sum = sum + a.NumericValue * a.NumericValue
                    + b.NumericValue * b.NumericValue;
            }
            else
            {
                double difference = a.NumericValue - b.NumericValue;
                sum = sum + difference * difference;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(sum);
    }

    static OdfFormulaResult SumIf(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult criteriaRange = arguments[0];
        OdfFormulaResult sumRange = arguments.Length > 2
            ? arguments[2]
            : criteriaRange;
        if (criteriaRange.Kind != OdfValueKind.Reference
            || sumRange.Kind != OdfValueKind.Reference
            || criteriaRange.Values.Length != sumRange.Values.Length)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double sum = 0;
        int index = 0;
        while (index < criteriaRange.Values.Length)
        {
            if (FormulaDatabaseFunctions.MatchesCriterion(
                criteriaRange.Values[index],
                arguments[1]))
            {
                OdfFormulaResult number = Number(sumRange.Values[index]);
                if (number.Error == OdfFormulaError.None)
                {
                    sum = sum + number.NumericValue;
                }
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(sum);
    }

    static OdfFormulaResult SumIfs(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult sumRange = arguments[0];
        if (arguments.Length % 2 == 0
            || sumRange.Kind != OdfValueKind.Reference)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int pair = 1;
        while (pair < arguments.Length)
        {
            if (arguments[pair].Kind != OdfValueKind.Reference
                || arguments[pair].Values.Length != sumRange.Values.Length)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            pair = pair + 2;
        }
        double sum = 0;
        int index = 0;
        while (index < sumRange.Values.Length)
        {
            bool matches = true;
            pair = 1;
            while (pair < arguments.Length && matches)
            {
                matches = FormulaDatabaseFunctions.MatchesCriterion(
                    arguments[pair].Values[index],
                    arguments[pair + 1]);
                pair = pair + 2;
            }
            if (matches)
            {
                OdfFormulaResult number = Number(sumRange.Values[index]);
                if (number.Error == OdfFormulaError.None)
                {
                    sum = sum + number.NumericValue;
                }
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(sum);
    }

    static OdfFormulaResult Subtotal(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult functionValue = Number(arguments[0]);
        if (functionValue.Error != OdfFormulaError.None)
        {
            return functionValue;
        }
        int function = (int)global::System.Math.Truncate(
            functionValue.NumericValue);
        if (function >= 101 && function <= 111)
        {
            function = function - 100;
        }
        if (function < 1 || function > 11)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        FormulaNumberSequence sequence = ReadSequence([arguments[1]]);
        if (sequence.Error != null)
        {
            return sequence.Error;
        }
        if (function == 2)
        {
            return FormulaValueModel.CreateNumber(sequence.Values.Length);
        }
        if (function == 3)
        {
            int count = 0;
            int countIndex = 0;
            while (countIndex < arguments[1].Values.Length)
            {
                if (arguments[1].Values[countIndex].Kind
                    != OdfValueKind.Empty)
                {
                    count = count + 1;
                }
                countIndex = countIndex + 1;
            }
            return FormulaValueModel.CreateNumber(count);
        }
        if (sequence.Values.Length == 0)
        {
            return FormulaValueModel.CreateNumber(0);
        }
        double sum = 0;
        double product = 1;
        double minimum = sequence.Values[0];
        double maximum = minimum;
        int index = 0;
        while (index < sequence.Values.Length)
        {
            double value = sequence.Values[index];
            sum = sum + value;
            product = product * value;
            minimum = global::System.Math.Min(minimum, value);
            maximum = global::System.Math.Max(maximum, value);
            index = index + 1;
        }
        if (function == 1)
        {
            return FormulaValueModel.CreateNumber(sum / sequence.Values.Length);
        }
        if (function == 4 || function == 5 || function == 6 || function == 9)
        {
            double simple = function == 4
                ? maximum
                : (function == 5 ? minimum : (function == 6 ? product : sum));
            return FormulaValueModel.CreateNumber(simple);
        }
        bool sample = function == 7 || function == 10;
        if (sample && sequence.Values.Length < 2)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        double mean = sum / sequence.Values.Length;
        double squared = 0;
        index = 0;
        while (index < sequence.Values.Length)
        {
            double difference = sequence.Values[index] - mean;
            squared = squared + difference * difference;
            index = index + 1;
        }
        double variance = squared
            / (sample ? sequence.Values.Length - 1 : sequence.Values.Length);
        return function == 7 || function == 8
            ? FormulaValueModel.CreateNumber(global::System.Math.Sqrt(variance))
            : FormulaValueModel.CreateNumber(variance);
    }

    static OdfFormulaResult Bessel(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult xValue = Number(arguments[0]);
        OdfFormulaResult orderValue = Number(arguments[1]);
        if (xValue.Error != OdfFormulaError.None)
        {
            return xValue;
        }
        if (orderValue.Error != OdfFormulaError.None)
        {
            return orderValue;
        }
        int order = (int)global::System.Math.Truncate(orderValue.NumericValue);
        double x = xValue.NumericValue;
        if (order < 0 || ((name == OdfFormulaNames.BESSELK
            || name == OdfFormulaNames.BESSELY) && x <= 0))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double result = BesselIntegral(name, x, order);
        return FormulaValueModel.CreateNumber(result);
    }

    static double BesselIntegral(string name, double x, int order)
    {
        if (name == OdfFormulaNames.BESSELY)
        {
            return BesselYIntegral(x, order);
        }
        int steps = 1024;
        double upper = name == OdfFormulaNames.BESSELK
            ? 16
            : global::System.Math.PI;
        double width = upper / steps;
        double sum = 0;
        int index = 0;
        while (index <= steps)
        {
            double t = index * width;
            double value;
            if (name == OdfFormulaNames.BESSELI)
            {
                value = global::System.Math.Exp(x * global::System.Math.Cos(t))
                    * global::System.Math.Cos(order * t)
                    / global::System.Math.PI;
            }
            else if (name == OdfFormulaNames.BESSELJ)
            {
                value = global::System.Math.Cos(
                    order * t - x * global::System.Math.Sin(t))
                    / global::System.Math.PI;
            }
            else if (name == OdfFormulaNames.BESSELK)
            {
                value = global::System.Math.Exp(
                    -x * global::System.Math.Cosh(t))
                    * global::System.Math.Cosh(order * t);
            }
            else value = 0;
            int weight = index == 0 || index == steps
                ? 1
                : (index % 2 == 0 ? 2 : 4);
            sum = sum + weight * value;
            index = index + 1;
        }
        return sum * width / 3;
    }

    static double BesselYIntegral(double x, int order)
    {
        int steps = 1024;
        double firstWidth = global::System.Math.PI / steps;
        double secondWidth = 16.0 / steps;
        double first = 0;
        double second = 0;
        int index = 0;
        while (index <= steps)
        {
            double firstT = index * firstWidth;
            double secondT = index * secondWidth;
            int weight = index == 0 || index == steps
                ? 1
                : (index % 2 == 0 ? 2 : 4);
            first = first + weight * global::System.Math.Sin(
                x * global::System.Math.Sin(firstT) - order * firstT);
            second = second + weight * global::System.Math.Exp(
                -x * global::System.Math.Sinh(secondT))
                * (global::System.Math.Exp(order * secondT)
                    + (order % 2 == 0 ? 1 : -1)
                    * global::System.Math.Exp(-order * secondT));
            index = index + 1;
        }
        return (first * firstWidth - second * secondWidth)
            / (3 * global::System.Math.PI);
    }

    static OdfFormulaResult EuroConvert(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult amount = Number(arguments[0]);
        OdfFormulaResult from = FormulaValueModel.ConvertToText(arguments[1]);
        OdfFormulaResult to = FormulaValueModel.ConvertToText(arguments[2]);
        if (amount.Error != OdfFormulaError.None)
        {
            return amount;
        }
        if (from.Error != OdfFormulaError.None)
        {
            return from;
        }
        if (to.Error != OdfFormulaError.None)
        {
            return to;
        }
        double fromRate = EuroRate(from.StringValue.ToUpperInvariant());
        double toRate = EuroRate(to.StringValue.ToUpperInvariant());
        if (fromRate == 0 || toRate == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        string fromCurrency = from.StringValue.ToUpperInvariant();
        string toCurrency = to.StringValue.ToUpperInvariant();
        if (fromCurrency == toCurrency)
        {
            return FormulaValueModel.CreateNumber(amount.NumericValue);
        }
        bool fullPrecision = false;
        if (arguments.Length > 3)
        {
            OdfFormulaResult precisionValue =
                FormulaValueModel.ConvertToBoolean(arguments[3]);
            if (precisionValue.Error != OdfFormulaError.None)
            {
                return precisionValue;
            }
            fullPrecision = precisionValue.BooleanValue;
        }
        double intermediate = amount.NumericValue / fromRate;
        if (arguments.Length > 4)
        {
            OdfFormulaResult digitsValue = Number(arguments[4]);
            if (digitsValue.Error != OdfFormulaError.None)
            {
                return digitsValue;
            }
            int digits = (int)global::System.Math.Truncate(
                digitsValue.NumericValue);
            if (digits < 3)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            intermediate = global::System.Math.Round(
                intermediate,
                digits,
                global::System.MidpointRounding.AwayFromZero);
        }
        double converted = intermediate * toRate;
        if (!fullPrecision)
        {
            converted = global::System.Math.Round(
                converted,
                EuroDecimals(toCurrency),
                global::System.MidpointRounding.AwayFromZero);
        }
        return FormulaValueModel.CreateNumber(converted);
    }

    static double EuroRate(string currency)
    {
        if (currency == "EUR") return 1;
        if (currency == "ATS") return 13.7603;
        if (currency == "BEF" || currency == "LUF") return 40.3399;
        if (currency == "DEM") return 1.95583;
        if (currency == "ESP") return 166.386;
        if (currency == "FIM") return 5.94573;
        if (currency == "FRF") return 6.55957;
        if (currency == "IEP") return 0.787564;
        if (currency == "ITL") return 1936.27;
        if (currency == "NLG") return 2.20371;
        if (currency == "PTE") return 200.482;
        if (currency == "GRD") return 340.750;
        if (currency == "SIT") return 239.640;
        if (currency == "CYP") return 0.585274;
        if (currency == "MTL") return 0.429300;
        if (currency == "SKK") return 30.1260;
        if (currency == "EEK") return 15.6466;
        if (currency == "LVL") return 0.702804;
        return currency == "LTL" ? 3.45280 : 0;
    }

    static int EuroDecimals(string currency)
    {
        return currency == "BEF"
            || currency == "ESP"
            || currency == "ITL"
            || currency == "LUF"
            ? 0
            : 2;
    }

    static FormulaNumberSequence ReadSequence(OdfFormulaResult[] arguments)
    {
        FormulaNumberSequence result = new FormulaNumberSequence();
        List<double> values = new List<double>();
        int argument = 0;
        while (argument < arguments.Length)
        {
            OdfFormulaResult current = arguments[argument];
            if (current.Error != OdfFormulaError.None)
            {
                result.Error = current;
                return result;
            }
            if (current.Kind == OdfValueKind.Array
                || current.Kind == OdfValueKind.Reference)
            {
                int index = 0;
                while (index < current.Values.Length)
                {
                    OdfFormulaResult item = current.Values[index];
                    if (item.Error != OdfFormulaError.None)
                    {
                        result.Error = item;
                        return result;
                    }
                    if (IsNumeric(item.Kind))
                    {
                        values.Add(item.NumericValue);
                    }
                    index = index + 1;
                }
            }
            else
            {
                OdfFormulaResult number = Number(current);
                if (number.Error != OdfFormulaError.None)
                {
                    result.Error = number;
                    return result;
                }
                values.Add(number.NumericValue);
            }
            argument = argument + 1;
        }
        result.Values = values.ToArray();
        return result;
    }

    static OdfFormulaResult Number(OdfFormulaResult value)
    {
        return FormulaValueModel.ConvertToNumber(value);
    }

    static bool IsNumeric(OdfValueKind kind)
    {
        return kind == OdfValueKind.Float
            || kind == OdfValueKind.Decimal
            || kind == OdfValueKind.Percentage
            || kind == OdfValueKind.Currency
            || kind == OdfValueKind.Date
            || kind == OdfValueKind.DateTime
            || kind == OdfValueKind.Time
            || kind == OdfValueKind.Duration;
    }

    static double Factorial(long value)
    {
        double result = 1;
        long index = 2;
        while (index <= value)
        {
            result = result * index;
            index = index + 1;
        }
        return result;
    }

    static long GreatestCommonDivisor(long first, long second)
    {
        first = global::System.Math.Abs(first);
        second = global::System.Math.Abs(second);
        while (second != 0)
        {
            long remainder = first % second;
            first = second;
            second = remainder;
        }
        return first;
    }

    static double RoundAway(double value, int multiple)
    {
        double magnitude = global::System.Math.Ceiling(
            global::System.Math.Abs(value) / multiple) * multiple;
        return value < 0 ? -magnitude : magnitude;
    }

    static double Erf(double value)
    {
        double sign = value < 0 ? -1 : 1;
        double x = global::System.Math.Abs(value);
        double t = 1 / (1 + 0.3275911 * x);
        double polynomial = (((((1.061405429 * t - 1.453152027) * t)
            + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t;
        return sign * (1 - polynomial * global::System.Math.Exp(-x * x));
    }

    static double Gamma(double value)
    {
        if (value < 0.5)
        {
            return global::System.Math.PI
                / (global::System.Math.Sin(global::System.Math.PI * value)
                    * Gamma(1 - value));
        }
        double[] coefficients =
        [
            0.99999999999980993,
            676.5203681218851,
            -1259.1392167224028,
            771.32342877765313,
            -176.61502916214059,
            12.507343278686905,
            -0.13857109526572012,
            0.0000099843695780195716,
            0.00000015056327351493116
        ];
        double shifted = value - 1;
        double sum = coefficients[0];
        int index = 1;
        while (index < coefficients.Length)
        {
            sum = sum + coefficients[index] / (shifted + index);
            index = index + 1;
        }
        double t = shifted + 7.5;
        return global::System.Math.Sqrt(2 * global::System.Math.PI)
            * global::System.Math.Pow(t, shifted + 0.5)
            * global::System.Math.Exp(-t) * sum;
    }
}
