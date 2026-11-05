namespace tiff;

/// <summary>Controls TIFF parsing, resource limits, unknown-tag preservation, and metadata-only behavior.</summary>
/// <remarks>Limits are applied before allocations and deep directory traversal. A null options argument is normalized to the same defaults as a new instance.</remarks>
public class TiffReadOptions
{
    /// <summary>Indicates whether nonconforming TIFF structures are treated as errors instead of recoverable warnings.</summary>
    public bool Strict;
    /// <summary>Indicates whether a caller-provided stream or random-access source remains open after the file closes.</summary>
    public bool LeaveOpen;
    /// <summary>Indicates whether raw strip and tile payloads may be read; metadata-only opens set this to <c>false</c>.</summary>
    public bool ReadImageData;
    /// <summary>Indicates whether unregistered tags retain their typed raw values for later copying or writing.</summary>
    public bool PreserveUnknownTags;
    /// <summary>Indicates whether bytes following the parsed TIFF structures remain available in the in-memory file image.</summary>
    public bool PreserveTrailingData;
    /// <summary>Specifies the minimum external tag-value size eligible for deferred loading.</summary>
    public long LazyValueByteThreshold;
    /// <summary>Specifies whether unsupported features produce errors, are skipped, or are preserved as raw data.</summary>
    public TiffUnsupportedFeaturePolicy UnsupportedFeaturePolicy;
    /// <summary>Specifies the resource limits enforced by the operation.</summary>
    public TiffLimits Limits;
    /// <summary>Specifies memory allocation, deferred-loading, segment-buffer, chopped-strip, and source-loading behavior.</summary>
    public TiffMemoryPolicy MemoryPolicy;
    /// <summary>Contains known and application-defined TIFF tag definitions.</summary>
    public TiffTagRegistry TagRegistry;
    /// <summary>Contains an optional application-provided compression decoder used before built-in codecs.</summary>
    public ITiffCompressionCodec? CompressionCodec;

    /// <summary>Creates a new instance of the TIFF Read Options class.</summary>
    public TiffReadOptions()
    {
        Strict = true;
        LeaveOpen = false;
        ReadImageData = true;
        PreserveUnknownTags = true;
        PreserveTrailingData = true;
        LazyValueByteThreshold = 4096;
        UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.PreserveRaw;
        Limits = new TiffLimits();
        MemoryPolicy = new TiffMemoryPolicy();
        TagRegistry = TiffTagRegistry.CreateDefault();
        CompressionCodec = null;
    }

    /// <summary>Creates a new instance of the TIFF Read Options class.</summary>
    /// <param name="limits">The allocation and decoded-size limits to enforce.</param>
    internal static TiffReadOptions Create(TiffLimits limits)
    {
        TiffReadOptions value = new TiffReadOptions();
        value.Initialize(limits);
        return value;
    }

    internal void Initialize(TiffLimits limits)
    {
        Strict = true;
        LeaveOpen = false;
        ReadImageData = true;
        PreserveUnknownTags = true;
        PreserveTrailingData = true;
        LazyValueByteThreshold = 4096;
        UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.PreserveRaw;
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
    internal static TiffReadOptions CreateDefault()
    {
        return new TiffReadOptions();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF read options.</returns>
    internal TiffReadOptions Copy()
    {
        TiffReadOptions copy = new TiffReadOptions();
        copy.Strict = Strict;
        copy.LeaveOpen = LeaveOpen;
        copy.ReadImageData = ReadImageData;
        copy.PreserveUnknownTags = PreserveUnknownTags;
        copy.PreserveTrailingData = PreserveTrailingData;
        copy.LazyValueByteThreshold = LazyValueByteThreshold;
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

    internal bool ShouldLoadTagValueLazily(TiffIfdEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (!entry.HasExternalValue())
        {
            return false;
        }

        long threshold = GetLazyValueByteThreshold();
        if (threshold < 0)
        {
            return false;
        }

        return entry.ValueByteCount > threshold;
    }

    internal long GetLazyValueByteThreshold()
    {
        TiffMemoryPolicy policy = GetMemoryPolicy();
        if (policy.LazyTagValueByteThreshold != 4096)
        {
            return policy.LazyTagValueByteThreshold;
        }

        return LazyValueByteThreshold;
    }

    internal TiffLazyTagValue CreateLazyTagValue(TiffIfdEntry entry, byte[] fileBytes)
    {
        return new TiffLazyTagValue(entry, fileBytes, TiffDirectoryReader.GetLimits(this));
    }

    internal TiffLazyTagValue CreateLazyTagValueFromStream(TiffIfdEntry entry, System.IO.Stream stream)
    {
        return TiffLazyTagValue.CreateFromStream(entry, stream, TiffDirectoryReader.GetLimits(this));
    }
}
