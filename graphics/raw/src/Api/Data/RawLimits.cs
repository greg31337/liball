namespace raw;

/// <summary>Defines hard resource bounds applied before parsing, allocation, or decoding work.</summary>
/// <remarks>Each operation receives its own limits instance by default so caller changes do not affect unrelated operations.</remarks>
public class RawLimits
{
    /// <summary>The maximum input size in bytes.</summary>
    public long MaxFileSizeBytes;
    /// <summary>The maximum image width in pixels.</summary>
    public int MaxWidth;
    /// <summary>The maximum image height in pixels.</summary>
    public int MaxHeight;
    /// <summary>The maximum pixel count across one decoded image.</summary>
    public long MaxPixelCount;
    /// <summary>The maximum number of TIFF image file directories.</summary>
    public int MaxIfdCount;
    /// <summary>The maximum nested TIFF image file directory depth.</summary>
    public int MaxIfdDepth;
    /// <summary>The maximum total TIFF tag count.</summary>
    public int MaxTagCount;
    /// <summary>The maximum bytes in one metadata tag value.</summary>
    public long MaxTagValueBytes;
    /// <summary>The maximum total strip or tile count for one image.</summary>
    public int MaxStripOrTileCount;
    /// <summary>The maximum combined metadata bytes retained in memory.</summary>
    public long MaxMetadataBytes;
    /// <summary>The maximum XMP packet size in bytes.</summary>
    public long MaxXmpBytes;
    /// <summary>The maximum encoded preview size in bytes.</summary>
    public long MaxPreviewBytes;
    /// <summary>The maximum general parser recursion depth.</summary>
    public int MaxRecursionDepth;
    /// <summary>The maximum combined decoded sample bytes.</summary>
    public long MaxDecodedBytes;
    /// <summary>The maximum bytes permitted in one managed allocation.</summary>
    public long MaxAllocationBytes;

    /// <summary>Creates conservative default limits suitable for untrusted input.</summary>
    public RawLimits()
    {
        MaxFileSizeBytes = 8L * 1024L * 1024L * 1024L;
        MaxWidth = 65536;
        MaxHeight = 65536;
        MaxPixelCount = 268435456L;
        MaxIfdCount = 4096;
        MaxIfdDepth = 32;
        MaxTagCount = 262144;
        MaxTagValueBytes = 64L * 1024L * 1024L;
        MaxStripOrTileCount = 1048576;
        MaxMetadataBytes = 128L * 1024L * 1024L;
        MaxXmpBytes = 16L * 1024L * 1024L;
        MaxPreviewBytes = 256L * 1024L * 1024L;
        MaxRecursionDepth = 64;
        MaxDecodedBytes = 2L * 1024L * 1024L * 1024L;
        MaxAllocationBytes = 512L * 1024L * 1024L;
    }

    /// <summary>Validates that every resource bound is positive and internally consistent.</summary>
    /// <returns>A report containing every invalid bound in declaration order.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        RawOptionValidation.AddPositive(messages, MaxFileSizeBytes, "Limits.MaxFileSizeBytes");
        RawOptionValidation.AddPositive(messages, MaxWidth, "Limits.MaxWidth");
        RawOptionValidation.AddPositive(messages, MaxHeight, "Limits.MaxHeight");
        RawOptionValidation.AddPositive(messages, MaxPixelCount, "Limits.MaxPixelCount");
        RawOptionValidation.AddPositive(messages, MaxIfdCount, "Limits.MaxIfdCount");
        RawOptionValidation.AddPositive(messages, MaxIfdDepth, "Limits.MaxIfdDepth");
        RawOptionValidation.AddPositive(messages, MaxTagCount, "Limits.MaxTagCount");
        RawOptionValidation.AddPositive(messages, MaxTagValueBytes, "Limits.MaxTagValueBytes");
        RawOptionValidation.AddPositive(messages, MaxStripOrTileCount, "Limits.MaxStripOrTileCount");
        RawOptionValidation.AddPositive(messages, MaxMetadataBytes, "Limits.MaxMetadataBytes");
        RawOptionValidation.AddPositive(messages, MaxXmpBytes, "Limits.MaxXmpBytes");
        RawOptionValidation.AddPositive(messages, MaxPreviewBytes, "Limits.MaxPreviewBytes");
        RawOptionValidation.AddPositive(messages, MaxRecursionDepth, "Limits.MaxRecursionDepth");
        RawOptionValidation.AddPositive(messages, MaxDecodedBytes, "Limits.MaxDecodedBytes");
        RawOptionValidation.AddPositive(messages, MaxAllocationBytes, "Limits.MaxAllocationBytes");

        if (MaxTagValueBytes > MaxMetadataBytes)
        {
            RawOptionValidation.AddError(messages, "Limits.MaxTagValueBytes", "A tag value cannot exceed the combined metadata limit.");
        }
        if (MaxXmpBytes > MaxMetadataBytes)
        {
            RawOptionValidation.AddError(messages, "Limits.MaxXmpBytes", "The XMP limit cannot exceed the combined metadata limit.");
        }
        if (MaxPreviewBytes > MaxFileSizeBytes)
        {
            RawOptionValidation.AddError(messages, "Limits.MaxPreviewBytes", "The preview limit cannot exceed the file-size limit.");
        }
        if (MaxMetadataBytes > MaxFileSizeBytes)
        {
            RawOptionValidation.AddError(messages, "Limits.MaxMetadataBytes", "The metadata limit cannot exceed the file-size limit.");
        }
        if (MaxAllocationBytes > MaxDecodedBytes)
        {
            RawOptionValidation.AddError(messages, "Limits.MaxAllocationBytes", "A single allocation cannot exceed the combined decoded-data limit.");
        }

        return RawOptionValidation.CreateReport(true, messages);
    }
}
