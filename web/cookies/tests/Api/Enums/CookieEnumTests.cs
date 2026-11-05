namespace cookies.Tests;

public class CookieEnumTests
{
    [Test]
    public void ComplianceModesHaveStableValues()
    {
        Assert.That((int)CookieComplianceMode.Rfc6265, Is.EqualTo(0));
        Assert.That((int)CookieComplianceMode.Rfc6265Bis, Is.EqualTo(1));
        Assert.That((int)CookieComplianceMode.Compatible, Is.EqualTo(2));
    }

    [Test]
    public void SameSiteModesHaveStableValues()
    {
        Assert.That((int)CookieSameSiteMode.Default, Is.EqualTo(0));
        Assert.That((int)CookieSameSiteMode.Lax, Is.EqualTo(1));
        Assert.That((int)CookieSameSiteMode.Strict, Is.EqualTo(2));
        Assert.That((int)CookieSameSiteMode.None, Is.EqualTo(3));
    }

    [Test]
    public void SameSiteContextsHaveStableValues()
    {
        Assert.That((int)CookieSameSiteContext.SameSite, Is.EqualTo(0));
        Assert.That((int)CookieSameSiteContext.CrossSite, Is.EqualTo(1));
    }

    [Test]
    public void AccessKindsHaveStableValues()
    {
        Assert.That((int)CookieAccessKind.Http, Is.EqualTo(0));
        Assert.That((int)CookieAccessKind.NonHttp, Is.EqualTo(1));
    }

    [Test]
    public void PrefixPoliciesHaveStableValues()
    {
        Assert.That((int)CookiePrefixPolicy.SilentReject, Is.EqualTo(0));
        Assert.That((int)CookiePrefixPolicy.ErrorReject, Is.EqualTo(1));
        Assert.That((int)CookiePrefixPolicy.Disabled, Is.EqualTo(2));
    }

    [Test]
    public void ParseSeveritiesHaveStableValues()
    {
        Assert.That((int)CookieParseSeverity.Information, Is.EqualTo(0));
        Assert.That((int)CookieParseSeverity.Warning, Is.EqualTo(1));
        Assert.That((int)CookieParseSeverity.Error, Is.EqualTo(2));
    }

    [Test]
    public void SetStatusesHaveStableValues()
    {
        Assert.That((int)CookieSetStatus.Accepted, Is.EqualTo(0));
        Assert.That((int)CookieSetStatus.Replaced, Is.EqualTo(1));
        Assert.That((int)CookieSetStatus.Deleted, Is.EqualTo(2));
        Assert.That((int)CookieSetStatus.Ignored, Is.EqualTo(3));
        Assert.That((int)CookieSetStatus.Rejected, Is.EqualTo(4));
    }

    [Test]
    public void MatchStatusesHaveStableValues()
    {
        Assert.That((int)CookieMatchStatus.Included, Is.EqualTo(0));
        Assert.That((int)CookieMatchStatus.Excluded, Is.EqualTo(1));
    }

    [Test]
    public void ImportModesHaveStableValues()
    {
        Assert.That((int)CookieImportMode.Merge, Is.EqualTo(0));
        Assert.That((int)CookieImportMode.Replace, Is.EqualTo(1));
    }

    [Test]
    public void ErrorCodesHaveStableValues()
    {
        Assert.That((int)CookieErrorCode.None, Is.EqualTo(0));
        Assert.That((int)CookieErrorCode.MalformedSyntax, Is.EqualTo(1));
        Assert.That((int)CookieErrorCode.HeaderInjection, Is.EqualTo(2));
        Assert.That((int)CookieErrorCode.InvalidName, Is.EqualTo(3));
        Assert.That((int)CookieErrorCode.InvalidValue, Is.EqualTo(4));
        Assert.That((int)CookieErrorCode.InvalidAttribute, Is.EqualTo(5));
        Assert.That((int)CookieErrorCode.InvalidDate, Is.EqualTo(6));
        Assert.That((int)CookieErrorCode.UnknownAttribute, Is.EqualTo(7));
        Assert.That((int)CookieErrorCode.DuplicateAttribute, Is.EqualTo(8));
        Assert.That((int)CookieErrorCode.IgnoredSegment, Is.EqualTo(9));
        Assert.That((int)CookieErrorCode.CompatibilityRecovery, Is.EqualTo(10));
        Assert.That((int)CookieErrorCode.UnsupportedScheme, Is.EqualTo(11));
        Assert.That((int)CookieErrorCode.InvalidHost, Is.EqualTo(12));
        Assert.That((int)CookieErrorCode.InvalidIdna, Is.EqualTo(13));
        Assert.That((int)CookieErrorCode.DomainMismatch, Is.EqualTo(14));
        Assert.That((int)CookieErrorCode.PublicSuffix, Is.EqualTo(15));
        Assert.That((int)CookieErrorCode.PublicSuffixListUnavailable, Is.EqualTo(16));
        Assert.That((int)CookieErrorCode.InvalidPath, Is.EqualTo(17));
        Assert.That((int)CookieErrorCode.InsecureSecureCookie, Is.EqualTo(18));
        Assert.That((int)CookieErrorCode.HttpOnlyApiViolation, Is.EqualTo(19));
        Assert.That((int)CookieErrorCode.SecureOverlayViolation, Is.EqualTo(20));
        Assert.That((int)CookieErrorCode.SameSiteViolation, Is.EqualTo(21));
        Assert.That((int)CookieErrorCode.PrefixViolation, Is.EqualTo(22));
        Assert.That((int)CookieErrorCode.MissingPartitionContext, Is.EqualTo(23));
        Assert.That((int)CookieErrorCode.PartitionMismatch, Is.EqualTo(24));
        Assert.That((int)CookieErrorCode.Expired, Is.EqualTo(25));
        Assert.That((int)CookieErrorCode.ExpiredDeletion, Is.EqualTo(26));
        Assert.That((int)CookieErrorCode.PolicyRejection, Is.EqualTo(27));
        Assert.That((int)CookieErrorCode.QuotaEviction, Is.EqualTo(28));
        Assert.That((int)CookieErrorCode.NameValueLimitExceeded, Is.EqualTo(29));
        Assert.That((int)CookieErrorCode.AttributeLimitExceeded, Is.EqualTo(30));
        Assert.That((int)CookieErrorCode.InputLimitExceeded, Is.EqualTo(31));
        Assert.That((int)CookieErrorCode.HeaderLimitExceeded, Is.EqualTo(32));
        Assert.That((int)CookieErrorCode.PerDomainLimitExceeded, Is.EqualTo(33));
        Assert.That((int)CookieErrorCode.TotalLimitExceeded, Is.EqualTo(34));
        Assert.That((int)CookieErrorCode.LifetimeLimitExceeded, Is.EqualTo(35));
        Assert.That((int)CookieErrorCode.SerializationFailure, Is.EqualTo(36));
        Assert.That((int)CookieErrorCode.StoreFailure, Is.EqualTo(37));
        Assert.That((int)CookieErrorCode.InvalidSnapshot, Is.EqualTo(38));
        Assert.That((int)CookieErrorCode.UnsupportedSnapshotVersion, Is.EqualTo(39));
        Assert.That((int)CookieErrorCode.DuplicateIdentity, Is.EqualTo(40));
        Assert.That((int)CookieErrorCode.LossyImport, Is.EqualTo(41));
        Assert.That((int)CookieErrorCode.MalformedNetscapeLine, Is.EqualTo(42));
        Assert.That((int)CookieErrorCode.InvalidConfiguration, Is.EqualTo(43));
    }
}
