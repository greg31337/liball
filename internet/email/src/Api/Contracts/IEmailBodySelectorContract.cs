using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBodySelector"/>.
/// </summary>
public interface IEmailBodySelectorContract
{
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithRoot(MimeEntity root);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithRootAndPreference(MimeEntity root, EmailBodyPreference preference);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithRootAndPreferenceAndMaximumDepthAndMaximumEntityCount(MimeEntity root, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Chooses the supplied value.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection Choose(MimeEntity? root, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithMessage(EmailMessage message);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithMessageAndPreference(EmailMessage message, EmailBodyPreference preference);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "preference">The preference used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The body selection produced by the operation.</returns>
    EmailBodySelection SelectWithMessageAndPreferenceAndMaximumDepthAndMaximumEntityCount(EmailMessage message, EmailBodyPreference preference, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current plain text.</returns>
    MimeEntity? GetPlainTextWithRoot(MimeEntity? root);
    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current plain text.</returns>
    MimeEntity? GetPlainTextWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current plain text.</returns>
    MimeEntity? GetPlainTextWithMessage(EmailMessage message);
    /// <summary>
    /// Gets the plain text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current plain text.</returns>
    MimeEntity? GetPlainTextWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current HTML.</returns>
    MimeEntity? GetHtmlWithRoot(MimeEntity? root);
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "root">The root used by the operation.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current HTML.</returns>
    MimeEntity? GetHtmlWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current HTML.</returns>
    MimeEntity? GetHtmlWithMessage(EmailMessage message);
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "maximumDepth">The maximum depth used by the operation.</param>
    /// <param name = "maximumEntityCount">The number of items or bytes to process.</param>
    /// <returns>The current HTML.</returns>
    MimeEntity? GetHtmlWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "root">The root used by the operation.</param>
    /// <returns>The current alternatives.</returns>
    List<MimeEntity> GetAlternativesWithRoot(MimeEntity? root);
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
    List<MimeEntity> GetAlternativesWithRootAndMaximumDepthAndMaximumEntityCount(MimeEntity? root, int maximumDepth, int maximumEntityCount);
    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The current alternatives.</returns>
    List<MimeEntity> GetAlternativesWithMessage(EmailMessage message);
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
    List<MimeEntity> GetAlternativesWithMessageAndMaximumDepthAndMaximumEntityCount(EmailMessage message, int maximumDepth, int maximumEntityCount);
}
