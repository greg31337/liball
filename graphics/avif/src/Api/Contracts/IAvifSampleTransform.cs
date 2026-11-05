using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifSampleTransform"/>.</summary>
public interface IAvifSampleTransform
{
    /// <summary>Returns the payload parse and expression validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the number of ordered input images.</summary>
    /// <returns>The input count from one through 32.</returns>
    int GetInputCount();

    /// <summary>Returns the signed intermediate arithmetic precision.</summary>
    /// <returns>Eight, 16, 32, or 64 bits.</returns>
    int GetIntermediateBitDepth();

    /// <summary>Returns the postfix token count.</summary>
    /// <returns>The validated token count.</returns>
    int GetTokenCount();

    /// <summary>Returns a copy of the complete aligned expression payload.</summary>
    /// <returns>The validated <c>sato</c> item payload.</returns>
    byte[] GetData();

    /// <summary>Returns the requested derived-image bit depth for authoring.</summary>
    /// <returns>Eight, 10, 12, or 16; zero for a parse-only transform.</returns>
    int GetOutputBitDepth();

    /// <summary>Returns one caller-owned authoring input image.</summary>
    /// <param name="index">The zero-based ordered input index.</param>
    /// <returns>The input image, or null when unavailable.</returns>
    AvifImage? GetInputImage(int index);

    /// <summary>Returns one postfix token.</summary>
    /// <param name="index">The zero-based token index.</param>
    /// <returns>The token, or null for an invalid index.</returns>
    AvifSampleTransformToken? GetToken(int index);

    /// <summary>Evaluates one output sample without applying output-range clamping.</summary>
    /// <param name="inputSamples">One unsigned sample value per ordered input image.</param>
    /// <param name="destination">A writable array whose first element receives the result.</param>
    /// <returns>A stable result code; the destination is unchanged on failure.</returns>
    AvifResultCode Evaluate(
        long[] inputSamples,
        long[] destination);

    /// <summary>Evaluates the expression across compatible input images.</summary>
    /// <param name="inputs">The ordered decoded input images.</param>
    /// <param name="outputBitDepth">The output depth: 8, 10, 12, or 16.</param>
    /// <returns>An owning reconstructed image, or an image carrying a stable error code.</returns>
    AvifImage Render(AvifImage[] inputs, int outputBitDepth);

    /// <summary>Evaluates the expression with explicit output allocation limits.</summary>
    /// <param name="inputs">The ordered decoded input images.</param>
    /// <param name="outputBitDepth">The output depth: 8, 10, 12, or 16.</param>
    /// <param name="limits">The output dimension, pixel, and decoded-byte limits.</param>
    /// <returns>An owning reconstructed image, or an image carrying a stable error code.</returns>
    AvifImage RenderWithLimits(
        AvifImage[] inputs,
        int outputBitDepth,
        AvifSecurityLimits limits);
}
