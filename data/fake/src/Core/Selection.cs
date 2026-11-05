using System;

namespace fake;

internal static class Selection
{
    internal static int WeightedChoiceIndex(FakeRandom random, int length, double[] weights)
    {
        Guard.NotNull(weights, "weights");
        Guard.Count(length, random.Context.CapturedOptions.MaximumCount, "values");
        Guard.Count(weights.Length, random.Context.CapturedOptions.MaximumCount, "weights");
        if (length == 0)
        {
            throw new ArgumentException("values cannot be empty.", "values");
        }
        if (weights.Length != length)
        {
            throw new ArgumentException("weights must contain exactly one weight for each value.", "weights");
        }
        return RandomDistributions.WeightedDiscrete(random, weights);
    }

    internal static int ChoiceIndex(FakeRandom random, int length)
    {
        Guard.Count(length, random.Context.CapturedOptions.MaximumCount, "values");
        if (length == 0)
        {
            throw new ArgumentException("values cannot be empty.", "values");
        }
        return random.IntFromMaximumExclusive(length);
    }

    internal static int[] ChoiceIndexes(FakeRandom random, int length, int count, bool withReplacement)
    {
        Guard.Count(length, random.Context.CapturedOptions.MaximumCount, "values");
        Guard.Count(count, random.Context.CapturedOptions.MaximumCount, "count");
        if (count == 0)
        {
            return new int[0];
        }
        if (length == 0)
        {
            throw new ArgumentException("values cannot be empty when count is positive.", "values");
        }
        if (!withReplacement && count > length)
        {
            throw new ArgumentOutOfRangeException("count", count, "count cannot exceed the number of values without replacement.");
        }
        int[] selected = new int[count];
        if (withReplacement)
        {
            int selectedIndex = 0;
            while (selectedIndex < count)
            {
                selected[selectedIndex] = random.IntFromMaximumExclusive(length);
                selectedIndex = selectedIndex + 1;
            }
            return selected;
        }
        int[] positions = new int[length];
        int index = 0;
        while (index < length)
        {
            positions[index] = index;
            index = index + 1;
        }
        index = 0;
        while (index < count)
        {
            int swapIndex = random.IntFromMinimumAndMaximum(index, length);
            int selectedPosition = positions[swapIndex];
            positions[swapIndex] = positions[index];
            positions[index] = selectedPosition;
            selected[index] = selectedPosition;
            index = index + 1;
        }
        return selected;
    }
}
