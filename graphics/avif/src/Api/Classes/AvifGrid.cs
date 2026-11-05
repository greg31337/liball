using System;
using System.Collections.Generic;

namespace avif;

/// <summary>
/// Parses and incrementally assembles one HEIF grid derived image.
/// </summary>
/// <remarks>
/// Decoded dependency images remain owned by the caller. Repeated item identifiers share one
/// cached image, and region rendering only requires cells intersecting the requested rectangle.
/// </remarks>
public class AvifGrid : IAvifGrid, IAvifDiagnosticSource
{
    AvifResultCode code;
    int rows;
    int columns;
    int width;
    int height;
    List<AvifGridCell>? cells;
    Dictionary<uint, AvifImage>? decodedItems;
    AvifImage? templateImage;
    AvifSecurityLimits? limits;
    int cellWidth;
    int cellHeight;

    /// <summary>Creates an empty authoring grid with default resource limits.</summary>
    /// <param name="rows">The cell row count from one through 256.</param>
    /// <param name="columns">The cell column count from one through 256.</param>
    /// <param name="width">The positive visible output width.</param>
    /// <param name="height">The positive visible output height.</param>
    /// <returns>The initialized authoring grid.</returns>
    internal static AvifGrid Create(
        int rows,
        int columns,
        int width,
        int height)
    {
        return Create(
            rows,
            columns,
            width,
            height,
            new AvifSecurityLimits());
    }

    /// <summary>Creates an empty authoring grid with explicit resource limits.</summary>
    /// <param name="rows">The cell row count from one through 256.</param>
    /// <param name="columns">The cell column count from one through 256.</param>
    /// <param name="width">The positive visible output width.</param>
    /// <param name="height">The positive visible output height.</param>
    /// <param name="limits">The dimensions, pixel count, and tile count limits.</param>
    /// <returns>The initialized authoring grid.</returns>
    internal static AvifGrid Create(
        int rows,
        int columns,
        int width,
        int height,
        AvifSecurityLimits limits)
    {
        return new AvifGrid(rows, columns, width, height, limits);
    }

    /// <summary>Parses a grid payload whose cells will be supplied by row-major index.</summary>
    /// <param name="payload">The complete grid derived-image item payload.</param>
    /// <returns>The parsed grid.</returns>
    internal static AvifGrid Create(byte[] payload)
    {
        return Create(payload, System.Array.Empty<uint>(), new AvifSecurityLimits());
    }

    /// <summary>Parses a grid payload and binds its row-major derived-image dependencies.</summary>
    /// <param name="payload">The complete grid derived-image item payload.</param>
    /// <param name="cellItemIds">The row-major <c>dimg</c> target item identifiers.</param>
    /// <returns>The parsed grid.</returns>
    internal static AvifGrid Create(byte[] payload, uint[] cellItemIds)
    {
        return Create(payload, cellItemIds, new AvifSecurityLimits());
    }

    /// <summary>Creates an empty authoring grid with explicit dimensions and resource limits.</summary>
    /// <param name="rows">The cell row count from one through 256.</param>
    /// <param name="columns">The cell column count from one through 256.</param>
    /// <param name="width">The positive visible output width.</param>
    /// <param name="height">The positive visible output height.</param>
    /// <param name="limits">The dimensions, pixel count, tile count, and allocation limits.</param>
    public AvifGrid(
        int rows,
        int columns,
        int width,
        int height,
        AvifSecurityLimits limits)
    {
        InitializeAuthoring(rows, columns, width, height, limits);
    }

    /// <summary>Parses a grid with explicit dependencies and resource limits.</summary>
    /// <returns>The parsed grid.</returns>
    internal static AvifGrid Create(
        byte[] payload,
        uint[] cellItemIds,
        AvifSecurityLimits limits)
    {
        AvifGrid result = new AvifGrid(1, 1, 1, 1, limits);
        result.Initialize(payload, cellItemIds, limits, true);
        return result;
    }

    /// <summary>Returns the grid-payload parsing result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns diagnostics for grid parsing, assembly, or rendering state.</summary>
    /// <returns>The structured rendering diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Rendering,
            "The grid rendering operation did not complete successfully.");
    }

    /// <summary>Returns the number of cell rows.</summary>
    /// <returns>One through 256, or zero when parsing failed.</returns>
    public int GetRowCount()
    {
        return rows;
    }

    /// <summary>Returns the number of cell columns.</summary>
    /// <returns>One through 256, or zero when parsing failed.</returns>
    public int GetColumnCount()
    {
        return columns;
    }

    /// <summary>Returns the declared output width.</summary>
    /// <returns>The luma width in pixels, or zero when parsing failed.</returns>
    public int GetWidth()
    {
        return width;
    }

    /// <summary>Returns the declared output height.</summary>
    /// <returns>The luma height in pixels, or zero when parsing failed.</returns>
    public int GetHeight()
    {
        return height;
    }

    /// <summary>Returns the row-major cell count.</summary>
    /// <returns>The product of row and column counts.</returns>
    public int GetCellCount()
    {
        return (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns one row-major cell descriptor.</summary>
    /// <param name="index">The zero-based cell index.</param>
    /// <returns>The cell, or null for an invalid index.</returns>
    public AvifGridCell? GetCell(int index)
    {
        if (index < 0 || index >= (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            return null;
        }
        return cells[index];
    }

    /// <summary>Sets the common decoded cell dimensions from dependency item metadata.</summary>
    /// <param name="cellWidth">The positive luma width of every dependency image.</param>
    /// <param name="cellHeight">The positive luma height of every dependency image.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetCellDimensions(int cellWidth, int cellHeight)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (templateImage != null
            && (templateImage.GetWidth() != cellWidth
                || templateImage.GetHeight() != cellHeight))
        {
            return AvifResultCode.NonConformingData;
        }
        AvifResultCode validation = GridRenderer.ValidateCellDimensions(
            cellWidth,
            cellHeight,
            rows,
            columns,
            width,
            height);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        this.cellWidth = cellWidth;
        this.cellHeight = cellHeight;
        SetCellGeometry();
        return AvifResultCode.Success;
    }

    /// <summary>Returns the common decoded cell width.</summary>
    /// <returns>The luma width, or zero before geometry is known.</returns>
    public int GetCellWidth()
    {
        return cellWidth;
    }

    /// <summary>Returns the common decoded cell height.</summary>
    /// <returns>The luma height, or zero before geometry is known.</returns>
    public int GetCellHeight()
    {
        return cellHeight;
    }

    /// <summary>Associates a decoded image with one row-major cell.</summary>
    /// <param name="index">The zero-based cell index.</param>
    /// <param name="image">The caller-owned decoded dependency image.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetDecodedCell(int index, AvifImage image)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        AvifGridCell? cell = GetCell(index);
        if (cell == null || image == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (cell.GetItemId() != 0)
        {
            return SetDecodedItem(cell.GetItemId(), image);
        }

        AvifResultCode validation = ValidateImage(image);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        cell.SetImage(image);
        return AvifResultCode.Success;
    }

    /// <summary>Sets one caller-owned image as a row-major authoring cell.</summary>
    /// <param name="index">The zero-based cell index.</param>
    /// <param name="image">The image that a writer will encode independently.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetCell(int index, AvifImage image)
    {
        return SetDecodedCell(index, image);
    }

    /// <summary>Caches a decoded dependency and attaches it to every cell using that item.</summary>
    /// <param name="itemId">The nonzero derived-image dependency identifier.</param>
    /// <param name="image">The caller-owned decoded dependency image.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetDecodedItem(uint itemId, AvifImage? image)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (itemId == 0 || image == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if ((decodedItems ?? throw new InvalidOperationException("Invalid internal state.")).ContainsKey(itemId))
        {
            return AvifResultCode.Success;
        }

        bool found = false;
        int index = 0;
        while (index < (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (cells[index].GetItemId() == itemId)
            {
                found = true;
                break;
            }
            index++;
        }
        if (!found)
        {
            return AvifResultCode.NotFound;
        }

        AvifResultCode validation = ValidateImage(image);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        decodedItems.Add(itemId, image);
        index = 0;
        while (index < cells.Count)
        {
            if (cells[index].GetItemId() == itemId)
            {
                cells[index].SetImage(image);
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    /// <summary>Returns the number of distinct decoded dependency items retained by the grid.</summary>
    /// <returns>The cache entry count.</returns>
    public int GetCachedImageCount()
    {
        return (decodedItems ?? throw new InvalidOperationException("Invalid internal state.")).Count;
    }

    /// <summary>Returns the number of cells whose decoded samples are available.</summary>
    /// <returns>The decoded cell count, including cells sharing one dependency.</returns>
    public int GetDecodedCellCount()
    {
        int count = 0;
        int index = 0;
        while (index < (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (cells[index].IsDecoded())
            {
                count++;
            }
            index++;
        }
        return count;
    }

    /// <summary>Reports whether every cell has decoded samples.</summary>
    /// <returns><c>true</c> when a complete render can be produced.</returns>
    public bool IsComplete()
    {
        return (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count > 0 && GetDecodedCellCount() == cells.Count;
    }

    /// <summary>Returns the number of completely decoded leading rows.</summary>
    /// <returns>The contiguous visible row count from the top of the grid.</returns>
    public int GetDecodedRowCount()
    {
        int row = 0;
        while (row < rows)
        {
            int column = 0;
            while (column < columns)
            {
                if (!(cells ?? throw new InvalidOperationException("Invalid internal state."))[row * columns + column].IsDecoded())
                {
                    return row;
                }
                column++;
            }
            row++;
        }
        return row;
    }

    /// <summary>Counts cells intersecting a valid luma rectangle.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The positive luma width.</param>
    /// <param name="height">The positive luma height.</param>
    /// <returns>The required cell count, or zero for an invalid rectangle.</returns>
    public int GetRequiredCellCount(
        int x,
        int y,
        int width,
        int height)
    {
        if (code != AvifResultCode.Success
            || cellWidth == 0
            || !GridRenderer.ValidateRegion(
                this.width, this.height, x, y, width, height))
        {
            return 0;
        }
        int count = 0;
        int index = 0;
        while (index < (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            if (GridRenderer.Intersects(cells[index], x, y, width, height))
            {
                count++;
            }
            index++;
        }
        return count;
    }

    /// <summary>Returns one cell intersecting a valid luma rectangle.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The positive luma width.</param>
    /// <param name="height">The positive luma height.</param>
    /// <param name="index">The zero-based index among intersecting cells.</param>
    /// <returns>The required cell, or null for invalid arguments.</returns>
    public AvifGridCell? GetRequiredCell(
        int x,
        int y,
        int width,
        int height,
        int index)
    {
        if (index < 0 || GetRequiredCellCount(x, y, width, height) == 0)
        {
            return null;
        }
        int found = 0;
        int cellIndex = 0;
        while (cellIndex < (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            AvifGridCell cell = cells[cellIndex];
            if (GridRenderer.Intersects(cell, x, y, width, height))
            {
                if (found == index)
                {
                    return cell;
                }
                found++;
            }
            cellIndex++;
        }
        return null;
    }

    /// <summary>Renders the complete grid after all dependencies are decoded.</summary>
    /// <returns>An owning assembled image, or an image carrying <c>NeedMoreData</c>.</returns>
    public AvifImage Render()
    {
        return GridRenderer.Render(this, 0, 0, width, height, false);
    }

    /// <summary>Renders only cells intersecting a requested luma rectangle.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The positive luma width.</param>
    /// <param name="height">The positive luma height.</param>
    /// <returns>An owning region image, or an image carrying a stable error code.</returns>
    public AvifImage RenderRegion(
        int x,
        int y,
        int width,
        int height)
    {
        return GridRenderer.Render(this, x, y, width, height, false);
    }

    /// <summary>Assembles decoded cells and leaves unavailable cells initialized to zero.</summary>
    /// <returns>An owning partial image, or an image carrying <c>NeedMoreData</c> before any cell.</returns>
    public AvifImage RenderAvailable()
    {
        return GridRenderer.Render(this, 0, 0, width, height, true);
    }

    void Initialize(
        byte[] payload,
        uint[]? cellItemIds,
        AvifSecurityLimits requestedLimits,
        bool requireItemIds)
    {
        cells = new List<AvifGridCell>();
        decodedItems = new Dictionary<uint, AvifImage>();
        templateImage = null;
        cellWidth = 0;
        cellHeight = 0;
        limits = requestedLimits == null
            ? new AvifSecurityLimits()
            : ResourceGuard.CopyLimits(requestedLimits);
        if (requireItemIds && cellItemIds == null)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }

        int dependencyCount = cellItemIds == null ? -1 : cellItemIds.Length;
        GridItemParseResult parsed = GridItemParser.Parse(
            payload,
            dependencyCount,
            requestedLimits);
        code = parsed.Code;
        if (code != AvifResultCode.Success)
        {
            return;
        }

        rows = parsed.Rows;
        columns = parsed.Columns;
        width = parsed.OutputWidth;
        height = parsed.OutputHeight;
        int index = 0;
        while (index < rows * columns)
        {
            uint itemId = cellItemIds == null ? 0 : cellItemIds[index];
            if (cellItemIds != null && itemId == 0)
            {
                cells.Clear();
                rows = 0;
                columns = 0;
                width = 0;
                height = 0;
                code = AvifResultCode.InvalidItem;
                return;
            }
            cells.Add(new AvifGridCell(
                index,
                index / columns,
                index % columns,
                itemId));
            index++;
        }
    }

    void InitializeAuthoring(
        int requestedRows,
        int requestedColumns,
        int requestedWidth,
        int requestedHeight,
        AvifSecurityLimits requestedLimits)
    {
        cells = new List<AvifGridCell>();
        decodedItems = new Dictionary<uint, AvifImage>();
        templateImage = null;
        cellWidth = 0;
        cellHeight = 0;
        limits = requestedLimits == null
            ? new AvifSecurityLimits()
            : ResourceGuard.CopyLimits(requestedLimits);
        if (requestedLimits == null
            || ResourceGuard.ValidateLimits(requestedLimits)
                != AvifResultCode.Success
            || requestedRows < 1
            || requestedRows > 256
            || requestedColumns < 1
            || requestedColumns > 256)
        {
            code = AvifResultCode.InvalidArgument;
            return;
        }
        int requestedCellCount = requestedRows * requestedColumns;
        if (requestedCellCount > limits.MaxTileCount)
        {
            code = AvifResultCode.LimitExceeded;
            return;
        }
        ResourceGuard guard = new ResourceGuard(limits);
        code = guard.CheckImageDimensions(requestedWidth, requestedHeight);
        if (code != AvifResultCode.Success)
        {
            return;
        }

        rows = requestedRows;
        columns = requestedColumns;
        width = requestedWidth;
        height = requestedHeight;
        int index = 0;
        while (index < requestedCellCount)
        {
            cells.Add(new AvifGridCell(
                index,
                index / columns,
                index % columns,
                0));
            index++;
        }
        code = AvifResultCode.Success;
    }

    AvifResultCode ValidateImage(AvifImage image)
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
        if (templateImage != null)
        {
            return GridRenderer.AreCompatible(templateImage, image)
                ? AvifResultCode.Success
                : AvifResultCode.NonConformingData;
        }

        AvifResultCode geometryCode = GridRenderer.ValidateCellGeometry(
            image,
            rows,
            columns,
            width,
            height);
        if (geometryCode != AvifResultCode.Success)
        {
            return geometryCode;
        }
        if (cellWidth != 0
            && (image.GetWidth() != cellWidth || image.GetHeight() != cellHeight))
        {
            return AvifResultCode.NonConformingData;
        }
        templateImage = image;
        cellWidth = image.GetWidth();
        cellHeight = image.GetHeight();
        SetCellGeometry();
        return AvifResultCode.Success;
    }

    void SetCellGeometry()
    {
        int index = 0;
        while (index < (cells ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            cells[index].SetGeometry(
                cellWidth, cellHeight, width, height);
            index++;
        }
    }

    internal AvifImage? GetTemplateImageValue()
    {
        return templateImage;
    }

    internal AvifSecurityLimits? GetLimitsValue()
    {
        return limits;
    }
}
