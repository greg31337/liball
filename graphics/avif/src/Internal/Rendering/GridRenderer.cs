using System;

namespace avif;

internal static class GridRenderer
{
    internal static AvifResultCode ValidateCellGeometry(
        AvifImage image,
        int rows,
        int columns,
        int outputWidth,
        int outputHeight)
    {
        if (image == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode imageCode = image.GetCode();
        if (imageCode != AvifResultCode.Success)
        {
            return imageCode;
        }

        int cellWidth = image.GetWidth();
        int cellHeight = image.GetHeight();
        AvifResultCode dimensionsCode = ValidateCellDimensions(
            cellWidth,
            cellHeight,
            rows,
            columns,
            outputWidth,
            outputHeight);
        if (dimensionsCode != AvifResultCode.Success)
        {
            return dimensionsCode;
        }

        AvifChromaFormat chroma = image.GetChromaFormat();
        bool subsamplingX = chroma == AvifChromaFormat.Yuv420
            || chroma == AvifChromaFormat.Yuv422;
        bool subsamplingY = chroma == AvifChromaFormat.Yuv420;
        if (columns > 1 && subsamplingX && (cellWidth & 1) != 0
            || rows > 1 && subsamplingY && (cellHeight & 1) != 0)
        {
            return AvifResultCode.NonConformingData;
        }
        return AvifResultCode.Success;
    }

    internal static AvifResultCode ValidateCellDimensions(
        int cellWidth,
        int cellHeight,
        int rows,
        int columns,
        int outputWidth,
        int outputHeight)
    {
        if (cellWidth <= 0 || cellHeight <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        long minimumWidth = (long)(columns - 1) * cellWidth;
        long maximumWidth = (long)columns * cellWidth;
        long minimumHeight = (long)(rows - 1) * cellHeight;
        long maximumHeight = (long)rows * cellHeight;
        if (outputWidth <= minimumWidth
            || outputWidth > maximumWidth
            || outputHeight <= minimumHeight
            || outputHeight > maximumHeight)
        {
            return AvifResultCode.NonConformingData;
        }

        return AvifResultCode.Success;
    }

    internal static bool AreCompatible(AvifImage first, AvifImage second)
    {
        if (first == null || second == null
            || first.GetCode() != AvifResultCode.Success
            || second.GetCode() != AvifResultCode.Success
            || first.GetWidth() != second.GetWidth()
            || first.GetHeight() != second.GetHeight()
            || first.GetBitDepth() != second.GetBitDepth()
            || first.GetChromaFormat() != second.GetChromaFormat()
            || first.GetChromaSamplePosition() != second.GetChromaSamplePosition()
            || first.GetRange() != second.GetRange()
            || first.GetAlphaMode() != second.GetAlphaMode())
        {
            return false;
        }

        AvifColorInfo? firstColor = first.GetColorInfo();
        AvifColorInfo? secondColor = second.GetColorInfo();
        if ((firstColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries != (secondColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries
            || firstColor.TransferCharacteristics
                != secondColor.TransferCharacteristics
            || firstColor.MatrixCoefficients != secondColor.MatrixCoefficients
            || firstColor.Range != secondColor.Range)
        {
            return false;
        }

        AvifIccProfile? firstIcc = first.GetIccProfile();
        AvifIccProfile? secondIcc = second.GetIccProfile();
        if (firstIcc == null || secondIcc == null)
        {
            return firstIcc == null && secondIcc == null;
        }
        byte[] firstData = firstIcc.GetData();
        byte[] secondData = secondIcc.GetData();
        if (firstData.Length != secondData.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstData.Length)
        {
            if (firstData[index] != secondData[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ValidateRegion(
        int gridWidth,
        int gridHeight,
        int x,
        int y,
        int width,
        int height)
    {
        return x >= 0
            && y >= 0
            && width > 0
            && height > 0
            && x <= gridWidth
            && y <= gridHeight
            && width <= gridWidth - x
            && height <= gridHeight - y;
    }

    internal static bool RegionIsChromaAligned(
        AvifImage template,
        int x,
        int y)
    {
        AvifChromaFormat chroma = template.GetChromaFormat();
        bool subsamplingX = chroma == AvifChromaFormat.Yuv420
            || chroma == AvifChromaFormat.Yuv422;
        bool subsamplingY = chroma == AvifChromaFormat.Yuv420;
        return (!subsamplingX || (x & 1) == 0)
            && (!subsamplingY || (y & 1) == 0);
    }

    internal static AvifImage Render(
        AvifGrid grid,
        int x,
        int y,
        int width,
        int height,
        bool allowMissing)
    {
        if (grid == null)
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifResultCode gridCode = grid.GetCode();
        if (gridCode != AvifResultCode.Success)
        {
            return AvifImage.FromCode(gridCode);
        }
        if (!ValidateRegion(
            grid.GetWidth(), grid.GetHeight(), x, y, width, height))
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }

        AvifImage? template = grid.GetTemplateImageValue();
        if (template == null)
        {
            return AvifImage.FromCode(AvifResultCode.NeedMoreData);
        }
        if (template.GetCode() != AvifResultCode.Success)
        {
            return AvifImage.Create(template.GetCode());
        }
        if (!RegionIsChromaAligned(template, x, y))
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }
        if (!allowMissing && !HasRequiredCells(grid, x, y, width, height))
        {
            return AvifImage.FromCode(AvifResultCode.NeedMoreData);
        }

        AvifImage output = AvifImage.Create(
            width,
            height,
            template.GetBitDepth(),
            template.GetChromaFormat(),
            template.GetRange(),
            template.GetChromaSamplePosition(),
            template.GetAlphaMode(),
            1,
            grid.GetLimitsValue());
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }

        AvifResultCode metadataCode = output.SetColorInfo(
            template.GetColorInfo());
        if (metadataCode == AvifResultCode.Success)
        {
            metadataCode = output.SetIccProfile(template.GetIccProfile());
        }
        if (metadataCode != AvifResultCode.Success)
        {
            output.Close();
            return AvifImage.FromCode(metadataCode);
        }

        int planeIndex = 0;
        while (planeIndex < 4)
        {
            AvifPlaneKind kind = (AvifPlaneKind)planeIndex;
            AvifPlane? destination = output.GetPlane(kind);
            if (destination != null)
            {
                CopyPlane(grid, destination, kind, x, y);
            }
            planeIndex++;
        }
        return output;
    }

    static bool HasRequiredCells(
        AvifGrid grid,
        int x,
        int y,
        int width,
        int height)
    {
        int index = 0;
        while (index < grid.GetCellCount())
        {
            AvifGridCell? cell = grid.GetCell(index);
            if (Intersects(cell, x, y, width, height) && !(cell ?? throw new InvalidOperationException("Invalid internal state.")).IsDecoded())
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool Intersects(
        AvifGridCell? cell,
        int x,
        int y,
        int width,
        int height)
    {
        return (cell ?? throw new InvalidOperationException("Invalid internal state.")).GetX() < x + width
            && cell.GetY() < y + height
            && x < cell.GetX() + cell.GetVisibleWidth()
            && y < cell.GetY() + cell.GetVisibleHeight();
    }

    static void CopyPlane(
        AvifGrid grid,
        AvifPlane destination,
        AvifPlaneKind kind,
        int regionX,
        int regionY)
    {
        AvifImage? template = grid.GetTemplateImageValue();
        int horizontalShift = 0;
        int verticalShift = 0;
        if (kind == AvifPlaneKind.U || kind == AvifPlaneKind.V)
        {
            AvifChromaFormat chroma = (template ?? throw new InvalidOperationException("Invalid internal state.")).GetChromaFormat();
            if (chroma == AvifChromaFormat.Yuv420
                || chroma == AvifChromaFormat.Yuv422)
            {
                horizontalShift = 1;
            }
            if (chroma == AvifChromaFormat.Yuv420)
            {
                verticalShift = 1;
            }
        }

        AvifPlane? templatePlane = (template ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(kind);
        int cellPlaneWidth = (templatePlane ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth();
        int cellPlaneHeight = templatePlane.GetHeight();
        int sourceOriginX = regionX >> horizontalShift;
        int sourceOriginY = regionY >> verticalShift;
        int bytesPerSample = template.GetBitDepth() > 8 ? 2 : 1;
        int outputY = 0;
        while (outputY < destination.GetHeight())
        {
            int globalY = sourceOriginY + outputY;
            int cellRow = globalY / cellPlaneHeight;
            int sourceY = globalY - cellRow * cellPlaneHeight;
            Span<byte> destinationRow = destination.GetRowSpan(outputY);
            int outputX = 0;
            while (outputX < destination.GetWidth())
            {
                int globalX = sourceOriginX + outputX;
                int cellColumn = globalX / cellPlaneWidth;
                int sourceX = globalX - cellColumn * cellPlaneWidth;
                int run = cellPlaneWidth - sourceX;
                if (run > destination.GetWidth() - outputX)
                {
                    run = destination.GetWidth() - outputX;
                }

                int cellIndex = cellRow * grid.GetColumnCount() + cellColumn;
                AvifGridCell? cell = grid.GetCell(cellIndex);
                if (cell != null && cell.IsDecoded())
                {
                    AvifPlane? source = (cell.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(kind);
                    (source ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(sourceY).Slice(
                        sourceX * bytesPerSample,
                        run * bytesPerSample).CopyTo(destinationRow.Slice(
                            outputX * bytesPerSample,
                            run * bytesPerSample));
                }
                outputX += run;
            }
            outputY++;
        }
    }
}
