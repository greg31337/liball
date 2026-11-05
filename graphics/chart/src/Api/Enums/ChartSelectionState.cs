namespace chart;

/// <summary>Specifies the selection and highlight state of one data row.</summary>
public enum ChartSelectionState
{
    /// <summary>The row is neither selected nor highlighted.</summary>
    Normal = 0,
    /// <summary>The row is selected.</summary>
    Selected = 1,
    /// <summary>The row is highlighted.</summary>
    Highlighted = 2,
    /// <summary>The row is both selected and highlighted.</summary>
    SelectedHighlighted = 3
}
