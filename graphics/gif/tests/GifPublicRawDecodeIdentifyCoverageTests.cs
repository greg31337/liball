using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicRawDecodeIdentifyCoverageTests
{
    [Test]
    public void PublicDecodeAndIdentifyExposeVersionsAndLogicalScreenFields()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] logicalScreenFixture = CreateLogicalScreenFixture();

        GifDocument gif87a = decoder.DecodeBytes(GifFixtures.MinimalGif87a(), null);
        GifDocument gif89a = decoder.DecodeBytes(GifFixtures.MinimalGif89a(), null);
        GifInfo gif87aInfo = decoder.IdentifyBytes(GifFixtures.MinimalGif87a(), null);
        GifInfo gif89aInfo = decoder.IdentifyBytes(GifFixtures.MinimalGif89a(), null);
        GifDocument document = decoder.DecodeBytes(logicalScreenFixture, null);
        GifInfo info = decoder.IdentifyBytes(logicalScreenFixture, null);

        Assert.That(gif87a.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(gif89a.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(gif87aInfo.Version, Is.EqualTo(GifVersion.Gif87a));
        Assert.That(gif89aInfo.Version, Is.EqualTo(GifVersion.Gif89a));
        Assert.That(document.LogicalScreen.Width, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.Height, Is.EqualTo(4));
        Assert.That(document.LogicalScreen.HasGlobalColorTable, Is.True);
        Assert.That(document.LogicalScreen.GlobalColorTableSize, Is.EqualTo(4));
        Assert.That(document.LogicalScreen.ColorResolution, Is.EqualTo(5));
        Assert.That(document.LogicalScreen.SortFlag, Is.True);
        Assert.That(document.LogicalScreen.BackgroundColorIndex, Is.EqualTo(2));
        Assert.That(document.LogicalScreen.PixelAspectRatio, Is.EqualTo(49));
        Assert.That(document.GlobalColorTable.Count, Is.EqualTo(4));
        Assert.That(document.GlobalColorTable.Colors[1].Red, Is.EqualTo(255));
        Assert.That(document.GlobalColorTable.Colors[2].Green, Is.EqualTo(255));
        Assert.That(document.GlobalColorTable.Colors[3].Blue, Is.EqualTo(255));
        Assert.That(info.Success, Is.True);
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(4));
        Assert.That(info.Version, Is.EqualTo(GifVersion.Gif89a));
    }

    [Test]
    public void PublicOnDemandDecodeReturnsLocalPaletteAndDeinterlacedIndexedPixels()
    {
        IGifDecoder decoder = new GifCodec();

        GifDocument local = decoder.DecodeFrameBytes(GifFixtures.LocalColorTableGif89a(), 0, null);
        GifDocument interlaced = decoder.DecodeFrameBytes(GifFixtures.InterlacedGif89a(), 0, null);
        GifInfo localInfo = decoder.IdentifyBytes(GifFixtures.LocalColorTableGif89a(), null);
        GifInfo interlacedInfo = decoder.IdentifyBytes(GifFixtures.InterlacedGif89a(), null);

        Assert.That(local.LogicalScreen.HasGlobalColorTable, Is.False);
        Assert.That(local.GlobalColorTable.IsEmpty(), Is.True);
        Assert.That(local.Frames[0].HasLocalColorTable, Is.True);
        Assert.That(local.Frames[0].LocalColorTable.Count, Is.EqualTo(2));
        Assert.That(local.Frames[0].LocalColorTable.Colors[0].Red, Is.EqualTo(255));
        Assert.That(local.Frames[0].LocalColorTable.Colors[1].Blue, Is.EqualTo(255));
        Assert.That(local.Frames[0].IndexedPixels, Is.EqualTo(new byte[] { 1 }));
        Assert.That(interlaced.Frames[0].Interlace, Is.True);
        Assert.That(interlaced.Frames[0].IndexedPixels, Is.EqualTo(new byte[]
        {
            0, 0,
            1, 1,
            2, 2,
            3, 3
        }));
        Assert.That(localInfo.Success, Is.True);
        Assert.That(localInfo.FrameCount, Is.EqualTo(1));
        Assert.That(interlacedInfo.Success, Is.True);
        Assert.That(interlacedInfo.Width, Is.EqualTo(2));
        Assert.That(interlacedInfo.Height, Is.EqualTo(4));
        Assert.That(interlacedInfo.EstimatedDecodedBytes, Is.EqualTo(32));
    }

    [Test]
    public void PublicDecodePreservesEveryStandardExtensionUnknownDataAndTrailerScope()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = CreateInterleavedExtensionGif();
        GifDecodeOptions options = new GifDecodeOptions();
        options.PlainTextExtensionPolicy = GifPlainTextExtensionPolicy.Expose;

        GifDocument document = decoder.DecodeBytes(source, options);
        GifInfo info = decoder.IdentifyBytes(source, options);

        Assert.That(document.Frames, Has.Length.EqualTo(2));
        Assert.That(document.GraphicControls, Has.Length.EqualTo(3));
        Assert.That(document.Comments, Has.Length.EqualTo(3));
        Assert.That(document.Comments[0].Text, Is.EqualTo("A"));
        Assert.That(document.Comments[1].Text, Is.EqualTo("B"));
        Assert.That(document.Comments[2].Text, Is.EqualTo("C"));
        Assert.That(document.ApplicationExtensions, Has.Length.EqualTo(1));
        Assert.That(document.ApplicationExtensions[0].Identifier, Is.EqualTo("TESTAPP1"));
        Assert.That(document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo("001"));
        Assert.That(document.ApplicationExtensions[0].Data, Is.EqualTo(new byte[] { 9 }));
        Assert.That(document.PlainTextExtensions, Has.Length.EqualTo(1));
        Assert.That(document.PlainTextExtensions[0].Text, Is.EqualTo("P"));
        Assert.That(document.PlainTextGraphics, Has.Length.EqualTo(1));
        Assert.That(document.UnknownExtensions, Has.Length.EqualTo(2));
        Assert.That(document.UnknownExtensions[0].Label, Is.EqualTo(0x99));
        Assert.That(document.UnknownExtensions[0].Data, Is.EqualTo(new byte[] { 7 }));
        Assert.That(document.UnknownExtensions[1].Label, Is.EqualTo(0x98));
        Assert.That(document.UnknownExtensions[1].Data, Is.EqualTo(new byte[] { 8 }));
        Assert.That(document.Frames[0].DelayHundredths, Is.EqualTo(1));
        Assert.That(document.PlainTextExtensions[0].GraphicControl.DelayHundredths, Is.EqualTo(2));
        Assert.That(document.BlockOrder, Has.Length.EqualTo(13));
        AssertRawBlock(document.BlockOrder[9], GifRawBlockKind.UnknownExtension, 1, GifRawBlockScope.Trailer);
        AssertRawBlock(document.BlockOrder[10], GifRawBlockKind.Comment, 2, GifRawBlockScope.Trailer);
        AssertRawBlock(document.BlockOrder[11], GifRawBlockKind.GraphicControl, 2, GifRawBlockScope.Trailer);
        AssertRawBlock(document.BlockOrder[12], GifRawBlockKind.Trailer, -1, GifRawBlockScope.Trailer);
        Assert.That(document.DecodeStatistics.BytesRead, Is.EqualTo(source.Length));
        Assert.That(document.DecodeStatistics.CompressedBytesRead, Is.EqualTo(4));
        Assert.That(document.DecodeStatistics.DecodedPixelCount, Is.EqualTo(2));
        Assert.That(document.DecodeStatistics.FramesRead, Is.EqualTo(2));
        Assert.That(document.DecodeStatistics.ExtensionCount, Is.EqualTo(10));
        Assert.That(document.DecodeStatistics.WarningCount, Is.EqualTo(0));
        Assert.That(info.Success, Is.True);
        Assert.That(info.FrameCount, Is.EqualTo(2));
        Assert.That(info.HasAnimation, Is.True);
        Assert.That(info.HasComments, Is.True);
        Assert.That(info.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "TESTAPP1" }));
    }

    [Test]
    public void PublicDecodeAndIdentifyRecognizeNetscapeAndAnimextsLoopExtensions()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] netscapeSource = GifFixtures.NetscapeLoopGif89a();
        byte[] animextsSource = CreateAnimextsLoopGif(300);

        GifDocument netscape = decoder.DecodeBytes(netscapeSource, null);
        GifInfo netscapeInfo = decoder.IdentifyBytes(netscapeSource, null);
        GifDocument animexts = decoder.DecodeBytes(animextsSource, null);
        GifInfo animextsInfo = decoder.IdentifyBytes(animextsSource, null);

        AssertLoopExtension(netscape, "NETSCAPE", "2.0", 5);
        Assert.That(netscapeInfo.LoopCount, Is.EqualTo(5));
        Assert.That(netscapeInfo.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "NETSCAPE" }));
        AssertLoopExtension(animexts, "ANIMEXTS", "1.0", 300);
        Assert.That(animextsInfo.Success, Is.True);
        Assert.That(animextsInfo.LoopCount, Is.EqualTo(300));
        Assert.That(animextsInfo.ApplicationExtensionIdentifiers, Is.EqualTo(new string[] { "ANIMEXTS" }));
    }

    [Test]
    public void PublicDecodeAndIdentifyExposeTrailerWarningsAndSuccessfulStatistics()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] baseGif = GifFixtures.MinimalGif89a();
        byte[] source = new byte[baseGif.Length + 2];
        Array.Copy(baseGif, source, baseGif.Length);
        source[source.Length - 2] = 1;
        source[source.Length - 1] = 2;

        GifDocument document = decoder.DecodeBytes(source, null);
        GifInfo info = decoder.IdentifyBytes(source, null);

        Assert.That(document.Warnings, Has.Length.EqualTo(1));
        Assert.That(document.Warnings[0].Code, Is.EqualTo(GifWarningCode.ExtraDataAfterTrailer));
        Assert.That(document.Warnings[0].Position.ByteOffset, Is.EqualTo(baseGif.Length));
        Assert.That(document.Warnings[0].Position.BlockName, Is.EqualTo("Trailer"));
        Assert.That(document.DecodeStatistics.BytesRead, Is.EqualTo(baseGif.Length));
        Assert.That(document.DecodeStatistics.CompressedBytesRead, Is.EqualTo(2));
        Assert.That(document.DecodeStatistics.DecodedPixelCount, Is.EqualTo(1));
        Assert.That(document.DecodeStatistics.FramesRead, Is.EqualTo(1));
        Assert.That(document.DecodeStatistics.ExtensionCount, Is.EqualTo(0));
        Assert.That(document.DecodeStatistics.WarningCount, Is.EqualTo(1));
        Assert.That(info.Success, Is.True);
        Assert.That(info.Warnings, Has.Length.EqualTo(1));
        Assert.That(info.Warnings[0].Code, Is.EqualTo(GifWarningCode.ExtraDataAfterTrailer));
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None));
    }

    static byte[] CreateLogicalScreenFixture()
    {
        byte[] data = GifFixtures.InterlacedGif89a();
        data[10] = 0xC9;
        data[11] = 2;
        data[12] = 49;
        return data;
    }

    static byte[] CreateAnimextsLoopGif(int loopCount)
    {
        return new byte[]
        {
            71, 73, 70, 56, 57, 97,
            1, 0, 1, 0, 0x80, 0, 0,
            0, 0, 0, 255, 255, 255,
            0x21, 0xFF, 11, 65, 78, 73, 77, 69, 88, 84, 83, 49, 46, 48,
            3, 1, (byte)(loopCount & 255), (byte)((loopCount >> 8) & 255), 0,
            0x2C, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            2, 2, 68, 1, 0,
            0x3B
        };
    }

    static byte[] CreateInterleavedExtensionGif()
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

    static void AssertRawBlock(GifRawBlock block, GifRawBlockKind kind, int index, GifRawBlockScope scope)
    {
        Assert.That(block.Kind, Is.EqualTo(kind));
        Assert.That(block.Index, Is.EqualTo(index));
        Assert.That(block.Scope, Is.EqualTo(scope));
        Assert.That(block.ScopeIndex, Is.EqualTo(-1));
    }

    static void AssertLoopExtension(GifDocument document, string identifier, string authenticationCode, int loopCount)
    {
        Assert.That(document.LoopCount, Is.EqualTo(loopCount));
        Assert.That(document.Metadata.LoopCount, Is.EqualTo(loopCount));
        Assert.That(document.ApplicationExtensions, Has.Length.EqualTo(1));
        Assert.That(document.ApplicationExtensions[0].Identifier, Is.EqualTo(identifier));
        Assert.That(document.ApplicationExtensions[0].AuthenticationCode, Is.EqualTo(authenticationCode));
        Assert.That(document.ApplicationExtensions[0].IsLoopExtension, Is.True);
        Assert.That(document.ApplicationExtensions[0].LoopCount, Is.EqualTo(loopCount));
    }
}
