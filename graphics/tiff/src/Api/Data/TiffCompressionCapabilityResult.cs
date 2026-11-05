namespace tiff;

/// <summary>Describes directional codec and raw-pass-through capabilities for one TIFF compression identifier.</summary>
public class TiffCompressionCapabilityResult
{
    /// <summary>Indicates whether the compression identifier is valid.</summary>
    public bool Success;
    /// <summary>Contains the queried compression identifier.</summary>
    public TiffCompression Compression;
    /// <summary>Indicates whether a decoder is available.</summary>
    public bool CanDecode;
    /// <summary>Indicates whether an encoder is available.</summary>
    public bool CanEncode;
    /// <summary>Indicates whether encoded bytes can be preserved without decoding or encoding.</summary>
    public bool CanPassThroughRaw;
    /// <summary>Identifies the codec source used for decoding.</summary>
    public TiffCodecCapabilitySource DecodeSource;
    /// <summary>Identifies the codec source used for encoding.</summary>
    public TiffCodecCapabilitySource EncodeSource;
    /// <summary>Contains warnings for unavailable directions or errors for an invalid identifier.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates an empty compression capability result.</summary>
    public TiffCompressionCapabilityResult()
    {
        Success = false;
        Compression = TiffCompression.Unknown;
        CanDecode = false;
        CanEncode = false;
        CanPassThroughRaw = false;
        DecodeSource = TiffCodecCapabilitySource.Unavailable;
        EncodeSource = TiffCodecCapabilitySource.Unavailable;
        Diagnostics = new TiffDiagnostics();
    }

    internal static TiffCompressionCapabilityResult Create(TiffCompression compression, TiffCodecCapabilitySource decodeSource, TiffCodecCapabilitySource encodeSource, bool canPassThroughRaw, TiffDiagnostics diagnostics)
    {
        TiffCompressionCapabilityResult value = new TiffCompressionCapabilityResult();
        value.Initialize(compression, decodeSource, encodeSource, canPassThroughRaw, diagnostics);
        return value;
    }

    internal void Initialize(TiffCompression compression, TiffCodecCapabilitySource decodeSource, TiffCodecCapabilitySource encodeSource, bool canPassThroughRaw, TiffDiagnostics diagnostics)
    {
        Compression = compression;
        DecodeSource = decodeSource;
        EncodeSource = encodeSource;
        CanDecode = decodeSource != TiffCodecCapabilitySource.Unavailable;
        CanEncode = encodeSource != TiffCodecCapabilitySource.Unavailable;
        CanPassThroughRaw = canPassThroughRaw;
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
