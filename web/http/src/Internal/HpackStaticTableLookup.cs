using System;

namespace http;

internal static class HpackStaticTableLookup
{
    internal static HpackTableEntry LookupByIndex(int index)
    {
        if (index <= 0)
        {
            return new HpackTableEntry();
        }

        int tableIndex = index - 1;
        if (tableIndex >= HpackStaticTable.Names.Length ||
            tableIndex >= HpackStaticTable.Values.Length)
        {
            return new HpackTableEntry();
        }

        return CreateEntry(tableIndex);
    }

    internal static HpackTableEntry FindExact(
        ReadOnlySpan<byte> name,
        ReadOnlySpan<byte> value)
    {
        int count = HpackStaticTable.Names.Length;
        if (HpackStaticTable.Values.Length < count)
        {
            count = HpackStaticTable.Values.Length;
        }

        int tableIndex = 0;
        while (tableIndex < count)
        {
            if (EqualsAscii(name, HpackStaticTable.Names[tableIndex]) &&
                EqualsAscii(value, HpackStaticTable.Values[tableIndex]))
            {
                return CreateEntry(tableIndex);
            }

            tableIndex = tableIndex + 1;
        }

        return new HpackTableEntry();
    }

    internal static HpackTableEntry FindName(ReadOnlySpan<byte> name)
    {
        int count = HpackStaticTable.Names.Length;
        if (HpackStaticTable.Values.Length < count)
        {
            count = HpackStaticTable.Values.Length;
        }

        int tableIndex = 0;
        while (tableIndex < count)
        {
            if (EqualsAscii(name, HpackStaticTable.Names[tableIndex]))
            {
                return CreateEntry(tableIndex);
            }

            tableIndex = tableIndex + 1;
        }

        return new HpackTableEntry();
    }

    static HpackTableEntry CreateEntry(int tableIndex)
    {
        HpackTableEntry entry = new HpackTableEntry();
        entry.Name = CopyAscii(HpackStaticTable.Names[tableIndex]);
        entry.Value = CopyAscii(HpackStaticTable.Values[tableIndex]);
        entry.Size = entry.Name.Length + entry.Value.Length + 32;
        entry.HasIndex = true;
        entry.Index = tableIndex + 1;
        entry.IsStatic = true;
        return entry;
    }

    static bool EqualsAscii(ReadOnlySpan<byte> octets, string value)
    {
        if (octets.Length != value.Length)
        {
            return false;
        }

        int index = 0;
        while (index < octets.Length)
        {
            if (value[index] > 0x7f || octets[index] != (byte)value[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static byte[] CopyAscii(string value)
    {
        byte[] octets = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                throw new InvalidOperationException(
                    "HPACK static-table strings must contain only ASCII characters.");
            }

            octets[index] = (byte)value[index];
            index = index + 1;
        }

        return octets;
    }
}
