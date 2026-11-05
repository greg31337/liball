namespace raw;

internal static class JpegParser
{
    internal const byte StartOfImage = 0xd8;
    internal const byte EndOfImage = 0xd9;
    internal const byte StartOfScan = 0xda;
    internal const byte DefineHuffmanTable = 0xc4;
    internal const byte DefineQuantizationTable = 0xdb;
    internal const byte DefineRestartInterval = 0xdd;

    internal static JpegDocument Parse(System.ReadOnlyMemory<byte> source, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        CheckedMath.FileSize(source.Length, limits);
        CheckedMath.RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        cancellationToken.ThrowIfCancellationRequested();
        if (source.Length < 2)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The JPEG SOI marker is truncated.", source.Length);
        }
        if (source.Span[0] != 0xff)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidSignature, "A JPEG stream must begin with an SOI marker.", 0);
        }
        JpegDocument document = new JpegDocument(source);
        JpegMarker first = ReadMarker(source, 0, document, limits);
        document.Markers.Add(first);
        if (first.Code != StartOfImage)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidSignature, "A JPEG stream must begin with an SOI marker.", first.Offset);
        }

        int offset = first.NextOffset;
        int restartInterval = 0;
        JpegFrameHeader? currentFrame = null;
        while (offset < source.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JpegMarker marker = ReadMarker(source, offset, document, limits);
            document.Markers.Add(marker);
            offset = marker.NextOffset;
            if (marker.Code == StartOfImage)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A JPEG stream cannot contain a second SOI marker.", marker.Offset);
            }
            if (marker.Code == EndOfImage)
            {
                if (offset != source.Length)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG stream contains trailing bytes after EOI.", offset);
                }
                return document;
            }
            if (marker.Code >= 0xd0 && marker.Code <= 0xd7)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG restart marker occurs outside entropy-coded data.", marker.Offset);
            }
            if (marker.Code == DefineQuantizationTable)
            {
                ParseQuantizationTables(marker, document, limits);
                continue;
            }
            if (marker.Code == DefineHuffmanTable)
            {
                ParseHuffmanTables(marker, document, limits);
                continue;
            }
            if (marker.Code == DefineRestartInterval)
            {
                restartInterval = ParseRestartInterval(marker, document);
                continue;
            }
            if (IsFrameMarker(marker.Code))
            {
                currentFrame = ParseFrame(marker, document, limits);
                continue;
            }
            if (marker.Code == StartOfScan)
            {
                JpegScanHeader scan = ParseScan(marker, currentFrame, restartInterval, document);
                JpegEntropyParseResult entropy = ParseEntropy(source, offset, scan, document, limits, cancellationToken);
                offset = entropy.NextMarkerOffset;
            }
        }

        throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The JPEG stream ends before an EOI marker.", source.Length);
    }

    internal static JpegMarker ReadMarker(System.ReadOnlyMemory<byte> source, int offset, JpegDocument document, RawLimits limits)
    {
        System.ReadOnlySpan<byte> bytes = source.Span;
        if (offset < 0 || offset >= bytes.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The JPEG stream ends before a marker prefix.", offset);
        }
        if (bytes[offset] != 0xff)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG marker prefix is required outside entropy-coded data.", offset);
        }

        int codeOffset = offset + 1;
        while (codeOffset < bytes.Length && bytes[codeOffset] == 0xff)
        {
            codeOffset++;
        }
        if (codeOffset >= bytes.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The JPEG marker code is truncated.", bytes.Length);
        }
        byte code = bytes[codeOffset];
        if (code == 0x00)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A stuffed JPEG data byte occurs outside entropy-coded data.", codeOffset);
        }
        if (code != 0x01 && code < 0xc0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The JPEG marker code is reserved or undefined.", codeOffset);
        }

        JpegMarker marker = new JpegMarker();
        marker.Code = code;
        marker.Offset = offset;
        marker.CodeOffset = codeOffset;
        marker.DeclaredLength = 0;
        marker.PayloadOffset = 0;
        marker.PayloadLength = 0;
        if (IsStandaloneMarker(code))
        {
            marker.NextOffset = codeOffset + 1;
            PreserveMarker(source, marker, document, limits);
            return marker;
        }

        if (bytes.Length - codeOffset < 3)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The JPEG marker length is truncated.", codeOffset + 1);
        }
        int declaredLength = bytes[codeOffset + 1] * 256 + bytes[codeOffset + 2];
        if (declaredLength < 2)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A JPEG marker length must include its two length bytes.", codeOffset + 1);
        }
        long nextOffset = CheckedMath.Add(codeOffset + 1, declaredLength);
        if (nextOffset > bytes.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "A JPEG marker payload extends beyond the input.", codeOffset + 1);
        }
        marker.DeclaredLength = declaredLength;
        marker.PayloadOffset = codeOffset - offset + 3;
        marker.PayloadLength = declaredLength - 2;
        marker.NextOffset = CheckedMath.ToInt32(nextOffset);
        PreserveMarker(source, marker, document, limits);
        return marker;
    }

    internal static void PreserveMarker(System.ReadOnlyMemory<byte> source, JpegMarker marker, JpegDocument document, RawLimits limits)
    {
        int rawLength = marker.NextOffset - marker.Offset;
        CheckedMath.AllocationSize(rawLength, 1, limits);
        long preservedBytes = CheckedMath.Add(document.PreservedMarkerBytes, rawLength);
        if (preservedBytes > limits.MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Preserved JPEG markers exceed the configured metadata limit.", marker.Offset);
        }
        marker.RawBytes = source.Slice(marker.Offset, rawLength).ToArray();
        document.PreservedMarkerBytes = preservedBytes;
    }

    internal static void ParseQuantizationTables(JpegMarker marker, JpegDocument document, RawLimits limits)
    {
        System.ReadOnlySpan<byte> payload = marker.GetPayload().Span;
        int offset = 0;
        if (payload.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A DQT marker must contain at least one table.", marker.Offset);
        }
        while (offset < payload.Length)
        {
            int informationOffset = offset;
            int information = payload[offset];
            offset++;
            int precision = information >> 4;
            int identifier = information & 15;
            if (precision > 1 || identifier > 3)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG quantization table has an invalid precision or identifier.", marker.Offset + marker.PayloadOffset + informationOffset);
            }
            int valueBytes = precision == 0 ? 64 : 128;
            if (payload.Length - offset < valueBytes)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "A JPEG quantization table is truncated.", marker.Offset + marker.PayloadOffset + offset);
            }
            CheckedMath.AllocationSize(64, 2, limits);
            JpegQuantizationTable table = new JpegQuantizationTable();
            table.Marker = marker;
            table.Precision = precision == 0 ? 8 : 16;
            table.Identifier = identifier;
            table.Values = new ushort[64];
            int index = 0;
            while (index < 64)
            {
                if (precision == 0)
                {
                    table.Values[index] = payload[offset];
                    offset++;
                }
                else
                {
                    table.Values[index] = (ushort)(payload[offset] * 256 + payload[offset + 1]);
                    offset += 2;
                }
                if (table.Values[index] == 0)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "JPEG quantization values must be nonzero.", marker.Offset + marker.PayloadOffset + offset - 1);
                }
                index++;
            }
            document.QuantizationTables.Add(table);
        }
    }

    internal static void ParseHuffmanTables(JpegMarker marker, JpegDocument document, RawLimits limits)
    {
        System.ReadOnlySpan<byte> payload = marker.GetPayload().Span;
        int offset = 0;
        if (payload.Length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A DHT marker must contain at least one table.", marker.Offset);
        }
        while (offset < payload.Length)
        {
            int informationOffset = offset;
            int information = payload[offset];
            offset++;
            int tableClass = information >> 4;
            int identifier = information & 15;
            if (tableClass > 1 || identifier > 3)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG Huffman table has an invalid class or identifier.", marker.Offset + marker.PayloadOffset + informationOffset);
            }
            if (payload.Length - offset < 16)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "JPEG Huffman code-length counts are truncated.", marker.Offset + marker.PayloadOffset + offset);
            }
            byte[] counts = payload.Slice(offset, 16).ToArray();
            offset += 16;
            int symbolCount = 0;
            int length = 0;
            while (length < counts.Length)
            {
                symbolCount += counts[length];
                length++;
            }
            if (symbolCount == 0 || symbolCount > 256)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG Huffman table has an invalid symbol count.", marker.Offset + marker.PayloadOffset + offset);
            }
            if (payload.Length - offset < symbolCount)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "JPEG Huffman symbols are truncated.", marker.Offset + marker.PayloadOffset + offset);
            }
            CheckedMath.AllocationSize(symbolCount, 1, limits);
            byte[] symbols = payload.Slice(offset, symbolCount).ToArray();
            offset += symbolCount;
            JpegHuffmanTableDefinition definition = new JpegHuffmanTableDefinition();
            definition.Marker = marker;
            definition.TableClass = tableClass;
            definition.Identifier = identifier;
            definition.CodeLengthCounts = counts;
            definition.Symbols = symbols;
            definition.Table = new HuffmanTable(counts, symbols, 8);
            if (definition.Table.IsComplete())
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG Huffman table cannot assign an all-ones code.", marker.Offset + marker.PayloadOffset + informationOffset);
            }
            document.HuffmanTables.Add(definition);
        }
    }

    internal static int ParseRestartInterval(JpegMarker marker, JpegDocument document)
    {
        System.ReadOnlySpan<byte> payload = marker.GetPayload().Span;
        if (payload.Length != 2)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A DRI marker must contain exactly one 16-bit restart interval.", marker.Offset);
        }
        int interval = payload[0] * 256 + payload[1];
        JpegRestartIntervalDefinition definition = new JpegRestartIntervalDefinition();
        definition.Marker = marker;
        definition.Interval = interval;
        document.RestartIntervals.Add(definition);
        return interval;
    }

    internal static JpegFrameHeader ParseFrame(JpegMarker marker, JpegDocument document, RawLimits limits)
    {
        System.ReadOnlySpan<byte> payload = marker.GetPayload().Span;
        if (payload.Length < 6)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "A JPEG SOF marker is truncated.", marker.Offset);
        }
        int componentCount = payload[5];
        int expectedLength = 6 + componentCount * 3;
        if (componentCount == 0 || payload.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A JPEG SOF component count does not match its marker length.", marker.Offset);
        }
        int precision = payload[0];
        int height = payload[1] * 256 + payload[2];
        int width = payload[3] * 256 + payload[4];
        if (precision < 2 || precision > 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "JPEG sample precision must be between 2 and 16 bits.", marker.Offset + marker.PayloadOffset);
        }
        CheckedMath.RequirePositiveLimit(limits.MaxWidth, "The configured width limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxHeight, "The configured height limit must be greater than zero.");
        if (width == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "JPEG frame width must be greater than zero.", marker.Offset + marker.PayloadOffset + 3);
        }
        if (width > limits.MaxWidth || height > limits.MaxHeight)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "JPEG frame dimensions exceed the configured limits.", marker.Offset);
        }
        if (height > 0)
        {
            CheckedMath.PixelCount(width, height, limits);
        }

        JpegFrameHeader frame = new JpegFrameHeader();
        frame.Marker = marker;
        frame.ProcessMarker = marker.Code;
        frame.Precision = precision;
        frame.Width = width;
        frame.Height = height;
        bool[] identifiers = new bool[256];
        int offset = 6;
        int index = 0;
        while (index < componentCount)
        {
            JpegFrameComponent component = new JpegFrameComponent();
            component.Identifier = payload[offset];
            component.HorizontalSampling = payload[offset + 1] >> 4;
            component.VerticalSampling = payload[offset + 1] & 15;
            component.QuantizationTable = payload[offset + 2];
            if (identifiers[component.Identifier])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG frame repeats a component identifier.", marker.Offset + marker.PayloadOffset + offset);
            }
            if (component.HorizontalSampling < 1 || component.HorizontalSampling > 4 || component.VerticalSampling < 1 || component.VerticalSampling > 4)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "JPEG sampling factors must be between one and four.", marker.Offset + marker.PayloadOffset + offset + 1);
            }
            if (component.QuantizationTable > 3)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG frame references an invalid quantization table identifier.", marker.Offset + marker.PayloadOffset + offset + 2);
            }
            identifiers[component.Identifier] = true;
            frame.Components.Add(component);
            offset += 3;
            index++;
        }
        document.Frames.Add(frame);
        return frame;
    }

    internal static JpegScanHeader ParseScan(JpegMarker marker, JpegFrameHeader? frame, int restartInterval, JpegDocument document)
    {
        if (frame == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A JPEG SOS marker requires a preceding frame header.", marker.Offset);
        }
        System.ReadOnlySpan<byte> payload = marker.GetPayload().Span;
        if (payload.Length < 4)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "A JPEG SOS marker is truncated.", marker.Offset);
        }
        int componentCount = payload[0];
        int expectedLength = 4 + componentCount * 2;
        if (componentCount < 1 || componentCount > 4 || payload.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A JPEG SOS component count does not match its marker length or scan limit.", marker.Offset);
        }

        JpegScanHeader scan = new JpegScanHeader();
        scan.Marker = marker;
        scan.Frame = frame;
        scan.RestartInterval = restartInterval;
        bool[] identifiers = new bool[256];
        int offset = 1;
        int index = 0;
        while (index < componentCount)
        {
            JpegScanComponent component = new JpegScanComponent();
            component.Identifier = payload[offset];
            component.DcTable = payload[offset + 1] >> 4;
            component.AcTable = payload[offset + 1] & 15;
            if (identifiers[component.Identifier] || !FrameContainsComponent(frame, component.Identifier))
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG scan component is duplicated or absent from its frame.", marker.Offset + marker.PayloadOffset + offset);
            }
            if (component.DcTable > 3 || component.AcTable > 3)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG scan references an invalid Huffman table identifier.", marker.Offset + marker.PayloadOffset + offset + 1);
            }
            identifiers[component.Identifier] = true;
            scan.Components.Add(component);
            offset += 2;
            index++;
        }
        scan.SpectralStart = payload[offset];
        scan.SpectralEnd = payload[offset + 1];
        scan.SuccessiveApproximationHigh = payload[offset + 2] >> 4;
        scan.SuccessiveApproximationLow = payload[offset + 2] & 15;
        scan.EntropyOffset = marker.NextOffset;
        document.Scans.Add(scan);
        return scan;
    }

    internal static JpegEntropyParseResult ParseEntropy(System.ReadOnlyMemory<byte> source, int offset, JpegScanHeader scan, JpegDocument document, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        System.ReadOnlySpan<byte> bytes = source.Span;
        int segmentStart = offset;
        int decodedLength = 0;
        int expectedRestart = 0;
        while (offset < bytes.Length)
        {
            if ((offset & 65535) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (bytes[offset] != 0xff)
            {
                offset++;
                decodedLength++;
                continue;
            }

            int prefixOffset = offset;
            int codeOffset = offset + 1;
            while (codeOffset < bytes.Length && bytes[codeOffset] == 0xff)
            {
                codeOffset++;
            }
            if (codeOffset >= bytes.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "JPEG entropy data ends within a marker prefix.", bytes.Length);
            }
            byte code = bytes[codeOffset];
            if (code == 0x00)
            {
                if (codeOffset != prefixOffset + 1)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A stuffed JPEG entropy byte cannot contain marker fill bytes.", prefixOffset);
                }
                decodedLength++;
                offset = codeOffset + 1;
                continue;
            }

            if (code >= 0xd0 && code <= 0xd7)
            {
                if (scan.RestartInterval == 0)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG restart marker requires a nonzero DRI value.", prefixOffset);
                }
                if (code != 0xd0 + expectedRestart)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "JPEG restart markers are not in RST0 through RST7 sequence.", prefixOffset);
                }
                if (decodedLength == 0)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG restart interval contains no entropy data.", prefixOffset);
                }
                JpegMarker restart = CreateRestartMarker(source, prefixOffset, codeOffset, document, limits);
                document.Markers.Add(restart);
                scan.RestartMarkers.Add(restart);
                JpegEntropySegment segment = CreateEntropySegment(segmentStart, prefixOffset, decodedLength, restart);
                scan.EntropySegments.Add(segment);
                expectedRestart = (expectedRestart + 1) & 7;
                offset = codeOffset + 1;
                segmentStart = offset;
                decodedLength = 0;
                continue;
            }

            if (decodedLength == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A JPEG scan contains no entropy data after its last marker boundary.", prefixOffset);
            }
            JpegEntropySegment finalSegment = CreateEntropySegment(segmentStart, prefixOffset, decodedLength, null);
            scan.EntropySegments.Add(finalSegment);
            scan.EntropyLength = prefixOffset - scan.EntropyOffset;
            JpegEntropyParseResult result = new JpegEntropyParseResult();
            result.NextMarkerOffset = prefixOffset;
            return result;
        }

        throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "JPEG entropy data ends before a terminating marker.", bytes.Length);
    }

    internal static JpegEntropySegment CreateEntropySegment(int start, int end, int decodedLength, JpegMarker? restartMarker)
    {
        JpegEntropySegment segment = new JpegEntropySegment();
        segment.Offset = start;
        segment.EncodedLength = end - start;
        segment.DecodedLength = decodedLength;
        segment.RestartMarker = restartMarker;
        return segment;
    }

    internal static JpegMarker CreateRestartMarker(System.ReadOnlyMemory<byte> source, int prefixOffset, int codeOffset, JpegDocument document, RawLimits limits)
    {
        JpegMarker marker = new JpegMarker();
        marker.Code = source.Span[codeOffset];
        marker.Offset = prefixOffset;
        marker.CodeOffset = codeOffset;
        marker.DeclaredLength = 0;
        marker.PayloadOffset = 0;
        marker.PayloadLength = 0;
        marker.NextOffset = codeOffset + 1;
        PreserveMarker(source, marker, document, limits);
        return marker;
    }

    internal static bool FrameContainsComponent(JpegFrameHeader frame, byte identifier)
    {
        int index = 0;
        while (index < frame.Components.Count)
        {
            if (frame.Components[index].Identifier == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool IsStandaloneMarker(byte code)
    {
        return code == 0x01 || code == StartOfImage || code == EndOfImage || code >= 0xd0 && code <= 0xd7;
    }

    internal static bool IsFrameMarker(byte code)
    {
        if (code >= 0xc0 && code <= 0xc3)
        {
            return true;
        }
        if (code >= 0xc5 && code <= 0xc7)
        {
            return true;
        }
        if (code >= 0xc9 && code <= 0xcb)
        {
            return true;
        }
        return code >= 0xcd && code <= 0xcf;
    }
}
