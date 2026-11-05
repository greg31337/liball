namespace raw;

internal class JpegMarker
{
    internal byte Code;
    internal int Offset;
    internal int CodeOffset;
    internal int DeclaredLength;
    internal int PayloadOffset;
    internal int PayloadLength;
    internal int NextOffset;
    internal byte[]? RawBytes;

    internal System.ReadOnlyMemory<byte> GetPayload()
    {
        return new System.ReadOnlyMemory<byte>(RawBytes, PayloadOffset, PayloadLength);
    }
}

internal class JpegQuantizationTable
{
    internal JpegMarker? Marker;
    internal int Precision;
    internal int Identifier;
    internal ushort[]? Values;
}

internal class JpegHuffmanTableDefinition
{
    internal JpegMarker? Marker;
    internal int TableClass;
    internal int Identifier;
    internal byte[]? CodeLengthCounts;
    internal byte[]? Symbols;
    internal HuffmanTable? Table;
}

internal class JpegRestartIntervalDefinition
{
    internal JpegMarker? Marker;
    internal int Interval;
}

internal class JpegFrameComponent
{
    internal byte Identifier;
    internal int HorizontalSampling;
    internal int VerticalSampling;
    internal int QuantizationTable;
}

internal class JpegFrameHeader
{
    internal JpegMarker? Marker;
    internal byte ProcessMarker;
    internal int Precision;
    internal int Width;
    internal int Height;
    internal System.Collections.Generic.List<JpegFrameComponent> Components;

    internal JpegFrameHeader()
    {
        Components = new System.Collections.Generic.List<JpegFrameComponent>();
    }
}

internal class JpegScanComponent
{
    internal byte Identifier;
    internal int DcTable;
    internal int AcTable;
}

internal class JpegEntropySegment
{
    internal int Offset;
    internal int EncodedLength;
    internal int DecodedLength;
    internal JpegMarker? RestartMarker;
}

internal class JpegScanHeader
{
    internal JpegMarker? Marker;
    internal JpegFrameHeader? Frame;
    internal System.Collections.Generic.List<JpegScanComponent> Components;
    internal int SpectralStart;
    internal int SpectralEnd;
    internal int SuccessiveApproximationHigh;
    internal int SuccessiveApproximationLow;
    internal int RestartInterval;
    internal int EntropyOffset;
    internal int EntropyLength;
    internal System.Collections.Generic.List<JpegEntropySegment> EntropySegments;
    internal System.Collections.Generic.List<JpegMarker> RestartMarkers;

    internal JpegScanHeader()
    {
        Components = new System.Collections.Generic.List<JpegScanComponent>();
        EntropySegments = new System.Collections.Generic.List<JpegEntropySegment>();
        RestartMarkers = new System.Collections.Generic.List<JpegMarker>();
    }
}

internal class JpegDocument
{
    internal System.ReadOnlyMemory<byte> Source;
    internal System.Collections.Generic.List<JpegMarker> Markers;
    internal System.Collections.Generic.List<JpegQuantizationTable> QuantizationTables;
    internal System.Collections.Generic.List<JpegHuffmanTableDefinition> HuffmanTables;
    internal System.Collections.Generic.List<JpegRestartIntervalDefinition> RestartIntervals;
    internal System.Collections.Generic.List<JpegFrameHeader> Frames;
    internal System.Collections.Generic.List<JpegScanHeader> Scans;
    internal long PreservedMarkerBytes;

    internal JpegDocument(System.ReadOnlyMemory<byte> source)
    {
        Source = source;
        Markers = new System.Collections.Generic.List<JpegMarker>();
        QuantizationTables = new System.Collections.Generic.List<JpegQuantizationTable>();
        HuffmanTables = new System.Collections.Generic.List<JpegHuffmanTableDefinition>();
        RestartIntervals = new System.Collections.Generic.List<JpegRestartIntervalDefinition>();
        Frames = new System.Collections.Generic.List<JpegFrameHeader>();
        Scans = new System.Collections.Generic.List<JpegScanHeader>();
        PreservedMarkerBytes = 0;
    }
}

internal class JpegEntropyParseResult
{
    internal int NextMarkerOffset;
}
