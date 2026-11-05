using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailTokenReader"/>.
/// </summary>
public class EmailTokenReaderOperations : IEmailTokenReaderOperationsContract
{
    /// <summary>Creates an unstarted token reader using default policy.</summary>
    /// <returns>The configured token reader.</returns>
    public EmailTokenReader CreateWith()
    {
        return EmailTokenReader.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new token reader.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured token reader.</returns>
    public EmailTokenReader CreateWithOptions(EmailParseOptions options)
    {
        return EmailTokenReader.CreateImplementationWithOptions(options);
    }

    /// <summary>
    /// Creates a token reader and starts it with the supplied bytes.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The started token reader.</returns>
    public EmailTokenReader CreateWithInputUsingValueArray(byte[] input)
    {
        return EmailTokenReader.CreateImplementationWithInputUsingValueArray(input);
    }

    /// <summary>
    /// Creates a token reader and starts it with the supplied memory.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The started token reader.</returns>
    public EmailTokenReader CreateWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return EmailTokenReader.CreateImplementationWithInputUsingReadOnlyMemory(input);
    }

    /// <summary>
    /// Creates a token reader and starts it with the supplied stream.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The started token reader.</returns>
    public EmailTokenReader CreateWithInputUsingStream(Stream input)
    {
        return EmailTokenReader.CreateImplementationWithInputUsingStream(input);
    }

    /// <summary>
    /// Creates a token reader with a policy and starts it with the supplied bytes.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The started token reader.</returns>
    public EmailTokenReader CreateWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        return EmailTokenReader.CreateImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Creates a token reader with a policy and starts it with the supplied memory.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The started token reader.</returns>
    public EmailTokenReader CreateWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        return EmailTokenReader.CreateImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    /// <summary>
    /// Creates a token reader with a policy and starts it with the supplied stream.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The started token reader.</returns>
    public EmailTokenReader CreateWithInputAndOptionsUsingStreamAndEmailParseOptions(Stream input, EmailParseOptions options)
    {
        return EmailTokenReader.CreateImplementationWithInputAndOptionsUsingStreamAndEmailParseOptions(input, options);
    }
}
