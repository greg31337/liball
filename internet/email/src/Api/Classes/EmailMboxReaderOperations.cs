using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMboxReader"/>.
/// </summary>
public class EmailMboxReaderOperations : IEmailMboxReaderOperationsContract
{
    /// <summary>Creates an mbox reader using default policy.</summary>
    /// <param name = "input">The readable mbox stream.</param>
    /// <returns>The configured mbox reader.</returns>
    public EmailMboxReader CreateWithInput(Stream input)
    {
        return EmailMboxReader.CreateImplementationWithInput(input);
    }

    /// <summary>
    /// Creates a new mbox reader.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured mbox reader.</returns>
    public EmailMboxReader CreateWithInputAndOptions(Stream input, EmailMboxReaderOptions options)
    {
        return EmailMboxReader.CreateImplementationWithInputAndOptions(input, options);
    }
}
