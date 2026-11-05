namespace gltf;

internal class ImageHeaderInspector
{
    internal GltfImageInfo InspectImage(GltfImageRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Bytes == null)
        {
            throw new System.ArgumentException("Image request bytes must not be null.", "request");
        }

        GltfImageFormat format = DetectFormat(request);
        GltfImageInfo result;
        switch (format)
        {
            case GltfImageFormat.Png:
                result = InspectPng(request.Bytes);
                break;
            case GltfImageFormat.Jpeg:
                result = InspectJpeg(request.Bytes);
                break;
            case GltfImageFormat.Ktx2:
                result = InspectKtx2(request.Bytes);
                break;
            case GltfImageFormat.WebP:
                result = InspectWebP(request.Bytes);
                break;
            case GltfImageFormat.Avif:
                result = InspectAvif(request.Bytes);
                break;
            case GltfImageFormat.Dds:
                result = InspectDds(request.Bytes);
                break;
            case GltfImageFormat.Astc:
                result = InspectAstc(request.Bytes);
                break;
            default:
                result = new GltfImageInfo();
                break;
        }

        ValidateDeclaredFormat(request, result);
        return result;
    }

    internal GltfImageFormat DetectFormat(GltfImageRequest request)
    {
        byte[] bytes = request.Bytes;
        if (HasPngSignature(bytes))
        {
            return GltfImageFormat.Png;
        }

        if (HasJpegSignature(bytes))
        {
            return GltfImageFormat.Jpeg;
        }

        if (HasKtx2Signature(bytes))
        {
            return GltfImageFormat.Ktx2;
        }

        if (HasWebPSignature(bytes))
        {
            return GltfImageFormat.WebP;
        }

        if (HasAvifSignature(bytes))
        {
            return GltfImageFormat.Avif;
        }

        if (HasDdsSignature(bytes))
        {
            return GltfImageFormat.Dds;
        }

        if (HasAstcSignature(bytes))
        {
            return GltfImageFormat.Astc;
        }

        if (IsPngDeclaration(request))
        {
            return GltfImageFormat.Png;
        }

        if (IsJpegDeclaration(request))
        {
            return GltfImageFormat.Jpeg;
        }

        if (IsKtx2Declaration(request))
        {
            return GltfImageFormat.Ktx2;
        }

        if (IsWebPDeclaration(request))
        {
            return GltfImageFormat.WebP;
        }

        if (IsAvifDeclaration(request))
        {
            return GltfImageFormat.Avif;
        }

        if (IsDdsDeclaration(request))
        {
            return GltfImageFormat.Dds;
        }

        if (IsAstcDeclaration(request))
        {
            return GltfImageFormat.Astc;
        }

        if (IsPngSignaturePrefix(bytes))
        {
            return GltfImageFormat.Png;
        }

        if (IsJpegSignaturePrefix(bytes))
        {
            return GltfImageFormat.Jpeg;
        }

        if (IsKtx2SignaturePrefix(bytes))
        {
            return GltfImageFormat.Ktx2;
        }

        if (IsWebPSignaturePrefix(bytes))
        {
            return GltfImageFormat.WebP;
        }

        if (IsAvifSignaturePrefix(bytes))
        {
            return GltfImageFormat.Avif;
        }

        if (IsDdsSignaturePrefix(bytes))
        {
            return GltfImageFormat.Dds;
        }

        if (IsAstcSignaturePrefix(bytes))
        {
            return GltfImageFormat.Astc;
        }

        return GltfImageFormat.Unknown;
    }

    internal bool IsPngDeclaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.Png
            || string.Equals(
                request.MimeType,
                GltfMimeTypes.Png,
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsJpegDeclaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.Jpeg
            || string.Equals(
                request.MimeType,
                GltfMimeTypes.Jpeg,
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsKtx2Declaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.Ktx2
            || string.Equals(
                request.MimeType,
                GltfMimeTypes.Ktx2,
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsWebPDeclaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.WebP
            || string.Equals(
                request.MimeType,
                GltfMimeTypes.WebP,
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsAvifDeclaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.Avif
            || string.Equals(
                request.MimeType,
                GltfMimeTypes.Avif,
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsDdsDeclaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.Dds
            || string.Equals(
                request.MimeType,
                GltfMimeTypes.Dds,
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsAstcDeclaration(GltfImageRequest request)
    {
        return request.SourceFormat == GltfImageFormat.Astc;
    }

    internal bool HasPngSignature(byte[] bytes)
    {
        if (bytes.Length < 8)
        {
            return false;
        }

        return bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A;
    }

    internal bool IsPngSignaturePrefix(byte[] bytes)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (bytes.Length == 0 || bytes.Length >= signature.Length)
        {
            return false;
        }

        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != signature[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool HasJpegSignature(byte[] bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8;
    }

    internal bool IsJpegSignaturePrefix(byte[] bytes)
    {
        return bytes.Length == 1 && bytes[0] == 0xFF;
    }

    internal bool HasKtx2Signature(byte[] bytes)
    {
        byte[] signature = [
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32,
            0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        ];
        return MatchesSignature(bytes, signature);
    }

    internal bool IsKtx2SignaturePrefix(byte[] bytes)
    {
        byte[] signature = [
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32,
            0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        ];
        return MatchesSignaturePrefix(bytes, signature);
    }

    internal bool HasWebPSignature(byte[] bytes)
    {
        return bytes.Length >= 12
            && IsFourCharacterCode(bytes, 0, 'R', 'I', 'F', 'F')
            && IsFourCharacterCode(bytes, 8, 'W', 'E', 'B', 'P');
    }

    internal bool IsWebPSignaturePrefix(byte[] bytes)
    {
        if (bytes.Length < 9 || bytes.Length >= 12
            || !IsFourCharacterCode(bytes, 0, 'R', 'I', 'F', 'F'))
        {
            return false;
        }

        byte[] webPCode = [0x57, 0x45, 0x42, 0x50];
        int index = 8;
        while (index < bytes.Length)
        {
            if (bytes[index] != webPCode[index - 8])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool HasAvifSignature(byte[] bytes)
    {
        if (bytes.Length < 12
            || !IsFourCharacterCode(bytes, 4, 'f', 't', 'y', 'p'))
        {
            return false;
        }

        int brandOffset = 8;
        int boxEnd = bytes.Length;
        uint shortBoxByteLength = ReadUInt32BigEndian(bytes, 0);
        if (shortBoxByteLength == 1)
        {
            if (bytes.Length < 20)
            {
                return false;
            }

            brandOffset = 16;
            ulong longBoxByteLength = ReadUInt64BigEndian(bytes, 8);
            if (longBoxByteLength <= (ulong)bytes.Length)
            {
                boxEnd = (int)longBoxByteLength;
            }
        }
        else if (shortBoxByteLength >= 8 && shortBoxByteLength <= bytes.Length)
        {
            boxEnd = (int)shortBoxByteLength;
        }

        if (boxEnd - brandOffset < 8)
        {
            return false;
        }

        if (IsAvifBrand(bytes, brandOffset))
        {
            return true;
        }

        int compatibleBrandOffset = brandOffset + 8;
        while (compatibleBrandOffset + 4 <= boxEnd)
        {
            if (IsAvifBrand(bytes, compatibleBrandOffset))
            {
                return true;
            }

            compatibleBrandOffset += 4;
        }

        return false;
    }

    internal bool IsAvifSignaturePrefix(byte[] bytes)
    {
        return bytes.Length >= 12
            && bytes.Length < 16
            && IsFourCharacterCode(bytes, 4, 'f', 't', 'y', 'p')
            && IsAvifBrand(bytes, 8);
    }

    internal bool IsAvifBrand(byte[] bytes, int offset)
    {
        return IsFourCharacterCode(bytes, offset, 'a', 'v', 'i', 'f')
            || IsFourCharacterCode(bytes, offset, 'a', 'v', 'i', 's');
    }

    internal bool HasAvifSequenceBrand(byte[] bytes, int offset, int byteLength)
    {
        int boxEnd = offset + byteLength;
        int headerByteLength = GetIsoBoxHeaderByteLength(bytes, offset);
        int brandOffset = offset + headerByteLength;
        if (boxEnd - brandOffset < 8)
        {
            return false;
        }

        if (IsFourCharacterCode(bytes, brandOffset, 'a', 'v', 'i', 's'))
        {
            return true;
        }

        int compatibleBrandOffset = brandOffset + 8;
        while (compatibleBrandOffset + 4 <= boxEnd)
        {
            if (IsFourCharacterCode(bytes, compatibleBrandOffset, 'a', 'v', 'i', 's'))
            {
                return true;
            }

            compatibleBrandOffset += 4;
        }

        return false;
    }

    internal bool HasDdsSignature(byte[] bytes)
    {
        return bytes.Length >= 4
            && IsFourCharacterCode(bytes, 0, 'D', 'D', 'S', ' ');
    }

    internal bool IsDdsSignaturePrefix(byte[] bytes)
    {
        byte[] signature = [0x44, 0x44, 0x53, 0x20];
        return MatchesSignaturePrefix(bytes, signature);
    }

    internal bool HasAstcSignature(byte[] bytes)
    {
        byte[] signature = [0x13, 0xAB, 0xA1, 0x5C];
        return MatchesSignature(bytes, signature);
    }

    internal bool IsAstcSignaturePrefix(byte[] bytes)
    {
        byte[] signature = [0x13, 0xAB, 0xA1, 0x5C];
        return MatchesSignaturePrefix(bytes, signature);
    }

    internal bool MatchesSignature(byte[] bytes, byte[] signature)
    {
        if (bytes.Length < signature.Length)
        {
            return false;
        }

        int index = 0;
        while (index < signature.Length)
        {
            if (bytes[index] != signature[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool MatchesSignaturePrefix(byte[] bytes, byte[] signature)
    {
        if (bytes.Length == 0 || bytes.Length >= signature.Length)
        {
            return false;
        }

        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != signature[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal GltfImageInfo InspectPng(byte[] bytes)
    {
        if (bytes.Length < 8)
        {
            throw new GltfResourceException("PNG signature is truncated.");
        }

        if (!HasPngSignature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain the PNG signature.");
        }

        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.Png;
        result.MimeType = GltfMimeTypes.Png;
        int offset = 8;
        bool hasHeader = false;
        bool hasEnd = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12)
            {
                throw new GltfResourceException(
                    $"PNG chunk header is truncated at byte {offset}.");
            }

            uint unsignedChunkByteLength = ReadUInt32BigEndian(bytes, offset);
            if (unsignedChunkByteLength > int.MaxValue)
            {
                throw new GltfResourceException(
                    $"PNG chunk at byte {offset} exceeds the supported signed byte length.");
            }

            int chunkByteLength = (int)unsignedChunkByteLength;
            long chunkEnd = (long)offset + 12L + chunkByteLength;
            if (chunkEnd > bytes.Length)
            {
                throw new GltfResourceException(
                    $"PNG chunk at byte {offset} is truncated; it declares {chunkByteLength} data bytes.");
            }

            bool isHeader = IsChunkType(bytes, offset + 4, 'I', 'H', 'D', 'R');
            if (!hasHeader)
            {
                if (!isHeader)
                {
                    throw new GltfResourceException("PNG IHDR is not the first chunk.");
                }

                ReadPngHeader(bytes, offset, chunkByteLength, result);
                hasHeader = true;
            }
            else if (isHeader)
            {
                throw new GltfResourceException("PNG contains more than one IHDR chunk.");
            }

            if (IsChunkType(bytes, offset + 4, 'a', 'c', 'T', 'L'))
            {
                result.Animated = true;
            }

            if (IsChunkType(bytes, offset + 4, 'i', 'C', 'C', 'P'))
            {
                result.HasUnsupportedColorProfile = true;
            }

            if (IsChunkType(bytes, offset + 4, 'I', 'E', 'N', 'D'))
            {
                if (chunkByteLength != 0)
                {
                    throw new GltfResourceException("PNG IEND chunk must be empty.");
                }

                hasEnd = true;
                break;
            }

            offset = (int)chunkEnd;
        }

        if (!hasHeader)
        {
            throw new GltfResourceException("PNG data is truncated before its IHDR chunk.");
        }

        if (!hasEnd)
        {
            throw new GltfResourceException("PNG data is truncated before its IEND chunk.");
        }

        return result;
    }

    internal void ReadPngHeader(
        byte[] bytes,
        int chunkOffset,
        int chunkByteLength,
        GltfImageInfo result)
    {
        if (chunkByteLength != 13)
        {
            throw new GltfResourceException(
                $"PNG IHDR chunk length {chunkByteLength} is not 13 bytes.");
        }

        int dataOffset = chunkOffset + 8;
        uint width = ReadUInt32BigEndian(bytes, dataOffset);
        uint height = ReadUInt32BigEndian(bytes, dataOffset + 4);
        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
        {
            throw new GltfResourceException(
                $"PNG dimensions {width} by {height} are outside the supported signed range.");
        }

        int bitDepth = bytes[dataOffset + 8];
        int colorType = bytes[dataOffset + 9];
        int componentCount = GetPngComponentCount(colorType);
        if (!IsSupportedPngBitDepth(colorType, bitDepth))
        {
            throw new GltfResourceException(
                $"PNG color type {colorType} does not support bit depth {bitDepth}.");
        }

        if (bytes[dataOffset + 10] != 0
            || bytes[dataOffset + 11] != 0
            || (bytes[dataOffset + 12] != 0 && bytes[dataOffset + 12] != 1))
        {
            throw new GltfResourceException("PNG IHDR uses unsupported compression, filtering, or interlace values.");
        }

        result.HasDimensions = true;
        result.Width = (int)width;
        result.Height = (int)height;
        result.HasComponentCount = true;
        result.ComponentCount = componentCount;
        result.HasBitDepth = true;
        result.BitDepth = bitDepth;
    }

    internal int GetPngComponentCount(int colorType)
    {
        switch (colorType)
        {
            case 0:
            case 3:
                return 1;
            case 2:
                return 3;
            case 4:
                return 2;
            case 6:
                return 4;
            default:
                throw new GltfResourceException($"PNG color type {colorType} is unsupported.");
        }
    }

    internal bool IsSupportedPngBitDepth(int colorType, int bitDepth)
    {
        switch (colorType)
        {
            case 0:
                return bitDepth == 1
                    || bitDepth == 2
                    || bitDepth == 4
                    || bitDepth == 8
                    || bitDepth == 16;
            case 2:
            case 4:
            case 6:
                return bitDepth == 8 || bitDepth == 16;
            case 3:
                return bitDepth == 1
                    || bitDepth == 2
                    || bitDepth == 4
                    || bitDepth == 8;
            default:
                return false;
        }
    }

    internal GltfImageInfo InspectJpeg(byte[] bytes)
    {
        if (bytes.Length < 2)
        {
            throw new GltfResourceException("JPEG signature is truncated.");
        }

        if (!HasJpegSignature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain the JPEG signature.");
        }

        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.Jpeg;
        result.MimeType = GltfMimeTypes.Jpeg;
        int offset = 2;
        while (offset < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                throw new GltfResourceException(
                    $"JPEG marker prefix is missing at byte {offset}.");
            }

            while (offset < bytes.Length && bytes[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                throw new GltfResourceException("JPEG marker is truncated at the end of the image data.");
            }

            int marker = bytes[offset];
            offset++;
            if (marker == 0x00)
            {
                throw new GltfResourceException(
                    $"JPEG contains a stuffed zero outside scan data at byte {offset - 1}.");
            }

            if (marker == 0xD9)
            {
                throw new GltfResourceException("JPEG ended before a start-of-frame segment.");
            }

            if (IsStandaloneJpegMarker(marker))
            {
                continue;
            }

            if (bytes.Length - offset < 2)
            {
                throw new GltfResourceException(
                    $"JPEG segment length is truncated at byte {offset}.");
            }

            int segmentByteLength = ReadUInt16BigEndian(bytes, offset);
            if (segmentByteLength < 2)
            {
                throw new GltfResourceException(
                    $"JPEG segment at byte {offset - 2} has invalid length {segmentByteLength}.");
            }

            long segmentEnd = (long)offset + segmentByteLength;
            if (segmentEnd > bytes.Length)
            {
                throw new GltfResourceException(
                    $"JPEG segment at byte {offset - 2} is truncated; it declares length {segmentByteLength}.");
            }

            if (marker == 0xE2 && HasJpegIccProfile(bytes, offset, segmentByteLength))
            {
                result.HasUnsupportedColorProfile = true;
            }

            if (IsJpegStartOfFrame(marker))
            {
                ReadJpegFrame(bytes, offset, segmentByteLength, result);
                return result;
            }

            if (marker == 0xDA)
            {
                throw new GltfResourceException("JPEG scan data begins before a start-of-frame segment.");
            }

            offset = (int)segmentEnd;
        }

        throw new GltfResourceException("JPEG data is truncated before a start-of-frame segment.");
    }

    internal bool IsStandaloneJpegMarker(int marker)
    {
        return marker == 0xD8
            || marker == 0x01
            || (marker >= 0xD0 && marker <= 0xD7);
    }

    internal bool IsJpegStartOfFrame(int marker)
    {
        return marker >= 0xC0
            && marker <= 0xCF
            && marker != 0xC4
            && marker != 0xC8
            && marker != 0xCC;
    }

    internal void ReadJpegFrame(
        byte[] bytes,
        int segmentOffset,
        int segmentByteLength,
        GltfImageInfo result)
    {
        if (segmentByteLength < 8)
        {
            throw new GltfResourceException(
                $"JPEG start-of-frame segment length {segmentByteLength} is too short.");
        }

        int bitDepth = bytes[segmentOffset + 2];
        int height = ReadUInt16BigEndian(bytes, segmentOffset + 3);
        int width = ReadUInt16BigEndian(bytes, segmentOffset + 5);
        int componentCount = bytes[segmentOffset + 7];
        int expectedByteLength = 8 + (componentCount * 3);
        if (componentCount == 0 || segmentByteLength != expectedByteLength)
        {
            throw new GltfResourceException(
                $"JPEG start-of-frame component layout does not match segment length {segmentByteLength}.");
        }

        if (width == 0 || height == 0 || bitDepth == 0)
        {
            throw new GltfResourceException(
                $"JPEG frame dimensions or bit depth are zero ({width} by {height}, {bitDepth} bits).");
        }

        result.HasDimensions = true;
        result.Width = width;
        result.Height = height;
        result.HasComponentCount = true;
        result.ComponentCount = componentCount;
        result.HasBitDepth = true;
        result.BitDepth = bitDepth;
    }

    internal bool HasJpegIccProfile(byte[] bytes, int segmentOffset, int segmentByteLength)
    {
        byte[] prefix = [
            0x49, 0x43, 0x43, 0x5F, 0x50, 0x52,
            0x4F, 0x46, 0x49, 0x4C, 0x45, 0x00
        ];
        int payloadByteLength = segmentByteLength - 2;
        if (payloadByteLength < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (bytes[segmentOffset + 2 + index] != prefix[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal GltfImageInfo InspectKtx2(byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            throw new GltfResourceException("KTX2 identifier is truncated.");
        }

        if (!HasKtx2Signature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain the KTX2 identifier.");
        }

        if (bytes.Length < 80)
        {
            throw new GltfResourceException(
                $"KTX2 header is truncated; expected at least 80 bytes but found {bytes.Length}.");
        }

        uint width = ReadUInt32LittleEndian(bytes, 20);
        uint height = ReadUInt32LittleEndian(bytes, 24);
        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
        {
            throw new GltfResourceException(
                $"KTX2 dimensions {width} by {height} are outside the supported signed range.");
        }

        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.Ktx2;
        result.MimeType = GltfMimeTypes.Ktx2;
        result.HasDimensions = true;
        result.Width = (int)width;
        result.Height = (int)height;
        return result;
    }

    internal GltfImageInfo InspectWebP(byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            throw new GltfResourceException("WebP RIFF header is truncated.");
        }

        if (!HasWebPSignature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain the WebP RIFF signature.");
        }

        uint riffByteLength = ReadUInt32LittleEndian(bytes, 4);
        long declaredEnd = (long)riffByteLength + 8L;
        if (declaredEnd < 12 || declaredEnd > bytes.Length)
        {
            throw new GltfResourceException(
                $"WebP RIFF data is truncated; it declares {declaredEnd} total bytes but contains {bytes.Length}.");
        }

        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.WebP;
        result.MimeType = GltfMimeTypes.WebP;
        int end = (int)declaredEnd;
        int offset = 12;
        while (offset < end)
        {
            if (end - offset < 8)
            {
                throw new GltfResourceException(
                    $"WebP chunk header is truncated at byte {offset}.");
            }

            uint unsignedChunkByteLength = ReadUInt32LittleEndian(bytes, offset + 4);
            if (unsignedChunkByteLength > int.MaxValue)
            {
                throw new GltfResourceException(
                    $"WebP chunk at byte {offset} exceeds the supported signed byte length.");
            }

            int chunkByteLength = (int)unsignedChunkByteLength;
            int paddingByteLength = chunkByteLength & 1;
            long chunkEnd = (long)offset + 8L + chunkByteLength + paddingByteLength;
            if (chunkEnd > end)
            {
                throw new GltfResourceException(
                    $"WebP chunk at byte {offset} is truncated; it declares {chunkByteLength} data bytes.");
            }

            int dataOffset = offset + 8;
            if (IsFourCharacterCode(bytes, offset, 'V', 'P', '8', 'X'))
            {
                ReadWebPExtendedHeader(bytes, dataOffset, chunkByteLength, result);
                return result;
            }

            if (IsFourCharacterCode(bytes, offset, 'V', 'P', '8', ' '))
            {
                ReadWebPLossyHeader(bytes, dataOffset, chunkByteLength, result);
                return result;
            }

            if (IsFourCharacterCode(bytes, offset, 'V', 'P', '8', 'L'))
            {
                ReadWebPLosslessHeader(bytes, dataOffset, chunkByteLength, result);
                return result;
            }

            if (IsFourCharacterCode(bytes, offset, 'I', 'C', 'C', 'P'))
            {
                result.HasUnsupportedColorProfile = true;
            }

            offset = (int)chunkEnd;
        }

        throw new GltfResourceException("WebP data does not contain a recognized image header chunk.");
    }

    internal void ReadWebPExtendedHeader(
        byte[] bytes,
        int dataOffset,
        int chunkByteLength,
        GltfImageInfo result)
    {
        if (chunkByteLength != 10)
        {
            throw new GltfResourceException(
                $"WebP VP8X chunk length {chunkByteLength} is not 10 bytes.");
        }

        int flags = bytes[dataOffset];
        if ((flags & 0xC1) != 0
            || bytes[dataOffset + 1] != 0
            || bytes[dataOffset + 2] != 0
            || bytes[dataOffset + 3] != 0)
        {
            throw new GltfResourceException("WebP VP8X header contains nonzero reserved bits.");
        }

        int width = ReadUInt24LittleEndian(bytes, dataOffset + 4) + 1;
        int height = ReadUInt24LittleEndian(bytes, dataOffset + 7) + 1;
        result.HasDimensions = true;
        result.Width = width;
        result.Height = height;
        result.Animated = (flags & 0x02) != 0;
        result.HasUnsupportedColorProfile = (flags & 0x20) != 0;
    }

    internal void ReadWebPLossyHeader(
        byte[] bytes,
        int dataOffset,
        int chunkByteLength,
        GltfImageInfo result)
    {
        if (chunkByteLength < 10)
        {
            throw new GltfResourceException(
                $"WebP VP8 frame header is truncated; expected 10 bytes but found {chunkByteLength}.");
        }

        if (bytes[dataOffset + 3] != 0x9D
            || bytes[dataOffset + 4] != 0x01
            || bytes[dataOffset + 5] != 0x2A)
        {
            throw new GltfResourceException("WebP VP8 frame synchronization code is invalid.");
        }

        int width = ReadUInt16LittleEndian(bytes, dataOffset + 6) & 0x3FFF;
        int height = ReadUInt16LittleEndian(bytes, dataOffset + 8) & 0x3FFF;
        if (width == 0 || height == 0)
        {
            throw new GltfResourceException(
                $"WebP VP8 frame dimensions {width} by {height} are invalid.");
        }

        result.HasDimensions = true;
        result.Width = width;
        result.Height = height;
    }

    internal void ReadWebPLosslessHeader(
        byte[] bytes,
        int dataOffset,
        int chunkByteLength,
        GltfImageInfo result)
    {
        if (chunkByteLength < 5)
        {
            throw new GltfResourceException(
                $"WebP VP8L frame header is truncated; expected 5 bytes but found {chunkByteLength}.");
        }

        if (bytes[dataOffset] != 0x2F)
        {
            throw new GltfResourceException("WebP VP8L frame signature is invalid.");
        }

        int width = 1
            + bytes[dataOffset + 1]
            + ((bytes[dataOffset + 2] & 0x3F) << 8);
        int height = 1
            + (bytes[dataOffset + 2] >> 6)
            + (bytes[dataOffset + 3] << 2)
            + ((bytes[dataOffset + 4] & 0x0F) << 10);
        if ((bytes[dataOffset + 4] & 0xE0) != 0)
        {
            throw new GltfResourceException("WebP VP8L frame header contains an unsupported version.");
        }

        result.HasDimensions = true;
        result.Width = width;
        result.Height = height;
    }

    internal GltfImageInfo InspectAvif(byte[] bytes)
    {
        if (bytes.Length < 16)
        {
            throw new GltfResourceException("AVIF file type box is truncated.");
        }

        if (!HasAvifSignature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain an AVIF file type brand.");
        }

        int fileTypeByteLength = ReadIsoBoxByteLength(bytes, 0, bytes.Length, "AVIF file type");
        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.Avif;
        result.MimeType = GltfMimeTypes.Avif;
        result.Animated = HasAvifSequenceBrand(bytes, 0, fileTypeByteLength);
        FindAvifDimensions(bytes, fileTypeByteLength, bytes.Length, result);
        return result;
    }

    internal bool FindAvifDimensions(
        byte[] bytes,
        int offset,
        int end,
        GltfImageInfo result)
    {
        while (offset < end)
        {
            int boxByteLength = ReadIsoBoxByteLength(bytes, offset, end, "AVIF top-level");
            int headerByteLength = GetIsoBoxHeaderByteLength(bytes, offset);
            if (IsFourCharacterCode(bytes, offset + 4, 'm', 'e', 't', 'a'))
            {
                int childOffset = offset + headerByteLength + 4;
                if (childOffset > offset + boxByteLength)
                {
                    throw new GltfResourceException("AVIF meta box is truncated before its full-box header.");
                }

                if (FindAvifItemProperties(
                    bytes,
                    childOffset,
                    offset + boxByteLength,
                    result))
                {
                    return true;
                }
            }

            offset += boxByteLength;
        }

        return false;
    }

    internal bool FindAvifItemProperties(
        byte[] bytes,
        int offset,
        int end,
        GltfImageInfo result)
    {
        while (offset < end)
        {
            int boxByteLength = ReadIsoBoxByteLength(bytes, offset, end, "AVIF meta child");
            int headerByteLength = GetIsoBoxHeaderByteLength(bytes, offset);
            if (IsFourCharacterCode(bytes, offset + 4, 'i', 'p', 'r', 'p'))
            {
                if (FindAvifPropertyContainer(
                    bytes,
                    offset + headerByteLength,
                    offset + boxByteLength,
                    result))
                {
                    return true;
                }
            }

            offset += boxByteLength;
        }

        return false;
    }

    internal bool FindAvifPropertyContainer(
        byte[] bytes,
        int offset,
        int end,
        GltfImageInfo result)
    {
        while (offset < end)
        {
            int boxByteLength = ReadIsoBoxByteLength(bytes, offset, end, "AVIF item properties");
            int headerByteLength = GetIsoBoxHeaderByteLength(bytes, offset);
            if (IsFourCharacterCode(bytes, offset + 4, 'i', 'p', 'c', 'o'))
            {
                if (FindAvifSpatialExtents(
                    bytes,
                    offset + headerByteLength,
                    offset + boxByteLength,
                    result))
                {
                    return true;
                }
            }

            offset += boxByteLength;
        }

        return false;
    }

    internal bool FindAvifSpatialExtents(
        byte[] bytes,
        int offset,
        int end,
        GltfImageInfo result)
    {
        while (offset < end)
        {
            int boxByteLength = ReadIsoBoxByteLength(bytes, offset, end, "AVIF property");
            int headerByteLength = GetIsoBoxHeaderByteLength(bytes, offset);
            if (IsFourCharacterCode(bytes, offset + 4, 'i', 's', 'p', 'e'))
            {
                if (boxByteLength < headerByteLength + 12)
                {
                    throw new GltfResourceException("AVIF ispe box is truncated.");
                }

                uint width = ReadUInt32BigEndian(bytes, offset + headerByteLength + 4);
                uint height = ReadUInt32BigEndian(bytes, offset + headerByteLength + 8);
                if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
                {
                    throw new GltfResourceException(
                        $"AVIF dimensions {width} by {height} are outside the supported signed range.");
                }

                result.HasDimensions = true;
                result.Width = (int)width;
                result.Height = (int)height;
                return true;
            }

            offset += boxByteLength;
        }

        return false;
    }

    internal int ReadIsoBoxByteLength(byte[] bytes, int offset, int end, string context)
    {
        if (end - offset < 8)
        {
            throw new GltfResourceException(
                $"{context} box header is truncated at byte {offset}.");
        }

        uint shortBoxByteLength = ReadUInt32BigEndian(bytes, offset);
        long boxByteLength;
        int headerByteLength = 8;
        if (shortBoxByteLength == 0)
        {
            boxByteLength = end - offset;
        }
        else if (shortBoxByteLength == 1)
        {
            if (end - offset < 16)
            {
                throw new GltfResourceException(
                    $"{context} extended box header is truncated at byte {offset}.");
            }

            ulong unsignedBoxByteLength = ReadUInt64BigEndian(bytes, offset + 8);
            if (unsignedBoxByteLength > int.MaxValue)
            {
                throw new GltfResourceException(
                    $"{context} box at byte {offset} exceeds the supported signed byte length.");
            }

            boxByteLength = (long)unsignedBoxByteLength;
            headerByteLength = 16;
        }
        else
        {
            boxByteLength = shortBoxByteLength;
        }

        if (boxByteLength < headerByteLength)
        {
            throw new GltfResourceException(
                $"{context} box at byte {offset} has invalid length {boxByteLength}.");
        }

        if ((long)offset + boxByteLength > end)
        {
            throw new GltfResourceException(
                $"{context} box at byte {offset} is truncated; it declares length {boxByteLength}.");
        }

        return (int)boxByteLength;
    }

    internal int GetIsoBoxHeaderByteLength(byte[] bytes, int offset)
    {
        if (ReadUInt32BigEndian(bytes, offset) == 1)
        {
            return 16;
        }

        return 8;
    }

    internal GltfImageInfo InspectDds(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            throw new GltfResourceException("DDS signature is truncated.");
        }

        if (!HasDdsSignature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain the DDS signature.");
        }

        if (bytes.Length < 128)
        {
            throw new GltfResourceException(
                $"DDS header is truncated; expected at least 128 bytes but found {bytes.Length}.");
        }

        uint headerByteLength = ReadUInt32LittleEndian(bytes, 4);
        uint pixelFormatByteLength = ReadUInt32LittleEndian(bytes, 76);
        if (headerByteLength != 124 || pixelFormatByteLength != 32)
        {
            throw new GltfResourceException(
                $"DDS header sizes {headerByteLength} and {pixelFormatByteLength} are invalid.");
        }

        uint height = ReadUInt32LittleEndian(bytes, 12);
        uint width = ReadUInt32LittleEndian(bytes, 16);
        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
        {
            throw new GltfResourceException(
                $"DDS dimensions {width} by {height} are outside the supported signed range.");
        }

        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.Dds;
        result.MimeType = GltfMimeTypes.Dds;
        result.HasDimensions = true;
        result.Width = (int)width;
        result.Height = (int)height;
        return result;
    }

    internal GltfImageInfo InspectAstc(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            throw new GltfResourceException("ASTC signature is truncated.");
        }

        if (!HasAstcSignature(bytes))
        {
            throw new GltfResourceException("Image bytes do not contain the ASTC signature.");
        }

        if (bytes.Length < 16)
        {
            throw new GltfResourceException(
                $"ASTC header is truncated; expected 16 bytes but found {bytes.Length}.");
        }

        if (bytes[4] == 0 || bytes[5] == 0 || bytes[6] == 0)
        {
            throw new GltfResourceException("ASTC header contains a zero block dimension.");
        }

        int width = ReadUInt24LittleEndian(bytes, 7);
        int height = ReadUInt24LittleEndian(bytes, 10);
        int depth = ReadUInt24LittleEndian(bytes, 13);
        if (width == 0 || height == 0 || depth == 0)
        {
            throw new GltfResourceException(
                $"ASTC dimensions {width} by {height} by {depth} are invalid.");
        }

        GltfImageInfo result = new GltfImageInfo();
        result.Format = GltfImageFormat.Astc;
        result.HasDimensions = true;
        result.Width = width;
        result.Height = height;
        return result;
    }

    internal void ValidateDeclaredFormat(GltfImageRequest request, GltfImageInfo info)
    {
        if (info.Format == GltfImageFormat.Unknown)
        {
            return;
        }

        if (request.SourceFormat != GltfImageFormat.Unknown
            && request.SourceFormat != info.Format)
        {
            throw new GltfResourceException(
                $"Image signature format {info.Format} does not match declared source format {request.SourceFormat}.");
        }

        if (info.MimeType != null
            && request.MimeType != null
            && !string.Equals(
                request.MimeType,
                info.MimeType,
                System.StringComparison.OrdinalIgnoreCase))
        {
            throw new GltfResourceException(
                $"Image signature media type '{info.MimeType}' does not match declared media type '{request.MimeType}'.");
        }
    }

    internal bool IsChunkType(
        byte[] bytes,
        int offset,
        char first,
        char second,
        char third,
        char fourth)
    {
        return bytes[offset] == (byte)first
            && bytes[offset + 1] == (byte)second
            && bytes[offset + 2] == (byte)third
            && bytes[offset + 3] == (byte)fourth;
    }

    internal uint ReadUInt32BigEndian(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal int ReadUInt16BigEndian(byte[] bytes, int offset)
    {
        return (bytes[offset] << 8) | bytes[offset + 1];
    }

    internal int ReadUInt16LittleEndian(byte[] bytes, int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8);
    }

    internal int ReadUInt24LittleEndian(byte[] bytes, int offset)
    {
        return bytes[offset]
            | (bytes[offset + 1] << 8)
            | (bytes[offset + 2] << 16);
    }

    internal uint ReadUInt32LittleEndian(byte[] bytes, int offset)
    {
        return bytes[offset]
            | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }

    internal ulong ReadUInt64BigEndian(byte[] bytes, int offset)
    {
        return ((ulong)bytes[offset] << 56)
            | ((ulong)bytes[offset + 1] << 48)
            | ((ulong)bytes[offset + 2] << 40)
            | ((ulong)bytes[offset + 3] << 32)
            | ((ulong)bytes[offset + 4] << 24)
            | ((ulong)bytes[offset + 5] << 16)
            | ((ulong)bytes[offset + 6] << 8)
            | bytes[offset + 7];
    }

    internal bool IsFourCharacterCode(
        byte[] bytes,
        int offset,
        char first,
        char second,
        char third,
        char fourth)
    {
        return bytes[offset] == (byte)first
            && bytes[offset + 1] == (byte)second
            && bytes[offset + 2] == (byte)third
            && bytes[offset + 3] == (byte)fourth;
    }
}
