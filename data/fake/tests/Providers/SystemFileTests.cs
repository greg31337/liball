using fake;

namespace fake.Tests;

[TestFixture]
public class SystemFileTests
{
    [Test]
    public void FileFormatTableIsCorrelatedRegisteredAndIndependentlyCopied()
    {
        Fake fake = Fake.Create(11300);
        SystemProvider system = fake.System();
        FileFormatRecord[] formats = system.FileFormats();
        string[] expectedExtensions = new string[]
        {
            "bin", "csv", "gif", "gz", "html", "jpg", "json", "mp3",
            "mp4", "ogg", "pdf", "png", "svg", "txt", "xml", "zip"
        };

        Assert.That(formats, Has.Length.EqualTo(expectedExtensions.Length));
        int index = 0;
        while (index < formats.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(formats[index].Extension,
                    Is.EqualTo(expectedExtensions[index]));
                Assert.That(fake.Internet().IsMimeType(formats[index].MimeType), Is.True);
                Assert.That(formats[index].MimeTypeIsRegistered, Is.True);
                Assert.That(formats[index].RegistryVersion, Is.Not.Empty);
                Assert.That(formats[index].Type, Is.AnyOf(
                    "archive", "audio", "binary", "data", "document", "image",
                    "text", "video"));
            });
            index++;
        }

        string original = formats[0].Extension;
        formats[0].Extension = "changed";
        FileFormatRecord png = system.FileFormatFromExtension(".PNG");
        Assert.Multiple(() =>
        {
            Assert.That(system.FileFormats()[0].Extension, Is.EqualTo(original));
            Assert.That(png.Extension, Is.EqualTo("png"));
            Assert.That(png.MimeType, Is.EqualTo("image/png"));
            Assert.That(png.Type, Is.EqualTo("image"));
        });
    }

    [Test]
    public void ScalarFileApisSelectFromTheSameCorrelatedFormatTable()
    {
        const long seed = 11301;
        FileFormatRecord expectedFormat = Fake.Create(seed).System().FileFormat();
        string extension = Fake.Create(seed).System().FileExtension();
        string alias = Fake.Create(seed).System().Extension();
        string mimeType = Fake.Create(seed).System().MimeType();
        string type = Fake.Create(seed).System().FileType();
        SystemProvider nameParts = Fake.Create(seed).System();
        FileFormatRecord nameFormat = nameParts.FileFormat();
        string baseName = nameParts.FileBaseName();

        Assert.Multiple(() =>
        {
            Assert.That(extension, Is.EqualTo(expectedFormat.Extension));
            Assert.That(alias, Is.EqualTo(expectedFormat.Extension));
            Assert.That(mimeType, Is.EqualTo(expectedFormat.MimeType));
            Assert.That(type, Is.EqualTo(expectedFormat.Type));
            Assert.That(Fake.Create(seed).System().FileName(),
                Is.EqualTo(baseName + "." + nameFormat.Extension));
            Assert.That(Fake.Create(seed).System().FileNameFromExtension(".PDF"),
                Does.EndWith(".pdf"));
        });
    }

    [Test]
    public void FileRecordsAreDeterministicCorrelatedShapeOnlyFixtures()
    {
        const long seed = 11302;
        FileRecord first = Fake.Create(seed).System().FileFromExtension("png");
        FileRecord second = Fake.Create(seed).System().FileFromExtension(".PNG");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.BaseName, Is.EqualTo(first.BaseName));
            Assert.That(second.Extension, Is.EqualTo(first.Extension));
            Assert.That(second.Name, Is.EqualTo(first.Name));
            Assert.That(second.Type, Is.EqualTo(first.Type));
            Assert.That(second.MimeType, Is.EqualTo(first.MimeType));
            Assert.That(second.Size, Is.EqualTo(first.Size));
            Assert.That(second.Checksum, Is.EqualTo(first.Checksum));
            Assert.That(first.Extension, Is.EqualTo("png"));
            Assert.That(first.Name, Is.EqualTo(first.BaseName + ".png"));
            Assert.That(first.Type, Is.EqualTo("image"));
            Assert.That(first.MimeType, Is.EqualTo("image/png"));
            Assert.That(first.Size, Is.InRange(0L, 10000000000L));
            Assert.That(first.Checksum, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(first.ChecksumAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(first.ChecksumIsContentDigest, Is.False);
            Assert.That(first.IsSynthetic, Is.True);
        });
    }

    [Test]
    public void FileSizesAndChecksumShapesAreBoundedAndDeterministic()
    {
        const long seed = 11303;
        SystemProvider first = Fake.Create(seed).System();
        SystemProvider second = Fake.Create(seed).System();

        int index = 0;
        while (index < 256)
        {
            long firstSize = first.FileSize();
            long secondSize = second.FileSize();
            string firstChecksum = first.FileChecksum();
            string secondChecksum = second.FileChecksum();
            Assert.Multiple(() =>
            {
                Assert.That(firstSize, Is.EqualTo(secondSize));
                Assert.That(firstSize, Is.InRange(0L, 10000000000L));
                Assert.That(firstChecksum, Is.EqualTo(secondChecksum));
                Assert.That(firstChecksum, Does.Match("^[0-9a-f]{64}$"));
            });
            index++;
        }
    }

    [Test]
    public void InvalidExtensionsFailWithoutConsumingEntropy()
    {
        const long seed = 11304;
        SystemProvider inspected = Fake.Create(seed).System();
        SystemProvider untouched = Fake.Create(seed).System();

        Assert.Multiple(() =>
        {
            Assert.That(() => inspected.FileNameFromExtension(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => inspected.FileNameFromExtension(""),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.FileNameFromExtension("bad/ext"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.FileFromExtension("unknown"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => inspected.FileFormatFromExtension("."),
                Throws.TypeOf<ArgumentException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    [Test]
    public void FileApisHonorCollectionAndOutputLimitsBeforeEntropy()
    {
        const long seed = 11305;
        FakeOptions formatOptions = FakeOptions.Create(seed);
        formatOptions.MaximumCount = 15;
        FakeOptions baseOptions = FakeOptions.Create(seed);
        baseOptions.MaximumCount = 7;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 63;
        SystemProvider inspected = Fake.Create(outputOptions).System();
        SystemProvider untouched = Fake.Create(outputOptions).System();

        Assert.Multiple(() =>
        {
            Assert.That(() => Fake.Create(formatOptions).System().FileFormat(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => Fake.Create(baseOptions).System().FileBaseName(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.FileChecksum(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => inspected.File(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(inspected.RandomValue.Long(), Is.EqualTo(untouched.RandomValue.Long()));
        });
    }

    [Test]
    public void FileGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(11306);
        Fake untouched = Fake.Create(11306);

        int index = 0;
        while (index < 32)
        {
            inspected.System().FileFormat();
            inspected.System().FileName();
            inspected.System().FileSize();
            inspected.System().FileChecksum();
            inspected.System().File();
            index++;
        }

        Assert.That(inspected.Person().FullName(), Is.EqualTo(untouched.Person().FullName()));
    }
}
