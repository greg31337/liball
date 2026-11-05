namespace cbor;

internal class CddlJoinPosition
{
    internal int Offset;
    internal int Components;

    internal CddlJoinPosition(int offset, int components)
    {
        Offset = offset;
        Components = components;
    }
}
