namespace fix;

internal class SessionOutboundMessage
{
    internal FixMessage Message = new FixMessage();
    internal byte[] Bytes = [];
    internal int Offset;
    internal int SequenceNumber;
    internal bool ConsumeSequence;
    internal bool IsAdministrative;
    internal bool IsPrepared;
    internal bool ConfirmLogoutResponse;
    internal bool DisconnectAfterSend;
}
