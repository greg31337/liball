using System.IO;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataReaderInterfaceTests
{
    [Test]
    public void DefaultReader_ImplementsInterfaceAndRetainsReaderBehavior()
    {
        IImageMetadataReader reader = new DefaultImageMetadataReader();
        MemoryStream stream = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        ImageMetadataDocument document = reader.ReadStream(stream);

        Assert.That(document.Format.Kind, Is.EqualTo(ImageFormatKind.Png));
        Assert.That(document.Diagnostics[0].Code, Is.EqualTo("png.chunk.header.truncated"));
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void Consumer_CanUseTestDoubleWithoutImageOrFileInput()
    {
        FakeImageMetadataReader reader = new FakeImageMetadataReader(ImageFormatKind.Avif);
        MetadataReadConsumer consumer = new MetadataReadConsumer(reader);

        ImageFormatKind kind = consumer.ReadKind(new MemoryStream());

        Assert.That(kind, Is.EqualTo(ImageFormatKind.Avif));
        Assert.That(reader.ReadStreamCallCount, Is.EqualTo(1));
    }
}

public class MetadataReadConsumer
{
    public IImageMetadataReader Reader;

    public MetadataReadConsumer(IImageMetadataReader reader)
    {
        Reader = reader;
    }

    public ImageFormatKind ReadKind(Stream stream)
    {
        return Reader.ReadStream(stream).Format.Kind;
    }
}

public class FakeImageMetadataReader : IImageMetadataReader
{
    public ImageFormatKind Kind;
    public int ReadStreamCallCount;

    public FakeImageMetadataReader(ImageFormatKind kind)
    {
        Kind = kind;
        ReadStreamCallCount = 0;
    }

    public ImageMetadataDocument ReadFile(string? path)
    {
        return CreateDocument();
    }

    public ImageMetadataDocument ReadFileWithOptions(string? path, ImageMetadataReaderOptions? options)
    {
        return CreateDocument();
    }

    public ImageMetadataDocument ReadStream(Stream? stream)
    {
        ReadStreamCallCount = ReadStreamCallCount + 1;
        return CreateDocument();
    }

    public ImageMetadataDocument ReadStreamWithOptions(Stream? stream, ImageMetadataReaderOptions? options)
    {
        ReadStreamCallCount = ReadStreamCallCount + 1;
        return CreateDocument();
    }

    public ImageMetadataDocument CreateDocument()
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format.Kind = Kind;
        return document;
    }
}
