using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifRoundTripTests
{
    [Test]
    public void DecodeEncode_FixtureCorpusPreservesExactBytes()
    {
        byte[][] fixtures = AllPreservationFixtures();

        foreach (byte[] fixture in fixtures)
        {
            GifDocument document = Gif.Decode(fixture);

            byte[] encoded = Gif.Encode(document);

            Assert.That(encoded, Is.EqualTo(fixture));
        }
    }

    [Test]
    public void DecodeWrite_FixtureCorpusPreservesExactBytes()
    {
        byte[][] fixtures = AllPreservationFixtures();

        foreach (byte[] fixture in fixtures)
        {
            GifDocument document = Gif.Decode(fixture);
            MemoryStream stream = new MemoryStream();

            Gif.Write(stream, document);

            Assert.That(stream.ToArray(), Is.EqualTo(fixture));
        }
    }

    [Test]
    public void DecodeEncode_MixedMetadataAndUnknownExtensionsPreserveExactBytes()
    {
        byte[] data = MixedMetadataUnknownExtensionGif89a();

        GifDocument document = Gif.Decode(data);
        byte[] encoded = Gif.Encode(document);

        Assert.That(document.LoopCount, Is.EqualTo(5));
        Assert.That(document.ApplicationExtensions.Length, Is.EqualTo(2));
        Assert.That(document.Comments.Length, Is.EqualTo(1));
        Assert.That(document.PlainTextExtensions.Length, Is.EqualTo(1));
        Assert.That(document.UnknownExtensions.Length, Is.EqualTo(1));
        Assert.That(document.UnknownExtensions[0].Label, Is.EqualTo(0x99));
        Assert.That(document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 7, 8, 9 }));
        Assert.That(encoded, Is.EqualTo(data));
    }

    [Test]
    public void DecodeEncode_CanDropUnknownExtensionsWhenPreservationIsDisabled()
    {
        byte[] data = MixedMetadataUnknownExtensionGif89a();
        GifDecodeOptions options = new GifDecodeOptions();
        options.PreserveUnknownExtensions = false;

        GifDocument document = Gif.Decode(data, options);
        byte[] encoded = Gif.Encode(document);
        GifDocument reparsed = Gif.Decode(encoded);

        Assert.That(document.UnknownExtensions, Is.Empty);
        Assert.That(document.Metadata.UnknownExtensions, Is.Empty);
        Assert.That(reparsed.UnknownExtensions, Is.Empty);
        Assert.That(encoded, Is.Not.EqualTo(data));
    }

    [Test]
    public void DecodeEditEncode_InterleavedBlocksPreserveOrderAndScope()
    {
        byte[] data = InterleavedScopedBlocksGif89a();
        GifCodec codec = new GifCodec();

        GifDocument document = codec.DecodeBytes(data, null);

        Assert.That(document.GraphicControls.Length, Is.EqualTo(3));
        Assert.That(document.BlockOrder.Length, Is.EqualTo(13));
        AssertRawBlock(document.BlockOrder[0], GifRawBlockKind.Comment, 0, GifRawBlockScope.Document, -1);
        AssertRawBlock(document.BlockOrder[1], GifRawBlockKind.GraphicControl, 0, GifRawBlockScope.Frame, 0);
        AssertRawBlock(document.BlockOrder[2], GifRawBlockKind.UnknownExtension, 0, GifRawBlockScope.Document, -1);
        AssertRawBlock(document.BlockOrder[3], GifRawBlockKind.Image, 0, GifRawBlockScope.Frame, 0);
        AssertRawBlock(document.BlockOrder[4], GifRawBlockKind.Application, 0, GifRawBlockScope.Document, -1);
        AssertRawBlock(document.BlockOrder[5], GifRawBlockKind.GraphicControl, 1, GifRawBlockScope.PlainText, 0);
        AssertRawBlock(document.BlockOrder[6], GifRawBlockKind.Comment, 1, GifRawBlockScope.Document, -1);
        AssertRawBlock(document.BlockOrder[7], GifRawBlockKind.PlainText, 0, GifRawBlockScope.PlainText, 0);
        AssertRawBlock(document.BlockOrder[8], GifRawBlockKind.Image, 1, GifRawBlockScope.Frame, 1);
        AssertRawBlock(document.BlockOrder[9], GifRawBlockKind.UnknownExtension, 1, GifRawBlockScope.Trailer, -1);
        AssertRawBlock(document.BlockOrder[10], GifRawBlockKind.Comment, 2, GifRawBlockScope.Trailer, -1);
        AssertRawBlock(document.BlockOrder[11], GifRawBlockKind.GraphicControl, 2, GifRawBlockScope.Trailer, -1);
        AssertRawBlock(document.BlockOrder[12], GifRawBlockKind.Trailer, -1, GifRawBlockScope.Trailer, -1);
        Assert.That(codec.Encode(document, null), Is.EqualTo(data));

        document.Comments[1] = new GifCommentExtension("Z");
        document.Frames[0].DelayHundredths = 4;
        document.PlainTextExtensions[0].GraphicControl.DelayHundredths = 5;
        byte[] edited = codec.Encode(document, null);
        GifDocument reparsed = codec.DecodeBytes(edited, null);

        Assert.That(reparsed.Comments[1].Text, Is.EqualTo("Z"));
        Assert.That(reparsed.Frames[0].DelayHundredths, Is.EqualTo(4));
        Assert.That(reparsed.PlainTextExtensions[0].GraphicControl.DelayHundredths, Is.EqualTo(5));
        Assert.That(reparsed.BlockOrder, Has.Length.EqualTo(13));
        AssertRawBlock(reparsed.BlockOrder[5], GifRawBlockKind.GraphicControl, 1, GifRawBlockScope.PlainText, 0);
        AssertRawBlock(reparsed.BlockOrder[6], GifRawBlockKind.Comment, 1, GifRawBlockScope.Document, -1);
        AssertRawBlock(reparsed.BlockOrder[7], GifRawBlockKind.PlainText, 0, GifRawBlockScope.PlainText, 0);
        AssertRawBlock(reparsed.BlockOrder[9], GifRawBlockKind.UnknownExtension, 1, GifRawBlockScope.Trailer, -1);
        AssertRawBlock(reparsed.BlockOrder[10], GifRawBlockKind.Comment, 2, GifRawBlockScope.Trailer, -1);
        AssertRawBlock(reparsed.BlockOrder[11], GifRawBlockKind.GraphicControl, 2, GifRawBlockScope.Trailer, -1);
    }

    [Test]
    public void Encode_RejectsRawBlockOrderWithAnInvalidCollectionIndex()
    {
        GifDocument document = Gif.Decode(GifFixtures.MinimalGif89a());
        document.BlockOrder[0].Index = 5;

        GifException exception = Assert.Throws<GifException>(() => new GifCodec().Encode(document, null));

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(exception.BlockName, Is.EqualTo("BlockOrder"));
    }

    static byte[][] AllPreservationFixtures()
    {
        return new byte[][]
        {
            GifFixtures.MinimalGif87a(),
            GifFixtures.MinimalGif89a(),
            GifFixtures.TransparentGif89a(),
            GifFixtures.InterlacedGif89a(),
            GifFixtures.LocalColorTableGif89a(),
            GifFixtures.AnimatedDisposalMethod0Gif89a(),
            GifFixtures.AnimatedDisposalMethod1Gif89a(),
            GifFixtures.AnimatedDisposalMethod2Gif89a(),
            GifFixtures.AnimatedDisposalMethod3Gif89a(),
            GifFixtures.NetscapeLoopGif89a(),
            GifFixtures.CommentGif89a(),
            GifFixtures.ApplicationExtensionGif89a(),
            GifFixtures.PlainTextGif89a()
        };
    }

    static byte[] MixedMetadataUnknownExtensionGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48,
            3, 1, 5, 0, 0,
            0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49,
            3, 9, 8, 7, 0,
            0x21, 0xFE, 7, 102, 105, 120, 116, 117, 114, 101, 0,
            0x21, 0x01, 12, 0, 0, 0, 0, 1, 0, 1, 0, 8, 8, 1, 0,
            5, 112, 108, 97, 105, 110, 0,
            0x21, 0x99, 3, 7, 8, 9, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    static byte[] InterleavedScopedBlocksGif89a()
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFE, 1, 65, 0,
            0x21, 0xF9, 4, 0, 1, 0, 0, 0,
            0x21, 0x99, 1, 7, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x21, 0xFF, 11, 84, 69, 83, 84, 65, 80, 80, 49, 48, 48, 49,
            1, 9, 0,
            0x21, 0xF9, 4, 0, 2, 0, 0, 0,
            0x21, 0xFE, 1, 66, 0,
            0x21, 0x01, 12, 0, 0, 0, 0, 1, 0, 1, 0, 8, 8, 1, 0,
            1, 80, 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x21, 0x98, 1, 8, 0,
            0x21, 0xFE, 1, 67, 0,
            0x21, 0xF9, 4, 0, 3, 0, 0, 0,
            0x3B
        };
    }

    static void AssertRawBlock(GifRawBlock block, GifRawBlockKind kind, int index, GifRawBlockScope scope, int scopeIndex)
    {
        Assert.That(block.Kind, Is.EqualTo(kind));
        Assert.That(block.Index, Is.EqualTo(index));
        Assert.That(block.Scope, Is.EqualTo(scope));
        Assert.That(block.ScopeIndex, Is.EqualTo(scopeIndex));
    }
}
