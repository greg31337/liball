namespace qrcode;

internal class RectangularMicroMatrixBuilder
{
    internal static QrMaskPattern SelectRectangularMicroMask(
        byte[] codewords,
        int versionIndex,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        if (requestedMask != QrMaskPattern.Automatic
            && requestedMask != QrMaskPattern.Four)
        {
            throw new QrInputException(
                "rMQR permits only its fixed mask pattern four.",
                "requestedMask");
        }

        CreateRectangularMicroCandidate(
            codewords,
            versionIndex,
            errorCorrectionIndex);
        return QrMaskPattern.Four;
    }

    internal static BitMatrixBuffer CreateSelectedRectangularMicroMatrix(
        byte[] codewords,
        int versionIndex,
        int errorCorrectionIndex,
        QrMaskPattern requestedMask)
    {
        SelectRectangularMicroMask(
            codewords,
            versionIndex,
            errorCorrectionIndex,
            requestedMask);
        return CreateRectangularMicroCandidate(
            codewords,
            versionIndex,
            errorCorrectionIndex);
    }

    static BitMatrixBuffer CreateRectangularMicroCandidate(
        byte[] codewords,
        int versionIndex,
        int errorCorrectionIndex)
    {
        BitMatrixBuffer matrix = FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(
            versionIndex,
            errorCorrectionIndex);
        DataModuleWalker.PlaceRectangularMicroCodewords(
            codewords,
            versionIndex,
            matrix);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateRectangularMicroReservedMap(
            versionIndex);
        MaskEvaluator.ApplyRectangularMicroMask(matrix, reserved);
        return matrix;
    }
}
