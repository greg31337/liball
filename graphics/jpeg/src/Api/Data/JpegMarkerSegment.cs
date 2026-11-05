using System.Collections.Generic;

namespace jpeg;

/// <summary>Describes one marker and its payload location in a JPEG stream.</summary>
public class JpegMarkerSegment
{
    /// <summary>Creates an empty marker description with an initialized payload buffer.</summary>
    public JpegMarkerSegment()
    {
    }

    /// <summary>The recognized marker kind.</summary>
    public JpegMarker Marker;
    /// <summary>The complete marker value including the <c>0xFF</c> prefix.</summary>
    public int Value;
    /// <summary>The marker's low byte.</summary>
    public int Code;
    /// <summary>The zero-based marker order in the stream.</summary>
    public int Order;
    /// <summary>The byte offset of the marker prefix.</summary>
    public long MarkerOffset;
    /// <summary>The byte offset of the segment-length field, or zero for standalone markers.</summary>
    public long LengthOffset;
    /// <summary>The byte offset of the first payload byte.</summary>
    public long PayloadOffset;
    /// <summary>The byte offset immediately after the marker and its payload.</summary>
    public long EndOffset;
    /// <summary>Indicates that this marker carries a two-byte segment length.</summary>
    public bool HasLength;
    /// <summary>The declared segment length including the two length bytes.</summary>
    public int SegmentLength;
    /// <summary>The number of bytes copied into <c>Payload</c>.</summary>
    public int PayloadLength;
    /// <summary>A copy of the marker payload without marker or length bytes.</summary>
    public byte[] Payload = new byte[0];
    /// <summary>Indicates that entropy-coded bytes occurred immediately before this marker.</summary>
    public bool FoundAfterEntropyData;
    /// <summary>The number of entropy-coded bytes skipped while locating this marker.</summary>
    public long EntropyBytesSkipped;
}

/// <summary>Combines ordered JPEG marker segments with parsed structure, diagnostics, and special-marker summaries.</summary>
public class JpegInspectionResult
{
    /// <summary>Creates an empty inspection result with initialized marker, scan, table, and diagnostic collections.</summary>
    public JpegInspectionResult()
    {
    }

    /// <summary>The complete identifier result produced by the same marker pass.</summary>
    public JpegInfo Info = new JpegInfo();
    /// <summary>Every marker segment in stream order, including structural and restart markers.</summary>
    public List<JpegMarkerSegment> Markers = new List<JpegMarkerSegment>();
    /// <summary>The parsed frame header and component definitions.</summary>
    public JpegFrameHeader Frame = new JpegFrameHeader();
    /// <summary>All parsed scan headers in stream order.</summary>
    public List<JpegScanHeader> ScanHeaders = new List<JpegScanHeader>();
    /// <summary>The parsed quantization, Huffman, and arithmetic-conditioning tables.</summary>
    public JpegTableSet Tables = new JpegTableSet();
    /// <summary>Indicates that a DRI marker declared a restart interval.</summary>
    public bool HasRestartInterval;
    /// <summary>The MCU interval declared by the DRI marker.</summary>
    public int RestartInterval;
    /// <summary>The number of restart markers found in entropy-coded data.</summary>
    public int RestartMarkerCount;
    /// <summary>Indicates that a DNL marker supplied the image height.</summary>
    public bool HasDefinedNumberOfLines;
    /// <summary>The line count supplied by the DNL marker.</summary>
    public int DefinedNumberOfLines;
    /// <summary>Indicates that hierarchical progression was declared.</summary>
    public bool HasHierarchicalProgression;
    /// <summary>The number of DHP markers found.</summary>
    public int HierarchicalProgressionMarkerCount;
    /// <summary>The number of EXP markers found.</summary>
    public int ExpandReferenceComponentMarkerCount;
    /// <summary>Indicates that the frame uses JPEG-LS.</summary>
    public bool IsJpegLs;
    /// <summary>The number of JPEG-LS frame markers found.</summary>
    public int JpegLsMarkerCount;
    /// <summary>Indicates that JPEG extension markers suggest SmartScale data.</summary>
    public bool HasSmartScale;
    /// <summary>The number of SmartScale/JPEG extension markers found.</summary>
    public int SmartScaleMarkerCount;
    /// <summary>Warnings and recoverable parsing diagnostics in encounter order.</summary>
    public List<JpegDiagnostic> Diagnostics = new List<JpegDiagnostic>();
}
