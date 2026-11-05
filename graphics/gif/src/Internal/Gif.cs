using System.IO;

namespace gif;

/// <summary>Provides the high-level API for identifying, decoding, creating, transforming, validating, and encoding GIF data.</summary>
/// <remarks>Methods report data errors through result models and diagnostics. They do not take ownership of caller-supplied streams or arrays.</remarks>
internal static class Gif
{
    /// <summary>Decodes GIF bytes into a raw document using default safety and preservation settings.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <returns>A document containing parsed blocks, raw frames, metadata, statistics, and warnings.</returns>
    internal static GifDocument Decode(byte[] data)
    {
        return Decode(data, new GifDecodeOptions());
    }

    /// <summary>Decodes GIF bytes into a raw document with caller-selected limits and recovery behavior.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>A document containing parsed blocks, raw frames, metadata, statistics, and warnings.</returns>
    internal static GifDocument Decode(byte[] data, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadBytesWithOptions(data, options);
        return result.Document;
    }

    /// <summary>Reads a GIF stream into a raw document using default settings.</summary>
    /// <param name="stream">A readable stream positioned at the GIF header; it remains open.</param>
    /// <returns>A document containing parsed blocks, raw frames, metadata, statistics, and warnings.</returns>
    internal static GifDocument Decode(Stream stream)
    {
        return Decode(stream, new GifDecodeOptions());
    }

    /// <summary>Reads a GIF stream into a raw document with caller-selected limits and recovery behavior.</summary>
    /// <param name="stream">A readable stream positioned at the GIF header; it remains open.</param>
    /// <param name="options">Decode settings; <c>null</c> uses defaults.</param>
    /// <returns>A document containing parsed blocks, raw frames, metadata, statistics, and warnings.</returns>
    internal static GifDocument Decode(Stream stream, GifDecodeOptions? options)
    {
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadStreamWithOptions(stream, options);
        return result.Document;
    }

    /// <summary>Decodes GIF bytes and composites every raw frame into a display-ready RGBA animation.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <returns>An animation whose frames include offsets, transparency, and disposal effects.</returns>
    internal static GifAnimation DecodeAnimation(byte[] data)
    {
        return DecodeAnimation(data, new GifDecodeOptions());
    }

    /// <summary>Decodes and composites GIF bytes with caller-selected limits and rendering behavior.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode and compositing settings; <c>null</c> uses defaults.</param>
    /// <returns>An animation whose frames include offsets, transparency, and disposal effects.</returns>
    internal static GifAnimation DecodeAnimation(byte[] data, GifDecodeOptions? options)
    {
        GifDocument document = Decode(data, options);
        return CoalesceDocumentForPublicApi(document, options);
    }

    /// <summary>Reads and composites a GIF stream into a display-ready RGBA animation.</summary>
    /// <param name="stream">A readable stream positioned at the GIF header; it remains open.</param>
    /// <returns>An animation whose frames include offsets, transparency, and disposal effects.</returns>
    internal static GifAnimation DecodeAnimation(Stream stream)
    {
        return DecodeAnimation(stream, new GifDecodeOptions());
    }

    /// <summary>Reads and composites a GIF stream with caller-selected limits and rendering behavior.</summary>
    /// <param name="stream">A readable stream positioned at the GIF header; it remains open.</param>
    /// <param name="options">Decode and compositing settings; <c>null</c> uses defaults.</param>
    /// <returns>An animation whose frames include offsets, transparency, and disposal effects.</returns>
    internal static GifAnimation DecodeAnimation(Stream stream, GifDecodeOptions? options)
    {
        GifDocument document = Decode(stream, options);
        return CoalesceDocumentForPublicApi(document, options);
    }

    /// <summary>Composites raw document frames into full-canvas RGBA frames using default rendering behavior.</summary>
    /// <param name="document">The decoded raw GIF document.</param>
    /// <returns>A display-ready animation with disposal and transparency applied, or an empty animation when <c>document</c> is <c>null</c>.</returns>
    internal static GifAnimation Coalesce(GifDocument? document)
    {
        return Coalesce(document, new GifDecodeOptions());
    }

    /// <summary>Composites raw document frames into full-canvas RGBA frames.</summary>
    /// <param name="document">The decoded raw GIF document.</param>
    /// <param name="options">Settings for background fill, disposal fallback, delay normalization, and limits; <c>null</c> uses defaults.</param>
    /// <returns>A display-ready animation with disposal and transparency applied, or an empty animation when <c>document</c> is <c>null</c>.</returns>
    internal static GifAnimation Coalesce(GifDocument? document, GifDecodeOptions? options)
    {
        return CoalesceDocumentForPublicApi(document, options);
    }

    /// <summary>Crops each coalesced frame to the pixels that changed from the preceding frame.</summary>
    /// <param name="animation">An animation containing same-sized full-canvas RGBA frames.</param>
    /// <returns>A copied animation whose frame bitmaps and source rectangles describe changed regions, or an empty animation when <c>animation</c> is <c>null</c>.</returns>
    internal static GifAnimation Deconstruct(GifAnimation? animation)
    {
        GifOptimizer optimizer = new GifOptimizer();
        GifDeconstructResult result = optimizer.Deconstruct(animation);
        return result.Animation;
    }

    /// <summary>Checks whether a document can be encoded with default settings without retaining the generated bytes.</summary>
    /// <param name="document">The document to validate.</param>
    /// <returns>A no-error diagnostic on success, otherwise the first fatal diagnostic.</returns>
    internal static GifErrorDiagnostic Validate(GifDocument? document)
    {
        return Validate(document, new GifEncodeOptions());
    }

    /// <summary>Checks whether a document can be encoded with the supplied settings.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The encode rules and limits to apply; <c>null</c> uses defaults.</param>
    /// <returns>A no-error diagnostic on success, otherwise the first fatal diagnostic.</returns>
    internal static GifErrorDiagnostic Validate(GifDocument? document, GifEncodeOptions? options)
    {
        GifEncodeResult result = EncodeDocumentForPublicApi(document, options);
        return result.Error;
    }

    /// <summary>Checks an indexed or RGBA bitmap using default encode settings.</summary>
    /// <param name="bitmap">The bitmap to validate.</param>
    /// <returns>A no-error diagnostic on success, otherwise the first fatal diagnostic.</returns>
    internal static GifErrorDiagnostic Validate(GifBitmap? bitmap)
    {
        return Validate(bitmap, new GifEncodeOptions());
    }

    /// <summary>Checks an indexed or RGBA bitmap with the supplied encode settings.</summary>
    /// <param name="bitmap">The bitmap to validate.</param>
    /// <param name="options">The encode rules and limits to apply; <c>null</c> uses defaults.</param>
    /// <returns>A no-error diagnostic on success, otherwise the first fatal diagnostic.</returns>
    internal static GifErrorDiagnostic Validate(GifBitmap? bitmap, GifEncodeOptions? options)
    {
        if (bitmap == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF bitmap is missing for validation.", new GifDiagnosticPosition(-1, -1, "Validate"));
        }

        GifEncodeResult result;
        if (bitmap.UsesIndexedPixels)
        {
            result = EncodeIndexedStill(bitmap, options);
        }
        else
        {
            result = EncodeRgbaStill(bitmap, options);
        }

        return result.Error;
    }

    /// <summary>Checks an indexed or RGBA animation using default encode settings.</summary>
    /// <param name="animation">The animation to validate.</param>
    /// <returns>A no-error diagnostic on success, otherwise the first fatal diagnostic.</returns>
    internal static GifErrorDiagnostic Validate(GifAnimation? animation)
    {
        return Validate(animation, new GifEncodeOptions());
    }

    /// <summary>Checks an indexed or RGBA animation with the supplied encode settings.</summary>
    /// <param name="animation">The animation to validate.</param>
    /// <param name="options">The encode rules and limits to apply; <c>null</c> uses defaults.</param>
    /// <returns>A no-error diagnostic on success, otherwise the first fatal diagnostic.</returns>
    internal static GifErrorDiagnostic Validate(GifAnimation? animation, GifEncodeOptions? options)
    {
        if (animation == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation is missing for validation.", new GifDiagnosticPosition(-1, -1, "Validate"));
        }

        if (UsesIndexedPublicAnimation(animation))
        {
            GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
            GifEncodeOptions activeOptions = stillEncoder.NormalizeOptions(options);
            GifAutomaticColorTableEncoder automaticEncoder = new GifAutomaticColorTableEncoder();
            GifEncodeOptions resolvedOptions = automaticEncoder.ResolveIndexedAnimationOptions(animation, activeOptions);
            GifIndexedAnimationEncoder indexedEncoder = new GifIndexedAnimationEncoder();
            return indexedEncoder.ValidateAnimation(animation, resolvedOptions);
        }

        GifRgbaStillEncoder rgbaStillEncoder = new GifRgbaStillEncoder();
        GifEncodeOptions activeEncodeOptions = rgbaStillEncoder.NormalizeEncodeOptions(options);
        GifRgbaAnimationEncoder rgbaEncoder = new GifRgbaAnimationEncoder();
        return rgbaEncoder.ValidateAnimation(animation, activeEncodeOptions);
    }

    /// <summary>Sets one display delay on every existing frame and recalculates total duration.</summary>
    /// <remarks>An invalid negative or out-of-range delay leaves the animation unchanged.</remarks>
    /// <param name="animation">The animation to update in place.</param>
    /// <param name="delayMilliseconds">The delay from 0 through 655350 milliseconds applied to every frame.</param>
    /// <returns>The supplied animation, or an empty animation when <c>animation</c> is <c>null</c>.</returns>
    internal static GifAnimation SetDelay(GifAnimation? animation, int delayMilliseconds)
    {
        if (animation == null)
        {
            return new GifAnimation();
        }

        GifFrameDelayEncoder delayEncoder = new GifFrameDelayEncoder();
        GifErrorDiagnostic validation = delayEncoder.ValidateDelayMilliseconds(delayMilliseconds, -1);
        if (validation.Code != GifErrorCode.None)
        {
            return animation;
        }

        int frameIndex = 0;
        while (animation.Frames != null && frameIndex < animation.Frames.Length)
        {
            if (animation.Frames[frameIndex] != null)
            {
                animation.Frames[frameIndex].DelayMilliseconds = delayMilliseconds;
            }

            frameIndex = frameIndex + 1;
        }

        RecalculatePublicAnimationDuration(animation);
        return animation;
    }

    /// <summary>Sets per-frame display delays and recalculates total duration.</summary>
    /// <remarks>All supplied delays are validated before any frame is changed.</remarks>
    /// <param name="animation">The animation to update in place.</param>
    /// <param name="delaysMilliseconds">Delays from 0 through 655350 milliseconds matched by array index; extra frames retain their current delays.</param>
    /// <returns>The supplied animation, or an empty animation when <c>animation</c> is <c>null</c>.</returns>
    internal static GifAnimation SetDelays(GifAnimation? animation, int[]? delaysMilliseconds)
    {
        if (animation == null)
        {
            return new GifAnimation();
        }

        if (delaysMilliseconds == null)
        {
            return animation;
        }

        GifFrameDelayEncoder delayEncoder = new GifFrameDelayEncoder();
        int frameIndex = 0;
        while (frameIndex < delaysMilliseconds.Length)
        {
            GifErrorDiagnostic validation = delayEncoder.ValidateDelayMilliseconds(delaysMilliseconds[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                return animation;
            }

            frameIndex = frameIndex + 1;
        }

        frameIndex = 0;
        while (animation.Frames != null && frameIndex < animation.Frames.Length && frameIndex < delaysMilliseconds.Length)
        {
            if (animation.Frames[frameIndex] != null)
            {
                animation.Frames[frameIndex].DelayMilliseconds = delaysMilliseconds[frameIndex];
            }

            frameIndex = frameIndex + 1;
        }

        RecalculatePublicAnimationDuration(animation);
        return animation;
    }

    /// <summary>Sets one GIF disposal method on every existing animation frame.</summary>
    /// <remarks>A value outside the GIF three-bit range leaves the animation unchanged.</remarks>
    /// <param name="animation">The animation to update in place.</param>
    /// <param name="disposalMethod">A GIF disposal value from 0 through 7.</param>
    /// <returns>The supplied animation, or an empty animation when <c>animation</c> is <c>null</c>.</returns>
    internal static GifAnimation SetDisposal(GifAnimation? animation, GifDisposalMethod disposalMethod)
    {
        if (animation == null)
        {
            return new GifAnimation();
        }

        GifFrameDisposalEncoder disposalEncoder = new GifFrameDisposalEncoder();
        GifErrorDiagnostic validation = disposalEncoder.ValidateDisposalMethod(disposalMethod, -1);
        if (validation.Code != GifErrorCode.None)
        {
            return animation;
        }

        int frameIndex = 0;
        while (animation.Frames != null && frameIndex < animation.Frames.Length)
        {
            if (animation.Frames[frameIndex] != null)
            {
                animation.Frames[frameIndex].DisposalMethod = disposalMethod;
            }

            frameIndex = frameIndex + 1;
        }

        return animation;
    }

    /// <summary>Sets per-frame GIF disposal methods.</summary>
    /// <remarks>All supplied disposal values are validated before any frame is changed.</remarks>
    /// <param name="animation">The animation to update in place.</param>
    /// <param name="disposalMethods">Disposal values matched by array index; extra frames retain their current values.</param>
    /// <returns>The supplied animation, or an empty animation when <c>animation</c> is <c>null</c>.</returns>
    internal static GifAnimation SetDisposals(GifAnimation? animation, GifDisposalMethod[]? disposalMethods)
    {
        if (animation == null)
        {
            return new GifAnimation();
        }

        if (disposalMethods == null)
        {
            return animation;
        }

        GifFrameDisposalEncoder disposalEncoder = new GifFrameDisposalEncoder();
        int frameIndex = 0;
        while (frameIndex < disposalMethods.Length)
        {
            GifErrorDiagnostic validation = disposalEncoder.ValidateDisposalMethod(disposalMethods[frameIndex], frameIndex);
            if (validation.Code != GifErrorCode.None)
            {
                return animation;
            }

            frameIndex = frameIndex + 1;
        }

        frameIndex = 0;
        while (animation.Frames != null && frameIndex < animation.Frames.Length && frameIndex < disposalMethods.Length)
        {
            if (animation.Frames[frameIndex] != null)
            {
                animation.Frames[frameIndex].DisposalMethod = disposalMethods[frameIndex];
            }

            frameIndex = frameIndex + 1;
        }

        return animation;
    }

    /// <summary>Sets animation repeat metadata.</summary>
    /// <remarks>An unsupported repeat count leaves the animation unchanged.</remarks>
    /// <param name="animation">The animation to update in place.</param>
    /// <param name="loopCount"><c>-1</c> for no loop extension, <c>0</c> for forever, or 1 through 65535 for finite repeats.</param>
    /// <returns>The supplied animation, or an empty animation when <c>animation</c> is <c>null</c>.</returns>
    internal static GifAnimation SetLoopCount(GifAnimation? animation, int loopCount)
    {
        if (animation == null)
        {
            return new GifAnimation();
        }

        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        GifErrorDiagnostic validation = loopEncoder.ValidateOptionalLoopCount(loopCount, -1);
        if (validation.Code != GifErrorCode.None)
        {
            return animation;
        }

        animation.LoopCount = loopCount;
        return animation;
    }

    /// <summary>Replaces loop metadata on a document and removes older loop application blocks.</summary>
    /// <remarks>An unsupported repeat count leaves the document unchanged.</remarks>
    /// <param name="document">The document to update in place.</param>
    /// <param name="loopCount"><c>-1</c> for no loop extension, <c>0</c> for forever, or 1 through 65535 for finite repeats.</param>
    /// <returns>The supplied document, or an empty document when <c>document</c> is <c>null</c>.</returns>
    internal static GifDocument SetLoopCount(GifDocument? document, int loopCount)
    {
        if (document == null)
        {
            return new GifDocument();
        }

        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        GifErrorDiagnostic validation = loopEncoder.ValidateOptionalLoopCount(loopCount, -1);
        if (validation.Code != GifErrorCode.None)
        {
            return document;
        }

        document.ApplicationExtensions = RemoveLoopApplications(document.ApplicationExtensions);
        if (document.Metadata == null)
        {
            document.Metadata = new GifMetadata();
        }

        document.Metadata.ApplicationExtensions = RemoveLoopApplications(document.Metadata.ApplicationExtensions);
        document.LoopCount = loopCount;
        document.Metadata.LoopCount = loopCount;
        return document;
    }

    /// <summary>Replaces document comments after converting each string to a GIF comment payload.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="comments">The replacement comment strings; <c>null</c> clears comments.</param>
    /// <returns>The supplied document, or an empty document when <c>document</c> is <c>null</c>.</returns>
    internal static GifDocument SetComments(GifDocument? document, string[]? comments)
    {
        if (document == null)
        {
            return new GifDocument();
        }

        return SetComments(document, CreatePublicCommentExtensions(comments));
    }

    /// <summary>Replaces the direct and grouped comment-extension arrays on a document.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="comments">The replacement extensions; <c>null</c> clears comments.</param>
    /// <returns>The supplied document, or an empty document when <c>document</c> is <c>null</c>.</returns>
    internal static GifDocument SetComments(GifDocument? document, GifCommentExtension[]? comments)
    {
        if (document == null)
        {
            return new GifDocument();
        }

        GifCommentEncoder commentEncoder = new GifCommentEncoder();
        GifErrorDiagnostic validation = commentEncoder.ValidateComments(comments);
        if (validation.Code != GifErrorCode.None)
        {
            return document;
        }

        if (comments == null)
        {
            comments = new GifCommentExtension[0];
        }

        document.Comments = comments;
        if (document.Metadata == null)
        {
            document.Metadata = new GifMetadata();
        }

        document.Metadata.Comments = comments;
        return document;
    }

    /// <summary>Applies one global palette and removes local palettes from the document's frames.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="palette">A legal GIF palette containing 2 through 256 power-of-two entries.</param>
    /// <returns>The supplied document, or an empty document when <c>document</c> is <c>null</c>.</returns>
    internal static GifDocument SetPalette(GifDocument? document, GifPalette palette)
    {
        if (document == null)
        {
            return new GifDocument();
        }

        GifGlobalColorTableEncoder encoder = new GifGlobalColorTableEncoder();
        encoder.Apply(document, palette);
        return document;
    }

    /// <summary>Applies local palettes to frames by array index.</summary>
    /// <param name="document">The document to update in place.</param>
    /// <param name="palettes">One legal GIF palette per frame.</param>
    /// <returns>The supplied document, or an empty document when <c>document</c> is <c>null</c>.</returns>
    internal static GifDocument SetPalettes(GifDocument? document, GifPalette[] palettes)
    {
        if (document == null)
        {
            return new GifDocument();
        }

        GifLocalColorTableEncoder encoder = new GifLocalColorTableEncoder();
        encoder.Apply(document, palettes);
        return document;
    }

    /// <summary>Copies an animation and refreshes its timeline without applying an optimization transform.</summary>
    /// <param name="animation">The animation to process.</param>
    /// <returns>A copied animation, or an empty animation when validation fails.</returns>
    internal static GifAnimation Optimize(GifAnimation? animation)
    {
        return Optimize(animation, new GifEncodeOptions());
    }

    /// <summary>Applies the selected deconstruction, duplicate-removal, or zero-delay-removal transform.</summary>
    /// <param name="animation">The animation to process without mutating its bitmap data.</param>
    /// <param name="options">Settings whose <see cref="GifEncodeOptions.OptimizationMode"/> selects the transform; <c>null</c> uses defaults.</param>
    /// <returns>The optimized copy, or an empty animation when validation fails.</returns>
    internal static GifAnimation Optimize(GifAnimation? animation, GifEncodeOptions? options)
    {
        GifOptimizer optimizer = new GifOptimizer();
        GifOptimizationResult result = optimizer.OptimizeWithOptions(animation, options);
        return result.Animation;
    }

    internal static GifAnimation CoalesceDocumentForPublicApi(GifDocument? document, GifDecodeOptions? options)
    {
        GifCoalescer coalescer = new GifCoalescer();
        GifCoalesceResult result = coalescer.CoalesceWithOptions(document, options);
        return result.Animation;
    }

    /// <summary>Reads dimensions, frame and extension metadata, warnings, and size estimates without retaining compressed image payloads.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <returns>An identification result whose <see cref="GifInfo.Success"/> and <see cref="GifInfo.Error"/> describe validity.</returns>
    internal static GifInfo Identify(byte[] data)
    {
        return Identify(data, new GifDecodeOptions());
    }

    /// <summary>Identifies GIF bytes with caller-selected limits and recovery behavior.</summary>
    /// <param name="data">The complete GIF file bytes.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults and image payloads are still processed in metadata-only mode.</param>
    /// <returns>An identification result whose <see cref="GifInfo.Success"/> and <see cref="GifInfo.Error"/> describe validity.</returns>
    internal static GifInfo Identify(byte[] data, GifDecodeOptions? options)
    {
        GifIdentifier identifier = new GifIdentifier();
        return identifier.IdentifyBytesWithOptions(data, options);
    }

    /// <summary>Identifies a GIF stream without retaining compressed image payloads.</summary>
    /// <param name="stream">A readable stream positioned at the GIF header; it remains open.</param>
    /// <returns>An identification result whose <see cref="GifInfo.Success"/> and <see cref="GifInfo.Error"/> describe validity.</returns>
    internal static GifInfo Identify(Stream stream)
    {
        return Identify(stream, new GifDecodeOptions());
    }

    /// <summary>Identifies a GIF stream with caller-selected limits and recovery behavior.</summary>
    /// <param name="stream">A readable stream positioned at the GIF header; it remains open.</param>
    /// <param name="options">Decode limits and strictness; <c>null</c> uses defaults and image payloads are still processed in metadata-only mode.</param>
    /// <returns>An identification result whose <see cref="GifInfo.Success"/> and <see cref="GifInfo.Error"/> describe validity.</returns>
    internal static GifInfo Identify(Stream stream, GifDecodeOptions? options)
    {
        GifIdentifier identifier = new GifIdentifier();
        return identifier.IdentifyStreamWithOptions(stream, options);
    }

    /// <summary>Validates and serializes a raw document using default encode settings.</summary>
    /// <param name="document">The document to encode.</param>
    /// <returns>The complete GIF file bytes, or an empty array when validation or encoding fails.</returns>
    internal static byte[] Encode(GifDocument? document)
    {
        return Encode(document, new GifEncodeOptions());
    }

    /// <summary>Validates and serializes a raw document with caller-selected version, metadata, palette, and limit behavior.</summary>
    /// <param name="document">The document to encode.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <returns>The complete GIF file bytes, or an empty array when validation or encoding fails.</returns>
    internal static byte[] Encode(GifDocument? document, GifEncodeOptions? options)
    {
        GifEncodeResult result = EncodeDocumentForPublicApi(document, options);
        return result.Data;
    }

    /// <summary>Encodes a document with default settings and writes the complete result at the stream's current position.</summary>
    /// <param name="stream">A writable destination stream; it remains open. <c>null</c> performs no work.</param>
    /// <param name="document">The document to encode; <c>null</c> produces no output.</param>
    internal static void Write(Stream? stream, GifDocument? document)
    {
        Write(stream, document, new GifEncodeOptions());
    }

    /// <summary>Encodes a document and writes the complete result at the stream's current position.</summary>
    /// <param name="stream">A writable destination stream; it remains open. <c>null</c> performs no work.</param>
    /// <param name="document">The document to encode; <c>null</c> produces no output.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    internal static void Write(Stream? stream, GifDocument? document, GifEncodeOptions? options)
    {
        if (stream == null)
        {
            return;
        }

        byte[] data = Encode(document, options);
        stream.Write(data, 0, data.Length);
    }

    internal static GifEncodeResult EncodeDocumentForPublicApi(GifDocument? document, GifEncodeOptions? options)
    {
        GifEncodeResult result = new GifEncodeResult();
        if (document == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for validation or encoding.", new GifDiagnosticPosition(-1, -1, "Validate"));
            return result;
        }

        GifIndexedStillEncoder encoder = new GifIndexedStillEncoder();
        GifEncodeOptions activeOptions = encoder.NormalizeOptions(options);
        return encoder.WriteDocument(document, activeOptions);
    }

    /// <summary>Builds a one-frame raw document from indexed or RGBA pixels using default settings.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <returns>The normalized document, including compressed frame data, or an empty document when validation fails.</returns>
    internal static GifDocument CreateStill(GifBitmap? bitmap)
    {
        return CreateStill(bitmap, new GifEncodeOptions());
    }

    /// <summary>Builds a one-frame raw document from indexed or RGBA pixels.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="options">Palette, quantization, metadata, version, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The normalized document, including compressed frame data, or an empty document when validation fails.</returns>
    internal static GifDocument CreateStill(GifBitmap? bitmap, GifEncodeOptions? options)
    {
        GifEncodeResult result;
        if (bitmap == null)
        {
            return new GifDocument();
        }

        if (bitmap.UsesIndexedPixels)
        {
            result = EncodeIndexedStill(bitmap, options);
        }
        else
        {
            result = EncodeRgbaStill(bitmap, options);
        }

        return result.Document;
    }

    /// <summary>Builds a raw GIF document from bitmap frames using default playback and encoding settings.</summary>
    /// <param name="frames">Indexed or RGBA bitmaps in playback order.</param>
    /// <returns>The normalized document, including compressed frame data, or an empty document when validation fails.</returns>
    internal static GifDocument CreateAnimation(GifBitmap[]? frames)
    {
        return CreateAnimation(frames, new GifAnimationOptions(), new GifEncodeOptions());
    }

    /// <summary>Builds a raw GIF document from bitmap frames and playback settings.</summary>
    /// <param name="frames">Indexed or RGBA bitmaps in playback order.</param>
    /// <param name="animationOptions">Per-frame timing, disposal, placement, transparency, and looping values; <c>null</c> uses defaults.</param>
    /// <returns>The normalized document, including compressed frame data, or an empty document when validation fails.</returns>
    internal static GifDocument CreateAnimation(GifBitmap[]? frames, GifAnimationOptions? animationOptions)
    {
        return CreateAnimation(frames, animationOptions, new GifEncodeOptions());
    }

    /// <summary>Builds a raw GIF document from bitmap frames with explicit playback and encoding settings.</summary>
    /// <param name="frames">Indexed or RGBA bitmaps in playback order; all non-null frames must use the same pixel representation.</param>
    /// <param name="animationOptions">Per-frame timing, disposal, placement, transparency, and looping values; <c>null</c> uses defaults.</param>
    /// <param name="encodeOptions">Palette, quantization, metadata, version, optimization, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The normalized document, including compressed frame data, or an empty document when validation fails.</returns>
    internal static GifDocument CreateAnimation(GifBitmap[]? frames, GifAnimationOptions? animationOptions, GifEncodeOptions? encodeOptions)
    {
        GifAnimation animation = CreateAnimationForPublicApi(frames, animationOptions);
        GifEncodeResult result;
        if (UsesIndexedPublicFrames(frames))
        {
            result = EncodeIndexedAnimation(animation, encodeOptions);
        }
        else
        {
            result = EncodeRgbaAnimation(animation, encodeOptions);
        }

        return result.Document;
    }

    internal static GifAnimation CreateAnimationForPublicApi(GifBitmap[]? bitmaps, GifAnimationOptions? options)
    {
        GifAnimation animation = new GifAnimation();
        GifAnimationOptions activeOptions = NormalizeAnimationOptionsForPublicApi(options);
        if (bitmaps == null || bitmaps.Length == 0)
        {
            return animation;
        }

        animation.LoopCount = activeOptions.LoopCount;
        animation.Frames = new GifAnimationFrame[bitmaps.Length];
        int frameIndex = 0;
        while (frameIndex < bitmaps.Length)
        {
            GifAnimationFrame frame = new GifAnimationFrame();
            frame.Bitmap = bitmaps[frameIndex];
            frame.SourceLeft = ResolveIntPublicOption(activeOptions.FrameLefts, frameIndex, 0);
            frame.SourceTop = ResolveIntPublicOption(activeOptions.FrameTops, frameIndex, 0);
            frame.DelayMilliseconds = ResolveIntPublicOption(activeOptions.DelaysMilliseconds, frameIndex, activeOptions.DefaultDelayMilliseconds);
            frame.DisposalMethod = ResolveDisposalPublicOption(activeOptions.DisposalMethods, frameIndex, activeOptions.DefaultDisposalMethod);
            frame.HasTransparency = activeOptions.UseTransparency;
            frame.TransparentColorIndex = activeOptions.TransparentColorIndex;
            frame.FrameIndex = frameIndex;
            if (bitmaps[frameIndex] != null)
            {
                frame.SourceWidth = bitmaps[frameIndex].Width;
                frame.SourceHeight = bitmaps[frameIndex].Height;
                long right = (long)frame.SourceLeft + bitmaps[frameIndex].Width;
                long bottom = (long)frame.SourceTop + bitmaps[frameIndex].Height;
                if (right > animation.Width)
                {
                    if (right > 2147483647)
                    {
                        animation.Width = 2147483647;
                    }
                    else
                    {
                        animation.Width = (int)right;
                    }
                }

                if (bottom > animation.Height)
                {
                    if (bottom > 2147483647)
                    {
                        animation.Height = 2147483647;
                    }
                    else
                    {
                        animation.Height = (int)bottom;
                    }
                }
            }

            animation.Frames[frameIndex] = frame;
            frameIndex = frameIndex + 1;
        }

        return animation;
    }

    internal static GifAnimationOptions NormalizeAnimationOptionsForPublicApi(GifAnimationOptions? options)
    {
        if (options == null)
        {
            return new GifAnimationOptions();
        }

        return options;
    }

    internal static int ResolveIntPublicOption(int[] values, int frameIndex, int fallback)
    {
        if (values == null)
        {
            return fallback;
        }

        if (frameIndex < 0 || frameIndex >= values.Length)
        {
            return fallback;
        }

        return values[frameIndex];
    }

    internal static GifDisposalMethod ResolveDisposalPublicOption(GifDisposalMethod[] values, int frameIndex, GifDisposalMethod fallback)
    {
        if (values == null)
        {
            return fallback;
        }

        if (frameIndex < 0 || frameIndex >= values.Length)
        {
            return fallback;
        }

        return values[frameIndex];
    }

    internal static bool UsesIndexedPublicFrames(GifBitmap[]? frames)
    {
        if (frames == null)
        {
            return true;
        }

        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            if (frames[frameIndex] != null)
            {
                return frames[frameIndex].UsesIndexedPixels;
            }

            frameIndex = frameIndex + 1;
        }

        return true;
    }

    internal static GifCommentExtension[] CreatePublicCommentExtensions(string[]? comments)
    {
        if (comments == null)
        {
            return new GifCommentExtension[0];
        }

        GifCommentExtension[] result = new GifCommentExtension[comments.Length];
        int index = 0;
        while (index < comments.Length)
        {
            result[index] = new GifCommentExtension(comments[index]);
            index = index + 1;
        }

        return result;
    }

    internal static GifApplicationExtension[] RemoveLoopApplications(GifApplicationExtension[] applications)
    {
        if (applications == null)
        {
            return new GifApplicationExtension[0];
        }

        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        int count = 0;
        int index = 0;
        while (index < applications.Length)
        {
            if (!writer.IsNetscapeLoopApplication(applications[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        GifApplicationExtension[] result = new GifApplicationExtension[count];
        int outputIndex = 0;
        index = 0;
        while (index < applications.Length)
        {
            if (!writer.IsNetscapeLoopApplication(applications[index]))
            {
                result[outputIndex] = applications[index];
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal static void RecalculatePublicAnimationDuration(GifAnimation animation)
    {
        GifTimeline timeline = new GifTimeline();
        GifTimelineResult result = timeline.Calculate(animation);
        if (result.Success)
        {
            animation.TotalDurationMilliseconds = result.TotalDurationMilliseconds;
        }
    }

    internal static bool UsesIndexedPublicAnimation(GifAnimation? animation)
    {
        if (animation == null)
        {
            return true;
        }

        if (animation.Frames == null)
        {
            return true;
        }

        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            if (animation.Frames[frameIndex] != null && animation.Frames[frameIndex].Bitmap != null)
            {
                return animation.Frames[frameIndex].Bitmap.UsesIndexedPixels;
            }

            frameIndex = frameIndex + 1;
        }

        return true;
    }

    /// <summary>Encodes one indexed bitmap with its palette using default settings.</summary>
    /// <param name="bitmap">An indexed bitmap with one palette index per pixel.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(GifBitmap bitmap)
    {
        GifIndexedStillEncoder encoder = new GifIndexedStillEncoder();
        return encoder.EncodeWithOptions(bitmap, new GifEncodeOptions());
    }

    /// <summary>Encodes one indexed bitmap with caller-selected metadata, palette, interlace, and limit settings.</summary>
    /// <param name="bitmap">An indexed bitmap with one palette index per pixel.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(GifBitmap bitmap, GifEncodeOptions? options)
    {
        GifIndexedStillEncoder encoder = new GifIndexedStillEncoder();
        return encoder.EncodeWithOptions(bitmap, options);
    }

    /// <summary>Encodes one indexed bitmap and explicitly controls its transparent palette entry.</summary>
    /// <param name="bitmap">An indexed bitmap with one palette index per pixel.</param>
    /// <param name="hasTransparency">Whether the transparent palette index is active.</param>
    /// <param name="transparentColorIndex">The palette index skipped by decoders, or <c>-1</c> when inactive.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(GifBitmap bitmap, bool hasTransparency, int transparentColorIndex)
    {
        GifIndexedStillEncoder encoder = new GifIndexedStillEncoder();
        return encoder.EncodeWithTransparency(bitmap, new GifEncodeOptions(), hasTransparency, transparentColorIndex);
    }

    /// <summary>Encodes one indexed bitmap with explicit settings and transparency.</summary>
    /// <param name="bitmap">An indexed bitmap with one palette index per pixel.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <param name="hasTransparency">Whether the transparent palette index is active.</param>
    /// <param name="transparentColorIndex">The palette index skipped by decoders, or <c>-1</c> when inactive.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(GifBitmap bitmap, GifEncodeOptions? options, bool hasTransparency, int transparentColorIndex)
    {
        GifIndexedStillEncoder encoder = new GifIndexedStillEncoder();
        return encoder.EncodeWithTransparency(bitmap, options, hasTransparency, transparentColorIndex);
    }

    /// <summary>Encodes row-major palette indexes as one still GIF using default settings.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="indexedPixels">Exactly width times height palette indexes.</param>
    /// <param name="palette">A legal GIF palette referenced by every index.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(int width, int height, byte[] indexedPixels, GifPalette palette)
    {
        GifBitmap bitmap = GifBitmap.CreateIndexed(width, height, indexedPixels, palette);
        return EncodeIndexedStill(bitmap, new GifEncodeOptions());
    }

    /// <summary>Encodes row-major palette indexes as one still GIF with explicit settings.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="indexedPixels">Exactly width times height palette indexes.</param>
    /// <param name="palette">A legal GIF palette referenced by every index.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(int width, int height, byte[] indexedPixels, GifPalette palette, GifEncodeOptions? options)
    {
        GifBitmap bitmap = GifBitmap.CreateIndexed(width, height, indexedPixels, palette);
        return EncodeIndexedStill(bitmap, options);
    }

    /// <summary>Encodes row-major palette indexes as one still GIF with explicit transparency.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="indexedPixels">Exactly width times height palette indexes.</param>
    /// <param name="palette">A legal GIF palette referenced by every index.</param>
    /// <param name="hasTransparency">Whether the transparent palette index is active.</param>
    /// <param name="transparentColorIndex">The palette index skipped by decoders, or <c>-1</c> when inactive.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(int width, int height, byte[] indexedPixels, GifPalette palette, bool hasTransparency, int transparentColorIndex)
    {
        GifBitmap bitmap = GifBitmap.CreateIndexed(width, height, indexedPixels, palette);
        return EncodeIndexedStill(bitmap, new GifEncodeOptions(), hasTransparency, transparentColorIndex);
    }

    /// <summary>Encodes row-major palette indexes as one still GIF with explicit settings and transparency.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="indexedPixels">Exactly width times height palette indexes.</param>
    /// <param name="palette">A legal GIF palette referenced by every index.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <param name="hasTransparency">Whether the transparent palette index is active.</param>
    /// <param name="transparentColorIndex">The palette index skipped by decoders, or <c>-1</c> when inactive.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedStill(int width, int height, byte[] indexedPixels, GifPalette palette, GifEncodeOptions? options, bool hasTransparency, int transparentColorIndex)
    {
        GifBitmap bitmap = GifBitmap.CreateIndexed(width, height, indexedPixels, palette);
        return EncodeIndexedStill(bitmap, options, hasTransparency, transparentColorIndex);
    }

    /// <summary>Encodes an animation whose frame bitmaps contain indexed pixels.</summary>
    /// <param name="animation">Indexed frames with playback and source-rectangle metadata.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedAnimation(GifAnimation animation)
    {
        GifIndexedAnimationEncoder encoder = new GifIndexedAnimationEncoder();
        return encoder.EncodeWithOptions(animation, new GifEncodeOptions());
    }

    /// <summary>Encodes an indexed animation with explicit palette, metadata, optimization, and limit settings.</summary>
    /// <param name="animation">Indexed frames with playback and source-rectangle metadata.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedAnimation(GifAnimation animation, GifEncodeOptions? options)
    {
        GifIndexedAnimationEncoder encoder = new GifIndexedAnimationEncoder();
        return encoder.EncodeWithOptions(animation, options);
    }

    /// <summary>Encodes indexed bitmaps as an animation with default frame metadata.</summary>
    /// <param name="frames">Indexed bitmaps in playback order.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedAnimation(GifBitmap[] frames)
    {
        GifIndexedAnimationEncoder encoder = new GifIndexedAnimationEncoder();
        GifAnimation animation = encoder.CreateAnimation(frames);
        return encoder.EncodeWithOptions(animation, new GifEncodeOptions());
    }

    /// <summary>Encodes indexed bitmaps as an animation with default frame metadata and explicit encoding settings.</summary>
    /// <param name="frames">Indexed bitmaps in playback order.</param>
    /// <param name="options">The encoding settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, normalized document, and diagnostic produced by encoding.</returns>
    internal static GifEncodeResult EncodeIndexedAnimation(GifBitmap[] frames, GifEncodeOptions? options)
    {
        GifIndexedAnimationEncoder encoder = new GifIndexedAnimationEncoder();
        GifAnimation animation = encoder.CreateAnimation(frames);
        return encoder.EncodeWithOptions(animation, options);
    }

    /// <summary>Quantizes and encodes one RGBA32 bitmap using default settings.</summary>
    /// <param name="bitmap">An RGBA bitmap containing four bytes per pixel.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaStill(GifBitmap bitmap)
    {
        GifRgbaStillEncoder encoder = new GifRgbaStillEncoder();
        return encoder.EncodeWithOptions(bitmap, new GifQuantizeOptions(), new GifEncodeOptions());
    }

    /// <summary>Quantizes and encodes one RGBA32 bitmap using quantization values copied from the encode options.</summary>
    /// <param name="bitmap">An RGBA bitmap containing four bytes per pixel.</param>
    /// <param name="options">Palette, quantizer, dither, metadata, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaStill(GifBitmap bitmap, GifEncodeOptions? options)
    {
        GifRgbaStillEncoder encoder = new GifRgbaStillEncoder();
        GifQuantizeOptions quantizeOptions = encoder.CreateQuantizeOptions(options);
        return encoder.EncodeWithOptions(bitmap, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes one RGBA32 bitmap with explicit color-reduction settings.</summary>
    /// <param name="bitmap">An RGBA bitmap containing four bytes per pixel.</param>
    /// <param name="quantizeOptions">Palette construction and pixel-mapping settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaStill(GifBitmap bitmap, GifQuantizeOptions? quantizeOptions)
    {
        GifRgbaStillEncoder encoder = new GifRgbaStillEncoder();
        return encoder.EncodeWithOptions(bitmap, quantizeOptions, new GifEncodeOptions());
    }

    /// <summary>Quantizes and encodes one RGBA32 bitmap with explicit quantization and encoding settings.</summary>
    /// <param name="bitmap">An RGBA bitmap containing four bytes per pixel.</param>
    /// <param name="quantizeOptions">Palette construction and pixel-mapping settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Metadata, version, interlace, compression, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaStill(GifBitmap bitmap, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        GifRgbaStillEncoder encoder = new GifRgbaStillEncoder();
        return encoder.EncodeWithOptions(bitmap, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes row-major RGBA32 bytes as one still GIF.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="rgbaPixels">Exactly width times height times four bytes in red, green, blue, alpha order.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaStill(int width, int height, byte[] rgbaPixels)
    {
        GifBitmap bitmap = new GifBitmap(width, height, rgbaPixels);
        return EncodeRgbaStill(bitmap);
    }

    /// <summary>Quantizes and encodes row-major RGBA32 bytes with explicit settings.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="rgbaPixels">Exactly width times height times four bytes in red, green, blue, alpha order.</param>
    /// <param name="quantizeOptions">Palette construction and pixel-mapping settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Metadata, version, interlace, compression, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaStill(int width, int height, byte[] rgbaPixels, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        GifBitmap bitmap = new GifBitmap(width, height, rgbaPixels);
        return EncodeRgbaStill(bitmap, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes an RGBA animation using default settings.</summary>
    /// <param name="animation">RGBA frames with playback and source-rectangle metadata.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaAnimation(GifAnimation animation)
    {
        GifRgbaAnimationEncoder encoder = new GifRgbaAnimationEncoder();
        return encoder.EncodeWithOptions(animation, new GifQuantizeOptions(), new GifEncodeOptions());
    }

    /// <summary>Quantizes and encodes an RGBA animation using quantization values copied from the encode options.</summary>
    /// <param name="animation">RGBA frames with playback and source-rectangle metadata.</param>
    /// <param name="options">Palette, quantizer, dither, metadata, optimization, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaAnimation(GifAnimation animation, GifEncodeOptions? options)
    {
        GifRgbaAnimationEncoder encoder = new GifRgbaAnimationEncoder();
        GifRgbaStillEncoder stillEncoder = new GifRgbaStillEncoder();
        GifQuantizeOptions quantizeOptions = stillEncoder.CreateQuantizeOptions(options);
        return encoder.EncodeWithOptions(animation, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes an RGBA animation with explicit color-reduction settings.</summary>
    /// <param name="animation">RGBA frames with playback and source-rectangle metadata.</param>
    /// <param name="quantizeOptions">Palette construction and pixel-mapping settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaAnimation(GifAnimation animation, GifQuantizeOptions? quantizeOptions)
    {
        GifRgbaAnimationEncoder encoder = new GifRgbaAnimationEncoder();
        return encoder.EncodeWithOptions(animation, quantizeOptions, new GifEncodeOptions());
    }

    /// <summary>Quantizes and encodes an RGBA animation with explicit quantization and encoding settings.</summary>
    /// <param name="animation">RGBA frames with playback and source-rectangle metadata.</param>
    /// <param name="quantizeOptions">Palette construction and pixel-mapping settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Metadata, version, interlace, optimization, compression, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaAnimation(GifAnimation animation, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        GifRgbaAnimationEncoder encoder = new GifRgbaAnimationEncoder();
        return encoder.EncodeWithOptions(animation, quantizeOptions, options);
    }

    /// <summary>Quantizes and encodes RGBA bitmaps as an animation with default frame and encoding settings.</summary>
    /// <param name="frames">RGBA bitmaps in playback order.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaAnimation(GifBitmap[] frames)
    {
        GifRgbaAnimationEncoder encoder = new GifRgbaAnimationEncoder();
        GifAnimation animation = encoder.CreateAnimation(frames);
        return encoder.EncodeWithOptions(animation, new GifQuantizeOptions(), new GifEncodeOptions());
    }

    /// <summary>Quantizes and encodes RGBA bitmaps as an animation with default frame metadata and explicit encoding settings.</summary>
    /// <param name="frames">RGBA bitmaps in playback order.</param>
    /// <param name="quantizeOptions">Palette construction and pixel-mapping settings; <c>null</c> uses defaults.</param>
    /// <param name="options">Metadata, version, interlace, optimization, compression, and limit settings; <c>null</c> uses defaults.</param>
    /// <returns>The bytes, indexed document, and diagnostic produced by quantization and encoding.</returns>
    internal static GifEncodeResult EncodeRgbaAnimation(GifBitmap[] frames, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? options)
    {
        GifRgbaAnimationEncoder encoder = new GifRgbaAnimationEncoder();
        GifAnimation animation = encoder.CreateAnimation(frames);
        return encoder.EncodeWithOptions(animation, quantizeOptions, options);
    }
}

internal class GifIndexedStillEncoder
{
    internal GifEncodeResult Encode(GifBitmap bitmap)
    {
        return EncodeWithOptions(bitmap, new GifEncodeOptions());
    }

    internal GifEncodeResult EncodeWithOptions(GifBitmap bitmap, GifEncodeOptions? options)
    {
        return EncodeWithTransparency(bitmap, options, false, -1);
    }

    internal GifEncodeResult EncodeWithTransparency(GifBitmap bitmap, GifEncodeOptions? options, bool hasTransparency, int transparentColorIndex)
    {
        GifEncodeResult result = new GifEncodeResult();
        GifEncodeOptions activeOptions = NormalizeOptions(options);
        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        GifErrorDiagnostic loopValidation = loopEncoder.ValidateOptionalLoopCount(activeOptions.LoopCount, -1);
        if (loopValidation.Code != GifErrorCode.None)
        {
            result.Error = loopValidation;
            return result;
        }

        GifCommentEncoder commentEncoder = new GifCommentEncoder();
        GifErrorDiagnostic commentValidation = commentEncoder.ValidateOptions(activeOptions);
        if (commentValidation.Code != GifErrorCode.None)
        {
            result.Error = commentValidation;
            return result;
        }

        GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
        GifErrorDiagnostic applicationValidation = applicationEncoder.ValidateOptions(activeOptions);
        if (applicationValidation.Code != GifErrorCode.None)
        {
            result.Error = applicationValidation;
            return result;
        }

        GifPlainTextExtensionEncoder plainTextEncoder = new GifPlainTextExtensionEncoder();
        GifErrorDiagnostic plainTextValidation = plainTextEncoder.ValidateOptions(activeOptions);
        if (plainTextValidation.Code != GifErrorCode.None)
        {
            result.Error = plainTextValidation;
            return result;
        }

        GifUnknownExtensionEncoder unknownEncoder = new GifUnknownExtensionEncoder();
        GifErrorDiagnostic unknownValidation = unknownEncoder.ValidateOptions(activeOptions);
        if (unknownValidation.Code != GifErrorCode.None)
        {
            result.Error = unknownValidation;
            return result;
        }

        GifErrorDiagnostic bitmapValidation = ValidateIndexedBitmap(bitmap, activeOptions);
        if (bitmapValidation.Code != GifErrorCode.None)
        {
            result.Error = bitmapValidation;
            return result;
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult paletteResult = builder.CompactToLegalColorTableSize(bitmap.Palette);
        if (!paletteResult.Success)
        {
            result.Error = paletteResult.Error;
            return result;
        }

        GifErrorDiagnostic indexValidation = ValidatePaletteIndexes(bitmap.IndexedPixels, bitmap.Palette.Count);
        if (indexValidation.Code != GifErrorCode.None)
        {
            result.Error = indexValidation;
            return result;
        }

        GifFrameTransparencyEncoder transparencyEncoder = new GifFrameTransparencyEncoder();
        GifErrorDiagnostic transparencyValidation = transparencyEncoder.ValidateTransparency(hasTransparency, transparentColorIndex, bitmap.Palette.Count, -1);
        if (transparencyValidation.Code != GifErrorCode.None)
        {
            result.Error = transparencyValidation;
            return result;
        }

        GifDocument document = CreateDocument(bitmap, paletteResult.Palette, activeOptions);
        GifFrame frame = document.Frames[0];
        transparencyEncoder.ApplyFrame(frame, hasTransparency, transparentColorIndex, -1);
        GifRawFrameEncoder rawEncoder = new GifRawFrameEncoder();
        GifRawFrameEncodeResult frameResult = rawEncoder.EncodeFrameWithOptions(frame, activeOptions);
        if (!frameResult.Success)
        {
            result.Error = frameResult.Error;
            result.Document = document;
            return result;
        }

        document.Version = DetermineVersion(document, activeOptions);

        return WriteDocument(document, activeOptions);
    }

    internal GifDocument CreateDocument(GifBitmap bitmap, GifPalette palette, GifEncodeOptions options)
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(bitmap.Width, bitmap.Height);
        document.LogicalScreen.ColorResolution = 8;
        document.LogicalScreen.BackgroundColorIndex = NormalizeBackgroundColorIndex(options, palette.Count);
        document.LogicalScreen.PixelAspectRatio = 0;

        GifFrame frame = new GifFrame(0, 0, bitmap.Width, bitmap.Height);
        frame.IndexedPixels = CopyBytes(bitmap.IndexedPixels);
        frame.Interlace = options.Interlace;
        frame.HasLocalColorTable = false;
        frame.LocalColorTable = new GifPalette(null);
        document.Frames = new GifFrame[] { frame };

        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        loopEncoder.Apply(document, options.LoopCount);
        GifCommentEncoder commentEncoder = new GifCommentEncoder();
        commentEncoder.Apply(document, options);
        GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
        applicationEncoder.Apply(document, options);
        GifPlainTextExtensionEncoder plainTextEncoder = new GifPlainTextExtensionEncoder();
        plainTextEncoder.Apply(document, options);
        GifUnknownExtensionEncoder unknownEncoder = new GifUnknownExtensionEncoder();
        unknownEncoder.Apply(document, options);

        if (options.ColorTableMode == GifColorTableMode.Local)
        {
            GifLocalColorTableEncoder localEncoder = new GifLocalColorTableEncoder();
            localEncoder.Apply(document, new GifPalette[] { palette });
        }
        else
        {
            GifGlobalColorTableEncoder globalEncoder = new GifGlobalColorTableEncoder();
            globalEncoder.Apply(document, palette);
        }

        return document;
    }

    internal GifEncodeResult WriteDocument(GifDocument document, GifEncodeOptions options)
    {
        GifEncodeResult result = new GifEncodeResult();
        result.Document = document;

        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter streamWriter = new GifStreamByteWriter(stream);
        streamWriter.MaxBytes = options.MaxBytes;
        IGifByteWriter writer = streamWriter;

        GifHeaderWriter headerWriter = new GifHeaderWriter();
        GifHeaderWriteResult header = headerWriter.Write(writer, document.Version);
        if (!header.Success)
        {
            result.Error = ResolveOutputLimitError(header.Error, streamWriter);
            return result;
        }

        GifLogicalScreenDescriptorWriter screenWriter = new GifLogicalScreenDescriptorWriter();
        GifLogicalScreenDescriptorWriteResult screen = screenWriter.Write(writer, document.LogicalScreen);
        if (!screen.Success)
        {
            result.Error = ResolveOutputLimitError(screen.Error, streamWriter);
            return result;
        }

        if (document.LogicalScreen.HasGlobalColorTable)
        {
            GifColorTableWriter colorTableWriter = new GifColorTableWriter();
            GifColorTableWriteResult colorTable = colorTableWriter.Write(writer, document.GlobalColorTable);
            if (!colorTable.Success)
            {
                result.Error = ResolveOutputLimitError(colorTable.Error, streamWriter);
                return result;
            }
        }

        if (document.BlockOrder != null && document.BlockOrder.Length > 0)
        {
            GifErrorDiagnostic orderedWrite = WriteRawBlocks(writer, document, options);
            if (orderedWrite.Code != GifErrorCode.None)
            {
                result.Error = ResolveOutputLimitError(orderedWrite, streamWriter);
                return result;
            }
        }
        else
        {
            GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
            GifErrorDiagnostic loopWrite = loopEncoder.Write(writer, document);
            if (loopWrite.Code != GifErrorCode.None)
            {
                result.Error = ResolveOutputLimitError(loopWrite, streamWriter);
                return result;
            }

            GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
            GifErrorDiagnostic applicationWrite = applicationEncoder.Write(writer, document);
            if (applicationWrite.Code != GifErrorCode.None)
            {
                result.Error = ResolveOutputLimitError(applicationWrite, streamWriter);
                return result;
            }

            GifCommentEncoder commentEncoder = new GifCommentEncoder();
            GifErrorDiagnostic commentWrite = commentEncoder.Write(writer, document);
            if (commentWrite.Code != GifErrorCode.None)
            {
                result.Error = ResolveOutputLimitError(commentWrite, streamWriter);
                return result;
            }

            GifPlainTextExtensionEncoder plainTextEncoder = new GifPlainTextExtensionEncoder();
            GifErrorDiagnostic plainTextWrite = plainTextEncoder.Write(writer, document);
            if (plainTextWrite.Code != GifErrorCode.None)
            {
                result.Error = ResolveOutputLimitError(plainTextWrite, streamWriter);
                return result;
            }

            GifUnknownExtensionEncoder unknownEncoder = new GifUnknownExtensionEncoder();
            GifErrorDiagnostic unknownWrite = unknownEncoder.Write(writer, document);
            if (unknownWrite.Code != GifErrorCode.None)
            {
                result.Error = ResolveOutputLimitError(unknownWrite, streamWriter);
                return result;
            }

            int frameIndex = 0;
            while (frameIndex < document.Frames.Length)
            {
                GifErrorDiagnostic frameWrite = WriteFrame(writer, document.Frames[frameIndex], options, frameIndex);
                if (frameWrite.Code != GifErrorCode.None)
                {
                    result.Error = ResolveOutputLimitError(frameWrite, streamWriter);
                    return result;
                }

                frameIndex = frameIndex + 1;
            }
        }

        GifTrailerWriter trailerWriter = new GifTrailerWriter();
        GifTrailerWriteResult trailer = trailerWriter.Write(writer);
        if (!trailer.Success)
        {
            result.Error = ResolveOutputLimitError(trailer.Error, streamWriter);
            return result;
        }

        writer.Flush();
        byte[] data = stream.ToArray();
        if (data.Length > options.MaxBytes)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Encoded GIF byte limit exceeded.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "GifEncode"));
            return result;
        }

        document.EncodeStatistics.BytesWritten = data.Length;
        document.EncodeStatistics.FramesWritten = document.Frames.Length;
        document.EncodeStatistics.CompressedBytesWritten = CountCompressedBytes(document);
        document.EncodeStatistics.IndexedPixelCount = CountIndexedPixels(document);
        document.EncodeStatistics.ExtensionCount = CountWrittenExtensions(document);
        document.EncodeStatistics.WarningCount = CountWarnings(document);
        result.Data = data;
        result.Success = true;
        return result;
    }

    internal GifErrorDiagnostic ResolveOutputLimitError(GifErrorDiagnostic error, GifStreamByteWriter writer)
    {
        if (writer != null && writer.LimitExceeded)
        {
            return new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Encoded GIF byte limit exceeded.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "GifEncode"));
        }

        return error;
    }

    internal GifErrorDiagnostic WriteFrame(IGifByteWriter writer, GifFrame frame, GifEncodeOptions options, int frameIndex)
    {
        if (NeedsGraphicControl(frame))
        {
            GifGraphicControlExtensionWriter controlWriter = new GifGraphicControlExtensionWriter();
            GifGraphicControl control = CreateGraphicControl(frame);
            GifGraphicControlExtensionWriteResult controlResult = controlWriter.Write(writer, control);
            if (!controlResult.Success)
            {
                return controlResult.Error;
            }
        }

        return WriteImageFrame(writer, frame, options, frameIndex);
    }

    internal GifErrorDiagnostic WriteImageFrame(IGifByteWriter writer, GifFrame frame, GifEncodeOptions options, int frameIndex)
    {

        GifImageDescriptorWriter descriptorWriter = new GifImageDescriptorWriter();
        GifImageDescriptorWriteResult descriptor = descriptorWriter.Write(writer, frame);
        if (!descriptor.Success)
        {
            return descriptor.Error;
        }

        if (frame.HasLocalColorTable)
        {
            GifColorTableWriter colorTableWriter = new GifColorTableWriter();
            GifColorTableWriteResult colorTable = colorTableWriter.Write(writer, frame.LocalColorTable);
            if (!colorTable.Success)
            {
                return colorTable.Error;
            }
        }

        return WriteImageData(writer, frame, options, frameIndex);
    }

    internal GifErrorDiagnostic WriteRawBlocks(IGifByteWriter writer, GifDocument document, GifEncodeOptions options)
    {
        GifCommentExtension[] comments = new GifCommentEncoder().ResolveDocumentComments(document);
        GifApplicationExtension[] applications = new GifApplicationExtensionEncoder().ResolveDocumentApplications(document);
        GifPlainTextExtension[] plainTexts = new GifPlainTextExtensionEncoder().ResolveDocumentPlainTexts(document);
        GifUnknownExtension[] unknownExtensions = new GifUnknownExtensionEncoder().ResolveDocumentUnknownExtensions(document);
        GifRawBlock[] blocks = document.BlockOrder;
        int blockOrderIndex = 0;
        while (blockOrderIndex < blocks.Length)
        {
            GifRawBlock block = blocks[blockOrderIndex];
            if (block == null)
            {
                return CreateRawBlockOrderError(writer, "Raw GIF block order contains a missing entry.");
            }

            if (!IsRawBlockScopeSupported(block.Scope))
            {
                return CreateRawBlockOrderError(writer, "Raw GIF block order contains an unsupported scope.");
            }

            if (block.Kind == GifRawBlockKind.Trailer)
            {
                if (block.Index != -1 || blockOrderIndex != blocks.Length - 1)
                {
                    return CreateRawBlockOrderError(writer, "The raw GIF trailer entry must be last and use index -1.");
                }

                blockOrderIndex = blockOrderIndex + 1;
                continue;
            }

            if (block.Kind == GifRawBlockKind.GraphicControl)
            {
                if (document.GraphicControls == null || block.Index < 0 || block.Index >= document.GraphicControls.Length)
                {
                    return CreateRawBlockOrderError(writer, "Raw GIF graphic-control block index is outside its collection.");
                }

                GifGraphicControl control = document.GraphicControls[block.Index];
                if (block.Scope == GifRawBlockScope.Frame)
                {
                    if (document.Frames == null || block.ScopeIndex < 0 || block.ScopeIndex >= document.Frames.Length)
                    {
                        return CreateRawBlockOrderError(writer, "Raw GIF graphic-control frame scope is outside the frame collection.");
                    }

                    control = CreateGraphicControl(document.Frames[block.ScopeIndex]);
                }
                else if (block.Scope == GifRawBlockScope.PlainText)
                {
                    if (block.ScopeIndex < 0 || block.ScopeIndex >= plainTexts.Length)
                    {
                        return CreateRawBlockOrderError(writer, "Raw GIF graphic-control plain-text scope is outside the plain-text collection.");
                    }

                    control = plainTexts[block.ScopeIndex].GraphicControl;
                }

                GifGraphicControlExtensionWriteResult write = new GifGraphicControlExtensionWriter().Write(writer, control);
                if (!write.Success)
                {
                    return write.Error;
                }
            }
            else if (block.Kind == GifRawBlockKind.Comment)
            {
                if (block.Index < 0 || block.Index >= comments.Length)
                {
                    return CreateRawBlockOrderError(writer, "Raw GIF comment block index is outside its collection.");
                }

                GifCommentExtensionWriteResult write = new GifCommentExtensionWriter().Write(writer, comments[block.Index]);
                if (!write.Success)
                {
                    return write.Error;
                }
            }
            else if (block.Kind == GifRawBlockKind.Application)
            {
                if (block.Index < 0 || block.Index >= applications.Length)
                {
                    return CreateRawBlockOrderError(writer, "Raw GIF application block index is outside its collection.");
                }

                GifApplicationExtensionWriteResult write = new GifApplicationExtensionWriter().Write(writer, applications[block.Index]);
                if (!write.Success)
                {
                    return write.Error;
                }
            }
            else if (block.Kind == GifRawBlockKind.PlainText)
            {
                if (block.Index < 0 || block.Index >= plainTexts.Length)
                {
                    return CreateRawBlockOrderError(writer, "Raw GIF plain-text block index is outside its collection.");
                }

                GifPlainTextExtensionWriteResult write = new GifPlainTextExtensionWriter().Write(writer, plainTexts[block.Index]);
                if (!write.Success)
                {
                    return write.Error;
                }
            }
            else if (block.Kind == GifRawBlockKind.UnknownExtension)
            {
                if (block.Index < 0 || block.Index >= unknownExtensions.Length)
                {
                    return CreateRawBlockOrderError(writer, "Raw GIF unknown-extension block index is outside its collection.");
                }

                GifUnknownExtensionWriteResult write = new GifUnknownExtensionWriter().Write(writer, unknownExtensions[block.Index]);
                if (!write.Success)
                {
                    return write.Error;
                }
            }
            else if (block.Kind == GifRawBlockKind.Image)
            {
                if (document.Frames == null || block.Index < 0 || block.Index >= document.Frames.Length)
                {
                    return CreateRawBlockOrderError(writer, "Raw GIF image block index is outside its collection.");
                }

                GifErrorDiagnostic frameWrite = WriteImageFrame(writer, document.Frames[block.Index], options, block.Index);
                if (frameWrite.Code != GifErrorCode.None)
                {
                    return frameWrite;
                }
            }
            else
            {
                return CreateRawBlockOrderError(writer, "Raw GIF block order contains an unsupported block kind.");
            }

            blockOrderIndex = blockOrderIndex + 1;
        }

        if (blocks.Length == 0 || blocks[blocks.Length - 1] == null || blocks[blocks.Length - 1].Kind != GifRawBlockKind.Trailer)
        {
            return CreateRawBlockOrderError(writer, "Raw GIF block order is missing its final trailer entry.");
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal bool IsRawBlockScopeSupported(GifRawBlockScope scope)
    {
        return scope == GifRawBlockScope.Document || scope == GifRawBlockScope.Frame || scope == GifRawBlockScope.PlainText || scope == GifRawBlockScope.Trailer;
    }

    internal GifErrorDiagnostic CreateRawBlockOrderError(IGifByteWriter writer, string message)
    {
        long byteOffset = -1;
        if (writer != null)
        {
            byteOffset = writer.GetByteOffset();
        }

        return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, message, new GifDiagnosticPosition(byteOffset, -1, "BlockOrder"));
    }

    internal GifErrorDiagnostic WriteImageData(IGifByteWriter writer, GifFrame frame, GifEncodeOptions options, int frameIndex)
    {
        if (frame.LzwMinimumCodeSize < GifConstants.MinimumLzwMinimumCodeSize || frame.LzwMinimumCodeSize > GifConstants.MaximumLzwMinimumCodeSize)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidLzwMinimumCodeSize, "Frame LZW minimum code size is invalid for GIF image data.", new GifDiagnosticPosition(writer.GetByteOffset(), frameIndex, "ImageData"));
        }

        if (!writer.WriteByte(frame.LzwMinimumCodeSize))
        {
            return new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write frame LZW minimum code size.", new GifDiagnosticPosition(writer.GetByteOffset(), frameIndex, "ImageData"));
        }

        GifSubBlockWriter subBlockWriter = new GifSubBlockWriter(options.MaxBytes);
        GifSubBlockWriteResult subBlocks = subBlockWriter.Write(writer, frame.CompressedImageData);
        if (!subBlocks.Success)
        {
            return subBlocks.Error;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifGraphicControl CreateGraphicControl(GifFrame frame)
    {
        GifGraphicControl control = new GifGraphicControl();
        control.DelayHundredths = frame.DelayHundredths;
        control.DisposalMethod = frame.DisposalMethod;
        control.UserInputFlag = frame.UserInputFlag;
        control.HasTransparency = frame.HasTransparency;
        control.TransparentColorIndex = frame.TransparentColorIndex;
        return control;
    }

    internal bool NeedsGraphicControl(GifFrame frame)
    {
        if (frame == null)
        {
            return false;
        }

        if (frame.DelayHundredths != 0)
        {
            return true;
        }

        if (frame.DisposalMethod != GifDisposalMethod.Unspecified)
        {
            return true;
        }

        if (frame.UserInputFlag)
        {
            return true;
        }

        return frame.HasTransparency;
    }

    internal GifEncodeOptions NormalizeOptions(GifEncodeOptions? options)
    {
        if (options == null)
        {
            return new GifEncodeOptions();
        }

        return options;
    }

    internal GifVersion DetermineVersion(GifDocument document, GifEncodeOptions options)
    {
        if (options.Version == GifVersion.Gif87a)
        {
            return GifVersion.Gif87a;
        }

        if (options.Version == GifVersion.Gif89a)
        {
            return GifVersion.Gif89a;
        }

        GifVersionSelector selector = new GifVersionSelector();
        if (selector.RequiresGif89a(document, options))
        {
            return GifVersion.Gif89a;
        }

        return GifVersion.Gif87a;
    }

    internal GifErrorDiagnostic ValidateIndexedBitmap(GifBitmap bitmap, GifEncodeOptions options)
    {
        if (bitmap == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed still bitmap is missing.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (!bitmap.UsesIndexedPixels)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Bitmap must use indexed pixels for indexed still encoding.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed still dimensions must be positive.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (bitmap.Width > 65535 || bitmap.Height > 65535)
        {
            return new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "Indexed still dimensions exceed GIF limits.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        long expectedPixelCount = (long)bitmap.Width * (long)bitmap.Height;
        if (expectedPixelCount > 2147483647)
        {
            return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Indexed still pixel count is too large to allocate.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (expectedPixelCount > options.MaxMemoryBytes)
        {
            return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Indexed still pixel count exceeds the configured memory limit.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (bitmap.IndexedPixels == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Indexed still pixel buffer is missing.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (bitmap.IndexedPixels.Length != (int)expectedPixelCount)
        {
            return new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Indexed still pixel count does not match image dimensions.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidatePaletteIndexes(byte[] indexedPixels, int paletteCount)
    {
        int index = 0;
        while (index < indexedPixels.Length)
        {
            if (indexedPixels[index] >= paletteCount)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Indexed still contains a palette index outside the source palette.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal int NormalizeBackgroundColorIndex(GifEncodeOptions options, int paletteCount)
    {
        if (options.BackgroundColorIndex < 0)
        {
            return 0;
        }

        if (options.BackgroundColorIndex >= paletteCount)
        {
            return 0;
        }

        return options.BackgroundColorIndex;
    }

    internal long CountCompressedBytes(GifDocument document)
    {
        long count = 0;
        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            count = count + document.Frames[frameIndex].CompressedImageData.Length;
            frameIndex = frameIndex + 1;
        }

        return count;
    }

    internal long CountIndexedPixels(GifDocument document)
    {
        long count = 0;
        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            count = count + document.Frames[frameIndex].IndexedPixels.Length;
            frameIndex = frameIndex + 1;
        }

        return count;
    }

    internal int CountWrittenExtensions(GifDocument document)
    {
        if (document.BlockOrder != null && document.BlockOrder.Length > 0)
        {
            int orderedCount = 0;
            int blockIndex = 0;
            while (blockIndex < document.BlockOrder.Length)
            {
                GifRawBlock block = document.BlockOrder[blockIndex];
                if (block != null && block.Kind != GifRawBlockKind.Image && block.Kind != GifRawBlockKind.Trailer)
                {
                    orderedCount = orderedCount + 1;
                }

                blockIndex = blockIndex + 1;
            }

            return orderedCount;
        }

        int count = 0;
        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        if (loopEncoder.ResolveDocumentLoopCount(document) >= 0)
        {
            count = count + 1;
        }

        GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
        GifApplicationExtension[] applications = applicationEncoder.ResolveDocumentApplications(document);
        int applicationIndex = 0;
        while (applicationIndex < applications.Length)
        {
            if (!applicationEncoder.ShouldSkipGeneratedLoopApplication(document, applications[applicationIndex]))
            {
                count = count + 1;
            }

            applicationIndex = applicationIndex + 1;
        }

        count = count + new GifCommentEncoder().ResolveDocumentComments(document).Length;
        count = count + new GifPlainTextExtensionEncoder().ResolveDocumentPlainTexts(document).Length;
        count = count + new GifUnknownExtensionEncoder().ResolveDocumentUnknownExtensions(document).Length;

        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            if (NeedsGraphicControl(document.Frames[frameIndex]))
            {
                count = count + 1;
            }

            frameIndex = frameIndex + 1;
        }

        return count;
    }

    internal int CountWarnings(GifDocument document)
    {
        if (document.Warnings == null)
        {
            return 0;
        }

        return document.Warnings.Length;
    }
}

internal class GifIndexedAnimationEncoder
{
    internal GifEncodeResult Encode(GifAnimation animation)
    {
        return EncodeWithOptions(animation, new GifEncodeOptions());
    }

    internal GifEncodeResult EncodeWithOptions(GifAnimation animation, GifEncodeOptions? options)
    {
        GifEncodeResult result = new GifEncodeResult();
        GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
        GifEncodeOptions activeOptions = stillEncoder.NormalizeOptions(options);
        GifAutomaticColorTableEncoder automaticEncoder = new GifAutomaticColorTableEncoder();
        GifEncodeOptions resolvedOptions = automaticEncoder.ResolveIndexedAnimationOptions(animation, activeOptions);

        GifErrorDiagnostic validation = ValidateAnimation(animation, resolvedOptions);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        GifBitmap firstBitmap = animation.Frames[0].Bitmap;
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult paletteResult = builder.CompactToLegalColorTableSize(firstBitmap.Palette);
        if (!paletteResult.Success)
        {
            result.Error = paletteResult.Error;
            return result;
        }

        GifDocument document = CreateDocument(animation, paletteResult.Palette, resolvedOptions);
        GifRawFrameEncoder rawEncoder = new GifRawFrameEncoder();
        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            GifRawFrameEncodeResult frameResult = rawEncoder.EncodeFrameWithOptions(document.Frames[frameIndex], resolvedOptions);
            if (!frameResult.Success)
            {
                result.Error = frameResult.Error;
                result.Document = document;
                return result;
            }

            frameIndex = frameIndex + 1;
        }

        document.Version = stillEncoder.DetermineVersion(document, resolvedOptions);
        return stillEncoder.WriteDocument(document, resolvedOptions);
    }

    internal GifAnimation CreateAnimation(GifBitmap[] bitmaps)
    {
        GifAnimation animation = new GifAnimation();
        if (bitmaps == null || bitmaps.Length == 0)
        {
            return animation;
        }

        if (bitmaps[0] != null)
        {
            animation.Width = bitmaps[0].Width;
            animation.Height = bitmaps[0].Height;
        }

        animation.Frames = new GifAnimationFrame[bitmaps.Length];
        int frameIndex = 0;
        while (frameIndex < bitmaps.Length)
        {
            GifAnimationFrame frame = new GifAnimationFrame();
            frame.Bitmap = bitmaps[frameIndex];
            frame.SourceLeft = 0;
            frame.SourceTop = 0;
            if (bitmaps[frameIndex] != null)
            {
                frame.SourceWidth = bitmaps[frameIndex].Width;
                frame.SourceHeight = bitmaps[frameIndex].Height;
            }

            frame.FrameIndex = frameIndex;
            animation.Frames[frameIndex] = frame;
            frameIndex = frameIndex + 1;
        }

        return animation;
    }

    internal GifDocument CreateDocument(GifAnimation animation, GifPalette palette, GifEncodeOptions options)
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(ResolveWidth(animation), ResolveHeight(animation));
        document.LogicalScreen.ColorResolution = 8;
        GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
        document.LogicalScreen.BackgroundColorIndex = stillEncoder.NormalizeBackgroundColorIndex(options, palette.Count);
        document.LogicalScreen.PixelAspectRatio = 0;
        document.Frames = new GifFrame[animation.Frames.Length];

        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        int loopCount = loopEncoder.ResolveAnimationLoopCount(options, animation);
        loopEncoder.Apply(document, loopCount);
        GifCommentEncoder commentEncoder = new GifCommentEncoder();
        commentEncoder.Apply(document, options);
        GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
        applicationEncoder.Apply(document, options);
        GifPlainTextExtensionEncoder plainTextEncoder = new GifPlainTextExtensionEncoder();
        plainTextEncoder.Apply(document, options);
        GifUnknownExtensionEncoder unknownEncoder = new GifUnknownExtensionEncoder();
        unknownEncoder.Apply(document, options);

        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            document.Frames[frameIndex] = CreateFrame(animation.Frames[frameIndex], options, frameIndex);
            frameIndex = frameIndex + 1;
        }

        if (options.ColorTableMode == GifColorTableMode.Local)
        {
            GifLocalColorTableEncoder localEncoder = new GifLocalColorTableEncoder();
            localEncoder.Apply(document, CreateLocalPalettes(animation));
        }
        else
        {
            GifGlobalColorTableEncoder globalEncoder = new GifGlobalColorTableEncoder();
            globalEncoder.Apply(document, palette);
        }

        return document;
    }

    internal GifFrame CreateFrame(GifAnimationFrame animationFrame, GifEncodeOptions options, int frameIndex)
    {
        GifBitmap bitmap = animationFrame.Bitmap;
        int left = animationFrame.SourceLeft;
        int top = animationFrame.SourceTop;
        GifFrame frame = new GifFrame(left, top, bitmap.Width, bitmap.Height);
        GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
        frame.IndexedPixels = stillEncoder.CopyBytes(bitmap.IndexedPixels);
        frame.Interlace = options.Interlace;
        frame.HasLocalColorTable = false;
        frame.LocalColorTable = new GifPalette(null);
        GifFrameDelayEncoder delayEncoder = new GifFrameDelayEncoder();
        delayEncoder.Apply(frame, animationFrame, frameIndex);
        GifFrameDisposalEncoder disposalEncoder = new GifFrameDisposalEncoder();
        disposalEncoder.Apply(frame, animationFrame, frameIndex);
        GifFrameTransparencyEncoder transparencyEncoder = new GifFrameTransparencyEncoder();
        transparencyEncoder.Apply(frame, animationFrame, frameIndex);
        return frame;
    }

    internal GifErrorDiagnostic ValidateAnimation(GifAnimation animation, GifEncodeOptions options)
    {
        if (animation == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed animation is missing.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (animation.Frames == null || animation.Frames.Length == 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed animation must contain at least one frame.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        GifErrorDiagnostic loopValidation = loopEncoder.ValidateAnimationLoopCounts(animation, options);
        if (loopValidation.Code != GifErrorCode.None)
        {
            return loopValidation;
        }

        GifCommentEncoder commentEncoder = new GifCommentEncoder();
        GifErrorDiagnostic commentValidation = commentEncoder.ValidateOptions(options);
        if (commentValidation.Code != GifErrorCode.None)
        {
            return commentValidation;
        }

        GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
        GifErrorDiagnostic applicationValidation = applicationEncoder.ValidateOptions(options);
        if (applicationValidation.Code != GifErrorCode.None)
        {
            return applicationValidation;
        }

        GifPlainTextExtensionEncoder plainTextEncoder = new GifPlainTextExtensionEncoder();
        GifErrorDiagnostic plainTextValidation = plainTextEncoder.ValidateOptions(options);
        if (plainTextValidation.Code != GifErrorCode.None)
        {
            return plainTextValidation;
        }

        GifUnknownExtensionEncoder unknownEncoder = new GifUnknownExtensionEncoder();
        GifErrorDiagnostic unknownValidation = unknownEncoder.ValidateOptions(options);
        if (unknownValidation.Code != GifErrorCode.None)
        {
            return unknownValidation;
        }

        int width = ResolveWidth(animation);
        int height = ResolveHeight(animation);
        if (width <= 0 || height <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed animation dimensions must be positive.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (width > 65535 || height > 65535)
        {
            return new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "Indexed animation dimensions exceed GIF limits.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (animation.Frames[0] == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed animation frame is missing.", new GifDiagnosticPosition(-1, 0, "GifEncode"));
        }

        GifBitmap firstBitmap = animation.Frames[0].Bitmap;
        if (firstBitmap == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed animation frame bitmap is missing.", new GifDiagnosticPosition(-1, 0, "GifEncode"));
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult firstPalette = builder.CompactToLegalColorTableSize(firstBitmap.Palette);
        if (!firstPalette.Success)
        {
            return firstPalette.Error;
        }

        long totalPixels = 0;
        GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
        GifLogicalScreen logicalScreen = new GifLogicalScreen(width, height);
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifErrorDiagnostic frameValidation = ValidateAnimationFrame(animation.Frames[frameIndex], firstBitmap.Palette, logicalScreen, options, frameIndex);
            if (frameValidation.Code != GifErrorCode.None)
            {
                return frameValidation;
            }

            totalPixels = totalPixels + animation.Frames[frameIndex].Bitmap.IndexedPixels.Length;
            if (totalPixels > options.MaxMemoryBytes)
            {
                return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Indexed animation pixel count exceeds the configured memory limit.", new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
            }

            GifErrorDiagnostic indexValidation = stillEncoder.ValidatePaletteIndexes(animation.Frames[frameIndex].Bitmap.IndexedPixels, animation.Frames[frameIndex].Bitmap.Palette.Count);
            if (indexValidation.Code != GifErrorCode.None)
            {
                return new GifErrorDiagnostic(indexValidation.Code, indexValidation.Message, new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
            }

            frameIndex = frameIndex + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateAnimationFrame(GifAnimationFrame animationFrame, GifPalette firstPalette, GifLogicalScreen logicalScreen, GifEncodeOptions options, int frameIndex)
    {
        if (animationFrame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "Indexed animation frame is missing.", new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
        }

        GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
        GifErrorDiagnostic bitmapValidation = stillEncoder.ValidateIndexedBitmap(animationFrame.Bitmap, options);
        if (bitmapValidation.Code != GifErrorCode.None)
        {
            return new GifErrorDiagnostic(bitmapValidation.Code, bitmapValidation.Message, new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult paletteResult = builder.CompactToLegalColorTableSize(animationFrame.Bitmap.Palette);
        if (!paletteResult.Success)
        {
            return paletteResult.Error;
        }

        GifFrameDelayEncoder delayEncoder = new GifFrameDelayEncoder();
        GifErrorDiagnostic delayValidation = delayEncoder.ValidateDelayMilliseconds(animationFrame.DelayMilliseconds, frameIndex);
        if (delayValidation.Code != GifErrorCode.None)
        {
            return delayValidation;
        }

        GifFrameDisposalEncoder disposalEncoder = new GifFrameDisposalEncoder();
        GifErrorDiagnostic disposalValidation = disposalEncoder.ValidateDisposalMethod(animationFrame.DisposalMethod, frameIndex);
        if (disposalValidation.Code != GifErrorCode.None)
        {
            return disposalValidation;
        }

        GifFrameTransparencyEncoder transparencyEncoder = new GifFrameTransparencyEncoder();
        GifErrorDiagnostic transparencyValidation = transparencyEncoder.ValidateAnimationFrame(animationFrame, animationFrame.Bitmap.Palette.Count, frameIndex);
        if (transparencyValidation.Code != GifErrorCode.None)
        {
            return transparencyValidation;
        }

        if (options.ColorTableMode != GifColorTableMode.Local && !PalettesMatch(firstPalette, animationFrame.Bitmap.Palette))
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Indexed animation frames must share one source palette for global color table encoding.", new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
        }

        GifFrame rectangle = new GifFrame(animationFrame.SourceLeft, animationFrame.SourceTop, animationFrame.Bitmap.Width, animationFrame.Bitmap.Height);
        GifImageRectangleValidator validator = new GifImageRectangleValidator();
        GifImageRectangleValidationResult rectangleResult = validator.ValidateAtFrameIndex(rectangle, logicalScreen, frameIndex);
        if (!rectangleResult.Success)
        {
            return rectangleResult.Error;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal bool PalettesMatch(GifPalette first, GifPalette second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        if (first.Colors == null || second.Colors == null)
        {
            return false;
        }

        if (first.Count > first.Colors.Length || second.Count > second.Colors.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Count)
        {
            GifColor firstColor = first.Colors[index];
            GifColor secondColor = second.Colors[index];
            if (firstColor == null || secondColor == null)
            {
                return false;
            }

            if (firstColor.Red != secondColor.Red || firstColor.Green != secondColor.Green || firstColor.Blue != secondColor.Blue)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal GifPalette[] CreateLocalPalettes(GifAnimation animation)
    {
        if (animation == null || animation.Frames == null)
        {
            return new GifPalette[0];
        }

        GifPalette[] palettes = new GifPalette[animation.Frames.Length];
        GifPaletteBuilder builder = new GifPaletteBuilder();
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(animation.Frames[frameIndex].Bitmap.Palette);
            if (!compacted.Success)
            {
                return new GifPalette[0];
            }

            palettes[frameIndex] = compacted.Palette;
            frameIndex = frameIndex + 1;
        }

        return palettes;
    }

    internal int ResolveWidth(GifAnimation animation)
    {
        if (animation.Width > 0)
        {
            return animation.Width;
        }

        if (animation.Frames == null || animation.Frames.Length == 0)
        {
            return 0;
        }

        if (animation.Frames[0] == null || animation.Frames[0].Bitmap == null)
        {
            return 0;
        }

        return animation.Frames[0].Bitmap.Width;
    }

    internal int ResolveHeight(GifAnimation animation)
    {
        if (animation.Height > 0)
        {
            return animation.Height;
        }

        if (animation.Frames == null || animation.Frames.Length == 0)
        {
            return 0;
        }

        if (animation.Frames[0] == null || animation.Frames[0].Bitmap == null)
        {
            return 0;
        }

        return animation.Frames[0].Bitmap.Height;
    }
}

internal class GifRgbaStillEncoder
{
    internal GifEncodeResult Encode(GifBitmap bitmap)
    {
        return EncodeWithOptions(bitmap, new GifQuantizeOptions(), new GifEncodeOptions());
    }

    internal GifEncodeResult EncodeWithQuantizeOptions(GifBitmap bitmap, GifQuantizeOptions? quantizeOptions)
    {
        return EncodeWithOptions(bitmap, quantizeOptions, new GifEncodeOptions());
    }

    internal GifEncodeResult EncodeWithOptions(GifBitmap bitmap, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? encodeOptions)
    {
        GifEncodeResult result = new GifEncodeResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic bitmapValidation = builder.ValidateBitmap(bitmap);
        if (bitmapValidation.Code != GifErrorCode.None)
        {
            result.Error = bitmapValidation;
            return result;
        }

        GifQuantizeOptions activeQuantizeOptions = NormalizeQuantizeOptions(quantizeOptions);
        GifEncodeOptions activeEncodeOptions = NormalizeEncodeOptions(encodeOptions);

        GifQuantizer quantizer = new GifQuantizer();
        GifPaletteBuildResult palette = quantizer.QuantizeBitmapWithOptions(bitmap, activeQuantizeOptions);
        if (!palette.Success)
        {
            result.Error = palette.Error;
            return result;
        }

        GifDither dither = new GifDither();
        GifIndexedColorMappingResult mapped = dither.MapWithOptions(bitmap, palette, activeQuantizeOptions);
        if (!mapped.Success)
        {
            result.Error = mapped.Error;
            return result;
        }

        GifIndexedStillEncoder stillEncoder = new GifIndexedStillEncoder();
        bool hasTransparency = palette.HasTransparency && mapped.TransparentPixelCount > 0;
        return stillEncoder.EncodeWithTransparency(mapped.Bitmap, activeEncodeOptions, hasTransparency, palette.TransparentColorIndex);
    }

    internal GifQuantizeOptions CreateQuantizeOptions(GifEncodeOptions? encodeOptions)
    {
        GifEncodeOptions activeEncodeOptions = NormalizeEncodeOptions(encodeOptions);
        GifQuantizeOptions quantizeOptions = new GifQuantizeOptions();
        quantizeOptions.QuantizerMode = activeEncodeOptions.QuantizerMode;
        quantizeOptions.DitherMode = activeEncodeOptions.DitherMode;
        quantizeOptions.PaletteMode = activeEncodeOptions.ColorTableMode;
        return quantizeOptions;
    }

    internal GifQuantizeOptions NormalizeQuantizeOptions(GifQuantizeOptions? options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }

    internal GifEncodeOptions NormalizeEncodeOptions(GifEncodeOptions? options)
    {
        if (options == null)
        {
            return new GifEncodeOptions();
        }

        return options;
    }
}

internal class GifRgbaAnimationMappingResult
{
    internal bool Success;
    internal GifAnimation Animation;
    internal GifErrorDiagnostic Error;

    internal GifRgbaAnimationMappingResult()
    {
        Success = false;
        Animation = new GifAnimation();
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifRgbaAnimationEncoder
{
    internal GifEncodeResult Encode(GifAnimation animation)
    {
        return EncodeWithOptions(animation, new GifQuantizeOptions(), new GifEncodeOptions());
    }

    internal GifEncodeResult EncodeWithQuantizeOptions(GifAnimation animation, GifQuantizeOptions? quantizeOptions)
    {
        return EncodeWithOptions(animation, quantizeOptions, new GifEncodeOptions());
    }

    internal GifEncodeResult EncodeWithOptions(GifAnimation animation, GifQuantizeOptions? quantizeOptions, GifEncodeOptions? encodeOptions)
    {
        GifEncodeResult result = new GifEncodeResult();
        GifRgbaStillEncoder stillEncoder = new GifRgbaStillEncoder();
        GifQuantizeOptions activeQuantizeOptions = stillEncoder.NormalizeQuantizeOptions(quantizeOptions);
        GifEncodeOptions activeEncodeOptions = stillEncoder.NormalizeEncodeOptions(encodeOptions);

        GifErrorDiagnostic validation = ValidateAnimation(animation, activeEncodeOptions);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        if (activeEncodeOptions.ColorTableMode == GifColorTableMode.Local)
        {
            return EncodeLocal(animation, activeQuantizeOptions, activeEncodeOptions);
        }

        if (activeEncodeOptions.ColorTableMode == GifColorTableMode.Auto)
        {
            return EncodeAuto(animation, activeQuantizeOptions, activeEncodeOptions);
        }

        return EncodeGlobal(animation, activeQuantizeOptions, activeEncodeOptions);
    }

    internal GifEncodeResult EncodeAuto(GifAnimation animation, GifQuantizeOptions quantizeOptions, GifEncodeOptions encodeOptions)
    {
        GifAutomaticColorTableEncoder automaticEncoder = new GifAutomaticColorTableEncoder();
        GifEncodeOptions globalOptions = automaticEncoder.CopyEncodeOptionsWithColorTableMode(encodeOptions, GifColorTableMode.Global);
        GifEncodeResult globalResult = EncodeGlobal(animation, quantizeOptions, globalOptions);
        if (globalResult.Success)
        {
            return globalResult;
        }

        if (globalResult.Error.Code == GifErrorCode.InvalidColorTableSize)
        {
            GifEncodeOptions localOptions = automaticEncoder.CopyEncodeOptionsWithColorTableMode(encodeOptions, GifColorTableMode.Local);
            return EncodeLocal(animation, quantizeOptions, localOptions);
        }

        return globalResult;
    }

    internal GifEncodeResult EncodeGlobal(GifAnimation animation, GifQuantizeOptions quantizeOptions, GifEncodeOptions encodeOptions)
    {
        GifEncodeResult result = new GifEncodeResult();
        byte[] combinedPixels = CombineRgbaPixels(animation);
        GifQuantizer quantizer = new GifQuantizer();
        GifPaletteBuildResult palette = quantizer.QuantizeRgbaPixelsWithOptions(combinedPixels, quantizeOptions);
        if (!palette.Success)
        {
            result.Error = palette.Error;
            return result;
        }

        GifRgbaAnimationMappingResult mapping = CreateIndexedAnimation(animation, palette, quantizeOptions);
        if (!mapping.Success)
        {
            result.Error = mapping.Error;
            return result;
        }

        GifIndexedAnimationEncoder indexedEncoder = new GifIndexedAnimationEncoder();
        return indexedEncoder.EncodeWithOptions(mapping.Animation, encodeOptions);
    }

    internal GifEncodeResult EncodeLocal(GifAnimation animation, GifQuantizeOptions quantizeOptions, GifEncodeOptions encodeOptions)
    {
        GifEncodeResult result = new GifEncodeResult();
        GifRgbaAnimationMappingResult mapping = CreateLocalIndexedAnimation(animation, quantizeOptions);
        if (!mapping.Success)
        {
            result.Error = mapping.Error;
            return result;
        }

        GifIndexedAnimationEncoder indexedEncoder = new GifIndexedAnimationEncoder();
        return indexedEncoder.EncodeWithOptions(mapping.Animation, encodeOptions);
    }

    internal GifAnimation CreateAnimation(GifBitmap[] bitmaps)
    {
        GifAnimation animation = new GifAnimation();
        if (bitmaps == null || bitmaps.Length == 0)
        {
            return animation;
        }

        if (bitmaps[0] != null)
        {
            animation.Width = bitmaps[0].Width;
            animation.Height = bitmaps[0].Height;
        }

        animation.Frames = new GifAnimationFrame[bitmaps.Length];
        int frameIndex = 0;
        while (frameIndex < bitmaps.Length)
        {
            GifAnimationFrame frame = new GifAnimationFrame();
            frame.Bitmap = bitmaps[frameIndex];
            frame.SourceLeft = 0;
            frame.SourceTop = 0;
            if (bitmaps[frameIndex] != null)
            {
                frame.SourceWidth = bitmaps[frameIndex].Width;
                frame.SourceHeight = bitmaps[frameIndex].Height;
            }

            frame.FrameIndex = frameIndex;
            animation.Frames[frameIndex] = frame;
            frameIndex = frameIndex + 1;
        }

        return animation;
    }

    internal GifRgbaAnimationMappingResult CreateIndexedAnimation(GifAnimation animation, GifPaletteBuildResult palette, GifQuantizeOptions options)
    {
        GifRgbaAnimationMappingResult result = new GifRgbaAnimationMappingResult();
        GifAnimation indexedAnimation = new GifAnimation();
        indexedAnimation.Width = ResolveWidth(animation);
        indexedAnimation.Height = ResolveHeight(animation);
        indexedAnimation.LoopCount = animation.LoopCount;
        indexedAnimation.Frames = new GifAnimationFrame[animation.Frames.Length];

        GifDither dither = new GifDither();
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifIndexedColorMappingResult mapped = dither.MapWithOptions(animation.Frames[frameIndex].Bitmap, palette, options);
            if (!mapped.Success)
            {
                result.Error = AddFrameContextToMappingError(mapped.Error, frameIndex);
                return result;
            }

            GifAnimationFrame frame = new GifAnimationFrame();
            frame.Bitmap = mapped.Bitmap;
            frame.SourceLeft = animation.Frames[frameIndex].SourceLeft;
            frame.SourceTop = animation.Frames[frameIndex].SourceTop;
            frame.SourceWidth = mapped.Bitmap.Width;
            frame.SourceHeight = mapped.Bitmap.Height;
            frame.DelayMilliseconds = animation.Frames[frameIndex].DelayMilliseconds;
            frame.DisposalMethod = animation.Frames[frameIndex].DisposalMethod;
            frame.HasTransparency = palette.HasTransparency && mapped.TransparentPixelCount > 0;
            if (frame.HasTransparency)
            {
                frame.TransparentColorIndex = palette.TransparentColorIndex;
            }

            frame.FrameIndex = ResolveFrameIndex(animation.Frames[frameIndex], frameIndex);
            indexedAnimation.Frames[frameIndex] = frame;
            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        result.Animation = indexedAnimation;
        return result;
    }

    internal GifRgbaAnimationMappingResult CreateLocalIndexedAnimation(GifAnimation animation, GifQuantizeOptions options)
    {
        GifRgbaAnimationMappingResult result = new GifRgbaAnimationMappingResult();
        GifAnimation indexedAnimation = new GifAnimation();
        indexedAnimation.Width = ResolveWidth(animation);
        indexedAnimation.Height = ResolveHeight(animation);
        indexedAnimation.LoopCount = animation.LoopCount;
        indexedAnimation.Frames = new GifAnimationFrame[animation.Frames.Length];

        GifQuantizer quantizer = new GifQuantizer();
        GifDither dither = new GifDither();
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifPaletteBuildResult palette = quantizer.QuantizeBitmapWithOptions(animation.Frames[frameIndex].Bitmap, options);
            if (!palette.Success)
            {
                result.Error = AddFrameContextToMappingError(palette.Error, frameIndex);
                return result;
            }

            GifIndexedColorMappingResult mapped = dither.MapWithOptions(animation.Frames[frameIndex].Bitmap, palette, options);
            if (!mapped.Success)
            {
                result.Error = AddFrameContextToMappingError(mapped.Error, frameIndex);
                return result;
            }

            GifAnimationFrame frame = new GifAnimationFrame();
            frame.Bitmap = mapped.Bitmap;
            frame.SourceLeft = animation.Frames[frameIndex].SourceLeft;
            frame.SourceTop = animation.Frames[frameIndex].SourceTop;
            frame.SourceWidth = mapped.Bitmap.Width;
            frame.SourceHeight = mapped.Bitmap.Height;
            frame.DelayMilliseconds = animation.Frames[frameIndex].DelayMilliseconds;
            frame.DisposalMethod = animation.Frames[frameIndex].DisposalMethod;
            frame.HasTransparency = palette.HasTransparency && mapped.TransparentPixelCount > 0;
            if (frame.HasTransparency)
            {
                frame.TransparentColorIndex = palette.TransparentColorIndex;
            }

            frame.FrameIndex = ResolveFrameIndex(animation.Frames[frameIndex], frameIndex);
            indexedAnimation.Frames[frameIndex] = frame;
            frameIndex = frameIndex + 1;
        }

        result.Success = true;
        result.Animation = indexedAnimation;
        return result;
    }

    internal GifErrorDiagnostic AddFrameContextToMappingError(GifErrorDiagnostic error, int frameIndex)
    {
        if (error == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "RGBA animation frame mapping failed.", new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
        }

        int resolvedFrameIndex = error.Position.FrameIndex;
        if (resolvedFrameIndex < 0)
        {
            resolvedFrameIndex = frameIndex;
        }

        return new GifErrorDiagnostic(error.Code, error.Message, new GifDiagnosticPosition(error.Position.ByteOffset, resolvedFrameIndex, error.Position.BlockName));
    }

    internal GifErrorDiagnostic ValidateAnimation(GifAnimation animation, GifEncodeOptions options)
    {
        if (animation == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "RGBA animation is missing.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (animation.Frames == null || animation.Frames.Length == 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "RGBA animation must contain at least one frame.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        GifLoopCountEncoder loopEncoder = new GifLoopCountEncoder();
        GifErrorDiagnostic loopValidation = loopEncoder.ValidateAnimationLoopCounts(animation, options);
        if (loopValidation.Code != GifErrorCode.None)
        {
            return loopValidation;
        }

        GifCommentEncoder commentEncoder = new GifCommentEncoder();
        GifErrorDiagnostic commentValidation = commentEncoder.ValidateOptions(options);
        if (commentValidation.Code != GifErrorCode.None)
        {
            return commentValidation;
        }

        GifApplicationExtensionEncoder applicationEncoder = new GifApplicationExtensionEncoder();
        GifErrorDiagnostic applicationValidation = applicationEncoder.ValidateOptions(options);
        if (applicationValidation.Code != GifErrorCode.None)
        {
            return applicationValidation;
        }

        GifPlainTextExtensionEncoder plainTextEncoder = new GifPlainTextExtensionEncoder();
        GifErrorDiagnostic plainTextValidation = plainTextEncoder.ValidateOptions(options);
        if (plainTextValidation.Code != GifErrorCode.None)
        {
            return plainTextValidation;
        }

        GifUnknownExtensionEncoder unknownEncoder = new GifUnknownExtensionEncoder();
        GifErrorDiagnostic unknownValidation = unknownEncoder.ValidateOptions(options);
        if (unknownValidation.Code != GifErrorCode.None)
        {
            return unknownValidation;
        }

        int width = ResolveWidth(animation);
        int height = ResolveHeight(animation);
        if (width <= 0 || height <= 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "RGBA animation dimensions must be positive.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        if (width > 65535 || height > 65535)
        {
            return new GifErrorDiagnostic(GifErrorCode.DimensionLimitExceeded, "RGBA animation dimensions exceed GIF limits.", new GifDiagnosticPosition(-1, -1, "GifEncode"));
        }

        long totalRgbaBytes = 0;
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifLogicalScreen logicalScreen = new GifLogicalScreen(width, height);
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            GifErrorDiagnostic frameValidation = builder.ValidateAnimationFrameBitmap(animation.Frames[frameIndex], frameIndex);
            if (frameValidation.Code != GifErrorCode.None)
            {
                return frameValidation;
            }

            GifFrameDelayEncoder delayEncoder = new GifFrameDelayEncoder();
            GifErrorDiagnostic delayValidation = delayEncoder.ValidateDelayMilliseconds(animation.Frames[frameIndex].DelayMilliseconds, frameIndex);
            if (delayValidation.Code != GifErrorCode.None)
            {
                return delayValidation;
            }

            GifFrameDisposalEncoder disposalEncoder = new GifFrameDisposalEncoder();
            GifErrorDiagnostic disposalValidation = disposalEncoder.ValidateDisposalMethod(animation.Frames[frameIndex].DisposalMethod, frameIndex);
            if (disposalValidation.Code != GifErrorCode.None)
            {
                return disposalValidation;
            }

            totalRgbaBytes = totalRgbaBytes + animation.Frames[frameIndex].Bitmap.RgbaPixels.Length;
            if (totalRgbaBytes > 2147483647)
            {
                return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "RGBA animation pixel data is too large to combine.", new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
            }

            if (totalRgbaBytes > options.MaxMemoryBytes)
            {
                return new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "RGBA animation pixel data exceeds the configured memory limit.", new GifDiagnosticPosition(-1, frameIndex, "GifEncode"));
            }

            GifFrame rectangle = new GifFrame(animation.Frames[frameIndex].SourceLeft, animation.Frames[frameIndex].SourceTop, animation.Frames[frameIndex].Bitmap.Width, animation.Frames[frameIndex].Bitmap.Height);
            GifImageRectangleValidator validator = new GifImageRectangleValidator();
            GifImageRectangleValidationResult rectangleResult = validator.ValidateAtFrameIndex(rectangle, logicalScreen, frameIndex);
            if (!rectangleResult.Success)
            {
                return rectangleResult.Error;
            }

            frameIndex = frameIndex + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal byte[] CombineRgbaPixels(GifAnimation animation)
    {
        int totalLength = 0;
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            totalLength = totalLength + animation.Frames[frameIndex].Bitmap.RgbaPixels.Length;
            frameIndex = frameIndex + 1;
        }

        byte[] combined = new byte[totalLength];
        int destination = 0;
        frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            byte[] source = animation.Frames[frameIndex].Bitmap.RgbaPixels;
            int sourceIndex = 0;
            while (sourceIndex < source.Length)
            {
                combined[destination] = source[sourceIndex];
                destination = destination + 1;
                sourceIndex = sourceIndex + 1;
            }

            frameIndex = frameIndex + 1;
        }

        return combined;
    }

    internal int ResolveFrameIndex(GifAnimationFrame frame, int fallback)
    {
        if (frame.FrameIndex >= 0)
        {
            return frame.FrameIndex;
        }

        return fallback;
    }

    internal int ResolveWidth(GifAnimation animation)
    {
        GifIndexedAnimationEncoder indexedEncoder = new GifIndexedAnimationEncoder();
        return indexedEncoder.ResolveWidth(animation);
    }

    internal int ResolveHeight(GifAnimation animation)
    {
        GifIndexedAnimationEncoder indexedEncoder = new GifIndexedAnimationEncoder();
        return indexedEncoder.ResolveHeight(animation);
    }
}

internal class GifGlobalColorTableEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, GifPalette palette)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for global color table encoding.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        if (document.LogicalScreen == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "Logical screen is missing for global color table encoding.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        GifColorTableValidator validator = new GifColorTableValidator();
        if (!validator.IsValidPalette(palette))
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Global color table mode requires a valid GIF palette.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        document.GlobalColorTable = palette;
        document.LogicalScreen.HasGlobalColorTable = true;
        document.LogicalScreen.GlobalColorTableSize = palette.Count;
        ClearLocalColorTables(document.Frames);
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal void ClearLocalColorTables(GifFrame[] frames)
    {
        if (frames == null)
        {
            return;
        }

        int frameIndex = 0;
        while (frameIndex < frames.Length)
        {
            GifFrame frame = frames[frameIndex];
            if (frame != null)
            {
                frame.HasLocalColorTable = false;
                frame.LocalColorTableSortFlag = false;
                frame.LocalColorTableSize = 0;
                frame.LocalColorTableSizeCode = 0;
                frame.LocalColorTable = new GifPalette(null);
            }

            frameIndex = frameIndex + 1;
        }
    }

    internal bool IsGlobalMode(GifEncodeOptions options)
    {
        if (options == null)
        {
            return true;
        }

        if (options.ColorTableMode == GifColorTableMode.Global)
        {
            return true;
        }

        return options.ColorTableMode == GifColorTableMode.Auto;
    }
}

internal class GifLocalColorTableEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, GifPalette[] palettes)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "GIF document is missing for local color table encoding.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        if (document.LogicalScreen == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingLogicalScreenDescriptor, "Logical screen is missing for local color table encoding.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        if (document.Frames == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frames are missing for local color table encoding.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        if (palettes == null || palettes.Length != document.Frames.Length)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Local color table mode requires one palette per frame.", new GifDiagnosticPosition(-1, -1, "ColorTableMode"));
        }

        int validationIndex = 0;
        while (validationIndex < document.Frames.Length)
        {
            GifErrorDiagnostic validation = ValidateFrame(document.Frames[validationIndex], palettes[validationIndex], validationIndex);
            if (validation.Code != GifErrorCode.None)
            {
                return validation;
            }

            validationIndex = validationIndex + 1;
        }

        document.GlobalColorTable = new GifPalette(null);
        document.LogicalScreen.HasGlobalColorTable = false;
        document.LogicalScreen.GlobalColorTableSize = 0;

        int frameIndex = 0;
        while (frameIndex < document.Frames.Length)
        {
            GifErrorDiagnostic result = ApplyFrame(document.Frames[frameIndex], palettes[frameIndex], frameIndex);
            if (result.Code != GifErrorCode.None)
            {
                return result;
            }

            frameIndex = frameIndex + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ApplyFrame(GifFrame frame, GifPalette palette, int frameIndex)
    {
        GifErrorDiagnostic validation = ValidateFrame(frame, palette, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        GifPackedFields packedFields = new GifPackedFields();
        frame.HasLocalColorTable = true;
        frame.LocalColorTableSortFlag = false;
        frame.LocalColorTableSize = palette.Count;
        frame.LocalColorTableSizeCode = packedFields.ComputeColorTableSizeCode(palette.Count);
        frame.LocalColorTable = palette;
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateFrame(GifFrame frame, GifPalette palette, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for local color table encoding.", new GifDiagnosticPosition(-1, frameIndex, "ColorTableMode"));
        }

        GifColorTableValidator validator = new GifColorTableValidator();
        if (!validator.IsValidPalette(palette))
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Local color table mode requires a valid GIF palette.", new GifDiagnosticPosition(-1, frameIndex, "ColorTableMode"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal bool IsLocalMode(GifEncodeOptions options)
    {
        if (options == null)
        {
            return false;
        }

        return options.ColorTableMode == GifColorTableMode.Local;
    }
}

internal class GifAutomaticColorTableEncoder
{
    internal GifEncodeOptions ResolveIndexedAnimationOptions(GifAnimation animation, GifEncodeOptions options)
    {
        GifEncodeOptions activeOptions = NormalizeOptions(options);
        if (activeOptions.ColorTableMode != GifColorTableMode.Auto)
        {
            return activeOptions;
        }

        GifColorTableMode selectedMode = SelectIndexedAnimationMode(animation);
        return CopyEncodeOptionsWithColorTableMode(activeOptions, selectedMode);
    }

    internal GifColorTableMode SelectIndexedAnimationMode(GifAnimation animation)
    {
        if (animation == null || animation.Frames == null || animation.Frames.Length == 0)
        {
            return GifColorTableMode.Global;
        }

        if (animation.Frames[0] == null || animation.Frames[0].Bitmap == null)
        {
            return GifColorTableMode.Global;
        }

        GifPalette firstPalette = animation.Frames[0].Bitmap.Palette;
        GifIndexedAnimationEncoder indexedEncoder = new GifIndexedAnimationEncoder();
        int frameIndex = 1;
        while (frameIndex < animation.Frames.Length)
        {
            if (animation.Frames[frameIndex] == null || animation.Frames[frameIndex].Bitmap == null)
            {
                return GifColorTableMode.Global;
            }

            if (!indexedEncoder.PalettesMatch(firstPalette, animation.Frames[frameIndex].Bitmap.Palette))
            {
                return GifColorTableMode.Local;
            }

            frameIndex = frameIndex + 1;
        }

        return GifColorTableMode.Global;
    }

    internal GifEncodeOptions CopyEncodeOptionsWithColorTableMode(GifEncodeOptions options, GifColorTableMode colorTableMode)
    {
        GifEncodeOptions source = NormalizeOptions(options);
        GifEncodeOptions copy = new GifEncodeOptions();
        copy.Version = source.Version;
        copy.ColorTableMode = colorTableMode;
        copy.QuantizerMode = source.QuantizerMode;
        copy.DitherMode = source.DitherMode;
        copy.OptimizationMode = source.OptimizationMode;
        copy.RemoveZeroDelayFrames = source.RemoveZeroDelayFrames;
        copy.RemoveDuplicateFrames = source.RemoveDuplicateFrames;
        copy.NormalizeFrameDelays = source.NormalizeFrameDelays;
        copy.MinimumFrameDelayMilliseconds = source.MinimumFrameDelayMilliseconds;
        copy.DeconstructFrames = source.DeconstructFrames;
        copy.RecalculateTimeline = source.RecalculateTimeline;
        copy.LzwClearPolicy = source.LzwClearPolicy;
        copy.LoopCount = source.LoopCount;
        copy.Comments = source.Comments;
        copy.ApplicationExtensions = source.ApplicationExtensions;
        copy.PlainTextExtensions = source.PlainTextExtensions;
        copy.UnknownExtensions = source.UnknownExtensions;
        copy.BackgroundColorIndex = source.BackgroundColorIndex;
        copy.TransparentColorMode = source.TransparentColorMode;
        copy.Interlace = source.Interlace;
        copy.PreserveMetadata = source.PreserveMetadata;
        copy.ValidateBeforeWrite = source.ValidateBeforeWrite;
        copy.MaxBytes = source.MaxBytes;
        copy.MaxMemoryBytes = source.MaxMemoryBytes;
        return copy;
    }

    internal GifEncodeOptions NormalizeOptions(GifEncodeOptions? options)
    {
        if (options == null)
        {
            return new GifEncodeOptions();
        }

        return options;
    }
}

internal class GifFrameDelayEncoder
{
    internal GifErrorDiagnostic Apply(GifFrame frame, GifAnimationFrame animationFrame, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for delay encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        if (animationFrame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for delay encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        GifErrorDiagnostic validation = ValidateDelayMilliseconds(animationFrame.DelayMilliseconds, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        frame.DelayHundredths = ConvertMillisecondsToHundredths(animationFrame.DelayMilliseconds);
        frame.GraphicControl.DelayHundredths = frame.DelayHundredths;
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateDelayMilliseconds(int delayMilliseconds, int frameIndex)
    {
        if (delayMilliseconds < 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Frame delay must be nonnegative for GIF encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        if (delayMilliseconds > 655350)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Frame delay exceeds the GIF graphic control extension limit.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal int ConvertMillisecondsToHundredths(int delayMilliseconds)
    {
        if (delayMilliseconds <= 0)
        {
            return 0;
        }

        int delayHundredths = delayMilliseconds / 10;
        if (delayMilliseconds % 10 != 0)
        {
            delayHundredths = delayHundredths + 1;
        }

        return delayHundredths;
    }
}

internal class GifFrameDisposalEncoder
{
    internal GifErrorDiagnostic Apply(GifFrame frame, GifAnimationFrame animationFrame, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for disposal encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        if (animationFrame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for disposal encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        GifErrorDiagnostic validation = ValidateDisposalMethod(animationFrame.DisposalMethod, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        frame.DisposalMethod = animationFrame.DisposalMethod;
        frame.GraphicControl.DisposalMethod = frame.DisposalMethod;
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateDisposalMethod(GifDisposalMethod disposalMethod, int frameIndex)
    {
        int value = (int)disposalMethod;
        if (value < 0 || value > 7)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Frame disposal method is outside the GIF graphic control extension range.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifFrameTransparencyEncoder
{
    internal GifErrorDiagnostic Apply(GifFrame frame, GifAnimationFrame animationFrame, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for transparency encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        if (animationFrame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for transparency encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        if (animationFrame.Bitmap == null || animationFrame.Bitmap.Palette == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.MissingColorTable, "GIF animation frame palette is missing for transparency encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        GifErrorDiagnostic validation = ValidateTransparency(animationFrame.HasTransparency, animationFrame.TransparentColorIndex, animationFrame.Bitmap.Palette.Count, frameIndex);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        return ApplyFrame(frame, animationFrame.HasTransparency, animationFrame.TransparentColorIndex, frameIndex);
    }

    internal GifErrorDiagnostic ApplyFrame(GifFrame frame, bool hasTransparency, int transparentColorIndex, int frameIndex)
    {
        if (frame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF frame is missing for transparency encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        frame.HasTransparency = hasTransparency;
        if (hasTransparency)
        {
            frame.TransparentColorIndex = transparentColorIndex;
        }
        else
        {
            frame.TransparentColorIndex = -1;
        }

        frame.GraphicControl.HasTransparency = frame.HasTransparency;
        frame.GraphicControl.TransparentColorIndex = frame.TransparentColorIndex;
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateAnimationFrame(GifAnimationFrame animationFrame, int paletteCount, int frameIndex)
    {
        if (animationFrame == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF animation frame is missing for transparency encoding.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        return ValidateTransparency(animationFrame.HasTransparency, animationFrame.TransparentColorIndex, paletteCount, frameIndex);
    }

    internal GifErrorDiagnostic ValidateTransparency(bool hasTransparency, int transparentColorIndex, int paletteCount, int frameIndex)
    {
        if (transparentColorIndex < -1 || transparentColorIndex > 255)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is outside the GIF palette index range.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
        }

        if (hasTransparency)
        {
            if (transparentColorIndex < 0)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index is required when transparency is enabled.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
            }

            if (transparentColorIndex >= paletteCount)
            {
                return new GifErrorDiagnostic(GifErrorCode.InvalidPaletteIndex, "Transparent color index exceeds the active palette size.", new GifDiagnosticPosition(-1, frameIndex, "GraphicControlExtension"));
            }
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifLoopCountEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, int loopCount)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for loop count encoding.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
        }

        GifErrorDiagnostic validation = ValidateOptionalLoopCount(loopCount, -1);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        if (loopCount < 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        GifApplicationExtension application = writer.CreateNetscapeLoopApplication(loopCount);
        document.LoopCount = loopCount;
        document.Metadata.LoopCount = loopCount;
        document.ApplicationExtensions = AppendApplication(document.ApplicationExtensions, application);
        document.Metadata.ApplicationExtensions = AppendApplication(document.Metadata.ApplicationExtensions, application);
        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic Write(IGifByteWriter writer, GifDocument document)
    {
        if (writer == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing GIF writer for loop count encoding.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
        }

        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for loop count encoding.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ApplicationExtension"));
        }

        int loopCount = ResolveDocumentLoopCount(document);
        GifErrorDiagnostic validation = ValidateOptionalLoopCount(loopCount, -1);
        if (validation.Code != GifErrorCode.None)
        {
            return new GifErrorDiagnostic(validation.Code, validation.Message, new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ApplicationExtension"));
        }

        if (loopCount < 0)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        GifApplicationExtensionWriter applicationWriter = new GifApplicationExtensionWriter();
        GifApplicationExtensionWriteResult write = applicationWriter.WriteNetscapeLoopExtension(writer, loopCount);
        if (!write.Success)
        {
            return write.Error;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal int ResolveAnimationLoopCount(GifEncodeOptions options, GifAnimation animation)
    {
        if (options != null)
        {
            if (options.LoopCount >= 0)
            {
                return options.LoopCount;
            }
        }

        if (animation != null)
        {
            if (animation.LoopCount >= 0)
            {
                return animation.LoopCount;
            }
        }

        return -1;
    }

    internal int ResolveDocumentLoopCount(GifDocument document)
    {
        if (document == null)
        {
            return -1;
        }

        if (document.LoopCount >= 0)
        {
            return document.LoopCount;
        }

        if (document.Metadata != null)
        {
            if (document.Metadata.LoopCount >= 0)
            {
                return document.Metadata.LoopCount;
            }
        }

        return -1;
    }

    internal GifErrorDiagnostic ValidateAnimationLoopCounts(GifAnimation animation, GifEncodeOptions options)
    {
        if (options != null)
        {
            GifErrorDiagnostic optionValidation = ValidateOptionalLoopCount(options.LoopCount, -1);
            if (optionValidation.Code != GifErrorCode.None)
            {
                return optionValidation;
            }
        }

        if (animation != null)
        {
            GifErrorDiagnostic animationValidation = ValidateOptionalLoopCount(animation.LoopCount, -1);
            if (animationValidation.Code != GifErrorCode.None)
            {
                return animationValidation;
            }
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateOptionalLoopCount(int loopCount, int frameIndex)
    {
        if (loopCount == -1)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        if (loopCount < 0 || loopCount > 65535)
        {
            return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Loop count must be -1 or a 16-bit unsigned GIF repeat count.", new GifDiagnosticPosition(-1, frameIndex, "ApplicationExtension"));
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifApplicationExtension[] AppendApplication(GifApplicationExtension[] items, GifApplicationExtension item)
    {
        if (items == null)
        {
            items = new GifApplicationExtension[0];
        }

        GifApplicationExtension[] result = new GifApplicationExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }
}

internal class GifCommentEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, GifEncodeOptions options)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for comment encoding.", new GifDiagnosticPosition(-1, -1, "CommentExtension"));
        }

        GifErrorDiagnostic validation = ValidateOptions(options);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        GifCommentExtension[] comments = ResolveOptionComments(options);
        int index = 0;
        while (index < comments.Length)
        {
            document.Comments = AppendComment(document.Comments, comments[index]);
            if (document.Metadata == null)
            {
                document.Metadata = new GifMetadata();
            }

            document.Metadata.Comments = AppendComment(document.Metadata.Comments, comments[index]);
            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic Write(IGifByteWriter writer, GifDocument document)
    {
        if (writer == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing GIF writer for comment encoding.", new GifDiagnosticPosition(-1, -1, "CommentExtension"));
        }

        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for comment encoding.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "CommentExtension"));
        }

        GifCommentExtension[] comments = ResolveDocumentComments(document);
        GifErrorDiagnostic validation = ValidateComments(comments);
        if (validation.Code != GifErrorCode.None)
        {
            return new GifErrorDiagnostic(validation.Code, validation.Message, new GifDiagnosticPosition(writer.GetByteOffset(), -1, "CommentExtension"));
        }

        GifCommentExtensionWriter commentWriter = new GifCommentExtensionWriter();
        int index = 0;
        while (index < comments.Length)
        {
            GifCommentExtensionWriteResult write = commentWriter.Write(writer, comments[index]);
            if (!write.Success)
            {
                return write.Error;
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateOptions(GifEncodeOptions options)
    {
        return ValidateComments(ResolveOptionComments(options));
    }

    internal GifErrorDiagnostic ValidateComments(GifCommentExtension[]? comments)
    {
        if (comments == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        int index = 0;
        while (index < comments.Length)
        {
            if (comments[index] == null)
            {
                return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Comment extension list contains a missing comment.", new GifDiagnosticPosition(-1, -1, "CommentExtension"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifCommentExtension[] ResolveOptionComments(GifEncodeOptions options)
    {
        if (options == null)
        {
            return new GifCommentExtension[0];
        }

        if (options.Comments == null)
        {
            return new GifCommentExtension[0];
        }

        return options.Comments;
    }

    internal GifCommentExtension[] ResolveDocumentComments(GifDocument document)
    {
        if (document == null)
        {
            return new GifCommentExtension[0];
        }

        if (document.Comments != null)
        {
            if (document.Comments.Length > 0)
            {
                return document.Comments;
            }
        }

        if (document.Metadata != null)
        {
            if (document.Metadata.Comments != null)
            {
                return document.Metadata.Comments;
            }
        }

        return new GifCommentExtension[0];
    }

    internal GifCommentExtension[] AppendComment(GifCommentExtension[] items, GifCommentExtension item)
    {
        if (items == null)
        {
            items = new GifCommentExtension[0];
        }

        GifCommentExtension[] result = new GifCommentExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }
}

internal class GifApplicationExtensionEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, GifEncodeOptions options)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for application extension encoding.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
        }

        GifErrorDiagnostic validation = ValidateOptions(options);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        GifApplicationExtension[] applications = ResolveOptionApplications(options);
        int index = 0;
        while (index < applications.Length)
        {
            document.ApplicationExtensions = AppendApplication(document.ApplicationExtensions, applications[index]);
            if (document.Metadata == null)
            {
                document.Metadata = new GifMetadata();
            }

            document.Metadata.ApplicationExtensions = AppendApplication(document.Metadata.ApplicationExtensions, applications[index]);
            ApplyLoopMetadata(document, applications[index]);
            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic Write(IGifByteWriter writer, GifDocument document)
    {
        if (writer == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing GIF writer for application extension encoding.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
        }

        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for application extension encoding.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ApplicationExtension"));
        }

        GifApplicationExtension[] applications = ResolveDocumentApplications(document);
        GifErrorDiagnostic validation = ValidateApplications(applications);
        if (validation.Code != GifErrorCode.None)
        {
            return new GifErrorDiagnostic(validation.Code, validation.Message, new GifDiagnosticPosition(writer.GetByteOffset(), -1, "ApplicationExtension"));
        }

        GifApplicationExtensionWriter applicationWriter = new GifApplicationExtensionWriter();
        int index = 0;
        while (index < applications.Length)
        {
            if (!ShouldSkipGeneratedLoopApplication(document, applications[index]))
            {
                GifApplicationExtensionWriteResult write = applicationWriter.Write(writer, applications[index]);
                if (!write.Success)
                {
                    return write.Error;
                }
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateOptions(GifEncodeOptions options)
    {
        return ValidateApplications(ResolveOptionApplications(options));
    }

    internal GifErrorDiagnostic ValidateApplications(GifApplicationExtension[] applications)
    {
        if (applications == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        int index = 0;
        while (index < applications.Length)
        {
            if (!writer.CanEncode(applications[index]))
            {
                return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Application extension list contains an unencodable application.", new GifDiagnosticPosition(-1, -1, "ApplicationExtension"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifApplicationExtension[] ResolveOptionApplications(GifEncodeOptions options)
    {
        if (options == null)
        {
            return new GifApplicationExtension[0];
        }

        if (options.ApplicationExtensions == null)
        {
            return new GifApplicationExtension[0];
        }

        return options.ApplicationExtensions;
    }

    internal GifApplicationExtension[] ResolveDocumentApplications(GifDocument document)
    {
        if (document == null)
        {
            return new GifApplicationExtension[0];
        }

        if (document.ApplicationExtensions != null)
        {
            if (document.ApplicationExtensions.Length > 0)
            {
                return document.ApplicationExtensions;
            }
        }

        if (document.Metadata != null)
        {
            if (document.Metadata.ApplicationExtensions != null)
            {
                return document.Metadata.ApplicationExtensions;
            }
        }

        return new GifApplicationExtension[0];
    }

    internal bool ShouldSkipGeneratedLoopApplication(GifDocument document, GifApplicationExtension application)
    {
        if (document == null || application == null)
        {
            return false;
        }

        if (document.LoopCount < 0)
        {
            return false;
        }

        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        if (!writer.IsNetscapeLoopApplication(application))
        {
            return false;
        }

        return application.LoopCount == document.LoopCount;
    }

    internal void ApplyLoopMetadata(GifDocument document, GifApplicationExtension application)
    {
        if (document == null || application == null)
        {
            return;
        }

        GifApplicationExtensionWriter writer = new GifApplicationExtensionWriter();
        if (!writer.IsNetscapeLoopApplication(application))
        {
            return;
        }

        if (application.LoopCount < 0)
        {
            return;
        }

        document.LoopCount = application.LoopCount;
        if (document.Metadata == null)
        {
            document.Metadata = new GifMetadata();
        }

        document.Metadata.LoopCount = application.LoopCount;
    }

    internal GifApplicationExtension[] AppendApplication(GifApplicationExtension[] items, GifApplicationExtension item)
    {
        if (items == null)
        {
            items = new GifApplicationExtension[0];
        }

        GifApplicationExtension[] result = new GifApplicationExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }
}

internal class GifPlainTextExtensionEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, GifEncodeOptions options)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for plain text extension encoding.", new GifDiagnosticPosition(-1, -1, "PlainTextExtension"));
        }

        GifErrorDiagnostic validation = ValidateOptions(options);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        GifPlainTextExtension[] plainTexts = ResolveOptionPlainTexts(options);
        int index = 0;
        while (index < plainTexts.Length)
        {
            document.PlainTextExtensions = AppendPlainText(document.PlainTextExtensions, plainTexts[index]);
            if (document.Metadata == null)
            {
                document.Metadata = new GifMetadata();
            }

            document.Metadata.PlainTextExtensions = AppendPlainText(document.Metadata.PlainTextExtensions, plainTexts[index]);
            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic Write(IGifByteWriter writer, GifDocument document)
    {
        if (writer == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing GIF writer for plain text extension encoding.", new GifDiagnosticPosition(-1, -1, "PlainTextExtension"));
        }

        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for plain text extension encoding.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "PlainTextExtension"));
        }

        GifPlainTextExtension[] plainTexts = ResolveDocumentPlainTexts(document);
        GifErrorDiagnostic validation = ValidatePlainTexts(plainTexts);
        if (validation.Code != GifErrorCode.None)
        {
            return new GifErrorDiagnostic(validation.Code, validation.Message, new GifDiagnosticPosition(writer.GetByteOffset(), -1, "PlainTextExtension"));
        }

        GifPlainTextExtensionWriter plainTextWriter = new GifPlainTextExtensionWriter();
        int index = 0;
        while (index < plainTexts.Length)
        {
            GifPlainTextExtensionWriteResult write = plainTextWriter.Write(writer, plainTexts[index]);
            if (!write.Success)
            {
                return write.Error;
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateOptions(GifEncodeOptions options)
    {
        return ValidatePlainTexts(ResolveOptionPlainTexts(options));
    }

    internal GifErrorDiagnostic ValidatePlainTexts(GifPlainTextExtension[] plainTexts)
    {
        if (plainTexts == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        GifPlainTextExtensionWriter writer = new GifPlainTextExtensionWriter();
        int index = 0;
        while (index < plainTexts.Length)
        {
            if (!writer.CanEncode(plainTexts[index]))
            {
                return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Plain text extension list contains an unencodable plain text extension.", new GifDiagnosticPosition(-1, -1, "PlainTextExtension"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifPlainTextExtension[] ResolveOptionPlainTexts(GifEncodeOptions options)
    {
        if (options == null)
        {
            return new GifPlainTextExtension[0];
        }

        if (options.PlainTextExtensions == null)
        {
            return new GifPlainTextExtension[0];
        }

        return options.PlainTextExtensions;
    }

    internal GifPlainTextExtension[] ResolveDocumentPlainTexts(GifDocument document)
    {
        if (document == null)
        {
            return new GifPlainTextExtension[0];
        }

        if (document.PlainTextExtensions != null)
        {
            if (document.PlainTextExtensions.Length > 0)
            {
                return document.PlainTextExtensions;
            }
        }

        if (document.Metadata != null)
        {
            if (document.Metadata.PlainTextExtensions != null)
            {
                return document.Metadata.PlainTextExtensions;
            }
        }

        return new GifPlainTextExtension[0];
    }

    internal GifPlainTextExtension[] AppendPlainText(GifPlainTextExtension[] items, GifPlainTextExtension item)
    {
        if (items == null)
        {
            items = new GifPlainTextExtension[0];
        }

        GifPlainTextExtension[] result = new GifPlainTextExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }
}

internal class GifUnknownExtensionEncoder
{
    internal GifErrorDiagnostic Apply(GifDocument document, GifEncodeOptions options)
    {
        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for unknown extension encoding.", new GifDiagnosticPosition(-1, -1, "UnknownExtension"));
        }

        GifErrorDiagnostic validation = ValidateOptions(options);
        if (validation.Code != GifErrorCode.None)
        {
            return validation;
        }

        GifUnknownExtension[] unknownExtensions = ResolveOptionUnknownExtensions(options);
        int index = 0;
        while (index < unknownExtensions.Length)
        {
            document.UnknownExtensions = AppendUnknown(document.UnknownExtensions, unknownExtensions[index]);
            if (document.Metadata == null)
            {
                document.Metadata = new GifMetadata();
            }

            document.Metadata.UnknownExtensions = AppendUnknown(document.Metadata.UnknownExtensions, unknownExtensions[index]);
            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic Write(IGifByteWriter writer, GifDocument document)
    {
        if (writer == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing GIF writer for unknown extension encoding.", new GifDiagnosticPosition(-1, -1, "UnknownExtension"));
        }

        if (document == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, "GIF document is missing for unknown extension encoding.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "UnknownExtension"));
        }

        GifUnknownExtension[] unknownExtensions = ResolveDocumentUnknownExtensions(document);
        GifErrorDiagnostic validation = ValidateUnknownExtensions(unknownExtensions);
        if (validation.Code != GifErrorCode.None)
        {
            return new GifErrorDiagnostic(validation.Code, validation.Message, new GifDiagnosticPosition(writer.GetByteOffset(), -1, "UnknownExtension"));
        }

        GifUnknownExtensionWriter unknownWriter = new GifUnknownExtensionWriter();
        int index = 0;
        while (index < unknownExtensions.Length)
        {
            GifUnknownExtensionWriteResult write = unknownWriter.Write(writer, unknownExtensions[index]);
            if (!write.Success)
            {
                return write.Error;
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifErrorDiagnostic ValidateOptions(GifEncodeOptions options)
    {
        return ValidateUnknownExtensions(ResolveOptionUnknownExtensions(options));
    }

    internal GifErrorDiagnostic ValidateUnknownExtensions(GifUnknownExtension[] unknownExtensions)
    {
        if (unknownExtensions == null)
        {
            return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
        }

        GifUnknownExtensionWriter writer = new GifUnknownExtensionWriter();
        int index = 0;
        while (index < unknownExtensions.Length)
        {
            if (!writer.CanEncode(unknownExtensions[index]))
            {
                return new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "Unknown extension list contains an unencodable unknown extension.", new GifDiagnosticPosition(-1, -1, "UnknownExtension"));
            }

            index = index + 1;
        }

        return new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }

    internal GifUnknownExtension[] ResolveOptionUnknownExtensions(GifEncodeOptions options)
    {
        if (options == null)
        {
            return new GifUnknownExtension[0];
        }

        if (options.UnknownExtensions == null)
        {
            return new GifUnknownExtension[0];
        }

        return options.UnknownExtensions;
    }

    internal GifUnknownExtension[] ResolveDocumentUnknownExtensions(GifDocument document)
    {
        if (document == null)
        {
            return new GifUnknownExtension[0];
        }

        if (document.UnknownExtensions != null)
        {
            if (document.UnknownExtensions.Length > 0)
            {
                return document.UnknownExtensions;
            }
        }

        if (document.Metadata != null)
        {
            if (document.Metadata.UnknownExtensions != null)
            {
                return document.Metadata.UnknownExtensions;
            }
        }

        return new GifUnknownExtension[0];
    }

    internal GifUnknownExtension[] AppendUnknown(GifUnknownExtension[] items, GifUnknownExtension item)
    {
        if (items == null)
        {
            items = new GifUnknownExtension[0];
        }

        GifUnknownExtension[] result = new GifUnknownExtension[items.Length + 1];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index];
            index = index + 1;
        }

        result[items.Length] = item;
        return result;
    }
}
