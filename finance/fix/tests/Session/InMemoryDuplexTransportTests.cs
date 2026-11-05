using fix;

namespace fix.Tests;

[TestFixture]
public class InMemoryDuplexTransportTests
{
    [Test]
    public void ConnectedPeersExchangeBidirectionalFragmentedTraffic()
    {
        InMemoryDuplexTransportPair pair = new InMemoryDuplexTransportPair();
        TransportWriteResult opened = pair.First.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        pair.Second.FragmentNextWrite(1, 2);
        pair.First.FragmentNextWrite(2, 1);

        TransportWriteResult firstWrite = pair.First.WriteTransport([1, 2, 3, 4, 5]);
        byte[][] atSecond = [Read(pair.Second), Read(pair.Second), Read(pair.Second)];
        TransportWriteResult secondWrite = pair.Second.WriteTransport([9, 8, 7, 6]);
        byte[][] atFirst = [Read(pair.First), Read(pair.First), Read(pair.First)];

        Assert.Multiple(() =>
        {
            AssertSuccess(opened, TransportState.Connected, 0);
            AssertSuccess(firstWrite, TransportState.Connected, 5);
            Assert.That(atSecond, Is.EqualTo(new[] { new byte[] { 1, 2 }, new byte[] { 3 }, new byte[] { 4, 5 } }));
            AssertSuccess(secondWrite, TransportState.Connected, 4);
            Assert.That(atFirst, Is.EqualTo(new[] { new byte[] { 9 }, new byte[] { 8, 7 }, new byte[] { 6 } }));
            Assert.That(pair.ConnectionGeneration, Is.EqualTo(1));
        });
    }

    [Test]
    public void CoalescingDelayDropAndDuplicationAreScriptedDeterministically()
    {
        InMemoryDuplexTransportPair pair = ConnectedPair();
        pair.First.CoalesceNextWrites(3);

        pair.First.WriteTransport([1]);
        TransportReadResult heldFirst = pair.Second.ReadTransport(new byte[8]);
        pair.First.WriteTransport([2, 3]);
        TransportReadResult heldSecond = pair.Second.ReadTransport(new byte[8]);
        pair.First.WriteTransport([4]);
        byte[] coalesced = Read(pair.Second);

        pair.First.DelayNextWrite(2);
        pair.First.DropNextWrite();
        pair.First.DuplicateNextWrite(2);
        pair.First.WriteTransport([5, 6]);
        pair.First.WriteTransport([99]);
        pair.First.WriteTransport([7, 8]);
        byte[] duplicateOne = Read(pair.Second);
        byte[] duplicateTwo = Read(pair.Second);
        TransportReadResult beforeDelay = pair.Second.ReadTransport(new byte[8]);
        pair.Advance(1);
        TransportReadResult midway = pair.Second.ReadTransport(new byte[8]);
        pair.Advance(1);
        byte[] delayed = Read(pair.Second);
        TransportReadResult afterAll = pair.Second.ReadTransport(new byte[8]);

        Assert.Multiple(() =>
        {
            AssertWouldBlock(heldFirst);
            AssertWouldBlock(heldSecond);
            Assert.That(coalesced, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(duplicateOne, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(duplicateTwo, Is.EqualTo(new byte[] { 7, 8 }));
            AssertWouldBlock(beforeDelay);
            AssertWouldBlock(midway);
            Assert.That(delayed, Is.EqualTo(new byte[] { 5, 6 }));
            AssertWouldBlock(afterAll);
            Assert.That(pair.CurrentTick, Is.EqualTo(2));
        });
    }

    [Test]
    public void ReorderingIsAvailableOnlyWhenDeliberatelyScripted()
    {
        InMemoryDuplexTransportPair orderedPair = ConnectedPair();
        orderedPair.First.WriteTransport([1]);
        orderedPair.First.WriteTransport([2]);
        byte[][] ordered = [Read(orderedPair.Second), Read(orderedPair.Second)];

        InMemoryDuplexTransportPair reorderedPair = ConnectedPair();
        reorderedPair.First.ReorderNextWrites();
        reorderedPair.First.WriteTransport([1]);
        TransportReadResult held = reorderedPair.Second.ReadTransport(new byte[4]);
        reorderedPair.First.WriteTransport([2]);
        byte[][] reordered = [Read(reorderedPair.Second), Read(reorderedPair.Second)];

        Assert.Multiple(() =>
        {
            Assert.That(ordered, Is.EqualTo(new[] { new byte[] { 1 }, new byte[] { 2 } }));
            AssertWouldBlock(held);
            Assert.That(reordered, Is.EqualTo(new[] { new byte[] { 2 }, new byte[] { 1 } }));
        });
    }

    [Test]
    public void DisconnectClearsInFlightTrafficAndReconnectCreatesAFreshDuplexLink()
    {
        InMemoryDuplexTransportPair pair = ConnectedPair();
        pair.First.DelayNextWrite(3);
        pair.First.WriteTransport([1, 2, 3]);

        pair.Disconnect();
        pair.Advance(3);
        TransportReadResult disconnectedRead = pair.Second.ReadTransport(new byte[8]);
        TransportWriteResult disconnectedWrite = pair.First.WriteTransport([4]);
        pair.Reconnect();
        TransportReadResult staleRead = pair.Second.ReadTransport(new byte[8]);
        TransportWriteResult reconnectedWrite = pair.First.WriteTransport([5, 6]);
        byte[] reconnectedBytes = Read(pair.Second);
        pair.Second.CloseTransport();
        TransportReadResult peerClosed = pair.First.ReadTransport(new byte[8]);

        Assert.Multiple(() =>
        {
            Assert.That(disconnectedRead.State, Is.EqualTo(TransportState.Disconnected));
            Assert.That(disconnectedRead.EndOfStream, Is.True);
            Assert.That(disconnectedWrite.State, Is.EqualTo(TransportState.Disconnected));
            Assert.That(disconnectedWrite.ErrorCode, Is.EqualTo("transport-disconnected"));
            AssertWouldBlock(staleRead);
            AssertSuccess(reconnectedWrite, TransportState.Connected, 2);
            Assert.That(reconnectedBytes, Is.EqualTo(new byte[] { 5, 6 }));
            Assert.That(pair.ConnectionGeneration, Is.EqualTo(2));
            Assert.That(peerClosed.State, Is.EqualTo(TransportState.Disconnected));
            Assert.That(peerClosed.EndOfStream, Is.True);
            Assert.That(pair.Second.State, Is.EqualTo(TransportState.Closed));
        });
    }

    private static InMemoryDuplexTransportPair ConnectedPair()
    {
        InMemoryDuplexTransportPair pair = new InMemoryDuplexTransportPair();
        pair.Connect();
        return pair;
    }

    private static byte[] Read(InMemoryDuplexTransportEndpoint endpoint)
    {
        byte[] destination = new byte[64];
        TransportReadResult result = endpoint.ReadTransport(destination);
        Assert.That(result.State, Is.EqualTo(TransportState.Connected), result.ErrorCode);
        Assert.That(result.WouldBlock, Is.False);
        Assert.That(result.BytesRead, Is.GreaterThan(0));
        return destination.AsSpan(0, result.BytesRead).ToArray();
    }

    private static void AssertSuccess(TransportWriteResult result, TransportState state, int count)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.BytesWritten, Is.EqualTo(count));
        Assert.That(result.WouldBlock, Is.False);
        Assert.That(result.ErrorCode, Is.Empty);
    }

    private static void AssertWouldBlock(TransportReadResult result)
    {
        Assert.That(result.State, Is.EqualTo(TransportState.Connected));
        Assert.That(result.BytesRead, Is.Zero);
        Assert.That(result.WouldBlock, Is.True);
        Assert.That(result.EndOfStream, Is.False);
        Assert.That(result.ErrorCode, Is.Empty);
    }
}
