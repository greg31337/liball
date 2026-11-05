using ttf;

namespace ttf.Tests;

internal class SfntTableCacheTests
{
    [Test]
    public void ExposesRawViewAndCachesOneParsedView()
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 0, 7 }, FontLoadMode.Compatible);
        int versionReads = 0;
        int parses = 0;
        SfntTableParser parser = new SfntTableParser(
            FontTags.Name,
            1,
            1,
            0,
            table =>
            {
                Interlocked.Increment(ref versionReads);
                return ReadByteVersion(table);
            },
            (table, version, support, limits, cache) =>
            {
                Interlocked.Increment(ref parses);
                return new ParsedMarker(table.GetDataSlice().GetMemory().Span[2]);
            });

        SfntParsedTableView? first = cache.FindParsedTable(parser);
        SfntParsedTableView? second = cache.FindParsedTable(parser);
        ParsedMarker parsed = (ParsedMarker)first!.GetParsedData();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first.GetRawView(), Is.SameAs(cache.FindRawTable(FontTags.Name)));
            Assert.That(first.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.Supported));
            Assert.That(first.GetVersion().GetMajorVersion(), Is.EqualTo(1));
            Assert.That(first.GetVersion().GetMinorVersion(), Is.Zero);
            Assert.That(first.HasParsedData(), Is.True);
            Assert.That(parsed.GetValue(), Is.EqualTo(7));
            Assert.That(versionReads, Is.EqualTo(1));
            Assert.That(parses, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompatibleModeParsesHigherMinorVersionAsExtension()
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 4, 9 }, FontLoadMode.Compatible);
        int parses = 0;
        SfntTableParser parser = CreateCountingParser(1, 1, 0, () => parses += 1);

        SfntParsedTableView? view = cache.FindParsedTable(parser);

        Assert.Multiple(() =>
        {
            Assert.That(view!.GetVersionSupport(), Is.EqualTo(
                SfntTableVersionSupport.CompatibleExtension));
            Assert.That(view.HasParsedData(), Is.True);
            Assert.That(parses, Is.EqualTo(1));
        });
    }

    [TestCase(FontLoadMode.Strict)]
    [TestCase(FontLoadMode.InspectOnly)]
    public void NonCompatibleModesKeepHigherMinorVersionRaw(FontLoadMode mode)
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 4, 9 }, mode);
        int parses = 0;
        SfntTableParser parser = CreateCountingParser(1, 1, 0, () => parses += 1);

        SfntParsedTableView? view = cache.FindParsedTable(parser);

        Assert.Multiple(() =>
        {
            Assert.That(view!.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(view.HasParsedData(), Is.False);
            Assert.That(view.GetRawView().CopyRawData(), Is.EqualTo(new byte[] { 1, 4, 9 }));
            Assert.Throws<InvalidOperationException>(() => view.GetParsedData());
            Assert.That(parses, Is.Zero);
        });
    }

    [Test]
    public void UnknownMajorVersionRemainsRawInCompatibleMode()
    {
        SfntTableCache cache = CreateCache(new byte[] { 3, 0, 9 }, FontLoadMode.Compatible);
        int parses = 0;
        SfntTableParser parser = CreateCountingParser(1, 2, 0, () => parses += 1);

        SfntParsedTableView? view = cache.FindParsedTable(parser);

        Assert.Multiple(() =>
        {
            Assert.That(view!.GetVersion().GetMajorVersion(), Is.EqualTo(3));
            Assert.That(view.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(view.HasParsedData(), Is.False);
            Assert.That(parses, Is.Zero);
        });
    }

    [Test]
    public void ConcurrentReadsExecuteVersionReaderAndParserExactlyOnce()
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 0, 5 }, FontLoadMode.Compatible);
        int versionReads = 0;
        int parses = 0;
        SfntTableParser parser = new SfntTableParser(
            FontTags.Name,
            1,
            1,
            0,
            table =>
            {
                Interlocked.Increment(ref versionReads);
                Thread.Sleep(5);
                return ReadByteVersion(table);
            },
            (table, version, support, limits, cache) =>
            {
                Interlocked.Increment(ref parses);
                Thread.Sleep(5);
                return new ParsedMarker(5);
            });
        SfntParsedTableView?[] results = new SfntParsedTableView[256];

        Parallel.For(0, results.Length, index =>
        {
            results[index] = cache.FindParsedTable(parser);
        });

        SfntParsedTableView? expected = results[0];

        Assert.Multiple(() =>
        {
            Assert.That(results.All(result => ReferenceEquals(result, expected)), Is.True);
            Assert.That(versionReads, Is.EqualTo(1));
            Assert.That(parses, Is.EqualTo(1));
        });
    }

    [Test]
    public void ParserFailuresAreCachedDeterministically()
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 0, 0 }, FontLoadMode.Compatible);
        int parses = 0;
        InvalidOperationException failure = new InvalidOperationException("expected failure");
        SfntTableParser parser = new SfntTableParser(
            FontTags.Name,
            1,
            1,
            0,
            ReadByteVersion,
            (table, version, support, limits, cache) =>
            {
                Interlocked.Increment(ref parses);
                throw failure;
            });

        InvalidOperationException? first = Assert.Throws<InvalidOperationException>(
            () => cache.FindParsedTable(parser));
        InvalidOperationException? second = Assert.Throws<InvalidOperationException>(
            () => cache.FindParsedTable(parser));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(failure));
            Assert.That(second, Is.SameAs(failure));
            Assert.That(parses, Is.EqualTo(1));
        });
    }

    [Test]
    public void IndependentFaceCachesDoNotShareParsedState()
    {
        SfntTableCache firstCache = CreateCache(new byte[] { 1, 0, 4 }, FontLoadMode.Compatible);
        SfntTableCache secondCache = CreateCache(new byte[] { 1, 0, 8 }, FontLoadMode.Compatible);
        int parses = 0;
        SfntTableParser parser = new SfntTableParser(
            FontTags.Name,
            1,
            1,
            0,
            ReadByteVersion,
            (table, version, support, limits, cache) =>
            {
                Interlocked.Increment(ref parses);
                return new ParsedMarker(table.GetDataSlice().GetMemory().Span[2]);
            });

        SfntParsedTableView? first = firstCache.FindParsedTable(parser);
        SfntParsedTableView? second = secondCache.FindParsedTable(parser);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(((ParsedMarker)first!.GetParsedData()).GetValue(), Is.EqualTo(4));
            Assert.That(((ParsedMarker)second!.GetParsedData()).GetValue(), Is.EqualTo(8));
            Assert.That(parses, Is.EqualTo(2));
        });
    }

    [Test]
    public void MissingTableDoesNotInvokeVersionReaderOrParser()
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 0 }, FontLoadMode.Compatible);
        int calls = 0;
        SfntTableParser parser = new SfntTableParser(
            FontTags.Head,
            1,
            1,
            0,
            table =>
            {
                calls += 1;
                return ReadByteVersion(table);
            },
            (table, version, support, limits, cache) =>
            {
                calls += 1;
                return new ParsedMarker(0);
            });

        Assert.Multiple(() =>
        {
            Assert.That(cache.FindParsedTable(parser), Is.Null);
            Assert.That(cache.FindRawTable(FontTags.Head), Is.Null);
            Assert.That(calls, Is.Zero);
        });
    }

    [Test]
    public void OneTableRejectsCompetingSemanticParsers()
    {
        SfntTableCache cache = CreateCache(new byte[] { 1, 0, 0 }, FontLoadMode.Compatible);
        SfntTableParser first = CreateCountingParser(1, 1, 0, () => { });
        SfntTableParser second = CreateCountingParser(1, 1, 0, () => { });

        cache.FindParsedTable(first);

        Assert.Throws<InvalidOperationException>(() => cache.FindParsedTable(second));
    }

    [Test]
    public void FaceOwnsCacheWithoutChangingRawTableBehavior()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("name", new byte[] { 1, 0, 6 })
            .Build();
        FontFace face = FontLoader.Load(
            font,
            FontLoadOptions.CreateInspectOnly()).GetFace(0);
        SfntTableCache cache = face.GetTableCache();

        Assert.Multiple(() =>
        {
            Assert.That(cache.GetRawTableStore(), Is.SameAs(face.GetTableStore()));
            Assert.That(cache.GetRawTableCount(), Is.EqualTo(face.GetTableCount()));
            Assert.That(cache.FindRawTable(FontTags.Name)!.CopyRawData(), Is.EqualTo(
                face.CopyTable(FontTags.Name)));
        });
    }

    [Test]
    public void CacheAndParserValidateArguments()
    {
        SfntTableStore store = CreateStore(new byte[] { 1, 0 });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SfntTableCache.Create(
                null!,
                FontLoadMode.Compatible));
            Assert.Throws<ArgumentOutOfRangeException>(() => SfntTableCache.Create(
                store,
                (FontLoadMode)99));
            Assert.Throws<ArgumentNullException>(() => SfntTableCache.Create(
                store,
                FontLoadMode.Compatible,
                null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfntTableVersion(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfntTableVersion(0, -1));
            Assert.Throws<ArgumentNullException>(() => new SfntTableParser(
                FontTags.Name,
                1,
                1,
                0,
                null!,
                (table, version, support, limits, cache) => new ParsedMarker(0)));
            Assert.Throws<ArgumentNullException>(() => new SfntTableParser(
                FontTags.Name,
                1,
                1,
                0,
                ReadByteVersion,
                null!));
        });
    }

    static SfntTableParser CreateCountingParser(
        int minimumMajor,
        int maximumMajor,
        int maximumMinor,
        Action onParse)
    {
        return new SfntTableParser(
            FontTags.Name,
            minimumMajor,
            maximumMajor,
            maximumMinor,
            ReadByteVersion,
            (table, version, support, limits, cache) =>
            {
                onParse();
                return new ParsedMarker(table.GetDataSlice().GetMemory().Span[2]);
            });
    }

    static SfntTableVersion ReadByteVersion(SfntTableData table)
    {
        ReadOnlySpan<byte> bytes = table.GetDataSlice().GetMemory().Span;
        return new SfntTableVersion(bytes[0], bytes[1]);
    }

    static SfntTableCache CreateCache(byte[] tableData, FontLoadMode mode)
    {
        return SfntTableCache.Create(CreateStore(tableData), mode);
    }

    static SfntTableStore CreateStore(byte[] tableData)
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("name", tableData)
            .Build();
        FontData data = FontData.Create(font);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        return SfntTableStoreBuilder.Create(
            data,
            directory,
            FontLoadOptions.CreateCompatible(),
            0);
    }

    class ParsedMarker
    {
        int value;

        internal ParsedMarker(int value)
        {
            this.value = value;
        }

        internal int GetValue()
        {
            return value;
        }
    }
}
