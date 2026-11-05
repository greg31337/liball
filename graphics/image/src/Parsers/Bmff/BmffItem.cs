using System.Collections.Generic;

namespace image;

internal class BmffItem
{
    internal uint Id;
    internal string Type;
    internal string Name;
    internal string ContentType;
    internal string ContentEncoding;
    internal bool Hidden;
    internal int ProtectionIndex;
    internal bool Primary;
    internal bool Thumbnail;
    internal bool Auxiliary;
    internal string AuxiliaryType;
    internal int Width;
    internal int Height;
    internal int ChannelCount;
    internal int BitDepth;
    internal int RotationDegrees;
    internal int MirrorAxis;
    internal string ColorType;
    internal int ColorPrimaries;
    internal int TransferCharacteristics;
    internal int MatrixCoefficients;
    internal bool FullRange;
    internal List<BmffExtent> Extents;
    internal List<BmffPropertyAssociation> Properties;

    internal BmffItem()
    {
        Id = 0;
        Type = "";
        Name = "";
        ContentType = "";
        ContentEncoding = "";
        Hidden = false;
        ProtectionIndex = 0;
        Primary = false;
        Thumbnail = false;
        Auxiliary = false;
        AuxiliaryType = "";
        Width = 0;
        Height = 0;
        ChannelCount = 0;
        BitDepth = 0;
        RotationDegrees = 0;
        MirrorAxis = -1;
        ColorType = "";
        ColorPrimaries = 0;
        TransferCharacteristics = 0;
        MatrixCoefficients = 0;
        FullRange = false;
        Extents = new List<BmffExtent>();
        Properties = new List<BmffPropertyAssociation>();
    }
}

internal class BmffExtent
{
    internal ulong Offset;
    internal ulong Length;
    internal ulong Index;
    internal int ConstructionMethod;
    internal int DataReferenceIndex;
    internal bool Resolved;
    internal long AbsoluteOffset;

    internal BmffExtent()
    {
        Offset = 0;
        Length = 0;
        Index = 0;
        ConstructionMethod = 0;
        DataReferenceIndex = 0;
        Resolved = false;
        AbsoluteOffset = -1;
    }
}

internal class BmffPropertyAssociation
{
    internal int Index;
    internal bool Essential;

    internal BmffPropertyAssociation()
    {
        Index = 0;
        Essential = false;
    }
}
