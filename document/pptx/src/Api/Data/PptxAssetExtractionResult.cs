using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents asset extraction result data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxAssetExtractionResult
{
    /// <summary>
    /// Contains the assets in package or presentation order.
    /// </summary>
    public List<PptxExtractedAsset> Assets;

    /// <summary>
    /// Initializes a new asset extraction result with safe defaults or the supplied values.
    /// </summary>
    public PptxAssetExtractionResult()
    {
        Assets = new List<PptxExtractedAsset>();
    }
    internal void Add(PptxExtractedAsset asset)
    {
        if (asset == null)
        {
            throw new System.ArgumentNullException("asset");
        }

        Assets.Add(asset);
    }
    internal int Count()
    {
        return Assets.Count;
    }
    internal PptxExtractedAsset Get(int index)
    {
        return Assets[index];
    }
    internal int WrittenCount()
    {
        int count = 0;
        for (int index = 0; index < Assets.Count; index++)
        {
            if (Assets[index].Written)
            {
                count = count + 1;
            }
        }

        return count;
    }
    internal int LinkedCount()
    {
        int count = 0;
        for (int index = 0; index < Assets.Count; index++)
        {
            if (Assets[index].Linked)
            {
                count = count + 1;
            }
        }

        return count;
    }
}

