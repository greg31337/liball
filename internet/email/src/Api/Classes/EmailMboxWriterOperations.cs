using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMboxWriter"/>.
/// </summary>
public class EmailMboxWriterOperations : IEmailMboxWriterOperationsContract
{
    /// <summary>Creates an mbox writer using default policy.</summary>
    /// <param name = "output">The writable mbox stream.</param>
    /// <returns>The configured mbox writer.</returns>
    public EmailMboxWriter CreateWithOutput(Stream output)
    {
        return EmailMboxWriter.CreateImplementationWithOutput(output);
    }

    /// <summary>
    /// Creates a new mbox writer.
    /// </summary>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured mbox writer.</returns>
    public EmailMboxWriter CreateWithOutputAndOptions(Stream output, EmailMboxWriterOptions options)
    {
        return EmailMboxWriter.CreateImplementationWithOutputAndOptions(output, options);
    }

    /// <summary>
    /// Validates the supplied envelope sender against the selected policy.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "parameterName">The parameter name used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void ValidateEnvelopeSender(string value, string parameterName)
    {
        EmailMboxWriter.ValidateEnvelopeSenderImplementation(value, parameterName);
    }
}
