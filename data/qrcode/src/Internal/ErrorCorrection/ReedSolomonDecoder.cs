namespace qrcode;

internal class ReedSolomonDecoder
{
    internal static QrCorrectionResult CorrectCodewords(
        byte[] codewords,
        int errorCorrectionCodewordCount)
    {
        return CorrectCodewordsWithErasures(
            codewords,
            errorCorrectionCodewordCount,
            new int[0]);
    }

    internal static QrCorrectionResult CorrectCodewordsWithErasures(
        byte[] codewords,
        int errorCorrectionCodewordCount,
        int[] erasurePositions)
    {
        int[] syndromes = CalculateSyndromes(codewords, errorCorrectionCodewordCount);
        ValidateErasurePositions(erasurePositions, codewords.Length);
        byte[] original = CopyCodewords(codewords);
        if (erasurePositions.Length > errorCorrectionCodewordCount)
        {
            return CreateFailedResult(original, syndromes);
        }

        if (!ContainsErrorSyndrome(syndromes))
        {
            return CreateSuccessfulResult(original, syndromes, 0, 0);
        }

        int[] forneySyndromes = ErrorLocationSolver.CreateForneySyndromes(
            syndromes,
            erasurePositions,
            codewords.Length);
        int[] errorLocator = ErrorLocationSolver.SolveLocator(forneySyndromes);
        int errorCount = errorLocator.Length - 1;
        if (errorCount * 2 + erasurePositions.Length > errorCorrectionCodewordCount)
        {
            return CreateFailedResult(original, syndromes);
        }

        int[] erasureLocator = ErrorLocationSolver.CreateErasureLocator(
            erasurePositions,
            codewords.Length);
        int[] locator = ErrorLocationSolver.MultiplyLocators(
            errorLocator,
            erasureLocator);
        int totalLocationCount = errorCount + erasurePositions.Length;
        if (totalLocationCount == 0)
        {
            return CreateFailedResult(original, syndromes);
        }

        int[] positions = ErrorLocationSolver.FindErrorPositions(locator, codewords.Length);
        if (positions.Length != totalLocationCount
            || !ContainsEveryErasure(positions, erasurePositions))
        {
            return CreateFailedResult(original, syndromes);
        }

        int[] evaluator = ErrorLocationSolver.CreateEvaluator(syndromes, locator);
        int[] magnitudes = ErrorLocationSolver.CreateMagnitudes(
            locator,
            evaluator,
            positions,
            codewords.Length);
        if (magnitudes.Length != totalLocationCount)
        {
            return CreateFailedResult(original, syndromes);
        }

        byte[] repaired = CopyCodewords(codewords);
        for (int index = 0; index < positions.Length; index++)
        {
            repaired[positions[index]] = (byte)(repaired[positions[index]] ^ magnitudes[index]);
        }

        int[] verificationSyndromes = CalculateSyndromes(
            repaired,
            errorCorrectionCodewordCount);
        if (ContainsErrorSyndrome(verificationSyndromes))
        {
            return CreateFailedResult(original, syndromes);
        }

        return CreateSuccessfulResult(
            repaired,
            syndromes,
            errorCount,
            erasurePositions.Length);
    }

    internal static int[] CalculateSyndromes(
        byte[] codewords,
        int errorCorrectionCodewordCount)
    {
        ValidateBlock(codewords, errorCorrectionCodewordCount);
        int[] syndromes = new int[errorCorrectionCodewordCount];
        for (int syndromeIndex = 0;
            syndromeIndex < errorCorrectionCodewordCount;
            syndromeIndex++)
        {
            int evaluationPoint = GaloisField256.GetExponent(syndromeIndex);
            int syndrome = 0;
            for (int codewordIndex = 0; codewordIndex < codewords.Length; codewordIndex++)
            {
                syndrome = GaloisField256.Add(
                    GaloisField256.Multiply(syndrome, evaluationPoint),
                    codewords[codewordIndex]);
            }

            syndromes[syndromeIndex] = syndrome;
        }

        return syndromes;
    }

    static QrCorrectionResult CreateSuccessfulResult(
        byte[] codewords,
        int[] syndromes,
        int correctedErrorCount,
        int correctedErasureCount)
    {
        return new QrCorrectionResult
        {
            Success = true,
            FailureReason = QrFailureReason.None,
            Codewords = codewords,
            ErrorCorrectionCodewordCount = syndromes.Length,
            CorrectedErrorCount = correctedErrorCount,
            CorrectedErasureCount = correctedErasureCount,
            Syndromes = syndromes,
            Verified = true
        };
    }

    static QrCorrectionResult CreateFailedResult(byte[] codewords, int[] syndromes)
    {
        return new QrCorrectionResult
        {
            Success = false,
            FailureReason = QrFailureReason.ChecksumFailure,
            Codewords = codewords,
            ErrorCorrectionCodewordCount = syndromes.Length,
            CorrectedErrorCount = 0,
            CorrectedErasureCount = 0,
            Syndromes = syndromes,
            Verified = false
        };
    }

    static bool ContainsErrorSyndrome(int[] syndromes)
    {
        for (int index = 0; index < syndromes.Length; index++)
        {
            if (syndromes[index] != 0)
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsEveryErasure(int[] positions, int[] erasurePositions)
    {
        for (int erasureIndex = 0; erasureIndex < erasurePositions.Length; erasureIndex++)
        {
            bool found = false;
            for (int positionIndex = 0; positionIndex < positions.Length; positionIndex++)
            {
                if (positions[positionIndex] == erasurePositions[erasureIndex])
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    static byte[] CopyCodewords(byte[] codewords)
    {
        byte[] copy = new byte[codewords.Length];
        for (int index = 0; index < codewords.Length; index++)
        {
            copy[index] = codewords[index];
        }

        return copy;
    }

    static void ValidateBlock(byte[] codewords, int errorCorrectionCodewordCount)
    {
        if (codewords == null)
        {
            throw new QrInputException("The codewords cannot be null.", "codewords");
        }

        if (errorCorrectionCodewordCount <= 0
            || errorCorrectionCodewordCount > GaloisField256.MultiplicativeOrder)
        {
            throw new QrInputException(
                "The error-correction codeword count must be between one and 255.",
                "errorCorrectionCodewordCount");
        }

        if (codewords.Length < errorCorrectionCodewordCount)
        {
            throw new QrInputException(
                "The codeword block is shorter than its error-correction region.",
                "codewords");
        }

        if (codewords.Length > GaloisField256.MultiplicativeOrder)
        {
            throw new QrResourceLimitException(
                "A Reed-Solomon block cannot exceed 255 codewords.",
                "ReedSolomonBlockLength",
                codewords.Length,
                GaloisField256.MultiplicativeOrder);
        }
    }

    static void ValidateErasurePositions(int[] erasurePositions, int codewordCount)
    {
        if (erasurePositions == null)
        {
            throw new QrInputException(
                "The erasure positions cannot be null.",
                "erasurePositions");
        }

        bool[] seen = new bool[codewordCount];
        for (int index = 0; index < erasurePositions.Length; index++)
        {
            int position = erasurePositions[index];
            if (position < 0 || position >= codewordCount)
            {
                throw new QrInputException(
                    "An erasure position is outside the codeword block.",
                    "erasurePositions");
            }

            if (seen[position])
            {
                throw new QrInputException(
                    "Erasure positions must be unique.",
                    "erasurePositions");
            }

            seen[position] = true;
        }
    }
}
