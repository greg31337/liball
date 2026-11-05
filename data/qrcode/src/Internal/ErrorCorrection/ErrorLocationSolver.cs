namespace qrcode;

internal class ErrorLocationSolver
{
    internal static int[] CreateErasureLocator(int[] positions, int codewordCount)
    {
        if (positions == null)
        {
            throw new QrInputException("The erasure positions cannot be null.", "positions");
        }

        if (codewordCount <= 0 || codewordCount > GaloisField256.MultiplicativeOrder)
        {
            throw new QrInputException(
                "The codeword count must be between one and 255.",
                "codewordCount");
        }

        int[] locator = new int[] { 1 };
        for (int positionIndex = 0; positionIndex < positions.Length; positionIndex++)
        {
            int locationPower = codewordCount - 1 - positions[positionIndex];
            int location = GaloisField256.GetExponent(locationPower);
            int[] product = new int[locator.Length + 1];
            for (int locatorIndex = 0; locatorIndex < locator.Length; locatorIndex++)
            {
                product[locatorIndex] = GaloisField256.Add(
                    product[locatorIndex],
                    locator[locatorIndex]);
                product[locatorIndex + 1] = GaloisField256.Add(
                    product[locatorIndex + 1],
                    GaloisField256.Multiply(locator[locatorIndex], location));
            }

            locator = product;
        }

        return locator;
    }

    internal static int[] CreateForneySyndromes(
        int[] syndromes,
        int[] erasurePositions,
        int codewordCount)
    {
        if (syndromes == null)
        {
            throw new QrInputException("The syndromes cannot be null.", "syndromes");
        }

        if (erasurePositions == null)
        {
            throw new QrInputException(
                "The erasure positions cannot be null.",
                "erasurePositions");
        }

        if (erasurePositions.Length > syndromes.Length)
        {
            return new int[0];
        }

        int[] reduced = new int[syndromes.Length];
        for (int index = 0; index < syndromes.Length; index++)
        {
            reduced[index] = syndromes[index];
        }

        int reducedLength = reduced.Length;
        for (int positionIndex = 0;
            positionIndex < erasurePositions.Length;
            positionIndex++)
        {
            int locationPower = codewordCount - 1 - erasurePositions[positionIndex];
            int location = GaloisField256.GetExponent(locationPower);
            for (int syndromeIndex = 0;
                syndromeIndex < reducedLength - 1;
                syndromeIndex++)
            {
                reduced[syndromeIndex] = GaloisField256.Add(
                    GaloisField256.Multiply(reduced[syndromeIndex], location),
                    reduced[syndromeIndex + 1]);
            }

            reducedLength--;
        }

        int[] result = new int[reducedLength];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = reduced[index];
        }

        return result;
    }

    internal static int[] MultiplyLocators(int[] left, int[] right)
    {
        if (left == null || left.Length == 0)
        {
            throw new QrInputException("The left locator cannot be empty.", "left");
        }

        if (right == null || right.Length == 0)
        {
            throw new QrInputException("The right locator cannot be empty.", "right");
        }

        int[] product = new int[left.Length + right.Length - 1];
        for (int leftIndex = 0; leftIndex < left.Length; leftIndex++)
        {
            for (int rightIndex = 0; rightIndex < right.Length; rightIndex++)
            {
                product[leftIndex + rightIndex] = GaloisField256.Add(
                    product[leftIndex + rightIndex],
                    GaloisField256.Multiply(left[leftIndex], right[rightIndex]));
            }
        }

        return product;
    }

    internal static int[] SolveLocator(int[] syndromes)
    {
        if (syndromes == null)
        {
            throw new QrInputException("The syndromes cannot be null.", "syndromes");
        }

        int[] current = new int[syndromes.Length + 1];
        int[] previous = new int[syndromes.Length + 1];
        current[0] = 1;
        previous[0] = 1;
        int degree = 0;
        int shift = 1;
        int previousDiscrepancy = 1;
        for (int step = 0; step < syndromes.Length; step++)
        {
            int discrepancy = syndromes[step];
            for (int index = 1; index <= degree; index++)
            {
                discrepancy = GaloisField256.Add(
                    discrepancy,
                    GaloisField256.Multiply(current[index], syndromes[step - index]));
            }

            if (discrepancy == 0)
            {
                shift++;
                continue;
            }

            int[] saved = new int[current.Length];
            for (int index = 0; index < current.Length; index++)
            {
                saved[index] = current[index];
            }

            int scale = GaloisField256.Divide(discrepancy, previousDiscrepancy);
            for (int index = 0; index + shift < current.Length; index++)
            {
                current[index + shift] = GaloisField256.Add(
                    current[index + shift],
                    GaloisField256.Multiply(scale, previous[index]));
            }

            if (degree * 2 <= step)
            {
                degree = step + 1 - degree;
                previous = saved;
                previousDiscrepancy = discrepancy;
                shift = 1;
            }
            else
            {
                shift++;
            }
        }

        while (degree > 0 && current[degree] == 0)
        {
            degree--;
        }

        int[] locator = new int[degree + 1];
        for (int index = 0; index < locator.Length; index++)
        {
            locator[index] = current[index];
        }

        return locator;
    }

    internal static int[] CreateEvaluator(int[] syndromes, int[] locator)
    {
        if (syndromes == null)
        {
            throw new QrInputException("The syndromes cannot be null.", "syndromes");
        }

        if (locator == null || locator.Length == 0)
        {
            throw new QrInputException("The locator polynomial cannot be empty.", "locator");
        }

        int evaluatorLength = locator.Length - 1;
        int[] evaluator = new int[evaluatorLength];
        for (int resultIndex = 0; resultIndex < evaluator.Length; resultIndex++)
        {
            int firstLocatorIndex = resultIndex - syndromes.Length + 1;
            if (firstLocatorIndex < 0)
            {
                firstLocatorIndex = 0;
            }

            int lastLocatorIndex = resultIndex;
            if (lastLocatorIndex >= locator.Length)
            {
                lastLocatorIndex = locator.Length - 1;
            }

            int value = 0;
            for (int locatorIndex = firstLocatorIndex;
                locatorIndex <= lastLocatorIndex;
                locatorIndex++)
            {
                value = GaloisField256.Add(
                    value,
                    GaloisField256.Multiply(
                        locator[locatorIndex],
                        syndromes[resultIndex - locatorIndex]));
            }

            evaluator[resultIndex] = value;
        }

        return evaluator;
    }

    internal static int[] FindErrorPositions(int[] locator, int codewordCount)
    {
        if (locator == null || locator.Length == 0)
        {
            throw new QrInputException("The locator polynomial cannot be empty.", "locator");
        }

        if (codewordCount <= 0 || codewordCount > GaloisField256.MultiplicativeOrder)
        {
            throw new QrInputException(
                "The codeword count must be between one and 255.",
                "codewordCount");
        }

        int degree = locator.Length - 1;
        int[] positions = new int[degree];
        int foundCount = 0;
        for (int position = 0; position < codewordCount; position++)
        {
            int locationPower = codewordCount - 1 - position;
            int inverseLocation = GaloisField256.GetExponent(-locationPower);
            if (EvaluateAscending(locator, inverseLocation) == 0)
            {
                if (foundCount >= positions.Length)
                {
                    return new int[0];
                }

                positions[foundCount] = position;
                foundCount++;
            }
        }

        if (foundCount != degree)
        {
            return new int[0];
        }

        return positions;
    }

    internal static int[] CreateMagnitudes(
        int[] locator,
        int[] evaluator,
        int[] positions,
        int codewordCount)
    {
        if (locator == null || locator.Length == 0)
        {
            throw new QrInputException("The locator polynomial cannot be empty.", "locator");
        }

        if (evaluator == null)
        {
            throw new QrInputException("The evaluator polynomial cannot be null.", "evaluator");
        }

        if (positions == null)
        {
            throw new QrInputException("The error positions cannot be null.", "positions");
        }

        if (codewordCount <= 0 || codewordCount > GaloisField256.MultiplicativeOrder)
        {
            throw new QrInputException(
                "The codeword count must be between one and 255.",
                "codewordCount");
        }

        int[] magnitudes = new int[positions.Length];
        for (int positionIndex = 0; positionIndex < positions.Length; positionIndex++)
        {
            int position = positions[positionIndex];
            if (position < 0 || position >= codewordCount)
            {
                throw new QrInputException(
                    "An error position is outside the codeword block.",
                    "positions");
            }

            int locationPower = codewordCount - 1 - position;
            int location = GaloisField256.GetExponent(locationPower);
            int inverseLocation = GaloisField256.GetExponent(-locationPower);
            int numerator = EvaluateAscending(evaluator, inverseLocation);
            int denominator = EvaluateDerivative(locator, inverseLocation);
            if (denominator == 0)
            {
                return new int[0];
            }

            magnitudes[positionIndex] = GaloisField256.Multiply(
                location,
                GaloisField256.Divide(numerator, denominator));
        }

        return magnitudes;
    }

    internal static int EvaluateAscending(int[] coefficients, int value)
    {
        if (coefficients == null)
        {
            throw new QrInputException(
                "The polynomial coefficients cannot be null.",
                "coefficients");
        }

        GaloisField256.Add(value, 0);
        int result = 0;
        for (int index = coefficients.Length - 1; index >= 0; index--)
        {
            result = GaloisField256.Add(
                GaloisField256.Multiply(result, value),
                coefficients[index]);
        }

        return result;
    }

    static int EvaluateDerivative(int[] locator, int value)
    {
        int result = 0;
        int power = 1;
        for (int index = 1; index < locator.Length; index++)
        {
            if ((index & 1) != 0)
            {
                result = GaloisField256.Add(
                    result,
                    GaloisField256.Multiply(locator[index], power));
            }

            power = GaloisField256.Multiply(power, value);
        }

        return result;
    }
}
