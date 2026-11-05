namespace ttf;

internal static class FontSetFaceIdentity
{
    internal static string Create(FontFace face)
    {
        SfntTableCache cache = face.GetTableCache();
        byte[] encodedValue = new byte[8];

        using (System.Security.Cryptography.IncrementalHash hash =
            System.Security.Cryptography.IncrementalHash.CreateHash(
                new System.Security.Cryptography.HashAlgorithmName("SHA256")))
        {
            WriteUInt32(encodedValue, 0, face.GetScalerType());
            WriteUInt32(encodedValue, 4, (uint)cache.GetRawTableCount());
            hash.AppendData(encodedValue);
            int index = 0;

            while (index < cache.GetRawTableCount())
            {
                SfntTableData table = cache.GetRawTable(index);
                WriteUInt32(
                    encodedValue,
                    0,
                    CheckedFontMath.ReinterpretInt32AsUInt32(table.GetTag()));
                WriteUInt32(encodedValue, 4, (uint)table.GetLength());
                hash.AppendData(encodedValue);
                AppendTableData(hash, table);
                index += 1;
            }

            return System.Convert.ToHexString(hash.GetHashAndReset());
        }
    }

    internal static bool AreEquivalent(FontFace first, FontFace second)
    {
        if (first.GetScalerType() != second.GetScalerType())
        {
            return false;
        }

        SfntTableCache firstCache = first.GetTableCache();
        SfntTableCache secondCache = second.GetTableCache();

        if (firstCache.GetRawTableCount() != secondCache.GetRawTableCount())
        {
            return false;
        }

        int index = 0;

        while (index < firstCache.GetRawTableCount())
        {
            SfntTableData firstTable = firstCache.GetRawTable(index);
            SfntTableData secondTable = secondCache.GetRawTable(index);

            if (firstTable.GetTag() != secondTable.GetTag() ||
                firstTable.GetLength() != secondTable.GetLength() ||
                !HaveEquivalentTableData(firstTable, secondTable))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static void AppendTableData(
        System.Security.Cryptography.IncrementalHash hash,
        SfntTableData table)
    {
        ReadOnlySpan<byte> data = table.GetDataSlice().GetMemory().Span;

        if (table.GetTag() == FontTags.Head && data.Length >= 12)
        {
            hash.AppendData(data.Slice(0, 8));
            hash.AppendData(new byte[4]);
            hash.AppendData(data.Slice(12));
            return;
        }

        hash.AppendData(data);
    }

    static bool HaveEquivalentTableData(SfntTableData first, SfntTableData second)
    {
        ReadOnlySpan<byte> firstData = first.GetDataSlice().GetMemory().Span;
        ReadOnlySpan<byte> secondData = second.GetDataSlice().GetMemory().Span;
        bool ignoreChecksumAdjustment = first.GetTag() == FontTags.Head &&
            firstData.Length >= 12;
        int index = 0;

        while (index < firstData.Length)
        {
            if ((!ignoreChecksumAdjustment || index < 8 || index >= 12) &&
                firstData[index] != secondData[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)((value >> 24) & 0xFFU);
        data[offset + 1] = (byte)((value >> 16) & 0xFFU);
        data[offset + 2] = (byte)((value >> 8) & 0xFFU);
        data[offset + 3] = (byte)(value & 0xFFU);
    }
}
