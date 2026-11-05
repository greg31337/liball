using email;

namespace email.Tests;

public class EmailIssueTests
{
    [Test]
    public void IssueRetainsSeverityCodeAndOptionalContext()
    {
        EmailSourcePosition position = new EmailSourcePosition(42, 3, 7);
        EmailPath path = new EmailPathOperations().CreateRoot().Append(0).Append(2);
        EmailIssue issue = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(
            EmailIssueSeverity.Warning,
            EmailIssueCode.HeaderMalformed,
            "A folded field was malformed.",
            position,
            path,
            "Subject",
            "The invalid continuation was retained as raw data.");

        Assert.That(issue.GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(issue.GetCode(), Is.EqualTo(EmailIssueCode.HeaderMalformed));
        Assert.That(issue.GetDescription(), Is.EqualTo("A folded field was malformed."));
        Assert.That(issue.HasSourcePosition(), Is.True);
        Assert.That(issue.GetSourcePosition(), Is.SameAs(position));
        Assert.That(issue.HasEntityPath(), Is.True);
        Assert.That(issue.GetEntityPath(), Is.SameAs(path));
        Assert.That(issue.HasFieldName(), Is.True);
        Assert.That(issue.GetFieldName(), Is.EqualTo("Subject"));
        Assert.That(issue.HasRecoveryDescription(), Is.True);
        Assert.That(issue.GetRecoveryDescription(), Does.Contain("retained"));
    }

    [Test]
    public void MinimalIssueReportsOptionalContextAsAbsent()
    {
        EmailIssue issue = new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(
            EmailIssueSeverity.Information,
            EmailIssueCode.RecoveryApplied,
            "Input was normalized.");

        Assert.That(issue.HasSourcePosition(), Is.False);
        Assert.That(issue.GetSourcePosition(), Is.Null);
        Assert.That(issue.HasEntityPath(), Is.False);
        Assert.That(issue.GetEntityPath(), Is.Null);
        Assert.That(issue.HasFieldName(), Is.False);
        Assert.That(issue.GetFieldName(), Is.Null);
        Assert.That(issue.HasRecoveryDescription(), Is.False);
        Assert.That(issue.GetRecoveryDescription(), Is.Null);
    }

    [Test]
    public void IssueRejectsInvalidRequiredValuesAndEmptyOptionalText()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(
            (EmailIssueSeverity)4,
            EmailIssueCode.GeneralInvalidInput,
            "bad severity"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(
            EmailIssueSeverity.Error,
            EmailIssueCode.Unknown,
            "bad code"));
        Assert.Throws<ArgumentNullException>(() => new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(
            EmailIssueSeverity.Error,
            EmailIssueCode.GeneralInvalidInput,
            null!));
        Assert.Throws<ArgumentException>(() => new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(
            EmailIssueSeverity.Error,
            EmailIssueCode.GeneralInvalidInput,
            string.Empty));
        Assert.Throws<ArgumentException>(() => new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(
            EmailIssueSeverity.Error,
            EmailIssueCode.HeaderMalformed,
            "bad field",
            null,
            null,
            string.Empty,
            null));
        Assert.Throws<ArgumentException>(() => new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(
            EmailIssueSeverity.Warning,
            EmailIssueCode.RecoveryApplied,
            "bad recovery",
            null,
            null,
            null,
            string.Empty));
    }

    [Test]
    public void SourcePositionUsesZeroBasedBytesAndOneBasedTextCoordinates()
    {
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);

        Assert.That(position.GetByteOffset(), Is.Zero);
        Assert.That(position.GetLineNumber(), Is.EqualTo(1));
        Assert.That(position.GetColumnNumber(), Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSourcePosition(-1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSourcePosition(0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailSourcePosition(0, 1, 0));
    }

    [Test]
    public void CodeRangesAreStableAndClassifiable()
    {
        Assert.That(EmailIssueCodeRangesConstants.GeneralStart, Is.EqualTo(1000));
        Assert.That(EmailIssueCodeRangesConstants.CryptographyEnd, Is.EqualTo(9999));
        Assert.That(
            new EmailIssueCodeRangesOperations().GetRangeStart(EmailIssueCode.HeaderMalformed),
            Is.EqualTo(EmailIssueCodeRangesConstants.HeaderStart));
        Assert.That(
            new EmailIssueCodeRangesOperations().GetRangeStart(EmailIssueCode.AuthenticationFailed),
            Is.EqualTo(EmailIssueCodeRangesConstants.AuthenticationStart));
        Assert.That(new EmailIssueCodeRangesOperations().GetRangeStart(EmailIssueCode.Unknown), Is.Zero);
        Assert.That(new EmailIssueCodeRangesOperations().IsLibraryCode(10000), Is.False);
    }
}
