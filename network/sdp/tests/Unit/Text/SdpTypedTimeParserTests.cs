using System.Text;

namespace sdp.Tests.Unit.Text;

public class SdpTypedTimeParserTests
{
    [TestCase("0", 0L, "")]
    [TestCase("7d", 7L, "d")]
    [TestCase("12h", 12L, "h")]
    [TestCase("30m", 30L, "m")]
    [TestCase("45s", 45L, "s")]
    public void CharacterParser_AcceptsUnitlessSecondsAndFixedUnits(string text, long expectedValue, string expectedUnit)
    {
        SdpTypedTimeParseResult result = SdpTypedTimeParser.ParseCharacterTypedTime(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value.Value, Is.EqualTo(expectedValue));
            Assert.That(result.Value.Unit, Is.EqualTo(expectedUnit));
            Assert.That(result.Value.RawValue, Is.EqualTo(text));
            Assert.That(result.DiagnosticCode, Is.Empty);
        });
    }

    [Test]
    public void Utf8Parser_AcceptsEveryFixedUnit()
    {
        string[] values = new string[] { "1", "2d", "3h", "4m", "5s" };
        string[] units = new string[] { "", "d", "h", "m", "s" };

        for (int index = 0; index < values.Length; index++)
        {
            byte[] input = Encoding.ASCII.GetBytes(values[index]);
            SdpTypedTimeParseResult result = SdpTypedTimeParser.ParseUtf8TypedTime(input.AsSpan());

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Value.Value, Is.EqualTo(index + 1));
                Assert.That(result.Value.Unit, Is.EqualTo(units[index]));
                Assert.That(result.Value.RawValue, Is.EqualTo(values[index]));
            });
        }
    }

    [Test]
    public void SignedParsers_AcceptZoneOffsetsAndRejectLeadingPlus()
    {
        SdpTypedTimeParseResult characterNegative = SdpTypedTimeParser.ParseCharacterSignedTypedTime("-25h".AsSpan());
        SdpTypedTimeParseResult utf8Negative = SdpTypedTimeParser.ParseUtf8SignedTypedTime(Encoding.ASCII.GetBytes("-90m"));
        SdpTypedTimeParseResult characterPositive = SdpTypedTimeParser.ParseCharacterSignedTypedTime("30s".AsSpan());
        SdpTypedTimeParseResult unsignedNegative = SdpTypedTimeParser.ParseCharacterTypedTime("-1h".AsSpan());
        SdpTypedTimeParseResult characterPlus = SdpTypedTimeParser.ParseCharacterSignedTypedTime("+1h".AsSpan());
        SdpTypedTimeParseResult utf8Plus = SdpTypedTimeParser.ParseUtf8SignedTypedTime(Encoding.ASCII.GetBytes("+1h"));

        Assert.Multiple(() =>
        {
            Assert.That(characterNegative.Success, Is.True);
            Assert.That(characterNegative.Value.Value, Is.EqualTo(-25));
            Assert.That(characterNegative.Value.Unit, Is.EqualTo("h"));
            Assert.That(characterNegative.Value.RawValue, Is.EqualTo("-25h"));

            Assert.That(utf8Negative.Success, Is.True);
            Assert.That(utf8Negative.Value.Value, Is.EqualTo(-90));
            Assert.That(utf8Negative.Value.Unit, Is.EqualTo("m"));
            Assert.That(characterPositive.Success, Is.True);
            Assert.That(characterPositive.Value.Value, Is.EqualTo(30));

            Assert.That(unsignedNegative.Success, Is.False);
            Assert.That(unsignedNegative.Invalid, Is.True);
            Assert.That(characterPlus.Success, Is.False);
            Assert.That(characterPlus.Invalid, Is.True);
            Assert.That(utf8Plus.Success, Is.False);
            Assert.That(utf8Plus.Invalid, Is.True);
        });
    }

    [Test]
    public void TypedTimeParsers_RejectMalformedTokensAndUnitCasing()
    {
        string[] invalidValues = new string[]
        {
            "",
            "h",
            "1H",
            "1w",
            "1.5h",
            "1ms",
            " 1h",
            "1h "
        };

        foreach (string input in invalidValues)
        {
            SdpTypedTimeParseResult characterResult = SdpTypedTimeParser.ParseCharacterTypedTime(input.AsSpan());
            SdpTypedTimeParseResult utf8Result = SdpTypedTimeParser.ParseUtf8TypedTime(Encoding.ASCII.GetBytes(input));

            Assert.Multiple(() =>
            {
                Assert.That(characterResult.Success, Is.False);
                Assert.That(characterResult.Invalid, Is.True);
                Assert.That(characterResult.Overflow, Is.False);
                Assert.That(characterResult.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
                Assert.That(utf8Result.Success, Is.False);
                Assert.That(utf8Result.Invalid, Is.True);
                Assert.That(utf8Result.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            });
        }
    }

    [Test]
    public void TypedTimeParsers_ReportInt64Overflow()
    {
        SdpTypedTimeParseResult unsignedBoundary = SdpTypedTimeParser.ParseCharacterTypedTime("9223372036854775807s".AsSpan());
        SdpTypedTimeParseResult unsignedOverflow = SdpTypedTimeParser.ParseCharacterTypedTime("9223372036854775808s".AsSpan());
        SdpTypedTimeParseResult signedBoundary = SdpTypedTimeParser.ParseUtf8SignedTypedTime(Encoding.ASCII.GetBytes("-9223372036854775808"));
        SdpTypedTimeParseResult signedOverflow = SdpTypedTimeParser.ParseUtf8SignedTypedTime(Encoding.ASCII.GetBytes("-9223372036854775809"));

        Assert.Multiple(() =>
        {
            Assert.That(unsignedBoundary.Success, Is.True);
            Assert.That(unsignedBoundary.Value.Value, Is.EqualTo(long.MaxValue));
            Assert.That(unsignedOverflow.Success, Is.False);
            Assert.That(unsignedOverflow.Overflow, Is.True);
            Assert.That(unsignedOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));

            Assert.That(signedBoundary.Success, Is.True);
            Assert.That(signedBoundary.Value.Value, Is.EqualTo(long.MinValue));
            Assert.That(signedOverflow.Success, Is.False);
            Assert.That(signedOverflow.Overflow, Is.True);
            Assert.That(signedOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
        });
    }

    [Test]
    public void CanonicalFormatter_UsesInvariantNumbersAndSelectedUnits()
    {
        SdpTypedTime days = new SdpTypedTime
        {
            Value = 7,
            Unit = "d",
            RawValue = "+0007D"
        };
        SdpTypedTime negativeHours = new SdpTypedTime
        {
            Value = -1,
            Unit = "h",
            RawValue = "-01h"
        };
        SdpTypedTime seconds = new SdpTypedTime
        {
            Value = 42,
            Unit = string.Empty,
            RawValue = "00042"
        };
        SdpTypedTime minutes = new SdpTypedTime
        {
            Value = 2,
            Unit = "m"
        };
        SdpTypedTime explicitSeconds = new SdpTypedTime
        {
            Value = 3,
            Unit = "s"
        };
        SdpTypedTime invalid = new SdpTypedTime
        {
            Value = 1,
            Unit = "H"
        };

        SdpTypedTimeFormatResult dayResult = SdpTypedTimeParser.FormatCanonicalTypedTime(days);
        SdpTypedTimeFormatResult hourResult = SdpTypedTimeParser.FormatCanonicalTypedTime(negativeHours);
        SdpTypedTimeFormatResult secondResult = SdpTypedTimeParser.FormatCanonicalTypedTime(seconds);
        SdpTypedTimeFormatResult minuteResult = SdpTypedTimeParser.FormatCanonicalTypedTime(minutes);
        SdpTypedTimeFormatResult explicitSecondResult = SdpTypedTimeParser.FormatCanonicalTypedTime(explicitSeconds);
        SdpTypedTimeFormatResult invalidResult = SdpTypedTimeParser.FormatCanonicalTypedTime(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(dayResult.Success, Is.True);
            Assert.That(dayResult.Text, Is.EqualTo("7d"));
            Assert.That(hourResult.Success, Is.True);
            Assert.That(hourResult.Text, Is.EqualTo("-1h"));
            Assert.That(secondResult.Success, Is.True);
            Assert.That(secondResult.Text, Is.EqualTo("42"));
            Assert.That(minuteResult.Success, Is.True);
            Assert.That(minuteResult.Text, Is.EqualTo("2m"));
            Assert.That(explicitSecondResult.Success, Is.True);
            Assert.That(explicitSecondResult.Text, Is.EqualTo("3s"));

            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Text, Is.Empty);
            Assert.That(invalidResult.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
        });
    }
}
