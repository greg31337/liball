namespace gif;

internal class GifMedianCutColor
{
    internal byte Red;
    internal byte Green;
    internal byte Blue;
    internal int OriginalIndex;

    internal GifMedianCutColor(byte red, byte green, byte blue, int originalIndex)
    {
        Red = red;
        Green = green;
        Blue = blue;
        OriginalIndex = originalIndex;
    }
}

internal class GifMedianCutBox
{
    internal int Start;
    internal int Length;
    internal int RedMin;
    internal int RedMax;
    internal int GreenMin;
    internal int GreenMax;
    internal int BlueMin;
    internal int BlueMax;
    internal int FirstOriginalIndex;

    internal GifMedianCutBox()
    {
        Start = 0;
        Length = 0;
        RedMin = 0;
        RedMax = 0;
        GreenMin = 0;
        GreenMax = 0;
        BlueMin = 0;
        BlueMax = 0;
        FirstOriginalIndex = 0;
    }
}

internal class GifMedianCutSampleResult
{
    internal bool Success;
    internal GifMedianCutColor[] Colors;
    internal int Count;
    internal int UniqueColorCount;
    internal bool HasTransparency;
    internal int TransparentPixelCount;
    internal GifErrorDiagnostic Error;

    internal GifMedianCutSampleResult()
    {
        Success = false;
        Colors = new GifMedianCutColor[0];
        Count = 0;
        UniqueColorCount = 0;
        HasTransparency = false;
        TransparentPixelCount = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifMedianCutQuantizer : IGifQuantizationStrategy
{
    internal GifPaletteBuildResult QuantizeBitmap(GifBitmap bitmap)
    {
        return QuantizeBitmapWithOptions(bitmap, new GifQuantizeOptions());
    }

    public GifPaletteBuildResult QuantizeBitmapWithOptions(GifBitmap bitmap, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic validation = builder.ValidateBitmap(bitmap);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        return QuantizeRgbaPixelsWithOptions(bitmap.RgbaPixels, options);
    }

    internal GifPaletteBuildResult QuantizeRgbaPixels(byte[] rgbaPixels)
    {
        return QuantizeRgbaPixelsWithOptions(rgbaPixels, new GifQuantizeOptions());
    }

    public GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (rgbaPixels == null)
        {
            rgbaPixels = new byte[0];
        }

        if (rgbaPixels.Length % 4 != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for median-cut quantization.", new GifDiagnosticPosition(-1, -1, "MedianCut"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        GifPaletteBuildResult exact = TryBuildExactPalette(rgbaPixels, activeOptions);
        if (exact.Success)
        {
            return exact;
        }

        if (exact.Error.Code != GifErrorCode.InvalidColorTableSize)
        {
            return exact;
        }

        GifMedianCutSampleResult samples = CollectSamples(rgbaPixels, activeOptions);
        if (!samples.Success)
        {
            result.Error = samples.Error;
            return result;
        }

        int targetColorCount = activeOptions.MaxColors;
        if (samples.HasTransparency)
        {
            targetColorCount = targetColorCount - 1;
        }

        if (targetColorCount <= 0 && samples.Count > 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Median-cut quantization has no palette slots for opaque colors.", new GifDiagnosticPosition(-1, -1, "MedianCut"));
            return result;
        }

        GifColor[] colors = CreateMedianCutColors(samples.Colors, samples.Count, targetColorCount, activeOptions.PreservePaletteOrder);
        int outputColorCount = colors.Length;
        if (samples.HasTransparency)
        {
            outputColorCount = outputColorCount + 1;
        }

        GifColor[] paletteColors = new GifColor[outputColorCount];
        int outputIndex = 0;
        if (samples.HasTransparency)
        {
            paletteColors[0] = new GifColor(0, 0, 0);
            result.HasTransparency = true;
            result.TransparentColorIndex = 0;
            result.TransparentPixelCount = samples.TransparentPixelCount;
            outputIndex = 1;
        }

        int colorIndex = 0;
        while (colorIndex < colors.Length)
        {
            paletteColors[outputIndex] = colors[colorIndex];
            outputIndex = outputIndex + 1;
            colorIndex = colorIndex + 1;
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(new GifPalette(paletteColors));
        if (!compacted.Success)
        {
            result.Error = compacted.Error;
            return result;
        }

        result.Palette = compacted.Palette;
        result.UniqueColorCount = samples.UniqueColorCount;
        if (samples.HasTransparency)
        {
            result.UniqueColorCount = result.UniqueColorCount + 1;
        }

        result.Exact = false;
        result.Success = true;
        return result;
    }

    internal GifPaletteBuildResult TryBuildExactPalette(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteBuildResult exact = builder.BuildExactRgbaPaletteWithTransparency(rgbaPixels, options);
        if (!exact.Success)
        {
            return exact;
        }

        GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(exact.Palette);
        if (!compacted.Success)
        {
            exact.Success = false;
            exact.Error = compacted.Error;
            return exact;
        }

        exact.Palette = compacted.Palette;
        return exact;
    }

    internal GifMedianCutSampleResult CollectSamples(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifMedianCutSampleResult result = new GifMedianCutSampleResult();
        int pixelCount = rgbaPixels.Length / 4;
        int capacity = (pixelCount / options.SamplingStep) + 1;
        result.Colors = new GifMedianCutColor[capacity];
        int[] uniqueKeys = new int[pixelCount];
        int uniqueCount = 0;
        int pixelIndex = 0;
        int opaqueIndex = 0;
        while (pixelIndex < pixelCount)
        {
            int offset = pixelIndex * 4;
            GifPaletteBuilder builder = new GifPaletteBuilder();
            if (builder.IsTransparentPixel(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2], rgbaPixels[offset + 3], options))
            {
                result.HasTransparency = true;
                result.TransparentPixelCount = result.TransparentPixelCount + 1;
            }
            else
            {
                int key = builder.CreateRgbKey(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2]);
                if (!builder.ContainsKey(uniqueKeys, uniqueCount, key))
                {
                    uniqueKeys[uniqueCount] = key;
                    uniqueCount = uniqueCount + 1;
                }

                if (opaqueIndex % options.SamplingStep == 0)
                {
                    result.Colors[result.Count] = new GifMedianCutColor(rgbaPixels[offset], rgbaPixels[offset + 1], rgbaPixels[offset + 2], pixelIndex);
                    result.Count = result.Count + 1;
                }

                opaqueIndex = opaqueIndex + 1;
            }

            pixelIndex = pixelIndex + 1;
        }

        result.UniqueColorCount = uniqueCount;
        result.Success = true;
        return result;
    }

    internal GifColor[] CreateMedianCutColors(GifMedianCutColor[] colors, int colorCount, int targetColorCount, bool preservePaletteOrder)
    {
        if (colorCount <= 0 || targetColorCount <= 0)
        {
            return new GifColor[0];
        }

        if (targetColorCount > colorCount)
        {
            targetColorCount = colorCount;
        }

        GifMedianCutBox[] boxes = new GifMedianCutBox[targetColorCount];
        boxes[0] = new GifMedianCutBox();
        boxes[0].Start = 0;
        boxes[0].Length = colorCount;
        UpdateBoxBounds(boxes[0], colors);

        int boxCount = 1;
        while (boxCount < targetColorCount)
        {
            int boxIndex = FindBoxToSplit(boxes, boxCount);
            if (boxIndex < 0)
            {
                break;
            }

            GifMedianCutBox box = boxes[boxIndex];
            int channel = GetLongestChannel(box);
            SortColorsByChannel(colors, box.Start, box.Length, channel);

            int firstLength = box.Length / 2;
            int secondLength = box.Length - firstLength;
            box.Length = firstLength;
            UpdateBoxBounds(box, colors);

            GifMedianCutBox second = new GifMedianCutBox();
            second.Start = box.Start + firstLength;
            second.Length = secondLength;
            UpdateBoxBounds(second, colors);
            boxes[boxCount] = second;
            boxCount = boxCount + 1;
        }

        if (preservePaletteOrder)
        {
            SortBoxesByFirstOriginalIndex(boxes, boxCount);
        }

        GifColor[] palette = new GifColor[boxCount];
        int index = 0;
        while (index < boxCount)
        {
            palette[index] = CreateAverageColor(colors, boxes[index]);
            index = index + 1;
        }

        return palette;
    }

    internal int FindBoxToSplit(GifMedianCutBox[] boxes, int boxCount)
    {
        int bestIndex = -1;
        long bestScore = -1;
        int index = 0;
        while (index < boxCount)
        {
            if (boxes[index].Length > 1)
            {
                int range = GetLargestRange(boxes[index]);
                long score = (long)range * boxes[index].Length;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = index;
                }
            }

            index = index + 1;
        }

        return bestIndex;
    }

    internal void UpdateBoxBounds(GifMedianCutBox box, GifMedianCutColor[] colors)
    {
        GifMedianCutColor first = colors[box.Start];
        box.RedMin = first.Red;
        box.RedMax = first.Red;
        box.GreenMin = first.Green;
        box.GreenMax = first.Green;
        box.BlueMin = first.Blue;
        box.BlueMax = first.Blue;
        box.FirstOriginalIndex = first.OriginalIndex;

        int index = box.Start + 1;
        int end = box.Start + box.Length;
        while (index < end)
        {
            GifMedianCutColor color = colors[index];
            if (color.Red < box.RedMin)
            {
                box.RedMin = color.Red;
            }

            if (color.Red > box.RedMax)
            {
                box.RedMax = color.Red;
            }

            if (color.Green < box.GreenMin)
            {
                box.GreenMin = color.Green;
            }

            if (color.Green > box.GreenMax)
            {
                box.GreenMax = color.Green;
            }

            if (color.Blue < box.BlueMin)
            {
                box.BlueMin = color.Blue;
            }

            if (color.Blue > box.BlueMax)
            {
                box.BlueMax = color.Blue;
            }

            if (color.OriginalIndex < box.FirstOriginalIndex)
            {
                box.FirstOriginalIndex = color.OriginalIndex;
            }

            index = index + 1;
        }
    }

    internal int GetLargestRange(GifMedianCutBox box)
    {
        int redRange = box.RedMax - box.RedMin;
        int greenRange = box.GreenMax - box.GreenMin;
        int blueRange = box.BlueMax - box.BlueMin;
        if (redRange >= greenRange && redRange >= blueRange)
        {
            return redRange;
        }

        if (greenRange >= blueRange)
        {
            return greenRange;
        }

        return blueRange;
    }

    internal int GetLongestChannel(GifMedianCutBox box)
    {
        int redRange = box.RedMax - box.RedMin;
        int greenRange = box.GreenMax - box.GreenMin;
        int blueRange = box.BlueMax - box.BlueMin;
        if (redRange >= greenRange && redRange >= blueRange)
        {
            return 0;
        }

        if (greenRange >= blueRange)
        {
            return 1;
        }

        return 2;
    }

    internal void SortColorsByChannel(GifMedianCutColor[] colors, int start, int length, int channel)
    {
        QuickSortColors(colors, start, start + length - 1, channel);
    }

    internal void QuickSortColors(GifMedianCutColor[] colors, int low, int high, int channel)
    {
        int left = low;
        int right = high;
        GifMedianCutColor pivot = colors[low + ((high - low) / 2)];
        while (left <= right)
        {
            while (CompareColors(colors[left], pivot, channel) < 0)
            {
                left = left + 1;
            }

            while (CompareColors(colors[right], pivot, channel) > 0)
            {
                right = right - 1;
            }

            if (left <= right)
            {
                SwapColors(colors, left, right);
                left = left + 1;
                right = right - 1;
            }
        }

        if (low < right)
        {
            QuickSortColors(colors, low, right, channel);
        }

        if (left < high)
        {
            QuickSortColors(colors, left, high, channel);
        }
    }

    internal int CompareColors(GifMedianCutColor left, GifMedianCutColor right, int channel)
    {
        int channelDifference = GetColorChannel(left, channel) - GetColorChannel(right, channel);
        if (channelDifference != 0)
        {
            return channelDifference;
        }

        int redDifference = left.Red - right.Red;
        if (redDifference != 0)
        {
            return redDifference;
        }

        int greenDifference = left.Green - right.Green;
        if (greenDifference != 0)
        {
            return greenDifference;
        }

        int blueDifference = left.Blue - right.Blue;
        if (blueDifference != 0)
        {
            return blueDifference;
        }

        if (left.OriginalIndex < right.OriginalIndex)
        {
            return -1;
        }

        if (left.OriginalIndex > right.OriginalIndex)
        {
            return 1;
        }

        return 0;
    }

    internal int GetColorChannel(GifMedianCutColor color, int channel)
    {
        if (channel == 0)
        {
            return color.Red;
        }

        if (channel == 1)
        {
            return color.Green;
        }

        return color.Blue;
    }

    internal void SwapColors(GifMedianCutColor[] colors, int left, int right)
    {
        GifMedianCutColor temporary = colors[left];
        colors[left] = colors[right];
        colors[right] = temporary;
    }

    internal void SortBoxesByFirstOriginalIndex(GifMedianCutBox[] boxes, int boxCount)
    {
        int index = 1;
        while (index < boxCount)
        {
            GifMedianCutBox value = boxes[index];
            int insert = index - 1;
            while (insert >= 0 && boxes[insert].FirstOriginalIndex > value.FirstOriginalIndex)
            {
                boxes[insert + 1] = boxes[insert];
                insert = insert - 1;
            }

            boxes[insert + 1] = value;
            index = index + 1;
        }
    }

    internal GifColor CreateAverageColor(GifMedianCutColor[] colors, GifMedianCutBox box)
    {
        long redSum = 0;
        long greenSum = 0;
        long blueSum = 0;
        int index = box.Start;
        int end = box.Start + box.Length;
        while (index < end)
        {
            redSum = redSum + colors[index].Red;
            greenSum = greenSum + colors[index].Green;
            blueSum = blueSum + colors[index].Blue;
            index = index + 1;
        }

        byte red = (byte)((redSum + (box.Length / 2)) / box.Length);
        byte green = (byte)((greenSum + (box.Length / 2)) / box.Length);
        byte blue = (byte)((blueSum + (box.Length / 2)) / box.Length);
        return new GifColor(red, green, blue);
    }

    internal GifQuantizeOptions NormalizeOptions(GifQuantizeOptions options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }

    internal GifErrorDiagnostic ValidateOptions(GifQuantizeOptions options)
    {
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic validation = builder.ValidateOptions(options);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        if (options.SamplingStep <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Sampling step must be positive for median-cut quantization.", new GifDiagnosticPosition(-1, -1, "MedianCut"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}
