using System.Buffers.Binary;

namespace avif.Tests;

public class GridAuthoringTests
{
    [Test]
    public void AuthorsIndependentTilesAndCropsRightAndBottomBorders()
    {
        AvifImage topLeft = CreateImage(3, 2, 17);
        AvifImage topRight = CreateImage(3, 2, 53);
        AvifImage bottomLeft = CreateImage(3, 2, 101);
        AvifImage bottomRight = CreateImage(3, 2, 149);
        AvifGrid grid = AvifGrid.Create(2, 2, 5, 3);
        Assert.That(grid.SetCell(0, topLeft), Is.EqualTo(AvifResultCode.Success));
        Assert.That(grid.SetCell(1, topRight), Is.EqualTo(AvifResultCode.Success));
        Assert.That(grid.SetCell(2, bottomLeft), Is.EqualTo(AvifResultCode.Success));
        Assert.That(grid.SetCell(3, bottomRight), Is.EqualTo(AvifResultCode.Success));

        AvifWriter writer = AvifWriter.Create();
        uint gridId = writer.AddGrid(grid);
        topLeft.Close();
        topRight.Close();
        bottomLeft.Close();
        bottomRight.Close();
        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifDocument? document = reader.GetDocument();
        AvifImageItem? gridItem = (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(gridId);
        AvifItemReference? derived = (gridItem ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReference(0);
        AvifDecodeResult rendered = reader.DecodePrimaryImage();
        AvifDecodeResult tile = reader.DecodeTile(gridId, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(gridId, Is.EqualTo(5));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(document.GetItemCount(), Is.EqualTo(5));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId(), Is.EqualTo(gridId));
            Assert.That(gridItem.GetItemType(), Is.EqualTo("grid"));
            Assert.That(gridItem.IsHidden(), Is.False);
            Assert.That(gridItem.IsDerivedImage(), Is.True);
            Assert.That((derived ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(), Is.EqualTo(AvifItemReferenceKind.DerivedImage));
            Assert.That(derived.GetTargetCount(), Is.EqualTo(4));
            Assert.That(derived.GetTargetItemId(0), Is.EqualTo(1));
            Assert.That(derived.GetTargetItemId(1), Is.EqualTo(2));
            Assert.That(derived.GetTargetItemId(2), Is.EqualTo(3));
            Assert.That(derived.GetTargetItemId(3), Is.EqualTo(4));
            Assert.That((document.FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That((document.FindItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That((document.FindItem(3) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That((document.FindItem(4) ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That(((document.FindItem(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(),
                Is.Not.EqualTo(((document.FindItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset()));
            Assert.That(((document.FindItem(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(),
                Is.Not.EqualTo(((document.FindItem(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset()));
            Assert.That(((document.FindItem(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset(),
                Is.Not.EqualTo(((document.FindItem(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetResolvedOffset()));
            Assert.That(rendered.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((rendered.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(5));
            Assert.That((rendered.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(3));
            Assert.That(tile.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((tile.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(3));
            Assert.That((tile.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(2));
        });
        AssertGridPixels(rendered.GetImage());
        AssertPlaneValue(tile.GetImage(), 149);

        (rendered.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (tile.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
    }

    [Test]
    public void RejectsIncompleteAndNonconformingGridsWithoutConsumingIds()
    {
        AvifImage image = CreateImage(3, 2, 29);
        AvifImage incompatible = AvifImage.Create(
            3,
            2,
            8,
            AvifChromaFormat.Yuv444,
            false);
        AvifGrid incomplete = AvifGrid.Create(2, 2, 5, 3);
        AvifResultCode firstCell = incomplete.SetCell(0, image);
        AvifResultCode incompatibleCell = incomplete.SetCell(1, incompatible);
        AvifGrid invalidBorder = AvifGrid.Create(1, 2, 3, 2);
        AvifResultCode borderCell = invalidBorder.SetCell(0, image);
        AvifGrid invalidRows = AvifGrid.Create(0, 2, 5, 3);
        AvifWriter writer = AvifWriter.Create();
        uint incompleteId = writer.AddGrid(incomplete);
        uint nextId = writer.AddImage(image);

        Assert.Multiple(() =>
        {
            Assert.That(firstCell, Is.EqualTo(AvifResultCode.Success));
            Assert.That(incompatibleCell,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(borderCell,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(invalidRows.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(incompleteId, Is.Zero);
            Assert.That(nextId, Is.EqualTo(1));
        });

        image.Close();
        incompatible.Close();
        writer.Close();
    }

    [Test]
    public void AuthorsLargeGridPayloadAndRaisesIndependentTileLevel()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxWidth = 100000;
        limits.MaxHeight = 16;
        limits.MaxPixels = 1000000;
        AvifEncoderOptions encoderOptions = new AvifEncoderOptions();
        encoderOptions.Limits = limits;
        AvifDecoderOptions decoderOptions = new AvifDecoderOptions();
        decoderOptions.Limits = limits;
        AvifImage tileImage = CreateImage(9000, 1, 67);
        AvifGrid grid = AvifGrid.Create(1, 8, 70000, 1, limits);
        int cellIndex = 0;
        while (cellIndex < grid.GetCellCount())
        {
            Assert.That(
                grid.SetCell(cellIndex, tileImage),
                Is.EqualTo(AvifResultCode.Success));
            cellIndex++;
        }

        AvifWriter writer = AvifWriter.Create(encoderOptions);
        uint gridId = writer.AddGrid(grid);
        Assert.That(gridId, Is.EqualTo(9));
        AvifEncodeResult finished = writer.Finish();
        AvifReader reader = AvifReader.Create(finished.GetData(), decoderOptions);
        AvifResultCode parsed = reader.Parse();
        Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
        byte[] gridPayload = reader.GetEncodedItem(gridId);
        AvifDocument? document = reader.GetDocument();
        AvifItemProperty? configuration = FindProperty(
            (document ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1),
            "av1C");

        Assert.Multiple(() =>
        {
            Assert.That(gridId, Is.EqualTo(9));
            Assert.That(finished.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(gridPayload.Length, Is.EqualTo(12));
            Assert.That(gridPayload[0], Is.Zero);
            Assert.That(gridPayload[1], Is.EqualTo(1));
            Assert.That(gridPayload[2], Is.Zero);
            Assert.That(gridPayload[3], Is.EqualTo(7));
            Assert.That(BinaryPrimitives.ReadUInt32BigEndian(
                gridPayload.AsSpan(4, 4)), Is.EqualTo(70000));
            Assert.That(BinaryPrimitives.ReadUInt32BigEndian(
                gridPayload.AsSpan(8, 4)), Is.EqualTo(1));
            Assert.That((configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetAv1Profile(), Is.EqualTo(0));
            Assert.That(configuration.GetAv1LevelIndex(), Is.EqualTo(16));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("MA1B"), Is.False);
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("MA1A"), Is.False);
        });

        tileImage.Close();
        reader.Close();
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

    static void AssertGridPixels(AvifImage? image)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            int column = 0;
            while (column < plane.GetWidth())
            {
                byte expected;
                if (row < 2)
                {
                    expected = column < 3 ? (byte)17 : (byte)53;
                }
                else
                {
                    expected = column < 3 ? (byte)101 : (byte)149;
                }
                Assert.That(plane.GetRowSpan(row)[column], Is.EqualTo(expected));
                column++;
            }
            row++;
        }
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
