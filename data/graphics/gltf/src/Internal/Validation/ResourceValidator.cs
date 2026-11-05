using System;
using System.Collections.Generic;
using System.IO;

namespace gltf;

internal class DataUriValidation
{
    internal bool Valid = true;
    internal bool LimitExceeded;
    internal int DecodedByteLength;
    internal string Message = "";
}

internal class DataUriSyntaxChecker
{
    internal DataUriCodec DataUriCodec = new DataUriCodec();
    internal Base64Codec Base64Codec = new Base64Codec();
    internal UriPercentCodec PercentCodec = new UriPercentCodec();

    internal DataUriValidation ValidateDataUri(string uri, int maximumByteLength)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        if (maximumByteLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumByteLength");
        }

        DataUriValidation result = new DataUriValidation();
        if (!uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return SetInvalid(result, "The URI does not use the data scheme.");
        }

        int commaIndex = uri.IndexOf(',', 5);
        if (commaIndex < 0)
        {
            return SetInvalid(result, "The data URI is missing its comma separator.");
        }

        string metadata = uri.Substring(5, commaIndex - 5);
        string payload = uri.Substring(commaIndex + 1);
        int invalidRawIndex = FindInvalidRawCharacter(metadata);
        if (invalidRawIndex >= 0)
        {
            return SetInvalid(
                result,
                $"The data URI metadata contains an unsafe character at character {invalidRawIndex}.");
        }

        invalidRawIndex = FindInvalidRawCharacter(payload);
        if (invalidRawIndex >= 0)
        {
            return SetInvalid(
                result,
                $"The data URI payload contains an unsafe character at character {invalidRawIndex}.");
        }

        int invalidEscapeIndex = FindInvalidPercentEscape(metadata);
        if (invalidEscapeIndex >= 0)
        {
            return SetInvalid(
                result,
                $"The data URI metadata contains a malformed percent escape at character {invalidEscapeIndex}.");
        }

        invalidEscapeIndex = FindInvalidPercentEscape(payload);
        if (invalidEscapeIndex >= 0)
        {
            return SetInvalid(
                result,
                $"The data URI payload contains a malformed percent escape at character {invalidEscapeIndex}.");
        }

        List<string> segments = DataUriCodec.SplitMetadata(metadata);
        bool base64 = false;
        int segmentIndex = 0;
        if (segments.Count > 0 && segments[0].Length > 0)
        {
            byte[] mediaTypeBytes = DecodePercentBytes(segments[0]);
            if (!IsValidMediaType(mediaTypeBytes))
            {
                return SetInvalid(result, "The data URI media type is invalid.");
            }

            segmentIndex = 1;
        }
        else if (segments.Count > 0)
        {
            segmentIndex = 1;
        }

        while (segmentIndex < segments.Count)
        {
            string segment = segments[segmentIndex];
            if (segment.Length == 0)
            {
                return SetInvalid(result, "The data URI contains an empty metadata segment.");
            }

            int equalsIndex = segment.IndexOf('=');
            if (equalsIndex < 0
                && string.Equals(segment, "base64", StringComparison.OrdinalIgnoreCase))
            {
                if (base64)
                {
                    return SetInvalid(result, "The data URI contains more than one base64 marker.");
                }

                if (segmentIndex != segments.Count - 1)
                {
                    return SetInvalid(
                        result,
                        "The data URI base64 marker must follow all media type parameters.");
                }

                base64 = true;
            }
            else
            {
                if (equalsIndex <= 0 || equalsIndex == segment.Length - 1)
                {
                    return SetInvalid(
                        result,
                        $"Data URI metadata segment {segmentIndex} is not a valid parameter.");
                }

                byte[] nameBytes = DecodePercentBytes(segment.Substring(0, equalsIndex));
                if (!IsMimeToken(nameBytes))
                {
                    return SetInvalid(
                        result,
                        $"Data URI parameter name {segmentIndex} is invalid.");
                }
            }

            segmentIndex++;
        }

        byte[] payloadBytes = DecodePercentBytes(payload);
        if (base64)
        {
            ValidateBase64Payload(payloadBytes, maximumByteLength, result);
        }
        else
        {
            result.DecodedByteLength = payloadBytes.Length;
            if (payloadBytes.Length > maximumByteLength)
            {
                result.Valid = false;
                result.LimitExceeded = true;
                result.Message = $"Data URI decoded length exceeds the configured maximum {maximumByteLength}.";
            }
        }

        return result;
    }

    internal int FindInvalidRawCharacter(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!DataUriCodec.IsDataUriRawCharacter(value[index]))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindInvalidPercentEscape(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                if (index + 2 >= value.Length
                    || PercentCodec.GetHexValue(value[index + 1]) < 0
                    || PercentCodec.GetHexValue(value[index + 2]) < 0)
                {
                    return index;
                }

                index += 3;
            }
            else
            {
                index++;
            }
        }

        return -1;
    }

    internal byte[] DecodePercentBytes(string value)
    {
        List<byte> result = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                int high = PercentCodec.GetHexValue(value[index + 1]);
                int low = PercentCodec.GetHexValue(value[index + 2]);
                result.Add((byte)((high * 16) + low));
                index += 3;
            }
            else
            {
                result.Add((byte)value[index]);
                index++;
            }
        }

        return result.ToArray();
    }

    internal bool IsValidMediaType(byte[] value)
    {
        int slashIndex = -1;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '/')
            {
                if (slashIndex >= 0)
                {
                    return false;
                }

                slashIndex = index;
            }

            index++;
        }

        if (slashIndex <= 0 || slashIndex == value.Length - 1)
        {
            return false;
        }

        return IsMimeTokenRange(value, 0, slashIndex)
            && IsMimeTokenRange(value, slashIndex + 1, value.Length);
    }

    internal bool IsMimeToken(byte[] value)
    {
        return IsMimeTokenRange(value, 0, value.Length);
    }

    internal bool IsMimeTokenRange(byte[] value, int start, int end)
    {
        if (start >= end)
        {
            return false;
        }

        int index = start;
        while (index < end)
        {
            byte character = value[index];
            if (character <= 0x20
                || character >= 0x7F
                || character == '('
                || character == ')'
                || character == '<'
                || character == '>'
                || character == '@'
                || character == ','
                || character == ';'
                || character == ':'
                || character == '\\'
                || character == '"'
                || character == '/'
                || character == '['
                || character == ']'
                || character == '?'
                || character == '=')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal void ValidateBase64Payload(
        byte[] payload,
        int maximumByteLength,
        DataUriValidation result)
    {
        if (payload.Length % 4 != 0)
        {
            SetInvalid(result, "Base64 input length is not a multiple of four.");
            return;
        }

        int padding = 0;
        if (payload.Length > 0 && payload[payload.Length - 1] == '=')
        {
            padding++;
        }

        if (payload.Length > 1 && payload[payload.Length - 2] == '=')
        {
            padding++;
        }

        int index = 0;
        while (index < payload.Length)
        {
            if (payload[index] == '=')
            {
                if (index < payload.Length - padding)
                {
                    SetInvalid(result, "Base64 padding must appear only at the end.");
                    return;
                }
            }
            else if (Base64Codec.GetAlphabetValue(payload[index]) < 0)
            {
                SetInvalid(
                    result,
                    $"Base64 input contains an invalid character at byte {index}.");
                return;
            }

            index++;
        }

        if (padding > 2)
        {
            SetInvalid(result, "Base64 input contains too much padding.");
            return;
        }

        if (payload.Length > 0 && padding == 2)
        {
            int second = Base64Codec.GetAlphabetValue(payload[payload.Length - 3]);
            if ((second & 0x0F) != 0)
            {
                SetInvalid(result, "Base64 input has non-zero unused bits before padding.");
                return;
            }
        }

        if (payload.Length > 0 && padding == 1)
        {
            int third = Base64Codec.GetAlphabetValue(payload[payload.Length - 2]);
            if ((third & 0x03) != 0)
            {
                SetInvalid(result, "Base64 input has non-zero unused bits before padding.");
                return;
            }
        }

        long decodedByteLength = ((long)payload.Length / 4L) * 3L - padding;
        if (decodedByteLength > int.MaxValue || decodedByteLength > maximumByteLength)
        {
            result.Valid = false;
            result.LimitExceeded = true;
            result.Message = $"Data URI decoded length {decodedByteLength} exceeds the configured maximum {maximumByteLength}.";
            return;
        }

        result.DecodedByteLength = (int)decodedByteLength;
    }

    internal DataUriValidation SetInvalid(DataUriValidation result, string message)
    {
        result.Valid = false;
        result.Message = message;
        return result;
    }
}

internal class ResourceValidator
{
    internal DataUriSyntaxChecker DataUris = new DataUriSyntaxChecker();
    internal FileResourceResolver FileResolver = new FileResourceResolver();
    internal MaterialTextureValidator Images = new MaterialTextureValidator();
    internal ImageCodecDispatcher ImageCodecs = new ImageCodecDispatcher();

    internal GltfValidationReport ValidateDocumentResourceRules(
        GltfDocument document,
        GltfValidationOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        GltfValidationReport report = new GltfValidationReport();
        if (!options.ValidateResources)
        {
            return report;
        }

        ValidateBuffers(document, report);
        ValidateImages(document, options, report);
        return report;
    }

    internal void ValidateBuffers(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Buffers == null)
        {
            return;
        }

        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (buffer != null)
            {
                ValidateBuffer(document, buffer, bufferIndex, report);
            }

            bufferIndex++;
        }
    }

    internal void ValidateBuffer(
        GltfDocument document,
        GltfBuffer buffer,
        int bufferIndex,
        GltfValidationReport report)
    {
        string pointer = $"/buffers/{bufferIndex}";
        if (!ValidateDataUriReference(
            buffer.Uri,
            buffer.PendingResourceRequest,
            $"{pointer}/uri",
            report))
        {
            return;
        }

        if (buffer.Resolved)
        {
            if (buffer.ResolvedBytes == null)
            {
                AddResourceIssue(
                    report,
                    GltfIssueCodes.ResourceUnavailable,
                    GltfValidationSeverity.Error,
                    "The buffer is marked resolved but has no resolved bytes.",
                    pointer,
                    buffer.Uri);
            }
            else if (buffer.ResolvedBytes.Length < buffer.ByteLength)
            {
                AddResourceIssue(
                    report,
                    GltfIssueCodes.ResourceLengthMismatch,
                    GltfValidationSeverity.Error,
                    $"Resolved buffer length {buffer.ResolvedBytes.Length} is less than declared byteLength {buffer.ByteLength}.",
                    $"{pointer}/byteLength",
                    buffer.Uri);
            }

            return;
        }

        if (buffer.Uri == null)
        {
            if (document.Container == null
                || document.Container.Format != GltfContainerFormat.Glb
                || !document.Container.HasBinChunk)
            {
                AddResourceIssue(
                    report,
                    GltfIssueCodes.BufferMissingGlbData,
                    GltfValidationSeverity.Error,
                    "The URI-less buffer has no GLB binary resource.",
                    pointer,
                    null);
            }
            else
            {
                AddResourceIssue(
                    report,
                    GltfIssueCodes.ResourceUnavailable,
                    GltfValidationSeverity.Warning,
                    "The GLB binary resource has not been bound to the buffer.",
                    pointer,
                    null);
            }

            return;
        }

        string? denial = GetPolicyDenial(buffer.PendingResourceRequest);
        if (denial != null)
        {
            AddResourceIssue(
                report,
                GltfIssueCodes.ResourcePolicyDenied,
                GltfValidationSeverity.Error,
                denial,
                $"{pointer}/uri",
                buffer.Uri);
            return;
        }

        GltfValidationSeverity severity = IsBufferReferenced(document, bufferIndex)
            ? GltfValidationSeverity.Warning
            : GltfValidationSeverity.Information;
        AddResourceIssue(
            report,
            GltfIssueCodes.ResourceUnavailable,
            severity,
            "The external buffer resource is unresolved.",
            pointer,
            buffer.Uri);
    }

    internal void ValidateImages(
        GltfDocument document,
        GltfValidationOptions options,
        GltfValidationReport report)
    {
        if (document.Images == null)
        {
            return;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image != null)
            {
                ValidateImage(document, image, imageIndex, options, report);
            }

            imageIndex++;
        }
    }

    internal void ValidateImage(
        GltfDocument document,
        GltfImage image,
        int imageIndex,
        GltfValidationOptions options,
        GltfValidationReport report)
    {
        string pointer = $"/images/{imageIndex}";
        if (!ValidateDataUriReference(
            image.Uri,
            image.PendingResourceRequest,
            $"{pointer}/uri",
            report))
        {
            return;
        }

        byte[]? bytes = Images.ResolveImageBytes(document, image);
        if (bytes == null)
        {
            if (image.Uri == null && !image.HasBufferView)
            {
                AddResourceIssue(
                    report,
                    GltfIssueCodes.ResourceUnavailable,
                    GltfValidationSeverity.Error,
                    "The image has no resource source.",
                    pointer,
                    null);
                return;
            }

            string? denial = GetPolicyDenial(image.PendingResourceRequest);
            if (denial != null)
            {
                AddResourceIssue(
                    report,
                    GltfIssueCodes.ResourcePolicyDenied,
                    GltfValidationSeverity.Error,
                    denial,
                    image.Uri == null ? pointer : $"{pointer}/uri",
                    image.Uri);
                return;
            }

            GltfValidationSeverity severity = IsImageReferenced(document, imageIndex)
                ? GltfValidationSeverity.Warning
                : GltfValidationSeverity.Information;
            AddResourceIssue(
                report,
                GltfIssueCodes.ResourceUnavailable,
                severity,
                "The image resource is unresolved.",
                pointer,
                image.Uri);
            return;
        }

        Images.ValidateResolvedImage(document, image, bytes, pointer, report);
        GltfImageFormat format = Images.DetectSignatureFormat(bytes);
        if (IsExternalCodecFormat(format)
            && !HasImageCodecSupport(document, image, imageIndex, bytes, format, options))
        {
            AddResourceIssue(
                report,
                GltfIssueCodes.ImageCodecUnavailable,
                GltfValidationSeverity.Warning,
                $"No registered image codec can fully validate {format} image data.",
                pointer,
                image.EffectiveUri ?? image.Uri);
        }
    }

    internal bool HasImageCodecSupport(
        GltfDocument document,
        GltfImage image,
        int imageIndex,
        byte[] bytes,
        GltfImageFormat format,
        GltfValidationOptions options)
    {
        if (options.ImageCodecs == null || options.ImageCodecs.Count == 0)
        {
            return false;
        }

        GltfImageRequest request = new GltfImageRequest();
        request.Document = document;
        request.HasImage = true;
        request.Image = imageIndex;
        request.Bytes = OwnedBytes.CopyArray(bytes);
        request.MimeType = image.MimeType;
        request.SourceFormat = format;
        GltfImageInfo inspected = ImageCodecs.InspectRecognized(
            request,
            options.ImageCodecs,
            format);
        return inspected.Format == format;
    }

    internal bool ValidateDataUriReference(
        string? uri,
        GltfResourceRequest? request,
        string pointer,
        GltfValidationReport report)
    {
        if (uri == null
            || !uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int maximumByteLength = GltfConstants.DefaultMaximumDataUriByteLength;
        if (request != null && request.MaximumByteLength >= 0)
        {
            maximumByteLength = request.MaximumByteLength;
        }

        DataUriValidation validation = DataUris.ValidateDataUri(uri, maximumByteLength);
        if (validation.Valid)
        {
            return true;
        }

        AddResourceIssue(
            report,
            validation.LimitExceeded
                ? GltfIssueCodes.ResourceLimitExceeded
                : GltfIssueCodes.DataUriInvalid,
            GltfValidationSeverity.Error,
            validation.Message,
            pointer,
            uri);
        return false;
    }

    internal string? GetPolicyDenial(GltfResourceRequest? request)
    {
        if (request == null)
        {
            return null;
        }

        if (request.Policy == null || request.Policy.FileRoots == null)
        {
            return "The retained resource policy is missing or invalid.";
        }

        string source = request.SourceUri ?? "";
        bool fileUri = source.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
        bool absolutePath = Path.IsPathRooted(source);
        if ((fileUri || absolutePath)
            && !request.Policy.AllowAbsolutePaths)
        {
            return $"Absolute resource URI '{source}' is denied by the retained resolver policy.";
        }

        if (!request.Policy.AllowParentPathSegments
            && FileResolver.ContainsParentPathSegment(source))
        {
            return $"Parent path traversal in resource URI '{source}' is denied by the retained resolver policy.";
        }

        bool relative = !absolutePath && !Images.HasUriScheme(source);
        if (relative
            && request.BaseUri == null
            && request.Policy.FileRoots.Count == 0)
        {
            return $"Relative resource URI '{source}' has no permitted base URI or file root.";
        }

        return null;
    }

    internal bool IsBufferReferenced(GltfDocument document, int bufferIndex)
    {
        if (document.BufferViews == null)
        {
            return false;
        }

        int index = 0;
        while (index < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[index];
            if (view != null && view.Buffer == bufferIndex)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool IsImageReferenced(GltfDocument document, int imageIndex)
    {
        if (document.Textures == null)
        {
            return false;
        }

        int index = 0;
        while (index < document.Textures.Count)
        {
            GltfTexture texture = document.Textures[index];
            if (texture != null
                && ((texture.HasSource && texture.Source == imageIndex)
                    || ReferencesExtensionImage(
                        texture,
                        imageIndex,
                        GltfExtensionNames.KhrTextureBasisu)
                    || ReferencesExtensionImage(
                        texture,
                        imageIndex,
                        GltfExtensionNames.ExtTextureWebp)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ReferencesExtensionImage(
        GltfTexture texture,
        int imageIndex,
        string extension)
    {
        if (!texture.PropertyJson.TypedExtensions.ContainsKey(extension))
        {
            return false;
        }

        GltfTypedExtensionData typed = texture.PropertyJson.TypedExtensions[extension];
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            return typed.TextureBasisu != null
                && typed.TextureBasisu.HasSource
                && typed.TextureBasisu.Source == imageIndex;
        }

        return typed.TextureWebp != null
            && typed.TextureWebp.HasSource
            && typed.TextureWebp.Source == imageIndex;
    }

    internal bool IsExternalCodecFormat(GltfImageFormat format)
    {
        return format == GltfImageFormat.Ktx2
            || format == GltfImageFormat.WebP
            || format == GltfImageFormat.Avif
            || format == GltfImageFormat.Dds
            || format == GltfImageFormat.Astc
            || format == GltfImageFormat.Custom;
    }

    internal void AddResourceIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer,
        string? resourceUri)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        issue.ResourceUri = resourceUri;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
