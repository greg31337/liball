namespace barcode;

internal static class Pdf417ReedSolomon
{
    internal const int MinimumErrorCorrectionLevel = 0;
    internal const int MaximumErrorCorrectionLevel = 8;
    internal const int MaximumBlockLength = 928;

    internal static int GetErrorCorrectionCodewordCount(int errorCorrectionLevel)
    {
        ValidateErrorCorrectionLevel(errorCorrectionLevel);
        return 1 << (errorCorrectionLevel + 1);
    }

    internal static ModulusPolynomial929 BuildGenerator(int errorCorrectionLevel)
    {
        int errorCorrectionCount = GetErrorCorrectionCodewordCount(errorCorrectionLevel);
        ModulusField929 field = ModulusField929.Pdf417;
        ModulusPolynomial929 generator = field.CreateOnePolynomial();
        int index = 1;
        while (index <= errorCorrectionCount)
        {
            int root = field.Exponent(index);
            int negativeRoot = ModulusField929.Negate(root);
            ModulusPolynomial929 factor = new ModulusPolynomial929(new int[] { 1, negativeRoot });
            generator = generator.MultiplyPolynomial(factor);
            index = index + 1;
        }

        return generator;
    }

    internal static int[] EncodeParity(int[] dataCodewords, int errorCorrectionLevel)
    {
        int errorCorrectionCount = GetErrorCorrectionCodewordCount(errorCorrectionLevel);
        ValidateDataBlock(dataCodewords, errorCorrectionCount);
        ModulusPolynomial929 generator = BuildGenerator(errorCorrectionLevel);
        ModulusPolynomial929 data = new ModulusPolynomial929(dataCodewords);
        ModulusPolynomial929 shifted = data.MultiplyByMonomial(errorCorrectionCount, 1);
        ModulusPolynomial929[] division = shifted.DivideByPolynomial(generator);
        int[] remainder = division[1].CopyCoefficients();
        int[] parity = new int[errorCorrectionCount];
        int offset = errorCorrectionCount - remainder.Length;
        int index = 0;
        while (index < remainder.Length)
        {
            parity[offset + index] = ModulusField929.Negate(remainder[index]);
            index = index + 1;
        }

        return parity;
    }

    internal static int[] EncodeCodewords(int[] dataCodewords, int errorCorrectionLevel)
    {
        int[] parity = EncodeParity(dataCodewords, errorCorrectionLevel);
        int[] encoded = new int[CheckedMath.Add(dataCodewords.Length, parity.Length)];
        int index = 0;
        while (index < dataCodewords.Length)
        {
            encoded[index] = dataCodewords[index];
            index = index + 1;
        }

        int parityIndex = 0;
        while (parityIndex < parity.Length)
        {
            encoded[dataCodewords.Length + parityIndex] = parity[parityIndex];
            parityIndex = parityIndex + 1;
        }

        return encoded;
    }

    internal static int[] CalculateSyndromes(int[] receivedCodewords, int errorCorrectionLevel)
    {
        int errorCorrectionCount = GetErrorCorrectionCodewordCount(errorCorrectionLevel);
        ValidateReceivedBlock(receivedCodewords, errorCorrectionCount);
        ModulusField929 field = ModulusField929.Pdf417;
        ModulusPolynomial929 received = new ModulusPolynomial929(receivedCodewords);
        int[] syndromes = new int[errorCorrectionCount];
        int index = 0;
        while (index < errorCorrectionCount)
        {
            int evaluationPoint = field.Exponent(index + 1);
            syndromes[index] = received.EvaluateAt(evaluationPoint);
            index = index + 1;
        }

        return syndromes;
    }

    internal static bool VerifyCodewords(int[] receivedCodewords, int errorCorrectionLevel)
    {
        int[] syndromes = CalculateSyndromes(receivedCodewords, errorCorrectionLevel);
        return AreSyndromesZero(syndromes);
    }

    internal static Pdf417CorrectionResult CorrectCodewords(
        int[] receivedCodewords,
        int errorCorrectionLevel,
        int[]? erasureIndices)
    {
        int errorCorrectionCount = GetErrorCorrectionCodewordCount(errorCorrectionLevel);
        ValidateReceivedBlock(receivedCodewords, errorCorrectionCount);
        int[] erasures = ValidateAndCopyErasures(erasureIndices, receivedCodewords.Length);
        int[] original = Pdf417CorrectionResult.CopyIntArray(receivedCodewords);
        if (erasures.Length > errorCorrectionCount)
        {
            return CreateFailureResult(original, erasures.Length, "TOO_MANY_ERASURES", "The number of erasures exceeds the PDF417 error-correction capacity.");
        }

        int[] syndromes = CalculateSyndromes(receivedCodewords, errorCorrectionLevel);
        if (AreSyndromesZero(syndromes))
        {
            return CreateSuccessResult(original, erasures.Length, new int[0]);
        }

        int[] forneySyndromes = CalculateForneySyndromes(syndromes, erasures, receivedCodewords.Length);
        int[] unknownLocator = FindErrorLocator(forneySyndromes);
        int unknownErrorCount = unknownLocator.Length - 1;
        if ((unknownErrorCount * 2) + erasures.Length > errorCorrectionCount)
        {
            return CreateFailureResult(original, erasures.Length, "ERROR_CAPACITY_EXCEEDED", "The PDF417 errors and erasures exceed the correction capacity.");
        }

        int[] erasureLocator = BuildErasureLocator(erasures, receivedCodewords.Length);
        int[] combinedLocator = MultiplyAscendingPolynomials(unknownLocator, erasureLocator);
        int expectedErrorCount = combinedLocator.Length - 1;
        int[] errorIndices = FindErrorIndices(combinedLocator, receivedCodewords.Length);
        if (errorIndices.Length != expectedErrorCount)
        {
            return CreateFailureResult(original, erasures.Length, "ERROR_LOCATIONS_NOT_FOUND", "The PDF417 error locator roots do not identify the expected codeword positions.");
        }

        int[] evaluator = CalculateErrorEvaluator(syndromes, combinedLocator, errorCorrectionCount);
        int[]? magnitudes = CalculateErrorMagnitudes(evaluator, combinedLocator, errorIndices, receivedCodewords.Length);
        if (magnitudes == null)
        {
            return CreateFailureResult(original, erasures.Length, "MAGNITUDE_SOLVE_FAILED", "The PDF417 error magnitudes could not be calculated.");
        }

        int[] corrected = Pdf417CorrectionResult.CopyIntArray(receivedCodewords);
        int[] changedBuffer = new int[errorIndices.Length];
        int changedCount = 0;
        int index = 0;
        while (index < errorIndices.Length)
        {
            if (magnitudes[index] != 0)
            {
                int position = errorIndices[index];
                corrected[position] = ModulusField929.Subtract(corrected[position], magnitudes[index]);
                changedBuffer[changedCount] = position;
                changedCount = changedCount + 1;
            }

            index = index + 1;
        }

        if (!VerifyCodewords(corrected, errorCorrectionLevel))
        {
            return CreateFailureResult(original, erasures.Length, "SYNDROME_REMAINS", "The corrected PDF417 block still contains a nonzero syndrome.");
        }

        int[] changedIndices = new int[changedCount];
        index = 0;
        while (index < changedCount)
        {
            changedIndices[index] = changedBuffer[index];
            index = index + 1;
        }

        return CreateSuccessResult(corrected, erasures.Length, changedIndices);
    }

    internal static int[] CalculateForneySyndromes(int[] syndromes, int[] erasureIndices, int receivedLength)
    {
        int[] reduced = Pdf417CorrectionResult.CopyIntArray(syndromes);
        ModulusField929 field = ModulusField929.Pdf417;
        int erasureIndex = 0;
        while (erasureIndex < erasureIndices.Length)
        {
            int degree = receivedLength - 1 - erasureIndices[erasureIndex];
            int location = field.Exponent(degree);
            int nextLength = reduced.Length - 1;
            int[] next = new int[nextLength];
            int syndromeIndex = 0;
            while (syndromeIndex < nextLength)
            {
                int product = field.Multiply(reduced[syndromeIndex], location);
                next[syndromeIndex] = ModulusField929.Subtract(reduced[syndromeIndex + 1], product);
                syndromeIndex = syndromeIndex + 1;
            }

            reduced = next;
            erasureIndex = erasureIndex + 1;
        }

        return reduced;
    }

    internal static int[] FindErrorLocator(int[] syndromes)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int maximumLength = CheckedMath.Add(syndromes.Length, 1);
        int[] locator = new int[maximumLength];
        int[] previous = new int[maximumLength];
        locator[0] = 1;
        previous[0] = 1;
        int locatorDegree = 0;
        int shift = 1;
        int previousDiscrepancy = 1;
        int syndromeIndex = 0;
        while (syndromeIndex < syndromes.Length)
        {
            int discrepancy = syndromes[syndromeIndex];
            int coefficientIndex = 1;
            while (coefficientIndex <= locatorDegree)
            {
                int term = field.Multiply(locator[coefficientIndex], syndromes[syndromeIndex - coefficientIndex]);
                discrepancy = ModulusField929.Add(discrepancy, term);
                coefficientIndex = coefficientIndex + 1;
            }

            if (discrepancy == 0)
            {
                shift = shift + 1;
            }
            else
            {
                int[] locatorBeforeUpdate = Pdf417CorrectionResult.CopyIntArray(locator);
                int scale = field.DivideElements(discrepancy, previousDiscrepancy);
                int previousIndex = 0;
                while (previousIndex + shift < locator.Length)
                {
                    int term = field.Multiply(scale, previous[previousIndex]);
                    locator[previousIndex + shift] = ModulusField929.Subtract(locator[previousIndex + shift], term);
                    previousIndex = previousIndex + 1;
                }

                if ((locatorDegree * 2) <= syndromeIndex)
                {
                    locatorDegree = syndromeIndex + 1 - locatorDegree;
                    previous = locatorBeforeUpdate;
                    previousDiscrepancy = discrepancy;
                    shift = 1;
                }
                else
                {
                    shift = shift + 1;
                }
            }

            syndromeIndex = syndromeIndex + 1;
        }

        int[] result = new int[CheckedMath.Add(locatorDegree, 1)];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = locator[index];
            index = index + 1;
        }

        return result;
    }

    internal static int[] BuildErasureLocator(int[] erasureIndices, int receivedLength)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int[] locator = new int[] { 1 };
        int index = 0;
        while (index < erasureIndices.Length)
        {
            int degree = receivedLength - 1 - erasureIndices[index];
            int location = field.Exponent(degree);
            int[] factor = new int[] { 1, ModulusField929.Negate(location) };
            locator = MultiplyAscendingPolynomials(locator, factor);
            index = index + 1;
        }

        return locator;
    }

    internal static int[] MultiplyAscendingPolynomials(int[] first, int[] second)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int length = CheckedMath.Subtract(CheckedMath.Add(first.Length, second.Length), 1);
        int[] product = new int[length];
        int firstIndex = 0;
        while (firstIndex < first.Length)
        {
            int secondIndex = 0;
            while (secondIndex < second.Length)
            {
                int term = field.Multiply(first[firstIndex], second[secondIndex]);
                int productIndex = firstIndex + secondIndex;
                product[productIndex] = ModulusField929.Add(product[productIndex], term);
                secondIndex = secondIndex + 1;
            }

            firstIndex = firstIndex + 1;
        }

        return product;
    }

    internal static int[] FindErrorIndices(int[] locator, int receivedLength)
    {
        int expectedCount = locator.Length - 1;
        int[] indices = new int[expectedCount];
        int found = 0;
        ModulusField929 field = ModulusField929.Pdf417;
        int position = 0;
        while (position < receivedLength && found < expectedCount)
        {
            int degree = receivedLength - 1 - position;
            int location = field.Exponent(degree);
            int inverseLocation = field.Inverse(location);
            int value = EvaluateAscendingPolynomial(locator, inverseLocation);
            if (value == 0)
            {
                indices[found] = position;
                found = found + 1;
            }

            position = position + 1;
        }

        if (found == expectedCount)
        {
            return indices;
        }

        int[] incomplete = new int[found];
        position = 0;
        while (position < found)
        {
            incomplete[position] = indices[position];
            position = position + 1;
        }

        return incomplete;
    }

    internal static int[] CalculateErrorEvaluator(int[] syndromes, int[] locator, int errorCorrectionCount)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int[] evaluator = new int[errorCorrectionCount];
        int syndromeIndex = 0;
        while (syndromeIndex < syndromes.Length)
        {
            int locatorIndex = 0;
            while (locatorIndex < locator.Length && syndromeIndex + locatorIndex < errorCorrectionCount)
            {
                int term = field.Multiply(syndromes[syndromeIndex], locator[locatorIndex]);
                int evaluatorIndex = syndromeIndex + locatorIndex;
                evaluator[evaluatorIndex] = ModulusField929.Add(evaluator[evaluatorIndex], term);
                locatorIndex = locatorIndex + 1;
            }

            syndromeIndex = syndromeIndex + 1;
        }

        return evaluator;
    }

    internal static int[]? CalculateErrorMagnitudes(
        int[] evaluator,
        int[] locator,
        int[] errorIndices,
        int receivedLength)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int[] magnitudes = new int[errorIndices.Length];
        int index = 0;
        while (index < errorIndices.Length)
        {
            int degree = receivedLength - 1 - errorIndices[index];
            int location = field.Exponent(degree);
            int inverseLocation = field.Inverse(location);
            int numerator = ModulusField929.Negate(EvaluateAscendingPolynomial(evaluator, inverseLocation));
            int denominator = EvaluateAscendingDerivative(locator, inverseLocation);
            if (denominator == 0)
            {
                return null;
            }

            magnitudes[index] = field.DivideElements(numerator, denominator);
            index = index + 1;
        }

        return magnitudes;
    }

    internal static int EvaluateAscendingPolynomial(int[] coefficients, int element)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int result = 0;
        int power = 1;
        int index = 0;
        while (index < coefficients.Length)
        {
            result = ModulusField929.Add(result, field.Multiply(coefficients[index], power));
            power = field.Multiply(power, element);
            index = index + 1;
        }

        return result;
    }

    internal static int EvaluateAscendingDerivative(int[] coefficients, int element)
    {
        ModulusField929 field = ModulusField929.Pdf417;
        int result = 0;
        int power = 1;
        int index = 1;
        while (index < coefficients.Length)
        {
            int degreeElement = index % ModulusField929.Modulus;
            int derivativeCoefficient = field.Multiply(coefficients[index], degreeElement);
            result = ModulusField929.Add(result, field.Multiply(derivativeCoefficient, power));
            power = field.Multiply(power, element);
            index = index + 1;
        }

        return result;
    }

    internal static bool AreSyndromesZero(int[] syndromes)
    {
        int index = 0;
        while (index < syndromes.Length)
        {
            if (syndromes[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int[] ValidateAndCopyErasures(int[]? erasureIndices, int receivedLength)
    {
        if (erasureIndices == null)
        {
            return new int[0];
        }

        int[] copy = Pdf417CorrectionResult.CopyIntArray(erasureIndices);
        int index = 0;
        while (index < copy.Length)
        {
            CheckedMath.ValidateIndex(copy[index], receivedLength);
            int previous = 0;
            while (previous < index)
            {
                if (copy[previous] == copy[index])
                {
                    throw new BarcodeValidationException("PDF417 erasure indices must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateDataBlock(int[] dataCodewords, int errorCorrectionCount)
    {
        if (dataCodewords == null)
        {
            throw new BarcodeValidationException("PDF417 data codewords must not be null.");
        }

        if (dataCodewords.Length == 0)
        {
            throw new BarcodeValidationException("PDF417 data block must not be empty.");
        }

        int totalLength = CheckedMath.Add(dataCodewords.Length, errorCorrectionCount);
        if (totalLength > MaximumBlockLength)
        {
            throw new BarcodeValidationException("PDF417 Reed-Solomon block must not exceed 928 codewords.");
        }

        ValidateCodewordValues(dataCodewords);
    }

    internal static void ValidateReceivedBlock(int[] receivedCodewords, int errorCorrectionCount)
    {
        if (receivedCodewords == null)
        {
            throw new BarcodeValidationException("PDF417 received codewords must not be null.");
        }

        if (receivedCodewords.Length <= errorCorrectionCount || receivedCodewords.Length > MaximumBlockLength)
        {
            throw new BarcodeValidationException("PDF417 received block length is incompatible with the error-correction level.");
        }

        ValidateCodewordValues(receivedCodewords);
    }

    internal static void ValidateCodewordValues(int[] codewords)
    {
        int index = 0;
        while (index < codewords.Length)
        {
            ModulusField929.ValidateElement(codewords[index]);
            index = index + 1;
        }
    }

    internal static void ValidateErrorCorrectionLevel(int errorCorrectionLevel)
    {
        if (errorCorrectionLevel < MinimumErrorCorrectionLevel || errorCorrectionLevel > MaximumErrorCorrectionLevel)
        {
            throw new BarcodeValidationException("PDF417 error-correction level must be from zero through eight.");
        }
    }

    internal static Pdf417CorrectionResult CreateSuccessResult(int[] corrected, int erasureCount, int[] changedIndices)
    {
        return new Pdf417CorrectionResult(
            true,
            corrected,
            changedIndices.Length,
            erasureCount,
            changedIndices,
            null,
            null);
    }

    internal static Pdf417CorrectionResult CreateFailureResult(
        int[] original,
        int erasureCount,
        string failureCode,
        string failureMessage)
    {
        return new Pdf417CorrectionResult(
            false,
            original,
            0,
            erasureCount,
            new int[0],
            failureCode,
            failureMessage);
    }
}
