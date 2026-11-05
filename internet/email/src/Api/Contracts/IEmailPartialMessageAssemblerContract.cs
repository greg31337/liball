using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageAssembler"/>.
/// </summary>
public interface IEmailPartialMessageAssemblerContract
{
    /// <summary>
    /// Assembles the supplied value.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    EmailPartialMessageAssemblyResult AssembleWithFragments(List<EmailMessage> fragments);
    /// <summary>
    /// Assembles the supplied value.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailPartialMessageAssemblyResult AssembleWithFragmentsAndOptions(List<EmailMessage> fragments, EmailPartialMessageAssemblyOptions options);
    /// <summary>
    /// Assembles the entities.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    EmailPartialMessageAssemblyResult AssembleEntitiesWithFragments(List<MimeEntity> fragments);
    /// <summary>
    /// Assembles the entities.
    /// </summary>
    /// <param name = "fragments">The fragments used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailPartialMessageAssemblyResult AssembleEntitiesWithFragmentsAndOptions(List<MimeEntity> fragments, EmailPartialMessageAssemblyOptions options);
}
