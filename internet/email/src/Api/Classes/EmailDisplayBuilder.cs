using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Builds display from caller-supplied values.
/// </summary>
public class EmailDisplayBuilder : IEmailDisplayBuilderContract
{
    internal static EmailDisplayModel BuildImplementationWithRoot(MimeEntity root)
    {
        return BuildImplementationWithRootAndOptions(root, new EmailDisplayOptions());
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The display model produced by the operation.</returns>
    public EmailDisplayModel BuildWithRoot(MimeEntity root)
    {
        return EmailDisplayBuilder.BuildImplementationWithRoot(root);
    }

    internal static EmailDisplayModel BuildImplementationWithRootAndOptions(MimeEntity root, EmailDisplayOptions options)
    {
        if (root == null)
            throw new ArgumentNullException("root");
        if (options == null)
            throw new ArgumentNullException("options");
        return new EmailDisplayBuilderWorker(options.Clone()).Build(root, root.GetIssues());
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The display model produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDisplayModel BuildWithRootAndOptions(MimeEntity root, EmailDisplayOptions options)
    {
        return EmailDisplayBuilder.BuildImplementationWithRootAndOptions(root, options);
    }

    internal static EmailDisplayModel BuildImplementationWithMessage(EmailMessage message)
    {
        return BuildImplementationWithMessageAndOptions(message, new EmailDisplayOptions());
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The display model produced by the operation.</returns>
    public EmailDisplayModel BuildWithMessage(EmailMessage message)
    {
        return EmailDisplayBuilder.BuildImplementationWithMessage(message);
    }

    internal static EmailDisplayModel BuildImplementationWithMessageAndOptions(EmailMessage message, EmailDisplayOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailDisplayBuilderWorker worker = new EmailDisplayBuilderWorker(options.Clone());
        if (!message.HasBody())
            return worker.BuildEmpty(message.GetIssues());
        return worker.Build(message.GetBody(), message.GetIssues());
    }

    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The display model produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDisplayModel BuildWithMessageAndOptions(EmailMessage message, EmailDisplayOptions options)
    {
        return EmailDisplayBuilder.BuildImplementationWithMessageAndOptions(message, options);
    }
}
