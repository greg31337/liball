using System.Globalization;

namespace cookies.Tests;

public class CookieDateFormatterTests
{
    [TestCase(2024, 1, 7, "Sun")]
    [TestCase(2024, 1, 8, "Mon")]
    [TestCase(2024, 1, 9, "Tue")]
    [TestCase(2024, 1, 10, "Wed")]
    [TestCase(2024, 1, 11, "Thu")]
    [TestCase(2024, 1, 12, "Fri")]
    [TestCase(2024, 1, 13, "Sat")]
    public void FormatterUsesCanonicalEnglishWeekdayNames(int year, int month, int day, string weekday)
    {
        DateTimeOffset value = new DateTimeOffset(year, month, day, 10, 18, 14, TimeSpan.Zero);

        string formatted = CookieDateFormatter.FormatExpires(value);

        Assert.That(formatted, Does.StartWith(weekday + ", "));
    }

    [TestCase(1, "Jan")]
    [TestCase(2, "Feb")]
    [TestCase(3, "Mar")]
    [TestCase(4, "Apr")]
    [TestCase(5, "May")]
    [TestCase(6, "Jun")]
    [TestCase(7, "Jul")]
    [TestCase(8, "Aug")]
    [TestCase(9, "Sep")]
    [TestCase(10, "Oct")]
    [TestCase(11, "Nov")]
    [TestCase(12, "Dec")]
    public void FormatterUsesCanonicalEnglishMonthNames(int month, string expected)
    {
        DateTimeOffset value = new DateTimeOffset(2024, month, 15, 10, 18, 14, TimeSpan.Zero);

        string formatted = CookieDateFormatter.FormatExpires(value);

        Assert.That(formatted.Substring(8, 3), Is.EqualTo(expected));
    }

    [Test]
    public void FormatterPadsEveryNumericFieldAndUsesGmt()
    {
        DateTimeOffset value = new DateTimeOffset(1601, 1, 1, 1, 2, 3, TimeSpan.Zero);

        string formatted = CookieDateFormatter.FormatExpires(value);

        Assert.That(formatted, Is.EqualTo("Mon, 01 Jan 1601 01:02:03 GMT"));
        Assert.That(formatted.Length, Is.EqualTo(29));
    }

    [Test]
    public void FormatterConvertsOffsetValuesToUtc()
    {
        DateTimeOffset value = new DateTimeOffset(2024, 6, 9, 12, 18, 14, TimeSpan.FromHours(2));

        string formatted = CookieDateFormatter.FormatExpires(value);

        Assert.That(formatted, Is.EqualTo("Sun, 09 Jun 2024 10:18:14 GMT"));
    }

    [Test]
    public void FormatterEmitsWholeSecondPrecision()
    {
        DateTimeOffset value = new DateTimeOffset(2024, 6, 9, 10, 18, 14, 987, TimeSpan.Zero);

        string formatted = CookieDateFormatter.FormatExpires(value);

        Assert.That(formatted, Is.EqualTo("Sun, 09 Jun 2024 10:18:14 GMT"));
    }

    [Test]
    [NonParallelizable]
    public void FormatterOutputDoesNotDependOnCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            DateTimeOffset value = new DateTimeOffset(2024, 6, 9, 10, 18, 14, TimeSpan.Zero);

            string formatted = CookieDateFormatter.FormatExpires(value);

            Assert.That(formatted, Is.EqualTo("Sun, 09 Jun 2024 10:18:14 GMT"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
