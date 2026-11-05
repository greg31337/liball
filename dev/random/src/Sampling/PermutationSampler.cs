using System;
using System.Collections.Generic;

namespace random;

internal static class PermutationSampler
{
    const int SparsePopulationThreshold = 4096;

    internal static int[] Permutation(IRandomSource generator, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(count, "count");
        int[] result = NaturalOrder(count);
        RandomCollections.Shuffle(generator, result);
        return result;
    }

    internal static int[] Subset(IRandomSource generator, int populationSize, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(populationSize, "populationSize");
        Guard.Argument(count >= 0 && count <= populationSize, "count", "The count must be within the population bounds.");
        if (count == 0)
        {
            return new int[0];
        }

        if (count == populationSize)
        {
            return NaturalOrder(populationSize);
        }

        int[] result;
        if (UseSparsePath(populationSize, count))
        {
            result = SparseOrderedIndexes(generator, populationSize, count);
        }
        else
        {
            int[] candidates = NaturalOrder(populationSize);
            RandomCollections.PartialShuffle(generator, candidates, count);
            result = new int[count];
            Array.Copy(candidates, result, count);
        }

        Array.Sort(result);
        return result;
    }

    internal static int[] Derangement(IRandomSource generator, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(count, "count");
        if (count == 0)
        {
            return new int[0];
        }

        if (count == 1)
        {
            throw RandomArgumentException.Create("count", "A one-element derangement is impossible.");
        }

        if (count == 2)
        {
            return new int[] { 1, 0 };
        }

        int[] result = NaturalOrder(count);
        while (true)
        {
            RandomCollections.Shuffle(generator, result);
            if (IsDerangement(result))
            {
                return result;
            }

            int index = 0;
            while (index < result.Length)
            {
                result[index] = index;
                index++;
            }
        }
    }

    static bool IsDerangement(int[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == index)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool UseSparsePath(int populationSize, int count)
    {
        return populationSize >= SparsePopulationThreshold && count <= populationSize / 8;
    }

    internal static int[] SparseOrderedIndexes(IRandomSource generator, int populationSize, int count)
    {
        Dictionary<int, int> movedIndexes = new Dictionary<int, int>();
        int[] result = new int[count];
        int current = 0;
        while (current < count)
        {
            int selected = current + IntegerUniform.NextInt32(generator, populationSize - current);
            int selectedValue = selected;
            if (movedIndexes.ContainsKey(selected))
            {
                selectedValue = movedIndexes[selected];
            }

            int currentValue = current;
            if (movedIndexes.ContainsKey(current))
            {
                currentValue = movedIndexes[current];
            }

            result[current] = selectedValue;
            movedIndexes[selected] = currentValue;
            movedIndexes.Remove(current);
            current++;
        }

        return result;
    }

    internal static int[] NaturalOrder(int count)
    {
        int[] result = new int[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = index;
            index++;
        }

        return result;
    }
}
