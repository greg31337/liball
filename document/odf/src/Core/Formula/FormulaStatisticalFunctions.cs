using System.Collections.Generic;

namespace odf;

internal class FormulaStatisticalSequence
{
    internal double[] Values = [];
    internal OdfFormulaResult? Error;
}

internal class FormulaRegressionValues
{
    internal double Slope;
    internal double Intercept;
    internal double Correlation;
    internal double ResidualSum;
    internal double RegressionSum;
    internal double StandardError;
    internal int Count;
    internal OdfFormulaResult? Error;
}

internal class FormulaRegressionData
{
    internal double[] Y = [];
    internal double[,] Predictors = new double[0, 0];
    internal int PredictorCount;
    internal int Orientation;
    internal int OutputRows;
    internal int OutputColumns;
    internal OdfFormulaResult? Error;
}

internal class FormulaMultipleRegressionValues
{
    internal double[] Coefficients = [];
    internal double[] CoefficientErrors = [];
    internal double Intercept;
    internal double InterceptError;
    internal double RSquared;
    internal double ResidualSum;
    internal double RegressionSum;
    internal double StandardError;
    internal double FStatistic;
    internal int Degrees;
    internal OdfFormulaResult? Error;
}

internal static class FormulaStatisticalFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.AVERAGEIF)
        {
            return ConditionalMean(arguments);
        }
        if (name == OdfFormulaNames.AVERAGEIFS)
        {
            return ConditionalMeans(arguments);
        }
        if (name == OdfFormulaNames.FREQUENCY)
        {
            return Frequency(arguments);
        }
        if (name == OdfFormulaNames.LARGE
            || name == OdfFormulaNames.SMALL)
        {
            return RankedValue(name, arguments);
        }
        if (name == OdfFormulaNames.PERCENTILE
            || name == OdfFormulaNames.QUARTILE
            || name == OdfFormulaNames.PERCENTRANK
            || name == OdfFormulaNames.RANK)
        {
            return RankingFunction(name, arguments);
        }
        if (name == OdfFormulaNames.CORREL
            || name == OdfFormulaNames.PEARSON
            || name == OdfFormulaNames.COVAR
            || name == OdfFormulaNames.SLOPE
            || name == OdfFormulaNames.INTERCEPT
            || name == OdfFormulaNames.RSQ
            || name == OdfFormulaNames.STEYX
            || name == OdfFormulaNames.FORECAST)
        {
            return RegressionScalar(name, arguments);
        }
        if (name == OdfFormulaNames.LINEST
            || name == OdfFormulaNames.LOGEST
            || name == OdfFormulaNames.TREND
            || name == OdfFormulaNames.GROWTH)
        {
            return RegressionArray(name, arguments);
        }
        if (name == OdfFormulaNames.BETADIST
            || name == OdfFormulaNames.BETAINV
            || name == OdfFormulaNames.CHISQDIST
            || name == OdfFormulaNames.CHISQINV
            || name == OdfFormulaNames.EXPONDIST
            || name == OdfFormulaNames.FDIST
            || name == OdfFormulaNames.FINV
            || name == OdfFormulaNames.GAMMADIST
            || name == OdfFormulaNames.GAMMAINV
            || name == OdfFormulaNames.GAUSS
            || name == OdfFormulaNames.LOGINV
            || name == OdfFormulaNames.LOGNORMDIST
            || name == OdfFormulaNames.NORMDIST
            || name == OdfFormulaNames.NORMINV
            || name == OdfFormulaNames.PHI
            || name == OdfFormulaNames.TINV
            || name == OdfFormulaNames.WEIBULL)
        {
            return ContinuousDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.BINOM_DIST_RANGE
            || name == OdfFormulaNames.BINOMDIST
            || name == OdfFormulaNames.CRITBINOM
            || name == OdfFormulaNames.HYPGEOMDIST
            || name == OdfFormulaNames.NEGBINOMDIST
            || name == OdfFormulaNames.POISSON
            || name == OdfFormulaNames.PROB
            || name == OdfFormulaNames.PERMUT
            || name == OdfFormulaNames.PERMUTATIONA)
        {
            return DiscreteDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.CONFIDENCE
            || name == OdfFormulaNames.FTEST
            || name == OdfFormulaNames.STANDARDIZE
            || name == OdfFormulaNames.TTEST
            || name == OdfFormulaNames.ZTEST)
        {
            return StatisticalTest(name, arguments);
        }
        if (name == OdfFormulaNames.FISHER
            || name == OdfFormulaNames.FISHERINV)
        {
            return Fisher(name, arguments[0]);
        }
        return Descriptive(name, arguments);
    }

    internal static OdfFormulaResult EvaluateLegacy(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.LEGACY_CHIDIST
            || name == OdfFormulaNames.LEGACY_CHIINV)
        {
            return LegacyChiDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.LEGACY_CHITEST)
        {
            return LegacyChiTest(arguments);
        }
        if (name == OdfFormulaNames.LEGACY_FDIST
            || name == OdfFormulaNames.LEGACY_FINV)
        {
            return LegacyFDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.LEGACY_NORMSDIST
            || name == OdfFormulaNames.LEGACY_NORMSINV)
        {
            return LegacyNormalDistribution(name, arguments[0]);
        }
        return LegacyTDistribution(arguments);
    }

    static OdfFormulaResult Descriptive(
        string name,
        OdfFormulaResult[] arguments)
    {
        bool includeAll = name == OdfFormulaNames.AVERAGEA
            || name == OdfFormulaNames.MAXA
            || name == OdfFormulaNames.MINA
            || name == OdfFormulaNames.STDEVA
            || name == OdfFormulaNames.STDEVPA
            || name == OdfFormulaNames.VARA
            || name == OdfFormulaNames.VARPA;
        OdfFormulaResult[] dataArguments = name == OdfFormulaNames.TRIMMEAN
            ? [arguments[0]]
            : arguments;
        FormulaStatisticalSequence sequence = ReadSequence(
            dataArguments,
            includeAll);
        if (sequence.Error != null)
        {
            return sequence.Error;
        }
        double[] values = sequence.Values;
        if (values.Length == 0)
        {
            if (name == OdfFormulaNames.MAX
                || name == OdfFormulaNames.MAXA
                || name == OdfFormulaNames.MIN
                || name == OdfFormulaNames.MINA)
            {
                return FormulaValueModel.CreateNumber(0);
            }
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        if (name == OdfFormulaNames.MAX || name == OdfFormulaNames.MAXA)
        {
            return FormulaValueModel.CreateNumber(MaximumValue(values));
        }
        if (name == OdfFormulaNames.MIN || name == OdfFormulaNames.MINA)
        {
            return FormulaValueModel.CreateNumber(MinimumValue(values));
        }
        if (name == OdfFormulaNames.AVERAGE
            || name == OdfFormulaNames.AVERAGEA)
        {
            return FormulaValueModel.CreateNumber(MeanValue(values));
        }
        if (name == OdfFormulaNames.AVEDEV)
        {
            double mean = MeanValue(values);
            double total = 0;
            int index = 0;
            while (index < values.Length)
            {
                total = total + global::System.Math.Abs(values[index] - mean);
                index = index + 1;
            }
            return FormulaValueModel.CreateNumber(total / values.Length);
        }
        if (name == OdfFormulaNames.DEVSQ)
        {
            return FormulaValueModel.CreateNumber(DeviationTotal(values));
        }
        if (name == OdfFormulaNames.GEOMEAN
            || name == OdfFormulaNames.HARMEAN)
        {
            return SpecializedMean(name, values);
        }
        if (name == OdfFormulaNames.MEDIAN
            || name == OdfFormulaNames.MODE
            || name == OdfFormulaNames.TRIMMEAN)
        {
            return OrderedStatistic(name, values, arguments);
        }
        if (name == OdfFormulaNames.KURT
            || name == OdfFormulaNames.SKEW
            || name == OdfFormulaNames.SKEWP)
        {
            return MomentStatistic(name, values);
        }
        bool sample = name == OdfFormulaNames.STDEV
            || name == OdfFormulaNames.STDEVA
            || name == OdfFormulaNames.VAR
            || name == OdfFormulaNames.VARA;
        double variance = VarianceValue(values, sample);
        if (!double.IsFinite(variance))
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        bool deviation = name == OdfFormulaNames.STDEV
            || name == OdfFormulaNames.STDEVA
            || name == OdfFormulaNames.STDEVP
            || name == OdfFormulaNames.STDEVPA;
        return FormulaValueModel.CreateNumber(
            deviation ? global::System.Math.Sqrt(variance) : variance);
    }

    static OdfFormulaResult SpecializedMean(string name, double[] values)
    {
        double total = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] <= 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            total = name == OdfFormulaNames.GEOMEAN
                ? total + global::System.Math.Log(values[index])
                : total + 1 / values[index];
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(
            name == OdfFormulaNames.GEOMEAN
                ? global::System.Math.Exp(total / values.Length)
                : values.Length / total);
    }

    static OdfFormulaResult OrderedStatistic(
        string name,
        double[] source,
        OdfFormulaResult[] arguments)
    {
        double[] values = CopyAndSort(source);
        if (name == OdfFormulaNames.MEDIAN)
        {
            int middle = values.Length / 2;
            return FormulaValueModel.CreateNumber(values.Length % 2 == 1
                ? values[middle]
                : (values[middle - 1] + values[middle]) / 2);
        }
        if (name == OdfFormulaNames.MODE)
        {
            int bestCount = 1;
            int currentCount = 1;
            double best = values[0];
            int index = 1;
            while (index < values.Length)
            {
                if (values[index] == values[index - 1])
                {
                    currentCount = currentCount + 1;
                }
                else
                {
                    currentCount = 1;
                }
                if (currentCount > bestCount)
                {
                    bestCount = currentCount;
                    best = values[index];
                }
                index = index + 1;
            }
            return bestCount > 1
                ? FormulaValueModel.CreateNumber(best)
                : FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        OdfFormulaResult fraction = Number(arguments[1]);
        if (fraction.Error != OdfFormulaError.None
            || fraction.NumericValue < 0
            || fraction.NumericValue >= 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        int removed = (int)global::System.Math.Floor(
            values.Length * fraction.NumericValue);
        removed = removed - removed % 2;
        int eachSide = removed / 2;
        double total = 0;
        int trimIndex = eachSide;
        while (trimIndex < values.Length - eachSide)
        {
            total = total + values[trimIndex];
            trimIndex = trimIndex + 1;
        }
        return FormulaValueModel.CreateNumber(
            total / (values.Length - removed));
    }

    static OdfFormulaResult MomentStatistic(string name, double[] values)
    {
        int count = values.Length;
        if ((name == OdfFormulaNames.KURT && count < 4)
            || (name == OdfFormulaNames.SKEW && count < 3)
            || (name == OdfFormulaNames.SKEWP && count < 2))
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        double mean = MeanValue(values);
        double second = 0;
        double third = 0;
        double fourth = 0;
        int index = 0;
        while (index < count)
        {
            double difference = values[index] - mean;
            double square = difference * difference;
            second = second + square;
            third = third + square * difference;
            fourth = fourth + square * square;
            index = index + 1;
        }
        if (second == 0)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        if (name == OdfFormulaNames.SKEWP)
        {
            double deviation = global::System.Math.Sqrt(second / count);
            return FormulaValueModel.CreateNumber(
                third / count / (deviation * deviation * deviation));
        }
        if (name == OdfFormulaNames.SKEW)
        {
            double deviation = global::System.Math.Sqrt(second / (count - 1));
            return FormulaValueModel.CreateNumber(
                count * third
                / ((count - 1.0) * (count - 2)
                    * deviation * deviation * deviation));
        }
        double variance = second / (count - 1);
        double normalizedFourth = fourth / (variance * variance);
        double result = count * (count + 1.0) * normalizedFourth
            / ((count - 1.0) * (count - 2) * (count - 3))
            - 3.0 * (count - 1) * (count - 1)
            / ((count - 2.0) * (count - 3));
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult ConditionalMean(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult criteria = arguments[0];
        OdfFormulaResult average = arguments.Length > 2
            ? arguments[2]
            : criteria;
        if (!MatchingShapes(criteria, average))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double total = 0;
        int count = 0;
        int index = 0;
        while (index < criteria.Values.Length)
        {
            if (FormulaDatabaseFunctions.MatchesCriterion(
                criteria.Values[index], arguments[1]))
            {
                OdfFormulaResult number = Number(average.Values[index]);
                if (number.Error == OdfFormulaError.None)
                {
                    total = total + number.NumericValue;
                    count = count + 1;
                }
            }
            index = index + 1;
        }
        return count == 0
            ? FormulaValueModel.CreateError(OdfFormulaError.DivisionByZero)
            : FormulaValueModel.CreateNumber(total / count);
    }

    static OdfFormulaResult ConditionalMeans(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult average = arguments[0];
        if (arguments.Length % 2 == 0 || average.Values.Length == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int pair = 1;
        while (pair < arguments.Length)
        {
            if (!MatchingShapes(average, arguments[pair]))
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            pair = pair + 2;
        }
        double total = 0;
        int count = 0;
        int index = 0;
        while (index < average.Values.Length)
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
                OdfFormulaResult number = Number(average.Values[index]);
                if (number.Error == OdfFormulaError.None)
                {
                    total = total + number.NumericValue;
                    count = count + 1;
                }
            }
            index = index + 1;
        }
        return count == 0
            ? FormulaValueModel.CreateError(OdfFormulaError.DivisionByZero)
            : FormulaValueModel.CreateNumber(total / count);
    }

    static OdfFormulaResult Frequency(OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence data = ReadSequence([arguments[0]], false);
        FormulaStatisticalSequence bins = ReadSequence([arguments[1]], false);
        if (data.Error != null) return data.Error;
        if (bins.Error != null) return bins.Error;
        double[] sortedBins = CopyAndSort(bins.Values);
        double[] sortedCounts = new double[sortedBins.Length + 1];
        int index = 0;
        while (index < data.Values.Length)
        {
            int bin = 0;
            while (bin < sortedBins.Length
                && data.Values[index] > sortedBins[bin])
            {
                bin = bin + 1;
            }
            sortedCounts[bin] = sortedCounts[bin] + 1;
            index = index + 1;
        }
        OdfFormulaResult[] result =
            new OdfFormulaResult[sortedBins.Length + 1];
        bool[] used = new bool[sortedBins.Length];
        index = 0;
        while (index < bins.Values.Length)
        {
            int sortedIndex = 0;
            while (sortedIndex < sortedBins.Length
                && (used[sortedIndex]
                    || sortedBins[sortedIndex] != bins.Values[index]))
            {
                sortedIndex = sortedIndex + 1;
            }
            used[sortedIndex] = true;
            result[index] = FormulaValueModel.CreateNumber(
                sortedCounts[sortedIndex]);
            index = index + 1;
        }
        result[result.Length - 1] = FormulaValueModel.CreateNumber(
            sortedCounts[sortedCounts.Length - 1]);
        return FormulaValueModel.CreateArray(result, result.Length, 1);
    }

    static OdfFormulaResult RankedValue(
        string name,
        OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence sequence = ReadSequence([arguments[0]], false);
        if (sequence.Error != null) return sequence.Error;
        double[] values = CopyAndSort(sequence.Values);
        OdfFormulaResult positions = arguments[1];
        if (positions.Kind == OdfValueKind.Array
            || positions.Kind == OdfValueKind.Reference)
        {
            OdfFormulaResult[] result =
                new OdfFormulaResult[positions.Values.Length];
            int index = 0;
            while (index < result.Length)
            {
                result[index] = SelectRanked(
                    name,
                    values,
                    positions.Values[index]);
                index = index + 1;
            }
            return FormulaValueModel.CreateArray(
                result,
                positions.RowCount,
                positions.ColumnCount);
        }
        return SelectRanked(name, values, positions);
    }

    static OdfFormulaResult SelectRanked(
        string name,
        double[] values,
        OdfFormulaResult position)
    {
        OdfFormulaResult number = Number(position);
        if (number.Error != OdfFormulaError.None) return number;
        int rank = (int)global::System.Math.Truncate(number.NumericValue);
        if (rank < 1 || rank > values.Length)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        int index = name == OdfFormulaNames.LARGE
            ? values.Length - rank
            : rank - 1;
        return FormulaValueModel.CreateNumber(values[index]);
    }

    static OdfFormulaResult RankingFunction(
        string name,
        OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence sequence = ReadSequence(
            [arguments[name == OdfFormulaNames.RANK ? 1 : 0]],
            false);
        if (sequence.Error != null) return sequence.Error;
        if (sequence.Values.Length == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        double[] values = CopyAndSort(sequence.Values);
        if (name == OdfFormulaNames.PERCENTILE
            || name == OdfFormulaNames.QUARTILE)
        {
            OdfFormulaResult fractionValue = Number(arguments[1]);
            if (fractionValue.Error != OdfFormulaError.None) return fractionValue;
            double fraction = name == OdfFormulaNames.QUARTILE
                ? global::System.Math.Truncate(fractionValue.NumericValue) / 4
                : fractionValue.NumericValue;
            return PercentileValue(values, fraction);
        }
        OdfFormulaResult soughtValue = Number(
            arguments[name == OdfFormulaNames.RANK ? 0 : 1]);
        if (soughtValue.Error != OdfFormulaError.None) return soughtValue;
        double sought = soughtValue.NumericValue;
        if (name == OdfFormulaNames.RANK)
        {
            bool ascending = false;
            if (arguments.Length > 2)
            {
                OdfFormulaResult order = Number(arguments[2]);
                if (order.Error != OdfFormulaError.None) return order;
                ascending = order.NumericValue != 0;
            }
            int index = 0;
            while (index < values.Length && values[index] != sought)
            {
                index = index + 1;
            }
            if (index == values.Length)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.NotAvailable);
            }
            int last = index;
            while (last + 1 < values.Length
                && values[last + 1] == sought)
            {
                last = last + 1;
            }
            return FormulaValueModel.CreateNumber(
                ascending ? index + 1 : values.Length - last);
        }
        if (sought < values[0] || sought > values[values.Length - 1])
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        int lower = 0;
        while (lower + 1 < values.Length && values[lower + 1] <= sought)
        {
            lower = lower + 1;
        }
        double rank = lower;
        if (values[lower] != sought && lower + 1 < values.Length)
        {
            rank = lower + (sought - values[lower])
                / (values[lower + 1] - values[lower]);
        }
        int significance = 3;
        if (arguments.Length > 2)
        {
            OdfFormulaResult significanceValue = Number(arguments[2]);
            if (significanceValue.Error != OdfFormulaError.None)
                return significanceValue;
            significance = (int)global::System.Math.Truncate(
                significanceValue.NumericValue);
        }
        if (significance < 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        if (values.Length == 1)
        {
            return FormulaValueModel.CreateNumber(1);
        }
        return FormulaValueModel.CreateNumber(global::System.Math.Round(
            rank / (values.Length - 1),
            significance,
            global::System.MidpointRounding.AwayFromZero));
    }

    static OdfFormulaResult PercentileValue(double[] values, double fraction)
    {
        if (fraction < 0 || fraction > 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double position = fraction * (values.Length - 1);
        int lower = (int)global::System.Math.Floor(position);
        int upper = (int)global::System.Math.Ceiling(position);
        double result = values[lower]
            + (values[upper] - values[lower]) * (position - lower);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult RegressionScalar(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult ySource = name == OdfFormulaNames.FORECAST
            ? arguments[1]
            : arguments[0];
        OdfFormulaResult xSource = name == OdfFormulaNames.FORECAST
            ? arguments[2]
            : arguments[1];
        FormulaStatisticalSequence y = ReadSequence([ySource], false);
        FormulaStatisticalSequence x = ReadSequence([xSource], false);
        if (y.Error != null) return y.Error;
        if (x.Error != null) return x.Error;
        FormulaRegressionValues regression = FitLine(y.Values, x.Values, true);
        if (regression.Error != null) return regression.Error;
        if (name == OdfFormulaNames.CORREL
            || name == OdfFormulaNames.PEARSON
            || name == OdfFormulaNames.RSQ)
        {
            if (DeviationTotal(y.Values) == 0)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.DivisionByZero);
            }
            if (name == OdfFormulaNames.RSQ)
            {
                return FormulaValueModel.CreateNumber(
                    regression.Correlation * regression.Correlation);
            }
            return FormulaValueModel.CreateNumber(regression.Correlation);
        }
        if (name == OdfFormulaNames.COVAR)
        {
            return FormulaValueModel.CreateNumber(
                regression.Slope * VarianceValue(x.Values, false));
        }
        if (name == OdfFormulaNames.SLOPE)
        {
            return FormulaValueModel.CreateNumber(regression.Slope);
        }
        if (name == OdfFormulaNames.INTERCEPT)
        {
            return FormulaValueModel.CreateNumber(regression.Intercept);
        }
        if (name == OdfFormulaNames.STEYX)
        {
            return FormulaValueModel.CreateNumber(regression.StandardError);
        }
        OdfFormulaResult forecast = Number(arguments[0]);
        return forecast.Error == OdfFormulaError.None
            ? FormulaValueModel.CreateNumber(
                regression.Intercept
                    + regression.Slope * forecast.NumericValue)
            : forecast;
    }

    static OdfFormulaResult RegressionArray(
        string name,
        OdfFormulaResult[] arguments)
    {
        bool exponential = name == OdfFormulaNames.LOGEST
            || name == OdfFormulaNames.GROWTH;
        OdfFormulaResult? knownX = arguments.Length > 1
            && arguments[1].Kind != OdfValueKind.Empty
            ? arguments[1]
            : null;
        FormulaRegressionData data = CreateRegressionData(
            arguments[0],
            knownX);
        if (data.Error != null) return data.Error;
        if (exponential)
        {
            int yIndex = 0;
            while (yIndex < data.Y.Length)
            {
                if (data.Y[yIndex] <= 0)
                {
                    return FormulaValueModel.CreateError(OdfFormulaError.Number);
                }
                data.Y[yIndex] = global::System.Math.Log(data.Y[yIndex]);
                yIndex = yIndex + 1;
            }
        }
        bool constant = true;
        int constantIndex = name == OdfFormulaNames.LINEST
            || name == OdfFormulaNames.LOGEST
            ? 2
            : 3;
        if (arguments.Length > constantIndex
            && arguments[constantIndex].Kind != OdfValueKind.Empty)
        {
            OdfFormulaResult constantValue =
                FormulaValueModel.ConvertToBoolean(arguments[constantIndex]);
            if (constantValue.Error != OdfFormulaError.None)
                return constantValue;
            constant = constantValue.BooleanValue;
        }
        FormulaMultipleRegressionValues regression = FitMultipleRegression(
            data,
            constant);
        if (regression.Error != null) return regression.Error;
        if (name == OdfFormulaNames.LINEST || name == OdfFormulaNames.LOGEST)
        {
            bool stats = false;
            if (arguments.Length > 3)
            {
                OdfFormulaResult statsValue =
                    FormulaValueModel.ConvertToBoolean(arguments[3]);
                if (statsValue.Error != OdfFormulaError.None) return statsValue;
                stats = statsValue.BooleanValue;
            }
            return RegressionParameters(regression, exponential, stats);
        }
        OdfFormulaResult? requestedX = arguments.Length > 2
            && arguments[2].Kind != OdfValueKind.Empty
            ? arguments[2]
            : null;
        FormulaRegressionData prediction = CreatePredictionData(
            data,
            requestedX);
        if (prediction.Error != null) return prediction.Error;
        OdfFormulaResult[] result =
            new OdfFormulaResult[prediction.Y.Length];
        int index = 0;
        while (index < result.Length)
        {
            double predicted = regression.Intercept;
            int predictor = 0;
            while (predictor < data.PredictorCount)
            {
                predicted = predicted
                    + regression.Coefficients[predictor]
                        * prediction.Predictors[index, predictor];
                predictor = predictor + 1;
            }
            if (exponential)
            {
                predicted = global::System.Math.Exp(predicted);
            }
            result[index] = FormulaValueModel.CreateNumber(predicted);
            index = index + 1;
        }
        return FormulaValueModel.CreateArray(
            result,
            prediction.OutputRows,
            prediction.OutputColumns);
    }

    static OdfFormulaResult RegressionParameters(
        FormulaMultipleRegressionValues regression,
        bool exponential,
        bool stats)
    {
        int columns = regression.Coefficients.Length + 1;
        OdfFormulaResult[] result = new OdfFormulaResult[
            (stats ? 5 : 1) * columns];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = FormulaValueModel.CreateEmpty();
            index = index + 1;
        }
        int coefficient = 0;
        while (coefficient < regression.Coefficients.Length)
        {
            double value = regression.Coefficients[coefficient];
            if (exponential) value = global::System.Math.Exp(value);
            result[regression.Coefficients.Length - coefficient - 1] =
                FormulaValueModel.CreateNumber(value);
            coefficient = coefficient + 1;
        }
        double intercept = exponential
            ? global::System.Math.Exp(regression.Intercept)
            : regression.Intercept;
        result[columns - 1] = FormulaValueModel.CreateNumber(intercept);
        if (!stats)
        {
            return FormulaValueModel.CreateArray(result, 1, columns);
        }
        coefficient = 0;
        while (coefficient < regression.CoefficientErrors.Length)
        {
            result[columns + regression.CoefficientErrors.Length
                - coefficient - 1] = FormulaValueModel.CreateNumber(
                    regression.CoefficientErrors[coefficient]);
            coefficient = coefficient + 1;
        }
        result[2 * columns - 1] = FormulaValueModel.CreateNumber(
            regression.InterceptError);
        result[2 * columns] = FormulaValueModel.CreateNumber(
            regression.RSquared);
        result[2 * columns + 1] = FormulaValueModel.CreateNumber(
            regression.StandardError);
        result[3 * columns] = FormulaValueModel.CreateNumber(
            regression.FStatistic);
        result[3 * columns + 1] = FormulaValueModel.CreateNumber(
            regression.Degrees);
        result[4 * columns] = FormulaValueModel.CreateNumber(
            regression.RegressionSum);
        result[4 * columns + 1] = FormulaValueModel.CreateNumber(
            regression.ResidualSum);
        return FormulaValueModel.CreateArray(result, 5, columns);
    }

    static FormulaRegressionData CreateRegressionData(
        OdfFormulaResult knownY,
        OdfFormulaResult? knownX)
    {
        FormulaRegressionData result = new FormulaRegressionData();
        FormulaStatisticalSequence y = ReadStrictSequence(knownY);
        if (y.Error != null)
        {
            result.Error = y.Error;
            return result;
        }
        result.Y = y.Values;
        result.OutputRows = knownY.RowCount;
        result.OutputColumns = knownY.ColumnCount;
        if (knownX == null)
        {
            result.PredictorCount = 1;
            result.Predictors = new double[result.Y.Length, 1];
            int coordinate = 0;
            while (coordinate < result.Y.Length)
            {
                result.Predictors[coordinate, 0] = coordinate + 1;
                coordinate = coordinate + 1;
            }
            return result;
        }
        FormulaStatisticalSequence x = ReadStrictSequence(knownX);
        if (x.Error != null)
        {
            result.Error = x.Error;
            return result;
        }
        if (knownY.RowCount == knownX.RowCount
            && knownY.ColumnCount == knownX.ColumnCount)
        {
            result.PredictorCount = 1;
            result.Predictors = new double[result.Y.Length, 1];
            int item = 0;
            while (item < result.Y.Length)
            {
                result.Predictors[item, 0] = x.Values[item];
                item = item + 1;
            }
            return result;
        }
        if (knownY.ColumnCount == 1
            && knownY.RowCount == knownX.RowCount)
        {
            result.PredictorCount = knownX.ColumnCount;
            result.Orientation = 1;
            result.Predictors = new double[
                result.Y.Length,
                result.PredictorCount];
            int row = 0;
            while (row < result.Y.Length)
            {
                int column = 0;
                while (column < result.PredictorCount)
                {
                    result.Predictors[row, column] =
                        x.Values[row * result.PredictorCount + column];
                    column = column + 1;
                }
                row = row + 1;
            }
            return result;
        }
        if (knownY.RowCount == 1
            && knownY.ColumnCount == knownX.ColumnCount)
        {
            result.PredictorCount = knownX.RowCount;
            result.Orientation = 2;
            result.Predictors = new double[
                result.Y.Length,
                result.PredictorCount];
            int observation = 0;
            while (observation < result.Y.Length)
            {
                int predictor = 0;
                while (predictor < result.PredictorCount)
                {
                    result.Predictors[observation, predictor] =
                        x.Values[predictor * knownX.ColumnCount + observation];
                    predictor = predictor + 1;
                }
                observation = observation + 1;
            }
            return result;
        }
        result.Error = FormulaValueModel.CreateError(OdfFormulaError.Value);
        return result;
    }

    static FormulaRegressionData CreatePredictionData(
        FormulaRegressionData known,
        OdfFormulaResult? requested)
    {
        FormulaRegressionData result = new FormulaRegressionData();
        result.PredictorCount = known.PredictorCount;
        result.Orientation = known.Orientation;
        if (requested == null)
        {
            result.Y = new double[known.Y.Length];
            result.Predictors = known.Predictors;
            result.OutputRows = known.OutputRows;
            result.OutputColumns = known.OutputColumns;
            return result;
        }
        FormulaStatisticalSequence sequence = ReadStrictSequence(requested);
        if (sequence.Error != null)
        {
            result.Error = sequence.Error;
            return result;
        }
        if (known.PredictorCount == 1)
        {
            result.Y = new double[sequence.Values.Length];
            result.Predictors = new double[sequence.Values.Length, 1];
            int item = 0;
            while (item < sequence.Values.Length)
            {
                result.Predictors[item, 0] = sequence.Values[item];
                item = item + 1;
            }
            result.OutputRows = requested.RowCount;
            result.OutputColumns = requested.ColumnCount;
            return result;
        }
        if (known.Orientation == 1
            && requested.ColumnCount == known.PredictorCount)
        {
            result.Y = new double[requested.RowCount];
            result.Predictors = new double[
                requested.RowCount,
                known.PredictorCount];
            int row = 0;
            while (row < requested.RowCount)
            {
                int column = 0;
                while (column < known.PredictorCount)
                {
                    result.Predictors[row, column] =
                        sequence.Values[row * known.PredictorCount + column];
                    column = column + 1;
                }
                row = row + 1;
            }
            result.OutputRows = requested.RowCount;
            result.OutputColumns = 1;
            return result;
        }
        if (known.Orientation == 2
            && requested.RowCount == known.PredictorCount)
        {
            result.Y = new double[requested.ColumnCount];
            result.Predictors = new double[
                requested.ColumnCount,
                known.PredictorCount];
            int observation = 0;
            while (observation < requested.ColumnCount)
            {
                int predictor = 0;
                while (predictor < known.PredictorCount)
                {
                    result.Predictors[observation, predictor] = sequence.Values[
                        predictor * requested.ColumnCount + observation];
                    predictor = predictor + 1;
                }
                observation = observation + 1;
            }
            result.OutputRows = 1;
            result.OutputColumns = requested.ColumnCount;
            return result;
        }
        result.Error = FormulaValueModel.CreateError(OdfFormulaError.Value);
        return result;
    }

    static FormulaMultipleRegressionValues FitMultipleRegression(
        FormulaRegressionData data,
        bool constant)
    {
        FormulaMultipleRegressionValues result =
            new FormulaMultipleRegressionValues();
        int parameterCount = data.PredictorCount + (constant ? 1 : 0);
        if (data.Y.Length <= parameterCount)
        {
            result.Error = FormulaValueModel.CreateError(OdfFormulaError.Value);
            return result;
        }
        double[,] augmented = new double[
            parameterCount,
            parameterCount * 2 + 1];
        int row = 0;
        while (row < parameterCount)
        {
            int column = 0;
            while (column < parameterCount)
            {
                double total = 0;
                int observation = 0;
                while (observation < data.Y.Length)
                {
                    total = total
                        + DesignValue(data, observation, row, constant)
                            * DesignValue(data, observation, column, constant);
                    observation = observation + 1;
                }
                augmented[row, column] = total;
                column = column + 1;
            }
            augmented[row, parameterCount + row] = 1;
            double response = 0;
            int responseIndex = 0;
            while (responseIndex < data.Y.Length)
            {
                response = response
                    + DesignValue(data, responseIndex, row, constant)
                        * data.Y[responseIndex];
                responseIndex = responseIndex + 1;
            }
            augmented[row, parameterCount * 2] = response;
            row = row + 1;
        }
        int pivot = 0;
        while (pivot < parameterCount)
        {
            int selected = pivot;
            int candidate = pivot + 1;
            while (candidate < parameterCount)
            {
                if (global::System.Math.Abs(augmented[candidate, pivot])
                    > global::System.Math.Abs(augmented[selected, pivot]))
                {
                    selected = candidate;
                }
                candidate = candidate + 1;
            }
            if (global::System.Math.Abs(augmented[selected, pivot]) < 1E-12)
            {
                result.Error = FormulaValueModel.CreateError(
                    OdfFormulaError.Number);
                return result;
            }
            if (selected != pivot)
            {
                int swapColumn = 0;
                while (swapColumn < parameterCount * 2 + 1)
                {
                    double swap = augmented[pivot, swapColumn];
                    augmented[pivot, swapColumn] =
                        augmented[selected, swapColumn];
                    augmented[selected, swapColumn] = swap;
                    swapColumn = swapColumn + 1;
                }
            }
            double divisor = augmented[pivot, pivot];
            int normalize = 0;
            while (normalize < parameterCount * 2 + 1)
            {
                augmented[pivot, normalize] =
                    augmented[pivot, normalize] / divisor;
                normalize = normalize + 1;
            }
            int eliminate = 0;
            while (eliminate < parameterCount)
            {
                if (eliminate != pivot)
                {
                    double factor = augmented[eliminate, pivot];
                    int eliminateColumn = 0;
                    while (eliminateColumn < parameterCount * 2 + 1)
                    {
                        augmented[eliminate, eliminateColumn] =
                            augmented[eliminate, eliminateColumn]
                            - factor * augmented[pivot, eliminateColumn];
                        eliminateColumn = eliminateColumn + 1;
                    }
                }
                eliminate = eliminate + 1;
            }
            pivot = pivot + 1;
        }
        result.Coefficients = new double[data.PredictorCount];
        int coefficient = 0;
        while (coefficient < data.PredictorCount)
        {
            result.Coefficients[coefficient] =
                augmented[coefficient, parameterCount * 2];
            coefficient = coefficient + 1;
        }
        result.Intercept = constant
            ? augmented[parameterCount - 1, parameterCount * 2]
            : 0;
        double mean = constant ? MeanValue(data.Y) : 0;
        double totalSum = 0;
        double residualSum = 0;
        int prediction = 0;
        while (prediction < data.Y.Length)
        {
            double predicted = result.Intercept;
            coefficient = 0;
            while (coefficient < data.PredictorCount)
            {
                predicted = predicted
                    + result.Coefficients[coefficient]
                        * data.Predictors[prediction, coefficient];
                coefficient = coefficient + 1;
            }
            double residual = data.Y[prediction] - predicted;
            double centered = data.Y[prediction] - mean;
            residualSum = residualSum + residual * residual;
            totalSum = totalSum + centered * centered;
            prediction = prediction + 1;
        }
        result.ResidualSum = residualSum;
        result.RegressionSum = global::System.Math.Max(0, totalSum - residualSum);
        result.RSquared = totalSum == 0
            ? 1
            : 1 - residualSum / totalSum;
        result.Degrees = data.Y.Length - parameterCount;
        result.StandardError = global::System.Math.Sqrt(
            residualSum / result.Degrees);
        result.CoefficientErrors = new double[data.PredictorCount];
        double errorVariance = residualSum / result.Degrees;
        coefficient = 0;
        while (coefficient < data.PredictorCount)
        {
            result.CoefficientErrors[coefficient] =
                global::System.Math.Sqrt(global::System.Math.Max(
                    0,
                    augmented[coefficient, parameterCount + coefficient]
                        * errorVariance));
            coefficient = coefficient + 1;
        }
        result.InterceptError = constant
            ? global::System.Math.Sqrt(global::System.Math.Max(
                0,
                augmented[parameterCount - 1, parameterCount * 2 - 1]
                    * errorVariance))
            : 0;
        result.FStatistic = residualSum == 0
            ? double.PositiveInfinity
            : result.RegressionSum / data.PredictorCount
                / (residualSum / result.Degrees);
        return result;
    }

    static double DesignValue(
        FormulaRegressionData data,
        int observation,
        int parameter,
        bool constant)
    {
        if (constant && parameter == data.PredictorCount) return 1;
        return data.Predictors[observation, parameter];
    }

    static FormulaStatisticalSequence ReadStrictSequence(
        OdfFormulaResult source)
    {
        FormulaStatisticalSequence result = new FormulaStatisticalSequence();
        if ((source.Kind != OdfValueKind.Array
            && source.Kind != OdfValueKind.Reference)
            || source.Values.Length == 0)
        {
            result.Error = FormulaValueModel.CreateError(OdfFormulaError.Value);
            return result;
        }
        result.Values = new double[source.Values.Length];
        int index = 0;
        while (index < source.Values.Length)
        {
            OdfFormulaResult value = Number(source.Values[index]);
            if (value.Error != OdfFormulaError.None)
            {
                result.Error = value;
                return result;
            }
            result.Values[index] = value.NumericValue;
            index = index + 1;
        }
        return result;
    }

    static FormulaRegressionValues FitLine(
        double[] y,
        double[] x,
        bool constant)
    {
        FormulaRegressionValues result = new FormulaRegressionValues();
        if (x.Length != y.Length || x.Length < 2)
        {
            result.Error = FormulaValueModel.CreateError(OdfFormulaError.Value);
            return result;
        }
        double xMean = constant ? MeanValue(x) : 0;
        double yMean = constant ? MeanValue(y) : 0;
        double cross = 0;
        double xSquare = 0;
        double ySquare = 0;
        int index = 0;
        while (index < x.Length)
        {
            double xDifference = x[index] - xMean;
            double yDifference = y[index] - yMean;
            cross = cross + xDifference * yDifference;
            xSquare = xSquare + xDifference * xDifference;
            ySquare = ySquare + yDifference * yDifference;
            index = index + 1;
        }
        if (xSquare == 0)
        {
            result.Error = FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
            return result;
        }
        result.Slope = cross / xSquare;
        result.Intercept = constant ? yMean - result.Slope * xMean : 0;
        result.Correlation = ySquare == 0
            ? 0
            : cross / global::System.Math.Sqrt(xSquare * ySquare);
        double residual = 0;
        double regression = 0;
        index = 0;
        while (index < x.Length)
        {
            double predicted = result.Intercept + result.Slope * x[index];
            double error = y[index] - predicted;
            double explained = predicted - MeanValue(y);
            residual = residual + error * error;
            regression = regression + explained * explained;
            index = index + 1;
        }
        result.Count = x.Length;
        result.ResidualSum = residual;
        result.RegressionSum = regression;
        result.StandardError = x.Length > 2
            ? global::System.Math.Sqrt(residual / (x.Length - 2))
            : 0;
        return result;
    }

    static OdfFormulaResult ContinuousDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.GAUSS || name == OdfFormulaNames.PHI)
        {
            OdfFormulaResult value = Number(arguments[0]);
            if (value.Error != OdfFormulaError.None) return value;
            return FormulaValueModel.CreateNumber(
                name == OdfFormulaNames.GAUSS
                    ? NormalCdf(value.NumericValue) - 0.5
                    : NormalDensity(value.NumericValue));
        }
        if (name == OdfFormulaNames.NORMDIST
            || name == OdfFormulaNames.NORMINV)
        {
            return NormalDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.LOGNORMDIST
            || name == OdfFormulaNames.LOGINV)
        {
            return LogNormalDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.BETADIST
            || name == OdfFormulaNames.BETAINV)
        {
            return BetaDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.GAMMADIST
            || name == OdfFormulaNames.GAMMAINV)
        {
            return GammaDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.CHISQDIST
            || name == OdfFormulaNames.CHISQINV)
        {
            return ChiSquareDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.FDIST || name == OdfFormulaNames.FINV)
        {
            return FDistribution(name, arguments);
        }
        if (name == OdfFormulaNames.EXPONDIST)
        {
            return ExponentialDistribution(arguments);
        }
        if (name == OdfFormulaNames.WEIBULL)
        {
            return WeibullDistribution(arguments);
        }
        return StudentInverse(arguments);
    }

    static OdfFormulaResult NormalDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult mean = Number(arguments[1]);
        OdfFormulaResult deviation = Number(arguments[2]);
        if (value.Error != OdfFormulaError.None) return value;
        if (mean.Error != OdfFormulaError.None) return mean;
        if (deviation.Error != OdfFormulaError.None) return deviation;
        if (deviation.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        if (name == OdfFormulaNames.NORMINV)
        {
            if (value.NumericValue <= 0 || value.NumericValue >= 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            return FormulaValueModel.CreateNumber(mean.NumericValue
                + deviation.NumericValue * NormalInverse(value.NumericValue));
        }
        OdfFormulaResult cumulative = arguments.Length > 3
            ? FormulaValueModel.ConvertToBoolean(arguments[3])
            : FormulaValueModel.CreateBoolean(true);
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        double z = (value.NumericValue - mean.NumericValue)
            / deviation.NumericValue;
        return FormulaValueModel.CreateNumber(cumulative.BooleanValue
            ? NormalCdf(z)
            : NormalDensity(z) / deviation.NumericValue);
    }

    static OdfFormulaResult LogNormalDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult mean = arguments.Length > 1
            ? Number(arguments[1])
            : FormulaValueModel.CreateNumber(0);
        OdfFormulaResult deviation = arguments.Length > 2
            ? Number(arguments[2])
            : FormulaValueModel.CreateNumber(1);
        if (value.Error != OdfFormulaError.None) return value;
        if (mean.Error != OdfFormulaError.None) return mean;
        if (deviation.Error != OdfFormulaError.None) return deviation;
        if (deviation.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        if (name == OdfFormulaNames.LOGINV)
        {
            if (value.NumericValue <= 0 || value.NumericValue >= 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            return FormulaValueModel.CreateNumber(global::System.Math.Exp(
                mean.NumericValue
                    + deviation.NumericValue * NormalInverse(value.NumericValue)));
        }
        OdfFormulaResult cumulative = arguments.Length > 3
            ? FormulaValueModel.ConvertToBoolean(arguments[3])
            : FormulaValueModel.CreateBoolean(true);
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        if (value.NumericValue <= 0)
            return FormulaValueModel.CreateNumber(0);
        double z = (global::System.Math.Log(value.NumericValue)
            - mean.NumericValue) / deviation.NumericValue;
        return FormulaValueModel.CreateNumber(cumulative.BooleanValue
            ? NormalCdf(z)
            : NormalDensity(z)
                / (value.NumericValue * deviation.NumericValue));
    }

    static OdfFormulaResult BetaDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult alpha = Number(arguments[1]);
        OdfFormulaResult beta = Number(arguments[2]);
        OdfFormulaResult lower = arguments.Length > 3
            ? Number(arguments[3])
            : FormulaValueModel.CreateNumber(0);
        OdfFormulaResult upper = arguments.Length > 4
            ? Number(arguments[4])
            : FormulaValueModel.CreateNumber(1);
        if (value.Error != OdfFormulaError.None) return value;
        if (alpha.Error != OdfFormulaError.None) return alpha;
        if (beta.Error != OdfFormulaError.None) return beta;
        if (lower.Error != OdfFormulaError.None) return lower;
        if (upper.Error != OdfFormulaError.None) return upper;
        if (alpha.NumericValue <= 0 || beta.NumericValue <= 0
            || upper.NumericValue <= lower.NumericValue)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        if (name == OdfFormulaNames.BETAINV)
        {
            if (value.NumericValue < 0 || value.NumericValue > 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            double inverse = InverseBeta(
                value.NumericValue,
                alpha.NumericValue,
                beta.NumericValue);
            return FormulaValueModel.CreateNumber(lower.NumericValue
                + inverse * (upper.NumericValue - lower.NumericValue));
        }
        OdfFormulaResult cumulative = arguments.Length > 5
            ? FormulaValueModel.ConvertToBoolean(arguments[5])
            : FormulaValueModel.CreateBoolean(true);
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        double scaled = (value.NumericValue - lower.NumericValue)
            / (upper.NumericValue - lower.NumericValue);
        if (scaled < 0)
            return FormulaValueModel.CreateNumber(0);
        if (scaled > 1)
            return FormulaValueModel.CreateNumber(
                cumulative.BooleanValue ? 1 : 0);
        double result = cumulative.BooleanValue
            ? RegularizedBeta(scaled, alpha.NumericValue, beta.NumericValue)
            : global::System.Math.Pow(scaled, alpha.NumericValue - 1)
                * global::System.Math.Pow(1 - scaled, beta.NumericValue - 1)
                / global::System.Math.Exp(LogBeta(
                    alpha.NumericValue,
                    beta.NumericValue))
                / (upper.NumericValue - lower.NumericValue);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult GammaDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult alpha = Number(arguments[1]);
        OdfFormulaResult scale = Number(arguments[2]);
        if (value.Error != OdfFormulaError.None) return value;
        if (alpha.Error != OdfFormulaError.None) return alpha;
        if (scale.Error != OdfFormulaError.None) return scale;
        if (alpha.NumericValue <= 0 || scale.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        if (name == OdfFormulaNames.GAMMAINV)
        {
            if (value.NumericValue < 0 || value.NumericValue >= 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            return FormulaValueModel.CreateNumber(scale.NumericValue
                * InverseGamma(value.NumericValue, alpha.NumericValue));
        }
        OdfFormulaResult cumulative = arguments.Length > 3
            ? FormulaValueModel.ConvertToBoolean(arguments[3])
            : FormulaValueModel.CreateBoolean(true);
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        if (value.NumericValue < 0)
            return FormulaValueModel.CreateNumber(0);
        if (value.NumericValue == 0)
        {
            if (cumulative.BooleanValue || alpha.NumericValue > 1)
                return FormulaValueModel.CreateNumber(0);
            if (alpha.NumericValue == 1)
                return FormulaValueModel.CreateNumber(1 / scale.NumericValue);
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double scaled = value.NumericValue / scale.NumericValue;
        double result = cumulative.BooleanValue
            ? RegularizedGamma(alpha.NumericValue, scaled)
            : global::System.Math.Exp((alpha.NumericValue - 1)
                * global::System.Math.Log(scaled) - scaled
                - LogGamma(alpha.NumericValue)) / scale.NumericValue;
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult ChiSquareDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult degrees = Number(arguments[1]);
        if (value.Error != OdfFormulaError.None) return value;
        if (degrees.Error != OdfFormulaError.None) return degrees;
        if (degrees.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double shape = degrees.NumericValue / 2;
        if (name == OdfFormulaNames.CHISQINV)
        {
            if (value.NumericValue <= 0 || value.NumericValue >= 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            return FormulaValueModel.CreateNumber(
                2 * InverseGamma(value.NumericValue, shape));
        }
        if (value.NumericValue <= 0)
            return FormulaValueModel.CreateNumber(0);
        OdfFormulaResult cumulative = arguments.Length > 2
            ? FormulaValueModel.ConvertToBoolean(arguments[2])
            : FormulaValueModel.CreateBoolean(true);
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        double result = cumulative.BooleanValue
            ? RegularizedGamma(shape, value.NumericValue / 2)
            : global::System.Math.Exp((shape - 1)
                * global::System.Math.Log(value.NumericValue / 2)
                - value.NumericValue / 2 - LogGamma(shape)) / 2;
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult FDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult firstDegrees = Number(arguments[1]);
        OdfFormulaResult secondDegrees = Number(arguments[2]);
        if (value.Error != OdfFormulaError.None) return value;
        if (firstDegrees.Error != OdfFormulaError.None) return firstDegrees;
        if (secondDegrees.Error != OdfFormulaError.None) return secondDegrees;
        double first = firstDegrees.NumericValue;
        double second = secondDegrees.NumericValue;
        if (first <= 0 || second <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        if (name == OdfFormulaNames.FINV)
        {
            if (value.NumericValue < 0 || value.NumericValue >= 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            return FormulaValueModel.CreateNumber(
                InverseF(value.NumericValue, first, second));
        }
        if (value.NumericValue < 0)
            return FormulaValueModel.CreateNumber(0);
        OdfFormulaResult cumulative = arguments.Length > 3
            ? FormulaValueModel.ConvertToBoolean(arguments[3])
            : FormulaValueModel.CreateBoolean(true);
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        double transformed = first * value.NumericValue
            / (first * value.NumericValue + second);
        double result = cumulative.BooleanValue
            ? RegularizedBeta(transformed, first / 2, second / 2)
            : global::System.Math.Exp(first / 2 * global::System.Math.Log(first / second)
                + (first / 2 - 1) * global::System.Math.Log(value.NumericValue)
                - (first + second) / 2
                    * global::System.Math.Log(1 + first * value.NumericValue / second)
                - LogBeta(first / 2, second / 2));
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult ExponentialDistribution(
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult rate = Number(arguments[1]);
        OdfFormulaResult cumulative = arguments.Length > 2
            ? FormulaValueModel.ConvertToBoolean(arguments[2])
            : FormulaValueModel.CreateBoolean(true);
        if (value.Error != OdfFormulaError.None) return value;
        if (rate.Error != OdfFormulaError.None) return rate;
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        if (rate.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        if (value.NumericValue < 0)
            return FormulaValueModel.CreateNumber(0);
        double exponential = global::System.Math.Exp(
            -rate.NumericValue * value.NumericValue);
        return FormulaValueModel.CreateNumber(cumulative.BooleanValue
            ? 1 - exponential
            : rate.NumericValue * exponential);
    }

    static OdfFormulaResult WeibullDistribution(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult shape = Number(arguments[1]);
        OdfFormulaResult scale = Number(arguments[2]);
        OdfFormulaResult cumulative =
            FormulaValueModel.ConvertToBoolean(arguments[3]);
        if (value.Error != OdfFormulaError.None) return value;
        if (shape.Error != OdfFormulaError.None) return shape;
        if (scale.Error != OdfFormulaError.None) return scale;
        if (cumulative.Error != OdfFormulaError.None) return cumulative;
        if (value.NumericValue < 0 || shape.NumericValue <= 0
            || scale.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double power = global::System.Math.Pow(
            value.NumericValue / scale.NumericValue,
            shape.NumericValue);
        double exponential = global::System.Math.Exp(-power);
        return FormulaValueModel.CreateNumber(cumulative.BooleanValue
            ? 1 - exponential
            : shape.NumericValue / scale.NumericValue
                * global::System.Math.Pow(
                    value.NumericValue / scale.NumericValue,
                    shape.NumericValue - 1) * exponential);
    }

    static OdfFormulaResult StudentInverse(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult probability = Number(arguments[0]);
        OdfFormulaResult degrees = Number(arguments[1]);
        if (probability.Error != OdfFormulaError.None) return probability;
        if (degrees.Error != OdfFormulaError.None) return degrees;
        if (probability.NumericValue <= 0 || probability.NumericValue > 1
            || degrees.NumericValue < 1)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double target = 1 - probability.NumericValue / 2;
        return FormulaValueModel.CreateNumber(
            InverseStudent(target, degrees.NumericValue));
    }

    static OdfFormulaResult DiscreteDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.PERMUT
            || name == OdfFormulaNames.PERMUTATIONA)
            return Permutation(name, arguments);
        if (name == OdfFormulaNames.PROB) return ProbabilityRange(arguments);
        if (name == OdfFormulaNames.HYPGEOMDIST) return Hypergeometric(arguments);
        if (name == OdfFormulaNames.POISSON) return Poisson(arguments);
        if (name == OdfFormulaNames.NEGBINOMDIST)
            return NegativeBinomial(arguments);
        return Binomial(name, arguments);
    }

    static OdfFormulaResult Binomial(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult first = Number(arguments[0]);
        OdfFormulaResult second = Number(arguments[1]);
        OdfFormulaResult third = Number(arguments[2]);
        if (first.Error != OdfFormulaError.None) return first;
        if (second.Error != OdfFormulaError.None) return second;
        if (third.Error != OdfFormulaError.None) return third;
        if (name == OdfFormulaNames.CRITBINOM)
        {
            int trials = (int)global::System.Math.Truncate(first.NumericValue);
            double probability = second.NumericValue;
            double alpha = third.NumericValue;
            if (trials < 0 || probability < 0 || probability > 1
                || alpha < 0 || alpha > 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            double cumulative = 0;
            int criticalIndex = 0;
            while (criticalIndex <= trials)
            {
                cumulative = cumulative
                    + BinomialProbability(trials, criticalIndex, probability);
                if (cumulative >= alpha)
                    return FormulaValueModel.CreateNumber(criticalIndex);
                criticalIndex = criticalIndex + 1;
            }
            return FormulaValueModel.CreateNumber(trials);
        }
        int successes;
        int total;
        double chance;
        int end;
        bool cumulativeResult;
        if (name == OdfFormulaNames.BINOM_DIST_RANGE)
        {
            total = (int)global::System.Math.Truncate(first.NumericValue);
            chance = second.NumericValue;
            successes = (int)global::System.Math.Truncate(third.NumericValue);
            OdfFormulaResult endValue = arguments.Length > 3
                ? Number(arguments[3])
                : FormulaValueModel.CreateNumber(successes);
            if (endValue.Error != OdfFormulaError.None) return endValue;
            end = (int)global::System.Math.Truncate(endValue.NumericValue);
            cumulativeResult = true;
        }
        else
        {
            successes = (int)global::System.Math.Truncate(first.NumericValue);
            total = (int)global::System.Math.Truncate(second.NumericValue);
            chance = third.NumericValue;
            OdfFormulaResult cumulative =
                FormulaValueModel.ConvertToBoolean(arguments[3]);
            if (cumulative.Error != OdfFormulaError.None) return cumulative;
            cumulativeResult = cumulative.BooleanValue;
            end = cumulativeResult ? successes : successes;
        }
        if (total < 0 || successes < 0 || end < successes || end > total
            || chance < 0 || chance > 1)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        int start = name == OdfFormulaNames.BINOMDIST && cumulativeResult
            ? 0
            : successes;
        double result = 0;
        int index = start;
        while (index <= end)
        {
            result = result + BinomialProbability(total, index, chance);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult Hypergeometric(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult xValue = Number(arguments[0]);
        OdfFormulaResult sampleValue = Number(arguments[1]);
        OdfFormulaResult successesValue = Number(arguments[2]);
        OdfFormulaResult populationValue = Number(arguments[3]);
        if (xValue.Error != OdfFormulaError.None) return xValue;
        if (sampleValue.Error != OdfFormulaError.None) return sampleValue;
        if (successesValue.Error != OdfFormulaError.None) return successesValue;
        if (populationValue.Error != OdfFormulaError.None) return populationValue;
        int x = (int)global::System.Math.Truncate(xValue.NumericValue);
        int sample = (int)global::System.Math.Truncate(sampleValue.NumericValue);
        int successes = (int)global::System.Math.Truncate(successesValue.NumericValue);
        int population = (int)global::System.Math.Truncate(populationValue.NumericValue);
        if (x < 0 || sample < 0 || successes < 0 || population <= 0
            || sample > population || successes > population)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        bool cumulative = false;
        if (arguments.Length > 4)
        {
            OdfFormulaResult cumulativeValue =
                FormulaValueModel.ConvertToBoolean(arguments[4]);
            if (cumulativeValue.Error != OdfFormulaError.None)
                return cumulativeValue;
            cumulative = cumulativeValue.BooleanValue;
        }
        int start = cumulative ? global::System.Math.Max(
            0,
            sample - (population - successes)) : x;
        double result = 0;
        int index = start;
        while (index <= x)
        {
            if (index <= successes && sample - index <= population - successes)
            {
                result = result + CombinationValue(successes, index)
                    * CombinationValue(population - successes, sample - index)
                    / CombinationValue(population, sample);
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult Poisson(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult xValue = Number(arguments[0]);
        OdfFormulaResult meanValue = Number(arguments[1]);
        if (xValue.Error != OdfFormulaError.None) return xValue;
        if (meanValue.Error != OdfFormulaError.None) return meanValue;
        int x = (int)global::System.Math.Truncate(xValue.NumericValue);
        double mean = meanValue.NumericValue;
        if (x < 0 || mean < 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        bool cumulative = true;
        if (arguments.Length > 2)
        {
            OdfFormulaResult cumulativeValue =
                FormulaValueModel.ConvertToBoolean(arguments[2]);
            if (cumulativeValue.Error != OdfFormulaError.None)
                return cumulativeValue;
            cumulative = cumulativeValue.BooleanValue;
        }
        int start = cumulative ? 0 : x;
        double result = 0;
        int index = start;
        while (index <= x)
        {
            result = result + global::System.Math.Exp(-mean)
                * global::System.Math.Pow(mean, index) / FactorialValue(index);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult NegativeBinomial(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult failuresValue = Number(arguments[0]);
        OdfFormulaResult successesValue = Number(arguments[1]);
        OdfFormulaResult probabilityValue = Number(arguments[2]);
        if (failuresValue.Error != OdfFormulaError.None) return failuresValue;
        if (successesValue.Error != OdfFormulaError.None) return successesValue;
        if (probabilityValue.Error != OdfFormulaError.None) return probabilityValue;
        int failures = (int)global::System.Math.Truncate(
            failuresValue.NumericValue);
        int successes = (int)global::System.Math.Truncate(
            successesValue.NumericValue);
        double probability = probabilityValue.NumericValue;
        if (failures < 0 || successes < 1
            || probability < 0 || probability > 1)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        return FormulaValueModel.CreateNumber(
            CombinationValue(failures + successes - 1, failures)
                * global::System.Math.Pow(probability, successes)
                * global::System.Math.Pow(1 - probability, failures));
    }

    static OdfFormulaResult ProbabilityRange(OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence data = ReadSequence([arguments[0]], false);
        FormulaStatisticalSequence probabilities = ReadSequence(
            [arguments[1]],
            false);
        OdfFormulaResult lower = Number(arguments[2]);
        OdfFormulaResult upper = arguments.Length > 3
            ? Number(arguments[3])
            : lower;
        if (data.Error != null) return data.Error;
        if (probabilities.Error != null) return probabilities.Error;
        if (lower.Error != OdfFormulaError.None) return lower;
        if (upper.Error != OdfFormulaError.None) return upper;
        if (data.Values.Length != probabilities.Values.Length)
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        double probabilityTotal = TotalValue(probabilities.Values);
        if (global::System.Math.Abs(probabilityTotal - 1) > 0.0000001)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double result = 0;
        int index = 0;
        while (index < data.Values.Length)
        {
            if (probabilities.Values[index] < 0
                || probabilities.Values[index] > 1)
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            if (data.Values[index] >= lower.NumericValue
                && data.Values[index] <= upper.NumericValue)
                result = result + probabilities.Values[index];
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult Permutation(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult totalValue = Number(arguments[0]);
        OdfFormulaResult chosenValue = Number(arguments[1]);
        if (totalValue.Error != OdfFormulaError.None) return totalValue;
        if (chosenValue.Error != OdfFormulaError.None) return chosenValue;
        int total = (int)global::System.Math.Truncate(totalValue.NumericValue);
        int chosen = (int)global::System.Math.Truncate(chosenValue.NumericValue);
        if (total < 0 || chosen < 0
            || (name == OdfFormulaNames.PERMUT && chosen > total))
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double result = name == OdfFormulaNames.PERMUTATIONA
            ? global::System.Math.Pow(total, chosen)
            : FactorialValue(total) / FactorialValue(total - chosen);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult StatisticalTest(
        string name,
        OdfFormulaResult[] arguments)
    {
        if (name == OdfFormulaNames.CONFIDENCE)
            return Confidence(arguments);
        if (name == OdfFormulaNames.STANDARDIZE)
            return Standardize(arguments);
        if (name == OdfFormulaNames.ZTEST) return ZTest(arguments);
        if (name == OdfFormulaNames.FTEST) return FTest(arguments);
        return TTest(arguments);
    }

    static OdfFormulaResult Confidence(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult alpha = Number(arguments[0]);
        OdfFormulaResult deviation = Number(arguments[1]);
        OdfFormulaResult sizeValue = Number(arguments[2]);
        if (alpha.Error != OdfFormulaError.None) return alpha;
        if (deviation.Error != OdfFormulaError.None) return deviation;
        if (sizeValue.Error != OdfFormulaError.None) return sizeValue;
        double size = global::System.Math.Truncate(sizeValue.NumericValue);
        if (alpha.NumericValue <= 0 || alpha.NumericValue >= 1
            || deviation.NumericValue <= 0 || size < 1)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        return FormulaValueModel.CreateNumber(
            -NormalInverse(alpha.NumericValue / 2)
                * deviation.NumericValue / global::System.Math.Sqrt(size));
    }

    static OdfFormulaResult Standardize(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult mean = Number(arguments[1]);
        OdfFormulaResult deviation = Number(arguments[2]);
        if (value.Error != OdfFormulaError.None) return value;
        if (mean.Error != OdfFormulaError.None) return mean;
        if (deviation.Error != OdfFormulaError.None) return deviation;
        if (deviation.NumericValue <= 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        return FormulaValueModel.CreateNumber(
            (value.NumericValue - mean.NumericValue) / deviation.NumericValue);
    }

    static OdfFormulaResult ZTest(OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence sample = ReadSequence([arguments[0]], false);
        OdfFormulaResult mean = Number(arguments[1]);
        if (sample.Error != null) return sample.Error;
        if (mean.Error != OdfFormulaError.None) return mean;
        double deviation;
        if (arguments.Length > 2)
        {
            OdfFormulaResult supplied = Number(arguments[2]);
            if (supplied.Error != OdfFormulaError.None) return supplied;
            deviation = supplied.NumericValue;
        }
        else
        {
            deviation = global::System.Math.Sqrt(
                VarianceValue(sample.Values, true));
        }
        if (deviation <= 0 || sample.Values.Length == 0)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double z = (MeanValue(sample.Values) - mean.NumericValue)
            / (deviation / global::System.Math.Sqrt(sample.Values.Length));
        return FormulaValueModel.CreateNumber(1 - NormalCdf(z));
    }

    static OdfFormulaResult FTest(OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence first = ReadSequence([arguments[0]], false);
        FormulaStatisticalSequence second = ReadSequence([arguments[1]], false);
        if (first.Error != null) return first.Error;
        if (second.Error != null) return second.Error;
        if (first.Values.Length < 2 || second.Values.Length < 2)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double ratio = VarianceValue(first.Values, true)
            / VarianceValue(second.Values, true);
        double cdf = FCdf(
            ratio,
            first.Values.Length - 1,
            second.Values.Length - 1);
        return FormulaValueModel.CreateNumber(
            2 * global::System.Math.Min(cdf, 1 - cdf));
    }

    static OdfFormulaResult TTest(OdfFormulaResult[] arguments)
    {
        FormulaStatisticalSequence first = ReadSequence([arguments[0]], false);
        FormulaStatisticalSequence second = ReadSequence([arguments[1]], false);
        OdfFormulaResult tailsValue = Number(arguments[2]);
        OdfFormulaResult typeValue = Number(arguments[3]);
        if (first.Error != null) return first.Error;
        if (second.Error != null) return second.Error;
        if (tailsValue.Error != OdfFormulaError.None) return tailsValue;
        if (typeValue.Error != OdfFormulaError.None) return typeValue;
        int tails = (int)global::System.Math.Truncate(tailsValue.NumericValue);
        int type = (int)global::System.Math.Truncate(typeValue.NumericValue);
        if ((tails != 1 && tails != 2) || type < 1 || type > 3)
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        double t;
        double degrees;
        if (type == 1)
        {
            if (first.Values.Length != second.Values.Length)
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            double[] differences = new double[first.Values.Length];
            int index = 0;
            while (index < differences.Length)
            {
                differences[index] = first.Values[index] - second.Values[index];
                index = index + 1;
            }
            double deviation = global::System.Math.Sqrt(
                VarianceValue(differences, true));
            t = MeanValue(differences)
                / (deviation / global::System.Math.Sqrt(differences.Length));
            degrees = differences.Length - 1;
        }
        else
        {
            double firstVariance = VarianceValue(first.Values, true);
            double secondVariance = VarianceValue(second.Values, true);
            double difference = MeanValue(first.Values) - MeanValue(second.Values);
            if (type == 2)
            {
                degrees = first.Values.Length + second.Values.Length - 2;
                double pooled = ((first.Values.Length - 1) * firstVariance
                    + (second.Values.Length - 1) * secondVariance) / degrees;
                t = difference / global::System.Math.Sqrt(pooled
                    * (1.0 / first.Values.Length + 1.0 / second.Values.Length));
            }
            else
            {
                double firstTerm = firstVariance / first.Values.Length;
                double secondTerm = secondVariance / second.Values.Length;
                t = difference / global::System.Math.Sqrt(firstTerm + secondTerm);
                degrees = (firstTerm + secondTerm) * (firstTerm + secondTerm)
                    / (firstTerm * firstTerm / (first.Values.Length - 1)
                        + secondTerm * secondTerm / (second.Values.Length - 1));
            }
        }
        double oneTail = 1 - StudentCdf(global::System.Math.Abs(t), degrees);
        return FormulaValueModel.CreateNumber(tails * oneTail);
    }

    static OdfFormulaResult Fisher(string name, OdfFormulaResult argument)
    {
        OdfFormulaResult value = Number(argument);
        if (value.Error != OdfFormulaError.None) return value;
        double result = name == OdfFormulaNames.FISHER
            ? 0.5 * global::System.Math.Log(
                (1 + value.NumericValue) / (1 - value.NumericValue))
            : global::System.Math.Tanh(value.NumericValue);
        return FormulaValueModel.CreateNumber(result);
    }

    static OdfFormulaResult LegacyChiDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult degrees = Number(arguments[1]);
        if (value.Error != OdfFormulaError.None)
        {
            return value;
        }
        if (degrees.Error != OdfFormulaError.None)
        {
            return degrees;
        }
        if (!IsPositiveInteger(degrees.NumericValue))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        double shape = degrees.NumericValue / 2;
        if (name == OdfFormulaNames.LEGACY_CHIINV)
        {
            if (value.NumericValue <= 0 || value.NumericValue > 1)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            return FormulaValueModel.CreateNumber(
                2 * InverseGamma(1 - value.NumericValue, shape));
        }
        if (value.NumericValue <= 0)
        {
            return FormulaValueModel.CreateNumber(1);
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Max(0,
                1 - RegularizedGamma(shape, value.NumericValue / 2)));
    }

    static OdfFormulaResult LegacyChiTest(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult actual = arguments[0];
        OdfFormulaResult expected = arguments[1];
        if (actual.Error != OdfFormulaError.None)
        {
            return actual;
        }
        if (expected.Error != OdfFormulaError.None)
        {
            return expected;
        }
        if (!MatchingShapes(actual, expected)
            || actual.Values.Length <= 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        double statistic = 0;
        int index = 0;
        while (index < actual.Values.Length)
        {
            OdfFormulaResult observed = actual.Values[index];
            OdfFormulaResult predicted = expected.Values[index];
            if (observed.Error != OdfFormulaError.None)
            {
                return observed;
            }
            if (predicted.Error != OdfFormulaError.None)
            {
                return predicted;
            }
            if (IsNumeric(observed.Kind) && IsNumeric(predicted.Kind))
            {
                if (predicted.NumericValue == 0)
                {
                    return FormulaValueModel.CreateError(
                        OdfFormulaError.DivisionByZero);
                }
                double difference = observed.NumericValue
                    - predicted.NumericValue;
                statistic = statistic + difference * difference
                    / predicted.NumericValue;
            }
            index = index + 1;
        }
        int degrees = actual.RowCount > 1 && actual.ColumnCount > 1
            ? (actual.RowCount - 1) * (actual.ColumnCount - 1)
            : actual.RowCount * actual.ColumnCount - 1;
        return FormulaValueModel.CreateNumber(
            global::System.Math.Max(0,
                1 - RegularizedGamma(degrees / 2.0, statistic / 2)));
    }

    static OdfFormulaResult LegacyFDistribution(
        string name,
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult firstDegrees = Number(arguments[1]);
        OdfFormulaResult secondDegrees = Number(arguments[2]);
        if (value.Error != OdfFormulaError.None)
        {
            return value;
        }
        if (firstDegrees.Error != OdfFormulaError.None)
        {
            return firstDegrees;
        }
        if (secondDegrees.Error != OdfFormulaError.None)
        {
            return secondDegrees;
        }
        if (!IsPositiveInteger(firstDegrees.NumericValue)
            || !IsPositiveInteger(secondDegrees.NumericValue))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        if (name == OdfFormulaNames.LEGACY_FINV)
        {
            if (value.NumericValue <= 0 || value.NumericValue > 1)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            return FormulaValueModel.CreateNumber(InverseF(
                1 - value.NumericValue,
                firstDegrees.NumericValue,
                secondDegrees.NumericValue));
        }
        if (value.NumericValue < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Max(0, 1 - FCdf(
                value.NumericValue,
                firstDegrees.NumericValue,
                secondDegrees.NumericValue)));
    }

    static OdfFormulaResult LegacyNormalDistribution(
        string name,
        OdfFormulaResult argument)
    {
        OdfFormulaResult value = Number(argument);
        if (value.Error != OdfFormulaError.None)
        {
            return value;
        }
        if (name == OdfFormulaNames.LEGACY_NORMSDIST)
        {
            return FormulaValueModel.CreateNumber(NormalCdf(value.NumericValue));
        }
        if (value.NumericValue <= 0 || value.NumericValue >= 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(
            NormalInverse(value.NumericValue));
    }

    static OdfFormulaResult LegacyTDistribution(
        OdfFormulaResult[] arguments)
    {
        OdfFormulaResult value = Number(arguments[0]);
        OdfFormulaResult degrees = Number(arguments[1]);
        OdfFormulaResult tails = Number(arguments[2]);
        if (value.Error != OdfFormulaError.None)
        {
            return value;
        }
        if (degrees.Error != OdfFormulaError.None)
        {
            return degrees;
        }
        if (tails.Error != OdfFormulaError.None)
        {
            return tails;
        }
        if (value.NumericValue < 0
            || !IsPositiveInteger(degrees.NumericValue)
            || (tails.NumericValue != 1 && tails.NumericValue != 2))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        return FormulaValueModel.CreateNumber(tails.NumericValue
            * (1 - StudentCdf(value.NumericValue, degrees.NumericValue)));
    }

    static bool IsPositiveInteger(double value)
    {
        return value >= 1 && value == global::System.Math.Truncate(value);
    }

    static FormulaStatisticalSequence ReadSequence(
        OdfFormulaResult[] arguments,
        bool includeAll)
    {
        FormulaStatisticalSequence result = new FormulaStatisticalSequence();
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
                    AddSequenceValue(values, current.Values[index], includeAll);
                    if (current.Values[index].Error != OdfFormulaError.None)
                    {
                        result.Error = current.Values[index];
                        return result;
                    }
                    index = index + 1;
                }
            }
            else if (includeAll)
            {
                AddSequenceValue(values, current, true);
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

    static void AddSequenceValue(
        List<double> values,
        OdfFormulaResult value,
        bool includeAll)
    {
        if (IsNumeric(value.Kind))
        {
            values.Add(value.NumericValue);
        }
        else if (includeAll)
        {
            values.Add(value.Kind == OdfValueKind.Boolean
                && value.BooleanValue ? 1 : 0);
        }
    }

    static FormulaStatisticalSequence ReadPair(
        OdfFormulaResult first,
        OdfFormulaResult second)
    {
        FormulaStatisticalSequence result = ReadSequence([first], false);
        if (result.Error != null) return result;
        FormulaStatisticalSequence other = ReadSequence([second], false);
        if (other.Error != null) return other;
        if (result.Values.Length != other.Values.Length)
        {
            result.Error = FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return result;
    }

    static bool MatchingShapes(
        OdfFormulaResult first,
        OdfFormulaResult second)
    {
        bool firstCollection = first.Kind == OdfValueKind.Array
            || first.Kind == OdfValueKind.Reference;
        bool secondCollection = second.Kind == OdfValueKind.Array
            || second.Kind == OdfValueKind.Reference;
        return firstCollection
            && secondCollection
            && first.RowCount == second.RowCount
            && first.ColumnCount == second.ColumnCount
            && first.Values.Length == second.Values.Length;
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

    static double MeanValue(double[] values)
    {
        return TotalValue(values) / values.Length;
    }

    static double TotalValue(double[] values)
    {
        double result = 0;
        int index = 0;
        while (index < values.Length)
        {
            result = result + values[index];
            index = index + 1;
        }
        return result;
    }

    static double MinimumValue(double[] values)
    {
        double result = values[0];
        int index = 1;
        while (index < values.Length)
        {
            result = global::System.Math.Min(result, values[index]);
            index = index + 1;
        }
        return result;
    }

    static double MaximumValue(double[] values)
    {
        double result = values[0];
        int index = 1;
        while (index < values.Length)
        {
            result = global::System.Math.Max(result, values[index]);
            index = index + 1;
        }
        return result;
    }

    static double DeviationTotal(double[] values)
    {
        double mean = MeanValue(values);
        double result = 0;
        int index = 0;
        while (index < values.Length)
        {
            double difference = values[index] - mean;
            result = result + difference * difference;
            index = index + 1;
        }
        return result;
    }

    static double VarianceValue(double[] values, bool sample)
    {
        if (values.Length == 0 || (sample && values.Length < 2))
            return double.NaN;
        return DeviationTotal(values)
            / (sample ? values.Length - 1 : values.Length);
    }

    static double[] CopyAndSort(double[] source)
    {
        double[] result = CopyValues(source);
        global::System.Array.Sort(result);
        return result;
    }

    static double[] CopyValues(double[] source)
    {
        double[] result = new double[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    static double[] SequenceCoordinates(int length)
    {
        double[] result = new double[length];
        int index = 0;
        while (index < length)
        {
            result[index] = index + 1;
            index = index + 1;
        }
        return result;
    }

    static double NormalDensity(double value)
    {
        return global::System.Math.Exp(-value * value / 2)
            / global::System.Math.Sqrt(2 * global::System.Math.PI);
    }

    static double NormalCdf(double value)
    {
        double sign = value < 0 ? -1 : 1;
        double x = global::System.Math.Abs(value) / global::System.Math.Sqrt(2);
        double t = 1 / (1 + 0.3275911 * x);
        double polynomial = (((((1.061405429 * t - 1.453152027) * t)
            + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t;
        double erf = sign * (1 - polynomial
            * global::System.Math.Exp(-x * x));
        return (1 + erf) / 2;
    }

    static double NormalInverse(double probability)
    {
        double[] a = [-39.69683028665376, 220.9460984245205,
            -275.9285104469687, 138.3577518672690,
            -30.66479806614716, 2.506628277459239];
        double[] b = [-54.47609879822406, 161.5858368580409,
            -155.6989798598866, 66.80131188771972,
            -13.28068155288572];
        double[] c = [-0.007784894002430293, -0.3223964580411365,
            -2.400758277161838, -2.549732539343734,
            4.374664141464968, 2.938163982698783];
        double[] d = [0.007784695709041462, 0.3224671290700398,
            2.445134137142996, 3.754408661907416];
        double lower = 0.02425;
        double upper = 1 - lower;
        if (probability < lower)
        {
            double q = global::System.Math.Sqrt(
                -2 * global::System.Math.Log(probability));
            return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3])
                * q + c[4]) * q + c[5])
                / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
        if (probability > upper)
        {
            double q = global::System.Math.Sqrt(
                -2 * global::System.Math.Log(1 - probability));
            return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3])
                * q + c[4]) * q + c[5])
                / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
        double centered = probability - 0.5;
        double square = centered * centered;
        return (((((a[0] * square + a[1]) * square + a[2]) * square
            + a[3]) * square + a[4]) * square + a[5]) * centered
            / (((((b[0] * square + b[1]) * square + b[2]) * square
                + b[3]) * square + b[4]) * square + 1);
    }

    static double LogGamma(double value)
    {
        double[] coefficients = [0.99999999999980993, 676.5203681218851,
            -1259.1392167224028, 771.32342877765313,
            -176.61502916214059, 12.507343278686905,
            -0.13857109526572012, 0.0000099843695780195716,
            0.00000015056327351493116];
        if (value < 0.5)
        {
            return global::System.Math.Log(global::System.Math.PI)
                - global::System.Math.Log(global::System.Math.Sin(
                    global::System.Math.PI * value))
                - LogGamma(1 - value);
        }
        double shifted = value - 1;
        double sum = coefficients[0];
        int index = 1;
        while (index < coefficients.Length)
        {
            sum = sum + coefficients[index] / (shifted + index);
            index = index + 1;
        }
        double t = shifted + 7.5;
        return 0.5 * global::System.Math.Log(2 * global::System.Math.PI)
            + (shifted + 0.5) * global::System.Math.Log(t) - t
            + global::System.Math.Log(sum);
    }

    static double LogBeta(double first, double second)
    {
        return LogGamma(first) + LogGamma(second) - LogGamma(first + second);
    }

    static double RegularizedGamma(double shape, double value)
    {
        if (value <= 0) return 0;
        if (value < shape + 1)
        {
            double term = 1 / shape;
            double sum = term;
            double current = shape;
            int index = 1;
            while (index < 200 && global::System.Math.Abs(term) > 1E-15)
            {
                current = current + 1;
                term = term * value / current;
                sum = sum + term;
                index = index + 1;
            }
            return sum * global::System.Math.Exp(-value
                + shape * global::System.Math.Log(value) - LogGamma(shape));
        }
        double b = value + 1 - shape;
        double c = 1E300;
        double d = 1 / b;
        double h = d;
        int iteration = 1;
        while (iteration < 200)
        {
            double an = -iteration * (iteration - shape);
            b = b + 2;
            d = an * d + b;
            if (global::System.Math.Abs(d) < 1E-300) d = 1E-300;
            c = b + an / c;
            if (global::System.Math.Abs(c) < 1E-300) c = 1E-300;
            d = 1 / d;
            double delta = d * c;
            h = h * delta;
            if (global::System.Math.Abs(delta - 1) < 1E-14) iteration = 200;
            else iteration = iteration + 1;
        }
        return 1 - global::System.Math.Exp(-value
            + shape * global::System.Math.Log(value) - LogGamma(shape)) * h;
    }

    static double RegularizedBeta(double value, double first, double second)
    {
        if (value <= 0) return 0;
        if (value >= 1) return 1;
        double factor = global::System.Math.Exp(first * global::System.Math.Log(value)
            + second * global::System.Math.Log(1 - value)
            - LogBeta(first, second));
        if (value < (first + 1) / (first + second + 2))
            return factor * BetaFraction(value, first, second) / first;
        return 1 - factor * BetaFraction(1 - value, second, first) / second;
    }

    static double BetaFraction(double value, double first, double second)
    {
        double qab = first + second;
        double qap = first + 1;
        double qam = first - 1;
        double c = 1;
        double d = 1 - qab * value / qap;
        if (global::System.Math.Abs(d) < 1E-300) d = 1E-300;
        d = 1 / d;
        double h = d;
        int iteration = 1;
        while (iteration <= 200)
        {
            int twice = 2 * iteration;
            double aa = iteration * (second - iteration) * value
                / ((qam + twice) * (first + twice));
            d = 1 + aa * d;
            if (global::System.Math.Abs(d) < 1E-300) d = 1E-300;
            c = 1 + aa / c;
            if (global::System.Math.Abs(c) < 1E-300) c = 1E-300;
            d = 1 / d;
            h = h * d * c;
            aa = -(first + iteration) * (qab + iteration) * value
                / ((first + twice) * (qap + twice));
            d = 1 + aa * d;
            if (global::System.Math.Abs(d) < 1E-300) d = 1E-300;
            c = 1 + aa / c;
            if (global::System.Math.Abs(c) < 1E-300) c = 1E-300;
            d = 1 / d;
            double delta = d * c;
            h = h * delta;
            if (global::System.Math.Abs(delta - 1) < 1E-14) return h;
            iteration = iteration + 1;
        }
        return h;
    }

    static double InverseBeta(
        double probability,
        double first,
        double second)
    {
        double low = 0;
        double high = 1;
        int iteration = 0;
        while (iteration < 100)
        {
            double middle = (low + high) / 2;
            if (RegularizedBeta(middle, first, second) < probability)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
            iteration = iteration + 1;
        }
        return (low + high) / 2;
    }

    static double InverseGamma(double probability, double shape)
    {
        double low = 0;
        double high = global::System.Math.Max(1, shape);
        while (RegularizedGamma(shape, high) < probability)
        {
            high = high * 2;
        }
        int iteration = 0;
        while (iteration < 120)
        {
            double middle = (low + high) / 2;
            if (RegularizedGamma(shape, middle) < probability)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
            iteration = iteration + 1;
        }
        return (low + high) / 2;
    }

    static double FCdf(double value, double first, double second)
    {
        if (value <= 0) return 0;
        double transformed = first * value / (first * value + second);
        return RegularizedBeta(transformed, first / 2, second / 2);
    }

    static double InverseF(
        double probability,
        double first,
        double second)
    {
        double low = 0;
        double high = 1;
        while (FCdf(high, first, second) < probability)
        {
            high = high * 2;
        }
        int iteration = 0;
        while (iteration < 120)
        {
            double middle = (low + high) / 2;
            if (FCdf(middle, first, second) < probability)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
            iteration = iteration + 1;
        }
        return (low + high) / 2;
    }

    static double StudentCdf(double value, double degrees)
    {
        if (value == 0) return 0.5;
        double fraction = degrees / (degrees + value * value);
        double tail = RegularizedBeta(fraction, degrees / 2, 0.5) / 2;
        return value > 0 ? 1 - tail : tail;
    }

    static double InverseStudent(double probability, double degrees)
    {
        double low = -1;
        double high = 1;
        while (StudentCdf(low, degrees) > probability)
        {
            low = low * 2;
        }
        while (StudentCdf(high, degrees) < probability)
        {
            high = high * 2;
        }
        int iteration = 0;
        while (iteration < 120)
        {
            double middle = (low + high) / 2;
            if (StudentCdf(middle, degrees) < probability)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
            iteration = iteration + 1;
        }
        return (low + high) / 2;
    }

    static double FactorialValue(int value)
    {
        double result = 1;
        int factor = 2;
        while (factor <= value)
        {
            result = result * factor;
            factor = factor + 1;
        }
        return result;
    }

    static double CombinationValue(int total, int chosen)
    {
        if (chosen < 0 || chosen > total) return 0;
        int count = global::System.Math.Min(chosen, total - chosen);
        double result = 1;
        int index = 1;
        while (index <= count)
        {
            result = result * (total - count + index) / index;
            index = index + 1;
        }
        return result;
    }

    static double BinomialProbability(
        int trials,
        int successes,
        double probability)
    {
        return CombinationValue(trials, successes)
            * global::System.Math.Pow(probability, successes)
            * global::System.Math.Pow(1 - probability, trials - successes);
    }
}
