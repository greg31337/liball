namespace barcode;

internal class DataMatrixCorrectionResult
{
    internal bool Success;
    internal int[] CorrectedCodewords;
    internal int CorrectedErrorCount;
    internal int ErasureCount;
    internal int[] CorrectedIndices;
    internal string? FailureCode;
    internal string? FailureMessage;

    internal DataMatrixCorrectionResult(
        bool success,
        int[] correctedCodewords,
        int correctedErrorCount,
        int erasureCount,
        int[] correctedIndices,
        string? failureCode,
        string? failureMessage)
    {
        Success = success;
        CorrectedCodewords = CopyIntArray(correctedCodewords);
        CorrectedErrorCount = correctedErrorCount;
        ErasureCount = erasureCount;
        CorrectedIndices = CopyIntArray(correctedIndices);
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    internal static int[] CopyIntArray(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}
