using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class FormatInformationCodecTests
{
    [Test]
    public void CreationMatchesEveryStandardizedInformationCode()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                Assert.That(
                    FormatInformationCodec.CreateModel2Code(level, mask),
                    Is.EqualTo(FormatCodeTable.GetCode(level, mask)));
                Assert.That(
                    FormatInformationCodec.CreateLegacyModel1Code(level, mask),
                    Is.EqualTo(LegacyModel1Table.GetFormatCode(level, mask)));
            }
        }

        AssertMicroCreation(1, MicroVersionTable.DetectionOnly);
        for (int version = 2; version <= 3; version++)
        {
            AssertMicroCreation(version, EccBlockTable.Low);
            AssertMicroCreation(version, EccBlockTable.Medium);
        }

        AssertMicroCreation(4, EccBlockTable.Low);
        AssertMicroCreation(4, EccBlockTable.Medium);
        AssertMicroCreation(4, EccBlockTable.Quartile);

        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                Assert.That(
                    FormatInformationCodec.CreateRectangularMicroFinderCode(version, level),
                    Is.EqualTo(RectangularMicroFormatCodeTable.GetFinderSideCode(
                        version,
                        level)));
                Assert.That(
                    FormatInformationCodec.CreateRectangularMicroSubfinderCode(version, level),
                    Is.EqualTo(RectangularMicroFormatCodeTable.GetSubfinderSideCode(
                        version,
                        level)));
            }
        }

        for (int version = 7; version <= 40; version++)
        {
            Assert.That(
                VersionInformationCodec.CreateCode(version),
                Is.EqualTo(VersionCodeTable.GetCode(version)));
        }
    }

    [Test]
    public void Model2AndLegacyCopiesCorrectThreeBitDamageForEveryValue()
    {
        for (int level = 0; level < 4; level++)
        {
            for (int mask = 0; mask < 8; mask++)
            {
                int data = level * 8 + mask;
                int model2 = FormatInformationCodec.CreateModel2Code(level, mask);
                BchCorrectionResult model2Result =
                    FormatInformationCodec.CorrectModel2Copies(
                        model2 ^ CreateDamageMask(data, 15, 3),
                        model2 ^ CreateDamageMask(data + 5, 15, 2));
                AssertCorrected(model2Result, data, 3, 2);

                int legacy = FormatInformationCodec.CreateLegacyModel1Code(level, mask);
                BchCorrectionResult legacyResult =
                    FormatInformationCodec.CorrectLegacyModel1Copies(
                        legacy ^ CreateDamageMask(data + 7, 15, 2),
                        legacy ^ CreateDamageMask(data + 11, 15, 3));
                AssertCorrected(legacyResult, data, 2, 3);
            }
        }
    }

    [Test]
    public void MicroCopiesCorrectThreeBitDamageForEveryValue()
    {
        for (int data = 0; data < 32; data++)
        {
            int code = BchCodec.CreateCode(data, 5, 10, 0x537, 0x4445);
            BchCorrectionResult result = FormatInformationCodec.CorrectMicroCopies(
                code ^ CreateDamageMask(data, 15, 3),
                code ^ CreateDamageMask(data + 3, 15, 1));
            AssertCorrected(result, data, 3, 1);
        }
    }

    [Test]
    public void RectangularCopiesCorrectThreeBitDamageDespiteDifferentMasks()
    {
        for (int level = 0; level < 2; level++)
        {
            for (int version = 0; version < 32; version++)
            {
                int data = level * 32 + version;
                int finder = FormatInformationCodec.CreateRectangularMicroFinderCode(
                    version,
                    level);
                int subfinder = FormatInformationCodec.CreateRectangularMicroSubfinderCode(
                    version,
                    level);
                BchCorrectionResult result =
                    FormatInformationCodec.CorrectRectangularMicroCopies(
                        finder ^ CreateDamageMask(data, 18, 3),
                        subfinder ^ CreateDamageMask(data + 9, 18, 2));
                AssertCorrected(result, data, 3, 2);
            }
        }
    }

    [Test]
    public void VersionCopiesCorrectThreeBitDamageForEveryVersion()
    {
        for (int version = 7; version <= 40; version++)
        {
            int code = VersionInformationCodec.CreateCode(version);
            BchCorrectionResult result = VersionInformationCodec.CorrectCopies(
                code ^ CreateDamageMask(version, 18, 3),
                code ^ CreateDamageMask(version + 13, 18, 2));
            AssertCorrected(result, version, 3, 2);
        }
    }

    [Test]
    public void RedundantCopiesUseTheValidOrCloserCopyAndRejectExactConflicts()
    {
        int first = FormatInformationCodec.CreateModel2Code(
            EccBlockTable.Low,
            2);
        int second = FormatInformationCodec.CreateModel2Code(
            EccBlockTable.High,
            6);
        int uncorrectable = FindUncorrectableModel2Code();

        BchCorrectionResult surviving = FormatInformationCodec.CorrectModel2Copies(
            uncorrectable,
            first);
        BchCorrectionResult closer = FormatInformationCodec.CorrectModel2Copies(
            first ^ 1,
            second);
        BchCorrectionResult conflict = FormatInformationCodec.CorrectModel2Copies(
            first,
            second);

        Assert.Multiple(() =>
        {
            Assert.That(surviving.Success, Is.True);
            Assert.That(surviving.Data, Is.EqualTo(EccBlockTable.Low * 8 + 2));
            Assert.That(surviving.SourceCopy, Is.EqualTo(1));
            Assert.That(surviving.CopiesAgree, Is.False);
            Assert.That(closer.Success, Is.True);
            Assert.That(closer.Data, Is.EqualTo(EccBlockTable.High * 8 + 6));
            Assert.That(closer.SourceCopy, Is.EqualTo(1));
            Assert.That(closer.CopiesAgree, Is.False);
            Assert.That(conflict.Success, Is.False);
            Assert.That(conflict.Data, Is.EqualTo(-1));
            Assert.That(conflict.Distance, Is.Zero);
        });
    }

    [Test]
    public void RectangularExactConflictsAreRejectedAcrossDistinctCopyMasks()
    {
        int first = FormatInformationCodec.CreateRectangularMicroFinderCode(4, 0);
        int second = FormatInformationCodec.CreateRectangularMicroSubfinderCode(17, 1);

        BchCorrectionResult result =
            FormatInformationCodec.CorrectRectangularMicroCopies(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FirstDistance, Is.Zero);
            Assert.That(result.SecondDistance, Is.Zero);
            Assert.That(result.CopiesAgree, Is.False);
        });
    }

    [Test]
    public void RejectsInvalidCreationAndReceivedCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => FormatInformationCodec.CreateModel2Code(-1, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => FormatInformationCodec.CreateModel2Code(0, 8),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FormatInformationCodec.CreateMicroCode(1, -1, 4),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FormatInformationCodec.CreateRectangularMicroFinderCode(32, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => VersionInformationCodec.CreateCode(6),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => VersionInformationCodec.CreateCode(41),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => FormatInformationCodec.CorrectModel2Copies(-1, 0),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => VersionInformationCodec.CorrectCopies(0, 0x40000),
                Throws.TypeOf<QrInputException>());
        });
    }

    private static void AssertMicroCreation(int version, int level)
    {
        for (int mask = 0; mask < 4; mask++)
        {
            Assert.That(
                FormatInformationCodec.CreateMicroCode(version, level, mask),
                Is.EqualTo(MicroFormatCodeTable.GetCode(version, level, mask)));
        }
    }

    private static void AssertCorrected(
        BchCorrectionResult result,
        int expectedData,
        int firstDistance,
        int secondDistance)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.EqualTo(expectedData));
        Assert.That(result.Distance, Is.EqualTo(Math.Min(firstDistance, secondDistance)));
        Assert.That(result.FirstDistance, Is.EqualTo(firstDistance));
        Assert.That(result.SecondDistance, Is.EqualTo(secondDistance));
        Assert.That(result.CopiesAgree, Is.True);
    }

    private static int CreateDamageMask(int seed, int bitCount, int damageCount)
    {
        int mask = 0;
        int position = seed % bitCount;
        for (int index = 0; index < damageCount; index++)
        {
            while ((mask & 1 << position) != 0)
            {
                position++;
                if (position == bitCount)
                {
                    position = 0;
                }
            }

            mask |= 1 << position;
            position = (position + 5) % bitCount;
        }

        return mask;
    }

    private static int FindUncorrectableModel2Code()
    {
        for (int candidate = 0; candidate < 0x8000; candidate++)
        {
            BchCorrectionResult result = FormatInformationCodec.CorrectModel2Copies(
                candidate,
                candidate);
            if (!result.Success && result.Distance > 3)
            {
                return candidate;
            }
        }

        Assert.Fail("Expected an uncorrectable Model 2 format word.");
        return -1;
    }
}
