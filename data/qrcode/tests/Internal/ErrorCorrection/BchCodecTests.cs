using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BchCodecTests
{
    [Test]
    public void CreatesKnownFormatAndVersionCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BchCodec.CreateCode(8, 5, 10, 0x537, 0x5412),
                Is.EqualTo(0x77C4));
            Assert.That(BchCodec.CreateCode(0, 5, 10, 0x537, 0x4445),
                Is.EqualTo(0x4445));
            Assert.That(BchCodec.CreateCode(7, 6, 12, 0x1F25, 0),
                Is.EqualTo(0x07C94));
            Assert.That(BchCodec.CreateCode(63, 6, 12, 0x1F25, 0x20A7B),
                Is.EqualTo(0x1F1FE));
        });
    }

    [Test]
    public void DifferenceCountUsesOnlyChangedBits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BchCodec.CountDifferences(0, 0, 15), Is.Zero);
            Assert.That(BchCodec.CountDifferences(0, 0x7FFF, 15), Is.EqualTo(15));
            Assert.That(BchCodec.CountDifferences(0x5412, 0x5412 ^ 0x421, 15),
                Is.EqualTo(3));
            Assert.That(BchCodec.CountDifferences(0x07C94, 0x28C69, 18),
                Is.EqualTo(BitOperationsReference(0x07C94 ^ 0x28C69)));
        });
    }

    [Test]
    public void NearestCorrectionAcceptsThreeChangesAndRejectsFour()
    {
        int[] codes = new int[32];
        int[] data = new int[32];
        for (int value = 0; value < 32; value++)
        {
            codes[value] = BchCodec.CreateCode(value, 5, 10, 0x537, 0x5412);
            data[value] = value;
        }

        int expectedData = 19;
        int code = codes[expectedData];
        BchCorrectionResult corrected = BchCodec.CorrectNearestCode(
            code ^ 0x421,
            15,
            codes,
            data,
            3);
        int uncorrectable = FindCodeBeyondLimit(codes, 15, 3);
        BchCorrectionResult failed = BchCodec.CorrectNearestCode(
            uncorrectable,
            15,
            codes,
            data,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(corrected.Success, Is.True);
            Assert.That(corrected.Data, Is.EqualTo(expectedData));
            Assert.That(corrected.Code, Is.EqualTo(code));
            Assert.That(corrected.Distance, Is.EqualTo(3));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Data, Is.EqualTo(-1));
            Assert.That(failed.Distance, Is.GreaterThan(3));
        });
    }

    [Test]
    public void EqualDistanceCandidatesAreRejectedAsAmbiguous()
    {
        BchCorrectionResult result = BchCodec.CorrectNearestCode(
            1,
            2,
            new int[] { 0, 3 },
            new int[] { 10, 20 },
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Distance, Is.EqualTo(1));
            Assert.That(result.Data, Is.EqualTo(-1));
        });
    }

    [Test]
    public void RejectsInvalidBchInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => BchCodec.CreateCode(-1, 5, 10, 0x537, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CreateCode(32, 5, 10, 0x537, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CreateCode(0, 0, 10, 0x537, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CreateCode(0, 5, 0, 0x537, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CreateCode(0, 20, 11, 0x537, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CountDifferences(-1, 0, 15),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CountDifferences(0, 0x8000, 15),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CorrectNearestCode(
                    0,
                    15,
                    null!,
                    new int[0],
                    3),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => BchCodec.CorrectNearestCode(
                    0,
                    15,
                    new int[] { 0 },
                    new int[0],
                    3),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static int FindCodeBeyondLimit(int[] validCodes, int bitCount, int limit)
    {
        int codeCount = 1 << bitCount;
        for (int candidate = 0; candidate < codeCount; candidate++)
        {
            int nearest = bitCount + 1;
            for (int index = 0; index < validCodes.Length; index++)
            {
                int distance = BitOperationsReference(candidate ^ validCodes[index]);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            if (nearest > limit)
            {
                return candidate;
            }
        }

        Assert.Fail("Expected at least one code beyond the correction radius.");
        return -1;
    }

    private static int BitOperationsReference(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }

        return count;
    }
}
