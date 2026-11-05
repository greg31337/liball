using System.Collections.Generic;

namespace jpeg;

internal class JpegTableStore
{
    internal List<JpegQuantizationTable> QuantizationTables = new List<JpegQuantizationTable>();
    internal List<JpegHuffmanTable> HuffmanTables = new List<JpegHuffmanTable>();
    internal List<JpegArithmeticConditioningTable> ArithmeticConditioningTables = new List<JpegArithmeticConditioningTable>();
}

internal class JpegIccProfileChunk
{
    internal bool IsPresent;
    internal int SequenceNumber;
    internal int ChunkCount;
    internal byte[] Data = new byte[0];
}

internal class JpegExifIfdEntry
{
    internal long EntryOffset;
    internal int Tag;
    internal int Type;
    internal long Count;
    internal long ValueOffset;
}

internal class JpegIccPolicyResult
{
    internal bool HasProfile;
    internal bool Ignored;
    internal bool Preserved;
    internal bool ConversionAttempted;
    internal bool ConversionSupported;
    internal bool Unsupported;
    internal bool ShouldFail;
    internal string Message = "";
    internal byte[] PreservedProfile = new byte[0];
    internal JpegIccProfile Icc = new JpegIccProfile();
    internal JpegIccMatrixTrcProfile MatrixTrcProfile = new JpegIccMatrixTrcProfile();
}

internal class JpegXyzColor
{
    internal double X;
    internal double Y;
    internal double Z;
}

internal class JpegMetadataSegment
{
    internal JpegMarker Marker;
    internal long Offset;
    internal int Order;
    internal byte[] Data = new byte[0];
}
