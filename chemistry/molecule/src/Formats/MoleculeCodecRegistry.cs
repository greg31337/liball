using System.Collections.Generic;
using System.IO;

namespace molecule;


internal class MoleculeCodecRegistry
{
    internal List<FormatCodecEntry> Entries;

    internal MoleculeCodecRegistry()
    {
        Entries = new List<FormatCodecEntry>();
    }

    internal void RegisterReader(FormatKind kind, IMoleculeReader reader)
    {
        if (kind == FormatKind.Unknown)
        {
            return;
        }

        FormatCodecEntry entry = GetByKind(kind);
        if (entry.Kind == FormatKind.Unknown)
        {
            entry.Kind = kind;
            Entries.Add(entry);
        }

        entry.Reader = reader;
    }

    internal void RegisterWriter(FormatKind kind, IMoleculeWriter writer)
    {
        if (kind == FormatKind.Unknown)
        {
            return;
        }

        FormatCodecEntry entry = GetByKind(kind);
        if (entry.Kind == FormatKind.Unknown)
        {
            entry.Kind = kind;
            Entries.Add(entry);
        }

        entry.Writer = writer;
    }

    internal IMoleculeReader? GetReader(FormatKind kind)
    {
        return GetByKind(kind).Reader;
    }

    internal IMoleculeWriter? GetWriter(FormatKind kind)
    {
        return GetByKind(kind).Writer;
    }

    internal FormatCodecEntry GetByKind(FormatKind kind)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].Kind == kind)
            {
                return Entries[index];
            }

            index = index + 1;
        }

        return new FormatCodecEntry();
    }

}

