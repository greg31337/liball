namespace gif;

internal class GifPaletteBuildResult
{
    internal bool Success;
    internal GifPalette Palette;
    internal int UniqueColorCount;
    internal bool Exact;
    internal bool HasTransparency;
    internal int TransparentColorIndex;
    internal int TransparentPixelCount;
    internal GifErrorDiagnostic Error;

    internal GifPaletteBuildResult()
    {
        Success = false;
        Palette = new GifPalette(null);
        UniqueColorCount = 0;
        Exact = false;
        HasTransparency = false;
        TransparentColorIndex = -1;
        TransparentPixelCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPaletteLookupResult
{
    internal bool Success;
    internal int ColorIndex;
    internal long DistanceSquared;
    internal GifColor Color;
    internal GifErrorDiagnostic Error;

    internal GifPaletteLookupResult()
    {
        Success = false;
        ColorIndex = -1;
        DistanceSquared = 0;
        Color = new GifColor(0, 0, 0);
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPaletteCompactionResult
{
    internal bool Success;
    internal GifPalette Palette;
    internal int OriginalColorCount;
    internal int LegalColorCount;
    internal int ColorTableSizeCode;
    internal bool AddedPadding;
    internal GifErrorDiagnostic Error;

    internal GifPaletteCompactionResult()
    {
        Success = false;
        Palette = new GifPalette(null);
        OriginalColorCount = 0;
        LegalColorCount = 0;
        ColorTableSizeCode = -1;
        AddedPadding = false;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLocalPaletteBuildResult
{
    internal bool Success;
    internal GifPaletteBuildResult[] FrameResults;
    internal GifPalette[] Palettes;
    internal int FrameCount;
    internal GifErrorDiagnostic Error;

    internal GifLocalPaletteBuildResult()
    {
        Success = false;
        FrameResults = new GifPaletteBuildResult[0];
        Palettes = new GifPalette[0];
        FrameCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPaletteReuseResult
{
    internal bool Success;
    internal bool CanReuse;
    internal int MatchedColorCount;
    internal int MissingColorCount;
    internal int FirstMissingColorIndex;
    internal GifErrorDiagnostic Error;

    internal GifPaletteReuseResult()
    {
        Success = false;
        CanReuse = false;
        MatchedColorCount = 0;
        MissingColorCount = 0;
        FirstMissingColorIndex = -1;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPaletteReusePlanResult
{
    internal bool Success;
    internal GifPalette GlobalPalette;
    internal GifPaletteBuildResult GlobalPaletteResult;
    internal GifLocalPaletteBuildResult LocalPaletteResult;
    internal bool[] ReuseGlobalPalette;
    internal bool[] UseLocalPalette;
    internal int ReusedGlobalPaletteCount;
    internal int LocalPaletteCount;
    internal GifErrorDiagnostic Error;

    internal GifPaletteReusePlanResult()
    {
        Success = false;
        GlobalPalette = new GifPalette(null);
        GlobalPaletteResult = new GifPaletteBuildResult();
        LocalPaletteResult = new GifLocalPaletteBuildResult();
        ReuseGlobalPalette = new bool[0];
        UseLocalPalette = new bool[0];
        ReusedGlobalPaletteCount = 0;
        LocalPaletteCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifPaletteBuilder
{
    internal GifPaletteReusePlanResult BuildPaletteReusePlan(GifAnimation animation)
    {
        return BuildPaletteReusePlanWithOptions(animation, new GifQuantizeOptions());
    }

    internal GifPaletteReusePlanResult BuildPaletteReusePlanWithOptions(GifAnimation animation, GifQuantizeOptions options)
    {
        GifPaletteReusePlanResult result = new GifPaletteReusePlanResult();
        GifPaletteBuildResult globalResult = BuildGlobalExactPaletteWithOptions(animation, options);
        result.GlobalPaletteResult = globalResult;
        if (!globalResult.Success)
        {
            result.Error = globalResult.Error;
            return result;
        }

        GifLocalPaletteBuildResult localResult = BuildLocalExactPalettesWithOptions(animation, options);
        result.LocalPaletteResult = localResult;
        if (!localResult.Success)
        {
            result.Error = localResult.Error;
            return result;
        }

        GifPaletteReusePlanResult plan = PlanGlobalPaletteReuseWithTransparency(globalResult.Palette, localResult, globalResult.TransparentColorIndex);
        plan.GlobalPaletteResult = globalResult;
        plan.LocalPaletteResult = localResult;
        return plan;
    }

    internal GifPaletteReusePlanResult PlanGlobalPaletteReuse(GifPalette globalPalette, GifLocalPaletteBuildResult localPalettes)
    {
        return PlanGlobalPaletteReuseWithTransparency(globalPalette, localPalettes, -1);
    }

    internal GifPaletteReusePlanResult PlanGlobalPaletteReuseWithTransparency(GifPalette globalPalette, GifLocalPaletteBuildResult localPalettes, int globalTransparentColorIndex)
    {
        GifPaletteReusePlanResult result = new GifPaletteReusePlanResult();
        GifErrorDiagnostic globalValidation = ValidateLookupPalette(globalPalette);
        if (globalValidation.Code != GifErrorCode.None)
        {
            result.Error = globalValidation;
            return result;
        }

        if (localPalettes == null || !localPalettes.Success || localPalettes.FrameResults == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Successful local palette results are required for palette reuse planning.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        if (globalTransparentColorIndex < -1 || globalTransparentColorIndex >= globalPalette.Count)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Global transparent color index is outside the palette for reuse planning.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        result.GlobalPalette = globalPalette;
        result.LocalPaletteResult = localPalettes;
        result.ReuseGlobalPalette = new bool[localPalettes.FrameResults.Length];
        result.UseLocalPalette = new bool[localPalettes.FrameResults.Length];
        int frameIndex = 0;
        while (frameIndex < localPalettes.FrameResults.Length)
        {
            GifPaletteBuildResult frameResult = localPalettes.FrameResults[frameIndex];
            if (frameResult == null || !frameResult.Success)
            {
                if (frameResult != null)
                {
                    result.Error = frameResult.Error;
                }
                else
                {
                    result.Error = new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Local palette result is missing for palette reuse planning.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
                }

                return result;
            }

            GifPaletteReuseResult reuse = CanReusePaletteWithTransparency(globalPalette, frameResult.Palette, globalTransparentColorIndex, frameResult.TransparentColorIndex);
            if (!reuse.Success)
            {
                result.Error = reuse.Error;
                return result;
            }

            if (reuse.CanReuse)
            {
                result.ReuseGlobalPalette[frameIndex] = true;
                result.ReusedGlobalPaletteCount = result.ReusedGlobalPaletteCount + 1;
            }
            else
            {
                result.UseLocalPalette[frameIndex] = true;
                result.LocalPaletteCount = result.LocalPaletteCount + 1;
            }

            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        return result;
    }

    internal GifPaletteReuseResult CanReusePalette(GifPalette candidatePalette, GifPalette requiredPalette)
    {
        return CanReusePaletteWithTransparency(candidatePalette, requiredPalette, -1, -1);
    }

    internal GifPaletteReuseResult CanReusePaletteWithTransparency(GifPalette candidatePalette, GifPalette requiredPalette, int candidateTransparentColorIndex, int requiredTransparentColorIndex)
    {
        GifPaletteReuseResult result = new GifPaletteReuseResult();
        GifErrorDiagnostic candidateValidation = ValidateLookupPalette(candidatePalette);
        if (candidateValidation.Code != GifErrorCode.None)
        {
            result.Error = candidateValidation;
            return result;
        }

        GifErrorDiagnostic requiredValidation = ValidateLookupPalette(requiredPalette);
        if (requiredValidation.Code != GifErrorCode.None)
        {
            result.Error = requiredValidation;
            return result;
        }

        if (candidateTransparentColorIndex < -1 || candidateTransparentColorIndex >= candidatePalette.Count)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Candidate transparent color index is outside the palette for reuse checking.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        if (requiredTransparentColorIndex < -1 || requiredTransparentColorIndex >= requiredPalette.Count)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Required transparent color index is outside the palette for reuse checking.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        if (requiredTransparentColorIndex >= 0 && candidateTransparentColorIndex < 0)
        {
            result.Success = true;
            result.CanReuse = false;
            result.MissingColorCount = 1;
            result.FirstMissingColorIndex = requiredTransparentColorIndex;
            return result;
        }

        int requiredIndex = 0;
        while (requiredIndex < requiredPalette.Count)
        {
            if (requiredIndex != requiredTransparentColorIndex)
            {
                if (ContainsPaletteColor(candidatePalette, requiredPalette.Colors[requiredIndex], candidateTransparentColorIndex))
                {
                    result.MatchedColorCount = result.MatchedColorCount + 1;
                }
                else
                {
                    result.MissingColorCount = result.MissingColorCount + 1;
                    if (result.FirstMissingColorIndex < 0)
                    {
                        result.FirstMissingColorIndex = requiredIndex;
                    }
                }
            }

            requiredIndex = requiredIndex + 1;
        }

        result.CanReuse = result.MissingColorCount == 0;
        result.Success = true;
        return result;
    }

    internal GifLocalPaletteBuildResult BuildLocalExactPalettes(GifAnimation animation)
    {
        return BuildLocalExactPalettesWithOptions(animation, new GifQuantizeOptions());
    }

    internal GifLocalPaletteBuildResult BuildLocalExactPalettesWithOptions(GifAnimation animation, GifQuantizeOptions options)
    {
        GifLocalPaletteBuildResult result = new GifLocalPaletteBuildResult();
        if (animation == null || animation.Frames == null || animation.Frames.Length <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation has no frames for local palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        result.FrameCount = animation.Frames.Length;
        result.FrameResults = new GifPaletteBuildResult[animation.Frames.Length];
        result.Palettes = new GifPalette[animation.Frames.Length];
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifPaletteBuildResult frameResult = BuildLocalExactPaletteForFrame(animation.Frames[frameIndex], activeOptions, frameIndex);
            result.FrameResults[frameIndex] = frameResult;
            if (!frameResult.Success)
            {
                result.Error = frameResult.Error;
                return result;
            }

            result.Palettes[frameIndex] = frameResult.Palette;
            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        return result;
    }

    internal GifPaletteBuildResult BuildLocalExactPaletteForFrame(GifAnimationFrame frame, GifQuantizeOptions options, int frameIndex)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        GifErrorDiagnostic frameValidation = ValidateAnimationFrameBitmap(frame, frameIndex);
        if (frameValidation.Code != GifErrorCode.None)
        {
            result.Error = frameValidation;
            return result;
        }

        GifBitmap bitmap = frame.Bitmap;
        int[] keys = new int[activeOptions.MaxColors];
        GifColor[] colors = new GifColor[activeOptions.MaxColors];
        int colorCount = 0;
        if (BitmapHasTransparentPixels(bitmap, activeOptions))
        {
            result.HasTransparency = true;
            result.TransparentColorIndex = 0;
            keys[0] = -1;
            colors[0] = new GifColor(0, 0, 0);
            colorCount = 1;
        }

        int offset = 0;
        while (offset < bitmap.RgbaPixels.Length)
        {
            if (IsTransparentPixel(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2], bitmap.RgbaPixels[offset + 3], activeOptions))
            {
                result.TransparentPixelCount = result.TransparentPixelCount + 1;
            }
            else
            {
                int key = CreateRgbKey(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2]);
                if (!ContainsKey(keys, colorCount, key))
                {
                    if (colorCount >= activeOptions.MaxColors)
                    {
                        result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Frame contains more unique RGB colors than the local palette limit.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
                        return result;
                    }

                    keys[colorCount] = key;
                    colors[colorCount] = new GifColor(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2]);
                    colorCount = colorCount + 1;
                }
            }

            offset = offset + 4;
        }

        GifPaletteCompactionResult compacted = CompactToLegalColorTableSize(new GifPalette(CopyColors(colors, colorCount)));
        if (!compacted.Success)
        {
            result.Error = compacted.Error;
            return result;
        }

        result.Palette = compacted.Palette;
        result.UniqueColorCount = colorCount;
        result.Exact = true;
        result.Success = true;
        return result;
    }

    internal GifPaletteBuildResult BuildGlobalExactPalette(GifAnimation animation)
    {
        return BuildGlobalExactPaletteWithOptions(animation, new GifQuantizeOptions());
    }

    internal GifPaletteBuildResult BuildGlobalExactPaletteWithOptions(GifAnimation animation, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (animation == null || animation.Frames == null || animation.Frames.Length <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation has no frames for global palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        bool hasTransparency = false;
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifErrorDiagnostic frameValidation = ValidateAnimationFrameBitmap(animation.Frames[frameIndex], frameIndex);
            if (frameValidation.Code != GifErrorCode.None)
            {
                result.Error = frameValidation;
                return result;
            }

            if (BitmapHasTransparentPixels(animation.Frames[frameIndex].Bitmap, activeOptions))
            {
                hasTransparency = true;
            }

            frameIndex = frameIndex + 1;
        }

        int[] keys = new int[activeOptions.MaxColors];
        GifColor[] colors = new GifColor[activeOptions.MaxColors];
        int colorCount = 0;
        if (hasTransparency)
        {
            result.HasTransparency = true;
            result.TransparentColorIndex = 0;
            keys[0] = -1;
            colors[0] = new GifColor(0, 0, 0);
            colorCount = 1;
        }

        frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifBitmap bitmap = animation.Frames[frameIndex].Bitmap;
            int offset = 0;
            while (offset < bitmap.RgbaPixels.Length)
            {
                if (IsTransparentPixel(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2], bitmap.RgbaPixels[offset + 3], activeOptions))
                {
                    result.TransparentPixelCount = result.TransparentPixelCount + 1;
                }
                else
                {
                    int key = CreateRgbKey(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2]);
                    if (!ContainsKey(keys, colorCount, key))
                    {
                        if (colorCount >= activeOptions.MaxColors)
                        {
                            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Animation contains more unique RGB colors than the global palette limit.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
                            return result;
                        }

                        keys[colorCount] = key;
                        colors[colorCount] = new GifColor(bitmap.RgbaPixels[offset], bitmap.RgbaPixels[offset + 1], bitmap.RgbaPixels[offset + 2]);
                        colorCount = colorCount + 1;
                    }
                }

                offset = offset + 4;
            }

            frameIndex = frameIndex + 1;
        }

        GifPaletteCompactionResult compacted = CompactToLegalColorTableSize(new GifPalette(CopyColors(colors, colorCount)));
        if (!compacted.Success)
        {
            result.Error = compacted.Error;
            return result;
        }

        result.Palette = compacted.Palette;
        result.UniqueColorCount = colorCount;
        result.Exact = true;
        result.Success = true;
        return result;
    }

    internal GifPaletteCompactionResult CompactToLegalColorTableSize(GifPalette palette)
    {
        GifPaletteCompactionResult result = new GifPaletteCompactionResult();
        GifErrorDiagnostic validation = ValidateCompactionPalette(palette);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        int legalColorCount = CalculateLegalColorTableSize(palette.Count);
        GifColor[] colors = new GifColor[legalColorCount];
        int index = 0;
        while (index < palette.Count)
        {
            colors[index] = CopyColor(palette.Colors[index]);
            index = index + 1;
        }

        GifColor paddingColor = CopyColor(palette.Colors[palette.Count - 1]);
        while (index < legalColorCount)
        {
            colors[index] = CopyColor(paddingColor);
            index = index + 1;
        }

        GifPackedFields packedFields = new GifPackedFields();
        result.Palette = new GifPalette(colors);
        result.OriginalColorCount = palette.Count;
        result.LegalColorCount = legalColorCount;
        result.ColorTableSizeCode = packedFields.ComputeColorTableSizeCode(legalColorCount);
        result.AddedPadding = legalColorCount != palette.Count;
        result.Success = true;
        return result;
    }

    internal GifPaletteLookupResult FindNearestPaletteColor(GifPalette palette, GifColor color)
    {
        if (color == null)
        {
            GifPaletteLookupResult result = new GifPaletteLookupResult();
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Target color is missing for nearest-color palette lookup.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        return FindNearestRgbColor(palette, color.Red, color.Green, color.Blue);
    }

    internal GifPaletteLookupResult FindNearestRgbColor(GifPalette palette, byte red, byte green, byte blue)
    {
        return FindNearestRgbColorExcludingIndex(palette, red, green, blue, -1);
    }

    internal GifPaletteLookupResult FindNearestRgbColorExcludingIndex(GifPalette palette, byte red, byte green, byte blue, int excludedIndex)
    {
        GifPaletteLookupResult result = new GifPaletteLookupResult();
        GifErrorDiagnostic validation = ValidateLookupPalette(palette);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        long bestDistance = 0;
        int bestIndex = -1;
        int colorIndex = 0;
        while (colorIndex < palette.Count)
        {
            if (colorIndex != excludedIndex)
            {
                long distance = CalculateRgbDistanceSquared(red, green, blue, palette.Colors[colorIndex]);
                if (bestIndex < 0 || distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = colorIndex;
                }
            }

            colorIndex = colorIndex + 1;
        }

        if (bestIndex < 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Palette has no usable colors for nearest-color lookup.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        result.ColorIndex = bestIndex;
        result.Color = palette.Colors[bestIndex];
        result.DistanceSquared = bestDistance;
        result.Success = true;
        return result;
    }

    internal GifPaletteBuildResult BuildExactBitmapPalette(GifBitmap bitmap)
    {
        return BuildExactBitmapPaletteWithOptions(bitmap, new GifQuantizeOptions());
    }

    internal GifPaletteBuildResult BuildExactBitmapPaletteWithOptions(GifBitmap bitmap, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifErrorDiagnostic validation = ValidateBitmap(bitmap);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        return BuildExactRgbaPaletteWithOptions(bitmap.RgbaPixels, options);
    }

    internal GifPaletteBuildResult BuildExactRgbaPalette(byte[] rgbaPixels)
    {
        return BuildExactRgbaPaletteWithOptions(rgbaPixels, new GifQuantizeOptions());
    }

    internal GifPaletteBuildResult BuildExactRgbaPaletteWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (rgbaPixels == null)
        {
            rgbaPixels = new byte[0];
        }

        if (rgbaPixels.Length % 4 != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for exact palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.MaxColors <= 0 || activeOptions.MaxColors > GifConstants.MaximumColorTableSize)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Maximum exact palette size must be from 1 through 256.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        int[] keys = new int[activeOptions.MaxColors];
        GifColor[] colors = new GifColor[activeOptions.MaxColors];
        int colorCount = 0;
        int offset = 0;
        while (offset < rgbaPixels.Length)
        {
            int key = CreateRgbKey(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2]);
            if (!ContainsKey(keys, colorCount, key))
            {
                if (colorCount >= activeOptions.MaxColors)
                {
                    result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Input contains more unique RGB colors than the exact palette limit.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
                    return result;
                }

                keys[colorCount] = key;
                colors[colorCount] = new GifColor(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2]);
                colorCount = colorCount + 1;
            }

            offset = offset + 4;
        }

        result.Palette = new GifPalette(CopyColors(colors, colorCount));
        result.UniqueColorCount = colorCount;
        result.Exact = true;
        result.Success = true;
        return result;
    }

    internal GifPaletteBuildResult BuildExactBitmapPaletteWithTransparency(GifBitmap bitmap, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifErrorDiagnostic validation = ValidateBitmap(bitmap);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        return BuildExactRgbaPaletteWithTransparency(bitmap.RgbaPixels, options);
    }

    internal GifPaletteBuildResult BuildExactRgbaPaletteWithTransparency(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (rgbaPixels == null)
        {
            rgbaPixels = new byte[0];
        }

        if (rgbaPixels.Length % 4 != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for transparent exact palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        int[] keys = new int[activeOptions.MaxColors];
        GifColor[] colors = new GifColor[activeOptions.MaxColors];
        int colorCount = 0;
        if (HasTransparentPixels(rgbaPixels, activeOptions))
        {
            result.HasTransparency = true;
            result.TransparentColorIndex = 0;
            keys[0] = -1;
            colors[0] = new GifColor(0, 0, 0);
            colorCount = 1;
        }

        int offset = 0;
        while (offset < rgbaPixels.Length)
        {
            if (IsTransparentPixel(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2], rgbaPixels[offset + 3], activeOptions))
            {
                result.TransparentPixelCount = result.TransparentPixelCount + 1;
            }
            else
            {
                int key = CreateRgbKey(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2]);
                if (!ContainsKey(keys, colorCount, key))
                {
                    if (colorCount >= activeOptions.MaxColors)
                    {
                        result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Input contains more unique RGB colors than the transparent exact palette limit.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
                        return result;
                    }

                    keys[colorCount] = key;
                    colors[colorCount] = new GifColor(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2]);
                    colorCount = colorCount + 1;
                }
            }

            offset = offset + 4;
        }

        result.Palette = new GifPalette(CopyColors(colors, colorCount));
        result.UniqueColorCount = colorCount;
        result.Exact = true;
        result.Success = true;
        return result;
    }

    internal int CreateRgbKey(byte red, byte green, byte blue)
    {
        return ((int)red << 16) | ((int)green << 8) | blue;
    }

    internal bool IsTransparentAlpha(byte alpha, GifQuantizeOptions options)
    {
        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.TransparentColorMode == GifTransparentColorMode.None)
        {
            return false;
        }

        if (activeOptions.TransparentAlphaThreshold < 0)
        {
            return false;
        }

        return alpha <= activeOptions.TransparentAlphaThreshold;
    }

    internal bool IsTransparentPixel(byte red, byte green, byte blue, byte alpha, GifQuantizeOptions options)
    {
        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.TransparentColorMode == GifTransparentColorMode.None)
        {
            return false;
        }

        if (activeOptions.TransparentColorMode == GifTransparentColorMode.ExactColor)
        {
            GifRgbaColor exactColor = activeOptions.ExactTransparentColor;
            if (exactColor == null)
            {
                return false;
            }

            return red == exactColor.Red && green == exactColor.Green && blue == exactColor.Blue && alpha == exactColor.Alpha;
        }

        return IsTransparentAlpha(alpha, activeOptions);
    }

    internal bool HasTransparentPixels(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        if (rgbaPixels == null)
        {
            return false;
        }

        int offset = 0;
        while (offset < rgbaPixels.Length)
        {
            if (IsTransparentPixel(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2], rgbaPixels[offset + 3], options))
            {
                return true;
            }

            offset = offset + 4;
        }

        return false;
    }

    internal bool BitmapHasTransparentPixels(GifBitmap bitmap, GifQuantizeOptions options)
    {
        if (bitmap == null || bitmap.RgbaPixels == null)
        {
            return false;
        }

        return HasTransparentPixels(bitmap.RgbaPixels, options);
    }

    internal long CalculateRgbDistanceSquared(byte red, byte green, byte blue, GifColor color)
    {
        int redDelta = (int)red - color.Red;
        int greenDelta = (int)green - color.Green;
        int blueDelta = (int)blue - color.Blue;
        return ((long)redDelta * redDelta) + ((long)greenDelta * greenDelta) + ((long)blueDelta * blueDelta);
    }

    internal GifErrorDiagnostic ValidateLookupPalette(GifPalette palette)
    {
        if (palette == null || palette.Colors == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Palette is missing for nearest-color lookup.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (palette.Count <= 0 || palette.Count > GifConstants.MaximumColorTableSize || palette.Count > palette.Colors.Length)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Palette color count is invalid for nearest-color lookup.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        int index = 0;
        while (index < palette.Count)
        {
            if (palette.Colors[index] == null)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Palette contains a missing color for nearest-color lookup.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateCompactionPalette(GifPalette palette)
    {
        if (palette == null || palette.Colors == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Palette is missing for legal color-table compaction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (palette.Count <= 0 || palette.Count > GifConstants.MaximumColorTableSize || palette.Count > palette.Colors.Length)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Palette color count is invalid for legal color-table compaction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        int index = 0;
        while (index < palette.Count)
        {
            if (palette.Colors[index] == null)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Palette contains a missing color for legal color-table compaction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal int CalculateLegalColorTableSize(int colorCount)
    {
        int legalColorCount = GifConstants.MinimumColorTableSize;
        while (legalColorCount < colorCount && legalColorCount < GifConstants.MaximumColorTableSize)
        {
            legalColorCount = legalColorCount * 2;
        }

        return legalColorCount;
    }

    internal GifColor CopyColor(GifColor color)
    {
        return new GifColor(color.Red, color.Green, color.Blue);
    }

    internal GifErrorDiagnostic ValidateBitmap(GifBitmap bitmap)
    {
        if (bitmap == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF bitmap is missing for exact palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF bitmap dimensions must be positive for exact palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        long pixelCount = (long)bitmap.Width * (long)bitmap.Height;
        if (pixelCount > 536870911)
        {
            return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "GIF bitmap is too large for exact palette construction.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        long expectedLength = pixelCount * 4;

        if (bitmap.RgbaPixels == null || bitmap.RgbaPixels.Length != (int)expectedLength)
        {
            return new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "GIF bitmap RGBA byte count does not match its dimensions.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateAnimationFrameBitmap(GifAnimationFrame frame, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for global palette construction.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
        }

        if (frame.Bitmap == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame bitmap is missing for global palette construction.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
        }

        if (frame.Bitmap.Width <= 0 || frame.Bitmap.Height <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame bitmap dimensions must be positive for global palette construction.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
        }

        long pixelCount = (long)frame.Bitmap.Width * (long)frame.Bitmap.Height;
        if (pixelCount > 536870911)
        {
            return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "GIF animation frame bitmap is too large for global palette construction.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
        }

        long expectedLength = pixelCount * 4;

        if (frame.Bitmap.RgbaPixels == null || frame.Bitmap.RgbaPixels.Length != (int)expectedLength)
        {
            return new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "GIF animation frame RGBA byte count does not match its dimensions.", new GifDiagnosticPosition(-1, frameIndex, "PaletteBuilder"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateOptions(GifQuantizeOptions options)
    {
        if (options == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Quantization options are missing.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.MaxColors <= 0 || options.MaxColors > GifConstants.MaximumColorTableSize)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Maximum exact palette size must be from 1 through 256.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.TransparentAlphaThreshold < 0 || options.TransparentAlphaThreshold > 255)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Transparent alpha threshold must be from 0 through 255.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.TransparentColorMode == GifTransparentColorMode.Preserve)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Preserve transparency mode is incompatible with RGBA quantization.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.TransparentColorMode != GifTransparentColorMode.None && options.TransparentColorMode != GifTransparentColorMode.AlphaThreshold && options.TransparentColorMode != GifTransparentColorMode.ExactColor)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Transparent color mode is invalid for RGBA quantization.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.TransparentColorMode == GifTransparentColorMode.ExactColor && options.ExactTransparentColor == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Exact transparent RGBA color is missing.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.TransparentColorIndex < -1 || options.TransparentColorIndex >= GifConstants.MaximumColorTableSize)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the GIF palette range.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.QuantizerMode != GifQuantizerMode.CallerPalette && options.CallerPalette != null && options.CallerPalette.Count > 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "A caller palette requires caller-palette quantization mode.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.QuantizerMode != GifQuantizerMode.CallerPalette && options.TransparentColorIndex >= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "A fixed transparent color index requires caller-palette quantization mode.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
        }

        if (options.QuantizerMode == GifQuantizerMode.CallerPalette)
        {
            if (options.CallerPalette == null)
            {
                return new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Caller palette is missing for caller-palette quantization.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }

            if (options.CallerPalette.Colors == null || options.CallerPalette.Count <= 0)
            {
                return new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "Caller palette has no colors for caller-palette quantization.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }

            GifErrorDiagnostic paletteValidation = ValidateLookupPalette(options.CallerPalette);
            if (paletteValidation.Code != GifErrorCode.None)
            {
                return paletteValidation;
            }

            if (options.CallerPalette.Count > options.MaxColors)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Caller palette exceeds the configured maximum color count.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }

            if (options.TransparentColorMode == GifTransparentColorMode.None && options.TransparentColorIndex >= 0)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index cannot be set when transparency is disabled.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }

            if (options.TransparentColorIndex >= options.CallerPalette.Count)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the caller palette.", new GifDiagnosticPosition(-1, -1, "PaletteBuilder"));
            }
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal bool ContainsKey(int[] keys, int count, int key)
    {
        int index = 0;
        while (index < count)
        {
            if (keys[index] == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsPaletteColor(GifPalette palette, GifColor color, int excludedIndex)
    {
        int index = 0;
        while (index < palette.Count)
        {
            if (index != excludedIndex)
            {
                GifColor candidate = palette.Colors[index];
                if (candidate.Red == color.Red && candidate.Green == color.Green && candidate.Blue == color.Blue)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal GifColor[] CopyColors(GifColor[] colors, int count)
    {
        GifColor[] result = new GifColor[count];
        int index = 0;
        while (index < count)
        {
            result[index] = colors[index];
            index = index + 1;
        }

        return result;
    }

    internal GifQuantizeOptions NormalizeOptions(GifQuantizeOptions options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }
}
