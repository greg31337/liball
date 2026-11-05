namespace qrcode;

internal class ErrorCorrectionSelector
{
    internal static QrCapacityRequest SelectParameters(
        QrCapacityRequest request,
        QrEncodeOptions options)
    {
        if (request == null)
        {
            throw new QrInputException("The capacity request cannot be null.", "request");
        }

        if (options == null)
        {
            throw new QrInputException("The encode options cannot be null.", "options");
        }

        int requestedLevel = (int)options.ErrorCorrectionLevel;
        if (requestedLevel < (int)QrErrorCorrectionLevel.None
            || requestedLevel > (int)QrErrorCorrectionLevel.High)
        {
            throw new QrInputException(
                "The requested error-correction level is not defined.",
                "ErrorCorrectionLevel");
        }

        QrCapacityRequest selectionRequest = CreateRequestForLevel(
            request,
            options.ErrorCorrectionLevel);
        QrCapacityRequest selected = SymbolParameterSelector.SelectSmallestFitting(
            selectionRequest,
            options);
        if (options.ForceErrorCorrectionLevel || !options.BoostErrorCorrectionLevel)
        {
            return selected;
        }

        int levelValue = (int)QrErrorCorrectionLevel.High;
        while (levelValue > (int)selected.ErrorCorrectionLevel)
        {
            QrErrorCorrectionLevel candidateLevel = (QrErrorCorrectionLevel)levelValue;
            if (IsLevelSupported(selected, candidateLevel))
            {
                QrCapacityRequest candidate = CreateRequestForLevel(selected, candidateLevel);
                QrCapacityResult capacity = CalculateSelectedCapacity(candidate);
                if (capacity.Fits)
                {
                    return candidate;
                }
            }

            levelValue--;
        }

        return selected;
    }

    static QrCapacityRequest CreateRequestForLevel(
        QrCapacityRequest source,
        QrErrorCorrectionLevel errorCorrectionLevel)
    {
        return new QrCapacityRequest
        {
            SymbolKind = source.SymbolKind,
            Version = source.Version,
            Width = source.Width,
            Height = source.Height,
            ErrorCorrectionLevel = errorCorrectionLevel,
            EncodingMode = source.EncodingMode,
            CharacterCount = source.CharacterCount,
            PayloadBitCount = source.PayloadBitCount,
            ControlBitCount = source.ControlBitCount
        };
    }

    static bool IsLevelSupported(
        QrCapacityRequest selected,
        QrErrorCorrectionLevel errorCorrectionLevel)
    {
        if (selected.SymbolKind == QrSymbolKind.Model2)
        {
            return errorCorrectionLevel >= QrErrorCorrectionLevel.Low
                && errorCorrectionLevel <= QrErrorCorrectionLevel.High;
        }

        if (selected.SymbolKind == QrSymbolKind.Micro)
        {
            int errorCorrectionIndex = (int)errorCorrectionLevel;
            return errorCorrectionIndex >= MicroVersionTable.DetectionOnly
                && errorCorrectionIndex <= EccBlockTable.Quartile
                && MicroVersionTable.IsErrorCorrectionSupported(
                    selected.Version,
                    errorCorrectionIndex);
        }

        if (selected.SymbolKind == QrSymbolKind.rMQR)
        {
            return errorCorrectionLevel == QrErrorCorrectionLevel.Medium
                || errorCorrectionLevel == QrErrorCorrectionLevel.High;
        }

        return false;
    }

    static QrCapacityResult CalculateSelectedCapacity(QrCapacityRequest request)
    {
        if (request.SymbolKind == QrSymbolKind.Model2)
        {
            return Model2CapacityCalculator.CalculateCapacity(request);
        }

        if (request.SymbolKind == QrSymbolKind.Micro)
        {
            return MicroCapacityCalculator.CalculateCapacity(request);
        }

        return RectangularMicroCapacityCalculator.CalculateCapacity(request);
    }
}
