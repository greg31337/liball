namespace cookies.Tests;

public class CookieOptionModelTests
{
    [Test]
    public void LimitsStartWithPortableCookieDefaults()
    {
        CookieLimits limits = new CookieLimits();

        Assert.That(limits.MaximumNameValueOctets, Is.EqualTo(CookieDefaults.MaximumNameValueOctets));
        Assert.That(limits.MaximumAttributeValueOctets, Is.EqualTo(CookieDefaults.MaximumAttributeValueOctets));
        Assert.That(limits.MaximumInputLineOctets, Is.EqualTo(CookieDefaults.MaximumInputLineOctets));
        Assert.That(limits.MaximumCookiesPerDomain, Is.EqualTo(CookieDefaults.MaximumCookiesPerDomain));
        Assert.That(limits.MaximumCookies, Is.EqualTo(CookieDefaults.MaximumCookies));
        Assert.That(limits.MaximumRequestHeaderOctets, Is.EqualTo(CookieDefaults.MaximumRequestHeaderOctets));
        Assert.That(limits.MaximumLifetime, Is.EqualTo(CookieDefaults.MaximumLifetime));
    }

    [Test]
    public void ParseOptionsDefaultToBaselineWithCompleteDiagnostics()
    {
        CookieParseOptions options = new CookieParseOptions();

        Assert.That(options.ComplianceMode, Is.EqualTo(CookieComplianceMode.Rfc6265));
        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.PreserveUnknownAttributes, Is.True);
        Assert.That(options.ReportUnknownAttributes, Is.True);
        Assert.That(options.ReportDuplicateAttributes, Is.True);
        Assert.That(options.ReportCompatibilityRecoveries, Is.True);
    }

    [Test]
    public void SerializationOptionsDefaultToCanonicalExtensibleOutput()
    {
        CookieSerializationOptions options = new CookieSerializationOptions();

        Assert.That(options.ComplianceMode, Is.EqualTo(CookieComplianceMode.Rfc6265));
        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.PreserveQuotedValues, Is.False);
        Assert.That(options.IncludeExtensions, Is.True);
        Assert.That(options.SplitCookieFields, Is.True);
    }

    [Test]
    public void JarOptionsDefaultToBaselineSafetyPolicy()
    {
        CookieJarOptions options = new CookieJarOptions();

        Assert.That(options.ComplianceMode, Is.EqualTo(CookieComplianceMode.Rfc6265));
        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.ParseOptions, Is.Not.Null);
        Assert.That(options.SerializationOptions, Is.Not.Null);
        Assert.That(options.NetscapeFileOptions, Is.Not.Null);
        Assert.That(options.EnforceSameSite, Is.False);
        Assert.That(options.DefaultSameSiteMode, Is.EqualTo(CookieSameSiteMode.Default));
        Assert.That(options.EnableLaxAllowingUnsafe, Is.False);
        Assert.That(options.LaxAllowingUnsafeGracePeriod, Is.EqualTo(CookieDefaults.LaxAllowingUnsafeGracePeriod));
        Assert.That(options.RequireSecureForSameSiteNone, Is.False);
        Assert.That(options.PrefixPolicy, Is.EqualTo(CookiePrefixPolicy.Disabled));
        Assert.That(options.UseCaseInsensitivePrefixes, Is.False);
        Assert.That(options.RequirePublicSuffixList, Is.True);
        Assert.That(options.EnablePartitionedCookies, Is.False);
        Assert.That(options.AllowedDomains, Is.Empty);
        Assert.That(options.BlockedDomains, Is.Empty);
        Assert.That(options.TreatLoopbackAsSecure, Is.False);
        Assert.That(options.ProtectSecureCookieOverlay, Is.False);
        Assert.That(options.EvictNonSecureCookiesFirst, Is.True);
    }

    [Test]
    public void JarOptionsRepresentExplicitModernPolicyChoices()
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            EnforceSameSite = true,
            DefaultSameSiteMode = CookieSameSiteMode.Lax,
            EnableLaxAllowingUnsafe = true,
            LaxAllowingUnsafeGracePeriod = TimeSpan.FromSeconds(30),
            RequireSecureForSameSiteNone = true,
            PrefixPolicy = CookiePrefixPolicy.ErrorReject,
            UseCaseInsensitivePrefixes = true,
            RequirePublicSuffixList = false,
            EnablePartitionedCookies = true,
            AllowedDomains = ["example.test"],
            BlockedDomains = ["blocked.test"],
            TreatLoopbackAsSecure = true,
            ProtectSecureCookieOverlay = true,
            EvictNonSecureCookiesFirst = false
        };

        Assert.That(options.ComplianceMode, Is.EqualTo(CookieComplianceMode.Rfc6265Bis));
        Assert.That(options.EnforceSameSite, Is.True);
        Assert.That(options.DefaultSameSiteMode, Is.EqualTo(CookieSameSiteMode.Lax));
        Assert.That(options.EnableLaxAllowingUnsafe, Is.True);
        Assert.That(options.LaxAllowingUnsafeGracePeriod, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(options.RequireSecureForSameSiteNone, Is.True);
        Assert.That(options.PrefixPolicy, Is.EqualTo(CookiePrefixPolicy.ErrorReject));
        Assert.That(options.UseCaseInsensitivePrefixes, Is.True);
        Assert.That(options.RequirePublicSuffixList, Is.False);
        Assert.That(options.EnablePartitionedCookies, Is.True);
        Assert.That(options.AllowedDomains, Is.EqualTo(new[] { "example.test" }));
        Assert.That(options.BlockedDomains, Is.EqualTo(new[] { "blocked.test" }));
        Assert.That(options.TreatLoopbackAsSecure, Is.True);
        Assert.That(options.ProtectSecureCookieOverlay, Is.True);
        Assert.That(options.EvictNonSecureCookiesFirst, Is.False);
    }

    [Test]
    public void OptionInstancesDoNotShareMutableLimitModels()
    {
        CookieJarOptions first = new CookieJarOptions();
        CookieJarOptions second = new CookieJarOptions();

        first.Limits.MaximumCookies = 1;
        first.ParseOptions.Limits.MaximumInputLineOctets = 2;
        first.SerializationOptions.Limits.MaximumRequestHeaderOctets = 3;
        first.NetscapeFileOptions.Limits.MaximumNameValueOctets = 4;

        Assert.That(second.Limits.MaximumCookies, Is.EqualTo(CookieDefaults.MaximumCookies));
        Assert.That(second.ParseOptions.Limits.MaximumInputLineOctets, Is.EqualTo(CookieDefaults.MaximumInputLineOctets));
        Assert.That(second.SerializationOptions.Limits.MaximumRequestHeaderOctets, Is.EqualTo(CookieDefaults.MaximumRequestHeaderOctets));
        Assert.That(second.NetscapeFileOptions.Limits.MaximumNameValueOctets, Is.EqualTo(CookieDefaults.MaximumNameValueOctets));
    }

    [Test]
    public void NetscapeOptionsExcludeExpiredEntriesByDefault()
    {
        NetscapeCookieFileOptions options = new NetscapeCookieFileOptions();

        Assert.That(options.Limits, Is.Not.Null);
        Assert.That(options.IncludeSessionCookies, Is.True);
        Assert.That(options.IncludeExpiredCookies, Is.False);
        Assert.That(options.ImportSessionCookies, Is.True);
        Assert.That(options.ImportExpiredCookies, Is.False);
        Assert.That(options.RejectPartitionedCookies, Is.True);
    }
}
