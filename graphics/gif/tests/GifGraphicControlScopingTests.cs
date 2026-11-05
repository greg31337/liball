using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifGraphicControlScopingTests
{
    [Test]
    public void Read_AttachesGraphicControlToNextImageAfterSpecialPurposeExtensions()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 4, 9, 5, 0, 1, 0,
            0x21, 0xFE, 1, 99, 0,
            0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49, 0,
            0x21, 0x99, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x2C, 1, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 88, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExtensionCount, Is.EqualTo(4));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(2));
        Assert.That(result.Document.Frames[0].GraphicControl.DelayHundredths, Is.EqualTo(5));
        Assert.That(result.Document.Frames[0].GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(result.Document.Frames[0].GraphicControl.HasTransparency, Is.True);
        Assert.That(result.Document.Frames[0].GraphicControl.TransparentColorIndex, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].DelayHundredths, Is.EqualTo(5));
        Assert.That(result.Document.Frames[0].DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(result.Document.Frames[0].HasTransparency, Is.True);
        Assert.That(result.Document.Frames[0].TransparentColorIndex, Is.EqualTo(1));
        Assert.That(result.Document.Frames[1].GraphicControl.DelayHundredths, Is.EqualTo(0));
        Assert.That(result.Document.Frames[1].DelayHundredths, Is.EqualTo(0));
        Assert.That(result.Document.Frames[1].TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(result.HasPendingGraphicControl, Is.False);
    }

    [Test]
    public void Read_AttachesGraphicControlToPlainTextAndDoesNotLeakToImage()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 4, 4, 7, 0, 0, 0,
            0x21, 0x01, 12, 0, 0, 0, 0, 1, 0, 1, 0, 8, 8, 1, 0, 1, 65, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(result.Document.PlainTextExtensions[0].GraphicControl.DelayHundredths, Is.EqualTo(7));
        Assert.That(result.Document.PlainTextExtensions[0].GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.DoNotDispose));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].GraphicControl.DelayHundredths, Is.EqualTo(0));
        Assert.That(result.Document.Frames[0].DelayHundredths, Is.EqualTo(0));
        Assert.That(result.HasPendingGraphicControl, Is.False);
    }

    [Test]
    public void Read_ReplacesPendingGraphicControlWhenAnotherGraphicControlAppearsBeforeImage()
    {
        byte[] data = new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0, 0, 0,
            0x21, 0xF9, 4, 0, 1, 0, 0, 0,
            0x21, 0xF9, 4, 0, 9, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 1, 77, 0,
            0x3B
        };
        GifBlockReader reader = new GifBlockReader();

        GifBlockReadResult result = reader.Read(data);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ExtensionCount, Is.EqualTo(2));
        Assert.That(result.Document.Frames.Length, Is.EqualTo(1));
        Assert.That(result.Document.Frames[0].GraphicControl.DelayHundredths, Is.EqualTo(9));
        Assert.That(result.Document.Frames[0].DelayHundredths, Is.EqualTo(9));
        Assert.That(result.HasPendingGraphicControl, Is.False);
    }
}
