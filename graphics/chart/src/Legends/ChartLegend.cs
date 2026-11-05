namespace chart;
internal class ChartLegendEntryLayout
{
    internal string EntryId;
    internal ChartRect SymbolBounds;
    internal ChartRect LabelBounds;
    internal ChartLegendEntryLayout(string entryId, ChartRect symbolBounds, ChartRect labelBounds)
    {
        ChartPlot.ValidateId(entryId);
        ChartAxis.ValidatePlotArea(symbolBounds);
        ChartAxis.ValidatePlotArea(labelBounds);
        EntryId = entryId;
        SymbolBounds = ChartPlot.CopyRect(symbolBounds);
        LabelBounds = ChartPlot.CopyRect(labelBounds);
    }

    internal ChartLegendEntryLayout Copy()
    {
        return new ChartLegendEntryLayout(EntryId, SymbolBounds, LabelBounds);
    }
}

internal class ChartLegendLayout
{
    internal ChartRect Bounds;
    internal ChartRect TitleBounds;
    internal ChartLegendEntryLayout[] Entries;
    internal int EntryCount;
    internal ChartLegendLayout(ChartRect bounds, ChartRect titleBounds)
    {
        ChartAxis.ValidatePlotArea(bounds);
        Bounds = ChartPlot.CopyRect(bounds);
        TitleBounds = ChartPlot.CopyRect(titleBounds);
        Entries = new ChartLegendEntryLayout[4];
        EntryCount = 0;
    }

    internal ChartLegendEntryLayout AddEntry(string entryId, ChartRect symbolBounds, ChartRect labelBounds)
    {
        EnsureCapacity(EntryCount + 1);
        Entries[EntryCount] = new ChartLegendEntryLayout(entryId, symbolBounds, labelBounds);
        EntryCount = EntryCount + 1;
        return Entries[EntryCount - 1];
    }

    internal ChartLegendEntryLayout EntryAt(int index)
    {
        if (index < 0 || index >= EntryCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Entries[index];
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Entries.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Entries.Length, capacity);
        ChartLegendEntryLayout[] entries = new ChartLegendEntryLayout[newCapacity];
        System.Array.Copy(Entries, entries, EntryCount);
        Entries = entries;
    }
}
