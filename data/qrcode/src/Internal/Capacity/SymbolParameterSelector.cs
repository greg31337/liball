namespace qrcode;

internal class SymbolParameterSelector
{
    internal static QrCapacityRequest SelectSmallestFitting(
        QrCapacityRequest request,
        QrEncodeOptions options)
    {
        ValidateInputs(request, options);
        SymbolSelectionState state = new SymbolSelectionState();
        int modeIndex = GetModeIndex(request.EncodingMode);

        if (IsFamilyEnabled(QrSymbolKind.Model2, options))
        {
            int minimumVersion = options.MinimumVersion;
            if (minimumVersion < QrLimits.Model2MinimumVersion)
            {
                minimumVersion = QrLimits.Model2MinimumVersion;
            }

            int maximumVersion = options.MaximumVersion;
            if (maximumVersion > QrLimits.Model2MaximumVersion)
            {
                maximumVersion = QrLimits.Model2MaximumVersion;
            }

            if (request.ErrorCorrectionLevel != QrErrorCorrectionLevel.None)
            {
                for (int version = minimumVersion; version <= maximumVersion; version++)
                {
                    int dimension = Model2VersionTable.GetDimension(version);
                    if (!AreDimensionsAllowed(dimension, dimension, options))
                    {
                        continue;
                    }

                    int countWidth = Model2VersionTable.GetCharacterCountBitWidth(
                        modeIndex,
                        version);
                    if (!DoesCharacterCountFit(request.CharacterCount, countWidth))
                    {
                        continue;
                    }

                    QrCapacityRequest candidate = CreateCandidate(
                        request,
                        QrSymbolKind.Model2,
                        version,
                        dimension,
                        dimension);
                    QrCapacityResult capacity = Model2CapacityCalculator.CalculateCapacity(candidate);
                    ConsiderCandidate(state, candidate, capacity);
                }
            }
        }

        if (IsFamilyEnabled(QrSymbolKind.Micro, options)
            && options.MaskPattern <= QrMaskPattern.Three
            && request.EncodingMode != QrEncodingMode.Hanzi
            && request.ControlBitCount == 0)
        {
            int minimumVersion = options.MinimumVersion;
            if (minimumVersion < QrLimits.MicroMinimumVersion)
            {
                minimumVersion = QrLimits.MicroMinimumVersion;
            }

            int maximumVersion = options.MaximumVersion;
            if (maximumVersion > QrLimits.MicroMaximumVersion)
            {
                maximumVersion = QrLimits.MicroMaximumVersion;
            }

            int errorCorrectionIndex = (int)request.ErrorCorrectionLevel;
            for (int version = minimumVersion; version <= maximumVersion; version++)
            {
                if (errorCorrectionIndex < MicroVersionTable.DetectionOnly
                    || errorCorrectionIndex > EccBlockTable.Quartile
                    || !MicroVersionTable.IsErrorCorrectionSupported(version, errorCorrectionIndex)
                    || !MicroVersionTable.IsModeSupported(version, modeIndex))
                {
                    continue;
                }

                int dimension = MicroVersionTable.GetDimension(version);
                if (!AreDimensionsAllowed(dimension, dimension, options))
                {
                    continue;
                }

                int countWidth = MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
                if (!DoesCharacterCountFit(request.CharacterCount, countWidth))
                {
                    continue;
                }

                QrCapacityRequest candidate = CreateCandidate(
                    request,
                    QrSymbolKind.Micro,
                    version,
                    dimension,
                    dimension);
                QrCapacityResult capacity = MicroCapacityCalculator.CalculateCapacity(candidate);
                ConsiderCandidate(state, candidate, capacity);
            }
        }

        if (IsFamilyEnabled(QrSymbolKind.rMQR, options)
            && (options.MaskPattern == QrMaskPattern.Automatic
                || options.MaskPattern == QrMaskPattern.Four)
            && request.EncodingMode != QrEncodingMode.Hanzi
            && (request.ErrorCorrectionLevel == QrErrorCorrectionLevel.Medium
                || request.ErrorCorrectionLevel == QrErrorCorrectionLevel.High))
        {
            for (int versionIndex = RectangularMicroVersionTable.MinimumVersionIndex;
                versionIndex <= RectangularMicroVersionTable.MaximumVersionIndex;
                versionIndex++)
            {
                int height = RectangularMicroVersionTable.GetHeight(versionIndex);
                int width = RectangularMicroVersionTable.GetWidth(versionIndex);
                if (!AreDimensionsAllowed(width, height, options))
                {
                    continue;
                }

                int countWidth = RectangularMicroVersionTable.GetCharacterCountBitWidth(
                    versionIndex,
                    modeIndex);
                if (!DoesCharacterCountFit(request.CharacterCount, countWidth))
                {
                    continue;
                }

                QrCapacityRequest candidate = CreateCandidate(
                    request,
                    QrSymbolKind.rMQR,
                    0,
                    width,
                    height);
                QrCapacityResult capacity =
                    RectangularMicroCapacityCalculator.CalculateCapacity(candidate);
                ConsiderCandidate(state, candidate, capacity);
            }
        }

        if (state.BestRequest != null)
        {
            return state.BestRequest;
        }

        QrSymbolKind failureKind = request.SymbolKind;
        int requiredBitCount = CheckedDimensions.CalculateSum(
            request.PayloadBitCount,
            request.ControlBitCount,
            int.MaxValue,
            "CapacityBitCount");
        int availableBitCount = 0;
        if (state.LargestCapacity != null)
        {
            failureKind = state.LargestKind;
            requiredBitCount = state.LargestCapacity.RequiredBitCount;
            availableBitCount = state.LargestCapacity.DataBitCapacity;
        }

        throw new QrCapacityException(
            "The payload does not fit any permitted symbol parameters.",
            failureKind,
            requiredBitCount,
            availableBitCount);
    }

    static void ValidateInputs(QrCapacityRequest request, QrEncodeOptions options)
    {
        if (request == null)
        {
            throw new QrInputException("The capacity request cannot be null.", "request");
        }

        if (options == null)
        {
            throw new QrInputException("The encode options cannot be null.", "options");
        }

        if (request.CharacterCount < 0)
        {
            throw new QrInputException("The character count cannot be negative.", "CharacterCount");
        }

        if (request.PayloadBitCount < 0)
        {
            throw new QrInputException("The payload bit count cannot be negative.", "PayloadBitCount");
        }

        if (request.ControlBitCount < 0)
        {
            throw new QrInputException("The control bit count cannot be negative.", "ControlBitCount");
        }

        GetModeIndex(request.EncodingMode);
        ValidatePayloadBitCount(request);
        int errorCorrectionValue = (int)request.ErrorCorrectionLevel;
        if (errorCorrectionValue < (int)QrErrorCorrectionLevel.None
            || errorCorrectionValue > (int)QrErrorCorrectionLevel.High)
        {
            throw new QrInputException(
                "The error-correction level is not defined.",
                "ErrorCorrectionLevel");
        }

        ValidateOptions(options);
    }

    static void ValidatePayloadBitCount(QrCapacityRequest request)
    {
        int expectedBitCount;
        if (request.EncodingMode == QrEncodingMode.Numeric)
        {
            expectedBitCount = NumericEncoder.CalculateBitLength(request.CharacterCount);
        }
        else if (request.EncodingMode == QrEncodingMode.Alphanumeric)
        {
            expectedBitCount = AlphanumericEncoder.CalculateBitLength(request.CharacterCount);
        }
        else if (request.EncodingMode == QrEncodingMode.Byte)
        {
            expectedBitCount = ByteEncoder.CalculateBitLength(request.CharacterCount);
        }
        else if (request.EncodingMode == QrEncodingMode.Kanji)
        {
            expectedBitCount = KanjiEncoder.CalculateBitLength(request.CharacterCount);
        }
        else
        {
            expectedBitCount = HanziEncoder.CalculateBitLength(request.CharacterCount);
        }

        if (request.PayloadBitCount != expectedBitCount)
        {
            throw new QrInputException(
                "The payload bit count is inconsistent with the mode and character count.",
                "PayloadBitCount");
        }
    }

    static void ValidateOptions(QrEncodeOptions options)
    {
        if (options.MaskPattern < QrMaskPattern.Automatic
            || options.MaskPattern > QrMaskPattern.Seven)
        {
            throw new QrInputException("The requested mask is not defined.", "MaskPattern");
        }

        if (options.MinimumVersion < 0)
        {
            throw new QrInputException("The minimum version cannot be negative.", "MinimumVersion");
        }

        if (options.MaximumVersion <= 0
            || options.MinimumVersion > options.MaximumVersion)
        {
            throw new QrInputException("The numbered version range is invalid.", "MaximumVersion");
        }

        if (options.MinimumWidth < 0
            || options.MaximumWidth < 0
            || options.MinimumHeight < 0
            || options.MaximumHeight < 0)
        {
            throw new QrInputException("Symbol dimension bounds cannot be negative.", "MinimumWidth");
        }

        if (options.MaximumWidth != 0 && options.MinimumWidth > options.MaximumWidth)
        {
            throw new QrInputException("The symbol width range is invalid.", "MaximumWidth");
        }

        if (options.MaximumHeight != 0 && options.MinimumHeight > options.MaximumHeight)
        {
            throw new QrInputException("The symbol height range is invalid.", "MaximumHeight");
        }

        if (options.ForceSymbolKind)
        {
            if (options.SymbolKind == QrSymbolKind.LegacyModel1)
            {
                throw new QrUnsupportedFeatureException(
                    "Legacy Model 1 encoding is not supported.",
                    QrFailureReason.InvalidInput,
                    "SymbolKind",
                    (int)options.SymbolKind);
            }

            if (!IsFamilyEnabled(options.SymbolKind, options))
            {
                throw new QrInputException(
                    "The forced symbol kind is disabled by the family options.",
                    "SymbolKind");
            }
        }
        else if (!options.AllowModel2 && !options.AllowMicro && !options.AllowRectangularMicro)
        {
            if (options.AllowLegacyModel1)
            {
                throw new QrUnsupportedFeatureException(
                    "Legacy Model 1 encoding is not supported.",
                    QrFailureReason.InvalidInput,
                    "SymbolKind",
                    (int)QrSymbolKind.LegacyModel1);
            }

            throw new QrInputException(
                "At least one encodable QR family must be enabled.",
                "AllowModel2");
        }
    }

    static bool IsFamilyEnabled(QrSymbolKind symbolKind, QrEncodeOptions options)
    {
        if (options.ForceSymbolKind && options.SymbolKind != symbolKind)
        {
            return false;
        }

        if (symbolKind == QrSymbolKind.Model2)
        {
            return options.AllowModel2;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return options.AllowMicro;
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return options.AllowRectangularMicro;
        }

        return false;
    }

    static bool AreDimensionsAllowed(int width, int height, QrEncodeOptions options)
    {
        if (width < options.MinimumWidth || height < options.MinimumHeight)
        {
            return false;
        }

        if (options.MaximumWidth != 0 && width > options.MaximumWidth)
        {
            return false;
        }

        return options.MaximumHeight == 0 || height <= options.MaximumHeight;
    }

    static bool DoesCharacterCountFit(int characterCount, int countWidth)
    {
        return characterCount <= (1 << countWidth) - 1;
    }

    static QrCapacityRequest CreateCandidate(
        QrCapacityRequest source,
        QrSymbolKind symbolKind,
        int version,
        int width,
        int height)
    {
        return new QrCapacityRequest
        {
            SymbolKind = symbolKind,
            Version = version,
            Width = width,
            Height = height,
            ErrorCorrectionLevel = source.ErrorCorrectionLevel,
            EncodingMode = source.EncodingMode,
            CharacterCount = source.CharacterCount,
            PayloadBitCount = source.PayloadBitCount,
            ControlBitCount = source.ControlBitCount
        };
    }

    static void ConsiderCandidate(
        SymbolSelectionState state,
        QrCapacityRequest candidate,
        QrCapacityResult capacity)
    {
        if (state.LargestCapacity == null
            || capacity.DataBitCapacity > state.LargestCapacity.DataBitCapacity)
        {
            state.LargestCapacity = capacity;
            state.LargestKind = candidate.SymbolKind;
        }

        if (capacity.Fits
            && (state.BestRequest == null || IsPreferred(candidate, state.BestRequest)))
        {
            state.BestRequest = candidate;
        }
    }

    static bool IsPreferred(QrCapacityRequest candidate, QrCapacityRequest current)
    {
        int candidateArea = candidate.Width * candidate.Height;
        int currentArea = current.Width * current.Height;
        if (candidateArea != currentArea)
        {
            return candidateArea < currentArea;
        }

        int candidateMaximumDimension = candidate.Width;
        if (candidate.Height > candidateMaximumDimension)
        {
            candidateMaximumDimension = candidate.Height;
        }

        int currentMaximumDimension = current.Width;
        if (current.Height > currentMaximumDimension)
        {
            currentMaximumDimension = current.Height;
        }

        if (candidateMaximumDimension != currentMaximumDimension)
        {
            return candidateMaximumDimension < currentMaximumDimension;
        }

        if (candidate.Height != current.Height)
        {
            return candidate.Height < current.Height;
        }

        if (candidate.Width != current.Width)
        {
            return candidate.Width < current.Width;
        }

        if (candidate.SymbolKind != current.SymbolKind)
        {
            return (int)candidate.SymbolKind < (int)current.SymbolKind;
        }

        return candidate.Version < current.Version;
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
            "The requested mode does not carry a character-count field.",
            "EncodingMode");
    }
}

internal class SymbolSelectionState
{
    internal QrCapacityRequest? BestRequest;
    internal QrCapacityResult? LargestCapacity;
    internal QrSymbolKind LargestKind;
}
