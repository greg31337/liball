namespace qrcode;

internal class ControlSegmentOptimizer
{
    internal static QrSegment[] OptimizeText(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] eciAssignmentPriorities,
        bool useFnc1First,
        int fnc1SecondApplicationIndicator,
        QrStructuredAppendInfo? structuredAppend,
        int maximumSegments,
        int maximumInputBytes)
    {
        ValidateControlCombination(
            symbolKind,
            useFnc1First,
            fnc1SecondApplicationIndicator,
            structuredAppend);
        QrSegment[] dataSegments = EciSegmentOptimizer.OptimizeText(
            text,
            symbolKind,
            version,
            eciAssignmentPriorities,
            maximumSegments,
            maximumInputBytes);

        int controlCount = 0;
        if (structuredAppend != null)
        {
            controlCount++;
        }

        if (useFnc1First || fnc1SecondApplicationIndicator >= 0)
        {
            controlCount++;
        }

        int resultLength = CheckedDimensions.CalculateSum(
            controlCount,
            dataSegments.Length,
            int.MaxValue,
            "SegmentCount");
        QrSegment[] segments = new QrSegment[resultLength];
        int resultIndex = 0;
        if (structuredAppend != null)
        {
            segments[resultIndex] = CreateStructuredAppendSegment(structuredAppend);
            resultIndex++;
        }

        if (useFnc1First)
        {
            segments[resultIndex] = Fnc1Encoder.CreateFirstPositionControlSegment();
            resultIndex++;
        }
        else if (fnc1SecondApplicationIndicator >= 0)
        {
            segments[resultIndex] = Fnc1Encoder.CreateSecondPositionControlSegment(
                fnc1SecondApplicationIndicator);
            resultIndex++;
        }

        for (int index = 0; index < dataSegments.Length; index++)
        {
            segments[resultIndex] = dataSegments[index];
            resultIndex++;
        }

        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            maximumSegments,
            maximumInputBytes,
            false);
        CalculateBitLength(segments, symbolKind, version, false);
        return segments;
    }

    internal static int CalculateBitLength(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility)
    {
        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            int.MaxValue,
            int.MaxValue,
            enableHanziCompatibility);
        int modeIndicatorWidth = GetModeIndicatorWidth(symbolKind, version);
        int totalBitLength = 0;
        bool fnc1Active = false;
        for (int index = 0; index < segments.Length; index++)
        {
            QrSegment segment = segments[index];
            if (segment.Mode == QrEncodingMode.Fnc1First
                || segment.Mode == QrEncodingMode.Fnc1Second)
            {
                fnc1Active = true;
            }

            int segmentBitLength = GetSegmentBitLength(
                segment,
                symbolKind,
                version,
                modeIndicatorWidth,
                fnc1Active);
            totalBitLength = CheckedDimensions.CalculateSum(
                totalBitLength,
                segmentBitLength,
                int.MaxValue,
                "OptimizedBitLength");
        }

        return totalBitLength;
    }

    static void ValidateControlCombination(
        QrSymbolKind symbolKind,
        bool useFnc1First,
        int fnc1SecondApplicationIndicator,
        QrStructuredAppendInfo? structuredAppend)
    {
        if (fnc1SecondApplicationIndicator < -1)
        {
            throw new QrInputException(
                "The FNC1 second-position sentinel is invalid.",
                "fnc1SecondApplicationIndicator");
        }

        if (useFnc1First && fnc1SecondApplicationIndicator >= 0)
        {
            throw new QrInputException(
                "FNC1 first and second position cannot both be requested.",
                "fnc1SecondApplicationIndicator");
        }

        bool hasFnc1 = useFnc1First || fnc1SecondApplicationIndicator >= 0;
        if (hasFnc1
            && symbolKind != QrSymbolKind.Model2
            && symbolKind != QrSymbolKind.rMQR)
        {
            throw new QrInputException(
                "FNC1 is not supported by the selected QR profile.",
                "symbolKind");
        }

        if (fnc1SecondApplicationIndicator >= 0)
        {
            Fnc1Encoder.ValidateSecondPositionIndicator(fnc1SecondApplicationIndicator);
        }

        if (structuredAppend != null)
        {
            if (symbolKind != QrSymbolKind.Model2)
            {
                throw new QrInputException(
                    "Structured append is supported only by Model 2.",
                    "symbolKind");
            }

            ValidateStructuredAppendInfo(structuredAppend);
        }
    }

    static void ValidateStructuredAppendInfo(QrStructuredAppendInfo info)
    {
        if (info.Total < QrLimits.StructuredAppendMinimumSymbols
            || info.Total > QrLimits.StructuredAppendMaximumSymbols)
        {
            throw new QrInputException(
                "The structured-append total is outside the permitted range.",
                "Total");
        }

        if (info.Index < 0 || info.Index >= info.Total)
        {
            throw new QrInputException(
                "The structured-append index is outside the declared sequence.",
                "Index");
        }

        if (info.Parity < 0 || info.Parity > QrLimits.StructuredAppendMaximumParity)
        {
            throw new QrInputException(
                "The structured-append parity must fit in eight bits.",
                "Parity");
        }
    }

    static QrSegment CreateStructuredAppendSegment(QrStructuredAppendInfo source)
    {
        QrStructuredAppendInfo copy = new QrStructuredAppendInfo
        {
            Index = source.Index,
            Total = source.Total,
            Parity = source.Parity,
            ApplicationIdentifier = source.ApplicationIdentifier,
            IsComplete = source.IsComplete
        };
        return new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = copy
        };
    }

    static int GetSegmentBitLength(
        QrSegment segment,
        QrSymbolKind symbolKind,
        int version,
        int modeIndicatorWidth,
        bool fnc1Active)
    {
        if (segment.Mode == QrEncodingMode.Eci)
        {
            return modeIndicatorWidth + EciEncoder.CalculateBitLength(segment.EciAssignment);
        }

        if (segment.Mode == QrEncodingMode.Fnc1First)
        {
            return modeIndicatorWidth;
        }

        if (segment.Mode == QrEncodingMode.Fnc1Second)
        {
            return modeIndicatorWidth + 8;
        }

        if (segment.Mode == QrEncodingMode.StructuredAppend)
        {
            return modeIndicatorWidth + 16;
        }

        int modeIndex = GetModeIndex(segment.Mode);
        int countWidth = GetCharacterCountWidth(symbolKind, version, modeIndex);
        int payloadBitLength;
        if (segment.Mode == QrEncodingMode.Numeric)
        {
            payloadBitLength = NumericEncoder.CalculateBitLength(segment.CharacterCount);
        }
        else if (segment.Mode == QrEncodingMode.Alphanumeric)
        {
            int characterCount = segment.CharacterCount;
            if (fnc1Active && segment.Text != null)
            {
                characterCount = Fnc1Encoder.CalculateEscapedLength(segment.Text);
            }

            payloadBitLength = AlphanumericEncoder.CalculateBitLength(characterCount);
        }
        else if (segment.Mode == QrEncodingMode.Byte)
        {
            payloadBitLength = ByteEncoder.CalculateBitLength(segment.CharacterCount);
        }
        else if (segment.Mode == QrEncodingMode.Kanji)
        {
            payloadBitLength = KanjiEncoder.CalculateBitLength(segment.CharacterCount);
        }
        else
        {
            payloadBitLength = CheckedDimensions.CalculateSum(
                HanziEncoder.SubsetIndicatorBitCount,
                HanziEncoder.CalculateBitLength(segment.CharacterCount),
                int.MaxValue,
                "HanziSegmentBitLength");
        }

        return CheckedDimensions.CalculateSum(
            modeIndicatorWidth + countWidth,
            payloadBitLength,
            int.MaxValue,
            "SegmentBitLength");
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

        return 3;
    }

    static int GetModeIndicatorWidth(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetModeIndicatorBitWidth(version);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.ModeIndicatorBitWidth;
        }

        return 4;
    }

    static int GetCharacterCountWidth(
        QrSymbolKind symbolKind,
        int version,
        int modeIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
        }

        return LegacyModel1Table.GetCharacterCountBitWidth(version, modeIndex);
    }
}
