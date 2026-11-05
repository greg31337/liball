using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailResentBuilder"/>.
/// </summary>
public interface IEmailResentBuilderOperationsContract
{
    /// <summary>Creates a resent builder using default policy.</summary>
    /// <param name = "original">The original message.</param>
    /// <returns>The configured resent builder.</returns>
    EmailResentBuilder CreateWithOriginal(EmailMessage original);
    /// <summary>
    /// Creates a new resent builder.
    /// </summary>
    /// <param name = "original">The original used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured resent builder.</returns>
    EmailResentBuilder CreateWithOriginalAndOptions(EmailMessage original, EmailResentOptions options);
}
