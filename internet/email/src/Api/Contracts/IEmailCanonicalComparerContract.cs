using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCanonicalComparer"/>.
/// </summary>
public interface IEmailCanonicalComparerContract
{
    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    EmailCanonicalComparisonResult CompareWithLeftAndRightUsingEmailMessageAndEmailMessage(EmailMessage left, EmailMessage right);
    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCanonicalComparisonResult CompareWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(EmailMessage left, EmailMessage right, EmailCanonicalComparisonOptions options);
    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    EmailCanonicalComparisonResult CompareWithLeftAndRightUsingMimeEntityAndMimeEntity(MimeEntity left, MimeEntity right);
    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCanonicalComparisonResult CompareWithLeftAndRightAndOptionsUsingMimeEntityAndMimeEntityAndEmailCanonicalComparisonOptions(MimeEntity left, MimeEntity right, EmailCanonicalComparisonOptions options);
}
