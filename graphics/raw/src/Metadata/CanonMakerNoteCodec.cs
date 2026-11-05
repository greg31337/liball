using System;

namespace raw;

internal class CanonMakerNoteDefinition
{
    internal ushort Id;
    internal string Name;
    internal RawTagType Type;
    internal RawCanonMakerNoteVersion MinimumVersion;
    internal int MinimumStorage;

    internal CanonMakerNoteDefinition(ushort id, string name, RawTagType type, RawCanonMakerNoteVersion minimumVersion, int minimumStorage)
    {
        Id = id;
        Name = name;
        Type = type;
        MinimumVersion = minimumVersion;
        MinimumStorage = minimumStorage;
    }
}

internal static class CanonMakerNoteCodec
{
    internal static RawCanonMakerNoteTag[] Decode(RawMakerNote note, RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        System.Collections.Generic.List<RawCanonMakerNoteTag> tags = new System.Collections.Generic.List<RawCanonMakerNoteTag>();
        if (note == null || note.Signature != "CanonIfd" || note.RawBytes == null || note.RawBytes.Length < 6)
        {
            return tags.ToArray();
        }
        ushort count = MakerNoteCodec.ReadUInt16(note.RawBytes, 0, note.ByteOrder);
        int index = 0;
        while (index < count)
        {
            int entryPosition = 2 + index * 12;
            ushort id = MakerNoteCodec.ReadUInt16(note.RawBytes, entryPosition, note.ByteOrder);
            ushort typeCode = MakerNoteCodec.ReadUInt16(note.RawBytes, entryPosition + 2, note.ByteOrder);
            int elementSize = MakerNoteCodec.ElementSize(typeCode);
            uint countValue = MakerNoteCodec.ReadUInt32(note.RawBytes, entryPosition + 4, note.ByteOrder);
            long valueLength = (long)countValue * elementSize;
            int valuePosition = entryPosition + 8;
            if (valueLength > 4)
            {
                uint storedOffset = MakerNoteCodec.ReadUInt32(note.RawBytes, entryPosition + 8, note.ByteOrder);
                valuePosition = ToPosition(note, storedOffset);
            }
            byte[] valueBytes = new byte[(int)valueLength];
            note.RawBytes.AsSpan(valuePosition, valueBytes.Length).CopyTo(valueBytes);
            RawTagValue value = new RawTagValue((RawTagType)typeCode, countValue, note.ByteOrder, valueBytes);
            CanonMakerNoteDefinition? definition = Definition(id);
            string name = definition == null ? "Tag0x" + id.ToString("X4", System.Globalization.CultureInfo.InvariantCulture) : definition.Name;
            RawCanonMakerNoteVersion minimumVersion = definition == null ? RawCanonMakerNoteVersion.Unknown : definition.MinimumVersion;
            bool writable = definition != null && definition.Type == value.Type && StorageSafe(definition, value);
            tags.Add(new RawCanonMakerNoteTag(id, name, minimumVersion, value, entryPosition, valuePosition, writable));
            index++;
        }
        return tags.ToArray();
    }

    internal static RawMakerNote Encode(RawMakerNote note, RawCanonMakerNoteEdit[] edits, RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        if (note == null || edits == null)
        {
            throw Invalid("A Canon maker note and an edit collection are required.", -1);
        }
        if (edits.LongLength > limits.MaxTagCount)
        {
            throw new RawWriteException(RawDiagnosticCode.ResourceLimitExceeded, "The Canon maker-note edit count exceeds configured tag limits.", note.Offset);
        }
        if (note.Signature != "CanonIfd" || note.OffsetBase != RawMakerNoteOffsetBase.TiffHeader || !note.RelocationSupported || !note.Validate(limits).IsValid())
        {
            throw Invalid("Only a structurally validated Canon main-IFD maker note can be encoded.", note.Offset);
        }
        RawCanonMakerNoteTag[] tags = Decode(note, limits);
        byte[] bytes = note.CopyBytes();
        System.Collections.Generic.List<ushort> editedIds = new System.Collections.Generic.List<ushort>();
        int index = 0;
        while (index < edits.Length)
        {
            RawCanonMakerNoteEdit edit = edits[index];
            if (edit == null || edit.Value == null || edit.Version < RawCanonMakerNoteVersion.Baseline || edit.Version > RawCanonMakerNoteVersion.Extended)
            {
                throw Invalid("Every Canon maker-note edit requires a reviewed schema version and typed value.", note.Offset);
            }
            if (Contains(editedIds, edit.TagId))
            {
                throw Invalid("A Canon maker-note tag cannot be replaced more than once in one encoding operation.", note.Offset);
            }
            CanonMakerNoteDefinition? definition = Definition(edit.TagId);
            if (definition == null || edit.Version < definition.MinimumVersion)
            {
                throw Unsupported("The requested Canon maker-note tag is not reviewed for the selected schema version.", note.Offset);
            }
            RawCanonMakerNoteTag tag = FindUnique(tags, edit.TagId, note.Offset);
            if (!tag.Writable || tag.Value.Type != definition.Type)
            {
                throw Unsupported("The existing Canon maker-note tag type or storage is not safe for reviewed replacement.", note.Offset + tag.EntryPosition);
            }
            WriteReplacement(bytes, tag, definition, edit.Value, note.ByteOrder, note.Offset);
            editedIds.Add(edit.TagId);
            index++;
        }
        return MakerNoteCodec.Parse(bytes, note.Offset, note.EntryOffset, note.IfdPath, note.Manufacturer, note.OffsetBaseAbsolute, note.ByteOrder, limits);
    }

    internal static void WriteReplacement(byte[] bytes, RawCanonMakerNoteTag tag, CanonMakerNoteDefinition definition, RawTagValue replacement, RawByteOrder byteOrder, long noteOffset)
    {
        if (replacement.Type != definition.Type)
        {
            throw Invalid("The Canon maker-note replacement type does not match the reviewed tag definition.", noteOffset + tag.ValuePosition);
        }
        if (definition.Type == RawTagType.Ascii)
        {
            WriteAscii(bytes, tag, definition, replacement, noteOffset);
            return;
        }
        if (replacement.Count != 1 || replacement.RawBytes == null || replacement.RawBytes.Length != 4)
        {
            throw Invalid("The reviewed Canon numeric replacement requires one unsigned 32-bit value.", noteOffset + tag.ValuePosition);
        }
        uint[] values = replacement.GetUInt32Values();
        EndianWriter.WriteUInt32(bytes.AsSpan(tag.ValuePosition, 4), values[0], byteOrder);
    }

    internal static void WriteAscii(byte[] bytes, RawCanonMakerNoteTag tag, CanonMakerNoteDefinition definition, RawTagValue replacement, long noteOffset)
    {
        if (replacement.RawBytes == null || replacement.RawBytes.Length == 0 || replacement.RawBytes[replacement.RawBytes.Length - 1] != 0)
        {
            throw Invalid("A reviewed Canon text replacement must be null-terminated ASCII.", noteOffset + tag.ValuePosition);
        }
        int index = 0;
        while (index < replacement.RawBytes.Length - 1)
        {
            byte value = replacement.RawBytes[index];
            if (value == 0 || value > 127)
            {
                throw Invalid("A reviewed Canon text replacement must contain only non-null seven-bit characters before its terminator.", noteOffset + tag.ValuePosition);
            }
            index++;
        }
        int storageLength = tag.Value.RawBytes.Length;
        if (storageLength < definition.MinimumStorage || replacement.RawBytes.Length > storageLength)
        {
            throw Invalid("The Canon text replacement does not fit the existing reviewed fixed storage.", noteOffset + tag.ValuePosition);
        }
        index = 0;
        while (index < storageLength)
        {
            bytes[tag.ValuePosition + index] = 0;
            index++;
        }
        replacement.RawBytes.AsSpan(0, replacement.RawBytes.Length - 1).CopyTo(bytes.AsSpan(tag.ValuePosition, replacement.RawBytes.Length - 1));
    }

    internal static RawCanonMakerNoteTag FindUnique(RawCanonMakerNoteTag[] tags, ushort id, long noteOffset)
    {
        RawCanonMakerNoteTag? match = null;
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index].Id == id)
            {
                if (match != null)
                {
                    throw Invalid("The Canon maker-note tag is duplicated and cannot be replaced unambiguously.", noteOffset + tags[index].EntryPosition);
                }
                match = tags[index];
            }
            index++;
        }
        if (match == null)
        {
            throw Invalid("Only existing Canon maker-note tags can be replaced without changing private layout.", noteOffset);
        }
        return match;
    }

    internal static bool StorageSafe(CanonMakerNoteDefinition definition, RawTagValue value)
    {
        if (definition.Type == RawTagType.Ascii)
        {
            return value.RawBytes.Length >= definition.MinimumStorage;
        }
        return value.Count == 1 && value.RawBytes.Length == 4;
    }

    internal static int ToPosition(RawMakerNote note, uint storedOffset)
    {
        long absolute = note.OffsetBaseAbsolute + storedOffset;
        return (int)(absolute - note.Offset);
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

    internal static CanonMakerNoteDefinition? Definition(ushort id)
    {
        switch (id)
        {
            case 0x0006: return new CanonMakerNoteDefinition(id, "CanonImageType", RawTagType.Ascii, RawCanonMakerNoteVersion.Baseline, 1);
            case 0x0007: return new CanonMakerNoteDefinition(id, "CanonFirmwareVersion", RawTagType.Ascii, RawCanonMakerNoteVersion.Baseline, 1);
            case 0x0008: return new CanonMakerNoteDefinition(id, "FileNumber", RawTagType.Long, RawCanonMakerNoteVersion.Baseline, 4);
            case 0x0009: return new CanonMakerNoteDefinition(id, "OwnerName", RawTagType.Ascii, RawCanonMakerNoteVersion.Baseline, 32);
            case 0x000C: return new CanonMakerNoteDefinition(id, "SerialNumber", RawTagType.Long, RawCanonMakerNoteVersion.Baseline, 4);
            case 0x0010: return new CanonMakerNoteDefinition(id, "ModelID", RawTagType.Long, RawCanonMakerNoteVersion.Extended, 4);
            case 0x0015: return new CanonMakerNoteDefinition(id, "SerialNumberFormat", RawTagType.Long, RawCanonMakerNoteVersion.Extended, 4);
            case 0x001E: return new CanonMakerNoteDefinition(id, "FirmwareRevision", RawTagType.Long, RawCanonMakerNoteVersion.Extended, 4);
            case 0x0095: return new CanonMakerNoteDefinition(id, "LensModel", RawTagType.Ascii, RawCanonMakerNoteVersion.Extended, 1);
            case 0x0096: return new CanonMakerNoteDefinition(id, "InternalSerialNumber", RawTagType.Ascii, RawCanonMakerNoteVersion.Extended, 1);
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
