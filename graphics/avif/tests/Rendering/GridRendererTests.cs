using System.Buffers.Binary;

namespace avif.Tests;

public class GridRendererTests
{
    [Test]
    public void GridPayloadParsesBothDimensionWidthsAndRejectsMalformedSyntax()
    {
        AvifGrid compact = AvifGrid.Create(
            CreatePayload(2, 3, 500, 300, false),
            new uint[] { 1, 2, 3, 4, 5, 6 });
        AvifSecurityLimits largeLimits = new AvifSecurityLimits();
        largeLimits.MaxWidth = 100000;
        largeLimits.MaxHeight = 100000;
        largeLimits.MaxPixels = 10000000000;
        AvifGrid large = AvifGrid.Create(
            CreatePayload(1, 1, 70000, 66000, true),
            new uint[] { 9 },
            largeLimits);

        byte[] reservedFlags = CreatePayload(1, 1, 2, 2, false);
        reservedFlags[1] = 2;
        byte[] wrongVersion = CreatePayload(1, 1, 2, 2, false);
        wrongVersion[0] = 1;
        AvifGrid mismatched = AvifGrid.Create(
            CreatePayload(2, 2, 4, 4, false),
            new uint[] { 1, 2, 3 });

        Assert.Multiple(() =>
        {
            Assert.That(compact.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(compact.GetRowCount(), Is.EqualTo(2));
            Assert.That(compact.GetColumnCount(), Is.EqualTo(3));
            Assert.That(compact.GetWidth(), Is.EqualTo(500));
            Assert.That(compact.GetHeight(), Is.EqualTo(300));
            Assert.That((compact.GetCell(5) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(6));
            Assert.That(large.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(large.GetWidth(), Is.EqualTo(70000));
            Assert.That(large.GetHeight(), Is.EqualTo(66000));
            Assert.That(
                AvifGrid.Create(reservedFlags).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(
                AvifGrid.Create(wrongVersion).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(
                AvifGrid.Create(new byte[7]).GetCode(),
                Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(mismatched.GetCode(), Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void CompleteRenderCropsRightAndBottomBorderCells()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(2, 2, 5, 3, false),
            new uint[] { 10, 20, 30, 40 });
        AvifImage topLeft = CreateMonochromeCell(3, 2, 10);
        AvifImage topRight = CreateMonochromeCell(3, 2, 20);
        AvifImage bottomLeft = CreateMonochromeCell(3, 2, 30);
        AvifImage bottomRight = CreateMonochromeCell(3, 2, 40);
        AvifResultCode firstSet = grid.SetDecodedCell(0, topLeft);
        grid.SetDecodedItem(20, topRight);
        grid.SetDecodedItem(30, bottomLeft);
        grid.SetDecodedItem(40, bottomRight);

        AvifImage output = grid.Render();

        Assert.Multiple(() =>
        {
            Assert.That(firstSet, Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetWidth(), Is.EqualTo(5));
            Assert.That(output.GetHeight(), Is.EqualTo(3));
            Assert.That(grid.GetCellWidth(), Is.EqualTo(3));
            Assert.That(grid.GetCellHeight(), Is.EqualTo(2));
            Assert.That((output.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 10, 10, 10, 20, 20 }));
            Assert.That((output.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(1).ToArray(),
                Is.EqualTo(new byte[] { 10, 10, 10, 20, 20 }));
            Assert.That((output.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(2).ToArray(),
                Is.EqualTo(new byte[] { 30, 30, 30, 40, 40 }));
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetX(), Is.EqualTo(3));
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetY(), Is.EqualTo(2));
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetRow(), Is.EqualTo(1));
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetColumn(), Is.EqualTo(1));
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).IsDecoded(), Is.True);
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetVisibleWidth(), Is.EqualTo(2));
            Assert.That((grid.GetCell(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetVisibleHeight(), Is.EqualTo(1));
        });

        output.Close();
        CloseImages(topLeft, topRight, bottomLeft, bottomRight);
    }

    [Test]
    public void RegionRenderingOnlyNeedsIntersectingDependency()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(2, 2, 6, 4, false),
            new uint[] { 1, 2, 3, 4 });
        AvifResultCode geometry = grid.SetCellDimensions(3, 2);
        int requiredCount = grid.GetRequiredCellCount(3, 0, 3, 2);
        AvifGridCell? required = grid.GetRequiredCell(3, 0, 3, 2, 0);
        AvifImage topRight = CreateMonochromeCell(3, 2, 22);
        grid.SetDecodedItem(2, topRight);

        AvifImage region = grid.RenderRegion(3, 0, 3, 2);
        AvifImage complete = grid.Render();

        Assert.Multiple(() =>
        {
            Assert.That(geometry, Is.EqualTo(AvifResultCode.Success));
            Assert.That(requiredCount, Is.EqualTo(1));
            Assert.That((required ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId(), Is.EqualTo(2));
            Assert.That(region.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((region.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 22, 22, 22 }));
            Assert.That(complete.GetCode(), Is.EqualTo(AvifResultCode.NeedMoreData));
        });

        region.Close();
        complete.Close();
        topRight.Close();
    }

    [Test]
    public void RequiredCellEnumerationUsesVisibleBorderGeometry()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(2, 3, 8, 3, false),
            new uint[] { 1, 2, 3, 4, 5, 6 });
        AvifResultCode geometry = grid.SetCellDimensions(3, 2);
        int count = grid.GetRequiredCellCount(2, 1, 5, 2);
        uint[] required = new uint[count];
        int index = 0;
        while (index < count)
        {
            required[index] = (grid.GetRequiredCell(
                2, 1, 5, 2, index) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(geometry, Is.EqualTo(AvifResultCode.Success));
            Assert.That(count, Is.EqualTo(6));
            Assert.That(required,
                Is.EqualTo(new uint[] { 1, 2, 3, 4, 5, 6 }));
            Assert.That(grid.GetRequiredCell(2, 1, 5, 2, count), Is.Null);
            Assert.That(grid.GetRequiredCellCount(-1, 0, 1, 1), Is.Zero);
            Assert.That(grid.GetRequiredCellCount(0, 0, 9, 1), Is.Zero);
        });
    }

    [Test]
    public void SharedDependenciesAreCachedOnceAndPopulateEveryMatchingCell()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(1, 2, 4, 2, false),
            new uint[] { 7, 7 });
        AvifImage shared = CreateMonochromeCell(2, 2, 55);
        AvifImage ignoredReplacement = CreateMonochromeCell(2, 2, 99);

        AvifResultCode first = grid.SetDecodedItem(7, shared);
        AvifResultCode second = grid.SetDecodedItem(7, ignoredReplacement);
        AvifImage output = grid.Render();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(AvifResultCode.Success));
            Assert.That(second, Is.EqualTo(AvifResultCode.Success));
            Assert.That(grid.GetCachedImageCount(), Is.EqualTo(1));
            Assert.That(grid.GetDecodedCellCount(), Is.EqualTo(2));
            Assert.That(grid.IsComplete(), Is.True);
            Assert.That((grid.GetCell(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetImage(), Is.SameAs(shared));
            Assert.That((grid.GetCell(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetImage(), Is.SameAs(shared));
            Assert.That((output.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 55, 55, 55, 55 }));
        });

        output.Close();
        shared.Close();
        ignoredReplacement.Close();
    }

    [Test]
    public void SubsampledPlanesAndAlphaFollowCellAndBorderGeometry()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(2, 2, 3, 3, false),
            new uint[] { 1, 2, 3, 4 });
        AvifImage[] cells = new AvifImage[4];
        int index = 0;
        while (index < cells.Length)
        {
            cells[index] = CreateYuvAlphaCell(
                (byte)(10 + index),
                (byte)(20 + index),
                (byte)(30 + index),
                (byte)(40 + index));
            grid.SetDecodedItem((uint)(index + 1), cells[index]);
            index++;
        }

        AvifImage output = grid.Render();

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(output.GetChromaFormat(), Is.EqualTo(AvifChromaFormat.Yuv420));
            Assert.That(output.GetAlphaMode(), Is.EqualTo(AvifAlphaMode.Straight));
            Assert.That((output.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(2).ToArray(),
                Is.EqualTo(new byte[] { 12, 12, 13 }));
            Assert.That((output.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 20, 21 }));
            Assert.That((output.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(1).ToArray(),
                Is.EqualTo(new byte[] { 22, 23 }));
            Assert.That((output.GetPlane(AvifPlaneKind.V) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(1).ToArray(),
                Is.EqualTo(new byte[] { 32, 33 }));
            Assert.That((output.GetPlane(AvifPlaneKind.Alpha) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(2).ToArray(),
                Is.EqualTo(new byte[] { 42, 42, 43 }));
        });

        output.Close();
        CloseImages(cells);
    }

    [Test]
    public void PartialAssemblyReportsLeadingRowsAndLeavesMissingCellsZeroed()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(2, 2, 4, 4, false),
            new uint[] { 1, 2, 3, 4 });
        AvifImage left = CreateMonochromeCell(2, 2, 11);
        AvifImage right = CreateMonochromeCell(2, 2, 12);
        grid.SetDecodedItem(1, left);
        grid.SetDecodedItem(2, right);

        AvifImage partial = grid.RenderAvailable();

        Assert.Multiple(() =>
        {
            Assert.That(grid.GetDecodedRowCount(), Is.EqualTo(1));
            Assert.That(grid.IsComplete(), Is.False);
            Assert.That(partial.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((partial.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 11, 11, 12, 12 }));
            Assert.That((partial.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(2).ToArray(),
                Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        });

        partial.Close();
        left.Close();
        right.Close();
    }

    [Test]
    public void CellValidationRejectsInvalidBorderGeometryAndIncompatibleImages()
    {
        AvifGrid grid = AvifGrid.Create(
            CreatePayload(1, 2, 5, 2, false),
            new uint[] { 1, 2 });
        AvifImage tooNarrow = CreateMonochromeCell(2, 2, 1);
        AvifImage valid = CreateMonochromeCell(3, 2, 2);
        AvifImage incompatible = AvifImage.Create(
            3, 2, 10, AvifChromaFormat.Monochrome, false);

        AvifResultCode narrowCode = grid.SetDecodedItem(1, tooNarrow);
        AvifResultCode validCode = grid.SetDecodedItem(1, valid);
        AvifResultCode incompatibleCode = grid.SetDecodedItem(2, incompatible);

        Assert.Multiple(() =>
        {
            Assert.That(narrowCode, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(validCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(incompatibleCode,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(grid.GetDecodedCellCount(), Is.EqualTo(1));
        });

        tooNarrow.Close();
        valid.Close();
        incompatible.Close();
    }

    private static byte[] CreatePayload(
        int rows,
        int columns,
        uint width,
        uint height,
        bool largeDimensions)
    {
        byte[] payload = new byte[largeDimensions ? 12 : 8];
        payload[1] = largeDimensions ? (byte)1 : (byte)0;
        payload[2] = (byte)(rows - 1);
        payload[3] = (byte)(columns - 1);
        if (largeDimensions)
        {
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(4, 4), width);
            BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(8, 4), height);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(4, 2), (ushort)width);
            BinaryPrimitives.WriteUInt16BigEndian(
                payload.AsSpan(6, 2), (ushort)height);
        }
        return payload;
    }

    private static AvifImage CreateMonochromeCell(
        int width,
        int height,
        byte value)
    {
        AvifImage image = AvifImage.Create(
            width, height, 8, AvifChromaFormat.Monochrome, false);
        int row = 0;
        while (row < height)
        {
            (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).Fill(value);
            row++;
        }
        return image;
    }

    private static AvifImage CreateYuvAlphaCell(
        byte yValue,
        byte uValue,
        byte vValue,
        byte alphaValue)
    {
        AvifImage image = AvifImage.Create(
            2,
            2,
            8,
            AvifChromaFormat.Yuv420,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.Straight,
            1,
            new AvifSecurityLimits());
        (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Fill(yValue);
        (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(1).Fill(yValue);
        (image.GetPlane(AvifPlaneKind.U) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Fill(uValue);
        (image.GetPlane(AvifPlaneKind.V) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Fill(vValue);
        (image.GetPlane(AvifPlaneKind.Alpha) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Fill(alphaValue);
        (image.GetPlane(AvifPlaneKind.Alpha) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(1).Fill(alphaValue);
        return image;
    }

    private static void CloseImages(params AvifImage[] images)
    {
        foreach (AvifImage image in images)
        {
            image.Close();
        }
    }
}
