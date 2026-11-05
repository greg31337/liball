namespace uri.Tests;

public class UnicodeScalarTests
{
    [TestCase("\u0000", 0, 0, 1)]
    [TestCase("A", 0, 65, 1)]
    [TestCase("\ud7ff", 0, 55295, 1)]
    [TestCase("\ue000", 0, 57344, 1)]
    [TestCase("\uffff", 0, 65535, 1)]
    [TestCase("\ud800\udc00", 0, 65536, 2)]
    [TestCase("\ud83d\ude00", 0, 128512, 2)]
    [TestCase("\udbff\udfff", 0, 1114111, 2)]
    [TestCase("x\ud83d\ude00", 1, 128512, 2)]
    public void ReadAtReturnsUnicodeScalarAndConsumedCodeUnits(
        string text,
        int offset,
        int expectedScalar,
        int expectedLength)
    {
        UnicodeScalarResult result = UnicodeScalar.ReadAt(text.AsSpan(), offset);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ScalarValue, Is.EqualTo(expectedScalar));
            Assert.That(result.CodeUnitLength, Is.EqualTo(expectedLength));
            Assert.That(result.ErrorOffset, Is.Zero);
            Assert.That(result.ErrorLength, Is.Zero);
        });
    }

    [Test]
    public void ReadAtRejectsMalformedUtf16CodeUnits()
    {
        string isolatedHigh = new string(new char[] { (char)55296 });
        string highBeforeAscii = new string(new char[] { (char)55296, 'A' });
        string isolatedLow = new string(new char[] { (char)57343 });
        string lowAfterAscii = new string(new char[] { 'A', (char)57343 });

        AssertMalformed(isolatedHigh, 0, 0);
        AssertMalformed(highBeforeAscii, 0, 0);
        AssertMalformed(isolatedLow, 0, 0);
        AssertMalformed(lowAfterAscii, 1, 1);
    }

    [TestCase("", 0, 0, 0)]
    [TestCase("A", 1, 1, 0)]
    [TestCase("A", 2, 1, 0)]
    [TestCase("A", -1, 0, 0)]
    public void ReadAtReportsUnavailableScalar(
        string text,
        int offset,
        int expectedErrorOffset,
        int expectedErrorLength)
    {
        UnicodeScalarResult result = UnicodeScalar.ReadAt(text.AsSpan(), offset);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ScalarValue, Is.Zero);
            Assert.That(result.CodeUnitLength, Is.Zero);
            Assert.That(result.ErrorOffset, Is.EqualTo(expectedErrorOffset));
            Assert.That(result.ErrorLength, Is.EqualTo(expectedErrorLength));
        });
    }

    private static void AssertMalformed(string text, int offset, int expectedErrorOffset)
    {
        UnicodeScalarResult result = UnicodeScalar.ReadAt(text.AsSpan(), offset);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ScalarValue, Is.Zero);
            Assert.That(result.CodeUnitLength, Is.Zero);
            Assert.That(result.ErrorOffset, Is.EqualTo(expectedErrorOffset));
            Assert.That(result.ErrorLength, Is.EqualTo(1));
        });
    }

    [TestCase(-1, false, 0)]
    [TestCase(0, true, 1)]
    [TestCase(55295, true, 1)]
    [TestCase(55296, false, 0)]
    [TestCase(57343, false, 0)]
    [TestCase(57344, true, 1)]
    [TestCase(65535, true, 1)]
    [TestCase(65536, true, 2)]
    [TestCase(1114111, true, 2)]
    [TestCase(1114112, false, 0)]
    public void ScalarValidationCoversUnicodeBoundaries(int scalar, bool valid, int codeUnitLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(UnicodeScalar.IsValid(scalar), Is.EqualTo(valid));
            Assert.That(UnicodeScalar.GetCodeUnitLength(scalar), Is.EqualTo(codeUnitLength));
        });
    }

    [Test]
    public void SequentialReadsPreserveUtf16OffsetsAcrossBmpAndSupplementaryScalars()
    {
        string text = "A\ud83d\ude00\ue000";
        int offset = 0;

        UnicodeScalarResult ascii = UnicodeScalar.ReadAt(text.AsSpan(), offset);
        offset = offset + ascii.CodeUnitLength;
        UnicodeScalarResult emoji = UnicodeScalar.ReadAt(text.AsSpan(), offset);
        offset = offset + emoji.CodeUnitLength;
        UnicodeScalarResult bmp = UnicodeScalar.ReadAt(text.AsSpan(), offset);
        offset = offset + bmp.CodeUnitLength;

        Assert.Multiple(() =>
        {
            Assert.That(ascii.ScalarValue, Is.EqualTo(65));
            Assert.That(emoji.ScalarValue, Is.EqualTo(128512));
            Assert.That(bmp.ScalarValue, Is.EqualTo(57344));
            Assert.That(offset, Is.EqualTo(text.Length));
        });
    }
}
