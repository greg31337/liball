using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ReedSolomonEncoderTests
{
    [Test]
    public void GeneratorCoefficientsMatchKnownQrPolynomials()
    {
        int[] degreeSeven = ReedSolomonGenerator.CreateCoefficients(7);
        int[] degreeTen = ReedSolomonGenerator.CreateCoefficients(10);

        Assert.Multiple(() =>
        {
            Assert.That(degreeSeven, Is.EqualTo(new int[]
            {
                1, 127, 122, 154, 164, 11, 68, 117
            }));
            Assert.That(degreeTen, Is.EqualTo(new int[]
            {
                1, 216, 194, 159, 111, 199, 94, 95, 113, 157, 193
            }));
        });
    }

    [Test]
    public void EveryStandardizedGeneratorHasTheRequiredRoots()
    {
        bool[] standardizedLengths = GetStandardizedLengths();
        int lengthCount = 0;
        for (int length = 1; length < standardizedLengths.Length; length++)
        {
            if (!standardizedLengths[length])
            {
                continue;
            }

            lengthCount++;
            int[] coefficients = ReedSolomonGenerator.CreateCoefficients(length);
            Assert.That(coefficients.Length, Is.EqualTo(length + 1));
            Assert.That(coefficients[0], Is.EqualTo(1));
            for (int rootIndex = 0; rootIndex < length; rootIndex++)
            {
                int root = GaloisField256.GetExponent(rootIndex);
                Assert.That(
                    GaloisPolynomial.Evaluate(coefficients, root),
                    Is.Zero,
                    "degree " + length + ", root " + rootIndex);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(lengthCount, Is.EqualTo(38));
            Assert.That(standardizedLengths[2], Is.True);
            Assert.That(standardizedLengths[30], Is.True);
            Assert.That(standardizedLengths[66], Is.True);
        });
    }

    [Test]
    public void ParityMatchesIndependentPolynomialDivisionForEveryStandardizedLength()
    {
        byte[] data = new byte[37];
        for (int index = 0; index < data.Length; index++)
        {
            data[index] = (byte)((index * 149 + 37) & 255);
        }

        bool[] standardizedLengths = GetStandardizedLengths();
        for (int length = 1; length < standardizedLengths.Length; length++)
        {
            if (!standardizedLengths[length])
            {
                continue;
            }

            byte[] expected = CreateParityReference(data, length);
            byte[] actual = ReedSolomonEncoder.CreateParity(data, length);
            Assert.That(actual, Is.EqualTo(expected), "ECC length " + length);

            int[] codewordPolynomial = new int[data.Length + actual.Length];
            for (int index = 0; index < data.Length; index++)
            {
                codewordPolynomial[index] = data[index];
            }

            for (int index = 0; index < actual.Length; index++)
            {
                codewordPolynomial[data.Length + index] = actual[index];
            }

            for (int rootIndex = 0; rootIndex < length; rootIndex++)
            {
                Assert.That(
                    GaloisPolynomial.Evaluate(
                        codewordPolynomial,
                        GaloisField256.GetExponent(rootIndex)),
                    Is.Zero,
                    "ECC length " + length + ", root " + rootIndex);
            }
        }
    }

    [Test]
    public void CachedGeneratorsAndParityResultsAreDefensiveCopies()
    {
        int[] firstGenerator = ReedSolomonGenerator.CreateCoefficients(7);
        firstGenerator[1] = 0;
        int[] secondGenerator = ReedSolomonGenerator.CreateCoefficients(7);
        byte[] data = new byte[] { 32, 91, 11, 120, 209, 114, 220 };
        byte[] firstParity = ReedSolomonEncoder.CreateParity(data, 10);
        firstParity[0] = 0;
        byte[] secondParity = ReedSolomonEncoder.CreateParity(data, 10);

        Assert.Multiple(() =>
        {
            Assert.That(secondGenerator[1], Is.EqualTo(127));
            Assert.That(secondGenerator, Is.Not.SameAs(firstGenerator));
            Assert.That(secondParity, Is.EqualTo(CreateParityReference(data, 10)));
            Assert.That(secondParity, Is.Not.SameAs(firstParity));
            Assert.That(data, Is.EqualTo(new byte[] { 32, 91, 11, 120, 209, 114, 220 }));
        });
    }

    [Test]
    public void EmptyDataProducesZeroParityAndMaximumDegreeRemainsBounded()
    {
        byte[] parity = ReedSolomonEncoder.CreateParity(new byte[0], 255);
        int[] generator = ReedSolomonGenerator.CreateCoefficients(255);

        Assert.Multiple(() =>
        {
            Assert.That(parity.Length, Is.EqualTo(255));
            Assert.That(parity, Is.All.Zero);
            Assert.That(generator.Length, Is.EqualTo(256));
            Assert.That(generator[0], Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsInvalidDegreesAndOversizedBlocks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => ReedSolomonGenerator.CreateCoefficients(0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonGenerator.CreateCoefficients(256),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonEncoder.CreateParity(null!, 7),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonEncoder.CreateParity(new byte[] { 1 }, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonEncoder.CreateParity(new byte[] { 1 }, 256),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => ReedSolomonEncoder.CreateParity(new byte[250], 6),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    private static bool[] GetStandardizedLengths()
    {
        bool[] lengths = new bool[256];
        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 40; version++)
            {
                lengths[EccBlockTable.GetEccCodewordCountPerBlock(version, level)] = true;
            }
        }

        lengths[MicroVersionTable.GetEccCodewordCount(1, -1)] = true;
        for (int version = 2; version <= 3; version++)
        {
            for (int level = 0; level <= 1; level++)
            {
                lengths[MicroVersionTable.GetEccCodewordCount(version, level)] = true;
            }
        }

        for (int level = 0; level <= 2; level++)
        {
            lengths[MicroVersionTable.GetEccCodewordCount(4, level)] = true;
        }

        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                lengths[RectangularMicroVersionTable.GetEccCodewordCountPerBlock(version, level)] = true;
            }
        }

        for (int level = 0; level < 4; level++)
        {
            for (int version = 1; version <= 14; version++)
            {
                lengths[LegacyModel1Table.GetErrorCorrectionCodewordCountPerBlock(version, level)] = true;
            }
        }

        return lengths;
    }

    private static byte[] CreateParityReference(byte[] data, int degree)
    {
        int[] generator = CreateGeneratorReference(degree);
        int[] message = new int[data.Length + degree];
        for (int index = 0; index < data.Length; index++)
        {
            message[index] = data[index];
        }

        for (int index = 0; index < data.Length; index++)
        {
            int factor = message[index];
            for (int coefficient = 0; coefficient < generator.Length; coefficient++)
            {
                message[index + coefficient] ^=
                    MultiplyReference(generator[coefficient], factor);
            }
        }

        byte[] parity = new byte[degree];
        for (int index = 0; index < degree; index++)
        {
            parity[index] = (byte)message[data.Length + index];
        }

        return parity;
    }

    private static int[] CreateGeneratorReference(int degree)
    {
        int[] generator = new int[] { 1 };
        int root = 1;
        for (int factorIndex = 0; factorIndex < degree; factorIndex++)
        {
            int[] product = new int[generator.Length + 1];
            for (int index = 0; index < generator.Length; index++)
            {
                product[index] ^= generator[index];
                product[index + 1] ^= MultiplyReference(generator[index], root);
            }

            generator = product;
            root = MultiplyReference(root, 2);
        }

        return generator;
    }

    private static int MultiplyReference(int left, int right)
    {
        int product = 0;
        int multiplicand = left;
        int multiplier = right;
        while (multiplier != 0)
        {
            if ((multiplier & 1) != 0)
            {
                product ^= multiplicand;
            }

            multiplier >>= 1;
            multiplicand <<= 1;
            if ((multiplicand & 0x100) != 0)
            {
                multiplicand ^= 0x11D;
            }
        }

        return product;
    }
}
