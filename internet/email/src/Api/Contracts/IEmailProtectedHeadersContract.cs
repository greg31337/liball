using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProtectedHeaders"/>.
/// </summary>
public interface IEmailProtectedHeadersContract
{
    /// <summary>
    /// Constructs the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "intent">The intent used by the operation.</param>
    /// <returns>The protected header construction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    EmailProtectedHeaderConstructionResult Construct(EmailMessage message, EmailProtectedHeaderIntent intent);
    /// <summary>
    /// Extracts the supplied value.
    /// </summary>
    /// <param name = "actualOuterHeaders">The actual outer headers used by the operation.</param>
    /// <param name = "authenticatedPayload">The authenticated payload used by the operation.</param>
    /// <param name = "securityContext">The security context used by the operation.</param>
    /// <returns>The protected header extraction result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailProtectedHeaderExtractionResult Extract(EmailHeaders actualOuterHeaders, MimeEntity authenticatedPayload, EmailProtectedHeaderSecurityContext securityContext);
}
