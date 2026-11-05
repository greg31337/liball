namespace svg;

internal class SvgCffIndexData
{
    internal int Count;
    internal int[] Starts;
    internal int[] Lengths;
    internal int End;

    internal SvgCffIndexData(int count)
    {
        Count = count;
        Starts = new int[count];
        Lengths = new int[count];
        End = 0;
    }
}

internal class SvgCffDictionaryData
{
    internal int CharStringsOffset;
    internal int PrivateSize;
    internal int PrivateOffset;
    internal int LocalSubrOffset;
    internal int FontDictOffset;
    internal int FontDictSelectOffset;
    internal int VariationStoreOffset;
    internal int CharStringType;
    internal bool Cid;
    internal bool HasPrivate;

    internal SvgCffDictionaryData()
    {
        CharStringsOffset = -1;
        PrivateSize = -1;
        PrivateOffset = -1;
        LocalSubrOffset = -1;
        FontDictOffset = -1;
        FontDictSelectOffset = -1;
        VariationStoreOffset = -1;
        CharStringType = 2;
        Cid = false;
        HasPrivate = false;
    }
}

internal class SvgCffParsedFont
{
    internal bool Cff2;
    internal SvgCffIndexData? CharStrings;
    internal SvgCffIndexData? GlobalSubrs;
    internal SvgCffIndexData? LocalSubrs;
    internal int VariationStoreOffset;

    internal SvgCffParsedFont(bool cff2)
    {
        Cff2 = cff2;
        CharStrings = null;
        GlobalSubrs = null;
        LocalSubrs = null;
        VariationStoreOffset = -1;
    }
}

internal class SvgCffNumber
{
    internal bool Valid;
    internal double Value;
    internal int Next;

    internal SvgCffNumber(bool valid, double value, int next)
    {
        Valid = valid;
        Value = value;
        Next = next;
    }
}

internal class SvgCffDecodeState
{
    internal SvgFont Font;
    internal SvgCffOutlineOptions Options;
    internal SvgOpenTypeTable Table;
    internal int Start;
    internal int End;
    internal SvgCffError Error;
    internal bool RequiresFallback;
    internal SvgCffFallbackReason FallbackReason;
    internal int UnsupportedOperator;
    internal SvgCffParsedFont? Parsed;
    internal System.Collections.Generic.List<double> Stack;
    internal SvgGeneratedPathBuilder Builder;
    internal double X;
    internal double Y;
    internal bool OpenContour;
    internal bool WidthSeen;
    internal bool Ended;
    internal int StemCount;
    internal int PointCount;
    internal int ContourCount;
    internal int SubroutineCalls;
    internal bool[]? ActiveGlobalSubrs;
    internal bool[]? ActiveLocalSubrs;
    internal bool HasHints;
    internal int CurrentVariationDataIndex;

    internal SvgCffDecodeState(SvgFont font, SvgCffOutlineOptions options, SvgOpenTypeTable table)
    {
        Font = font;
        Options = options;
        Table = table;
        Start = table.Offset;
        End = table.Offset + table.Length;
        Error = SvgCffError.None;
        RequiresFallback = false;
        FallbackReason = SvgCffFallbackReason.None;
        UnsupportedOperator = 0;
        Parsed = null;
        Stack = new System.Collections.Generic.List<double>();
        Builder = new SvgGeneratedPathBuilder();
        X = 0;
        Y = 0;
        OpenContour = false;
        WidthSeen = false;
        Ended = false;
        StemCount = 0;
        PointCount = 0;
        ContourCount = 0;
        SubroutineCalls = 0;
        ActiveGlobalSubrs = null;
        ActiveLocalSubrs = null;
        HasHints = false;
        CurrentVariationDataIndex = 0;
    }
}
