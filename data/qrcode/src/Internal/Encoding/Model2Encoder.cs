namespace qrcode;

internal class Model2Encoder
{
    internal static QrSymbol EncodeSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        ValidateOptions(options);
        SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.Model2,
            QrLimits.Model2MaximumVersion,
            options.MaximumSegments,
            options.MaximumInputBytes,
            options.EnableHanziCompatibility);

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

        int requestedLevel = (int)options.ErrorCorrectionLevel;
        int selectedVersion = 0;
        int requiredBitCount = 0;
        int largestRequiredBitCount = 0;
        int largestAvailableBitCount = 0;
        for (int version = minimumVersion; version <= maximumVersion; version++)
        {
            if (!DoCharacterCountsFit(segments, version))
            {
                continue;
            }

            int candidateRequiredBitCount = CodewordFinalizer.CalculateRequiredBitCount(
                segments,
                QrSymbolKind.Model2,
                version,
                options.EnableHanziCompatibility,
                options.MaximumSegments,
                options.MaximumInputBytes);
            int dimension = Model2VersionTable.GetDimension(version);
            if (!AreDimensionsAllowed(dimension, options))
            {
                continue;
            }

            int availableBitCount = CheckedDimensions.CalculateProduct(
                EccBlockTable.GetDataCodewordCount(version, requestedLevel),
                8,
                int.MaxValue,
                "CapacityBitCount");
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
                "The segments do not fit any permitted Model 2 version.",
                QrSymbolKind.Model2,
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
            QrSymbolKind.Model2,
            selectedVersion,
            selectedLevel,
            options.EnableHanziCompatibility);
        byte[] codewords = BlockInterleaver.CreateModel2Codewords(
            dataCodewords,
            selectedVersion,
            selectedLevel);
        QrMaskPattern selectedMask = Model2MatrixBuilder.SelectModel2Mask(
            codewords,
            selectedVersion,
            selectedLevel,
            options.MaskPattern);
        BitMatrixBuffer bits = Model2MatrixBuilder.CreateSelectedModel2Matrix(
            codewords,
            selectedVersion,
            selectedLevel,
            selectedMask);
        QrMatrix core = QrMatrixProcessor.ConvertBitMatrix(bits);
        QrMatrix matrix = MatrixTransformer.AddQuietZone(
            core,
            QrSymbolKind.Model2,
            options.QuietZoneModules);
        ModuleClassifier.ClassifyModules(
            matrix,
            QrSymbolKind.Model2,
            selectedVersion,
            selectedLevel);

        int dataCodewordCount = EccBlockTable.GetDataCodewordCount(
            selectedVersion,
            selectedLevel);
        int totalCodewordCount = Model2VersionTable.GetTotalCodewordCount(selectedVersion);
        int dataBitCapacity = CheckedDimensions.CalculateProduct(
            dataCodewordCount,
            8,
            int.MaxValue,
            "CapacityBitCount");
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = QrSymbolKind.Model2,
            Version = selectedVersion,
            Width = bits.GetWidth(),
            Height = bits.GetHeight(),
            ErrorCorrectionLevel = (QrErrorCorrectionLevel)selectedLevel,
            MaskPattern = selectedMask,
            Matrix = matrix,
            Codewords = codewords,
            Segments = CopySegments(segments),
            DataCodewordCount = dataCodewordCount,
            ErrorCorrectionCodewordCount = totalCodewordCount - dataCodewordCount,
            TotalCodewordCount = totalCodewordCount,
            RemainderBitCount = Model2VersionTable.GetRemainderBitCount(selectedVersion),
            DataBitCapacity = dataBitCapacity,
            UsedDataBitCount = requiredBitCount,
            UnusedDataBitCount = dataBitCapacity - requiredBitCount,
            UsesHanziCompatibility = ContainsHanziSegment(segments)
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

        if (!options.AllowModel2)
        {
            throw new QrInputException("Model 2 encoding is disabled.", "AllowModel2");
        }

        if (options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.Model2)
        {
            throw new QrInputException(
                "The forced symbol kind is not Model 2.",
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
        if (errorCorrectionLevel < EccBlockTable.Low
            || errorCorrectionLevel > EccBlockTable.High)
        {
            throw new QrInputException(
                "Model 2 requires a defined error-correction level.",
                "ErrorCorrectionLevel");
        }

        int mask = (int)options.MaskPattern;
        if (mask < (int)QrMaskPattern.Automatic || mask > (int)QrMaskPattern.Seven)
        {
            throw new QrInputException(
                "The requested Model 2 mask is not defined.",
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

            int modeIndex = GetDataModeIndex(segment.Mode);
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

            int width = Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
            if (characterCount > (1 << width) - 1)
            {
                return false;
            }
        }

        return true;
    }

    static int GetDataModeIndex(QrEncodingMode mode)
    {
        if (mode == QrEncodingMode.Numeric)
        {
            return 0;
        }

        if (mode == QrEncodingMode.Alphanumeric)
        {
            return 1;
        }

        if (mode == QrEncodingMode.Byte)
        {
            return 2;
        }

        if (mode == QrEncodingMode.Kanji || mode == QrEncodingMode.Hanzi)
        {
            return 3;
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

        for (int level = EccBlockTable.High; level > requestedLevel; level--)
        {
            int capacity = CheckedDimensions.CalculateProduct(
                EccBlockTable.GetDataCodewordCount(version, level),
                8,
                int.MaxValue,
                "CapacityBitCount");
            if (requiredBitCount <= capacity)
            {
                return level;
            }
        }

        return requestedLevel;
    }

    static bool ContainsHanziSegment(QrSegment[] segments)
    {
        for (int index = 0; index < segments.Length; index++)
        {
            if (segments[index].Mode == QrEncodingMode.Hanzi)
            {
                return true;
            }
        }

        return false;
    }

    static QrSegment[] CopySegments(QrSegment[] segments)
    {
        QrSegment[] result = new QrSegment[segments.Length];
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment source = segments[index];
            QrSegment copy = new QrSegment
            {
                Mode = source.Mode,
                CharacterCount = source.CharacterCount,
                Data = CopyBytes(source.Data),
                Text = source.Text,
                EciAssignment = source.EciAssignment,
                Fnc1ApplicationIndicator = source.Fnc1ApplicationIndicator,
                IsHanziCompatibilityMode = source.IsHanziCompatibilityMode
            };
            if (source.StructuredAppend != null)
            {
                copy.StructuredAppend = CopyStructuredAppend(source.StructuredAppend);
            }

            result[index] = copy;
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

    static QrStructuredAppendInfo CopyStructuredAppend(QrStructuredAppendInfo source)
    {
        return new QrStructuredAppendInfo
        {
            Index = source.Index,
            Total = source.Total,
            Parity = source.Parity,
            ApplicationIdentifier = source.ApplicationIdentifier,
            IsComplete = source.IsComplete
        };
    }
}
