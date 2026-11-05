namespace cookies.Tests;

public class CookieDateParserTests
{
    [Test]
    public void DateDelimiterClassificationMatchesRfc6265OctetRanges()
    {
        for (int value = 0; value <= 255; value++)
        {
            bool expected = value == 0x09 ||
                value >= 0x20 && value <= 0x2f ||
                value >= 0x3b && value <= 0x40 ||
                value >= 0x5b && value <= 0x60 ||
                value >= 0x7b && value <= 0x7e;

            Assert.That(CookieCharacterRules.IsDateDelimiter((char)value), Is.EqualTo(expected), $"Octet {value}");
        }
    }

    [Test]
    public void TokenizerReturnsOriginalOffsetsAcrossDelimiterClasses()
    {
        string input = "\t/Wed, 09;Jun[2021{10:18:14~";
        CookieDateTokenResult first = CookieDateTokenizer.ReadNextDateToken(input.AsSpan(), 0);
        CookieDateTokenResult second = CookieDateTokenizer.ReadNextDateToken(input.AsSpan(), first.NextOffset);
        CookieDateTokenResult third = CookieDateTokenizer.ReadNextDateToken(input.AsSpan(), second.NextOffset);
        CookieDateTokenResult fourth = CookieDateTokenizer.ReadNextDateToken(input.AsSpan(), third.NextOffset);
        CookieDateTokenResult fifth = CookieDateTokenizer.ReadNextDateToken(input.AsSpan(), fourth.NextOffset);
        CookieDateTokenResult end = CookieDateTokenizer.ReadNextDateToken(input.AsSpan(), fifth.NextOffset);

        Assert.That(input.AsSpan(first.Start, first.Length).ToString(), Is.EqualTo("Wed"));
        Assert.That(input.AsSpan(second.Start, second.Length).ToString(), Is.EqualTo("09"));
        Assert.That(input.AsSpan(third.Start, third.Length).ToString(), Is.EqualTo("Jun"));
        Assert.That(input.AsSpan(fourth.Start, fourth.Length).ToString(), Is.EqualTo("2021"));
        Assert.That(input.AsSpan(fifth.Start, fifth.Length).ToString(), Is.EqualTo("10:18:14"));
        Assert.That(end.HasToken, Is.False);
        Assert.That(end.NextOffset, Is.EqualTo(input.Length));
    }

    [TestCase(-1)]
    [TestCase(4)]
    public void TokenizerReportsInvalidStartingOffsets(int start)
    {
        CookieDateTokenResult result = CookieDateTokenizer.ReadNextDateToken("abc".AsSpan(), start);

        Assert.That(result.HasToken, Is.False);
        Assert.That(result.InvalidBounds, Is.True);
    }

    [Test]
    public void ParserAcceptsCanonicalImfFixdateInput()
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate("Wed, 09 Jun 2021 10:18:14 GMT".AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero)));
        Assert.That(result.Value.Offset, Is.EqualTo(TimeSpan.Zero));
    }

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
    public void ParserRecognizesEveryAsciiMonthCaseInsensitively(string month, int expectedMonth)
    {
        string text = "01 " + month.ToUpperInvariant() + " 2021 1:2:3";
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Month, Is.EqualTo(expectedMonth));
        Assert.That(result.Value.Hour, Is.EqualTo(1));
        Assert.That(result.Value.Minute, Is.EqualTo(2));
        Assert.That(result.Value.Second, Is.EqualTo(3));
    }

    [TestCase("09 Jun 69 10:18:14", 2069)]
    [TestCase("09 Jun 70 10:18:14", 1970)]
    [TestCase("09 Jun 00 10:18:14", 2000)]
    [TestCase("09 Jun 99 10:18:14", 1999)]
    public void ParserNormalizesTwoDigitYears(string text, int expectedYear)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value.Year, Is.EqualTo(expectedYear));
    }

    [Test]
    public void ParserAcceptsProductionSuffixesAndIgnoresUnknownTokens()
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(
            "unknown 09th June 2021year 10:18:14GMT ignored".AsSpan());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero)));
    }

    [TestCase("29 Feb 2020 23:59:59", true)]
    [TestCase("29 Feb 2019 23:59:59", false)]
    [TestCase("31 Apr 2021 10:18:14", false)]
    [TestCase("31 Dec 1601 10:18:14", true)]
    [TestCase("31 Dec 1600 10:18:14", false)]
    public void ParserValidatesRealCalendarDatesAndThe1601Floor(string text, bool expectedSuccess)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase("00 Jun 2021 10:18:14")]
    [TestCase("32 Jun 2021 10:18:14")]
    [TestCase("09 Jun 2021 24:18:14")]
    [TestCase("09 Jun 2021 10:60:14")]
    [TestCase("09 Jun 2021 10:18:60")]
    [TestCase("09 Jun 20210 10:18:14")]
    [TestCase("009 Jun 2021 10:18:14")]
    [TestCase("09 Jun 2021 010:18:14")]
    public void ParserRejectsOutOfRangeOrOverlongComponents(string text)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.False);
    }

    [TestCase("")]
    [TestCase("09 Jun 2021")]
    [TestCase("Jun 2021 10:18:14")]
    [TestCase("09 2021 10:18:14")]
    [TestCase("09 Jun 10:18:14")]
    public void ParserReturnsFailureWhenARequiredComponentIsMissing(string text)
    {
        CookieDateParseResult result = CookieDateParser.ParseCookieDate(text.AsSpan());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.EqualTo(default(DateTimeOffset)));
    }
}
