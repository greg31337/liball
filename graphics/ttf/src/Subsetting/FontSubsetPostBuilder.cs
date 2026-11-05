namespace ttf;

internal static class FontSubsetPostBuilder
{
    const int SlantTag = 0x736C6E74;
    const uint Version2 = 0x00020000U;
    const uint Version3 = 0x00030000U;

    internal static byte[]? Create(
        FontInstance instance,
        int[] newToOldGlyphIds,
        FontSubsetMetadataPolicy metadataPolicy)
    {
        ValidateArguments(instance, newToOldGlyphIds, metadataPolicy);
        FontFace face = instance.GetFace();
        SfntPostTable? source = face.GetPostTable();

        if (source == null)
        {
            return null;
        }

        SfntTableData? postTableData = face.GetTableStore().FindTable(FontTags.Post);
        System.ArgumentNullException.ThrowIfNull(postTableData);
        byte[] header = postTableData
            .GetDataSlice()
            .CopyBytes();
        header = new SfntStaticInstanceMetadata(
            instance,
            new int[] { 0, 0, 0, 0 })
            .PatchTable(FontTags.Post, header);
        int italicAngle = ResolveItalicAngle(
            instance.GetCoordinates(),
            ReadInt32(header, 4));

        if (metadataPolicy == FontSubsetMetadataPolicy.Minimal ||
            !source.ProvidesGlyphNames())
        {
            return WriteVersion3(header, italicAngle, face.GetTableCache()
                .GetLimits());
        }

        return WriteVersion2(
            header,
            italicAngle,
            source,
            newToOldGlyphIds,
            face.GetTableCache().GetLimits());
    }

    static byte[] WriteVersion3(
        byte[] sourceHeader,
        int italicAngle,
        FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            32,
            limits.GetMaxOutputBytes());
        WriteHeader(writer, sourceHeader, Version3, italicAngle);
        return writer.CopyBytes();
    }

    static byte[] WriteVersion2(
        byte[] sourceHeader,
        int italicAngle,
        SfntPostTable source,
        int[] newToOldGlyphIds,
        FontLimits limits)
    {
        int glyphCount = newToOldGlyphIds.Length;

        if (glyphCount > ushort.MaxValue)
        {
            throw FontLimitException.Create(
                "A subset post table has too many glyph names.");
        }

        int[] nameIndices = new int[glyphCount];
        System.Collections.Generic.List<string> customNames =
            new System.Collections.Generic.List<string>();
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            string name = SelectGlyphName(
                source,
                newToOldGlyphIds[glyphId],
                glyphId);
            int standardIndex = FindStandardName(name);

            if (standardIndex >= 0)
            {
                nameIndices[glyphId] = standardIndex;
            }
            else
            {
                int customIndex = FindCustomName(customNames, name);

                if (customIndex < 0)
                {
                    customIndex = customNames.Count;
                    customNames.Add(name);
                }

                if (customIndex > ushort.MaxValue -
                    SfntPostStandardNames.GetCount())
                {
                    throw FontLimitException.Create(
                        "A subset post table has too many custom names.");
                }

                nameIndices[glyphId] =
                    SfntPostStandardNames.GetCount() + customIndex;
            }

            glyphId++;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        WriteHeader(writer, sourceHeader, Version2, italicAngle);
        writer.WriteUInt16((ushort)glyphCount);
        glyphId = 0;

        while (glyphId < glyphCount)
        {
            writer.WriteUInt16((ushort)nameIndices[glyphId]);
            glyphId++;
        }

        int writeIndex = 0;
        while (writeIndex < customNames.Count)
        {
            WriteCustomName(writer, customNames[writeIndex], limits);
            writeIndex++;
        }

        return writer.CopyBytes();
    }

    static void WriteHeader(
        BigEndianWriter writer,
        byte[] source,
        uint version,
        int italicAngle)
    {
        writer.WriteUInt32(version);
        writer.WriteInt32(italicAngle);
        writer.WriteInt16((short)ReadInt16(source, 8));
        writer.WriteInt16((short)ReadInt16(source, 10));
        writer.WriteUInt32(ReadUInt32(source, 12));
        writer.WriteUInt32(ReadUInt32(source, 16));
        writer.WriteUInt32(ReadUInt32(source, 20));
        writer.WriteUInt32(ReadUInt32(source, 24));
        writer.WriteUInt32(ReadUInt32(source, 28));
    }

    static string SelectGlyphName(
        SfntPostTable source,
        int oldGlyphId,
        int newGlyphId)
    {
        if (oldGlyphId >= 0)
        {
            if (oldGlyphId >= source.GetGlyphCount())
            {
                throw FontFormatException.Create(
                    "A subset post name references an invalid glyph.");
            }

            SfntPostGlyphName record = source.GetGlyphNameRecord(oldGlyphId);

            if (record.HasValidName())
            {
                return record.GetName();
            }
        }

        return "gid" + newGlyphId.ToString(
            "D4",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    static int FindStandardName(string name)
    {
        int index = 0;
        while (index < SfntPostStandardNames.GetCount())
        {
            if (SfntPostStandardNames.GetName(index) == name)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    static int FindCustomName(
        System.Collections.Generic.List<string> names,
        string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index] == name) { return index; }
            index++;
        }
        return -1;
    }

    static void WriteCustomName(
        BigEndianWriter writer,
        string name,
        FontLimits limits)
    {
        if (name.Length == 0 || name.Length > 63 ||
            name.Length > limits.GetMaxStringBytes())
        {
            throw FontLimitException.Create(
                "A subset post glyph name exceeds the supported length.");
        }

        writer.WriteUInt8((byte)name.Length);
        int index = 0;
        while (index < name.Length)
        {
            char character = name[index];

            if (!IsValidNameCharacter(character))
            {
                throw FontFormatException.Create(
                    "A subset post glyph name is invalid.");
            }

            writer.WriteUInt8((byte)character);
            index++;
        }
    }

    static int ResolveItalicAngle(
        NormalizedFontCoordinates coordinates,
        int sourceAngle)
    {
        int index = 0;
        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == SlantTag)
            {
                double angle = coordinates.GetUserValue(index);
                double encoded = System.Math.Round(
                    angle * 65536,
                    System.MidpointRounding.AwayFromZero);

                if (!double.IsFinite(encoded) ||
                    encoded < int.MinValue || encoded > int.MaxValue)
                {
                    throw FontFormatException.Create(
                        "The subset post italic angle is outside Fixed16.16.");
                }

                return (int)encoded;
            }

            index++;
        }

        return sourceAngle;
    }

    static bool IsValidNameCharacter(char character)
    {
        return character >= 'A' && character <= 'Z' ||
            character >= 'a' && character <= 'z' ||
            character >= '0' && character <= '9' ||
            character == '.' || character == '_';
    }

    static void ValidateArguments(
        FontInstance instance,
        int[] newToOldGlyphIds,
        FontSubsetMetadataPolicy metadataPolicy)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }
        if (newToOldGlyphIds == null)
        {
            throw new System.ArgumentNullException("newToOldGlyphIds");
        }
        if (metadataPolicy < FontSubsetMetadataPolicy.Minimal ||
            metadataPolicy > FontSubsetMetadataPolicy.RetainSafe)
        {
            throw new System.ArgumentOutOfRangeException("metadataPolicy");
        }
    }

    static int ReadInt16(byte[] data, int offset)
    {
        int value = data[offset] << 8 | data[offset + 1];
        return value < 32768 ? value : value - 65536;
    }

    static int ReadInt32(byte[] data, int offset)
    {
        return CheckedFontMath.ReinterpretUInt32AsInt32(
            ReadUInt32(data, offset));
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset] << 24 |
            (uint)data[offset + 1] << 16 |
            (uint)data[offset + 2] << 8 |
            data[offset + 3];
    }
}
