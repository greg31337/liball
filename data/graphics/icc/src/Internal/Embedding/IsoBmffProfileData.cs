namespace icc;

internal class IsoBmffBoxRegion
{
    internal int Start;
    internal int End;
    internal int HeaderLength;
    internal int DataOffset;
    internal int DataLength;
    internal uint Type;
    internal bool ExtendsToEnd;

    internal IsoBmffBoxRegion()
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

internal class IsoBmffProfileEntry
{
    internal IsoBmffBoxRegion Box;
    internal IsoBmffBoxRegion[] Containers;
    internal uint ColourType;
    internal int ProfileOffset;
    internal int ProfileLength;

    internal IsoBmffProfileEntry()
    {
        Box = new IsoBmffBoxRegion();
        Containers = new IsoBmffBoxRegion[0];
        ColourType = 0;
        ProfileOffset = 0;
        ProfileLength = 0;
    }
}

internal class IsoBmffOffsetReference
{
    internal int Position;
    internal int Width;
    internal long Value;
    internal long TargetOffset;

    internal IsoBmffOffsetReference()
    {
        Position = 0;
        Width = 0;
        Value = 0;
        TargetOffset = 0;
    }
}

internal class IsoBmffProfileData
{
    internal bool IsValid;
    internal IccEmbeddedFormat Format;
    internal IccOperationStatus Status;
    internal string Message;
    internal int ErrorOffset;
    internal int ErrorLength;
    internal IsoBmffProfileEntry[] Profiles;
    internal IsoBmffOffsetReference[] OffsetReferences;

    internal IsoBmffProfileData()
    {
        IsValid = false;
        Format = IccEmbeddedFormat.IsoBmff;
        Status = IccOperationStatus.InvalidData;
        Message = string.Empty;
        ErrorOffset = 0;
        ErrorLength = 0;
        Profiles = new IsoBmffProfileEntry[0];
        OffsetReferences = new IsoBmffOffsetReference[0];
    }
}

internal class IsoBmffParseState
{
    internal System.ReadOnlyMemory<byte> Carrier;
    internal IccResourceLimits Limits;
    internal IsoBmffProfileData Result;
    internal System.Collections.Generic.List<IsoBmffProfileEntry> Profiles;
    internal System.Collections.Generic.List<IsoBmffOffsetReference> Offsets;
    internal bool Failed;

    internal IsoBmffParseState(System.ReadOnlyMemory<byte> carrier,
        IccResourceLimits limits, IsoBmffProfileData result)
    {
        Carrier = carrier;
        Limits = limits;
        Result = result;
        Profiles = new System.Collections.Generic.List<IsoBmffProfileEntry>();
        Offsets = new System.Collections.Generic.List<IsoBmffOffsetReference>();
        Failed = false;
    }
}

internal class IsoBmffParseCursor
{
    internal int Offset;
    internal int End;

    internal IsoBmffParseCursor(int offset, int end)
    {
        Offset = offset;
        End = end;
    }
}
