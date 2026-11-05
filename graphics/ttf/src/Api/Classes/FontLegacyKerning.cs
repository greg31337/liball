namespace ttf;

/// <summary>Contains legacy kern table inspection data.</summary>
public class FontLegacyKerning : IFontLegacyKerning
{
    SfntKernTable table;
    FontLegacyKerningSubtable[] subtables;

    internal FontLegacyKerning(SfntKernTable table, int glyphCount)
    {
        this.table = table;
        subtables = new FontLegacyKerningSubtable[table.GetSubtableCount()];
        int index = 0;

        while (index < subtables.Length)
        {
            subtables[index] = new FontLegacyKerningSubtable(
                table.GetSubtable(index),
                glyphCount);
            index += 1;
        }
    }

    /// <summary>Gets the raw table version.</summary>
    /// <returns>The raw version value.</returns>
    public uint GetVersion() { return table.GetVersionRaw(); }

    /// <summary>Reports whether the table uses the Apple header form.</summary>
    /// <returns>True for the Apple form.</returns>
    public bool IsAppleFormat() { return table.IsAppleFormat(); }

    /// <summary>Gets the number of kerning subtables.</summary>
    /// <returns>The subtable count.</returns>
    public int GetSubtableCount() { return subtables.Length; }

    /// <summary>Gets one kerning subtable.</summary>
    /// <param name="index">The zero-based subtable index.</param>
    /// <returns>The stable subtable inspection.</returns>
    public FontLegacyKerningSubtable GetSubtable(int index)
    {
        if (index < 0 || index >= subtables.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return subtables[index];
    }
}
