namespace gif;

/// <summary>Defines GIF validation, metadata editing, and animation transformation operations.</summary>
public interface IGifTransformer
{
    /// <summary>Deconstructs full-canvas animation frames into changed rectangles.</summary>
    /// <param name="animation">The source animation.</param>
    /// <returns>The deconstructed animation.</returns>
    /// <exception cref="GifException">Thrown when deconstruction produces a fatal diagnostic.</exception>
    GifAnimation Deconstruct(GifAnimation? animation);

    /// <summary>Validates a raw GIF document.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">Encode constraints; <c>null</c> uses defaults.</param>
    /// <returns>The first fatal diagnostic, or a no-error diagnostic.</returns>
    GifErrorDiagnostic ValidateDocument(GifDocument? document, GifEncodeOptions? options);

    /// <summary>Validates a bitmap for GIF encoding.</summary>
    /// <param name="bitmap">The bitmap to validate.</param>
    /// <param name="options">Encode constraints; <c>null</c> uses defaults.</param>
    /// <returns>The first fatal diagnostic, or a no-error diagnostic.</returns>
    GifErrorDiagnostic ValidateBitmap(GifBitmap? bitmap, GifEncodeOptions? options);

    /// <summary>Validates an animation for GIF encoding.</summary>
    /// <param name="animation">The animation to validate.</param>
    /// <param name="options">Encode constraints; <c>null</c> uses defaults.</param>
    /// <returns>The first fatal diagnostic, or a no-error diagnostic.</returns>
    GifErrorDiagnostic ValidateAnimation(GifAnimation? animation, GifEncodeOptions? options);

    /// <summary>Converts a GIF delay in hundredths of a second to milliseconds.</summary>
    /// <param name="delayHundredths">A delay from 0 through 65535 hundredths.</param>
    /// <returns>The exact delay in milliseconds.</returns>
    /// <exception cref="GifException">Thrown when the delay is negative, outside the GIF range, or would overflow the millisecond result.</exception>
    int ConvertDelayHundredthsToMilliseconds(int delayHundredths);

    /// <summary>Converts milliseconds to GIF hundredths of a second using explicit rounding.</summary>
    /// <param name="delayMilliseconds">A nonnegative delay in milliseconds.</param>
    /// <param name="roundingMode">The rounding rule applied at 10-millisecond boundaries.</param>
    /// <returns>The rounded GIF delay in hundredths.</returns>
    /// <exception cref="GifException">Thrown when the input, rounding mode, or rounded GIF delay is invalid.</exception>
    int ConvertDelayMillisecondsToHundredths(int delayMilliseconds, GifDelayRoundingMode roundingMode);

    /// <summary>Sets one delay on every animation frame.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="delayMilliseconds">The frame delay in milliseconds.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or delay is invalid.</exception>
    GifAnimation SetDelay(GifAnimation? animation, int delayMilliseconds);

    /// <summary>Sets individual animation-frame delays.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="delaysMilliseconds">One delay per frame.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or a delay is invalid.</exception>
    GifAnimation SetDelays(GifAnimation? animation, int[]? delaysMilliseconds);

    /// <summary>Sets one disposal method on every animation frame.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="disposalMethod">The disposal method.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or disposal method is invalid.</exception>
    GifAnimation SetDisposal(GifAnimation? animation, GifDisposalMethod disposalMethod);

    /// <summary>Sets individual animation-frame disposal methods.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="disposalMethods">One disposal method per frame.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or a disposal method is invalid.</exception>
    GifAnimation SetDisposals(GifAnimation? animation, GifDisposalMethod[]? disposalMethods);

    /// <summary>Sets the loop count on an animation.</summary>
    /// <param name="animation">The animation to edit.</param>
    /// <param name="loopCount">The loop count.</param>
    /// <returns>The edited animation.</returns>
    /// <exception cref="GifException">Thrown when the animation or loop count is invalid.</exception>
    GifAnimation SetAnimationLoopCount(GifAnimation? animation, int loopCount);

    /// <summary>Sets the loop count on a raw document.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="loopCount">The loop count.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or loop count is invalid.</exception>
    GifDocument SetDocumentLoopCount(GifDocument? document, int loopCount);

    /// <summary>Replaces document comments from strings.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comments">The comment text values.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document is missing.</exception>
    GifDocument SetCommentsFromText(GifDocument? document, string[]? comments);

    /// <summary>Replaces structured document comments.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comments">The structured comments.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or comments are invalid.</exception>
    GifDocument SetComments(GifDocument? document, GifCommentExtension[]? comments);

    /// <summary>Sets one global palette on a raw document.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="palette">The global palette.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or palette is invalid.</exception>
    GifDocument SetPalette(GifDocument? document, GifPalette palette);

    /// <summary>Sets one local palette for each raw frame.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="palettes">One palette per frame.</param>
    /// <returns>The edited document.</returns>
    /// <exception cref="GifException">Thrown when the document or palettes are invalid.</exception>
    GifDocument SetPalettes(GifDocument? document, GifPalette[] palettes);

    /// <summary>Optimizes an animation using caller-selected encode settings.</summary>
    /// <param name="animation">The animation to optimize.</param>
    /// <param name="options">Optimization settings; <c>null</c> uses defaults.</param>
    /// <returns>The optimized animation.</returns>
    /// <exception cref="GifException">Thrown when optimization produces a fatal diagnostic.</exception>
    GifAnimation Optimize(GifAnimation? animation, GifEncodeOptions? options);
}
