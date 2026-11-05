using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicRoundTripCoverageTests
{
    [Test]
    public void PublicRoundTripPreservesSemanticMetadataFlagsPalettesAndRawOrder()
    {
        GifCodec codec = new GifCodec();
        IGifDecoder decoder = codec;
        IGifEncoder encoder = codec;
        byte[] source = CreateSemanticMetadataFixture();

        GifDocument decoded = decoder.DecodeBytes(source, null);
        byte[] encoded = encoder.Encode(decoded, null);
        GifDocument reparsed = decoder.DecodeBytes(encoded, null);

        AssertLogicalScreen(reparsed);
        AssertComments(reparsed);
        AssertApplication(reparsed);
        AssertPlainText(reparsed);
        AssertUnknownExtensions(reparsed);
        AssertLocalPalette(reparsed);
        AssertRawOrder(decoded.BlockOrder);
        AssertRawOrder(reparsed.BlockOrder);
        Assert.That(encoded, Is.Not.EqualTo(source), "Semantic round trips may canonicalize data sub-block boundaries.");
    }

    [Test]
    public void PublicRoundTripPreservesEveryReservedDisposalValue()
    {
        GifCodec codec = new GifCodec();
        IGifDecoder decoder = codec;
        IGifEncoder encoder = codec;
        byte[] source = CreateReservedDisposalsFixture();

        GifDocument decoded = decoder.DecodeBytes(source, null);
        byte[] encoded = encoder.Encode(decoded, null);
        GifDocument reparsed = decoder.DecodeBytes(encoded, null);

        Assert.That(decoded.Frames, Has.Length.EqualTo(4));
        Assert.That(reparsed.Frames, Has.Length.EqualTo(4));
        Assert.That(encoded, Is.EqualTo(source));
        int frameIndex = 0;
        while (frameIndex < 4)
        {
            GifDisposalMethod expected = (GifDisposalMethod)(frameIndex + 4);
            Assert.That(decoded.Frames[frameIndex].DisposalMethod, Is.EqualTo(expected));
            Assert.That(decoded.Frames[frameIndex].GraphicControl.DisposalMethod, Is.EqualTo(expected));
            Assert.That(reparsed.Frames[frameIndex].DisposalMethod, Is.EqualTo(expected));
            Assert.That(reparsed.GraphicControls[frameIndex].DisposalMethod, Is.EqualTo(expected));
            AssertRawBlock(reparsed.BlockOrder[frameIndex * 2], GifRawBlockKind.GraphicControl, frameIndex, GifRawBlockScope.Frame, frameIndex);
            AssertRawBlock(reparsed.BlockOrder[frameIndex * 2 + 1], GifRawBlockKind.Image, frameIndex, GifRawBlockScope.Frame, frameIndex);
            frameIndex = frameIndex + 1;
        }

        AssertRawBlock(reparsed.BlockOrder[8], GifRawBlockKind.Trailer, -1, GifRawBlockScope.Trailer, -1);
    }

    [TestCase(-1, 0)]
    [TestCase(0, 1)]
    [TestCase(7, 1)]
    public void PublicRoundTripPreservesAbsentInfiniteAndFiniteLoopSemantics(int loopCount, int expectedApplications)
    {
        GifCodec codec = new GifCodec();
        IGifDecoder decoder = codec;
        IGifEncoder encoder = codec;
        GifBitmap bitmap = codec.CreateIndexedBitmap(1, 1, new byte[] { 0 }, CreatePalette());
        GifEncodeOptions options = new GifEncodeOptions();
        options.LoopCount = loopCount;

        GifEncodeResult encoded = encoder.EncodeIndexedStill(bitmap, options, false, -1);
        GifDocument decoded = decoder.DecodeBytes(encoded.Data, null);
        GifDocument reparsed = decoder.DecodeBytes(encoder.Encode(decoded, null), null);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.LoopCount, Is.EqualTo(loopCount));
        Assert.That(decoded.Metadata.LoopCount, Is.EqualTo(loopCount));
        Assert.That(decoded.ApplicationExtensions, Has.Length.EqualTo(expectedApplications));
        Assert.That(reparsed.LoopCount, Is.EqualTo(loopCount));
        Assert.That(reparsed.Metadata.LoopCount, Is.EqualTo(loopCount));
        Assert.That(reparsed.ApplicationExtensions, Has.Length.EqualTo(expectedApplications));
        if (expectedApplications > 0)
        {
            Assert.That(reparsed.ApplicationExtensions[0].Identifier, Is.EqualTo("NETSCAPE"));
            Assert.That(reparsed.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("2.0"));
            Assert.That(reparsed.ApplicationExtensions[0].IsLoopExtension, Is.True);
            Assert.That(reparsed.ApplicationExtensions[0].LoopCount, Is.EqualTo(loopCount));
        }
    }

    static void AssertLogicalScreen(GifDocument document)
    {
        Assert.That(document.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.LogicalScreen.GlobalColorTableSize, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.ColorResolution, Is.EqualTo(5));
        Assert.That(document.LogicalScreen.SortFlag, Is.True);
        Assert.That(document.LogicalScreen.BackgroundColorIndex, Is.EqualTo(1));
        Assert.That(document.LogicalScreen.PixelAspectRatio, Is.EqualTo(49));
        Assert.That(document.GlobalColorTable.Count, Is.EqualTo(2));
        Assert.That(document.GlobalColorTable.Colors[0].Red, Is.EqualTo(0));
        Assert.That(document.GlobalColorTable.Colors[1].Red, Is.EqualTo(255));
    }

    static void AssertComments(GifDocument document)
    {
        Assert.That(document.Comments, Has.Length.EqualTo(2));
        Assert.That(document.Comments[0].Data, Is.EqualTo(new byte[] { 65, 66, 67 }));
        Assert.That(document.Comments[0].Text, Is.EqualTo("ABC"));
        Assert.That(document.Comments[1].Data, Is.EqualTo(new byte[] { 90 }));
        Assert.That(document.Comments[1].Text, Is.EqualTo("Z"));
        Assert.That(document.Metadata.Comments, Has.Length.EqualTo(2));
    }

    static void AssertApplication(GifDocument document)
    {
        Assert.That(document.ApplicationExtensions, Has.Length.EqualTo(1));
        Assert.That(document.ApplicationExtensions[0].Identifier, Is.EqualTo("APPID123"));
        Assert.That(document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("XYZ"));
        Assert.That(document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(document.ApplicationExtensions[0].IsLoopExtension, Is.False);
        Assert.That(document.ApplicationExtensions[0].LoopCount, Is.EqualTo(-1));
    }

    static void AssertPlainText(GifDocument document)
    {
        Assert.That(document.PlainTextExtensions, Has.Length.EqualTo(1));
        GifPlainTextExtension plainText = document.PlainTextExtensions[0];
        Assert.That(plainText.TextGridLeft, Is.EqualTo(0));
        Assert.That(plainText.TextGridTop, Is.EqualTo(0));
        Assert.That(plainText.TextGridWidth, Is.EqualTo(2));
        Assert.That(plainText.TextGridHeight, Is.EqualTo(1));
        Assert.That(plainText.CharacterCellWidth, Is.EqualTo(8));
        Assert.That(plainText.CharacterCellHeight, Is.EqualTo(9));
        Assert.That(plainText.ForegroundColorIndex, Is.EqualTo(1));
        Assert.That(plainText.BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(plainText.TextData, Is.EqualTo(new byte[] { 80, 84 }));
        Assert.That(plainText.Text, Is.EqualTo("PT"));
        Assert.That(plainText.GraphicControl.DisposalMethod, Is.EqualTo(GifDisposalMethod.Reserved4));
        Assert.That(plainText.GraphicControl.UserInputFlag, Is.True);
        Assert.That(plainText.GraphicControl.HasTransparency, Is.True);
        Assert.That(plainText.GraphicControl.DelayHundredths, Is.EqualTo(0x1234));
        Assert.That(plainText.GraphicControl.TransparentColorIndex, Is.EqualTo(1));
    }

    static void AssertUnknownExtensions(GifDocument document)
    {
        Assert.That(document.UnknownExtensions, Has.Length.EqualTo(2));
        Assert.That(document.UnknownExtensions[0].Label, Is.EqualTo(0x99));
        Assert.That(document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 5, 6, 7 }));
        Assert.That(document.UnknownExtensions[1].Label, Is.EqualTo(0x98));
        Assert.That(document.UnknownExtensions[1].Data, Is.EqualTo(new byte[] { 8 }));
        Assert.That(document.Metadata.UnknownExtensions, Has.Length.EqualTo(2));
    }

    static void AssertLocalPalette(GifDocument document)
    {
        Assert.That(document.Frames, Has.Length.EqualTo(1));
        GifFrame frame = document.Frames[0];
        Assert.That(frame.HasLocalColorTable, Is.True);
        Assert.That(frame.LocalColorTableSortFlag, Is.True);
        Assert.That(frame.LocalColorTableSize, Is.EqualTo(2));
        Assert.That(frame.LocalColorTableSizeCode, Is.EqualTo(0));
        Assert.That(frame.LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(frame.LocalColorTable.Colors[0].Red, Is.EqualTo(255));
        Assert.That(frame.LocalColorTable.Colors[1].Blue, Is.EqualTo(255));
        Assert.That(frame.DisposalMethod, Is.EqualTo(GifDisposalMethod.Reserved5));
    }

    static void AssertRawOrder(GifRawBlock[] order)
    {
        Assert.That(order, Has.Length.EqualTo(10));
        AssertRawBlock(order[0], GifRawBlockKind.Comment, 0, GifRawBlockScope.Document, -1);
        AssertRawBlock(order[1], GifRawBlockKind.Comment, 1, GifRawBlockScope.Document, -1);
        AssertRawBlock(order[2], GifRawBlockKind.Application, 0, GifRawBlockScope.Document, -1);
        AssertRawBlock(order[3], GifRawBlockKind.GraphicControl, 0, GifRawBlockScope.PlainText, 0);
        AssertRawBlock(order[4], GifRawBlockKind.PlainText, 0, GifRawBlockScope.PlainText, 0);
        AssertRawBlock(order[5], GifRawBlockKind.UnknownExtension, 0, GifRawBlockScope.Document, -1);
        AssertRawBlock(order[6], GifRawBlockKind.GraphicControl, 1, GifRawBlockScope.Frame, 0);
        AssertRawBlock(order[7], GifRawBlockKind.Image, 0, GifRawBlockScope.Frame, 0);
        AssertRawBlock(order[8], GifRawBlockKind.UnknownExtension, 1, GifRawBlockScope.Trailer, -1);
        AssertRawBlock(order[9], GifRawBlockKind.Trailer, -1, GifRawBlockScope.Trailer, -1);
    }

    static void AssertRawBlock(GifRawBlock block, GifRawBlockKind kind, int index, GifRawBlockScope scope, int scopeIndex)
    {
        Assert.That(block.Kind, Is.EqualTo(kind));
        Assert.That(block.Index, Is.EqualTo(index));
        Assert.That(block.Scope, Is.EqualTo(scope));
        Assert.That(block.ScopeIndex, Is.EqualTo(scopeIndex));
    }

    static GifPalette CreatePalette()
    {
        return new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 255, 255)
        });
    }

    static byte[] CreateSemanticMetadataFixture()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            2, 0, 1, 0, 0xC8, 1, 49,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFE, 2, 65, 66, 1, 67, 0,
            0x21, 0xFE, 1, 90, 0,
            0x21, 0xFF, 11, 65, 80, 80, 73, 68, 49, 50, 51, 88, 89, 90,
            2, 9, 8, 1, 7, 0,
            0x21, 0xF9, 4, 0x13, 0x34, 0x12, 1, 0,
            0x21, 0x01, 12, 0, 0, 0, 0, 2, 0, 1, 0, 8, 9, 1, 0,
            1, 80, 1, 84, 0,
            0x21, 0x99, 2, 5, 6, 1, 7, 0,
            0x21, 0xF9, 4, 0x14, 2, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0xA0,
            255, 0, 0, 0, 0, 255,
            2, 2, 0x4C, 0x01, 0,
            0x21, 0x98, 1, 8, 0,
            0x3B
        };
    }

    static byte[] CreateReservedDisposalsFixture()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xF9, 4, 0x10, 4, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0, 2, 2, 0x44, 0x01, 0,
            0x21, 0xF9, 4, 0x14, 5, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0, 2, 2, 0x44, 0x01, 0,
            0x21, 0xF9, 4, 0x18, 6, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0, 2, 2, 0x44, 0x01, 0,
            0x21, 0xF9, 4, 0x1C, 7, 0, 0, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0, 2, 2, 0x44, 0x01, 0,
            0x3B
        };
    }
}
