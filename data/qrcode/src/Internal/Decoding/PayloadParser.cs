namespace qrcode;

internal class PayloadParser
{
    // Input is corrected data in logical block order, with an explicit bit length
    // for Micro's partial last byte. Mapping byte/Kanji/Hanzi text is a later stage.
    // Structural checks always apply; Strict additionally validates pad contents.
    internal static PayloadParseResult Parse(byte[] data, int bitCount, QrSymbolKind kind, int version, QrDecodeOptions options)
    {
        DecodeStatus status = new DecodeStatus();
        PayloadParseResult? result = TryParse(data, bitCount, kind, version, options, status);
        if (result == null) { throw status.Error ?? InvalidData("The payload is invalid."); }
        return result;
    }

    internal static PayloadParseResult? TryParse(byte[] data, int bitCount, QrSymbolKind kind, int version, QrDecodeOptions options, DecodeStatus status)
    {
        if (!ValidateInput(data, bitCount, kind, version, options, status)) { return null; }
        int prefixBits = kind == QrSymbolKind.LegacyModel1 ? 4 : 0;
        BitReader reader = new BitReader(data, bitCount + prefixBits);
        reader.SkipBits(prefixBits);
        PayloadParseResult result = new PayloadParseResult { SymbolKind = kind };
        global::System.Collections.Generic.List<QrSegment> segments = new global::System.Collections.Generic.List<QrSegment>();
        bool fnc1 = false;
        bool sawEciOrData = false;
        int allocated = data.Length + 256;
        int terminatorBits = kind == QrSymbolKind.Micro ? MicroVersionTable.GetTerminatorBitCount(version)
            : kind == QrSymbolKind.rMQR ? 3 : 4;
        int modeBits = kind == QrSymbolKind.Micro ? MicroVersionTable.GetModeIndicatorBitWidth(version)
            : kind == QrSymbolKind.rMQR ? 3 : 4;
        while (reader.GetRemainingBitCount() > 0)
        {
            int start = reader.GetPosition();
            int probeBits = global::System.Math.Min(terminatorBits, reader.GetRemainingBitCount());
            if (reader.ReadBits(probeBits) == 0)
            {
                result.HasTerminator = true;
                result.PayloadBitCount = start - prefixBits;
                if (!ValidatePadding(reader, options.Strict, status)) { return null; }
                break;
            }

            reader.SeekBitPosition(start);
            if (reader.GetRemainingBitCount() < modeBits) { status.Fail(QrFailureReason.InvalidData, "The mode indicator is truncated."); return null; }
            QrEncodingMode mode = ReadMode(reader, kind, version, modeBits);
            if (mode == QrEncodingMode.Terminator) { status.Fail(QrFailureReason.InvalidMode, "The mode is reserved or unsupported."); return null; }
            if (segments.Count >= options.MaximumSegments)
            {
                status.FailLimit("MaximumSegments", segments.Count + 1, options.MaximumSegments);
                return null;
            }

            if (!status.CheckAllocation((long)allocated + 256, options.MaximumAllocationBytes)) { return null; }
            allocated += 256;
            QrSegment? segment;
            if (mode == QrEncodingMode.StructuredAppend)
            {
                if (segments.Count != 0)
                {
                    status.Fail(QrFailureReason.InvalidData, "Structured append must occur once as the first segment."); return null;
                }

                if (reader.GetRemainingBitCount() < 16) { status.Fail(QrFailureReason.InvalidData, "The structured-append header is truncated."); return null; }
                int index = (int)reader.ReadBits(4);
                int total = (int)reader.ReadBits(4) + 1;
                int parity = (int)reader.ReadBits(8);
                if (total < 2 || index >= total)
                {
                    status.Fail(QrFailureReason.InvalidData, "The structured-append position or total is invalid."); return null;
                }

                segment = new QrSegment
                {
                    Mode = mode, StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = total, Parity = parity }
                };
            }
            else if (mode == QrEncodingMode.Fnc1First || mode == QrEncodingMode.Fnc1Second)
            {
                if (fnc1 || sawEciOrData)
                {
                    status.Fail(QrFailureReason.InvalidData, "FNC1 must occur once before ECI and data segments."); return null;
                }

                fnc1 = true;
                int indicator = mode == QrEncodingMode.Fnc1Second ? Fnc1Decoder.TryDecodeSecondPositionIndicator(reader) : -1;
                if (mode == QrEncodingMode.Fnc1Second && indicator < 0) { status.Fail(QrFailureReason.InvalidData, "The FNC1 indicator is invalid or truncated."); return null; }
                segment = new QrSegment { Mode = mode, Fnc1ApplicationIndicator = indicator,
                    Data = indicator < 0 ? new byte[0] : new byte[] { (byte)indicator } };
            }
            else if (mode == QrEncodingMode.Eci)
            {
                int assignment = EciDecoder.TryDecodeAssignment(reader);
                if (assignment < 0) { status.Fail(QrFailureReason.InvalidData, "The ECI designator is invalid or truncated."); return null; }
                segment = new QrSegment { Mode = mode, EciAssignment = assignment, Data = EciEncoder.GetDesignatorBytes(assignment) };
                result.ActiveEciAssignment = segment.EciAssignment;
                sawEciOrData = true;
            }
            else
            {
                if (mode == QrEncodingMode.Hanzi)
                {
                    if (!options.EnableHanziCompatibility)
                    {
                        status.Fail(QrFailureReason.InvalidMode, "Hanzi compatibility is disabled."); return null;
                    }

                    if (reader.GetRemainingBitCount() < 4) { status.Fail(QrFailureReason.InvalidData, "The Hanzi subset is truncated."); return null; }
                    if (reader.ReadBits(4) != 1) { status.Fail(QrFailureReason.InvalidMode, "The Hanzi subset is unsupported."); return null; }
                }

                int modeIndex = DataModeIndex(mode);
                int countBits = kind == QrSymbolKind.LegacyModel1 ? LegacyModel1Table.GetCharacterCountBitWidth(version, modeIndex)
                    : kind == QrSymbolKind.Model2 ? Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version)
                    : kind == QrSymbolKind.Micro ? MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex)
                    : RectangularMicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
                if (reader.GetRemainingBitCount() < countBits) { status.Fail(QrFailureReason.InvalidData, "The character count is truncated."); return null; }
                int count = (int)reader.ReadBits(countBits);
                int requiredBits = modeIndex == 0 ? NumericEncoder.CalculateBitLength(count)
                    : modeIndex == 1 ? AlphanumericEncoder.CalculateBitLength(count)
                    : count * (modeIndex == 2 ? 8 : 13);
                if (requiredBits > reader.GetRemainingBitCount())
                {
                    status.Fail(QrFailureReason.InvalidData, "The payload ends before the declared segment data."); return null;
                }

                if (!status.CheckAllocation((long)allocated + count * 8, options.MaximumAllocationBytes)) { return null; }
                allocated += count * 8;
                segment = ReadData(reader, mode, count, fnc1);
                if (segment == null) { status.Fail(QrFailureReason.InvalidData, "The segment contains an invalid encoded value."); return null; }
                if (mode == QrEncodingMode.Byte)
                {
                    segment.EciAssignment = result.ActiveEciAssignment;
                }

                sawEciOrData = true;
            }

            segments.Add(segment);
            result.PayloadBitCount = reader.GetPosition() - prefixBits;
        }

        result.Segments = segments.ToArray();
        return result;
    }

    static QrSegment? ReadData(BitReader reader, QrEncodingMode mode, int count, bool fnc1)
    {
        QrSegment segment = new QrSegment { Mode = mode, CharacterCount = count };
        if (mode == QrEncodingMode.Numeric || mode == QrEncodingMode.Alphanumeric)
        {
            string? text = mode == QrEncodingMode.Numeric ? NumericDecoder.TryDecodeDigits(reader, count)
                : AlphanumericDecoder.TryDecodeText(reader, count);
            if (text == null) { return null; }
            segment.Data = new byte[text.Length];
            for (int index = 0; index < text.Length; index++)
            {
                segment.Data[index] = (byte)text[index];
            }

            segment.Text = fnc1 && mode == QrEncodingMode.Alphanumeric ? Fnc1Decoder.DecodeGs1AlphanumericText(text) : text;
        }
        else if (mode == QrEncodingMode.Byte)
        {
            segment.Data = ByteDecoder.DecodeBytes(reader, count);
        }
        else
        {
            segment.IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi;
            segment.Data = new byte[count * 2];
            for (int index = 0; index < count; index++)
            {
                int transformed = (int)reader.ReadBits(13);
                int value = mode == QrEncodingMode.Kanji ? KanjiDecoder.TryRestoreShiftJisValue(transformed)
                    : HanziDecoder.TryRestoreGb2312Value(transformed);
                if (value < 0) { return null; }
                segment.Data[index * 2] = (byte)(value >> 8);
                segment.Data[index * 2 + 1] = (byte)(value & 255);
            }
        }

        return segment;
    }

    static QrEncodingMode ReadMode(BitReader reader, QrSymbolKind kind, int version, int modeBits)
    {
        int mode = (int)reader.ReadBits(modeBits);
        if (kind == QrSymbolKind.Micro)
        {
            if (mode <= 3 && MicroVersionTable.IsModeSupported(version, mode))
            {
                return DataMode(mode);
            }
        }
        else if (kind == QrSymbolKind.rMQR)
        {
            if (mode >= 1 && mode <= 4)
            {
                return DataMode(mode - 1);
            }

            if (mode == 5) { return QrEncodingMode.Fnc1First; }
            if (mode == 6) { return QrEncodingMode.Fnc1Second; }
            if (mode == 7) { return QrEncodingMode.Eci; }
        }
        else if (kind == QrSymbolKind.LegacyModel1)
        {
            if (mode == 1 || mode == 2 || mode == 4 || mode == 8) { return (QrEncodingMode)mode; }
        }
        else if (mode == 1 || mode == 2 || mode == 3 || mode == 4 || mode == 5 || mode == 7 || mode == 8 || mode == 9 || mode == 13)
        {
            return (QrEncodingMode)mode;
        }

        return QrEncodingMode.Terminator;
    }

    static QrEncodingMode DataMode(int index)
    {
        if (index == 0) { return QrEncodingMode.Numeric; }
        if (index == 1) { return QrEncodingMode.Alphanumeric; }
        if (index == 2) { return QrEncodingMode.Byte; }
        return QrEncodingMode.Kanji;
    }

    static int DataModeIndex(QrEncodingMode mode)
    {
        if (mode == QrEncodingMode.Numeric) { return 0; }
        if (mode == QrEncodingMode.Alphanumeric) { return 1; }
        if (mode == QrEncodingMode.Byte) { return 2; }
        return 3;
    }

    static bool ValidatePadding(BitReader reader, bool strict, DecodeStatus status)
    {
        if (!strict)
        {
            reader.SkipBits(reader.GetRemainingBitCount());
            return true;
        }

        while (reader.GetRemainingBitCount() > 0 && reader.GetPosition() % 8 != 0)
        {
            if (reader.ReadBit()) { return status.Fail(QrFailureReason.InvalidData, "Terminator alignment bits must be zero."); }
        }

        int pad = 0xEC;
        while (reader.GetRemainingBitCount() >= 8)
        {
            if (reader.ReadBits(8) != (uint)pad) { return status.Fail(QrFailureReason.InvalidData, "The payload pad codewords do not alternate EC and 11."); }
            pad = pad == 0xEC ? 0x11 : 0xEC;
        }

        if (reader.ReadBits(reader.GetRemainingBitCount()) != 0)
        {
            return status.Fail(QrFailureReason.InvalidData, "The final partial pad byte must be zero.");
        }
        return true;
    }

    static bool ValidateInput(byte[] data, int bitCount, QrSymbolKind kind, int version, QrDecodeOptions options, DecodeStatus status)
    {
        if (data == null || options == null)
        {
            throw new QrInputException("Data and decode options are required.", data == null ? "data" : "options");
        }

        int maximumBits;
        int width;
        int height;
        if (kind == QrSymbolKind.LegacyModel1 && version >= 1 && version <= 14)
        {
            maximumBits = LegacyModel1Table.GetDataBitCount(version, 0);
            width = LegacyModel1Table.GetDimension(version);
            height = width;
            if (bitCount < 4 || bitCount % 8 != 4)
            {
                throw new QrInputException("Legacy data must use a four-bit first symbol character.", "bitCount");
            }
        }
        else if (kind == QrSymbolKind.Model2 && version >= 1 && version <= 40)
        {
            maximumBits = EccBlockTable.GetDataCodewordCount(version, 0) * 8;
            width = Model2VersionTable.GetDimension(version);
            height = width;
        }
        else if (kind == QrSymbolKind.Micro && version >= 1 && version <= 4)
        {
            maximumBits = MicroVersionTable.GetDataBitCapacity(version, version == 1 ? -1 : 0);
            width = MicroVersionTable.GetDimension(version);
            height = width;
        }
        else if (kind == QrSymbolKind.rMQR && version >= 0 && version <= 31)
        {
            maximumBits = RectangularMicroVersionTable.GetDataBitCapacity(version, 0);
            width = RectangularMicroVersionTable.GetWidth(version);
            height = RectangularMicroVersionTable.GetHeight(version);
        }
        else
        {
            throw new QrInputException("The payload family or version is unsupported.", "version");
        }

        if (bitCount < 0 || bitCount > maximumBits || data.Length != (bitCount + 7) / 8)
        {
            throw new QrInputException("The data storage and bit length must fit the selected profile.", "bitCount");
        }

        if (options.MaximumSegments <= 0 || options.MaximumAllocationBytes <= 0)
        {
            throw new QrInputException("Segment and allocation limits must be positive.", "options");
        }

        CheckedDimensions.ValidateDimensionBounds(width, height, options.MaximumMatrixDimension, options.MaximumMatrixDimension, int.MaxValue);
        if (!status.CheckAllocation((long)data.Length + 256, options.MaximumAllocationBytes)) { return false; }
        if (kind == QrSymbolKind.LegacyModel1 && (data[0] & 0xF0) != 0)
        {
            return status.Fail(QrFailureReason.InvalidData, "The implicit leading legacy nibble must be zero.");
        }

        if (options.Strict && kind != QrSymbolKind.LegacyModel1 && bitCount % 8 != 0 && (data[data.Length - 1] & ((1 << (8 - bitCount % 8)) - 1)) != 0)
        {
            return status.Fail(QrFailureReason.InvalidData, "Unused storage bits must be zero.");
        }
        return true;
    }

    static QrDecodeException InvalidData(string message)
    {
        return new QrDecodeException(message, QrFailureReason.InvalidData, -1, 0);
    }
}
