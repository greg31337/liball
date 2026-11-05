namespace gif;

internal class GifRawFrameEncodeResult
{
    internal bool Success;
    internal byte[] CompressedData;
    internal int LzwMinimumCodeSize;
    internal GifErrorDiagnostic Error;

    internal GifRawFrameEncodeResult()
    {
        Success = false;
        CompressedData = new byte[0];
        LzwMinimumCodeSize = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifRawFrameEncoder
{
    internal GifRawFrameEncodeResult EncodeFrame(GifFrame frame)
    {
        return EncodeFrameWithOptions(frame, new GifEncodeOptions());
    }

    internal GifRawFrameEncodeResult EncodeFrameWithOptions(GifFrame frame, GifEncodeOptions options)
    {
        GifRawFrameEncodeResult result = new GifRawFrameEncodeResult();
        if (frame == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Frame is missing for raw indexed encode.", new GifDiagnosticPosition(-1, -1, "RawFrameEncode"));
            return result;
        }

        int minimumCodeSize = frame.LzwMinimumCodeSize;
        if (minimumCodeSize == 0)
        {
            minimumCodeSize = CalculateMinimumCodeSize(frame.IndexedPixels);
        }

        result = EncodeIndexedPixelsWithOptions(frame.IndexedPixels, minimumCodeSize, frame.Width, frame.Height, frame.Interlace, options);
        if (result.Success)
        {
            frame.LzwMinimumCodeSize = result.LzwMinimumCodeSize;
            frame.CompressedImageData = result.CompressedData;
        }

        return result;
    }

    internal GifRawFrameEncodeResult EncodeIndexedPixels(byte[] rowMajorPixels, int minimumCodeSize, int width, int height, bool interlace)
    {
        return EncodeIndexedPixelsWithOptions(rowMajorPixels, minimumCodeSize, width, height, interlace, new GifEncodeOptions());
    }

    internal GifRawFrameEncodeResult EncodeIndexedPixelsWithOptions(byte[] rowMajorPixels, int minimumCodeSize, int width, int height, bool interlace, GifEncodeOptions options)
    {
        GifRawFrameEncodeResult result = ValidateInputs(rowMajorPixels, minimumCodeSize, width, height);
        if (!result.Success)
        {
            return result;
        }

        GifEncodeOptions activeOptions = NormalizeOptions(options);
        GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
        GifInterlaceWriteResult preparedPixels = mapper.PrepareIndexedPixelsForWrite(rowMajorPixels, width, height, interlace);
        if (!preparedPixels.Success)
        {
            result.Error = preparedPixels.Error;
            return result;
        }

        GifLzwEncoder encoder = new GifLzwEncoder();
        encoder.Configure(activeOptions);
        GifLzwCodeWriteResult initialize = encoder.Initialize(minimumCodeSize);
        if (!initialize.Success)
        {
            result.Error = initialize.Error;
            return result;
        }

        GifLzwCodeWriteResult clear = encoder.WriteClearCode();
        if (!clear.Success)
        {
            result.Error = clear.Error;
            return result;
        }

        int prefixCode = encoder.Dictionary.FindRoot(preparedPixels.IndexedPixels[0]);
        int currentCodeSize = minimumCodeSize + 1;
        int pendingNextCode = -1;
        bool hasPendingCodeSizeGrowth = false;
        int index = 1;
        while (index < preparedPixels.IndexedPixels.Length)
        {
            byte suffixByte = preparedPixels.IndexedPixels[index];
            int existingCode = encoder.Dictionary.Find(prefixCode, suffixByte);
            if (existingCode >= 0)
            {
                prefixCode = existingCode;
                index = index + 1;
                continue;
            }

            GifLzwCodeWriteResult writePrefix = encoder.Writer.WriteCode(prefixCode, currentCodeSize);
            if (!writePrefix.Success)
            {
                result.Error = writePrefix.Error;
                return result;
            }

            currentCodeSize = ApplyPendingCodeSizeGrowth(currentCodeSize, pendingNextCode, hasPendingCodeSizeGrowth);
            encoder.CurrentCodeSize = currentCodeSize;
            hasPendingCodeSizeGrowth = false;
            pendingNextCode = -1;

            if (ShouldClearBeforeAddingEntry(encoder, currentCodeSize))
            {
                GifLzwCodeWriteResult clearCode = encoder.WriteClearCode();
                if (!clearCode.Success)
                {
                    result.Error = clearCode.Error;
                    return result;
                }

                currentCodeSize = minimumCodeSize + 1;
                prefixCode = encoder.Dictionary.FindRoot(suffixByte);
                if (prefixCode < 0)
                {
                    result.Error = CreatePaletteIndexError(suffixByte, minimumCodeSize);
                    return result;
                }

                index = index + 1;
                continue;
            }

            GifLzwDictionaryResult addResult = encoder.Dictionary.Add(prefixCode, suffixByte);
            if (!addResult.Success)
            {
                result.Error = addResult.Error;
                return result;
            }

            pendingNextCode = encoder.Dictionary.NextCode;
            hasPendingCodeSizeGrowth = true;
            prefixCode = encoder.Dictionary.FindRoot(suffixByte);
            if (prefixCode < 0)
            {
                result.Error = CreatePaletteIndexError(suffixByte, minimumCodeSize);
                return result;
            }

            index = index + 1;
        }

        GifLzwCodeWriteResult finalCode = encoder.Writer.WriteCode(prefixCode, currentCodeSize);
        if (!finalCode.Success)
        {
            result.Error = finalCode.Error;
            return result;
        }

        currentCodeSize = ApplyPendingCodeSizeGrowth(currentCodeSize, pendingNextCode, hasPendingCodeSizeGrowth);
        encoder.CurrentCodeSize = currentCodeSize;
        GifLzwCodeWriteResult end = encoder.WriteEndCode();
        if (!end.Success)
        {
            result.Error = end.Error;
            return result;
        }

        result.CompressedData = encoder.ToArray();
        result.LzwMinimumCodeSize = minimumCodeSize;
        if (result.CompressedData.Length > activeOptions.MaxBytes)
        {
            result.Success = false;
            result.CompressedData = new byte[0];
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Raw indexed frame compressed data exceeds the configured byte limit.", new GifDiagnosticPosition(-1, -1, "RawFrameEncode"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal int ApplyPendingCodeSizeGrowth(int currentCodeSize, int pendingNextCode, bool hasPendingCodeSizeGrowth)
    {
        if (!hasPendingCodeSizeGrowth)
        {
            return currentCodeSize;
        }

        if (currentCodeSize >= GifConstants.MaximumLzwCodeSize)
        {
            return currentCodeSize;
        }

        if (pendingNextCode >= (1 << currentCodeSize))
        {
            return currentCodeSize + 1;
        }

        return currentCodeSize;
    }

    internal bool ShouldClearBeforeAddingEntry(GifLzwEncoder encoder, int currentCodeSize)
    {
        if (encoder == null)
        {
            return false;
        }

        if (encoder.Dictionary.IsFull())
        {
            return true;
        }

        if (encoder.ClearPolicy != GifLzwClearPolicy.EarlyClear)
        {
            return false;
        }

        if (currentCodeSize >= GifConstants.MaximumLzwCodeSize)
        {
            return encoder.Dictionary.NextCode >= GifConstants.MaximumLzwTableSize - 1;
        }

        return encoder.Dictionary.NextCode >= (1 << currentCodeSize) - 1;
    }

    internal GifRawFrameEncodeResult ValidateInputs(byte[] indexedPixels, int minimumCodeSize, int width, int height)
    {
        GifRawFrameEncodeResult result = new GifRawFrameEncodeResult();

        if (width <= 0 || height <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Raw indexed frame dimensions must be positive for encoding.", new GifDiagnosticPosition(-1, -1, "RawFrameEncode"));
            return result;
        }

        long expectedPixelCount = (long)width * (long)height;
        if (expectedPixelCount > 2147483647)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Raw indexed frame is too large to encode.", new GifDiagnosticPosition(-1, -1, "RawFrameEncode"));
            return result;
        }

        if (indexedPixels == null)
        {
            indexedPixels = new byte[0];
        }

        if (indexedPixels.Length != (int)expectedPixelCount)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Indexed pixel count does not match frame dimensions for encoding.", new GifDiagnosticPosition(-1, -1, "RawFrameEncode"));
            return result;
        }

        GifLzwEncoderDictionary dictionary = new GifLzwEncoderDictionary();
        GifLzwDictionaryResult minimumCodeSizeResult = dictionary.ValidateMinimumCodeSize(minimumCodeSize);
        if (!minimumCodeSizeResult.Success)
        {
            result.Error = minimumCodeSizeResult.Error;
            return result;
        }

        GifErrorDiagnostic paletteError = ValidatePaletteIndices(indexedPixels, minimumCodeSize);
        if (paletteError.Code != GifErrorCode.None)
        {
            result.Error = paletteError;
            return result;
        }

        result.Success = true;
        result.LzwMinimumCodeSize = minimumCodeSize;
        return result;
    }

    internal GifErrorDiagnostic ValidatePaletteIndices(byte[] indexedPixels, int minimumCodeSize)
    {
        if (indexedPixels == null)
        {
            indexedPixels = new byte[0];
        }

        int maximumIndex = (1 << minimumCodeSize) - 1;
        int index = 0;
        while (index < indexedPixels.Length)
        {
            if (indexedPixels[index] > maximumIndex)
            {
                return CreatePaletteIndexError(indexedPixels[index], minimumCodeSize);
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic CreatePaletteIndexError(byte paletteIndex, int minimumCodeSize)
    {
        string message = "Indexed pixel value " + ((int)paletteIndex).ToString() + " exceeds the palette range for LZW minimum code size " + minimumCodeSize.ToString() + ".";
        return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, message, new GifDiagnosticPosition(-1, -1, "RawFrameEncode"));
    }

    internal int CalculateMinimumCodeSize(byte[] indexedPixels)
    {
        if (indexedPixels == null)
        {
            return GifConstants.MinimumLzwMinimumCodeSize;
        }

        int maximumIndex = 0;
        int index = 0;
        while (index < indexedPixels.Length)
        {
            if (indexedPixels[index] > maximumIndex)
            {
                maximumIndex = indexedPixels[index];
            }

            index = index + 1;
        }

        int codeSize = GifConstants.MinimumLzwMinimumCodeSize;
        while (codeSize < GifConstants.MaximumLzwMinimumCodeSize)
        {
            if (maximumIndex < (1 << codeSize))
            {
                return codeSize;
            }

            codeSize = codeSize + 1;
        }

        return GifConstants.MaximumLzwMinimumCodeSize;
    }

    internal GifEncodeOptions NormalizeOptions(GifEncodeOptions options)
    {
        if (options == null)
        {
            return new GifEncodeOptions();
        }

        return options;
    }
}

internal class GifVersionSelector
{
    internal GifVersion Select(GifDocument document, GifEncodeOptions options)
    {
        GifEncodeOptions normalizedOptions = options;
        if (normalizedOptions == null)
        {
            normalizedOptions = new GifEncodeOptions();
        }

        if (normalizedOptions.Version == GifVersion.Gif87a)
        {
            return GifVersion.Gif87a;
        }

        if (normalizedOptions.Version == GifVersion.Gif89a)
        {
            return GifVersion.Gif89a;
        }

        if (RequiresGif89a(document, normalizedOptions))
        {
            return GifVersion.Gif89a;
        }

        return GifVersion.Gif87a;
    }

    internal bool RequiresGif89a(GifDocument document, GifEncodeOptions options)
    {
        if (OptionsRequireGif89a(options))
        {
            return true;
        }

        if (document == null)
        {
            return false;
        }

        if (document.LoopCount >= 0)
        {
            return true;
        }

        if (HasComments(document.Comments))
        {
            return true;
        }

        if (HasApplications(document.ApplicationExtensions))
        {
            return true;
        }

        if (HasPlainTextExtensions(document.PlainTextExtensions))
        {
            return true;
        }

        if (HasUnknownExtensions(document.UnknownExtensions))
        {
            return true;
        }

        if (MetadataRequiresGif89a(document.Metadata))
        {
            return true;
        }

        if (FramesRequireGif89a(document.Frames))
        {
            return true;
        }

        return false;
    }

    internal bool OptionsRequireGif89a(GifEncodeOptions options)
    {
        if (options == null)
        {
            return false;
        }

        if (options.TransparentColorMode != GifTransparentColorMode.None)
        {
            return true;
        }

        if (options.LoopCount >= 0)
        {
            return true;
        }

        if (HasComments(options.Comments))
        {
            return true;
        }

        if (HasApplications(options.ApplicationExtensions))
        {
            return true;
        }

        if (HasPlainTextExtensions(options.PlainTextExtensions))
        {
            return true;
        }

        if (HasUnknownExtensions(options.UnknownExtensions))
        {
            return true;
        }

        return false;
    }

    internal bool MetadataRequiresGif89a(GifMetadata metadata)
    {
        if (metadata == null)
        {
            return false;
        }

        if (metadata.LoopCount >= 0)
        {
            return true;
        }

        if (HasComments(metadata.Comments))
        {
            return true;
        }

        if (HasApplications(metadata.ApplicationExtensions))
        {
            return true;
        }

        if (HasPlainTextExtensions(metadata.PlainTextExtensions))
        {
            return true;
        }

        if (HasUnknownExtensions(metadata.UnknownExtensions))
        {
            return true;
        }

        return false;
    }

    internal bool FramesRequireGif89a(GifFrame[] frames)
    {
        if (frames == null)
        {
            return false;
        }

        int index = 0;
        while (index < frames.Length)
        {
            GifFrame frame = frames[index];
            if (FrameRequiresGif89a(frame))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool FrameRequiresGif89a(GifFrame frame)
    {
        if (frame == null)
        {
            return false;
        }

        if (frame.DelayHundredths > 0)
        {
            return true;
        }

        if (frame.DisposalMethod != GifDisposalMethod.Unspecified)
        {
            return true;
        }

        if (frame.HasTransparency)
        {
            return true;
        }

        if (frame.TransparentColorIndex >= 0)
        {
            return true;
        }

        if (frame.UserInputFlag)
        {
            return true;
        }

        if (HasUnknownExtensions(frame.UnknownExtensions))
        {
            return true;
        }

        return GraphicControlRequiresGif89a(frame.GraphicControl);
    }

    internal bool GraphicControlRequiresGif89a(GifGraphicControl control)
    {
        if (control == null)
        {
            return false;
        }

        if (control.DelayHundredths > 0)
        {
            return true;
        }

        if (control.DisposalMethod != GifDisposalMethod.Unspecified)
        {
            return true;
        }

        if (control.HasTransparency)
        {
            return true;
        }

        if (control.TransparentColorIndex >= 0)
        {
            return true;
        }

        if (control.UserInputFlag)
        {
            return true;
        }

        return false;
    }

    internal bool HasComments(GifCommentExtension[] items)
    {
        return items != null && items.Length > 0;
    }

    internal bool HasApplications(GifApplicationExtension[] items)
    {
        return items != null && items.Length > 0;
    }

    internal bool HasPlainTextExtensions(GifPlainTextExtension[] items)
    {
        return items != null && items.Length > 0;
    }

    internal bool HasUnknownExtensions(GifUnknownExtension[] items)
    {
        return items != null && items.Length > 0;
    }
}
