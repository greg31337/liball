namespace barcode.Tests;

public class Code128Modulo103Tests
{
    [TestCase(new int[] { 104, 48, 42, 42, 17, 18, 19, 35 }, 55)]
    [TestCase(new int[] { 105, 12, 34 }, 82)]
    [TestCase(new int[] { 103 }, 0)]
    [TestCase(new int[] { 104 }, 1)]
    [TestCase(new int[] { 105 }, 2)]
    public void CalculateCheckCodewordAppliesStartAndPositionWeights(int[] codewords, int expected)
    {
        Assert.That(Code128Modulo103.CalculateCheckCodeword(codewords), Is.EqualTo(expected));
    }

    [Test]
    public void CalculateCheckCodewordKeepsLongWeightedSumBounded()
    {
        int[] codewords = new int[100000];
        codewords[0] = 104;
        int index = 1;
        while (index < codewords.Length)
        {
            codewords[index] = 102;
            index = index + 1;
        }

        Assert.That(Code128Modulo103.CalculateCheckCodeword(codewords), Is.EqualTo(13));
    }

    [Test]
    public void AppendAndVerifyCodewordPreserveCallerArray()
    {
        int[] source = new int[] { 104, 48, 42, 42, 17, 18, 19, 35 };

        int[] complete = Code128Modulo103.AppendCheckCodeword(source);
        complete[1] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(source, Is.EqualTo(new int[] { 104, 48, 42, 42, 17, 18, 19, 35 }));
            Assert.That(Code128Modulo103.AppendCheckCodeword(source), Is.EqualTo(new int[] { 104, 48, 42, 42, 17, 18, 19, 35, 55 }));
            Assert.That(Code128Modulo103.VerifyCompleteCodewords(new int[] { 104, 48, 42, 42, 17, 18, 19, 35, 55 }), Is.True);
            Assert.That(Code128Modulo103.VerifyCompleteCodewords(new int[] { 104, 48, 42, 42, 17, 18, 19, 35, 54 }), Is.False);
            Assert.That(Code128Modulo103.VerifyCheckCodeword(source, 55), Is.True);
            Assert.That(Code128Modulo103.VerifyCheckCodeword(source, 54), Is.False);
        });
    }

    [Test]
    public void Code128ChecksumRejectsMalformedSequencesAndValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Code128Modulo103.CalculateCheckCodeword(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodeword(new int[0]), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodeword(new int[] { 102, 1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodeword(new int[] { 106, 1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodeword(new int[] { 104, -1 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodeword(new int[] { 104, 103 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.VerifyCompleteCodewords(null!), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.VerifyCompleteCodewords(new int[] { 104 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.VerifyCompleteCodewords(new int[] { 104, 103 }), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.VerifyCheckCodeword(new int[] { 104 }, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodewordForPrefix(null!, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128Modulo103.CalculateCheckCodewordForPrefix(new int[] { 104 }, 0), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void AlgorithmNameIsStable()
    {
        Assert.That(Code128Modulo103.AlgorithmName, Is.EqualTo("Code 128 modulo-103"));
    }
}
