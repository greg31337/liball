namespace asn1;

internal static class StableOrder
{
    internal static int CompareByteArrays(byte[] left, byte[] right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        int commonLength = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < commonLength)
        {
            if (left[index] < right[index])
            {
                return -1;
            }

            if (left[index] > right[index])
            {
                return 1;
            }

            index = index + 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    internal static int CompareStringsOrdinal(string left, string right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        return System.String.CompareOrdinal(left, right);
    }

    internal static int[] GetByteArrayOrder(byte[][] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        int[] order = new int[values.Length];
        int index = 0;
        while (index < order.Length)
        {
            order[index] = index;
            index = index + 1;
        }

        int insertionIndex = 1;
        while (insertionIndex < order.Length)
        {
            int originalIndex = order[insertionIndex];
            int position = insertionIndex;
            while (position > 0 && CompareByteArrays(values[order[position - 1]], values[originalIndex]) > 0)
            {
                order[position] = order[position - 1];
                position = position - 1;
            }

            order[position] = originalIndex;
            insertionIndex = insertionIndex + 1;
        }

        return order;
    }

    internal static int[] GetStringOrder(string[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        int[] order = new int[values.Length];
        int index = 0;
        while (index < order.Length)
        {
            order[index] = index;
            index = index + 1;
        }

        int insertionIndex = 1;
        while (insertionIndex < order.Length)
        {
            int originalIndex = order[insertionIndex];
            int position = insertionIndex;
            while (position > 0 && CompareStringsOrdinal(values[order[position - 1]], values[originalIndex]) > 0)
            {
                order[position] = order[position - 1];
                position = position - 1;
            }

            order[position] = originalIndex;
            insertionIndex = insertionIndex + 1;
        }

        return order;
    }
}
