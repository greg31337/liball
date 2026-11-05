using System;

namespace email;
/// <summary>
/// Provides operations for working with canonical comparer.
/// </summary>
public class EmailCanonicalComparer : IEmailCanonicalComparerContract
{
    internal static EmailCanonicalComparisonResult CompareImplementationWithLeftAndRightUsingEmailMessageAndEmailMessage(EmailMessage left, EmailMessage right)
    {
        return CompareImplementationWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(left, right, new EmailCanonicalComparisonOptions());
    }

    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    public EmailCanonicalComparisonResult CompareWithLeftAndRightUsingEmailMessageAndEmailMessage(EmailMessage left, EmailMessage right)
    {
        return EmailCanonicalComparer.CompareImplementationWithLeftAndRightUsingEmailMessageAndEmailMessage(left, right);
    }

    internal static EmailCanonicalComparisonResult CompareImplementationWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(EmailMessage left, EmailMessage right, EmailCanonicalComparisonOptions options)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailCanonicalComparisonOptions snapshot = options.Clone();
        EmailHashResult leftFingerprint = new EmailFingerprint().ComputeSemanticWithMessageAndOptions(left, snapshot);
        EmailHashResult rightFingerprint = new EmailFingerprint().ComputeSemanticWithMessageAndOptions(right, snapshot);
        return CreateResult(leftFingerprint, rightFingerprint);
    }

    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCanonicalComparisonResult CompareWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(EmailMessage left, EmailMessage right, EmailCanonicalComparisonOptions options)
    {
        return EmailCanonicalComparer.CompareImplementationWithLeftAndRightAndOptionsUsingEmailMessageAndEmailMessageAndEmailCanonicalComparisonOptions(left, right, options);
    }

    internal static EmailCanonicalComparisonResult CompareImplementationWithLeftAndRightUsingMimeEntityAndMimeEntity(MimeEntity left, MimeEntity right)
    {
        return CompareImplementationWithLeftAndRightAndOptionsUsingMimeEntityAndMimeEntityAndEmailCanonicalComparisonOptions(left, right, new EmailCanonicalComparisonOptions());
    }

    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    public EmailCanonicalComparisonResult CompareWithLeftAndRightUsingMimeEntityAndMimeEntity(MimeEntity left, MimeEntity right)
    {
        return EmailCanonicalComparer.CompareImplementationWithLeftAndRightUsingMimeEntityAndMimeEntity(left, right);
    }

    internal static EmailCanonicalComparisonResult CompareImplementationWithLeftAndRightAndOptionsUsingMimeEntityAndMimeEntityAndEmailCanonicalComparisonOptions(MimeEntity left, MimeEntity right, EmailCanonicalComparisonOptions options)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailCanonicalComparisonOptions snapshot = options.Clone();
        EmailHashResult leftFingerprint = new EmailFingerprint().ComputeSemanticWithEntityAndOptions(left, snapshot);
        EmailHashResult rightFingerprint = new EmailFingerprint().ComputeSemanticWithEntityAndOptions(right, snapshot);
        return CreateResult(leftFingerprint, rightFingerprint);
    }

    /// <summary>
    /// Compares the supplied value.
    /// </summary>
    /// <param name = "left">The left used by the operation.</param>
    /// <param name = "right">The right used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The canonical comparison result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCanonicalComparisonResult CompareWithLeftAndRightAndOptionsUsingMimeEntityAndMimeEntityAndEmailCanonicalComparisonOptions(MimeEntity left, MimeEntity right, EmailCanonicalComparisonOptions options)
    {
        return EmailCanonicalComparer.CompareImplementationWithLeftAndRightAndOptionsUsingMimeEntityAndMimeEntityAndEmailCanonicalComparisonOptions(left, right, options);
    }

    static EmailCanonicalComparisonResult CreateResult(EmailHashResult left, EmailHashResult right)
    {
        bool equal = left.GetHashedByteCount() == right.GetHashedByteCount() && left.Matches(right.GetDigestBytes());
        return new EmailCanonicalComparisonResult(equal, left, right);
    }
}
