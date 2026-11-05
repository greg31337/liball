namespace qrcode;

internal class SymbolEncoder
{
    internal static QrSymbol EncodeSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        int[] state = SelectSegments(segments, options);
        if (state[0] < 0)
        {
            throw new QrCapacityException(
                "The segments do not fit any permitted QR-family symbol.",
                (QrSymbolKind)state[7],
                state[6],
                state[5]);
        }

        return EncodeSelectedSegments(segments, options, state);
    }

    internal static QrEncodeAttemptResult TryEncodeSegments(
        QrSegment[] segments,
        QrEncodeOptions options)
    {
        int[] state = SelectSegments(segments, options);
        if (state[0] < 0)
        {
            return new QrEncodeAttemptResult
            {
                Success = false,
                FailureReason = QrFailureReason.CapacityExceeded,
                RequiredBitCount = state[6],
                AvailableBitCount = state[5]
            };
        }

        QrSymbol symbol = EncodeSelectedSegments(segments, options, state);
        return new QrEncodeAttemptResult
        {
            Success = true,
            FailureReason = QrFailureReason.None,
            Symbol = symbol,
            Matrix = symbol.Matrix,
            RequiredBitCount = symbol.UsedDataBitCount,
            AvailableBitCount = symbol.DataBitCapacity
        };
    }

    internal static void ValidateConfiguration(QrEncodeOptions options)
    {
        ValidateOptions(options);
    }

    static int[] SelectSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        ValidateOptions(options);
        SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.Model2,
            QrLimits.Model2MaximumVersion,
            options.MaximumSegments,
            options.MaximumInputBytes,
            options.EnableHanziCompatibility);
        ValidateForcedFamilySegments(segments, options);
        int[] state = new int[] { -1, 0, 0, 0, 0, 0, 0, (int)QrSymbolKind.Model2 };
        ConsiderModel2Candidates(segments, options, state);
        ConsiderMicroCandidates(segments, options, state);
        ConsiderRectangularMicroCandidates(segments, options, state);
        return state;
    }

    static QrSymbol EncodeSelectedSegments(
        QrSegment[] segments,
        QrEncodeOptions options,
        int[] state)
    {
        QrEncodeOptions selectedOptions = CopySelectedOptions(
            options,
            (QrSymbolKind)state[0],
            state[1],
            state[2],
            state[3]);
        return EncodeForcedSegments(segments, selectedOptions);
    }

    static void ValidateForcedFamilySegments(
        QrSegment[] segments,
        QrEncodeOptions options)
    {
        if (!options.ForceSymbolKind || options.SymbolKind == QrSymbolKind.Model2)
        {
            return;
        }

        if (options.SymbolKind == QrSymbolKind.Micro)
        {
            SegmentValidator.ValidateSegments(
                segments,
                QrSymbolKind.Micro,
                MicroVersionTable.MaximumVersion,
                options.MaximumSegments,
                options.MaximumInputBytes,
                false);
            return;
        }

        if (options.SymbolKind == QrSymbolKind.rMQR)
        {
            SegmentValidator.ValidateSegments(
                segments,
                QrSymbolKind.rMQR,
                RectangularMicroVersionTable.MaximumVersionIndex,
                options.MaximumSegments,
                options.MaximumInputBytes,
                false);
        }
    }

    static void ValidateOptions(QrEncodeOptions options)
    {
        if (options == null)
        {
            throw new QrInputException("The encode options are required.", "options");
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

        int level = (int)options.ErrorCorrectionLevel;
        if (level < (int)QrErrorCorrectionLevel.None
            || level > (int)QrErrorCorrectionLevel.High)
        {
            throw new QrInputException(
                "The requested error-correction level is not defined.",
                "ErrorCorrectionLevel");
        }

        int mask = (int)options.MaskPattern;
        if (mask < (int)QrMaskPattern.Automatic || mask > (int)QrMaskPattern.Seven)
        {
            throw new QrInputException("The requested mask is not defined.", "MaskPattern");
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

            return;
        }

        if (!options.AllowModel2 && !options.AllowMicro && !options.AllowRectangularMicro)
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

    static QrSymbol EncodeForcedSegments(QrSegment[] segments, QrEncodeOptions options)
    {
        if (options.SymbolKind == QrSymbolKind.Model2)
        {
            return Model2Encoder.EncodeSegments(segments, options);
        }

        if (options.SymbolKind == QrSymbolKind.Micro)
        {
            return MicroEncoder.EncodeSegments(segments, options);
        }

        if (options.SymbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroEncoder.EncodeSegments(segments, options);
        }

        throw new QrUnsupportedFeatureException(
            "Legacy Model 1 encoding is not supported.",
            QrFailureReason.InvalidInput,
            "SymbolKind",
            (int)options.SymbolKind);
    }

    static void ConsiderModel2Candidates(
        QrSegment[] segments,
        QrEncodeOptions options,
        int[] state)
    {
        int level = (int)options.ErrorCorrectionLevel;
        int mask = (int)options.MaskPattern;
        if (!options.AllowModel2
            || options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.Model2
            || level < EccBlockTable.Low
            || level > EccBlockTable.High
            || mask > (int)QrMaskPattern.Seven)
        {
            return;
        }

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

        for (int version = minimumVersion; version <= maximumVersion; version++)
        {
            int dimension = Model2VersionTable.GetDimension(version);
            if (!AreDimensionsAllowed(dimension, dimension, options)
                || !DoCharacterCountsFitModel2(segments, version))
            {
                continue;
            }

            int required = CodewordFinalizer.CalculateRequiredBitCount(
                segments,
                QrSymbolKind.Model2,
                version,
                options.EnableHanziCompatibility,
                options.MaximumSegments,
                options.MaximumInputBytes);
            int available = EccBlockTable.GetDataCodewordCount(version, level) * 8;
            ConsiderCandidate(
                state,
                QrSymbolKind.Model2,
                version,
                dimension,
                dimension,
                required,
                available);
        }
    }

    static void ConsiderMicroCandidates(
        QrSegment[] segments,
        QrEncodeOptions options,
        int[] state)
    {
        int level = (int)options.ErrorCorrectionLevel;
        int mask = (int)options.MaskPattern;
        if (!options.AllowMicro
            || options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.Micro
            || level < MicroVersionTable.DetectionOnly
            || level > EccBlockTable.Quartile
            || mask > (int)QrMaskPattern.Three
            || ContainsControlOrHanzi(segments))
        {
            return;
        }

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

        for (int version = minimumVersion; version <= maximumVersion; version++)
        {
            if (!MicroVersionTable.IsErrorCorrectionSupported(version, level)
                || !DoModesAndCountsFitMicro(segments, version))
            {
                continue;
            }

            int dimension = MicroVersionTable.GetDimension(version);
            if (!AreDimensionsAllowed(dimension, dimension, options))
            {
                continue;
            }

            int required = CodewordFinalizer.CalculateRequiredBitCount(
                segments,
                QrSymbolKind.Micro,
                version,
                false,
                options.MaximumSegments,
                options.MaximumInputBytes);
            int available = MicroVersionTable.GetDataBitCapacity(version, level);
            ConsiderCandidate(
                state,
                QrSymbolKind.Micro,
                version,
                dimension,
                dimension,
                required,
                available);
        }
    }

    static void ConsiderRectangularMicroCandidates(
        QrSegment[] segments,
        QrEncodeOptions options,
        int[] state)
    {
        int level = GetRectangularMicroLevel(options.ErrorCorrectionLevel);
        if (!options.AllowRectangularMicro
            || options.ForceSymbolKind && options.SymbolKind != QrSymbolKind.rMQR
            || level < 0
            || options.MaskPattern != QrMaskPattern.Automatic
                && options.MaskPattern != QrMaskPattern.Four
            || ContainsStructuredAppendOrHanzi(segments))
        {
            return;
        }

        for (int version = RectangularMicroVersionTable.MinimumVersionIndex;
            version <= RectangularMicroVersionTable.MaximumVersionIndex;
            version++)
        {
            int width = RectangularMicroVersionTable.GetWidth(version);
            int height = RectangularMicroVersionTable.GetHeight(version);
            if (!AreDimensionsAllowed(width, height, options)
                || !DoCharacterCountsFitRectangularMicro(segments, version))
            {
                continue;
            }

            int required = CodewordFinalizer.CalculateRequiredBitCount(
                segments,
                QrSymbolKind.rMQR,
                version,
                false,
                options.MaximumSegments,
                options.MaximumInputBytes);
            int available = RectangularMicroVersionTable.GetDataBitCapacity(version, level);
            ConsiderCandidate(
                state,
                QrSymbolKind.rMQR,
                version,
                width,
                height,
                required,
                available);
        }
    }

    static void ConsiderCandidate(
        int[] state,
        QrSymbolKind kind,
        int version,
        int width,
        int height,
        int required,
        int available)
    {
        if (available > state[5])
        {
            state[5] = available;
            state[6] = required;
            state[7] = (int)kind;
        }

        if (required > available)
        {
            return;
        }

        if (state[0] < 0
            || IsPreferred(kind, version, width, height, state))
        {
            state[0] = (int)kind;
            state[1] = version;
            state[2] = width;
            state[3] = height;
            state[4] = required;
        }
    }

    static bool IsPreferred(
        QrSymbolKind kind,
        int version,
        int width,
        int height,
        int[] state)
    {
        int area = width * height;
        int selectedArea = state[2] * state[3];
        if (area != selectedArea)
        {
            return area < selectedArea;
        }

        int maximumDimension = width;
        if (height > maximumDimension)
        {
            maximumDimension = height;
        }

        int selectedMaximumDimension = state[2];
        if (state[3] > selectedMaximumDimension)
        {
            selectedMaximumDimension = state[3];
        }

        if (maximumDimension != selectedMaximumDimension)
        {
            return maximumDimension < selectedMaximumDimension;
        }

        if (height != state[3])
        {
            return height < state[3];
        }

        if (width != state[2])
        {
            return width < state[2];
        }

        if ((int)kind != state[0])
        {
            return (int)kind < state[0];
        }

        return version < state[1];
    }

    static bool DoCharacterCountsFitModel2(QrSegment[] segments, int version)
    {
        bool fnc1 = false;
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            if (segment.Mode == QrEncodingMode.Fnc1First
                || segment.Mode == QrEncodingMode.Fnc1Second)
            {
                fnc1 = true;
                continue;
            }

            int modeIndex = GetModeIndex(segment.Mode);
            if (modeIndex < 0)
            {
                continue;
            }

            int count = GetEncodedCharacterCount(segment, fnc1);
            int width = Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
            if (count > (1 << width) - 1)
            {
                return false;
            }
        }

        return true;
    }

    static bool DoModesAndCountsFitMicro(QrSegment[] segments, int version)
    {
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            int modeIndex = GetModeIndex(segment.Mode);
            if (modeIndex < 0
                || !MicroVersionTable.IsModeSupported(version, modeIndex))
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

    static bool DoCharacterCountsFitRectangularMicro(
        QrSegment[] segments,
        int version)
    {
        bool fnc1 = false;
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            if (segment.Mode == QrEncodingMode.Fnc1First
                || segment.Mode == QrEncodingMode.Fnc1Second)
            {
                fnc1 = true;
                continue;
            }

            int modeIndex = GetModeIndex(segment.Mode);
            if (modeIndex < 0)
            {
                continue;
            }

            int count = GetEncodedCharacterCount(segment, fnc1);
            int width = RectangularMicroVersionTable.GetCharacterCountBitWidth(
                version,
                modeIndex);
            if (count > (1 << width) - 1)
            {
                return false;
            }
        }

        return true;
    }

    static int GetEncodedCharacterCount(QrSegment segment, bool fnc1)
    {
        if (fnc1 && segment.Mode == QrEncodingMode.Alphanumeric)
        {
            if (segment.Text == null)
            {
                throw new QrInputException(
                    "The alphanumeric segment must retain its source text.",
                    "Text");
            }

            return Fnc1Encoder.CalculateEscapedLength(segment.Text);
        }

        return segment.CharacterCount;
    }

    static int GetModeIndex(QrEncodingMode mode)
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

    static bool ContainsControlOrHanzi(QrSegment[] segments)
    {
        for (int index = 0; index < segments.Length; index++)
        {
            QrEncodingMode mode = segments[index].Mode;
            if (mode == QrEncodingMode.Eci
                || mode == QrEncodingMode.Fnc1First
                || mode == QrEncodingMode.Fnc1Second
                || mode == QrEncodingMode.StructuredAppend
                || mode == QrEncodingMode.Hanzi)
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsStructuredAppendOrHanzi(QrSegment[] segments)
    {
        for (int index = 0; index < segments.Length; index++)
        {
            QrEncodingMode mode = segments[index].Mode;
            if (mode == QrEncodingMode.StructuredAppend || mode == QrEncodingMode.Hanzi)
            {
                return true;
            }
        }

        return false;
    }

    static int GetRectangularMicroLevel(QrErrorCorrectionLevel level)
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

    static bool IsFamilyEnabled(QrSymbolKind kind, QrEncodeOptions options)
    {
        if (kind == QrSymbolKind.Model2)
        {
            return options.AllowModel2;
        }

        if (kind == QrSymbolKind.Micro)
        {
            return options.AllowMicro;
        }

        if (kind == QrSymbolKind.rMQR)
        {
            return options.AllowRectangularMicro;
        }

        return false;
    }

    static QrEncodeOptions CopySelectedOptions(
        QrEncodeOptions source,
        QrSymbolKind kind,
        int version,
        int width,
        int height)
    {
        QrEncodeOptions result = new QrEncodeOptions
        {
            AllowModel2 = source.AllowModel2,
            AllowMicro = source.AllowMicro,
            AllowRectangularMicro = source.AllowRectangularMicro,
            AllowLegacyModel1 = source.AllowLegacyModel1,
            ForceSymbolKind = true,
            SymbolKind = kind,
            MinimumVersion = source.MinimumVersion,
            MaximumVersion = source.MaximumVersion,
            MinimumWidth = width,
            MaximumWidth = width,
            MinimumHeight = height,
            MaximumHeight = height,
            ForceErrorCorrectionLevel = source.ForceErrorCorrectionLevel,
            ErrorCorrectionLevel = source.ErrorCorrectionLevel,
            BoostErrorCorrectionLevel = source.BoostErrorCorrectionLevel,
            MaskPattern = source.MaskPattern,
            OptimizeSegments = source.OptimizeSegments,
            EciAssignment = source.EciAssignment,
            UseFnc1First = source.UseFnc1First,
            Fnc1SecondApplicationIndicator = source.Fnc1SecondApplicationIndicator,
            EnableHanziCompatibility = source.EnableHanziCompatibility,
            QuietZoneModules = source.QuietZoneModules,
            MaximumInputBytes = source.MaximumInputBytes,
            MaximumSegments = source.MaximumSegments,
            MaximumStructuredAppendSymbols = source.MaximumStructuredAppendSymbols
        };
        if (kind == QrSymbolKind.Model2 || kind == QrSymbolKind.Micro)
        {
            result.MinimumVersion = version;
            result.MaximumVersion = version;
        }

        return result;
    }
}
