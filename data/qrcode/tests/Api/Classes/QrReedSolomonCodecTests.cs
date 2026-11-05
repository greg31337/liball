using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrReedSolomonCodecTests
{
    [Test]
    public void CreatesKnownQrParityThroughThePublicContract()
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        byte[] data = new byte[]
        {
            16, 32, 12, 86, 97, 128, 236, 17, 236, 17,
            236, 17, 236, 17, 236, 17, 236, 17, 236
        };
        byte[] original = (byte[])data.Clone();

        byte[] parity = codec.CreateErrorCorrection(data, 7);

        Assert.Multiple(() =>
        {
            Assert.That(parity, Is.EqualTo(new byte[] { 83, 85, 151, 103, 16, 5, 132 }));
            Assert.That(data, Is.EqualTo(original));
        });
    }

    [Test]
    public void CorrectsMixedErrorsAndErasuresThroughThePublicContract()
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        byte[] data = CreateData(31);
        byte[] parity = codec.CreateErrorCorrection(data, 10);
        byte[] original = Join(data, parity);
        byte[] damaged = (byte[])original.Clone();
        int[] erasures = new int[] { 0, damaged.Length - 1 };
        int[] errorPositions = new int[] { 5, 10, 15, 20 };
        damaged[erasures[0]] ^= 29;
        damaged[erasures[1]] ^= 71;
        for (int index = 0; index < errorPositions.Length; index++)
        {
            damaged[errorPositions[index]] ^= (byte)(index * 41 + 17);
        }

        byte[] supplied = (byte[])damaged.Clone();
        int[] suppliedErasures = (int[])erasures.Clone();
        QrCorrectionResult result = codec.CorrectCodewords(
            supplied,
            10,
            suppliedErasures);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.None));
            Assert.That(result.Codewords, Is.EqualTo(original));
            Assert.That(result.Codewords, Is.Not.SameAs(supplied));
            Assert.That(result.CorrectedErrorCount, Is.EqualTo(4));
            Assert.That(result.CorrectedErasureCount, Is.EqualTo(2));
            Assert.That(result.Syndromes, Is.Not.All.Zero);
            Assert.That(result.Verified, Is.True);
            Assert.That(supplied, Is.EqualTo(damaged));
            Assert.That(suppliedErasures, Is.EqualTo(erasures));
        });
    }

    [Test]
    public void ValidBlockReturnsAnIndependentVerifiedCopy()
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        byte[] data = CreateData(19);
        byte[] block = Join(data, codec.CreateErrorCorrection(data, 7));

        QrCorrectionResult result = codec.CorrectCodewords(block, 7, new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Codewords, Is.EqualTo(block));
            Assert.That(result.Codewords, Is.Not.SameAs(block));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(result.Syndromes, Is.All.Zero);
            Assert.That(result.Verified, Is.True);
        });
    }

    [Test]
    public void UncorrectableBlockReturnsChecksumFailureWithoutChangingInput()
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        byte[] data = CreateData(29);
        byte[] block = Join(data, codec.CreateErrorCorrection(data, 7));
        int[] positions = new int[] { 1, 8, 17, 31 };
        for (int index = 0; index < positions.Length; index++)
        {
            block[positions[index]] ^= (byte)(index * 53 + 19);
        }

        byte[] supplied = (byte[])block.Clone();
        QrCorrectionResult result = codec.CorrectCodewords(supplied, 7, new int[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(result.Codewords, Is.EqualTo(block));
            Assert.That(result.Codewords, Is.Not.SameAs(supplied));
            Assert.That(result.Verified, Is.False);
            Assert.That(supplied, Is.EqualTo(block));
        });
    }

    [Test]
    public void ReusableInstancesDoNotShareMutableResults()
    {
        IQrReedSolomonCodec first = new QrReedSolomonCodec();
        IQrReedSolomonCodec second = new QrReedSolomonCodec();
        byte[] data = CreateData(23);

        byte[] firstParity = first.CreateErrorCorrection(data, 10);
        byte[] secondParity = second.CreateErrorCorrection(data, 10);
        firstParity[0] ^= 255;

        Assert.Multiple(() =>
        {
            Assert.That(secondParity, Is.EqualTo(first.CreateErrorCorrection(data, 10)));
            Assert.That(secondParity, Is.Not.SameAs(firstParity));
        });
    }

    [Test]
    public void InvalidPublicInputsUseTypedLibraryExceptions()
    {
        IQrReedSolomonCodec codec = new QrReedSolomonCodec();
        Assert.Multiple(() =>
        {
            Assert.That(() => codec.CreateErrorCorrection(null!, 7),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => codec.CreateErrorCorrection(new byte[] { 1 }, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => codec.CreateErrorCorrection(new byte[250], 6),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => codec.CorrectCodewords(null!, 7, new int[0]),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => codec.CorrectCodewords(new byte[7], 0, new int[0]),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => codec.CorrectCodewords(new byte[7], 7, null!),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => codec.CorrectCodewords(new byte[7], 7, new int[] { 1, 1 }),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => codec.CorrectCodewords(new byte[6], 7, new int[0]),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static byte[] CreateData(int length)
    {
        byte[] data = new byte[length];
        for (int index = 0; index < data.Length; index++)
        {
            data[index] = (byte)((index * 149 + length * 11 + 37) & 255);
        }

        return data;
    }

    private static byte[] Join(byte[] data, byte[] parity)
    {
        byte[] block = new byte[data.Length + parity.Length];
        Array.Copy(data, block, data.Length);
        Array.Copy(parity, 0, block, data.Length, parity.Length);
        return block;
    }
}
