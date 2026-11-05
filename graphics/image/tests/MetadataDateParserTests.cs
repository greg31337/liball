using System.Text.Json;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class MetadataDateParserTests
{
    [Test]
    public void ConstructorAndParseNull_ReturnInitializedInvalidResults()
    {
        MetadataDate empty = new MetadataDate();
        IMetadataDateParser parser = new MetadataDateParser();
        MetadataDate parsed = parser.Parse(null);

        Assert.That(empty.IsValid, Is.False);
        Assert.That(empty.OriginalText, Is.Empty);
        Assert.That(empty.Precision, Is.EqualTo(MetadataDatePrecision.Unknown));
        Assert.That(empty.FractionalSecondText, Is.Empty);
        Assert.That(empty.HasTimeZone, Is.False);
        Assert.That(parsed.IsValid, Is.False);
        Assert.That(parsed.OriginalText, Is.Empty);
    }

    [TestCase("2026", MetadataDatePrecision.Year, 0, 0)]
    [TestCase("2026-08", MetadataDatePrecision.Month, 8, 0)]
    [TestCase("2026-08-12", MetadataDatePrecision.Day, 8, 12)]
    public void Parse_XmpReducedPrecisionPreservesAvailableComponents(string text, MetadataDatePrecision precision, int month, int day)
    {
        MetadataDate result = new MetadataDateParser().Parse(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.OriginalText, Is.EqualTo(text));
        Assert.That(result.Precision, Is.EqualTo(precision));
        Assert.That(result.Year, Is.EqualTo(2026));
        Assert.That(result.Month, Is.EqualTo(month));
        Assert.That(result.Day, Is.EqualTo(day));
        Assert.That(result.HasTimeZone, Is.False);
    }

    [Test]
    public void Parse_XmpFractionalTimePreservesDigitsAndExplicitOffset()
    {
        MetadataDate result = new MetadataDateParser().Parse("2026-08-12T10:11:12.123400-07:30");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Precision, Is.EqualTo(MetadataDatePrecision.FractionalSecond));
        Assert.That(result.Year, Is.EqualTo(2026));
        Assert.That(result.Month, Is.EqualTo(8));
        Assert.That(result.Day, Is.EqualTo(12));
        Assert.That(result.Hour, Is.EqualTo(10));
        Assert.That(result.Minute, Is.EqualTo(11));
        Assert.That(result.Second, Is.EqualTo(12));
        Assert.That(result.FractionalSecondText, Is.EqualTo("123400"));
        Assert.That(result.HasTimeZone, Is.True);
        Assert.That(result.TimeZoneOffsetMinutes, Is.EqualTo(-450));
    }

    [Test]
    public void Parse_ExifAndTgaFormDoesNotInventTimeZone()
    {
        string text = " 2026:08:12 14:35:09 ";
        MetadataDate result = new MetadataDateParser().Parse(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.OriginalText, Is.EqualTo(text));
        Assert.That(result.Precision, Is.EqualTo(MetadataDatePrecision.Second));
        Assert.That(result.Hour, Is.EqualTo(14));
        Assert.That(result.HasTimeZone, Is.False);
    }

    [Test]
    public void Parse_IptcCombinedDateAndTimeSupportsCompactOffset()
    {
        MetadataDate result = new MetadataDateParser().Parse("20260812 141530-0700");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Precision, Is.EqualTo(MetadataDatePrecision.Second));
        Assert.That(result.Year, Is.EqualTo(2026));
        Assert.That(result.Month, Is.EqualTo(8));
        Assert.That(result.Day, Is.EqualTo(12));
        Assert.That(result.Hour, Is.EqualTo(14));
        Assert.That(result.Minute, Is.EqualTo(15));
        Assert.That(result.Second, Is.EqualTo(30));
        Assert.That(result.HasTimeZone, Is.True);
        Assert.That(result.TimeZoneOffsetMinutes, Is.EqualTo(-420));
    }

    [Test]
    public void Parse_PngTimeAcceptsUtcLeapSecond()
    {
        MetadataDate result = new MetadataDateParser().Parse("2024-06-30T23:59:60Z");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Second, Is.EqualTo(60));
        Assert.That(result.HasTimeZone, Is.True);
        Assert.That(result.TimeZoneOffsetMinutes, Is.Zero);
    }

    [Test]
    public void Parse_PngRfcStyleCreationTimeUsesEnglishMonthAndUtc()
    {
        MetadataDate result = new MetadataDateParser().Parse("Wed, 12 Aug 2026 10:11:12 GMT");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Year, Is.EqualTo(2026));
        Assert.That(result.Month, Is.EqualTo(8));
        Assert.That(result.Day, Is.EqualTo(12));
        Assert.That(result.Hour, Is.EqualTo(10));
        Assert.That(result.HasTimeZone, Is.True);
        Assert.That(result.TimeZoneOffsetMinutes, Is.Zero);
    }

    [TestCase("Wed Aug 12 10:11:12 2026", 10, 11)]
    [TestCase("(8/12/2026 2:35:09 PM)", 14, 35)]
    [TestCase("D:20260812143509-07'00'", 14, 35)]
    public void Parse_CommonEpsCreationDates(string text, int hour, int minute)
    {
        MetadataDate result = new MetadataDateParser().Parse(text);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Year, Is.EqualTo(2026));
        Assert.That(result.Month, Is.EqualTo(8));
        Assert.That(result.Day, Is.EqualTo(12));
        Assert.That(result.Hour, Is.EqualTo(hour));
        Assert.That(result.Minute, Is.EqualTo(minute));
    }

    [TestCase("")]
    [TestCase("not a date")]
    [TestCase("2025-02-29T10:00:00Z")]
    [TestCase("2026-13-01")]
    [TestCase("2026-08-12T24:00:00Z")]
    [TestCase("2026-08-12T10:00:00+14:30")]
    public void Parse_InvalidOrUnsupportedTextReturnsInvalidResult(string text)
    {
        MetadataDate result = new MetadataDateParser().Parse(text);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.OriginalText, Is.EqualTo(text));
        Assert.That(result.Precision, Is.EqualTo(MetadataDatePrecision.Unknown));
        Assert.That(result.Year, Is.Zero);
    }

    [Test]
    public void BuildSummary_ExposesParsedCaptureTimeWithoutChangingSourceText()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Exif, "ExifIFD", "EXIF");
        directory.AddTagWithValue("Exif.Photo.DateTimeOriginal", "Date Time Original", MetadataValue.FromDateText("2026:08:12 10:11:12"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.CaptureTimeText, Is.EqualTo("2026:08:12 10:11:12"));
        Assert.That(summary.CaptureTime.IsValid, Is.True);
        Assert.That(summary.CaptureTime.OriginalText, Is.EqualTo(summary.CaptureTimeText));
        Assert.That(summary.CaptureTime.Precision, Is.EqualTo(MetadataDatePrecision.Second));
        Assert.That(summary.CaptureTime.HasTimeZone, Is.False);
    }

    [Test]
    public void BuildSummary_PreservesUnsupportedCaptureTextWithInvalidParsedResult()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "DSC", "EPS DSC");
        directory.AddTagWithValue("Eps.DSC.CreationDate", "Creation Date", MetadataValue.FromDateText("sometime yesterday"));

        ImageMetadataSummary summary = new ImageMetadataNormalizer().BuildSummary(document);

        Assert.That(summary.CaptureTimeText, Is.EqualTo("sometime yesterday"));
        Assert.That(summary.CaptureTime.IsValid, Is.False);
        Assert.That(summary.CaptureTime.OriginalText, Is.EqualTo("sometime yesterday"));
    }

    [Test]
    public void ToJson_IncludesParsedCaptureTimeComponents()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Xmp, "xmp", "XMP");
        directory.AddTagWithValue("Xmp.xmp.CreateDate", "Create Date", MetadataValue.FromDateText("2026-08-12T10:11:12.50Z"));
        new ImageMetadataNormalizer().BuildSummary(document);

        using (JsonDocument json = JsonDocument.Parse(new ImageMetadataFormatter().ToJson(document)))
        {
            JsonElement captureTime = json.RootElement.GetProperty("summary").GetProperty("captureTime");
            Assert.That(captureTime.GetProperty("isValid").GetBoolean(), Is.True);
            Assert.That(captureTime.GetProperty("precision").GetString(), Is.EqualTo("FractionalSecond"));
            Assert.That(captureTime.GetProperty("fractionalSecondText").GetString(), Is.EqualTo("50"));
            Assert.That(captureTime.GetProperty("hasTimeZone").GetBoolean(), Is.True);
            Assert.That(captureTime.GetProperty("timeZoneOffsetMinutes").GetInt32(), Is.Zero);
        }
    }
}
