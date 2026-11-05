using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTokenReader"/>.
/// </summary>
public interface IEmailTokenReaderOperationsContract
{
    /// <summary>Creates an unstarted token reader using default policy.</summary>
    /// <returns>The configured token reader.</returns>
    EmailTokenReader CreateWith();
    /// <summary>
    /// Creates a new token reader.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured token reader.</returns>
    EmailTokenReader CreateWithOptions(EmailParseOptions options);
    /// <summary>
    /// Creates a token reader and starts it with the supplied bytes.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The started token reader.</returns>
    EmailTokenReader CreateWithInputUsingValueArray(byte[] input);
    /// <summary>
    /// Creates a token reader and starts it with the supplied memory.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The started token reader.</returns>
    EmailTokenReader CreateWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input);
    /// <summary>
    /// Creates a token reader and starts it with the supplied stream.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The started token reader.</returns>
    EmailTokenReader CreateWithInputUsingStream(Stream input);
    /// <summary>
    /// Creates a token reader with a policy and starts it with the supplied bytes.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The started token reader.</returns>
    EmailTokenReader CreateWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options);
    /// <summary>
    /// Creates a token reader with a policy and starts it with the supplied memory.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The started token reader.</returns>
    EmailTokenReader CreateWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options);
    /// <summary>
    /// Creates a token reader with a policy and starts it with the supplied stream.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The started token reader.</returns>
    EmailTokenReader CreateWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options);
}
