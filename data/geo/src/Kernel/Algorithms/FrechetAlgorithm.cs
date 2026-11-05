namespace geo;

internal static class FrechetAlgorithm
{
    internal static DistanceResult GetDistance(
        Geometry first, Geometry second)
    {
        return GetDistanceWithSubdivisions(first, second, 1);
    }

    internal static DistanceResult GetDensifiedDistance(
        Geometry first, Geometry second, double densifyFraction)
    {
        int subdivisionCount =
            DiscreteMetricSamples.GetSubdivisionCount(densifyFraction);
        return GetDistanceWithSubdivisions(
            first, second, subdivisionCount);
    }

    internal static DistanceResult GetDistanceWithSubdivisions(
        Geometry first, Geometry second, int subdivisionCount)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        DiscreteMetricSample[] firstSamples =
            DiscreteMetricSamples.CreateSamples(first, subdivisionCount);
        DiscreteMetricSample[] secondSamples =
            DiscreteMetricSamples.CreateSamples(second, subdivisionCount);
        if (firstSamples.Length == 0 || secondSamples.Length == 0)
        {
            return new DistanceResult();
        }
        return GetCouplingDistance(firstSamples, secondSamples);
    }

    internal static DistanceResult GetCouplingDistance(
        DiscreteMetricSample[] first, DiscreteMetricSample[] second)
    {
        DistanceResult[] previous = new DistanceResult[second.Length];
        DistanceResult[] current = new DistanceResult[second.Length];
        int firstIndex = 0;
        while (firstIndex < first.Length)
        {
            int secondIndex = 0;
            while (secondIndex < second.Length)
            {
                DistanceResult pair = DiscreteMetricSamples.CreatePairResult(
                    first[firstIndex], second[secondIndex]);
                if (firstIndex == 0 && secondIndex == 0)
                {
                    current[secondIndex] = pair;
                }
                else if (firstIndex == 0)
                {
                    current[secondIndex] =
                        DiscreteMetricSamples.SelectFartherResult(
                            current[secondIndex - 1], pair);
                }
                else if (secondIndex == 0)
                {
                    current[secondIndex] =
                        DiscreteMetricSamples.SelectFartherResult(
                            previous[secondIndex], pair);
                }
                else
                {
                    DistanceResult predecessor =
                        DiscreteMetricSamples.SelectCloserResult(
                            previous[secondIndex - 1],
                            previous[secondIndex]);
                    predecessor = DiscreteMetricSamples.SelectCloserResult(
                        predecessor, current[secondIndex - 1]);
                    current[secondIndex] =
                        DiscreteMetricSamples.SelectFartherResult(
                            predecessor, pair);
                }
                secondIndex++;
            }
            DistanceResult[] completed = previous;
            previous = current;
            current = completed;
            firstIndex++;
        }
        return previous[second.Length - 1];
    }
}
