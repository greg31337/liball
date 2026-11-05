using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with attachment finder.
/// </summary>
public class EmailAttachmentFinder : IEmailAttachmentFinderContract
{
    internal static List<EmailAttachmentView> EnumerateImplementationWithRoot(MimeEntity? root)
    {
        return EnumerateImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> EnumerateWithRoot(MimeEntity? root)
    {
        return EmailAttachmentFinder.EnumerateImplementationWithRoot(root);
    }

    internal static List<EmailAttachmentView> EnumerateImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        EmailAttachmentFinderWorker worker = new EmailAttachmentFinderWorker();
        return worker.Enumerate(root);
    }

    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailAttachmentFinder.EnumerateImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static List<EmailAttachmentView> EnumerateImplementationWithMessage(EmailMessage message)
    {
        return EnumerateImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> EnumerateWithMessage(EmailMessage message)
    {
        return EmailAttachmentFinder.EnumerateImplementationWithMessage(message);
    }

    internal static List<EmailAttachmentView> EnumerateImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EnumerateImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> EnumerateWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailAttachmentFinder.EnumerateImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static List<EmailAttachmentView> FindImplementationWithRoot(MimeEntity? root)
    {
        return FindImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> FindWithRoot(MimeEntity? root)
    {
        return EmailAttachmentFinder.FindImplementationWithRoot(root);
    }

    internal static List<EmailAttachmentView> FindImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return Filter(EnumerateImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount), EmailAttachmentFilterKind.Downloadable);
    }

    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> FindWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailAttachmentFinder.FindImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static List<EmailAttachmentView> FindImplementationWithMessage(EmailMessage message)
    {
        return FindImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> FindWithMessage(EmailMessage message)
    {
        return EmailAttachmentFinder.FindImplementationWithMessage(message);
    }

    internal static List<EmailAttachmentView> FindImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return FindImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Finds matching values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailAttachmentView> FindWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailAttachmentFinder.FindImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static List<EmailAttachmentView> GetBodiesImplementationWithRoot(MimeEntity? root)
    {
        return Filter(EnumerateImplementationWithRoot(root), EmailAttachmentFilterKind.Body);
    }

    /// <summary>
    /// Gets the bodies.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current bodies.</returns>
    public List<EmailAttachmentView> GetBodiesWithRoot(MimeEntity? root)
    {
        return EmailAttachmentFinder.GetBodiesImplementationWithRoot(root);
    }

    internal static List<EmailAttachmentView> GetInlineResourcesImplementationWithRoot(MimeEntity? root)
    {
        return Filter(EnumerateImplementationWithRoot(root), EmailAttachmentFilterKind.InlineResource);
    }

    /// <summary>
    /// Gets the inline resources.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current inline resources.</returns>
    public List<EmailAttachmentView> GetInlineResourcesWithRoot(MimeEntity? root)
    {
        return EmailAttachmentFinder.GetInlineResourcesImplementationWithRoot(root);
    }

    internal static List<EmailAttachmentView> GetNestedMessagesImplementationWithRoot(MimeEntity? root)
    {
        return Filter(EnumerateImplementationWithRoot(root), EmailAttachmentFilterKind.NestedMessage);
    }

    /// <summary>
    /// Gets the nested messages.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current nested messages.</returns>
    public List<EmailAttachmentView> GetNestedMessagesWithRoot(MimeEntity? root)
    {
        return EmailAttachmentFinder.GetNestedMessagesImplementationWithRoot(root);
    }

    internal static List<EmailAttachmentView> GetCryptographicPartsImplementationWithRoot(MimeEntity? root)
    {
        return Filter(EnumerateImplementationWithRoot(root), EmailAttachmentFilterKind.Cryptographic);
    }

    /// <summary>
    /// Gets the cryptographic parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current cryptographic parts.</returns>
    public List<EmailAttachmentView> GetCryptographicPartsWithRoot(MimeEntity? root)
    {
        return EmailAttachmentFinder.GetCryptographicPartsImplementationWithRoot(root);
    }

    internal static List<EmailAttachmentView> GetBodiesImplementationWithMessage(EmailMessage message)
    {
        return GetBodiesImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the bodies.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current bodies.</returns>
    public List<EmailAttachmentView> GetBodiesWithMessage(EmailMessage message)
    {
        return EmailAttachmentFinder.GetBodiesImplementationWithMessage(message);
    }

    internal static List<EmailAttachmentView> GetInlineResourcesImplementationWithMessage(EmailMessage message)
    {
        return GetInlineResourcesImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the inline resources.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current inline resources.</returns>
    public List<EmailAttachmentView> GetInlineResourcesWithMessage(EmailMessage message)
    {
        return EmailAttachmentFinder.GetInlineResourcesImplementationWithMessage(message);
    }

    internal static List<EmailAttachmentView> GetNestedMessagesImplementationWithMessage(EmailMessage message)
    {
        return GetNestedMessagesImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the nested messages.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current nested messages.</returns>
    public List<EmailAttachmentView> GetNestedMessagesWithMessage(EmailMessage message)
    {
        return EmailAttachmentFinder.GetNestedMessagesImplementationWithMessage(message);
    }

    internal static List<EmailAttachmentView> GetCryptographicPartsImplementationWithMessage(EmailMessage message)
    {
        return GetCryptographicPartsImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the cryptographic parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current cryptographic parts.</returns>
    public List<EmailAttachmentView> GetCryptographicPartsWithMessage(EmailMessage message)
    {
        return EmailAttachmentFinder.GetCryptographicPartsImplementationWithMessage(message);
    }

    static List<EmailAttachmentView> Filter(List<EmailAttachmentView> views, EmailAttachmentFilterKind filter)
    {
        List<EmailAttachmentView> matches = new List<EmailAttachmentView>();
        int index = 0;
        while (index < views.Count)
        {
            EmailAttachmentView view = views[index];
            bool include = filter == EmailAttachmentFilterKind.Downloadable && view.IsDownloadable();
            if (filter == EmailAttachmentFilterKind.Body && view.IsBody())
            {
                include = true;
            }
            else if (filter == EmailAttachmentFilterKind.InlineResource && view.IsInlineResource())
            {
                include = true;
            }
            else if (filter == EmailAttachmentFilterKind.NestedMessage && view.IsNestedMessage())
            {
                include = true;
            }
            else if (filter == EmailAttachmentFilterKind.Cryptographic && view.IsCryptographicPart())
            {
                include = true;
            }

            if (include)
            {
                matches.Add(view);
            }

            index++;
        }

        return matches;
    }

    static MimeEntity? GetMessageRoot(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (!message.HasBody())
        {
            throw new ArgumentException("A message without a MIME body has no attachment projection.", "message");
        }

        return message.GetBody();
    }
}
