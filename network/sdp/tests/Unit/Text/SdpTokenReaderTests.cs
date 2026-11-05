using System.Text;

namespace sdp.Tests.Unit.Text;

public class SdpTokenReaderTests
{
    [Test]
    public void SpaceReaders_ConsumeAsciiSpacesFromTheRequestedOffset()
    {
        string characters = "x   value";
        byte[] utf8 = Encoding.UTF8.GetBytes(characters);

        int characterOffset = SdpTokenReader.SkipCharacterSpaces(characters.AsSpan(), 1);
        int byteOffset = SdpTokenReader.SkipUtf8Spaces(utf8.AsSpan(), 1);

        Assert.Multiple(() =>
        {
            Assert.That(characterOffset, Is.EqualTo(4));
            Assert.That(byteOffset, Is.EqualTo(4));
            Assert.That(SdpTokenReader.SkipCharacterSpaces("\tvalue".AsSpan(), 0), Is.EqualTo(0));
            Assert.That(SdpTokenReader.SkipUtf8Spaces(new byte[] { 9, 65 }, 0), Is.EqualTo(0));
            Assert.That(SdpTokenReader.SkipCharacterSpaces(characters.AsSpan(), characters.Length), Is.EqualTo(characters.Length));
        });
    }

    [Test]
    public void NonWhitespaceReaders_ReturnOffsetsWithoutAllocatingTokens()
    {
        string characters = "  alpha\tbeta";
        byte[] utf8 = Encoding.UTF8.GetBytes("éclair beta");

        SdpTokenReadResult characterResult = SdpTokenReader.ReadCharacterNonWhitespaceToken(characters.AsSpan(), 2, 5);
        SdpTokenReadResult utf8Result = SdpTokenReader.ReadUtf8NonWhitespaceToken(utf8.AsSpan(), 0, 7);

        Assert.Multiple(() =>
        {
            Assert.That(characterResult.Success, Is.True);
            Assert.That(characterResult.StartOffset, Is.EqualTo(2));
            Assert.That(characterResult.Length, Is.EqualTo(5));
            Assert.That(characterResult.NextOffset, Is.EqualTo(7));
            Assert.That(characters.AsSpan(characterResult.StartOffset, characterResult.Length).ToString(), Is.EqualTo("alpha"));

            Assert.That(utf8Result.Success, Is.True);
            Assert.That(utf8Result.StartOffset, Is.EqualTo(0));
            Assert.That(utf8Result.Length, Is.EqualTo(7));
            Assert.That(utf8Result.NextOffset, Is.EqualTo(7));
            Assert.That(Encoding.UTF8.GetString(utf8.AsSpan(utf8Result.StartOffset, utf8Result.Length)), Is.EqualTo("éclair"));
        });
    }

    [Test]
    public void NonWhitespaceReaders_EnforceBoundariesAndRequireContent()
    {
        SdpTokenReadResult boundary = SdpTokenReader.ReadCharacterNonWhitespaceToken("abcd next".AsSpan(), 0, 4);
        SdpTokenReadResult overlong = SdpTokenReader.ReadCharacterNonWhitespaceToken("abcd next".AsSpan(), 0, 3);
        SdpTokenReadResult empty = SdpTokenReader.ReadCharacterNonWhitespaceToken(" rest".AsSpan(), 0, 4);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Success, Is.True);
            Assert.That(boundary.TokenLengthExceeded, Is.False);

            Assert.That(overlong.Success, Is.False);
            Assert.That(overlong.TokenLengthExceeded, Is.True);
            Assert.That(overlong.Length, Is.EqualTo(4));
            Assert.That(overlong.NextOffset, Is.EqualTo(4));

            Assert.That(empty.Success, Is.False);
            Assert.That(empty.TokenLengthExceeded, Is.False);
            Assert.That(empty.Length, Is.EqualTo(0));
            Assert.That(empty.NextOffset, Is.EqualTo(0));
        });
    }

    [Test]
    public void AttributeNameReaders_RecognizeTokenGrammarAndColonBoundary()
    {
        string valued = "rtpmap:96 opus/48000";
        string property = "recvonly";
        byte[] utf8 = Encoding.UTF8.GetBytes("ice-ufrag:value");

        SdpTokenReadResult valuedResult = SdpTokenReader.ReadCharacterAttributeName(valued.AsSpan(), 0, 20);
        SdpTokenReadResult propertyResult = SdpTokenReader.ReadCharacterAttributeName(property.AsSpan(), 0, 20);
        SdpTokenReadResult invalidResult = SdpTokenReader.ReadCharacterAttributeName("bad/name:value".AsSpan(), 0, 20);
        SdpTokenReadResult utf8Result = SdpTokenReader.ReadUtf8AttributeName(utf8.AsSpan(), 0, 20);

        Assert.Multiple(() =>
        {
            Assert.That(valuedResult.Success, Is.True);
            Assert.That(valuedResult.Length, Is.EqualTo(6));
            Assert.That(valuedResult.NextOffset, Is.EqualTo(6));
            Assert.That(valued[valuedResult.NextOffset], Is.EqualTo(':'));

            Assert.That(propertyResult.Success, Is.True);
            Assert.That(propertyResult.NextOffset, Is.EqualTo(property.Length));

            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Length, Is.EqualTo(3));
            Assert.That(invalidResult.NextOffset, Is.EqualTo(3));

            Assert.That(utf8Result.Success, Is.True);
            Assert.That(utf8Result.Length, Is.EqualTo(9));
            Assert.That(utf8[utf8Result.NextOffset], Is.EqualTo(58));
        });
    }

    [Test]
    public void DecimalReaders_RecognizeUnsignedAndSignedTokens()
    {
        string characters = "x1234 -27 +9 - tail";
        byte[] utf8 = Encoding.UTF8.GetBytes("42 +17");

        SdpTokenReadResult unsignedResult = SdpTokenReader.ReadCharacterDecimalToken(characters.AsSpan(), 1, 4);
        SdpTokenReadResult negativeResult = SdpTokenReader.ReadCharacterSignedDecimalToken(characters.AsSpan(), 6, 3);
        SdpTokenReadResult positiveResult = SdpTokenReader.ReadCharacterSignedDecimalToken(characters.AsSpan(), 10, 2);
        SdpTokenReadResult loneSign = SdpTokenReader.ReadCharacterSignedDecimalToken(characters.AsSpan(), 13, 4);
        SdpTokenReadResult utf8Unsigned = SdpTokenReader.ReadUtf8DecimalToken(utf8.AsSpan(), 0, 2);
        SdpTokenReadResult utf8Signed = SdpTokenReader.ReadUtf8SignedDecimalToken(utf8.AsSpan(), 3, 3);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedResult.Success, Is.True);
            Assert.That(characters.AsSpan(unsignedResult.StartOffset, unsignedResult.Length).ToString(), Is.EqualTo("1234"));
            Assert.That(unsignedResult.NextOffset, Is.EqualTo(5));

            Assert.That(negativeResult.Success, Is.True);
            Assert.That(negativeResult.Length, Is.EqualTo(3));
            Assert.That(characters.AsSpan(negativeResult.StartOffset, negativeResult.Length).ToString(), Is.EqualTo("-27"));

            Assert.That(positiveResult.Success, Is.True);
            Assert.That(characters.AsSpan(positiveResult.StartOffset, positiveResult.Length).ToString(), Is.EqualTo("+9"));

            Assert.That(loneSign.Success, Is.False);
            Assert.That(loneSign.Length, Is.EqualTo(0));
            Assert.That(loneSign.NextOffset, Is.EqualTo(13));

            Assert.That(utf8Unsigned.Success, Is.True);
            Assert.That(utf8Unsigned.Length, Is.EqualTo(2));
            Assert.That(utf8Signed.Success, Is.True);
            Assert.That(utf8Signed.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void RemainderReaders_PreserveWhitespaceAndAllowEmptyValues()
    {
        string characters = "name:  complete value";
        byte[] utf8 = Encoding.UTF8.GetBytes(characters);

        SdpTokenReadResult characterResult = SdpTokenReader.CaptureCharacterRemainder(characters.AsSpan(), 5);
        SdpTokenReadResult characterEmpty = SdpTokenReader.CaptureCharacterRemainder(characters.AsSpan(), characters.Length);
        SdpTokenReadResult utf8Result = SdpTokenReader.CaptureUtf8Remainder(utf8.AsSpan(), 5);
        SdpTokenReadResult utf8Empty = SdpTokenReader.CaptureUtf8Remainder(utf8.AsSpan(), utf8.Length);

        Assert.Multiple(() =>
        {
            Assert.That(characterResult.Success, Is.True);
            Assert.That(characters.AsSpan(characterResult.StartOffset, characterResult.Length).ToString(), Is.EqualTo("  complete value"));
            Assert.That(characterResult.NextOffset, Is.EqualTo(characters.Length));
            Assert.That(characterEmpty.Success, Is.True);
            Assert.That(characterEmpty.Length, Is.EqualTo(0));

            Assert.That(utf8Result.Success, Is.True);
            Assert.That(Encoding.UTF8.GetString(utf8.AsSpan(utf8Result.StartOffset, utf8Result.Length)), Is.EqualTo("  complete value"));
            Assert.That(utf8Result.NextOffset, Is.EqualTo(utf8.Length));
            Assert.That(utf8Empty.Success, Is.True);
            Assert.That(utf8Empty.Length, Is.EqualTo(0));
        });
    }

    [Test]
    public void TokenReaders_RejectInvalidOffsetsAndLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpTokenReader.SkipCharacterSpaces("x".AsSpan(), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpTokenReader.SkipUtf8Spaces(new byte[1], 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpTokenReader.ReadCharacterNonWhitespaceToken("x".AsSpan(), 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpTokenReader.ReadUtf8DecimalToken(new byte[1], -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpTokenReader.CaptureCharacterRemainder("x".AsSpan(), 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpTokenReader.CaptureUtf8Remainder(new byte[1], -1));
        });
    }
}
