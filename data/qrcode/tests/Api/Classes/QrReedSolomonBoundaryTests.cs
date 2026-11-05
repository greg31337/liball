using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrReedSolomonBoundaryTests
{
    [Test]
    public void IndependentReferenceMatchesFixedParityVectors()
    {
        byte[] data = [16, 32, 12, 86, 97, 128, 236, 17, 236, 17, 236, 17, 236, 17, 236, 17, 236, 17, 236];
        Assert.That(Parity(data, 7), Is.EqualTo(new byte[] { 83, 85, 151, 103, 16, 5, 132 }));
        Assert.That(Parity([1, 2, 4, 8, 16, 32, 64, 128], 1), Is.EqualTo(new byte[] { 255 }));
        // All nonzero field elements except alpha^254 are roots of the degree-254
        // generator, giving successive coefficients divided by alpha (2).
        byte[] geometric = new byte[254];
        int previous = 173;
        for (int index = 0; index < geometric.Length; index++)
        {
            previous = (previous >> 1) ^ ((previous & 1) == 0 ? 0 : 0x8e);
            geometric[index] = (byte)previous;
        }
        Assert.That(Parity([173], 254), Is.EqualTo(geometric));
        // With degree 255 there are no data symbols and only the zero codeword remains.
        Assert.That(Parity([], 255), Is.All.Zero);
    }

    [Test]
    public void EveryEccLengthMatchesIndependentParityAndSyndromeVectors([Range(1, 255)] int ecc)
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        foreach (int length in new[] { 0, Math.Min(7, 255 - ecc), 255 - ecc }.Distinct())
        {
            byte[] data = Data(length);
            byte[] before = (byte[])data.Clone();
            byte[] expected = Parity(data, ecc);
            byte[] actual = codec.CreateErrorCorrection(data, ecc);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(data, Is.EqualTo(before));
            byte[] block = data.Concat(expected).ToArray();
            Assert.That(Syndromes(block, ecc), Is.All.Zero);
            AssertRepair(codec, block, block, ecc, [], 0);
            actual[0] ^= 1;
            Assert.That(codec.CreateErrorCorrection(data, ecc), Is.EqualTo(expected));
        }
    }

    [Test]
    public void EveryEccLengthRepairsItsEntireUnknownErrorBudget([Range(1, 255)] int ecc)
    {
        byte[] block = Block(255 - ecc, ecc);
        int count = ecc / 2;
        byte[] damaged = Damage(block, Positions(block.Length).Take(count).ToArray());
        AssertRepair(new QrReedSolomonCodec(), damaged, block, ecc, [], count);
    }

    [Test]
    public void EveryEccLengthRepairsItsEntireErasureBudgetInUnsortedOrder([Range(1, 255)] int ecc)
    {
        byte[] block = Block(Math.Min(17, 255 - ecc), ecc);
        int[] positions = Positions(block.Length).Take(ecc).ToArray();
        byte[] damaged = Damage(block, positions);
        Array.Reverse(positions);
        AssertRepair(new QrReedSolomonCodec(), damaged, block, ecc, positions, 0);
    }

    [Test]
    public void MixedDamageAtTheWeightedBoundaryRepairsDataAndParity([Range(3, 255)] int ecc)
    {
        byte[] block = Block(Math.Min(37, 255 - ecc), ecc);
        int[] positions = Positions(block.Length);
        foreach (int errors in new[] { 1, Math.Max(1, ecc / 4), (ecc - 1) / 2 }.Distinct())
        {
            int erasureCount = ecc - 2 * errors;
            int[] erasures = positions.Take(erasureCount).Reverse().ToArray();
            byte[] damaged = Damage(block, positions.Take(erasureCount + errors).ToArray());
            AssertRepair(new QrReedSolomonCodec(), damaged, block, ecc, erasures, errors);
        }
    }

    [Test]
    public void EveryMixedSplitForQrSizedParityBudgetsIsRecoverable([Range(2, 30)] int ecc)
    {
        byte[] block = Block(47, ecc);
        int[] positions = Positions(block.Length);
        for (int errors = 0; errors <= ecc / 2; errors++)
        {
            int erasureCount = ecc - 2 * errors;
            int[] erasures = positions.Take(erasureCount).Reverse().ToArray();
            byte[] damaged = Damage(block, positions.Take(erasureCount + errors).ToArray());
            AssertRepair(new QrReedSolomonCodec(), damaged, block, ecc, erasures, errors);
        }
    }

    [Test]
    public void TooManyErasureHintsFailEvenWhenTheBlockHasNoDamage([Range(1, 254)] int ecc)
    {
        byte[] block = Block(255 - ecc, ecc);
        int[] erasures = Positions(block.Length).Take(ecc + 1).ToArray();
        AssertFailure(block, ecc, erasures);
        AssertFailure(Damage(block, erasures), ecc, erasures);
    }

    [Test]
    public void ConsecutiveErrorsBeyondTheUnknownErrorBudgetAreRejected([Range(1, 255)] int ecc)
    {
        byte[] block = Block(255 - ecc, ecc);
        byte[] damaged = (byte[])block.Clone();
        for (int index = 0; index <= ecc / 2; index++) damaged[damaged.Length - 1 - index] ^= 1;
        AssertFailure(damaged, ecc, []);
    }

    [TestCase(7, 2, 4)]
    [TestCase(10, 3, 5)]
    [TestCase(30, 7, 17)]
    public void MixedDamageOneBeyondTheWeightedBudgetDoesNotPublishPartialRepairs(int ecc, int errors, int erasures)
    {
        byte[] block = Block(63, ecc);
        int[] positions = Positions(block.Length);
        AssertFailure(Damage(block, positions.Take(errors + erasures).ToArray()), ecc, positions.Take(erasures).ToArray());
    }

    [TestCase(7)]
    [TestCase(10)]
    [TestCase(30)]
    [TestCase(254)]
    public void CorrectErasureHintsCanHaveZeroMagnitudeDuringAMixedRepair(int ecc)
    {
        byte[] block = Block(255 - ecc, ecc);
        int[] positions = Positions(block.Length);
        int[] erasures = positions.Take(ecc - 2).Reverse().ToArray();
        // Only two of the hinted positions are damaged; the final damage is an unknown error.
        byte[] damaged = Damage(block, [positions[0], positions[1], positions[ecc - 2]]);
        AssertRepair(new QrReedSolomonCodec(), damaged, block, ecc, erasures, 1);
    }

    [Test]
    public void EveryPositionAndMagnitudeCanBeRepairedInAShortenedBlock()
    {
        const int ecc = 7;
        byte[] block = Block(4, ecc);
        for (int position = 0; position < block.Length; position++)
            for (int magnitude = 1; magnitude <= 255; magnitude++)
            {
                byte[] damaged = (byte[])block.Clone();
                damaged[position] ^= (byte)magnitude;
                AssertRepair(new QrReedSolomonCodec(), damaged, block, ecc, [], 1);
            }
    }

    [Test]
    public void VerificationProvesCodewordValidityButCannotRecoverIntentBeyondTheBound()
    {
        // With two parity symbols, both 00 00 00 and 01 03 02 are valid blocks.
        // This input is two errors from the former and one from the latter.
        byte[] received = [1, 3, 0];
        QrCorrectionResult result = new QrReedSolomonCodec().CorrectCodewords(received, 2, []);
        Assert.That(result.Success && result.Verified, Is.True);
        Assert.That(result.Codewords, Is.EqualTo(new byte[] { 1, 3, 2 }));
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(1));
        Assert.That(result.Syndromes, Is.EqualTo(new int[] { 2, 2 }));
        Assert.That(Syndromes(result.Codewords, 2), Is.All.Zero);
        Assert.That(received, Is.EqualTo(new byte[] { 1, 3, 0 }));
    }

    static void AssertRepair(IQrReedSolomonCodec codec, byte[] received, byte[] expected, int ecc, int[] erasures, int errors)
    {
        byte[] before = (byte[])received.Clone();
        int[] hintsBefore = (int[])erasures.Clone();
        int[] expectedSyndromes = Syndromes(received, ecc);
        QrCorrectionResult result = codec.CorrectCodewords(received, ecc, erasures);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Verified, Is.True);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(result.Codewords, Is.EqualTo(expected));
        Assert.That(result.Codewords, Is.Not.SameAs(received));
        Assert.That(result.ErrorCorrectionCodewordCount, Is.EqualTo(ecc));
        Assert.That(result.CorrectedErrorCount, Is.EqualTo(errors));
        Assert.That(result.CorrectedErasureCount, Is.EqualTo(erasures.Length));
        Assert.That(result.Syndromes, Is.EqualTo(expectedSyndromes));
        Assert.That(Syndromes(result.Codewords, ecc), Is.All.Zero);
        Assert.That(received, Is.EqualTo(before));
        Assert.That(erasures, Is.EqualTo(hintsBefore));
        // A second repair must recognize the verified output as clean and return another copy.
        QrCorrectionResult clean = codec.CorrectCodewords(result.Codewords, ecc, []);
        Assert.That(clean.Success && clean.Verified, Is.True);
        Assert.That(clean.CorrectedErrorCount + clean.CorrectedErasureCount, Is.Zero);
        Assert.That(clean.Syndromes, Is.All.Zero);
        Assert.That(clean.Codewords, Is.Not.SameAs(result.Codewords));
    }

    static void AssertFailure(byte[] received, int ecc, int[] erasures)
    {
        byte[] before = (byte[])received.Clone();
        int[] hintsBefore = (int[])erasures.Clone();
        QrCorrectionResult result = new QrReedSolomonCodec().CorrectCodewords(received, ecc, erasures);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Verified, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(result.Codewords, Is.EqualTo(before));
        Assert.That(result.Codewords, Is.Not.SameAs(received));
        Assert.That(result.CorrectedErrorCount + result.CorrectedErasureCount, Is.Zero);
        Assert.That(result.ErrorCorrectionCodewordCount, Is.EqualTo(ecc));
        Assert.That(result.Syndromes, Is.EqualTo(Syndromes(received, ecc)));
        Assert.That(received, Is.EqualTo(before));
        Assert.That(erasures, Is.EqualTo(hintsBefore));
    }

    static byte[] Data(int length) => Enumerable.Range(0, length).Select(i => (byte)((i * 149 + length * 11 + 37) & 255)).ToArray();

    static byte[] Block(int dataLength, int ecc)
    {
        byte[] data = Data(dataLength);
        return data.Concat(Parity(data, ecc)).ToArray();
    }

    static int[] Positions(int length) => Enumerable.Range(0, length).Select(i => i % 2 == 0 ? i / 2 : length - 1 - i / 2).ToArray();

    static byte[] Damage(byte[] block, int[] positions)
    {
        byte[] damaged = (byte[])block.Clone();
        for (int index = 0; index < positions.Length; index++) damaged[positions[index]] ^= (byte)((index * 73 + 17) % 255 + 1);
        return damaged;
    }

    // Independent carryless polynomial arithmetic: no production field tables, generators,
    // encoder, or decoder are used to create the expected blocks and syndrome vectors.
    static int Multiply(int left, int right)
    {
        int product = 0;
        for (int bit = 0; bit < 8; bit++) if ((right & (1 << bit)) != 0) product ^= left << bit;
        for (int bit = 14; bit >= 8; bit--) if ((product & (1 << bit)) != 0) product ^= 0x11d << (bit - 8);
        return product;
    }

    static byte[] Parity(byte[] data, int ecc)
    {
        int[] generator = [1];
        int root = 1;
        for (int degree = 0; degree < ecc; degree++)
        {
            int[] next = new int[generator.Length + 1];
            for (int index = 0; index < generator.Length; index++)
            {
                next[index] ^= generator[index];
                next[index + 1] ^= Multiply(generator[index], root);
            }
            generator = next;
            root = Multiply(root, 2);
        }
        int[] dividend = new int[data.Length + ecc];
        for (int index = 0; index < data.Length; index++) dividend[index] = data[index];
        for (int index = 0; index < data.Length; index++)
        {
            int factor = dividend[index];
            for (int term = 0; term < generator.Length; term++) dividend[index + term] ^= Multiply(factor, generator[term]);
        }
        return dividend.Skip(data.Length).Select(value => (byte)value).ToArray();
    }

    static int[] Syndromes(byte[] block, int ecc)
    {
        int[] result = new int[ecc];
        int root = 1;
        for (int index = 0; index < ecc; index++)
        {
            foreach (byte value in block) result[index] = Multiply(result[index], root) ^ value;
            root = Multiply(root, 2);
        }
        return result;
    }
}
