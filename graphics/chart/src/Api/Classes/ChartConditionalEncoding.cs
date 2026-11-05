namespace chart;

/// <summary>Stores channel bindings selected from a row selection state.</summary>
public class ChartConditionalEncoding : IChartConditionalEncoding
{
    internal ChartEncodingChannel Channel;
    internal ChartEncodingBinding? UnselectedBinding;
    internal ChartEncodingBinding? SelectedBinding;
    internal ChartEncodingBinding? HighlightedBinding;

    /// <summary>Initializes a conditional encoding for one channel.</summary>
    /// <param name="channel">The channel affected by this condition.</param>
    public ChartConditionalEncoding(ChartEncodingChannel channel)
    {
        ChartEncodingBinding.ValidateChannel(channel);
        Channel = channel;
        UnselectedBinding = null;
        SelectedBinding = null;
        HighlightedBinding = null;
    }

    /// <summary>Sets the binding used for unselected rows.</summary>
    /// <param name="binding">A binding for this condition's channel.</param>
    public void SetUnselectedBinding(ChartEncodingBinding binding)
    {
        UnselectedBinding = ValidateBinding(binding);
    }

    /// <summary>Sets the binding used for selected rows.</summary>
    /// <param name="binding">A binding for this condition's channel.</param>
    public void SetSelectedBinding(ChartEncodingBinding binding)
    {
        SelectedBinding = ValidateBinding(binding);
    }

    /// <summary>Sets the binding used for highlighted rows.</summary>
    /// <param name="binding">A binding for this condition's channel.</param>
    public void SetHighlightedBinding(ChartEncodingBinding binding)
    {
        HighlightedBinding = ValidateBinding(binding);
    }

    /// <summary>Returns the binding for a state, or <c>null</c> when none is configured.</summary>
    /// <param name="state">The selection state.</param>
    /// <returns>The matching binding, or <c>null</c>.</returns>
    public ChartEncodingBinding? BindingForState(ChartSelectionState state)
    {
        ValidateState(state);
        if ((state == ChartSelectionState.Highlighted || state == ChartSelectionState.SelectedHighlighted) && HighlightedBinding != null)
        {
            return HighlightedBinding;
        }

        if ((state == ChartSelectionState.Selected || state == ChartSelectionState.SelectedHighlighted) && SelectedBinding != null)
        {
            return SelectedBinding;
        }

        if (state == ChartSelectionState.Normal && UnselectedBinding != null)
        {
            return UnselectedBinding;
        }

        return null;
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>The copied conditional encoding.</returns>
    public ChartConditionalEncoding Copy()
    {
        ChartConditionalEncoding copy = new ChartConditionalEncoding(Channel);
        if (UnselectedBinding != null)
        {
            copy.UnselectedBinding = UnselectedBinding.Copy();
        }

        if (SelectedBinding != null)
        {
            copy.SelectedBinding = SelectedBinding.Copy();
        }

        if (HighlightedBinding != null)
        {
            copy.HighlightedBinding = HighlightedBinding.Copy();
        }

        return copy;
    }

    /// <summary>Returns the affected channel.</summary>
    /// <returns>The affected channel.</returns>
    public ChartEncodingChannel GetChannel()
    {
        return Channel;
    }

    internal ChartEncodingBinding ValidateBinding(ChartEncodingBinding binding)
    {
        if (binding == null)
        {
            throw new System.ArgumentNullException("binding");
        }

        if (binding.Channel != Channel)
        {
            throw new System.ArgumentException("Conditional binding must use the condition channel.");
        }

        return binding.Copy();
    }

    internal static void ValidateState(ChartSelectionState state)
    {
        int value = (int)state;
        if (value < 0 || value > 3)
        {
            throw new System.ArgumentOutOfRangeException("state");
        }
    }
}
