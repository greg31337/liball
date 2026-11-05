namespace diameter;

internal class CreditControlApplicationPendingRequest
{
    internal long RequestIdentifier;
    internal long OperationIdentifier;
    internal bool Event;
    internal bool Retransmission;
    internal DiameterCreditControlRequest Request =
        new DiameterCreditControlRequest();
}
