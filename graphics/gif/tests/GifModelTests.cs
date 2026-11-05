using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifModelTests
{
    [Test]
    public void PaletteAndColor_ConstructorsPreserveRgbValues()
    {
        GifColor red = new GifColor(255, 0, 0);
        GifColor blue = new GifColor(0, 0, 255);
        GifPalette palette = new GifPalette(new GifColor[] { red, blue });

        Assert.That(red.Red, Is.EqualTo(255));
        Assert.That(red.Green, Is.EqualTo(0));
        Assert.That(red.Blue, Is.EqualTo(0));
        Assert.That(palette.Count, Is.EqualTo(2));
        Assert.That(palette.Colors[1], Is.SameAs(blue));
        Assert.That(palette.IsEmpty(), Is.False);
    }

    [Test]
    public void RgbaColor_ConstructorPreservesEveryChannel()
    {
        GifRgbaColor color = new GifRgbaColor(1, 2, 3, 4);

        Assert.That(color.Red, Is.EqualTo(1));
        Assert.That(color.Green, Is.EqualTo(2));
        Assert.That(color.Blue, Is.EqualTo(3));
        Assert.That(color.Alpha, Is.EqualTo(4));
    }

    [Test]
    public void NullableModelInputs_NormalizeWithoutCloningNonNullArrays()
    {
        GifPalette emptyPalette = new GifPalette(null);
        GifBitmap emptyBitmap = new GifBitmap(2, 3, null);
        GifCommentExtension emptyComment = new GifCommentExtension(null);
        GifUnknownExtension emptyUnknown = new GifUnknownExtension(240, null, null);
        GifColor[] colors = new GifColor[] { new GifColor(1, 2, 3) };
        byte[] pixels = new byte[] { 1, 2, 3, 4 };

        Assert.That(emptyPalette.Colors, Is.Not.Null.And.Empty);
        Assert.That(emptyPalette.Count, Is.EqualTo(0));
        Assert.That(emptyBitmap.RgbaPixels, Is.Not.Null.And.Empty);
        Assert.That(emptyComment.Text, Is.EqualTo(string.Empty));
        Assert.That(emptyUnknown.Data, Is.Not.Null.And.Empty);
        Assert.That(emptyUnknown.BlockName, Is.EqualTo(string.Empty));
        Assert.That(new GifPalette(colors).Colors, Is.SameAs(colors));
        Assert.That(new GifBitmap(1, 1, pixels).RgbaPixels, Is.SameAs(pixels));
    }

    [Test]
    public void Bitmap_StrideHandlesExactInt32MultiplicationBoundaries()
    {
        Assert.That(new GifBitmap(536870911, 0, null).Stride, Is.EqualTo(2147483644));
        Assert.That(new GifBitmap(536870912, 0, null).Stride, Is.EqualTo(0));
        Assert.That(new GifBitmap(-536870912, 0, null).Stride, Is.EqualTo(int.MinValue));
        Assert.That(new GifBitmap(-536870913, 0, null).Stride, Is.EqualTo(0));
    }

    [Test]
    public void LogicalScreen_EmptyAndSizedDimensionsAreStable()
    {
        GifLogicalScreen empty = new GifLogicalScreen(0, 0);
        GifLogicalScreen sized = new GifLogicalScreen(10, 20);

        Assert.That(empty.Width, Is.EqualTo(0));
        Assert.That(empty.Height, Is.EqualTo(0));
        Assert.That(empty.HasGlobalColorTable, Is.False);
        Assert.That(empty.GlobalColorTableSize, Is.EqualTo(0));
        Assert.That(empty.ColorResolution, Is.EqualTo(0));
        Assert.That(empty.SortFlag, Is.False);
        Assert.That(empty.BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(empty.PixelAspectRatio, Is.EqualTo(0));
        Assert.That(sized.Width, Is.EqualTo(10));
        Assert.That(sized.Height, Is.EqualTo(20));
    }

    [Test]
    public void Frame_DefaultsIncludeEmptyPalettePixelsMetadataAndWarnings()
    {
        GifFrame frame = new GifFrame(1, 2, 3, 4);

        Assert.That(frame.Left, Is.EqualTo(1));
        Assert.That(frame.Top, Is.EqualTo(2));
        Assert.That(frame.Width, Is.EqualTo(3));
        Assert.That(frame.Height, Is.EqualTo(4));
        Assert.That(frame.DelayHundredths, Is.EqualTo(0));
        Assert.That(frame.DisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(frame.HasTransparency, Is.False);
        Assert.That(frame.TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(frame.UserInputFlag, Is.False);
        Assert.That(frame.Interlace, Is.False);
        Assert.That(frame.HasLocalColorTable, Is.False);
        Assert.That(frame.LocalColorTableSortFlag, Is.False);
        Assert.That(frame.LocalColorTableSize, Is.EqualTo(0));
        Assert.That(frame.LocalColorTableSizeCode, Is.EqualTo(0));
        Assert.That(frame.LocalColorTable, Is.Not.Null);
        Assert.That(frame.LocalColorTable.IsEmpty(), Is.True);
        Assert.That(frame.LzwMinimumCodeSize, Is.EqualTo(0));
        Assert.That(frame.CompressedImageData, Is.Empty);
        Assert.That(frame.IndexedPixels, Is.Empty);
        Assert.That(frame.GraphicControl, Is.Not.Null);
        Assert.That(frame.UnknownExtensions, Is.Empty);
        Assert.That(frame.Warnings, Is.Empty);
    }

    [Test]
    public void Bitmap_ConstructorsDistinguishRgbaAndIndexedPixels()
    {
        byte[] rgbaPixels = new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 };
        byte[] indexedPixels = new byte[] { 0, 1 };
        GifPalette palette = new GifPalette(new GifColor[] { new GifColor(0, 0, 0), new GifColor(255, 255, 255) });

        GifBitmap rgba = new GifBitmap(2, 1, rgbaPixels);
        GifBitmap indexed = GifBitmap.CreateIndexed(2, 1, indexedPixels, palette);

        Assert.That(rgba.Width, Is.EqualTo(2));
        Assert.That(rgba.Height, Is.EqualTo(1));
        Assert.That(rgba.Stride, Is.EqualTo(8));
        Assert.That(rgba.RgbaPixels, Is.SameAs(rgbaPixels));
        Assert.That(rgba.IndexedPixels, Is.Empty);
        Assert.That(rgba.Palette, Is.Not.Null);
        Assert.That(rgba.UsesIndexedPixels, Is.False);
        Assert.That(indexed.Stride, Is.EqualTo(2));
        Assert.That(indexed.IndexedPixels, Is.SameAs(indexedPixels));
        Assert.That(indexed.Palette, Is.SameAs(palette));
        Assert.That(indexed.UsesIndexedPixels, Is.True);
    }

    [Test]
    public void AnimationAndAnimationFrame_DefaultsAreEmptyAndUnknown()
    {
        GifAnimation animation = new GifAnimation();
        GifAnimationFrame frame = new GifAnimationFrame();

        Assert.That(animation.Width, Is.EqualTo(0));
        Assert.That(animation.Height, Is.EqualTo(0));
        Assert.That(animation.Frames, Is.Empty);
        Assert.That(animation.LoopCount, Is.EqualTo(-1));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(0));
        Assert.That(animation.Warnings, Is.Empty);
        Assert.That(animation.PlainTextGraphics, Is.Empty);
        Assert.That(frame.Bitmap, Is.Not.Null);
        Assert.That(frame.DelayMilliseconds, Is.EqualTo(0));
        Assert.That(frame.DisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(frame.HasTransparency, Is.False);
        Assert.That(frame.TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(frame.SourceLeft, Is.EqualTo(0));
        Assert.That(frame.SourceTop, Is.EqualTo(0));
        Assert.That(frame.SourceWidth, Is.EqualTo(0));
        Assert.That(frame.SourceHeight, Is.EqualTo(0));
        Assert.That(frame.FrameIndex, Is.EqualTo(-1));
    }

    [Test]
    public void MetadataExtensions_DefaultToEmptyPayloads()
    {
        GifGraphicControl control = new GifGraphicControl();
        GifCommentExtension comment = new GifCommentExtension("hello");
        GifApplicationExtension application = new GifApplicationExtension();
        GifPlainTextExtension plainText = new GifPlainTextExtension();
        GifUnknownExtension unknown = new GifUnknownExtension(99, new byte[] { 1, 2 }, "CustomExtension");

        Assert.That(control.DisposalMethod, Is.EqualTo(GifDisposalMethod.Unspecified));
        Assert.That(control.UserInputFlag, Is.False);
        Assert.That(control.HasTransparency, Is.False);
        Assert.That(control.DelayHundredths, Is.EqualTo(0));
        Assert.That(control.TransparentColorIndex, Is.EqualTo(-1));
        Assert.That(comment.Text, Is.EqualTo("hello"));
        Assert.That(comment.Data, Is.Empty);
        Assert.That(application.Identifier, Is.EqualTo(string.Empty));
        Assert.That(application.AuthenticationCode, Is.EqualTo(string.Empty));
        Assert.That(application.Data, Is.Empty);
        Assert.That(application.IsLoopExtension, Is.False);
        Assert.That(application.LoopCount, Is.EqualTo(-1));
        Assert.That(plainText.TextData, Is.Empty);
        Assert.That(plainText.Text, Is.EqualTo(string.Empty));
        Assert.That(plainText.GraphicControl, Is.Not.Null);
        Assert.That(plainText.TextGridLeft, Is.EqualTo(0));
        Assert.That(plainText.TextGridTop, Is.EqualTo(0));
        Assert.That(plainText.TextGridWidth, Is.EqualTo(0));
        Assert.That(plainText.TextGridHeight, Is.EqualTo(0));
        Assert.That(plainText.CharacterCellWidth, Is.EqualTo(0));
        Assert.That(plainText.CharacterCellHeight, Is.EqualTo(0));
        Assert.That(plainText.ForegroundColorIndex, Is.EqualTo(0));
        Assert.That(plainText.BackgroundColorIndex, Is.EqualTo(0));
        Assert.That(unknown.Label, Is.EqualTo(99));
        Assert.That(unknown.Data, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(unknown.BlockName, Is.EqualTo("CustomExtension"));
    }

    [Test]
    public void Document_DefaultsInitializeNestedCollectionsAndStatistics()
    {
        GifDocument document = new GifDocument();

        Assert.That(document.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(document.LogicalScreen, Is.Not.Null);
        Assert.That(document.GlobalColorTable, Is.Not.Null);
        Assert.That(document.Frames, Is.Empty);
        Assert.That(document.GraphicControls, Is.Empty);
        Assert.That(document.BlockOrder, Is.Empty);
        Assert.That(document.Metadata, Is.Not.Null);
        Assert.That(document.Comments, Is.Empty);
        Assert.That(document.ApplicationExtensions, Is.Empty);
        Assert.That(document.PlainTextExtensions, Is.Empty);
        Assert.That(document.PlainTextGraphics, Is.Empty);
        Assert.That(document.UnknownExtensions, Is.Empty);
        Assert.That(document.LoopCount, Is.EqualTo(-1));
        Assert.That(document.Warnings, Is.Empty);
        Assert.That(document.Statistics, Is.Not.Null);
        Assert.That(document.DecodeStatistics, Is.Not.Null);
        Assert.That(document.EncodeStatistics, Is.Not.Null);
    }

    [Test]
    public void RawBlock_DefaultsToDocumentScopeWithoutACollectionIndex()
    {
        GifRawBlock block = new GifRawBlock();

        Assert.That(block.Kind, Is.EqualTo(GifRawBlockKind.GraphicControl));
        Assert.That(block.Index, Is.EqualTo(-1));
        Assert.That(block.Scope, Is.EqualTo(GifRawBlockScope.Document));
        Assert.That(block.ScopeIndex, Is.EqualTo(-1));
    }

    [Test]
    public void RawBlockOrder_RepresentsCallerSelectedKindsIndexesAndScopesInOrder()
    {
        GifRawBlock comment = new GifRawBlock();
        comment.Kind = GifRawBlockKind.Comment;
        comment.Index = 2;
        comment.Scope = GifRawBlockScope.Trailer;
        comment.ScopeIndex = -1;
        GifRawBlock image = new GifRawBlock();
        image.Kind = GifRawBlockKind.Image;
        image.Index = 1;
        image.Scope = GifRawBlockScope.Frame;
        image.ScopeIndex = 1;
        GifRawBlock[] order = new GifRawBlock[] { comment, image };
        GifDocument document = new GifDocument();
        document.BlockOrder = order;

        Assert.That(document.BlockOrder, Is.SameAs(order));
        Assert.That(document.BlockOrder[0].Kind, Is.EqualTo(GifRawBlockKind.Comment));
        Assert.That(document.BlockOrder[0].Index, Is.EqualTo(2));
        Assert.That(document.BlockOrder[0].Scope, Is.EqualTo(GifRawBlockScope.Trailer));
        Assert.That(document.BlockOrder[0].ScopeIndex, Is.EqualTo(-1));
        Assert.That(document.BlockOrder[1].Kind, Is.EqualTo(GifRawBlockKind.Image));
        Assert.That(document.BlockOrder[1].Index, Is.EqualTo(1));
        Assert.That(document.BlockOrder[1].Scope, Is.EqualTo(GifRawBlockScope.Frame));
        Assert.That(document.BlockOrder[1].ScopeIndex, Is.EqualTo(1));
    }

    [Test]
    public void Metadata_RepresentsEveryExtensionCollectionAndLoopCount()
    {
        GifCommentExtension[] comments = new GifCommentExtension[] { new GifCommentExtension("comment") };
        GifApplicationExtension[] applications = new GifApplicationExtension[] { new GifApplicationExtension() };
        GifPlainTextExtension[] plainText = new GifPlainTextExtension[] { new GifPlainTextExtension() };
        GifUnknownExtension[] unknown = new GifUnknownExtension[] { new GifUnknownExtension(240, new byte[] { 7 }, "Custom") };
        GifMetadata metadata = new GifMetadata();
        metadata.Comments = comments;
        metadata.ApplicationExtensions = applications;
        metadata.PlainTextExtensions = plainText;
        metadata.UnknownExtensions = unknown;
        metadata.LoopCount = 3;

        Assert.That(metadata.Comments, Is.SameAs(comments));
        Assert.That(metadata.ApplicationExtensions, Is.SameAs(applications));
        Assert.That(metadata.PlainTextExtensions, Is.SameAs(plainText));
        Assert.That(metadata.UnknownExtensions, Is.SameAs(unknown));
        Assert.That(metadata.LoopCount, Is.EqualTo(3));
    }

    [Test]
    public void InfoAndStatistics_DefaultsAreEmpty()
    {
        GifInfo info = new GifInfo();
        GifMetadata metadata = new GifMetadata();
        GifStatistics statistics = new GifStatistics();
        GifDecodeStatistics decodeStatistics = new GifDecodeStatistics();
        GifEncodeStatistics encodeStatistics = new GifEncodeStatistics();

        Assert.That(info.Success, Is.False);
        Assert.That(info.Version, Is.EqualTo(GifVersion.Unknown));
        Assert.That(info.Width, Is.EqualTo(0));
        Assert.That(info.Height, Is.EqualTo(0));
        Assert.That(info.FrameCount, Is.EqualTo(0));
        Assert.That(info.HasAnimation, Is.False);
        Assert.That(info.HasTransparency, Is.False);
        Assert.That(info.LoopCount, Is.EqualTo(-1));
        Assert.That(info.HasComments, Is.False);
        Assert.That(info.ApplicationExtensionIdentifiers, Is.Empty);
        Assert.That(info.EstimatedDecodedBytes, Is.EqualTo(0));
        Assert.That(info.Warnings, Is.Empty);
        Assert.That(info.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(metadata.Comments, Is.Empty);
        Assert.That(metadata.ApplicationExtensions, Is.Empty);
        Assert.That(metadata.PlainTextExtensions, Is.Empty);
        Assert.That(metadata.UnknownExtensions, Is.Empty);
        Assert.That(metadata.LoopCount, Is.EqualTo(-1));
        Assert.That(statistics.BytesRead, Is.EqualTo(0));
        Assert.That(statistics.BytesWritten, Is.EqualTo(0));
        Assert.That(statistics.FramesRead, Is.EqualTo(0));
        Assert.That(statistics.FramesWritten, Is.EqualTo(0));
        Assert.That(statistics.ErrorCount, Is.EqualTo(0));
        Assert.That(statistics.WarningCount, Is.EqualTo(0));
        Assert.That(decodeStatistics.BytesRead, Is.EqualTo(0));
        Assert.That(decodeStatistics.CompressedBytesRead, Is.EqualTo(0));
        Assert.That(decodeStatistics.DecodedPixelCount, Is.EqualTo(0));
        Assert.That(decodeStatistics.FramesRead, Is.EqualTo(0));
        Assert.That(decodeStatistics.ExtensionCount, Is.EqualTo(0));
        Assert.That(decodeStatistics.WarningCount, Is.EqualTo(0));
        Assert.That(encodeStatistics.BytesWritten, Is.EqualTo(0));
        Assert.That(encodeStatistics.CompressedBytesWritten, Is.EqualTo(0));
        Assert.That(encodeStatistics.IndexedPixelCount, Is.EqualTo(0));
        Assert.That(encodeStatistics.FramesWritten, Is.EqualTo(0));
        Assert.That(encodeStatistics.ExtensionCount, Is.EqualTo(0));
        Assert.That(encodeStatistics.WarningCount, Is.EqualTo(0));
    }

    [Test]
    public void EncodeResult_DefaultsToAnInitializedUnsuccessfulRepresentation()
    {
        GifEncodeResult result = new GifEncodeResult();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Data, Is.Not.Null.And.Empty);
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(result.Error.Message, Is.EqualTo(string.Empty));
        Assert.That(result.Error.Position.ByteOffset, Is.EqualTo(-1));
        Assert.That(result.Error.Position.FrameIndex, Is.EqualTo(-1));
        Assert.That(result.Error.Position.BlockName, Is.EqualTo(string.Empty));
    }

    [Test]
    public void StatisticsFields_PreserveEveryAssignedCounter()
    {
        GifStatistics statistics = new GifStatistics();
        statistics.BytesRead = 1;
        statistics.BytesWritten = 2;
        statistics.FramesRead = 3;
        statistics.FramesWritten = 4;
        statistics.ErrorCount = 5;
        statistics.WarningCount = 6;
        GifDecodeStatistics decoded = new GifDecodeStatistics();
        decoded.BytesRead = 7;
        decoded.CompressedBytesRead = 8;
        decoded.DecodedPixelCount = 9;
        decoded.FramesRead = 10;
        decoded.ExtensionCount = 11;
        decoded.WarningCount = 12;
        GifEncodeStatistics encoded = new GifEncodeStatistics();
        encoded.BytesWritten = 13;
        encoded.CompressedBytesWritten = 14;
        encoded.IndexedPixelCount = 15;
        encoded.FramesWritten = 16;
        encoded.ExtensionCount = 17;
        encoded.WarningCount = 18;

        Assert.That(new long[] { statistics.BytesRead, statistics.BytesWritten, statistics.FramesRead, statistics.FramesWritten, statistics.ErrorCount, statistics.WarningCount }, Is.EqualTo(new long[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(new long[] { decoded.BytesRead, decoded.CompressedBytesRead, decoded.DecodedPixelCount, decoded.FramesRead, decoded.ExtensionCount, decoded.WarningCount }, Is.EqualTo(new long[] { 7, 8, 9, 10, 11, 12 }));
        Assert.That(new long[] { encoded.BytesWritten, encoded.CompressedBytesWritten, encoded.IndexedPixelCount, encoded.FramesWritten, encoded.ExtensionCount, encoded.WarningCount }, Is.EqualTo(new long[] { 13, 14, 15, 16, 17, 18 }));
    }
}
