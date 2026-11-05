namespace chart;

/// <summary>Stores deterministic metadata snapshots for linked selections.</summary>
public class ChartSelectionSidecar : IChartSelectionSidecar
{
    internal ChartSelection[] Selections;
    internal int SelectionCount;
    internal ChartBrush[] Brushes;
    internal int BrushCount;

    /// <summary>Initializes an empty linked-selection sidecar.</summary>
    public ChartSelectionSidecar()
    {
        Selections = new ChartSelection[4];
        SelectionCount = 0;
        Brushes = new ChartBrush[4];
        BrushCount = 0;
    }

    /// <summary>Adds a snapshot of linked selection state.</summary>
    /// <param name="selection">The selection to snapshot.</param>
    public void AddSelection(ChartSelection selection)
    {
        if (selection == null)
        {
            throw new System.ArgumentNullException("selection");
        }

        if (SelectionIndex(selection.SelectionId) >= 0)
        {
            throw new System.ArgumentException("A selection with the same id already exists.");
        }

        EnsureCapacity(SelectionCount + 1);
        Selections[SelectionCount] = selection.Copy();
        SelectionCount = SelectionCount + 1;
    }

    /// <summary>Returns the requested selection snapshot.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The selection snapshot.</returns>
    public ChartSelection SelectionAt(int index)
    {
        if (index < 0 || index >= SelectionCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Selections[index];
    }

    /// <summary>Adds a deterministic snapshot of linked brush geometry and rows.</summary>
    /// <param name = "brush">The brush to snapshot.</param>
    public void AddBrush(ChartBrush brush)
    {
        if (brush == null)
        {
            throw new System.ArgumentNullException("brush");
        }

        if (BrushIndex(brush.BrushId) >= 0)
        {
            throw new System.ArgumentException("A brush with the same id already exists.");
        }

        EnsureBrushCapacity(BrushCount + 1);
        Brushes[BrushCount] = brush.Copy();
        BrushCount = BrushCount + 1;
    }

    /// <summary>Returns the requested linked brush snapshot.</summary>
    /// <param name = "index">The zero-based brush index.</param>
    /// <returns>An independent brush snapshot.</returns>
    public ChartBrush BrushAt(int index)
    {
        if (index < 0 || index >= BrushCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Brushes[index].Copy();
    }

    /// <summary>Returns the sorted union of rows brushed in one linked-selection group.</summary>
    /// <param name = "linkGroup">The linked-selection group.</param>
    /// <returns>An independent sorted row-index array.</returns>
    public int[] CopyLinkedRows(string linkGroup)
    {
        ChartPlot.ValidateId(linkGroup);
        int length = 0;
        int index = 0;
        while (index < BrushCount)
        {
            if (Brushes[index].LinkGroup == linkGroup)
            {
                length = length + Brushes[index].Rows.Length;
            }

            index = index + 1;
        }

        int[] rows = new int[length];
        int output = 0;
        index = 0;
        while (index < BrushCount)
        {
            if (Brushes[index].LinkGroup == linkGroup)
            {
                System.Array.Copy(Brushes[index].Rows, 0, rows, output, Brushes[index].Rows.Length);
                output = output + Brushes[index].Rows.Length;
            }

            index = index + 1;
        }

        return ChartSelection.NormalizeRows(rows);
    }

    /// <summary>Creates a selected-row snapshot resolved from all brushes in one link group.</summary>
    /// <param name = "selectionId">The stable target selection identifier.</param>
    /// <param name = "linkGroup">The linked-selection group.</param>
    /// <returns>The resolved target selection.</returns>
    public ChartSelection CreateLinkedSelection(string selectionId, string linkGroup)
    {
        ChartSelection selection = new ChartSelection(selectionId, linkGroup);
        selection.SetSelectedRows(CopyLinkedRows(linkGroup));
        return selection;
    }

    /// <summary>Returns deterministic linked-selection metadata text.</summary>
    /// <returns>The metadata text.</returns>
    public string MetadataText()
    {
        ChartSelection[] ordered = OrderedSelections();
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < ordered.Length)
        {
            ChartSelection selection = ordered[index];
            builder.Append("linked-selection ");
            builder.Append(selection.SelectionId);
            builder.Append(" group ");
            builder.Append(selection.LinkGroup);
            builder.Append(": selected=");
            AppendRows(builder, selection.SelectedRows);
            builder.Append("; highlighted=");
            AppendRows(builder, selection.HighlightedRows);
            if (index < ordered.Length - 1)
            {
                builder.Append("\n");
            }

            index = index + 1;
        }

        ChartBrush[] orderedBrushes = OrderedBrushes();
        index = 0;
        while (index < orderedBrushes.Length)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n");
            }

            ChartBrush brush = orderedBrushes[index];
            builder.Append("linked-brush ");
            builder.Append(brush.BrushId);
            builder.Append(" source ");
            builder.Append(brush.SourceId);
            builder.Append(" group ");
            builder.Append(brush.LinkGroup);
            builder.Append(": bounds=");
            AppendBounds(builder, brush.Bounds);
            builder.Append("; rows=");
            AppendRows(builder, brush.Rows);
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>The copied sidecar.</returns>
    public ChartSelectionSidecar Copy()
    {
        ChartSelectionSidecar copy = new ChartSelectionSidecar();
        int index = 0;
        while (index < SelectionCount)
        {
            copy.AddSelection(Selections[index]);
            index = index + 1;
        }

        index = 0;
        while (index < BrushCount)
        {
            copy.AddBrush(Brushes[index]);
            index = index + 1;
        }

        return copy;
    }

    /// <summary>Returns the number of selection snapshots.</summary>
    /// <returns>The selection count.</returns>
    public int GetSelectionCount()
    {
        return SelectionCount;
    }

    /// <summary>Returns the number of brush snapshots.</summary>
    /// <returns>The brush count.</returns>
    public int GetBrushCount()
    {
        return BrushCount;
    }

    internal int SelectionIndex(string selectionId)
    {
        ChartPlot.ValidateId(selectionId);
        int index = 0;
        while (index < SelectionCount)
        {
            if (Selections[index].SelectionId == selectionId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ChartSelection[] OrderedSelections()
    {
        ChartSelection[] ordered = new ChartSelection[SelectionCount];
        System.Array.Copy(Selections, ordered, SelectionCount);
        int index = 1;
        while (index < ordered.Length)
        {
            ChartSelection value = ordered[index];
            int scan = index - 1;
            while (scan >= 0 && System.String.CompareOrdinal(ordered[scan].SelectionId, value.SelectionId) > 0)
            {
                ordered[scan + 1] = ordered[scan];
                scan = scan - 1;
            }

            ordered[scan + 1] = value;
            index = index + 1;
        }

        return ordered;
    }

    internal int BrushIndex(string brushId)
    {
        ChartPlot.ValidateId(brushId);
        int index = 0;
        while (index < BrushCount)
        {
            if (Brushes[index].BrushId == brushId)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ChartBrush[] OrderedBrushes()
    {
        ChartBrush[] ordered = new ChartBrush[BrushCount];
        System.Array.Copy(Brushes, ordered, BrushCount);
        int index = 1;
        while (index < ordered.Length)
        {
            ChartBrush value = ordered[index];
            int scan = index - 1;
            while (scan >= 0 && System.String.CompareOrdinal(ordered[scan].BrushId, value.BrushId) > 0)
            {
                ordered[scan + 1] = ordered[scan];
                scan = scan - 1;
            }

            ordered[scan + 1] = value;
            index = index + 1;
        }

        return ordered;
    }

    internal static void AppendRows(System.Text.StringBuilder builder, int[] rows)
    {
        int index = 0;
        while (index < rows.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(rows[index].ToString(System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }
    }

    internal static void AppendBounds(System.Text.StringBuilder builder, ChartRect bounds)
    {
        builder.Append(bounds.X.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(",");
        builder.Append(bounds.Y.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(",");
        builder.Append(bounds.Width.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(",");
        builder.Append(bounds.Height.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Selections.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Selections.Length, capacity);
        ChartSelection[] selections = new ChartSelection[newCapacity];
        System.Array.Copy(Selections, selections, SelectionCount);
        Selections = selections;
    }

    internal void EnsureBrushCapacity(int capacity)
    {
        if (capacity <= Brushes.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Brushes.Length, capacity);
        ChartBrush[] brushes = new ChartBrush[newCapacity];
        System.Array.Copy(Brushes, brushes, BrushCount);
        Brushes = brushes;
    }
}
