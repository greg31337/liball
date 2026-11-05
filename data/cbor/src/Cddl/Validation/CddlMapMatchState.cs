namespace cbor;

internal class CddlMapMatchState
{
    internal bool[] UsedEntries;
    internal CddlControlResult[] ControlResults = [];

    internal CddlMapMatchState(int entryCount)
    {
        UsedEntries = new bool[entryCount];
    }

}
