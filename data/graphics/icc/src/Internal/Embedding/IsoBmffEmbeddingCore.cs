namespace icc;

internal static class IsoBmffEmbeddingCore
{
    internal const uint FileType = 0x66747970;
    internal const uint Meta = 0x6d657461;
    internal const uint ItemProperties = 0x69707270;
    internal const uint ItemPropertyContainer = 0x6970636f;
    internal const uint ItemLocation = 0x696c6f63;
    internal const uint ColourInformation = 0x636f6c72;
    internal const uint RestrictedIcc = 0x72494343;
    internal const uint IccProfile = 0x70726f66;
    internal const uint Movie = 0x6d6f6f76;
    internal const uint Track = 0x7472616b;
    internal const uint Media = 0x6d646961;
    internal const uint MediaInformation = 0x6d696e66;
    internal const uint SampleTable = 0x7374626c;
    internal const uint SampleDescription = 0x73747364;
    internal const uint ChunkOffset = 0x7374636f;
    internal const uint ChunkLargeOffset = 0x636f3634;
    internal const uint UserData = 0x75647461;

    internal static IsoBmffProfileData Analyze(
        System.ReadOnlyMemory<byte> carrierBytes, IccResourceLimits limits)
    {
        IsoBmffProfileData result = new IsoBmffProfileData();
        if (limits.MaximumNestingDepth < 0)
        {
            return Fail(result, IccOperationStatus.ResourceLimitExceeded,
                "The configured ISO BMFF nesting-depth limit is invalid.",
                0, 0);
        }
        IsoBmffParseState state = new IsoBmffParseState(carrierBytes,
            limits, result);
        IsoBmffBoxRegion? fileType = ReadBox(state, 0,
            carrierBytes.Length);
        if (fileType == null)
        {
            return result;
        }
        if (fileType.Type != FileType || fileType.DataLength < 8
            || ((fileType.DataLength - 8) & 3) != 0)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The ISO BMFF carrier must begin with a valid File Type box.",
                fileType.Start, fileType.End - fileType.Start);
        }
        result.Format = ClassifyBrands(carrierBytes.Span,
            fileType.DataOffset, fileType.End);
        if (result.Format != IccEmbeddedFormat.Avif
            && result.Format != IccEmbeddedFormat.Heif)
        {
            return Fail(result, IccOperationStatus.UnsupportedFeature,
                "The ISO BMFF File Type box does not declare AVIF or HEIF compatibility.",
                fileType.DataOffset, fileType.DataLength);
        }
        if (!ParseBoxes(state, fileType.End, carrierBytes.Length, 0, 0,
            new IsoBmffBoxRegion[0]))
        {
            return result;
        }
        result.Profiles = state.Profiles.ToArray();
        result.OffsetReferences = state.Offsets.ToArray();
        result.IsValid = true;
        result.Status = IccOperationStatus.Success;
        result.Message = result.Profiles.Length == 0
            ? $"The {result.Format} box structure contains no prof or rICC profile."
            : $"The {result.Format} carrier contains {result.Profiles.Length} prof/rICC profile(s).";
        return result;
    }

    internal static bool ParseBoxes(IsoBmffParseState state, int start,
        int end, int depth, uint containerType,
        IsoBmffBoxRegion[] containers)
    {
        int offset = start;
        while (offset < end)
        {
            IsoBmffBoxRegion? box = ReadBox(state, offset, end);
            if (box == null)
            {
                return false;
            }
            bool profileContainer = containerType == ItemPropertyContainer
                || IsVisualSampleEntry(containerType);
            if (box.Type == ColourInformation && profileContainer)
            {
                if (!ParseColourInformation(state, box, containers))
                {
                    return false;
                }
            }
            if (box.Type == ItemLocation && containerType == Meta)
            {
                if (!ParseItemLocation(state, box))
                {
                    return false;
                }
            }
            if (box.Type == ChunkOffset || box.Type == ChunkLargeOffset)
            {
                if (!ParseChunkOffsets(state, box))
                {
                    return false;
                }
            }

            int childStart = box.DataOffset;
            bool recurse = IsPureContainer(box.Type);
            if (box.Type == Meta)
            {
                if (box.DataLength < 4)
                {
                    FailState(state, IccOperationStatus.InvalidData,
                        "An ISO BMFF Meta box is shorter than its FullBox header.",
                        box.DataOffset, box.DataLength);
                    return false;
                }
                childStart = box.DataOffset + 4;
                recurse = true;
            }
            if (box.Type == SampleDescription)
            {
                if (!RequireNesting(state, depth, box))
                {
                    return false;
                }
                IsoBmffBoxRegion[] nested = AppendContainer(containers,
                    box);
                if (!ParseSampleDescriptions(state, box, depth + 1,
                    nested))
                {
                    return false;
                }
            }
            else if (recurse)
            {
                if (!RequireNesting(state, depth, box))
                {
                    return false;
                }
                IsoBmffBoxRegion[] nested = AppendContainer(containers,
                    box);
                if (!ParseBoxes(state, childStart, box.End, depth + 1,
                    box.Type, nested))
                {
                    return false;
                }
            }
            offset = box.End;
        }
        return true;
    }

    internal static bool ParseSampleDescriptions(IsoBmffParseState state,
        IsoBmffBoxRegion box, int depth, IsoBmffBoxRegion[] containers)
    {
        if (box.DataLength < 8)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "An ISO BMFF Sample Description box is shorter than its entry header.",
                box.DataOffset, box.DataLength);
            return false;
        }
        uint encodedCount = ReadUInt32(state.Carrier.Span,
            box.DataOffset + 4);
        if (encodedCount > int.MaxValue)
        {
            FailState(state, IccOperationStatus.ResourceLimitExceeded,
                "An ISO BMFF Sample Description entry count exceeds the managed limit.",
                box.DataOffset + 4, 4);
            return false;
        }
        int count = (int)encodedCount;
        int offset = box.DataOffset + 8;
        int index = 0;
        while (index < count)
        {
            IsoBmffBoxRegion? entry = ReadBox(state, offset, box.End);
            if (entry == null)
            {
                return false;
            }
            if (IsVisualSampleEntry(entry.Type))
            {
                if (entry.DataLength < 78)
                {
                    FailState(state, IccOperationStatus.InvalidData,
                        "An ISO BMFF visual sample entry is shorter than its fixed fields.",
                        entry.DataOffset, entry.DataLength);
                    return false;
                }
                if (!RequireNesting(state, depth, entry))
                {
                    return false;
                }
                IsoBmffBoxRegion[] nested = AppendContainer(containers,
                    entry);
                if (!ParseBoxes(state, entry.DataOffset + 78, entry.End,
                    depth + 1, entry.Type, nested))
                {
                    return false;
                }
            }
            offset = entry.End;
            index = index + 1;
        }
        if (offset != box.End)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "The ISO BMFF Sample Description entry count does not consume its payload.",
                offset, box.End - offset);
            return false;
        }
        return true;
    }

    internal static bool ParseColourInformation(IsoBmffParseState state,
        IsoBmffBoxRegion box, IsoBmffBoxRegion[] containers)
    {
        if (box.DataLength < 4)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "An ISO BMFF Colour Information box is shorter than its colour type.",
                box.DataOffset, box.DataLength);
            return false;
        }
        uint colourType = ReadUInt32(state.Carrier.Span, box.DataOffset);
        if (colourType != IccProfile && colourType != RestrictedIcc)
        {
            return true;
        }
        int profileOffset = box.DataOffset + 4;
        int profileLength = box.DataLength - 4;
        if (profileLength > state.Limits.MaximumProfileBytes)
        {
            FailState(state, IccOperationStatus.ResourceLimitExceeded,
                "An ISO BMFF Colour Information ICC payload exceeds the configured profile-size limit.",
                profileOffset, profileLength);
            return false;
        }
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ResourceLimits = state.Limits;
        IccOperationResult probe = new ProfileReaderCore().ProbeMemory(
            state.Carrier.Slice(profileOffset, profileLength), options);
        if (probe.Status != IccOperationStatus.Success)
        {
            FailState(state, probe.Status,
                $"The ISO BMFF {FourCcText(colourType)} ICC payload is invalid: {probe.Message}",
                profileOffset, profileLength);
            return false;
        }
        IsoBmffProfileEntry entry = new IsoBmffProfileEntry();
        entry.Box = box;
        entry.Containers = containers;
        entry.ColourType = colourType;
        entry.ProfileOffset = profileOffset;
        entry.ProfileLength = profileLength;
        state.Profiles.Add(entry);
        return true;
    }

    internal static bool ParseItemLocation(IsoBmffParseState state,
        IsoBmffBoxRegion box)
    {
        if (box.DataLength < 8)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "An ISO BMFF Item Location box is shorter than its header.",
                box.DataOffset, box.DataLength);
            return false;
        }
        int version = state.Carrier.Span[box.DataOffset];
        if (version > 2)
        {
            FailState(state, IccOperationStatus.UnsupportedFeature,
                "The ISO BMFF Item Location box version is not supported.",
                box.DataOffset, 1);
            return false;
        }
        int offsetSize = state.Carrier.Span[box.DataOffset + 4] >> 4;
        int lengthSize = state.Carrier.Span[box.DataOffset + 4] & 15;
        int baseOffsetSize = state.Carrier.Span[box.DataOffset + 5] >> 4;
        int indexSize = version == 0 ? 0
            : state.Carrier.Span[box.DataOffset + 5] & 15;
        if ((version == 0 && (state.Carrier.Span[box.DataOffset + 5] & 15) != 0)
            || offsetSize > 8 || lengthSize > 8
            || baseOffsetSize > 8 || indexSize > 8)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "The ISO BMFF Item Location field sizes are invalid.",
                box.DataOffset + 4, 2);
            return false;
        }
        IsoBmffParseCursor cursor = new IsoBmffParseCursor(
            box.DataOffset + 6, box.End);
        int countWidth = version < 2 ? 2 : 4;
        long itemCountValue = ReadSizedField(state, cursor, countWidth,
            "item count");
        if (state.Failed)
        {
            return false;
        }
        if (itemCountValue > int.MaxValue)
        {
            FailState(state, IccOperationStatus.ResourceLimitExceeded,
                "The ISO BMFF Item Location item count exceeds the managed limit.",
                cursor.Offset - countWidth, countWidth);
            return false;
        }
        int itemCount = (int)itemCountValue;
        int itemIndex = 0;
        while (itemIndex < itemCount)
        {
            int itemIdWidth = version < 2 ? 2 : 4;
            ReadSizedField(state, cursor, itemIdWidth, "item identifier");
            int constructionMethod = 0;
            if (version == 1 || version == 2)
            {
                long encodedMethod = ReadSizedField(state, cursor, 2,
                    "construction method");
                constructionMethod = (int)(encodedMethod & 15);
            }
            long dataReference = ReadSizedField(state, cursor, 2,
                "data reference index");
            int basePosition = cursor.Offset;
            long baseOffset = ReadSizedField(state, cursor, baseOffsetSize,
                "base offset");
            long extentCountValue = ReadSizedField(state, cursor, 2,
                "extent count");
            if (state.Failed)
            {
                return false;
            }
            int extentCount = (int)extentCountValue;
            int extentIndex = 0;
            while (extentIndex < extentCount)
            {
                if ((version == 1 || version == 2) && indexSize != 0)
                {
                    ReadSizedField(state, cursor, indexSize,
                        "extent index");
                }
                int extentPosition = cursor.Offset;
                long extentOffset = ReadSizedField(state, cursor,
                    offsetSize, "extent offset");
                ReadSizedField(state, cursor, lengthSize,
                    "extent length");
                if (state.Failed)
                {
                    return false;
                }
                if (constructionMethod == 0 && dataReference == 0)
                {
                    if (baseOffset > long.MaxValue - extentOffset)
                    {
                        FailState(state,
                            IccOperationStatus.ResourceLimitExceeded,
                            "An ISO BMFF item extent offset overflows the supported range.",
                            extentPosition, offsetSize);
                        return false;
                    }
                    long target = baseOffset + extentOffset;
                    if (offsetSize != 0)
                    {
                        AddOffsetReference(state, extentPosition,
                            offsetSize, extentOffset, target);
                    }
                    else if (baseOffsetSize != 0)
                    {
                        AddUniqueOffsetReference(state, basePosition,
                            baseOffsetSize, baseOffset, target);
                    }
                }
                extentIndex = extentIndex + 1;
            }
            itemIndex = itemIndex + 1;
        }
        if (cursor.Offset != cursor.End)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "The ISO BMFF Item Location fields do not consume the box payload.",
                cursor.Offset, cursor.End - cursor.Offset);
            return false;
        }
        return true;
    }

    internal static bool ParseChunkOffsets(IsoBmffParseState state,
        IsoBmffBoxRegion box)
    {
        if (box.DataLength < 8)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "An ISO BMFF chunk-offset box is shorter than its header.",
                box.DataOffset, box.DataLength);
            return false;
        }
        int width = box.Type == ChunkOffset ? 4 : 8;
        uint encodedCount = ReadUInt32(state.Carrier.Span,
            box.DataOffset + 4);
        long requiredLength = 8 + (long)encodedCount * width;
        if (requiredLength != box.DataLength)
        {
            IccOperationStatus status = requiredLength > int.MaxValue
                ? IccOperationStatus.ResourceLimitExceeded
                : IccOperationStatus.InvalidData;
            FailState(state, status,
                "The ISO BMFF chunk-offset count does not match its payload length.",
                box.DataOffset + 4, box.DataLength - 4);
            return false;
        }
        int offset = box.DataOffset + 8;
        int index = 0;
        while (index < (int)encodedCount)
        {
            IsoBmffParseCursor cursor = new IsoBmffParseCursor(offset,
                offset + width);
            long value = ReadSizedField(state, cursor, width,
                "chunk offset");
            if (state.Failed)
            {
                return false;
            }
            AddOffsetReference(state, offset, width, value, value);
            offset = offset + width;
            index = index + 1;
        }
        return true;
    }

    internal static byte[] ReplaceProfile(
        System.ReadOnlySpan<byte> carrierBytes, int profileIndex,
        byte[] profileBytes, IccEmbeddedFormat format,
        IccResourceLimits limits)
    {
        if (profileBytes == null || profileBytes.Length
            > limits.MaximumProfileBytes)
        {
            throw new IccEmbeddingException(
                "The ICC profile exceeds the configured ISO BMFF embedding limit.");
        }
        IsoBmffProfileData data = Analyze(carrierBytes.ToArray(), limits);
        if (!data.IsValid)
        {
            throw new IccEmbeddingException(data.Message);
        }
        if (data.Format != format)
        {
            throw new IccEmbeddingException(
                $"The detected {data.Format} carrier does not match the requested {format} format.");
        }
        if (profileIndex < 0 || profileIndex >= data.Profiles.Length)
        {
            throw new IccEmbeddingException(
                $"The {format} carrier does not expose an ICC profile at index {profileIndex}.");
        }
        IsoBmffProfileEntry entry = data.Profiles[profileIndex];
        byte[] replacement = EncodeColourBox(carrierBytes, entry,
            profileBytes);
        long outputLength = (long)carrierBytes.Length
            - (entry.Box.End - entry.Box.Start) + replacement.Length;
        if (outputLength > limits.MaximumCarrierBytes
            || outputLength > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The updated ISO BMFF carrier exceeds the configured or managed size limit.");
        }
        byte[] output = new byte[(int)outputLength];
        carrierBytes.Slice(0, entry.Box.Start).CopyTo(output);
        System.Array.Copy(replacement, 0, output, entry.Box.Start,
            replacement.Length);
        carrierBytes.Slice(entry.Box.End).CopyTo(new System.Span<byte>(output,
            entry.Box.Start + replacement.Length,
            carrierBytes.Length - entry.Box.End));
        int difference = replacement.Length
            - (entry.Box.End - entry.Box.Start);
        UpdateContainerLengths(output, entry, difference);
        UpdateReferencedOffsets(output, data.OffsetReferences, entry,
            difference);
        return output;
    }

    internal static void UpdateContainerLengths(byte[] output,
        IsoBmffProfileEntry entry, int difference)
    {
        int index = 0;
        while (index < entry.Containers.Length)
        {
            IsoBmffBoxRegion container = entry.Containers[index];
            if (!container.ExtendsToEnd)
            {
                long changedLength = (long)(container.End - container.Start)
                    + difference;
                if (container.HeaderLength == 8)
                {
                    if (changedLength > uint.MaxValue)
                    {
                        throw new IccEmbeddingException(
                            "A resized ISO BMFF box exceeds its 32-bit length field.");
                    }
                    WriteUInt32(output, container.Start,
                        (uint)changedLength);
                }
                else
                {
                    WriteUInt64(output, container.Start + 8,
                        changedLength);
                }
            }
            index = index + 1;
        }
    }

    internal static void UpdateReferencedOffsets(byte[] output,
        IsoBmffOffsetReference[] references, IsoBmffProfileEntry entry,
        int difference)
    {
        if (difference == 0)
        {
            return;
        }
        int index = 0;
        while (index < references.Length)
        {
            IsoBmffOffsetReference reference = references[index];
            if (reference.TargetOffset >= entry.Box.End)
            {
                long changedValue = reference.Value + difference;
                if (changedValue < 0
                    || !FitsSizedValue(changedValue, reference.Width))
                {
                    throw new IccEmbeddingException(
                        "A shifted ISO BMFF media offset exceeds its encoded field.");
                }
                int position = reference.Position;
                if (position >= entry.Box.End)
                {
                    position = position + difference;
                }
                WriteSizedValue(output, position, reference.Width,
                    changedValue);
            }
            index = index + 1;
        }
    }

    internal static byte[] EncodeColourBox(
        System.ReadOnlySpan<byte> carrierBytes,
        IsoBmffProfileEntry entry, byte[] profileBytes)
    {
        long encodedLength = (long)entry.Box.HeaderLength + 4
            + profileBytes.Length;
        if (encodedLength > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The encoded ISO BMFF Colour Information box exceeds the managed size limit.");
        }
        if (entry.Box.HeaderLength == 8 && !entry.Box.ExtendsToEnd
            && encodedLength > uint.MaxValue)
        {
            throw new IccEmbeddingException(
                "The encoded ISO BMFF Colour Information box exceeds its 32-bit length field.");
        }
        byte[] result = new byte[(int)encodedLength];
        if (entry.Box.ExtendsToEnd)
        {
            WriteUInt32(result, 0, 0);
        }
        else if (entry.Box.HeaderLength == 16)
        {
            WriteUInt32(result, 0, 1);
            WriteUInt64(result, 8, encodedLength);
        }
        else
        {
            WriteUInt32(result, 0, (uint)encodedLength);
        }
        WriteUInt32(result, 4, ColourInformation);
        WriteUInt32(result, entry.Box.HeaderLength, entry.ColourType);
        System.Array.Copy(profileBytes, 0, result,
            entry.Box.HeaderLength + 4, profileBytes.Length);
        return result;
    }

    internal static IsoBmffBoxRegion? ReadBox(IsoBmffParseState state,
        int offset, int end)
    {
        if (offset > end - 8)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "The ISO BMFF carrier contains a truncated box header.",
                offset, end - offset);
            return null;
        }
        uint encodedLength = ReadUInt32(state.Carrier.Span, offset);
        int headerLength = 8;
        long boxLength = encodedLength;
        bool extendsToEnd = encodedLength == 0;
        if (encodedLength == 1)
        {
            if (offset > end - 16)
            {
                FailState(state, IccOperationStatus.InvalidData,
                    "The ISO BMFF carrier contains a truncated extended box header.",
                    offset, end - offset);
                return null;
            }
            headerLength = 16;
            boxLength = ReadBoxUInt64(state.Carrier.Span, offset + 8);
        }
        else if (extendsToEnd)
        {
            boxLength = end - offset;
        }
        if (boxLength < headerLength)
        {
            FailState(state, IccOperationStatus.InvalidData,
                "An ISO BMFF box length is shorter than its header.",
                offset, headerLength);
            return null;
        }
        if (boxLength > end - offset)
        {
            IccOperationStatus status = boxLength > int.MaxValue
                ? IccOperationStatus.ResourceLimitExceeded
                : IccOperationStatus.InvalidData;
            FailState(state, status,
                "An ISO BMFF box extends beyond its containing box.",
                offset, end - offset);
            return null;
        }
        IsoBmffBoxRegion box = new IsoBmffBoxRegion();
        box.Start = offset;
        box.End = offset + (int)boxLength;
        box.HeaderLength = headerLength;
        box.DataOffset = offset + headerLength;
        box.DataLength = (int)boxLength - headerLength;
        box.Type = ReadUInt32(state.Carrier.Span, offset + 4);
        box.ExtendsToEnd = extendsToEnd;
        return box;
    }

    internal static IccEmbeddedFormat DetectFormat(
        System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16 || ReadUInt32(bytes, 4) != FileType)
        {
            return IccEmbeddedFormat.Unknown;
        }
        uint encodedLength = ReadUInt32(bytes, 0);
        int dataOffset = 8;
        long declaredLength = encodedLength;
        if (encodedLength == 1)
        {
            if (bytes.Length < 24)
            {
                return IccEmbeddedFormat.Unknown;
            }
            dataOffset = 16;
            declaredLength = ReadBoxUInt64(bytes, 8);
        }
        else if (encodedLength == 0)
        {
            declaredLength = bytes.Length;
        }
        if (declaredLength < dataOffset + 8)
        {
            return IccEmbeddedFormat.Unknown;
        }
        int availableEnd = declaredLength > bytes.Length ? bytes.Length
            : (int)declaredLength;
        IccEmbeddedFormat format = ClassifyBrands(bytes, dataOffset,
            availableEnd);
        return format == IccEmbeddedFormat.IsoBmff
            ? IccEmbeddedFormat.IsoBmff : format;
    }

    internal static IccEmbeddedFormat ClassifyBrands(
        System.ReadOnlySpan<byte> bytes, int dataOffset, int end)
    {
        bool avif = false;
        bool heif = false;
        int offset = dataOffset;
        while (offset <= end - 4)
        {
            uint brand = ReadUInt32(bytes, offset);
            if (brand == 0x61766966 || brand == 0x61766973)
            {
                avif = true;
            }
            if (brand == 0x68656963 || brand == 0x68656978
                || brand == 0x68657663 || brand == 0x68657678
                || brand == 0x6865696d || brand == 0x68656973
                || brand == 0x6d696631 || brand == 0x6d736631)
            {
                heif = true;
            }
            offset = offset == dataOffset ? dataOffset + 8 : offset + 4;
        }
        if (avif)
        {
            return IccEmbeddedFormat.Avif;
        }
        if (heif)
        {
            return IccEmbeddedFormat.Heif;
        }
        return IccEmbeddedFormat.IsoBmff;
    }

    internal static bool IsPureContainer(uint type)
    {
        return type == ItemProperties || type == ItemPropertyContainer
            || type == Movie || type == Track || type == Media
            || type == MediaInformation || type == SampleTable
            || type == UserData;
    }

    internal static bool IsVisualSampleEntry(uint type)
    {
        return type == 0x61763031 || type == 0x61763032
            || type == 0x68766331 || type == 0x68657631
            || type == 0x61766331 || type == 0x61766333
            || type == 0x6a706567 || type == 0x6d6a7032
            || type == 0x76766331 || type == 0x76766931
            || type == 0x65766331 || type == 0x65766d31;
    }

    internal static bool RequireNesting(IsoBmffParseState state, int depth,
        IsoBmffBoxRegion box)
    {
        if (depth >= state.Limits.MaximumNestingDepth)
        {
            FailState(state, IccOperationStatus.ResourceLimitExceeded,
                $"The ISO BMFF box hierarchy exceeds the configured nesting depth of {state.Limits.MaximumNestingDepth}.",
                box.Start, box.End - box.Start);
            return false;
        }
        return true;
    }

    internal static long ReadSizedField(IsoBmffParseState state,
        IsoBmffParseCursor cursor, int width, string description)
    {
        if (width < 0 || width > 8 || cursor.Offset > cursor.End - width)
        {
            FailState(state, IccOperationStatus.InvalidData,
                $"The ISO BMFF {description} field is truncated or has an invalid width.",
                cursor.Offset, cursor.End - cursor.Offset);
            return 0;
        }
        if (width == 8 && (state.Carrier.Span[cursor.Offset] & 0x80) != 0)
        {
            FailState(state, IccOperationStatus.ResourceLimitExceeded,
                $"The ISO BMFF {description} exceeds the supported signed range.",
                cursor.Offset, width);
            return 0;
        }
        long value = 0;
        int index = 0;
        while (index < width)
        {
            value = (value << 8) | state.Carrier.Span[cursor.Offset + index];
            index = index + 1;
        }
        cursor.Offset = cursor.Offset + width;
        return value;
    }

    internal static void AddOffsetReference(IsoBmffParseState state,
        int position, int width, long value, long target)
    {
        IsoBmffOffsetReference reference = new IsoBmffOffsetReference();
        reference.Position = position;
        reference.Width = width;
        reference.Value = value;
        reference.TargetOffset = target;
        state.Offsets.Add(reference);
    }

    internal static void AddUniqueOffsetReference(IsoBmffParseState state,
        int position, int width, long value, long target)
    {
        int index = 0;
        while (index < state.Offsets.Count)
        {
            if (state.Offsets[index].Position == position)
            {
                return;
            }
            index = index + 1;
        }
        AddOffsetReference(state, position, width, value, target);
    }

    internal static IsoBmffBoxRegion[] AppendContainer(
        IsoBmffBoxRegion[] containers, IsoBmffBoxRegion box)
    {
        IsoBmffBoxRegion[] result =
            new IsoBmffBoxRegion[containers.Length + 1];
        System.Array.Copy(containers, result, containers.Length);
        result[containers.Length] = box;
        return result;
    }

    internal static IsoBmffProfileData Fail(IsoBmffProfileData result,
        IccOperationStatus status, string message, int offset, int length)
    {
        result.IsValid = false;
        result.Profiles = new IsoBmffProfileEntry[0];
        result.OffsetReferences = new IsoBmffOffsetReference[0];
        result.Status = status;
        result.Message = message;
        result.ErrorOffset = offset;
        result.ErrorLength = length;
        return result;
    }

    internal static void FailState(IsoBmffParseState state,
        IccOperationStatus status, string message, int offset, int length)
    {
        state.Failed = true;
        Fail(state.Result, status, message, offset, length);
    }

    internal static bool FitsSizedValue(long value, int width)
    {
        if (value < 0 || width < 1 || width > 8)
        {
            return false;
        }
        if (width == 8)
        {
            return true;
        }
        long maximum = (1L << (width * 8)) - 1;
        return value <= maximum;
    }

    internal static void WriteSizedValue(byte[] bytes, int offset,
        int width, long value)
    {
        int index = width - 1;
        while (index >= 0)
        {
            bytes[offset + index] = (byte)(value & 0xff);
            value = value >> 8;
            index = index - 1;
        }
    }

    internal static string FourCcText(uint value)
    {
        char[] characters = new char[4];
        characters[0] = (char)((value >> 24) & 0xff);
        characters[1] = (char)((value >> 16) & 0xff);
        characters[2] = (char)((value >> 8) & 0xff);
        characters[3] = (char)(value & 0xff);
        return new string(characters);
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static long ReadBoxUInt64(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        uint high = ReadUInt32(bytes, offset);
        if (high != 0)
        {
            return (long)int.MaxValue + 1;
        }
        return ReadUInt32(bytes, offset + 4);
    }

    internal static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xff);
        bytes[offset + 1] = (byte)((value >> 16) & 0xff);
        bytes[offset + 2] = (byte)((value >> 8) & 0xff);
        bytes[offset + 3] = (byte)(value & 0xff);
    }

    internal static void WriteUInt64(byte[] bytes, int offset, long value)
    {
        WriteUInt32(bytes, offset, (uint)((value >> 32) & 0xffffffff));
        WriteUInt32(bytes, offset + 4, (uint)(value & 0xffffffff));
    }
}
