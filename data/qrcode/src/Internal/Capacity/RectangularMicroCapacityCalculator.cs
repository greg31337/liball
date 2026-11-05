namespace qrcode;

internal class RectangularMicroCapacityCalculator
{
    internal static QrCapacityResult CalculateCapacity(QrCapacityRequest request)
    {
        int versionIndex = ValidateRequest(request);
        int errorCorrectionIndex = GetErrorCorrectionIndex(request.ErrorCorrectionLevel);
        int modeIndex = GetModeIndex(request.EncodingMode);
        int characterCountBitCount = RectangularMicroVersionTable.GetCharacterCountBitWidth(
            versionIndex,
            modeIndex);
        int maximumCharacterCount = (1 << characterCountBitCount) - 1;
        if (request.CharacterCount > maximumCharacterCount)
        {
            throw new QrInputException(
                "The character count does not fit the rMQR count field.",
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
            RectangularMicroVersionTable.ModeIndicatorBitWidth,
            characterCountBitCount,
            request.ControlBitCount,
            request.PayloadBitCount);
        int dataCodewordCount = RectangularMicroVersionTable.GetDataCodewordCount(
            versionIndex,
            errorCorrectionIndex);
        int totalCodewordCount = RectangularMicroVersionTable.GetTotalCodewordCount(versionIndex);
        int dataBitCapacity = RectangularMicroVersionTable.GetDataBitCapacity(
            versionIndex,
            errorCorrectionIndex);
        int unusedBitCount = dataBitCapacity - requiredBitCount;
        return new QrCapacityResult
        {
            Fits = unusedBitCount >= 0,
            RequiredBitCount = requiredBitCount,
            DataBitCapacity = dataBitCapacity,
            UnusedBitCount = unusedBitCount,
            DataCodewordCount = dataCodewordCount,
            ErrorCorrectionCodewordCount = totalCodewordCount - dataCodewordCount,
            TotalCodewordCount = totalCodewordCount,
            RemainderBitCount = RectangularMicroVersionTable.GetRemainderBitCount(versionIndex)
        };
    }

    static int ValidateRequest(QrCapacityRequest request)
    {
        if (request == null)
        {
            throw new QrInputException("The capacity request cannot be null.", "request");
        }

        if (request.SymbolKind != QrSymbolKind.rMQR)
        {
            throw new QrInputException(
                "The rMQR capacity calculator requires the rMQR profile.",
                "SymbolKind");
        }

        if (request.Version != 0)
        {
            throw new QrInputException(
                "rMQR sizes are selected by width and height instead of a numbered version.",
                "Version");
        }

        int versionIndex = ResolveVersionIndex(request.Height, request.Width);
        GetErrorCorrectionIndex(request.ErrorCorrectionLevel);
        GetModeIndex(request.EncodingMode);
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

        if (request.ControlBitCount < 0)
        {
            throw new QrInputException(
                "The control bit count cannot be negative.",
                "ControlBitCount");
        }

        return versionIndex;
    }

    static int ResolveVersionIndex(int height, int width)
    {
        for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
            versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
            versionIndex++)
        {
            if (RectangularMicroVersionTable.GetHeight(versionIndex) == height
                && RectangularMicroVersionTable.GetWidth(versionIndex) == width)
            {
                return versionIndex;
            }
        }

        throw new QrInputException(
            "The dimensions do not identify a standardized rMQR size.",
            "Height");
    }

    static int GetErrorCorrectionIndex(QrErrorCorrectionLevel errorCorrectionLevel)
    {
        if (errorCorrectionLevel == QrErrorCorrectionLevel.Medium)
        {
            return RectangularMicroVersionTable.Medium;
        }

        if (errorCorrectionLevel == QrErrorCorrectionLevel.High)
        {
            return RectangularMicroVersionTable.High;
        }

        throw new QrInputException(
            "rMQR supports only Medium and High error correction.",
            "ErrorCorrectionLevel");
    }

    static int GetModeIndex(QrEncodingMode encodingMode)
    {
        if (encodingMode == QrEncodingMode.Numeric)
        {
            return RectangularMicroVersionTable.NumericMode;
        }

        if (encodingMode == QrEncodingMode.Alphanumeric)
        {
            return RectangularMicroVersionTable.AlphanumericMode;
        }

        if (encodingMode == QrEncodingMode.Byte)
        {
            return RectangularMicroVersionTable.ByteMode;
        }

        if (encodingMode == QrEncodingMode.Kanji)
        {
            return RectangularMicroVersionTable.KanjiMode;
        }

        throw new QrInputException(
            "The requested mode does not carry an rMQR character-count field.",
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
