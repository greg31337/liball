using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Collects ordered render source-map entries and supports output-offset lookup.
/// </summary>
public class TemplateSourceMapCollector : ITemplateSourceMapCollector
{
    internal List<TemplateSourceMapEntry> Entries;
    internal object SyncRoot;

    /// <summary>
    /// Initializes an empty source-map collector.
    /// </summary>
    public TemplateSourceMapCollector()
    {
        Entries = new List<TemplateSourceMapEntry>();
        SyncRoot = new object();
    }

    /// <summary>
    /// Reports one rendered-output mapping.
    /// </summary>
    /// <param name="entry">The mapping to collect.</param>
    public void Report(TemplateSourceMapEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException("entry");
        }

        lock (SyncRoot)
        {
            Entries.Add(entry);
        }
    }

    /// <summary>
    /// Returns a defensive copy of mappings in rendered-output order.
    /// </summary>
    /// <returns>The collected mappings.</returns>
    public List<TemplateSourceMapEntry> GetMappings()
    {
        lock (SyncRoot)
        {
            return new List<TemplateSourceMapEntry>(Entries);
        }
    }

    /// <summary>
    /// Returns the mapping containing an output offset, or <c>null</c> when no mapping contains it.
    /// </summary>
    /// <param name="outputOffset">The zero-based rendered-output offset.</param>
    /// <returns>The matching mapping, or <c>null</c>.</returns>
    public TemplateSourceMapEntry? GetMappingAt(int outputOffset)
    {
        if (outputOffset < 0)
        {
            throw new ArgumentOutOfRangeException("outputOffset");
        }

        lock (SyncRoot)
        {
            int index = 0;
            while (index < Entries.Count)
            {
                TemplateSourceMapEntry entry = Entries[index];
                if (outputOffset >= entry.OutputStart && outputOffset < entry.OutputEnd)
                {
                    return entry;
                }

                index = index + 1;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes all collected mappings.
    /// </summary>
    public void Clear()
    {
        lock (SyncRoot)
        {
            Entries.Clear();
        }
    }
}
