namespace random;

internal class CategoricalProbabilityTable
{
    double[] normalizedProbabilities = new double[0];
    double[] cumulativeProbabilities = new double[0];
    int deterministicIndex;

    internal CategoricalProbabilityTable(double[] probabilities, string argumentName)
    {
        Initialize(probabilities, argumentName);
    }

    void Initialize(double[] probabilities, string argumentName)
    {
        Guard.NotNull(probabilities, argumentName);
        if (probabilities.Length == 0)
        {
            throw RandomArgumentException.Create(argumentName, "At least one probability is required.");
        }

        double maximum = 0.0;
        int positiveCount = 0;
        int lastPositiveIndex = -1;
        int index = 0;
        while (index < probabilities.Length)
        {
            double probability = probabilities[index];
            if (!double.IsFinite(probability) || probability < 0.0)
            {
                throw RandomArgumentException.Create(argumentName, "Every probability must be finite and nonnegative.");
            }

            if (probability > 0.0)
            {
                positiveCount++;
                lastPositiveIndex = index;
                if (probability > maximum)
                {
                    maximum = probability;
                }
            }

            index++;
        }

        if (positiveCount == 0)
        {
            throw RandomArgumentException.Create(argumentName, "At least one probability must be positive.");
        }

        normalizedProbabilities = new double[probabilities.Length];
        cumulativeProbabilities = new double[probabilities.Length];
        deterministicIndex = -1;
        if (positiveCount == 1)
        {
            normalizedProbabilities[lastPositiveIndex] = 1.0;
            index = lastPositiveIndex;
            while (index < cumulativeProbabilities.Length)
            {
                cumulativeProbabilities[index] = 1.0;
                index++;
            }

            deterministicIndex = lastPositiveIndex;
            return;
        }

        double total = 0.0;
        double compensation = 0.0;
        index = 0;
        while (index < probabilities.Length)
        {
            if (probabilities[index] > 0.0)
            {
                double scaled = probabilities[index] / maximum;
                double adjusted = scaled - compensation;
                double next = total + adjusted;
                compensation = (next - total) - adjusted;
                total = next;
            }

            index++;
        }

        double running = 0.0;
        compensation = 0.0;
        index = 0;
        while (index < probabilities.Length)
        {
            if (probabilities[index] > 0.0)
            {
                double scaled = probabilities[index] / maximum;
                normalizedProbabilities[index] = scaled / total;
                double adjusted = scaled - compensation;
                double next = running + adjusted;
                compensation = (next - running) - adjusted;
                running = next;
            }

            cumulativeProbabilities[index] = running / total;
            index++;
        }

        index = lastPositiveIndex;
        while (index < cumulativeProbabilities.Length)
        {
            cumulativeProbabilities[index] = 1.0;
            index++;
        }
    }

    internal int Length()
    {
        return normalizedProbabilities.Length;
    }

    internal double NormalizedProbability(int index)
    {
        return normalizedProbabilities[index];
    }

    internal double CumulativeProbability(int index)
    {
        return cumulativeProbabilities[index];
    }

    internal int DeterministicIndex()
    {
        return deterministicIndex;
    }
}
