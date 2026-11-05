using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAttachmentFinder"/>.
/// </summary>
public interface IEmailAttachmentFinderContract
{
    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> EnumerateWithRoot(MimeEntity? root);
    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> EnumerateWithMessage(EmailMessage message);
    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> EnumerateWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> FindWithRoot(MimeEntity? root);
    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> FindWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> FindWithMessage(EmailMessage message);
    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    List<EmailAttachmentView> FindWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the bodies.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current bodies.</returns>
    List<EmailAttachmentView> GetBodiesWithRoot(MimeEntity? root);
    /// <summary>
    /// Gets the inline resources.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current inline resources.</returns>
    List<EmailAttachmentView> GetInlineResourcesWithRoot(MimeEntity? root);
    /// <summary>
    /// Gets the nested messages.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current nested messages.</returns>
    List<EmailAttachmentView> GetNestedMessagesWithRoot(MimeEntity? root);
    /// <summary>
    /// Gets the cryptographic parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current cryptographic parts.</returns>
    List<EmailAttachmentView> GetCryptographicPartsWithRoot(MimeEntity? root);
    /// <summary>
    /// Gets the bodies.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current bodies.</returns>
    List<EmailAttachmentView> GetBodiesWithMessage(EmailMessage message);
    /// <summary>
    /// Gets the inline resources.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current inline resources.</returns>
    List<EmailAttachmentView> GetInlineResourcesWithMessage(EmailMessage message);
    /// <summary>
    /// Gets the nested messages.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current nested messages.</returns>
    List<EmailAttachmentView> GetNestedMessagesWithMessage(EmailMessage message);
    /// <summary>
    /// Gets the cryptographic parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current cryptographic parts.</returns>
    List<EmailAttachmentView> GetCryptographicPartsWithMessage(EmailMessage message);
}
