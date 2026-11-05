namespace raw;

/// <summary>Describes encoded or uncompressed thumbnail bytes and can decode supported representations.</summary>
public class RawThumbnail
{
    /// <summary>The zero-based thumbnail index.</summary>
    public int Index;
    /// <summary>The encoded thumbnail representation.</summary>
    public RawThumbnailKind Kind;
    /// <summary>The declared thumbnail width.</summary>
    public int Width;
    /// <summary>The declared thumbnail height.</summary>
    public int Height;
    /// <summary>The thumbnail orientation.</summary>
    public RawOrientation Orientation;
    /// <summary>The media type when known.</summary>
    public string MimeType;
    /// <summary>The exact encoded or uncompressed thumbnail bytes.</summary>
    public byte[] EncodedBytes;
    /// <summary>The source payload mapping, or <c>null</c>.</summary>
    public RawPayloadBlock? Payload;
    /// <summary>Diagnostics produced while discovering the thumbnail.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a thumbnail model.</summary>
    /// <param name="index">The thumbnail index.</param>
    /// <param name="kind">The thumbnail representation.</param>
    /// <param name="width">The declared width.</param>
    /// <param name="height">The declared height.</param>
    /// <param name="orientation">The thumbnail orientation.</param>
    /// <param name="mimeType">The media type.</param>
    /// <param name="encodedBytes">The exact thumbnail bytes.</param>
    /// <param name="payload">The mapped source payload, or <c>null</c>.</param>
    /// <param name="diagnostics">Discovery diagnostics.</param>
    public RawThumbnail(int index, RawThumbnailKind kind, int width, int height, RawOrientation orientation, string? mimeType, byte[]? encodedBytes, RawPayloadBlock? payload, RawDiagnostic[]? diagnostics)
    {
        Index = index;
        Kind = kind;
        Width = width;
        Height = height;
        Orientation = orientation;
        MimeType = mimeType == null ? string.Empty : mimeType;
        EncodedBytes = encodedBytes == null ? System.Array.Empty<byte>() : encodedBytes;
        Payload = payload;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Validates identity, geometry, encoded bytes, payload mapping, and resource limits.</summary>
    /// <param name="limits">Resource limits for the thumbnail.</param>
    /// <returns>All thumbnail validation findings.</returns>
    internal RawValidationReport Validate(RawLimits limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Index < 0 || Kind <= RawThumbnailKind.Unknown || Kind > RawThumbnailKind.Opaque)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Thumbnail.Identity", "A thumbnail requires a nonnegative index and defined representation.");
        }
        if (!RawModelValidation.ValidImageGeometry(Width, Height, limits))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Thumbnail.Geometry", "Thumbnail dimensions are invalid or exceed limits.");
        }
        if (limits == null || EncodedBytes == null || EncodedBytes.Length == 0 || EncodedBytes.LongLength > limits.MaxPreviewBytes || EncodedBytes.LongLength > limits.MaxAllocationBytes)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "Thumbnail.Bytes", "Thumbnail bytes are missing or exceed configured preview or allocation limits.");
        }
        if (MimeType == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "Thumbnail.MimeType", "The thumbnail media type cannot be null.");
        }
        if (Orientation < RawOrientation.Unknown || Orientation > RawOrientation.Rotate270Clockwise)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Thumbnail.Orientation", "The thumbnail orientation is not defined.");
        }
        if (Payload != null)
        {
            long sourceLength = Payload.Offset >= 0 && Payload.Length > 0 && Payload.Offset <= long.MaxValue - Payload.Length ? Payload.Offset + Payload.Length : -1;
            RawModelValidation.AddReport(messages, Payload.Validate(sourceLength));
            if (Payload.Kind != RawPayloadKind.Thumbnail && Payload.Kind != RawPayloadKind.Preview || EncodedBytes != null && Payload.Length != EncodedBytes.LongLength)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Thumbnail.Payload", "The mapped thumbnail payload kind and length must match the retained bytes.");
            }
        }
        RawImageBufferValidation.ValidateDiagnostics(messages, Diagnostics, "Thumbnail.Diagnostics");
        return new RawValidationReport(true, messages.ToArray());
    }

    /// <summary>Decodes baseline JPEG, uncompressed RGB, or uncompressed grayscale bytes without changing orientation.</summary>
    /// <param name="limits">Resource limits for parsing and output allocation.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>A caller-owned byte image retaining the thumbnail orientation.</returns>
    /// <exception cref="RawFormatException">The thumbnail model or encoded content is invalid.</exception>
    /// <exception cref="RawUnsupportedException">The thumbnail representation is recognized but not decodable.</exception>
    internal RawRgbImage Decode(RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        RawValidationReport report = Validate(limits);
        if (!report.IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A valid thumbnail model is required for decode.", -1);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (Kind == RawThumbnailKind.Rgb8)
        {
            RequireUncompressedLength(3);
            return CreateByteImage(EncodedBytes, 3, RgbChannels(), false);
        }
        if (Kind == RawThumbnailKind.Gray8)
        {
            RequireUncompressedLength(1);
            return CreateByteImage(EncodedBytes, 1, GrayChannels(), false);
        }
        if (Kind != RawThumbnailKind.Jpeg)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The thumbnail representation does not have a built-in decoder.", Payload == null ? -1 : Payload.Offset);
        }
        BaselineJpegImage decoded = BaselineJpegCodec.Decode(EncodedBytes, limits, cancellationToken);
        if (decoded.Width != Width || decoded.Height != Height)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The decoded thumbnail dimensions disagree with its declared metadata.", Payload == null ? -1 : Payload.Offset);
        }
        if (decoded.Planes.Count == 1)
        {
            byte[]? gray = decoded.Planes[0].Samples;
            return CreateByteImage(gray, 1, GrayChannels(), true);
        }
        YCbCrConversionParameters parameters = new YCbCrConversionParameters(0.299, 0.114, 0, 255, 128, 255, 128, 255, YCbCrChromaPositioning.Centered, YCbCrUpsamplingMode.Bilinear);
        RgbByteImage rgb = YCbCrConverter.Convert(decoded, parameters, limits, cancellationToken);
        return CreateByteImage(rgb.Samples, 3, RgbChannels(), true);
    }

    internal void RequireUncompressedLength(int channelCount)
    {
        long pixels = (long)Width * Height;
        if (pixels > long.MaxValue / channelCount)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "Uncompressed thumbnail sample geometry overflows.", Payload == null ? -1 : Payload.Offset);
        }
        long expected = pixels * channelCount;
        if (EncodedBytes.LongLength != expected)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Uncompressed thumbnail bytes do not match declared geometry.", Payload == null ? -1 : Payload.Offset);
        }
    }

    internal RawRgbImage CreateByteImage(byte[]? samples, int channelCount, RawChannelDescription[] channels, bool ownsSamples)
    {
        return new RawRgbImage(Width, Height, channelCount, Width * channelCount, samples, null, null, channels, RawColorSpace.Srgb, RawTransferFunction.Srgb, Orientation, false, ownsSamples, Diagnostics);
    }

    internal static RawChannelDescription[] RgbChannels()
    {
        return new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
    }

    internal static RawChannelDescription[] GrayChannels()
    {
        return new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
    }
}
