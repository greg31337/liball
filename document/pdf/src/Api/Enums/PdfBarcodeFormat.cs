namespace pdf;

/// <summary>
/// Specifies a supported one-dimensional business barcode symbology.
/// </summary>
public enum PdfBarcodeFormat
{
    /// <summary>
    /// Encodes uppercase letters, digits, and the standard Code 39 punctuation set.
    /// </summary>
    Code39 = 0,
    /// <summary>
    /// Encodes printable ASCII using Code 128 character set B.
    /// </summary>
    Code128 = 1,
    /// <summary>
    /// Encodes a twelve- or thirteen-digit EAN-13 value.
    /// </summary>
    Ean13 = 2,
    /// <summary>
    /// Encodes an eleven- or twelve-digit UPC-A value.
    /// </summary>
    UpcA = 3
}
