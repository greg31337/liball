using System.Net;

namespace tftp;

internal class TftpPeerTracker
{
    internal IPAddress ExpectedAddress;
    internal int TransferPort;
    internal bool IsTransferPortLocked;

    internal TftpPeerTracker(
        IPAddress expectedAddress,
        int transferPort,
        bool isTransferPortLocked)
    {
        if (expectedAddress == null)
        {
            throw new TftpProtocolException(
                "The expected peer address cannot be null.",
                false,
                0);
        }

        if (isTransferPortLocked && (transferPort < 1 || transferPort > 65535))
        {
            throw new TftpProtocolException(
                "A locked transfer port must be within the UDP port range.",
                false,
                0);
        }

        ExpectedAddress = expectedAddress;
        TransferPort = transferPort;
        IsTransferPortLocked = isTransferPortLocked;
    }

    internal TftpPeerValidation ClassifyPeer(IPEndPoint peerEndPoint)
    {
        if (peerEndPoint == null || !ExpectedAddress.Equals(peerEndPoint.Address))
        {
            return TftpPeerValidation.UnexpectedAddress;
        }

        if (peerEndPoint.Port < 1)
        {
            return TftpPeerValidation.UnknownTransferId;
        }

        if (!IsTransferPortLocked)
        {
            return TftpPeerValidation.Candidate;
        }

        if (peerEndPoint.Port == TransferPort)
        {
            return TftpPeerValidation.Expected;
        }

        return TftpPeerValidation.UnknownTransferId;
    }

    internal bool LockTransferEndpoint(IPEndPoint peerEndPoint)
    {
        TftpPeerValidation validation = ClassifyPeer(peerEndPoint);
        if (validation == TftpPeerValidation.Expected)
        {
            return true;
        }

        if (validation != TftpPeerValidation.Candidate)
        {
            return false;
        }

        TransferPort = peerEndPoint.Port;
        IsTransferPortLocked = true;
        return true;
    }
}
