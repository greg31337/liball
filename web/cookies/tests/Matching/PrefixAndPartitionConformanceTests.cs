namespace cookies.Tests;

public class PrefixAndPartitionConformanceTests
{
    [TestCase(CookieComplianceMode.Rfc6265, false, false)]
    [TestCase(CookieComplianceMode.Rfc6265, true, true)]
    [TestCase(CookieComplianceMode.Rfc6265Bis, false, true)]
    [TestCase(CookieComplianceMode.Rfc6265Bis, true, true)]
    [TestCase(CookieComplianceMode.Compatible, false, false)]
    [TestCase(CookieComplianceMode.Compatible, true, true)]
    public void MixedCasePrefixRecognitionIsProfileAndPolicySpecific(
        CookieComplianceMode mode,
        bool configuredInsensitive,
        bool expectedRecognized)
    {
        CookieJarOptions options = PrefixOptions(CookiePrefixPolicy.ErrorReject);
        options.ComplianceMode = mode;
        options.UseCaseInsensitivePrefixes = configuredInsensitive;
        SetCookie cookie = new SetCookie
        {
            Name = "__SeCuRe-session",
            Secure = false
        };

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie, PrefixContext(options, null), new ThrowingSecureOriginPolicy());

        Assert.That(result.Success, Is.True);
        Assert.That(result.HasSecurePrefix, Is.EqualTo(expectedRecognized));
        Assert.That(result.Allowed, Is.EqualTo(!expectedRecognized));
        Assert.That(result.ErrorCode, Is.EqualTo(expectedRecognized
            ? CookieErrorCode.PrefixViolation
            : CookieErrorCode.None));
    }

    [Test]
    public void SecurePrefixRequiresSecureFlagAndSecureOrigin()
    {
        SetCookie missingFlag = new SetCookie
        {
            Name = "__Secure-session",
            Secure = false
        };
        SetCookie secure = new SetCookie
        {
            Name = "__Secure-session",
            Secure = true
        };
        CookieJarOptions options = PrefixOptions(CookiePrefixPolicy.ErrorReject);

        CookiePrefixValidationResult flag = CookiePrefixValidator.Validate(
            missingFlag,
            PrefixContext(options, null),
            new ThrowingSecureOriginPolicy());
        CookiePrefixValidationResult origin = CookiePrefixValidator.Validate(
            secure,
            PrefixContext(options, new Uri("http://example.test/")),
            new DefaultSecureOriginPolicy(false));
        CookiePrefixValidationResult valid = CookiePrefixValidator.Validate(
            secure,
            PrefixContext(options, new Uri("https://example.test/")),
            new DefaultSecureOriginPolicy(false));

        AssertPrefixRejected(flag);
        Assert.That(flag.OriginWasEvaluated, Is.False);
        AssertPrefixRejected(origin);
        Assert.That(origin.OriginWasEvaluated, Is.True);
        Assert.That(origin.OriginIsSecure, Is.False);
        AssertPrefixAllowed(valid);
        Assert.That(valid.OriginWasEvaluated, Is.True);
        Assert.That(valid.OriginIsSecure, Is.True);
    }

    [Test]
    public void HostPrefixRequiresSecureNoDomainAndExplicitRootPath()
    {
        CookieJarOptions options = PrefixOptions(CookiePrefixPolicy.ErrorReject);
        SetCookie validCookie = HostCookie();
        SetCookie missingSecure = HostCookie();
        missingSecure.Secure = false;
        SetCookie suppliedDomain = HostCookie();
        suppliedDomain.HasDomain = true;
        suppliedDomain.Domain = "example.test";
        SetCookie absentPath = HostCookie();
        absentPath.HasPath = false;
        absentPath.Path = "/";
        SetCookie nonRootPath = HostCookie();
        nonRootPath.Path = "/account";

        CookiePrefixValidationResult valid = ValidateHost(validCookie, options);
        CookiePrefixValidationResult secure = ValidateStaticHost(
            missingSecure, options);
        CookiePrefixValidationResult domain = ValidateStaticHost(
            suppliedDomain, options);
        CookiePrefixValidationResult path = ValidateStaticHost(absentPath, options);
        CookiePrefixValidationResult root = ValidateStaticHost(
            nonRootPath, options);

        AssertPrefixAllowed(valid);
        AssertPrefixRejected(secure);
        AssertPrefixRejected(domain);
        AssertPrefixRejected(path);
        AssertPrefixRejected(root);
    }

    [Test]
    public void HostPrefixAlsoRequiresASecureOriginAfterStaticRequirementsPass()
    {
        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            HostCookie(),
            PrefixContext(
                PrefixOptions(CookiePrefixPolicy.ErrorReject),
                new Uri("http://example.test/")),
            new DefaultSecureOriginPolicy(false));

        AssertPrefixRejected(result);
        Assert.That(result.OriginWasEvaluated, Is.True);
        Assert.That(result.OriginIsSecure, Is.False);
    }

    [TestCase(CookiePrefixPolicy.Disabled, true, false, CookieErrorCode.None)]
    [TestCase(CookiePrefixPolicy.SilentReject, false, true, CookieErrorCode.None)]
    [TestCase(CookiePrefixPolicy.ErrorReject, false, false, CookieErrorCode.PrefixViolation)]
    public void EveryPrefixPolicyModeHasADistinctInvalidPrefixOutcome(
        CookiePrefixPolicy policy,
        bool expectedAllowed,
        bool expectedSilent,
        CookieErrorCode expectedError)
    {
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = false
        };

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie,
            PrefixContext(PrefixOptions(policy), null),
            new ThrowingSecureOriginPolicy());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.EqualTo(expectedAllowed));
        Assert.That(result.SilentlyRejected, Is.EqualTo(expectedSilent));
        Assert.That(result.ErrorCode, Is.EqualTo(expectedError));
        Assert.That(result.EnforcementDisabled,
            Is.EqualTo(policy == CookiePrefixPolicy.Disabled));
    }

    [Test]
    public void PartitionedCreationRequiresOptInSecureAndTopLevelSite()
    {
        SetCookie secure = new SetCookie
        {
            Partitioned = true,
            Secure = true
        };
        SetCookie insecure = new SetCookie
        {
            Partitioned = true,
            Secure = false
        };

        CookiePartitionResult disabled = CookiePartitionEvaluator.EvaluateCreation(
            secure, PartitionSetContext(false, true, "https://top.example"));
        CookiePartitionResult missingSecure = CookiePartitionEvaluator.EvaluateCreation(
            insecure, PartitionSetContext(true, false, ""));
        CookiePartitionResult missingSite = CookiePartitionEvaluator.EvaluateCreation(
            secure, PartitionSetContext(true, false, ""));
        CookiePartitionResult valid = CookiePartitionEvaluator.EvaluateCreation(
            secure, PartitionSetContext(true, true, "https://top.example"));

        Assert.That(disabled.Allowed, Is.True);
        Assert.That(disabled.AttributeIgnored, Is.True);
        Assert.That(disabled.HasPartitionKey, Is.False);
        AssertPartitionDenied(
            missingSecure, CookieErrorCode.InsecureSecureCookie);
        AssertPartitionDenied(
            missingSite, CookieErrorCode.MissingPartitionContext);
        Assert.That(valid.Allowed, Is.True);
        Assert.That(valid.HasPartitionKey, Is.True);
        Assert.That(valid.PartitionKey, Is.EqualTo("https://top.example"));
    }

    [Test]
    public void PartitionPresenceAndKeyArePartOfCookieIdentity()
    {
        CookieIdentity unpartitioned = Identity(false, "");
        CookieIdentity first = Identity(true, "https://first.example");
        CookieIdentity sameFirst = Identity(true, "https://first.example");
        CookieIdentity second = Identity(true, "https://second.example");

        Assert.That(CookieIdentityUtility.AreEqual(first, sameFirst), Is.True);
        Assert.That(CookieIdentityUtility.AreEqual(unpartitioned, first), Is.False);
        Assert.That(CookieIdentityUtility.AreEqual(first, second), Is.False);
        Assert.That(CookieIdentityUtility.CreateKey(unpartitioned),
            Is.Not.EqualTo(CookieIdentityUtility.CreateKey(first)));
        Assert.That(CookieIdentityUtility.CreateKey(first),
            Is.Not.EqualTo(CookieIdentityUtility.CreateKey(second)));
    }

    [TestCase(true, "https://first.example", true, CookieErrorCode.None)]
    [TestCase(true, "https://second.example", false, CookieErrorCode.PartitionMismatch)]
    [TestCase(true, "http://first.example", false, CookieErrorCode.PartitionMismatch)]
    [TestCase(false, "", false, CookieErrorCode.MissingPartitionContext)]
    public void PartitionedRetrievalRequiresTheExactTopLevelSite(
        bool hasTopLevelSite,
        string topLevelSite,
        bool expectedAllowed,
        CookieErrorCode expectedError)
    {
        StoredCookie cookie = new StoredCookie
        {
            HasPartitionKey = true,
            PartitionKey = "https://first.example"
        };
        CookieRequestContext context = new CookieRequestContext
        {
            HasTopLevelSite = hasTopLevelSite,
            TopLevelSite = topLevelSite
        };

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            context,
            new CookieJarOptions { EnablePartitionedCookies = true });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.EqualTo(expectedAllowed));
        Assert.That(result.ErrorCode, Is.EqualTo(expectedError));
        Assert.That(result.PartitionMatched, Is.EqualTo(expectedAllowed));
    }

    [Test]
    public void DisabledPartitionFeatureWithholdsPreviouslyPartitionedCookies()
    {
        StoredCookie cookie = new StoredCookie
        {
            HasPartitionKey = true,
            PartitionKey = "https://top.example"
        };

        CookiePartitionResult result = CookiePartitionEvaluator.EvaluateRetrieval(
            cookie,
            new CookieRequestContext
            {
                HasTopLevelSite = true,
                TopLevelSite = "https://top.example"
            },
            new CookieJarOptions { EnablePartitionedCookies = false });

        AssertPartitionDenied(result, CookieErrorCode.PartitionMismatch);
    }

    static CookieJarOptions PrefixOptions(CookiePrefixPolicy policy)
    {
        return new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            PrefixPolicy = policy
        };
    }

    static CookieSetContext PrefixContext(
        CookieJarOptions options,
        Uri? origin)
    {
        return new CookieSetContext
        {
            Options = options,
            Origin = origin
        };
    }

    static SetCookie HostCookie()
    {
        return new SetCookie
        {
            Name = "__Host-session",
            Secure = true,
            HasDomain = false,
            HasPath = true,
            Path = "/"
        };
    }

    static CookiePrefixValidationResult ValidateHost(
        SetCookie cookie,
        CookieJarOptions options)
    {
        return CookiePrefixValidator.Validate(
            cookie,
            PrefixContext(options, new Uri("https://example.test/")),
            new DefaultSecureOriginPolicy(false));
    }

    static CookiePrefixValidationResult ValidateStaticHost(
        SetCookie cookie,
        CookieJarOptions options)
    {
        return CookiePrefixValidator.Validate(
            cookie,
            PrefixContext(options, null),
            new ThrowingSecureOriginPolicy());
    }

    static CookieSetContext PartitionSetContext(
        bool enabled,
        bool hasTopLevelSite,
        string topLevelSite)
    {
        return new CookieSetContext
        {
            Options = new CookieJarOptions
            {
                EnablePartitionedCookies = enabled
            },
            HasTopLevelSite = hasTopLevelSite,
            TopLevelSite = topLevelSite
        };
    }

    static CookieIdentity Identity(bool partitioned, string partitionKey)
    {
        return new CookieIdentity
        {
            Name = "session",
            Domain = "example.test",
            Path = "/",
            HasPartitionKey = partitioned,
            PartitionKey = partitionKey
        };
    }

    static void AssertPrefixAllowed(CookiePrefixValidationResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
    }

    static void AssertPrefixRejected(CookiePrefixValidationResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.PrefixViolation));
    }

    static void AssertPartitionDenied(
        CookiePartitionResult result,
        CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues.Single().ErrorCode, Is.EqualTo(expected));
    }
}
