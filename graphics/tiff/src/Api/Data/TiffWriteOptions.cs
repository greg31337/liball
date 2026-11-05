namespace tiff;

/// <summary>Controls TIFF output format, byte order, validation, resource limits, and stream ownership.</summary>
/// <remarks>The output stream is left open only when <c>LeaveOpen</c> is set. Validation failures are collected as diagnostics before bytes are committed.</remarks>
public class TiffWriteOptions
{
    /// <summary>Indicates whether nonconforming TIFF structures are treated as errors instead of recoverable warnings.</summary>
    public bool Strict;
    /// <summary>Indicates whether a caller-provided stream remains open after the file or writer closes.</summary>
    public bool LeaveOpen;
    /// <summary>Indicates whether the complete directory tree is validated before output is committed.</summary>
    public bool ValidateBeforeWrite;
    /// <summary>Indicates whether output automatically switches to BigTIFF when classic 32-bit offsets would overflow.</summary>
    public bool AutoUpgradeToBigTiff;
    /// <summary>Indicates whether directories and tile payloads are ordered for Cloud Optimized GeoTIFF access.</summary>
    public bool CloudOptimizedGeoTiffLayout;
    /// <summary>Specifies the byte order used for multibyte values.</summary>
    public TiffByteOrder ByteOrder;
    /// <summary>Specifies whether the container uses classic TIFF or BigTIFF offsets.</summary>
    public TiffFileKind FileKind;
    /// <summary>Specifies whether unsupported features produce errors, are skipped, or are preserved as raw data.</summary>
    public TiffUnsupportedFeaturePolicy UnsupportedFeaturePolicy;
    /// <summary>Specifies the resource limits enforced by the operation.</summary>
    public TiffLimits Limits;
    /// <summary>Specifies memory allocation and reusable segment-buffer behavior.</summary>
    public TiffMemoryPolicy MemoryPolicy;
    /// <summary>Contains known and application-defined TIFF tag definitions.</summary>
    public TiffTagRegistry TagRegistry;
    /// <summary>Contains an optional application-provided compression codec used before built-in codecs when encoding segments or decoding them for updates.</summary>
    public ITiffCompressionCodec? CompressionCodec;

    /// <summary>Creates a new instance of the TIFF Write Options class.</summary>
    public TiffWriteOptions()
    {
        Strict = true;
        LeaveOpen = false;
        ValidateBeforeWrite = true;
        AutoUpgradeToBigTiff = true;
        CloudOptimizedGeoTiffLayout = false;
        ByteOrder = TiffByteOrder.LittleEndian;
        FileKind = TiffFileKind.Classic;
        UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.Error;
        Limits = new TiffLimits();
        MemoryPolicy = new TiffMemoryPolicy();
        TagRegistry = TiffTagRegistry.CreateDefault();
        CompressionCodec = null;
    }

    /// <summary>Creates a new instance of the TIFF Write Options class.</summary>
    /// <param name="limits">The allocation and decoded-size limits to enforce.</param>
    internal static TiffWriteOptions Create(TiffLimits limits)
    {
        TiffWriteOptions value = new TiffWriteOptions();
        value.Initialize(limits);
        return value;
    }

    internal void Initialize(TiffLimits limits)
    {
        Strict = true;
        LeaveOpen = false;
        ValidateBeforeWrite = true;
        AutoUpgradeToBigTiff = true;
        CloudOptimizedGeoTiffLayout = false;
        ByteOrder = TiffByteOrder.LittleEndian;
        FileKind = TiffFileKind.Classic;
        UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.Error;
        TagRegistry = TiffTagRegistry.CreateDefault();
        CompressionCodec = null;
        MemoryPolicy = new TiffMemoryPolicy();
        if (limits == null)
        {
            Limits = new TiffLimits();
        }
        else
        {
            Limits = limits;
        }
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffWriteOptions CreateDefault()
    {
        return new TiffWriteOptions();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF write options.</returns>
    internal TiffWriteOptions Copy()
    {
        TiffWriteOptions copy = new TiffWriteOptions();
        copy.Strict = Strict;
        copy.LeaveOpen = LeaveOpen;
        copy.ValidateBeforeWrite = ValidateBeforeWrite;
        copy.AutoUpgradeToBigTiff = AutoUpgradeToBigTiff;
        copy.CloudOptimizedGeoTiffLayout = CloudOptimizedGeoTiffLayout;
        copy.ByteOrder = ByteOrder;
        copy.FileKind = FileKind;
        copy.UnsupportedFeaturePolicy = UnsupportedFeaturePolicy;
        copy.MemoryPolicy = GetMemoryPolicy().Copy();
        copy.Limits = copy.MemoryPolicy.ApplyTo(Limits);
        copy.TagRegistry = GetTagRegistry().Copy();
        copy.CompressionCodec = CompressionCodec;
        return copy;
    }

    /// <summary>Returns the tag registry.</summary>
    /// <returns>The requested value or model.</returns>
    internal TiffTagRegistry GetTagRegistry()
    {
        if (TagRegistry == null)
        {
            TagRegistry = TiffTagRegistry.CreateDefault();
        }

        return TagRegistry;
    }

    internal TiffMemoryPolicy GetMemoryPolicy()
    {
        if (MemoryPolicy == null)
        {
            MemoryPolicy = new TiffMemoryPolicy();
        }

        return MemoryPolicy;
    }

    /// <summary>Registers tag definition for type and count validation during TIFF processing.</summary>
    /// <param name="definition">The definition.</param>
    internal void RegisterTagDefinition(TiffTagDefinition definition)
    {
        GetTagRegistry().Register(definition);
    }

    /// <summary>Finds the tag definition, or returns the type's documented missing-value result.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A detached copy of the requested definition, or <c>null</c> when the tag is not registered.</returns>
    internal TiffTagDefinition? FindTagDefinition(int tagId)
    {
        return GetTagRegistry().Find(tagId);
    }
}
