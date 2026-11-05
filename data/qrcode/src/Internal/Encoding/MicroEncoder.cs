namespace qrcode;

internal class MicroEncoder
{
    internal static QrSymbol EncodeSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        ValidateOptions(options);
        SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.Micro,
            MicroVersionTable.MaximumVersion,
            options.MaximumSegments,
            options.MaximumInputBytes,
            false);

        int minimumVersion = options.MinimumVersion;
        if (minimumVersion < MicroVersionTable.MinimumVersion)
        {
            minimumVersion = MicroVersionTable.MinimumVersion;
        }

        int maximumVersion = options.MaximumVersion;
        if (maximumVersion > MicroVersionTable.MaximumVersion)
        {
            maximumVersion = MicroVersionTable.MaximumVersion;
        }

        int requestedLevel = (int)options.ErrorCorrectionLevel;
        int selectedVersion = 0;
        int requiredBitCount = 0;
        int largestRequiredBitCount = 0;
        int largestAvailableBitCount = 0;
        for (int version = minimumVersion; version <= maximumVersion; version++)
        {
            if (requestedLevel > EccBlockTable.Quartile
                || !MicroVersionTable.IsErrorCorrectionSupported(version, requestedLevel)
                || !DoModesAndCharacterCountsFit(segments, version))
            {
                continue;
            }

            int candidateRequiredBitCount = CodewordFinalizer.CalculateRequiredBitCount(
                segments,
                QrSymbolKind.Micro,
                version,
                false,
                options.MaximumSegments,
                options.MaximumInputBytes);
            int dimension = MicroVersionTable.GetDimension(version);
            if (!AreDimensionsAllowed(dimension, options))
            {
                continue;
            }

            int availableBitCount = MicroVersionTable.GetDataBitCapacity(
                version,
                requestedLevel);
            if (availableBitCount > largestAvailableBitCount)
            {
                largestAvailableBitCount = availableBitCount;
                largestRequiredBitCount = candidateRequiredBitCount;
            }

            if (candidateRequiredBitCount <= availableBitCount)
            {
                selectedVersion = version;
                requiredBitCount = candidateRequiredBitCount;
                break;
            }
        }

        if (selectedVersion == 0)
        {
            throw new QrCapacityException(
                "The segments do not fit any permitted Micro QR version.",
                QrSymbolKind.Micro,
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
            QrSymbolKind.Micro,
            selectedVersion,
            selectedLevel,
            false);
        byte[] codewords = BlockInterleaver.CreateMicroCodewords(
            dataCodewords,
            selectedVersion,
            selectedLevel);
        QrMaskPattern selectedMask = MicroMatrixBuilder.SelectMicroMask(
            codewords,
            selectedVersion,
            selectedLevel,
            options.MaskPattern);
        BitMatrixBuffer bits = MicroMatrixBuilder.CreateSelectedMicroMatrix(
            codewords,
            selectedVersion,
            selectedLevel,
            selectedMask);
        QrMatrix core = QrMatrixProcessor.ConvertBitMatrix(bits);
        QrMatrix matrix = MatrixTransformer.AddQuietZone(
            core,
            QrSymbolKind.Micro,
            options.QuietZoneModules);
        ModuleClassifier.ClassifyModules(
            matrix,
            QrSymbolKind.Micro,
            selectedVersion,
            selectedLevel);

        int dataCodewordCount = MicroVersionTable.GetDataSymbolCharacterCount(
            selectedVersion,
            selectedLevel);
        int errorCorrectionCodewordCount = MicroVersionTable.GetEccCodewordCount(
            selectedVersion,
            selectedLevel);
        int dataBitCapacity = MicroVersionTable.GetDataBitCapacity(
            selectedVersion,
            selectedLevel);
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = QrSymbolKind.Micro,
            Version = selectedVersion,
            Width = bits.GetWidth(),
            Height = bits.GetHeight(),
            ErrorCorrectionLevel = (QrErrorCorrectionLevel)selectedLevel,
            MaskPattern = selectedMask,
            Matrix = matrix,
            Codewords = codewords,
            Segments = CopySegments(segments),
            DataCodewordCount = dataCodewordCount,
            ErrorCorrectionCodewordCount = errorCorrectionCodewordCount,
            TotalCodewordCount = dataCodewordCount + errorCorrectionCodewordCount,
            RemainderBitCount = 0,
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

        if (!options.AllowMicro)
        {
            throw new QrInputException("Micro QR encoding is disabled.", "AllowMicro");
        }

        if (options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.Micro)
        {
            throw new QrInputException(
                "The forced symbol kind is not Micro QR.",
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
        if (errorCorrectionLevel < MicroVersionTable.DetectionOnly
            || errorCorrectionLevel > EccBlockTable.High)
        {
            throw new QrInputException(
                "The requested error-correction level is not defined.",
                "ErrorCorrectionLevel");
        }

        int mask = (int)options.MaskPattern;
        if (mask < (int)QrMaskPattern.Automatic || mask > (int)QrMaskPattern.Three)
        {
            throw new QrInputException(
                "The requested Micro QR mask is not defined.",
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

    static bool AreDimensionsAllowed(int dimension, QrEncodeOptions options)
    {
        if (dimension < options.MinimumWidth || dimension < options.MinimumHeight)
        {
            return false;
        }

        if (options.MaximumWidth != 0 && dimension > options.MaximumWidth)
        {
            return false;
        }

        return options.MaximumHeight == 0 || dimension <= options.MaximumHeight;
    }

    static bool DoModesAndCharacterCountsFit(QrSegment[] segments, int version)
    {
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            int modeIndex = GetModeIndex(segment.Mode);
            if (modeIndex < 0 || !MicroVersionTable.IsModeSupported(version, modeIndex))
            {
                return false;
            }

            int width = MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
            if (segment.CharacterCount > (1 << width) - 1)
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
            return MicroVersionTable.NumericMode;
        }

        if (mode == QrEncodingMode.Alphanumeric)
        {
            return MicroVersionTable.AlphanumericMode;
        }

        if (mode == QrEncodingMode.Byte)
        {
            return MicroVersionTable.ByteMode;
        }

        if (mode == QrEncodingMode.Kanji)
        {
            return MicroVersionTable.KanjiMode;
        }

        return -1;
    }

    static int SelectBoostedErrorCorrectionLevel(
        int version,
        int requestedLevel,
        int requiredBitCount,
        QrEncodeOptions options)
    {
        if (options.ForceErrorCorrectionLevel || !options.BoostErrorCorrectionLevel)
        {
            return requestedLevel;
        }

        for (int level = EccBlockTable.Quartile; level > requestedLevel; level--)
        {
            if (MicroVersionTable.IsErrorCorrectionSupported(version, level)
                && requiredBitCount <= MicroVersionTable.GetDataBitCapacity(version, level))
            {
                return level;
            }
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
