using System;
using System.IO;

namespace gltf;

internal static class CollectionLimit
{
    internal static int EnsureCountWithinLimit(int count, int maximumCount, string limitName)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (maximumCount < 0)
        {
            throw new ArgumentOutOfRangeException("maximumCount");
        }

        if (count > maximumCount)
        {
            throw new InvalidDataException($"The {limitName} count of {count} exceeds the configured limit of {maximumCount}.");
        }

        return count;
    }

    internal static int EnsureAdditionalCountWithinLimit(int currentCount, int additionalCount, int maximumCount, string limitName)
    {
        EnsureCountWithinLimit(currentCount, maximumCount, limitName);

        if (additionalCount < 0)
        {
            throw new ArgumentOutOfRangeException("additionalCount");
        }

        int availableCount = maximumCount - currentCount;
        if (additionalCount > availableCount)
        {
            throw new InvalidDataException($"Adding {additionalCount} items would exceed the configured {limitName} limit of {maximumCount}.");
        }

        return currentCount + additionalCount;
    }
}
