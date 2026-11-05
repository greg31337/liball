using System;
using System.Collections.Generic;

namespace diameter;

internal class PeerConnectionElectionResult
{
    internal bool Elected;
    internal bool LocalWon;
    internal bool DuplicateDisposed;
    internal int OriginHostComparison;
    internal DiameterConnectionDirection SurvivingDirection;
    internal IDiameterConnection? SurvivingConnection;
    internal List<IDiameterConnection> DiscardedConnections =
        new List<IDiameterConnection>();
    internal List<DiameterTransportOperationResult> DisposalResults =
        new List<DiameterTransportOperationResult>();
    internal string Code = string.Empty;
}

internal class PeerConnectionElection
{
    internal PeerConnectionElection()
    {
    }

    internal PeerConnectionElectionResult ElectPeerConnection(
        DiameterIdentity localOriginHost,
        DiameterIdentity remoteOriginHost,
        IDiameterConnection outboundConnection,
        IDiameterConnection inboundConnection)
    {
        if (outboundConnection == null || inboundConnection == null ||
            object.ReferenceEquals(outboundConnection, inboundConnection))
        {
            throw new ArgumentException(
                "An election requires distinct outbound and inbound connections.");
        }

        DiameterValueResult local = ValueCodecs.NormalizeDiameterIdentity(
            localOriginHost);
        DiameterValueResult remote = ValueCodecs.NormalizeDiameterIdentity(
            remoteOriginHost);
        if (local.Status != DiameterCodecStatus.Success ||
            remote.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "An election requires valid local and remote Origin-Host identities.");
        }

        PeerConnectionElectionResult result = new PeerConnectionElectionResult();
        result.OriginHostComparison = string.CompareOrdinal(local.Text, remote.Text);
        if (result.OriginHostComparison > 0)
        {
            result.Elected = true;
            result.LocalWon = true;
            result.SurvivingDirection = DiameterConnectionDirection.Inbound;
            result.SurvivingConnection = inboundConnection;
            result.DiscardedConnections.Add(outboundConnection);
            result.Code = "responder-connection-selected";
        }
        else if (result.OriginHostComparison < 0)
        {
            result.Elected = true;
            result.SurvivingDirection = DiameterConnectionDirection.Outbound;
            result.SurvivingConnection = outboundConnection;
            result.DiscardedConnections.Add(inboundConnection);
            result.Code = "initiator-connection-selected";
        }
        else
        {
            result.DiscardedConnections.Add(outboundConnection);
            result.DiscardedConnections.Add(inboundConnection);
            result.Code = "origin-host-identity-collision";
        }

        int index = 0;
        result.DuplicateDisposed = true;
        while (index < result.DiscardedConnections.Count)
        {
            DiameterTransportOperationResult disposal =
                result.DiscardedConnections[index].CloseDiameterConnection();
            result.DisposalResults.Add(CloneElectionOperationResult(disposal));
            if (disposal == null ||
                (disposal.Status != DiameterTransportStatus.Success &&
                disposal.Status != DiameterTransportStatus.Closed))
            {
                result.DuplicateDisposed = false;
            }

            index = index + 1;
        }

        if (!result.DuplicateDisposed)
        {
            result.Code = "duplicate-connection-close-failed";
        }

        return result;
    }

    internal DiameterTransportOperationResult CloneElectionOperationResult(
        DiameterTransportOperationResult? source)
    {
        DiameterTransportOperationResult result =
            new DiameterTransportOperationResult();
        if (source == null)
        {
            result.Status = DiameterTransportStatus.Failed;
            result.Code = "missing-close-result";
            result.Message = "The discarded connection did not return a close result.";
            return result;
        }

        result.Status = source.Status;
        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }
}
