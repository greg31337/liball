namespace gltf;

internal class ImageExtensionReadProcessor
{
    internal ImageCodecDispatcher Dispatcher = new ImageCodecDispatcher();
    internal MaterialTextureValidator Images = new MaterialTextureValidator();

    internal void ProcessRead(GltfReadResult readResult, GltfReadOptions options)
    {
        GltfDocument? document = readResult.Document;
        if (document == null)
        {
            return;
        }

        int textureIndex = 0;
        while (textureIndex < document.Textures.Count)
        {
            GltfTexture texture = document.Textures[textureIndex];
            SelectCoreFallback(texture);
            ProcessTextureExtension(
                readResult,
                options,
                document,
                textureIndex,
                texture,
                GltfExtensionNames.KhrTextureBasisu,
                GltfImageFormat.Ktx2,
                GltfMimeTypes.Ktx2);
            if (texture.DecodedImage == null)
            {
                ProcessTextureExtension(
                    readResult,
                    options,
                    document,
                    textureIndex,
                    texture,
                    GltfExtensionNames.ExtTextureWebp,
                    GltfImageFormat.WebP,
                    GltfMimeTypes.WebP);
            }

            textureIndex++;
        }
    }

    internal void SelectCoreFallback(GltfTexture texture)
    {
        texture.HasSelectedSource = texture.HasSource;
        texture.SelectedSource = texture.Source;
        texture.DecodedImage = null;
    }

    internal void ProcessTextureExtension(
        GltfReadResult readResult,
        GltfReadOptions options,
        GltfDocument document,
        int textureIndex,
        GltfTexture texture,
        string extension,
        GltfImageFormat format,
        string mimeType)
    {
        int imageIndex = GetExtensionImage(texture, extension);
        if (imageIndex < 0
            || imageIndex >= document.Images.Count
            || options.ImageCodecs.Count == 0)
        {
            return;
        }

        GltfImage image = document.Images[imageIndex];
        byte[]? bytes = Images.ResolveImageBytes(document, image);
        if (bytes == null)
        {
            return;
        }

        GltfImageRequest request = new GltfImageRequest();
        request.Codec = extension;
        request.Document = document;
        request.HasTexture = true;
        request.Texture = textureIndex;
        request.HasImage = true;
        request.Image = imageIndex;
        request.Bytes = OwnedBytes.CopyArray(bytes);
        request.MimeType = image.MimeType;
        if (request.MimeType == null)
        {
            request.MimeType = mimeType;
        }

        request.SourceFormat = format;
        request.MaximumOutputByteLength = options.Limits.MaximumResourceByteLength;
        GltfImageResult decoded = Dispatcher.DecodeImage(request, options.ImageCodecs);
        if (decoded.Status == GltfOperationStatus.Unavailable && texture.HasSource)
        {
            return;
        }

        AppendResult(
            readResult,
            decoded,
            $"/textures/{textureIndex}/extensions/{extension}/source");
        if (decoded.Status == GltfOperationStatus.Success)
        {
            texture.HasSelectedSource = true;
            texture.SelectedSource = imageIndex;
            texture.DecodedImage = CopyResult(decoded);
        }
    }

    internal int GetExtensionImage(GltfTexture texture, string extension)
    {
        if (!texture.PropertyJson.TypedExtensions.ContainsKey(extension))
        {
            return GltfConstants.AbsentIndex;
        }

        GltfTypedExtensionData typed = texture.PropertyJson.TypedExtensions[extension];
        if (extension == GltfExtensionNames.KhrTextureBasisu
            && typed.TextureBasisu != null
            && typed.TextureBasisu.HasSource)
        {
            return typed.TextureBasisu.Source;
        }

        if (extension == GltfExtensionNames.ExtTextureWebp
            && typed.TextureWebp != null
            && typed.TextureWebp.HasSource)
        {
            return typed.TextureWebp.Source;
        }

        return GltfConstants.AbsentIndex;
    }

    internal GltfImageResult CopyResult(GltfImageResult source)
    {
        GltfImageResult result = new GltfImageResult();
        result.Status = source.Status;
        result.Codec = source.Codec;
        result.Bytes = OwnedBytes.CopyArray(source.Bytes);
        result.Info.Format = source.Info.Format;
        result.Info.MimeType = source.Info.MimeType;
        result.Info.HasDimensions = source.Info.HasDimensions;
        result.Info.Width = source.Info.Width;
        result.Info.Height = source.Info.Height;
        result.Info.HasComponentCount = source.Info.HasComponentCount;
        result.Info.ComponentCount = source.Info.ComponentCount;
        result.Info.HasBitDepth = source.Info.HasBitDepth;
        result.Info.BitDepth = source.Info.BitDepth;
        result.Info.Animated = source.Info.Animated;
        result.Info.HasUnsupportedColorProfile = source.Info.HasUnsupportedColorProfile;
        int index = 0;
        while (index < source.Diagnostics.Count)
        {
            result.Diagnostics.Add(source.Diagnostics[index]);
            index++;
        }

        return result;
    }

    internal void AppendResult(
        GltfReadResult readResult,
        GltfImageResult result,
        string pointer)
    {
        if ((int)result.Status > (int)readResult.Status)
        {
            readResult.Status = result.Status;
        }

        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            GltfValidationIssue issue = CopyDiagnostic(result.Diagnostics[index]);
            if (issue.JsonPointer == null || issue.JsonPointer.Length == 0)
            {
                issue.JsonPointer = pointer;
            }

            readResult.Diagnostics.Add(issue);
            index++;
        }
    }

    internal GltfValidationIssue CopyDiagnostic(GltfValidationIssue source)
    {
        GltfValidationIssue result = new GltfValidationIssue();
        result.Code = source.Code;
        result.Severity = source.Severity;
        result.Message = source.Message;
        result.JsonPointer = source.JsonPointer;
        result.HasByteOffset = source.HasByteOffset;
        result.ByteOffset = source.ByteOffset;
        result.ResourceUri = source.ResourceUri;
        int index = 0;
        while (index < source.RelatedJsonPointers.Count)
        {
            result.RelatedJsonPointers.Add(source.RelatedJsonPointers[index]);
            index++;
        }

        return result;
    }
}
