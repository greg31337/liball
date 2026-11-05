namespace qrcode;

internal class MicroCapacityCalculator
{
    internal static QrCapacityResult CalculateCapacity(QrCapacityRequest request)
    {
        ValidateRequest(request);
        int errorCorrectionIndex = (int)request.ErrorCorrectionLevel;
        int modeIndex = GetModeIndex(request.EncodingMode);
        int characterCountBitCount = MicroVersionTable.GetCharacterCountBitWidth(
            request.Version,
            modeIndex);
        int maximumCharacterCount = (1 << characterCountBitCount) - 1;
        if (request.CharacterCount > maximumCharacterCount)
        {
            throw new QrInputException(
                "The character count does not fit the Micro QR count field.",
                "CharacterCount");
        }

        int expectedPayloadBitCount = GetPayloadBitCount(
            request.EncodingMode,
            request.CharacterCount);
        if (request.PayloadBitCount != expectedPayloadBitCount)
        {
            throw new QrInputException(
                "The payload bit count is inconsistent with the mode and character count.",
                "PayloadBitCount");
        }

        int requiredBitCount = CheckedDimensions.CalculateRequiredCapacityBits(
            MicroVersionTable.GetModeIndicatorBitWidth(request.Version),
            characterCountBitCount,
            0,
            request.PayloadBitCount);
        int dataBitCapacity = MicroVersionTable.GetDataBitCapacity(
            request.Version,
            errorCorrectionIndex);
        int dataCodewordCount = MicroVersionTable.GetDataSymbolCharacterCount(
            request.Version,
            errorCorrectionIndex);
        int errorCorrectionCodewordCount = MicroVersionTable.GetEccCodewordCount(
            request.Version,
            errorCorrectionIndex);
        int unusedBitCount = dataBitCapacity - requiredBitCount;
        return new QrCapacityResult
        {
            Fits = unusedBitCount >= 0,
            RequiredBitCount = requiredBitCount,
            DataBitCapacity = dataBitCapacity,
            UnusedBitCount = unusedBitCount,
            DataCodewordCount = dataCodewordCount,
            ErrorCorrectionCodewordCount = errorCorrectionCodewordCount,
            TotalCodewordCount = dataCodewordCount + errorCorrectionCodewordCount,
            RemainderBitCount = 0
        };
    }

    static void ValidateRequest(QrCapacityRequest request)
    {
        if (request == null)
        {
            throw new QrInputException("The capacity request cannot be null.", "request");
        }

        if (request.SymbolKind != QrSymbolKind.Micro)
        {
            throw new QrInputException(
                "The Micro QR capacity calculator requires the Micro profile.",
                "SymbolKind");
        }

        if (request.Version < QrLimits.MicroMinimumVersion
            || request.Version > QrLimits.MicroMaximumVersion)
        {
            throw new QrInputException(
                "The Micro QR version is outside the standardized range.",
                "Version");
        }

        int dimension = MicroVersionTable.GetDimension(request.Version);
        if (request.Width != 0 && request.Width != dimension)
        {
            throw new QrInputException(
                "The requested width does not match the Micro QR version.",
                "Width");
        }

        if (request.Height != 0 && request.Height != dimension)
        {
            throw new QrInputException(
                "The requested height does not match the Micro QR version.",
                "Height");
        }

        int errorCorrectionIndex = (int)request.ErrorCorrectionLevel;
        if (errorCorrectionIndex < MicroVersionTable.DetectionOnly
            || errorCorrectionIndex > EccBlockTable.Quartile
            || !MicroVersionTable.IsErrorCorrectionSupported(
                request.Version,
                errorCorrectionIndex))
        {
            throw new QrInputException(
                "The error-correction level is not supported by the Micro QR version.",
                "ErrorCorrectionLevel");
        }

        int modeIndex = GetModeIndex(request.EncodingMode);
        if (!MicroVersionTable.IsModeSupported(request.Version, modeIndex))
        {
            throw new QrInputException(
                "The encoding mode is not supported by the Micro QR version.",
                "EncodingMode");
        }

        if (request.CharacterCount < 0)
        {
            throw new QrInputException(
                "The character count cannot be negative.",
                "CharacterCount");
        }

        if (request.PayloadBitCount < 0)
        {
            throw new QrInputException(
                "The payload bit count cannot be negative.",
                "PayloadBitCount");
        }

        if (request.ControlBitCount != 0)
        {
            throw new QrInputException(
                "Micro QR does not support additional control segments.",
                "ControlBitCount");
        }
    }

    static int GetModeIndex(QrEncodingMode encodingMode)
    {
        if (encodingMode == QrEncodingMode.Numeric)
        {
            return MicroVersionTable.NumericMode;
        }

        if (encodingMode == QrEncodingMode.Alphanumeric)
        {
            return MicroVersionTable.AlphanumericMode;
        }

        if (encodingMode == QrEncodingMode.Byte)
        {
            return MicroVersionTable.ByteMode;
        }

        if (encodingMode == QrEncodingMode.Kanji)
        {
            return MicroVersionTable.KanjiMode;
        }

        throw new QrInputException(
            "The requested mode is not supported by Micro QR.",
            "EncodingMode");
    }

    static int GetPayloadBitCount(QrEncodingMode encodingMode, int characterCount)
    {
        if (encodingMode == QrEncodingMode.Numeric)
        {
            return NumericEncoder.CalculateBitLength(characterCount);
        }

        if (encodingMode == QrEncodingMode.Alphanumeric)
        {
            return AlphanumericEncoder.CalculateBitLength(characterCount);
        }

        if (encodingMode == QrEncodingMode.Byte)
        {
            return ByteEncoder.CalculateBitLength(characterCount);
        }

        return KanjiEncoder.CalculateBitLength(characterCount);
    }
}
