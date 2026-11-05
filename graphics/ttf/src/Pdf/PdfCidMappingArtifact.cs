namespace ttf;

internal static class PdfCidMappingBuilder
{
    internal static PdfCidMappingArtifact BuildType2(
        PdfCidAllocation allocation,
        PdfFontOptions options)
    {
        if (allocation == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        int[] glyphIds = allocation.CopyCidToSubsetGlyphIds();
        ValidateGlyphIds(glyphIds);
        bool identity = IsIdentity(glyphIds);
        byte[] mapBytes = identity ?
            new byte[0] : CreateMapBytes(glyphIds);
        int[] embeddedCids = CreateDenseCids(allocation.GetCidCount());
        return CreateArtifact(
            allocation.GetCidCount(),
            glyphIds,
            true,
            identity,
            mapBytes,
            embeddedCids,
            options.GetArchivalProfile());
    }

    internal static PdfCidMappingArtifact BuildType0(
        PdfCidAllocation allocation,
        int[] embeddedCids,
        PdfFontOptions options)
    {
        if (allocation == null || embeddedCids == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ValidateCids(embeddedCids);
        return CreateArtifact(
            allocation.GetCidCount(),
            new int[0],
            false,
            false,
            new byte[0],
            embeddedCids,
            options.GetArchivalProfile());
    }

    internal static int[] CreateDenseCids(int count)
    {
        if (count <= 0 || count > 65536)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        int[] cids = new int[count];
        int index = 0;

        while (index < cids.Length)
        {
            cids[index] = index;
            index += 1;
        }

        return cids;
    }

    static PdfCidMappingArtifact CreateArtifact(
        int cidCount,
        int[] glyphIds,
        bool hasMap,
        bool identity,
        byte[] mapBytes,
        int[] embeddedCids,
        PdfArchivalProfile profile)
    {
        bool hasCidSet = profile == PdfArchivalProfile.PdfA1;
        byte[] cidSetBytes = hasCidSet ?
            CreateCidSet(embeddedCids) : new byte[0];
        return new PdfCidMappingArtifact(
            cidCount,
            glyphIds,
            hasMap,
            identity,
            mapBytes,
            hasCidSet,
            cidSetBytes);
    }

    static bool IsIdentity(int[] glyphIds)
    {
        int cid = 0;

        while (cid < glyphIds.Length)
        {
            if (glyphIds[cid] != cid)
            {
                return false;
            }

            cid += 1;
        }

        return true;
    }

    static byte[] CreateMapBytes(int[] glyphIds)
    {
        byte[] bytes = new byte[glyphIds.Length * 2];
        int cid = 0;

        while (cid < glyphIds.Length)
        {
            int glyphId = glyphIds[cid];
            bytes[cid * 2] = (byte)((glyphId >> 8) & 0xFF);
            bytes[cid * 2 + 1] = (byte)(glyphId & 0xFF);
            cid += 1;
        }

        return bytes;
    }

    static byte[] CreateCidSet(int[] cids)
    {
        int maximum = 0;
        int index = 0;

        while (index < cids.Length)
        {
            maximum = System.Math.Max(maximum, cids[index]);
            index += 1;
        }

        byte[] bytes = new byte[maximum / 8 + 1];
        index = 0;

        while (index < cids.Length)
        {
            int cid = cids[index];
            bytes[cid / 8] = (byte)(bytes[cid / 8] |
                (0x80 >> (cid % 8)));
            index += 1;
        }

        return bytes;
    }

    static void ValidateGlyphIds(int[] glyphIds)
    {
        if (glyphIds.Length == 0 || glyphIds[0] != 0)
        {
            throw new System.ArgumentException(
                "CID-to-GID mappings must reserve glyph zero.",
                "glyphIds");
        }

        int index = 0;

        while (index < glyphIds.Length)
        {
            if (glyphIds[index] < 0 || glyphIds[index] > ushort.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("glyphIds");
            }

            index += 1;
        }
    }

    static void ValidateCids(int[] cids)
    {
        if (cids.Length == 0)
        {
            throw new System.ArgumentException(
                "An embedded CID collection must contain CID zero.",
                "embeddedCids");
        }

        bool containsZero = false;
        System.Collections.Generic.HashSet<int> unique =
            new System.Collections.Generic.HashSet<int>();
        int index = 0;

        while (index < cids.Length)
        {
            int cid = cids[index];

            if (cid < 0 || cid > ushort.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException(
                    "embeddedCids");
            }

            if (!unique.Add(cid))
            {
                throw new System.ArgumentException(
                    "Embedded CIDs must be unique.",
                    "embeddedCids");
            }

            if (cid == 0)
            {
                containsZero = true;
            }

            index += 1;
        }

        if (!containsZero)
        {
            throw new System.ArgumentException(
                "An embedded CID collection must contain CID zero.",
                "embeddedCids");
        }
    }
}
