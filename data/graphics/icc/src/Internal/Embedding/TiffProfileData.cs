namespace icc;

internal class TiffDirectoryData
{
    internal int Offset;
    internal int EntryCount;
    internal int EntriesOffset;
    internal int NextPointerOffset;
    internal ulong NextOffset;
    internal int LinkOffset;

    internal TiffDirectoryData()
    {
        Offset = 0;
        EntryCount = 0;
        EntriesOffset = 0;
        NextPointerOffset = 0;
        NextOffset = 0;
        LinkOffset = 0;
    }
}

internal class TiffProfileEntry
{
    internal int DirectoryIndex;
    internal int EntryOffset;
    internal int ValueFieldOffset;
    internal int DataOffset;
    internal int DataLength;
    internal bool IsInline;

    internal TiffProfileEntry()
    {
        DirectoryIndex = 0;
        EntryOffset = 0;
        ValueFieldOffset = 0;
        DataOffset = 0;
        DataLength = 0;
        IsInline = false;
    }
}

internal class TiffProfileData
{
    internal bool IsValid;
    internal bool IsBigTiff;
    internal bool IsLittleEndian;
    internal bool IsDng;
    internal IccOperationStatus Status;
    internal string Message;
    internal int ErrorOffset;
    internal int ErrorLength;
    internal TiffDirectoryData[] Directories;
    internal TiffProfileEntry[] Profiles;

    internal TiffProfileData()
    {
        IsValid = false;
        IsBigTiff = false;
        IsLittleEndian = false;
        IsDng = false;
        Status = IccOperationStatus.InvalidData;
        Message = string.Empty;
        ErrorOffset = 0;
        ErrorLength = 0;
        Directories = new TiffDirectoryData[0];
        Profiles = new TiffProfileEntry[0];
    }
}
