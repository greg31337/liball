namespace ttf.Tests;

internal class LegacyEncodingDataTests
{
    [TestCase(932, new byte[] { 0x82, 0xA0 }, "\u3042")]
    [TestCase(936, new byte[] { 0xD6, 0xD0 }, "\u4E2D")]
    [TestCase(949, new byte[] { 0xB0, 0xA1 }, "\uAC00")]
    [TestCase(950, new byte[] { 0xA4, 0x40 }, "\u4E00")]
    [TestCase(10006, new byte[] { 0xB0 }, "\u0391")]
    [TestCase(10007, new byte[] { 0x80 }, "\u0410")]
    [TestCase(10021, new byte[] { 0xA1 }, "\u0E01")]
    public void DecodesPinnedSingleAndDoubleByteMappings(
        int codePage,
        byte[] bytes,
        string expected)
    {
        LegacyDecodedText? decoded = Decode(codePage, bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decoded!.GetValue(), Is.EqualTo(expected));
            Assert.That(decoded.GetReplacementCount(), Is.Zero);
        });
    }

    [Test]
    public void ProvidesDeterministicForwardAndReverseMappingIndexes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LegacyEncodingData.DecodeEncodedValue(936, 0xD6D0),
                Is.EqualTo(0x4E2D));
            Assert.That(LegacyEncodingData.EncodeUnicodeValue(936, 0x4E2D),
                Is.EqualTo(0xD6D0));
            Assert.That(LegacyEncodingData.EncodeUnicodeValue(932, 0x5C),
                Is.EqualTo(0x5C));
            Assert.That(LegacyEncodingData.GetEncodedValueCount(932, 0x5C),
                Is.GreaterThanOrEqualTo(1));
            Assert.That(LegacyEncodingData.GetUnicodeValueCount(936),
                Is.GreaterThan(20000));
            Assert.That(LegacyEncodingData.DecodeEncodedValue(12345, 0x41),
                Is.EqualTo(-1));
            Assert.That(LegacyEncodingData.EncodeUnicodeValue(12345, 0x41),
                Is.EqualTo(-1));
        });
    }

    [Test]
    public void SupportsEveryCheckedInCodePageAndReplacesInvalidInput()
    {
        int[] codePages = new int[]
        {
            932, 936, 949, 950,
            10000, 10001, 10002, 10003, 10004, 10005, 10006, 10007,
            10008, 10010, 10021, 10029, 10079, 10081, 10082
        };

        for (int index = 0; index < codePages.Length; index += 1)
        {
            Assert.That(LegacyEncodingData.IsCodePageSupported(codePages[index]),
                Is.True);
        }

        LegacyDecodedText? invalid = Decode(936, new byte[] { 0x81 });

        Assert.Multiple(() =>
        {
            Assert.That(invalid!.GetValue(), Is.EqualTo("\uFFFD"));
            Assert.That(invalid.GetReplacementCount(), Is.EqualTo(1));
            Assert.That(LegacyEncodingData.IsCodePageSupported(1361), Is.False);
        });
    }

    static LegacyDecodedText? Decode(int codePage, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return LegacyEncodingData.Decode(
            data.GetSlice(0, data.GetLength()),
            codePage);
    }
}
