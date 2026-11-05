using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifGrid"/>.</summary>
public interface IAvifGrid
{
    /// <summary>Returns the grid-payload parsing result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the number of cell rows.</summary>
    /// <returns>One through 256, or zero when parsing failed.</returns>
    int GetRowCount();

    /// <summary>Returns the number of cell columns.</summary>
    /// <returns>One through 256, or zero when parsing failed.</returns>
    int GetColumnCount();

    /// <summary>Returns the declared output width.</summary>
    /// <returns>The luma width in pixels, or zero when parsing failed.</returns>
    int GetWidth();

    /// <summary>Returns the declared output height.</summary>
    /// <returns>The luma height in pixels, or zero when parsing failed.</returns>
    int GetHeight();

    /// <summary>Returns the row-major cell count.</summary>
    /// <returns>The product of row and column counts.</returns>
    int GetCellCount();

    /// <summary>Returns one row-major cell descriptor.</summary>
    /// <param name="index">The zero-based cell index.</param>
    /// <returns>The cell, or null for an invalid index.</returns>
    AvifGridCell? GetCell(int index);

    /// <summary>Sets the common decoded cell dimensions from dependency item metadata.</summary>
    /// <param name="cellWidth">The positive luma width of every dependency image.</param>
    /// <param name="cellHeight">The positive luma height of every dependency image.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetCellDimensions(int cellWidth, int cellHeight);

    /// <summary>Returns the common decoded cell width.</summary>
    /// <returns>The luma width, or zero before geometry is known.</returns>
    int GetCellWidth();

    /// <summary>Returns the common decoded cell height.</summary>
    /// <returns>The luma height, or zero before geometry is known.</returns>
    int GetCellHeight();

    /// <summary>Associates a decoded image with one row-major cell.</summary>
    /// <param name="index">The zero-based cell index.</param>
    /// <param name="image">The caller-owned decoded dependency image.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetDecodedCell(int index, AvifImage image);

    /// <summary>Sets one caller-owned image as a row-major authoring cell.</summary>
    /// <param name="index">The zero-based cell index.</param>
    /// <param name="image">The image that a writer will encode independently.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetCell(int index, AvifImage image);

    /// <summary>Caches a decoded dependency and attaches it to every cell using that item.</summary>
    /// <param name="itemId">The nonzero derived-image dependency identifier.</param>
    /// <param name="image">The caller-owned decoded dependency image.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetDecodedItem(uint itemId, AvifImage? image);

    /// <summary>Returns the number of distinct decoded dependency items retained by the grid.</summary>
    /// <returns>The cache entry count.</returns>
    int GetCachedImageCount();

    /// <summary>Returns the number of cells whose decoded samples are available.</summary>
    /// <returns>The decoded cell count, including cells sharing one dependency.</returns>
    int GetDecodedCellCount();

    /// <summary>Reports whether every cell has decoded samples.</summary>
    /// <returns><c>true</c> when a complete render can be produced.</returns>
    bool IsComplete();

    /// <summary>Returns the number of completely decoded leading rows.</summary>
    /// <returns>The contiguous visible row count from the top of the grid.</returns>
    int GetDecodedRowCount();

    /// <summary>Counts cells intersecting a valid luma rectangle.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The positive luma width.</param>
    /// <param name="height">The positive luma height.</param>
    /// <returns>The required cell count, or zero for an invalid rectangle.</returns>
    int GetRequiredCellCount(
        int x,
        int y,
        int width,
        int height);

    /// <summary>Returns one cell intersecting a valid luma rectangle.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The positive luma width.</param>
    /// <param name="height">The positive luma height.</param>
    /// <param name="index">The zero-based index among intersecting cells.</param>
    /// <returns>The required cell, or null for invalid arguments.</returns>
    AvifGridCell? GetRequiredCell(
        int x,
        int y,
        int width,
        int height,
        int index);

    /// <summary>Renders the complete grid after all dependencies are decoded.</summary>
    /// <returns>An owning assembled image, or an image carrying <c>NeedMoreData</c>.</returns>
    AvifImage Render();

    /// <summary>Renders only cells intersecting a requested luma rectangle.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The positive luma width.</param>
    /// <param name="height">The positive luma height.</param>
    /// <returns>An owning region image, or an image carrying a stable error code.</returns>
    AvifImage RenderRegion(
        int x,
        int y,
        int width,
        int height);

    /// <summary>Assembles decoded cells and leaves unavailable cells initialized to zero.</summary>
    /// <returns>An owning partial image, or an image carrying <c>NeedMoreData</c> before any cell.</returns>
    AvifImage RenderAvailable();
}
