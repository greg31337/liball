namespace cookies.Tests;

public class CookieDateAndLifetimeConformanceTests
{
    static readonly DateTimeOffset ReceiptTime = new DateTimeOffset(
        2025, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [TestCase("Jan", 1)]
    [TestCase("Feb", 2)]
    [TestCase("Mar", 3)]
    [TestCase("Apr", 4)]
    [TestCase("May", 5)]
    [TestCase("Jun", 6)]
    [TestCase("Jul", 7)]
    [TestCase("Aug", 8)]
    [TestCase("Sep", 9)]
    [TestCase("Oct", 10)]
    [TestCase("Nov", 11)]
    [TestCase("Dec", 12)]
    public void CookieDateRecognizesEveryMonth(string month, int expectedMonth)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(
            ("01-" + month + "-2024 02:03:04").AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(new DateTimeOffset(
            2024, expectedMonth, 1, 2, 3, 4, TimeSpan.Zero)));
    }

    [Test]
    public void EveryRfcDateDelimiterOctetSeparatesDateTokens()
    {
        for (int value = 0; value <= 0xff; value++)
        {
            char delimiter = (char)value;
            if (!CookieCharacterRules.IsDateDelimiter(delimiter))
            {
                continue;
            }

            string text = "01" + delimiter + "Jan" + delimiter +
                "2024" + delimiter + "02:03:04";
            CookieDateParseResult result = CookieDateParser.ParseCookieDate(
                text.AsSpan());

            Assert.That(result.Success, Is.True, $"Delimiter 0x{value:X2}");
            Assert.That(result.Value, Is.EqualTo(new DateTimeOffset(
                2024, 1, 1, 2, 3, 4, TimeSpan.Zero)),
                $"Delimiter 0x{value:X2}");
        }
    }

    [TestCase("01 Jan 69 00:00:00", 2069)]
    [TestCase("01 Jan 70 00:00:00", 1970)]
    public void TwoDigitYearMappingChangesAtTheSixtyNineSeventyBoundary(
        string text,
        int expectedYear)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Year, Is.EqualTo(expectedYear));
    }

    [TestCase("31 Apr 2024 00:00:00")]
    [TestCase("01 Jan 2024 24:00:00")]
    [TestCase("01 Jan 2024 00:60:00")]
    [TestCase("01 Jan 2024 00:00:60")]
    public void CookieDateRejectsInvalidCalendarAndTimeValues(string text)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.False);
    }

    [TestCase("29 Feb 2000 23:59:59", true)]
    [TestCase("29 Feb 1900 23:59:59", false)]
    [TestCase("29 Feb 2024 23:59:59", true)]
    [TestCase("29 Feb 2023 23:59:59", false)]
    public void CookieDateUsesGregorianLeapYearRules(string text, bool expectedSuccess)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [Test]
    public void CookieDateAcceptsThe1601LowerBoundAndRejectsThePriorInstant()
    {
        CookieDateParseResult lowerBound = CookieDateParser.ParseCookieDate(
            "01 Jan 1601 00:00:00".AsSpan());
        CookieDateParseResult priorDate = CookieDateParser.ParseCookieDate(
            "31 Dec 1600 23:59:59".AsSpan());

        Assert.That(lowerBound.Success, Is.True);
        Assert.That(lowerBound.Value, Is.EqualTo(new DateTimeOffset(
            1601, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(priorDate.Success, Is.False);
    }

    [Test]
    public void HugeMaxAgeParsesAndSaturatesLifetimeArithmeticSafely()
    {
        CookieParseOptions options = new CookieParseOptions();
        SetCookieParseResult positive = SetCookieParserCore.Parse(
            "id=1; Max-Age=999999999999999999999999999999".AsSpan(),
            options);
        SetCookieParseResult negative = SetCookieParserCore.Parse(
            "id=1; Max-Age=-999999999999999999999999999999".AsSpan(),
            options);

        CookieExpiryResult positiveExpiry = Normalize(
            positive.Cookie!, CookieComplianceMode.Rfc6265);
        CookieExpiryResult negativeExpiry = Normalize(
            negative.Cookie!, CookieComplianceMode.Rfc6265);

        Assert.That(positive.Success, Is.True);
        Assert.That(positive.Cookie!.MaxAgeSeconds, Is.EqualTo(long.MaxValue));
        Assert.That(positiveExpiry.Expires, Is.EqualTo(DateTimeOffset.MaxValue));
        Assert.That(positiveExpiry.ArithmeticSaturated, Is.True);
        Assert.That(negative.Success, Is.True);
        Assert.That(negative.Cookie!.MaxAgeSeconds, Is.EqualTo(long.MinValue));
        Assert.That(negativeExpiry.Expires, Is.EqualTo(DateTimeOffset.MinValue));
        Assert.That(negativeExpiry.IsImmediateDeletion, Is.True);
    }

    [Test]
    public void MaxAgeTakesPrecedenceOverExpiresIncludingDeletion()
    {
        SetCookie positive = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 60,
            HasExpires = true,
            Expires = ReceiptTime.AddDays(-1)
        };
        SetCookie deletion = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 0,
            HasExpires = true,
            Expires = ReceiptTime.AddYears(1)
        };

        CookieExpiryResult positiveResult = Normalize(
            positive, CookieComplianceMode.Rfc6265Bis);
        CookieExpiryResult deletionResult = Normalize(
            deletion, CookieComplianceMode.Rfc6265Bis);

        Assert.That(positiveResult.Expires, Is.EqualTo(ReceiptTime.AddSeconds(60)));
        Assert.That(positiveResult.IsImmediateDeletion, Is.False);
        Assert.That(positiveResult.UsedMaxAge, Is.True);
        Assert.That(deletionResult.Expires, Is.EqualTo(DateTimeOffset.MinValue));
        Assert.That(deletionResult.IsImmediateDeletion, Is.True);
        Assert.That(deletionResult.UsedMaxAge, Is.True);
    }

    [Test]
    public void ModernLifetimeCapChangesOnlyValuesBeyondFourHundredDays()
    {
        SetCookie exact = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 34560000
        };
        SetCookie excessiveMaxAge = new SetCookie
        {
            HasMaxAge = true,
            MaxAgeSeconds = 34560001
        };
        SetCookie excessiveExpires = new SetCookie
        {
            HasExpires = true,
            Expires = ReceiptTime.AddDays(401)
        };

        CookieExpiryResult exactResult = Normalize(
            exact, CookieComplianceMode.Rfc6265Bis);
        CookieExpiryResult maxAgeResult = Normalize(
            excessiveMaxAge, CookieComplianceMode.Rfc6265Bis);
        CookieExpiryResult expiresResult = Normalize(
            excessiveExpires, CookieComplianceMode.Rfc6265Bis);

        DateTimeOffset cap = ReceiptTime.AddDays(400);
        Assert.That(exactResult.Expires, Is.EqualTo(cap));
        Assert.That(exactResult.WasCapped, Is.False);
        Assert.That(maxAgeResult.Expires, Is.EqualTo(cap));
        Assert.That(maxAgeResult.WasCapped, Is.True);
        Assert.That(expiresResult.Expires, Is.EqualTo(cap));
        Assert.That(expiresResult.WasCapped, Is.True);
    }

    [Test]
    public void ExpiresFormattingAlwaysUsesCanonicalUtcForm()
    {
        DateTimeOffset supplied = new DateTimeOffset(
            2024, 6, 9, 12, 18, 14, TimeSpan.FromHours(2));

        string value = CookieDateFormatter.FormatExpires(supplied);

        Assert.That(value, Is.EqualTo("Sun, 09 Jun 2024 10:18:14 GMT"));
    }

    [Test]
    public void ExpirationUsesAnInclusiveUtcClockBoundary()
    {
        StoredCookie cookie = new StoredCookie
        {
            HasExpires = true,
            Persistent = true,
            Expires = new DateTimeOffset(
                2025, 1, 2, 5, 4, 5, TimeSpan.FromHours(2))
        };

        Assert.That(CookieExpiryEvaluator.IsExpired(
            cookie, ReceiptTime.AddTicks(-1)), Is.False);
        Assert.That(CookieExpiryEvaluator.IsExpired(
            cookie, ReceiptTime), Is.True);
        Assert.That(CookieExpiryEvaluator.IsExpired(
            cookie, ReceiptTime.AddTicks(1)), Is.True);
    }

    static CookieExpiryResult Normalize(
        SetCookie cookie,
        CookieComplianceMode complianceMode)
    {
        return CookieExpiryEvaluator.Normalize(
            cookie,
            ReceiptTime,
            complianceMode,
            CookieDefaults.MaximumLifetime);
    }
}
