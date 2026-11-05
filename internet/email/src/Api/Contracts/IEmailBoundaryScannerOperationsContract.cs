using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBoundaryScanner"/>.
/// </summary>
public interface IEmailBoundaryScannerOperationsContract
{
    /// <summary>Creates a boundary scanner using default policy.</summary>
    /// <param name = "boundary">The MIME boundary.</param>
    /// <returns>The configured boundary scanner.</returns>
    EmailBoundaryScanner CreateWithBoundary(string boundary);
    /// <summary>
    /// Creates a new boundary scanner.
    /// </summary>
    /// <param name = "boundary">The boundary used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The configured boundary scanner.</returns>
    EmailBoundaryScanner CreateWithBoundaryAndOptions(string? boundary, EmailParseOptions options);
}
