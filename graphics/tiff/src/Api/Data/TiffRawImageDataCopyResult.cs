namespace tiff;

/// <summary>Contains the values and diagnostics produced by a raw image data copy operation.</summary>
public class TiffRawImageDataCopyResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Indicates whether tiled applies.</summary>
    public bool IsTiled;
    /// <summary>Contains the raw strips.</summary>
    public byte[][] RawStrips;
    /// <summary>Contains the raw tiles.</summary>
    public byte[][] RawTiles;
    /// <summary>Contains the definition.</summary>
    public TiffImageDefinition Definition;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty successful raw-image-data copy result.</summary>
    public TiffRawImageDataCopyResult()
    {
        Success = true;
        IsTiled = false;
        RawStrips = new byte[0][];
        RawTiles = new byte[0][];
        Definition = new TiffImageDefinition();
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffRawImageDataCopyResult Create(bool isTiled, byte[][] rawStrips, byte[][] rawTiles, TiffImageDefinition definition, TiffDiagnostics diagnostics)
    {
        TiffRawImageDataCopyResult value = new TiffRawImageDataCopyResult();
        value.Initialize(isTiled, rawStrips, rawTiles, definition, diagnostics);
        return value;
    }

    internal void Initialize(bool isTiled, byte[][] rawStrips, byte[][] rawTiles, TiffImageDefinition definition, TiffDiagnostics diagnostics)
    {
        IsTiled = isTiled;
        RawStrips = TiffImageDefinition.CopyBytesArray(rawStrips);
        RawTiles = TiffImageDefinition.CopyBytesArray(rawTiles);
        Definition = TiffCopy.NormalizeDefinition(definition);
        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }

    internal int StripCount()
    {
        return RawStrips.Length;
    }

    internal int TileCount()
    {
        return RawTiles.Length;
    }
}
