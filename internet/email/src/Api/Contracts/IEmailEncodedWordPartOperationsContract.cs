using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodedWordPart"/>.
/// </summary>
public interface IEmailEncodedWordPartOperationsContract
{
    /// <summary>
    /// Creates a literal configured for the requested behavior.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <returns>The encoded word part produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailEncodedWordPart CreateLiteral(byte[] bytes, EmailSourcePosition? sourcePosition);
    /// <summary>
    /// Creates an encoded word configured for the requested behavior.
    /// </summary>
    /// <param name = "decodedBytes">The source bytes or data to process.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "charset">The charset used by the operation.</param>
    /// <param name = "language">The language used by the operation.</param>
    /// <param name = "languagePresent">Whether to enable the language present behavior.</param>
    /// <param name = "encoding">The encoding used by the operation.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <returns>The encoded word part produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailEncodedWordPart CreateEncodedWord(byte[] decodedBytes, byte[] rawBytes, string charset, string? language, bool languagePresent, EmailEncodedWordEncoding encoding, EmailSourcePosition sourcePosition);
}
