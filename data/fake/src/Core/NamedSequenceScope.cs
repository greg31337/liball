namespace fake;

internal class NamedSequenceScope
{
    internal long Start;
    internal long Increment;
    internal SequenceState State;

    internal NamedSequenceScope(
        long start,
        long increment,
        int maximumCount)
    {
        Start = start;
        Increment = increment;
        State = new SequenceState(start, increment, maximumCount);
    }

    internal void RequireConfiguration(
        string name,
        long start,
        long increment)
    {
        if (Start != start || Increment != increment)
            throw FakeException.Create(
                "Named sequence '" + name +
                "' was reused with incompatible configuration.");
    }
}
