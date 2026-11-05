using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBoundaryGenerator"/>.
/// </summary>
public interface IEmailBoundaryGeneratorContract
{
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <returns>The boundary generation result produced by the operation.</returns>
    EmailBoundaryGenerationResult GenerateWith();
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The boundary generation result produced by the operation.</returns>
    EmailBoundaryGenerationResult GenerateWithOptions(EmailWriteOptions options);
    /// <summary>
    /// Generates the supplied value.
    /// </summary>
    /// <param name = "contents">The contents used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The boundary generation result produced by the operation.</returns>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailBoundaryGenerationResult GenerateWithContentsAndOptions(List<EmailContent> contents, EmailWriteOptions options);
    /// <summary>
    /// Creates a candidate configured for the requested behavior.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <param name = "attempt">The attempt used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string CreateCandidate(EmailWriteOptions options, int attempt);
}
