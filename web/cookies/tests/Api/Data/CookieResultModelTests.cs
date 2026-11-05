namespace cookies.Tests;

public class CookieResultModelTests
{
    [Test]
    public void SetCookieParseResultCarriesFailureWithoutAnException()
    {
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.InvalidName, Severity = CookieParseSeverity.Error };
        SetCookieParseResult result = new SetCookieParseResult
        {
            Success = false,
            Cookie = null,
            ErrorCode = CookieErrorCode.InvalidName,
            Issues = [issue]
        };

        Assert.That(result.Success, Is.False);
        Assert.That(result.Cookie, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidName));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void CookieHeaderParseResultPreservesPairAndIssueOrder()
    {
        CookiePair first = new CookiePair { Name = "id", Value = "1" };
        CookiePair second = new CookiePair { Name = "id", Value = "2" };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.IgnoredSegment };
        CookieHeaderParseResult result = new CookieHeaderParseResult
        {
            Success = true,
            Pairs = [first, second],
            ErrorCode = CookieErrorCode.None,
            Issues = [issue]
        };

        Assert.That(result.Pairs, Is.EqualTo(new[] { first, second }));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
    }

    [Test]
    public void SetDecisionCarriesPrimaryReasonAndNormalizedCandidate()
    {
        StoredCookie cookie = new StoredCookie { Name = "id", Value = "1" };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.CompatibilityRecovery };
        CookieSetDecision decision = new CookieSetDecision
        {
            Status = CookieSetStatus.Accepted,
            ErrorCode = CookieErrorCode.None,
            NormalizedCookie = cookie,
            Issues = [issue]
        };

        Assert.That(decision.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(decision.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(decision.NormalizedCookie, Is.SameAs(cookie));
        Assert.That(decision.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void SetResultCarriesMutationParseAndDiagnosticState()
    {
        StoredCookie cookie = new StoredCookie { Name = "id", Value = "2" };
        SetCookieParseResult parse = new SetCookieParseResult { Success = true, Cookie = new SetCookie { Name = "id" } };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.QuotaEviction };
        CookieSetResult result = new CookieSetResult
        {
            Status = CookieSetStatus.Replaced,
            ErrorCode = CookieErrorCode.None,
            Cookie = cookie,
            ParseResult = parse,
            Issues = [issue]
        };

        Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Replaced));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookie, Is.SameAs(cookie));
        Assert.That(result.ParseResult, Is.SameAs(parse));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void BatchSetResultPreservesPerFieldOrderAndCounts()
    {
        CookieSetResult accepted = new CookieSetResult { Status = CookieSetStatus.Accepted };
        CookieSetResult rejected = new CookieSetResult { Status = CookieSetStatus.Rejected };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.InvalidValue };
        CookieBatchSetResult result = new CookieBatchSetResult
        {
            Results = [accepted, rejected],
            AcceptedCount = 1,
            ReplacedCount = 2,
            DeletedCount = 3,
            IgnoredCount = 4,
            RejectedCount = 5,
            Issues = [issue]
        };

        Assert.That(result.Results, Is.EqualTo(new[] { accepted, rejected }));
        Assert.That(result.AcceptedCount, Is.EqualTo(1));
        Assert.That(result.ReplacedCount, Is.EqualTo(2));
        Assert.That(result.DeletedCount, Is.EqualTo(3));
        Assert.That(result.IgnoredCount, Is.EqualTo(4));
        Assert.That(result.RejectedCount, Is.EqualTo(5));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void MatchAndSelectionResultsPreserveEvaluationAndOutputOrder()
    {
        StoredCookie cookie = new StoredCookie { Name = "id", Value = "1" };
        CookiePair pair = new CookiePair { Name = "id", Value = "1" };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.None };
        CookieMatchResult match = new CookieMatchResult
        {
            Cookie = cookie,
            Pair = pair,
            Status = CookieMatchStatus.Included,
            ErrorCode = CookieErrorCode.None,
            Issues = [issue]
        };
        CookieSelectionResult selection = new CookieSelectionResult
        {
            Success = true,
            ErrorCode = CookieErrorCode.None,
            Cookies = [cookie],
            Pairs = [pair],
            FieldValues = ["id=1"],
            Matches = [match],
            Issues = [issue]
        };

        Assert.That(match.Cookie, Is.SameAs(cookie));
        Assert.That(match.Pair, Is.SameAs(pair));
        Assert.That(match.Status, Is.EqualTo(CookieMatchStatus.Included));
        Assert.That(match.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(match.Issues, Is.EqualTo(new[] { issue }));
        Assert.That(selection.Success, Is.True);
        Assert.That(selection.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(selection.Cookies, Is.EqualTo(new[] { cookie }));
        Assert.That(selection.Pairs, Is.EqualTo(new[] { pair }));
        Assert.That(selection.FieldValues, Is.EqualTo(new[] { "id=1" }));
        Assert.That(selection.Matches, Is.EqualTo(new[] { match }));
        Assert.That(selection.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void JarSnapshotRetainsVersionOrderAndSequenceState()
    {
        StoredCookie first = new StoredCookie { Name = "a", SequenceNumber = 5 };
        StoredCookie second = new StoredCookie { Name = "b", SequenceNumber = 6 };
        CookieJarSnapshot snapshot = new CookieJarSnapshot
        {
            Version = 7,
            Cookies = [first, second],
            NextSequenceNumber = 8
        };

        Assert.That(snapshot.Version, Is.EqualTo(7));
        Assert.That(snapshot.Cookies, Is.EqualTo(new[] { first, second }));
        Assert.That(snapshot.NextSequenceNumber, Is.EqualTo(8));
        Assert.That(new CookieJarSnapshot().Version, Is.EqualTo(CookieDefaults.SnapshotVersion));
    }

    [Test]
    public void SnapshotParseResultCarriesCorruptInputFailure()
    {
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.InvalidSnapshot };
        CookieSnapshotParseResult result = new CookieSnapshotParseResult
        {
            Success = false,
            Snapshot = null,
            ErrorCode = CookieErrorCode.InvalidSnapshot,
            Issues = [issue]
        };

        Assert.That(result.Success, Is.False);
        Assert.That(result.Snapshot, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidSnapshot));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void ImportResultRetainsModeCountsAndEntryOutcomes()
    {
        CookieSetResult entry = new CookieSetResult { Status = CookieSetStatus.Accepted };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.LossyImport };
        CookieImportResult result = new CookieImportResult
        {
            Success = true,
            Mode = CookieImportMode.Replace,
            ErrorCode = CookieErrorCode.None,
            Results = [entry],
            AcceptedCount = 1,
            ReplacedCount = 2,
            DeletedCount = 3,
            IgnoredCount = 4,
            RejectedCount = 5,
            RemovedCount = 6,
            Issues = [issue]
        };

        Assert.That(result.Success, Is.True);
        Assert.That(result.Mode, Is.EqualTo(CookieImportMode.Replace));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Results, Is.EqualTo(new[] { entry }));
        Assert.That(result.AcceptedCount, Is.EqualTo(1));
        Assert.That(result.ReplacedCount, Is.EqualTo(2));
        Assert.That(result.DeletedCount, Is.EqualTo(3));
        Assert.That(result.IgnoredCount, Is.EqualTo(4));
        Assert.That(result.RejectedCount, Is.EqualTo(5));
        Assert.That(result.RemovedCount, Is.EqualTo(6));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
    }

    [Test]
    public void NetscapeParseResultRetainsEntriesAndLossDiagnostics()
    {
        StoredCookie cookie = new StoredCookie { Name = "id" };
        CookieIssue issue = new CookieIssue { ErrorCode = CookieErrorCode.LossyImport };
        NetscapeCookieFileParseResult result = new NetscapeCookieFileParseResult
        {
            Success = true,
            Cookies = [cookie],
            ErrorCode = CookieErrorCode.None,
            Issues = [issue]
        };

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookies, Is.EqualTo(new[] { cookie }));
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Issues, Is.EqualTo(new[] { issue }));
    }
}
