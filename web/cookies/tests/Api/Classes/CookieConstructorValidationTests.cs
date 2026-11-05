namespace cookies.Tests;

public class CookieConstructorValidationTests
{
    static readonly DateTimeOffset EvaluationTime = new DateTimeOffset(
        2030,
        1,
        2,
        3,
        4,
        5,
        TimeSpan.Zero);

    [Test]
    public void ValidOptionAndDependencyGraphsConstructEveryConfiguredService()
    {
        CookieJarOptions options = ValidOptions();
        MatcherClock clock = new MatcherClock(EvaluationTime);
        MatcherPublicSuffixList suffixes =
            new MatcherPublicSuffixList("example.test", []);
        DefaultSecureOriginPolicy secureOrigins =
            new DefaultSecureOriginPolicy(false);
        CookieMatcher matcher = new CookieMatcher(
            options,
            suffixes,
            secureOrigins,
            clock);
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            suffixes,
            secureOrigins,
            clock);
        CookieJar jar = new CookieJar(
            options,
            new CookieParser(),
            policy,
            new MemoryCookieStore(),
            new CookieSerializer(),
            clock);
        NetscapeCookieFileCodec codec = new NetscapeCookieFileCodec(clock);

        Assert.That(matcher, Is.AssignableTo<ICookieMatcher>());
        Assert.That(policy, Is.AssignableTo<ICookiePolicy>());
        Assert.That(jar, Is.AssignableTo<ICookieJar>());
        Assert.That(codec, Is.AssignableTo<INetscapeCookieFileCodec>());
    }

    [Test]
    public void MatcherAndPolicyRejectMissingRequiredDependenciesAndPsl()
    {
        CookieJarOptions options = ValidOptions();
        MatcherClock clock = new MatcherClock(EvaluationTime);
        MatcherPublicSuffixList suffixes =
            new MatcherPublicSuffixList("example.test", []);
        DefaultSecureOriginPolicy secureOrigins =
            new DefaultSecureOriginPolicy(false);

        CookiePolicyException[] failures =
        [
            Assert.Throws<CookiePolicyException>(() => new CookieMatcher(
                null!, suffixes, secureOrigins, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieMatcher(
                options, suffixes, null!, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieMatcher(
                options, suffixes, secureOrigins, null!))!,
            Assert.Throws<CookiePolicyException>(() => new CookieMatcher(
                options, null, secureOrigins, clock))!,
            Assert.Throws<CookiePolicyException>(() => new DefaultCookiePolicy(
                null!, suffixes, secureOrigins, clock))!,
            Assert.Throws<CookiePolicyException>(() => new DefaultCookiePolicy(
                options, suffixes, null!, clock))!,
            Assert.Throws<CookiePolicyException>(() => new DefaultCookiePolicy(
                options, suffixes, secureOrigins, null!))!,
            Assert.Throws<CookiePolicyException>(() => new DefaultCookiePolicy(
                options, null, secureOrigins, clock))!
        ];

        Assert.That(failures.Select(failure => failure.Message),
            Is.All.EqualTo(
                "Cookie service configuration failed with error code 43."));
    }

    [Test]
    public void JarRejectsEveryMissingRequiredServiceAtConstruction()
    {
        CookieJarOptions options = ValidOptions();
        MatcherClock clock = new MatcherClock(EvaluationTime);
        MatcherPublicSuffixList suffixes =
            new MatcherPublicSuffixList("example.test", []);
        DefaultSecureOriginPolicy secureOrigins =
            new DefaultSecureOriginPolicy(false);
        ICookieParser parser = new CookieParser();
        ICookiePolicy policy = new DefaultCookiePolicy(
            options,
            suffixes,
            secureOrigins,
            clock);
        ICookieStore store = new MemoryCookieStore();
        ICookieSerializer serializer = new CookieSerializer();

        CookiePolicyException[] failures =
        [
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                null!, parser, policy, store, serializer, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                options, null!, policy, store, serializer, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                options, parser, null!, store, serializer, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                options, parser, policy, null!, serializer, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                options, parser, policy, store, null!, clock))!,
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                options, parser, policy, store, serializer, null!))!
        ];

        Assert.That(failures.Select(failure => failure.Message),
            Is.All.EqualTo(
                "Cookie service configuration failed with error code 43."));
    }

    [Test]
    public void ConstructorsRejectIncompleteNegativeAndContradictoryOptionGraphs()
    {
        CookieJarOptions[] invalid = InvalidOptions();
        MatcherClock clock = new MatcherClock(EvaluationTime);
        MatcherPublicSuffixList suffixes =
            new MatcherPublicSuffixList("example.test", []);
        DefaultSecureOriginPolicy secureOrigins =
            new DefaultSecureOriginPolicy(false);

        int index = 0;
        while (index < invalid.Length)
        {
            CookiePolicyException failure = Assert.Throws<CookiePolicyException>(() =>
                new CookieMatcher(
                    invalid[index],
                    suffixes,
                    secureOrigins,
                    clock));
            Assert.That(failure.Message,
                Is.EqualTo(
                    "Cookie service configuration failed with error code 43."));
            index = index + 1;
        }
    }

    [Test]
    public void PolicyAndJarApplyTheSameOptionGraphValidation()
    {
        CookieJarOptions valid = ValidOptions();
        CookieJarOptions invalidPolicy = ValidOptions();
        invalidPolicy.Limits.MaximumCookiesPerDomain =
            invalidPolicy.Limits.MaximumCookies + 1;
        CookieJarOptions invalidJar = ValidOptions();
        invalidJar.ParseOptions.ComplianceMode = CookieComplianceMode.Compatible;
        MatcherClock clock = new MatcherClock(EvaluationTime);
        MatcherPublicSuffixList suffixes =
            new MatcherPublicSuffixList("example.test", []);
        DefaultSecureOriginPolicy secureOrigins =
            new DefaultSecureOriginPolicy(false);
        DefaultCookiePolicy validPolicy = new DefaultCookiePolicy(
            valid,
            suffixes,
            secureOrigins,
            clock);

        CookiePolicyException policyFailure =
            Assert.Throws<CookiePolicyException>(() => new DefaultCookiePolicy(
                invalidPolicy,
                suffixes,
                secureOrigins,
                clock));
        CookiePolicyException jarFailure =
            Assert.Throws<CookiePolicyException>(() => new CookieJar(
                invalidJar,
                new CookieParser(),
                validPolicy,
                new MemoryCookieStore(),
                new CookieSerializer(),
                clock));

        Assert.That(policyFailure.Message,
            Is.EqualTo("Cookie service configuration failed with error code 43."));
        Assert.That(jarFailure.Message,
            Is.EqualTo("Cookie service configuration failed with error code 43."));
    }

    [Test]
    public void NetscapeCodecRejectsItsMissingClockImmediately()
    {
        CookieSerializationException failure =
            Assert.Throws<CookieSerializationException>(() =>
                new NetscapeCookieFileCodec(null!));

        Assert.That(failure.Message,
            Is.EqualTo("Cookie service configuration failed with error code 43."));
    }

    static CookieJarOptions ValidOptions()
    {
        return new CookieJarOptions();
    }

    static CookieJarOptions[] InvalidOptions()
    {
        CookieJarOptions nullLimits = ValidOptions();
        nullLimits.Limits = null!;
        CookieJarOptions nullParse = ValidOptions();
        nullParse.ParseOptions = null!;
        CookieJarOptions nullParseLimits = ValidOptions();
        nullParseLimits.ParseOptions.Limits = null!;
        CookieJarOptions nullSerialization = ValidOptions();
        nullSerialization.SerializationOptions = null!;
        CookieJarOptions nullSerializationLimits = ValidOptions();
        nullSerializationLimits.SerializationOptions.Limits = null!;
        CookieJarOptions nullNetscape = ValidOptions();
        nullNetscape.NetscapeFileOptions = null!;
        CookieJarOptions nullNetscapeLimits = ValidOptions();
        nullNetscapeLimits.NetscapeFileOptions.Limits = null!;
        CookieJarOptions negativeOuter = ValidOptions();
        negativeOuter.Limits.MaximumCookies = -1;
        CookieJarOptions negativeParse = ValidOptions();
        negativeParse.ParseOptions.Limits.MaximumInputLineOctets = -1;
        CookieJarOptions negativeSerialization = ValidOptions();
        negativeSerialization.SerializationOptions.Limits.MaximumLifetime =
            TimeSpan.FromTicks(-1);
        CookieJarOptions negativeNetscape = ValidOptions();
        negativeNetscape.NetscapeFileOptions.Limits.MaximumNameValueOctets = -1;
        CookieJarOptions impossibleOuterQuota = ValidOptions();
        impossibleOuterQuota.Limits.MaximumCookiesPerDomain =
            impossibleOuterQuota.Limits.MaximumCookies + 1;
        CookieJarOptions impossibleNestedQuota = ValidOptions();
        impossibleNestedQuota.ParseOptions.Limits.MaximumCookiesPerDomain =
            impossibleNestedQuota.ParseOptions.Limits.MaximumCookies + 1;
        CookieJarOptions mismatchedParseProfile = ValidOptions();
        mismatchedParseProfile.ParseOptions.ComplianceMode =
            CookieComplianceMode.Compatible;
        CookieJarOptions mismatchedSerializationProfile = ValidOptions();
        mismatchedSerializationProfile.SerializationOptions.ComplianceMode =
            CookieComplianceMode.Rfc6265Bis;
        CookieJarOptions weakerParseLimit = ValidOptions();
        weakerParseLimit.Limits.MaximumInputLineOctets = 1;
        CookieJarOptions weakerSerializationLimit = ValidOptions();
        weakerSerializationLimit.Limits.MaximumRequestHeaderOctets = 1;
        CookieJarOptions weakerNetscapeLimit = ValidOptions();
        weakerNetscapeLimit.Limits.MaximumAttributeValueOctets = 1;

        return
        [
            nullLimits,
            nullParse,
            nullParseLimits,
            nullSerialization,
            nullSerializationLimits,
            nullNetscape,
            nullNetscapeLimits,
            negativeOuter,
            negativeParse,
            negativeSerialization,
            negativeNetscape,
            impossibleOuterQuota,
            impossibleNestedQuota,
            mismatchedParseProfile,
            mismatchedSerializationProfile,
            weakerParseLimit,
            weakerSerializationLimit,
            weakerNetscapeLimit
        ];
    }
}
