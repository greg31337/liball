namespace icc;

internal class Jpeg2000BoxRegion
{
    internal int Start;
    internal int End;
    internal int HeaderLength;
    internal int DataOffset;
    internal int DataLength;
    internal uint Type;
    internal bool ExtendsToEnd;

    internal Jpeg2000BoxRegion()
    {
        Start = 0;
        End = 0;
        HeaderLength = 0;
        DataOffset = 0;
        DataLength = 0;
        Type = 0;
        ExtendsToEnd = false;
    }
}

internal class Jpeg2000ProfileEntry
{
    internal Jpeg2000BoxRegion Box;
    internal Jpeg2000BoxRegion[] Containers;
    internal int Method;
    internal int ProfileOffset;
    internal int ProfileLength;

    internal Jpeg2000ProfileEntry()
    {
        Box = new Jpeg2000BoxRegion();
        Containers = new Jpeg2000BoxRegion[0];
        Method = 0;
        ProfileOffset = 0;
        ProfileLength = 0;
    }
}

internal class Jpeg2000ProfileData
{
    internal bool IsValid;
    internal bool IsJpx;
    internal IccOperationStatus Status;
    internal string Message;
    internal int ErrorOffset;
    internal int ErrorLength;
    internal Jpeg2000ProfileEntry[] Profiles;

    internal Jpeg2000ProfileData()
    {
        IsValid = false;
        IsJpx = false;
        Status = IccOperationStatus.InvalidData;
        Message = string.Empty;
        ErrorOffset = 0;
        ErrorLength = 0;
        Profiles = new Jpeg2000ProfileEntry[0];
    }
}

internal class Jpeg2000ParseState
{
    internal System.ReadOnlyMemory<byte> Carrier;
    internal IccResourceLimits Limits;
    internal Jpeg2000ProfileData Result;
    internal System.Collections.Generic.List<Jpeg2000ProfileEntry> Profiles;

    internal Jpeg2000ParseState(System.ReadOnlyMemory<byte> carrier,
        IccResourceLimits limits, Jpeg2000ProfileData result)
    {
        Carrier = carrier;
        Limits = limits;
        Result = result;
        Profiles = new System.Collections.Generic.List<Jpeg2000ProfileEntry>();
    }
}
