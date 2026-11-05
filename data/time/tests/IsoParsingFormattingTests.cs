using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class IsoParsingFormattingTests
{
    [Test]
    public void ReadFixedDigits_DelegatesToCursorAndAdvances()
    {
        TextCursor cursor = TextCursor.Create("2024-");

        Assert.That(IsoParsing.ReadFixedDigits(cursor, 4), Is.EqualTo(2024));
        Assert.That(cursor.GetPosition(), Is.EqualTo(4));
    }

    [Test]
    public void ReadFixedDigits_RequiresCursor()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => IsoParsing.ReadFixedDigits(null!, 2))!;

        Assert.That(exception.ParamName, Is.EqualTo("cursor"));
    }

    [TestCase("1", 1000000L)]
    [TestCase("12", 1200000L)]
    [TestCase("1234567", 1234567L)]
    public void ReadFractionalSecondTicks_ReadsAndScalesDigits(string text, long expectedTicks)
    {
        TextCursor cursor = TextCursor.Create(text + "Z");

        Assert.That(IsoParsing.ReadFractionalSecondTicks(cursor, 7), Is.EqualTo(expectedTicks));
        Assert.That(cursor.Peek(), Is.EqualTo('Z'));
    }

    [Test]
    public void ReadFractionalSecondTicks_RejectsInvalidInput()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => IsoParsing.ReadFractionalSecondTicks(TextCursor.Create("1"), 0))!.ParamName, Is.EqualTo("maxDigits"));
        Assert.Throws<FormatException>(() => IsoParsing.ReadFractionalSecondTicks(TextCursor.Create(""), 7));
        Assert.Throws<FormatException>(() => IsoParsing.ReadFractionalSecondTicks(TextCursor.Create("x"), 7));
        Assert.Throws<FormatException>(() => IsoParsing.ReadFractionalSecondTicks(TextCursor.Create("12345678"), 7));
    }

    [Test]
    public void FindSingleSeparator_ReturnsOnlySeparatorIndex()
    {
        Assert.That(IsoParsing.FindSingleSeparator("left/right", '/'), Is.EqualTo(4));
    }

    [Test]
    public void FindSingleSeparator_RejectsMissingDuplicateOrEmptyComponents()
    {
        Assert.Throws<FormatException>(() => IsoParsing.FindSingleSeparator("leftright", '/'));
        Assert.Throws<FormatException>(() => IsoParsing.FindSingleSeparator("left/right/again", '/'));
        Assert.Throws<FormatException>(() => IsoParsing.FindSingleSeparator("/right", '/'));
        Assert.Throws<FormatException>(() => IsoParsing.FindSingleSeparator("left/", '/'));
    }

    [TestCase("2024-01-01T00:00:00Z", 19)]
    [TestCase("2024-01-01T00:00:00+05:30", 19)]
    [TestCase("2024-01-01T00:00:00-07:30:15", 19)]
    [TestCase("2024-01-01T00:00:00", -1)]
    public void FindOffsetStart_FindsSupportedOffsetForms(string text, int expected)
    {
        Assert.That(IsoParsing.FindOffsetStart(text), Is.EqualTo(expected));
    }

    [TestCase("PT1H", true)]
    [TestCase("+PT1H", true)]
    [TestCase("-PT1H", true)]
    [TestCase("2024-01-01T00:00:00Z", false)]
    [TestCase("", false)]
    public void StartsWithDurationDesignator_RecognizesDurationPrefixes(string text, bool expected)
    {
        Assert.That(IsoParsing.StartsWithDurationDesignator(text), Is.EqualTo(expected));
    }

    [Test]
    public void FormatFixedDigits_FormatsInvariantPaddedText()
    {
        Assert.That(IsoFormatting.FormatFixedDigits(7, 2), Is.EqualTo("07"));
        Assert.That(IsoFormatting.FormatFixedDigits(2024, 4), Is.EqualTo("2024"));
    }

    [Test]
    public void FormatFixedDigits_RejectsInvalidArguments()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => IsoFormatting.FormatFixedDigits(-1, 2))!.ParamName, Is.EqualTo("value"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => IsoFormatting.FormatFixedDigits(1, 0))!.ParamName, Is.EqualTo("digits"));
    }

    [Test]
    public void FormatDate_FormatsIsoDate()
    {
        Assert.That(IsoFormatting.FormatDate(2024, 2, 29), Is.EqualTo("2024-02-29"));
    }

    [Test]
    public void FormatTime_FormatsIsoTimeWithOptionalFraction()
    {
        Assert.That(IsoFormatting.FormatTime(12, 34, 56, 0L), Is.EqualTo("12:34:56"));
        Assert.That(IsoFormatting.FormatTime(12, 34, 56, 1234000L), Is.EqualTo("12:34:56.1234"));
    }

    [Test]
    public void FormatFractionalSecond_RejectsOutOfRangeTicks()
    {
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => IsoFormatting.FormatFractionalSecond(-1L))!.ParamName, Is.EqualTo("fractionalTicks"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => IsoFormatting.FormatFractionalSecond(TimeConstants.TicksPerSecond))!.ParamName, Is.EqualTo("fractionalTicks"));
    }

    [TestCase(0, "Z")]
    [TestCase(19800, "+05:30")]
    [TestCase(-27015, "-07:30:15")]
    public void FormatOffsetSeconds_FormatsIsoOffset(int totalSeconds, string expected)
    {
        Assert.That(IsoFormatting.FormatOffsetSeconds(totalSeconds), Is.EqualTo(expected));
    }
}
