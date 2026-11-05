using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailBoundaryScanner"/>.
/// </summary>
public class EmailBoundaryScannerOperations : IEmailBoundaryScannerOperationsContract
{
    /// <summary>Creates a boundary scanner using default policy.</summary>
    /// <param name = "boundary">The MIME boundary.</param>
    /// <returns>The configured boundary scanner.</returns>
    public EmailBoundaryScanner CreateWithBoundary(string boundary)
    {
        return EmailBoundaryScanner.CreateImplementationWithBoundary(boundary);
    }

    /// <summary>
    /// Creates a new boundary scanner.
    /// </summary>
    /// <param name = "boundary">The boundary used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The configured boundary scanner.</returns>
    public EmailBoundaryScanner CreateWithBoundaryAndOptions(string? boundary, EmailParseOptions options)
    {
        return EmailBoundaryScanner.CreateImplementationWithBoundaryAndOptions(boundary, options);
    }
}
