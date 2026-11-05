using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngPlteParser
{
    internal const int BytesPerEntry = 3;

    internal static PngPalette Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngPalette Parse(byte[] data, int offset, int length)
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

        if (length % BytesPerEntry != 0)
        {
            throw new InvalidDataException("PLTE data length must be a multiple of 3 bytes.");
        }

        int entryCount = length / BytesPerEntry;
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[entryCount];
        int entryIndex = 0;
        int dataIndex = offset;
        while (entryIndex < entryCount)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = data[dataIndex];
            entry.Green = data[dataIndex + 1];
            entry.Blue = data[dataIndex + 2];
            palette.Entries[entryIndex] = entry;
            dataIndex += BytesPerEntry;
            entryIndex += 1;
        }

        return palette;
    }

    internal static PngPalette Parse(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkPLTE)
        {
            throw new InvalidDataException("Chunk type must be PLTE.");
        }

        return Parse(chunk.Data);
    }
}

internal class PngPlteWriter
{
    internal static byte[] WriteData(PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        byte[] data = new byte[palette.Entries.Length * PngPlteParser.BytesPerEntry];
        WriteData(data, 0, palette);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        int length = palette.Entries.Length * PngPlteParser.BytesPerEntry;
        if (offset < 0 || offset > data.Length - length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        int entryIndex = 0;
        int dataIndex = offset;
        while (entryIndex < palette.Entries.Length)
        {
            PngPaletteEntry entry = palette.Entries[entryIndex];
            ArgumentNullException.ThrowIfNull(entry);
            data[dataIndex] = entry.Red;
            data[dataIndex + 1] = entry.Green;
            data[dataIndex + 2] = entry.Blue;
            dataIndex += PngPlteParser.BytesPerEntry;
            entryIndex += 1;
        }
    }

    internal static PngChunk WriteChunk(PngPalette palette)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkPLTE;
        chunk.Data = WriteData(palette);
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = true;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }
}

internal class PngPlteValidator
{
    internal const int MaximumEntryCount = 256;
    internal const int MaximumPayloadLength = 768;

    internal static bool IsValid(PngPalette palette)
    {
        return Validate(palette).IsValid;
    }

    internal static bool IsValid(PngPalette palette, PngIhdr ihdr)
    {
        return Validate(palette, ihdr).IsValid;
    }

    internal static PngValidationResult Validate(PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        ValidateEntries(errors, palette, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult Validate(PngPalette palette, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidatePresence(errors, true, ihdr, 0);
        ValidateEntries(errors, palette, 0);
        ValidateIndexedEntryLimit(errors, palette, ihdr, 0);
        return FinishResult(result, errors);
    }

    internal static PngValidationResult Validate(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        bool canParse = ValidateChunkShape(errors, chunk);
        if (canParse)
        {
            PngPalette palette = PngPlteParser.Parse(chunk.Data);
            ValidateEntries(errors, palette, chunk.Offset);
        }

        return FinishResult(result, errors);
    }

    internal static PngValidationResult Validate(PngChunk chunk, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidatePresence(errors, true, ihdr, chunk.Offset);
        bool canParse = ValidateChunkShape(errors, chunk);
        if (canParse)
        {
            PngPalette palette = PngPlteParser.Parse(chunk.Data);
            ValidateEntries(errors, palette, chunk.Offset);
            ValidateIndexedEntryLimit(errors, palette, ihdr, chunk.Offset);
        }

        return FinishResult(result, errors);
    }

    internal static PngValidationResult ValidatePresence(bool hasPlte, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(ihdr);

        PngValidationResult result = CreateBaseResult(ihdr);
        List<PngError> errors = new List<PngError>();
        ValidatePresence(errors, hasPlte, ihdr, 0);
        return FinishResult(result, errors);
    }

    internal static bool ValidateChunkShape(List<PngError> errors, PngChunk chunk)
    {
        bool canParse = true;
        if (chunk.Type != PngConstants.ChunkPLTE)
        {
            AddError(errors, "PNG_PLTE_CHUNK_TYPE_INVALID", "PLTE validation requires a PLTE chunk.", chunk.Type, chunk.Offset);
        }

        if (chunk.Data.Length == 0 || chunk.Data.Length > MaximumPayloadLength || chunk.Data.Length % PngPlteParser.BytesPerEntry != 0)
        {
            AddError(errors, "PNG_PLTE_PAYLOAD_LENGTH_INVALID", "PLTE payload length must be 3 to 768 bytes and a multiple of 3.", chunk.Type, chunk.Offset);
        }

        if (chunk.Data.Length % PngPlteParser.BytesPerEntry != 0)
        {
            canParse = false;
        }

        return canParse;
    }

    internal static void ValidatePresence(List<PngError> errors, bool hasPlte, PngIhdr ihdr, long offset)
    {
        if (ihdr.ColorType == PngColorType.IndexedColor && !hasPlte)
        {
            AddError(errors, "PNG_PLTE_REQUIRED", "PLTE is required for indexed-color PNG images.", PngConstants.ChunkPLTE, offset);
        }

        if (hasPlte && IsPlteDisallowed(ihdr.ColorType))
        {
            AddError(errors, "PNG_PLTE_DISALLOWED", "PLTE is not allowed for grayscale PNG images.", PngConstants.ChunkPLTE, offset);
        }
    }

    internal static void ValidateEntries(List<PngError> errors, PngPalette palette, long offset)
    {
        if (palette.Entries.Length == 0 || palette.Entries.Length > MaximumEntryCount)
        {
            AddError(errors, "PNG_PLTE_ENTRY_COUNT_INVALID", "PLTE entry count must be 1 through 256.", PngConstants.ChunkPLTE, offset);
        }

        int index = 0;
        while (index < palette.Entries.Length)
        {
            if (palette.Entries[index] == null)
            {
                AddError(errors, "PNG_PLTE_ENTRY_NULL", "PLTE entries must not be null.", PngConstants.ChunkPLTE, offset);
            }

            index += 1;
        }
    }

    internal static void ValidateIndexedEntryLimit(List<PngError> errors, PngPalette palette, PngIhdr ihdr, long offset)
    {
        if (ihdr.ColorType != PngColorType.IndexedColor)
        {
            return;
        }

        int maximumEntries = GetMaximumIndexedEntries(ihdr.BitDepth);
        if (maximumEntries > 0 && palette.Entries.Length > maximumEntries)
        {
            AddError(errors, "PNG_PLTE_ENTRY_COUNT_EXCEEDS_BIT_DEPTH", "PLTE entry count exceeds the indexed-color bit depth.", PngConstants.ChunkPLTE, offset);
        }
    }

    internal static bool IsPlteDisallowed(PngColorType colorType)
    {
        return colorType == PngColorType.Grayscale || colorType == PngColorType.GrayscaleAlpha;
    }

    internal static int GetMaximumIndexedEntries(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return 2;
            case PngBitDepth.Two:
                return 4;
            case PngBitDepth.Four:
                return 16;
            case PngBitDepth.Eight:
                return 256;
            default:
                return 0;
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
