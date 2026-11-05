namespace sip.Tests;

public class FoundationUtilityTests
{
    [Test]
    public void BoundedByteReaderTracksPositionAndPreventsOverread()
    {
        SipBoundedByteReader reader = new SipBoundedByteReader(new byte[] { 1, 2, 3, 4 }, 4);

        Assert.That(reader.GetPosition(), Is.Zero);
        Assert.That(reader.GetRemainingCount(), Is.EqualTo(4));
        Assert.That(reader.PeekByte(), Is.EqualTo(1));
        Assert.That(reader.GetPosition(), Is.Zero);
        Assert.That(reader.ReadByte(), Is.EqualTo(1));
        Assert.That(reader.ReadBytes(2), Is.EqualTo(new byte[] { 2, 3 }));
        Assert.That(reader.TrySkip(1), Is.True);
        Assert.That(reader.GetPosition(), Is.EqualTo(4));
        Assert.That(reader.GetRemainingCount(), Is.Zero);
        Assert.That(reader.ReadByte(), Is.EqualTo(-1));
        Assert.That(reader.TrySkip(1), Is.False);
        Assert.Throws<InvalidOperationException>(() => reader.ReadBytes(1));
    }

    [Test]
    public void BoundedTextReaderTracksPositionAndPreventsOverread()
    {
        SipBoundedTextReader reader = new SipBoundedTextReader("INVITE", 6);

        Assert.That(reader.PeekCharacter(), Is.EqualTo('I'));
        Assert.That(reader.ReadCharacter(), Is.EqualTo('I'));
        Assert.That(reader.ReadText(4), Is.EqualTo("NVIT"));
        Assert.That(reader.GetPosition(), Is.EqualTo(5));
        Assert.That(reader.GetRemainingCount(), Is.EqualTo(1));
        Assert.That(reader.TrySkip(1), Is.True);
        Assert.That(reader.PeekCharacter(), Is.EqualTo(-1));
        Assert.That(reader.TrySkip(-1), Is.False);
        Assert.Throws<InvalidOperationException>(() => reader.ReadText(1));
    }

    [Test]
    public void BoundedReadersRejectInputsAboveTheirLimits()
    {
        Assert.Throws<ArgumentException>(() => new SipBoundedByteReader(new byte[3], 2));
        Assert.Throws<ArgumentException>(() => new SipBoundedTextReader("SIP", 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SipBoundedByteReader(Array.Empty<byte>(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SipBoundedTextReader(string.Empty, -1));
    }

    [Test]
    public void AsciiUtilitiesRecognizeSipTokensAndCompareAsciiCase()
    {
        Assert.That(SipAscii.IsToken("AZaz09-.!%*_+`'~"), Is.True);
        Assert.That(SipAscii.IsToken("not a token"), Is.False);
        Assert.That(SipAscii.IsToken("bad/token"), Is.False);
        Assert.That(SipAscii.IsToken("café"), Is.False);
        Assert.That(SipAscii.IsToken(string.Empty), Is.False);
        Assert.That(SipAscii.IsToken(null), Is.False);
        Assert.That(SipAscii.EqualsIgnoreCase("Content-Length", "content-length"), Is.True);
        Assert.That(SipAscii.EqualsIgnoreCase("Ä", "ä"), Is.False);
        Assert.That(SipAscii.EqualsIgnoreCase(null, null), Is.True);
        Assert.That(SipAscii.EqualsIgnoreCase(null, ""), Is.False);
        Assert.That(SipAscii.CompareIgnoreCase("Via", "via"), Is.Zero);
        Assert.That(SipAscii.CompareIgnoreCase("Accept", "Via"), Is.LessThan(0));
        Assert.That(SipAscii.CompareIgnoreCase("Via", "Via-Branch"), Is.LessThan(0));
    }

    [Test]
    public void NumericParserAcceptsBoundsAndReportsInvalidOffsets()
    {
        SipNumericParseResult maximum = SipNumericParser.ParseNonNegativeInt32Text("2147483647", int.MaxValue);
        SipNumericParseResult bounded = SipNumericParser.ParseNonNegativeInt32Text("699", 699);
        SipNumericParseResult bytes = SipNumericParser.ParseNonNegativeInt32Bytes(System.Text.Encoding.ASCII.GetBytes("5070"), 9999);
        SipNumericParseResult overflow = SipNumericParser.ParseNonNegativeInt32Text("2147483648", int.MaxValue);
        SipNumericParseResult aboveBound = SipNumericParser.ParseNonNegativeInt32Text("700", 699);
        SipNumericParseResult malformed = SipNumericParser.ParseNonNegativeInt32Text("12x", int.MaxValue);
        SipNumericParseResult empty = SipNumericParser.ParseNonNegativeInt32Text("", int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Success, Is.True);
            Assert.That(maximum.Value, Is.EqualTo(int.MaxValue));
            Assert.That(maximum.CharactersRead, Is.EqualTo(10));
            Assert.That(maximum.ErrorOffset, Is.EqualTo(-1));
            Assert.That(bounded.Success, Is.True);
            Assert.That(bounded.Value, Is.EqualTo(699));
            Assert.That(bytes.Success, Is.True);
            Assert.That(bytes.Value, Is.EqualTo(5070));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.ErrorOffset, Is.EqualTo(9));
            Assert.That(aboveBound.Success, Is.False);
            Assert.That(aboveBound.ErrorOffset, Is.EqualTo(2));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.CharactersRead, Is.EqualTo(2));
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.ErrorOffset, Is.Zero);
        });
    }

    [Test]
    public void OwnedBufferDefensivelyCopiesAndCanBeCleared()
    {
        byte[] source = new byte[] { 1, 2, 3, 4 };
        SipOwnedBuffer owner = new SipOwnedBuffer(source);
        source[0] = 9;

        byte[] firstCopy = owner.CopyBytes();
        firstCopy[1] = 9;

        Assert.That(owner.GetLength(), Is.EqualTo(4));
        Assert.That(owner.CopyBytes(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(owner.CopySegment(1, 2), Is.EqualTo(new byte[] { 2, 3 }));
        Assert.Throws<ArgumentException>(() => owner.CopySegment(4, 1));

        owner.Clear();

        Assert.That(owner.CopyBytes(), Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    [Test]
    public void RandomTokenGeneratorUsesInjectedBytesAndUrlSafeEncoding()
    {
        SipRandomTokenGenerator hexGenerator = new SipRandomTokenGenerator(new FixedRandomSource(new byte[] { 0, 1, 2, 255 }));
        SipRandomTokenGenerator urlGenerator = new SipRandomTokenGenerator(new FixedRandomSource(new byte[] { 251, 255, 255 }));

        Assert.That(hexGenerator.CreateHexToken(4), Is.EqualTo("000102ff"));
        Assert.That(urlGenerator.CreateBase64UrlToken(3), Is.EqualTo("-___"));
        Assert.Throws<ArgumentOutOfRangeException>(() => hexGenerator.CreateHexToken(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => hexGenerator.CreateBase64UrlToken(1025));
    }

    [Test]
    public void CryptographicRandomSourceProducesTokenShapedOutput()
    {
        SipRandomTokenGenerator generator = new SipRandomTokenGenerator(new SipCryptographicRandomSource());

        string token = generator.CreateHexToken(16);

        Assert.That(token, Has.Length.EqualTo(32));
        Assert.That(token, Does.Match("^[0-9a-f]{32}$"));
    }

    [Test]
    public void MonotonicClocksNeverMoveBackward()
    {
        SipManualMonotonicClock manual = new SipManualMonotonicClock(100);
        ISipMonotonicClock injected = manual;
        SipSystemMonotonicClock system = new SipSystemMonotonicClock();
        long systemStart = system.GetMilliseconds();

        manual.AdvanceMilliseconds(25);
        long systemEnd = system.GetMilliseconds();

        Assert.That(injected.GetMilliseconds(), Is.EqualTo(125));
        Assert.That(systemEnd, Is.GreaterThanOrEqualTo(systemStart));
        Assert.Throws<ArgumentOutOfRangeException>(() => manual.AdvanceMilliseconds(-1));

        SipManualMonotonicClock nearLimit = new SipManualMonotonicClock(long.MaxValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => nearLimit.AdvanceMilliseconds(1));
    }
}

class FixedRandomSource : ISipRandomSource
{
    byte[] values;

    public FixedRandomSource(byte[] values)
    {
        this.values = values;
    }

    public void FillBytes(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = values[index % values.Length];
            index = index + 1;
        }
    }
}
