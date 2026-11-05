namespace qrcode;

/// <summary>Controls raw grayscale rendering of a QR matrix or module-role map.</summary>
public class QrRenderOptions
{
    /// <summary>The profile used to derive a standard quiet-zone width.</summary>
    public QrSymbolKind SymbolKind = QrSymbolKind.Model2;

    /// <summary>The integer number of output pixels per module.</summary>
    public int Scale = 1;

    /// <summary>Whether a quiet zone is included in the rendered raster.</summary>
    public bool IncludeQuietZone = true;

    /// <summary>The quiet-zone width in modules, or -1 to use the selected profile's standard width.</summary>
    public int QuietZoneModules = -1;

    /// <summary>The byte value written for dark modules.</summary>
    public byte DarkValue;

    /// <summary>The byte value written for light modules.</summary>
    public byte LightValue = 255;

    /// <summary>Whether dark and light output values are exchanged.</summary>
    public bool Invert;

    /// <summary>Whether rendering uses module roles instead of module values.</summary>
    public bool RenderRoleMap;

    /// <summary>The role emphasized when <see cref="RenderRoleMap"/> is enabled.</summary>
    public QrModuleRole HighlightRole = QrModuleRole.Unknown;
}
