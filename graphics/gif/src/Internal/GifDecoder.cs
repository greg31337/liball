namespace gif;

internal class GifRawFrameDecodeResult
{
    internal bool Success;
    internal byte[] IndexedPixels;
    internal GifWarningDiagnostic[] Warnings;
    internal GifErrorDiagnostic Error;

    internal GifRawFrameDecodeResult()
    {
        Success = false;
        IndexedPixels = new byte[0];
        Warnings = new GifWarningDiagnostic[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifRawFrameCodeWriteResult
{
    internal bool Success;
    internal int BytesWritten;
    internal GifErrorDiagnostic Error;

    internal GifRawFrameCodeWriteResult()
    {
        Success = false;
        BytesWritten = 0;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifRawFrameDecoder
{
    internal GifRawFrameDecodeResult DecodeFrame(GifFrame frame)
    {
        return DecodeFrameWithOptions(frame, new GifDecodeOptions());
    }

    internal GifRawFrameDecodeResult DecodeFrameWithOptions(GifFrame frame, GifDecodeOptions options)
    {
        GifRawFrameDecodeResult result = new GifRawFrameDecodeResult();
        if (frame == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Frame is missing for raw indexed decode.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        result = DecodeIndexedPixelsWithOptions(frame.CompressedImageData, frame.LzwMinimumCodeSize, frame.Width, frame.Height, frame.Interlace, options);
        if (result.Success)
        {
            frame.IndexedPixels = result.IndexedPixels;
            frame.Warnings = AppendWarnings(frame.Warnings, result.Warnings);
        }

        return result;
    }

    internal GifRawFrameDecodeResult DecodeIndexedPixels(byte[] compressedData, int minimumCodeSize, int width, int height, bool interlace)
    {
        return DecodeIndexedPixelsWithOptions(compressedData, minimumCodeSize, width, height, interlace, new GifDecodeOptions());
    }

    internal GifRawFrameDecodeResult DecodeIndexedPixelsWithOptions(byte[] compressedData, int minimumCodeSize, int width, int height, bool interlace, GifDecodeOptions options)
    {
        GifRawFrameDecodeResult result = ValidateInputs(width, height);
        if (!result.Success)
        {
            return result;
        }

        if (compressedData == null)
        {
            compressedData = new byte[0];
        }

        GifDecodeOptions activeOptions = NormalizeOptions(options);
        long expectedPixelCount = (long)width * (long)height;
        if (expectedPixelCount > activeOptions.MaxMemoryBytes)
        {
            result.Success = false;
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Raw indexed frame exceeds the configured memory limit.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        GifLzwDecoder decoder = new GifLzwDecoder();
        decoder.Configure(activeOptions);

        GifLzwDecodeCodeResult initialize = decoder.Initialize(minimumCodeSize);
        if (!initialize.Success)
        {
            result.Success = false;
            result.Error = initialize.Error;
            return result;
        }

        GifLzwOutputCountResult outputValidation = decoder.ConfigureOutputValidation(expectedPixelCount, activeOptions.MaxDecodedPixels);
        if (!outputValidation.Success)
        {
            result.Success = false;
            result.Error = outputValidation.Error;
            return result;
        }

        byte[] indexedPixels = new byte[(int)expectedPixelCount];
        GifLzwBitReader reader = new GifLzwBitReader(compressedData);
        int outputOffset = 0;
        int previousOutputCode = -1;
        bool hasPreviousOutputCode = false;

        while (true)
        {
            GifLzwCodeReadResult readCode = reader.ReadCode(decoder.CurrentCodeSize);
            if (!readCode.Success)
            {
                if (readCode.EndOfData && decoder.IsTolerantMode())
                {
                    break;
                }

                result.Success = false;
                result.Error = readCode.Error;
                return result;
            }

            GifLzwDecodeCodeResult decodedCode = decoder.HandleCode(readCode.Code);
            if (!decodedCode.Success)
            {
                result.Success = false;
                result.Error = decodedCode.Error;
                return result;
            }

            if (decodedCode.IsClearCode)
            {
                previousOutputCode = -1;
                hasPreviousOutputCode = false;
                continue;
            }

            if (decodedCode.IsEndCode)
            {
                break;
            }

            if (decodedCode.IsIgnoredCode)
            {
                continue;
            }

            if (hasPreviousOutputCode)
            {
                if (!decoder.Dictionary.IsFull())
                {
                    GifLzwDictionaryResult addResult = decoder.AddDictionaryEntry(previousOutputCode, decodedCode.FirstByte);
                    if (!addResult.Success)
                    {
                        result.Success = false;
                        result.Error = addResult.Error;
                        return result;
                    }
                }
            }

            GifRawFrameCodeWriteResult writeResult = WriteDecodedCode(indexedPixels, outputOffset, decoder, decodedCode);
            if (!writeResult.Success)
            {
                result.Success = false;
                result.Error = writeResult.Error;
                return result;
            }

            GifLzwOutputCountResult countResult = decoder.RecordOutputCount(writeResult.BytesWritten);
            if (!countResult.Success)
            {
                result.Success = false;
                result.Error = countResult.Error;
                return result;
            }

            outputOffset = outputOffset + writeResult.BytesWritten;
            previousOutputCode = decodedCode.Code;
            hasPreviousOutputCode = true;
        }

        GifLzwDecodeCodeResult finalCodeStream = decoder.ValidateFinalCodeStream();
        if (!finalCodeStream.Success)
        {
            result.Success = false;
            result.Error = finalCodeStream.Error;
            return result;
        }

        GifLzwOutputCountResult finalOutputCount = decoder.ValidateFinalOutputCount();
        if (!finalOutputCount.Success)
        {
            if (!decoder.IsTolerantMode())
            {
                result.Success = false;
                result.Error = finalOutputCount.Error;
                return result;
            }

            decoder.AddToleratedWarning("LZW decoded output was shorter than the frame rectangle; missing palette indexes were filled with zero.");
        }

        if (interlace)
        {
            GifInterlaceRowMapper mapper = new GifInterlaceRowMapper();
            GifDeinterlaceResult deinterlaced = mapper.DeinterlaceIndexedPixels(indexedPixels, width, height);
            if (!deinterlaced.Success)
            {
                result.Success = false;
                result.Error = deinterlaced.Error;
                return result;
            }

            result.IndexedPixels = deinterlaced.IndexedPixels;
        }
        else
        {
            result.IndexedPixels = indexedPixels;
        }

        result.Warnings = decoder.Warnings;
        result.Success = true;
        return result;
    }

    internal GifRawFrameDecodeResult ValidateInputs(int width, int height)
    {
        GifRawFrameDecodeResult result = new GifRawFrameDecodeResult();

        if (width <= 0 || height <= 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Raw indexed frame dimensions must be positive.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        long expectedPixelCount = (long)width * (long)height;
        if (expectedPixelCount > 2147483647)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Raw indexed frame is too large to allocate.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal GifRawFrameCodeWriteResult WriteDecodedCode(byte[] indexedPixels, int outputOffset, GifLzwDecoder decoder, GifLzwDecodeCodeResult decodedCode)
    {
        if (decodedCode.IsSpecialCurrentCode)
        {
            GifRawFrameCodeWriteResult prefix = WriteDictionaryCode(indexedPixels, outputOffset, decoder, decodedCode.PrefixCode);
            if (!prefix.Success)
            {
                return prefix;
            }

            GifRawFrameCodeWriteResult suffix = WriteByte(indexedPixels, outputOffset + prefix.BytesWritten, decodedCode.SuffixByte);
            if (!suffix.Success)
            {
                return suffix;
            }

            prefix.BytesWritten = prefix.BytesWritten + suffix.BytesWritten;
            return prefix;
        }

        return WriteDictionaryCode(indexedPixels, outputOffset, decoder, decodedCode.Code);
    }

    internal GifRawFrameCodeWriteResult WriteDictionaryCode(byte[] indexedPixels, int outputOffset, GifLzwDecoder decoder, int code)
    {
        GifRawFrameCodeWriteResult result = new GifRawFrameCodeWriteResult();
        if (indexedPixels == null || decoder == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "Raw indexed decode output buffer or dictionary is missing.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        if (!decoder.Dictionary.ContainsDataCode(code))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW data code cannot be materialized.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        int length = decoder.Dictionary.GetLength(code);
        if (outputOffset < 0 || outputOffset + length > indexedPixels.Length)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "LZW data code writes beyond the expected raw indexed frame size.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        byte[] stack = new byte[GifConstants.MaximumLzwTableSize];
        int stackCount = 0;
        int currentCode = code;
        while (currentCode >= 0)
        {
            if (!decoder.Dictionary.ContainsDataCode(currentCode))
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW dictionary chain is invalid.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
                return result;
            }

            if (stackCount >= stack.Length)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "LZW dictionary chain exceeds the maximum table size.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
                return result;
            }

            stack[stackCount] = decoder.Dictionary.GetSuffixByte(currentCode);
            stackCount = stackCount + 1;
            currentCode = decoder.Dictionary.GetPrefixCode(currentCode);
        }

        int writeIndex = 0;
        while (writeIndex < stackCount)
        {
            indexedPixels[outputOffset + writeIndex] = stack[stackCount - writeIndex - 1];
            writeIndex = writeIndex + 1;
        }

        result.Success = true;
        result.BytesWritten = stackCount;
        return result;
    }

    internal GifRawFrameCodeWriteResult WriteByte(byte[] indexedPixels, int outputOffset, byte value)
    {
        GifRawFrameCodeWriteResult result = new GifRawFrameCodeWriteResult();
        if (indexedPixels == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "Raw indexed decode output buffer is missing.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        if (outputOffset < 0 || outputOffset >= indexedPixels.Length)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "LZW data byte writes beyond the expected raw indexed frame size.", new GifDiagnosticPosition(-1, -1, "RawFrameDecode"));
            return result;
        }

        indexedPixels[outputOffset] = value;
        result.Success = true;
        result.BytesWritten = 1;
        return result;
    }

    internal GifWarningDiagnostic[] AppendWarnings(GifWarningDiagnostic[] items, GifWarningDiagnostic[] warnings)
    {
        if (items == null)
        {
            items = new GifWarningDiagnostic[0];
        }

        if (warnings == null)
        {
            warnings = new GifWarningDiagnostic[0];
        }

        GifWarningDiagnostic[] result = new GifWarningDiagnostic[items.Length + warnings.Length];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        int warningIndex = 0;
        while (warningIndex < warnings.Length)
        {
            result[index] = warnings[warningIndex];
            index = index + 1;
            warningIndex = warningIndex + 1;
        }

        return result;
    }

    internal GifDecodeOptions NormalizeOptions(GifDecodeOptions options)
    {
        if (options == null)
        {
            return new GifDecodeOptions();
        }

        return options;
    }
}
