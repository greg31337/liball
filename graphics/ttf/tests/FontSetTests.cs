using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class FontSetTests
{
    [Test]
    public void UsesTheCommonFaceCollectionContractForSnapshotsAndIngestion()
    {
        FontPackage package = FontLoader.Load(
            BuildTwoFaceWoff2Collection(),
            FontLoadOptions.CreateCompatible());
        IFontFaceCollection packageView = package;
        TestFontFaceCollection source = new TestFontFaceCollection(
            new FontFace[] { packageView.GetFace(1), packageView.GetFace(0) });
        FontSet set = new FontSet();

        int added = set.AddCollection(source);
        int repeated = set.AddCollection(source);
        IFontFaceCollection setView = set;

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.EqualTo(2));
            Assert.That(repeated, Is.Zero);
            Assert.That(source.GetCountCallCount(), Is.EqualTo(2));
            Assert.That(source.GetFaceCallCount(), Is.EqualTo(4));
            Assert.That(setView.GetFaceCount(), Is.EqualTo(2));
            Assert.That(setView.GetFace(0), Is.SameAs(package.GetFace(1)));
            Assert.That(setView.GetFace(1), Is.SameAs(package.GetFace(0)));
            Assert.That(package, Is.InstanceOf<IFontFaceCollection>());
            Assert.That(set, Is.InstanceOf<IFontFaceCollection>());
            Assert.That(typeof(IFontFaceCollection).IsAssignableFrom(
                typeof(FontFamily)), Is.True);
        });
    }

    [Test]
    public void DoesNotPartiallyAddAnInvalidFaceCollection()
    {
        FontPackage package = FontLoader.Load(
            BuildTwoFaceWoff2Collection(),
            FontLoadOptions.CreateCompatible());
        FontSet set = new FontSet();
        TestFontFaceCollection source = new TestFontFaceCollection(
            new FontFace[] { package.GetFace(0), null! });

        Assert.Throws<ArgumentException>(() => set.AddCollection(source));
        Assert.That(set.GetFaceCount(), Is.Zero);
    }

    [Test]
    public void AddsLoadedFacesAndPackagesOnceInStableOrder()
    {
        FontPackage collection = FontLoader.Load(
            BuildTwoFaceWoff2Collection(),
            FontLoadOptions.CreateCompatible());
        FontPackage reloaded = FontLoader.Load(
            BuildTwoFaceWoff2Collection(),
            FontLoadOptions.CreateCompatible());
        FontSet set = new FontSet();

        int firstAdded = set.AddPackage(collection);
        int repeatedAdded = set.AddPackage(collection);
        bool faceAdded = set.Add(collection.GetFace(0));
        int reloadedAdded = set.AddPackage(reloaded);

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.EqualTo(2));
            Assert.That(repeatedAdded, Is.Zero);
            Assert.That(faceAdded, Is.False);
            Assert.That(reloadedAdded, Is.Zero);
            Assert.That(set.GetFaceCount(), Is.EqualTo(2));
            Assert.That(set.GetFace(0), Is.SameAs(collection.GetFace(0)));
            Assert.That(set.GetFace(1), Is.SameAs(collection.GetFace(1)));
            Assert.That(set.GetFace(0).CopyTable(FontTags.Name),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(set.GetFace(1).CopyTable(FontTags.Name),
                Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
        });
    }

    [Test]
    public void DetectsContentDuplicatesWithoutRawPreservation()
    {
        byte[] source = BuildValidSfnt(new byte[] { 7, 8, 9 });
        FontPackage preserved = FontLoader.Load(source, FontLoadOptions.CreateCompatible());
        FontPackage unpreserved = FontLoader.Load(source, WithoutRawPreservationOptions());
        FontSet set = new FontSet();

        bool firstAdded = set.Add(preserved.GetFace(0));
        bool duplicateAdded = set.Add(unpreserved.GetFace(0));

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.True);
            Assert.That(duplicateAdded, Is.False);
            Assert.That(set.GetFaceCount(), Is.EqualTo(1));
            Assert.That(set.GetFace(0), Is.SameAs(preserved.GetFace(0)));
            Assert.Throws<InvalidOperationException>(
                () => unpreserved.GetFace(0).CopyTable(FontTags.Name));
        });
    }

    [Test]
    public void IgnoresHeadChecksumAdjustmentWhenDetectingDuplicates()
    {
        byte[] source = BuildValidSfnt(new byte[] { 3, 1, 4 });
        byte[] adjusted = source.ToArray();
        int headOffset = FindTableOffset(adjusted, FontTags.Head);
        adjusted[headOffset + 8] ^= 0x5A;
        FontSet set = new FontSet();
        FontFace first = FontLoader.Load(source, FontLoadOptions.CreateCompatible()).GetFace(0);
        FontFace second = FontLoader.Load(adjusted, FontLoadOptions.CreateCompatible()).GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(set.Add(first), Is.True);
            Assert.That(set.Add(second), Is.False);
            Assert.That(set.GetFaceCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AddsFilesAndDirectoryFontsInOrdinalPathOrder()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            byte[] firstFont = BuildValidSfnt(new byte[] { 1 });
            byte[] secondFont = BuildValidSfnt(new byte[] { 2 });
            string directPath = Path.Combine(directory, "direct.ttf");
            File.WriteAllBytes(directPath, firstFont);
            FontSet directSet = new FontSet();

            Assert.That(directSet.AddFile(directPath), Is.EqualTo(1));
            Assert.That(directSet.AddFileWithOptions(
                directPath,
                FontLoadOptions.CreateCompatible()), Is.Zero);

            File.Delete(directPath);
            File.WriteAllBytes(Path.Combine(directory, "z.TTF"), secondFont);
            File.WriteAllBytes(Path.Combine(directory, "A.otf"), firstFont);
            File.WriteAllBytes(Path.Combine(directory, "m.woff"), firstFont);
            File.WriteAllBytes(Path.Combine(directory, "ignored.txt"), new byte[] { 9, 8, 7 });
            FontSet directorySet = new FontSet();

            int added = directorySet.AddDirectoryWithOptions(
                directory,
                FontLoadOptions.CreateCompatible());
            int repeated = directorySet.AddDirectory(directory);

            Assert.Multiple(() =>
            {
                Assert.That(added, Is.EqualTo(2));
                Assert.That(repeated, Is.Zero);
                Assert.That(directorySet.GetFaceCount(), Is.EqualTo(2));
                Assert.That(directorySet.GetFace(0).CopyTable(FontTags.Name),
                    Is.EqualTo(new byte[] { 1 }));
                Assert.That(directorySet.GetFace(1).CopyTable(FontTags.Name),
                    Is.EqualTo(new byte[] { 2 }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DoesNotPartiallyAddADirectoryWhenFontLoadingFails()
    {
        string directory = CreateTemporaryDirectory();

        try
        {
            File.WriteAllBytes(
                Path.Combine(directory, "a.ttf"),
                BuildValidSfnt(new byte[] { 1 }));
            File.WriteAllBytes(Path.Combine(directory, "z.ttf"), new byte[] { 0, 1, 2 });
            FontSet set = new FontSet();

            Assert.Throws<FontFormatException>(() => set.AddDirectory(directory));
            Assert.That(set.GetFaceCount(), Is.Zero);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RejectsNullInputsEmptyPathsAndInvalidIndexes()
    {
        FontSet set = new FontSet();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => set.Add((FontFace?)null!));
            Assert.Throws<ArgumentNullException>(
                () => set.AddPackage((FontPackage?)null!));
            Assert.Throws<ArgumentNullException>(
                () => set.AddCollection((IFontFaceCollection?)null!));
            Assert.Throws<ArgumentNullException>(() => set.AddFile(null!));
            Assert.Throws<ArgumentNullException>(
                () => set.AddFileWithOptions(
                    "font.ttf",
                    (FontLoadOptions?)null!));
            Assert.Throws<ArgumentException>(() => set.AddFile(string.Empty));
            Assert.Throws<ArgumentNullException>(() => set.AddDirectory(null!));
            Assert.Throws<ArgumentNullException>(
                () => set.AddDirectoryWithOptions(
                    "fonts",
                    (FontLoadOptions?)null!));
            Assert.Throws<ArgumentException>(() => set.AddDirectory(string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => set.GetFace(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => set.GetFace(0));
        });
    }

    class TestFontFaceCollection : IFontFaceCollection
    {
        FontFace[]? faces;
        int countCallCount;
        int faceCallCount;

        internal TestFontFaceCollection(FontFace[]? faces)
        {
            this.faces = faces;
        }

        public int GetFaceCount()
        {
            countCallCount += 1;
            return faces!.Length;
        }

        public FontFace GetFace(int index)
        {
            faceCallCount += 1;
            return faces![index];
        }

        internal int GetCountCallCount()
        {
            return countCallCount;
        }

        internal int GetFaceCallCount()
        {
            return faceCallCount;
        }
    }

    static byte[] BuildValidSfnt(byte[] nameData)
    {
        return new TinySfntBuilder()
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", nameData)
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .Build();
    }

    static byte[] BuildTwoFaceWoff2Collection()
    {
        return new Woff2Builder()
            .AddKnownTable(0, new byte[4])
            .AddKnownTable(1, new byte[12])
            .AddKnownTable(2, new byte[4])
            .AddKnownTable(3, new byte[4])
            .AddKnownTable(4, new byte[4])
            .AddKnownTable(5, new byte[] { 1, 2, 3 })
            .AddKnownTable(6, new byte[4])
            .AddKnownTable(7, new byte[4])
            .AddKnownTable(10, new byte[4])
            .AddKnownTable(11, new byte[4])
            .AddKnownTable(5, new byte[] { 4, 5, 6, 7 })
            .AddCollectionFace(0x00010000U, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9)
            .AddCollectionFace(0x00010000U, 0, 1, 2, 3, 4, 10, 6, 7, 8, 9)
            .Build();
    }

    static int FindTableOffset(byte[] sfnt, int tag)
    {
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(sfnt.AsSpan(4, 2));

        for (int index = 0; index < tableCount; index += 1)
        {
            int recordOffset = 12 + index * 16;
            int recordTag = BinaryPrimitives.ReadInt32BigEndian(sfnt.AsSpan(recordOffset, 4));

            if (recordTag == tag)
            {
                return System.Convert.ToInt32(
                    BinaryPrimitives.ReadUInt32BigEndian(
                        sfnt.AsSpan(recordOffset + 8, 4)));
            }
        }

        throw new InvalidOperationException("The table was not found.");
    }

    static FontLoadOptions WithoutRawPreservationOptions()
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            false,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "ttf-font-set-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
