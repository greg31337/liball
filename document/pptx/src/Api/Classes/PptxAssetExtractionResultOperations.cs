using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxAssetExtractionResult values.
/// </summary>
public class PptxAssetExtractionResultOperations : IPptxAssetExtractionResultOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxAssetExtractionResultOperations()
    {
    }

    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="asset">The asset value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Add(PptxAssetExtractionResult target, PptxExtractedAsset asset)
    {
        target.Add(asset);
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Count(PptxAssetExtractionResult target)
    {
        return target.Count();
    }

    /// <summary>
    /// Returns the extracted asset at the requested zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting extracted asset value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxExtractedAsset Get(PptxAssetExtractionResult target, int index)
    {
        return target.Get(index);
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int WrittenCount(PptxAssetExtractionResult target)
    {
        return target.WrittenCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int LinkedCount(PptxAssetExtractionResult target)
    {
        return target.LinkedCount();
    }
}
