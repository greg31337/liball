using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicIdentifyTests
{
    [Test]
    public void Identify_FromBytesReturnsMetadata()
    {
        GifInfo info = Gif.Identify(CreateIdentifiableGif());

        Assert.That(info.Success, Is.True);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(2));
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.HasTransparency, Is.True);
        Assert.That(info.LoopCount, Is.EqualTo(5));
        Assert.That(info.HasComments, Is.True);
        Assert.That(info.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "NETSCAPE" }));
        Assert.That(info.EstimatedDecodedBytes, Is.EqualTo(12));
    }

    [Test]
    public void Identify_FromStreamReturnsMetadata()
    {
        MemoryStream stream = new MemoryStream(CreateIdentifiableGif());

        GifInfo info = Gif.Identify(stream);

        Assert.That(info.Success, Is.True);
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(info.FrameCount, Is.EqualTo(2));
    }

    [Test]
    public void Identify_HonorsFirstFrameOnlyOption()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.FirstFrameOnly = true;

        GifInfo info = Gif.Identify(CreateIdentifiableGif(), options);

        Assert.That(info.Success, Is.True);
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(info.HasAnimation, Is.False);
    }

    [Test]
    public void Identify_StreamHonorsOptionsWithoutMutatingCallerOptions()
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.MetadataOnly = false;
        options.FirstFrameOnly = true;
        options.PreserveUnknownExtensions = false;
        options.DefaultGlobalColorTable = new byte[] { 1, 2, 3 };
        MemoryStream stream = new MemoryStream(CreateIdentifiableGif());

        GifInfo info = Gif.Identify(stream, options);

        Assert.That(info.Success, Is.True);
        Assert.That(info.FrameCount, Is.EqualTo(1));
        Assert.That(options.MetadataOnly, Is.False);
        Assert.That(options.FirstFrameOnly, Is.True);
        Assert.That(options.PreserveUnknownExtensions, Is.False);
        Assert.That(options.DefaultGlobalColorTable, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Identify_InvalidBytesReturnDiagnostic()
    {
        GifInfo info = Gif.Identify(new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.That(info.Success, Is.False);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.InvalidSignature));
        Assert.That(info.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(info.FrameCount, Is.EqualTo(0));
    }

    static byte[] CreateIdentifiableGif()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x21, 0xFE, 2, 79, 75, 0,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 5, 0, 0,
            0x21, 0xF9, 4, 1, 10, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 2, 0, 1, 0, 0,
            2, 3, 1, 2, 3, 2, 4, 5, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 6, 0,
            0x3B
        };
    }
}
