using System;

namespace xlsx;

/// <summary>
/// Represents the selection state of one slicer cache item.
/// </summary>
public class XlsxSlicerSelectionItem
{
    /// <summary>Stores the shared-item index.</summary>
    public int Index;
    /// <summary>Indicates whether the item is selected.</summary>
    public bool Selected;
    /// <summary>Indicates whether the selection flag is present.</summary>
    public bool HasSelected;
    /// <summary>Stores the item display name.</summary>
    public string Name;

    /// <summary>Initializes an empty slicer selection item.</summary>
    public XlsxSlicerSelectionItem()
    {
        Index = 0;
        Selected = false;
        HasSelected = false;
        Name = "";
    }
}
