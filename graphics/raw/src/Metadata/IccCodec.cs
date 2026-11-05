using System;

namespace raw;

internal static class IccCodec
{
    internal const ushort IccProfileTag = 34675;

    internal static RawIccProfile Parse(byte[] rawBytes, RawLimits limits, long offset, string ifdPath)
    {
        ValidateInput(rawBytes, limits, offset);
        uint declaredSize = ReadUInt32(rawBytes, 0);
        if (declaredSize != rawBytes.Length)
        {
            throw Invalid("The ICC header profile size does not match the bounded profile length.", offset);
        }
        if ((declaredSize & 3) != 0)
        {
            throw Invalid("The ICC profile size must be aligned to four bytes.", offset);
        }
        if (ReadSignature(rawBytes, 36) != "acsp")
        {
            throw Invalid("The ICC profile file signature is not 'acsp'.", At(offset, 36));
        }
        RequireHeaderSignature(rawBytes, 4, true, offset);
        RequireHeaderSignature(rawBytes, 12, false, offset);
        RequireHeaderSignature(rawBytes, 16, false, offset);
        RequireHeaderSignature(rawBytes, 20, false, offset);
        RequireHeaderSignature(rawBytes, 40, true, offset);
        RequireHeaderSignature(rawBytes, 48, true, offset);
        RequireHeaderSignature(rawBytes, 52, true, offset);
        RequireHeaderSignature(rawBytes, 80, true, offset);
        if (rawBytes[10] != 0 || rawBytes[11] != 0)
        {
            throw Invalid("The reserved ICC version bytes must be zero.", At(offset, 10));
        }
        int versionMajor = rawBytes[8];
        int versionMinor = rawBytes[9] >> 4;
        int versionBugFix = rawBytes[9] & 15;
        if ((versionMajor != 2 && versionMajor != 4) || versionMinor > 9 || versionBugFix > 9)
        {
            throw Invalid("Only valid ICC v2 and v4 profile version encodings are supported.", At(offset, 8));
        }
        if (!AllZero(rawBytes, 100, 28))
        {
            throw Invalid("The reserved ICC profile header bytes must be zero.", At(offset, 100));
        }
        uint renderingIntent = ReadUInt32(rawBytes, 64);
        if (renderingIntent > 3)
        {
            throw Invalid("The ICC rendering intent must be from zero through three.", At(offset, 64));
        }

        int year = ReadUInt16(rawBytes, 24);
        int month = ReadUInt16(rawBytes, 26);
        int day = ReadUInt16(rawBytes, 28);
        int hour = ReadUInt16(rawBytes, 30);
        int minute = ReadUInt16(rawBytes, 32);
        int second = ReadUInt16(rawBytes, 34);
        bool createdValid = ValidDateTime(year, month, day, hour, minute, second);
        double[] illuminant = new double[3];
        illuminant[0] = ReadFixed(rawBytes, 68);
        illuminant[1] = ReadFixed(rawBytes, 72);
        illuminant[2] = ReadFixed(rawBytes, 76);
        RawIccHeader header = new RawIccHeader(declaredSize, ReadOptionalSignature(rawBytes, 4), versionMajor, versionMinor, versionBugFix, ReadSignature(rawBytes, 12), ReadSignature(rawBytes, 16), ReadSignature(rawBytes, 20), year, month, day, hour, minute, second, createdValid, ReadOptionalSignature(rawBytes, 40), ReadUInt32(rawBytes, 44), ReadOptionalSignature(rawBytes, 48), ReadOptionalSignature(rawBytes, 52), ReadUInt64(rawBytes, 56), renderingIntent, illuminant, ReadOptionalSignature(rawBytes, 80));

        uint unsignedTagCount = ReadUInt32(rawBytes, 128);
        if (unsignedTagCount > int.MaxValue || unsignedTagCount > limits.MaxTagCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The ICC tag count exceeds the configured tag-count limit.", At(offset, 128));
        }
        int tagCount = (int)unsignedTagCount;
        long tableLength = CheckedMath.Add(132, CheckedMath.Multiply(tagCount, 12));
        if (tableLength > rawBytes.Length)
        {
            throw Truncated("The ICC tag table is truncated.", At(offset, 128));
        }
        RawIccTag[] tags = new RawIccTag[tagCount];
        System.Collections.Generic.HashSet<string> signatures = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        System.Collections.Generic.Dictionary<ulong, byte[]> sharedData = new System.Collections.Generic.Dictionary<ulong, byte[]>();
        int tagIndex = 0;
        while (tagIndex < tagCount)
        {
            int entryOffset = 132 + tagIndex * 12;
            string signature = ReadSignature(rawBytes, entryOffset);
            if (!ValidSignature(rawBytes, entryOffset) || !signatures.Add(signature))
            {
                throw Invalid("Every ICC tag signature must be printable and unique.", At(offset, entryOffset));
            }
            uint tagOffset = ReadUInt32(rawBytes, entryOffset + 4);
            uint tagSize = ReadUInt32(rawBytes, entryOffset + 8);
            if ((tagOffset & 3) != 0)
            {
                throw Invalid("An ICC tag data element is not aligned to four bytes.", At(offset, entryOffset + 4));
            }
            if (tagOffset < tableLength || tagSize < 8 || tagOffset > rawBytes.Length || tagSize > rawBytes.Length - tagOffset)
            {
                throw Invalid("An ICC tag data element range is outside the bounded profile.", At(offset, entryOffset + 4));
            }
            int signedOffset = (int)tagOffset;
            int signedSize = (int)tagSize;
            if (!AllZero(rawBytes, signedOffset + 4, 4))
            {
                throw Invalid("The reserved ICC tag type bytes must be zero.", At(offset, signedOffset + 4));
            }
            if (!ValidSignature(rawBytes, signedOffset))
            {
                throw Invalid("An ICC tag type signature is not printable.", At(offset, signedOffset));
            }
            ulong dataKey = ((ulong)tagOffset << 32) | tagSize;
            byte[] tagBytes;
            if (sharedData.ContainsKey(dataKey))
            {
                tagBytes = sharedData[dataKey];
            }
            else
            {
                tagBytes = rawBytes.AsSpan(signedOffset, signedSize).ToArray();
                sharedData.Add(dataKey, tagBytes);
            }
            tags[tagIndex] = new RawIccTag(signature, tagOffset, tagSize, ReadSignature(tagBytes, 0), tagBytes);
            tagIndex++;
        }

        bool contiguous = ValidateTagRanges(rawBytes, tags, (int)tableLength, offset);
        RawIccTag? redXyz = FindTag(tags, "rXYZ");
        RawIccTag? greenXyz = FindTag(tags, "gXYZ");
        RawIccTag? blueXyz = FindTag(tags, "bXYZ");
        double[] matrix = ParseMatrix(redXyz, greenXyz, blueXyz, offset);
        double[] whitePoint = ParseOptionalXyz(FindTag(tags, "wtpt"), offset);
        RawIccToneCurve? redTrc = ParseOptionalCurve(FindTag(tags, "rTRC"), offset);
        RawIccToneCurve? greenTrc = ParseOptionalCurve(FindTag(tags, "gTRC"), offset);
        RawIccToneCurve? blueTrc = ParseOptionalCurve(FindTag(tags, "bTRC"), offset);
        string description = ParseDescription(FindTag(tags, "desc"), offset);

        byte[] embeddedId = rawBytes.AsSpan(84, 16).ToArray();
        byte[] calculatedId = CalculateProfileId(rawBytes);
        bool hasEmbeddedId = !AllZero(embeddedId, 0, embeddedId.Length);
        bool idMatches = hasEmbeddedId && EqualBytes(embeddedId, calculatedId);
        string embeddedHex = hasEmbeddedId ? System.Convert.ToHexString(embeddedId) : string.Empty;
        string calculatedHex = System.Convert.ToHexString(calculatedId);
        string identity = description.Length == 0 ? calculatedHex : description + " [" + calculatedHex + "]";
        return new RawIccProfile(RawTextValue.Copy(rawBytes), header, tags, description, identity, embeddedId, calculatedId, embeddedHex, calculatedHex, hasEmbeddedId, idMatches, contiguous, matrix, whitePoint, redTrc, greenTrc, blueTrc, offset, ifdPath == null ? string.Empty : ifdPath);
    }

    internal static RawIccProfile[] Discover(TiffIfdGraph graph, RawLimits? limits)
    {
        if (graph == null)
        {
            return System.Array.Empty<RawIccProfile>();
        }
        System.Collections.Generic.List<RawIccProfile> profiles = new System.Collections.Generic.List<RawIccProfile>();
        long total = 0;
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (entry.Tag == IccProfileTag && (entry.FieldType == TiffFieldType.Undefined || entry.FieldType == TiffFieldType.Byte) && entry.RawBytes.Length > 0)
                {
                    total = CheckedMath.Add(total, entry.RawBytes.Length);
                    if (total > (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxMetadataBytes)
                    {
                        throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Combined ICC profiles exceed the configured metadata limit.", entry.EntryOffset);
                    }
                    profiles.Add(Parse(entry.RawBytes, limits, XmpCodec.GetValueOffset(graph, entry), ifd.Path));
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        return profiles.ToArray();
    }

    internal static RawIccProfile ReplaceTagDataFixedSize(RawIccProfile profile, string signature, byte[] replacementBytes, RawLimits limits)
    {
        if (profile == null || profile.RawBytes == null || signature == null || replacementBytes == null)
        {
            throw Invalid("A complete ICC profile, tag signature, and replacement bytes are required.", -1);
        }
        RawIccProfile source = Parse(profile.CopyBytes(), limits, profile.Offset, profile.IfdPath == null ? string.Empty : profile.IfdPath);
        RawIccTag? tag = source.GetTag(signature);
        if (tag == null)
        {
            throw Invalid("The ICC tag selected for replacement was not found.", profile.Offset);
        }
        if (replacementBytes.Length != tag.RawBytes.Length)
        {
            throw Invalid("A fixed-size ICC tag replacement must retain the exact tag data length.", At(profile.Offset, (int)tag.Offset));
        }
        int tagIndex = 0;
        while (tagIndex < (source.Tags ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            RawIccTag other = source.Tags[tagIndex];
            if (other != null && other.Signature != signature && other.Offset == tag.Offset && other.Size == tag.Size)
            {
                throw Invalid("A fixed-size ICC replacement cannot modify tag data shared by another tag.", At(profile.Offset, (int)tag.Offset));
            }
            tagIndex++;
        }
        byte[] encoded = source.CopyBytes();
        replacementBytes.AsSpan().CopyTo(encoded.AsSpan((int)tag.Offset, replacementBytes.Length));
        if (source.HasEmbeddedProfileId)
        {
            byte[] profileId = CalculateProfileId(encoded);
            profileId.AsSpan().CopyTo(encoded.AsSpan(84, 16));
        }
        return Parse(encoded, limits, profile.Offset, profile.IfdPath == null ? string.Empty : profile.IfdPath);
    }

    internal static bool ValidateTagRanges(byte[] rawBytes, RawIccTag[] tags, int tableEnd, long offset)
    {
        int[] order = new int[tags.Length];
        int index = 0;
        while (index < order.Length)
        {
            order[index] = index;
            index++;
        }
        System.Array.Sort(order, (left, right) => CompareTags(tags[left], tags[right]));
        long expectedOffset = tableEnd;
        long previousOffset = -1;
        long previousSize = -1;
        long previousEnd = tableEnd;
        bool contiguous = true;
        index = 0;
        while (index < order.Length)
        {
            RawIccTag tag = tags[order[index]];
            long tagOffset = tag.Offset;
            long tagSize = tag.Size;
            if (tagOffset == previousOffset)
            {
                if (tagSize != previousSize)
                {
                    throw Invalid("ICC tags sharing an offset must reference the same complete data element.", At(offset, (int)tagOffset));
                }
                index++;
                continue;
            }
            if (tagOffset < previousEnd)
            {
                throw Invalid("ICC tag data elements partially overlap.", At(offset, (int)tagOffset));
            }
            if (tagOffset != expectedOffset)
            {
                contiguous = false;
            }
            long dataEnd = tagOffset + tagSize;
            long paddedEnd = AlignFour(dataEnd);
            if (paddedEnd > rawBytes.Length)
            {
                throw Invalid("ICC tag alignment padding extends beyond the bounded profile.", At(offset, (int)dataEnd));
            }
            if (!AllZero(rawBytes, (int)dataEnd, (int)(paddedEnd - dataEnd)))
            {
                throw Invalid("ICC tag alignment padding must contain zero bytes.", At(offset, (int)dataEnd));
            }
            previousOffset = tagOffset;
            previousSize = tagSize;
            previousEnd = dataEnd;
            expectedOffset = paddedEnd;
            index++;
        }
        if (expectedOffset != rawBytes.Length)
        {
            contiguous = false;
        }
        return contiguous;
    }

    internal static double[] ParseMatrix(RawIccTag? red, RawIccTag? green, RawIccTag? blue, long profileOffset)
    {
        double[]? redColumn = red == null ? null : ParseXyz(red, profileOffset);
        double[]? greenColumn = green == null ? null : ParseXyz(green, profileOffset);
        double[]? blueColumn = blue == null ? null : ParseXyz(blue, profileOffset);
        if (red == null || green == null || blue == null)
        {
            return System.Array.Empty<double>();
        }
        double[] matrix = new double[9];
        matrix[0] = (redColumn ?? throw new System.InvalidOperationException("A required value is unavailable."))[0];
        matrix[1] = (greenColumn ?? throw new System.InvalidOperationException("A required value is unavailable."))[0];
        matrix[2] = (blueColumn ?? throw new System.InvalidOperationException("A required value is unavailable."))[0];
        matrix[3] = redColumn[1];
        matrix[4] = greenColumn[1];
        matrix[5] = blueColumn[1];
        matrix[6] = redColumn[2];
        matrix[7] = greenColumn[2];
        matrix[8] = blueColumn[2];
        return matrix;
    }

    internal static double[] ParseOptionalXyz(RawIccTag? tag, long profileOffset)
    {
        if (tag == null)
        {
            return System.Array.Empty<double>();
        }
        return ParseXyz(tag, profileOffset);
    }

    internal static double[] ParseXyz(RawIccTag tag, long profileOffset)
    {
        if (tag.TypeSignature != "XYZ " || tag.RawBytes.Length != 20)
        {
            throw Invalid("An extracted ICC XYZ tag must contain exactly one XYZType value.", At(profileOffset, (int)tag.Offset));
        }
        double[] values = new double[3];
        values[0] = ReadFixed(tag.RawBytes, 8);
        values[1] = ReadFixed(tag.RawBytes, 12);
        values[2] = ReadFixed(tag.RawBytes, 16);
        return values;
    }

    internal static RawIccToneCurve? ParseOptionalCurve(RawIccTag? tag, long profileOffset)
    {
        if (tag == null)
        {
            return null;
        }
        if (tag.TypeSignature == "curv")
        {
            return ParseCurveType(tag, profileOffset);
        }
        if (tag.TypeSignature == "para")
        {
            return ParseParametricCurve(tag, profileOffset);
        }
        throw Invalid("An ICC RGB TRC tag must use curveType or parametricCurveType.", At(profileOffset, (int)tag.Offset));
    }

    internal static RawIccToneCurve ParseCurveType(RawIccTag tag, long profileOffset)
    {
        if (tag.RawBytes.Length < 12)
        {
            throw Truncated("An ICC curveType header is truncated.", At(profileOffset, (int)tag.Offset));
        }
        uint unsignedCount = ReadUInt32(tag.RawBytes, 8);
        if (unsignedCount > int.MaxValue)
        {
            throw Invalid("An ICC curveType sample count is too large.", At(profileOffset, (int)tag.Offset + 8));
        }
        int count = (int)unsignedCount;
        long expected = 12L + count * 2L;
        if (expected != tag.RawBytes.Length)
        {
            throw Invalid("The ICC curveType size does not match its sample count.", At(profileOffset, (int)tag.Offset));
        }
        if (count == 0)
        {
            return new RawIccToneCurve(RawIccCurveKind.Identity, 0, 1.0, null, null);
        }
        if (count == 1)
        {
            double gamma = ReadUInt16(tag.RawBytes, 12) / 256.0;
            return new RawIccToneCurve(RawIccCurveKind.Gamma, 0, gamma, null, null);
        }
        ushort[] samples = new ushort[count];
        int index = 0;
        while (index < count)
        {
            samples[index] = ReadUInt16(tag.RawBytes, 12 + index * 2);
            index++;
        }
        return new RawIccToneCurve(RawIccCurveKind.Sampled, 0, 1.0, samples, null);
    }

    internal static RawIccToneCurve ParseParametricCurve(RawIccTag tag, long profileOffset)
    {
        if (tag.RawBytes.Length < 12)
        {
            throw Truncated("An ICC parametricCurveType header is truncated.", At(profileOffset, (int)tag.Offset));
        }
        int functionType = ReadUInt16(tag.RawBytes, 8);
        if (tag.RawBytes[10] != 0 || tag.RawBytes[11] != 0 || functionType < 0 || functionType > 4)
        {
            throw Invalid("An ICC parametric curve function or reserved field is invalid.", At(profileOffset, (int)tag.Offset + 8));
        }
        int parameterCount = ParametricCount(functionType);
        if (tag.RawBytes.Length != 12 + parameterCount * 4)
        {
            throw Invalid("The ICC parametric curve size does not match its function type.", At(profileOffset, (int)tag.Offset));
        }
        double[] parameters = new double[parameterCount];
        int index = 0;
        while (index < parameterCount)
        {
            parameters[index] = ReadFixed(tag.RawBytes, 12 + index * 4);
            index++;
        }
        return new RawIccToneCurve(RawIccCurveKind.Parametric, functionType, parameters[0], null, parameters);
    }

    internal static string ParseDescription(RawIccTag? tag, long profileOffset)
    {
        if (tag == null)
        {
            return string.Empty;
        }
        if (tag.TypeSignature == "mluc")
        {
            return ParseMluc(tag, profileOffset);
        }
        if (tag.TypeSignature == "desc")
        {
            return ParseTextDescription(tag, profileOffset);
        }
        throw Invalid("An ICC profile description tag must use a localized Unicode or text description type.", At(profileOffset, (int)tag.Offset));
    }

    internal static string ParseMluc(RawIccTag tag, long profileOffset)
    {
        byte[] bytes = tag.RawBytes;
        if (bytes.Length < 16)
        {
            throw Truncated("An ICC multiLocalizedUnicodeType header is truncated.", At(profileOffset, (int)tag.Offset));
        }
        uint unsignedCount = ReadUInt32(bytes, 8);
        uint recordSize = ReadUInt32(bytes, 12);
        if (unsignedCount > int.MaxValue || recordSize != 12)
        {
            throw Invalid("An ICC localized string record count or record size is invalid.", At(profileOffset, (int)tag.Offset + 8));
        }
        int count = (int)unsignedCount;
        long recordsEnd = 16L + count * 12L;
        if (recordsEnd > bytes.Length)
        {
            throw Truncated("The ICC localized string records are truncated.", At(profileOffset, (int)tag.Offset + 16));
        }
        string first = string.Empty;
        string englishUnitedStates = string.Empty;
        int index = 0;
        while (index < count)
        {
            int recordOffset = 16 + index * 12;
            uint stringLength = ReadUInt32(bytes, recordOffset + 4);
            uint stringOffset = ReadUInt32(bytes, recordOffset + 8);
            if ((stringLength & 1) != 0 || stringOffset < recordsEnd || stringOffset > bytes.Length || stringLength > bytes.Length - stringOffset)
            {
                throw Invalid("An ICC localized Unicode string range is invalid.", At(profileOffset, (int)tag.Offset + recordOffset));
            }
            string value = DecodeUtf16BigEndian(bytes, (int)stringOffset, (int)stringLength, At(profileOffset, (int)tag.Offset + (int)stringOffset));
            if (index == 0)
            {
                first = value;
            }
            if (bytes[recordOffset] == 101 && bytes[recordOffset + 1] == 110 && bytes[recordOffset + 2] == 85 && bytes[recordOffset + 3] == 83)
            {
                englishUnitedStates = value;
            }
            index++;
        }
        return englishUnitedStates.Length == 0 ? first : englishUnitedStates;
    }

    internal static string ParseTextDescription(RawIccTag tag, long profileOffset)
    {
        byte[] bytes = tag.RawBytes;
        if (bytes.Length < 12)
        {
            throw Truncated("An ICC textDescriptionType header is truncated.", At(profileOffset, (int)tag.Offset));
        }
        uint unsignedLength = ReadUInt32(bytes, 8);
        if (unsignedLength > int.MaxValue || unsignedLength > bytes.Length - 12)
        {
            throw Invalid("The ICC text description length exceeds its tag.", At(profileOffset, (int)tag.Offset + 8));
        }
        int length = (int)unsignedLength;
        if (length == 0)
        {
            return string.Empty;
        }
        if (bytes[12 + length - 1] != 0 || !AsciiRange(bytes, 12, length - 1))
        {
            throw Invalid("The ICC text description must be null-terminated seven-bit ASCII.", At(profileOffset, (int)tag.Offset + 12));
        }
        return System.Text.Encoding.ASCII.GetString(bytes, 12, length - 1);
    }

    internal static byte[] CalculateProfileId(byte[] rawBytes)
    {
        byte[] fingerprintBytes = RawTextValue.Copy(rawBytes);
        System.Array.Clear(fingerprintBytes, 44, 4);
        System.Array.Clear(fingerprintBytes, 64, 4);
        System.Array.Clear(fingerprintBytes, 84, 16);
        return System.Security.Cryptography.MD5.HashData(fingerprintBytes);
    }

    internal static int ParametricCount(int functionType)
    {
        switch (functionType)
        {
            case 0: return 1;
            case 1: return 3;
            case 2: return 4;
            case 3: return 5;
            case 4: return 7;
            default: return 0;
        }
    }

    internal static RawIccTag? FindTag(RawIccTag[] tags, string signature)
    {
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index].Signature == signature)
            {
                return tags[index];
            }
            index++;
        }
        return null;
    }

    internal static int CompareTags(RawIccTag left, RawIccTag right)
    {
        int offsetComparison = left.Offset.CompareTo(right.Offset);
        if (offsetComparison != 0)
        {
            return offsetComparison;
        }
        return left.Size.CompareTo(right.Size);
    }

    internal static string DecodeUtf16BigEndian(byte[] bytes, int offset, int length, long absoluteOffset)
    {
        if (!ValidUtf16BigEndian(bytes, offset, length))
        {
            throw Invalid("An ICC localized string contains invalid UTF-16BE.", absoluteOffset);
        }
        return new System.Text.UnicodeEncoding(true, false, false).GetString(bytes, offset, length);
    }

    internal static bool ValidUtf16BigEndian(byte[] bytes, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            ushort value = ReadUInt16(bytes, offset + index);
            if (value >= 55296 && value <= 56319)
            {
                if (index + 2 >= length)
                {
                    return false;
                }
                ushort low = ReadUInt16(bytes, offset + index + 2);
                if (low < 56320 || low > 57343)
                {
                    return false;
                }
                index += 4;
                continue;
            }
            if (value >= 56320 && value <= 57343)
            {
                return false;
            }
            index += 2;
        }
        return true;
    }

    internal static bool ValidDateTime(int year, int month, int day, int hour, int minute, int second)
    {
        if (year < 1 || year > 9999 || month < 1 || month > 12 || hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
        {
            return false;
        }
        return day >= 1 && day <= System.DateTime.DaysInMonth(year, month);
    }

    internal static bool ValidSignature(byte[] bytes, int offset)
    {
        int index = 0;
        while (index < 4)
        {
            if (bytes[offset + index] < 32 || bytes[offset + index] > 126)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static string ReadOptionalSignature(byte[] bytes, int offset)
    {
        if (AllZero(bytes, offset, 4))
        {
            return string.Empty;
        }
        return ReadSignature(bytes, offset);
    }

    internal static void RequireHeaderSignature(byte[] bytes, int relativeOffset, bool optional, long profileOffset)
    {
        if (optional && AllZero(bytes, relativeOffset, 4))
        {
            return;
        }
        if (!ValidSignature(bytes, relativeOffset))
        {
            throw Invalid("An ICC profile header signature is not printable or zero.", At(profileOffset, relativeOffset));
        }
    }

    internal static string ReadSignature(byte[] bytes, int offset)
    {
        return System.Text.Encoding.ASCII.GetString(bytes, offset, 4);
    }

    internal static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return EndianReader.ReadUInt16(bytes.AsSpan(offset, 2), RawByteOrder.BigEndian);
    }

    internal static uint ReadUInt32(byte[] bytes, int offset)
    {
        return EndianReader.ReadUInt32(bytes.AsSpan(offset, 4), RawByteOrder.BigEndian);
    }

    internal static ulong ReadUInt64(byte[] bytes, int offset)
    {
        return EndianReader.ReadUInt64(bytes.AsSpan(offset, 8), RawByteOrder.BigEndian);
    }

    internal static double ReadFixed(byte[] bytes, int offset)
    {
        return EndianReader.ReadInt32(bytes.AsSpan(offset, 4), RawByteOrder.BigEndian) / 65536.0;
    }

    internal static long AlignFour(long value)
    {
        return (value + 3) & -4L;
    }

    internal static bool AllZero(byte[] bytes, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            if (bytes[offset + index] != 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool AsciiRange(byte[] bytes, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            if (bytes[offset + index] > 127)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool EqualBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static void ValidateInput(byte[] rawBytes, RawLimits limits, long offset)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit(limits.MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagValueBytes, "The configured tag-value limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
        if (rawBytes == null || rawBytes.Length < 132)
        {
            throw Truncated("An ICC profile requires a 128-byte header and tag count.", offset);
        }
        if (rawBytes.LongLength > limits.MaxMetadataBytes || rawBytes.LongLength > limits.MaxTagValueBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The ICC profile exceeds a configured metadata or tag-value limit.", offset);
        }
        CheckedMath.AllocationSize(rawBytes.LongLength, 1, limits);
    }

    internal static long At(long offset, int relative)
    {
        if (offset < 0)
        {
            return offset;
        }
        return CheckedMath.Add(offset, relative);
    }

    internal static RawFormatException Invalid(string message, long offset)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidMetadata, message, offset);
    }

    internal static RawFormatException Truncated(string message, long offset)
    {
        return new RawFormatException(RawDiagnosticCode.TruncatedInput, message, offset);
    }
}
