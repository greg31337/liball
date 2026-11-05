using System;

namespace fake;

internal static class RandomDistributions
{
    internal static double Normal(FakeRandom random, double mean, double standardDeviation)
    {
        FloatingRandom.Finite(mean, "mean");
        FloatingRandom.Finite(standardDeviation, "standardDeviation");
        if (standardDeviation < 0.0)
        {
            throw new ArgumentOutOfRangeException("standardDeviation", standardDeviation, "standardDeviation must be zero or greater.");
        }
        if (standardDeviation == 0.0)
        {
            return mean;
        }
        double first = 1.0 - random.Double();
        double second = random.Double();
        double standard = Math.Sqrt(-2.0 * Math.Log(first)) * Math.Cos(2.0 * Math.PI * second);
        return Finite(mean + (standardDeviation * standard));
    }

    internal static double LogNormal(FakeRandom random, double mean, double standardDeviation)
    {
        double logarithm = Normal(random, mean, standardDeviation);
        if (logarithm >= Math.Log(double.MaxValue))
        {
            return double.MaxValue;
        }
        if (logarithm <= Math.Log(double.Epsilon))
        {
            return double.Epsilon;
        }
        return Math.Exp(logarithm);
    }

    internal static double Exponential(FakeRandom random, double rate)
    {
        FloatingRandom.Finite(rate, "rate");
        if (rate <= 0.0)
        {
            throw new ArgumentOutOfRangeException("rate", rate, "rate must be greater than zero.");
        }
        double value = -Math.Log(1.0 - random.Double()) / rate;
        return Finite(value);
    }

    internal static double Triangular(FakeRandom random, double minimum, double maximum, double mode)
    {
        FloatingRandom.Finite(minimum, "minimum");
        FloatingRandom.Finite(maximum, "maximum");
        FloatingRandom.Finite(mode, "mode");
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum.");
        }
        if (mode < minimum || mode > maximum)
        {
            throw new ArgumentOutOfRangeException("mode", mode, "mode must be between minimum and maximum, inclusive.");
        }
        double left = mode - minimum;
        double right = maximum - mode;
        double split;
        if (double.IsFinite(left) && double.IsFinite(right))
        {
            double distanceScale = Math.Max(left, right);
            double scaledLeft = left / distanceScale;
            double scaledRight = right / distanceScale;
            split = scaledLeft / (scaledLeft + scaledRight);
        }
        else
        {
            double coordinateScale = Math.Max(Math.Abs(minimum), Math.Max(Math.Abs(mode), Math.Abs(maximum)));
            double scaledMinimum = minimum / coordinateScale;
            double scaledMode = mode / coordinateScale;
            double scaledMaximum = maximum / coordinateScale;
            double scaledLeft = scaledMode - scaledMinimum;
            double scaledRight = scaledMaximum - scaledMode;
            split = scaledLeft / (scaledLeft + scaledRight);
        }
        double unit = random.Double();
        double position;
        if (unit < split)
        {
            position = Math.Sqrt(unit * split);
        }
        else
        {
            position = 1.0 - Math.Sqrt((1.0 - unit) * (1.0 - split));
        }
        return Interpolate(minimum, maximum, position);
    }

    internal static int WeightedDiscrete(FakeRandom random, double[] weights)
    {
        Guard.NotNull(weights, "weights");
        if (weights.Length == 0)
        {
            throw new ArgumentException("weights cannot be empty.", "weights");
        }
        Guard.Count(weights.Length, random.Context.CapturedOptions.MaximumCount, "weights");
        double maximumWeight = 0.0;
        int positiveCount = 0;
        int lastPositive = -1;
        int index = 0;
        while (index < weights.Length)
        {
            double weight = weights[index];
            FloatingRandom.Finite(weight, "weights");
            if (weight < 0.0)
            {
                throw new ArgumentOutOfRangeException("weights", weight, $"weights[{index}] must be zero or greater.");
            }
            if (weight > 0.0)
            {
                positiveCount = positiveCount + 1;
                lastPositive = index;
                if (weight > maximumWeight)
                {
                    maximumWeight = weight;
                }
            }
            index = index + 1;
        }
        if (positiveCount == 0)
        {
            throw new ArgumentException("weights must contain at least one positive value.", "weights");
        }
        if (positiveCount == 1)
        {
            return lastPositive;
        }
        double total = 0.0;
        index = 0;
        while (index < weights.Length)
        {
            total = total + (weights[index] / maximumWeight);
            index = index + 1;
        }
        double target = random.Double() * total;
        double cumulative = 0.0;
        index = 0;
        while (index < weights.Length)
        {
            cumulative = cumulative + (weights[index] / maximumWeight);
            if (target < cumulative)
            {
                return index;
            }
            index = index + 1;
        }
        return lastPositive;
    }

    internal static double Midpoint(double minimum, double maximum)
    {
        if (minimum < 0.0 && maximum > 0.0)
        {
            return (minimum + maximum) / 2.0;
        }
        return minimum + ((maximum - minimum) / 2.0);
    }

    static double Interpolate(double minimum, double maximum, double position)
    {
        double result;
        if (minimum >= 0.0)
        {
            result = minimum + ((maximum - minimum) * position);
        }
        else if (maximum <= 0.0)
        {
            result = maximum - ((maximum - minimum) * (1.0 - position));
        }
        else
        {
            result = (minimum * (1.0 - position)) + (maximum * position);
        }
        if (result < minimum)
        {
            return minimum;
        }
        if (result >= maximum)
        {
            double below = Math.BitDecrement(maximum);
            return below < minimum ? minimum : below;
        }
        return result;
    }

    static double Finite(double value)
    {
        if (double.IsPositiveInfinity(value))
        {
            return double.MaxValue;
        }
        if (double.IsNegativeInfinity(value))
        {
            return double.MinValue;
        }
        return value;
    }
}
