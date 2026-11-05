using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class FontLoaderTests
{
    [Test]
    public void LoadsEveryMemoryOverloadAndCopiesArrayMemoryAndSpanInputs()
    {
        byte[] font = BuildValidSfnt();
        byte[] expected = font.ToArray();
        FontPackage arrayPackage = FontLoader.Load(font, RetainOptions());
        FontPackage memoryPackage = FontLoader.Load(
            new ReadOnlyMemory<byte>(font),
            CompatibleOptions());
        FontPackage spanPackage = FontLoader.Load(
            font.AsSpan(),
            RetainOptions());
        int mutationOffset = font.Length - 1;
        font[mutationOffset] ^= 0xFF;

        Assert.Multiple(() =>
        {
            Assert.That(arrayPackage.GetLoadedSourceKind(), Is.EqualTo(FontContainerKind.Sfnt));
            Assert.That(arrayPackage.GetLoadedFaceCount(), Is.EqualTo(1));
            Assert.That(arrayPackage.CopyOriginalData(), Is.EqualTo(expected));
            Assert.That(memoryPackage.CopyOriginalData(), Is.EqualTo(expected));
            Assert.That(spanPackage.CopyOriginalData(), Is.EqualTo(expected));
            Assert.That(arrayPackage.CopySfntData(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void RetainsOnlyReadOnlyMemoryWhenTrustedRetentionIsSelected()
    {
        byte[] retainedBytes = BuildValidSfnt();
        FontPackage retained = FontLoader.Load(
            new ReadOnlyMemory<byte>(retainedBytes),
            RetainOptions());
        int mutationOffset = retainedBytes.Length - 1;
        retainedBytes[mutationOffset] = 0x7D;

        Assert.That(retained.CopyOriginalData()[mutationOffset], Is.EqualTo(0x7D));
    }

    [Test]
    public void ReadsSeekableStreamFromCurrentPositionWithoutClosingOrRewinding()
    {
        byte[] font = BuildValidSfnt();
        byte[] prefixed = new byte[font.Length + 3];
        prefixed[0] = 9;
        prefixed[1] = 8;
        prefixed[2] = 7;
        font.CopyTo(prefixed, 3);
        MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 3;

        FontPackage package = FontLoader.Load(stream, CompatibleOptions());

        Assert.Multiple(() =>
        {
            Assert.That(package.CopyOriginalData(), Is.EqualTo(font));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
            Assert.That(stream.CanRead, Is.True);
            Assert.DoesNotThrow(() => stream.ReadByte());
        });
    }

    [Test]
    public void ReadsNonSeekableStreamWithoutQueryingLengthPositionOrSeeking()
    {
        byte[] font = BuildValidSfnt();
        ForwardOnlyStream stream = new ForwardOnlyStream(font);

        FontPackage package = FontLoader.Load(stream, CompatibleOptions());

        Assert.Multiple(() =>
        {
            Assert.That(package.CopyOriginalData(), Is.EqualTo(font));
            Assert.That(stream.GetBytesRead(), Is.EqualTo(font.Length));
            Assert.That(stream.WasDisposed(), Is.False);
        });
    }

    [Test]
    public void LoadFileCopiesDataAndPropagatesFileSystemErrors()
    {
        byte[] font = BuildValidSfnt();
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, font);
        FontPackage package;

        try
        {
            package = FontLoader.LoadFile(path, CompatibleOptions());
        }
        finally
        {
            File.Delete(path);
        }

        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ttf");

        Assert.Multiple(() =>
        {
            Assert.That(package.CopyOriginalData(), Is.EqualTo(font));
            Assert.Throws<FileNotFoundException>(() => FontLoader.LoadFile(
                missing,
                CompatibleOptions()));
            Assert.Throws<ArgumentException>(() => FontLoader.LoadFile(
                string.Empty,
                CompatibleOptions()));
        });
    }

    [Test]
    public void EnforcesInputLimitIncrementallyAndConsumesOnlyTheOverflowProbe()
    {
        byte[] font = BuildValidSfnt();
        FontLoadOptions exactOptions = OptionsWithInputLimit(font.Length);
        FontPackage exact = FontLoader.Load(new MemoryStream(font), exactOptions);
        MemoryStream oversizedStream = new MemoryStream(font);
        FontLoadOptions shortOptions = OptionsWithInputLimit(font.Length - 1);

        Assert.Multiple(() =>
        {
            Assert.That(exact.GetOriginalDataLength(), Is.EqualTo(font.Length));
            Assert.Throws<FontLimitException>(() => FontLoader.Load(
                oversizedStream,
                shortOptions));
            Assert.That(oversizedStream.Position, Is.EqualTo(font.Length));
            Assert.Throws<FontLimitException>(() => FontLoader.Load(font, shortOptions));
        });
    }

    [Test]
    public void ObservesCancellationBeforeReadingCallerStream()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        MemoryStream stream = new MemoryStream(BuildValidSfnt());

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => FontLoader.Load(
                stream,
                CompatibleOptions(cancellation.Token)));
            Assert.That(stream.Position, Is.Zero);
        });
    }

    [Test]
    public void RejectsInvalidArgumentsUnknownContainersAndUnreadableStreams()
    {
        UnreadableStream unreadable = new UnreadableStream();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => FontLoader.Load(
                (byte[]?)null!,
                CompatibleOptions()));
            Assert.Throws<ArgumentNullException>(() => FontLoader.Load(
                BuildValidSfnt(),
                null!));
            Assert.Throws<ArgumentNullException>(() => FontLoader.Load(
                (Stream?)null!,
                CompatibleOptions()));
            Assert.Throws<ArgumentException>(() => FontLoader.Load(
                unreadable,
                CompatibleOptions()));
            Assert.Throws<FontFormatException>(() => FontLoader.Load(
                Array.Empty<byte>(),
                CompatibleOptions()));
            Assert.Throws<FontFormatException>(() => FontLoader.Load(
                new byte[] { 1, 2, 3, 4 },
                CompatibleOptions()));
            Assert.Throws<FontFormatException>(() => FontLoader.Load(
                new byte[] { 0x80, 0x01 },
                CompatibleOptions()));
        });
    }

    [Test]
    public void EnforcesSfntTableLimitBeforeReadingDirectoryRecords()
    {
        byte[] bytes = new byte[6];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(4, 2), 257);

        Assert.Throws<FontLimitException>(() => FontLoader.Load(
            bytes,
            FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void LoadsWoff1Woff2AndNativeCollectionContainers()
    {
        FontPackage woff1 = FontLoader.Load(BuildValidWoff1(), CompatibleOptions());
        FontPackage woff2 = FontLoader.Load(BuildValidWoff2(false), CompatibleOptions());
        FontPackage woff2Collection = FontLoader.Load(
            BuildValidWoff2(true),
            CompatibleOptions());
        FontPackage collection = FontLoader.Load(
            woff2Collection.CopySfntData(),
            CompatibleOptions());

        Assert.Multiple(() =>
        {
            Assert.That(woff1.GetLoadedSourceKind(), Is.EqualTo(FontContainerKind.Woff1));
            Assert.That(woff1.GetLoadedFaceCount(), Is.EqualTo(1));
            Assert.That(woff2.GetLoadedSourceKind(), Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(woff2.GetLoadedFaceCount(), Is.EqualTo(1));
            Assert.That(woff2Collection.GetLoadedSourceKind(), Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(woff2Collection.GetLoadedFaceCount(), Is.EqualTo(1));
            Assert.That(collection.GetLoadedSourceKind(), Is.EqualTo(FontContainerKind.Collection));
            Assert.That(collection.GetLoadedFaceCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReconstructsTransformedWoff2DuringLoad()
    {
        byte[] transformedGlyf = BuildEmptyTransformedGlyf();
        Woff2Builder builder = AddValidWoff2Tables(new Woff2Builder(), false)
            .AddTransformedKnownTable(10, 0, (uint)transformedGlyf.Length, transformedGlyf)
            .AddTransformedKnownTable(11, 0, 4, Array.Empty<byte>())
            .AddTransformedKnownTable(3, 1, 4, new byte[] { 3, 1, 244 });

        FontPackage package = FontLoader.Load(builder.Build(), CompatibleOptions());
        SfntDirectory directory = SfntDirectoryParser.Parse(
            package.GetSfntData().GetSlice(0, package.GetSfntDataLength()));

        Assert.Multiple(() =>
        {
            Assert.That(package.GetLoadedSourceKind(), Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(directory.GetTableCount(), Is.EqualTo(10));
            Assert.That(FindTableLength(directory, FontTags.Glyf), Is.Zero);
            Assert.That(FindTableLength(directory, FontTags.Loca), Is.EqualTo(4));
            Assert.That(FindTableLength(directory, FontTags.Hmtx), Is.EqualTo(4));
            Assert.That(FontChecksum.VerifyFont(package.CopySfntData()), Is.True);
        });
    }

    [Test]
    public void InspectOnlyReturnsBoundedPackageWithFatalValidationDiagnostics()
    {
        byte[] incomplete = new TinySfntBuilder()
            .AddTable("name", new byte[] { 1 })
            .Build();

        FontPackage package = FontLoader.Load(
            incomplete,
            FontLoadOptions.CreateInspectOnly());

        Assert.Multiple(() =>
        {
            Assert.That(package.GetLoadedFaceCount(), Is.EqualTo(1));
            Assert.That(package.GetLoadedDiagnosticCount(), Is.GreaterThan(0));
            Assert.Throws<FontFormatException>(() => FontLoader.Load(
                incomplete,
                CompatibleOptions()));
        });
    }

    static byte[] BuildValidSfnt()
    {
        return AddValidSfntTables(new TinySfntBuilder())
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .Build();
    }

    static TinySfntBuilder AddValidSfntTables(TinySfntBuilder builder)
    {
        return builder
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 1 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4]);
    }

    static byte[] BuildValidWoff1()
    {
        return new Woff1Builder()
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 1 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .Build();
    }

    static byte[] BuildValidWoff2(bool collection)
    {
        Woff2Builder builder = AddValidWoff2Tables(new Woff2Builder(), true)
            .AddKnownTable(10, new byte[4])
            .AddKnownTable(11, new byte[4])
            .AddKnownTable(3, new byte[4]);

        if (collection)
        {
            builder.AddCollectionFace(0x00010000U, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        return builder.Build();
    }

    static Woff2Builder AddValidWoff2Tables(Woff2Builder builder, bool shortDependencies)
    {
        byte[] head = shortDependencies ? new byte[12] : BuildHead();
        byte[] hhea = shortDependencies ? new byte[4] : BuildHhea();
        byte[] maxp = shortDependencies ? new byte[4] : BuildMaxp();
        return builder
            .AddKnownTable(0, new byte[4])
            .AddKnownTable(1, head)
            .AddKnownTable(2, hhea)
            .AddKnownTable(4, maxp)
            .AddKnownTable(5, new byte[] { 1 })
            .AddKnownTable(6, new byte[4])
            .AddKnownTable(7, new byte[4]);
    }

    static byte[] BuildEmptyTransformedGlyf()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt32(2)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .WriteUInt32(4)
            .WriteUInt32(0)
            .WriteInt16(0)
            .WriteUInt32(0)
            .CopyBytes();
    }

    static byte[] BuildHead()
    {
        byte[] data = new byte[54];
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(50, 2), 0);
        return data;
    }

    static byte[] BuildHhea()
    {
        byte[] data = new byte[36];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(34, 2), 1);
        return data;
    }

    static byte[] BuildMaxp()
    {
        byte[] data = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(4, 2), 1);
        return data;
    }

    static long FindTableLength(SfntDirectory directory, int tag)
    {
        for (int index = 0; index < directory.GetTableCount(); index += 1)
        {
            if (directory.GetTableRecord(index).GetTag() == tag)
            {
                return directory.GetTableRecord(index).GetLength();
            }
        }

        throw new InvalidOperationException("The table was not found.");
    }

    static FontLoadOptions CompatibleOptions()
    {
        return CompatibleOptions(CancellationToken.None);
    }

    static FontLoadOptions CompatibleOptions(CancellationToken cancellationToken)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            cancellationToken);
    }

    static FontLoadOptions RetainOptions()
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.RetainReadOnlyMemory,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    static FontLoadOptions OptionsWithInputLimit(int maximumInput)
    {
        FontLimits limits = FontLimits.Create(
            maximumInput,
            1_000_000,
            10,
            100,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            100,
            100,
            100,
            1_000_000,
            100,
            100,
            10,
            100);
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            limits,
            CancellationToken.None);
    }

    sealed class ForwardOnlyStream : Stream
    {
        byte[] data;
        int position;
        bool disposed;

        internal ForwardOnlyStream(byte[] data)
        {
            this.data = data.ToArray();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = data.Length - position;
            int length = Math.Min(available, count);
            data.AsSpan(position, length).CopyTo(buffer.AsSpan(offset, length));
            position += length;
            return length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            disposed = true;
            base.Dispose(disposing);
        }

        internal int GetBytesRead()
        {
            return position;
        }

        internal bool WasDisposed()
        {
            return disposed;
        }
    }

    sealed class UnreadableStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
