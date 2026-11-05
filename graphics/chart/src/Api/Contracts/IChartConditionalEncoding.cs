namespace chart;

/// <summary>Defines channel bindings selected from a row selection state.</summary>
public interface IChartConditionalEncoding
{
    /// <summary>Sets the binding used for unselected rows.</summary>
    /// <param name="binding">A binding for this condition's channel.</param>
    void SetUnselectedBinding(ChartEncodingBinding binding);
    /// <summary>Sets the binding used for selected rows.</summary>
    /// <param name="binding">A binding for this condition's channel.</param>
    void SetSelectedBinding(ChartEncodingBinding binding);
    /// <summary>Sets the binding used for highlighted rows.</summary>
    /// <param name="binding">A binding for this condition's channel.</param>
    void SetHighlightedBinding(ChartEncodingBinding binding);
    /// <summary>Returns the binding for a state, or <c>null</c> when none is configured.</summary>
    /// <param name="state">The selection state.</param>
    /// <returns>The matching binding, or <c>null</c>.</returns>
    ChartEncodingBinding? BindingForState(ChartSelectionState state);
    /// <summary>Creates an independent copy.</summary>
    /// <returns>The copied conditional encoding.</returns>
    ChartConditionalEncoding Copy();
    /// <summary>Returns the affected channel.</summary>
    /// <returns>The affected channel.</returns>
    ChartEncodingChannel GetChannel();
}
