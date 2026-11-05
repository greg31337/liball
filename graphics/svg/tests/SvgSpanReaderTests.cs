namespace svg.Tests;

public class SvgSpanReaderTests
{
    [Test]
    public void CharacterReadsTrackPositionsAndPreservePositionAfterFailure()
    {
        SvgSpanReader reader = new SvgSpanReader("abc", 16);

        SvgSpanCharacterResult peek = reader.PeekCharacter();
        SvgSpanCharacterResult read = reader.ReadCharacter();
        bool consumedWrong = reader.Consume('x');
        bool consumedRight = reader.Consume('b');
        bool movedBeforeStart = reader.SetPosition(-1);
        bool movedAfterEnd = reader.SetPosition(4);
        bool movedToEnd = reader.SetPosition(3);
        SvgSpanCharacterResult end = reader.ReadCharacter();

        Assert.Multiple(() =>
        {
            Assert.That(peek.Success, Is.True);
            Assert.That(peek.Value, Is.EqualTo('a'));
            Assert.That(peek.Start, Is.Zero);
            Assert.That(peek.End, Is.Zero);
            Assert.That(read.Success, Is.True);
            Assert.That(read.Value, Is.EqualTo('a'));
            Assert.That(read.Start, Is.Zero);
            Assert.That(read.End, Is.EqualTo(1));
            Assert.That(consumedWrong, Is.False);
            Assert.That(consumedRight, Is.True);
            Assert.That(movedBeforeStart, Is.False);
            Assert.That(movedAfterEnd, Is.False);
            Assert.That(movedToEnd, Is.True);
            Assert.That(end.Success, Is.False);
            Assert.That(end.Error, Is.EqualTo(SvgSpanReadError.EndOfInput));
            Assert.That(end.ErrorOffset, Is.EqualTo(3));
            Assert.That(reader.GetPosition(), Is.EqualTo(3));
            Assert.That(reader.IsEnd(), Is.True);
        });
    }

    [Test]
    public void ReaderUsesMemorySliceOffsetsWithoutCopyingOrParentOffsets()
    {
        ReadOnlyMemory<char> memory = "--value++".AsMemory(2, 5);
        SvgSpanReader reader = SvgSpanReader.FromMemory(memory, 5);
        SvgSpanTokenResult token = reader.ReadToken(IsAsciiLetter, false);

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetLength(), Is.EqualTo(5));
            Assert.That(reader.GetMaximumTokenCharacters(), Is.EqualTo(5));
            Assert.That(token.Success, Is.True);
            Assert.That(token.Start, Is.Zero);
            Assert.That(token.End, Is.EqualTo(5));
            Assert.That(token.RawText.Span.ToString(), Is.EqualTo("value"));
            Assert.That(token.DecodedValue, Is.Null);
            Assert.That(token.GetValue(), Is.EqualTo("value"));
        });
    }

    [Test]
    public void SvgAndCssWhitespaceHaveDistinctFormFeedHandling()
    {
        SvgSpanReader reader = new SvgSpanReader(" \t\r\n\fX", 16);

        int svgCount = reader.SkipWhitespace();
        int cssCount = reader.SkipCssWhitespace();

        Assert.Multiple(() =>
        {
            Assert.That(svgCount, Is.EqualTo(4));
            Assert.That(cssCount, Is.EqualTo(1));
            Assert.That(reader.GetPosition(), Is.EqualTo(5));
            Assert.That(SvgSpanCharacters.IsSvgWhitespace('\f'), Is.False);
            Assert.That(SvgSpanCharacters.IsCssWhitespace('\f'), Is.True);
            Assert.That(SvgSpanCharacters.IsCssWhitespace('X'), Is.False);
        });
    }

    [Test]
    public void OptionalCommaWhitespaceReportsEverythingItConsumes()
    {
        SvgSpanReader reader = new SvgSpanReader(" \t, \nX", 16);

        SvgSpanSeparatorResult separator = reader.SkipOptionalCommaWhitespace();

        Assert.Multiple(() =>
        {
            Assert.That(separator.Success, Is.True);
            Assert.That(separator.Error, Is.EqualTo(SvgSpanReadError.None));
            Assert.That(separator.Start, Is.Zero);
            Assert.That(separator.End, Is.EqualTo(5));
            Assert.That(separator.HasComma, Is.True);
            Assert.That(separator.WhitespaceCharacters, Is.EqualTo(4));
            Assert.That(reader.GetPosition(), Is.EqualTo(5));
            Assert.That(reader.PeekCharacter().Value, Is.EqualTo('X'));
        });
    }

    [Test]
    public void RequiredCommaWhitespaceAcceptsEitherFormAndRollsBackWhenAbsent()
    {
        SvgSpanReader whitespaceReader = new SvgSpanReader(" \tX", 16);
        SvgSpanReader commaReader = new SvgSpanReader(",X", 16);
        SvgSpanReader missingReader = new SvgSpanReader("X", 16);
        SvgSpanReader emptyReader = new SvgSpanReader("", 16);

        SvgSpanSeparatorResult whitespace = whitespaceReader.ReadCommaWhitespace();
        SvgSpanSeparatorResult comma = commaReader.ReadCommaWhitespace();
        SvgSpanSeparatorResult missing = missingReader.ReadCommaWhitespace();
        SvgSpanSeparatorResult empty = emptyReader.ReadCommaWhitespace();

        Assert.Multiple(() =>
        {
            Assert.That(whitespace.Success, Is.True);
            Assert.That(whitespace.HasComma, Is.False);
            Assert.That(whitespace.WhitespaceCharacters, Is.EqualTo(2));
            Assert.That(comma.Success, Is.True);
            Assert.That(comma.HasComma, Is.True);
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Error, Is.EqualTo(SvgSpanReadError.UnexpectedCharacter));
            Assert.That(missing.ErrorOffset, Is.Zero);
            Assert.That(missingReader.GetPosition(), Is.Zero);
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Error, Is.EqualTo(SvgSpanReadError.EndOfInput));
            Assert.That(emptyReader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void EscapeReaderDecodesSimpleHexAndSupplementaryEscapes()
    {
        SvgSpanReader simpleReader = new SvgSpanReader("\\)", 16);
        SvgSpanReader hexReader = new SvgSpanReader("\\41 xy", 16);
        SvgSpanReader supplementaryReader = new SvgSpanReader("\\01F600z", 16);

        SvgSpanEscapeResult simple = simpleReader.ReadEscape();
        SvgSpanEscapeResult hex = hexReader.ReadEscape();
        SvgSpanEscapeResult supplementary = supplementaryReader.ReadEscape();

        Assert.Multiple(() =>
        {
            Assert.That(simple.Success, Is.True);
            Assert.That(simple.CodePoint, Is.EqualTo(')'));
            Assert.That(simple.Value, Is.EqualTo(")"));
            Assert.That(simple.End, Is.EqualTo(2));
            Assert.That(hex.Success, Is.True);
            Assert.That(hex.CodePoint, Is.EqualTo(0x41));
            Assert.That(hex.Value, Is.EqualTo("A"));
            Assert.That(hexReader.GetPosition(), Is.EqualTo(4));
            Assert.That(hexReader.PeekCharacter().Value, Is.EqualTo('x'));
            Assert.That(supplementary.Success, Is.True);
            Assert.That(supplementary.CodePoint, Is.EqualTo(0x1F600));
            Assert.That(supplementary.Value, Is.EqualTo("😀"));
            Assert.That(supplementaryReader.GetPosition(), Is.EqualTo(7));
        });
    }

    [TestCase("\\0 ")]
    [TestCase("\\D800 ")]
    [TestCase("\\110000 ")]
    public void EscapeReaderReplacesInvalidUnicodeScalars(string input)
    {
        SvgSpanEscapeResult result = new SvgSpanReader(input, 16).ReadEscape();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CodePoint, Is.EqualTo(0xFFFD));
            Assert.That(result.Value, Is.EqualTo("�"));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void HexEscapeConsumesCrLfAsOneTerminatingWhitespaceSequence()
    {
        SvgSpanReader reader = new SvgSpanReader("\\61\r\nb", 16);

        SvgSpanEscapeResult result = reader.ReadEscape();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo("a"));
            Assert.That(result.End, Is.EqualTo(5));
            Assert.That(reader.GetPosition(), Is.EqualTo(5));
            Assert.That(reader.PeekCharacter().Value, Is.EqualTo('b'));
        });
    }

    [TestCase("", 1, 0, 0)]
    [TestCase("x", 2, 0, 0)]
    [TestCase("\\", 3, 1, 1)]
    [TestCase("\\\n", 3, 1, 2)]
    public void MalformedEscapesReturnExactFailuresAndRollBack(string input, int errorValue, int errorOffset, int end)
    {
        SvgSpanReader reader = new SvgSpanReader(input, 16);

        SvgSpanEscapeResult result = reader.ReadEscape();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That((int)result.Error, Is.EqualTo(errorValue));
            Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
            Assert.That(result.End, Is.EqualTo(end));
            Assert.That(result.Value, Is.Empty);
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void TokenReaderReturnsRawSlicesAndDecodesEscapesOnlyWhenNeeded()
    {
        SvgSpanReader rawReader = new SvgSpanReader("alpha-1!", 16);
        SvgSpanReader escapedReader = new SvgSpanReader("ab\\63 d!", 16);

        SvgSpanTokenResult raw = rawReader.ReadToken(IsNameCharacter, true);
        SvgSpanTokenResult escaped = escapedReader.ReadToken(IsNameCharacter, true);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Success, Is.True);
            Assert.That(raw.RawText.Span.ToString(), Is.EqualTo("alpha-1"));
            Assert.That(raw.DecodedValue, Is.Null);
            Assert.That(raw.HadEscape, Is.False);
            Assert.That(rawReader.GetPosition(), Is.EqualTo(7));
            Assert.That(escaped.Success, Is.True);
            Assert.That(escaped.RawText.Span.ToString(), Is.EqualTo("ab\\63 d"));
            Assert.That(escaped.GetValue(), Is.EqualTo("abcd"));
            Assert.That(escaped.HadEscape, Is.True);
            Assert.That(escapedReader.GetPosition(), Is.EqualTo(7));
        });
    }

    [Test]
    public void TokenLengthFailuresReportExactOffsetsAndRollBack()
    {
        SvgSpanReader ordinaryReader = new SvgSpanReader("abcd", 3);
        SvgSpanReader exactReader = new SvgSpanReader("abc!", 3);
        SvgSpanReader escapedReader = new SvgSpanReader("a\\62", 3);

        SvgSpanTokenResult ordinary = ordinaryReader.ReadToken(IsAsciiLetter, false);
        SvgSpanTokenResult exact = exactReader.ReadToken(IsAsciiLetter, false);
        SvgSpanTokenResult escaped = escapedReader.ReadToken(IsAsciiLetter, true);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.Success, Is.False);
            Assert.That(ordinary.Error, Is.EqualTo(SvgSpanReadError.TokenTooLong));
            Assert.That(ordinary.ErrorOffset, Is.EqualTo(3));
            Assert.That(ordinary.End, Is.EqualTo(4));
            Assert.That(ordinary.RawText.Span.ToString(), Is.EqualTo("abcd"));
            Assert.That(ordinaryReader.GetPosition(), Is.Zero);
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.GetValue(), Is.EqualTo("abc"));
            Assert.That(exactReader.GetPosition(), Is.EqualTo(3));
            Assert.That(escaped.Success, Is.False);
            Assert.That(escaped.Error, Is.EqualTo(SvgSpanReadError.TokenTooLong));
            Assert.That(escaped.ErrorOffset, Is.EqualTo(3));
            Assert.That(escapedReader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void TokenReaderReturnsResultsForInvalidConfigurationAndMalformedEscapes()
    {
        SvgSpanReader invalidLimitReader = new SvgSpanReader("abc", 0);
        SvgSpanReader invalidPredicateReader = new SvgSpanReader("abc", 16);
        SvgSpanReader invalidEscapeReader = new SvgSpanReader("ab\\\n", 16);

        SvgSpanTokenResult invalidLimit = invalidLimitReader.ReadToken(IsAsciiLetter, false);
        SvgSpanTokenResult invalidPredicate = invalidPredicateReader.ReadToken(null, false);
        SvgSpanTokenResult invalidEscape = invalidEscapeReader.ReadToken(IsAsciiLetter, true);

        Assert.Multiple(() =>
        {
            Assert.That(invalidLimit.Success, Is.False);
            Assert.That(invalidLimit.Error, Is.EqualTo(SvgSpanReadError.InvalidArgument));
            Assert.That(invalidLimitReader.GetPosition(), Is.Zero);
            Assert.That(invalidPredicate.Success, Is.False);
            Assert.That(invalidPredicate.Error, Is.EqualTo(SvgSpanReadError.InvalidArgument));
            Assert.That(invalidPredicateReader.GetPosition(), Is.Zero);
            Assert.That(invalidEscape.Success, Is.False);
            Assert.That(invalidEscape.Error, Is.EqualTo(SvgSpanReadError.InvalidEscape));
            Assert.That(invalidEscape.ErrorOffset, Is.EqualTo(3));
            Assert.That(invalidEscape.End, Is.EqualTo(4));
            Assert.That(invalidEscapeReader.GetPosition(), Is.Zero);
        });
    }

    static bool IsAsciiLetter(char value)
    {
        return value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
    }

    static bool IsNameCharacter(char value)
    {
        return IsAsciiLetter(value) || value >= '0' && value <= '9' || value == '-';
    }
}
