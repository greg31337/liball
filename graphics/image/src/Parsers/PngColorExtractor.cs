using System.Collections.Generic;

namespace image;

internal class PngColorExtractor
{
    internal static void Extract(ImageMetadataDocument document, List<PngChunk> chunks, ImageMetadataReaderOptions options)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        foreach (PngChunk chunk in chunks)
        {
            if (chunk != null && !chunk.DataLimited)
            {
                ExtractChunk(document, chunk, options);
            }
        }

        ValidateColorChunkCombinations(document, chunks);
        ApplyColorSpacePrecedence(document, chunks);
    }

    internal static void ExtractChunk(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options)
    {
        switch (chunk.Type)
        {
            case "PLTE":
                ExtractPalette(document, chunk);
                return;
            case "gAMA":
                ExtractGamma(document, chunk);
                return;
            case "cHRM":
                ExtractChromaticities(document, chunk);
                return;
            case "sRGB":
                ExtractSrgb(document, chunk);
                return;
            case "iCCP":
                ExtractIcc(document, chunk, options);
                return;
            case "cICP":
                ExtractCicp(document, chunk);
                return;
            case "mDCV":
            case "mDCv":
                ExtractMdcv(document, chunk);
                return;
            case "cLLI":
            case "cLLi":
                ExtractClli(document, chunk);
                return;
            default:
                return;
        }
    }

    internal static void ExtractPalette(ImageMetadataDocument document, PngChunk chunk)
    {
        if (chunk.Data == null || chunk.Data.Length == 0 || chunk.Data.Length > 768 || (chunk.Data.Length % 3) != 0)
        {
            AddInvalidLength(document, chunk, "png.plte.length.invalid", "PNG PLTE length must contain between 1 and 256 three-byte palette entries.");
            return;
        }

        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "Png.PLTE.EntryCount", "Palette Entry Count", MetadataValue.FromInteger(chunk.Data.Length / 3), chunk, 0, chunk.Data.Length, "");
        MetadataTag palette = AddTag(directory, "Png.PLTE.Entries", "Palette Entries", MetadataValue.FromBytes(CopyRange(chunk.Data, 0, chunk.Data.Length)), chunk, 0, chunk.Data.Length, "");
        palette.RawValue = CopyRange(chunk.Data, 0, chunk.Data.Length);
    }

    internal static void ExtractGamma(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!HasLength(document, chunk, 4, "png.gama.length.invalid"))
        {
            return;
        }

        uint encoded = ReadUnsigned32(chunk.Data, 0);
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "Png.gAMA.EncodedValue", "Encoded Gamma", MetadataValue.FromUnsignedInteger(encoded), chunk, 0, 4, "");
        AddTag(directory, "Png.gAMA.Gamma", "Image Gamma", MetadataValue.FromDouble(encoded / 100000.0), chunk, 0, 4, "");

        if (encoded == 0)
        {
            AddInvalidField(document, chunk, "png.gama.value.invalid", "PNG gAMA value must be greater than zero.", 0, 4);
        }
    }

    internal static void ExtractChromaticities(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!HasLength(document, chunk, 32, "png.chrm.length.invalid"))
        {
            return;
        }

        MetadataDirectory directory = FindDirectory(document, chunk);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.WhitePointX", "White Point X", 0, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.WhitePointY", "White Point Y", 4, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.RedX", "Red X", 8, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.RedY", "Red Y", 12, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.GreenX", "Green X", 16, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.GreenY", "Green Y", 20, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.BlueX", "Blue X", 24, 100000.0);
        AddScaledUnsigned32(directory, chunk, "Png.cHRM.BlueY", "Blue Y", 28, 100000.0);
    }

    internal static void ExtractSrgb(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!HasLength(document, chunk, 1, "png.srgb.length.invalid"))
        {
            return;
        }

        int intent = chunk.Data[0];
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "Png.sRGB.RenderingIntent", "Rendering Intent", MetadataValue.FromInteger(intent), chunk, 0, 1, RenderingIntentName(intent));

        if (intent > 3)
        {
            AddInvalidField(document, chunk, "png.srgb.intent.invalid", "PNG sRGB rendering intent must be between zero and three.", 0, 1);
        }
        else
        {
            SetFrameColorSpace(document, ImageColorSpaceKind.Rgb);
        }
    }

    internal static void ExtractIcc(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options)
    {
        if (chunk.Data == null || chunk.Data.Length < 3)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.iccp.truncated", "PNG iCCP payload was too short for a profile name and compression method.", "iCCP", chunk.DataOffset, chunk.Length);
            return;
        }

        int separator = FindNull(chunk.Data, 0, 80);
        if (separator < 1 || separator > 79)
        {
            AddInvalidField(document, chunk, "png.iccp.name.invalid", "PNG iCCP profile name must contain 1 to 79 bytes followed by a null separator.", 0, chunk.Data.Length);
            return;
        }

        if (separator + 1 >= chunk.Data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.iccp.compression.truncated", "PNG iCCP compression method was missing.", "iCCP", chunk.DataOffset + separator + 1, 1);
            return;
        }

        string profileName = MetadataStringReader.DecodeLatin1Range(chunk.Data, 0, separator, document.Diagnostics, "iCCP");
        int compressionMethod = chunk.Data[separator + 1];
        int compressedOffset = separator + 2;
        int compressedLength = chunk.Data.Length - compressedOffset;
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "Png.iCCP.ProfileName", "Profile Name", MetadataValue.FromString(profileName), chunk, 0, separator, profileName);
        AddTag(directory, "Png.iCCP.CompressionMethod", "Compression Method", MetadataValue.FromInteger(compressionMethod), chunk, separator + 1, 1, compressionMethod == 0 ? "Deflate" : "Unknown");
        AddTag(directory, "Png.iCCP.CompressedByteLength", "Compressed Byte Length", MetadataValue.FromInteger(compressedLength), chunk, compressedOffset, compressedLength, "");

        if (!IsValidProfileName(chunk.Data, separator))
        {
            AddInvalidField(document, chunk, "png.iccp.name.characters.invalid", "PNG iCCP profile name contained invalid spacing or character bytes.", 0, separator);
        }

        if (compressionMethod != 0)
        {
            AddInvalidField(document, chunk, "png.iccp.compression.invalid", "PNG iCCP compression method must be zero.", separator + 1, 1);
            return;
        }

        if (!ShouldParseIcc(options))
        {
            return;
        }

        byte[] compressed = CopyRange(chunk.Data, compressedOffset, compressedLength);
        long limit = GetIccLimit(options);
        byte[] profileData = ZlibHelper.Decompress(compressed, limit, document.Diagnostics, "iCCP");
        if (profileData.Length == 0)
        {
            return;
        }

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Icc;
        profile.Family = MetadataFamily.Icc;
        profile.Key = "Icc.PNG.iCCP";
        profile.Name = profileName;
        profile.MimeType = "application/vnd.iccprofile";
        profile.Data = profileData;
        profile.Offset = chunk.DataOffset + compressedOffset;
        profile.Length = profileData.Length;
        document.AddProfile(profile);
        IccProfileParser.ParseProfile(document, profile, options, "PNG/iCCP/ICC", "iCCP");
        AddTag(directory, "Png.iCCP.ProfileByteLength", "Profile Byte Length", MetadataValue.FromInteger(profileData.Length), chunk, compressedOffset, compressedLength, "");
    }

    internal static void ExtractCicp(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!HasLength(document, chunk, 4, "png.cicp.length.invalid"))
        {
            return;
        }

        int primaries = chunk.Data[0];
        int transfer = chunk.Data[1];
        int matrix = chunk.Data[2];
        int fullRange = chunk.Data[3];
        MetadataDirectory directory = FindDirectory(document, chunk);
        AddTag(directory, "Png.cICP.ColorPrimaries", "Color Primaries", MetadataValue.FromInteger(primaries), chunk, 0, 1, ColorPrimariesName(primaries));
        AddTag(directory, "Png.cICP.TransferFunction", "Transfer Function", MetadataValue.FromInteger(transfer), chunk, 1, 1, TransferFunctionName(transfer));
        AddTag(directory, "Png.cICP.MatrixCoefficients", "Matrix Coefficients", MetadataValue.FromInteger(matrix), chunk, 2, 1, matrix == 0 ? "Identity" : "Unknown");
        AddTag(directory, "Png.cICP.VideoFullRange", "Video Full Range", MetadataValue.FromBoolean(fullRange == 1), chunk, 3, 1, fullRange == 1 ? "Full" : "Narrow");

        if (matrix != 0)
        {
            AddInvalidField(document, chunk, "png.cicp.matrix.invalid", "PNG cICP matrix coefficients must be zero for RGB samples.", 2, 1);
        }

        if (fullRange != 0 && fullRange != 1)
        {
            AddInvalidField(document, chunk, "png.cicp.range.invalid", "PNG cICP video full range flag must be zero or one.", 3, 1);
        }

        SetFrameColorSpace(document, ColorSpaceForPrimaries(primaries));
    }

    internal static void ExtractMdcv(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!HasLength(document, chunk, 24, "png.mdcv.length.invalid"))
        {
            return;
        }

        MetadataDirectory directory = FindDirectory(document, chunk);
        string prefix = "Png." + chunk.Type;
        AddScaledUnsigned16(directory, chunk, prefix + ".RedX", "Red X", 0, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".RedY", "Red Y", 2, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".GreenX", "Green X", 4, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".GreenY", "Green Y", 6, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".BlueX", "Blue X", 8, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".BlueY", "Blue Y", 10, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".WhitePointX", "White Point X", 12, 50000.0);
        AddScaledUnsigned16(directory, chunk, prefix + ".WhitePointY", "White Point Y", 14, 50000.0);
        AddScaledUnsigned32(directory, chunk, prefix + ".MaximumLuminance", "Maximum Luminance", 16, 10000.0);
        AddScaledUnsigned32(directory, chunk, prefix + ".MinimumLuminance", "Minimum Luminance", 20, 10000.0);
    }

    internal static void ExtractClli(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!HasLength(document, chunk, 8, "png.clli.length.invalid"))
        {
            return;
        }

        MetadataDirectory directory = FindDirectory(document, chunk);
        string prefix = "Png." + chunk.Type;
        AddScaledUnsigned32(directory, chunk, prefix + ".MaximumContentLightLevel", "Maximum Content Light Level", 0, 10000.0);
        AddScaledUnsigned32(directory, chunk, prefix + ".MaximumFrameAverageLightLevel", "Maximum Frame-Average Light Level", 4, 10000.0);
    }

    internal static void ValidateColorChunkCombinations(ImageMetadataDocument document, List<PngChunk> chunks)
    {
        if (HasChunk(chunks, "iCCP") && HasChunk(chunks, "sRGB"))
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "png.color.profile.conflict", "PNG contained both iCCP and sRGB color profile chunks.", "PNG", 0, 0);
        }

        PngChunk? mdcv = FindFirstChunk(chunks, "mDCV", "mDCv");
        if (mdcv != null && !HasChunk(chunks, "cICP"))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.mdcv.cicp.missing", "PNG mDCV chunk requires an accompanying cICP chunk.", "mDCV", mdcv.Offset, 12 + mdcv.Length);
        }
    }

    internal static void ApplyColorSpacePrecedence(ImageMetadataDocument document, List<PngChunk> chunks)
    {
        PngChunk? cicp = FindChunk(chunks, "cICP");
        if (cicp != null && cicp.Data != null && cicp.Data.Length == 4)
        {
            ImageColorSpaceKind cicpColorSpace = ColorSpaceForPrimaries(cicp.Data[0]);
            if (cicpColorSpace != ImageColorSpaceKind.Unknown)
            {
                SetFrameColorSpace(document, cicpColorSpace);
                return;
            }
        }

        PngChunk? srgb = FindChunk(chunks, "sRGB");
        if (srgb != null && srgb.Data != null && srgb.Data.Length == 1 && srgb.Data[0] <= 3)
        {
            SetFrameColorSpace(document, ImageColorSpaceKind.Rgb);
        }
    }

    internal static bool HasLength(ImageMetadataDocument document, PngChunk chunk, int expected, string code)
    {
        if (chunk.Data != null && chunk.Data.Length == expected && chunk.Length == expected)
        {
            return true;
        }

        AddInvalidLength(document, chunk, code, "PNG " + chunk.Type + " chunk length must be exactly " + expected.ToString() + " bytes.");
        return false;
    }

    internal static void AddInvalidLength(ImageMetadataDocument document, PngChunk chunk, string code, string message)
    {
        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, code, message, chunk.Type, chunk.DataOffset, chunk.Length);
    }

    internal static void AddInvalidField(ImageMetadataDocument document, PngChunk chunk, string code, string message, int relativeOffset, long length)
    {
        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, code, message, chunk.Type, chunk.DataOffset + relativeOffset, length);
    }

    internal static MetadataDirectory FindDirectory(ImageMetadataDocument document, PngChunk chunk)
    {
        if (document == null || document.Directories == null || chunk == null)
        {
            return new MetadataDirectory();
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.Png && directory.Group == chunk.Type && directory.Offset == chunk.Offset)
            {
                return directory;
            }
        }

        return PngChunkTraverser.AddChunkDirectory(document, chunk);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, PngChunk chunk, int relativeOffset, long length, string displayValue)
    {
        if (directory == null)
        {
            return new MetadataTag();
        }

        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = chunk.DataOffset + relativeOffset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        tag.RawValue = CopyRange(chunk.Data, relativeOffset, ToIntLength(length));
        return tag;
    }

    internal static void AddScaledUnsigned32(MetadataDirectory directory, PngChunk chunk, string key, string name, int offset, double scale)
    {
        uint encoded = ReadUnsigned32(chunk.Data, offset);
        AddTag(directory, key, name, MetadataValue.FromDouble(encoded / scale), chunk, offset, 4, "");
    }

    internal static void AddScaledUnsigned16(MetadataDirectory directory, PngChunk chunk, string key, string name, int offset, double scale)
    {
        int encoded = ReadUnsigned16(chunk.Data, offset);
        AddTag(directory, key, name, MetadataValue.FromDouble(encoded / scale), chunk, offset, 2, "");
    }

    internal static uint ReadUnsigned32(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 4 > bytes.Length)
        {
            return 0;
        }

        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static int ReadUnsigned16(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 2 > bytes.Length)
        {
            return 0;
        }

        return (bytes[offset] * 256) + bytes[offset + 1];
    }

    internal static byte[] CopyRange(byte[] bytes, int offset, int count)
    {
        if (bytes == null || offset < 0 || count <= 0 || offset >= bytes.Length)
        {
            return new byte[0];
        }

        int actualCount = count;
        if (actualCount > bytes.Length - offset)
        {
            actualCount = bytes.Length - offset;
        }

        byte[] result = new byte[actualCount];
        int index = 0;
        while (index < actualCount)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static int FindNull(byte[] bytes, int offset, int maximumCount)
    {
        if (bytes == null || offset < 0 || offset >= bytes.Length)
        {
            return -1;
        }

        int end = offset + maximumCount;
        if (end > bytes.Length)
        {
            end = bytes.Length;
        }

        int index = offset;
        while (index < end)
        {
            if (bytes[index] == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool IsValidProfileName(byte[] bytes, int length)
    {
        if (bytes == null || length < 1 || length > 79 || bytes[0] == 32 || bytes[length - 1] == 32)
        {
            return false;
        }

        int index = 0;
        bool previousSpace = false;
        while (index < length)
        {
            int value = bytes[index];
            bool printable = (value >= 32 && value <= 126) || value >= 161;
            if (!printable || (value == 32 && previousSpace))
            {
                return false;
            }

            previousSpace = value == 32;
            index = index + 1;
        }

        return true;
    }

    internal static string RenderingIntentName(int intent)
    {
        switch (intent)
        {
            case 0:
                return "Perceptual";
            case 1:
                return "Relative colorimetric";
            case 2:
                return "Saturation";
            case 3:
                return "Absolute colorimetric";
            default:
                return "Unknown";
        }
    }

    internal static string ColorPrimariesName(int primaries)
    {
        switch (primaries)
        {
            case 1:
                return "BT.709";
            case 9:
                return "BT.2020";
            case 12:
                return "Display P3";
            default:
                return "Unknown";
        }
    }

    internal static string TransferFunctionName(int transfer)
    {
        switch (transfer)
        {
            case 1:
                return "BT.709";
            case 13:
                return "sRGB";
            case 16:
                return "PQ";
            case 18:
                return "HLG";
            default:
                return "Unknown";
        }
    }

    internal static ImageColorSpaceKind ColorSpaceForPrimaries(int primaries)
    {
        if (primaries == 12)
        {
            return ImageColorSpaceKind.DisplayP3;
        }

        if (primaries == 9)
        {
            return ImageColorSpaceKind.WideGamutRgb;
        }

        if (primaries == 1)
        {
            return ImageColorSpaceKind.Rgb;
        }

        return ImageColorSpaceKind.Unknown;
    }

    internal static void SetFrameColorSpace(ImageMetadataDocument document, ImageColorSpaceKind colorSpace)
    {
        if (document == null || document.Frames == null || colorSpace == ImageColorSpaceKind.Unknown)
        {
            return;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null)
            {
                frame.ColorSpace = colorSpace;
            }
        }
    }

    internal static bool HasChunk(List<PngChunk> chunks, string type)
    {
        return FindChunk(chunks, type) != null;
    }

    internal static PngChunk? FindChunk(List<PngChunk> chunks, string type)
    {
        if (chunks == null)
        {
            return null;
        }

        foreach (PngChunk chunk in chunks)
        {
            if (chunk != null && chunk.Type == type)
            {
                return chunk;
            }
        }

        return null;
    }

    internal static PngChunk? FindFirstChunk(List<PngChunk> chunks, string firstType, string secondType)
    {
        PngChunk? first = FindChunk(chunks, firstType);
        if (first != null)
        {
            return first;
        }

        return FindChunk(chunks, secondType);
    }

    internal static bool ShouldParseIcc(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseIcc;
    }

    internal static long GetIccLimit(ImageMetadataReaderOptions options)
    {
        ImageMetadataReaderOptions actual = options;
        if (actual == null)
        {
            actual = new ImageMetadataReaderOptions();
        }

        long limit = actual.MaxProfileBytes;
        if (actual.MaxDecompressedBytes < limit)
        {
            limit = actual.MaxDecompressedBytes;
        }

        return limit;
    }

    internal static int ToIntLength(long length)
    {
        if (length <= 0)
        {
            return 0;
        }

        if (length > 2147483647)
        {
            return 2147483647;
        }

        return (int)length;
    }
}
