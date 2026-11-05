namespace cookies.Tests;

public class CookieSerializerTests
{
    [Test]
    public void SerializerImplementsTheContractAndRoutesSuccessfulOperations()
    {
        ICookieSerializer serializer = new CookieSerializer();
        CookieSerializationOptions options = new CookieSerializationOptions();
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "b", Value = "2" }
        ];

        string setCookie = serializer.SerializeSetCookie(
            new SetCookie { Name = "id", Value = "1", Secure = true },
            options);
        string cookieValue = serializer.SerializeCookiePairs(pairs, options);
        CookieSelectionResult fields = serializer.SerializeCookieFields(pairs, options);

        Assert.That(setCookie, Is.EqualTo("id=1; Secure"));
        Assert.That(cookieValue, Is.EqualTo("a=1; b=2"));
        Assert.That(fields.Success, Is.True);
        Assert.That(fields.FieldValues, Is.EqualTo(new string[] { "a=1; b=2" }));
    }

    [Test]
    public void SetCookieSerializationExposesAllCoreBehavior()
    {
        CookieSerializer serializer = new CookieSerializer();
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "quoted value",
            IsValueQuoted = true,
            HasExpires = true,
            Expires = new DateTimeOffset(2024, 6, 9, 10, 18, 14, TimeSpan.Zero),
            HasMaxAge = true,
            MaxAgeSeconds = 60,
            HasDomain = true,
            Domain = "example.test",
            HasPath = true,
            Path = "/",
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.None,
            Partitioned = true,
            Extensions = [new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" }]
        };
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = true
        };

        string value = serializer.SerializeSetCookie(cookie, options);

        Assert.That(
            value,
            Is.EqualTo(
                "id=\"quoted value\"; Expires=Sun, 09 Jun 2024 10:18:14 GMT; Max-Age=60; " +
                "Domain=example.test; Path=/; Secure; HttpOnly; SameSite=None; Partitioned; Priority=High"));
    }

    [Test]
    public void MultiFieldSerializationReturnsBoundedFieldsAndDefensivePairs()
    {
        CookieSerializer serializer = new CookieSerializer();
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "bb", Value = "22" },
            new CookiePair { Name = "c", Value = "3" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;

        CookieSelectionResult result = serializer.SerializeCookieFields(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FieldValues, Is.EqualTo(new string[] { "a=1; bb=22", "c=3" }));
        Assert.That(result.Pairs, Is.Not.SameAs(pairs));
        pairs[0].Name = "changed";
        Assert.That(result.Pairs[0].Name, Is.EqualTo("a"));
    }

    [Test]
    public void IndivisiblePairLimitIsReturnedWithoutThrowingOrTruncation()
    {
        CookieSerializer serializer = new CookieSerializer();
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 4;

        CookieSelectionResult result = serializer.SerializeCookieFields(
            [new CookiePair { Name = "large", Value = "value" }],
            options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderLimitExceeded));
        Assert.That(result.FieldValues, Is.Empty);
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.HeaderLimitExceeded));
    }

    [Test]
    public void SingleFieldLimitFailureThrowsTheDedicatedException()
    {
        CookieSerializer serializer = new CookieSerializer();
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 3;

        CookieSerializationException exception = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookiePairs(
                [
                    new CookiePair { Name = "a", Value = "1" },
                    new CookiePair { Name = "b", Value = "2" }
                ],
                options));

        Assert.That(exception.Message, Is.EqualTo("Cookie serialization failed with error code 32."));
    }

    [Test]
    public void InvalidSetCookieThrowsASafeCodeOnlyMessage()
    {
        CookieSerializer serializer = new CookieSerializer();
        string sensitiveValue = "private-value";

        CookieSerializationException exception = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeSetCookie(
                new SetCookie { Name = "bad name", Value = sensitiveValue },
                new CookieSerializationOptions()));

        Assert.That(exception.Message, Is.EqualTo("Cookie serialization failed with error code 3."));
        Assert.That(exception.Message, Does.Not.Contain(sensitiveValue));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void HeaderInjectionInPairsThrowsWithoutReflectingInput()
    {
        CookieSerializer serializer = new CookieSerializer();
        string injected = "secret\rInjected: value";

        CookieSerializationException exception = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookiePairs(
                [new CookiePair { Name = "id", Value = injected }],
                new CookieSerializationOptions()));

        Assert.That(exception.Message, Is.EqualTo("Cookie serialization failed with error code 2."));
        Assert.That(exception.Message, Does.Not.Contain("secret"));
        Assert.That(exception.Message, Does.Not.Contain("Injected"));
    }

    [Test]
    public void InvalidPairsInTheMultiFieldMethodThrowInsteadOfReturningPartialOutput()
    {
        CookieSerializer serializer = new CookieSerializer();

        CookieSerializationException exception = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookieFields(
                [
                    new CookiePair { Name = "ok", Value = "1" },
                    new CookiePair { Name = "bad name", Value = "2" }
                ],
                new CookieSerializationOptions()));

        Assert.That(exception.Message, Is.EqualTo("Cookie serialization failed with error code 3."));
    }

    [Test]
    public void InvalidConfigurationThrowsForEveryPublicOperation()
    {
        CookieSerializer serializer = new CookieSerializer();
        SetCookie cookie = new SetCookie { Name = "id", Value = "1" };
        CookiePair[] pairs = [new CookiePair { Name = "id", Value = "1" }];

        CookieSerializationException setCookieException = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeSetCookie(cookie, null!));
        CookieSerializationException pairException = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookiePairs(pairs, null!));
        CookieSerializationException fieldException = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookieFields(pairs, null!));

        Assert.That(setCookieException.Message, Is.EqualTo("Cookie serialization failed with error code 43."));
        Assert.That(pairException.Message, Is.EqualTo("Cookie serialization failed with error code 43."));
        Assert.That(fieldException.Message, Is.EqualTo("Cookie serialization failed with error code 43."));
    }

    [Test]
    public void NullModelsThrowSerializationFailure()
    {
        CookieSerializer serializer = new CookieSerializer();
        CookieSerializationOptions options = new CookieSerializationOptions();

        CookieSerializationException setCookieException = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeSetCookie(null!, options));
        CookieSerializationException pairException = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookiePairs(null!, options));
        CookieSerializationException fieldException = Assert.Throws<CookieSerializationException>(() =>
            serializer.SerializeCookieFields(null!, options));

        Assert.That(setCookieException.Message, Is.EqualTo("Cookie serialization failed with error code 36."));
        Assert.That(pairException.Message, Is.EqualTo("Cookie serialization failed with error code 36."));
        Assert.That(fieldException.Message, Is.EqualTo("Cookie serialization failed with error code 36."));
    }

    [Test]
    public void EmptyPairsProduceEmptyStringAndNoFields()
    {
        CookieSerializer serializer = new CookieSerializer();
        CookieSerializationOptions options = new CookieSerializationOptions();

        string value = serializer.SerializeCookiePairs([], options);
        CookieSelectionResult result = serializer.SerializeCookieFields([], options);

        Assert.That(value, Is.Empty);
        Assert.That(result.Success, Is.True);
        Assert.That(result.FieldValues, Is.Empty);
    }
}
