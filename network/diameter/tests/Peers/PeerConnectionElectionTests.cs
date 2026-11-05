namespace diameter.Tests;

public class PeerConnectionElectionTests
{
    [TestCase(
        "z.example",
        "a.example",
        true,
        DiameterConnectionDirection.Inbound,
        "responder-connection-selected")]
    [TestCase(
        "a.example",
        "z.example",
        false,
        DiameterConnectionDirection.Outbound,
        "initiator-connection-selected")]
    public void ElectionTableSelectsTheRfcSurvivingConnection(
        string localIdentity,
        string peerIdentity,
        bool localWon,
        DiameterConnectionDirection survivingDirection,
        string code)
    {
        PeerConnectionElection election = new PeerConnectionElection();
        ElectionTestConnection outbound = new ElectionTestConnection(
            "outbound",
            DiameterConnectionDirection.Outbound);
        ElectionTestConnection inbound = new ElectionTestConnection(
            "inbound",
            DiameterConnectionDirection.Inbound);

        PeerConnectionElectionResult result = election.ElectPeerConnection(
            CreateIdentity(localIdentity),
            CreateIdentity(peerIdentity),
            outbound,
            inbound);

        IDiameterConnection survivor = survivingDirection ==
            DiameterConnectionDirection.Inbound ? inbound : outbound;
        IDiameterConnection discarded = survivingDirection ==
            DiameterConnectionDirection.Inbound ? outbound : inbound;
        Assert.Multiple(() =>
        {
            Assert.That(result.Elected, Is.True);
            Assert.That(result.LocalWon, Is.EqualTo(localWon));
            Assert.That(result.SurvivingDirection,
                Is.EqualTo(survivingDirection));
            Assert.That(result.SurvivingConnection, Is.SameAs(survivor));
            Assert.That(result.DiscardedConnections,
                Is.EqualTo(new[] { discarded }));
            Assert.That(result.DuplicateDisposed, Is.True);
            Assert.That(result.Code, Is.EqualTo(code));
        });
    }

    [Test]
    public void HigherLocalOriginHostKeepsResponderAndClosesInitiator()
    {
        PeerConnectionElection election = new PeerConnectionElection();
        ElectionTestConnection outbound = new ElectionTestConnection(
            "outbound",
            DiameterConnectionDirection.Outbound);
        ElectionTestConnection inbound = new ElectionTestConnection(
            "inbound",
            DiameterConnectionDirection.Inbound);

        PeerConnectionElectionResult result = election.ElectPeerConnection(
            CreateIdentity("z.example"),
            CreateIdentity("a.example"),
            outbound,
            inbound);
        outbound.CloseCode = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Elected, Is.True);
            Assert.That(result.LocalWon, Is.True);
            Assert.That(result.OriginHostComparison, Is.GreaterThan(0));
            Assert.That(result.SurvivingDirection,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(result.SurvivingConnection, Is.SameAs(inbound));
            Assert.That(result.DiscardedConnections,
                Is.EqualTo(new IDiameterConnection[] { outbound }));
            Assert.That(result.DuplicateDisposed, Is.True);
            Assert.That(result.Code, Is.EqualTo("responder-connection-selected"));
            Assert.That(result.DisposalResults, Has.Count.EqualTo(1));
            Assert.That(result.DisposalResults[0].Code, Is.EqualTo("closed"));
            Assert.That(outbound.CloseCount, Is.EqualTo(1));
            Assert.That(outbound.Closed, Is.True);
            Assert.That(inbound.CloseCount, Is.Zero);
            Assert.That(inbound.Closed, Is.False);
        });
    }

    [Test]
    public void LowerLocalOriginHostKeepsInitiatorAndClosesResponder()
    {
        PeerConnectionElection election = new PeerConnectionElection();
        ElectionTestConnection outbound = new ElectionTestConnection(
            "outbound",
            DiameterConnectionDirection.Outbound);
        ElectionTestConnection inbound = new ElectionTestConnection(
            "inbound",
            DiameterConnectionDirection.Inbound);

        PeerConnectionElectionResult result = election.ElectPeerConnection(
            CreateIdentity("A.Example."),
            CreateIdentity("z.example"),
            outbound,
            inbound);

        Assert.Multiple(() =>
        {
            Assert.That(result.Elected, Is.True);
            Assert.That(result.LocalWon, Is.False);
            Assert.That(result.OriginHostComparison, Is.LessThan(0));
            Assert.That(result.SurvivingDirection,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(result.SurvivingConnection, Is.SameAs(outbound));
            Assert.That(result.DiscardedConnections,
                Is.EqualTo(new IDiameterConnection[] { inbound }));
            Assert.That(result.DuplicateDisposed, Is.True);
            Assert.That(result.Code, Is.EqualTo("initiator-connection-selected"));
            Assert.That(outbound.CloseCount, Is.Zero);
            Assert.That(inbound.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void OppositePeersSelectOppositeDirectionsForTheSameConnection()
    {
        PeerConnectionElection election = new PeerConnectionElection();
        PeerConnectionElectionResult lowerPeer = election.ElectPeerConnection(
            CreateIdentity("a.example"),
            CreateIdentity("z.example"),
            new ElectionTestConnection("a-outbound", DiameterConnectionDirection.Outbound),
            new ElectionTestConnection("a-inbound", DiameterConnectionDirection.Inbound));
        PeerConnectionElectionResult higherPeer = election.ElectPeerConnection(
            CreateIdentity("z.example"),
            CreateIdentity("a.example"),
            new ElectionTestConnection("z-outbound", DiameterConnectionDirection.Outbound),
            new ElectionTestConnection("z-inbound", DiameterConnectionDirection.Inbound));

        Assert.Multiple(() =>
        {
            Assert.That(lowerPeer.SurvivingDirection,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(higherPeer.SurvivingDirection,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(lowerPeer.OriginHostComparison,
                Is.EqualTo(-higherPeer.OriginHostComparison));
        });
    }

    [Test]
    public void EquivalentOriginHostsCloseBothConnectionsWithoutElecting()
    {
        PeerConnectionElection election = new PeerConnectionElection();
        ElectionTestConnection outbound = new ElectionTestConnection(
            "outbound",
            DiameterConnectionDirection.Outbound);
        ElectionTestConnection inbound = new ElectionTestConnection(
            "inbound",
            DiameterConnectionDirection.Inbound);

        PeerConnectionElectionResult result = election.ElectPeerConnection(
            CreateIdentity("NODE.Example."),
            CreateIdentity("node.example"),
            outbound,
            inbound);

        Assert.Multiple(() =>
        {
            Assert.That(result.Elected, Is.False);
            Assert.That(result.OriginHostComparison, Is.Zero);
            Assert.That(result.SurvivingConnection, Is.Null);
            Assert.That(result.DiscardedConnections,
                Is.EqualTo(new IDiameterConnection[] { outbound, inbound }));
            Assert.That(result.DisposalResults, Has.Count.EqualTo(2));
            Assert.That(result.DuplicateDisposed, Is.True);
            Assert.That(result.Code, Is.EqualTo("origin-host-identity-collision"));
            Assert.That(outbound.CloseCount, Is.EqualTo(1));
            Assert.That(inbound.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidInputsDoNotConsumeConnectionOwnership()
    {
        PeerConnectionElection election = new PeerConnectionElection();
        ElectionTestConnection outbound = new ElectionTestConnection(
            "outbound",
            DiameterConnectionDirection.Outbound);
        ElectionTestConnection inbound = new ElectionTestConnection(
            "inbound",
            DiameterConnectionDirection.Inbound);

        Assert.Multiple(() =>
        {
            Assert.That(() => election.ElectPeerConnection(
                CreateIdentity("bad_name.example"),
                CreateIdentity("peer.example"),
                outbound,
                inbound), Throws.ArgumentException);
            Assert.That(() => election.ElectPeerConnection(
                CreateIdentity("local.example"),
                CreateIdentity("peer.example"),
                outbound,
                outbound), Throws.ArgumentException);
            Assert.That(outbound.CloseCount, Is.Zero);
            Assert.That(inbound.CloseCount, Is.Zero);
        });
    }

    [Test]
    public void FailedDuplicateCloseIsReportedWithoutClosingTheWinner()
    {
        PeerConnectionElection election = new PeerConnectionElection();
        ElectionTestConnection outbound = new ElectionTestConnection(
            "outbound",
            DiameterConnectionDirection.Outbound);
        outbound.CloseStatus = DiameterTransportStatus.Failed;
        outbound.CloseCode = "close-failed";
        ElectionTestConnection inbound = new ElectionTestConnection(
            "inbound",
            DiameterConnectionDirection.Inbound);

        PeerConnectionElectionResult result = election.ElectPeerConnection(
            CreateIdentity("z.example"),
            CreateIdentity("a.example"),
            outbound,
            inbound);

        Assert.Multiple(() =>
        {
            Assert.That(result.Elected, Is.True);
            Assert.That(result.SurvivingConnection, Is.SameAs(inbound));
            Assert.That(result.DuplicateDisposed, Is.False);
            Assert.That(result.Code, Is.EqualTo("duplicate-connection-close-failed"));
            Assert.That(result.DisposalResults[0].Status,
                Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(result.DisposalResults[0].Code, Is.EqualTo("close-failed"));
            Assert.That(outbound.CloseCount, Is.EqualTo(1));
            Assert.That(outbound.Closed, Is.False);
            Assert.That(inbound.CloseCount, Is.Zero);
        });
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class ElectionTestConnection : IDiameterConnection
{
    internal string ConnectionId;
    internal DiameterConnectionDirection Direction;
    internal DiameterTransportStatus CloseStatus = DiameterTransportStatus.Success;
    internal string CloseCode = "closed";
    internal int CloseCount;
    internal bool Closed;

    internal ElectionTestConnection(
        string connectionId,
        DiameterConnectionDirection direction)
    {
        ConnectionId = connectionId;
        Direction = direction;
    }

    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        if (Closed)
        {
            result.Status = DiameterTransportStatus.Closed;
        }
        else
        {
            result.TransferredCount = source.Length;
        }

        return result;
    }

    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        if (Closed)
        {
            result.Status = DiameterTransportStatus.Closed;
        }

        return result;
    }

    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = ConnectionId;
        result.Direction = Direction;
        result.Open = !Closed;
        return result;
    }

    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        CloseCount = CloseCount + 1;
        if (CloseStatus == DiameterTransportStatus.Success ||
            CloseStatus == DiameterTransportStatus.Closed)
        {
            Closed = true;
        }

        DiameterTransportOperationResult result =
            new DiameterTransportOperationResult();
        result.Status = CloseStatus;
        result.Code = CloseCode;
        return result;
    }
}
