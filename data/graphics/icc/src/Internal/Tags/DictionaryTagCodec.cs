namespace icc;

internal class DictionaryWriteRecord
{
    internal IccMetadataEntry Entry;
    internal byte[] NameBytes;
    internal byte[] ValueBytes;
    internal byte[] DisplayNameBytes;
    internal byte[] DisplayValueBytes;
    internal int NameStorageIndex;
    internal int ValueStorageIndex;
    internal int DisplayNameStorageIndex;
    internal int DisplayValueStorageIndex;
    internal bool HasValue;
    internal bool HasDisplayNameElement;
    internal bool HasDisplayValueElement;

    internal DictionaryWriteRecord(IccMetadataEntry entry)
    {
        Entry = entry;
        NameBytes = new byte[0];
        ValueBytes = new byte[0];
        DisplayNameBytes = new byte[0];
        DisplayValueBytes = new byte[0];
        NameStorageIndex = -1;
        ValueStorageIndex = -1;
        DisplayNameStorageIndex = -1;
        DisplayValueStorageIndex = -1;
        HasValue = false;
        HasDisplayNameElement = false;
        HasDisplayValueElement = false;
    }
}

internal static class DictionaryTagCodec
{
    internal static void DecodeDictionary(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 16)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A dictType payload requires at least 16 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The dictType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 8), state.AbsolutePayloadOffset + 8);
        uint declaredRecordCount = headerReader.ReadUInt32();
        uint declaredRecordSize = headerReader.ReadUInt32();
        if (declaredRecordSize != 16 && declaredRecordSize != 24 && declaredRecordSize != 32)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A dictType record size shall be 16, 24, or 32 bytes, but this payload declares {declaredRecordSize} bytes.");
            return;
        }

        int recordSize = (int)declaredRecordSize;
        int maximumRecordCount = (payload.Length - 16) / recordSize;
        if (declaredRecordCount > (uint)maximumRecordCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The dictType declares {declaredRecordCount} records, but its table can contain at most {maximumRecordCount} records of {recordSize} bytes.");
            return;
        }

        int recordCount = (int)declaredRecordCount;
        int storageOffset = 16 + (recordCount * recordSize);
        IccMetadataEntry[] entries = new IccMetadataEntry[recordCount];
        System.Collections.Generic.Dictionary<string, bool> names = new System.Collections.Generic.Dictionary<string, bool>();
        System.Collections.Generic.Dictionary<ulong, IccLocalizedString[]> displayElements = new System.Collections.Generic.Dictionary<ulong, IccLocalizedString[]>();
        int registeredCount = 0;
        int recordIndex = 0;
        while (recordIndex < recordCount)
        {
            int recordOffset = 16 + (recordIndex * recordSize);
            BigEndianReader recordReader = new BigEndianReader(payload.Slice(recordOffset, recordSize), state.AbsolutePayloadOffset + recordOffset);
            uint nameOffset = recordReader.ReadUInt32();
            uint nameLength = recordReader.ReadUInt32();
            uint valueOffset = recordReader.ReadUInt32();
            uint valueLength = recordReader.ReadUInt32();
            uint displayNameOffset = 0;
            uint displayNameLength = 0;
            uint displayValueOffset = 0;
            uint displayValueLength = 0;
            if (recordSize >= 24)
            {
                displayNameOffset = recordReader.ReadUInt32();
                displayNameLength = recordReader.ReadUInt32();
            }

            if (recordSize == 32)
            {
                displayValueOffset = recordReader.ReadUInt32();
                displayValueLength = recordReader.ReadUInt32();
            }

            if (nameOffset == 0 || nameLength == 0 || !IsPositionValid(payload.Length, storageOffset, nameOffset, nameLength) || (nameLength & 1) != 0)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} does not reference a non-empty, aligned UTF-16BE name in the storage area.");
                return;
            }

            if (valueOffset != 0 && (!IsPositionValid(payload.Length, storageOffset, valueOffset, valueLength) || (valueLength & 1) != 0))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} does not reference an aligned UTF-16BE value in the storage area.");
                return;
            }

            if (displayNameOffset != 0 && !IsDisplayPositionValid(payload.Length, storageOffset, displayNameOffset, displayNameLength))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} has an invalid localized display-name position.");
                return;
            }

            if (displayValueOffset != 0 && !IsDisplayPositionValid(payload.Length, storageOffset, displayValueOffset, displayValueLength))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} has an invalid localized display-value position.");
                return;
            }

            System.ReadOnlyMemory<byte> nameBytes = payload.Slice((int)nameOffset, (int)nameLength);
            if (!LocalizedUnicodeTagCodec.IsValidUtf16BigEndian(nameBytes))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} contains an invalid UTF-16BE name.");
                return;
            }

            string name = LocalizedUnicodeTagCodec.DecodeUtf16BigEndian(nameBytes);
            if (names.ContainsKey(name))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} duplicates the name '{name}'.");
                return;
            }

            names.Add(name, true);
            string value = string.Empty;
            if (valueOffset != 0)
            {
                System.ReadOnlyMemory<byte> valueBytes = payload.Slice((int)valueOffset, (int)valueLength);
                if (!LocalizedUnicodeTagCodec.IsValidUtf16BigEndian(valueBytes))
                {
                    TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} contains an invalid UTF-16BE value.");
                    return;
                }

                value = LocalizedUnicodeTagCodec.DecodeUtf16BigEndian(valueBytes);
            }

            IccLocalizedString[]? displayNames = DecodeDisplayElement(state, payload, displayNameOffset, displayNameLength, displayElements);
            if (displayNames == null)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} contains an invalid multiLocalizedUnicodeType display-name element.");
                return;
            }

            IccLocalizedString[]? displayValues = DecodeDisplayElement(state, payload, displayValueOffset, displayValueLength, displayElements);
            if (displayValues == null)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Dictionary record {recordIndex} contains an invalid multiLocalizedUnicodeType display-value element.");
                return;
            }

            IccMetadataEntry entry = new IccMetadataEntry();
            entry.Name = name;
            entry.Value = value;
            entry.HasValue = valueOffset != 0;
            entry.IsRegisteredName = IsRegisteredMetadataName(name);
            entry.DisplayNames = displayNames;
            entry.HasDisplayNameElement = displayNameOffset != 0;
            entry.DisplayValues = displayValues;
            entry.HasDisplayValueElement = displayValueOffset != 0;
            entries[recordIndex] = entry;
            if (entry.IsRegisteredName)
            {
                registeredCount = registeredCount + 1;
            }

            recordIndex = recordIndex + 1;
        }

        int unknownCount = recordCount - registeredCount;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Dictionary);
        data.Metadata = entries;
        string message = $"Decoded {recordCount} dictionary entries in encoded order; {registeredCount} names are registered ICC metadata keys and {unknownCount} names are retained as unknown keys.";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static byte[] SerializeDictionary(IccMetadataEntry[] entries)
    {
        return SerializeDictionaryForMode(entries, IccWriteMode.Canonical);
    }

    internal static byte[] SerializeDictionaryForMode(IccMetadataEntry[] entries, IccWriteMode mode)
    {
        if (entries == null)
        {
            throw new IccProfileWriteException("The dictionary entry collection cannot be null.");
        }

        IccMetadataEntry[] orderedEntries = CreateEntryOrderForMode(entries, mode);
        int recordSize = DetermineRecordSize(orderedEntries);
        long tableSize = 16L + ((long)orderedEntries.Length * recordSize);
        if (tableSize > int.MaxValue)
        {
            throw new IccProfileWriteException("The dictionary record table exceeds the supported managed size.");
        }

        DictionaryWriteRecord[] records = new DictionaryWriteRecord[orderedEntries.Length];
        System.Collections.Generic.Dictionary<string, bool> names = new System.Collections.Generic.Dictionary<string, bool>();
        System.Collections.Generic.Dictionary<string, int> storageIndices = new System.Collections.Generic.Dictionary<string, int>();
        System.Collections.Generic.List<byte[]> storageItems = new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < orderedEntries.Length)
        {
            IccMetadataEntry entry = orderedEntries[index];
            string name = DeterministicTagSerialization.NormalizeUnicode(entry.Name, mode, "Dictionary entry name");
            string value = DeterministicTagSerialization.NormalizeUnicode(entry.Value, mode, "Dictionary entry value");
            if (name.Length == 0)
            {
                throw new IccProfileWriteException("Every dictionary entry must have a non-empty name.");
            }

            if (names.ContainsKey(name))
            {
                throw new IccProfileWriteException($"The dictionary contains more than one entry named '{name}'.");
            }

            names.Add(name, true);
            DictionaryWriteRecord record = new DictionaryWriteRecord(entry);
            record.NameBytes = LocalizedUnicodeTagCodec.EncodeUtf16BigEndian(name);
            record.ValueBytes = LocalizedUnicodeTagCodec.EncodeUtf16BigEndian(value);
            record.HasValue = entry.HasValue || value.Length > 0;
            record.HasDisplayNameElement = entry.HasDisplayNameElement || entry.DisplayNames.Length > 0;
            record.HasDisplayValueElement = entry.HasDisplayValueElement || entry.DisplayValues.Length > 0;
            record.NameStorageIndex = AddStorageItem(record.NameBytes, storageItems, storageIndices);
            if (record.HasValue)
            {
                record.ValueStorageIndex = AddStorageItem(record.ValueBytes, storageItems, storageIndices);
            }

            if (record.HasDisplayNameElement)
            {
                if (entry.DisplayNames.Length > 0)
                {
                    record.DisplayNameBytes = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(entry.DisplayNames, mode);
                }

                record.DisplayNameStorageIndex = AddStorageItem(record.DisplayNameBytes, storageItems, storageIndices);
            }

            if (record.HasDisplayValueElement)
            {
                if (entry.DisplayValues.Length > 0)
                {
                    record.DisplayValueBytes = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(entry.DisplayValues, mode);
                }

                record.DisplayValueStorageIndex = AddStorageItem(record.DisplayValueBytes, storageItems, storageIndices);
            }

            records[index] = record;
            index = index + 1;
        }

        int[] storageOffsets = new int[storageItems.Count];
        long payloadSize = tableSize;
        index = 0;
        while (index < storageItems.Count)
        {
            payloadSize = AlignSizeToFour(payloadSize);
            if (payloadSize > int.MaxValue)
            {
                throw new IccProfileWriteException("The dictionary storage alignment exceeds the supported managed size.");
            }

            storageOffsets[index] = (int)payloadSize;
            payloadSize = payloadSize + storageItems[index].Length;
            if (payloadSize > int.MaxValue)
            {
                throw new IccProfileWriteException("The dictionary storage area exceeds the supported managed size.");
            }

            index = index + 1;
        }

        payloadSize = AlignSizeToFour(payloadSize);
        if (payloadSize > int.MaxValue)
        {
            throw new IccProfileWriteException("The aligned dictionary payload exceeds the supported managed size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Dictionary);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)records.Length);
        writer.WriteUInt32((uint)recordSize);
        index = 0;
        while (index < records.Length)
        {
            DictionaryWriteRecord record = records[index];
            WriteStoragePosition(writer, record.NameStorageIndex, record.NameBytes.Length, storageOffsets);
            WriteStoragePosition(writer, record.ValueStorageIndex, record.ValueBytes.Length, storageOffsets);
            if (recordSize >= 24)
            {
                WriteStoragePosition(writer, record.DisplayNameStorageIndex, record.DisplayNameBytes.Length, storageOffsets);
            }

            if (recordSize == 32)
            {
                WriteStoragePosition(writer, record.DisplayValueStorageIndex, record.DisplayValueBytes.Length, storageOffsets);
            }

            index = index + 1;
        }

        index = 0;
        while (index < storageItems.Count)
        {
            writer.AlignWritePosition(4);
            writer.WriteExactBytes(storageItems[index]);
            index = index + 1;
        }

        writer.AlignWritePosition(4);
        return payload;
    }

    internal static IccMetadataEntry[] CreateDeterministicEntryOrder(IccMetadataEntry[] entries)
    {
        return CreateEntryOrderForMode(entries, IccWriteMode.Canonical);
    }

    internal static IccMetadataEntry[] CreateEntryOrderForMode(IccMetadataEntry[] entries, IccWriteMode mode)
    {
        IccMetadataEntry[] ordered = new IccMetadataEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            IccMetadataEntry entry = entries[index];
            if (entry == null || entry.Name == null || entry.Value == null || entry.DisplayNames == null || entry.DisplayValues == null)
            {
                throw new IccProfileWriteException($"Dictionary entry {index} and its fields cannot be null.");
            }

            ordered[index] = entry;
            int insertionIndex = index;
            string name = DeterministicTagSerialization.NormalizeUnicode(entry.Name, mode, "Dictionary entry name");
            while (mode == IccWriteMode.Canonical && insertionIndex > 0
                && System.String.CompareOrdinal(DeterministicTagSerialization.NormalizeUnicode(ordered[insertionIndex - 1].Name, mode, "Dictionary entry name"), name) > 0)
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            ordered[insertionIndex] = entry;
            index = index + 1;
        }

        return ordered;
    }

    internal static int DetermineRecordSize(IccMetadataEntry[] entries)
    {
        int recordSize = 16;
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].HasDisplayValueElement || entries[index].DisplayValues.Length > 0)
            {
                return 32;
            }

            if (entries[index].HasDisplayNameElement || entries[index].DisplayNames.Length > 0)
            {
                recordSize = 24;
            }

            index = index + 1;
        }

        return recordSize;
    }

    internal static IccLocalizedString[]? DecodeDisplayElement(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint elementOffset, uint elementLength, System.Collections.Generic.Dictionary<ulong, IccLocalizedString[]> cache)
    {
        if (elementOffset == 0 || elementLength == 0)
        {
            return new IccLocalizedString[0];
        }

        ulong key = ((ulong)elementOffset << 32) | elementLength;
        if (cache.ContainsKey(key))
        {
            return cache[key];
        }

        int offset = (int)elementOffset;
        int length = (int)elementLength;
        System.ReadOnlyMemory<byte> elementBytes = payload.Slice(offset, length);
        BigEndianReader signatureReader = new BigEndianReader(elementBytes.Slice(0, 4), state.AbsolutePayloadOffset + offset);
        if (signatureReader.ReadUInt32() != IccTypeSignatures.MultiLocalizedUnicode)
        {
            return null;
        }

        TagDecodeSource source = new TagDecodeSource(elementBytes);
        long absoluteOffset = CheckedSize.AddOffsets(state.AbsolutePayloadOffset, offset, long.MaxValue);
        TagDecodeState nestedState = new TagDecodeState(source, 0, length, absoluteOffset, state.ProfileVersion, false);
        LocalizedUnicodeTagCodec.DecodeMultiLocalizedUnicode(nestedState, elementBytes, IccTypeSignatures.MultiLocalizedUnicode);
        if (nestedState.Result.Status != IccOperationStatus.Success || nestedState.Data.Kind != IccTagDataKind.LocalizedText)
        {
            return null;
        }

        cache.Add(key, nestedState.Data.LocalizedStrings);
        return nestedState.Data.LocalizedStrings;
    }

    internal static bool IsPositionValid(int payloadLength, int storageOffset, uint itemOffset, uint itemLength)
    {
        if (itemOffset > int.MaxValue || itemLength > int.MaxValue)
        {
            return false;
        }

        int offset = (int)itemOffset;
        int length = (int)itemLength;
        return offset >= storageOffset && (offset & 3) == 0 && offset <= payloadLength && length <= payloadLength - offset;
    }

    internal static bool IsDisplayPositionValid(int payloadLength, int storageOffset, uint itemOffset, uint itemLength)
    {
        if (!IsPositionValid(payloadLength, storageOffset, itemOffset, itemLength))
        {
            return false;
        }

        return itemLength == 0 || itemLength >= 16;
    }

    internal static int AddStorageItem(byte[] bytes, System.Collections.Generic.List<byte[]> storageItems, System.Collections.Generic.Dictionary<string, int> storageIndices)
    {
        string key = System.Convert.ToBase64String(bytes);
        if (storageIndices.ContainsKey(key))
        {
            return storageIndices[key];
        }

        int index = storageItems.Count;
        storageItems.Add(bytes);
        storageIndices.Add(key, index);
        return index;
    }

    internal static void WriteStoragePosition(BigEndianWriter writer, int storageIndex, int length, int[] storageOffsets)
    {
        if (storageIndex < 0)
        {
            writer.WriteUInt32(0);
            writer.WriteUInt32(0);
            return;
        }

        writer.WriteUInt32((uint)storageOffsets[storageIndex]);
        writer.WriteUInt32((uint)length);
    }

    internal static long AlignSizeToFour(long size)
    {
        long remainder = size & 3L;
        if (remainder == 0)
        {
            return size;
        }

        return size + (4L - remainder);
    }

    internal static bool IsRegisteredMetadataName(string name)
    {
        switch (name)
        {
            case "MDCV":
            case "CCV":
            case "CLL":
            case "CRWL":
            case "DeviceModel":
            case "DeviceType":
            case "HalftoneType":
            case "LineScreenFrequency":
            case "Look-and-feel":
            case "ManufacturerName":
            case "MediaBrightness":
            case "MediaColor":
            case "MediaFinish":
            case "MediaWeight":
            case "NumDeviceLevels":
            case "Resolution":
            case "Rotation":
                return true;
            default:
                return false;
        }
    }
}
