namespace gltf;

/// <summary>Provides built-in image inspection and core image passthrough operations.</summary>
public class GltfImageCodec : IGltfImageCodec
{
    internal const string CodecIdentifier = "gltf.core";
    internal ImageHeaderInspector HeaderInspector;

    /// <summary>Initializes an image codec.</summary>
    public GltfImageCodec()
    {
        HeaderInspector = new ImageHeaderInspector();
    }

    /// <summary>Inspects encoded image metadata.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The recognized image information.</returns>
    public GltfImageInfo InspectImage(GltfImageRequest request)
    {
        return HeaderInspector.InspectImage(request);
    }

    /// <summary>Decodes image data.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The image operation result.</returns>
    public GltfImageResult DecodeImage(GltfImageRequest request)
    {
        ValidateOperationRequest(request);
        GltfImageInfo info = HeaderInspector.InspectImage(request);
        return CreateUnavailableResult(
            info,
            "Image pixel decoding requires a registered external image codec.");
    }

    /// <summary>Encodes image data.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The image operation result.</returns>
    public GltfImageResult EncodeImage(GltfImageRequest request)
    {
        ValidateOperationRequest(request);
        return CreateUnavailableResult(
            new GltfImageInfo(),
            "Image pixel encoding requires a registered external image codec.");
    }

    /// <summary>Transcodes encoded image data.</summary>
    /// <param name="request">The image request.</param>
    /// <returns>The image operation result.</returns>
    public GltfImageResult TranscodeImage(GltfImageRequest request)
    {
        ValidateOperationRequest(request);
        GltfImageInfo info = HeaderInspector.InspectImage(request);
        if (!CanPassThroughImage(request, info))
        {
            return CreateUnavailableResult(
                info,
                "Image format conversion or resizing requires a registered external image codec.");
        }

        if (request.Bytes.Length > request.MaximumOutputByteLength)
        {
            throw new GltfResourceException(
                $"Image passthrough output length {request.Bytes.Length} exceeds the configured maximum {request.MaximumOutputByteLength}.");
        }

        GltfImageResult result = new GltfImageResult();
        result.Codec = CodecIdentifier;
        result.Bytes = OwnedBytes.CopyArray(request.Bytes);
        result.Info = info;
        return result;
    }

    internal void ValidateOperationRequest(GltfImageRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Bytes == null)
        {
            throw new System.ArgumentException("Image request bytes must not be null.", "request");
        }

        if (request.MaximumOutputByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "request",
                "The maximum image output byte length must not be negative.");
        }

        if (!IsKnownFormatValue(request.SourceFormat)
            || !IsKnownFormatValue(request.TargetFormat))
        {
            throw new System.ArgumentOutOfRangeException(
                "request",
                "The source and target image formats must be recognized enum values.");
        }

        if ((request.HasTargetWidth && request.TargetWidth <= 0)
            || (request.HasTargetHeight && request.TargetHeight <= 0))
        {
            throw new System.ArgumentOutOfRangeException(
                "request",
                "Requested image dimensions must be positive.");
        }
    }

    internal bool IsKnownFormatValue(GltfImageFormat format)
    {
        switch (format)
        {
            case GltfImageFormat.Unknown:
            case GltfImageFormat.Png:
            case GltfImageFormat.Jpeg:
            case GltfImageFormat.Ktx2:
            case GltfImageFormat.WebP:
            case GltfImageFormat.Avif:
            case GltfImageFormat.Dds:
            case GltfImageFormat.Astc:
            case GltfImageFormat.Custom:
                return true;
            default:
                return false;
        }
    }

    internal bool CanPassThroughImage(GltfImageRequest request, GltfImageInfo info)
    {
        GltfImageFormat sourceFormat = info.Format;
        if (sourceFormat == GltfImageFormat.Unknown)
        {
            sourceFormat = request.SourceFormat;
        }

        if (request.TargetFormat != GltfImageFormat.Unknown
            && request.TargetFormat != sourceFormat)
        {
            return false;
        }

        if (request.HasTargetWidth
            && (!info.HasDimensions || request.TargetWidth != info.Width))
        {
            return false;
        }

        if (request.HasTargetHeight
            && (!info.HasDimensions || request.TargetHeight != info.Height))
        {
            return false;
        }

        return true;
    }

    internal GltfImageResult CreateUnavailableResult(GltfImageInfo info, string message)
    {
        GltfValidationIssue diagnostic = new GltfValidationIssue();
        diagnostic.Code = GltfIssueCodes.ImageCodecUnavailable;
        diagnostic.Severity = GltfValidationSeverity.Error;
        diagnostic.Message = message;
        GltfImageResult result = new GltfImageResult();
        result.Status = GltfOperationStatus.Unavailable;
        result.Codec = CodecIdentifier;
        result.Info = info;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
