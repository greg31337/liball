namespace barcode;

internal static class DataMatrixReedSolomon
{
    internal static bool IsSupportedErrorCorrectionCount(int errorCorrectionCount)
    {
        switch (errorCorrectionCount)
        {
            case 5:
            case 7:
            case 10:
            case 11:
            case 12:
            case 14:
            case 18:
            case 20:
            case 24:
            case 28:
            case 36:
            case 42:
            case 48:
            case 56:
            case 62:
            case 68:
                return true;
            default:
                return false;
        }
    }

    internal static GaloisPolynomial256 BuildGenerator(int errorCorrectionCount)
    {
        ValidateErrorCorrectionCount(errorCorrectionCount);
        GaloisField256 field = GaloisField256.DataMatrix;
        GaloisPolynomial256 generator = field.CreateOnePolynomial();
        int index = 0;
        while (index < errorCorrectionCount)
        {
            int root = field.Exponent(index + GaloisField256.GeneratorBase);
            GaloisPolynomial256 factor = new GaloisPolynomial256(new int[] { 1, root });
            generator = generator.MultiplyPolynomial(factor);
            index = index + 1;
        }

        return generator;
    }

    internal static int[] EncodeParity(int[] dataCodewords, int errorCorrectionCount)
    {
        ValidateDataBlock(dataCodewords, errorCorrectionCount);
        GaloisPolynomial256 generator = BuildGenerator(errorCorrectionCount);
        GaloisPolynomial256 data = new GaloisPolynomial256(dataCodewords);
        GaloisPolynomial256 shifted = data.MultiplyByMonomial(errorCorrectionCount, 1);
        GaloisPolynomial256[] division = shifted.DivideByPolynomial(generator);
        int[] remainder = division[1].CopyCoefficients();
        int[] parity = new int[errorCorrectionCount];
        int offset = errorCorrectionCount - remainder.Length;
        int index = 0;
        while (index < remainder.Length)
        {
            parity[offset + index] = remainder[index];
            index = index + 1;
        }

        return parity;
    }

    internal static int[] EncodeCodewords(int[] dataCodewords, int errorCorrectionCount)
    {
        int[] parity = EncodeParity(dataCodewords, errorCorrectionCount);
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

    internal static int[] CalculateSyndromes(int[] receivedCodewords, int errorCorrectionCount)
    {
        ValidateReceivedBlock(receivedCodewords, errorCorrectionCount);
        GaloisField256 field = GaloisField256.DataMatrix;
        GaloisPolynomial256 received = new GaloisPolynomial256(receivedCodewords);
        int[] syndromes = new int[errorCorrectionCount];
        int index = 0;
        while (index < errorCorrectionCount)
        {
            int evaluationPoint = field.Exponent(index + GaloisField256.GeneratorBase);
            syndromes[index] = received.EvaluateAt(evaluationPoint);
            index = index + 1;
        }

        return syndromes;
    }

    internal static bool VerifyCodewords(int[] receivedCodewords, int errorCorrectionCount)
    {
        int[] syndromes = CalculateSyndromes(receivedCodewords, errorCorrectionCount);
        return AreSyndromesZero(syndromes);
    }

    internal static DataMatrixCorrectionResult CorrectCodewords(
        int[] receivedCodewords,
        int errorCorrectionCount,
        int[]? erasureIndices)
    {
        ValidateReceivedBlock(receivedCodewords, errorCorrectionCount);
        int[] erasures = ValidateAndCopyErasures(erasureIndices, receivedCodewords.Length);
        int[] original = DataMatrixCorrectionResult.CopyIntArray(receivedCodewords);
        if (erasures.Length > errorCorrectionCount)
        {
            return CreateFailureResult(original, erasures.Length, "TOO_MANY_ERASURES", "The number of erasures exceeds the available error-correction codewords.");
        }

        int[] syndromes = CalculateSyndromes(receivedCodewords, errorCorrectionCount);
        if (AreSyndromesZero(syndromes))
        {
            return CreateSuccessResult(original, erasures.Length, new int[0]);
        }

        int[] forneySyndromes = CalculateForneySyndromes(syndromes, erasures, receivedCodewords.Length);
        int[] unknownLocator = FindErrorLocator(forneySyndromes);
        int unknownErrorCount = unknownLocator.Length - 1;
        if ((unknownErrorCount * 2) + erasures.Length > errorCorrectionCount)
        {
            return CreateFailureResult(original, erasures.Length, "ERROR_CAPACITY_EXCEEDED", "The errors and erasures exceed the correction capacity.");
        }

        int[] unknownIndices = FindErrorIndices(unknownLocator, receivedCodewords.Length);
        if (unknownIndices.Length != unknownErrorCount)
        {
            return CreateFailureResult(original, erasures.Length, "ERROR_LOCATIONS_NOT_FOUND", "The error locator roots do not identify the expected number of codeword positions.");
        }

        int[] allIndices = CombineErrataIndices(erasures, unknownIndices);
        if (allIndices.Length != erasures.Length + unknownIndices.Length)
        {
            return CreateFailureResult(original, erasures.Length, "DUPLICATE_ERROR_LOCATION", "An error location duplicates a supplied erasure position.");
        }

        int[]? magnitudes = SolveErrorMagnitudes(syndromes, allIndices, receivedCodewords.Length);
        if (magnitudes == null)
        {
            return CreateFailureResult(original, erasures.Length, "MAGNITUDE_SOLVE_FAILED", "The error magnitudes could not be solved.");
        }

        int[] corrected = DataMatrixCorrectionResult.CopyIntArray(receivedCodewords);
        int[] changedBuffer = new int[allIndices.Length];
        int changedCount = 0;
        int index = 0;
        while (index < allIndices.Length)
        {
            if (magnitudes[index] != 0)
            {
                int position = allIndices[index];
                corrected[position] = GaloisField256.AddOrSubtract(corrected[position], magnitudes[index]);
                changedBuffer[changedCount] = position;
                changedCount = changedCount + 1;
            }

            index = index + 1;
        }

        if (!VerifyCodewords(corrected, errorCorrectionCount))
        {
            return CreateFailureResult(original, erasures.Length, "SYNDROME_REMAINS", "The corrected block still contains a nonzero syndrome.");
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
        int[] reduced = DataMatrixCorrectionResult.CopyIntArray(syndromes);
        GaloisField256 field = GaloisField256.DataMatrix;
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
                next[syndromeIndex] = GaloisField256.AddOrSubtract(
                    field.Multiply(reduced[syndromeIndex], location),
                    reduced[syndromeIndex + 1]);
                syndromeIndex = syndromeIndex + 1;
            }

            reduced = next;
            erasureIndex = erasureIndex + 1;
        }

        return reduced;
    }

    internal static int[] FindErrorLocator(int[] syndromes)
    {
        GaloisField256 field = GaloisField256.DataMatrix;
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
                discrepancy = GaloisField256.AddOrSubtract(discrepancy, term);
                coefficientIndex = coefficientIndex + 1;
            }

            if (discrepancy == 0)
            {
                shift = shift + 1;
            }
            else
            {
                int[] locatorBeforeUpdate = DataMatrixCorrectionResult.CopyIntArray(locator);
                int scale = field.DivideElements(discrepancy, previousDiscrepancy);
                int previousIndex = 0;
                while (previousIndex + shift < locator.Length)
                {
                    int term = field.Multiply(scale, previous[previousIndex]);
                    locator[previousIndex + shift] = GaloisField256.AddOrSubtract(locator[previousIndex + shift], term);
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

    internal static int[] FindErrorIndices(int[] locator, int receivedLength)
    {
        int expectedCount = locator.Length - 1;
        int[] indices = new int[expectedCount];
        int found = 0;
        GaloisField256 field = GaloisField256.DataMatrix;
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

    internal static int EvaluateAscendingPolynomial(int[] coefficients, int element)
    {
        GaloisField256 field = GaloisField256.DataMatrix;
        int result = 0;
        int power = 1;
        int index = 0;
        while (index < coefficients.Length)
        {
            result = GaloisField256.AddOrSubtract(result, field.Multiply(coefficients[index], power));
            power = field.Multiply(power, element);
            index = index + 1;
        }

        return result;
    }

    internal static int[]? SolveErrorMagnitudes(int[] syndromes, int[] errorIndices, int receivedLength)
    {
        int count = errorIndices.Length;
        if (count == 0)
        {
            return new int[0];
        }

        GaloisField256 field = GaloisField256.DataMatrix;
        int[][] matrix = new int[count][];
        int row = 0;
        while (row < count)
        {
            matrix[row] = new int[CheckedMath.Add(count, 1)];
            int column = 0;
            while (column < count)
            {
                int degree = receivedLength - 1 - errorIndices[column];
                int location = field.Exponent(degree);
                matrix[row][column] = field.Power(location, row + GaloisField256.GeneratorBase);
                column = column + 1;
            }

            matrix[row][count] = syndromes[row];
            row = row + 1;
        }

        int pivotColumn = 0;
        while (pivotColumn < count)
        {
            int pivotRow = pivotColumn;
            while (pivotRow < count && matrix[pivotRow][pivotColumn] == 0)
            {
                pivotRow = pivotRow + 1;
            }

            if (pivotRow == count)
            {
                return null;
            }

            if (pivotRow != pivotColumn)
            {
                int[] temporary = matrix[pivotRow];
                matrix[pivotRow] = matrix[pivotColumn];
                matrix[pivotColumn] = temporary;
            }

            int inversePivot = field.Inverse(matrix[pivotColumn][pivotColumn]);
            int scaleColumn = pivotColumn;
            while (scaleColumn <= count)
            {
                matrix[pivotColumn][scaleColumn] = field.Multiply(matrix[pivotColumn][scaleColumn], inversePivot);
                scaleColumn = scaleColumn + 1;
            }

            row = 0;
            while (row < count)
            {
                if (row != pivotColumn)
                {
                    int factor = matrix[row][pivotColumn];
                    if (factor != 0)
                    {
                        int eliminationColumn = pivotColumn;
                        while (eliminationColumn <= count)
                        {
                            int term = field.Multiply(factor, matrix[pivotColumn][eliminationColumn]);
                            matrix[row][eliminationColumn] = GaloisField256.AddOrSubtract(matrix[row][eliminationColumn], term);
                            eliminationColumn = eliminationColumn + 1;
                        }
                    }
                }

                row = row + 1;
            }

            pivotColumn = pivotColumn + 1;
        }

        int[] magnitudes = new int[count];
        row = 0;
        while (row < count)
        {
            magnitudes[row] = matrix[row][count];
            row = row + 1;
        }

        return magnitudes;
    }

    internal static int[] CombineErrataIndices(int[] erasures, int[] errors)
    {
        int[] combined = new int[CheckedMath.Add(erasures.Length, errors.Length)];
        int count = 0;
        int index = 0;
        while (index < erasures.Length)
        {
            combined[count] = erasures[index];
            count = count + 1;
            index = index + 1;
        }

        index = 0;
        while (index < errors.Length)
        {
            int candidate = errors[index];
            bool duplicate = false;
            int existing = 0;
            while (existing < count)
            {
                if (combined[existing] == candidate)
                {
                    duplicate = true;
                    break;
                }

                existing = existing + 1;
            }

            if (!duplicate)
            {
                combined[count] = candidate;
                count = count + 1;
            }

            index = index + 1;
        }

        if (count == combined.Length)
        {
            return combined;
        }

        int[] unique = new int[count];
        index = 0;
        while (index < count)
        {
            unique[index] = combined[index];
            index = index + 1;
        }

        return unique;
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

        int[] copy = DataMatrixCorrectionResult.CopyIntArray(erasureIndices);
        int index = 0;
        while (index < copy.Length)
        {
            CheckedMath.ValidateIndex(copy[index], receivedLength);
            int previous = 0;
            while (previous < index)
            {
                if (copy[previous] == copy[index])
                {
                    throw new BarcodeValidationException("Data Matrix erasure indices must be unique.");
                }

                previous = previous + 1;
            }

            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateDataBlock(int[] dataCodewords, int errorCorrectionCount)
    {
        ValidateErrorCorrectionCount(errorCorrectionCount);
        if (dataCodewords == null)
        {
            throw new BarcodeValidationException("Data Matrix data codewords must not be null.");
        }

        if (dataCodewords.Length == 0)
        {
            throw new BarcodeValidationException("Data Matrix data block must not be empty.");
        }

        int totalLength = CheckedMath.Add(dataCodewords.Length, errorCorrectionCount);
        if (totalLength > GaloisField256.MultiplicativeOrder)
        {
            throw new BarcodeValidationException("Data Matrix Reed-Solomon block must not exceed 255 codewords.");
        }

        ValidateCodewordValues(dataCodewords);
    }

    internal static void ValidateReceivedBlock(int[] receivedCodewords, int errorCorrectionCount)
    {
        ValidateErrorCorrectionCount(errorCorrectionCount);
        if (receivedCodewords == null)
        {
            throw new BarcodeValidationException("Data Matrix received codewords must not be null.");
        }

        if (receivedCodewords.Length <= errorCorrectionCount || receivedCodewords.Length > GaloisField256.MultiplicativeOrder)
        {
            throw new BarcodeValidationException("Data Matrix received block length is incompatible with the error-correction count.");
        }

        ValidateCodewordValues(receivedCodewords);
    }

    internal static void ValidateCodewordValues(int[] codewords)
    {
        int index = 0;
        while (index < codewords.Length)
        {
            GaloisField256.ValidateElement(codewords[index]);
            index = index + 1;
        }
    }

    internal static void ValidateErrorCorrectionCount(int errorCorrectionCount)
    {
        if (!IsSupportedErrorCorrectionCount(errorCorrectionCount))
        {
            throw new BarcodeValidationException("Data Matrix error-correction count is not a supported ECC 200 block size.");
        }
    }

    internal static DataMatrixCorrectionResult CreateSuccessResult(int[] corrected, int erasureCount, int[] changedIndices)
    {
        return new DataMatrixCorrectionResult(
            true,
            corrected,
            changedIndices.Length,
            erasureCount,
            changedIndices,
            null,
            null);
    }

    internal static DataMatrixCorrectionResult CreateFailureResult(
        int[] original,
        int erasureCount,
        string failureCode,
        string failureMessage)
    {
        return new DataMatrixCorrectionResult(
            false,
            original,
            0,
            erasureCount,
            new int[0],
            failureCode,
            failureMessage);
    }
}
