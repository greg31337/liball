using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailEncodedWordPart"/>.
/// </summary>
public class EmailEncodedWordPartOperations : IEmailEncodedWordPartOperationsContract
{
    /// <summary>
    /// Creates a literal configured for the requested behavior.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <returns>The encoded word part produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEncodedWordPart CreateLiteral(byte[] bytes, EmailSourcePosition? sourcePosition)
    {
        return EmailEncodedWordPart.CreateLiteralImplementation(bytes, sourcePosition);
    }

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
    public EmailEncodedWordPart CreateEncodedWord(byte[] decodedBytes, byte[] rawBytes, string charset, string? language, bool languagePresent, EmailEncodedWordEncoding encoding, EmailSourcePosition sourcePosition)
    {
        return EmailEncodedWordPart.CreateEncodedWordImplementation(decodedBytes, rawBytes, charset, language, languagePresent, encoding, sourcePosition);
    }
}
