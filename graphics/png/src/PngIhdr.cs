using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngIhdrParser
{
    internal const int DataLength = 13;

    internal static PngIhdr Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != DataLength)
        {
            throw new InvalidDataException("IHDR data must contain exactly 13 bytes.");
        }

        return Parse(data, 0);
    }

    internal static PngIhdr Parse(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length - DataLength)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        uint width = PngBinaryReader.ReadUInt32(data, offset);
        uint height = PngBinaryReader.ReadUInt32(data, offset + 4);
        if (width > int.MaxValue || height > int.MaxValue)
        {
            throw new OverflowException();
        }

        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = (int)width;
        ihdr.Height = (int)height;
        ihdr.BitDepth = (PngBitDepth)data[offset + 8];
        ihdr.ColorType = (PngColorType)data[offset + 9];
        ihdr.CompressionMethod = (PngCompressionMethod)data[offset + 10];
        ihdr.FilterMethod = (PngFilterMethod)data[offset + 11];
        ihdr.InterlaceMethod = (PngInterlaceMethod)data[offset + 12];
        return ihdr;
    }

    internal static PngIhdr Parse(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkIHDR)
        {
            throw new InvalidDataException("Chunk type must be IHDR.");
        }

        return Parse(chunk.Data);
    }
}

internal class PngIhdrWriter
{
    internal static byte[] WriteData(PngIhdr ihdr)
    {
        byte[] data = new byte[PngIhdrParser.DataLength];
        WriteData(data, 0, ihdr);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(ihdr);

        if (offset < 0 || offset > data.Length - PngIhdrParser.DataLength)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (ihdr.Width < 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (ihdr.Height < 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        PngBinaryWriter.WriteUInt32(data, offset, (uint)ihdr.Width);
        PngBinaryWriter.WriteUInt32(data, offset + 4, (uint)ihdr.Height);
        data[offset + 8] = ToByte((int)ihdr.BitDepth, "BitDepth");
        data[offset + 9] = ToByte((int)ihdr.ColorType, "ColorType");
        data[offset + 10] = ToByte((int)ihdr.CompressionMethod, "CompressionMethod");
        data[offset + 11] = ToByte((int)ihdr.FilterMethod, "FilterMethod");
        data[offset + 12] = ToByte((int)ihdr.InterlaceMethod, "InterlaceMethod");
    }

    internal static PngChunk WriteChunk(PngIhdr ihdr)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIHDR;
        chunk.Data = WriteData(ihdr);
        chunk.Length = PngIhdrParser.DataLength;
        chunk.IsKnown = true;
        chunk.IsCritical = true;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static byte ToByte(int value, string name)
    {
        if (value < 0 || value > 255)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return (byte)value;
    }
}

internal class PngIhdrValidator
{
    internal static bool IsValid(PngIhdr ihdr)
    {
        return Validate(ihdr).IsValid;
    }

    internal static bool IsValid(PngIhdr ihdr, PngSecurityLimits limits)
    {
        return Validate(ihdr, limits).IsValid;
    }

    internal static PngValidationResult Validate(PngIhdr ihdr)
    {
        return Validate(ihdr, new PngSecurityLimits());
    }

    internal static PngValidationResult Validate(PngIhdr ihdr, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(limits);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidateDimensions(errors, ihdr, limits, 0);
        ValidateColorAndBitDepth(errors, ihdr, 0);
        ValidateMethods(errors, ihdr, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult Validate(PngChunk chunk)
    {
        return Validate(chunk, new PngSecurityLimits());
    }

    internal static PngValidationResult Validate(PngChunk chunk, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
        ArgumentNullException.ThrowIfNull(limits);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        if (chunk.Type != PngConstants.ChunkIHDR)
        {
            AddError(errors, "PNG_IHDR_CHUNK_TYPE_INVALID", "IHDR validation requires an IHDR chunk.", chunk.Type, chunk.Offset);
        }

        if (chunk.Data.Length != PngIhdrParser.DataLength)
        {
            AddError(errors, "PNG_IHDR_LENGTH_INVALID", "IHDR must contain exactly 13 bytes.", chunk.Type, chunk.Offset);
            return FinishResult(result, errors);
        }

        PngIhdr ihdr = PngIhdrParser.Parse(chunk.Data);
        result = CreateBaseResult(ihdr);
        ValidateDimensions(errors, ihdr, limits, chunk.Offset);
        ValidateColorAndBitDepth(errors, ihdr, chunk.Offset);
        ValidateMethods(errors, ihdr, chunk.Offset);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult CreateBaseResult(PngIhdr ihdr)
    {
        PngValidationResult result = new PngValidationResult();
        result.Info.Width = ihdr.Width;
        result.Info.Height = ihdr.Height;
        result.Info.BitDepth = ihdr.BitDepth;
        result.Info.ColorType = ihdr.ColorType;
        result.Info.CompressionMethod = ihdr.CompressionMethod;
        result.Info.FilterMethod = ihdr.FilterMethod;
        result.Info.InterlaceMethod = ihdr.InterlaceMethod;
        result.Info.PixelFormat = GetPixelFormat(ihdr.ColorType, ihdr.BitDepth);
        return result;
    }

    internal static void ValidateDimensions(List<PngError> errors, PngIhdr ihdr, PngSecurityLimits limits, long offset)
    {
        if (ihdr.Width <= 0)
        {
            AddError(errors, "PNG_IHDR_WIDTH_INVALID", "IHDR width must be greater than zero.", PngConstants.ChunkIHDR, offset);
        }

        if (ihdr.Height <= 0)
        {
            AddError(errors, "PNG_IHDR_HEIGHT_INVALID", "IHDR height must be greater than zero.", PngConstants.ChunkIHDR, offset);
        }

        if (limits.MaxWidth > 0 && ihdr.Width > limits.MaxWidth)
        {
            AddError(errors, "PNG_IHDR_WIDTH_LIMIT_EXCEEDED", "IHDR width exceeds the configured limit.", PngConstants.ChunkIHDR, offset);
        }

        if (limits.MaxHeight > 0 && ihdr.Height > limits.MaxHeight)
        {
            AddError(errors, "PNG_IHDR_HEIGHT_LIMIT_EXCEEDED", "IHDR height exceeds the configured limit.", PngConstants.ChunkIHDR, offset);
        }

        if (ihdr.Width > 0 && ihdr.Height > 0 && limits.MaxPixelCount > 0)
        {
            long pixelCount = (long)ihdr.Width * ihdr.Height;
            if (pixelCount > limits.MaxPixelCount)
            {
                AddError(errors, "PNG_IHDR_PIXEL_COUNT_LIMIT_EXCEEDED", "IHDR pixel count exceeds the configured limit.", PngConstants.ChunkIHDR, offset);
            }
        }
    }

    internal static void ValidateColorAndBitDepth(List<PngError> errors, PngIhdr ihdr, long offset)
    {
        bool bitDepthValid = IsKnownBitDepth(ihdr.BitDepth);
        bool colorTypeValid = IsKnownColorType(ihdr.ColorType);

        if (!bitDepthValid)
        {
            AddError(errors, "PNG_IHDR_BIT_DEPTH_INVALID", "IHDR bit depth must be 1, 2, 4, 8, or 16.", PngConstants.ChunkIHDR, offset);
        }

        if (!colorTypeValid)
        {
            AddError(errors, "PNG_IHDR_COLOR_TYPE_INVALID", "IHDR color type must be 0, 2, 3, 4, or 6.", PngConstants.ChunkIHDR, offset);
        }

        if (bitDepthValid && colorTypeValid && !IsBitDepthAllowedForColorType(ihdr.BitDepth, ihdr.ColorType))
        {
            AddError(errors, "PNG_IHDR_BIT_DEPTH_COLOR_TYPE_INVALID", "IHDR bit depth is not allowed for the color type.", PngConstants.ChunkIHDR, offset);
        }
    }

    internal static void ValidateMethods(List<PngError> errors, PngIhdr ihdr, long offset)
    {
        if ((int)ihdr.CompressionMethod != PngConstants.CompressionMethodDeflate)
        {
            AddError(errors, "PNG_IHDR_COMPRESSION_METHOD_INVALID", "IHDR compression method must be 0.", PngConstants.ChunkIHDR, offset);
        }

        if ((int)ihdr.FilterMethod != PngConstants.FilterMethodAdaptive)
        {
            AddError(errors, "PNG_IHDR_FILTER_METHOD_INVALID", "IHDR filter method must be 0.", PngConstants.ChunkIHDR, offset);
        }

        if ((int)ihdr.InterlaceMethod != PngConstants.InterlaceMethodNone
            && (int)ihdr.InterlaceMethod != PngConstants.InterlaceMethodAdam7)
        {
            AddError(errors, "PNG_IHDR_INTERLACE_METHOD_INVALID", "IHDR interlace method must be 0 or 1.", PngConstants.ChunkIHDR, offset);
        }
    }

    internal static bool IsKnownBitDepth(PngBitDepth bitDepth)
    {
        return (int)bitDepth == PngConstants.BitDepth1
            || (int)bitDepth == PngConstants.BitDepth2
            || (int)bitDepth == PngConstants.BitDepth4
            || (int)bitDepth == PngConstants.BitDepth8
            || (int)bitDepth == PngConstants.BitDepth16;
    }

    internal static bool IsKnownColorType(PngColorType colorType)
    {
        return (int)colorType == PngConstants.ColorTypeGrayscale
            || (int)colorType == PngConstants.ColorTypeTruecolor
            || (int)colorType == PngConstants.ColorTypeIndexedColor
            || (int)colorType == PngConstants.ColorTypeGrayscaleAlpha
            || (int)colorType == PngConstants.ColorTypeTruecolorAlpha;
    }

    internal static bool IsBitDepthAllowedForColorType(PngBitDepth bitDepth, PngColorType colorType)
    {
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return IsKnownBitDepth(bitDepth);
            case PngColorType.Truecolor:
                return bitDepth == PngBitDepth.Eight || bitDepth == PngBitDepth.Sixteen;
            case PngColorType.IndexedColor:
                return bitDepth == PngBitDepth.One
                    || bitDepth == PngBitDepth.Two
                    || bitDepth == PngBitDepth.Four
                    || bitDepth == PngBitDepth.Eight;
            case PngColorType.GrayscaleAlpha:
                return bitDepth == PngBitDepth.Eight || bitDepth == PngBitDepth.Sixteen;
            case PngColorType.TruecolorAlpha:
                return bitDepth == PngBitDepth.Eight || bitDepth == PngBitDepth.Sixteen;
            default:
                return false;
        }
    }

    internal static PngPixelFormat GetPixelFormat(PngColorType colorType, PngBitDepth bitDepth)
    {
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return GetGrayscalePixelFormat(bitDepth);
            case PngColorType.Truecolor:
                return GetTruecolorPixelFormat(bitDepth);
            case PngColorType.IndexedColor:
                return GetIndexedPixelFormat(bitDepth);
            case PngColorType.GrayscaleAlpha:
                return GetGrayscaleAlphaPixelFormat(bitDepth);
            case PngColorType.TruecolorAlpha:
                return GetTruecolorAlphaPixelFormat(bitDepth);
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetGrayscalePixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return PngPixelFormat.Grayscale1;
            case PngBitDepth.Two:
                return PngPixelFormat.Grayscale2;
            case PngBitDepth.Four:
                return PngPixelFormat.Grayscale4;
            case PngBitDepth.Eight:
                return PngPixelFormat.Grayscale8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.Grayscale16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetTruecolorPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.Eight:
                return PngPixelFormat.Rgb8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.Rgb16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetIndexedPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return PngPixelFormat.Indexed1;
            case PngBitDepth.Two:
                return PngPixelFormat.Indexed2;
            case PngBitDepth.Four:
                return PngPixelFormat.Indexed4;
            case PngBitDepth.Eight:
                return PngPixelFormat.Indexed8;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetGrayscaleAlphaPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.Eight:
                return PngPixelFormat.GrayscaleAlpha8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.GrayscaleAlpha16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetTruecolorAlphaPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.Eight:
                return PngPixelFormat.Rgba8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.Rgba16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngValidationResult FinishResult(PngValidationResult result, List<PngError> errors)
    {
        result.Errors = errors.ToArray();
        result.IsValid = errors.Count == 0;
        return result;
    }

    internal static void AddError(List<PngError> errors, string code, string message, string chunkType, long offset)
    {
        PngError error = new PngError();
        error.Code = code;
        error.Message = message;
        error.ChunkType = chunkType;
        error.Offset = offset;
        error.IsFatal = true;
        errors.Add(error);
    }
}
