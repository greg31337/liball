namespace gltf;

internal class ImageExtensionValidator
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal ImageExtensionJsonReader Reader = new ImageExtensionJsonReader();
    internal MaterialTextureValidator Images = new MaterialTextureValidator();

    internal GltfExtensionResult ValidateImageExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result;
        GltfTypedExtensionData? typed;
        if (context.TypedData == null)
        {
            result = Reader.ReadImageExtension(context, registry);
            typed = result.TypedData;
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, registry);
            typed = context.TypedData;
            result.TypedData = typed;
        }

        if (context.ObjectLocation != "texture")
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                $"Extension '{context.Name}' is not valid on {context.ObjectLocation}.",
                Fallback.CreateExtensionPointer(context));
        }

        int imageIndex = GetImageIndex(context.Name, typed);
        if (imageIndex < 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnsatisfiedDependency,
                "The texture image extension must define a source image.",
                Fallback.CreateExtensionPointer(context) + "/source");
            return result;
        }

        if (context.TypedData != null)
        {
            Reader.AddImageReference(context, result, imageIndex);
        }

        ValidateExtensionImage(context, result, imageIndex);
        ValidateCoreFallback(context, result);
        return result;
    }

    internal int GetImageIndex(string extension, GltfTypedExtensionData? typed)
    {
        if (typed == null)
        {
            return GltfConstants.AbsentIndex;
        }

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

    internal void ValidateExtensionImage(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int imageIndex)
    {
        string pointer = Fallback.CreateExtensionPointer(context) + "/source";
        if (context.Document == null
            || imageIndex >= context.Document.Images.Count)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The texture image extension source does not resolve.",
                pointer);
            return;
        }

        GltfImage image = context.Document.Images[imageIndex];
        GltfImageFormat expectedFormat = GetExpectedFormat(context.Name);
        string expectedMimeType = GetExpectedMimeType(context.Name);
        if (image.MimeType != null
            && !string.Equals(
                image.MimeType,
                expectedMimeType,
                System.StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(
                result,
                GltfIssueCodes.TextureInvalidImageMimeType,
                $"The extension image must use {expectedMimeType}.",
                pointer);
        }

        if (image.ResolvedMimeType != null
            && !string.Equals(
                image.ResolvedMimeType,
                expectedMimeType,
                System.StringComparison.OrdinalIgnoreCase))
        {
            AddIssue(
                result,
                GltfIssueCodes.ImageMimeTypeInvalid,
                $"The resolved extension image must use {expectedMimeType}.",
                pointer);
        }

        byte[]? bytes = Images.ResolveImageBytes(context.Document, image);
        if (bytes != null)
        {
            GltfImageFormat detected = Images.DetectSignatureFormat(bytes);
            if (detected != expectedFormat)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.TextureInvalidImageMimeType,
                    $"The extension source does not contain the expected {expectedFormat} image format.",
                    pointer);
            }
        }
    }

    internal void ValidateCoreFallback(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfTexture? texture = ResolveTexture(context);
        if (texture == null)
        {
            return;
        }

        bool required = context.Document != null
            && context.Document.ExtensionsRequired.Contains(context.Name);
        if (!texture.HasSource)
        {
            if (!required)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.UnsatisfiedDependency,
                    "A texture without a core image fallback requires the image extension.",
                    "/extensionsRequired");
            }

            return;
        }

        if (context.Document == null
            || texture.Source < 0
            || texture.Source >= context.Document.Images.Count)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "The core texture fallback source does not resolve.",
                CreateTexturePointer(context) + "/source");
            return;
        }

        GltfImage fallback = context.Document.Images[texture.Source];
        GltfImageFormat format = Images.GetImageEffectiveFormat(context.Document, fallback);
        if (format != GltfImageFormat.Unknown
            && format != GltfImageFormat.Png
            && format != GltfImageFormat.Jpeg)
        {
            AddIssue(
                result,
                GltfIssueCodes.TextureInvalidImageMimeType,
                "The core texture fallback must use PNG or JPEG.",
                CreateTexturePointer(context) + "/source");
        }
    }

    internal GltfTexture? ResolveTexture(GltfExtensionContext context)
    {
        if (context.Document == null
            || !context.HasObjectIndex
            || context.ObjectIndex < 0
            || context.ObjectIndex >= context.Document.Textures.Count)
        {
            return null;
        }

        return context.Document.Textures[context.ObjectIndex];
    }

    internal GltfImageFormat GetExpectedFormat(string extension)
    {
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            return GltfImageFormat.Ktx2;
        }

        return GltfImageFormat.WebP;
    }

    internal string GetExpectedMimeType(string extension)
    {
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            return GltfMimeTypes.Ktx2;
        }

        return GltfMimeTypes.WebP;
    }

    internal string CreateTexturePointer(GltfExtensionContext context)
    {
        if (context.HasObjectIndex)
        {
            return "/textures/" + context.ObjectIndex;
        }

        return "/texture";
    }

    internal void AddIssue(
        GltfExtensionResult result,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.Status = GltfOperationStatus.Invalid;
    }
}
