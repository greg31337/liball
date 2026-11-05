using ttf;

namespace ttf.Tests;

public class FontLimitsTests
{
    [Test]
    public void DefaultConstructorUsesCompatibleProfile()
    {
        FontLimits defaults = FontLimits.Create();
        FontLimits compatible = FontLimits.CreateCompatible();

        Assert.That(GetValues(defaults), Is.EqualTo(GetValues(compatible)));
    }

    [Test]
    public void StrictProfileHasExpectedConservativeLimits()
    {
        FontLimits limits = FontLimits.CreateStrict();

        Assert.That(GetValues(limits), Is.EqualTo(new int[]
        {
            67108864, 134217728, 32, 256, 65535, 131072, 32767, 32, 10,
            16, 4096, 1000000, 1000000, 1048576, 134217728, 100, 32768,
            64, 1024
        }));
    }

    [Test]
    public void CompatibleProfileHasExpectedProductionLimits()
    {
        FontLimits limits = FontLimits.CreateCompatible();

        Assert.That(GetValues(limits), Is.EqualTo(new int[]
        {
            268435456, 536870912, 256, 1024, 65535, 1048576, 32767, 64,
            10, 32, 16384, 5000000, 2000000, 4194304, 536870912, 250,
            65536, 128, 4096
        }));
    }

    [Test]
    public void InspectOnlyProfileFavorsBroadShallowInspection()
    {
        FontLimits limits = FontLimits.CreateInspectOnly();

        Assert.That(GetValues(limits), Is.EqualTo(new int[]
        {
            268435456, 67108864, 1024, 4096, 65535, 131072, 32767, 16,
            10, 16, 4096, 250000, 1000000, 4194304, 536870912, 250,
            32768, 32, 16384
        }));
    }

    [Test]
    public void ConfiguredFactoryPreservesEveryLimit()
    {
        int[] values = CreatePositiveValues();

        FontLimits limits = Create(values);

        Assert.That(GetValues(limits), Is.EqualTo(values));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(18)]
    public void ConfiguredFactoryRejectsNonPositiveLimit(int index)
    {
        int[] values = CreatePositiveValues();
        values[index] = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() => Create(values));

        values[index] = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(values));
    }

    [Test]
    public void ProfileFactoriesReturnIndependentInstances()
    {
        FontLimits strictFirst = FontLimits.CreateStrict();
        FontLimits strictSecond = FontLimits.CreateStrict();
        FontLimits compatibleFirst = FontLimits.CreateCompatible();
        FontLimits compatibleSecond = FontLimits.CreateCompatible();
        FontLimits inspectFirst = FontLimits.CreateInspectOnly();
        FontLimits inspectSecond = FontLimits.CreateInspectOnly();

        Assert.Multiple(() =>
        {
            Assert.That(strictFirst, Is.Not.SameAs(strictSecond));
            Assert.That(compatibleFirst, Is.Not.SameAs(compatibleSecond));
            Assert.That(inspectFirst, Is.Not.SameAs(inspectSecond));
        });
    }

    private static FontLimits Create(int[] values)
    {
        return FontLimits.Create(
            values[0], values[1], values[2], values[3], values[4],
            values[5], values[6], values[7], values[8], values[9],
            values[10], values[11], values[12], values[13], values[14],
            values[15], values[16], values[17], values[18]);
    }

    private static int[] CreatePositiveValues()
    {
        return new int[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19
        };
    }

    private static int[] GetValues(FontLimits limits)
    {
        return new int[]
        {
            limits.GetMaxInputBytes(),
            limits.GetMaxOutputBytes(),
            limits.GetMaxFaces(),
            limits.GetMaxTablesPerFace(),
            limits.GetMaxGlyphsPerFace(),
            limits.GetMaxPointsPerGlyph(),
            limits.GetMaxContoursPerGlyph(),
            limits.GetMaxComponentDepth(),
            limits.GetMaxCffSubroutineDepth(),
            limits.GetMaxLayoutNesting(),
            limits.GetMaxLookupsPerTable(),
            limits.GetMaxLayoutOperations(),
            limits.GetMaxCoverageEntries(),
            limits.GetMaxStringBytes(),
            limits.GetMaxDecompressedBytes(),
            limits.GetMaxDecompressionRatio(),
            limits.GetMaxVariationRegions(),
            limits.GetMaxPaintDepth(),
            limits.GetMaxDiagnostics()
        };
    }
}
