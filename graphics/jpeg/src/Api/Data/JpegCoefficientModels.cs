using System.Collections.Generic;

namespace jpeg;

/// <summary>Contains quantized DCT coefficients and the stream information needed for lossless JPEG inspection and rewriting.</summary>
/// <remarks>Each component stores blocks in row-major order, with 64 coefficients per block in encoded zig-zag order.</remarks>
public class JpegCoefficientImage
{
    /// <summary>Creates an empty coefficient image with initialized metadata, diagnostic, and component collections.</summary>
    public JpegCoefficientImage()
    {
    }

    /// <summary>The image width in pixels.</summary>
    public int Width;
    /// <summary>The image height in pixels.</summary>
    public int Height;
    /// <summary>The number of meaningful bits in each source sample.</summary>
    public int Precision;
    /// <summary>The number of coefficient components.</summary>
    public int ComponentCount;
    /// <summary>The coding process used by the source stream.</summary>
    public JpegProcess Process;
    /// <summary>The entropy coding used by the coefficient scans.</summary>
    public JpegEntropyCoding EntropyCoding;
    /// <summary>Whether the coefficient scans use progressive coding.</summary>
    public bool Progressive;
    /// <summary>Whether the coefficient scans use arithmetic entropy coding.</summary>
    public bool ArithmeticCoding;
    /// <summary>The inferred color space of the components.</summary>
    public JpegColorSpace ColorSpace;
    /// <summary>The inferred component sampling preset.</summary>
    public JpegSubsampling Subsampling;
    /// <summary>The frame header used when the coefficients are written.</summary>
    public JpegFrameHeader Frame = new JpegFrameHeader();
    /// <summary>The source scan headers in encoded order.</summary>
    public List<JpegScanHeader> ScanHeaders = new List<JpegScanHeader>();
    /// <summary>The quantization tables required by the coefficient components.</summary>
    public List<JpegQuantizationTable> QuantizationTables = new List<JpegQuantizationTable>();
    /// <summary>The Huffman tables required by the coefficient scans.</summary>
    public List<JpegHuffmanTable> HuffmanTables = new List<JpegHuffmanTable>();
    /// <summary>The arithmetic-conditioning tables retained for arithmetic-coded coefficient workflows.</summary>
    public List<JpegArithmeticConditioningTable> ArithmeticConditioningTables = new List<JpegArithmeticConditioningTable>();
    /// <summary>Indicates that the source declares a restart interval.</summary>
    public bool HasRestartInterval;
    /// <summary>The number of MCU units between restart markers.</summary>
    public int RestartInterval;
    /// <summary>The unit used by <c>RestartInterval</c>; coefficient streams use encoded MCU blocks.</summary>
    public JpegRestartIntervalUnit RestartIntervalUnit;
    /// <summary>The number of restart markers observed in the source entropy data.</summary>
    public int RestartMarkerCount;
    /// <summary>The source marker segments in stream order, retained for inspection while structural output is regenerated from the coefficient model.</summary>
    public List<JpegMarkerSegment> Markers = new List<JpegMarkerSegment>();
    /// <summary>The metadata retained from the source stream.</summary>
    public JpegMetadata Metadata = new JpegMetadata();
    /// <summary>Recoverable conditions observed while reading coefficients.</summary>
    public List<JpegDiagnostic> Diagnostics = new List<JpegDiagnostic>();
    internal JpegDiagnosticCollection DiagnosticCollection = new JpegDiagnosticCollection();
    /// <summary>The coefficient planes in frame-component order.</summary>
    public List<JpegCoefficientComponent> Components = new List<JpegCoefficientComponent>();
}

/// <summary>Contains all quantized 8-by-8 coefficient blocks for one JPEG frame component.</summary>
public class JpegCoefficientComponent
{
    /// <summary>Creates an empty component with an initialized coefficient buffer.</summary>
    public JpegCoefficientComponent()
    {
    }

    /// <summary>The zero-based position of the component in the frame header.</summary>
    public int ComponentIndex;
    /// <summary>The component selector stored in the JPEG frame.</summary>
    public int Identifier;
    /// <summary>The horizontal sampling factor from one through four.</summary>
    public int HorizontalSamplingFactor;
    /// <summary>The vertical sampling factor from one through four.</summary>
    public int VerticalSamplingFactor;
    /// <summary>The identifier of the quantization table used by this component.</summary>
    public int QuantizationTableIdentifier;
    /// <summary>The number of 8-by-8 blocks in each padded component row.</summary>
    public int BlocksPerRow;
    /// <summary>The number of padded rows of 8-by-8 blocks.</summary>
    public int BlockRows;
    /// <summary>The quantized coefficients, with 64 encoded zig-zag-order values for each row-major block.</summary>
    public int[] Coefficients = new int[0];
}
