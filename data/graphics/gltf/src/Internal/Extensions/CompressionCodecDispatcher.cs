using System.Collections.Generic;

namespace gltf;

internal class CompressionCodecDispatcher
{
    internal GltfCompressionResult DecodeGeometry(
        GltfCompressionRequest request,
        List<IGltfCompressionCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfCompressionCodec codec = GetCodec(codecs, index);
            GltfCompressionResult result = codec.DecodeGeometry(request);
            ValidateResult(result, request);
            if (result.Status != GltfOperationStatus.Unavailable)
            {
                return result;
            }

            index++;
        }

        return CreateUnavailable(request.Codec);
    }

    internal GltfCompressionResult EncodeGeometry(
        GltfCompressionRequest request,
        List<IGltfCompressionCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfCompressionCodec codec = GetCodec(codecs, index);
            GltfCompressionResult result = codec.EncodeGeometry(request);
            ValidateResult(result, request);
            if (result.Status != GltfOperationStatus.Unavailable)
            {
                return result;
            }

            index++;
        }

        return CreateUnavailable(request.Codec);
    }

    internal GltfCompressionResult DecodeMeshopt(
        GltfCompressionRequest request,
        List<IGltfCompressionCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfCompressionCodec codec = GetCodec(codecs, index);
            GltfCompressionResult result = codec.DecodeMeshoptData(request);
            ValidateResult(result, request);
            if (result.Status != GltfOperationStatus.Unavailable)
            {
                return result;
            }

            index++;
        }

        return CreateUnavailable(request.Codec);
    }

    internal GltfCompressionResult EncodeMeshopt(
        GltfCompressionRequest request,
        List<IGltfCompressionCodec> codecs)
    {
        ValidateRequest(request, codecs);
        int index = 0;
        while (index < codecs.Count)
        {
            IGltfCompressionCodec codec = GetCodec(codecs, index);
            GltfCompressionResult result = codec.EncodeMeshoptData(request);
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
        GltfCompressionRequest request,
        List<IGltfCompressionCodec> codecs)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (codecs == null)
        {
            throw new System.ArgumentNullException("codecs");
        }

        if (request.MaximumOutputByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("request");
        }

        if (request.Bytes == null || request.Attributes == null)
        {
            throw new System.ArgumentException("Compression request input collections must not be null.", "request");
        }
    }

    internal IGltfCompressionCodec GetCodec(
        List<IGltfCompressionCodec> codecs,
        int index)
    {
        IGltfCompressionCodec? codec = codecs[index];
        if (codec == null)
        {
            throw new System.ArgumentException("Compression codec collections cannot contain null entries.", "codecs");
        }

        return codec;
    }

    internal void ValidateResult(
        GltfCompressionResult result,
        GltfCompressionRequest request)
    {
        if (result == null)
        {
            throw new GltfUnsupportedFeatureException("A compression codec returned a null result.");
        }

        if (result.Bytes == null || result.Attributes == null || result.Diagnostics == null)
        {
            throw new GltfUnsupportedFeatureException("A compression codec returned incomplete output collections.");
        }

        long outputByteLength = result.Bytes.Length;
        foreach (KeyValuePair<string, byte[]> attribute in result.Attributes)
        {
            if (attribute.Key == null || attribute.Value == null)
            {
                throw new GltfUnsupportedFeatureException("A compression codec returned an invalid attribute stream.");
            }

            outputByteLength += attribute.Value.Length;
            if (outputByteLength > request.MaximumOutputByteLength)
            {
                throw new GltfResourceException("Compression codec output exceeds the configured maximum byte length.");
            }
        }

        if (outputByteLength > request.MaximumOutputByteLength)
        {
            throw new GltfResourceException("Compression codec output exceeds the configured maximum byte length.");
        }
    }

    internal GltfCompressionResult CreateUnavailable(string codec)
    {
        GltfCompressionResult result = new GltfCompressionResult();
        result.Status = GltfOperationStatus.Unavailable;
        result.Codec = codec;
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.UnsupportedExtension;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = $"No registered compression codec supports {codec}.";
        result.Diagnostics.Add(issue);
        return result;
    }
}
