namespace chart;

internal static class ChartAxisBreakOperations
{
    internal static double Normalize(double value, ChartRange domain, double[] minimums, double[] maximums, int count)
    {
        double effectiveLength = EffectiveLength(domain, minimums, maximums, count);
        double distance = value - domain.Minimum;
        int index = 0;
        while (index < count)
        {
            if (value >= maximums[index])
            {
                distance = distance - (maximums[index] - minimums[index]);
            }
            else if (value > minimums[index])
            {
                distance = distance - (value - minimums[index]);
            }

            index = index + 1;
        }

        return distance / effectiveLength;
    }

    internal static double Denormalize(double amount, ChartRange domain, double[] minimums, double[] maximums, int count)
    {
        double effectiveLength = EffectiveLength(domain, minimums, maximums, count);
        if (amount < 0.0)
        {
            return domain.Minimum + amount * effectiveLength;
        }

        if (amount > 1.0)
        {
            return domain.Maximum + (amount - 1.0) * effectiveLength;
        }

        double distance = amount * effectiveLength;
        double cursor = domain.Minimum;
        int index = 0;
        while (index < count)
        {
            double segment = minimums[index] - cursor;
            if (distance <= segment)
            {
                return cursor + distance;
            }

            distance = distance - segment;
            cursor = maximums[index];
            index = index + 1;
        }

        return cursor + distance;
    }

    internal static double EffectiveLength(ChartRange domain, double[] minimums, double[] maximums, int count)
    {
        double length = domain.Maximum - domain.Minimum;
        int index = 0;
        while (index < count)
        {
            length = length - (maximums[index] - minimums[index]);
            index = index + 1;
        }

        return length;
    }

    internal static bool IsVisible(double value, double[] minimums, double[] maximums, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (value > minimums[index] && value < maximums[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
