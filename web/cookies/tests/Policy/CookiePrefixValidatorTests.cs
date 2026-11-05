namespace cookies.Tests;

public class CookiePrefixValidatorTests
{
    [Test]
    public void OrdinaryCookieDoesNotRequireOrInvokeSecureOriginPolicy()
    {
        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            new SetCookie { Name = "session" },
            Context(ErrorOptions(), null),
            new ThrowingSecureOriginPolicy());

        AssertAllowed(result, false, false, false, true, false, false, false);
    }

    [Test]
    public void ValidSecurePrefixRequiresSecureFlagAndSecureOrigin()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = true
        };

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie,
            Context(ErrorOptions(), new Uri("https://example.test/path")),
            new DefaultSecureOriginPolicy(false));

        AssertAllowed(result, true, false, false, true, false, true, true);
    }

    [Test]
    public void SecurePrefixRejectsMissingSecureFlagBeforeCallingOriginPolicy()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = false
        };

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie,
            Context(ErrorOptions(), null),
            new ThrowingSecureOriginPolicy());

        AssertErrorRejected(result, true, false, false, true, false, false);
    }

    [Test]
    public void SecurePrefixRejectsAnInsecureOrigin()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = true
        };

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie,
            Context(ErrorOptions(), new Uri("http://example.test/path")),
            new DefaultSecureOriginPolicy(false));

        AssertErrorRejected(result, true, false, false, true, true, false);
    }

    [Test]
    public void ValidHostPrefixRequiresEveryHostInvariant()
    {
        SetCookie cookie = ValidHostCookie();

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie,
            Context(ErrorOptions(), new Uri("https://example.test/path")),
            new DefaultSecureOriginPolicy(false));

        AssertAllowed(result, false, true, false, true, false, true, true);
    }

    [Test]
    public void HostPrefixRequiresExplicitRootPathNotMerelyDefaultedRoot()
    {
        SetCookie absentPath = ValidHostCookie();
        absentPath.HasPath = false;
        absentPath.Path = "/";
        SetCookie emptyPath = ValidHostCookie();
        emptyPath.Path = "";
        SetCookie nonRootPath = ValidHostCookie();
        nonRootPath.Path = "/account";

        CookiePrefixValidationResult absent = ValidateWithoutOrigin(absentPath);
        CookiePrefixValidationResult empty = ValidateWithoutOrigin(emptyPath);
        CookiePrefixValidationResult nonRoot = ValidateWithoutOrigin(nonRootPath);

        AssertErrorRejected(absent, false, true, false, true, false, false);
        AssertErrorRejected(empty, false, true, false, true, false, false);
        AssertErrorRejected(nonRoot, false, true, false, true, false, false);
    }

    [Test]
    public void HostPrefixRejectsAnySuppliedDomainAttribute()
    {
        SetCookie populatedDomain = ValidHostCookie();
        populatedDomain.HasDomain = true;
        populatedDomain.Domain = "example.test";
        SetCookie emptyDomain = ValidHostCookie();
        emptyDomain.HasDomain = true;
        emptyDomain.Domain = "";

        CookiePrefixValidationResult populated = ValidateWithoutOrigin(populatedDomain);
        CookiePrefixValidationResult empty = ValidateWithoutOrigin(emptyDomain);

        AssertErrorRejected(populated, false, true, false, true, false, false);
        AssertErrorRejected(empty, false, true, false, true, false, false);
    }

    [Test]
    public void HostPrefixRejectsMissingSecureFlagBeforeOtherDependencies()
    {
        SetCookie cookie = ValidHostCookie();
        cookie.Secure = false;

        CookiePrefixValidationResult result = ValidateWithoutOrigin(cookie);

        AssertErrorRejected(result, false, true, false, true, false, false);
    }

    [TestCase("__SeCuRe-session", true, false)]
    [TestCase("__hOsT-session", false, true)]
    public void BisProfileAlwaysMatchesPrefixesCaseInsensitively(
        string name,
        bool securePrefix,
        bool hostPrefix)
    {
        CookieJarOptions options = ErrorOptions();
        options.UseCaseInsensitivePrefixes = false;
        SetCookie cookie = hostPrefix ? ValidHostCookie() : new SetCookie { Secure = true };
        cookie.Name = name;

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie,
            Context(options, new Uri("https://example.test/path")),
            new DefaultSecureOriginPolicy(false));

        AssertAllowed(result, securePrefix, hostPrefix, false, true, false, true, true);
    }

    [TestCase(CookieComplianceMode.Rfc6265)]
    [TestCase(CookieComplianceMode.Compatible)]
    public void BaselineAndCompatibleProfilesRetainConfiguredCaseBehavior(
        CookieComplianceMode complianceMode)
    {
        CookieJarOptions exactOptions = ErrorOptions();
        exactOptions.ComplianceMode = complianceMode;
        exactOptions.UseCaseInsensitivePrefixes = false;
        CookieJarOptions insensitiveOptions = ErrorOptions();
        insensitiveOptions.ComplianceMode = complianceMode;
        insensitiveOptions.UseCaseInsensitivePrefixes = true;
        SetCookie cookie = new SetCookie
        {
            Name = "__SeCuRe-session",
            Secure = false
        };

        CookiePrefixValidationResult exact = CookiePrefixValidator.Validate(
            cookie, Context(exactOptions, null), new ThrowingSecureOriginPolicy());
        CookiePrefixValidationResult insensitive = CookiePrefixValidator.Validate(
            cookie, Context(insensitiveOptions, null), new ThrowingSecureOriginPolicy());

        AssertAllowed(exact, false, false, false, false, false, false, false);
        AssertErrorRejected(insensitive, true, false, false, true, false, false);
    }

    [Test]
    public void ExactCasePrefixIsRecognizedInEveryProfile()
    {
        CookieJarOptions options = ErrorOptions();
        options.ComplianceMode = CookieComplianceMode.Rfc6265;
        options.UseCaseInsensitivePrefixes = false;

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            new SetCookie { Name = "__Secure-session", Secure = false },
            Context(options, null),
            new ThrowingSecureOriginPolicy());

        AssertErrorRejected(result, true, false, false, false, false, false);
    }

    [Test]
    public void SilentErrorAndDisabledPoliciesHaveDistinctOutcomes()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = false
        };
        CookieJarOptions silentOptions = ErrorOptions();
        silentOptions.PrefixPolicy = CookiePrefixPolicy.SilentReject;
        CookieJarOptions errorOptions = ErrorOptions();
        CookieJarOptions disabledOptions = ErrorOptions();
        disabledOptions.PrefixPolicy = CookiePrefixPolicy.Disabled;

        CookiePrefixValidationResult silent = CookiePrefixValidator.Validate(
            cookie, Context(silentOptions, null), new ThrowingSecureOriginPolicy());
        CookiePrefixValidationResult error = CookiePrefixValidator.Validate(
            cookie, Context(errorOptions, null), new ThrowingSecureOriginPolicy());
        CookiePrefixValidationResult disabled = CookiePrefixValidator.Validate(
            cookie, Context(disabledOptions, null), new ThrowingSecureOriginPolicy());

        AssertSilentRejected(silent, true, false, false, true);
        AssertErrorRejected(error, true, false, false, true, false, false);
        AssertAllowed(disabled, true, false, false, true, true, false, false);
    }

    [TestCase("__Secure-hidden", true, false)]
    [TestCase("__Host-hidden", false, true)]
    [TestCase("__sEcUrE-hidden", true, false)]
    [TestCase("__hOsT-hidden", false, true)]
    public void NamelessCookiesCannotHidePrefixInTheirValue(
        string value,
        bool securePrefix,
        bool hostPrefix)
    {
        SetCookie cookie = new SetCookie
        {
            Name = "",
            Value = value,
            Secure = true,
            HasPath = true,
            Path = "/"
        };

        CookiePrefixValidationResult result = CookiePrefixValidator.Validate(
            cookie, Context(ErrorOptions(), null), new ThrowingSecureOriginPolicy());

        AssertErrorRejected(result, securePrefix, hostPrefix, true, true, false, false);
    }

    [Test]
    public void ValidPrefixHonorsReplacementSecureOriginPolicy()
    {
        Uri origin = new Uri("custom://device.example/path");
        RecordingSecureOriginPolicy allowing = new RecordingSecureOriginPolicy(true);
        RecordingSecureOriginPolicy rejecting = new RecordingSecureOriginPolicy(false);
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = true
        };

        CookiePrefixValidationResult allowed = CookiePrefixValidator.Validate(
            cookie, Context(ErrorOptions(), origin), allowing);
        CookiePrefixValidationResult rejected = CookiePrefixValidator.Validate(
            cookie, Context(ErrorOptions(), origin), rejecting);

        AssertAllowed(allowed, true, false, false, true, false, true, true);
        AssertErrorRejected(rejected, true, false, false, true, true, false);
        Assert.That(allowing.Origins, Is.EqualTo(new[] { origin }));
        Assert.That(rejecting.Origins, Is.EqualTo(new[] { origin }));
    }

    [Test]
    public void MissingThrowingOrInvalidOriginFailsOnlyAfterStaticRequirementsPass()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "__Secure-session",
            Secure = true
        };
        CookieSetContext validContext = Context(
            ErrorOptions(), new Uri("https://example.test/path"));
        CookieSetContext nullOrigin = Context(ErrorOptions(), null);
        CookieSetContext relativeOrigin = Context(
            ErrorOptions(), new Uri("relative", UriKind.Relative));

        CookiePrefixValidationResult missingPolicy = CookiePrefixValidator.Validate(
            cookie, validContext, null!);
        CookiePrefixValidationResult throwingPolicy = CookiePrefixValidator.Validate(
            cookie, validContext, new ThrowingSecureOriginPolicy());
        CookiePrefixValidationResult absent = CookiePrefixValidator.Validate(
            cookie, nullOrigin, new DefaultSecureOriginPolicy(false));
        CookiePrefixValidationResult relative = CookiePrefixValidator.Validate(
            cookie, relativeOrigin, new DefaultSecureOriginPolicy(false));

        AssertFailure(missingPolicy, CookieErrorCode.InvalidConfiguration);
        AssertFailure(throwingPolicy, CookieErrorCode.InvalidConfiguration);
        AssertFailure(absent, CookieErrorCode.InvalidHost);
        AssertFailure(relative, CookieErrorCode.InvalidHost);
    }

    [Test]
    public void InvalidInputsAndOptionsReturnStableFailures()
    {
        CookieSetContext nullOptions = new CookieSetContext { Options = null! };
        CookieJarOptions invalidPolicyOptions = ErrorOptions();
        invalidPolicyOptions.PrefixPolicy = (CookiePrefixPolicy)99;
        CookieJarOptions invalidModeOptions = ErrorOptions();
        invalidModeOptions.ComplianceMode = (CookieComplianceMode)99;

        CookiePrefixValidationResult nullCookie = CookiePrefixValidator.Validate(
            null!, Context(ErrorOptions(), null), null!);
        CookiePrefixValidationResult missingOptions = CookiePrefixValidator.Validate(
            new SetCookie(), nullOptions, null!);
        CookiePrefixValidationResult invalidPolicy = CookiePrefixValidator.Validate(
            new SetCookie(), Context(invalidPolicyOptions, null), null!);
        CookiePrefixValidationResult invalidMode = CookiePrefixValidator.Validate(
            new SetCookie(), Context(invalidModeOptions, null), null!);

        AssertFailure(nullCookie, CookieErrorCode.InvalidAttribute);
        AssertFailure(missingOptions, CookieErrorCode.InvalidConfiguration);
        AssertFailure(invalidPolicy, CookieErrorCode.InvalidConfiguration);
        AssertFailure(invalidMode, CookieErrorCode.InvalidConfiguration);
    }

    static CookiePrefixValidationResult ValidateWithoutOrigin(SetCookie cookie)
    {
        return CookiePrefixValidator.Validate(
            cookie,
            Context(ErrorOptions(), null),
            new ThrowingSecureOriginPolicy());
    }

    static CookieJarOptions ErrorOptions()
    {
        return new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            PrefixPolicy = CookiePrefixPolicy.ErrorReject
        };
    }

    static CookieSetContext Context(CookieJarOptions options, Uri? origin)
    {
        return new CookieSetContext
        {
            Options = options,
            Origin = origin
        };
    }

    static SetCookie ValidHostCookie()
    {
        return new SetCookie
        {
            Name = "__Host-session",
            Secure = true,
            HasPath = true,
            Path = "/",
            HasDomain = false
        };
    }

    static void AssertAllowed(
        CookiePrefixValidationResult result,
        bool securePrefix,
        bool hostPrefix,
        bool namelessPrefix,
        bool caseInsensitive,
        bool enforcementDisabled,
        bool originWasEvaluated,
        bool originIsSecure)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.HasSecurePrefix, Is.EqualTo(securePrefix));
        Assert.That(result.HasHostPrefix, Is.EqualTo(hostPrefix));
        Assert.That(result.WasNamelessPrefix, Is.EqualTo(namelessPrefix));
        Assert.That(result.UsedCaseInsensitiveMatch, Is.EqualTo(caseInsensitive));
        Assert.That(result.EnforcementDisabled, Is.EqualTo(enforcementDisabled));
        Assert.That(result.SilentlyRejected, Is.False);
        Assert.That(result.OriginWasEvaluated, Is.EqualTo(originWasEvaluated));
        Assert.That(result.OriginIsSecure, Is.EqualTo(originIsSecure));
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertSilentRejected(
        CookiePrefixValidationResult result,
        bool securePrefix,
        bool hostPrefix,
        bool namelessPrefix,
        bool caseInsensitive)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.HasSecurePrefix, Is.EqualTo(securePrefix));
        Assert.That(result.HasHostPrefix, Is.EqualTo(hostPrefix));
        Assert.That(result.WasNamelessPrefix, Is.EqualTo(namelessPrefix));
        Assert.That(result.UsedCaseInsensitiveMatch, Is.EqualTo(caseInsensitive));
        Assert.That(result.EnforcementDisabled, Is.False);
        Assert.That(result.SilentlyRejected, Is.True);
        Assert.That(result.Issues, Is.Empty);
    }

    static void AssertErrorRejected(
        CookiePrefixValidationResult result,
        bool securePrefix,
        bool hostPrefix,
        bool namelessPrefix,
        bool caseInsensitive,
        bool originWasEvaluated,
        bool originIsSecure)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.PrefixViolation));
        Assert.That(result.HasSecurePrefix, Is.EqualTo(securePrefix));
        Assert.That(result.HasHostPrefix, Is.EqualTo(hostPrefix));
        Assert.That(result.WasNamelessPrefix, Is.EqualTo(namelessPrefix));
        Assert.That(result.UsedCaseInsensitiveMatch, Is.EqualTo(caseInsensitive));
        Assert.That(result.EnforcementDisabled, Is.False);
        Assert.That(result.SilentlyRejected, Is.False);
        Assert.That(result.OriginWasEvaluated, Is.EqualTo(originWasEvaluated));
        Assert.That(result.OriginIsSecure, Is.EqualTo(originIsSecure));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.PrefixViolation));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }

    static void AssertFailure(CookiePrefixValidationResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Allowed, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.EnforcementDisabled, Is.False);
        Assert.That(result.SilentlyRejected, Is.False);
        Assert.That(result.OriginWasEvaluated, Is.False);
        Assert.That(result.OriginIsSecure, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
