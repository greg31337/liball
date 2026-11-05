namespace barcode.Tests;

public class Pdf417NumericCompactionTests
{
    [Test]
    public void PublishedFifteenDigitReferenceVectorPreservesLeadingZeros()
    {
        const string digits = "000213298174000";

        Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(digits);
        Pdf417NumericDecodingResult decoded = Pdf417NumericCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.DataCodewords, Is.EqualTo(new int[] { 1, 624, 434, 632, 282, 200 }));
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 902, 1, 624, 434, 632, 282, 200 }));
            Assert.That(encoded.SegmentDigitStarts, Is.EqualTo(new int[] { 0 }));
            Assert.That(encoded.SegmentDigitCounts, Is.EqualTo(new int[] { 15 }));
            Assert.That(encoded.SegmentCodewordStarts, Is.EqualTo(new int[] { 0 }));
            Assert.That(encoded.SegmentCodewordCounts, Is.EqualTo(new int[] { 6 }));
            Assert.That(decoded.Digits, Is.EqualTo(digits));
            Assert.That(decoded.DataCodewords, Is.EqualTo(encoded.DataCodewords));
            Assert.That(decoded.ConsumedCodewordCount, Is.EqualTo(7));
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(43)]
    [TestCase(44)]
    [TestCase(45)]
    [TestCase(87)]
    [TestCase(88)]
    [TestCase(89)]
    public void EveryFortyFourDigitBoundaryRoundTrips(int length)
    {
        string digits = CreateDigits(length);

        Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(digits);
        Pdf417NumericDecodingResult decoded = Pdf417NumericCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Digits, Is.EqualTo(digits));
            Assert.That(encoded.SegmentDigitCounts.Sum(), Is.EqualTo(length));
            Assert.That(encoded.SegmentDigitCounts, Has.All.LessThanOrEqualTo(44));
            Assert.That(encoded.SegmentCodewordCounts, Has.All.LessThanOrEqualTo(15));
            Assert.That(decoded.SegmentDigitCounts, Is.EqualTo(encoded.SegmentDigitCounts));
            Assert.That(decoded.SegmentCodewordCounts, Is.EqualTo(encoded.SegmentCodewordCounts));
        });
    }

    [Test]
    public void FortyFiveDigitsCreateOneFullSegmentAndOneExactRemainderSegment()
    {
        string digits = new string('0', 44) + "5";

        Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(digits);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.SegmentDigitStarts, Is.EqualTo(new int[] { 0, 44 }));
            Assert.That(encoded.SegmentDigitCounts, Is.EqualTo(new int[] { 44, 1 }));
            Assert.That(encoded.SegmentCodewordStarts, Is.EqualTo(new int[] { 0, 15 }));
            Assert.That(encoded.SegmentCodewordCounts, Is.EqualTo(new int[] { 15, 1 }));
            Assert.That(encoded.DataCodewords, Has.Length.EqualTo(16));
            Assert.That(encoded.DataCodewords[^1], Is.EqualTo(15));
            Assert.That(Pdf417NumericCompaction.DecodeDataCodewords(encoded.DataCodewords).Digits, Is.EqualTo(digits));
        });
    }

    [Test]
    public void LongInputUsesArbitraryPrecisionAndPreservesEveryLeadingZero()
    {
        string digits = new string('0', 137) + CreateDigits(1000);

        Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(digits);
        Pdf417NumericDecodingResult decoded = Pdf417NumericCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Digits, Is.EqualTo(digits));
            Assert.That(encoded.SegmentDigitCounts, Has.Length.EqualTo(26));
            Assert.That(encoded.SegmentDigitCounts.Take(25), Has.All.EqualTo(44));
            Assert.That(encoded.SegmentDigitCounts[^1], Is.EqualTo(37));
            Assert.That(encoded.DataCodewords, Has.Length.LessThanOrEqualTo(26 * 15));
        });
    }

    [Test]
    public void SingleDigitsMapThroughTheSentinelWithoutLosingZero()
    {
        Pdf417NumericEncodingResult zero = Pdf417NumericCompaction.EncodeDigits("0");
        Pdf417NumericEncodingResult nine = Pdf417NumericCompaction.EncodeDigits("9");

        Assert.Multiple(() =>
        {
            Assert.That(zero.Codewords, Is.EqualTo(new int[] { 902, 10 }));
            Assert.That(nine.Codewords, Is.EqualTo(new int[] { 902, 19 }));
            Assert.That(Pdf417NumericCompaction.DecodeCodewords(zero.Codewords).Digits, Is.EqualTo("0"));
            Assert.That(Pdf417NumericCompaction.DecodeCodewords(nine.Codewords).Digits, Is.EqualTo("9"));
        });
    }

    [Test]
    public void ResultsOwnCodewordAndSegmentMetadataArrays()
    {
        Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(
            CreateDigits(45));
        Pdf417NumericDecodingResult decoded = Pdf417NumericCompaction.DecodeDataCodewords(
            encoded.DataCodewords);
        int expectedCodeword = encoded.Codewords[1];
        int expectedDigitCount = decoded.SegmentDigitCounts[0];
        encoded.DataCodewords[0] = 0;
        decoded.DataCodewords[0] = 0;
        encoded.SegmentDigitCounts[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Codewords[1], Is.EqualTo(expectedCodeword));
            Assert.That(decoded.Digits, Is.EqualTo(CreateDigits(45)));
            Assert.That(decoded.SegmentDigitCounts[0], Is.EqualTo(expectedDigitCount));
        });
    }

    [Test]
    public void NonCanonicalNonterminalSegmentIsRejectedInsteadOfChangingBoundaries()
    {
        Pdf417NumericEncodingResult shortSegment = Pdf417NumericCompaction.EncodeDigits(
            new string('9', 43));
        Pdf417NumericEncodingResult finalSegment = Pdf417NumericCompaction.EncodeDigits("1");
        Assert.That(shortSegment.DataCodewords, Has.Length.EqualTo(15));
        int[] combined = new int[16];
        global::System.Array.Copy(shortSegment.DataCodewords, combined, 15);
        combined[15] = finalSegment.DataCodewords[0];

        Assert.That(
            () => Pdf417NumericCompaction.DecodeDataCodewords(combined),
            Throws.TypeOf<BarcodeDecodingException>());
    }

    [Test]
    public void InvalidDigitsLatchesCodewordsAndSentinelsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Pdf417NumericCompaction.EncodeDigits(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417NumericCompaction.EncodeDigits(""), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417NumericCompaction.EncodeDigits("12A3"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417NumericCompaction.EncodeDigits("12 3"), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeCodewords(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeCodewords(new int[0]), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeCodewords(new int[] { 902 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeCodewords(new int[] { 900, 10 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeDataCodewords(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeDataCodewords(new int[0]), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeDataCodewords(new int[] { -1 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeDataCodewords(new int[] { 900 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeDataCodewords(new int[] { 0 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417NumericCompaction.DecodeDataCodewords(new int[] { 1 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417NumericCompaction.DecodeDataCodewords(new int[]
                {
                    899, 899, 899, 899, 899, 899, 899, 899,
                    899, 899, 899, 899, 899, 899, 899
                }),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    static string CreateDigits(int length)
    {
        char[] digits = new char[length];
        int index = 0;
        while (index < length)
        {
            digits[index] = (char)('0' + ((index * 7) % 10));
            index++;
        }

        return new string(digits);
    }
}
