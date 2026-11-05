namespace ttf.Tests;

internal class SfntFeatureVariationsResolverTests
{
    [Test]
    public void ResolvesFirstMatchingRecordAndRemovesFeatureVariations()
    {
        byte[] source = BuildLayout(
            new int[] { 1, 1 },
            new short[] { 0x3000, 0x1000 },
            new short[] { 0x4000, 0x3000 },
            new int[] { 1, 1 },
            new int[] { 0, 1 });

        byte[] resolved = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.5),
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt32(resolved, 0), Is.EqualTo(0x00010000U));
            Assert.That(resolved.Length, Is.LessThan(source.Length));
            Assert.That(ReadFeatureLookup(resolved, 0), Is.EqualTo(1));
            Assert.That(ReadFeatureLookup(resolved, 1), Is.EqualTo(0));
            Assert.That(ReadUInt16(resolved, 4), Is.EqualTo(10));
            Assert.That(ReadUInt16(resolved, 6), Is.EqualTo(12));
        });
    }

    [Test]
    public void UsesInclusiveRangesAndKeepsDefaultsWhenNothingMatches()
    {
        byte[] source = BuildLayout(
            new int[] { 1 },
            new short[] { 0x1000 },
            new short[] { 0x3000 },
            new int[] { 1 },
            new int[] { 1 });

        byte[] boundary = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.25),
            FontLimits.CreateCompatible());
        byte[] outside = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0),
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(ReadFeatureLookup(boundary, 0), Is.EqualTo(1));
            Assert.That(ReadFeatureLookup(outside, 0), Is.EqualTo(0));
            Assert.That(ReadUInt32(outside, 0), Is.EqualTo(0x00010000U));
        });
    }

    [Test]
    public void EvaluatesFinalNormalizedCoordinateRatherThanUserCoordinate()
    {
        byte[] source = BuildLayout(
            new int[] { 1 },
            new short[] { 0x1000 },
            new short[] { 0x3000 },
            new int[] { 1 },
            new int[] { 1 });
        NormalizedFontCoordinates coordinates = new NormalizedFontCoordinates(
            new int[] { (int)TestSfntEncoding.EncodeTag("wght") },
            new double[] { 0 },
            new double[] { 0.5 },
            new int[] { 0x2000 },
            "wght=0.5",
            -1,
            false);

        byte[] resolved = SfntFeatureVariationsResolver.Resolve(
            source,
            coordinates,
            FontLimits.CreateCompatible());

        Assert.That(ReadFeatureLookup(resolved, 0), Is.EqualTo(1));
    }

    [Test]
    public void RequiresEveryConditionInAConditionSetToMatch()
    {
        byte[] source = BuildConjunctiveLayout();
        byte[] matching = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.5, -0.25),
            FontLimits.CreateCompatible());
        byte[] partiallyMatching = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.5, 0.25),
            FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(ReadFeatureLookup(matching, 0), Is.EqualTo(1));
            Assert.That(ReadFeatureLookup(partiallyMatching, 0), Is.EqualTo(0));
        });
    }

    [Test]
    public void SkipsUnsupportedCandidatesAndUsesLaterUniversalRecord()
    {
        byte[] source = BuildLayout(
            new int[] { 2, 0 },
            new short[] { 0, 0 },
            new short[] { 0x4000, 0 },
            new int[] { 1, 1 },
            new int[] { 0, 1 });

        byte[] resolved = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.5),
            FontLimits.CreateCompatible());

        Assert.That(ReadFeatureLookup(resolved, 0), Is.EqualTo(1));
    }

    [Test]
    public void InvalidAxisConditionFallsThroughToLaterUniversalRecord()
    {
        byte[] source = BuildLayout(
            new int[] { 1, 0 },
            new short[] { 0, 0 },
            new short[] { 0x4000, 0 },
            new int[] { 1, 1 },
            new int[] { 0, 1 });
        int variations = (int)ReadUInt32(source, 10);
        int conditionSet = variations + 8 + 2 * 8;
        int condition = conditionSet + (int)ReadUInt32(
            source,
            conditionSet + 2);
        source[condition + 2] = 0;
        source[condition + 3] = 1;

        byte[] resolved = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.5),
            FontLimits.CreateCompatible());

        Assert.That(ReadFeatureLookup(resolved, 0), Is.EqualTo(1));
    }

    [Test]
    public void SkipsMatchingRecordWithUnsupportedSubstitutionVersion()
    {
        byte[] source = BuildLayout(
            new int[] { 0, 0 },
            new short[] { 0, 0 },
            new short[] { 0, 0 },
            new int[] { 2, 1 },
            new int[] { 0, 1 });

        byte[] resolved = SfntFeatureVariationsResolver.Resolve(
            source,
            Coordinates(0.5),
            FontLimits.CreateCompatible());

        Assert.That(ReadFeatureLookup(resolved, 0), Is.EqualTo(1));
    }

    [Test]
    public void PreservesSelectedAlternateFeatureParams()
    {
        byte[] source = BuildLayout(
            new int[] { 0 },
            new short[] { 0 },
            new short[] { 0 },
            new int[] { 1 },
            new int[] { 1 });
        byte[] withParameters = new byte[source.Length + 2];
        source.CopyTo(withParameters, 0);
        withParameters[source.Length - 6] = 0;
        withParameters[source.Length - 5] = 6;
        withParameters[withParameters.Length - 2] = 0xAB;
        withParameters[withParameters.Length - 1] = 0xCD;

        byte[] resolved = SfntFeatureVariationsResolver.Resolve(
            withParameters,
            Coordinates(0.5),
            FontLimits.CreateCompatible());
        int featureList = ReadUInt16(resolved, 6);
        int feature = featureList + ReadUInt16(resolved, featureList + 6);

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt16(resolved, feature), Is.EqualTo(6));
            Assert.That(ReadUInt16(resolved, feature + 6), Is.EqualTo(0xABCD));
        });
    }

    [Test]
    public void RejectsSelectedFeatureWithInvalidLookupIndex()
    {
        byte[] source = BuildLayout(
            new int[] { 0 },
            new short[] { 0 },
            new short[] { 0 },
            new int[] { 1 },
            new int[] { 2 });

        Assert.Throws<FontFormatException>(() =>
            SfntFeatureVariationsResolver.Resolve(
                source,
                Coordinates(0.5),
                FontLimits.CreateCompatible()));
    }

    static NormalizedFontCoordinates Coordinates(double normalized)
    {
        return new NormalizedFontCoordinates(
            new int[] { (int)TestSfntEncoding.EncodeTag("wght") },
            new double[] { normalized },
            new double[] { normalized },
            new int[] { (int)System.Math.Round(normalized * 16384) },
            "wght=" + normalized.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            -1,
            normalized == 0);
    }

    static NormalizedFontCoordinates Coordinates(
        double first,
        double second)
    {
        return new NormalizedFontCoordinates(
            new int[]
            {
                (int)TestSfntEncoding.EncodeTag("wght"),
                (int)TestSfntEncoding.EncodeTag("wdth")
            },
            new double[] { first, second },
            new double[] { first, second },
            new int[]
            {
                (int)System.Math.Round(first * 16384),
                (int)System.Math.Round(second * 16384)
            },
            "two-axis",
            -1,
            false);
    }

    static byte[] BuildConjunctiveLayout()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00010001U)
            .WriteUInt16(14).WriteUInt16(16).WriteUInt16(30).WriteUInt32(48)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteTag("kern").WriteUInt16(8)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(2).WriteUInt16(6).WriteUInt16(12)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt32(1)
            .WriteUInt32(16).WriteUInt32(42)
            .WriteUInt16(2).WriteUInt32(10).WriteUInt32(18)
            .WriteUInt16(1).WriteUInt16(0)
            .WriteInt16(0x1000).WriteInt16(0x3000)
            .WriteUInt16(1).WriteUInt16(1)
            .WriteInt16(-0x2000).WriteInt16(0)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(1)
            .WriteUInt16(0).WriteUInt32(12)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(1)
            .CopyBytes();
    }

    static byte[] BuildLayout(
        int[] conditionFormats,
        short[] minimums,
        short[] maximums,
        int[] substitutionVersions,
        int[] alternateLookups)
    {
        if (conditionFormats.Length != minimums.Length ||
            conditionFormats.Length != maximums.Length ||
            conditionFormats.Length != substitutionVersions.Length ||
            conditionFormats.Length != alternateLookups.Length)
        {
            throw new ArgumentException("Feature-variation test arrays differ.");
        }

        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x00010001U)
            .WriteUInt16(14)
            .WriteUInt16(16)
            .WriteUInt16(42)
            .WriteUInt32(60)
            .WriteUInt16(0)
            .WriteUInt16(2)
            .WriteTag("kern").WriteUInt16(14)
            .WriteTag("liga").WriteUInt16(20)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(0).WriteUInt16(1).WriteUInt16(0)
            .WriteUInt16(2).WriteUInt16(6).WriteUInt16(12)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0)
            .WriteUInt16(1).WriteUInt16(0).WriteUInt16(0);
        int variationsStart = builder.GetLength();
        builder.WriteUInt16(1).WriteUInt16(0)
            .WriteUInt32((uint)conditionFormats.Length);
        int[] recordOffsets = new int[conditionFormats.Length];
        int index = 0;

        while (index < recordOffsets.Length)
        {
            recordOffsets[index] = builder.GetLength();
            builder.WriteUInt32(0).WriteUInt32(0);
            index += 1;
        }

        index = 0;
        while (index < conditionFormats.Length)
        {
            if (conditionFormats[index] != 0)
            {
                int conditionSetStart = builder.GetLength();
                builder.PatchUInt32(
                    recordOffsets[index],
                    (uint)(conditionSetStart - variationsStart));
                builder.WriteUInt16(1).WriteUInt32(6)
                    .WriteUInt16((ushort)conditionFormats[index])
                    .WriteUInt16(0)
                    .WriteInt16(minimums[index])
                    .WriteInt16(maximums[index]);
            }

            int substitutionStart = builder.GetLength();
            builder.PatchUInt32(
                recordOffsets[index] + 4,
                (uint)(substitutionStart - variationsStart));
            builder.WriteUInt16((ushort)substitutionVersions[index])
                .WriteUInt16(0)
                .WriteUInt16(1)
                .WriteUInt16(0)
                .WriteUInt32(12)
                .WriteUInt16(0)
                .WriteUInt16(1)
                .WriteUInt16((ushort)alternateLookups[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static int ReadFeatureLookup(byte[] data, int featureIndex)
    {
        int featureList = ReadUInt16(data, 6);
        int record = featureList + 2 + featureIndex * 6;
        int feature = featureList + ReadUInt16(data, record + 4);
        Assert.That(ReadUInt16(data, feature + 2), Is.EqualTo(1));
        return ReadUInt16(data, feature + 4);
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] << 8 | data[offset + 1];
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset] << 24 |
            (uint)data[offset + 1] << 16 |
            (uint)data[offset + 2] << 8 |
            data[offset + 3];
    }
}
