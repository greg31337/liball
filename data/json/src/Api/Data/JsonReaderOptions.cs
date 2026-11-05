namespace json;

/// <summary>Contains policies that control JSON input processing.</summary>
public class JsonReaderOptions
{
    /// <summary>The conformance profile applied to input.</summary>
    public JsonConformanceProfile ConformanceProfile;

    /// <summary>The policy applied to duplicate decoded object names.</summary>
    public JsonDuplicateKeyPolicy DuplicateKeyPolicy;

    /// <summary>The framing mode applied to input.</summary>
    public JsonInputMode InputMode;

    /// <summary>The recovery policy applied to malformed JSON text sequence records.</summary>
    public JsonSequenceErrorPolicy SequenceErrorPolicy;

    /// <summary>The policy applied to decoded UTF-16 surrogate code units.</summary>
    public JsonUnicodePolicy UnicodePolicy;

    /// <summary>The policy applied to an initial UTF-8 byte order mark.</summary>
    public JsonBomPolicy BomPolicy;

    /// <summary>Enables one-based line and byte-column tracking in addition to byte offsets.</summary>
    public bool TrackLineInformation;

    /// <summary>Enables one-based Unicode-scalar columns when line information is tracked.</summary>
    public bool TrackUnicodeScalarColumn;

    /// <summary>Retains an owned copy of each raw UTF-8 token.</summary>
    public bool CaptureRawTokens;

    /// <summary>Retains source ranges on materialized document model values.</summary>
    public bool CaptureSourceRanges;

    /// <summary>The input stream buffer size, or zero to use <see cref="JsonDefaults.StreamBufferBytes"/>.</summary>
    public int StreamBufferBytes;

    /// <summary>The resource limits, or <c>null</c> to use all values from <see cref="JsonDefaults"/>.</summary>
    public JsonLimits? Limits;
}
