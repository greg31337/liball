namespace qrcode;

internal class Model2MatrixBuilder
{
    internal static QrMaskPattern SelectModel2Mask(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        int requestedValue = (int)requestedMask;
        if (requestedValue < (int)QrMaskPattern.Automatic
            || requestedValue > (int)QrMaskPattern.Seven)
        {
            throw new QrInputException(
                "The requested Model 2 mask is not defined.",
                "requestedMask");
        }

        if (requestedMask != QrMaskPattern.Automatic)
        {
            CreateModel2Candidate(
                codewords,
                version,
                errorCorrectionIndex,
                requestedValue);
            return requestedMask;
        }

        int bestMask = 0;
        int bestPenalty = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            BitMatrixBuffer candidate = CreateModel2Candidate(
                codewords,
                version,
                errorCorrectionIndex,
                mask);
            int penalty = MaskEvaluator.CalculateModel2Penalty(candidate);
            if (penalty < bestPenalty)
            {
                bestMask = mask;
                bestPenalty = penalty;
            }
        }

        return (QrMaskPattern)bestMask;
    }

    internal static BitMatrixBuffer CreateSelectedModel2Matrix(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        QrMaskPattern selectedMask = SelectModel2Mask(
            codewords,
            version,
            errorCorrectionIndex,
            requestedMask);
        return CreateModel2Candidate(
            codewords,
            version,
            errorCorrectionIndex,
            (int)selectedMask);
    }

    static BitMatrixBuffer CreateModel2Candidate(
        byte[] codewords,
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        BitMatrixBuffer matrix = FunctionPatternBuilder.CreateModel2FunctionPatterns(
            version,
            errorCorrectionIndex,
            mask);
        DataModuleWalker.PlaceModel2Codewords(codewords, version, matrix);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        MaskEvaluator.ApplyModel2Mask(matrix, reserved, mask);
        return matrix;
    }
}
