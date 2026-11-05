namespace cookies.Tests;

public class CookieModelValidatorTests
{
    [Test]
    public void CompleteSafeSetCookieModelIsValid()
    {
        SetCookie cookie = ValidSetCookie();

        CookieValidationResult result = CookieModelValidator.ValidateSetCookie(
            cookie,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void OrderedCookiePairsAllowDuplicatesAndEmptyValues()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "id", Value = "1" },
            new CookiePair { Name = "id", Value = "2" },
            new CookiePair { Name = "empty", Value = "" },
            new CookiePair { Name = "punctuation!#$%&'*+-.^_`|~", Value = "a=b" }
        ];

        CookieValidationResult result = CookieModelValidator.ValidateCookiePairs(
            pairs,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void CookiePairsIndependentlyEnforceStrictNamesAndValues()
    {
        CookiePair invalidName = new CookiePair { Name = "bad name", Value = "1" };
        CookiePair invalidValue = new CookiePair { Name = "id", Value = "bad value" };

        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([invalidName], new CookieSerializationOptions()),
            CookieErrorCode.InvalidName);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([invalidValue], new CookieSerializationOptions()),
            CookieErrorCode.InvalidValue);
    }

    [TestCase("")]
    [TestCase("bad name")]
    [TestCase("bad:name")]
    [TestCase("é")]
    public void SetCookieRequiresANonemptyAsciiTokenName(string name)
    {
        SetCookie cookie = ValidSetCookie();
        cookie.Name = name;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, new CookieSerializationOptions()),
            CookieErrorCode.InvalidName);
    }

    [TestCase("a b")]
    [TestCase("a,b")]
    [TestCase("\"quoted\"")]
    [TestCase("a\\b")]
    [TestCase("a;b")]
    [TestCase("é")]
    public void CanonicalSetCookieValuesRequireCookieOctets(string value)
    {
        SetCookie cookie = ValidSetCookie();
        cookie.Value = value;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, new CookieSerializationOptions()),
            CookieErrorCode.InvalidValue);
    }

    [Test]
    public void CompatibleQuotedHintAllowsOnlySafeQuotedOutput()
    {
        CookieSerializationOptions compatible = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = true
        };
        SetCookie safe = ValidSetCookie();
        safe.Value = "a value,with commas";
        safe.IsValueQuoted = true;
        SetCookie unsafeCookie = ValidSetCookie();
        unsafeCookie.Value = "back\\slash";
        unsafeCookie.IsValueQuoted = true;
        CookiePair safePair = new CookiePair
        {
            Name = "id",
            Value = "a value,with commas",
            IsQuoted = true
        };

        CookieValidationResult safeCookieResult = CookieModelValidator.ValidateSetCookie(safe, compatible);
        CookieValidationResult unsafeCookieResult = CookieModelValidator.ValidateSetCookie(unsafeCookie, compatible);
        CookieValidationResult safePairResult = CookieModelValidator.ValidateCookiePairs(
            [safePair],
            compatible);

        Assert.That(safeCookieResult.Success, Is.True);
        Assert.That(safePairResult.Success, Is.True);
        AssertFailure(unsafeCookieResult, CookieErrorCode.InvalidValue);
    }

    [Test]
    public void QuotedHintCanonicalizationStillRequiresUnquotedCookieOctets()
    {
        SetCookie cookie = ValidSetCookie();
        cookie.Value = "value with space";
        cookie.IsValueQuoted = true;

        CookieValidationResult result = CookieModelValidator.ValidateSetCookie(
            cookie,
            new CookieSerializationOptions());

        AssertFailure(result, CookieErrorCode.InvalidValue);
    }

    [TestCase("name", "bad\0value")]
    [TestCase("na\rme", "value")]
    [TestCase("name", "bad\nvalue")]
    public void HeaderInjectionHasAStableProducerFailure(string name, string value)
    {
        SetCookie cookie = ValidSetCookie();
        cookie.Name = name;
        cookie.Value = value;
        CookiePair pair = new CookiePair { Name = name, Value = value };

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, new CookieSerializationOptions()),
            CookieErrorCode.HeaderInjection);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([pair], new CookieSerializationOptions()),
            CookieErrorCode.HeaderInjection);
    }

    [Test]
    public void NameValueLimitsAcceptTheBoundaryAndRejectOneOctetOver()
    {
        SetCookie cookie = new SetCookie { Name = "id", Value = "1" };
        CookiePair pair = new CookiePair { Name = "id", Value = "1" };
        CookieSerializationOptions boundary = new CookieSerializationOptions();
        boundary.Limits.MaximumNameValueOctets = 3;
        CookieSerializationOptions over = new CookieSerializationOptions();
        over.Limits.MaximumNameValueOctets = 2;

        Assert.That(CookieModelValidator.ValidateSetCookie(cookie, boundary).Success, Is.True);
        Assert.That(CookieModelValidator.ValidateCookiePairs([pair], boundary).Success, Is.True);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, over),
            CookieErrorCode.NameValueLimitExceeded);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([pair], over),
            CookieErrorCode.NameValueLimitExceeded);
    }

    [TestCase(".example.test")]
    [TestCase("example.test.")]
    [TestCase("a..test")]
    [TestCase("-example.test")]
    [TestCase("example-.test")]
    [TestCase("under_score.test")]
    public void DomainAttributeRequiresStrictAsciiLabels(string domain)
    {
        SetCookie cookie = ValidSetCookie();
        cookie.Domain = domain;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, new CookieSerializationOptions()),
            CookieErrorCode.InvalidAttribute);
    }

    [TestCase("")]
    [TestCase("relative")]
    [TestCase("/bad;path")]
    public void PathAttributeMustBeAbsoluteAndSafe(string path)
    {
        SetCookie cookie = ValidSetCookie();
        cookie.Path = path;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, new CookieSerializationOptions()),
            CookieErrorCode.InvalidPath);
    }

    [Test]
    public void AttributeHeaderInjectionIsNeverReportedAsOrdinaryInvalidity()
    {
        SetCookie domain = ValidSetCookie();
        domain.Domain = "example.test\rInjected";
        SetCookie path = ValidSetCookie();
        path.Path = "/path\nInjected";
        SetCookie extension = ValidSetCookie();
        extension.Extensions[0].Value = "High\0Injected";

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(domain, new CookieSerializationOptions()),
            CookieErrorCode.HeaderInjection);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(path, new CookieSerializationOptions()),
            CookieErrorCode.HeaderInjection);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(extension, new CookieSerializationOptions()),
            CookieErrorCode.HeaderInjection);
    }

    [Test]
    public void ExpiresAndSameSitePresenceRequireSerializableValues()
    {
        SetCookie oldExpires = ValidSetCookie();
        oldExpires.Expires = new DateTimeOffset(1600, 1, 1, 0, 0, 0, TimeSpan.Zero);
        SetCookie defaultSameSite = ValidSetCookie();
        defaultSameSite.SameSiteMode = CookieSameSiteMode.Default;
        SetCookie invalidSameSite = ValidSetCookie();
        invalidSameSite.SameSiteMode = (CookieSameSiteMode)99;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(oldExpires, new CookieSerializationOptions()),
            CookieErrorCode.InvalidDate);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(defaultSameSite, new CookieSerializationOptions()),
            CookieErrorCode.InvalidAttribute);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(invalidSameSite, new CookieSerializationOptions()),
            CookieErrorCode.InvalidAttribute);
    }

    [Test]
    public void EveryAttributeValueUsesTheConfiguredOctetLimit()
    {
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumAttributeValueOctets = 2;
        SetCookie expires = ValidSetCookie();
        SetCookie maxAge = ValidSetCookie();
        maxAge.HasExpires = false;
        maxAge.MaxAgeSeconds = 100;
        SetCookie domain = ValidSetCookie();
        domain.HasExpires = false;
        domain.HasMaxAge = false;
        SetCookie path = ValidSetCookie();
        path.HasExpires = false;
        path.HasMaxAge = false;
        path.HasDomain = false;
        SetCookie sameSite = ValidSetCookie();
        sameSite.HasExpires = false;
        sameSite.HasMaxAge = false;
        sameSite.HasDomain = false;
        sameSite.HasPath = false;
        SetCookie extension = new SetCookie
        {
            Name = "id",
            Value = "1",
            Extensions = [new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" }]
        };

        AssertFailure(CookieModelValidator.ValidateSetCookie(expires, options), CookieErrorCode.AttributeLimitExceeded);
        AssertFailure(CookieModelValidator.ValidateSetCookie(maxAge, options), CookieErrorCode.AttributeLimitExceeded);
        AssertFailure(CookieModelValidator.ValidateSetCookie(domain, options), CookieErrorCode.AttributeLimitExceeded);
        AssertFailure(CookieModelValidator.ValidateSetCookie(path, options), CookieErrorCode.AttributeLimitExceeded);
        AssertFailure(CookieModelValidator.ValidateSetCookie(sameSite, options), CookieErrorCode.AttributeLimitExceeded);
        AssertFailure(CookieModelValidator.ValidateSetCookie(extension, options), CookieErrorCode.AttributeLimitExceeded);
    }

    [Test]
    public void MaxAgeSizingHandlesTheEntireSignedLongRange()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "1",
            HasMaxAge = true,
            MaxAgeSeconds = long.MinValue
        };
        CookieSerializationOptions boundary = new CookieSerializationOptions();
        boundary.Limits.MaximumAttributeValueOctets = 20;
        CookieSerializationOptions over = new CookieSerializationOptions();
        over.Limits.MaximumAttributeValueOctets = 19;

        Assert.That(CookieModelValidator.ValidateSetCookie(cookie, boundary).Success, Is.True);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, over),
            CookieErrorCode.AttributeLimitExceeded);
    }

    [Test]
    public void SafeUnknownExtensionsCannotShadowRecognizedAttributes()
    {
        SetCookie safe = new SetCookie
        {
            Name = "id",
            Value = "1",
            Extensions =
            [
                new CookieAttribute { Name = "Priority", HasValue = true, Value = "High Value=1" },
                new CookieAttribute { Name = "Flag", HasValue = false, Value = "" }
            ]
        };
        SetCookie shadow = new SetCookie
        {
            Name = "id",
            Value = "1",
            Extensions = [new CookieAttribute { Name = "sEcUrE", HasValue = false, Value = "" }]
        };

        Assert.That(CookieModelValidator.ValidateSetCookie(safe, new CookieSerializationOptions()).Success, Is.True);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(shadow, new CookieSerializationOptions()),
            CookieErrorCode.InvalidAttribute);
    }

    [Test]
    public void InvalidExtensionGraphsUseStableFailures()
    {
        SetCookie nullExtension = ValidSetCookie();
        nullExtension.Extensions = new CookieAttribute[] { null! };
        SetCookie invalidName = ValidSetCookie();
        invalidName.Extensions[0].Name = "Bad Name";
        SetCookie invalidValue = ValidSetCookie();
        invalidValue.Extensions[0].Value = "bad;value";
        SetCookie inconsistent = ValidSetCookie();
        inconsistent.Extensions[0].HasValue = false;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(nullExtension, new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(invalidName, new CookieSerializationOptions()),
            CookieErrorCode.InvalidAttribute);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(invalidValue, new CookieSerializationOptions()),
            CookieErrorCode.InvalidAttribute);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(inconsistent, new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
    }

    [Test]
    public void SetCookieOutputLimitUsesTheExactSerializedOctetCount()
    {
        SetCookie cookie = new SetCookie { Name = "id", Value = "1" };
        CookieSerializationOptions boundary = new CookieSerializationOptions();
        boundary.Limits.MaximumInputLineOctets = 4;
        CookieSerializationOptions over = new CookieSerializationOptions();
        over.Limits.MaximumInputLineOctets = 3;

        Assert.That(CookieModelValidator.ValidateSetCookie(cookie, boundary).Success, Is.True);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, over),
            CookieErrorCode.InputLimitExceeded);
    }

    [Test]
    public void CompleteSetCookieOutputSizeIncludesEveryEmittedDelimiterAndAttribute()
    {
        SetCookie cookie = ValidSetCookie();
        CookieSerializationOptions boundary = new CookieSerializationOptions();
        boundary.Limits.MaximumInputLineOctets = 156;
        CookieSerializationOptions over = new CookieSerializationOptions();
        over.Limits.MaximumInputLineOctets = 155;

        Assert.That(CookieModelValidator.ValidateSetCookie(cookie, boundary).Success, Is.True);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(cookie, over),
            CookieErrorCode.InputLimitExceeded);
    }

    [Test]
    public void RequestHeaderLimitSupportsBoundarySplittingAndIndivisibleFailures()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "b", Value = "2" }
        ];
        CookieSerializationOptions split = new CookieSerializationOptions();
        split.Limits.MaximumRequestHeaderOctets = 3;
        CookieSerializationOptions unsplit = new CookieSerializationOptions
        {
            SplitCookieFields = false
        };
        unsplit.Limits.MaximumRequestHeaderOctets = 7;
        CookieSerializationOptions indivisible = new CookieSerializationOptions();
        indivisible.Limits.MaximumRequestHeaderOctets = 2;

        Assert.That(CookieModelValidator.ValidateCookiePairs(pairs, split).Success, Is.True);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs(pairs, unsplit),
            CookieErrorCode.HeaderLimitExceeded);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs(pairs, indivisible),
            CookieErrorCode.HeaderLimitExceeded);
    }

    [Test]
    public void NullModelsAndMembersUseSerializationFailure()
    {
        SetCookie nullName = ValidSetCookie();
        nullName.Name = null!;
        CookiePair nullValue = new CookiePair { Name = "id", Value = null! };

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(null!, new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(nullName, new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs(null!, new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs(new CookiePair[] { null! }, new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([nullValue], new CookieSerializationOptions()),
            CookieErrorCode.SerializationFailure);
    }

    [Test]
    public void InvalidSerializationOptionsUseConfigurationFailure()
    {
        CookieSerializationOptions nullLimits = new CookieSerializationOptions { Limits = null! };
        CookieSerializationOptions invalidMode = new CookieSerializationOptions
        {
            ComplianceMode = (CookieComplianceMode)99
        };
        CookieSerializationOptions negativeLimit = new CookieSerializationOptions();
        negativeLimit.Limits.MaximumInputLineOctets = -1;

        AssertFailure(
            CookieModelValidator.ValidateSetCookie(ValidSetCookie(), null!),
            CookieErrorCode.InvalidConfiguration);
        AssertFailure(
            CookieModelValidator.ValidateSetCookie(ValidSetCookie(), nullLimits),
            CookieErrorCode.InvalidConfiguration);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([], invalidMode),
            CookieErrorCode.InvalidConfiguration);
        AssertFailure(
            CookieModelValidator.ValidateCookiePairs([], negativeLimit),
            CookieErrorCode.InvalidConfiguration);
    }

    static SetCookie ValidSetCookie()
    {
        return new SetCookie
        {
            Name = "id",
            Value = "a=b",
            HasExpires = true,
            Expires = new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero),
            HasMaxAge = true,
            MaxAgeSeconds = -1,
            HasDomain = true,
            Domain = "example.test",
            HasPath = true,
            Path = "/account",
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict,
            Partitioned = true,
            Extensions =
            [
                new CookieAttribute
                {
                    Name = "Priority",
                    HasValue = true,
                    Value = "High"
                }
            ]
        };
    }

    static void AssertFailure(CookieValidationResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
        Assert.That(result.Issues[0].HasOffset, Is.False);
        Assert.That(result.Issues[0].SafeFragment, Is.Empty);
    }
}
