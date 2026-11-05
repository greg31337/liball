namespace random;

internal static class ReservoirSampler
{
    internal static object?[] Sample(IRandomSource generator, System.Collections.IEnumerable values, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Count(count, "count");
        object?[] result = new object?[count];
        if (count == 0)
        {
            return result;
        }

        long seen = 0L;
        foreach (object? value in values)
        {
            if (seen < (long)count)
            {
                result[(int)seen] = value;
            }
            else
            {
                if (seen == long.MaxValue)
                {
                    throw RandomStateException.Create("The source contains too many elements for reservoir sampling.");
                }

                long selected = IntegerUniform.NextInt64(generator, seen + 1L);
                if (selected < (long)count)
                {
                    result[(int)selected] = value;
                }
            }

            seen++;
        }

        Guard.Argument(seen >= (long)count, "count", "The count must not exceed the number of source elements.");
        return result;
    }

    internal static object?[] Sample(IRandomSource generator, System.Collections.IList values, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        int sourceCount = values.Count;
        Guard.Argument(count >= 0 && count <= sourceCount, "count", "The count must be within the source bounds.");
        object?[] result = new object?[count];
        if (count == 0)
        {
            return result;
        }

        int index = 0;
        while (index < count)
        {
            result[index] = values[index];
            index++;
        }

        while (index < sourceCount)
        {
            object? value = values[index];
            long selected = IntegerUniform.NextInt64(generator, (long)index + 1L);
            if (selected < (long)count)
            {
                result[(int)selected] = value;
            }

            index++;
        }

        return result;
    }
}
