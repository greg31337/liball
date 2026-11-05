using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace pdf;

internal static class PdfTrueTypeSubsetter
{
    internal static byte[] CreateFontProgram(PdfTrueTypeFontUsage usage)
    {
        if (!usage.Font.SubsettingAllowed)
        {
            byte[] fullFont = new byte[usage.Font.FontData.Length];
            usage.Font.FontData.CopyTo(fullFont, 0);
            return fullFont;
        }

        byte[] data = usage.Font.FontData;
        Dictionary<string, PdfFontTable> tables = PdfTrueTypeFontParser.ReadTableDirectory(data);
        PdfFontTable head = PdfTrueTypeFontParser.RequireTable(tables, "head", 54);
        PdfFontTable loca = PdfTrueTypeFontParser.RequireTable(tables, "loca", 2);
        PdfFontTable glyf = PdfTrueTypeFontParser.RequireTable(tables, "glyf", 0);
        int locationFormat = PdfTrueTypeFontParser.ReadInt16(data, head.Offset + 50);
        if (locationFormat != 0 && locationFormat != 1)
        {
            throw new InvalidDataException("The TrueType font uses an invalid glyph-location format.");
        }

        int[] offsets = ReadGlyphOffsets(data, loca, glyf, usage.Font.GlyphCount, locationFormat);
        bool[] included = FindCompositeClosure(data, glyf, offsets, usage);
        byte[] subsetGlyf = CreateGlyfTable(data, glyf, offsets, included);
        int[] subsetOffsets = CreateSubsetOffsets(data, glyf, offsets, included);
        byte[] subsetLoca = CreateLocaTable(subsetOffsets, locationFormat);

        Dictionary<string, byte[]> outputTables = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (PdfFontTable table in tables.Values)
        {
            byte[] tableData = new byte[table.Length];
            Array.Copy(data, table.Offset, tableData, 0, table.Length);
            outputTables.Add(table.Tag, tableData);
        }

        outputTables["glyf"] = subsetGlyf;
        outputTables["loca"] = subsetLoca;
        byte[] outputHead = outputTables["head"];
        outputHead[8] = 0;
        outputHead[9] = 0;
        outputHead[10] = 0;
        outputHead[11] = 0;
        return BuildSfnt(outputTables);
    }

    internal static int[] ReadGlyphOffsets(byte[] data, PdfFontTable loca, PdfFontTable glyf, int glyphCount, int locationFormat)
    {
        int entrySize = locationFormat == 0 ? 2 : 4;
        long requiredLength = (glyphCount + 1L) * entrySize;
        if (requiredLength > loca.Length)
        {
            throw new InvalidDataException("The loca table does not contain an offset for every glyph.");
        }

        int[] offsets = new int[glyphCount + 1];
        int previous = 0;
        for (int index = 0; index <= glyphCount; index++)
        {
            long value;
            if (locationFormat == 0)
            {
                value = PdfTrueTypeFontParser.ReadUInt16(data, loca.Offset + index * 2) * 2L;
            }
            else
            {
                value = PdfTrueTypeFontParser.ReadUInt32(data, loca.Offset + index * 4);
            }

            if (value > glyf.Length || value < previous)
            {
                throw new InvalidDataException("The loca table contains an invalid or unordered glyph offset.");
            }

            offsets[index] = (int)value;
            previous = (int)value;
        }

        return offsets;
    }

    internal static bool[] FindCompositeClosure(byte[] data, PdfFontTable glyf, int[] offsets, PdfTrueTypeFontUsage usage)
    {
        bool[] included = new bool[usage.Font.GlyphCount];
        bool[] processed = new bool[usage.Font.GlyphCount];
        included[0] = true;
        foreach (int glyphIndex in usage.GlyphIndices)
        {
            if (glyphIndex < 0 || glyphIndex >= included.Length)
            {
                throw new InvalidDataException("A used glyph index lies outside the font.");
            }

            included[glyphIndex] = true;
        }

        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int glyphIndex = 0; glyphIndex < included.Length; glyphIndex++)
            {
                if (included[glyphIndex] && !processed[glyphIndex])
                {
                    processed[glyphIndex] = true;
                    if (AddCompositeComponents(data, glyf, offsets, glyphIndex, included))
                    {
                        changed = true;
                    }
                }
            }
        }

        return included;
    }

    internal static bool AddCompositeComponents(byte[] data, PdfFontTable glyf, int[] offsets, int glyphIndex, bool[] included)
    {
        int start = offsets[glyphIndex];
        int end = offsets[glyphIndex + 1];
        if (start == end)
        {
            return false;
        }

        if (end - start < 10)
        {
            throw new InvalidDataException("A glyph record is too short.");
        }

        int absoluteStart = glyf.Offset + start;
        int contourCount = PdfTrueTypeFontParser.ReadInt16(data, absoluteStart);
        if (contourCount >= 0)
        {
            return false;
        }

        bool added = false;
        int position = absoluteStart + 10;
        int absoluteEnd = glyf.Offset + end;
        int flags = 0;
        bool moreComponents = true;
        while (moreComponents)
        {
            if (position + 4 > absoluteEnd)
            {
                throw new InvalidDataException("A composite glyph component header is truncated.");
            }

            flags = PdfTrueTypeFontParser.ReadUInt16(data, position);
            int componentGlyph = PdfTrueTypeFontParser.ReadUInt16(data, position + 2);
            if (componentGlyph >= included.Length)
            {
                throw new InvalidDataException("A composite glyph references a glyph outside the font.");
            }

            if (!included[componentGlyph])
            {
                included[componentGlyph] = true;
                added = true;
            }

            position += 4;
            position += (flags & 0x0001) != 0 ? 4 : 2;
            if ((flags & 0x0008) != 0)
            {
                position += 2;
            }
            else if ((flags & 0x0040) != 0)
            {
                position += 4;
            }
            else if ((flags & 0x0080) != 0)
            {
                position += 8;
            }

            if (position > absoluteEnd)
            {
                throw new InvalidDataException("A composite glyph component is truncated.");
            }

            moreComponents = (flags & 0x0020) != 0;
        }

        if ((flags & 0x0100) != 0)
        {
            if (position + 2 > absoluteEnd)
            {
                throw new InvalidDataException("Composite glyph instructions are truncated.");
            }

            int instructionLength = PdfTrueTypeFontParser.ReadUInt16(data, position);
            if (position + 2L + instructionLength > absoluteEnd)
            {
                throw new InvalidDataException("Composite glyph instructions extend beyond the glyph record.");
            }
        }

        return added;
    }

    internal static byte[] CreateGlyfTable(byte[] data, PdfFontTable glyf, int[] offsets, bool[] included)
    {
        List<byte> output = new List<byte>();
        for (int glyphIndex = 0; glyphIndex < included.Length; glyphIndex++)
        {
            if (included[glyphIndex])
            {
                int length = offsets[glyphIndex + 1] - offsets[glyphIndex];
                for (int index = 0; index < length; index++)
                {
                    output.Add(data[glyf.Offset + offsets[glyphIndex] + index]);
                }

                if ((output.Count & 1) != 0)
                {
                    output.Add(0);
                }
            }
        }

        return output.ToArray();
    }

    internal static int[] CreateSubsetOffsets(byte[] data, PdfFontTable glyf, int[] offsets, bool[] included)
    {
        int[] subsetOffsets = new int[included.Length + 1];
        int outputOffset = 0;
        for (int glyphIndex = 0; glyphIndex < included.Length; glyphIndex++)
        {
            subsetOffsets[glyphIndex] = outputOffset;
            if (included[glyphIndex])
            {
                int length = offsets[glyphIndex + 1] - offsets[glyphIndex];
                outputOffset += length;
                if ((outputOffset & 1) != 0)
                {
                    outputOffset++;
                }
            }
        }

        subsetOffsets[included.Length] = outputOffset;
        return subsetOffsets;
    }

    internal static byte[] CreateLocaTable(int[] offsets, int locationFormat)
    {
        int entrySize = locationFormat == 0 ? 2 : 4;
        byte[] output = new byte[offsets.Length * entrySize];
        for (int index = 0; index < offsets.Length; index++)
        {
            if (locationFormat == 0)
            {
                int shortOffset = offsets[index] / 2;
                if (shortOffset > 65535)
                {
                    throw new InvalidDataException("The subset glyph data is too large for short loca offsets.");
                }

                WriteUInt16(output, index * 2, shortOffset);
            }
            else
            {
                WriteUInt32(output, index * 4, (uint)offsets[index]);
            }
        }

        return output;
    }

    internal static byte[] BuildSfnt(Dictionary<string, byte[]> tables)
    {
        List<string> tags = SortTags(tables);
        int tableCount = tags.Count;
        int directoryLength = 12 + tableCount * 16;
        int totalLength = directoryLength;
        foreach (string tag in tags)
        {
            totalLength = Align4(totalLength);
            totalLength += Align4(tables[tag].Length);
        }

        byte[] output = new byte[totalLength];
        WriteUInt32(output, 0, 0x00010000U);
        WriteUInt16(output, 4, tableCount);
        int greatestPower = 1;
        int selector = 0;
        while (greatestPower <= tableCount / 2)
        {
            greatestPower *= 2;
            selector++;
        }

        WriteUInt16(output, 6, greatestPower * 16);
        WriteUInt16(output, 8, selector);
        WriteUInt16(output, 10, tableCount * 16 - greatestPower * 16);
        int tableOffset = directoryLength;
        int headOffset = 0;
        for (int index = 0; index < tags.Count; index++)
        {
            string tag = tags[index];
            byte[] table = tables[tag];
            tableOffset = Align4(tableOffset);
            int record = 12 + index * 16;
            Encoding.ASCII.GetBytes(tag).CopyTo(output, record);
            WriteUInt32(output, record + 4, CalculateChecksum(table));
            WriteUInt32(output, record + 8, (uint)tableOffset);
            WriteUInt32(output, record + 12, (uint)table.Length);
            table.CopyTo(output, tableOffset);
            if (tag == "head")
            {
                headOffset = tableOffset;
            }

            tableOffset += Align4(table.Length);
        }

        uint wholeChecksum = CalculateChecksum(output);
        ulong adjustmentValue = 0xB1B0AFBAUL + 0x100000000UL - wholeChecksum;
        uint adjustment = (uint)(adjustmentValue & 0xFFFFFFFFUL);
        WriteUInt32(output, headOffset + 8, adjustment);
        return output;
    }

    internal static List<string> SortTags(Dictionary<string, byte[]> tables)
    {
        List<string> tags = new List<string>();
        foreach (string tag in tables.Keys)
        {
            int position = 0;
            while (position < tags.Count && string.CompareOrdinal(tags[position], tag) < 0)
            {
                position++;
            }

            tags.Insert(position, tag);
        }

        return tags;
    }

    internal static uint CalculateChecksum(byte[] data)
    {
        ulong sum = 0;
        int offset = 0;
        while (offset < data.Length)
        {
            uint value = 0;
            for (int index = 0; index < 4; index++)
            {
                value <<= 8;
                if (offset + index < data.Length)
                {
                    value |= data[offset + index];
                }
            }

            sum += value;
            offset += 4;
        }

        return (uint)(sum & 0xFFFFFFFFUL);
    }

    internal static string CreateSubsetFontName(PdfTrueTypeFont font)
    {
        StringBuilder prefix = new StringBuilder();
        for (int index = 0; index < 6; index++)
        {
            char value = font.Identity[index];
            int numeric = value >= '0' && value <= '9' ? value - '0' : value - 'A' + 10;
            prefix.Append((char)('A' + numeric));
        }

        return prefix.ToString() + "+" + SanitizePostScriptName(font.PostScriptName);
    }

    internal static string CreateEmbeddedFontName(PdfTrueTypeFont font)
    {
        if (!font.SubsettingAllowed)
        {
            return SanitizePostScriptName(font.PostScriptName);
        }

        return CreateSubsetFontName(font);
    }

    internal static string SanitizePostScriptName(string name)
    {
        StringBuilder result = new StringBuilder();
        foreach (char character in name)
        {
            if (character > 32 && character < 127 && "()<>[]{}/%".IndexOf(character, StringComparison.Ordinal) < 0)
            {
                result.Append(character);
            }
        }

        if (result.Length == 0)
        {
            return "EmbeddedFont";
        }

        return result.ToString();
    }

    internal static int Align4(int value)
    {
        return (value + 3) & -4;
    }

    internal static void WriteUInt16(byte[] data, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), (ushort)value);
    }

    internal static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }
}
