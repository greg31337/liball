using ttf;

namespace ttf.Tests;

internal class Cff2VariationTests
{
    [Test]
    public void ParsesCff2VariationRegionsAndCalculatesScalars()
    {
        Cff2VariationStore store = ParseStore(BuildTwoAxisStore());
        Cff2VariationLocation firstLocation = store.CreateLocation(
            new double[] { 0.5, 0.25 });
        double[] first = firstLocation.CopyRegionScalars(0);
        double[] second = store.CopyRegionScalars(
            1,
            new double[] { -1, 0.75 });

        Assert.Multiple(() =>
        {
            Assert.That(store.GetAxisCount(), Is.EqualTo(2));
            Assert.That(store.GetRegionCount(), Is.EqualTo(3));
            Assert.That(store.GetVariationDataCount(), Is.EqualTo(2));
            Assert.That(store.GetVariationData(0).GetRegionCount(), Is.EqualTo(2));
            Assert.That(store.GetVariationData(0).GetRegionIndex(1), Is.EqualTo(2));
            Assert.That(first, Is.EqualTo(new double[] { 0.5, 0.5 }));
            Assert.That(second, Is.EqualTo(new double[] { 1 }));
            Assert.That(store.GetRegion(0).GetAxis(0).GetPeak(), Is.EqualTo(1));
        });
    }

    [Test]
    public void RegionScalarHandlesPeaksEdgesIgnoredAxesAndOutOfRangeCoordinates()
    {
        SfntVariationRegion positive = new SfntVariationRegion(
            new SfntVariationRegionAxis[]
            {
                new SfntVariationRegionAxis(0, 0.5, 1)
            });
        SfntVariationRegion ignored = new SfntVariationRegion(
            new SfntVariationRegionAxis[]
            {
                new SfntVariationRegionAxis(-1, 0, 1)
            });

        Assert.Multiple(() =>
        {
            Assert.That(Scalar(positive, 0), Is.Zero);
            Assert.That(Scalar(positive, 0.25), Is.EqualTo(0.5));
            Assert.That(Scalar(positive, 0.5), Is.EqualTo(1));
            Assert.That(Scalar(positive, 0.75), Is.EqualTo(0.5));
            Assert.That(Scalar(positive, -0.1), Is.Zero);
            Assert.That(Scalar(ignored, -0.75), Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Scalar(positive, 1.1));
        });
    }

    [Test]
    public void EvaluatesPrivateDictionaryBlendUsingSelectedRegions()
    {
        Cff2VariationStore store = ParseStore(BuildTwoAxisStore());
        byte[] program = Join(
            Number(10),
            Number(20),
            Number(4),
            Number(8),
            Number(10),
            Number(20),
            Number(2),
            new byte[] { 23, 6 });
        Cff2PrivateDictionary raw = Cff2PrivateDictionary.Parse(Slice(program));
        Cff2PrivateDictionary evaluated = raw.Evaluate(
            store,
            new double[] { 0.5, 0.5 });

        Assert.Multiple(() =>
        {
            Assert.That(raw.HasBlend(), Is.True);
            Assert.That(raw.IsEvaluated(), Is.False);
            Assert.That(evaluated.HasBlend(), Is.True);
            Assert.That(evaluated.IsEvaluated(), Is.True);
            Assert.That(evaluated.CopyBlueValues(),
                Is.EqualTo(new double[] { 20, 65 }));
            Assert.Throws<InvalidOperationException>(() => raw.CopyBlueValues());
        });
    }

    [Test]
    public void PrivateDictionaryVsIndexSelectsAnotherRegionList()
    {
        Cff2VariationStore store = ParseStore(BuildTwoAxisStore());
        byte[] program = Join(
            Number(1),
            new byte[] { 22 },
            Number(10),
            Number(4),
            Number(1),
            new byte[] { 23, 10 });
        Cff2PrivateDictionary evaluated =
            Cff2PrivateDictionary.Parse(Slice(program)).Evaluate(
                store,
                new double[] { -1, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(evaluated.GetVariationStoreDataIndex(), Is.EqualTo(1));
            Assert.That(evaluated.GetStandardHorizontalWidth(), Is.EqualTo(14));
        });
    }

    [Test]
    public void ExecutesCff2CharStringBlendAtNormalizedCoordinate()
    {
        Cff2VariationStore store = ParseStore(BuildOneAxisStore());
        byte[] program = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(120), Number(52), Number(1), new byte[] { 16, 6 });
        Type2CharStringResult midpoint = Type2CharStringExecutor.ExecuteCff2(
            Slice(program),
            0,
            null,
            null!,
            store,
            new double[] { 0.75 },
            10,
            1000);
        Type2CharStringResult defaultInstance = Type2CharStringExecutor.ExecuteCff2(
            Slice(program),
            0,
            null,
            null!,
            store,
            new double[] { 0 },
            10,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(midpoint.GetCommandCount(), Is.EqualTo(2));
            Assert.That(midpoint.GetCommand(1).GetEndX(), Is.EqualTo(159));
            Assert.That(defaultInstance.GetCommand(1).GetEndX(), Is.EqualTo(120));
            Assert.That(midpoint.GetWidth(), Is.Zero);
            Assert.That(midpoint.HasExplicitWidth(), Is.False);
        });
    }

    [Test]
    public void BlendsMultipleCharStringOperandsInStackOrder()
    {
        Cff2VariationStore store = ParseStore(BuildOneAxisStore());
        byte[] program = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(100), Number(200),
            Number(20), Number(-40),
            Number(2), new byte[] { 16, 5 });

        Type2CharStringResult result = Type2CharStringExecutor.ExecuteCff2(
            Slice(program),
            0,
            null,
            null!,
            store,
            new double[] { 0.75 },
            10,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetCommandCount(), Is.EqualTo(2));
            Assert.That(result.GetCommand(1).GetEndX(), Is.EqualTo(115));
            Assert.That(result.GetCommand(1).GetEndY(), Is.EqualTo(170));
        });
    }

    [Test]
    public void Cff2CharStringVsIndexOverridesPrivateDefault()
    {
        Cff2VariationStore store = ParseStore(BuildTwoAxisStore());
        byte[] program = Join(
            Number(1), new byte[] { 15 },
            Number(0), Number(0), new byte[] { 21 },
            Number(100), Number(20), Number(1), new byte[] { 16, 6 });
        Type2CharStringResult result = Type2CharStringExecutor.ExecuteCff2(
            Slice(program),
            0,
            null,
            null!,
            store,
            new double[] { -1, 0 },
            10,
            1000);

        Assert.That(result.GetCommand(1).GetEndX(), Is.EqualTo(120));
    }

    [Test]
    public void BlendStateFlowsThroughCff2LocalSubroutines()
    {
        Cff2VariationStore store = ParseStore(BuildOneAxisStore());
        Cff2Index localSubroutines = Cff2IndexParser.Parse(
            Slice(BuildIndex(Join(
                Number(120), Number(52), Number(1),
                new byte[] { 16, 6 }))),
            0,
            10);
        byte[] program = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(-107), new byte[] { 10 });
        Type2CharStringResult result = Type2CharStringExecutor.ExecuteCff2(
            Slice(program),
            0,
            localSubroutines,
            null!,
            store,
            new double[] { 0.75 },
            10,
            1000);

        Assert.That(result.GetCommand(1).GetEndX(), Is.EqualTo(159));
    }

    [Test]
    public void TableExecutesCff2BlendUsingFontDictionaryDefault()
    {
        SfntCff2Table? table = ParseTable(BuildVariableCff2());
        Type2CharStringResult result = table!.ExecuteCharString(
            0,
            new double[] { 0.75 });
        Cff2PrivateDictionary privateDictionary =
            table.GetEvaluatedPrivateDictionary(0, new double[] { 0.75 });

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVariationData()!.GetAxisCount(), Is.EqualTo(1));
            Assert.That(result.GetCommand(1).GetEndX(), Is.EqualTo(159));
            Assert.That(privateDictionary.IsEvaluated(), Is.True);
            Assert.That(privateDictionary.GetVariationStoreDataIndex(), Is.Zero);
        });
    }

    [Test]
    public void RejectsMalformedCff2VariationStores()
    {
        byte[] badFormat = BuildOneAxisStore();
        badFormat[1] = 2;
        byte[] badRegionOrder = BuildOneAxisStore();
        badRegionOrder[16] = 0x40;
        badRegionOrder[18] = 0x20;
        byte[] embeddedDeltas = BuildOneAxisStore();
        embeddedDeltas[23] = 1;
        byte[] badRegionIndex = BuildOneAxisStore();
        badRegionIndex[29] = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseStore(badFormat));
            Assert.Throws<FontFormatException>(() => ParseStore(badRegionOrder));
            Assert.Throws<FontFormatException>(() => ParseStore(embeddedDeltas));
            Assert.Throws<FontFormatException>(() => ParseStore(badRegionIndex));
            Assert.Throws<FontLimitException>(() =>
                ParseStore(BuildTwoAxisStore(), 2));
            Assert.Throws<FontLimitException>(() =>
                ParseStore(BuildTwoAxisStore(), 100, 1));
        });
    }

    [Test]
    public void RejectsInvalidCoordinatesAndVariationPrograms()
    {
        Cff2VariationStore store = ParseStore(BuildOneAxisStore());
        byte[] move = Join(Number(0), Number(0), new byte[] { 21 });
        byte[] missingBlendOperands = Join(
            move,
            Number(1),
            new byte[] { 16 });
        byte[] repeatedVsIndex = Join(
            Number(0), new byte[] { 15 },
            Number(0), new byte[] { 15 });
        byte[] vsIndexAfterBlend = Join(
            Number(10), Number(2), Number(1), new byte[] { 16 },
            Number(0), new byte[] { 15 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                store.CopyRegionScalars(0, Array.Empty<double>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                store.CopyRegionScalars(0, new double[] { 2 }));
            Assert.Throws<FontFormatException>(() => Execute(
                missingBlendOperands,
                store));
            Assert.Throws<FontFormatException>(() => Execute(
                repeatedVsIndex,
                store));
            Assert.Throws<FontFormatException>(() => Execute(
                vsIndexAfterBlend,
                store));
            Assert.Throws<FontFormatException>(() => Execute(
                new byte[] { 14 },
                store));
            Assert.Throws<FontFormatException>(() =>
                Cff2PrivateDictionary.Parse(Slice(Join(
                    Number(2), new byte[] { 22 },
                    Number(10), new byte[] { 10 }))).Evaluate(
                        store,
                        new double[] { 0 }));
        });
    }

    static Type2CharStringResult Execute(
        byte[] program,
        Cff2VariationStore store)
    {
        return Type2CharStringExecutor.ExecuteCff2(
            Slice(program),
            0,
            null,
            null!,
            store,
            new double[] { 0.5 },
            10,
            1000);
    }

    static byte[] BuildOneAxisStore()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteUInt16(1);
        writer.WriteUInt32(12);
        writer.WriteUInt16(1);
        writer.WriteUInt32(22);
        writer.WriteUInt16(1);
        writer.WriteUInt16(1);
        writer.WriteF2Dot14(0);
        writer.WriteF2Dot14(1);
        writer.WriteF2Dot14(1);
        writer.WriteUInt16(0);
        writer.WriteUInt16(0);
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        return writer.CopyBytes();
    }

    static byte[] BuildTwoAxisStore()
    {
        BigEndianWriter writer = BigEndianWriter.Create();
        writer.WriteUInt16(1);
        writer.WriteUInt32(16);
        writer.WriteUInt16(2);
        writer.WriteUInt32(56);
        writer.WriteUInt32(66);
        writer.WriteUInt16(2);
        writer.WriteUInt16(3);
        WriteRegion(writer, 0, 1, 1, 0, 0, 0);
        WriteRegion(writer, -1, -1, 0, 0, 0, 0);
        WriteRegion(writer, 0, 0, 0, 0, 0.5, 1);
        writer.WriteUInt16(0);
        writer.WriteUInt16(0);
        writer.WriteUInt16(2);
        writer.WriteUInt16(0);
        writer.WriteUInt16(2);
        writer.WriteUInt16(0);
        writer.WriteUInt16(0);
        writer.WriteUInt16(1);
        writer.WriteUInt16(1);
        return writer.CopyBytes();
    }

    static void WriteRegion(
        BigEndianWriter writer,
        double firstStart,
        double firstPeak,
        double firstEnd,
        double secondStart,
        double secondPeak,
        double secondEnd)
    {
        writer.WriteF2Dot14(firstStart);
        writer.WriteF2Dot14(firstPeak);
        writer.WriteF2Dot14(firstEnd);
        writer.WriteF2Dot14(secondStart);
        writer.WriteF2Dot14(secondPeak);
        writer.WriteF2Dot14(secondEnd);
    }

    static Cff2VariationStore ParseStore(byte[] bytes)
    {
        return ParseStore(bytes, 100);
    }

    static double Scalar(SfntVariationRegion region, double coordinate)
    {
        SfntVariationRegionLocation location =
            SfntVariationRegionLocation.Create(
                new SfntVariationRegion[] { region },
                1,
                new double[] { coordinate });
        return location.GetRegionScalar(0);
    }

    static Cff2VariationStore ParseStore(byte[] bytes, int maximumRegions)
    {
        return ParseStore(bytes, maximumRegions, int.MaxValue);
    }

    static Cff2VariationStore ParseStore(
        byte[] bytes,
        int maximumRegions,
        int maximumEntries)
    {
        return Cff2VariationStoreParser.Parse(
            new Cff2VariationStoreReference(0, bytes.Length, Slice(bytes)),
            maximumRegions,
            maximumEntries);
    }

    static byte[] BuildVariableCff2()
    {
        byte[] store = BuildOneAxisStore();
        byte[] charString = Join(
            Number(0), Number(0), new byte[] { 21 },
            Number(120), Number(52), Number(1), new byte[] { 16, 6 });
        byte[] charStrings = BuildIndex(charString);
        int topSize = 19;
        int fixedEnd = 5 + topSize + 4;
        int charStringsOffset = fixedEnd;
        int fontDictionaryArrayOffset = charStringsOffset + charStrings.Length;
        int fontDictionaryArrayLength = 4 + 1 + 2 + 11;
        int variationStoreOffset =
            fontDictionaryArrayOffset + fontDictionaryArrayLength;
        int privateOffset = variationStoreOffset + 2 + store.Length;
        byte[] fontDictionaryArray = BuildIndex(Join(
            Integer(2), Integer(privateOffset), new byte[] { 18 }));
        byte[] top = Join(
            Integer(charStringsOffset), new byte[] { 17 },
            Integer(variationStoreOffset), new byte[] { 24 },
            Integer(fontDictionaryArrayOffset), new byte[] { 12, 36 });
        return Join(
            new byte[] { 2, 0, 5, 0, (byte)top.Length },
            top,
            new byte[] { 0, 0, 0, 0 },
            charStrings,
            fontDictionaryArray,
            new byte[] { 0, (byte)store.Length },
            store,
            new byte[] { 139, 22 });
    }

    static byte[] BuildIndex(params byte[][] objects)
    {
        List<byte> bytes = new List<byte>();
        WriteUInt32(bytes, objects.Length);
        bytes.Add(1);
        int offset = 1;
        bytes.Add((byte)offset);

        foreach (byte[] value in objects)
        {
            offset += value.Length;
            bytes.Add((byte)offset);
        }

        foreach (byte[] value in objects)
        {
            bytes.AddRange(value);
        }

        return bytes.ToArray();
    }

    static byte[] Number(int value)
    {
        return new byte[] { 28, (byte)(value >> 8), (byte)value };
    }

    static byte[] Integer(int value)
    {
        return new byte[]
        {
            29,
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
    }

    static void WriteUInt32(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static SfntCff2Table? ParseTable(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Cff2,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, bytes.Length),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict,
            FontLimits.CreateStrict());
        return SfntCff2TableParser.FindTable(cache);
    }

    static FontDataSlice Slice(byte[] bytes)
    {
        return FontData.Create(bytes).GetSlice(0, bytes.Length);
    }

    static byte[] Join(params byte[][] values)
    {
        return values.SelectMany(value => value).ToArray();
    }
}
