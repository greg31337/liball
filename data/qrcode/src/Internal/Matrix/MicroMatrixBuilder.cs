namespace qrcode;

internal class MicroMatrixBuilder
{
    internal static QrMaskPattern SelectMicroMask(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        int requestedValue = (int)requestedMask;
        if (requestedValue < (int)QrMaskPattern.Automatic
            || requestedValue > (int)QrMaskPattern.Three)
        {
            throw new QrInputException(
                "The requested Micro QR mask is not defined.",
                "requestedMask");
        }

        if (requestedMask != QrMaskPattern.Automatic)
        {
            CreateMicroCandidate(
                codewords,
                version,
                errorCorrectionIndex,
                requestedValue);
            return requestedMask;
        }

        int bestMask = 0;
        int bestScore = -1;
        for (int mask = 0; mask < 4; mask++)
        {
            BitMatrixBuffer candidate = CreateMicroCandidate(
                codewords,
                version,
                errorCorrectionIndex,
                mask);
            int score = MaskEvaluator.CalculateMicroScore(candidate);
            if (score > bestScore)
            {
                bestMask = mask;
                bestScore = score;
            }
        }

        return (QrMaskPattern)bestMask;
    }

    internal static BitMatrixBuffer CreateSelectedMicroMatrix(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        QrMaskPattern selectedMask = SelectMicroMask(
            codewords,
            version,
            errorCorrectionIndex,
            requestedMask);
        return CreateMicroCandidate(
            codewords,
            version,
            errorCorrectionIndex,
            (int)selectedMask);
    }

    static BitMatrixBuffer CreateMicroCandidate(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        BitMatrixBuffer matrix = FunctionPatternBuilder.CreateMicroFunctionPatterns(
            version,
            errorCorrectionIndex,
            mask);
        DataModuleWalker.PlaceMicroCodewords(
            codewords,
            version,
            errorCorrectionIndex,
            matrix);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
        MaskEvaluator.ApplyMicroMask(matrix, reserved, mask);
        return matrix;
    }
}
