using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailResentBuilder"/>.
/// </summary>
public class EmailResentBuilderOperations : IEmailResentBuilderOperationsContract
{
    /// <summary>Creates a resent builder using default policy.</summary>
    /// <param name = "original">The original message.</param>
    /// <returns>The configured resent builder.</returns>
    public EmailResentBuilder CreateWithOriginal(EmailMessage original)
    {
        return EmailResentBuilder.CreateImplementationWithOriginal(original);
    }

    /// <summary>
    /// Creates a new resent builder.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured resent builder.</returns>
    public EmailResentBuilder CreateWithOriginalAndOptions(EmailMessage original, EmailResentOptions options)
    {
        return EmailResentBuilder.CreateImplementationWithOriginalAndOptions(original, options);
    }
}
