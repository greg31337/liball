namespace ttf;

internal static class Woff2TransformReconstructor
{
    internal static Woff2Reconstruction Reconstruct(
        Woff2Font font,
        FontLoadOptions options)
    {
        if (font == null)
        {
            throw new System.ArgumentNullException("font");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();

        if (!HasTransformedTable(font))
        {
            return Woff2NullTransformReconstructor.Reconstruct(font, options);
        }

        byte[][] resolvedData = new byte[font.GetTableCount()][];
        ResolveNullTables(font, resolvedData);
        ResolveGlyfLocaTables(font, resolvedData, options);
        ResolveHmtxTables(font, resolvedData, options);
        EnsureAllResolved(font, resolvedData);
        Woff2Font resolvedFont = CreateResolvedFont(font, resolvedData, options.GetLimits());
        return Woff2NullTransformReconstructor.Reconstruct(resolvedFont, options);
    }

    static bool HasTransformedTable(Woff2Font font)
    {
        int index = 0;

        while (index < font.GetTableCount())
        {
            if (font.GetTable(index).IsTransformed())
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void ResolveNullTables(Woff2Font font, byte[][] resolvedData)
    {
        int index = 0;

        while (index < font.GetTableCount())
        {
            if (!font.GetTable(index).IsTransformed())
            {
                resolvedData[index] = font.GetTable(index).CopyData();
            }

            index += 1;
        }
    }

    static void ResolveGlyfLocaTables(
        Woff2Font font,
        byte[][] resolvedData,
        FontLoadOptions options)
    {
        int glyfIndex = 0;

        while (glyfIndex < font.GetTableCount())
        {
            Woff2Table glyfTable = font.GetTable(glyfIndex);

            if (glyfTable.GetTag() == FontTags.Glyf && glyfTable.IsTransformed())
            {
                int locaIndex = FindPairedLocaIndex(font, glyfIndex);
                Woff2GlyfReconstruction reconstruction = Woff2GlyfTransform.Reconstruct(
                    glyfTable,
                    font.GetTable(locaIndex),
                    options);
                resolvedData[glyfIndex] = reconstruction.CopyGlyfData();
                resolvedData[locaIndex] = reconstruction.CopyLocaData();
            }

            glyfIndex += 1;
        }
    }

    static int FindPairedLocaIndex(Woff2Font font, int glyfIndex)
    {
        if (font.GetCollection().IsPresent())
        {
            if (glyfIndex + 1 >= font.GetTableCount() ||
                font.GetTable(glyfIndex + 1).GetTag() != FontTags.Loca)
            {
                throw FontFormatException.Create("A transformed collection glyf table lacks its paired loca table.");
            }

            return glyfIndex + 1;
        }

        int index = 0;
        int result = -1;

        while (index < font.GetTableCount())
        {
            if (font.GetTable(index).GetTag() == FontTags.Loca)
            {
                if (result >= 0)
                {
                    throw FontFormatException.Create("A scalar WOFF 2 font contains duplicate loca tables.");
                }

                result = index;
            }

            index += 1;
        }

        if (result < 0)
        {
            throw FontFormatException.Create("A transformed glyf table lacks its paired loca table.");
        }

        return result;
    }

    static void ResolveHmtxTables(
        Woff2Font font,
        byte[][] resolvedData,
        FontLoadOptions options)
    {
        int tableIndex = 0;

        while (tableIndex < font.GetTableCount())
        {
            Woff2Table table = font.GetTable(tableIndex);

            if (table.GetTag() == FontTags.Hmtx && table.IsTransformed())
            {
                resolvedData[tableIndex] = ResolveHmtxTable(
                    font,
                    tableIndex,
                    options);
            }

            tableIndex += 1;
        }
    }

    static byte[] ResolveHmtxTable(
        Woff2Font font,
        int hmtxTableIndex,
        FontLoadOptions options)
    {
        if (!font.GetCollection().IsPresent())
        {
            return Woff2HmtxTransform.Reconstruct(font, options).CopyData();
        }

        byte[]? result = null;
        int faceIndex = 0;

        while (faceIndex < font.GetCollection().GetFaceCount())
        {
            Woff2CollectionFace face = font.GetCollection().GetFace(faceIndex);

            if (FaceContainsTable(face, hmtxTableIndex))
            {
                byte[] faceData = Woff2HmtxTransform.Reconstruct(
                    font,
                    faceIndex,
                    options).CopyData();

                if (result == null)
                {
                    result = faceData;
                }
                else if (!BytesEqual(result, faceData))
                {
                    throw FontFormatException.Create(
                        "A shared transformed WOFF 2 hmtx table has inconsistent face dependencies.");
                }
            }

            faceIndex += 1;
        }

        if (result == null)
        {
            throw FontFormatException.Create("A transformed WOFF 2 hmtx table is not referenced by a face.");
        }

        return result;
    }

    static bool FaceContainsTable(Woff2CollectionFace face, int tableIndex)
    {
        int index = 0;

        while (index < face.GetTableCount())
        {
            if (face.GetTableIndex(index) == tableIndex)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static bool BytesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;

        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static void EnsureAllResolved(Woff2Font font, byte[][] resolvedData)
    {
        int index = 0;

        while (index < resolvedData.Length)
        {
            if (resolvedData[index] == null)
            {
                throw FontFormatException.Create(
                    $"The transformed WOFF 2 table at directory index {font.GetTable(index).GetDirectoryIndex()} could not be reconstructed.");
            }

            index += 1;
        }
    }

    static Woff2Font CreateResolvedFont(
        Woff2Font font,
        byte[][] resolvedData,
        FontLimits limits)
    {
        BigEndianWriter dataWriter = BigEndianWriter.Create(limits.GetMaxOutputBytes());
        int[] offsets = new int[resolvedData.Length];
        int index = 0;

        while (index < resolvedData.Length)
        {
            offsets[index] = dataWriter.GetLength();
            dataWriter.WriteBytes(resolvedData[index]);
            index += 1;
        }

        FontData data = FontData.Create(dataWriter.CopyBytes());
        Woff2Table[] tables = new Woff2Table[resolvedData.Length];
        index = 0;

        while (index < tables.Length)
        {
            Woff2Table source = font.GetTable(index);
            tables[index] = new Woff2Table(
                source.GetDirectoryIndex(),
                source.GetFlags(),
                source.GetTagIndex(),
                source.GetTag(),
                source.GetTransformVersion(),
                source.UsesCustomTag(),
                false,
                resolvedData[index].Length,
                resolvedData[index].Length,
                offsets[index],
                data.GetSlice(offsets[index], resolvedData[index].Length));
            index += 1;
        }

        return new Woff2Font(
            font.GetFlavor(),
            font.GetDeclaredLength(),
            font.GetReservedValue(),
            font.GetTotalSfntSize(),
            font.GetMajorVersion(),
            font.GetMinorVersion(),
            font.GetCompressedDataOffset(),
            font.GetCompressedDataLength(),
            data,
            tables,
            font.GetCollection(),
            font.GetMetadata(),
            font.GetPrivateData());
    }
}
