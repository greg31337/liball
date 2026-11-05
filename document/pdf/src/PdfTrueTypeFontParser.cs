using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace pdf;

internal static class PdfTrueTypeFontParser
{
    internal static PdfTrueTypeFont Parse(byte[] data)
    {
        if (data.Length < 12)
        {
            throw new InvalidDataException("Font data is too short to contain an sfnt header.");
        }

        uint signature = ReadUInt32(data, 0);
        bool isOpenType = signature == 0x4F54544FU;
        if (signature != 0x00010000U && signature != 0x74727565U && signature != 0x74797031U && !isOpenType)
        {
            throw new InvalidDataException("Font data does not have a supported TrueType or OpenType signature.");
        }

        Dictionary<string, PdfFontTable> tables = ReadTableDirectory(data);
        PdfFontTable head = RequireTable(tables, "head", 54);
        PdfFontTable hhea = RequireTable(tables, "hhea", 36);
        PdfFontTable maxp = RequireTable(tables, "maxp", 6);
        PdfFontTable hmtx = RequireTable(tables, "hmtx", 4);
        PdfFontTable name = RequireTable(tables, "name", 6);
        PdfFontTable cmap = RequireTable(tables, "cmap", 4);

        if (ReadUInt32(data, head.Offset) != 0x00010000U || ReadUInt32(data, head.Offset + 12) != 0x5F0F3CF5U)
        {
            throw new InvalidDataException("The head table version or magic number is invalid.");
        }

        if (ReadUInt32(data, hhea.Offset) != 0x00010000U)
        {
            throw new InvalidDataException("The hhea table version is not supported.");
        }

        uint maxpVersion = ReadUInt32(data, maxp.Offset);
        if (maxpVersion != 0x00005000U && maxpVersion != 0x00010000U)
        {
            throw new InvalidDataException("The maxp table version is not supported.");
        }

        int unitsPerEm = ReadUInt16(data, head.Offset + 18);
        if (unitsPerEm < 16 || unitsPerEm > 16384)
        {
            throw new InvalidDataException("The head table contains an invalid units-per-em value.");
        }

        int xMin = ReadInt16(data, head.Offset + 36);
        int yMin = ReadInt16(data, head.Offset + 38);
        int xMax = ReadInt16(data, head.Offset + 40);
        int yMax = ReadInt16(data, head.Offset + 42);
        if (xMin > xMax || yMin > yMax)
        {
            throw new InvalidDataException("The head table contains an invalid font bounding box.");
        }

        int glyphCount = ReadUInt16(data, maxp.Offset + 4);
        if (glyphCount == 0)
        {
            throw new InvalidDataException("The maxp table must declare at least one glyph.");
        }

        int ascent = ReadInt16(data, hhea.Offset + 4);
        int descent = ReadInt16(data, hhea.Offset + 6);
        int lineGap = ReadInt16(data, hhea.Offset + 8);
        int metricCount = ReadUInt16(data, hhea.Offset + 34);
        if (metricCount == 0 || metricCount > glyphCount)
        {
            throw new InvalidDataException("The hhea table contains an invalid horizontal metric count.");
        }

        int[] advanceWidths = ReadAdvanceWidths(data, hmtx, glyphCount, metricCount);
        PdfFontNameData names = ReadNames(data, name);
        CompleteNames(names);
        PdfCmapData unicodeCmap = ReadUnicodeCmap(data, cmap, glyphCount);

        int capHeight = 0;
        int xHeight = 0;
        int fsType = 0;
        if (tables.ContainsKey("OS/2"))
        {
            PdfFontTable os2 = tables["OS/2"];
            if (os2.Length < 10)
            {
                throw new InvalidDataException("The OS/2 table is too short.");
            }

            fsType = ReadUInt16(data, os2.Offset + 8);
            int os2Version = ReadUInt16(data, os2.Offset);
            if (os2Version >= 2 && os2.Length >= 90)
            {
                xHeight = ReadInt16(data, os2.Offset + 86);
                capHeight = ReadInt16(data, os2.Offset + 88);
            }
        }

        PdfFontEmbeddingPermission embeddingPermission = GetEmbeddingPermission(fsType);
        bool embeddingAllowed = embeddingPermission != PdfFontEmbeddingPermission.Restricted && embeddingPermission != PdfFontEmbeddingPermission.BitmapOnly;
        bool subsettingAllowed = embeddingAllowed && (fsType & 0x0100) == 0;
        string identity = Convert.ToHexString(SHA256.HashData(data));
        string resourceName = "TT" + identity;

        return new PdfTrueTypeFont(identity, resourceName, names.FamilyName, names.SubfamilyName, names.FullName, names.PostScriptName, isOpenType, unitsPerEm, xMin, yMin, xMax, yMax, ascent, descent, lineGap, capHeight, xHeight, glyphCount, advanceWidths, unicodeCmap.CodePoints, unicodeCmap.GlyphIndices, unicodeCmap.PlatformId, unicodeCmap.EncodingId, unicodeCmap.Format, embeddingPermission, embeddingAllowed, subsettingAllowed, data);
    }

    internal static Dictionary<string, PdfFontTable> ReadTableDirectory(byte[] data)
    {
        int tableCount = ReadUInt16(data, 4);
        if (tableCount == 0)
        {
            throw new InvalidDataException("The sfnt table directory is empty.");
        }

        long directoryEnd = 12L + tableCount * 16L;
        if (directoryEnd > data.Length)
        {
            throw new InvalidDataException("The sfnt table directory extends beyond the font data.");
        }

        ValidateDirectorySearchFields(data, tableCount);
        Dictionary<string, PdfFontTable> tables = new Dictionary<string, PdfFontTable>(StringComparer.Ordinal);
        for (int index = 0; index < tableCount; index++)
        {
            int recordOffset = 12 + index * 16;
            string tag = ReadTag(data, recordOffset);
            uint unsignedOffset = ReadUInt32(data, recordOffset + 8);
            uint unsignedLength = ReadUInt32(data, recordOffset + 12);
            if (unsignedOffset > int.MaxValue || unsignedLength > int.MaxValue)
            {
                throw new InvalidDataException("An sfnt table offset or length is too large.");
            }

            int offset = (int)unsignedOffset;
            int length = (int)unsignedLength;
            if ((offset & 3) != 0)
            {
                throw new InvalidDataException("An sfnt table offset is not four-byte aligned.");
            }

            if (offset < directoryEnd || (long)offset + length > data.Length)
            {
                throw new InvalidDataException("An sfnt table lies outside the font data.");
            }

            if (tables.ContainsKey(tag))
            {
                throw new InvalidDataException("The sfnt table directory contains a duplicate table tag.");
            }

            PdfFontTable table = new PdfFontTable(tag, offset, length);
            ValidateTableDoesNotOverlap(tables, table);
            tables.Add(tag, table);
        }

        return tables;
    }

    internal static void ValidateDirectorySearchFields(byte[] data, int tableCount)
    {
        int greatestPower = 1;
        int selector = 0;
        while (greatestPower <= tableCount / 2)
        {
            greatestPower *= 2;
            selector++;
        }

        int expectedRange = greatestPower * 16;
        int expectedShift = tableCount * 16 - expectedRange;
        if (ReadUInt16(data, 6) != expectedRange || ReadUInt16(data, 8) != selector || ReadUInt16(data, 10) != expectedShift)
        {
            throw new InvalidDataException("The sfnt table directory search fields are inconsistent.");
        }
    }

    internal static void ValidateTableDoesNotOverlap(Dictionary<string, PdfFontTable> tables, PdfFontTable candidate)
    {
        if (candidate.Length == 0)
        {
            return;
        }

        foreach (PdfFontTable table in tables.Values)
        {
            if (table.Length > 0 && candidate.Offset < table.Offset + table.Length && table.Offset < candidate.Offset + candidate.Length)
            {
                throw new InvalidDataException("The sfnt table directory contains overlapping tables.");
            }
        }
    }

    internal static PdfFontTable RequireTable(Dictionary<string, PdfFontTable> tables, string tag, int minimumLength)
    {
        if (!tables.ContainsKey(tag))
        {
            throw new InvalidDataException("The required " + tag + " table is missing.");
        }

        PdfFontTable table = tables[tag];
        if (table.Length < minimumLength)
        {
            throw new InvalidDataException("The " + tag + " table is too short.");
        }

        return table;
    }

    internal static int[] ReadAdvanceWidths(byte[] data, PdfFontTable hmtx, int glyphCount, int metricCount)
    {
        long requiredLength = metricCount * 4L + (glyphCount - metricCount) * 2L;
        if (requiredLength > hmtx.Length)
        {
            throw new InvalidDataException("The hmtx table does not contain all declared horizontal metrics.");
        }

        int[] widths = new int[glyphCount];
        int lastWidth = 0;
        for (int index = 0; index < metricCount; index++)
        {
            lastWidth = ReadUInt16(data, hmtx.Offset + index * 4);
            widths[index] = lastWidth;
        }

        for (int index = metricCount; index < glyphCount; index++)
        {
            widths[index] = lastWidth;
        }

        return widths;
    }

    internal static PdfFontNameData ReadNames(byte[] data, PdfFontTable nameTable)
    {
        int format = ReadUInt16(data, nameTable.Offset);
        if (format != 0 && format != 1)
        {
            throw new InvalidDataException("The name table format is not supported.");
        }

        int count = ReadUInt16(data, nameTable.Offset + 2);
        int stringOffset = ReadUInt16(data, nameTable.Offset + 4);
        long recordsEnd = 6L + count * 12L;
        if (recordsEnd > nameTable.Length || stringOffset < recordsEnd || stringOffset > nameTable.Length)
        {
            throw new InvalidDataException("The name table header is malformed.");
        }

        if (format == 1)
        {
            if (recordsEnd + 2 > nameTable.Length)
            {
                throw new InvalidDataException("The format 1 name table language-tag header is truncated.");
            }

            int languageTagCount = ReadUInt16(data, nameTable.Offset + (int)recordsEnd);
            if (recordsEnd + 2L + languageTagCount * 4L > stringOffset)
            {
                throw new InvalidDataException("The format 1 name table language-tag records are truncated.");
            }
        }

        PdfFontNameData names = new PdfFontNameData();
        for (int index = 0; index < count; index++)
        {
            int record = nameTable.Offset + 6 + index * 12;
            int platformId = ReadUInt16(data, record);
            int encodingId = ReadUInt16(data, record + 2);
            int languageId = ReadUInt16(data, record + 4);
            int nameId = ReadUInt16(data, record + 6);
            int length = ReadUInt16(data, record + 8);
            int offset = ReadUInt16(data, record + 10);
            long valueEnd = (long)stringOffset + offset + length;
            if (valueEnd > nameTable.Length)
            {
                throw new InvalidDataException("A name record lies outside the name table.");
            }

            if (nameId == 1 || nameId == 2 || nameId == 4 || nameId == 6 || nameId == 16 || nameId == 17)
            {
                string value = DecodeName(data, nameTable.Offset + stringOffset + offset, length, platformId, encodingId);
                int score = GetNameScore(platformId, encodingId, languageId);
                StoreName(names, nameId, value, score);
            }
        }

        return names;
    }

    internal static string DecodeName(byte[] data, int offset, int length, int platformId, int encodingId)
    {
        if (platformId == 0 || platformId == 3)
        {
            if ((length & 1) != 0)
            {
                throw new InvalidDataException("A Unicode name record has an odd byte length.");
            }

            return Encoding.BigEndianUnicode.GetString(data, offset, length).Trim('\0');
        }

        if (platformId == 1 && encodingId == 0)
        {
            return Encoding.Latin1.GetString(data, offset, length).Trim('\0');
        }

        return string.Empty;
    }

    internal static int GetNameScore(int platformId, int encodingId, int languageId)
    {
        int score = 0;
        if (platformId == 3 && (encodingId == 1 || encodingId == 10))
        {
            score = 300;
        }
        else if (platformId == 0)
        {
            score = 200;
        }
        else if (platformId == 1 && encodingId == 0)
        {
            score = 100;
        }

        if (languageId == 0x0409)
        {
            score += 20;
        }
        else if (languageId == 0)
        {
            score += 10;
        }

        return score;
    }

    internal static void StoreName(PdfFontNameData names, int nameId, string value, int score)
    {
        if (value.Length == 0 || score == 0)
        {
            return;
        }

        if (nameId == 1 && score > names.FamilyScore)
        {
            names.FamilyName = value;
            names.FamilyScore = score;
        }
        else if (nameId == 2 && score > names.SubfamilyScore)
        {
            names.SubfamilyName = value;
            names.SubfamilyScore = score;
        }
        else if (nameId == 4 && score > names.FullScore)
        {
            names.FullName = value;
            names.FullScore = score;
        }
        else if (nameId == 6 && score > names.PostScriptScore)
        {
            names.PostScriptName = value;
            names.PostScriptScore = score;
        }
        else if (nameId == 16 && score + 1000 > names.FamilyScore)
        {
            names.FamilyName = value;
            names.FamilyScore = score + 1000;
        }
        else if (nameId == 17 && score + 1000 > names.SubfamilyScore)
        {
            names.SubfamilyName = value;
            names.SubfamilyScore = score + 1000;
        }
    }

    internal static void CompleteNames(PdfFontNameData names)
    {
        if (names.FamilyName.Length == 0)
        {
            names.FamilyName = names.FullName;
        }

        if (names.FamilyName.Length == 0)
        {
            names.FamilyName = names.PostScriptName;
        }

        if (names.FamilyName.Length == 0)
        {
            throw new InvalidDataException("The name table does not contain a usable family, full, or PostScript name.");
        }

        if (names.SubfamilyName.Length == 0)
        {
            names.SubfamilyName = "Regular";
        }

        if (names.FullName.Length == 0)
        {
            names.FullName = names.FamilyName + " " + names.SubfamilyName;
        }

        if (names.PostScriptName.Length == 0)
        {
            names.PostScriptName = CreatePostScriptName(names.FullName);
        }
    }

    internal static string CreatePostScriptName(string fullName)
    {
        StringBuilder result = new StringBuilder();
        foreach (char character in fullName)
        {
            if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-')
            {
                result.Append(character);
            }
        }

        if (result.Length == 0)
        {
            throw new InvalidDataException("The font name cannot be converted to a PostScript name.");
        }

        return result.ToString();
    }

    internal static PdfCmapData ReadUnicodeCmap(byte[] data, PdfFontTable cmapTable, int glyphCount)
    {
        if (ReadUInt16(data, cmapTable.Offset) != 0)
        {
            throw new InvalidDataException("The cmap table version is not supported.");
        }

        int count = ReadUInt16(data, cmapTable.Offset + 2);
        if (count == 0 || 4L + count * 8L > cmapTable.Length)
        {
            throw new InvalidDataException("The cmap encoding record array is malformed.");
        }

        PdfCmapCandidate? best = null;
        for (int index = 0; index < count; index++)
        {
            int record = cmapTable.Offset + 4 + index * 8;
            int platformId = ReadUInt16(data, record);
            int encodingId = ReadUInt16(data, record + 2);
            uint relativeOffset = ReadUInt32(data, record + 4);
            if (relativeOffset > int.MaxValue || relativeOffset + 2L > cmapTable.Length)
            {
                throw new InvalidDataException("A cmap subtable offset lies outside the cmap table.");
            }

            int offset = cmapTable.Offset + (int)relativeOffset;
            int format = ReadUInt16(data, offset);
            int length = ReadCmapLength(data, cmapTable, offset, format);
            int score = GetCmapScore(platformId, encodingId, format);
            if (score > 0 && (best == null || score > best.Score))
            {
                best = new PdfCmapCandidate(platformId, encodingId, format, offset, length, score);
            }
        }

        if (best == null)
        {
            throw new InvalidDataException("The font does not contain a supported Unicode cmap.");
        }

        if (best.Format == 4)
        {
            return ReadFormat4Cmap(data, best, glyphCount);
        }

        return ReadFormat12Cmap(data, best, glyphCount);
    }

    internal static int ReadCmapLength(byte[] data, PdfFontTable cmapTable, int offset, int format)
    {
        long relativeOffset = offset - cmapTable.Offset;
        long length;
        if (format == 4)
        {
            if (relativeOffset + 4 > cmapTable.Length)
            {
                throw new InvalidDataException("A format 4 cmap header is truncated.");
            }

            length = ReadUInt16(data, offset + 2);
        }
        else if (format == 12)
        {
            if (relativeOffset + 8 > cmapTable.Length)
            {
                throw new InvalidDataException("A format 12 cmap header is truncated.");
            }

            length = ReadUInt32(data, offset + 4);
        }
        else
        {
            return 0;
        }

        if (length > int.MaxValue || length < 4 || relativeOffset + length > cmapTable.Length)
        {
            throw new InvalidDataException("A cmap subtable length is invalid.");
        }

        return (int)length;
    }

    internal static int GetCmapScore(int platformId, int encodingId, int format)
    {
        if (format != 4 && format != 12)
        {
            return 0;
        }

        int score = format == 12 ? 20 : 10;
        if (platformId == 3 && encodingId == 10)
        {
            return 400 + score;
        }

        if (platformId == 0)
        {
            return 300 + score;
        }

        if (platformId == 3 && encodingId == 1)
        {
            return 200 + score;
        }

        return 0;
    }

    internal static PdfCmapData ReadFormat4Cmap(byte[] data, PdfCmapCandidate candidate, int glyphCount)
    {
        if (candidate.Length < 16)
        {
            throw new InvalidDataException("The selected format 4 cmap is too short.");
        }

        int segmentBytes = ReadUInt16(data, candidate.Offset + 6);
        if (segmentBytes == 0 || (segmentBytes & 1) != 0)
        {
            throw new InvalidDataException("The selected format 4 cmap has an invalid segment count.");
        }

        int segmentCount = segmentBytes / 2;
        long arraysEnd = 16L + segmentCount * 8L;
        if (arraysEnd > candidate.Length)
        {
            throw new InvalidDataException("The selected format 4 cmap segment arrays are truncated.");
        }

        int greatestPower = 1;
        int selector = 0;
        while (greatestPower <= segmentCount / 2)
        {
            greatestPower *= 2;
            selector++;
        }

        if (ReadUInt16(data, candidate.Offset + 8) != greatestPower * 2 || ReadUInt16(data, candidate.Offset + 10) != selector || ReadUInt16(data, candidate.Offset + 12) != segmentCount * 2 - greatestPower * 2)
        {
            throw new InvalidDataException("The selected format 4 cmap search fields are inconsistent.");
        }

        int endCodes = candidate.Offset + 14;
        int startCodes = endCodes + segmentCount * 2 + 2;
        int deltas = startCodes + segmentCount * 2;
        int rangeOffsets = deltas + segmentCount * 2;
        if (ReadUInt16(data, endCodes + segmentCount * 2) != 0)
        {
            throw new InvalidDataException("The selected format 4 cmap reserved pad is not zero.");
        }

        List<int> codePoints = new List<int>();
        List<int> glyphIndices = new List<int>();
        int previousEnd = -1;
        for (int segment = 0; segment < segmentCount; segment++)
        {
            int start = ReadUInt16(data, startCodes + segment * 2);
            int end = ReadUInt16(data, endCodes + segment * 2);
            if (start > end || start <= previousEnd)
            {
                throw new InvalidDataException("The selected format 4 cmap segments are not ordered and disjoint.");
            }

            previousEnd = end;
            int delta = ReadInt16(data, deltas + segment * 2);
            int rangeOffset = ReadUInt16(data, rangeOffsets + segment * 2);
            for (int codePoint = start; codePoint <= end && codePoint != 0xFFFF; codePoint++)
            {
                int glyphIndex;
                if (rangeOffset == 0)
                {
                    glyphIndex = (codePoint + delta) & 0xFFFF;
                }
                else
                {
                    int rangeAddress = rangeOffsets + segment * 2;
                    long glyphAddress = (long)rangeAddress + rangeOffset + (codePoint - start) * 2L;
                    if (glyphAddress + 2 > candidate.Offset + candidate.Length)
                    {
                        throw new InvalidDataException("A format 4 cmap glyph index lies outside the subtable.");
                    }

                    glyphIndex = ReadUInt16(data, (int)glyphAddress);
                    if (glyphIndex != 0)
                    {
                        glyphIndex = (glyphIndex + delta) & 0xFFFF;
                    }
                }

                AddUnicodeMapping(codePoints, glyphIndices, codePoint, glyphIndex, glyphCount);
            }
        }

        if (previousEnd != 0xFFFF || ReadUInt16(data, startCodes + (segmentCount - 1) * 2) != 0xFFFF)
        {
            throw new InvalidDataException("The selected format 4 cmap does not have a terminal segment.");
        }

        return new PdfCmapData(candidate.PlatformId, candidate.EncodingId, candidate.Format, codePoints.ToArray(), glyphIndices.ToArray());
    }

    internal static PdfCmapData ReadFormat12Cmap(byte[] data, PdfCmapCandidate candidate, int glyphCount)
    {
        if (candidate.Length < 16 || ReadUInt16(data, candidate.Offset + 2) != 0)
        {
            throw new InvalidDataException("The selected format 12 cmap header is malformed.");
        }

        uint unsignedGroupCount = ReadUInt32(data, candidate.Offset + 12);
        if (unsignedGroupCount > int.MaxValue || 16L + unsignedGroupCount * 12L != candidate.Length)
        {
            throw new InvalidDataException("The selected format 12 cmap group count does not match its length.");
        }

        int groupCount = (int)unsignedGroupCount;
        List<int> codePoints = new List<int>();
        List<int> glyphIndices = new List<int>();
        long previousEnd = -1;
        for (int group = 0; group < groupCount; group++)
        {
            int groupOffset = candidate.Offset + 16 + group * 12;
            uint start = ReadUInt32(data, groupOffset);
            uint end = ReadUInt32(data, groupOffset + 4);
            uint startGlyph = ReadUInt32(data, groupOffset + 8);
            if (start > end || end > 0x10FFFFU || start <= previousEnd)
            {
                throw new InvalidDataException("The selected format 12 cmap groups are not valid ordered Unicode ranges.");
            }

            ulong lastGlyph = (ulong)startGlyph + end - start;
            if (lastGlyph >= (ulong)glyphCount)
            {
                throw new InvalidDataException("A format 12 cmap mapping references a glyph outside the font.");
            }

            previousEnd = end;
            for (uint codePoint = start; codePoint <= end; codePoint++)
            {
                int glyphIndex = (int)(startGlyph + codePoint - start);
                AddUnicodeMapping(codePoints, glyphIndices, (int)codePoint, glyphIndex, glyphCount);
            }
        }

        return new PdfCmapData(candidate.PlatformId, candidate.EncodingId, candidate.Format, codePoints.ToArray(), glyphIndices.ToArray());
    }

    internal static void AddUnicodeMapping(List<int> codePoints, List<int> glyphIndices, int codePoint, int glyphIndex, int glyphCount)
    {
        if (glyphIndex < 0 || glyphIndex >= glyphCount)
        {
            throw new InvalidDataException("A Unicode cmap mapping references a glyph outside the font.");
        }

        if (glyphIndex != 0 && (codePoint < 0xD800 || codePoint > 0xDFFF))
        {
            codePoints.Add(codePoint);
            glyphIndices.Add(glyphIndex);
        }
    }

    internal static PdfFontEmbeddingPermission GetEmbeddingPermission(int fsType)
    {
        if ((fsType & 0x0200) != 0)
        {
            return PdfFontEmbeddingPermission.BitmapOnly;
        }

        if ((fsType & 0x0002) != 0)
        {
            return PdfFontEmbeddingPermission.Restricted;
        }

        if ((fsType & 0x0004) != 0)
        {
            return PdfFontEmbeddingPermission.PreviewAndPrint;
        }

        if ((fsType & 0x0008) != 0)
        {
            return PdfFontEmbeddingPermission.Editable;
        }

        return PdfFontEmbeddingPermission.Installable;
    }

    internal static string ReadTag(byte[] data, int offset)
    {
        for (int index = 0; index < 4; index++)
        {
            if (data[offset + index] < 0x20 || data[offset + index] > 0x7E)
            {
                throw new InvalidDataException("An sfnt table tag contains a non-printable byte.");
            }
        }

        return Encoding.ASCII.GetString(data, offset, 4);
    }

    internal static int ReadUInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
    }

    internal static int ReadInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset, 2));
    }

    internal static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }
}
