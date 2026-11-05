namespace icc;

internal class ProfileSequenceDescriptionBounds
{
    internal bool IsValid;
    internal int LogicalSize;
    internal int PaddedSize;
    internal string Message;

    internal ProfileSequenceDescriptionBounds()
    {
        IsValid = false;
        LogicalSize = 0;
        PaddedSize = 0;
        Message = string.Empty;
    }
}

internal class ProfileSequenceDecodedDescription
{
    internal bool IsValid;
    internal IccTextData Text;
    internal IccLocalizedString[] LocalizedStrings;
    internal string Message;

    internal ProfileSequenceDecodedDescription()
    {
        IsValid = false;
        Text = new IccTextData();
        LocalizedStrings = new IccLocalizedString[0];
        Message = string.Empty;
    }
}

internal class ProfileSequencePosition
{
    internal int Offset;
    internal int Size;

    internal ProfileSequencePosition()
    {
        Offset = 0;
        Size = 0;
    }
}

internal class ProfileSequenceWriteItem
{
    internal byte[] Bytes;
    internal int StorageIndex;

    internal ProfileSequenceWriteItem()
    {
        Bytes = new byte[0];
        StorageIndex = 0;
    }
}

internal static class ProfileSequenceTagCodec
{
    internal static void DecodeProfileSequenceDescription(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A profileSequenceDescType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The profileSequenceDescType reserved bytes shall be zero.");
            return;
        }

        if (state.ProfileVersion != IccVersionKind.Version2 && state.ProfileVersion != IccVersionKind.Version4)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "A profileSequenceDescType requires an ICC.1 version 2 or version 4 profile context.");
            return;
        }

        BigEndianReader countReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint declaredCount = countReader.ReadUInt32();
        int minimumRecordSize = state.ProfileVersion == IccVersionKind.Version2 ? 200 : 52;
        int maximumCount = (payload.Length - 12) / minimumRecordSize;
        if (declaredCount > (uint)maximumCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The profileSequenceDescType declares {declaredCount} records, but its payload can contain at most {maximumCount} minimum-sized records for this profile generation.");
            return;
        }

        int count = (int)declaredCount;
        IccProfileSequenceEntry[] entries = new IccProfileSequenceEntry[count];
        int offset = 12;
        int index = 0;
        while (index < count)
        {
            if (offset > payload.Length - 20)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile sequence record {index} is truncated before its 20-byte header.");
                return;
            }

            BigEndianReader recordReader = new BigEndianReader(payload.Slice(offset, 20), state.AbsolutePayloadOffset + offset);
            IccProfileSequenceEntry entry = new IccProfileSequenceEntry();
            entry.DeviceManufacturerSignature = recordReader.ReadUInt32();
            entry.DeviceModelSignature = recordReader.ReadUInt32();
            entry.DeviceAttributes = recordReader.ReadUInt64();
            entry.TechnologySignature = recordReader.ReadUInt32();
            offset = offset + 20;

            ProfileSequenceDescriptionBounds manufacturerBounds = ReadNestedDescriptionBounds(payload, offset, state.ProfileVersion);
            if (!manufacturerBounds.IsValid)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile sequence record {index} has an invalid manufacturer description: {manufacturerBounds.Message}");
                return;
            }

            ProfileSequenceDecodedDescription manufacturer = DecodeNestedDescription(state, payload, offset, manufacturerBounds.LogicalSize, state.ProfileVersion);
            if (!manufacturer.IsValid)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile sequence record {index} has an invalid manufacturer description: {manufacturer.Message}");
                return;
            }

            entry.ManufacturerDescription = manufacturer.Text;
            entry.ManufacturerLocalizedDescriptions = manufacturer.LocalizedStrings;
            offset = offset + manufacturerBounds.PaddedSize;

            ProfileSequenceDescriptionBounds modelBounds = ReadNestedDescriptionBounds(payload, offset, state.ProfileVersion);
            if (!modelBounds.IsValid)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile sequence record {index} has an invalid model description: {modelBounds.Message}");
                return;
            }

            ProfileSequenceDecodedDescription model = DecodeNestedDescription(state, payload, offset, modelBounds.LogicalSize, state.ProfileVersion);
            if (!model.IsValid)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile sequence record {index} has an invalid model description: {model.Message}");
                return;
            }

            entry.ModelDescription = model.Text;
            entry.ModelLocalizedDescriptions = model.LocalizedStrings;
            offset = offset + modelBounds.PaddedSize;
            entries[index] = entry;
            index = index + 1;
        }

        if (offset != payload.Length)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The profileSequenceDescType contains {payload.Length - offset} unexpected trailing bytes after its declared records.");
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.ProfileSequence);
        data.ProfileSequence = entries;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {count} complete profile sequence description records with generation-specific nested descriptions.");
    }

    internal static void DecodeProfileSequenceIdentifier(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A profileSequenceIdentifierType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The profileSequenceIdentifierType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader countReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint declaredCount = countReader.ReadUInt32();
        int maximumCount = (payload.Length - 12) / 8;
        if (declaredCount > (uint)maximumCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The profileSequenceIdentifierType declares {declaredCount} positions, but its table can contain at most {maximumCount}.");
            return;
        }

        int count = (int)declaredCount;
        int storageOffset = 12 + (count * 8);
        ProfileSequencePosition[] positions = new ProfileSequencePosition[count];
        int index = 0;
        while (index < count)
        {
            int positionOffset = 12 + (index * 8);
            BigEndianReader positionReader = new BigEndianReader(payload.Slice(positionOffset, 8), state.AbsolutePayloadOffset + positionOffset);
            uint declaredOffset = positionReader.ReadUInt32();
            uint declaredSize = positionReader.ReadUInt32();
            if (declaredOffset > int.MaxValue || declaredSize > int.MaxValue)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile identifier position {index} exceeds the supported managed offset or size range.");
                return;
            }

            int itemOffset = (int)declaredOffset;
            int itemSize = (int)declaredSize;
            if (itemOffset < storageOffset || (itemOffset & 3) != 0 || itemSize < 32 || itemOffset > payload.Length || itemSize > payload.Length - itemOffset)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile identifier position {index} does not reference an aligned, bounded structure of at least 32 bytes.");
                return;
            }

            ProfileSequencePosition position = new ProfileSequencePosition();
            position.Offset = itemOffset;
            position.Size = itemSize;
            positions[index] = position;
            index = index + 1;
        }

        string topologyError = ValidateIdentifierTopology(payload, positions, storageOffset);
        if (topologyError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, topologyError);
            return;
        }

        IccProfileSequenceIdentifier[] identifiers = new IccProfileSequenceIdentifier[count];
        System.Collections.Generic.Dictionary<ulong, IccProfileSequenceIdentifier> shared = new System.Collections.Generic.Dictionary<ulong, IccProfileSequenceIdentifier>();
        int sharedCount = 0;
        index = 0;
        while (index < count)
        {
            ProfileSequencePosition position = positions[index];
            ulong key = ((ulong)(uint)position.Offset << 32) | (uint)position.Size;
            if (shared.ContainsKey(key))
            {
                identifiers[index] = shared[key];
                sharedCount = sharedCount + 1;
                index = index + 1;
                continue;
            }

            int descriptionOffset = position.Offset + 16;
            int descriptionSize = position.Size - 16;
            ProfileSequenceDescriptionBounds bounds = ReadMultiLocalizedUnicodeBounds(payload, descriptionOffset);
            if (!bounds.IsValid || bounds.LogicalSize != descriptionSize)
            {
                string reason = bounds.IsValid ? "the multiLocalizedUnicodeType size does not exactly fill the identifier structure" : bounds.Message;
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile identifier position {index} has an invalid profile description: {reason}.");
                return;
            }

            ProfileSequenceDecodedDescription description = DecodeNestedDescription(state, payload, descriptionOffset, descriptionSize, IccVersionKind.Version4);
            if (!description.IsValid)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Profile identifier position {index} has an invalid profile description: {description.Message}");
                return;
            }

            IccProfileSequenceIdentifier identifier = new IccProfileSequenceIdentifier();
            identifier.ProfileId.Bytes = payload.Slice(position.Offset, 16).ToArray();
            identifier.ProfileId.IsPresent = ContainsNonZeroByte(identifier.ProfileId.Bytes);
            identifier.Descriptions = description.LocalizedStrings;
            shared.Add(key, identifier);
            identifiers[index] = identifier;
            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.ProfileSequence);
        data.ProfileSequenceIdentifiers = identifiers;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {count} complete profile identifier records with {sharedCount} shared position references.");
    }

    internal static byte[] SerializeProfileSequenceDescription(IccProfileSequenceEntry[] entries, IccVersionKind profileVersion)
    {
        return SerializeProfileSequenceDescriptionForMode(entries, profileVersion, IccWriteMode.Preservation);
    }

    internal static byte[] SerializeProfileSequenceDescriptionForMode(IccProfileSequenceEntry[] entries, IccVersionKind profileVersion, IccWriteMode mode)
    {
        if (entries == null)
        {
            throw new IccProfileWriteException("The profile sequence description collection cannot be null.");
        }

        if (profileVersion != IccVersionKind.Version2 && profileVersion != IccVersionKind.Version4)
        {
            throw new IccProfileWriteException("A profileSequenceDescType can be written only for ICC.1 version 2 or version 4 profiles.");
        }

        byte[][] manufacturerDescriptions = new byte[entries.Length][];
        byte[][] modelDescriptions = new byte[entries.Length][];
        long payloadSize = 12;
        int index = 0;
        while (index < entries.Length)
        {
            IccProfileSequenceEntry entry = entries[index];
            if (entry == null || entry.ManufacturerDescription == null || entry.ModelDescription == null || entry.ManufacturerLocalizedDescriptions == null || entry.ModelLocalizedDescriptions == null)
            {
                throw new IccProfileWriteException($"Profile sequence description record {index} and its description fields cannot be null.");
            }

            if (profileVersion == IccVersionKind.Version2)
            {
                manufacturerDescriptions[index] = LegacyTagCodec.SerializeTextDescription(entry.ManufacturerDescription);
                modelDescriptions[index] = LegacyTagCodec.SerializeTextDescription(entry.ModelDescription);
            }
            else
            {
                manufacturerDescriptions[index] = SerializeLocalizedDescriptionForMode(entry.ManufacturerDescription, entry.ManufacturerLocalizedDescriptions, mode);
                modelDescriptions[index] = SerializeLocalizedDescriptionForMode(entry.ModelDescription, entry.ModelLocalizedDescriptions, mode);
            }

            payloadSize = payloadSize + 20;
            if (profileVersion == IccVersionKind.Version2)
            {
                payloadSize = payloadSize + manufacturerDescriptions[index].Length;
                payloadSize = payloadSize + modelDescriptions[index].Length;
            }
            else
            {
                payloadSize = payloadSize + AlignLongSizeToFour(manufacturerDescriptions[index].Length);
                payloadSize = payloadSize + AlignLongSizeToFour(modelDescriptions[index].Length);
            }
            if (payloadSize > int.MaxValue)
            {
                throw new IccProfileWriteException("The profile sequence description payload exceeds the supported managed size.");
            }

            index = index + 1;
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ProfileSequenceDescription);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)entries.Length);
        index = 0;
        while (index < entries.Length)
        {
            IccProfileSequenceEntry entry = entries[index];
            writer.WriteUInt32(entry.DeviceManufacturerSignature);
            writer.WriteUInt32(entry.DeviceModelSignature);
            writer.WriteUInt64(entry.DeviceAttributes);
            writer.WriteUInt32(entry.TechnologySignature);
            writer.WriteExactBytes(manufacturerDescriptions[index]);
            if (profileVersion == IccVersionKind.Version4)
            {
                writer.AlignWritePosition(4);
            }

            writer.WriteExactBytes(modelDescriptions[index]);
            if (profileVersion == IccVersionKind.Version4)
            {
                writer.AlignWritePosition(4);
            }

            index = index + 1;
        }

        return payload;
    }

    internal static byte[] SerializeProfileSequenceIdentifier(IccProfileSequenceIdentifier[] identifiers)
    {
        return SerializeProfileSequenceIdentifierForMode(identifiers, IccWriteMode.Preservation);
    }

    internal static byte[] SerializeProfileSequenceIdentifierForMode(IccProfileSequenceIdentifier[] identifiers, IccWriteMode mode)
    {
        if (identifiers == null)
        {
            throw new IccProfileWriteException("The profile sequence identifier collection cannot be null.");
        }

        long tableSize = 12L + ((long)identifiers.Length * 8L);
        if (tableSize > int.MaxValue)
        {
            throw new IccProfileWriteException("The profile sequence identifier position table exceeds the supported managed size.");
        }

        ProfileSequenceWriteItem[] items = new ProfileSequenceWriteItem[identifiers.Length];
        System.Collections.Generic.Dictionary<string, int> storageIndices = new System.Collections.Generic.Dictionary<string, int>();
        System.Collections.Generic.List<byte[]> storageItems = new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < identifiers.Length)
        {
            IccProfileSequenceIdentifier identifier = identifiers[index];
            if (identifier == null || identifier.ProfileId == null || identifier.ProfileId.Bytes == null || identifier.Descriptions == null)
            {
                throw new IccProfileWriteException($"Profile sequence identifier record {index} and its fields cannot be null.");
            }

            if (identifier.ProfileId.Bytes.Length != 16)
            {
                throw new IccProfileWriteException($"Profile sequence identifier record {index} must contain exactly 16 Profile ID bytes.");
            }

            byte[] descriptionBytes = LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(identifier.Descriptions, mode);
            byte[] structureBytes = new byte[16 + descriptionBytes.Length];
            System.Array.Copy(identifier.ProfileId.Bytes, 0, structureBytes, 0, 16);
            System.Array.Copy(descriptionBytes, 0, structureBytes, 16, descriptionBytes.Length);
            string key = System.Convert.ToBase64String(structureBytes);
            int storageIndex;
            if (storageIndices.ContainsKey(key))
            {
                storageIndex = storageIndices[key];
            }
            else
            {
                storageIndex = storageItems.Count;
                storageIndices.Add(key, storageIndex);
                storageItems.Add(structureBytes);
            }

            ProfileSequenceWriteItem item = new ProfileSequenceWriteItem();
            item.Bytes = structureBytes;
            item.StorageIndex = storageIndex;
            items[index] = item;
            index = index + 1;
        }

        int[] storageOffsets = new int[storageItems.Count];
        long payloadSize = tableSize;
        index = 0;
        while (index < storageItems.Count)
        {
            payloadSize = AlignLongSizeToFour(payloadSize);
            storageOffsets[index] = (int)payloadSize;
            payloadSize = payloadSize + storageItems[index].Length;
            if (payloadSize > int.MaxValue)
            {
                throw new IccProfileWriteException("The profile sequence identifier storage exceeds the supported managed size.");
            }

            index = index + 1;
        }

        payloadSize = AlignLongSizeToFour(payloadSize);
        if (payloadSize > int.MaxValue)
        {
            throw new IccProfileWriteException("The aligned profile sequence identifier payload exceeds the supported managed size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ProfileSequenceIdentifier);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)identifiers.Length);
        index = 0;
        while (index < items.Length)
        {
            ProfileSequenceWriteItem item = items[index];
            writer.WriteUInt32((uint)storageOffsets[item.StorageIndex]);
            writer.WriteUInt32((uint)item.Bytes.Length);
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

    internal static ProfileSequenceDescriptionBounds ReadNestedDescriptionBounds(System.ReadOnlyMemory<byte> payload, int offset, IccVersionKind profileVersion)
    {
        if (profileVersion == IccVersionKind.Version2)
        {
            return ReadTextDescriptionBounds(payload, offset);
        }

        return ReadMultiLocalizedUnicodeBounds(payload, offset);
    }

    internal static ProfileSequenceDescriptionBounds ReadTextDescriptionBounds(System.ReadOnlyMemory<byte> payload, int offset)
    {
        ProfileSequenceDescriptionBounds bounds = new ProfileSequenceDescriptionBounds();
        if (offset < 0 || offset > payload.Length || payload.Length - offset < 20)
        {
            bounds.Message = "the nested textDescriptionType header is truncated";
            return bounds;
        }

        BigEndianReader signatureReader = new BigEndianReader(payload.Slice(offset, 12), offset);
        if (signatureReader.ReadUInt32() != IccTypeSignatures.TextDescription)
        {
            bounds.Message = "ICC version 2 records shall embed a textDescriptionType";
            return bounds;
        }

        if (!HasZeroReservedBytesAt(payload, offset))
        {
            bounds.Message = "the nested textDescriptionType reserved bytes shall be zero";
            return bounds;
        }

        signatureReader.SkipExactBytes(4);
        uint declaredAsciiCount = signatureReader.ReadUInt32();
        if (declaredAsciiCount > int.MaxValue)
        {
            bounds.Message = "the nested textDescriptionType ASCII count exceeds the managed range";
            return bounds;
        }

        long unicodeHeaderOffset = (long)offset + 12L + declaredAsciiCount;
        if (unicodeHeaderOffset > payload.Length - 8)
        {
            bounds.Message = "the nested textDescriptionType ASCII field exceeds the enclosing payload";
            return bounds;
        }

        BigEndianReader unicodeReader = new BigEndianReader(payload.Slice((int)unicodeHeaderOffset + 4, 4), unicodeHeaderOffset + 4);
        uint declaredUnicodeCount = unicodeReader.ReadUInt32();
        long logicalSize = 90L + declaredAsciiCount + ((long)declaredUnicodeCount * 2L);
        if (logicalSize > int.MaxValue || logicalSize > payload.Length - offset)
        {
            bounds.Message = "the nested textDescriptionType Unicode field exceeds the enclosing payload";
            return bounds;
        }

        int size = (int)logicalSize;
        bounds.IsValid = true;
        bounds.LogicalSize = size;
        bounds.PaddedSize = size;
        return bounds;
    }

    internal static ProfileSequenceDescriptionBounds ReadMultiLocalizedUnicodeBounds(System.ReadOnlyMemory<byte> payload, int offset)
    {
        ProfileSequenceDescriptionBounds bounds = new ProfileSequenceDescriptionBounds();
        if (offset < 0 || offset > payload.Length || payload.Length - offset < 16)
        {
            bounds.Message = "the nested multiLocalizedUnicodeType header is truncated";
            return bounds;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(offset, 16), offset);
        if (headerReader.ReadUInt32() != IccTypeSignatures.MultiLocalizedUnicode)
        {
            bounds.Message = "ICC version 4 records shall embed a multiLocalizedUnicodeType";
            return bounds;
        }

        if (!HasZeroReservedBytesAt(payload, offset))
        {
            bounds.Message = "the nested multiLocalizedUnicodeType reserved bytes shall be zero";
            return bounds;
        }

        headerReader.SkipExactBytes(4);
        uint declaredCount = headerReader.ReadUInt32();
        uint recordSize = headerReader.ReadUInt32();
        if (recordSize != 12)
        {
            bounds.Message = "the nested multiLocalizedUnicodeType record size shall be 12 bytes";
            return bounds;
        }

        long tableEnd = 16L + ((long)declaredCount * 12L);
        if (tableEnd > int.MaxValue || tableEnd > payload.Length - offset)
        {
            bounds.Message = "the nested multiLocalizedUnicodeType record table exceeds the enclosing payload";
            return bounds;
        }

        long logicalSize = tableEnd;
        int index = 0;
        while ((uint)index < declaredCount)
        {
            int recordOffset = offset + 16 + (index * 12);
            BigEndianReader recordReader = new BigEndianReader(payload.Slice(recordOffset, 12), recordOffset);
            recordReader.SkipExactBytes(4);
            uint stringLength = recordReader.ReadUInt32();
            uint stringOffset = recordReader.ReadUInt32();
            long stringEnd = (long)stringOffset + stringLength;
            if ((stringLength & 1) != 0 || (stringOffset & 1) != 0 || stringOffset < tableEnd || stringEnd > payload.Length - offset)
            {
                bounds.Message = $"nested localized string record {index} references bytes outside the aligned string-storage area";
                return bounds;
            }

            if (stringEnd > logicalSize)
            {
                logicalSize = stringEnd;
            }

            index = index + 1;
        }

        int size = (int)logicalSize;
        int paddedSize = AlignSizeToFour(size);
        if (paddedSize < size || paddedSize > payload.Length - offset || !HasZeroBytes(payload, offset + size, paddedSize - size))
        {
            bounds.Message = "the nested multiLocalizedUnicodeType is not followed by the required zero alignment padding";
            return bounds;
        }

        bounds.IsValid = true;
        bounds.LogicalSize = size;
        bounds.PaddedSize = paddedSize;
        return bounds;
    }

    internal static ProfileSequenceDecodedDescription DecodeNestedDescription(TagDecodeState state, System.ReadOnlyMemory<byte> payload, int offset, int size, IccVersionKind profileVersion)
    {
        ProfileSequenceDecodedDescription result = new ProfileSequenceDecodedDescription();
        System.ReadOnlyMemory<byte> nestedBytes = payload.Slice(offset, size);
        TagDecodeSource source = new TagDecodeSource(nestedBytes);
        long absoluteOffset = CheckedSize.AddOffsets(state.AbsolutePayloadOffset, offset, long.MaxValue);
        TagDecodeState nestedState = new TagDecodeState(source, 0, size, absoluteOffset, profileVersion, false);
        if (profileVersion == IccVersionKind.Version2)
        {
            LegacyTagCodec.DecodeTextDescription(nestedState, nestedBytes, IccTypeSignatures.TextDescription);
            if (nestedState.Result.Status != IccOperationStatus.Success || nestedState.Data.Kind != IccTagDataKind.Text || nestedState.Data.Text == null)
            {
                result.Message = nestedState.Result.Message;
                return result;
            }

            result.Text = nestedState.Data.Text;
        }
        else
        {
            LocalizedUnicodeTagCodec.DecodeMultiLocalizedUnicode(nestedState, nestedBytes, IccTypeSignatures.MultiLocalizedUnicode);
            if (nestedState.Result.Status != IccOperationStatus.Success || nestedState.Data.Kind != IccTagDataKind.LocalizedText)
            {
                result.Message = nestedState.Result.Message;
                return result;
            }

            result.LocalizedStrings = nestedState.Data.LocalizedStrings;
            result.Text.TypeSignature = IccTypeSignatures.MultiLocalizedUnicode;
            result.Text.Bytes = nestedBytes.ToArray();
            if (result.LocalizedStrings.Length > 0)
            {
                result.Text.Text = result.LocalizedStrings[0].Text;
            }
        }

        result.IsValid = true;
        return result;
    }

    internal static string ValidateIdentifierTopology(System.ReadOnlyMemory<byte> payload, ProfileSequencePosition[] positions, int storageOffset)
    {
        System.Collections.Generic.List<ProfileSequencePosition> unique = new System.Collections.Generic.List<ProfileSequencePosition>();
        int index = 0;
        while (index < positions.Length)
        {
            ProfileSequencePosition candidate = positions[index];
            bool isShared = false;
            int previousIndex = 0;
            while (previousIndex < index)
            {
                ProfileSequencePosition previous = positions[previousIndex];
                if (candidate.Offset == previous.Offset && candidate.Size == previous.Size)
                {
                    isShared = true;
                    break;
                }

                int candidateEnd = candidate.Offset + candidate.Size;
                int previousEnd = previous.Offset + previous.Size;
                if (candidate.Offset < previousEnd && previous.Offset < candidateEnd)
                {
                    return $"Profile identifier positions {previousIndex} and {index} overlap without being identical shared structures.";
                }

                previousIndex = previousIndex + 1;
            }

            if (!isShared)
            {
                int insertionIndex = unique.Count;
                while (insertionIndex > 0 && unique[insertionIndex - 1].Offset > candidate.Offset)
                {
                    insertionIndex = insertionIndex - 1;
                }

                unique.Insert(insertionIndex, candidate);
            }

            index = index + 1;
        }

        int expectedOffset = storageOffset;
        index = 0;
        while (index < unique.Count)
        {
            ProfileSequencePosition position = unique[index];
            if (position.Offset != expectedOffset)
            {
                return $"Profile identifier structure storage contains an unexpected gap before byte {position.Offset}.";
            }

            int end = position.Offset + position.Size;
            int alignedEnd = AlignSizeToFour(end);
            if (alignedEnd < end || alignedEnd > payload.Length || !HasZeroBytes(payload, end, alignedEnd - end))
            {
                return $"Profile identifier structure at byte {position.Offset} is not followed by valid zero alignment padding.";
            }

            expectedOffset = alignedEnd;
            index = index + 1;
        }

        if (unique.Count == 0)
        {
            if (payload.Length != storageOffset)
            {
                return "The empty profileSequenceIdentifierType contains unexpected trailing bytes.";
            }
        }
        else if (payload.Length != expectedOffset)
        {
            return $"The profileSequenceIdentifierType contains {payload.Length - expectedOffset} unexpected trailing bytes after its structures.";
        }

        return string.Empty;
    }

    internal static byte[] SerializeLocalizedDescription(IccTextData text, IccLocalizedString[] localizedStrings)
    {
        return SerializeLocalizedDescriptionForMode(text, localizedStrings, IccWriteMode.Preservation);
    }

    internal static byte[] SerializeLocalizedDescriptionForMode(IccTextData text, IccLocalizedString[] localizedStrings, IccWriteMode mode)
    {
        if (localizedStrings.Length > 0)
        {
            return LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(localizedStrings, mode);
        }

        if (text.Text == null || text.Bytes == null)
        {
            throw new IccProfileWriteException("A profile sequence localized description and its text fields cannot be null.");
        }

        if (text.Text.Length == 0)
        {
            return LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(new IccLocalizedString[0], mode);
        }

        IccLocalizedString localized = new IccLocalizedString();
        localized.Language = "en";
        localized.Country = "US";
        localized.Text = text.Text;
        return LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(new IccLocalizedString[] { localized }, mode);
    }

    internal static int AlignSizeToFour(int size)
    {
        int remainder = size & 3;
        if (remainder == 0)
        {
            return size;
        }

        if (size > int.MaxValue - 3)
        {
            return -1;
        }

        return size + (4 - remainder);
    }

    internal static long AlignLongSizeToFour(long size)
    {
        long remainder = size & 3L;
        if (remainder == 0)
        {
            return size;
        }

        return size + (4L - remainder);
    }

    internal static bool HasZeroReservedBytesAt(System.ReadOnlyMemory<byte> payload, int offset)
    {
        return HasZeroBytes(payload, offset + 4, 4);
    }

    internal static bool HasZeroBytes(System.ReadOnlyMemory<byte> payload, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > payload.Length || count > payload.Length - offset)
        {
            return false;
        }

        System.ReadOnlySpan<byte> bytes = payload.Span;
        int index = 0;
        while (index < count)
        {
            if (bytes[offset + index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsNonZeroByte(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
