using email;

namespace email.Tests;

public class EmailIssueListTests
{
    [Test]
    public void ListPreservesInsertionOrderAndReturnsSnapshot()
    {
        EmailIssue first = CreateIssue(EmailIssueSeverity.Warning, EmailIssueCode.HeaderMalformed, "first");
        EmailIssue second = CreateIssue(EmailIssueSeverity.Error, EmailIssueCode.MimeStructureMalformed, "second");
        EmailIssueList issues = new EmailIssueList();
        issues.Add(first);
        issues.Add(second);

        List<EmailIssue> snapshot = issues.GetAll();
        snapshot.Clear();

        Assert.That(issues.GetCount(), Is.EqualTo(2));
        Assert.That(issues.GetAt(0), Is.SameAs(first));
        Assert.That(issues.GetAt(1), Is.SameAs(second));
        Assert.That(issues.GetCountBySeverity(EmailIssueSeverity.Warning), Is.EqualTo(1));
        Assert.That(issues.GetCountBySeverity(EmailIssueSeverity.Error), Is.EqualTo(1));
        Assert.That(issues.HasSeverityAtLeast(EmailIssueSeverity.Error), Is.True);
        Assert.That(issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal), Is.False);
        Assert.That(issues.ContainsCode(EmailIssueCode.MimeStructureMalformed), Is.True);
    }

    [Test]
    public void AddRangeSnapshotsSourceAndSupportsSelfAppend()
    {
        EmailIssue first = CreateIssue(EmailIssueSeverity.Information, EmailIssueCode.RecoveryApplied, "first");
        EmailIssue second = CreateIssue(EmailIssueSeverity.Warning, EmailIssueCode.UnsafeContent, "second");
        EmailIssueList source = new EmailIssueList();
        source.Add(first);
        source.Add(second);
        EmailIssueList target = new EmailIssueList();

        target.AddRange(source);
        source.Clear();
        target.AddRange(target);

        Assert.That(target.GetCount(), Is.EqualTo(4));
        Assert.That(target.GetAt(0), Is.SameAs(first));
        Assert.That(target.GetAt(1), Is.SameAs(second));
        Assert.That(target.GetAt(2), Is.SameAs(first));
        Assert.That(target.GetAt(3), Is.SameAs(second));
    }

    [Test]
    public void ListRejectsNullInvalidSeverityAndInvalidIndexes()
    {
        EmailIssueList issues = new EmailIssueList();

        Assert.Throws<ArgumentNullException>(() => issues.Add(null!));
        Assert.Throws<ArgumentNullException>(() => issues.AddRange(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => issues.GetAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => issues.GetCountBySeverity((EmailIssueSeverity)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => issues.HasSeverityAtLeast((EmailIssueSeverity)(-1)));
    }

    [Test]
    public void ClearRemovesAllIssues()
    {
        EmailIssueList issues = new EmailIssueList();
        issues.Add(CreateIssue(EmailIssueSeverity.Fatal, EmailIssueCode.LimitExceeded, "limit"));

        issues.Clear();

        Assert.That(issues.GetCount(), Is.Zero);
        Assert.That(issues.GetAll(), Is.Empty);
    }

    static EmailIssue CreateIssue(EmailIssueSeverity severity, EmailIssueCode code, string description)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(severity, code, description);
    }
}
