using System.Collections.Generic;

namespace jpeg;

/// <summary>Describes one JPEG frame header and its ordered component definitions.</summary>
public class JpegFrameHeader
{
    /// <summary>Creates an empty frame header with an initialized component collection.</summary>
    public JpegFrameHeader()
    {
    }

    /// <summary>The start-of-frame marker.</summary>
    public JpegMarker Marker;
    /// <summary>The numeric marker value, including the marker prefix.</summary>
    public int Value;
    /// <summary>The coding process selected by the frame marker.</summary>
    public JpegProcess Process;
    /// <summary>The image width declared by the frame.</summary>
    public int Width;
    /// <summary>The image height declared by the frame.</summary>
    public int Height;
    /// <summary>The number of meaningful bits in each sample.</summary>
    public int Precision;
    /// <summary>The number of frame components.</summary>
    public int ComponentCount;
    /// <summary>Whether the frame uses progressive coding.</summary>
    public bool Progressive;
    /// <summary>Whether the frame uses arithmetic entropy coding.</summary>
    public bool ArithmeticCoding;
    /// <summary>The frame components in encoded order.</summary>
    public List<JpegFrameComponent> Components = new List<JpegFrameComponent>();
}

/// <summary>Describes one component entry in a JPEG frame header.</summary>
public class JpegFrameComponent
{
    /// <summary>Creates an empty frame-component definition.</summary>
    public JpegFrameComponent()
    {
    }

    /// <summary>The component identifier stored in the frame.</summary>
    public int Identifier;
    /// <summary>The horizontal sampling factor from one through four.</summary>
    public int HorizontalSamplingFactor;
    /// <summary>The vertical sampling factor from one through four.</summary>
    public int VerticalSamplingFactor;
    /// <summary>The identifier of the component's quantization table.</summary>
    public int QuantizationTableIdentifier;
}

/// <summary>Maps one source component into the encoded frame order and sampling layout.</summary>
public class JpegEncoderComponent
{
    /// <summary>Creates a component mapping with unit sampling factors.</summary>
    public JpegEncoderComponent()
    {
    }

    /// <summary>The zero-based source component index.</summary>
    public int SourceComponentIndex;
    /// <summary>The identifier written into the JPEG frame.</summary>
    public int Identifier;
    /// <summary>The horizontal sampling factor from one through four.</summary>
    public int HorizontalSamplingFactor = 1;
    /// <summary>The vertical sampling factor from one through four.</summary>
    public int VerticalSamplingFactor = 1;
    /// <summary>The quantization-table identifier from zero through three, or minus one for automatic assignment.</summary>
    public int QuantizationTableIdentifier = -1;
}

/// <summary>Assigns entropy tables to one component entry in a generated encoder scan.</summary>
public class JpegEncoderScanTableAssignment
{
    /// <summary>Creates an assignment with automatic DC and AC table selection.</summary>
    public JpegEncoderScanTableAssignment()
    {
    }

    /// <summary>The zero-based generated scan index.</summary>
    public int ScanIndex;
    /// <summary>The encoded component identifier selected in that scan.</summary>
    public int ComponentIdentifier;
    /// <summary>The DC or lossless table identifier from zero through three, or minus one to retain the generated assignment.</summary>
    public int DcEntropyTableIdentifier = -1;
    /// <summary>The AC table identifier from zero through three, or minus one to retain the generated assignment.</summary>
    public int AcEntropyTableIdentifier = -1;
}

/// <summary>Describes one JPEG scan header and its ordered component selectors.</summary>
public class JpegScanHeader
{
    /// <summary>Creates an empty scan header with an initialized component collection.</summary>
    public JpegScanHeader()
    {
    }

    /// <summary>The number of components participating in the scan.</summary>
    public int ComponentCount;
    /// <summary>The first spectral coefficient selected by the scan.</summary>
    public int SpectralStart;
    /// <summary>The last spectral coefficient selected by the scan.</summary>
    public int SpectralEnd;
    /// <summary>The high bit position used by successive approximation.</summary>
    public int SuccessiveHigh;
    /// <summary>The low bit position used by successive approximation.</summary>
    public int SuccessiveLow;
    /// <summary>The scan components in encoded order.</summary>
    public List<JpegScanComponent> Components = new List<JpegScanComponent>();
}

/// <summary>Describes one component selector and its entropy-table assignments in a JPEG scan.</summary>
public class JpegScanComponent
{
    /// <summary>Creates an empty scan-component definition.</summary>
    public JpegScanComponent()
    {
    }

    /// <summary>The frame-component identifier selected by this scan entry.</summary>
    public int Selector;
    /// <summary>The identifier of the DC or lossless entropy table.</summary>
    public int DcEntropyTableIdentifier;
    /// <summary>The identifier of the AC entropy table.</summary>
    public int AcEntropyTableIdentifier;
}

/// <summary>Contains an ordered progressive JPEG scan script.</summary>
/// <remarks>An empty script on <see cref="JpegWriteOptions"/> selects the encoder's standard DC-first and per-component AC script.</remarks>
public class JpegProgressiveScanScript
{
    /// <summary>Creates an empty script with an initialized scan collection.</summary>
    public JpegProgressiveScanScript()
    {
    }

    /// <summary>The progressive scan headers in encoding order.</summary>
    public List<JpegScanHeader> Scans = new List<JpegScanHeader>();
}

/// <summary>Contains one JPEG quantization table in natural and zig-zag coefficient order.</summary>
public class JpegQuantizationTable
{
    /// <summary>Creates an empty quantization table with initialized 64-entry value arrays.</summary>
    public JpegQuantizationTable()
    {
    }

    /// <summary>The table identifier from zero through three.</summary>
    public int Identifier;
    /// <summary>The table precision in bits: 8 or 16.</summary>
    public int Precision;
    /// <summary>Whether the encoded table uses sixteen-bit values.</summary>
    public bool Is16Bit;
    /// <summary>The 64 quantizers in natural coefficient order.</summary>
    public int[] Values = new int[64];
    /// <summary>The 64 quantizers in encoded zig-zag order.</summary>
    public int[] ZigZagValues = new int[64];
}

/// <summary>Contains one JPEG Huffman table as code-length counts and ordered symbols.</summary>
public class JpegHuffmanTable
{
    /// <summary>Creates an empty Huffman table with initialized code-count and symbol arrays.</summary>
    public JpegHuffmanTable()
    {
    }

    /// <summary>The table class: zero for DC or lossless and one for AC.</summary>
    public int TableClass;
    /// <summary>The table identifier from zero through three.</summary>
    public int Identifier;
    /// <summary>The number of codes of lengths one through sixteen.</summary>
    public int[] CodeCounts = new int[16];
    /// <summary>The number of symbols described by <c>CodeCounts</c>.</summary>
    public int SymbolCount;
    /// <summary>The symbols in canonical Huffman code order.</summary>
    public int[] Symbols = new int[0];
    internal int[] Codes = new int[0];
    internal int[] CodeLengths = new int[0];
    internal int[] MinCode = new int[17];
    internal int[] MaxCode = new int[17];
    internal int[] ValueOffset = new int[17];
}

/// <summary>Contains one JPEG arithmetic-conditioning table.</summary>
public class JpegArithmeticConditioningTable
{
    /// <summary>Creates an empty arithmetic-conditioning table.</summary>
    public JpegArithmeticConditioningTable()
    {
    }

    /// <summary>The table class: zero for DC or lossless and one for AC.</summary>
    public int TableClass;
    /// <summary>The table identifier from zero through three.</summary>
    public int Identifier;
    /// <summary>The encoded conditioning byte.</summary>
    public int ConditioningValue;
    /// <summary>The lower DC conditioning bound.</summary>
    public int Lower;
    /// <summary>The upper DC conditioning bound.</summary>
    public int Upper;
    /// <summary>The AC conditioning value.</summary>
    public int Kx;
}

/// <summary>Contains the quantization and entropy tables shared by abbreviated JPEG streams.</summary>
public class JpegTableSet
{
    /// <summary>Creates an empty table set with initialized table collections.</summary>
    public JpegTableSet()
    {
    }

    /// <summary>The quantization tables in definition order.</summary>
    public List<JpegQuantizationTable> QuantizationTables = new List<JpegQuantizationTable>();
    /// <summary>The Huffman tables in definition order.</summary>
    public List<JpegHuffmanTable> HuffmanTables = new List<JpegHuffmanTable>();
    /// <summary>The arithmetic-conditioning tables in definition order.</summary>
    public List<JpegArithmeticConditioningTable> ArithmeticConditioningTables = new List<JpegArithmeticConditioningTable>();
}

/// <summary>Contains one preserved APPn, COM, or unknown JPEG marker payload and its original location.</summary>
public class JpegRawMarker
{
    /// <summary>Creates an empty preserved marker with an initialized payload.</summary>
    public JpegRawMarker()
    {
    }

    /// <summary>The recognized marker enum, or <see cref="JpegMarker.Unknown"/>.</summary>
    public JpegMarker Marker;
    /// <summary>The numeric marker value, including the marker prefix.</summary>
    public int Value;
    /// <summary>The marker code byte without the prefix.</summary>
    public int Code;
    /// <summary>The zero-based source order in which the marker appeared; list position controls subsequent write order.</summary>
    public int Order;
    /// <summary>The source byte offset of the marker prefix.</summary>
    public long Offset;
    /// <summary>The retained payload length in bytes.</summary>
    public int PayloadLength;
    /// <summary>A copied marker payload without its length field.</summary>
    public byte[] Payload = new byte[0];
    /// <summary>Whether the marker is APP0 through APP15.</summary>
    public bool IsApplicationMarker;
    /// <summary>Whether the marker is COM.</summary>
    public bool IsComment;
    /// <summary>Whether the marker is not otherwise recognized by the library.</summary>
    public bool IsUnknownMarker;
}
