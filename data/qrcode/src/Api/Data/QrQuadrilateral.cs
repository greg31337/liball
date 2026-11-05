namespace qrcode;

/// <summary>Represents the four image-space corners of a QR symbol.</summary>
public class QrQuadrilateral
{
    /// <summary>The top-left corner, or <c>null</c> when it is not available.</summary>
    public QrPoint? TopLeft;

    /// <summary>The top-right corner, or <c>null</c> when it is not available.</summary>
    public QrPoint? TopRight;

    /// <summary>The bottom-right corner, or <c>null</c> when it is not available.</summary>
    public QrPoint? BottomRight;

    /// <summary>The bottom-left corner, or <c>null</c> when it is not available.</summary>
    public QrPoint? BottomLeft;
}
