using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxAssetExtractionResultOperations"/>.
/// </summary>
public interface IPptxAssetExtractionResultOperations
{
    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="asset">The asset value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void Add(PptxAssetExtractionResult target, PptxExtractedAsset asset);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Count(PptxAssetExtractionResult target);
    /// <summary>
    /// Returns the extracted asset at the requested zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting extracted asset value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxExtractedAsset Get(PptxAssetExtractionResult target, int index);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int WrittenCount(PptxAssetExtractionResult target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int LinkedCount(PptxAssetExtractionResult target);
}
