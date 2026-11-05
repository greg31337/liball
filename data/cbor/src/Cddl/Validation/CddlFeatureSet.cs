using System;
using System.Collections.Generic;

namespace cbor;

internal class CddlFeatureSet
{
    internal HashSet<string> Features =
        new HashSet<string>(StringComparer.Ordinal);
    internal List<string> OrderedFeatures = new List<string>();

    internal CddlFeatureSet(string[] enabledFeatures)
    {
        int index = 0;
        while (index < enabledFeatures.Length)
        {
            string? feature = enabledFeatures[index];
            if (feature != null && Features.Add(feature))
            {
                OrderedFeatures.Add(feature);
            }
            index = index + 1;
        }
    }

    internal bool ContainsFeature(string feature)
    {
        return Features.Contains(feature);
    }

    internal string[] CreateFeatureArray()
    {
        return OrderedFeatures.ToArray();
    }
}
