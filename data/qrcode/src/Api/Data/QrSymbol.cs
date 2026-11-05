namespace qrcode;

/// <summary>Contains an encoded or decoded QR symbol and its exact construction metadata.</summary>
public class QrSymbol
{
    /// <summary>The standardized QR family.</summary>
    public QrSymbolKind SymbolKind = QrSymbolKind.Model2;

    /// <summary>The numbered symbol version, or zero when the profile uses explicit dimensions.</summary>
    public int Version;

    /// <summary>The symbol width in modules, excluding any quiet zone.</summary>
    public int Width;

    /// <summary>The symbol height in modules, excluding any quiet zone.</summary>
    public int Height;

    /// <summary>The selected error-correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel = QrErrorCorrectionLevel.None;

    /// <summary>The selected mask pattern.</summary>
    public QrMaskPattern MaskPattern = QrMaskPattern.Automatic;

    /// <summary>The completed module matrix, or <c>null</c> when unavailable.</summary>
    public QrMatrix? Matrix;

    /// <summary>The final interleaved codewords in placement order.</summary>
    public byte[] Codewords = new byte[0];

    /// <summary>The payload and control segments in symbol order.</summary>
    public QrSegment[] Segments = new QrSegment[0];

    /// <summary>The number of data codewords.</summary>
    public int DataCodewordCount;

    /// <summary>The number of error-correction codewords.</summary>
    public int ErrorCorrectionCodewordCount;

    /// <summary>The total number of data and error-correction codewords.</summary>
    public int TotalCodewordCount;

    /// <summary>The number of remainder bits after codeword placement.</summary>
    public int RemainderBitCount;

    /// <summary>The available data capacity in bits.</summary>
    public int DataBitCapacity;

    /// <summary>The number of data bits used before final padding.</summary>
    public int UsedDataBitCount;

    /// <summary>The number of unused data-capacity bits.</summary>
    public int UnusedDataBitCount;

    /// <summary>Whether logical validation found the symbol conformant.</summary>
    public bool IsConformant;

    /// <summary>The detailed validation result, or <c>null</c> when validation was not performed.</summary>
    public QrValidationResult? Validation;

    /// <summary>Whether the symbol uses the opt-in Hanzi compatibility extension.</summary>
    public bool UsesHanziCompatibility;
}
