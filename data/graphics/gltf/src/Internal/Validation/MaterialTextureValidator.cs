using System;
using System.Collections.Generic;
using System.Globalization;

namespace gltf;

internal class ImageValidationData
{
    internal GltfImageFormat Format = GltfImageFormat.Unknown;
    internal bool HasDimensions;
    internal int Width;
    internal int Height;
    internal bool UnsupportedFeatures;
}

internal class MaterialTextureValidator
{
    internal MaterialEvaluator MaterialEvaluator = new MaterialEvaluator();
    internal SamplerEvaluator SamplerEvaluator = new SamplerEvaluator();
    internal ImageHeaderInspector ImageInspector = new ImageHeaderInspector();

    internal GltfValidationReport ValidateDocumentMaterialTextureRules(
        GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        ValidateMaterials(document, report);
        ValidateSamplers(document, report);
        ValidateImages(document, report);
        ValidateTextures(document, report);
        ValidatePrimitiveBindings(document, report);
        return report;
    }

    internal void ValidateMaterials(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Materials == null)
        {
            return;
        }

        int index = 0;
        while (index < document.Materials.Count)
        {
            GltfMaterial material = document.Materials[index];
            string pointer = $"/materials/{index}";
            if (material != null)
            {
                ValidateMaterialFields(material, pointer, report);
            }

            index++;
        }
    }

    internal void ValidateMaterialFields(
        GltfMaterial material,
        string pointer,
        GltfValidationReport report)
    {
        if (material.PbrMetallicRoughness != null)
        {
            GltfPbrMetallicRoughness pbr = material.PbrMetallicRoughness;
            if (pbr.HasBaseColorFactor)
            {
                ValidateFactorArray(
                    pbr.BaseColorFactor,
                    4,
                    0.0,
                    true,
                    1.0,
                    $"{pointer}/pbrMetallicRoughness/baseColorFactor",
                    report);
            }

            if (pbr.HasMetallicFactor)
            {
                ValidateUnitValue(
                    pbr.MetallicFactor,
                    $"{pointer}/pbrMetallicRoughness/metallicFactor",
                    report);
            }

            if (pbr.HasRoughnessFactor)
            {
                ValidateUnitValue(
                    pbr.RoughnessFactor,
                    $"{pointer}/pbrMetallicRoughness/roughnessFactor",
                    report);
            }

            ValidateTextureInfoFields(
                pbr.BaseColorTexture,
                $"{pointer}/pbrMetallicRoughness/baseColorTexture",
                report);
            ValidateTextureInfoFields(
                pbr.MetallicRoughnessTexture,
                $"{pointer}/pbrMetallicRoughness/metallicRoughnessTexture",
                report);
        }

        if (material.HasEmissiveFactor)
        {
            ValidateFactorArray(
                material.EmissiveFactor,
                3,
                0.0,
                false,
                0.0,
                $"{pointer}/emissiveFactor",
                report);
        }

        if (material.HasAlphaMode && !IsKnownAlphaMode(material.AlphaMode))
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ValueNotInList,
                GltfValidationSeverity.Warning,
                $"Alpha mode value {(int)material.AlphaMode} is not recognized.",
                $"{pointer}/alphaMode");
        }

        if (material.HasAlphaCutoff)
        {
            if (!IsFiniteNonnegative(material.AlphaCutoff))
            {
                AddRangeIssue(
                    report,
                    material.AlphaCutoff,
                    $"{pointer}/alphaCutoff");
            }

            GltfAlphaMode effectiveMode = material.HasAlphaMode
                ? material.AlphaMode
                : GltfAlphaMode.Opaque;
            if (effectiveMode != GltfAlphaMode.Mask)
            {
                AddMaterialTextureIssue(
                    report,
                    GltfIssueCodes.MaterialAlphaCutoffInvalidMode,
                    GltfValidationSeverity.Warning,
                    "Alpha cutoff is used only by MASK materials.",
                    $"{pointer}/alphaCutoff");
            }
        }

        if (material.NormalTexture != null)
        {
            ValidateTextureCoordinateValue(
                material.NormalTexture.HasTextureCoordinate,
                material.NormalTexture.TextureCoordinate,
                $"{pointer}/normalTexture/texCoord",
                report);
            if (material.NormalTexture.HasScale
                && !IsFiniteValue(material.NormalTexture.Scale))
            {
                AddRangeIssue(
                    report,
                    material.NormalTexture.Scale,
                    $"{pointer}/normalTexture/scale");
            }
        }

        if (material.OcclusionTexture != null)
        {
            ValidateTextureCoordinateValue(
                material.OcclusionTexture.HasTextureCoordinate,
                material.OcclusionTexture.TextureCoordinate,
                $"{pointer}/occlusionTexture/texCoord",
                report);
            if (material.OcclusionTexture.HasStrength)
            {
                ValidateUnitValue(
                    material.OcclusionTexture.Strength,
                    $"{pointer}/occlusionTexture/strength",
                    report);
            }
        }

        ValidateTextureInfoFields(
            material.EmissiveTexture,
            $"{pointer}/emissiveTexture",
            report);
    }

    internal void ValidateFactorArray(
        double[]? values,
        int expectedLength,
        double minimum,
        bool hasMaximum,
        double maximum,
        string pointer,
        GltfValidationReport report)
    {
        if (values == null || values.Length != expectedLength)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                $"The factor must contain {expectedLength} values.",
                pointer);
            return;
        }

        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (!IsFiniteValue(value)
                || value < minimum
                || (hasMaximum && value > maximum))
            {
                AddRangeIssue(report, value, $"{pointer}/{index}");
            }

            index++;
        }
    }

    internal void ValidateUnitValue(
        double value,
        string pointer,
        GltfValidationReport report)
    {
        if (!IsFiniteValue(value) || value < 0.0 || value > 1.0)
        {
            AddRangeIssue(report, value, pointer);
        }
    }

    internal void ValidateTextureInfoFields(
        GltfTextureInfo? info,
        string pointer,
        GltfValidationReport report)
    {
        if (info != null)
        {
            ValidateTextureCoordinateValue(
                info.HasTextureCoordinate,
                info.TextureCoordinate,
                $"{pointer}/texCoord",
                report);
        }
    }

    internal void ValidateTextureCoordinateValue(
        bool present,
        int coordinate,
        string pointer,
        GltfValidationReport report)
    {
        if (present && coordinate < 0)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                $"Texture coordinate set {coordinate} is negative.",
                pointer);
        }
    }

    internal bool IsKnownAlphaMode(GltfAlphaMode mode)
    {
        return mode == GltfAlphaMode.Opaque
            || mode == GltfAlphaMode.Mask
            || mode == GltfAlphaMode.Blend;
    }

    internal bool IsFiniteValue(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal bool IsFiniteNonnegative(double value)
    {
        return IsFiniteValue(value) && value >= 0.0;
    }

    internal void ValidateSamplers(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Samplers == null)
        {
            return;
        }

        int index = 0;
        while (index < document.Samplers.Count)
        {
            GltfSampler sampler = document.Samplers[index];
            if (sampler != null)
            {
                ValidateSamplerFields(
                    sampler,
                    $"/samplers/{index}",
                    report);
            }

            index++;
        }
    }

    internal void ValidateSamplerFields(
        GltfSampler sampler,
        string pointer,
        GltfValidationReport report)
    {
        if (sampler.HasMagnificationFilter
            && !SamplerEvaluator.IsLegalMagnificationFilter(
                sampler.MagnificationFilter))
        {
            AddInvalidGlValue(
                report,
                (int)sampler.MagnificationFilter,
                $"{pointer}/magFilter");
        }

        if (sampler.HasMinificationFilter
            && !SamplerEvaluator.IsLegalMinificationFilter(
                sampler.MinificationFilter))
        {
            AddInvalidGlValue(
                report,
                (int)sampler.MinificationFilter,
                $"{pointer}/minFilter");
        }

        if (sampler.HasWrapS && !SamplerEvaluator.IsLegalSamplerWrap(sampler.WrapS))
        {
            AddInvalidGlValue(report, (int)sampler.WrapS, $"{pointer}/wrapS");
        }

        if (sampler.HasWrapT && !SamplerEvaluator.IsLegalSamplerWrap(sampler.WrapT))
        {
            AddInvalidGlValue(report, (int)sampler.WrapT, $"{pointer}/wrapT");
        }
    }

    internal void AddInvalidGlValue(
        GltfValidationReport report,
        int value,
        string pointer)
    {
        AddMaterialTextureIssue(
            report,
            GltfIssueCodes.InvalidGlValue,
            GltfValidationSeverity.Error,
            $"Value {value} is not legal for this texture sampler field.",
            pointer);
    }

    internal void ValidateImages(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Images == null)
        {
            return;
        }

        int index = 0;
        while (index < document.Images.Count)
        {
            GltfImage image = document.Images[index];
            string pointer = $"/images/{index}";
            if (image != null)
            {
                ValidateImageSource(document, image, pointer, report);
                byte[]? bytes = ResolveImageBytes(document, image);
                if (bytes != null)
                {
                    ValidateResolvedImage(document, image, bytes, pointer, report);
                }
            }

            index++;
        }
    }

    internal void ValidateImageSource(
        GltfDocument document,
        GltfImage image,
        string pointer,
        GltfValidationReport report)
    {
        bool hasUri = image.Uri != null;
        if (hasUri == image.HasBufferView)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.OneOfMismatch,
                GltfValidationSeverity.Error,
                "An image must define exactly one of uri or bufferView.",
                pointer);
        }

        if (image.HasBufferView && image.MimeType == null)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.UnsatisfiedDependency,
                GltfValidationSeverity.Error,
                "A bufferView image must declare mimeType.",
                $"{pointer}/mimeType");
        }

        if (image.MimeType != null)
        {
            ValidateImageMimeType(document, image.MimeType, pointer, report);
        }

        if (image.Uri != null)
        {
            ValidateImageUri(document, image.Uri, $"{pointer}/uri", report);
        }
    }

    internal void ValidateImageMimeType(
        GltfDocument document,
        string mimeType,
        string pointer,
        GltfValidationReport report)
    {
        GltfImageFormat format = GetMimeTypeFormat(mimeType);
        if (format != GltfImageFormat.Unknown
            && !IsImageFormatEnabled(document, format))
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ImageNonEnabledMimeType,
                GltfValidationSeverity.Error,
                $"Image MIME type '{mimeType}' requires an extension.",
                $"{pointer}/mimeType");
        }
    }

    internal void ValidateImageUri(
        GltfDocument document,
        string uri,
        string pointer,
        GltfValidationReport report)
    {
        if (!IsUriLexicallyValid(uri))
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.InvalidUri,
                GltfValidationSeverity.Error,
                $"Image URI '{uri}' is not a valid URI reference.",
                pointer);
            return;
        }

        bool dataUri = uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        bool absolute = HasUriScheme(uri);
        if (absolute && !dataUri)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.NonRelativeUri,
                GltfValidationSeverity.Warning,
                "The image URI is not relative.",
                pointer);
        }

        if (document.Container != null
            && document.Container.Format == GltfContainerFormat.Glb)
        {
            AddMaterialTextureIssue(
                report,
                dataUri ? GltfIssueCodes.DataUriGlb : GltfIssueCodes.UriGlb,
                dataUri
                    ? GltfValidationSeverity.Warning
                    : GltfValidationSeverity.Information,
                "The GLB image uses a URI instead of a bufferView.",
                pointer);
        }
    }

    internal bool IsUriLexicallyValid(string uri)
    {
        int index = 0;
        while (index < uri.Length)
        {
            char value = uri[index];
            if (value <= 0x20
                || value == 0x7F
                || value == '"'
                || value == '<'
                || value == '>'
                || value == '\\'
                || value == '^'
                || value == '`'
                || value == '{'
                || value == '|'
                || value == '}')
            {
                return false;
            }

            if (value == '%')
            {
                if (index + 2 >= uri.Length
                    || !IsHexDigit(uri[index + 1])
                    || !IsHexDigit(uri[index + 2]))
                {
                    return false;
                }

                index += 2;
            }

            index++;
        }

        int colon = uri.IndexOf(':');
        int delimiter = FindFirstUriDelimiter(uri);
        if (colon >= 0 && (delimiter < 0 || colon < delimiter))
        {
            return IsValidUriScheme(uri.Substring(0, colon));
        }

        return true;
    }

    internal int FindFirstUriDelimiter(string uri)
    {
        int result = -1;
        int slash = uri.IndexOf('/');
        int query = uri.IndexOf('?');
        int fragment = uri.IndexOf('#');
        if (slash >= 0)
        {
            result = slash;
        }

        if (query >= 0 && (result < 0 || query < result))
        {
            result = query;
        }

        if (fragment >= 0 && (result < 0 || fragment < result))
        {
            result = fragment;
        }

        return result;
    }

    internal bool HasUriScheme(string uri)
    {
        int colon = uri.IndexOf(':');
        int delimiter = FindFirstUriDelimiter(uri);
        return colon > 0
            && (delimiter < 0 || colon < delimiter)
            && IsValidUriScheme(uri.Substring(0, colon));
    }

    internal bool IsValidUriScheme(string scheme)
    {
        if (scheme.Length == 0 || !IsAsciiLetter(scheme[0]))
        {
            return false;
        }

        int index = 1;
        while (index < scheme.Length)
        {
            char value = scheme[index];
            if (!IsAsciiLetter(value)
                && (value < '0' || value > '9')
                && value != '+'
                && value != '-'
                && value != '.')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z');
    }

    internal bool IsHexDigit(char value)
    {
        return (value >= '0' && value <= '9')
            || (value >= 'A' && value <= 'F')
            || (value >= 'a' && value <= 'f');
    }

    internal byte[]? ResolveImageBytes(GltfDocument document, GltfImage image)
    {
        if (image.Resolved && image.ResolvedBytes != null)
        {
            return image.ResolvedBytes;
        }

        if (image.HasResource
            && document.Resources != null
            && document.Resources.Resources != null
            && image.Resource >= 0
            && image.Resource < document.Resources.Resources.Count)
        {
            GltfResource resource = document.Resources.Resources[image.Resource];
            if (resource != null
                && resource.Status == GltfOperationStatus.Success
                && resource.Bytes != null)
            {
                return resource.Bytes;
            }
        }

        if (!image.HasBufferView
            || document.BufferViews == null
            || image.BufferView < 0
            || image.BufferView >= document.BufferViews.Count)
        {
            return null;
        }

        GltfBufferView view = document.BufferViews[image.BufferView];
        if (view == null
            || document.Buffers == null
            || view.Buffer < 0
            || view.Buffer >= document.Buffers.Count)
        {
            return null;
        }

        GltfBuffer buffer = document.Buffers[view.Buffer];
        if (buffer == null || !buffer.Resolved || buffer.ResolvedBytes == null)
        {
            return null;
        }

        int viewOffset = view.HasByteOffset ? view.ByteOffset : 0;
        long end = (long)viewOffset + view.ByteLength;
        if (viewOffset < 0
            || view.ByteLength < 0
            || end > buffer.ResolvedBytes.Length)
        {
            return null;
        }

        byte[] result = new byte[view.ByteLength];
        Array.Copy(buffer.ResolvedBytes, viewOffset, result, 0, view.ByteLength);
        return result;
    }

    internal void ValidateResolvedImage(
        GltfDocument document,
        GltfImage image,
        byte[] bytes,
        string pointer,
        GltfValidationReport report)
    {
        if (bytes.Length == 0)
        {
            AddImageEndIssue(report, pointer);
            return;
        }

        GltfImageFormat format = DetectSignatureFormat(bytes);
        if (format == GltfImageFormat.Unknown)
        {
            if (HasImageSignaturePrefix(bytes))
            {
                AddImageEndIssue(report, pointer);
            }
            else
            {
                AddMaterialTextureIssue(
                    report,
                    GltfIssueCodes.ImageUnrecognizedFormat,
                    GltfValidationSeverity.Warning,
                    "The resolved image format is not recognized.",
                    pointer);
            }

            return;
        }

        GltfImageFormat declared = GetMimeTypeFormat(image.MimeType);
        if (declared != GltfImageFormat.Unknown && declared != format)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ImageMimeTypeInvalid,
                GltfValidationSeverity.Error,
                $"Detected image format {format} does not match declared format {declared}.",
                $"{pointer}/mimeType");
        }

        if (!IsImageFormatEnabled(document, format)
            && (declared == GltfImageFormat.Unknown || declared != format))
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ImageNonEnabledMimeType,
                GltfValidationSeverity.Error,
                $"Detected image format {format} requires an extension.",
                pointer);
        }

        ImageValidationData data = new ImageValidationData();
        data.Format = format;
        if (format == GltfImageFormat.Png)
        {
            data = ValidatePngData(bytes, pointer, report);
        }
        else if (format == GltfImageFormat.Jpeg)
        {
            data = ValidateJpegData(bytes, pointer, report);
        }

        if (data.UnsupportedFeatures)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ImageFeaturesUnsupported,
                GltfValidationSeverity.Warning,
                "The image contains animation, non-square pixels, or color-profile data.",
                pointer);
        }

        if (data.HasDimensions
            && (!IsPowerOfTwo(data.Width) || !IsPowerOfTwo(data.Height)))
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.ImageNpotDimensions,
                GltfValidationSeverity.Information,
                $"Image dimensions {data.Width} by {data.Height} are not powers of two.",
                pointer);
        }
    }

    internal GltfImageFormat DetectSignatureFormat(byte[] bytes)
    {
        if (ImageInspector.HasPngSignature(bytes))
        {
            return GltfImageFormat.Png;
        }

        if (ImageInspector.HasJpegSignature(bytes))
        {
            return GltfImageFormat.Jpeg;
        }

        if (ImageInspector.HasKtx2Signature(bytes))
        {
            return GltfImageFormat.Ktx2;
        }

        if (ImageInspector.HasWebPSignature(bytes))
        {
            return GltfImageFormat.WebP;
        }

        if (ImageInspector.HasAvifSignature(bytes))
        {
            return GltfImageFormat.Avif;
        }

        if (ImageInspector.HasDdsSignature(bytes))
        {
            return GltfImageFormat.Dds;
        }

        if (ImageInspector.HasAstcSignature(bytes))
        {
            return GltfImageFormat.Astc;
        }

        return GltfImageFormat.Unknown;
    }

    internal bool HasImageSignaturePrefix(byte[] bytes)
    {
        return ImageInspector.IsPngSignaturePrefix(bytes)
            || ImageInspector.IsJpegSignaturePrefix(bytes)
            || ImageInspector.IsKtx2SignaturePrefix(bytes)
            || ImageInspector.IsWebPSignaturePrefix(bytes)
            || ImageInspector.IsAvifSignaturePrefix(bytes)
            || ImageInspector.IsDdsSignaturePrefix(bytes)
            || ImageInspector.IsAstcSignaturePrefix(bytes);
    }

    internal ImageValidationData ValidatePngData(
        byte[] bytes,
        string pointer,
        GltfValidationReport report)
    {
        ImageValidationData result = new ImageValidationData();
        result.Format = GltfImageFormat.Png;
        int offset = 8;
        bool hasHeader = false;
        bool hasEnd = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12)
            {
                AddImageEndIssue(report, pointer);
                return result;
            }

            uint unsignedLength = ImageInspector.ReadUInt32BigEndian(bytes, offset);
            if (unsignedLength > int.MaxValue)
            {
                AddImageDataIssue(report, "PNG chunk length is too large.", pointer);
                return result;
            }

            int length = (int)unsignedLength;
            long end = (long)offset + 12L + length;
            if (end > bytes.Length)
            {
                AddImageEndIssue(report, pointer);
                return result;
            }

            bool header = ImageInspector.IsChunkType(
                bytes,
                offset + 4,
                'I',
                'H',
                'D',
                'R');
            if (!hasHeader)
            {
                if (!header || length != 13)
                {
                    AddImageDataIssue(report, "PNG must begin with a 13-byte IHDR chunk.", pointer);
                    return result;
                }

                if (!ReadPngDimensions(bytes, offset + 8, result))
                {
                    AddImageDataIssue(report, "PNG IHDR values are invalid.", pointer);
                    return result;
                }

                hasHeader = true;
            }
            else if (header)
            {
                AddImageDataIssue(report, "PNG contains duplicate IHDR chunks.", pointer);
                return result;
            }

            if (ImageInspector.IsChunkType(bytes, offset + 4, 'a', 'c', 'T', 'L')
                || ImageInspector.IsChunkType(bytes, offset + 4, 'i', 'C', 'C', 'P'))
            {
                result.UnsupportedFeatures = true;
            }

            if (ImageInspector.IsChunkType(bytes, offset + 4, 'I', 'E', 'N', 'D'))
            {
                if (length != 0)
                {
                    AddImageDataIssue(report, "PNG IEND chunk must be empty.", pointer);
                    return result;
                }

                hasEnd = true;
                break;
            }

            offset = (int)end;
        }

        if (!hasEnd)
        {
            AddImageEndIssue(report, pointer);
        }

        return result;
    }

    internal bool ReadPngDimensions(
        byte[] bytes,
        int offset,
        ImageValidationData result)
    {
        uint width = ImageInspector.ReadUInt32BigEndian(bytes, offset);
        uint height = ImageInspector.ReadUInt32BigEndian(bytes, offset + 4);
        int bitDepth = bytes[offset + 8];
        int colorType = bytes[offset + 9];
        if (width == 0
            || height == 0
            || width > int.MaxValue
            || height > int.MaxValue
            || !ImageInspector.IsSupportedPngBitDepth(colorType, bitDepth)
            || bytes[offset + 10] != 0
            || bytes[offset + 11] != 0
            || (bytes[offset + 12] != 0 && bytes[offset + 12] != 1))
        {
            return false;
        }

        result.HasDimensions = true;
        result.Width = (int)width;
        result.Height = (int)height;
        return true;
    }

    internal ImageValidationData ValidateJpegData(
        byte[] bytes,
        string pointer,
        GltfValidationReport report)
    {
        ImageValidationData result = new ImageValidationData();
        result.Format = GltfImageFormat.Jpeg;
        int offset = 2;
        while (offset < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                AddImageDataIssue(report, "JPEG marker prefix is missing.", pointer);
                return result;
            }

            while (offset < bytes.Length && bytes[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                AddImageEndIssue(report, pointer);
                return result;
            }

            int marker = bytes[offset];
            offset++;
            if (marker == 0xD9 || marker == 0xDA || marker == 0x00)
            {
                AddImageDataIssue(report, "JPEG ended before a frame header.", pointer);
                return result;
            }

            if (ImageInspector.IsStandaloneJpegMarker(marker))
            {
                continue;
            }

            if (bytes.Length - offset < 2)
            {
                AddImageEndIssue(report, pointer);
                return result;
            }

            int length = ImageInspector.ReadUInt16BigEndian(bytes, offset);
            long end = (long)offset + length;
            if (length < 2)
            {
                AddImageDataIssue(report, "JPEG segment length is invalid.", pointer);
                return result;
            }

            if (end > bytes.Length)
            {
                AddImageEndIssue(report, pointer);
                return result;
            }

            if (marker == 0xE2
                && ImageInspector.HasJpegIccProfile(bytes, offset, length))
            {
                result.UnsupportedFeatures = true;
            }

            if (marker == 0xE0 && HasNonsquareJpegPixels(bytes, offset, length))
            {
                result.UnsupportedFeatures = true;
            }

            if (ImageInspector.IsJpegStartOfFrame(marker))
            {
                if (!ReadJpegDimensions(bytes, offset, length, result))
                {
                    AddImageDataIssue(report, "JPEG frame header is invalid.", pointer);
                }

                return result;
            }

            offset = (int)end;
        }

        AddImageEndIssue(report, pointer);
        return result;
    }

    internal bool HasNonsquareJpegPixels(
        byte[] bytes,
        int offset,
        int length)
    {
        if (length < 16)
        {
            return false;
        }

        int data = offset + 2;
        bool jfif = bytes[data] == 'J'
            && bytes[data + 1] == 'F'
            && bytes[data + 2] == 'I'
            && bytes[data + 3] == 'F'
            && bytes[data + 4] == 0;
        if (!jfif || bytes[data + 7] == 0)
        {
            return false;
        }

        int horizontal = ImageInspector.ReadUInt16BigEndian(bytes, data + 8);
        int vertical = ImageInspector.ReadUInt16BigEndian(bytes, data + 10);
        return horizontal != vertical;
    }

    internal bool ReadJpegDimensions(
        byte[] bytes,
        int offset,
        int length,
        ImageValidationData result)
    {
        if (length < 8)
        {
            return false;
        }

        int bitDepth = bytes[offset + 2];
        int height = ImageInspector.ReadUInt16BigEndian(bytes, offset + 3);
        int width = ImageInspector.ReadUInt16BigEndian(bytes, offset + 5);
        int componentCount = bytes[offset + 7];
        if (bitDepth == 0
            || width == 0
            || height == 0
            || componentCount == 0
            || length != 8 + componentCount * 3)
        {
            return false;
        }

        result.HasDimensions = true;
        result.Width = width;
        result.Height = height;
        if (bitDepth != 8 || (componentCount != 1 && componentCount != 3))
        {
            result.UnsupportedFeatures = true;
        }

        return true;
    }

    internal bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    internal GltfImageFormat GetMimeTypeFormat(string? mimeType)
    {
        if (string.Equals(mimeType, GltfMimeTypes.Png, StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Png;
        }

        if (string.Equals(mimeType, GltfMimeTypes.Jpeg, StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Jpeg;
        }

        if (string.Equals(mimeType, GltfMimeTypes.Ktx2, StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Ktx2;
        }

        if (string.Equals(mimeType, GltfMimeTypes.WebP, StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.WebP;
        }

        if (string.Equals(mimeType, GltfMimeTypes.Avif, StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Avif;
        }

        if (string.Equals(mimeType, GltfMimeTypes.Dds, StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Dds;
        }

        return GltfImageFormat.Unknown;
    }

    internal bool IsImageFormatEnabled(
        GltfDocument document,
        GltfImageFormat format)
    {
        if (format == GltfImageFormat.Png || format == GltfImageFormat.Jpeg)
        {
            return true;
        }

        if (document.ExtensionsUsed == null)
        {
            return false;
        }

        if (format == GltfImageFormat.Ktx2)
        {
            return document.ExtensionsUsed.Contains(GltfExtensionNames.KhrTextureBasisu);
        }

        if (format == GltfImageFormat.WebP)
        {
            return document.ExtensionsUsed.Contains(GltfExtensionNames.ExtTextureWebp);
        }

        if (format == GltfImageFormat.Dds)
        {
            return document.ExtensionsUsed.Contains(GltfExtensionNames.MsftTextureDds);
        }

        if (format == GltfImageFormat.Astc)
        {
            return document.ExtensionsUsed.Contains(GltfExtensionNames.ExtTextureAstc);
        }

        return false;
    }

    internal void ValidateTextures(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Textures == null)
        {
            return;
        }

        int index = 0;
        while (index < document.Textures.Count)
        {
            GltfTexture texture = document.Textures[index];
            if (texture != null
                && texture.HasSource
                && document.Images != null
                && texture.Source >= 0
                && texture.Source < document.Images.Count)
            {
                GltfImage image = document.Images[texture.Source];
                if (image != null)
                {
                    GltfImageFormat format = GetImageEffectiveFormat(document, image);
                    bool invalidKnownFormat = format != GltfImageFormat.Unknown
                        && format != GltfImageFormat.Png
                        && format != GltfImageFormat.Jpeg;
                    bool invalidDeclaredMime = format == GltfImageFormat.Unknown
                        && image.MimeType != null;
                    if (invalidKnownFormat || invalidDeclaredMime)
                    {
                        AddMaterialTextureIssue(
                            report,
                            GltfIssueCodes.TextureInvalidImageMimeType,
                            GltfValidationSeverity.Error,
                            $"Core texture source format {format} is not PNG or JPEG.",
                            $"/textures/{index}/source");
                    }
                }
            }

            index++;
        }
    }

    internal GltfImageFormat GetImageEffectiveFormat(
        GltfDocument document,
        GltfImage image)
    {
        byte[]? bytes = ResolveImageBytes(document, image);
        if (bytes != null)
        {
            GltfImageFormat detected = DetectSignatureFormat(bytes);
            if (detected != GltfImageFormat.Unknown)
            {
                return detected;
            }
        }

        return GetMimeTypeFormat(image.MimeType);
    }

    internal void ValidatePrimitiveBindings(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Meshes == null || document.Materials == null)
        {
            return;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            if (mesh != null && mesh.Primitives != null)
            {
                int primitiveIndex = 0;
                while (primitiveIndex < mesh.Primitives.Count)
                {
                    GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                    if (primitive != null)
                    {
                        ValidatePrimitiveMaterial(
                            document,
                            primitive,
                            $"/meshes/{meshIndex}/primitives/{primitiveIndex}",
                            report);
                    }

                    primitiveIndex++;
                }
            }

            meshIndex++;
        }
    }

    internal void ValidatePrimitiveMaterial(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string pointer,
        GltfValidationReport report)
    {
        GltfMaterial? material = null;
        if (primitive.HasMaterial
            && primitive.Material >= 0
            && primitive.Material < document.Materials.Count)
        {
            material = document.Materials[primitive.Material];
        }

        MaterialEvaluation evaluation = MaterialEvaluator.EvaluateMaterial(material);
        int bindingIndex = 0;
        while (bindingIndex < evaluation.Textures.Count)
        {
            MaterialTextureBinding binding = evaluation.Textures[bindingIndex];
            if (IsTextureReferenceUsable(document, binding.Texture)
                && binding.TextureCoordinate >= 0)
            {
                string semantic = GltfSemantics.TextureCoordinatePrefix
                    + binding.TextureCoordinate.ToString(CultureInfo.InvariantCulture);
                if (!PrimitiveHasSemantic(primitive, semantic))
                {
                    AddMaterialTextureIssue(
                        report,
                        GltfIssueCodes.MeshPrimitiveTooFewTexcoords,
                        GltfValidationSeverity.Error,
                        $"Material texture slot {binding.Slot} requires {semantic}.",
                        $"{pointer}/attributes");
                }
            }

            bindingIndex++;
        }

        bool hasTangent = PrimitiveHasSemantic(primitive, GltfSemantics.Tangent);
        bool hasNormal = PrimitiveHasSemantic(primitive, GltfSemantics.Normal);
        bool hasNormalTexture = material != null
            && material.NormalTexture != null
            && IsTextureReferenceUsable(document, material.NormalTexture.Index);
        if (hasTangent && !hasNormalTexture)
        {
            AddMaterialTextureIssue(
                report,
                GltfIssueCodes.UnusedMeshTangent,
                GltfValidationSeverity.Information,
                "The primitive tangent attribute is unused without a normal texture.",
                $"{pointer}/attributes/TANGENT");
        }
        else if (!hasTangent && hasNormalTexture
            && material != null && material.NormalTexture != null)
        {
            GltfNormalTextureInfo normalTexture = material.NormalTexture;
            int coordinate = normalTexture.HasTextureCoordinate
                ? normalTexture.TextureCoordinate
                : 0;
            string semantic = GltfSemantics.TextureCoordinatePrefix
                + coordinate.ToString(CultureInfo.InvariantCulture);
            if (hasNormal && PrimitiveHasSemantic(primitive, semantic))
            {
                AddMaterialTextureIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveGeneratedTangentSpace,
                    GltfValidationSeverity.Warning,
                    "The normal texture requires runtime-generated tangent space.",
                    $"{pointer}/attributes");
            }
            else
            {
                AddMaterialTextureIssue(
                    report,
                    GltfIssueCodes.MeshPrimitiveNoTangentSpace,
                    GltfValidationSeverity.Error,
                    "The normal texture has no usable tangent space.",
                    $"{pointer}/attributes");
            }
        }
    }

    internal bool IsTextureReferenceUsable(
        GltfDocument document,
        int textureIndex)
    {
        return document.Textures != null
            && textureIndex >= 0
            && textureIndex < document.Textures.Count
            && document.Textures[textureIndex] != null;
    }

    internal bool PrimitiveHasSemantic(
        GltfMeshPrimitive primitive,
        string semantic)
    {
        if (primitive.Attributes == null)
        {
            return false;
        }

        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[index];
            if (attribute != null && attribute.Semantic == semantic)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void AddRangeIssue(
        GltfValidationReport report,
        double value,
        string pointer)
    {
        AddMaterialTextureIssue(
            report,
            GltfIssueCodes.ValueNotInRange,
            GltfValidationSeverity.Error,
            $"Value {value} is outside its permitted range.",
            pointer);
    }

    internal void AddImageEndIssue(
        GltfValidationReport report,
        string pointer)
    {
        AddMaterialTextureIssue(
            report,
            GltfIssueCodes.ImageUnexpectedEos,
            GltfValidationSeverity.Error,
            "The image data ended before its required header was complete.",
            pointer);
    }

    internal void AddImageDataIssue(
        GltfValidationReport report,
        string message,
        string pointer)
    {
        AddMaterialTextureIssue(
            report,
            GltfIssueCodes.ImageDataInvalid,
            GltfValidationSeverity.Error,
            message,
            pointer);
    }

    internal void AddMaterialTextureIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
