using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with body selector.
/// </summary>
public class EmailBodySelector : IEmailBodySelectorContract
{
    internal static EmailBodySelection SelectImplementationWithRoot(MimeEntity root)
    {
        return ChooseImplementation(root, EmailBodyPreference.Html, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithRoot(MimeEntity root)
    {
        return EmailBodySelector.SelectImplementationWithRoot(root);
    }

    internal static EmailBodySelection SelectImplementationWithRootAndPreference(MimeEntity root, EmailBodyPreference preference)
    {
        return ChooseImplementation(root, preference, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithRootAndPreference(MimeEntity root, EmailBodyPreference preference)
    {
        return EmailBodySelector.SelectImplementationWithRootAndPreference(root, preference);
    }

    internal static EmailBodySelection SelectImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity root, int maximumDepth, int maximumEntityCount)
    {
        return ChooseImplementation(root, EmailBodyPreference.Html, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity root, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.SelectImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static EmailBodySelection SelectImplementationWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(MimeEntity root, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount)
    {
        return ChooseImplementation(root, preference, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(MimeEntity root, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.SelectImplementationWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(root, preference, maximumDepth, maximumEntityCount);
    }

    internal static EmailBodySelection ChooseImplementation(MimeEntity? root, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount)
    {
        ValidatePreference(preference);
        new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
        EmailBodySelectorWorker worker = new EmailBodySelectorWorker(preference);
        return worker.Choose(root);
    }

    /// <summary>
    /// Chooses the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection Choose(MimeEntity? root, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.ChooseImplementation(root, preference, maximumDepth, maximumEntityCount);
    }

    internal static EmailBodySelection SelectImplementationWithMessage(EmailMessage message)
    {
        return ChooseImplementation(GetMessageRoot(message), EmailBodyPreference.Html, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithMessage(EmailMessage message)
    {
        return EmailBodySelector.SelectImplementationWithMessage(message);
    }

    internal static EmailBodySelection SelectImplementationWithMessageAndPreference(EmailMessage message, EmailBodyPreference preference)
    {
        return ChooseImplementation(GetMessageRoot(message), preference, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithMessageAndPreference(EmailMessage message, EmailBodyPreference preference)
    {
        return EmailBodySelector.SelectImplementationWithMessageAndPreference(message, preference);
    }

    internal static EmailBodySelection SelectImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return ChooseImplementation(GetMessageRoot(message), EmailBodyPreference.Html, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.SelectImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static EmailBodySelection SelectImplementationWithMessageAndPreferenceAndMaximumDepthAndMaximumEntityCount(EmailMessage message, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount)
    {
        return ChooseImplementation(GetMessageRoot(message), preference, maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    public EmailBodySelection SelectWithMessageAndPreferenceAndMaximumDepthAndMaximumEntityCount(EmailMessage message, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.SelectImplementationWithMessageAndPreferenceAndMaximumDepthAndMaximumEntityCount(message, preference, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetPlainTextImplementationWithRoot(MimeEntity? root)
    {
        return GetPlainTextImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current plain text.</returns>
    public MimeEntity? GetPlainTextWithRoot(MimeEntity? root)
    {
        return EmailBodySelector.GetPlainTextImplementationWithRoot(root);
    }

    internal static MimeEntity? GetPlainTextImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        EmailBodySelection selection = ChooseImplementation(root, EmailBodyPreference.PlainText, maximumDepth, maximumEntityCount);
        return selection.HasBody() && ((selection.GetBody() ?? throw new InvalidOperationException()).GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "plain") ? selection.GetBody() : null;
    }

    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current plain text.</returns>
    public MimeEntity? GetPlainTextWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.GetPlainTextImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetPlainTextImplementationWithMessage(EmailMessage message)
    {
        return GetPlainTextImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current plain text.</returns>
    public MimeEntity? GetPlainTextWithMessage(EmailMessage message)
    {
        return EmailBodySelector.GetPlainTextImplementationWithMessage(message);
    }

    internal static MimeEntity? GetPlainTextImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return GetPlainTextImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current plain text.</returns>
    public MimeEntity? GetPlainTextWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.GetPlainTextImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetHtmlImplementationWithRoot(MimeEntity? root)
    {
        return GetHtmlImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current HTML.</returns>
    public MimeEntity? GetHtmlWithRoot(MimeEntity? root)
    {
        return EmailBodySelector.GetHtmlImplementationWithRoot(root);
    }

    internal static MimeEntity? GetHtmlImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        EmailBodySelection selection = ChooseImplementation(root, EmailBodyPreference.Html, maximumDepth, maximumEntityCount);
        return selection.HasBody() && ((selection.GetBody() ?? throw new InvalidOperationException()).GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html") ? selection.GetBody() : null;
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current HTML.</returns>
    public MimeEntity? GetHtmlWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.GetHtmlImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static MimeEntity? GetHtmlImplementationWithMessage(EmailMessage message)
    {
        return GetHtmlImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current HTML.</returns>
    public MimeEntity? GetHtmlWithMessage(EmailMessage message)
    {
        return EmailBodySelector.GetHtmlImplementationWithMessage(message);
    }

    internal static MimeEntity? GetHtmlImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return GetHtmlImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current HTML.</returns>
    public MimeEntity? GetHtmlWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.GetHtmlImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> GetAlternativesImplementationWithRoot(MimeEntity? root)
    {
        return GetAlternativesImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, EmailTraversalConstants.DefaultMaximumDepth, EmailTraversalConstants.DefaultMaximumEntityCount);
    }

    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current alternatives.</returns>
    public List<MimeEntity> GetAlternativesWithRoot(MimeEntity? root)
    {
        return EmailBodySelector.GetAlternativesImplementationWithRoot(root);
    }

    internal static List<MimeEntity> GetAlternativesImplementationWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return ChooseImplementation(root, EmailBodyPreference.Html, maximumDepth, maximumEntityCount).GetAlternatives();
    }

    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current alternatives.</returns>
    public List<MimeEntity> GetAlternativesWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.GetAlternativesImplementationWithRootAndMaximumDepthAndMaximumEntityCount(root, maximumDepth, maximumEntityCount);
    }

    internal static List<MimeEntity> GetAlternativesImplementationWithMessage(EmailMessage message)
    {
        return GetAlternativesImplementationWithRoot(GetMessageRoot(message));
    }

    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current alternatives.</returns>
    public List<MimeEntity> GetAlternativesWithMessage(EmailMessage message)
    {
        return EmailBodySelector.GetAlternativesImplementationWithMessage(message);
    }

    internal static List<MimeEntity> GetAlternativesImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return GetAlternativesImplementationWithRootAndMaximumDepthAndMaximumEntityCount(GetMessageRoot(message), maximumDepth, maximumEntityCount);
    }

    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current alternatives.</returns>
    public List<MimeEntity> GetAlternativesWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount)
    {
        return EmailBodySelector.GetAlternativesImplementationWithMessageAndMaximumDepthAndMaximumEntityCount(message, maximumDepth, maximumEntityCount);
    }

    static MimeEntity? GetMessageRoot(EmailMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (!message.HasBody())
        {
            throw new ArgumentException("A message without a MIME body has no selectable body.", "message");
        }

        return message.GetBody();
    }

    static void ValidatePreference(EmailBodyPreference preference)
    {
        if (preference < EmailBodyPreference.Html || preference > EmailBodyPreference.PlainText)
        {
            throw new ArgumentOutOfRangeException("preference");
        }
    }
}
