namespace qrcode;

/// <summary>Contains a row-major QR module matrix and optional per-module metadata.</summary>
public class QrMatrix
{
    /// <summary>The matrix width in modules.</summary>
    public int Width;

    /// <summary>The matrix height in modules.</summary>
    public int Height;

    /// <summary>The row-major module values.</summary>
    public QrModuleValue[] Values = new QrModuleValue[0];

    /// <summary>The optional row-major role assigned to each module.</summary>
    public QrModuleRole[]? Roles;

    /// <summary>The optional row-major confidence for each module, from zero through 255.</summary>
    public byte[]? Confidence;

    /// <summary>The number of known quiet-zone modules on the left edge.</summary>
    public int QuietZoneLeft;

    /// <summary>The number of known quiet-zone modules on the top edge.</summary>
    public int QuietZoneTop;

    /// <summary>The number of known quiet-zone modules on the right edge.</summary>
    public int QuietZoneRight;

    /// <summary>The number of known quiet-zone modules on the bottom edge.</summary>
    public int QuietZoneBottom;
}
