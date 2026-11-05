namespace qrcode;

/// <summary>Contains a lossless decoded payload and the metadata needed to interpret it.</summary>
public class QrDecodeResult
{
    /// <summary>The decoded text, or <c>null</c> when unavailable or not representable.</summary>
    public string? Text;

    /// <summary>The lossless decoded payload bytes.</summary>
    public byte[] RawBytes = new byte[0];

    /// <summary>The decoded payload and control segments in stream order.</summary>
    public QrSegment[] Segments = new QrSegment[0];

    /// <summary>The decoded symbol and its codewords, or <c>null</c> when not retained.</summary>
    public QrSymbol? Symbol;

    /// <summary>The source detection, or <c>null</c> for direct matrix decoding.</summary>
    public QrDetection? Detection;

    /// <summary>The structured-append metadata, or <c>null</c> for an ordinary payload.</summary>
    public QrStructuredAppendInfo? StructuredAppend;

    /// <summary>The ISO/IEC 15424 symbology identifier, or <c>null</c> when unavailable.</summary>
    public string? SymbologyIdentifier;

    /// <summary>The ECI assignments encountered in payload order.</summary>
    public int[] EciAssignments = new int[0];

    /// <summary>Whether FNC1 first-position processing was active.</summary>
    public bool UsesFnc1First;

    /// <summary>The FNC1 second-position application indicator, or -1 when it does not apply.</summary>
    public int Fnc1SecondApplicationIndicator = -1;

    /// <summary>The number of corrected codeword errors.</summary>
    public int CorrectedErrorCount;

    /// <summary>The number of corrected erasures.</summary>
    public int CorrectedErasureCount;

    /// <summary>The individual symbol's Reed-Solomon block results in deinterleaved block order.</summary>
    /// <remarks>Retains actual correction usage and parity capacities. Empty when unavailable, including combined structured-append payloads. Format and version BCH repairs are not included.</remarks>
    public QrCorrectionResult[] ErrorCorrectionBlocks = new QrCorrectionResult[0];

    /// <summary>Whether logical validation found the decoded symbol conformant.</summary>
    public bool IsConformant;

    /// <summary>Whether the payload uses the opt-in Hanzi compatibility extension.</summary>
    public bool UsesHanziCompatibility;

    /// <summary>Non-fatal decode and conformance warnings in processing order.</summary>
    public string[] Warnings = new string[0];
}
