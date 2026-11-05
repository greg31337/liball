namespace qrcode;

internal class RectangularMicroEncoder
{
    internal static QrSymbol EncodeSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        ValidateOptions(options);
        SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.rMQR,
            RectangularMicroVersionTable.MaximumVersionIndex,
            options.MaximumSegments,
            options.MaximumInputBytes,
            false);

        int requestedLevel = GetErrorCorrectionIndex(options.ErrorCorrectionLevel);
        int selectedVersion = -1;
        int requiredBitCount = 0;
        int largestRequiredBitCount = 0;
        int largestAvailableBitCount = 0;
        if (requestedLevel >= RectangularMicroVersionTable.Medium)
        {
            for (int version = RectangularMicroVersionTable.MinimumVersionIndex;
                version <= RectangularMicroVersionTable.MaximumVersionIndex;
                version++)
            {
                if (!DoCharacterCountsFit(segments, version))
                {
                    continue;
                }

                int candidateRequiredBitCount = CodewordFinalizer.CalculateRequiredBitCount(
                    segments,
                    QrSymbolKind.rMQR,
                    version,
                    false,
                    options.MaximumSegments,
                    options.MaximumInputBytes);
                int width = RectangularMicroVersionTable.GetWidth(version);
                int height = RectangularMicroVersionTable.GetHeight(version);
                if (!AreDimensionsAllowed(width, height, options))
                {
                    continue;
                }

                int availableBitCount = RectangularMicroVersionTable.GetDataBitCapacity(
                    version,
                    requestedLevel);
                if (availableBitCount > largestAvailableBitCount)
                {
                    largestAvailableBitCount = availableBitCount;
                    largestRequiredBitCount = candidateRequiredBitCount;
                }

                if (candidateRequiredBitCount <= availableBitCount
                    && IsPreferredSize(version, selectedVersion))
                {
                    selectedVersion = version;
                    requiredBitCount = candidateRequiredBitCount;
                }
            }
        }

        if (selectedVersion < 0)
        {
            throw new QrCapacityException(
                "The segments do not fit any permitted rMQR size.",
                QrSymbolKind.rMQR,
                largestRequiredBitCount,
                largestAvailableBitCount);
        }

        int selectedLevel = SelectBoostedErrorCorrectionLevel(
            selectedVersion,
            requestedLevel,
            requiredBitCount,
            options);
        byte[] dataCodewords = CodewordFinalizer.CreateDataCodewords(
            segments,
            QrSymbolKind.rMQR,
            selectedVersion,
            selectedLevel,
            false);
        byte[] codewords = BlockInterleaver.CreateRectangularMicroCodewords(
            dataCodewords,
            selectedVersion,
            selectedLevel);
        QrMaskPattern selectedMask =
            RectangularMicroMatrixBuilder.SelectRectangularMicroMask(
                codewords,
                selectedVersion,
                selectedLevel,
                options.MaskPattern);
        BitMatrixBuffer bits =
            RectangularMicroMatrixBuilder.CreateSelectedRectangularMicroMatrix(
                codewords,
                selectedVersion,
                selectedLevel,
                selectedMask);
        QrMatrix core = QrMatrixProcessor.ConvertBitMatrix(bits);
        QrMatrix matrix = MatrixTransformer.AddQuietZone(
            core,
            QrSymbolKind.rMQR,
            options.QuietZoneModules);
        ModuleClassifier.ClassifyModules(
            matrix,
            QrSymbolKind.rMQR,
            selectedVersion,
            selectedLevel);

        int dataCodewordCount = RectangularMicroVersionTable.GetDataCodewordCount(
            selectedVersion,
            selectedLevel);
        int totalCodewordCount = RectangularMicroVersionTable.GetTotalCodewordCount(
            selectedVersion);
        int dataBitCapacity = RectangularMicroVersionTable.GetDataBitCapacity(
            selectedVersion,
            selectedLevel);
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = QrSymbolKind.rMQR,
            Version = selectedVersion,
            Width = bits.GetWidth(),
            Height = bits.GetHeight(),
            ErrorCorrectionLevel = GetPublicErrorCorrectionLevel(selectedLevel),
            MaskPattern = selectedMask,
            Matrix = matrix,
            Codewords = codewords,
            Segments = CopySegments(segments),
            DataCodewordCount = dataCodewordCount,
            ErrorCorrectionCodewordCount = totalCodewordCount - dataCodewordCount,
            TotalCodewordCount = totalCodewordCount,
            RemainderBitCount = RectangularMicroVersionTable.GetRemainderBitCount(
                selectedVersion),
            DataBitCapacity = dataBitCapacity,
            UsedDataBitCount = requiredBitCount,
            UnusedDataBitCount = dataBitCapacity - requiredBitCount
        };
        QrValidationResult validation = MatrixValidator.ValidateSymbol(symbol);
        symbol.Validation = validation;
        symbol.IsConformant = validation.IsConformant;
        return symbol;
    }

    static void ValidateOptions(QrEncodeOptions options)
    {
        if (options == null)
        {
            throw new QrInputException("The encode options are required.", "options");
        }

        if (!options.AllowRectangularMicro)
        {
            throw new QrInputException(
                "rMQR encoding is disabled.",
                "AllowRectangularMicro");
        }

        if (options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.rMQR)
        {
            throw new QrInputException(
                "The forced symbol kind is not rMQR.",
                "SymbolKind");
        }

        if (options.MinimumVersion < 0)
        {
            throw new QrInputException(
                "The minimum version cannot be negative.",
                "MinimumVersion");
        }

        if (options.MaximumVersion <= 0
            || options.MinimumVersion > options.MaximumVersion)
        {
            throw new QrInputException(
                "The numbered version range is invalid.",
                "MaximumVersion");
        }

        if (options.MinimumWidth < 0
            || options.MaximumWidth < 0
            || options.MinimumHeight < 0
            || options.MaximumHeight < 0)
        {
            throw new QrInputException(
                "Symbol dimension bounds cannot be negative.",
                "MinimumWidth");
        }

        if ((options.MaximumWidth != 0
                && options.MinimumWidth > options.MaximumWidth)
            || (options.MaximumHeight != 0
                && options.MinimumHeight > options.MaximumHeight))
        {
            throw new QrInputException(
                "The symbol dimension range is invalid.",
                "MaximumWidth");
        }

        int errorCorrectionLevel = (int)options.ErrorCorrectionLevel;
        if (errorCorrectionLevel < (int)QrErrorCorrectionLevel.None
            || errorCorrectionLevel > (int)QrErrorCorrectionLevel.High)
        {
            throw new QrInputException(
                "The requested error-correction level is not defined.",
                "ErrorCorrectionLevel");
        }

        if (options.MaskPattern != QrMaskPattern.Automatic
            && options.MaskPattern != QrMaskPattern.Four)
        {
            throw new QrInputException(
                "rMQR permits only its fixed mask pattern four.",
                "MaskPattern");
        }

        if (options.QuietZoneModules < -1)
        {
            throw new QrInputException(
                "The quiet-zone width must be nonnegative or -1 for the profile default.",
                "QuietZoneModules");
        }

        if (options.MaximumSegments <= 0)
        {
            throw new QrInputException(
                "The maximum segment count must be positive.",
                "MaximumSegments");
        }

        if (options.MaximumInputBytes < 0)
        {
            throw new QrInputException(
                "The maximum input byte count cannot be negative.",
                "MaximumInputBytes");
        }
    }

    static int GetErrorCorrectionIndex(QrErrorCorrectionLevel level)
    {
        if (level == QrErrorCorrectionLevel.Medium)
        {
            return RectangularMicroVersionTable.Medium;
        }

        if (level == QrErrorCorrectionLevel.High)
        {
            return RectangularMicroVersionTable.High;
        }

        return -1;
    }

    static QrErrorCorrectionLevel GetPublicErrorCorrectionLevel(int level)
    {
        if (level == RectangularMicroVersionTable.Medium)
        {
            return QrErrorCorrectionLevel.Medium;
        }

        return QrErrorCorrectionLevel.High;
    }

    static bool AreDimensionsAllowed(
        int width,
        int height,
        QrEncodeOptions options)
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

    static bool DoCharacterCountsFit(QrSegment[] segments, int version)
    {
        bool fnc1Active = false;
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            if (segment.Mode == QrEncodingMode.Fnc1First
                || segment.Mode == QrEncodingMode.Fnc1Second)
            {
                fnc1Active = true;
                continue;
            }

            int modeIndex = GetModeIndex(segment.Mode);
            if (modeIndex < 0)
            {
                continue;
            }

            int characterCount = segment.CharacterCount;
            if (fnc1Active && segment.Mode == QrEncodingMode.Alphanumeric)
            {
                if (segment.Text == null)
                {
                    throw new QrInputException(
                        "The alphanumeric segment must retain its source text.",
                        "Text");
                }

                characterCount = Fnc1Encoder.CalculateEscapedLength(segment.Text);
            }

            int width = RectangularMicroVersionTable.GetCharacterCountBitWidth(
                version,
                modeIndex);
            if (characterCount > (1 << width) - 1)
            {
                return false;
            }
        }

        return true;
    }

    static int GetModeIndex(QrEncodingMode mode)
    {
        if (mode == QrEncodingMode.Numeric)
        {
            return RectangularMicroVersionTable.NumericMode;
        }

        if (mode == QrEncodingMode.Alphanumeric)
        {
            return RectangularMicroVersionTable.AlphanumericMode;
        }

        if (mode == QrEncodingMode.Byte)
        {
            return RectangularMicroVersionTable.ByteMode;
        }

        if (mode == QrEncodingMode.Kanji)
        {
            return RectangularMicroVersionTable.KanjiMode;
        }

        return -1;
    }

    static bool IsPreferredSize(int candidate, int current)
    {
        if (current < RectangularMicroVersionTable.MinimumVersionIndex)
        {
            return true;
        }

        int candidateWidth = RectangularMicroVersionTable.GetWidth(candidate);
        int candidateHeight = RectangularMicroVersionTable.GetHeight(candidate);
        int currentWidth = RectangularMicroVersionTable.GetWidth(current);
        int currentHeight = RectangularMicroVersionTable.GetHeight(current);
        int candidateArea = candidateWidth * candidateHeight;
        int currentArea = currentWidth * currentHeight;
        if (candidateArea != currentArea)
        {
            return candidateArea < currentArea;
        }

        int candidateMaximumDimension = candidateWidth;
        if (candidateHeight > candidateMaximumDimension)
        {
            candidateMaximumDimension = candidateHeight;
        }

        int currentMaximumDimension = currentWidth;
        if (currentHeight > currentMaximumDimension)
        {
            currentMaximumDimension = currentHeight;
        }

        if (candidateMaximumDimension != currentMaximumDimension)
        {
            return candidateMaximumDimension < currentMaximumDimension;
        }

        if (candidateHeight != currentHeight)
        {
            return candidateHeight < currentHeight;
        }

        if (candidateWidth != currentWidth)
        {
            return candidateWidth < currentWidth;
        }

        return candidate < current;
    }

    static int SelectBoostedErrorCorrectionLevel(
        int version,
        int requestedLevel,
        int requiredBitCount,
        QrEncodeOptions options)
    {
        if (options.ForceErrorCorrectionLevel
            || !options.BoostErrorCorrectionLevel
            || requestedLevel == RectangularMicroVersionTable.High)
        {
            return requestedLevel;
        }

        int highCapacity = RectangularMicroVersionTable.GetDataBitCapacity(
            version,
            RectangularMicroVersionTable.High);
        if (requiredBitCount <= highCapacity)
        {
            return RectangularMicroVersionTable.High;
        }

        return requestedLevel;
    }

    static QrSegment[] CopySegments(QrSegment[] segments)
    {
        QrSegment[] result = new QrSegment[segments.Length];
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment source = segments[index];
            result[index] = new QrSegment
            {
                Mode = source.Mode,
                CharacterCount = source.CharacterCount,
                Data = CopyBytes(source.Data),
                Text = source.Text,
                EciAssignment = source.EciAssignment,
                Fnc1ApplicationIndicator = source.Fnc1ApplicationIndicator,
                IsHanziCompatibilityMode = source.IsHanziCompatibilityMode
            };
        }

        return result;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        for (int index = 0; index < source.Length; index++)
        {
            result[index] = source[index];
        }

        return result;
    }
}
