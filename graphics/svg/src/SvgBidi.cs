namespace svg;

internal class SvgBidiStatus
{
    internal int Level;
    internal int Override;
    internal bool Isolate;

    internal SvgBidiStatus(int level, int directionOverride, bool isolate)
    {
        Level = level;
        Override = directionOverride;
        Isolate = isolate;
    }
}

internal class SvgBidiLevelRun
{
    internal System.Collections.Generic.List<int> Indices;
    internal int Previous;
    internal int Next;

    internal SvgBidiLevelRun()
    {
        Indices = new System.Collections.Generic.List<int>();
        Previous = -1;
        Next = -1;
    }
}

internal class SvgBidiBracket
{
    internal int Pair;
    internal bool Open;

    internal SvgBidiBracket(int pair, bool open)
    {
        Pair = pair;
        Open = open;
    }
}

internal class SvgBidiBracketPair
{
    internal int Open;
    internal int Close;

    internal SvgBidiBracketPair(int open, int close)
    {
        Open = open;
        Close = close;
    }
}
