using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailComposer"/>.
/// </summary>
public interface IEmailComposerOperationsContract
{
    /// <summary>Creates a composer using default policy.</summary>
    /// <returns>The configured composer.</returns>
    EmailComposer CreateWith();
    /// <summary>
    /// Creates a new composer.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured composer.</returns>
    EmailComposer CreateWithOptions(EmailComposerOptions options);
}
