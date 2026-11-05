namespace barcode;

/// <summary>
/// Holds a payload, target format, and all options needed to encode a barcode.
/// </summary>
public class BarcodeEncodeRequest
{
    /// <summary>
    /// The target format or automatic format-selection request.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The payload to encode.
    /// </summary>
    public BarcodePayload? Payload;

    /// <summary>
    /// The format-independent encoding options.
    /// </summary>
    public BarcodeEncodeOptions? Options;

    /// <summary>
    /// The Code 128 options when applicable.
    /// </summary>
    public Code128Options? Code128;

    /// <summary>
    /// The EAN and UPC options when applicable.
    /// </summary>
    public EanUpcOptions? EanUpc;

    /// <summary>
    /// The Code 39 options when applicable.
    /// </summary>
    public Code39Options? Code39;

    /// <summary>
    /// The Interleaved 2 of 5 options when applicable.
    /// </summary>
    public ItfOptions? Itf;

    /// <summary>
    /// The Codabar options when applicable.
    /// </summary>
    public CodabarOptions? Codabar;

    /// <summary>
    /// The Data Matrix options when applicable.
    /// </summary>
    public DataMatrixOptions? DataMatrix;

    /// <summary>
    /// The PDF417 options when applicable.
    /// </summary>
    public Pdf417Options? Pdf417;
}
