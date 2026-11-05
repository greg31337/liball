namespace qrcode;

internal class CodewordFinalizer
{
    internal static int CalculateRequiredBitCount(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility,
        int maximumSegments,
        int maximumInputBytes)
    {
        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            maximumSegments,
            maximumInputBytes,
            enableHanziCompatibility);
        BitBuffer buffer = new BitBuffer(0);
        bool fnc1Active = false;
        for (int index = 0; index < segments.Length; index++)
        {
            fnc1Active = AppendSegment(
                segments[index],
                symbolKind,
                version,
                enableHanziCompatibility,
                fnc1Active,
                buffer);
        }

        return buffer.GetBitLength();
    }

    internal static byte[] CreateDataCodewords(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex,
        bool enableHanziCompatibility)
    {
        int capacityBitCount = GetCapacityBitCount(
            symbolKind,
            version,
            errorCorrectionIndex);
        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            int.MaxValue,
            int.MaxValue,
            enableHanziCompatibility);
        BitBuffer buffer = new BitBuffer((capacityBitCount + 7) / 8);
        bool fnc1Active = false;
        for (int index = 0; index < segments.Length; index++)
        {
            fnc1Active = AppendSegment(
                segments[index],
                symbolKind,
                version,
                enableHanziCompatibility,
                fnc1Active,
                buffer);
        }

        int requiredBitCount = buffer.GetBitLength();
        if (requiredBitCount > capacityBitCount)
        {
            throw new QrCapacityException(
                "The finalized segment bitstream exceeds the selected symbol capacity.",
                symbolKind,
                requiredBitCount,
                capacityBitCount);
        }

        int terminatorBitCount = GetTerminatorBitCount(symbolKind, version);
        int remainingBitCount = capacityBitCount - buffer.GetBitLength();
        if (terminatorBitCount > remainingBitCount)
        {
            terminatorBitCount = remainingBitCount;
        }

        AppendZeroBits(buffer, terminatorBitCount);
        int alignmentOffset = symbolKind == QrSymbolKind.LegacyModel1 ? 4 : 0;
        while (buffer.GetBitLength() < capacityBitCount
            && (buffer.GetBitLength() - alignmentOffset) % 8 != 0)
        {
            buffer.AppendBit(false);
        }

        bool useFirstPadCodeword = true;
        while (capacityBitCount - buffer.GetBitLength() >= 8)
        {
            int padCodeword = useFirstPadCodeword ? 0xEC : 0x11;
            buffer.AppendBits((uint)padCodeword, 8);
            useFirstPadCodeword = !useFirstPadCodeword;
        }

        AppendZeroBits(buffer, capacityBitCount - buffer.GetBitLength());
        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            return CreateLegacyModel1Bytes(buffer, capacityBitCount);
        }

        return buffer.GetBytes();
    }

    static bool AppendSegment(
        QrSegment segment,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility,
        bool fnc1Active,
        BitBuffer destination)
    {
        AppendModeIndicator(segment.Mode, symbolKind, version, destination);
        if (segment.Mode == QrEncodingMode.Eci)
        {
            EciEncoder.EncodeAssignment(segment.EciAssignment, destination);
            return fnc1Active;
        }

        if (segment.Mode == QrEncodingMode.Fnc1First)
        {
            return true;
        }

        if (segment.Mode == QrEncodingMode.Fnc1Second)
        {
            Fnc1Encoder.EncodeSecondPositionIndicator(
                segment.Fnc1ApplicationIndicator,
                destination);
            return true;
        }

        if (segment.Mode == QrEncodingMode.StructuredAppend)
        {
            if (segment.StructuredAppend == null)
            {
                throw new QrInputException(
                    "Structured append metadata is required.",
                    "StructuredAppend");
            }

            QrStructuredAppendInfo info = segment.StructuredAppend;
            destination.AppendBits((uint)info.Index, 4);
            destination.AppendBits((uint)(info.Total - 1), 4);
            destination.AppendBits((uint)info.Parity, 8);
            return fnc1Active;
        }

        int modeIndex = GetDataModeIndex(segment.Mode);
        int countBitWidth = GetCharacterCountBitWidth(
            symbolKind,
            version,
            modeIndex);
        int encodedCharacterCount = segment.CharacterCount;
        string text = "";
        if (segment.Mode != QrEncodingMode.Byte)
        {
            text = GetSegmentText(segment);
        }

        if (fnc1Active && segment.Mode == QrEncodingMode.Alphanumeric)
        {
            encodedCharacterCount = Fnc1Encoder.CalculateEscapedLength(text);
        }

        if (segment.Mode == QrEncodingMode.Hanzi)
        {
            HanziEncoder.EncodeSubsetIndicator(destination, enableHanziCompatibility);
        }

        destination.AppendBits((uint)encodedCharacterCount, countBitWidth);
        if (segment.Mode == QrEncodingMode.Numeric)
        {
            NumericEncoder.EncodeDigits(text, destination);
        }
        else if (segment.Mode == QrEncodingMode.Alphanumeric)
        {
            if (fnc1Active)
            {
                Fnc1Encoder.EncodeFirstPositionText(text, destination);
            }
            else
            {
                AlphanumericEncoder.EncodeText(text, destination);
            }
        }
        else if (segment.Mode == QrEncodingMode.Byte)
        {
            ByteEncoder.EncodeBytes(segment.Data, destination);
        }
        else if (segment.Mode == QrEncodingMode.Kanji)
        {
            KanjiEncoder.EncodeText(text, destination);
        }
        else
        {
            HanziEncoder.EncodeText(text, destination, enableHanziCompatibility);
        }

        return fnc1Active;
    }

    static void AppendModeIndicator(
        QrEncodingMode mode,
        QrSymbolKind symbolKind,
        int version,
        BitBuffer destination)
    {
        if (symbolKind == QrSymbolKind.Micro)
        {
            int modeIndex = GetDataModeIndex(mode);
            int indicator = MicroVersionTable.GetModeIndicator(version, modeIndex);
            destination.AppendBits(
                (uint)indicator,
                MicroVersionTable.GetModeIndicatorBitWidth(version));
            return;
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            int indicator;
            int modeIndex = GetDataModeIndex(mode);
            if (modeIndex >= 0)
            {
                indicator = RectangularMicroVersionTable.GetModeIndicator(modeIndex);
            }
            else if (mode == QrEncodingMode.Fnc1First)
            {
                indicator = RectangularMicroVersionTable.Fnc1FirstModeIndicator;
            }
            else if (mode == QrEncodingMode.Fnc1Second)
            {
                indicator = RectangularMicroVersionTable.Fnc1SecondModeIndicator;
            }
            else
            {
                indicator = RectangularMicroVersionTable.EciModeIndicator;
            }

            destination.AppendBits(
                (uint)indicator,
                RectangularMicroVersionTable.ModeIndicatorBitWidth);
            return;
        }

        destination.AppendBits((uint)mode, 4);
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

    static int GetCharacterCountBitWidth(
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
            return RectangularMicroVersionTable.GetCharacterCountBitWidth(
                version,
                modeIndex);
        }

        return LegacyModel1Table.GetCharacterCountBitWidth(version, modeIndex);
    }

    static int GetCapacityBitCount(
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return EccBlockTable.GetDataCodewordCount(version, errorCorrectionIndex) * 8;
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetDataBitCapacity(version, errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.GetDataBitCapacity(
                version,
                errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            return LegacyModel1Table.GetDataBitCount(version, errorCorrectionIndex);
        }

        throw new QrInputException("The symbol kind is not defined.", "symbolKind");
    }

    static int GetTerminatorBitCount(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetTerminatorBitCount(version);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.TerminatorBitCount;
        }

        return 4;
    }

    static string GetSegmentText(QrSegment segment)
    {
        if (segment.Text == null)
        {
            throw new QrInputException("The text segment must retain its source text.", "Text");
        }

        return segment.Text;
    }

    static void AppendZeroBits(BitBuffer destination, int count)
    {
        for (int index = 0; index < count; index++)
        {
            destination.AppendBit(false);
        }
    }

    static byte[] CreateLegacyModel1Bytes(BitBuffer buffer, int capacityBitCount)
    {
        int byteCount = (capacityBitCount + 4) / 8;
        byte[] result = new byte[byteCount];
        int bitIndex = 0;
        for (int index = 0; index < 4; index++)
        {
            result[0] <<= 1;
            if (buffer.GetBit(bitIndex))
            {
                result[0] = (byte)(result[0] | 1);
            }

            bitIndex++;
        }

        for (int byteIndex = 1; byteIndex < result.Length; byteIndex++)
        {
            for (int index = 0; index < 8; index++)
            {
                result[byteIndex] <<= 1;
                if (buffer.GetBit(bitIndex))
                {
                    result[byteIndex] = (byte)(result[byteIndex] | 1);
                }

                bitIndex++;
            }
        }

        return result;
    }
}
