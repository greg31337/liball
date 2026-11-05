namespace geo;

/// <summary>Identifies how competing polygon coverage regions are retained during cleaning.</summary>
public enum CoverageMergeStrategy
{
    /// <summary>Retain the component that appears first in source order.</summary>
    KeepFirst = 0,

    /// <summary>Retain the component that appears last in source order.</summary>
    KeepLast = 1,

    /// <summary>Retain the component having the largest area.</summary>
    LargestArea = 2
}
