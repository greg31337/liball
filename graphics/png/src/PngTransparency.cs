using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngTransparencyParser
{
    internal const int GrayscaleDataLength = 2;
    internal const int TruecolorDataLength = 6;

    internal static PngIndexedTransparency ParseIndexed(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseIndexed(data, 0, data.Length);
    }

    internal static PngIndexedTransparency ParseIndexed(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        PngIndexedTransparency transparency = new PngIndexedTransparency();
        transparency.Alpha = new byte[length];
        int index = 0;
        while (index < length)
        {
            transparency.Alpha[index] = data[offset + index];
            index += 1;
        }

        return transparency;
    }

    internal static PngIndexedTransparency ParseIndexed(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkTransparency)
        {
            throw new InvalidDataException("Chunk type must be tRNS.");
        }

        return ParseIndexed(chunk.Data);
    }

    internal static PngGrayscaleTransparency ParseGrayscale(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseGrayscale(data, 0, data.Length);
    }

    internal static PngGrayscaleTransparency ParseGrayscale(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (length != GrayscaleDataLength)
        {
            throw new InvalidDataException("Grayscale tRNS data must contain exactly 2 bytes.");
        }

        PngGrayscaleTransparency transparency = new PngGrayscaleTransparency();
        transparency.Gray = PngBinaryReader.ReadUInt16(data, offset);
        return transparency;
    }

    internal static PngGrayscaleTransparency ParseGrayscale(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkTransparency)
        {
            throw new InvalidDataException("Chunk type must be tRNS.");
        }

        return ParseGrayscale(chunk.Data);
    }

    internal static PngTruecolorTransparency ParseTruecolor(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return ParseTruecolor(data, 0, data.Length);
    }

    internal static PngTruecolorTransparency ParseTruecolor(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (length != TruecolorDataLength)
        {
            throw new InvalidDataException("Truecolor tRNS data must contain exactly 6 bytes.");
        }

        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        transparency.Red = PngBinaryReader.ReadUInt16(data, offset);
        transparency.Green = PngBinaryReader.ReadUInt16(data, offset + 2);
        transparency.Blue = PngBinaryReader.ReadUInt16(data, offset + 4);
        return transparency;
    }

    internal static PngTruecolorTransparency ParseTruecolor(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkTransparency)
        {
            throw new InvalidDataException("Chunk type must be tRNS.");
        }

        return ParseTruecolor(chunk.Data);
    }
}

internal class PngTransparencyWriter
{
    internal static byte[] WriteIndexedData(PngIndexedTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(transparency.Alpha);

        byte[] data = new byte[transparency.Alpha.Length];
        WriteIndexedData(data, 0, transparency);
        return data;
    }

    internal static void WriteIndexedData(byte[] data, int offset, PngIndexedTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(transparency.Alpha);

        if (offset < 0 || offset > data.Length - transparency.Alpha.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        int index = 0;
        while (index < transparency.Alpha.Length)
        {
            data[offset + index] = transparency.Alpha[index];
            index += 1;
        }
    }

    internal static PngChunk WriteIndexedChunk(PngIndexedTransparency transparency)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTransparency;
        chunk.Data = WriteIndexedData(transparency);
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static byte[] WriteGrayscaleData(PngGrayscaleTransparency transparency)
    {
        byte[] data = new byte[PngTransparencyParser.GrayscaleDataLength];
        WriteGrayscaleData(data, 0, transparency);
        return data;
    }

    internal static void WriteGrayscaleData(byte[] data, int offset, PngGrayscaleTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(transparency);

        if (offset < 0 || offset > data.Length - PngTransparencyParser.GrayscaleDataLength)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        PngBinaryWriter.WriteUInt16(data, offset, transparency.Gray);
    }

    internal static PngChunk WriteGrayscaleChunk(PngGrayscaleTransparency transparency)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTransparency;
        chunk.Data = WriteGrayscaleData(transparency);
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static byte[] WriteTruecolorData(PngTruecolorTransparency transparency)
    {
        byte[] data = new byte[PngTransparencyParser.TruecolorDataLength];
        WriteTruecolorData(data, 0, transparency);
        return data;
    }

    internal static void WriteTruecolorData(byte[] data, int offset, PngTruecolorTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(transparency);

        if (offset < 0 || offset > data.Length - PngTransparencyParser.TruecolorDataLength)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        PngBinaryWriter.WriteUInt16(data, offset, transparency.Red);
        PngBinaryWriter.WriteUInt16(data, offset + 2, transparency.Green);
        PngBinaryWriter.WriteUInt16(data, offset + 4, transparency.Blue);
    }

    internal static PngChunk WriteTruecolorChunk(PngTruecolorTransparency transparency)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTransparency;
        chunk.Data = WriteTruecolorData(transparency);
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }
}

internal class PngTransparencyValidator
{
    internal const int MaximumIndexedAlphaCount = 256;

    internal static bool IsValidIndexed(PngIndexedTransparency transparency)
    {
        return ValidateIndexed(transparency).IsValid;
    }

    internal static bool IsValidIndexed(PngIndexedTransparency transparency, PngPalette palette)
    {
        return ValidateIndexed(transparency, palette).IsValid;
    }

    internal static bool IsValidGrayscale(PngGrayscaleTransparency transparency)
    {
        return ValidateGrayscale(transparency).IsValid;
    }

    internal static bool IsValidGrayscale(PngGrayscaleTransparency transparency, PngIhdr ihdr)
    {
        return ValidateGrayscale(transparency, ihdr).IsValid;
    }

    internal static bool IsValidTruecolor(PngTruecolorTransparency transparency)
    {
        return ValidateTruecolor(transparency).IsValid;
    }

    internal static bool IsValidTruecolor(PngTruecolorTransparency transparency, PngIhdr ihdr)
    {
        return ValidateTruecolor(transparency, ihdr).IsValid;
    }

    internal static PngValidationResult ValidateIndexed(PngIndexedTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(transparency.Alpha);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        ValidateIndexedAlpha(errors, transparency, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateIndexed(PngIndexedTransparency transparency, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(transparency.Alpha);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        ValidateIndexedAlpha(errors, transparency, 0);
        ValidatePaletteEntryLimit(errors, transparency, palette, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateIndexed(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        ValidateChunkType(errors, chunk);
        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(chunk.Data);
        ValidateIndexedAlpha(errors, transparency, chunk.Offset);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateIndexed(PngChunk chunk, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        ValidateChunkType(errors, chunk);
        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(chunk.Data);
        ValidateIndexedAlpha(errors, transparency, chunk.Offset);
        ValidatePaletteEntryLimit(errors, transparency, palette, chunk.Offset);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateIndexed(PngChunk chunk, PngIhdr ihdr, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidateIndexedColorType(errors, ihdr, chunk.Offset);
        ValidateChunkType(errors, chunk);
        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(chunk.Data);
        ValidateIndexedAlpha(errors, transparency, chunk.Offset);
        ValidatePaletteEntryLimit(errors, transparency, palette, chunk.Offset);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateGrayscale(PngGrayscaleTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateGrayscale(PngGrayscaleTransparency transparency, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidateGrayscaleColorType(errors, ihdr, 0);
        ValidateGrayscaleSampleRange(errors, transparency, ihdr, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateGrayscale(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        bool canParse = ValidateGrayscaleChunkShape(errors, chunk);
        if (canParse)
        {
            PngGrayscaleTransparency transparency = PngTransparencyParser.ParseGrayscale(chunk.Data);
            ValidateGrayscale(transparency);
        }

        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateGrayscale(PngChunk chunk, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidateGrayscaleColorType(errors, ihdr, chunk.Offset);
        bool canParse = ValidateGrayscaleChunkShape(errors, chunk);
        if (canParse)
        {
            PngGrayscaleTransparency transparency = PngTransparencyParser.ParseGrayscale(chunk.Data);
            ValidateGrayscaleSampleRange(errors, transparency, ihdr, chunk.Offset);
        }

        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateTruecolor(PngTruecolorTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(transparency);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateTruecolor(PngTruecolorTransparency transparency, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidateTruecolorColorType(errors, ihdr, 0);
        ValidateTruecolorSampleRange(errors, transparency, ihdr, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateTruecolor(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        bool canParse = ValidateTruecolorChunkShape(errors, chunk);
        if (canParse)
        {
            PngTruecolorTransparency transparency = PngTransparencyParser.ParseTruecolor(chunk.Data);
            ValidateTruecolor(transparency);
        }

        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidateTruecolor(PngChunk chunk, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidateTruecolorColorType(errors, ihdr, chunk.Offset);
        bool canParse = ValidateTruecolorChunkShape(errors, chunk);
        if (canParse)
        {
            PngTruecolorTransparency transparency = PngTransparencyParser.ParseTruecolor(chunk.Data);
            ValidateTruecolorSampleRange(errors, transparency, ihdr, chunk.Offset);
        }

        return FinishResult(result, errors);
    }

    internal static void ValidateChunkType(List<PngError> errors, PngChunk chunk)
    {
        if (chunk.Type != PngConstants.ChunkTransparency)
        {
            AddError(errors, "PNG_TRNS_CHUNK_TYPE_INVALID", "tRNS validation requires a tRNS chunk.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateIndexedAlpha(List<PngError> errors, PngIndexedTransparency transparency, long offset)
    {
        if (transparency.Alpha.Length == 0 || transparency.Alpha.Length > MaximumIndexedAlphaCount)
        {
            AddError(errors, "PNG_TRNS_INDEXED_ALPHA_LENGTH_INVALID", "Indexed-color tRNS alpha count must be 1 through 256.", PngConstants.ChunkTransparency, offset);
        }
    }

    internal static void ValidatePaletteEntryLimit(List<PngError> errors, PngIndexedTransparency transparency, PngPalette palette, long offset)
    {
        if (transparency.Alpha.Length > palette.Entries.Length)
        {
            AddError(errors, "PNG_TRNS_INDEXED_ALPHA_EXCEEDS_PALETTE", "Indexed-color tRNS alpha count must not exceed PLTE entry count.", PngConstants.ChunkTransparency, offset);
        }
    }

    internal static void ValidateIndexedColorType(List<PngError> errors, PngIhdr ihdr, long offset)
    {
        if (ihdr.ColorType != PngColorType.IndexedColor)
        {
            AddError(errors, "PNG_TRNS_INDEXED_COLOR_REQUIRED", "Indexed-color tRNS validation requires indexed-color IHDR.", PngConstants.ChunkTransparency, offset);
        }
    }

    internal static bool ValidateGrayscaleChunkShape(List<PngError> errors, PngChunk chunk)
    {
        bool canParse = true;
        ValidateChunkType(errors, chunk);
        if (chunk.Data.Length != PngTransparencyParser.GrayscaleDataLength)
        {
            AddError(errors, "PNG_TRNS_GRAYSCALE_LENGTH_INVALID", "Grayscale tRNS payload length must be exactly 2 bytes.", chunk.Type, chunk.Offset);
            canParse = false;
        }

        return canParse;
    }

    internal static void ValidateGrayscaleColorType(List<PngError> errors, PngIhdr ihdr, long offset)
    {
        if (ihdr.ColorType != PngColorType.Grayscale)
        {
            AddError(errors, "PNG_TRNS_GRAYSCALE_COLOR_REQUIRED", "Grayscale tRNS validation requires grayscale IHDR.", PngConstants.ChunkTransparency, offset);
        }
    }

    internal static void ValidateGrayscaleSampleRange(List<PngError> errors, PngGrayscaleTransparency transparency, PngIhdr ihdr, long offset)
    {
        int maximumSample = GetMaximumSampleValue(ihdr.BitDepth);
        if (maximumSample >= 0 && transparency.Gray > maximumSample)
        {
            AddError(errors, "PNG_TRNS_GRAYSCALE_SAMPLE_OUT_OF_RANGE", "Grayscale tRNS sample exceeds the IHDR bit depth.", PngConstants.ChunkTransparency, offset);
        }
    }

    internal static int GetMaximumSampleValue(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return 1;
            case PngBitDepth.Two:
                return 3;
            case PngBitDepth.Four:
                return 15;
            case PngBitDepth.Eight:
                return 255;
            case PngBitDepth.Sixteen:
                return 65535;
            default:
                return -1;
        }
    }

    internal static bool ValidateTruecolorChunkShape(List<PngError> errors, PngChunk chunk)
    {
        bool canParse = true;
        ValidateChunkType(errors, chunk);
        if (chunk.Data.Length != PngTransparencyParser.TruecolorDataLength)
        {
            AddError(errors, "PNG_TRNS_TRUECOLOR_LENGTH_INVALID", "Truecolor tRNS payload length must be exactly 6 bytes.", chunk.Type, chunk.Offset);
            canParse = false;
        }

        return canParse;
    }

    internal static void ValidateTruecolorColorType(List<PngError> errors, PngIhdr ihdr, long offset)
    {
        if (ihdr.ColorType != PngColorType.Truecolor)
        {
            AddError(errors, "PNG_TRNS_TRUECOLOR_COLOR_REQUIRED", "Truecolor tRNS validation requires truecolor IHDR.", PngConstants.ChunkTransparency, offset);
        }
    }

    internal static void ValidateTruecolorSampleRange(List<PngError> errors, PngTruecolorTransparency transparency, PngIhdr ihdr, long offset)
    {
        int maximumSample = GetMaximumSampleValue(ihdr.BitDepth);
        if (maximumSample >= 0 && (transparency.Red > maximumSample || transparency.Green > maximumSample || transparency.Blue > maximumSample))
        {
            AddError(errors, "PNG_TRNS_TRUECOLOR_SAMPLE_OUT_OF_RANGE", "Truecolor tRNS sample exceeds the IHDR bit depth.", PngConstants.ChunkTransparency, offset);
        }
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
        return result;
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
