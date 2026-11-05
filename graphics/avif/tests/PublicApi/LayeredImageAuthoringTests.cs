namespace avif.Tests;

public class LayeredImageAuthoringTests
{
    [Test]
    public void AuthorsProgressiveLayersWithOperatingPointAndSharedPayload()
    {
        AvifImage preview = CreateImage(2, 2, 23);
        AvifImage intermediate = CreateImage(3, 3, 79);
        AvifImage final = CreateImage(4, 4, 157);
        AvifLayeredImage layered = AvifLayeredImage.Create(2, true);
        Assert.That(layered.AddLayer(preview), Is.EqualTo(AvifResultCode.Success));
        Assert.That(layered.AddLayer(intermediate), Is.EqualTo(AvifResultCode.Success));
        Assert.That(layered.AddLayer(final), Is.EqualTo(AvifResultCode.Success));
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddLayeredImage(layered);
        preview.Close();
        intermediate.Close();
        final.Close();

        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDocument? document = reader.GetDocument();
        AvifImageItem? item = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        AvifItemProperty? operatingPoint = FindProperty(item, "a1op");
        AvifItemProperty? selector = FindProperty(item, "lsel");
        AvifItemProperty? index = FindProperty(item, "a1lx");
        AvifDecodeResult first = reader.DecodeLayer(0);
        AvifDecodeResult second = reader.DecodeLayer(1);
        AvifDecodeResult third = reader.DecodeLayer(2);
        AvifDecodeResult primary = reader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(itemId, Is.EqualTo(1));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(document.GetItemCount(), Is.EqualTo(1));
            Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount(), Is.EqualTo(1));
            Assert.That((operatingPoint ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.True);
            Assert.That(operatingPoint.GetOperatingPointIndex(), Is.EqualTo(2));
            Assert.That((selector ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerId(), Is.EqualTo(65535));
            Assert.That((index ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.False);
            Assert.That(index.GetLayerCount(), Is.EqualTo(3));
            Assert.That(index.UsesLargeLayerSizes(), Is.False);
            Assert.That(index.GetLayerSize(0), Is.GreaterThan(0));
            Assert.That(index.GetLayerSize(1), Is.GreaterThan(0));
            Assert.That(first.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(second.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(third.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(primary.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(2));
            Assert.That((second.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(3));
            Assert.That((third.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(4));
            Assert.That((primary.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(4));
        });
        AssertPlaneValue(first.GetImage(), 23);
        AssertPlaneValue(second.GetImage(), 79);
        AssertPlaneValue(third.GetImage(), 157);
        AssertPlaneValue(primary.GetImage(), 157);

        (first.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (second.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (third.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (primary.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsFinalLayerSelectionWhenProgressivePresentationIsDisabled()
    {
        AvifImage firstImage = CreateImage(2, 2, 41);
        AvifImage finalImage = CreateImage(2, 2, 109);
        AvifLayer firstLayer = new AvifLayer(firstImage);
        AvifLayer finalLayer = new AvifLayer(finalImage);
        AvifLayeredImage layered = AvifLayeredImage.Create(0, false);
        AvifResultCode firstAdded = layered.AddLayerDescriptor(firstLayer);
        AvifResultCode finalAdded = layered.AddLayerDescriptor(finalLayer);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddLayeredImage(layered);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifItemProperty? selector = FindProperty(
            (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId),
            "lsel");
        AvifImageItem? previewItem = (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        AvifItemProperty? previewSelector = FindProperty(previewItem, "lsel");
        AvifReader reader = AvifReader.Create(finished.GetData());
        reader.Parse();
        AvifDecodeResult decodedPreview = reader.DecodeItem(1);
        AvifDecodeResult decodedFinal = reader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(finalAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstLayer.GetLayerId(), Is.EqualTo(0));
            Assert.That(finalLayer.GetLayerId(), Is.EqualTo(1));
            Assert.That(layered.IsProgressive(), Is.False);
            Assert.That(itemId, Is.EqualTo(2));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount(), Is.EqualTo(2));
            Assert.That((previewItem ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That((previewSelector ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerId(), Is.EqualTo(0));
            Assert.That((selector ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerId(), Is.EqualTo(1));
            Assert.That((previewItem.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(),
                Is.EqualTo((((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId) ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset()));
            Assert.That((previewItem.GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedLength(),
                Is.EqualTo((((read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId) ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedLength()));
            Assert.That(decodedPreview.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedFinal.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertPlaneValue(decodedPreview.GetImage(), 41);
        AssertPlaneValue(decodedFinal.GetImage(), 109);

        (decodedPreview.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (decodedFinal.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        firstImage.Close();
        finalImage.Close();
        writer.Close();
    }

    [Test]
    public void UsesLargeLayerIndexSizesWhenPreviewSegmentExceedsSixteenBits()
    {
        AvifImage preview = CreateImage(300, 300, 31);
        AvifImage final = CreateImage(300, 300, 211);
        AvifLayeredImage layered = AvifLayeredImage.Create();
        layered.AddLayer(preview);
        layered.AddLayer(final);
        AvifWriter writer = AvifWriter.Create();
        uint itemId = writer.AddLayeredImage(layered);
        AvifEncodeResult finished = writer.Finish();
        AvifReadResult read = Avif.Read(finished.GetData());
        AvifItemProperty? index = FindProperty(
            (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId),
            "a1lx");

        Assert.Multiple(() =>
        {
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((index ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerCount(), Is.EqualTo(2));
            Assert.That(index.UsesLargeLayerSizes(), Is.True);
            Assert.That(index.GetLayerSize(0), Is.GreaterThan(ushort.MaxValue));
        });

        preview.Close();
        final.Close();
        writer.Close();
    }

    [Test]
    public void RejectsInvalidLayerSetsWithoutConsumingAnItemId()
    {
        AvifImage large = CreateImage(3, 3, 11);
        AvifImage small = CreateImage(2, 2, 13);
        AvifImage color = AvifImage.Create(
            3,
            3,
            8,
            AvifChromaFormat.Yuv444,
            false);
        AvifLayeredImage invalidOperatingPoint = AvifLayeredImage.Create(32, true);
        AvifLayeredImage incomplete = AvifLayeredImage.Create();
        AvifResultCode onlyLayer = incomplete.AddLayer(large);
        AvifLayeredImage incompatible = AvifLayeredImage.Create();
        AvifResultCode largeAdded = incompatible.AddLayer(large);
        AvifResultCode smallerAdded = incompatible.AddLayer(small);
        AvifResultCode colorAdded = incompatible.AddLayer(color);
        AvifLayeredImage capped = AvifLayeredImage.Create();
        capped.AddLayer(large);
        capped.AddLayer(large);
        capped.AddLayer(large);
        capped.AddLayer(large);
        AvifResultCode fifthLayer = capped.AddLayer(large);
        AvifWriter writer = AvifWriter.Create();
        uint incompleteId = writer.AddLayeredImage(incomplete);
        uint nextId = writer.AddImage(large);

        Assert.Multiple(() =>
        {
            Assert.That(invalidOperatingPoint.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(onlyLayer, Is.EqualTo(AvifResultCode.Success));
            Assert.That(largeAdded, Is.EqualTo(AvifResultCode.Success));
            Assert.That(smallerAdded,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(colorAdded,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(capped.GetLayerCount(), Is.EqualTo(4));
            Assert.That(fifthLayer, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(incompleteId, Is.Zero);
            Assert.That(nextId, Is.EqualTo(1));
        });

        large.Close();
        small.Close();
        color.Close();
        writer.Close();
    }

    static AvifItemProperty? FindProperty(AvifImageItem? item, string type)
    {
        int index = 0;
        while (index < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(index);
            if ((property ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return property;
            }
            index++;
        }
        return null;
    }

    static AvifImage CreateImage(int width, int height, byte value)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            8,
            AvifChromaFormat.Monochrome,
            false);
        AvifPlane? plane = image.GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            plane.GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    static void AssertPlaneValue(AvifImage? image, byte expected)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            int column = 0;
            while (column < plane.GetWidth())
            {
                Assert.That(plane.GetRowSpan(row)[column], Is.EqualTo(expected));
                column++;
            }
            row++;
        }
    }
}
