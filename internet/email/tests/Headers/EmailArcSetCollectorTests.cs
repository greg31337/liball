using System.Text;
using email;

namespace email.Tests;

public class EmailArcSetCollectorTests
{
    [Test]
    public void ReturnsNoneAndTheFirstInstanceWhenNoArcHeadersExist()
    {
        EmailHeaders headers = ParseHeaders("From: sender@example.com\r\nSubject: hello\r\n\r\n");

        EmailArcSetCollectionResult result = new EmailArcSetCollector().CollectWithHeaders(headers);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcSetCollectionStatus.None));
        Assert.That(result.HasArcHeaders(), Is.False);
        Assert.That(result.GetSetCount(), Is.Zero);
        Assert.That(result.HasContinuousInstances(), Is.True);
        Assert.That(result.HasCompleteSets(), Is.True);
        Assert.That(result.HasValidChainStatuses(), Is.True);
        Assert.That(result.GetNextInstance(), Is.EqualTo(1));
    }

    [Test]
    public void CollectsCompleteSetsInInstanceOrderAndLinksAuthenticationResults()
    {
        string source = Set(2, "pass", "mx2.example") + Set(1, "none", "mx1.example") + "From: sender@example.com\r\n\r\n";
        EmailHeaders headers = ParseHeaders(source);

        EmailArcSetCollectionResult result = new EmailArcSetCollector().CollectWithHeaders(headers);

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcSetCollectionStatus.Valid));
        Assert.That(result.IsStructurallyValid(), Is.True);
        Assert.That(result.GetSetCount(), Is.EqualTo(2));
        Assert.That(result.GetMaximumInstance(), Is.EqualTo(2));
        Assert.That(result.GetNextInstance(), Is.EqualTo(3));
        Assert.That(result.HasContinuousInstances(), Is.True);
        Assert.That(result.HasCompleteSets(), Is.True);
        Assert.That(result.HasValidChainStatuses(), Is.True);
        Assert.That(result.GetSet(0).GetInstance(), Is.EqualTo(1));
        Assert.That(result.GetSet(1).GetInstance(), Is.EqualTo(2));
        Assert.That(result.GetSetByInstance(1)!.GetSealField(0).GetSourceIndex(), Is.EqualTo(5));
        Assert.That(result.GetSetByInstance(2)!.GetSealField(0).GetSourceIndex(), Is.EqualTo(2));
        Assert.That(result.GetField(0).GetKind(), Is.EqualTo(EmailSignatureHeaderKind.ArcAuthenticationResults));
        Assert.That(result.GetField(0).GetSourceIndex(), Is.Zero);

        EmailArcSet first = result.GetSetByInstance(1)!;
        Assert.That(first!.HasAuthenticationResultsLink(), Is.True);
        Assert.That(first.GetAuthenticationResults()!.GetAuthenticationServiceIdentifier(), Is.EqualTo("mx1.example"));
        Assert.That(first.GetAuthenticationResults()!.GetResultCount(), Is.EqualTo(1));
        Assert.That(first.GetAuthenticationResults()!.GetResult(0).GetMethod(), Is.EqualTo("dkim"));
        Assert.That(first.GetAuthenticationResults()!.GetResult(0).GetResult(), Is.EqualTo("pass"));
    }

    [Test]
    public void ReportsMissingInstancesIncompleteSetsAndDuplicateFields()
    {
        string first = AuthenticationResults(1, "mx1.example") + MessageSignature(1);
        string third = Set(3, "pass", "mx3.example") + AuthenticationResults(3, "duplicate.example");
        EmailArcSetCollectionResult result = new EmailArcSetCollector().CollectWithHeaders(ParseHeaders(first + third + "\r\n"));

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcSetCollectionStatus.Invalid));
        Assert.That(result.HasContinuousInstances(), Is.False);
        Assert.That(result.HasCompleteSets(), Is.False);
        Assert.That(result.GetSetCount(), Is.EqualTo(2));
        Assert.That(result.GetSetByInstance(1)!.GetSealFieldCount(), Is.Zero);
        Assert.That(result.GetSetByInstance(3)!.GetAuthenticationResultsFieldCount(), Is.EqualTo(2));
        Assert.That(result.GetSetByInstance(3)!.HasDuplicateFields(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ArcInstanceSequenceInvalid), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ArcSetIncomplete), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ArcSetDuplicateHeader), Is.True);
        Assert.That(result.GetNextInstance(), Is.Zero);
    }

    [TestCase(1, "pass")]
    [TestCase(2, "none")]
    [TestCase(2, "fail")]
    public void RejectsChainStatusThatDoesNotMatchTheInstance(int instance, string chainStatus)
    {
        string source = instance == 1 ? Set(1, chainStatus, "mx1.example") : Set(1, "none", "mx1.example") + Set(2, chainStatus, "mx2.example");

        EmailArcSetCollectionResult result = new EmailArcSetCollector().CollectWithHeaders(ParseHeaders(source + "\r\n"));

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcSetCollectionStatus.Invalid));
        Assert.That(result.HasValidChainStatuses(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ArcChainStatusInvalid), Is.True);
    }

    [Test]
    public void RetainsMalformedOutOfRangeHeadersAsUnassignedDiagnostics()
    {
        string source = "ARC-Seal: i=51; a=rsa-sha256; b=YWJjZA==; cv=pass; d=example.com; s=arc; t=1\r\n\r\n";

        EmailArcSetCollectionResult result = new EmailArcSetCollector().CollectWithHeaders(ParseHeaders(source));

        Assert.That(result.GetStatus(), Is.EqualTo(EmailArcSetCollectionStatus.Invalid));
        Assert.That(result.GetFieldCount(), Is.EqualTo(1));
        Assert.That(result.GetSetCount(), Is.Zero);
        Assert.That(result.GetUnassignedFieldCount(), Is.EqualTo(1));
        Assert.That(result.GetUnassignedFields()[0].GetInstance(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ArcHeaderMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.ArcHeaderUnassigned), Is.True);
    }

    [Test]
    public void EnforcesArcHeaderAndSetCollectionLimits()
    {
        EmailHeaders twoHeaders = ParseHeaders(AuthenticationResults(1, "mx1.example") + MessageSignature(1) + "\r\n");
        EmailArcSetCollectionOptions headerOptions = new EmailArcSetCollectionOptions();
        headerOptions.SetMaximumArcHeaderCount(1);

        EmailLimitException headerException = Assert.Throws<EmailLimitException>(() => new EmailArcSetCollector().CollectWithHeadersAndOptions(twoHeaders, headerOptions));
        Assert.That(headerException.GetLimitName(), Is.EqualTo("MaxArcHeaderCount"));
        Assert.That(headerException.GetObserved(), Is.EqualTo(2));

        EmailHeaders twoSets = ParseHeaders(AuthenticationResults(1, "mx1.example") + AuthenticationResults(2, "mx2.example") + "\r\n");
        EmailArcSetCollectionOptions setOptions = new EmailArcSetCollectionOptions();
        setOptions.SetMaximumSetCount(1);

        EmailLimitException setException = Assert.Throws<EmailLimitException>(() => new EmailArcSetCollector().CollectWithHeadersAndOptions(twoSets, setOptions));
        Assert.That(setException.GetLimitName(), Is.EqualTo("MaxArcSetCount"));
        Assert.That(setException.GetObserved(), Is.EqualTo(2));
    }

    static string Set(int instance, string chainStatus, string service)
    {
        return AuthenticationResults(instance, service) + MessageSignature(instance) + Seal(instance, chainStatus);
    }

    static string AuthenticationResults(int instance, string service)
    {
        return "ARC-Authentication-Results: i=" + instance + "; " + service + "; dkim=pass header.d=example.com\r\n";
    }

    static string MessageSignature(int instance)
    {
        return "ARC-Message-Signature: i=" + instance + "; a=rsa-sha256; b=YWJjZA==; bh=YWJjZA==; d=example.com; h=from; s=arc\r\n";
    }

    static string Seal(int instance, string chainStatus)
    {
        return "ARC-Seal: i=" + instance + "; a=rsa-sha256; b=YWJjZA==; cv=" + chainStatus + "; d=example.com; s=arc; t=1\r\n";
    }

    static EmailHeaders ParseHeaders(string source)
    {
        EmailHeaderBlockParseResult parsed = new EmailHeaderBlockParser().ParseWithInput(Encoding.ASCII.GetBytes(source));
        Assert.That(parsed.IsComplete(), Is.True);
        return parsed.GetHeaders();
    }
}
