using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPackedFieldsTests
{
    [Test]
    public void LogicalScreenPackedFields_ExtractFlagsAndSize()
    {
        GifPackedFields fields = new GifPackedFields();

        Assert.That(fields.HasLogicalScreenGlobalColorTable(0xF7), Is.True);
        Assert.That(fields.ExtractLogicalScreenColorResolution(0xF7), Is.EqualTo(8));
        Assert.That(fields.HasLogicalScreenSortFlag(0xF7), Is.False);
        Assert.That(fields.ExtractLogicalScreenColorTableSizeCode(0xF7), Is.EqualTo(7));
        Assert.That(fields.ExtractLogicalScreenColorTableSize(0xF7), Is.EqualTo(256));
    }

    [Test]
    public void LogicalScreenPackedFields_ConstructExpectedBytes()
    {
        GifPackedFields fields = new GifPackedFields();

        Assert.That(fields.CreateLogicalScreenPackedFields(true, 8, false, 256), Is.EqualTo(0xF7));
        Assert.That(fields.CreateLogicalScreenPackedFields(true, 1, true, 2), Is.EqualTo(0x88));
        Assert.That(fields.CreateLogicalScreenPackedFields(false, 0, false, 0), Is.EqualTo(0));
    }

    [Test]
    public void ImagePackedFields_ExtractFlagsAndSize()
    {
        GifPackedFields fields = new GifPackedFields();
        int packedFields = 0xE4;

        Assert.That(fields.HasImageLocalColorTable(packedFields), Is.True);
        Assert.That(fields.HasImageInterlaceFlag(packedFields), Is.True);
        Assert.That(fields.HasImageSortFlag(packedFields), Is.True);
        Assert.That(fields.ExtractImageColorTableSizeCode(packedFields), Is.EqualTo(4));
        Assert.That(fields.ExtractImageColorTableSize(packedFields), Is.EqualTo(32));
    }

    [Test]
    public void ImagePackedFields_ConstructExpectedBytes()
    {
        GifPackedFields fields = new GifPackedFields();

        Assert.That(fields.CreateImagePackedFields(true, true, true, 32), Is.EqualTo(0xE4));
        Assert.That(fields.CreateImagePackedFields(false, true, false, 0), Is.EqualTo(0x40));
    }

    [Test]
    public void GraphicControlPackedFields_ExtractFlagsAndDisposal()
    {
        GifPackedFields fields = new GifPackedFields();
        int packedFields = 0x0F;

        Assert.That(fields.ExtractGraphicControlDisposalMethod(packedFields), Is.EqualTo(3));
        Assert.That(fields.HasGraphicControlUserInputFlag(packedFields), Is.True);
        Assert.That(fields.HasGraphicControlTransparentColorFlag(packedFields), Is.True);
    }

    [Test]
    public void GraphicControlPackedFields_ConstructExpectedBytes()
    {
        GifPackedFields fields = new GifPackedFields();

        Assert.That(fields.CreateGraphicControlPackedFields(3, true, true), Is.EqualTo(0x0F));
        Assert.That(fields.CreateGraphicControlPackedFields(7, false, false), Is.EqualTo(0x1C));
    }

    [Test]
    public void PackedFieldHelpers_RejectUnencodableValues()
    {
        GifPackedFields fields = new GifPackedFields();

        Assert.That(fields.ComputeColorTableSizeCode(3), Is.EqualTo(-1));
        Assert.That(fields.ComputeColorTableSizeFromCode(8), Is.EqualTo(-1));
        Assert.That(fields.CreateLogicalScreenPackedFields(true, 9, false, 2), Is.EqualTo(-1));
        Assert.That(fields.CreateLogicalScreenPackedFields(true, 8, false, 3), Is.EqualTo(-1));
        Assert.That(fields.CreateImagePackedFields(true, false, false, 3), Is.EqualTo(-1));
        Assert.That(fields.CreateGraphicControlPackedFields(8, false, false), Is.EqualTo(-1));
    }
}
