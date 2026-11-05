namespace gif;

internal class GifColorTableValidator
{
    internal bool IsValidColorCount(int colorCount)
    {
        if (colorCount < GifConstants.MinimumColorTableSize)
        {
            return false;
        }

        if (colorCount > GifConstants.MaximumColorTableSize)
        {
            return false;
        }

        return IsPowerOfTwo(colorCount);
    }

    internal bool IsPowerOfTwo(int value)
    {
        if (value <= 0)
        {
            return false;
        }

        return (value & (value - 1)) == 0;
    }

    internal bool IsValidPalette(GifPalette palette)
    {
        if (palette == null)
        {
            return false;
        }

        if (!IsValidColorCount(palette.Count))
        {
            return false;
        }

        if (palette.Colors == null)
        {
            return false;
        }

        if (palette.Count > palette.Colors.Length)
        {
            return false;
        }

        int index = 0;
        while (index < palette.Count)
        {
            if (palette.Colors[index] == null)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal GifErrorDiagnostic CreateInvalidColorTableSizeError(long byteOffset)
    {
        return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Color table size must be a power of two from 2 through 256.", new GifDiagnosticPosition(byteOffset, -1, "ColorTable"));
    }
}

internal class GifActiveColorTableResult
{
    internal bool Success;
    internal GifPalette Palette;
    internal bool UsedLocalColorTable;
    internal bool UsedGlobalColorTable;
    internal bool UsedDefaultGlobalColorTable;
    internal GifErrorDiagnostic Error;

    internal GifActiveColorTableResult()
    {
        Success = false;
        Palette = new GifPalette(null);
        UsedLocalColorTable = false;
        UsedGlobalColorTable = false;
        UsedDefaultGlobalColorTable = false;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifActiveColorTableResolver
{
    internal GifActiveColorTableResult Resolve(GifDocument document, int frameIndex)
    {
        return ResolveDocumentWithOptions(document, frameIndex, new GifDecodeOptions());
    }

    internal GifActiveColorTableResult ResolveDocumentWithOptions(GifDocument document, int frameIndex, GifDecodeOptions options)
    {
        GifActiveColorTableResult result = new GifActiveColorTableResult();
        if (document == null)
        {
            result.Error = CreateMissingError("GIF document is missing.", frameIndex);
            return result;
        }

        if (document.Frames == null)
        {
            result.Error = CreateMissingError("GIF document has no frames.", frameIndex);
            return result;
        }

        if (frameIndex < 0 || frameIndex >= document.Frames.Length)
        {
            result.Error = CreateMissingError("GIF frame index is outside the document frame list.", frameIndex);
            return result;
        }

        return ResolveFrameWithOptions(document.Frames[frameIndex], document.GlobalColorTable, options, frameIndex);
    }

    internal GifActiveColorTableResult ResolveFrame(GifFrame frame, GifPalette globalColorTable)
    {
        return ResolveFrameAtIndex(frame, globalColorTable, -1);
    }

    internal GifActiveColorTableResult ResolveFrameAtIndex(GifFrame frame, GifPalette globalColorTable, int frameIndex)
    {
        return ResolveFrameWithOptions(frame, globalColorTable, new GifDecodeOptions(), frameIndex);
    }

    internal GifActiveColorTableResult ResolveFrameWithOptions(GifFrame frame, GifPalette globalColorTable, GifDecodeOptions options, int frameIndex)
    {
        GifActiveColorTableResult result = new GifActiveColorTableResult();
        if (frame == null)
        {
            result.Error = CreateMissingError("GIF frame is missing.", frameIndex);
            return result;
        }

        if (frame.HasLocalColorTable)
        {
            return ResolveLocal(frame.LocalColorTable, frameIndex);
        }

        if (HasPaletteData(globalColorTable))
        {
            return ResolveGlobal(globalColorTable, frameIndex);
        }

        return ResolveDefaultGlobalColorTable(options, frameIndex);
    }

    internal GifActiveColorTableResult ResolveLocal(GifPalette localColorTable, int frameIndex)
    {
        GifActiveColorTableResult result = new GifActiveColorTableResult();
        GifErrorDiagnostic validation = ValidatePalette(localColorTable, "Local color table", frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        result.Success = true;
        result.Palette = localColorTable;
        result.UsedLocalColorTable = true;
        return result;
    }

    internal GifActiveColorTableResult ResolveGlobal(GifPalette globalColorTable, int frameIndex)
    {
        GifActiveColorTableResult result = new GifActiveColorTableResult();
        GifErrorDiagnostic validation = ValidatePalette(globalColorTable, "Global color table", frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        result.Success = true;
        result.Palette = globalColorTable;
        result.UsedGlobalColorTable = true;
        return result;
    }

    internal GifActiveColorTableResult ResolveDefaultGlobalColorTable(GifDecodeOptions options, int frameIndex)
    {
        GifActiveColorTableResult result = new GifActiveColorTableResult();
        if (options == null)
        {
            result.Error = CreateMissingError("Global color table is missing.", frameIndex);
            return result;
        }

        if (options.DefaultGlobalColorTable == null || options.DefaultGlobalColorTable.Length == 0)
        {
            result.Error = CreateMissingError("Global color table is missing.", frameIndex);
            return result;
        }

        GifColorTableReadResult colorTable = ParseDefaultGlobalColorTable(options.DefaultGlobalColorTable, frameIndex);
        if (!colorTable.Success)
        {
            result.Error = colorTable.Error;
            return result;
        }

        result.Success = true;
        result.Palette = colorTable.Palette;
        result.UsedGlobalColorTable = true;
        result.UsedDefaultGlobalColorTable = true;
        return result;
    }

    internal GifColorTableReadResult ParseDefaultGlobalColorTable(byte[] data, int frameIndex)
    {
        GifColorTableReadResult result = new GifColorTableReadResult();
        if (data == null)
        {
            result.Error = CreateMissingError("Default global color table is missing.", frameIndex);
            return result;
        }

        if (data.Length == 0)
        {
            result.Error = CreateMissingError("Default global color table is empty.", frameIndex);
            return result;
        }

        if ((data.Length % GifConstants.ColorTableEntrySize) != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Default global color table byte count must contain RGB triples.", new GifDiagnosticPosition(-1, frameIndex, "ColorTable"));
            return result;
        }

        int colorCount = data.Length / GifConstants.ColorTableEntrySize;
        GifColorTableReader reader = new GifColorTableReader();
        result = reader.ParseColorTable(data, colorCount, -1);
        if (!result.Success)
        {
            result.Error.Position.FrameIndex = frameIndex;
        }

        return result;
    }

    internal GifErrorDiagnostic ValidatePalette(GifPalette palette, string paletteName, int frameIndex)
    {
        if (palette == null)
        {
            return CreateMissingError(paletteName + " is missing.", frameIndex);
        }

        if (palette.IsEmpty())
        {
            return CreateMissingError(paletteName + " is empty.", frameIndex);
        }

        GifColorTableValidator validator = new GifColorTableValidator();
        if (!validator.IsValidPalette(palette))
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, paletteName + " is not a valid GIF color table.", new GifDiagnosticPosition(-1, frameIndex, "ColorTable"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal bool HasPaletteData(GifPalette palette)
    {
        if (palette == null)
        {
            return false;
        }

        return !palette.IsEmpty();
    }

    internal GifErrorDiagnostic CreateMissingError(string message, int frameIndex)
    {
        return new GifErrorDiagnostic(GifErrorCode.MissingColorTable, message, new GifDiagnosticPosition(-1, frameIndex, "ColorTable"));
    }
}
