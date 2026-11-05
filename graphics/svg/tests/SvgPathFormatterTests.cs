namespace svg.Tests;

public class SvgPathFormatterTests
{
    [Test]
    public void CanonicalWriterEmitsEveryExplicitCommandWithStableSpacing()
    {
        string input = "M1 2L3 4H5V6C7 8 9 10 11 12S13 14 15 16Q17 18 19 20T21 22A2 3 45 0 1 23 24z";
        SvgPathParseResult parsed = new SvgPathParser().Parse(input, 16, 32);
        SvgPathFormatResult formatted = new SvgPathFormatter().FormatCanonicalWithPath(parsed.PathData);
        SvgPathParseResult reparsed = new SvgPathParser().Parse(formatted.Text, 16, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Success, Is.True);
            Assert.That(formatted.Text, Is.EqualTo("M 1 2 L 3 4 H 5 V 6 C 7 8 9 10 11 12 S 13 14 15 16 Q 17 18 19 20 T 21 22 A 2 3 45 0 1 23 24 z"));
            Assert.That(reparsed.Success, Is.True);
        });
        AssertEquivalent(parsed.PathData, reparsed.PathData);
    }

    [Test]
    public void MinifiedWriterOmitsSafeRepeatedCommandLettersAndSeparators()
    {
        SvgPathParseResult parsed = new SvgPathParser().Parse("M0 0 L10 -20 L30 40 H50 H-60 V70 V-80 Z", 16, 32);
        SvgPathFormatResult formatted = new SvgPathFormatter().FormatMinifiedWithPath(parsed.PathData);
        SvgPathParseResult reparsed = new SvgPathParser().Parse(formatted.Text, 16, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Success, Is.True);
            Assert.That(formatted.Text, Is.EqualTo("M0 0 10-20 30 40H50-60V70-80Z"));
            Assert.That(reparsed.Success, Is.True);
        });
        AssertEquivalent(parsed.PathData, reparsed.PathData);
    }

    [Test]
    public void MinifiedArcWriterCompactsFlagsAndFollowingCoordinates()
    {
        SvgPathParseResult parsed = new SvgPathParser().Parse("M0 0 A5 6 45 0 1 10 -20 A7 8 0 1 0 .5 .6", 8, 32);
        SvgPathFormatResult formatted = new SvgPathFormatter().FormatMinifiedWithPath(parsed.PathData);
        SvgPathParseResult reparsed = new SvgPathParser().Parse(formatted.Text, 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Text, Is.EqualTo("M0 0A5 6 45 0110-20 7 8 0 10.5.6"));
            Assert.That(reparsed.Success, Is.True);
        });
        AssertEquivalent(parsed.PathData, reparsed.PathData);
    }

    [Test]
    public void MinifiedWriterPreservesExponentSignAndDecimalBoundaries()
    {
        SvgPathParseResult parsed = new SvgPathParser().Parse("M1e21 -2 L3 -4 L5e-7 .6", 8, 64);
        SvgPathFormatResult formatted = new SvgPathFormatter().FormatMinifiedWithPath(parsed.PathData);
        SvgPathParseResult reparsed = new SvgPathParser().Parse(formatted.Text, 8, 64);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Text, Is.EqualTo("M1e21-2 3-4 5e-7.6"));
            Assert.That(reparsed.Success, Is.True);
        });
        AssertEquivalent(parsed.PathData, reparsed.PathData);
    }

    [Test]
    public void ConfiguredFractionalPrecisionRoundsAndTrimsBothModes()
    {
        SvgPathParseResult parsed = new SvgPathParser().Parse("M1.23456 -0.0049 L0.5 -0.5", 4, 32);
        SvgPathFormatResult canonical = new SvgPathFormatter().FormatCanonicalWithPathAndFractionalDigits(parsed.PathData, 2);
        SvgPathFormatResult minified = new SvgPathFormatter().FormatMinifiedWithPathAndFractionalDigits(parsed.PathData, 2);

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Success, Is.True);
            Assert.That(canonical.Text, Is.EqualTo("M 1.23 0 L 0.5 -0.5"));
            Assert.That(minified.Success, Is.True);
            Assert.That(minified.Text, Is.EqualTo("M1.23 0 .5-.5"));
            Assert.That(new SvgPathParser().Parse(canonical.Text, 4, 32).Success, Is.True);
            Assert.That(new SvgPathParser().Parse(minified.Text, 4, 32).Success, Is.True);
        });
    }

    [Test]
    public void ExactModesRoundTripExtremeFiniteValues()
    {
        string input = "M1.7976931348623157e308 5e-324 L-1.7976931348623157e308 -5e-324";
        SvgPathParseResult parsed = new SvgPathParser().Parse(input, 4, 64);
        SvgPathFormatResult canonical = new SvgPathFormatter().FormatCanonicalWithPath(parsed.PathData);
        SvgPathFormatResult minified = new SvgPathFormatter().FormatMinifiedWithPath(parsed.PathData);
        SvgPathParseResult canonicalParsed = new SvgPathParser().Parse(canonical.Text, 4, 512);
        SvgPathParseResult minifiedParsed = new SvgPathParser().Parse(minified.Text, 4, 512);

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Success, Is.True);
            Assert.That(minified.Success, Is.True);
            Assert.That(canonicalParsed.Success, Is.True);
            Assert.That(minifiedParsed.Success, Is.True);
        });
        AssertEquivalent(parsed.PathData, canonicalParsed.PathData);
        AssertEquivalent(parsed.PathData, minifiedParsed.PathData);
    }

    [Test]
    public void PathConvenienceMethodsUseExactCanonicalAndMinifiedWriters()
    {
        SvgPathData path = new SvgPathParser().Parse("M0.5 0.25 L1 -2", 4, 32).PathData;

        Assert.Multiple(() =>
        {
            Assert.That(path.ToText(), Is.EqualTo("M 0.5 0.25 L 1 -2"));
            Assert.That(path.ToMinifiedText(), Is.EqualTo("M.5.25 1-2"));
            Assert.That(new SvgPathParser().Parse(path.ToMinifiedText(), 4, 32).Success, Is.True);
        });
    }

    [Test]
    public void InvalidOptionsAndMutablePathStateReturnStableFailures()
    {
        SvgPathParseResult parsed = new SvgPathParser().Parse("M0 0 A1 1 0 0 1 2 2", 4, 32);
        SvgPathFormatOptions invalidModeOptions = SvgPathFormatOptions.Create((SvgPathFormatMode)99, false, 0);
        SvgPathFormatResult nullPath = new SvgPathFormatter().FormatCanonicalWithPath(null);
        SvgPathFormatResult nullOptions = new SvgPathFormatter().Format(parsed.PathData, null);
        SvgPathFormatResult invalidMode = new SvgPathFormatter().Format(parsed.PathData, invalidModeOptions);
        SvgPathFormatResult lowPrecision = new SvgPathFormatter().FormatCanonicalWithPathAndFractionalDigits(parsed.PathData, -1);
        SvgPathFormatResult highPrecision = new SvgPathFormatter().FormatMinifiedWithPathAndFractionalDigits(parsed.PathData, 100);
        parsed.PathData.Commands[1].Values[3] = 2;
        SvgPathFormatResult invalidArc = new SvgPathFormatter().FormatCanonicalWithPath(parsed.PathData);

        Assert.Multiple(() =>
        {
            Assert.That(nullPath.Error, Is.EqualTo(SvgPathFormatError.InvalidArgument));
            Assert.That(nullOptions.Error, Is.EqualTo(SvgPathFormatError.InvalidArgument));
            Assert.That(invalidMode.Error, Is.EqualTo(SvgPathFormatError.InvalidMode));
            Assert.That(lowPrecision.Error, Is.EqualTo(SvgPathFormatError.InvalidPrecision));
            Assert.That(highPrecision.Error, Is.EqualTo(SvgPathFormatError.InvalidPrecision));
            Assert.That(invalidArc.Error, Is.EqualTo(SvgPathFormatError.InvalidPath));
            Assert.That(invalidArc.Text, Is.Empty);
        });
    }

    [Test]
    public void EmptyAndRecoveredInvalidPathsHaveDeterministicResults()
    {
        SvgPathData empty = new SvgPathParser().Parse("", 4, 32).PathData;
        SvgPathData recovered = new SvgPathParser().Parse("M0 0 X L1 1", 4, 32).PathData;

        Assert.Multiple(() =>
        {
            Assert.That(new SvgPathFormatter().FormatCanonicalWithPath(empty).Text, Is.Empty);
            Assert.That(new SvgPathFormatter().FormatMinifiedWithPath(empty).Text, Is.Empty);
            Assert.That(new SvgPathFormatter().FormatCanonicalWithPath(recovered).Error, Is.EqualTo(SvgPathFormatError.InvalidPath));
            Assert.That(recovered.ToText(), Is.Empty);
        });
    }

    static void AssertEquivalent(SvgPathData expected, SvgPathData actual)
    {
        Assert.That(actual.Commands, Has.Count.EqualTo(expected.Commands.Count));
        int commandIndex = 0;
        while (commandIndex < expected.Commands.Count)
        {
            SvgPathCommand expectedCommand = expected.Commands[commandIndex];
            SvgPathCommand actualCommand = actual.Commands[commandIndex];
            Assert.Multiple(() =>
            {
                Assert.That(actualCommand.Kind, Is.EqualTo(expectedCommand.Kind));
                Assert.That(actualCommand.Relative, Is.EqualTo(expectedCommand.Relative));
                Assert.That(actualCommand.Values, Has.Length.EqualTo(expectedCommand.Values.Length));
            });
            int valueIndex = 0;
            while (valueIndex < expectedCommand.Values.Length)
            {
                Assert.That(BitConverter.DoubleToInt64Bits(actualCommand.Values[valueIndex]), Is.EqualTo(BitConverter.DoubleToInt64Bits(expectedCommand.Values[valueIndex])));
                valueIndex = valueIndex + 1;
            }
            commandIndex = commandIndex + 1;
        }
    }
}
