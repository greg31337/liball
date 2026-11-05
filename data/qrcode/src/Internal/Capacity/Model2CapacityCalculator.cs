namespace qrcode;

internal class Model2CapacityCalculator
{
    internal static QrCapacityResult CalculateCapacity(QrCapacityRequest request)
    {
        ValidateRequest(request);
        int errorCorrectionIndex = GetErrorCorrectionIndex(request.ErrorCorrectionLevel);
        int modeIndex = GetModeIndex(request.EncodingMode);
        int characterCountBitCount = Model2VersionTable.GetCharacterCountBitWidth(
            modeIndex,
            request.Version);
        int maximumCharacterCount = (1 << characterCountBitCount) - 1;
        if (request.CharacterCount > maximumCharacterCount)
        {
            throw new QrInputException(
                "The character count does not fit the Model 2 count field.",
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

        int inherentControlBitCount = 0;
        if (request.EncodingMode == QrEncodingMode.Hanzi)
        {
            inherentControlBitCount = HanziEncoder.SubsetIndicatorBitCount;
        }

        int controlBitCount = CheckedDimensions.CalculateSum(
            request.ControlBitCount,
            inherentControlBitCount,
            int.MaxValue,
            "CapacityBitCount");
        int requiredBitCount = CheckedDimensions.CalculateRequiredCapacityBits(
            4,
            characterCountBitCount,
            controlBitCount,
            request.PayloadBitCount);
        int dataCodewordCount = EccBlockTable.GetDataCodewordCount(
            request.Version,
            errorCorrectionIndex);
        int totalCodewordCount = Model2VersionTable.GetTotalCodewordCount(request.Version);
        int dataBitCapacity = CheckedDimensions.CalculateProduct(
            dataCodewordCount,
            8,
            int.MaxValue,
            "CapacityBitCount");
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
            RemainderBitCount = Model2VersionTable.GetRemainderBitCount(request.Version)
        };
    }

    static void ValidateRequest(QrCapacityRequest request)
    {
        if (request == null)
        {
            throw new QrInputException("The capacity request cannot be null.", "request");
        }

        if (request.SymbolKind != QrSymbolKind.Model2)
        {
            throw new QrInputException(
                "The Model 2 capacity calculator requires the Model 2 profile.",
                "SymbolKind");
        }

        if (request.Version < QrLimits.Model2MinimumVersion
            || request.Version > QrLimits.Model2MaximumVersion)
        {
            throw new QrInputException(
                "The Model 2 version is outside the standardized range.",
                "Version");
        }

        int dimension = Model2VersionTable.GetDimension(request.Version);
        if (request.Width != 0 && request.Width != dimension)
        {
            throw new QrInputException(
                "The requested width does not match the Model 2 version.",
                "Width");
        }

        if (request.Height != 0 && request.Height != dimension)
        {
            throw new QrInputException(
                "The requested height does not match the Model 2 version.",
                "Height");
        }

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
    }

    static int GetErrorCorrectionIndex(QrErrorCorrectionLevel errorCorrectionLevel)
    {
        int index = (int)errorCorrectionLevel;
        if (index < EccBlockTable.Low || index > EccBlockTable.High)
        {
            throw new QrInputException(
                "Model 2 requires a defined error-correction level.",
                "ErrorCorrectionLevel");
        }

        return index;
    }

    static int GetModeIndex(QrEncodingMode encodingMode)
    {
        if (encodingMode == QrEncodingMode.Numeric)
        {
            return 0;
        }

        if (encodingMode == QrEncodingMode.Alphanumeric)
        {
            return 1;
        }

        if (encodingMode == QrEncodingMode.Byte)
        {
            return 2;
        }

        if (encodingMode == QrEncodingMode.Kanji || encodingMode == QrEncodingMode.Hanzi)
        {
            return 3;
        }

        throw new QrInputException(
            "The requested mode does not carry a Model 2 character-count field.",
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

        if (encodingMode == QrEncodingMode.Kanji)
        {
            return KanjiEncoder.CalculateBitLength(characterCount);
        }

        return HanziEncoder.CalculateBitLength(characterCount);
    }
}
