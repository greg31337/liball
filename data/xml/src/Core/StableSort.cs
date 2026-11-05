using System;
using System.Collections;

namespace xml;

internal static class StableSort
{
    internal static void SortObjects(object[] values, IComparer comparer)
    {
        Guard.NotNull(values, "values");
        Guard.NotNull(comparer, "comparer");
        if (values.Length < 2)
        {
            return;
        }

        object[] buffer = new object[values.Length];
        object[] source = values;
        object[] destination = buffer;
        int width = 1;
        while (width < values.Length)
        {
            int start = 0;
            while (start < values.Length)
            {
                int middle = start + Math.Min(width, values.Length - start);
                int end = middle + Math.Min(width, values.Length - middle);
                MergeRange(source, destination, comparer, start, middle, end);
                start = end;
            }

            object[] swap = source;
            source = destination;
            destination = swap;
            if (width > values.Length / 2)
            {
                width = values.Length;
            }
            else
            {
                width = width * 2;
            }
        }

        if (!object.ReferenceEquals(source, values))
        {
            Array.Copy(source, values, values.Length);
        }
    }

    internal static void MergeRange(object[] source, object[] destination, IComparer comparer, int start, int middle, int end)
    {
        int left = start;
        int right = middle;
        int output = start;
        while (left < middle && right < end)
        {
            if (comparer.Compare(source[left], source[right]) <= 0)
            {
                destination[output] = source[left];
                left = left + 1;
            }
            else
            {
                destination[output] = source[right];
                right = right + 1;
            }

            output = output + 1;
        }

        while (left < middle)
        {
            destination[output] = source[left];
            left = left + 1;
            output = output + 1;
        }

        while (right < end)
        {
            destination[output] = source[right];
            right = right + 1;
            output = output + 1;
        }
    }
}
