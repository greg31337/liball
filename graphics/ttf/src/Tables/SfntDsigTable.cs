namespace ttf;

internal class SfntDsigSignatureRecord
{
    uint format;
    int blockLength;
    long blockOffset;
    FontDataSlice blockData;
    FontDataSlice? signatureData;

    internal SfntDsigSignatureRecord(
        uint format,
        int blockLength,
        long blockOffset,
        FontDataSlice blockData,
        FontDataSlice? signatureData)
    {
        this.format = format;
        this.blockLength = blockLength;
        this.blockOffset = blockOffset;
        this.blockData = blockData;
        this.signatureData = signatureData;
    }

    internal uint GetFormat() { return format; }
    internal int GetBlockLength() { return blockLength; }
    internal long GetBlockOffset() { return blockOffset; }
    internal bool IsPkcs7Format() { return format == 1; }
    internal byte[] CopyBlockData() { return blockData.CopyBytes(); }
    internal int GetSignatureLength() { return signatureData == null ? 0 : signatureData.GetLength(); }
    internal byte[] CopySignatureData()
    {
        return signatureData == null ? new byte[0] : signatureData.CopyBytes();
    }
}

internal class SfntDsigTable
{
    uint version;
    int flags;
    SfntDsigSignatureRecord[] signatures;
    FontDiagnostic[] diagnostics;

    internal SfntDsigTable(
        uint version,
        int flags,
        SfntDsigSignatureRecord[] signatures,
        FontDiagnostic[] diagnostics)
    {
        this.version = version;
        this.flags = flags;
        this.signatures = signatures;
        this.diagnostics = diagnostics;
    }

    internal uint GetVersion() { return version; }
    internal int GetFlags() { return flags; }
    internal bool PreventsResigning() { return (flags & 1) != 0; }
    internal int GetSignatureCount() { return signatures.Length; }
    internal SfntDsigSignatureRecord GetSignature(int index) { return signatures[index]; }
    internal bool WereSignaturesVerified() { return false; }
    internal int GetDiagnosticCount() { return diagnostics.Length; }
    internal FontDiagnostic GetDiagnostic(int index) { return diagnostics[index]; }
}

internal static class SfntDsigTableParser
{
    const uint CurrentVersion = 1;
    const int HeaderLength = 8;
    const int RecordLength = 12;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Dsig, 1, 1, 0, ReadVersion, Parse);

    internal static SfntTableParser GetParser() { return parser; }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null) { throw new System.ArgumentNullException("cache"); }
        return cache.FindParsedTable(parser);
    }

    internal static SfntDsigTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);
        if (view == null || !view.HasParsedData()) { return null; }
        return (SfntDsigTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        uint version = reader.ReadUInt32();
        return new SfntTableVersion(version > int.MaxValue ? int.MaxValue : (int)version, 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice data = table.GetDataSlice();
        if (data.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The DSIG table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        uint encodedVersion = reader.ReadUInt32();
        int signatureCount = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        if (encodedVersion != CurrentVersion)
        {
            throw FontFormatException.Create("The DSIG table version is not supported.");
        }
        if ((flags & 0xFFFE) != 0)
        {
            throw FontFormatException.Create("The DSIG table uses reserved flags.");
        }
        if (signatureCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create("The DSIG signature count exceeds the configured coverage limit.");
        }

        int recordsLength = CheckedFontMath.MultiplyCount(signatureCount, RecordLength);
        int blocksStart = CheckedFontMath.AddOffset(HeaderLength, recordsLength);
        if (blocksStart > data.GetLength())
        {
            throw FontFormatException.Create("The DSIG signature-record array is truncated.");
        }

        SfntDsigSignatureRecord[] signatures = new SfntDsigSignatureRecord[signatureCount];
        int[] offsets = new int[signatureCount];
        int[] lengths = new int[signatureCount];
        int index = 0;
        while (index < signatureCount)
        {
            uint format = reader.ReadUInt32();
            uint encodedLength = reader.ReadUInt32();
            uint encodedOffset = reader.ReadUInt32();
            if (encodedLength > int.MaxValue || encodedOffset > int.MaxValue)
            {
                throw FontFormatException.Create("A DSIG signature block is too large.");
            }
            int length = (int)encodedLength;
            int offset = (int)encodedOffset;
            if (length == 0 || offset < blocksStart || offset > data.GetLength() - length)
            {
                throw FontFormatException.Create("A DSIG signature block is out of bounds.");
            }

            int prior = 0;
            while (prior < index)
            {
                if (offset < offsets[prior] + lengths[prior] && offsets[prior] < offset + length)
                {
                    throw FontFormatException.Create("DSIG signature blocks overlap.");
                }
                prior += 1;
            }

            FontDataSlice blockData = data.GetSlice(offset, length);
            FontDataSlice? signatureData = null;
            if (format == 1)
            {
                if (length < 8)
                {
                    throw FontFormatException.Create("A DSIG format 1 signature block is truncated.");
                }
                BigEndianReader blockReader = new BigEndianReader(blockData);
                int reserved1 = blockReader.ReadUInt16();
                int reserved2 = blockReader.ReadUInt16();
                uint encodedSignatureLength = blockReader.ReadUInt32();
                if (reserved1 != 0 || reserved2 != 0)
                {
                    throw FontFormatException.Create("A DSIG signature block has nonzero reserved fields.");
                }
                if (encodedSignatureLength != (uint)(length - 8))
                {
                    throw FontFormatException.Create("A DSIG signature length is inconsistent.");
                }
                signatureData = blockData.GetSlice(8, length - 8);
            }

            offsets[index] = offset;
            lengths[index] = length;
            signatures[index] = new SfntDsigSignatureRecord(
                format, length, encodedOffset, blockData, signatureData);
            index += 1;
        }

        FontDiagnostic[] diagnostics = new FontDiagnostic[signatureCount == 0 ? 0 : 1];
        if (signatureCount != 0)
        {
            diagnostics[0] = new FontDiagnostic(
                FontDiagnosticSeverity.Warning,
                FontDiagnosticCode.SignatureNotVerified,
                "Digital signatures were preserved but cryptographic verification was not performed.",
                -1,
                FontTags.Dsig,
                table.GetOffset(),
                "DSIG.signatures");
        }
        return new SfntDsigTable(encodedVersion, flags, signatures, diagnostics);
    }
}
