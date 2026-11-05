namespace qrcode;

internal class SegmentValidator
{
    internal static void ValidateSegments(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        int maximumSegments,
        int maximumInputBytes,
        bool enableHanziCompatibility)
    {
        if (segments == null)
        {
            throw new QrInputException("The segment collection cannot be null.", "segments");
        }

        if (maximumSegments <= 0)
        {
            throw new QrInputException("The maximum segment count must be positive.", "maximumSegments");
        }

        if (maximumInputBytes < 0)
        {
            throw new QrInputException("The maximum input byte count cannot be negative.", "maximumInputBytes");
        }

        ValidateVersion(symbolKind, version);
        if (segments.Length > maximumSegments)
        {
            throw new QrResourceLimitException(
                "The manual segment count exceeds the configured limit.",
                "MaximumSegments",
                segments.Length,
                maximumSegments);
        }

        bool sawStructuredAppend = false;
        bool sawFnc1 = false;
        bool sawEci = false;
        bool sawData = false;
        int totalInputBytes = 0;
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            if (segment == null)
            {
                throw new QrInputException("The segment collection contains a null entry.", "segments");
            }

            ValidateModeForFamily(segment.Mode, symbolKind, version, enableHanziCompatibility);
            if (segment.Mode == QrEncodingMode.StructuredAppend)
            {
                if (index != 0 || sawStructuredAppend)
                {
                    throw new QrInputException(
                        "Structured append must occur once as the first segment.",
                        "segments");
                }

                sawStructuredAppend = true;
            }
            else if (segment.Mode == QrEncodingMode.Fnc1First
                || segment.Mode == QrEncodingMode.Fnc1Second)
            {
                if (sawFnc1 || sawEci || sawData)
                {
                    throw new QrInputException(
                        "FNC1 must occur once before ECI and data segments.",
                        "segments");
                }

                sawFnc1 = true;
            }
            else if (segment.Mode == QrEncodingMode.Eci)
            {
                sawEci = true;
            }
            else
            {
                sawData = true;
            }

            int segmentInputBytes = ValidateSegmentData(segment, enableHanziCompatibility, sawFnc1);
            ValidateCharacterCountWidth(segment, symbolKind, version);
            totalInputBytes = CheckedDimensions.CalculateSum(
                totalInputBytes,
                segmentInputBytes,
                maximumInputBytes,
                "MaximumInputBytes");
        }
    }

    static void ValidateVersion(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            if (version < QrLimits.Model2MinimumVersion || version > QrLimits.Model2MaximumVersion)
            {
                throw new QrInputException("The Model 2 version is outside the standardized range.", "version");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            if (version < QrLimits.MicroMinimumVersion || version > QrLimits.MicroMaximumVersion)
            {
                throw new QrInputException("The Micro QR version is outside the standardized range.", "version");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            if (version < RectangularMicroVersionTable.MinimumVersionIndex
                || version > RectangularMicroVersionTable.MaximumVersionIndex)
            {
                throw new QrInputException("The rMQR version index is outside the standardized range.", "version");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            if (version < QrLimits.LegacyModel1MinimumVersion
                || version > QrLimits.LegacyModel1MaximumVersion)
            {
                throw new QrInputException("The Model 1 version is outside the standardized range.", "version");
            }

            return;
        }

        throw new QrInputException("The symbol kind is not defined.", "symbolKind");
    }

    static void ValidateModeForFamily(
        QrEncodingMode mode,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility)
    {
        if (mode == QrEncodingMode.Terminator)
        {
            throw new QrInputException("Terminator segments are generated automatically.", "mode");
        }

        if (mode == QrEncodingMode.Hanzi)
        {
            if (symbolKind != QrSymbolKind.Model2 || !enableHanziCompatibility)
            {
                throw new QrInputException(
                    "Hanzi mode requires Model 2 and explicit compatibility opt-in.",
                    "mode");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.Model2)
        {
            if (!IsKnownNonHanziMode(mode))
            {
                throw new QrInputException("The encoding mode is not defined.", "mode");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            int modeIndex = GetDataModeIndex(mode);
            if (modeIndex < 0 || !MicroVersionTable.IsModeSupported(version, modeIndex))
            {
                throw new QrInputException("The mode is not supported by the Micro QR version.", "mode");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            if (GetDataModeIndex(mode) < 0
                && mode != QrEncodingMode.Eci
                && mode != QrEncodingMode.Fnc1First
                && mode != QrEncodingMode.Fnc1Second)
            {
                throw new QrInputException("The mode is not supported by rMQR.", "mode");
            }

            return;
        }

        if (symbolKind == QrSymbolKind.LegacyModel1 && GetDataModeIndex(mode) < 0)
        {
            throw new QrInputException("The mode is not supported by legacy Model 1.", "mode");
        }
    }

    static bool IsKnownNonHanziMode(QrEncodingMode mode)
    {
        return GetDataModeIndex(mode) >= 0
            || mode == QrEncodingMode.Eci
            || mode == QrEncodingMode.Fnc1First
            || mode == QrEncodingMode.Fnc1Second
            || mode == QrEncodingMode.StructuredAppend;
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

        if (mode == QrEncodingMode.Kanji)
        {
            return 3;
        }

        return -1;
    }

    static int ValidateSegmentData(QrSegment segment, bool enableHanziCompatibility, bool usesFnc1)
    {
        if (segment.Data == null)
        {
            throw new QrInputException("Segment data cannot be null.", "Data");
        }

        if (segment.CharacterCount < 0)
        {
            throw new QrInputException("The segment character count cannot be negative.", "CharacterCount");
        }

        if (segment.Mode == QrEncodingMode.Numeric)
        {
            string text = RequireText(segment);
            NumericEncoder.ValidateDigits(text);
            ValidateTextCharacterCount(segment, text);
            ValidateOrdinaryMetadata(segment, false);
            return GetTextInputByteCount(segment, text.Length, 1);
        }

        if (segment.Mode == QrEncodingMode.Alphanumeric)
        {
            string text = RequireText(segment);
            if (usesFnc1)
            {
                Fnc1Encoder.CalculateEscapedLength(text);
            }
            else
            {
                AlphanumericEncoder.ValidateText(text);
            }
            ValidateTextCharacterCount(segment, text);
            ValidateOrdinaryMetadata(segment, false);
            return GetTextInputByteCount(segment, text.Length, 1);
        }

        if (segment.Mode == QrEncodingMode.Byte)
        {
            if (segment.CharacterCount != segment.Data.Length)
            {
                throw new QrInputException("The byte segment count must equal its data length.", "CharacterCount");
            }

            ValidateOrdinaryMetadata(segment, true);
            return segment.Data.Length;
        }

        if (segment.Mode == QrEncodingMode.Kanji)
        {
            string text = RequireText(segment);
            KanjiEncoder.ValidateText(text);
            ValidateTextCharacterCount(segment, text);
            ValidateOrdinaryMetadata(segment, false);
            return GetTextInputByteCount(segment, text.Length, 2);
        }

        if (segment.Mode == QrEncodingMode.Hanzi)
        {
            string text = RequireText(segment);
            if (!segment.IsHanziCompatibilityMode || !enableHanziCompatibility)
            {
                throw new QrInputException("The Hanzi compatibility marker is required.", "IsHanziCompatibilityMode");
            }

            HanziEncoder.ValidateText(text, true);
            ValidateTextCharacterCount(segment, text);
            if (segment.EciAssignment != -1
                || segment.Fnc1ApplicationIndicator != -1
                || segment.StructuredAppend != null)
            {
                throw new QrInputException("The Hanzi segment contains unrelated control metadata.", "segment");
            }

            return GetTextInputByteCount(segment, text.Length, 2);
        }

        ValidateControlSegment(segment);
        return 0;
    }

    static string RequireText(QrSegment segment)
    {
        if (segment.Text == null)
        {
            throw new QrInputException("The text segment must retain its source text.", "Text");
        }

        return segment.Text;
    }

    static void ValidateTextCharacterCount(QrSegment segment, string text)
    {
        if (segment.CharacterCount != text.Length)
        {
            throw new QrInputException("The segment count must equal its text length.", "CharacterCount");
        }
    }

    static void ValidateOrdinaryMetadata(QrSegment segment, bool allowEciAssignment)
    {
        if ((!allowEciAssignment && segment.EciAssignment != -1)
            || segment.Fnc1ApplicationIndicator != -1
            || segment.StructuredAppend != null
            || segment.IsHanziCompatibilityMode)
        {
            throw new QrInputException("The data segment contains unrelated control metadata.", "segment");
        }

        if (allowEciAssignment && segment.EciAssignment >= 0)
        {
            EciEncoder.ValidateAssignment(segment.EciAssignment);
        }

        if (allowEciAssignment && segment.EciAssignment < -1)
        {
            throw new QrInputException("The ECI assignment sentinel is invalid.", "EciAssignment");
        }
    }

    static int GetTextInputByteCount(QrSegment segment, int characterCount, int bytesPerCharacter)
    {
        int textByteCount = CheckedDimensions.CalculateProduct(
            characterCount,
            bytesPerCharacter,
            int.MaxValue,
            "InputBytes");
        if (segment.Data.Length > textByteCount)
        {
            return segment.Data.Length;
        }

        return textByteCount;
    }

    static void ValidateControlSegment(QrSegment segment)
    {
        if (segment.CharacterCount != 0 || segment.Text != null || segment.IsHanziCompatibilityMode)
        {
            throw new QrInputException("Control segments cannot contain text or character counts.", "segment");
        }

        if (segment.Mode == QrEncodingMode.Eci)
        {
            EciEncoder.ValidateAssignment(segment.EciAssignment);
            byte[] expected = EciEncoder.GetDesignatorBytes(segment.EciAssignment);
            if (!BytesEqual(segment.Data, expected)
                || segment.Fnc1ApplicationIndicator != -1
                || segment.StructuredAppend != null)
            {
                throw new QrInputException("The ECI control metadata is inconsistent.", "segment");
            }

            return;
        }

        if (segment.Mode == QrEncodingMode.Fnc1First)
        {
            if (segment.Data.Length != 0
                || segment.EciAssignment != -1
                || segment.Fnc1ApplicationIndicator != -1
                || segment.StructuredAppend != null)
            {
                throw new QrInputException("The FNC1 first-position metadata is inconsistent.", "segment");
            }

            return;
        }

        if (segment.Mode == QrEncodingMode.Fnc1Second)
        {
            Fnc1Encoder.ValidateSecondPositionIndicator(segment.Fnc1ApplicationIndicator);
            if (segment.Data.Length != 1
                || segment.Data[0] != segment.Fnc1ApplicationIndicator
                || segment.EciAssignment != -1
                || segment.StructuredAppend != null)
            {
                throw new QrInputException("The FNC1 second-position metadata is inconsistent.", "segment");
            }

            return;
        }

        if (segment.Mode == QrEncodingMode.StructuredAppend)
        {
            ValidateStructuredAppend(segment);
            return;
        }

        throw new QrInputException("The encoding mode is not defined.", "mode");
    }

    static void ValidateStructuredAppend(QrSegment segment)
    {
        if (segment.StructuredAppend == null)
        {
            throw new QrInputException("Structured append metadata is required.", "StructuredAppend");
        }

        QrStructuredAppendInfo info = segment.StructuredAppend;
        if (info.Total < QrLimits.StructuredAppendMinimumSymbols
            || info.Total > QrLimits.StructuredAppendMaximumSymbols)
        {
            throw new QrInputException("The structured append total is outside the permitted range.", "Total");
        }

        if (info.Index < 0 || info.Index >= info.Total)
        {
            throw new QrInputException("The structured append index is outside the declared sequence.", "Index");
        }

        if (info.Parity < 0 || info.Parity > QrLimits.StructuredAppendMaximumParity)
        {
            throw new QrInputException("The structured append parity must fit in eight bits.", "Parity");
        }

        if (segment.Data.Length != 0
            || segment.EciAssignment != -1
            || segment.Fnc1ApplicationIndicator != -1)
        {
            throw new QrInputException("The structured append control metadata is inconsistent.", "segment");
        }
    }

    static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    static void ValidateCharacterCountWidth(
        QrSegment segment,
        QrSymbolKind symbolKind,
        int version)
    {
        int modeIndex = GetDataModeIndex(segment.Mode);
        if (segment.Mode == QrEncodingMode.Hanzi)
        {
            modeIndex = 3;
        }

        if (modeIndex < 0)
        {
            return;
        }

        int bitWidth;
        if (symbolKind == QrSymbolKind.Model2)
        {
            bitWidth = Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
        }
        else if (symbolKind == QrSymbolKind.Micro)
        {
            bitWidth = MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
        }
        else if (symbolKind == QrSymbolKind.rMQR)
        {
            bitWidth = RectangularMicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
        }
        else
        {
            bitWidth = LegacyModel1Table.GetCharacterCountBitWidth(version, modeIndex);
        }

        int maximumCharacterCount = (1 << bitWidth) - 1;
        if (segment.CharacterCount > maximumCharacterCount)
        {
            throw new QrInputException(
                "The segment character count does not fit the selected profile header.",
                "CharacterCount");
        }
    }
}
