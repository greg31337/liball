namespace icc;

internal class ProfileReaderCore
{
    internal IccProfile ReadBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        byte[] ownedBytes = profileBytes.ToArray();
        return ProfileCodec.ReadEnvelope(ownedBytes, 0, 0, options);
    }

    internal IccProfile ReadMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        return ProfileCodec.ReadEnvelope(profileMemory, 0, 0, options);
    }

    internal IccProfile ReadStream(System.IO.Stream profileStream, IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        if (profileStream == null)
        {
            throw new IccProfileValidationException("A profile stream cannot be null.");
        }

        if (!profileStream.CanRead)
        {
            throw new IccProfileFormatException("The profile stream is not readable.");
        }

        StreamBuffer buffer = new StreamBuffer(profileStream, 4096);
        long profileOffset = buffer.GetAbsoluteOffset();
        byte[] sizeBytes = buffer.ReadExactBytes(4);
        uint encodedSize = ReadUInt32(sizeBytes, 0);
        int declaredSize = ProfileCodec.ConvertDeclaredProfileSize(encodedSize, options.ResourceLimits.MaximumProfileBytes, profileOffset);
        if (declaredSize < IccConstants.MinimumProfileSize)
        {
            throw new IccProfileFormatException($"The profile at byte offset {profileOffset} declares {declaredSize} bytes; the minimum complete ICC profile size is {IccConstants.MinimumProfileSize} bytes.");
        }

        CheckedSize.MultiplyCount(declaredSize, 1, System.Array.MaxLength);
        byte[] profileBytes = new byte[declaredSize];
        int index = 0;
        while (index < sizeBytes.Length)
        {
            profileBytes[index] = sizeBytes[index];
            index = index + 1;
        }

        buffer.ReadExactInto(profileBytes, 4, declaredSize - 4);
        if (options.RequireExactProfileSize)
        {
            EnsureStreamEnded(profileStream, profileOffset, declaredSize);
        }

        return ProfileCodec.ReadEnvelope(profileBytes, 0, profileOffset, options);
    }

    internal IccOperationResult ProbeBytes(System.ReadOnlySpan<byte> profileBytes, IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        return ProbePrefix(profileBytes, 0, profileBytes.Length, true, options);
    }

    internal IccOperationResult ProbeMemory(System.ReadOnlyMemory<byte> profileMemory, IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        return ProbePrefix(profileMemory.Span, 0, profileMemory.Length, true, options);
    }

    internal IccOperationResult ProbeStream(System.IO.Stream profileStream, IccReadOptions options)
    {
        ProfileCodec.ValidateEnvelopeOptions(options);
        TagTableCodec.ValidateTableLimits(options.ResourceLimits);
        if (profileStream == null)
        {
            return CreateResult(IccOperationStatus.Failed, "A profile stream cannot be null.", 0, 0);
        }

        if (!profileStream.CanRead)
        {
            return CreateResult(IccOperationStatus.Failed, "The profile stream is not readable.", 0, 0);
        }

        bool canSeek = profileStream.CanSeek;
        long profileOffset = canSeek ? profileStream.Position : 0;
        long availableLength = -1;
        if (canSeek)
        {
            availableLength = profileStream.Length - profileOffset;
            if (availableLength < 0)
            {
                return CreateResult(IccOperationStatus.Failed, "The profile stream position is beyond the end of the stream.", profileOffset, 0);
            }
        }

        byte[] prefix = new byte[IccConstants.MinimumProfileSize];
        int prefixLength = ReadAtMost(profileStream, prefix, 0, IccConstants.MinimumProfileSize);
        if (prefixLength == IccConstants.MinimumProfileSize)
        {
            int requiredPrefixSize = GetRequiredProbePrefixSize(prefix, options);
            if (requiredPrefixSize > prefix.Length)
            {
                byte[] tablePrefix = new byte[requiredPrefixSize];
                System.Array.Copy(prefix, tablePrefix, prefix.Length);
                int remainingRead = ReadAtMost(profileStream, tablePrefix, prefix.Length, requiredPrefixSize - prefix.Length);
                prefix = tablePrefix;
                prefixLength = prefixLength + remainingRead;
            }
        }

        IccOperationResult result = ProbePrefix(new System.ReadOnlySpan<byte>(prefix, 0, prefixLength), profileOffset, availableLength, canSeek, options);
        if (canSeek)
        {
            profileStream.Position = profileOffset;
        }

        return result;
    }

    internal static void EnsureStreamEnded(System.IO.Stream profileStream, long profileOffset, int declaredSize)
    {
        if (profileStream.CanSeek)
        {
            long trailingCount = profileStream.Length - profileStream.Position;
            if (trailingCount != 0)
            {
                long profileEnd = CheckedSize.AddOffsets(profileOffset, declaredSize, long.MaxValue);
                string trailingUnit = trailingCount == 1 ? "byte" : "bytes";
                throw new IccProfileFormatException($"The profile declared at byte offset {profileOffset} ends at byte offset {profileEnd}, but {trailingCount} trailing {trailingUnit} remain while exact profile size is required.");
            }

            return;
        }

        byte[] trailingByte = new byte[1];
        int read = profileStream.Read(trailingByte, 0, 1);
        if (read != 0)
        {
            long profileEnd = CheckedSize.AddOffsets(profileOffset, declaredSize, long.MaxValue);
            throw new IccProfileFormatException($"The profile declared at byte offset {profileOffset} ends at byte offset {profileEnd}, but trailing bytes remain while exact profile size is required.");
        }
    }

    internal static int ReadAtMost(System.IO.Stream profileStream, byte[] destination, int destinationOffset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = profileStream.Read(destination, destinationOffset + totalRead, count - totalRead);
            if (read == 0)
            {
                return totalRead;
            }

            totalRead = totalRead + read;
        }

        return totalRead;
    }

    internal static int GetRequiredProbePrefixSize(System.ReadOnlySpan<byte> headerPrefix, IccReadOptions options)
    {
        uint encodedTagCount = ReadUInt32(headerPrefix, IccConstants.HeaderSize);
        if (encodedTagCount > int.MaxValue || encodedTagCount > options.ResourceLimits.MaximumTagCount)
        {
            return IccConstants.MinimumProfileSize;
        }

        long tableEnd = IccConstants.MinimumProfileSize + ((long)encodedTagCount * IccConstants.TagTableEntrySize);
        if (tableEnd > int.MaxValue || tableEnd > options.ResourceLimits.MaximumProfileBytes)
        {
            return IccConstants.MinimumProfileSize;
        }

        return (int)tableEnd;
    }

    internal static IccOperationResult ProbePrefix(System.ReadOnlySpan<byte> prefix, long profileOffset, long availableLength, bool availableLengthIsKnown, IccReadOptions options)
    {
        if (prefix.Length < 4)
        {
            return CreateResult(IccOperationStatus.InvalidData, "The input is too short to contain an ICC profile size.", profileOffset, prefix.Length);
        }

        uint encodedSize = ReadUInt32(prefix, 0);
        if (encodedSize > int.MaxValue)
        {
            return CreateResult(IccOperationStatus.ResourceLimitExceeded, $"The declared profile size of {encodedSize} bytes cannot be represented by the managed profile model.", profileOffset, 4);
        }

        int declaredSize = (int)encodedSize;
        if (declaredSize > options.ResourceLimits.MaximumProfileBytes)
        {
            return CreateResult(IccOperationStatus.ResourceLimitExceeded, $"The declared profile size of {declaredSize} bytes exceeds the configured maximum of {options.ResourceLimits.MaximumProfileBytes} bytes.", profileOffset, 4);
        }

        if (declaredSize < IccConstants.MinimumProfileSize)
        {
            return CreateResult(IccOperationStatus.InvalidData, $"The declared profile size of {declaredSize} bytes is smaller than the minimum complete ICC profile size.", profileOffset, 4);
        }

        if (availableLengthIsKnown && declaredSize > availableLength)
        {
            return CreateResult(IccOperationStatus.InvalidData, $"The profile declares {declaredSize} bytes, but only {availableLength} bytes are available.", profileOffset, availableLength);
        }

        if (availableLengthIsKnown && options.RequireExactProfileSize && declaredSize != availableLength)
        {
            return CreateResult(IccOperationStatus.InvalidData, $"The profile declares {declaredSize} bytes, but the bounded input contains {availableLength} bytes.", profileOffset, availableLength);
        }

        if (prefix.Length < IccConstants.MinimumProfileSize)
        {
            return CreateResult(IccOperationStatus.InvalidData, "The input is too short to contain an ICC header and tag count.", profileOffset, prefix.Length);
        }

        uint signature = ReadUInt32(prefix, 36);
        if (signature != IccConstants.ProfileFileSignature && options.Mode != IccReadMode.Recovery)
        {
            return CreateResult(IccOperationStatus.InvalidData, "The ICC profile file signature is not 'acsp'.", profileOffset + 36, 4);
        }

        IccVersion version = HeaderCodec.DecodeVersion(ReadUInt32(prefix, 8));
        if (options.Mode == IccReadMode.Strict && !HeaderCodec.HasValidVersionDigits(version))
        {
            return CreateResult(IccOperationStatus.InvalidData, "The ICC profile version is not valid binary-coded decimal.", profileOffset + 8, 4);
        }

        if (options.Mode == IccReadMode.Strict && version.Kind == IccVersionKind.Unknown)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature, $"ICC profile major version {version.Major} is unsupported.", profileOffset + 8, 4);
        }

        if (options.Mode == IccReadMode.Strict && version.IsFutureMinor)
        {
            return CreateResult(IccOperationStatus.UnsupportedFeature, "The ICC profile version is newer than the supported range.", profileOffset + 8, 4);
        }

        uint encodedTagCount = ReadUInt32(prefix, IccConstants.HeaderSize);
        if (encodedTagCount > int.MaxValue || encodedTagCount > options.ResourceLimits.MaximumTagCount)
        {
            return CreateResult(IccOperationStatus.ResourceLimitExceeded, $"The declared tag count of {encodedTagCount} exceeds the configured or managed limit.", profileOffset + IccConstants.HeaderSize, 4);
        }

        long tableEnd = IccConstants.MinimumProfileSize + ((long)encodedTagCount * IccConstants.TagTableEntrySize);
        if (tableEnd > declaredSize)
        {
            return CreateResult(IccOperationStatus.InvalidData, "The declared tag table does not fit within the profile.", profileOffset + IccConstants.HeaderSize, 4);
        }

        if (prefix.Length < tableEnd)
        {
            return CreateResult(IccOperationStatus.InvalidData, "The input is too short to contain the declared tag table.", profileOffset + IccConstants.HeaderSize, prefix.Length - IccConstants.HeaderSize);
        }

        int totalPayloadBytes = 0;
        int entryIndex = 0;
        while (entryIndex < (int)encodedTagCount)
        {
            int entryOffset = IccConstants.MinimumProfileSize + (entryIndex * IccConstants.TagTableEntrySize);
            uint encodedOffset = ReadUInt32(prefix, entryOffset + 4);
            uint encodedPayloadSize = ReadUInt32(prefix, entryOffset + 8);
            if (encodedOffset > int.MaxValue || encodedOffset > declaredSize)
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} has a payload offset outside the declared profile.", profileOffset + entryOffset + 4, 4);
            }

            if (encodedPayloadSize > int.MaxValue || encodedPayloadSize > options.ResourceLimits.MaximumTagBytes)
            {
                return CreateResult(IccOperationStatus.ResourceLimitExceeded, $"Tag-table entry {entryIndex} has a payload size that exceeds the configured or managed limit.", profileOffset + entryOffset + 8, 4);
            }

            int relativeOffset = (int)encodedOffset;
            int payloadSize = (int)encodedPayloadSize;
            if (payloadSize > declaredSize - relativeOffset)
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} extends beyond the declared profile.", profileOffset + entryOffset + 4, 8);
            }

            long alignedPayloadEnd = (((long)relativeOffset + payloadSize + IccConstants.TagPayloadAlignment - 1L) / IccConstants.TagPayloadAlignment) * IccConstants.TagPayloadAlignment;
            if (alignedPayloadEnd > declaredSize)
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} requires alignment padding beyond the declared profile.", profileOffset + entryOffset + 4, 8);
            }

            if (options.Mode != IccReadMode.Recovery && (relativeOffset & (IccConstants.TagPayloadAlignment - 1)) != 0)
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} has an unaligned payload offset.", profileOffset + entryOffset + 4, 4);
            }

            if (options.Mode != IccReadMode.Recovery && relativeOffset < (int)tableEnd)
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} intersects the tag table.", profileOffset + entryOffset + 4, 4);
            }

            if (options.Mode != IccReadMode.Recovery && payloadSize < 8)
            {
                return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} is too short to contain a tag type header.", profileOffset + entryOffset + 8, 4);
            }

            bool isSharedPayload = false;
            int previousIndex = 0;
            while (previousIndex < entryIndex)
            {
                int previousOffset = IccConstants.MinimumProfileSize + (previousIndex * IccConstants.TagTableEntrySize);
                uint previousSignature = ReadUInt32(prefix, previousOffset);
                uint entrySignature = ReadUInt32(prefix, entryOffset);
                if (options.Mode != IccReadMode.Recovery && previousSignature == entrySignature)
                {
                    return CreateResult(IccOperationStatus.InvalidData, $"Tag-table entry {entryIndex} duplicates an earlier tag signature.", profileOffset + entryOffset, 4);
                }

                uint previousPayloadOffset = ReadUInt32(prefix, previousOffset + 4);
                uint previousPayloadSize = ReadUInt32(prefix, previousOffset + 8);
                if (previousPayloadOffset == encodedOffset && previousPayloadSize == encodedPayloadSize)
                {
                    isSharedPayload = true;
                }

                previousIndex = previousIndex + 1;
            }

            if (!isSharedPayload)
            {
                if (payloadSize > int.MaxValue - totalPayloadBytes || totalPayloadBytes + payloadSize > options.ResourceLimits.MaximumTotalTagBytes)
                {
                    return CreateResult(IccOperationStatus.ResourceLimitExceeded, $"Unique tag payloads exceed the configured or managed total at entry {entryIndex}.", profileOffset + entryOffset + 8, 4);
                }

                totalPayloadBytes = totalPayloadBytes + payloadSize;
            }

            entryIndex = entryIndex + 1;
        }

        return CreateResult(IccOperationStatus.Success, "The input contains a plausible bounded ICC profile header and tag table.", profileOffset, declaredSize);
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    internal static IccOperationResult CreateResult(IccOperationStatus status, string message, long offset, long length)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        result.Location.Offset = offset;
        result.Location.Length = length;
        return result;
    }
}
