namespace xml;

internal class XdmSequenceCursor
{
    internal XdmSequence Sequence;
    internal XdmSequenceOperations Operations;
    internal int Position;
    internal XdmSequenceItem? Current;

    internal XdmSequenceCursor(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        Operations = new XdmSequenceOperations();
        Operations.ValidateXdmSequenceStorage(sequence);
        Sequence = sequence;
        Position = -1;
        Current = null;
    }

    internal bool MoveNextXdmSequenceItem()
    {
        int next = Position + 1;
        if (next >= Sequence.Count)
        {
            Position = Sequence.Count;
            Current = null;
            return false;
        }

        Position = next;
        Current = Operations.ReadValidatedXdmSequenceItem(Sequence, Position);
        return true;
    }

    internal XdmSequenceItem ReadCurrentXdmSequenceItem()
    {
        XdmSequenceItem? item = Current;
        if (item == null)
        {
            throw new System.InvalidOperationException("The XDM sequence cursor is not positioned on an item.");
        }

        return item;
    }
}
