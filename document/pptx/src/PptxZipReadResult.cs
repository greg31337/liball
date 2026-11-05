using System.Collections.Generic;

namespace pptx;

internal class PptxZipReadResult
{
    internal List<PptxZipEntry> Entries;
    internal PptxResult Diagnostics;

    internal PptxZipReadResult()
    {
        Entries = new List<PptxZipEntry>();
        Diagnostics = new PptxResult();
    }

    internal bool Succeeded()
    {
        return Diagnostics.Succeeded();
    }

    internal bool Failed()
    {
        return Diagnostics.Failed();
    }

    internal int Count()
    {
        return Entries.Count;
    }

    internal void AddEntry(PptxZipEntry entry)
    {
        if (entry == null)
        {
            throw new System.ArgumentNullException("entry");
        }

        Entries.Add(entry);
    }
}
