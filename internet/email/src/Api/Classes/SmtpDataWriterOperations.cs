using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "SmtpDataWriter"/>.
/// </summary>
public class SmtpDataWriterOperations : ISmtpDataWriterOperationsContract
{
    /// <summary>Creates an SMTP DATA writer backed by a writable stream.</summary>
    /// <param name = "output">The destination stream.</param>
    /// <returns>The configured SMTP DATA writer.</returns>
    public SmtpDataWriter CreateWithOutputUsingStream(Stream output)
    {
        return SmtpDataWriter.CreateImplementationWithOutputUsingStream(output);
    }

    /// <summary>
    /// Creates a new SMTP data writer.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured SMTP DATA writer.</returns>
    public SmtpDataWriter CreateWithOutputUsingEmailCountingStream(EmailCountingStream output)
    {
        return SmtpDataWriter.CreateImplementationWithOutputUsingEmailCountingStream(output);
    }

    /// <summary>
    /// Creates an SMTP DATA writer backed by a hashing stream.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured SMTP DATA writer.</returns>
    public SmtpDataWriter CreateWithOutputUsingEmailHashingStream(EmailHashingStream output)
    {
        return SmtpDataWriter.CreateImplementationWithOutputUsingEmailHashingStream(output);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] Format(ReadOnlySpan<byte> input)
    {
        return SmtpDataWriter.FormatImplementation(input);
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The SMTP data write result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public SmtpDataWriteResult Write(Stream input, Stream output)
    {
        return SmtpDataWriter.WriteImplementation(input, output);
    }
}
