namespace fix;

internal class QueuedSessionMessage
{
    internal FixMessage Message = new FixMessage();
    internal int SequenceNumber;
    internal bool PossibleDuplicate;
    internal bool Rejected;
}
