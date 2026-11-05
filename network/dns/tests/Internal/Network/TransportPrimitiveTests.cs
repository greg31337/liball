using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dns.Tests.Internal.Network;

public class TransportPrimitiveTests
{
    [Test]
    public void DeadlineUsesMonotonicTimeOuterLimitAndSaturation()
    {
        DnsClockValue now = Clock(1000, 2000);
        DnsClockValue outer = Clock(1025, 2025);
        DnsClockValue bounded = TransportTime.CreateDeadlineAt(
            now, 100, true, outer);
        DnsClockValue immediate = TransportTime.CreateDeadlineAt(
            now, -10, false, new DnsClockValue());
        DnsClockValue nearMaximum = Clock(long.MaxValue - 2, long.MaxValue - 2);
        DnsClockValue saturated = TransportTime.CreateDeadlineAt(
            nearMaximum, 50, false, new DnsClockValue());

        Assert.Multiple(() =>
        {
            Assert.That(bounded.UnixTimeMilliseconds, Is.EqualTo(1025));
            Assert.That(bounded.MonotonicMilliseconds, Is.EqualTo(2025));
            Assert.That(TransportTime.RemainingMillisecondsAt(bounded, 2015),
                Is.EqualTo(10));
            Assert.That(TransportTime.RemainingMillisecondsAt(bounded, 2025),
                Is.Zero);
            Assert.That(immediate.MonotonicMilliseconds, Is.EqualTo(2000));
            Assert.That(saturated.UnixTimeMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(saturated.MonotonicMilliseconds,
                Is.EqualTo(long.MaxValue));
            Assert.That(TransportTime.ElapsedMilliseconds(now, outer),
                Is.EqualTo(25));
            Assert.That(TransportTime.ElapsedMilliseconds(outer, now),
                Is.Zero);
        });
    }

    [Test]
    public void ClockProvidesWallAndMonotonicReadings()
    {
        long beforeWall = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long beforeMonotonic = Environment.TickCount64;

        DnsClockValue clock = TransportTime.ReadClock();

        Assert.Multiple(() =>
        {
            Assert.That(clock.UnixTimeMilliseconds,
                Is.GreaterThanOrEqualTo(beforeWall));
            Assert.That(clock.MonotonicMilliseconds,
                Is.GreaterThanOrEqualTo(beforeMonotonic));
            Assert.That(clock.UnixTimeMilliseconds,
                Is.LessThanOrEqualTo(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
            Assert.That(clock.MonotonicMilliseconds,
                Is.LessThanOrEqualTo(Environment.TickCount64));
        });
    }

    [Test]
    public void CancellationCombinesRequestFlagAndCancellationToken()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        bool active = TransportCancellation.IsRequested(false, source.Token);
        source.Cancel();
        bool tokenCancelled = TransportCancellation.IsRequested(
            false, source.Token);
        bool flagCancelled = TransportCancellation.IsRequested(
            true, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(active, Is.False);
            Assert.That(tokenCancelled, Is.True);
            Assert.That(flagCancelled, Is.True);
        });
    }

    [Test]
    public void EntropyCreatesBoundedValuesAndRequestedBytes()
    {
        byte[] bytes = TransportEntropy.CreateBytes(32);
        int[] values = new int[64];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = TransportEntropy.NextInt32(7);
            index++;
        }

        ushort identifier = TransportEntropy.NextIdentifier();

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Has.Length.EqualTo(32));
            Assert.That(bytes, Has.Some.Not.Zero);
            Assert.That(values, Has.All.InRange(0, 6));
            Assert.That(identifier, Is.InRange(ushort.MinValue, ushort.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TransportEntropy.NextInt32(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TransportEntropy.CreateBytes(-1));
        });
    }

    [Test]
    public void NumericEndpointsRoundTripWithoutSharingAddressBuffers()
    {
        DnsEndpoint source = new DnsEndpoint();
        source.HasAddress = true;
        source.AddressFamily = 4;
        source.Address = [192, 0, 2, 25];
        source.Port = 5353;

        IPEndPoint endpoint = EndpointOperations.CreateNumeric(source);
        List<IPEndPoint> resolved = EndpointOperations.Resolve(source);
        DnsEndpoint roundTrip = EndpointOperations.ToDnsEndpoint(endpoint);
        source.Address[0] = 203;

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Address, Is.EqualTo(IPAddress.Parse("192.0.2.25")));
            Assert.That(endpoint.Port, Is.EqualTo(5353));
            Assert.That(resolved, Has.Count.EqualTo(1));
            Assert.That(EndpointOperations.EndpointsEqual(
                endpoint, resolved[0]), Is.True);
            Assert.That(roundTrip.HasAddress, Is.True);
            Assert.That(roundTrip.AddressFamily, Is.EqualTo(4));
            Assert.That(roundTrip.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 25 }));
        });
    }

    [Test]
    public void HostEndpointsResolveAndInvalidNumericEndpointsAreRejected()
    {
        DnsEndpoint host = new DnsEndpoint();
        host.Host = "localhost";
        host.Port = 53;
        DnsEndpoint invalid = new DnsEndpoint();
        invalid.HasAddress = true;
        invalid.AddressFamily = 4;
        invalid.Address = [127, 0, 0];

        List<IPEndPoint> resolved = EndpointOperations.Resolve(host);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Empty);
            Assert.That(resolved, Has.All.Matches<IPEndPoint>(
                endpoint => endpoint.Port == 53 &&
                    (endpoint.AddressFamily == AddressFamily.InterNetwork ||
                     endpoint.AddressFamily == AddressFamily.InterNetworkV6)));
            Assert.Throws<ArgumentException>(
                () => EndpointOperations.CreateNumeric(invalid));
        });
    }

    [Test]
    public void SocketPollingReportsTimeoutCancellationAndReadiness()
    {
        ConnectedSocketPair pair = CreateConnectedSockets();
        using (pair.Client)
        using (pair.Server)
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            TransportCancellationState active = Cancellation(
                CancellationToken.None);
            DnsClockValue timeout = TransportTime.CreateDeadline(
                20, false, new DnsClockValue());
            SocketPollResult timedOut = SocketOperations.WaitForReadable(
                pair.Client, timeout, active);
            source.Cancel();
            TransportCancellationState cancelledState = Cancellation(
                source.Token);
            DnsClockValue later = TransportTime.CreateDeadline(
                1000, false, new DnsClockValue());
            SocketPollResult cancelled = SocketOperations.WaitForReadable(
                pair.Client, later, cancelledState);
            pair.Server.Send([1, 2, 3]);
            SocketPollResult ready = SocketOperations.WaitForReadable(
                pair.Client, later, active);

            Assert.Multiple(() =>
            {
                Assert.That(timedOut.TimedOut, Is.True);
                Assert.That(timedOut.Ready, Is.False);
                Assert.That(cancelled.Cancelled, Is.True);
                Assert.That(cancelled.Ready, Is.False);
                Assert.That(ready.Ready, Is.True);
            });
        }
    }

    [Test]
    public void CompleteSocketIoHonorsRangesAndReceivesExactLength()
    {
        ConnectedSocketPair pair = CreateConnectedSockets();
        using (pair.Client)
        using (pair.Server)
        {
            byte[] source = [99, 10, 20, 30, 40, 88];
            DnsClockValue deadline = TransportTime.CreateDeadline(
                1000, false, new DnsClockValue());
            TransportCancellationState cancellation = Cancellation(
                CancellationToken.None);
            SocketIoResult sent = SocketOperations.SendAll(
                pair.Client,
                source,
                1,
                4,
                deadline,
                cancellation);
            SocketIoResult received = SocketOperations.ReceiveExact(
                pair.Server,
                4,
                deadline,
                cancellation);

            Assert.Multiple(() =>
            {
                Assert.That(sent.Success, Is.True);
                Assert.That(sent.BytesProcessed, Is.EqualTo(4));
                Assert.That(received.Success, Is.True);
                Assert.That(received.BytesProcessed, Is.EqualTo(4));
                Assert.That(received.Bytes,
                    Is.EqualTo(new byte[] { 10, 20, 30, 40 }));
            });
        }
    }

    [Test]
    public void ExactReceivePreservesPartialBytesWhenPeerCloses()
    {
        ConnectedSocketPair pair = CreateConnectedSockets();
        using (pair.Client)
        using (pair.Server)
        {
            pair.Server.Send([7, 8]);
            pair.Server.Shutdown(SocketShutdown.Send);
            DnsClockValue deadline = TransportTime.CreateDeadline(
                1000, false, new DnsClockValue());
            TransportCancellationState cancellation = Cancellation(
                CancellationToken.None);

            SocketIoResult result = SocketOperations.ReceiveExact(
                pair.Client,
                4,
                deadline,
                cancellation);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Closed, Is.True);
                Assert.That(result.BytesProcessed, Is.EqualTo(2));
                Assert.That(result.Bytes, Is.EqualTo(new byte[] { 7, 8 }));
            });
        }
    }

    static DnsClockValue Clock(long unix, long monotonic)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = unix;
        result.MonotonicMilliseconds = monotonic;
        return result;
    }

    static TransportCancellationState Cancellation(CancellationToken token)
    {
        TransportCancellationState result = new TransportCancellationState();
        result.CancellationToken = token;
        return result;
    }

    static ConnectedSocketPair CreateConnectedSockets()
    {
        using Socket listener = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;
        Socket client = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        client.Connect(endpoint);
        Socket server = listener.Accept();
        ConnectedSocketPair result = new ConnectedSocketPair();
        result.Client = client;
        result.Server = server;
        return result;
    }

    class ConnectedSocketPair
    {
        public Socket Client = null!;
        public Socket Server = null!;
    }
}
