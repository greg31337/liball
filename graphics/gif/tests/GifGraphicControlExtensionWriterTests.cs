using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifGraphicControlExtensionWriterTests
{
    [Test]
    public void Create_WritesGraphicControlExtensionBytes()
    {
        GifGraphicControl control = new GifGraphicControl();
        control.DisposalMethod = GifDisposalMethod.RestoreToPrevious;
        control.UserInputFlag = true;
        control.HasTransparency = true;
        control.DelayHundredths = 0x1234;
        control.TransparentColorIndex = 7;
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        GifGraphicControlExtensionWriteResult result = writer.Create(control);

        Assert.That(result.Success, Is.True);
        Assert.That(result.PackedFields, Is.EqualTo(0x0F));
        Assert.That(result.BytesWritten, Is.EqualTo(8));
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xF9, 4, 0x0F, 0x34, 0x12, 7, 0 }));
    }

    [Test]
    public void Create_WritesReservedDisposalAndInactiveTransparentIndexByte()
    {
        GifGraphicControl control = new GifGraphicControl();
        control.DisposalMethod = GifDisposalMethod.Reserved7;
        control.TransparentColorIndex = 9;
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        GifGraphicControlExtensionWriteResult result = writer.Create(control);

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xF9, 4, 0x1C, 0, 0, 9, 0 }));
    }

    [Test]
    public void Create_WritesDefaultGraphicControlExtension()
    {
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        GifGraphicControlExtensionWriteResult result = writer.Create(new GifGraphicControl());

        Assert.That(result.Success, Is.True);
        Assert.That(result.EncodedData, Is.EqualTo(new byte[] { 0x21, 0xF9, 4, 0, 0, 0, 0, 0 }));
    }

    [Test]
    public void Create_RoundTripsThroughParser()
    {
        GifGraphicControl control = new GifGraphicControl();
        control.DisposalMethod = GifDisposalMethod.RestoreToBackground;
        control.HasTransparency = true;
        control.DelayHundredths = 25;
        control.TransparentColorIndex = 3;
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();
        GifGraphicControlExtensionParser parser = new GifGraphicControlExtensionParser();

        GifGraphicControlExtensionWriteResult writeResult = writer.Create(control);
        GifGraphicControlExtensionReadResult readResult = parser.Parse(writeResult.EncodedData);

        Assert.That(writeResult.Success, Is.True);
        Assert.That(readResult.Success, Is.True);
        Assert.That(readResult.GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.RestoreToBackground));
        Assert.That(readResult.GraphicControl.HasTransparency, Is.True);
        Assert.That(readResult.GraphicControl.DelayHundredths, Is.EqualTo(25));
        Assert.That(readResult.GraphicControl.TransparentColorIndex, Is.EqualTo(3));
    }

    [Test]
    public void Write_WritesToProvidedStreamAndAdvancesOffset()
    {
        MemoryStream stream = new MemoryStream();
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        GifGraphicControlExtensionWriteResult result = writer.Write(byteWriter, new GifGraphicControl());

        Assert.That(result.Success, Is.True);
        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x21, 0xF9, 4, 0, 0, 0, 0, 0 }));
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(8));
    }

    [Test]
    public void Create_RejectsUnencodableValues()
    {
        GifGraphicControl invalidDisposal = new GifGraphicControl();
        GifGraphicControl invalidDelay = new GifGraphicControl();
        GifGraphicControl invalidIndex = new GifGraphicControl();
        GifGraphicControl missingActiveIndex = new GifGraphicControl();
        invalidDisposal.DisposalMethod = (GifDisposalMethod)8;
        invalidDelay.DelayHundredths = 65536;
        invalidIndex.TransparentColorIndex = 256;
        missingActiveIndex.HasTransparency = true;
        missingActiveIndex.TransparentColorIndex = -1;
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        Assert.That(writer.Create(invalidDisposal).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(writer.Create(invalidDelay).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(writer.Create(invalidIndex).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(writer.Create(missingActiveIndex).Error.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
    }

    [Test]
    public void Write_ReturnsFailureForMissingWriter()
    {
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        GifGraphicControlExtensionWriteResult result = writer.Write(null!, new GifGraphicControl());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.ByteLimitExceeded));
    }

    [Test]
    public void Write_ReturnsFailureWhenUnderlyingStreamCannotWrite()
    {
        MemoryStream stream = new MemoryStream(new byte[8], false);
        GifStreamByteWriter byteWriter = new GifStreamByteWriter(stream);
        GifGraphicControlExtensionWriter writer = new GifGraphicControlExtensionWriter();

        GifGraphicControlExtensionWriteResult result = writer.Write(byteWriter, new GifGraphicControl());

        Assert.That(result.Success, Is.False);
        Assert.That(byteWriter.Failed, Is.True);
        Assert.That(byteWriter.ByteOffset, Is.EqualTo(0));
    }
}
