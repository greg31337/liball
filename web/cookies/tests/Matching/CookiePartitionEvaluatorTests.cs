namespace cookies.Tests;

public class CookiePartitionEvaluatorTests
{
    [Test]
    public void DisabledExtensionObservesButDoesNotStorePartitionedAttribute()
    {
        SetCookie cookie = new SetCookie
        {
            Partitioned = true,
            Secure = true
        };
        CookieSetContext context = SetContext(false, true, "https://top.example");

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateCreation(cookie, context);

        AssertAllowed(result, false, true, true, false, "", true);
    }

    [Test]
    public void EnabledExtensionDerivesStorageKeyFromTopLevelSiteInput()
    {
        SetCookie cookie = new SetCookie
        {
            Partitioned = true,
            Secure = true
        };
        CookieSetContext context = SetContext(true, true, "https://top.example");

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateCreation(cookie, context);

        AssertAllowed(result, true, true, false, true, "https://top.example", true);
    }

    [Test]
    public void EnabledPartitionedCookieRequiresSecureBeforePartitionContext()
    {
        SetCookie cookie = new SetCookie
        {
            Partitioned = true,
            Secure = false
        };
        CookieSetContext context = SetContext(true, false, "");

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateCreation(cookie, context);

        AssertDenied(
            result,
            CookieErrorCode.InsecureSecureCookie,
            true,
            true,
            false,
            "",
            false);
    }

    [TestCase(false, "")]
    [TestCase(true, "")]
    [TestCase(true, null)]
    public void EnabledPartitionedCookieRequiresTopLevelSiteInput(
        bool hasTopLevelSite,
        string? topLevelSite)
    {
        SetCookie cookie = new SetCookie
        {
            Partitioned = true,
            Secure = true
        };
        CookieSetContext context = SetContext(true, hasTopLevelSite, topLevelSite!);

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateCreation(cookie, context);

        AssertDenied(
            result,
            CookieErrorCode.MissingPartitionContext,
            true,
            true,
            false,
            "",
            false);
    }

    [Test]
    public void OrdinaryCookieRemainsUnpartitionedWhenExtensionIsEnabled()
    {
        CookieSetContext context = SetContext(true, true, null!);

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateCreation(
            new SetCookie { Partitioned = false, Secure = false },
            context);

        AssertAllowed(result, true, false, false, false, "", true);
    }

    [Test]
    public void PartitionedCookieIsReturnedOnlyInItsExactPartition()
    {
        StoredCookie cookie = PartitionedCookie("https://top.example");
        CookieJarOptions options = Options(true);

        CookiePartitionResult matching = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext(true, "https://top.example"),
            options);
        CookiePartitionResult otherScheme = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext(true, "http://top.example"),
            options);
        CookiePartitionResult otherCase = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext(true, "https://TOP.example"),
            options);

        AssertAllowed(matching, true, false, false, true, "https://top.example", true);
        AssertDenied(
            otherScheme,
            CookieErrorCode.PartitionMismatch,
            true,
            false,
            true,
            "https://top.example",
            false);
        AssertDenied(
            otherCase,
            CookieErrorCode.PartitionMismatch,
            true,
            false,
            true,
            "https://top.example",
            false);
    }

    [TestCase(false, "")]
    [TestCase(true, "")]
    [TestCase(true, null)]
    public void PartitionedRetrievalRequiresTopLevelSiteInput(
        bool hasTopLevelSite,
        string? topLevelSite)
    {
        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateRetrieval(
            PartitionedCookie("https://top.example"),
            RequestContext(hasTopLevelSite, topLevelSite!),
            Options(true));

        AssertDenied(
            result,
            CookieErrorCode.MissingPartitionContext,
            true,
            false,
            true,
            "https://top.example",
            false);
    }

    [Test]
    public void DisabledExtensionNeverReturnsPreviouslyPartitionedState()
    {
        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateRetrieval(
            PartitionedCookie("https://top.example"),
            RequestContext(true, "https://top.example"),
            Options(false));

        AssertDenied(
            result,
            CookieErrorCode.PartitionMismatch,
            false,
            false,
            true,
            "https://top.example",
            false);
    }

    [Test]
    public void UnpartitionedCookieDoesNotRequirePartitionContext()
    {
        StoredCookie cookie = new StoredCookie
        {
            HasPartitionKey = false,
            PartitionKey = null!
        };

        CookiePartitionResult enabled = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext(true, null!),
            Options(true));
        CookiePartitionResult disabled = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            RequestContext(false, ""),
            Options(false));

        AssertAllowed(enabled, true, false, false, false, "", true);
        AssertAllowed(disabled, false, false, false, false, "", true);
    }

    [Test]
    public void PartitionIdentitySeparatesUnpartitionedAndEveryKeyedPartition()
    {
        Assert.That(
            CookiePartitionEvaluator.PartitionKeysMatch(false, "", false, "ignored"),
            Is.True);
        Assert.That(
            CookiePartitionEvaluator.PartitionKeysMatch(false, "", true, "https://top.example"),
            Is.False);
        Assert.That(
            CookiePartitionEvaluator.PartitionKeysMatch(
                true,
                "https://top.example",
                true,
                "https://top.example"),
            Is.True);
        Assert.That(
            CookiePartitionEvaluator.PartitionKeysMatch(
                true,
                "https://top.example",
                true,
                "https://other.example"),
            Is.False);
        Assert.That(
            CookiePartitionEvaluator.PartitionKeysMatch(true, "", true, ""),
            Is.False);
    }

    [Test]
    public void InvalidGraphsReturnStableFailures()
    {
        CookieSetContext missingOptions = new CookieSetContext { Options = null! };
        StoredCookie missingKey = PartitionedCookie("");

        CookiePartitionResult nullSetCookie = CookiePartitionEvaluator.EvaluateCreation(
            null!,
            SetContext(true, true, "https://top.example"));
        CookiePartitionResult nullSetContext = CookiePartitionEvaluator.EvaluateCreation(
            new SetCookie(),
            null!);
        CookiePartitionResult nullSetOptions = CookiePartitionEvaluator.EvaluateCreation(
            new SetCookie { Partitioned = true },
            missingOptions);
        CookiePartitionResult nullStoredCookie = CookiePartitionEvaluator.EvaluateRetrieval(
            null!,
            RequestContext(true, "https://top.example"),
            Options(true));
        CookiePartitionResult nullRequestContext = CookiePartitionEvaluator.EvaluateRetrieval(
            new StoredCookie(),
            null!,
            Options(true));
        CookiePartitionResult nullRequestOptions = CookiePartitionEvaluator.EvaluateRetrieval(
            new StoredCookie(),
            new CookieRequestContext(),
            null!);
        CookiePartitionResult invalidStoredKey = CookiePartitionEvaluator.EvaluateRetrieval(
            missingKey,
            RequestContext(true, "https://top.example"),
            Options(true));

        AssertFailure(nullSetCookie, CookieErrorCode.InvalidAttribute);
        AssertFailure(nullSetContext, CookieErrorCode.InvalidAttribute);
        AssertFailure(nullSetOptions, CookieErrorCode.InvalidConfiguration);
        AssertFailure(nullStoredCookie, CookieErrorCode.InvalidAttribute);
        AssertFailure(nullRequestContext, CookieErrorCode.InvalidAttribute);
        AssertFailure(nullRequestOptions, CookieErrorCode.InvalidConfiguration);
        AssertFailure(invalidStoredKey, CookieErrorCode.InvalidAttribute);
    }

    static CookieJarOptions Options(bool enabled)
    {
        return new CookieJarOptions { EnablePartitionedCookies = enabled };
    }

    static CookieSetContext SetContext(
        bool enabled,
        bool hasTopLevelSite,
        string topLevelSite)
    {
        return new CookieSetContext
        {
            Options = Options(enabled),
            HasTopLevelSite = hasTopLevelSite,
            TopLevelSite = topLevelSite
        };
    }

    static CookieRequestContext RequestContext(bool hasTopLevelSite, string topLevelSite)
    {
        return new CookieRequestContext
        {
            HasTopLevelSite = hasTopLevelSite,
            TopLevelSite = topLevelSite
        };
    }

    static StoredCookie PartitionedCookie(string partitionKey)
    {
        return new StoredCookie
        {
            HasPartitionKey = true,
            PartitionKey = partitionKey
        };
    }

    static void AssertAllowed(
        CookiePartitionResult result,
        bool enforcementEnabled,
        bool attributeObserved,
        bool attributeIgnored,
        bool hasPartitionKey,
        string partitionKey,
        bool partitionMatched)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.EnforcementEnabled, Is.EqualTo(enforcementEnabled));
        Assert.That(result.AttributeObserved, Is.EqualTo(attributeObserved));
        Assert.That(result.AttributeIgnored, Is.EqualTo(attributeIgnored));
        Assert.That(result.HasPartitionKey, Is.EqualTo(hasPartitionKey));
        Assert.That(result.PartitionKey, Is.EqualTo(partitionKey));
        Assert.That(result.PartitionMatched, Is.EqualTo(partitionMatched));
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertDenied(
        CookiePartitionResult result,
        CookieErrorCode errorCode,
        bool enforcementEnabled,
        bool attributeObserved,
        bool hasPartitionKey,
        string partitionKey,
        bool partitionMatched)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.EnforcementEnabled, Is.EqualTo(enforcementEnabled));
        Assert.That(result.AttributeObserved, Is.EqualTo(attributeObserved));
        Assert.That(result.AttributeIgnored, Is.False);
        Assert.That(result.HasPartitionKey, Is.EqualTo(hasPartitionKey));
        Assert.That(result.PartitionKey, Is.EqualTo(partitionKey));
        Assert.That(result.PartitionMatched, Is.EqualTo(partitionMatched));
        AssertIssue(result, errorCode);
    }

    static void AssertFailure(CookiePartitionResult result, CookieErrorCode errorCode)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.AttributeIgnored, Is.False);
        Assert.That(result.HasPartitionKey, Is.False);
        Assert.That(result.PartitionKey, Is.Empty);
        Assert.That(result.PartitionMatched, Is.False);
        AssertIssue(result, errorCode);
    }

    static void AssertIssue(CookiePartitionResult result, CookieErrorCode errorCode)
    {
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(errorCode));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
        Assert.That(result.Issues[0].HasOffset, Is.False);
        Assert.That(result.Issues[0].SafeFragment, Is.Empty);
    }
}
