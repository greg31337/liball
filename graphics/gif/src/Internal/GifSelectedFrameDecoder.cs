using System;
using System.IO;

namespace gif;

internal class GifSelectedRawFrameDecodeResult
{
    internal bool Success;
    internal GifDocument Document;
    internal GifErrorDiagnostic Error;

    internal GifSelectedRawFrameDecodeResult()
    {
        Success = false;
        Document = new GifDocument();
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifSelectedAnimationFrameDecodeResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal GifErrorDiagnostic Error;

    internal GifSelectedAnimationFrameDecodeResult()
    {
        Success = false;
        Animation = new GifAnimation();
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifSelectedFrameDecoder
{
    internal GifSelectedRawFrameDecodeResult DecodeRawBytes(byte[] data, int frameIndex, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult metadata = reader.ReadBytesWithOptions(data, CreateMetadataOptions(options));
        GifErrorDiagnostic validation = ValidateMetadata(metadata, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return CreateRawFailure(validation);
        }

        GifDecodeOptions selectionOptions = CreateSelectionOptions(options);
        GifBlockReadResult selection = reader.ReadFrameRangeBytesWithOptions(data, selectionOptions, frameIndex, frameIndex);
        return CreateRawResult(metadata, selection, frameIndex, selectionOptions);
    }

    internal GifSelectedRawFrameDecodeResult DecodeRawMemory(ReadOnlyMemory<byte> data, int frameIndex, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult metadata = reader.ReadMemoryWithOptions(data, CreateMetadataOptions(options));
        GifErrorDiagnostic validation = ValidateMetadata(metadata, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return CreateRawFailure(validation);
        }

        GifDecodeOptions selectionOptions = CreateSelectionOptions(options);
        GifBlockReadResult selection = reader.ReadFrameRangeMemoryWithOptions(data, selectionOptions, frameIndex, frameIndex);
        return CreateRawResult(metadata, selection, frameIndex, selectionOptions);
    }

    internal GifSelectedRawFrameDecodeResult DecodeRawStream(Stream stream, int frameIndex, GifDecodeOptions? options)
    {
        GifErrorDiagnostic streamValidation = ValidateSeekableStream(stream, frameIndex);
        if (streamValidation.Code != GifErrorCode.None)
        {
            return CreateRawFailure(streamValidation);
        }

        long startPosition = stream.Position;
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult metadata = reader.ReadStreamWithOptions(stream, CreateMetadataOptions(options));
        GifErrorDiagnostic validation = ValidateMetadata(metadata, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return CreateRawFailure(validation);
        }

        stream.Position = startPosition;
        GifDecodeOptions selectionOptions = CreateSelectionOptions(options);
        GifBlockReadResult selection = reader.ReadFrameRangeStreamWithOptions(stream, selectionOptions, frameIndex, frameIndex);
        return CreateRawResult(metadata, selection, frameIndex, selectionOptions);
    }

    internal GifSelectedAnimationFrameDecodeResult DecodeCompositedBytes(byte[] data, int frameIndex, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult metadata = reader.ReadBytesWithOptions(data, CreateMetadataOptions(options));
        GifErrorDiagnostic validation = ValidateMetadata(metadata, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return CreateAnimationFailure(validation);
        }

        GifDecodeOptions selectionOptions = CreateSelectionOptions(options);
        GifBlockReadResult selection = reader.ReadFrameRangeBytesWithOptions(data, selectionOptions, 0, frameIndex);
        return CreateAnimationResult(metadata, selection, frameIndex, selectionOptions);
    }

    internal GifSelectedAnimationFrameDecodeResult DecodeCompositedMemory(ReadOnlyMemory<byte> data, int frameIndex, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult metadata = reader.ReadMemoryWithOptions(data, CreateMetadataOptions(options));
        GifErrorDiagnostic validation = ValidateMetadata(metadata, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return CreateAnimationFailure(validation);
        }

        GifDecodeOptions selectionOptions = CreateSelectionOptions(options);
        GifBlockReadResult selection = reader.ReadFrameRangeMemoryWithOptions(data, selectionOptions, 0, frameIndex);
        return CreateAnimationResult(metadata, selection, frameIndex, selectionOptions);
    }

    internal GifSelectedAnimationFrameDecodeResult DecodeCompositedStream(Stream stream, int frameIndex, GifDecodeOptions? options)
    {
        GifErrorDiagnostic streamValidation = ValidateSeekableStream(stream, frameIndex);
        if (streamValidation.Code != GifErrorCode.None)
        {
            return CreateAnimationFailure(streamValidation);
        }

        long startPosition = stream.Position;
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult metadata = reader.ReadStreamWithOptions(stream, CreateMetadataOptions(options));
        GifErrorDiagnostic validation = ValidateMetadata(metadata, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return CreateAnimationFailure(validation);
        }

        stream.Position = startPosition;
        GifDecodeOptions selectionOptions = CreateSelectionOptions(options);
        GifBlockReadResult selection = reader.ReadFrameRangeStreamWithOptions(stream, selectionOptions, 0, frameIndex);
        return CreateAnimationResult(metadata, selection, frameIndex, selectionOptions);
    }

    internal GifSelectedRawFrameDecodeResult CreateRawResult(GifBlockReadResult metadata, GifBlockReadResult selection, int frameIndex, GifDecodeOptions options)
    {
        GifSelectedRawFrameDecodeResult result = new GifSelectedRawFrameDecodeResult();
        if (selection == null || !selection.Success)
        {
            if (selection == null)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Selected GIF frame read result is missing.", new GifDiagnosticPosition(-1, frameIndex, "FrameSelection"));
            }
            else
            {
                result.Error = selection.Error;
            }

            return result;
        }

        if (metadata == null || metadata.Document == null || metadata.Document.Frames == null || frameIndex >= metadata.Document.Frames.Length)
        {
            result.Error = CreateFrameIndexError(frameIndex);
            return result;
        }

        if (selection.Document == null || selection.Document.Frames == null || frameIndex >= selection.Document.Frames.Length)
        {
            result.Error = CreateFrameIndexError(frameIndex);
            return result;
        }

        GifFrame frame = metadata.Document.Frames[frameIndex];
        GifFrame selectedFrame = selection.Document.Frames[frameIndex];
        frame.LzwMinimumCodeSize = selectedFrame.LzwMinimumCodeSize;
        frame.CompressedImageData = selectedFrame.CompressedImageData;
        GifRawFrameDecoder decoder = new GifRawFrameDecoder();
        GifRawFrameDecodeResult decoded = decoder.DecodeFrameWithOptions(frame, options);
        if (!decoded.Success)
        {
            GifCoalescer coalescer = new GifCoalescer();
            result.Error = coalescer.AddFrameContextToError(decoded.Error, frameIndex);
            return result;
        }

        GifCoalescer warningHelper = new GifCoalescer();
        frame.Warnings = warningHelper.AppendWarningsWithFrameContext(new GifWarningDiagnostic[0], frame.Warnings, frameIndex);
        metadata.Document.Warnings = warningHelper.AppendWarnings(metadata.Document.Warnings, frame.Warnings);
        metadata.Document.DecodeStatistics.WarningCount = metadata.Document.Warnings.Length;
        result.Success = true;
        result.Document = metadata.Document;
        return result;
    }

    internal GifSelectedAnimationFrameDecodeResult CreateAnimationResult(GifBlockReadResult metadata, GifBlockReadResult selection, int frameIndex, GifDecodeOptions options)
    {
        GifSelectedAnimationFrameDecodeResult result = new GifSelectedAnimationFrameDecodeResult();
        if (selection == null || !selection.Success)
        {
            if (selection == null)
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Selected GIF frame read result is missing.", new GifDiagnosticPosition(-1, frameIndex, "FrameSelection"));
            }
            else
            {
                result.Error = selection.Error;
            }

            return result;
        }

        if (metadata == null || metadata.Document == null || selection.Document == null)
        {
            result.Error = CreateFrameIndexError(frameIndex);
            return result;
        }

        selection.Document.LoopCount = metadata.Document.LoopCount;
        selection.Document.Warnings = metadata.Document.Warnings;
        GifCoalescer coalescer = new GifCoalescer();
        GifCoalesceResult coalesced = coalescer.CoalesceSelectedFrameWithOptions(selection.Document, options, frameIndex);
        if (!coalesced.Success)
        {
            result.Error = coalesced.Error;
            return result;
        }

        result.Success = true;
        result.Animation = coalesced.Animation;
        return result;
    }

    internal GifDecodeOptions CreateMetadataOptions(GifDecodeOptions? options)
    {
        GifIdentifier identifier = new GifIdentifier();
        GifDecodeOptions activeOptions = identifier.CreateMetadataOnlyOptions(options);
        activeOptions.FirstFrameOnly = false;
        return activeOptions;
    }

    internal GifDecodeOptions CreateSelectionOptions(GifDecodeOptions? options)
    {
        GifDecodeOptions activeOptions = CreateMetadataOptions(options);
        activeOptions.MetadataOnly = false;
        activeOptions.FirstFrameOnly = false;
        return activeOptions;
    }

    internal GifErrorDiagnostic ValidateMetadata(GifBlockReadResult metadata, int frameIndex)
    {
        if (metadata == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "GIF metadata result is missing for frame selection.", new GifDiagnosticPosition(-1, frameIndex, "FrameSelection"));
        }

        if (!metadata.Success)
        {
            return metadata.Error;
        }

        if (frameIndex < 0 || metadata.Document == null || metadata.Document.Frames == null || frameIndex >= metadata.Document.Frames.Length)
        {
            return CreateFrameIndexError(frameIndex);
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateSeekableStream(Stream stream, int frameIndex)
    {
        if (stream == null || !stream.CanRead || !stream.CanSeek)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidOperationState, "Selected-frame decoding requires a readable, seekable GIF stream.", new GifDiagnosticPosition(-1, frameIndex, "FrameSelection"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic CreateFrameIndexError(int frameIndex)
    {
        return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Selected GIF frame index is outside the available frame list.", new GifDiagnosticPosition(-1, frameIndex, "FrameSelection"));
    }

    internal GifSelectedRawFrameDecodeResult CreateRawFailure(GifErrorDiagnostic error)
    {
        GifSelectedRawFrameDecodeResult result = new GifSelectedRawFrameDecodeResult();
        result.Error = error;
        return result;
    }

    internal GifSelectedAnimationFrameDecodeResult CreateAnimationFailure(GifErrorDiagnostic error)
    {
        GifSelectedAnimationFrameDecodeResult result = new GifSelectedAnimationFrameDecodeResult();
        result.Error = error;
        return result;
    }
}
