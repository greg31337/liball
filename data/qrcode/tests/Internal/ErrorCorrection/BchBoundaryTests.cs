using System.Numerics;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BchBoundaryTests
{
    [TestCase("Model2", 32, 15)]
    [TestCase("Legacy", 32, 15)]
    [TestCase("Micro", 32, 15)]
    [TestCase("Rectangular", 64, 18)]
    [TestCase("Version", 34, 18)]
    public void EveryValidValueAndEveryCorrectableBitPatternMatchesIndependentCodes(string family, int count, int width)
    {
        int[] masks = DamageMasks(width).ToArray();
        Assert.That(masks.Length, Is.EqualTo(width == 15 ? 576 : 988));
        for (int value = 0; value < count; value++)
        {
            int first = Reference(family, value, 0);
            int second = Reference(family, value, 1);
            Assert.That(Create(family, value, 0), Is.EqualTo(first));
            Assert.That(Create(family, value, 1), Is.EqualTo(second));
            foreach (int damage in masks)
            {
                int distance = BitOperations.PopCount((uint)damage);
                BchCorrectionResult result = Correct(family, first ^ damage, second ^ damage);
                Assert.That(result.Success, Is.True, $"{family}, value {value}, damage {damage:X}");
                Assert.That(result.Data, Is.EqualTo(Data(family, value)));
                Assert.That(result.Code, Is.EqualTo(first));
                Assert.That(result.Distance, Is.EqualTo(distance));
                Assert.That(result.FirstDistance, Is.EqualTo(distance));
                Assert.That(result.SecondDistance, Is.EqualTo(distance));
                Assert.That(result.SourceCopy, Is.Zero);
                Assert.That(result.CopiesAgree, Is.True);
            }
        }
    }

    [TestCase("Model2", 32)]
    [TestCase("Legacy", 32)]
    [TestCase("Micro", 32)]
    [TestCase("Rectangular", 64)]
    [TestCase("Version", 34)]
    public void EveryPairOfRedundantValuesUsesDistanceAndRejectsEqualDistanceDisagreement(string family, int count)
    {
        for (int firstValue = 0; firstValue < count; firstValue++)
            for (int secondValue = 0; secondValue < count; secondValue++)
                for (int firstDistance = 0; firstDistance <= 3; firstDistance++)
                    for (int secondDistance = 0; secondDistance <= 3; secondDistance++)
                    {
                        int first = Reference(family, firstValue, 0);
                        int second = Reference(family, secondValue, 1);
                        BchCorrectionResult result = Correct(family, first ^ ((1 << firstDistance) - 1), second ^ ((1 << secondDistance) - 1));
                        Assert.That(result.FirstDistance, Is.EqualTo(firstDistance));
                        Assert.That(result.SecondDistance, Is.EqualTo(secondDistance));
                        Assert.That(result.Distance, Is.EqualTo(Math.Min(firstDistance, secondDistance)));
                        bool conflict = firstValue != secondValue && firstDistance == secondDistance;
                        if (conflict)
                        {
                            AssertFailure(result);
                            continue;
                        }
                        int selected = firstDistance <= secondDistance ? 0 : 1;
                        Assert.That(result.Success, Is.True);
                        Assert.That(result.SourceCopy, Is.EqualTo(selected));
                        Assert.That(result.Data, Is.EqualTo(Data(family, selected == 0 ? firstValue : secondValue)));
                        Assert.That(result.Code, Is.EqualTo(selected == 0 ? first : second));
                        Assert.That(result.CopiesAgree, Is.EqualTo(firstValue == secondValue));
                    }
    }

    [TestCase("Model2", 32, 15)]
    [TestCase("Legacy", 32, 15)]
    [TestCase("Micro", 32, 15)]
    [TestCase("Rectangular", 64, 18)]
    [TestCase("Version", 34, 18)]
    public void UncorrectableCopiesFailOrYieldToTheSurvivingCopy(string family, int count, int width)
    {
        int[] codes = Enumerable.Range(0, count).Select(value => Reference(family, value, 0)).ToArray();
        int bad = Enumerable.Range(0, 1 << width).First(word => NearestDistance(word, codes) > 3);
        int badDistance = NearestDistance(bad, codes);
        int copyMaskDifference = Reference(family, 0, 0) ^ Reference(family, 0, 1);
        BchCorrectionResult failed = Correct(family, bad, bad ^ copyMaskDifference);
        AssertFailure(failed);
        Assert.That(failed.Distance, Is.EqualTo(badDistance));
        Assert.That(failed.FirstDistance, Is.EqualTo(badDistance));
        Assert.That(failed.SecondDistance, Is.EqualTo(badDistance));
        for (int value = 0; value < count; value++)
            for (int distance = 0; distance <= 3; distance++)
                for (int surviving = 0; surviving < 2; surviving++)
                {
                    int first = surviving == 0 ? Reference(family, value, 0) ^ ((1 << distance) - 1) : bad;
                    int second = surviving == 1 ? Reference(family, value, 1) ^ ((1 << distance) - 1) : bad ^ copyMaskDifference;
                    BchCorrectionResult result = Correct(family, first, second);
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Data, Is.EqualTo(Data(family, value)));
                    Assert.That(result.Code, Is.EqualTo(Reference(family, value, surviving)));
                    Assert.That(result.SourceCopy, Is.EqualTo(surviving));
                    Assert.That(result.CopiesAgree, Is.False);
                    Assert.That(result.Distance, Is.EqualTo(distance));
                    Assert.That(result.FirstDistance, Is.EqualTo(surviving == 0 ? distance : badDistance));
                    Assert.That(result.SecondDistance, Is.EqualTo(surviving == 1 ? distance : badDistance));
                }
        Assert.Throws<QrInputException>(() => Correct(family, -1, 0));
        Assert.Throws<QrInputException>(() => Correct(family, 0, 1 << width));
    }

    [Test]
    public void GenericEncodingSupportsEveryPermittedBitLayout([Range(1, 29)] int dataBits)
    {
        for (int parityBits = 1; parityBits <= 30 - dataBits; parityBits++)
        {
            int generator = (1 << parityBits) | 1;
            int mask = (1 << (dataBits + parityBits)) - 1;
            foreach (int value in new[] { 0, 1, (1 << dataBits) - 1, ((1 << dataBits) - 1) / 3 })
                foreach (int xorMask in new[] { 0, mask })
                    Assert.That(BchCodec.CreateCode(value, dataBits, parityBits, generator, xorMask),
                        Is.EqualTo(Divide(value, dataBits, parityBits, generator, xorMask)),
                        $"data bits {dataBits}, parity bits {parityBits}, value {value}");
        }
    }

    [TestCase(int.MaxValue, 1)]
    [TestCase(1, int.MaxValue)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public void OversizedWidthsUseLibraryExceptionsWithoutArithmeticOverflow(int dataBits, int parityBits)
    {
        Assert.Throws<QrInputException>(() => BchCodec.CreateCode(0, dataBits, parityBits, 1, 0));
    }

    [TestCase(1)]
    [TestCase(0x237)]
    [TestCase(0x3ff)]
    public void RejectsGeneratorsMissingTheLeadingParityTerm(int generator)
    {
        Assert.Throws<QrInputException>(() => BchCodec.CreateCode(31, 5, 10, generator, 0));
    }

    static void AssertFailure(BchCorrectionResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Data, Is.EqualTo(-1));
        Assert.That(result.Code, Is.EqualTo(-1));
        Assert.That(result.SourceCopy, Is.EqualTo(-1));
        Assert.That(result.CopiesAgree, Is.False);
    }

    [TestCase("Model2", 32, 15)]
    [TestCase("Legacy", 32, 15)]
    [TestCase("Micro", 32, 15)]
    [TestCase("Rectangular", 64, 18)]
    [TestCase("Version", 34, 18)]
    public void FourBitDamageUsesTheNearestValidWordOnlyWithinTheCorrectionRadius(string family, int count, int width)
    {
        int[] codes = Enumerable.Range(0, count).Select(value => Reference(family, value, 0)).ToArray();
        for (int value = 0; value < count; value++)
            foreach (int damage in new[] { 0xf, 0x33, 0x55, 0xf << (width - 4) })
            {
                int received = codes[value] ^ damage;
                int[] distances = codes.Select(code => BitOperations.PopCount((uint)(code ^ received))).ToArray();
                int distance = distances.Min();
                BchCorrectionResult result = Correct(family, received, Reference(family, value, 1) ^ damage);
                Assert.That(result.Distance, Is.EqualTo(distance));
                if (distance > 3)
                    AssertFailure(result);
                else
                {
                    int expected = Array.IndexOf(distances, distance);
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Data, Is.EqualTo(Data(family, expected)));
                    Assert.That(result.Code, Is.EqualTo(codes[expected]));
                    Assert.That(result.Data, Is.Not.EqualTo(Data(family, value)));
                }
            }
    }

    [Test]
    public void ValidBchWordsForReservedVersionNumbersAreNotAccepted()
    {
        foreach (int version in Enumerable.Range(0, 64).Where(v => v < 7 || v > 40))
        {
            int reserved = Divide(version, 6, 12, 0x1f25, 0);
            BchCorrectionResult result = VersionInformationCodec.CorrectCopies(reserved, reserved);
            AssertFailure(result);
            Assert.That(result.Distance, Is.GreaterThan(3));
        }
    }

    static int NearestDistance(int word, int[] codes) => codes.Min(code => BitOperations.PopCount((uint)(word ^ code)));

    static IEnumerable<int> DamageMasks(int width)
    {
        yield return 0;
        for (int a = 0; a < width; a++)
        {
            yield return 1 << a;
            for (int b = a + 1; b < width; b++)
            {
                yield return (1 << a) | (1 << b);
                for (int c = b + 1; c < width; c++) yield return (1 << a) | (1 << b) | (1 << c);
            }
        }
    }

    static int Data(string family, int value) => family == "Version" ? value + 7 : value;

    static int Reference(string family, int value, int copy)
    {
        int raw = family is "Model2" or "Legacy" ? new[] { 1, 0, 3, 2 }[value / 8] * 8 + value % 8 : Data(family, value);
        return family switch
        {
            "Model2" => Divide(raw, 5, 10, 0x537, 0x5412),
            "Legacy" => Divide(raw, 5, 10, 0x537, 0x2825),
            "Micro" => Divide(raw, 5, 10, 0x537, 0x4445),
            "Rectangular" => Divide(raw, 6, 12, 0x1f25, copy == 0 ? 0x1fab2 : 0x20a7b),
            "Version" => Divide(raw, 6, 12, 0x1f25, 0),
            _ => throw new ArgumentException("Unknown code family.")
        };
    }

    // Binary polynomial long division, independent of production BCH code and lookup tables.
    static int Divide(int data, int dataBits, int parityBits, int generator, int xorMask)
    {
        int shifted = data << parityBits;
        int remainder = shifted;
        for (int bit = dataBits + parityBits - 1; bit >= parityBits; bit--)
            if ((remainder & (1 << bit)) != 0) remainder ^= generator << (bit - parityBits);
        return (shifted | remainder) ^ xorMask;
    }

    static int Create(string family, int value, int copy) => family switch
    {
        "Model2" => FormatInformationCodec.CreateModel2Code(value / 8, value % 8),
        "Legacy" => FormatInformationCodec.CreateLegacyModel1Code(value / 8, value % 8),
        "Micro" => FormatInformationCodec.CreateMicroCode(new[] { 1, 2, 2, 3, 3, 4, 4, 4 }[value / 4], new[] { -1, 0, 1, 0, 1, 0, 1, 2 }[value / 4], value % 4),
        "Rectangular" => copy == 0 ? FormatInformationCodec.CreateRectangularMicroFinderCode(value % 32, value / 32)
            : FormatInformationCodec.CreateRectangularMicroSubfinderCode(value % 32, value / 32),
        "Version" => VersionInformationCodec.CreateCode(value + 7),
        _ => throw new ArgumentException("Unknown code family.")
    };

    static BchCorrectionResult Correct(string family, int first, int second) => family switch
    {
        "Model2" => FormatInformationCodec.CorrectModel2Copies(first, second),
        "Legacy" => FormatInformationCodec.CorrectLegacyModel1Copies(first, second),
        "Micro" => FormatInformationCodec.CorrectMicroCopies(first, second),
        "Rectangular" => FormatInformationCodec.CorrectRectangularMicroCopies(first, second),
        "Version" => VersionInformationCodec.CorrectCopies(first, second),
        _ => throw new ArgumentException("Unknown code family.")
    };
}
