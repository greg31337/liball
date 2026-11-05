using System;

namespace raw;

internal class NikonMakerNoteDefinition
{
    internal ushort Id;
    internal string Name;
    internal RawTagType Type;
    internal ulong Count;
    internal RawNikonMakerNoteVersion MinimumVersion;
    internal bool Protected;

    internal NikonMakerNoteDefinition(ushort id, string name, RawTagType type, ulong count, RawNikonMakerNoteVersion minimumVersion, bool protectedValue)
    {
        Id = id;
        Name = name;
        Type = type;
        Count = count;
        MinimumVersion = minimumVersion;
        Protected = protectedValue;
    }
}

internal class NikonColorBalanceLayout
{
    internal bool Known;
    internal bool Encrypted;
    internal int DecryptStart;
    internal int LevelsPosition;
    internal string ChannelOrder;

    internal NikonColorBalanceLayout(bool known, bool encrypted, int decryptStart, int levelsPosition, string channelOrder)
    {
        Known = known;
        Encrypted = encrypted;
        DecryptStart = decryptStart;
        LevelsPosition = levelsPosition;
        ChannelOrder = channelOrder;
    }
}

internal static class NikonMakerNoteKeys
{
    internal static readonly byte[] Serial = new byte[]
    {
        0xc1,0xbf,0x6d,0x0d,0x59,0xc5,0x13,0x9d,0x83,0x61,0x6b,0x4f,0xc7,0x7f,0x3d,0x3d,
        0x53,0x59,0xe3,0xc7,0xe9,0x2f,0x95,0xa7,0x95,0x1f,0xdf,0x7f,0x2b,0x29,0xc7,0x0d,
        0xdf,0x07,0xef,0x71,0x89,0x3d,0x13,0x3d,0x3b,0x13,0xfb,0x0d,0x89,0xc1,0x65,0x1f,
        0xb3,0x0d,0x6b,0x29,0xe3,0xfb,0xef,0xa3,0x6b,0x47,0x7f,0x95,0x35,0xa7,0x47,0x4f,
        0xc7,0xf1,0x59,0x95,0x35,0x11,0x29,0x61,0xf1,0x3d,0xb3,0x2b,0x0d,0x43,0x89,0xc1,
        0x9d,0x9d,0x89,0x65,0xf1,0xe9,0xdf,0xbf,0x3d,0x7f,0x53,0x97,0xe5,0xe9,0x95,0x17,
        0x1d,0x3d,0x8b,0xfb,0xc7,0xe3,0x67,0xa7,0x07,0xf1,0x71,0xa7,0x53,0xb5,0x29,0x89,
        0xe5,0x2b,0xa7,0x17,0x29,0xe9,0x4f,0xc5,0x65,0x6d,0x6b,0xef,0x0d,0x89,0x49,0x2f,
        0xb3,0x43,0x53,0x65,0x1d,0x49,0xa3,0x13,0x89,0x59,0xef,0x6b,0xef,0x65,0x1d,0x0b,
        0x59,0x13,0xe3,0x4f,0x9d,0xb3,0x29,0x43,0x2b,0x07,0x1d,0x95,0x59,0x59,0x47,0xfb,
        0xe5,0xe9,0x61,0x47,0x2f,0x35,0x7f,0x17,0x7f,0xef,0x7f,0x95,0x95,0x71,0xd3,0xa3,
        0x0b,0x71,0xa3,0xad,0x0b,0x3b,0xb5,0xfb,0xa3,0xbf,0x4f,0x83,0x1d,0xad,0xe9,0x2f,
        0x71,0x65,0xa3,0xe5,0x07,0x35,0x3d,0x0d,0xb5,0xe9,0xe5,0x47,0x3b,0x9d,0xef,0x35,
        0xa3,0xbf,0xb3,0xdf,0x53,0xd3,0x97,0x53,0x49,0x71,0x07,0x35,0x61,0x71,0x2f,0x43,
        0x2f,0x11,0xdf,0x17,0x97,0xfb,0x95,0x3b,0x7f,0x6b,0xd3,0x25,0xbf,0xad,0xc7,0xc5,
        0xc5,0xb5,0x8b,0xef,0x2f,0xd3,0x07,0x6b,0x25,0x49,0x95,0x25,0x49,0x6d,0x71,0xc7
    };

    internal static readonly byte[] Shutter = new byte[]
    {
        0xa7,0xbc,0xc9,0xad,0x91,0xdf,0x85,0xe5,0xd4,0x78,0xd5,0x17,0x46,0x7c,0x29,0x4c,
        0x4d,0x03,0xe9,0x25,0x68,0x11,0x86,0xb3,0xbd,0xf7,0x6f,0x61,0x22,0xa2,0x26,0x34,
        0x2a,0xbe,0x1e,0x46,0x14,0x68,0x9d,0x44,0x18,0xc2,0x40,0xf4,0x7e,0x5f,0x1b,0xad,
        0x0b,0x94,0xb6,0x67,0xb4,0x0b,0xe1,0xea,0x95,0x9c,0x66,0xdc,0xe7,0x5d,0x6c,0x05,
        0xda,0xd5,0xdf,0x7a,0xef,0xf6,0xdb,0x1f,0x82,0x4c,0xc0,0x68,0x47,0xa1,0xbd,0xee,
        0x39,0x50,0x56,0x4a,0xdd,0xdf,0xa5,0xf8,0xc6,0xda,0xca,0x90,0xca,0x01,0x42,0x9d,
        0x8b,0x0c,0x73,0x43,0x75,0x05,0x94,0xde,0x24,0xb3,0x80,0x34,0xe5,0x2c,0xdc,0x9b,
        0x3f,0xca,0x33,0x45,0xd0,0xdb,0x5f,0xf5,0x52,0xc3,0x21,0xda,0xe2,0x22,0x72,0x6b,
        0x3e,0xd0,0x5b,0xa8,0x87,0x8c,0x06,0x5d,0x0f,0xdd,0x09,0x19,0x93,0xd0,0xb9,0xfc,
        0x8b,0x0f,0x84,0x60,0x33,0x1c,0x9b,0x45,0xf1,0xf0,0xa3,0x94,0x3a,0x12,0x77,0x33,
        0x4d,0x44,0x78,0x28,0x3c,0x9e,0xfd,0x65,0x57,0x16,0x94,0x6b,0xfb,0x59,0xd0,0xc8,
        0x22,0x36,0xdb,0xd2,0x63,0x98,0x43,0xa1,0x04,0x87,0x86,0xf7,0xa6,0x26,0xbb,0xd6,
        0x59,0x4d,0xbf,0x6a,0x2e,0xaa,0x2b,0xef,0xe6,0x78,0xb6,0x4e,0xe0,0x2f,0xdc,0x7c,
        0xbe,0x57,0x19,0x32,0x7e,0x2a,0xd0,0xb8,0xba,0x29,0x00,0x3c,0x52,0x7d,0xa8,0x49,
        0x3b,0x2d,0xeb,0x25,0x49,0xfa,0xa3,0xaa,0x39,0xa7,0xc5,0xa7,0x50,0x11,0x36,0xfb,
        0xc6,0x67,0x4a,0xf5,0xa5,0x12,0x65,0x7e,0xb0,0xdf,0xaf,0x4e,0xb3,0x61,0x7f,0x2f
    };
}

internal static class NikonMakerNoteCodec
{
    internal static RawNikonMakerNoteTag[] Decode(RawMakerNote note, RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        System.Collections.Generic.List<RawNikonMakerNoteTag> tags = new System.Collections.Generic.List<RawNikonMakerNoteTag>();
        if (note == null || note.Signature != "NikonEmbeddedTiff" || note.RawBytes == null || note.RawBytes.Length < 20)
        {
            return tags.ToArray();
        }
        int ifdPosition = 10 + (int)MakerNoteCodec.ReadUInt32(note.RawBytes, 14, note.ByteOrder);
        ushort count = MakerNoteCodec.ReadUInt16(note.RawBytes, ifdPosition, note.ByteOrder);
        int index = 0;
        while (index < count)
        {
            int entryPosition = ifdPosition + 2 + index * 12;
            ushort id = MakerNoteCodec.ReadUInt16(note.RawBytes, entryPosition, note.ByteOrder);
            ushort typeCode = MakerNoteCodec.ReadUInt16(note.RawBytes, entryPosition + 2, note.ByteOrder);
            int elementSize = MakerNoteCodec.ElementSize(typeCode);
            uint countValue = MakerNoteCodec.ReadUInt32(note.RawBytes, entryPosition + 4, note.ByteOrder);
            int valueLength = (int)((long)countValue * elementSize);
            int valuePosition = entryPosition + 8;
            if (valueLength > 4)
            {
                uint storedOffset = MakerNoteCodec.ReadUInt32(note.RawBytes, entryPosition + 8, note.ByteOrder);
                valuePosition = (int)(note.OffsetBaseAbsolute + storedOffset - note.Offset);
            }
            byte[] valueBytes = new byte[valueLength];
            note.RawBytes.AsSpan(valuePosition, valueLength).CopyTo(valueBytes);
            RawTagValue value = new RawTagValue((RawTagType)typeCode, countValue, note.ByteOrder, valueBytes);
            NikonMakerNoteDefinition? definition = Definition(id);
            string name = definition == null ? "Tag0x" + id.ToString("X4", System.Globalization.CultureInfo.InvariantCulture) : definition.Name;
            RawNikonMakerNoteVersion minimumVersion = definition == null ? RawNikonMakerNoteVersion.Unknown : definition.MinimumVersion;
            bool protectedValue = definition != null && definition.Protected;
            bool writable = definition != null && !definition.Protected && definition.Type == value.Type && (definition.Count == 0 || definition.Count == value.Count);
            tags.Add(new RawNikonMakerNoteTag(id, name, minimumVersion, value, entryPosition, valuePosition, writable, protectedValue));
            index++;
        }
        return tags.ToArray();
    }

    internal static string Version(RawNikonMakerNoteTag[] tags)
    {
        RawNikonMakerNoteTag? tag = Find(tags, 0x0001);
        if (tag == null || tag.Value == null || tag.Value.RawBytes == null || tag.Value.RawBytes.Length < 4)
        {
            return string.Empty;
        }
        int index = 0;
        while (index < 4)
        {
            if (tag.Value.RawBytes[index] < (byte)'0' || tag.Value.RawBytes[index] > (byte)'9')
            {
                return string.Empty;
            }
            index++;
        }
        return System.Text.Encoding.ASCII.GetString(tag.Value.RawBytes, 0, 4);
    }

    internal static RawNikonColorBalance? DecodeColorBalance(RawMakerNote note, RawNikonMakerNoteTag[] tags)
    {
        RawNikonMakerNoteTag? colorTag = Find(tags, 0x0097);
        if (colorTag == null || colorTag.Value == null || colorTag.Value.RawBytes == null || colorTag.Value.RawBytes.Length < 4)
        {
            return null;
        }
        byte[] original = colorTag.Value.CopyBytes();
        string version = System.Text.Encoding.ASCII.GetString(original, 0, 4);
        NikonColorBalanceLayout layout = ColorLayout(version);
        if (!layout.Known || layout.LevelsPosition < 0 || layout.LevelsPosition > original.Length - 8)
        {
            return new RawNikonColorBalance(version, layout.Encrypted, false, original, null, layout.ChannelOrder, null, layout.DecryptStart, layout.LevelsPosition);
        }
        byte[] decoded = RawTextValue.Copy(original);
        if (layout.Encrypted)
        {
            uint serial = SerialKey(Find(tags, 0x001D));
            uint shutter = ShutterKey(Find(tags, 0x00A7));
            if (serial == uint.MaxValue || shutter == uint.MaxValue || layout.DecryptStart < 0 || layout.DecryptStart > decoded.Length)
            {
                return new RawNikonColorBalance(version, true, false, original, null, layout.ChannelOrder, null, layout.DecryptStart, layout.LevelsPosition);
            }
            Transform(decoded, layout.DecryptStart, serial, shutter);
        }
        ushort[] levels = new ushort[4];
        int index = 0;
        while (index < levels.Length)
        {
            levels[index] = EndianReader.ReadUInt16(decoded.AsSpan(layout.LevelsPosition + index * 2, 2), note.ByteOrder);
            index++;
        }
        return new RawNikonColorBalance(version, layout.Encrypted, true, original, decoded, layout.ChannelOrder, levels, layout.DecryptStart, layout.LevelsPosition);
    }

    internal static RawMakerNote Encode(RawMakerNote note, RawNikonMakerNoteEdit[] edits, RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        if (note == null || edits == null)
        {
            throw Invalid("A Nikon maker note and an edit collection are required.", -1);
        }
        if (edits.LongLength > limits.MaxTagCount)
        {
            throw new RawWriteException(RawDiagnosticCode.ResourceLimitExceeded, "The Nikon maker-note edit count exceeds configured tag limits.", note.Offset);
        }
        if (note.Signature != "NikonEmbeddedTiff" || note.OffsetBase != RawMakerNoteOffsetBase.EmbeddedTiffHeader || !note.RelocationSupported || (note.RawBytes ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length < 8 || note.RawBytes[6] != 2 || !note.Validate(limits).IsValid())
        {
            throw Invalid("Only a structurally validated Nikon type-3 version-2 maker note can be encoded.", note.Offset);
        }
        byte[] bytes = note.CopyBytes();
        System.Collections.Generic.List<ushort> editedIds = new System.Collections.Generic.List<ushort>();
        int index = 0;
        while (index < edits.Length)
        {
            RawNikonMakerNoteEdit edit = edits[index];
            if (edit == null || edit.Value == null || edit.Version < RawNikonMakerNoteVersion.Baseline || edit.Version > RawNikonMakerNoteVersion.Extended)
            {
                throw Invalid("Every Nikon maker-note edit requires a reviewed schema version and typed value.", note.Offset);
            }
            if (!ActualVersionSupports(note.NikonVersion, edit.Version))
            {
                throw Unsupported("The Nikon maker-note data version does not support the selected reviewed schema.", note.Offset);
            }
            if (Contains(editedIds, edit.TagId))
            {
                throw Invalid("A Nikon maker-note tag cannot be replaced more than once in one encoding operation.", note.Offset);
            }
            NikonMakerNoteDefinition? definition = Definition(edit.TagId);
            if (definition == null || definition.Protected || edit.Version < definition.MinimumVersion)
            {
                throw Unsupported("The requested Nikon maker-note tag is protected or not reviewed for the selected schema version.", note.Offset);
            }
            RawNikonMakerNoteTag tag = FindUnique(note.NikonTags, edit.TagId, note.Offset);
            if (!tag.Writable)
            {
                throw Unsupported("The existing Nikon maker-note tag type or count is not safe for reviewed replacement.", note.Offset + tag.EntryPosition);
            }
            WriteReplacement(bytes, tag, definition, edit.Value, note.ByteOrder, note.Offset);
            editedIds.Add(edit.TagId);
            index++;
        }
        return MakerNoteCodec.Parse(bytes, note.Offset, note.EntryOffset, note.IfdPath, note.Manufacturer, note.Offset, note.ByteOrder, limits);
    }

    internal static void WriteReplacement(byte[] bytes, RawNikonMakerNoteTag tag, NikonMakerNoteDefinition definition, RawTagValue replacement, RawByteOrder byteOrder, long noteOffset)
    {
        if (replacement.Type != definition.Type)
        {
            throw Invalid("The Nikon maker-note replacement type does not match the reviewed existing tag.", noteOffset + tag.ValuePosition);
        }
        if (definition.Type == RawTagType.Ascii)
        {
            WriteAscii(bytes, tag, replacement, noteOffset);
            return;
        }
        if (replacement.Count != tag.Value.Count || replacement.RawBytes == null || replacement.RawBytes.Length != tag.Value.RawBytes.Length)
        {
            throw Invalid("The Nikon maker-note numeric replacement must exactly match existing fixed storage.", noteOffset + tag.ValuePosition);
        }
        int elementSize = MakerNoteCodec.ElementSize((ushort)definition.Type);
        int index = 0;
        while (index < (int)replacement.Count)
        {
            if (elementSize == 2)
            {
                ushort value = EndianReader.ReadUInt16(replacement.RawBytes.AsSpan(index * 2, 2), replacement.ByteOrder);
                EndianWriter.WriteUInt16(bytes.AsSpan(tag.ValuePosition + index * 2, 2), value, byteOrder);
            }
            else
            {
                uint value = EndianReader.ReadUInt32(replacement.RawBytes.AsSpan(index * 4, 4), replacement.ByteOrder);
                EndianWriter.WriteUInt32(bytes.AsSpan(tag.ValuePosition + index * 4, 4), value, byteOrder);
            }
            index++;
        }
    }

    internal static void WriteAscii(byte[] bytes, RawNikonMakerNoteTag tag, RawTagValue replacement, long noteOffset)
    {
        if (replacement.RawBytes == null || replacement.RawBytes.Length == 0 || replacement.RawBytes[replacement.RawBytes.Length - 1] != 0 || replacement.RawBytes.Length > tag.Value.RawBytes.Length)
        {
            throw Invalid("A reviewed Nikon text replacement must be null-terminated ASCII that fits existing storage.", noteOffset + tag.ValuePosition);
        }
        int index = 0;
        while (index < replacement.RawBytes.Length - 1)
        {
            if (replacement.RawBytes[index] == 0 || replacement.RawBytes[index] > 127)
            {
                throw Invalid("A reviewed Nikon text replacement must contain only non-null seven-bit characters before its terminator.", noteOffset + tag.ValuePosition);
            }
            index++;
        }
        index = 0;
        while (index < tag.Value.RawBytes.Length)
        {
            bytes[tag.ValuePosition + index] = 0;
            index++;
        }
        replacement.RawBytes.AsSpan(0, replacement.RawBytes.Length - 1).CopyTo(bytes.AsSpan(tag.ValuePosition, replacement.RawBytes.Length - 1));
    }

    internal static void Transform(byte[] bytes, int start, uint serial, uint shutter)
    {
        byte key = (byte)((shutter & 0xff) ^ (shutter >> 8 & 0xff) ^ (shutter >> 16 & 0xff) ^ (shutter >> 24 & 0xff));
        int ci = NikonMakerNoteKeys.Serial[serial & 0xff];
        int cj = NikonMakerNoteKeys.Shutter[key];
        int ck = 0x60;
        int index = start;
        while (index < bytes.Length)
        {
            cj = (cj + ci * ck) & 0xff;
            ck = (ck + 1) & 0xff;
            bytes[index] = (byte)(bytes[index] ^ cj);
            index++;
        }
    }

    internal static uint SerialKey(RawNikonMakerNoteTag? tag)
    {
        if (tag == null || tag.Value == null || tag.Value.RawBytes == null || tag.Value.RawBytes.Length == 0)
        {
            return uint.MaxValue;
        }
        uint value = 0;
        int index = 0;
        bool any = false;
        while (index < tag.Value.RawBytes.Length && tag.Value.RawBytes[index] != 0)
        {
            byte digit = tag.Value.RawBytes[index];
            if (digit < (byte)'0' || digit > (byte)'9' || value > (uint.MaxValue - (uint)(digit - (byte)'0')) / 10)
            {
                return uint.MaxValue;
            }
            value = value * 10 + (uint)(digit - (byte)'0');
            any = true;
            index++;
        }
        return any ? value : uint.MaxValue;
    }

    internal static uint ShutterKey(RawNikonMakerNoteTag? tag)
    {
        if (tag == null || tag.Value == null || tag.Value.Type != RawTagType.Long || tag.Value.Count != 1)
        {
            return uint.MaxValue;
        }
        return tag.Value.GetUInt32Values()[0];
    }

    internal static NikonColorBalanceLayout ColorLayout(string version)
    {
        if (version == "0100") return new NikonColorBalanceLayout(true, false, -1, 72, "RBGG");
        if (version == "0102") return new NikonColorBalanceLayout(true, false, -1, 10, "RGGB");
        if (version == "0103") return new NikonColorBalanceLayout(true, false, -1, 20, "RGBG");
        if (version == "0205") return new NikonColorBalanceLayout(true, true, 4, 18, "RGGB");
        if (version == "0204" || version == "0206" || version == "0207" || version == "0208" || version == "0210") return new NikonColorBalanceLayout(true, true, 284, 290, "RGGB");
        if (version == "0209" || version == "0212" || version == "0214") return new NikonColorBalanceLayout(true, true, 284, 294, "GRBG");
        if (version == "0211") return new NikonColorBalanceLayout(true, true, 284, 300, "GRBG");
        if (version == "0213") return new NikonColorBalanceLayout(true, true, 284, 294, "RGGB");
        if (version == "0215" || version == "0216" || version == "0217") return new NikonColorBalanceLayout(true, true, 284, 288, "GRBG");
        if (version == "0219" || version == "0221" || version == "0222" || version == "0223" || version == "0224") return new NikonColorBalanceLayout(true, true, 4, 128, "RGGB");
        return new NikonColorBalanceLayout(false, version.Length == 4 && version[0] == '0' && version[1] == '2', -1, -1, string.Empty);
    }

    internal static bool ActualVersionSupports(string? version, RawNikonMakerNoteVersion schema)
    {
        if (version == null || version.Length != 4)
        {
            return false;
        }
        int index = 0;
        while (index < version.Length)
        {
            if (version[index] < '0' || version[index] > '9')
            {
                return false;
            }
            index++;
        }
        if (schema == RawNikonMakerNoteVersion.Extended)
        {
            return string.CompareOrdinal(version, "0200") >= 0;
        }
        return string.CompareOrdinal(version, "0100") >= 0;
    }

    internal static RawNikonMakerNoteTag? Find(RawNikonMakerNoteTag[] tags, ushort id)
    {
        if (tags == null)
        {
            return null;
        }
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index] != null && tags[index].Id == id)
            {
                return tags[index];
            }
            index++;
        }
        return null;
    }

    internal static RawNikonMakerNoteTag FindUnique(RawNikonMakerNoteTag[]? tags, ushort id, long noteOffset)
    {
        RawNikonMakerNoteTag? match = null;
        int index = 0;
        while (index < (tags ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            if (tags[index].Id == id)
            {
                if (match != null)
                {
                    throw Invalid("The Nikon maker-note tag is duplicated and cannot be replaced unambiguously.", noteOffset + tags[index].EntryPosition);
                }
                match = tags[index];
            }
            index++;
        }
        if (match == null)
        {
            throw Invalid("Only existing Nikon maker-note tags can be replaced without changing private layout.", noteOffset);
        }
        return match;
    }

    internal static bool Contains(System.Collections.Generic.List<ushort> values, ushort value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static NikonMakerNoteDefinition? Definition(ushort id)
    {
        switch (id)
        {
            case 0x0001: return new NikonMakerNoteDefinition(id, "MakerNoteVersion", RawTagType.Undefined, 4, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0002: return new NikonMakerNoteDefinition(id, "ISO", RawTagType.Short, 2, RawNikonMakerNoteVersion.Baseline, false);
            case 0x0003: return new NikonMakerNoteDefinition(id, "ColorMode", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Baseline, false);
            case 0x0004: return new NikonMakerNoteDefinition(id, "Quality", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Baseline, false);
            case 0x0005: return new NikonMakerNoteDefinition(id, "WhiteBalance", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Baseline, false);
            case 0x0006: return new NikonMakerNoteDefinition(id, "Sharpness", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Baseline, false);
            case 0x0007: return new NikonMakerNoteDefinition(id, "FocusMode", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Baseline, false);
            case 0x001D: return new NikonMakerNoteDefinition(id, "SerialNumber", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0083: return new NikonMakerNoteDefinition(id, "LensType", RawTagType.Byte, 1, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0084: return new NikonMakerNoteDefinition(id, "Lens", RawTagType.Rational, 4, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0089: return new NikonMakerNoteDefinition(id, "ShootingMode", RawTagType.Short, 1, RawNikonMakerNoteVersion.Unknown, true);
            case 0x008c: return new NikonMakerNoteDefinition(id, "CompressionCurve", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0092: return new NikonMakerNoteDefinition(id, "HueAdjustment", RawTagType.SignedShort, 1, RawNikonMakerNoteVersion.Extended, false);
            case 0x0093: return new NikonMakerNoteDefinition(id, "NEFCompression", RawTagType.Short, 1, RawNikonMakerNoteVersion.Extended, false);
            case 0x0094: return new NikonMakerNoteDefinition(id, "SaturationAdjustment", RawTagType.SignedShort, 1, RawNikonMakerNoteVersion.Extended, false);
            case 0x0095: return new NikonMakerNoteDefinition(id, "NoiseReduction", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Extended, false);
            case 0x0096: return new NikonMakerNoteDefinition(id, "NEFCompressionInfo", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0097: return new NikonMakerNoteDefinition(id, "ColorBalance", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0098: return new NikonMakerNoteDefinition(id, "LensData", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x00A7: return new NikonMakerNoteDefinition(id, "ShutterCount", RawTagType.Long, 1, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0E01: return new NikonMakerNoteDefinition(id, "NikonCaptureData", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0E09: return new NikonMakerNoteDefinition(id, "NikonCaptureVersion", RawTagType.Ascii, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0E0E: return new NikonMakerNoteDefinition(id, "NikonCaptureOffsets", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0E13: return new NikonMakerNoteDefinition(id, "NikonCaptureEditVersions", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            case 0x0E1E: return new NikonMakerNoteDefinition(id, "NikonCaptureOutput", RawTagType.Undefined, 0, RawNikonMakerNoteVersion.Unknown, true);
            default: return null;
        }
    }

    internal static RawWriteException Invalid(string message, long offset)
    {
        return new RawWriteException(RawDiagnosticCode.InvalidMetadata, message, offset);
    }

    internal static RawWriteException Unsupported(string message, long offset)
    {
        return new RawWriteException(RawDiagnosticCode.UnsupportedProfile, message, offset);
    }
}
