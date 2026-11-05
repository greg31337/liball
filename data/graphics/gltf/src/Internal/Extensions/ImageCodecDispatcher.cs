using System.Collections.Generic;

namespace gltf;

internal class ImageCodecDispatcher
{
    internal GltfImageInfo InspectRecognized(
        GltfImageRequest request,
        List<IGltfImageCodec> codecs,
        GltfImageFormat expectedFormat)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfImageCodec codec = GetCodec(codecs, index);
            GltfImageInfo result = codec.InspectImage(request);
            if (result == null)
            {
                throw new GltfUnsupportedFeatureException("An image codec returned null inspection metadata.");
            }

            if (result.Format == expectedFormat)
            {
                return result;
            }

            index++;
        }

        return new GltfImageInfo();
    }

    internal GltfImageResult DecodeImage(
        GltfImageRequest request,
        List<IGltfImageCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfImageCodec codec = GetCodec(codecs, index);
            GltfImageResult result = codec.DecodeImage(request);
            ValidateResult(result, request);
            if (result.Status != GltfOperationStatus.Unavailable)
            {
                return result;
            }

            index++;
        }

        return CreateUnavailable(request.Codec);
    }

    internal GltfImageResult EncodeImage(
        GltfImageRequest request,
        List<IGltfImageCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfImageCodec codec = GetCodec(codecs, index);
            GltfImageResult result = codec.EncodeImage(request);
            ValidateResult(result, request);
            if (result.Status != GltfOperationStatus.Unavailable)
            {
                return result;
            }

            index++;
        }

        return CreateUnavailable(request.Codec);
    }

    internal GltfImageResult TranscodeImage(
        GltfImageRequest request,
        List<IGltfImageCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfImageCodec codec = GetCodec(codecs, index);
            GltfImageResult result = codec.TranscodeImage(request);
            ValidateResult(result, request);
            if (result.Status != GltfOperationStatus.Unavailable)
            {
                return result;
            }

            index++;
        }

        return CreateUnavailable(request.Codec);
    }

    internal void ValidateRequest(
        GltfImageRequest request,
        List<IGltfImageCodec> codecs)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (codecs == null)
        {
            throw new System.ArgumentNullException("codecs");
        }

        if (request.Bytes == null)
        {
            throw new System.ArgumentException("Image request bytes must not be null.", "request");
        }

        if (request.MaximumOutputByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("request");
        }
    }

    internal IGltfImageCodec GetCodec(List<IGltfImageCodec> codecs, int index)
    {
        IGltfImageCodec? codec = codecs[index];
        if (codec == null)
        {
            throw new System.ArgumentException("Image codec collections cannot contain null entries.", "codecs");
        }

        return codec;
    }

    internal void ValidateResult(GltfImageResult result, GltfImageRequest request)
    {
        if (result == null)
        {
            throw new GltfUnsupportedFeatureException("An image codec returned a null result.");
        }

        if (result.Bytes == null || result.Info == null || result.Diagnostics == null)
        {
            throw new GltfUnsupportedFeatureException("An image codec returned incomplete output data.");
        }

        if (result.Bytes.Length > request.MaximumOutputByteLength)
        {
            throw new GltfResourceException("Image codec output exceeds the configured maximum byte length.");
        }
    }

    internal GltfImageResult CreateUnavailable(string codec)
    {
        GltfImageResult result = new GltfImageResult();
        result.Status = GltfOperationStatus.Unavailable;
        result.Codec = codec;
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.ImageCodecUnavailable;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = $"No registered image codec supports {codec}.";
        result.Diagnostics.Add(issue);
        return result;
    }
}
