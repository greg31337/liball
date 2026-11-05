using System.IO;

namespace png;

internal class PngSuggestedPaletteParser
{
    internal const int MinimumNameLength = PngTextKeywordValidator.MinimumKeywordLength;
    internal const int MaximumNameLength = PngTextKeywordValidator.MaximumKeywordLength;
    internal const byte SampleDepth8 = 8;
    internal const byte SampleDepth16 = 16;
    internal const int BytesPer8BitEntry = 6;
    internal const int BytesPer16BitEntry = 10;

    internal static PngSuggestedPalette Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngSuggestedPalette Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        int separatorOffset = FindSeparator(data, offset, length);
        int nameLength = separatorOffset - offset;
        ValidateName(data, offset, nameLength);
        int sampleDepthOffset = separatorOffset + 1;
        ValidateSampleDepthOffset(offset, length, sampleDepthOffset);
        byte sampleDepth = data[sampleDepthOffset];
        ValidateSampleDepth(sampleDepth);

        int entriesOffset = sampleDepthOffset + 1;
        int entriesLength = offset + length - entriesOffset;
        ValidateEntriesLength(sampleDepth, entriesLength);

        PngSuggestedPalette palette = new PngSuggestedPalette();
        palette.Name = DecodeLatin1(data, offset, nameLength);
        palette.SampleDepth = sampleDepth;
        palette.Entries = ParseEntries(data, entriesOffset, entriesLength, sampleDepth);
        return palette;
    }

    internal static PngSuggestedPalette Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkSuggestedPalette)
        {
            throw new InvalidDataException("Chunk type must be sPLT.");
        }
    }

    internal static void ValidateRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }
    }

    internal static int FindSeparator(byte[] data, int offset, int length)
    {
        int index = offset;
        int end = offset + length;
        while (index < end)
        {
            if (data[index] == 0)
            {
                return index;
            }

            index += 1;
        }

        throw new InvalidDataException("sPLT data must include a palette name separator.");
    }

    internal static void ValidateName(byte[] data, int offset, int length)
    {
        if (length < MinimumNameLength || length > MaximumNameLength)
        {
            throw new InvalidDataException("sPLT palette name must contain 1 through 79 bytes.");
        }

        if (!PngTextKeywordValidator.IsValidBytes(data, offset, length))
        {
            throw new InvalidDataException("sPLT palette name contains invalid characters or spacing.");
        }
    }

    internal static void ValidateSampleDepthOffset(int offset, int length, int sampleDepthOffset)
    {
        if (sampleDepthOffset >= offset + length)
        {
            throw new InvalidDataException("sPLT data must include a sample depth byte.");
        }
    }

    internal static void ValidateSampleDepth(byte sampleDepth)
    {
        if (sampleDepth != SampleDepth8 && sampleDepth != SampleDepth16)
        {
            throw new InvalidDataException("sPLT sample depth must be 8 or 16.");
        }
    }

    internal static void ValidateEntriesLength(byte sampleDepth, int entriesLength)
    {
        int bytesPerEntry = GetBytesPerEntry(sampleDepth);
        if (entriesLength % bytesPerEntry != 0)
        {
            throw new InvalidDataException("sPLT entry data length is not valid for the sample depth.");
        }
    }

    internal static PngSuggestedPaletteEntry[] ParseEntries(byte[] data, int offset, int length, byte sampleDepth)
    {
        int bytesPerEntry = GetBytesPerEntry(sampleDepth);
        int entryCount = length / bytesPerEntry;
        PngSuggestedPaletteEntry[] entries = new PngSuggestedPaletteEntry[entryCount];
        int entryIndex = 0;
        int dataIndex = offset;
        while (entryIndex < entryCount)
        {
            PngSuggestedPaletteEntry entry = ParseEntry(data, dataIndex, sampleDepth);
            entries[entryIndex] = entry;
            dataIndex += bytesPerEntry;
            entryIndex += 1;
        }

        return entries;
    }

    internal static PngSuggestedPaletteEntry ParseEntry(byte[] data, int offset, byte sampleDepth)
    {
        if (sampleDepth == SampleDepth8)
        {
            return Parse8BitEntry(data, offset);
        }

        return Parse16BitEntry(data, offset);
    }

    internal static PngSuggestedPaletteEntry Parse8BitEntry(byte[] data, int offset)
    {
        PngSuggestedPaletteEntry entry = new PngSuggestedPaletteEntry();
        entry.Red = data[offset];
        entry.Green = data[offset + 1];
        entry.Blue = data[offset + 2];
        entry.Alpha = data[offset + 3];
        entry.Frequency = PngBinaryReader.ReadUInt16(data, offset + 4);
        return entry;
    }

    internal static PngSuggestedPaletteEntry Parse16BitEntry(byte[] data, int offset)
    {
        PngSuggestedPaletteEntry entry = new PngSuggestedPaletteEntry();
        entry.Red = PngBinaryReader.ReadUInt16(data, offset);
        entry.Green = PngBinaryReader.ReadUInt16(data, offset + 2);
        entry.Blue = PngBinaryReader.ReadUInt16(data, offset + 4);
        entry.Alpha = PngBinaryReader.ReadUInt16(data, offset + 6);
        entry.Frequency = PngBinaryReader.ReadUInt16(data, offset + 8);
        return entry;
    }

    internal static int GetBytesPerEntry(byte sampleDepth)
    {
        if (sampleDepth == SampleDepth8)
        {
            return BytesPer8BitEntry;
        }

        return BytesPer16BitEntry;
    }

    internal static string DecodeLatin1(byte[] data, int offset, int length)
    {
        char[] chars = new char[length];
        int index = 0;
        while (index < length)
        {
            chars[index] = (char)data[offset + index];
            index += 1;
        }

        return new string(chars);
    }
}

internal class PngSuggestedPaletteWriter
{
    internal static byte[] WriteData(PngSuggestedPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Name);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        ValidateSampleDepth(palette.SampleDepth);
        byte[] name = EncodeName(palette.Name);
        int bytesPerEntry = GetBytesPerEntry(palette.SampleDepth);
        int length = name.Length + 2 + palette.Entries.Length * bytesPerEntry;
        byte[] data = new byte[length];
        WriteData(data, 0, palette);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngSuggestedPalette palette)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Name);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        ValidateSampleDepth(palette.SampleDepth);
        byte[] name = EncodeName(palette.Name);
        int bytesPerEntry = GetBytesPerEntry(palette.SampleDepth);
        int length = name.Length + 2 + palette.Entries.Length * bytesPerEntry;
        ValidateWriteRange(data, offset, length);

        CopyBytes(name, 0, data, offset, name.Length);
        int dataIndex = offset + name.Length;
        data[dataIndex] = 0;
        data[dataIndex + 1] = palette.SampleDepth;
        dataIndex += 2;
        WriteEntries(data, dataIndex, palette);
    }

    internal static PngChunk WriteChunk(PngSuggestedPalette palette)
    {
        return CreateChunk(WriteData(palette));
    }

    internal static void WriteEntries(byte[] data, int offset, PngSuggestedPalette palette)
    {
        int bytesPerEntry = GetBytesPerEntry(palette.SampleDepth);
        int entryIndex = 0;
        int dataIndex = offset;
        while (entryIndex < palette.Entries.Length)
        {
            PngSuggestedPaletteEntry entry = palette.Entries[entryIndex];
            ArgumentNullException.ThrowIfNull(entry);
            WriteEntry(data, dataIndex, entry, palette.SampleDepth);
            dataIndex += bytesPerEntry;
            entryIndex += 1;
        }
    }

    internal static void WriteEntry(byte[] data, int offset, PngSuggestedPaletteEntry entry, byte sampleDepth)
    {
        if (sampleDepth == PngSuggestedPaletteParser.SampleDepth8)
        {
            Write8BitEntry(data, offset, entry);
        }
        else
        {
            Write16BitEntry(data, offset, entry);
        }
    }

    internal static void Write8BitEntry(byte[] data, int offset, PngSuggestedPaletteEntry entry)
    {
        Validate8BitSample(entry.Red, "Red");
        Validate8BitSample(entry.Green, "Green");
        Validate8BitSample(entry.Blue, "Blue");
        Validate8BitSample(entry.Alpha, "Alpha");
        data[offset] = (byte)entry.Red;
        data[offset + 1] = (byte)entry.Green;
        data[offset + 2] = (byte)entry.Blue;
        data[offset + 3] = (byte)entry.Alpha;
        PngBinaryWriter.WriteUInt16(data, offset + 4, entry.Frequency);
    }

    internal static void Write16BitEntry(byte[] data, int offset, PngSuggestedPaletteEntry entry)
    {
        PngBinaryWriter.WriteUInt16(data, offset, entry.Red);
        PngBinaryWriter.WriteUInt16(data, offset + 2, entry.Green);
        PngBinaryWriter.WriteUInt16(data, offset + 4, entry.Blue);
        PngBinaryWriter.WriteUInt16(data, offset + 6, entry.Alpha);
        PngBinaryWriter.WriteUInt16(data, offset + 8, entry.Frequency);
    }

    internal static byte[] EncodeName(string name)
    {
        PngTextKeywordValidator.Validate(name);
        byte[] data = EncodeLatin1(name, "Name");
        if (data.Length < PngSuggestedPaletteParser.MinimumNameLength || data.Length > PngSuggestedPaletteParser.MaximumNameLength)
        {
            throw new ArgumentOutOfRangeException("Name");
        }

        return data;
    }

    internal static byte[] EncodeLatin1(string value, string name)
    {
        byte[] data = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\0' || character > 255)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            data[index] = (byte)character;
            index += 1;
        }

        return data;
    }

    internal static void ValidateSampleDepth(byte sampleDepth)
    {
        if (sampleDepth != PngSuggestedPaletteParser.SampleDepth8 && sampleDepth != PngSuggestedPaletteParser.SampleDepth16)
        {
            throw new ArgumentOutOfRangeException("SampleDepth");
        }
    }

    internal static void Validate8BitSample(ushort sample, string name)
    {
        if (sample > 255)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateWriteRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length - length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    internal static int GetBytesPerEntry(byte sampleDepth)
    {
        if (sampleDepth == PngSuggestedPaletteParser.SampleDepth8)
        {
            return PngSuggestedPaletteParser.BytesPer8BitEntry;
        }

        return PngSuggestedPaletteParser.BytesPer16BitEntry;
    }

    internal static PngChunk CreateChunk(byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkSuggestedPalette;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
