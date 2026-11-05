using System.Collections.Generic;

namespace http;

/// <summary>
/// Applies HTTP content codings to representation data.
/// </summary>
public interface IHttpContentCoding
{
    /// <summary>
    /// Encodes content using the configured coding operation.
    /// </summary>
    /// <param name="content">The content bytes to encode.</param>
    /// <param name="coding">The content-coding token.</param>
    /// <param name="maximumOutputSize">The maximum transformed size.</param>
    /// <returns>The content-coding operation result.</returns>
    HttpContentCodingResult EncodeContent(byte[] content, string coding, int maximumOutputSize);

    /// <summary>Decodes content using one coding.</summary>
    /// <param name="content">The coded content bytes.</param>
    /// <param name="coding">The content-coding token.</param>
    /// <param name="maximumOutputSize">The maximum transformed size.</param>
    /// <returns>The decoding result.</returns>
    HttpContentCodingResult DecodeContent(byte[] content, string coding, int maximumOutputSize);

    /// <summary>Applies an ordered stack of content codings.</summary>
    /// <param name="content">The original content bytes.</param>
    /// <param name="codings">The codings in application order.</param>
    /// <param name="maximumOutputSize">The maximum transformed size.</param>
    /// <returns>The encoding result.</returns>
    HttpContentCodingResult EncodeCodingStack(
        byte[] content,
        List<string> codings,
        int maximumOutputSize);

    /// <summary>Removes an ordered stack of content codings.</summary>
    /// <param name="content">The coded content bytes.</param>
    /// <param name="codings">The codings in application order.</param>
    /// <param name="maximumOutputSize">The maximum transformed size.</param>
    /// <returns>The decoding result.</returns>
    HttpContentCodingResult DecodeCodingStack(
        byte[] content,
        List<string> codings,
        int maximumOutputSize);
}
