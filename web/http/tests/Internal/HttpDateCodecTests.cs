using System.Globalization;
using System.Text;

namespace http.Tests;

public class HttpDateCodecTests
{
    [Test]
    public void ParserAcceptsAllThreeHttpDateFormats()
    {
        DateTimeOffset expected = new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero);
        DateTimeOffset reference = new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero);

        HttpDateParseResult imf = HttpDateCodec.ParseHttpDateAt(
            "Sun, 06 Nov 1994 08:49:37 GMT"u8.ToArray(),
            64,
            reference);
        HttpDateParseResult rfc850 = HttpDateCodec.ParseHttpDateAt(
            "Sunday, 06-Nov-94 08:49:37 GMT"u8.ToArray(),
            64,
            reference);
        HttpDateParseResult asctime = HttpDateCodec.ParseHttpDateAt(
            "Sun Nov  6 08:49:37 1994"u8.ToArray(),
            64,
            reference);

        Assert.Multiple(() =>
        {
            Assert.That(imf.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(imf.Value, Is.EqualTo(expected));
            Assert.That(imf.Consumed, Is.EqualTo(29));
            Assert.That(rfc850.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(rfc850.Value, Is.EqualTo(expected));
            Assert.That(asctime.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(asctime.Value, Is.EqualTo(expected));
        });
    }

    [Test]
    public void AsctimeParserAcceptsOneAndTwoDigitDayForms()
    {
        DateTimeOffset reference = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        HttpDateParseResult padded = HttpDateCodec.ParseHttpDateAt(
            "Sun Nov  6 08:49:37 1994"u8.ToArray(),
            64,
            reference);
        HttpDateParseResult twoDigits = HttpDateCodec.ParseHttpDateAt(
            "Wed Nov 16 08:49:37 1994"u8.ToArray(),
            64,
            reference);

        Assert.Multiple(() =>
        {
            Assert.That(padded.Value.Day, Is.EqualTo(6));
            Assert.That(twoDigits.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(twoDigits.Value.Day, Is.EqualTo(16));
        });
    }

    [Test]
    public void Rfc850YearUsesTheFiftyYearFutureRule()
    {
        DateTimeOffset reference = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset withinWindow = new DateTimeOffset(2075, 5, 31, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset beyondWindowPast = new DateTimeOffset(1975, 6, 2, 12, 0, 0, TimeSpan.Zero);
        string withinInput = withinWindow.ToString("dddd, dd-MMM-yy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
        string beyondInput = beyondWindowPast.ToString("dddd, dd-MMM-yy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);

        HttpDateParseResult within = HttpDateCodec.ParseHttpDateAt(
            Encoding.ASCII.GetBytes(withinInput),
            64,
            reference);
        HttpDateParseResult beyond = HttpDateCodec.ParseHttpDateAt(
            Encoding.ASCII.GetBytes(beyondInput),
            64,
            reference);

        Assert.Multiple(() =>
        {
            Assert.That(within.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(within.Value, Is.EqualTo(withinWindow));
            Assert.That(beyond.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(beyond.Value, Is.EqualTo(beyondWindowPast));
        });
    }

    [Test]
    public void ParserRequiresExactCaseSpacingAndCompleteConsumption()
    {
        DateTimeOffset reference = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        string[] malformed =
        [
            "sun, 06 Nov 1994 08:49:37 GMT",
            "Sun, 06 nov 1994 08:49:37 GMT",
            "Sun, 06 Nov 1994 08:49:37 gmt",
            "Sun,  06 Nov 1994 08:49:37 GMT",
            "Sun, 06 Nov 1994 08:49:37 GMT ",
            "Sunday, 06-Nov-94 08:49:37 UTC",
            "Sun Nov 6 08:49:37 1994"
        ];

        foreach (string value in malformed)
        {
            HttpDateParseResult result = HttpDateCodec.ParseHttpDateAt(
                Encoding.ASCII.GetBytes(value),
                64,
                reference);
            Assert.That(result.IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void ParserRejectsInvalidCalendarAndClockValues()
    {
        DateTimeOffset reference = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        string[] malformed =
        [
            "Mon, 29 Feb 2021 08:49:37 GMT",
            "Fri, 31 Apr 2026 08:49:37 GMT",
            "Sun, 00 Nov 1994 08:49:37 GMT",
            "Sun, 06 Nov 1994 24:49:37 GMT",
            "Sun, 06 Nov 1994 08:60:37 GMT",
            "Sun, 06 Nov 1994 08:49:60 GMT",
            "Sun, 06 Nov 0000 08:49:37 GMT"
        ];

        foreach (string value in malformed)
        {
            HttpDateParseResult result = HttpDateCodec.ParseHttpDateAt(
                Encoding.ASCII.GetBytes(value),
                64,
                reference);
            Assert.That(result.IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void ParserRejectsAWeekdayThatDoesNotMatchTheCalendarDate()
    {
        HttpDateParseResult result = HttpDateCodec.ParseHttpDateAt(
            "Mon, 06 Nov 1994 08:49:37 GMT"u8.ToArray(),
            64,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.That(result.IsMalformed, Is.True);
    }

    [Test]
    public void ParserReportsSizeExhaustionSeparatelyFromMalformedInput()
    {
        byte[] input = "Sun, 06 Nov 1994 08:49:37 GMT"u8.ToArray();

        HttpDateParseResult result = HttpDateCodec.ParseHttpDateAt(
            input,
            input.Length - 1,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsLimitExceeded, Is.True);
            Assert.That(result.IsMalformed, Is.False);
        });
    }

    [Test]
    public void SerializerAlwaysProducesUtcImfFixdateWithoutFractions()
    {
        DateTimeOffset local = new DateTimeOffset(1994, 11, 6, 9, 49, 37, 456, TimeSpan.FromHours(1));

        FieldValueWriteResult result = HttpDateCodec.SerializeHttpDate(local, 29);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo("Sun, 06 Nov 1994 08:49:37 GMT"));
            Assert.That(result.Written, Is.EqualTo(29));
            Assert.That(result.RequiredSize, Is.EqualTo(29));
        });
    }

    [Test]
    public void SerializerSupportsAllRepresentableFourDigitYears()
    {
        DateTimeOffset early = new DateTimeOffset(42, 1, 2, 3, 4, 5, TimeSpan.Zero);

        FieldValueWriteResult result = HttpDateCodec.SerializeHttpDate(early, 29);
        HttpDateParseResult parsed = HttpDateCodec.ParseHttpDateAt(
            result.Bytes,
            29,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Does.Contain(" 0042 "));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Value, Is.EqualTo(early));
        });
    }

    [Test]
    public void BoundedDateWritingIsTransactionalAndReportsRequiredCapacity()
    {
        DateTimeOffset value = new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero);
        byte[] tooSmall = new byte[30];
        byte[] output = new byte[31];
        Array.Fill(tooSmall, (byte)0xcc);
        Array.Fill(output, (byte)0xcc);

        FieldValueWriteResult insufficient = HttpDateCodec.WriteHttpDate(value, tooSmall, 2, 29);
        FieldValueWriteResult written = HttpDateCodec.WriteHttpDate(value, output, 1, 29);
        FieldValueWriteResult limited = HttpDateCodec.SerializeHttpDate(value, 28);

        Assert.Multiple(() =>
        {
            Assert.That(insufficient.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(insufficient.RequiredSize, Is.EqualTo(31));
            Assert.That(tooSmall, Is.All.EqualTo(0xcc));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(29));
            Assert.That(output[0], Is.EqualTo(0xcc));
            Assert.That(Encoding.ASCII.GetString(output, 1, 29), Is.EqualTo("Sun, 06 Nov 1994 08:49:37 GMT"));
            Assert.That(output[30], Is.EqualTo(0xcc));
            Assert.That(limited.IsLimitExceeded, Is.True);
            Assert.That(limited.RequiredSize, Is.EqualTo(29));
        });
    }

    [Test]
    public void SecondPrecisionComparisonNormalizesOffsetsAndIgnoresFractions()
    {
        DateTimeOffset first = new DateTimeOffset(2026, 9, 10, 12, 0, 0, 100, TimeSpan.Zero);
        DateTimeOffset sameSecond = new DateTimeOffset(2026, 9, 10, 14, 0, 0, 900, TimeSpan.FromHours(2));
        DateTimeOffset nextSecond = new DateTimeOffset(2026, 9, 10, 12, 0, 1, TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(HttpDateCodec.CompareAtSecondPrecision(first, sameSecond), Is.Zero);
            Assert.That(HttpDateCodec.EqualsAtSecondPrecision(first, sameSecond), Is.True);
            Assert.That(HttpDateCodec.CompareAtSecondPrecision(first, nextSecond), Is.LessThan(0));
            Assert.That(HttpDateCodec.CompareAtSecondPrecision(nextSecond, first), Is.GreaterThan(0));
            Assert.That(HttpDateCodec.EqualsAtSecondPrecision(first, nextSecond), Is.False);
        });
    }
}
