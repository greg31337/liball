using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageInterfaceTests
{
    [Test]
    public void Open_DelegatesArchiveReadingThroughInterface()
    {
        PptxPackage source = PptxMinimumPresentation.CreatePackage();
        FakePackageReader reader = new FakePackageReader(source.BuildZipEntries());

        using MemoryStream stream = new MemoryStream();
        PptxPackage opened = PptxPackage.Open(stream, PptxZipReadOptions.CreateDefault(), reader);

        Assert.That(reader.ReadCount, Is.EqualTo(1));
        Assert.That(opened.HasPart(PptxPartNames.Presentation), Is.True);
        Assert.That(opened.Validate().Succeeded(), Is.True, opened.Validate().FormatDiagnostics());
    }

    [Test]
    public void Save_DelegatesArchiveWritingThroughInterface()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        FakePackageWriter writer = new FakePackageWriter(false);

        using MemoryStream stream = new MemoryStream();
        package.SaveWithWriter(stream, writer);

        Assert.That(writer.WriteCount, Is.EqualTo(1));
        Assert.That(writer.Entries, Is.Not.Null);
        Assert.That(writer.Entries.Count, Is.GreaterThan(0));
        Assert.That(package.RawEntryCount(), Is.EqualTo(writer.Entries.Count));
        Assert.That(package.IsDirty, Is.False);
    }

    [Test]
    public void Save_PropagatesWriterDiagnosticsWithoutCleaningPackage()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        FakePackageWriter writer = new FakePackageWriter(true);

        using MemoryStream stream = new MemoryStream();
        System.ArgumentException exception = Assert.Throws<System.ArgumentException>(() => package.SaveWithWriter(stream, writer))!;

        Assert.That(writer.WriteCount, Is.EqualTo(1));
        Assert.That(exception.Message, Does.Contain("TEST001"));
        Assert.That(package.IsDirty, Is.True);
    }

    class FakePackageReader : IPptxPackageReader
    {
        public List<PptxZipEntry> Entries;
        public int ReadCount;

        public FakePackageReader(List<PptxZipEntry> entries)
        {
            Entries = entries;
            ReadCount = 0;
        }

        public PptxZipReadResult Read(Stream stream, PptxZipReadOptions options)
        {
            ReadCount = ReadCount + 1;
            PptxZipReadResult result = new PptxZipReadResult();
            for (int index = 0; index < Entries.Count; index++)
            {
                result.AddEntry(Entries[index]);
            }

            return result;
        }
    }

    class FakePackageWriter : IPptxPackageWriter
    {
        public bool Fail;
        public int WriteCount;
        public List<PptxZipEntry> Entries;

        public FakePackageWriter(bool fail)
        {
            Fail = fail;
            WriteCount = 0;
            Entries = null!;
        }

        public PptxResult Write(Stream stream, List<PptxZipEntry> entries, PptxZipWriteOptions options)
        {
            WriteCount = WriteCount + 1;
            Entries = entries;
            PptxResult result = new PptxResult();
            if (Fail)
            {
                result.AddError("TEST001", "Synthetic writer failure.", "", "", "");
            }

            return result;
        }
    }
}
