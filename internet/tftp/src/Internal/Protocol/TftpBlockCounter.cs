namespace tftp;

internal class TftpBlockCounter
{
    internal ushort CurrentBlock = 1;
    internal ushort PreviousBlock = 0;
    internal bool HasPreviousBlock = false;

    internal ushort AdvanceBlock()
    {
        PreviousBlock = CurrentBlock;
        HasPreviousBlock = true;
        if (CurrentBlock == ushort.MaxValue)
        {
            CurrentBlock = 0;
        }
        else
        {
            CurrentBlock = (ushort)(CurrentBlock + 1);
        }

        return CurrentBlock;
    }

    internal TftpBlockClassification ClassifyDataBlock(ushort blockNumber)
    {
        if (blockNumber == CurrentBlock)
        {
            return TftpBlockClassification.Expected;
        }

        if (HasPreviousBlock && blockNumber == PreviousBlock)
        {
            return TftpBlockClassification.Previous;
        }

        return TftpBlockClassification.Unexpected;
    }

    internal TftpBlockClassification ClassifyAcknowledgementBlock(ushort blockNumber)
    {
        if (blockNumber == CurrentBlock)
        {
            return TftpBlockClassification.Expected;
        }

        if (HasPreviousBlock && blockNumber == PreviousBlock)
        {
            return TftpBlockClassification.Previous;
        }

        if (!HasPreviousBlock && CurrentBlock == 1 && blockNumber == 0)
        {
            return TftpBlockClassification.HandshakeAcknowledgement;
        }

        return TftpBlockClassification.Unexpected;
    }
}
