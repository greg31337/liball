using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifConstantsTests
{
    [Test]
    public void HeaderConstants_MatchGifSignaturesAndVersions()
    {
        Assert.That(GifConstants.Signature, Is.EqualTo("GIF"));
        Assert.That(GifConstants.Version87a, Is.EqualTo("87a"));
        Assert.That(GifConstants.Version89a, Is.EqualTo("89a"));
        Assert.That(GifConstants.Header87a, Is.EqualTo("GIF87a"));
        Assert.That(GifConstants.Header89a, Is.EqualTo("GIF89a"));
        Assert.That(GifConstants.HeaderSize, Is.EqualTo(6));
        Assert.That(GifConstants.SignatureByteG, Is.EqualTo((byte)'G'));
        Assert.That(GifConstants.SignatureByteI, Is.EqualTo((byte)'I'));
        Assert.That(GifConstants.SignatureByteF, Is.EqualTo((byte)'F'));
        Assert.That(GifConstants.VersionByte8, Is.EqualTo((byte)'8'));
        Assert.That(GifConstants.Version87Byte7, Is.EqualTo((byte)'7'));
        Assert.That(GifConstants.Version89Byte9, Is.EqualTo((byte)'9'));
        Assert.That(GifConstants.VersionByteA, Is.EqualTo((byte)'a'));
    }

    [Test]
    public void BlockAndExtensionLabels_MatchGifByteValues()
    {
        Assert.That(GifConstants.ImageSeparator, Is.EqualTo(0x2C));
        Assert.That(GifConstants.ExtensionIntroducer, Is.EqualTo(0x21));
        Assert.That(GifConstants.Trailer, Is.EqualTo(0x3B));
        Assert.That(GifConstants.BlockTerminator, Is.EqualTo(0));
        Assert.That(GifConstants.MaximumDataSubBlockSize, Is.EqualTo(255));
        Assert.That(GifConstants.GraphicControlExtensionLabel, Is.EqualTo(0xF9));
        Assert.That(GifConstants.CommentExtensionLabel, Is.EqualTo(0xFE));
        Assert.That(GifConstants.PlainTextExtensionLabel, Is.EqualTo(0x01));
        Assert.That(GifConstants.ApplicationExtensionLabel, Is.EqualTo(0xFF));
        Assert.That(GifConstants.NetscapeLoopExtensionPayloadSize, Is.EqualTo(3));
        Assert.That(GifConstants.NetscapeLoopExtensionSubBlockId, Is.EqualTo(0x01));
        Assert.That(GifConstants.NetscapeLoopApplicationIdentifier, Is.EqualTo("NETSCAPE"));
        Assert.That(GifConstants.NetscapeLoopAuthenticationCode, Is.EqualTo("2.0"));
        Assert.That(GifConstants.AnimextsLoopApplicationIdentifier, Is.EqualTo("ANIMEXTS"));
        Assert.That(GifConstants.AnimextsLoopAuthenticationCode, Is.EqualTo("1.0"));
    }

    [Test]
    public void PackedFieldMasks_MatchGifLayout()
    {
        Assert.That(GifConstants.LogicalScreenGlobalColorTableFlagMask, Is.EqualTo(0x80));
        Assert.That(GifConstants.LogicalScreenColorResolutionMask, Is.EqualTo(0x70));
        Assert.That(GifConstants.LogicalScreenSortFlagMask, Is.EqualTo(0x08));
        Assert.That(GifConstants.LogicalScreenColorTableSizeMask, Is.EqualTo(0x07));
        Assert.That(GifConstants.ImageLocalColorTableFlagMask, Is.EqualTo(0x80));
        Assert.That(GifConstants.ImageInterlaceFlagMask, Is.EqualTo(0x40));
        Assert.That(GifConstants.ImageSortFlagMask, Is.EqualTo(0x20));
        Assert.That(GifConstants.ImageColorTableSizeMask, Is.EqualTo(0x07));
        Assert.That(GifConstants.GraphicControlDisposalMethodMask, Is.EqualTo(0x1C));
        Assert.That(GifConstants.GraphicControlUserInputFlagMask, Is.EqualTo(0x02));
        Assert.That(GifConstants.GraphicControlTransparentColorFlagMask, Is.EqualTo(0x01));
        Assert.That(GifConstants.GraphicControlDisposalMethodShift, Is.EqualTo(2));
    }

    [Test]
    public void PaletteAndLzwLimits_MatchGifFormatBounds()
    {
        Assert.That(GifConstants.MinimumColorTableSize, Is.EqualTo(2));
        Assert.That(GifConstants.MaximumColorTableSize, Is.EqualTo(256));
        Assert.That(GifConstants.ColorTableEntrySize, Is.EqualTo(3));
        Assert.That(GifConstants.MinimumLzwMinimumCodeSize, Is.EqualTo(2));
        Assert.That(GifConstants.MaximumLzwMinimumCodeSize, Is.EqualTo(8));
        Assert.That(GifConstants.MaximumLzwCodeSize, Is.EqualTo(12));
        Assert.That(GifConstants.MaximumLzwTableSize, Is.EqualTo(4096));
    }

    [Test]
    public void DefaultSafetyLimits_ArePositiveAndInternallyConsistent()
    {
        Assert.That(GifConstants.DefaultMaxWidth, Is.GreaterThan(0));
        Assert.That(GifConstants.DefaultMaxHeight, Is.GreaterThan(0));
        Assert.That(GifConstants.DefaultMaxFrames, Is.GreaterThan(0));
        Assert.That(GifConstants.DefaultMaxBytes, Is.GreaterThan(0));
        Assert.That(GifConstants.DefaultMaxSubBlocks, Is.GreaterThan(0));
        Assert.That(GifConstants.DefaultMaxSubBlockBytes, Is.GreaterThan(GifConstants.MaximumDataSubBlockSize));
        Assert.That(GifConstants.DefaultMaxDecodedPixels, Is.LessThanOrEqualTo(GifConstants.DefaultMaxWidth * GifConstants.DefaultMaxHeight));
        Assert.That(GifConstants.DefaultMaxMemoryBytes, Is.GreaterThanOrEqualTo(GifConstants.DefaultMaxBytes));
    }
}
