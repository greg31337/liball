using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tftp;

namespace tftp.Tests;

public class ServerListenerLifecycleTests
{
    [Test]
    public void BindsMultipleDynamicEndpointsAndReturnsIndependentStatusSnapshots()
    {
        TftpServerOptions options = CreateOptions(2);
        options.Bindings.Add(CreateBinding());
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);

        options.Bindings[0].Address = IPAddress.Any;
        lifecycle.BindListeners(CancellationToken.None);
        TftpServerStatus firstStatus = lifecycle.GetStatusSnapshot();
        firstStatus.BoundEndpoints.Clear();
        TftpServerStatus secondStatus = lifecycle.GetStatusSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(secondStatus.IsRunning, Is.True);
            Assert.That(secondStatus.IsDraining, Is.False);
            Assert.That(secondStatus.BoundEndpoints, Has.Count.EqualTo(2));
            Assert.That(secondStatus.BoundEndpoints[0].Address,
                Is.EqualTo(IPAddress.Loopback));
            Assert.That(secondStatus.BoundEndpoints[0].Port, Is.GreaterThan(0));
            Assert.That(secondStatus.BoundEndpoints[1].Port, Is.GreaterThan(0));
            Assert.That(secondStatus.BoundEndpoints[1].Port,
                Is.Not.EqualTo(secondStatus.BoundEndpoints[0].Port));
        });

        lifecycle.Stop(TftpServerStopMode.Immediate);
        TftpServerStatus stoppedStatus = lifecycle.GetStatusSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(stoppedStatus.IsRunning, Is.False);
            Assert.That(stoppedStatus.BoundEndpoints, Is.Empty);
        });
    }

    [Test]
    public void PollsEveryBindingAndReturnsReceivedDatagramDetails()
    {
        TftpServerOptions options = CreateOptions(2);
        options.Bindings.Add(CreateBinding());
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        TftpServerStatus status = lifecycle.GetStatusSnapshot();
        TftpServerBinding destination = status.BoundEndpoints[1];

        using Socket client = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        byte[] request = [0, 1, 102, 0];
        client.SendTo(
            request,
            SocketFlags.None,
            new IPEndPoint(destination.Address, destination.Port));

        TftpListenerPollResult result = lifecycle.PollListeners(
            2000,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasDatagram, Is.True);
            Assert.That(result.Listener, Is.Not.Null);
            Assert.That(result.Datagram, Is.Not.Null);
            Assert.That(result.Listener!.LocalEndPoint.Port,
                Is.EqualTo(destination.Port));
            Assert.That(result.Datagram!.Length, Is.EqualTo(request.Length));
            Assert.That(result.Datagram.RemoteEndPoint,
                Is.EqualTo(client.LocalEndPoint));
            Assert.That(result.Buffer.AsSpan(0, request.Length).ToArray(),
                Is.EqualTo(request));
        });

        lifecycle.Stop(TftpServerStopMode.Immediate);
    }

    [Test]
    public void PollCancellationStopsAccordingToConfiguredMode()
    {
        TftpServerOptions options = CreateOptions(1);
        options.Bindings.Add(CreateBinding());
        options.StopMode = TftpServerStopMode.Immediate;
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.CancelAfter(50);

        Assert.Throws<OperationCanceledException>(() => lifecycle.PollListeners(
            5000,
            cancellation.Token));

        TftpServerStatus status = lifecycle.GetStatusSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(status.IsRunning, Is.False);
            Assert.That(status.BoundEndpoints, Is.Empty);
        });
    }

    [Test]
    public void StopWakesAWaitingPollWithoutWaitingForItsTimeout()
    {
        TftpServerOptions options = CreateOptions(1);
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        ManualResetEventSlim pollStarted = new ManualResetEventSlim(false);
        Task<TftpListenerPollResult> pollTask = Task.Run(() =>
        {
            pollStarted.Set();
            return lifecycle.PollListeners(5000, CancellationToken.None);
        });
        Assert.That(pollStarted.Wait(1000), Is.True);

        lifecycle.Stop(TftpServerStopMode.Graceful);

        Assert.That(pollTask.Wait(1000), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(pollTask.Result.HasDatagram, Is.False);
            Assert.That(lifecycle.GetStatusSnapshot().IsRunning, Is.False);
        });
    }

    [Test]
    public void DetectsDuplicatesAndEnforcesSessionLimitBeforeNewAdmission()
    {
        TftpServerOptions options = CreateOptions(1);
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        IPEndPoint client = new IPEndPoint(IPAddress.Loopback, 41000);
        TftpPacket firstRequest = CreateRequest("first.bin");
        TftpPacket secondRequest = CreateRequest("second.bin");

        TftpSessionAdmissionResult accepted =
            lifecycle.AdmitRequest(client, firstRequest);
        TftpSessionAdmissionResult duplicate =
            lifecycle.AdmitRequest(client, firstRequest);
        TftpSessionAdmissionResult limited =
            lifecycle.AdmitRequest(client, secondRequest);
        bool completed = lifecycle.CompleteTransfer(
            accepted.RequestIdentity,
            true);
        TftpSessionAdmissionResult acceptedAfterCompletion =
            lifecycle.AdmitRequest(client, secondRequest);
        TftpServerStatus status = lifecycle.GetStatusSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Duplicate));
            Assert.That(duplicate.RequestIdentity,
                Is.EqualTo(accepted.RequestIdentity));
            Assert.That(limited.Action,
                Is.EqualTo(TftpSessionAdmissionAction.SessionLimitReached));
            Assert.That(completed, Is.True);
            Assert.That(acceptedAfterCompletion.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(status.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(2));
            Assert.That(status.CompletedTransferCount, Is.EqualTo(1));
            Assert.That(status.FailedTransferCount, Is.Zero);
        });

        lifecycle.Stop(TftpServerStopMode.Immediate);
    }

    [Test]
    public void RequestIdentityIncludesPeerDirectionAndNormalizedWireNames()
    {
        TftpServerOptions options = CreateOptions(5);
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        IPEndPoint firstPeer = new IPEndPoint(IPAddress.Loopback, 41000);
        IPEndPoint secondPeer = new IPEndPoint(IPAddress.Loopback, 41001);
        TftpPacket request = CreateRequest("same.bin");
        TftpOption option = new TftpOption();
        option.Name = "BLKSIZE";
        option.Value = "1024";
        request.Options.Add(option);

        TftpSessionAdmissionResult first = lifecycle.AdmitRequest(
            firstPeer,
            request);
        TftpPacket equivalent = CreateRequest("same.bin");
        equivalent.Mode = "OCTET";
        TftpOption equivalentOption = new TftpOption();
        equivalentOption.Name = "blksize";
        equivalentOption.Value = "1024";
        equivalent.Options.Add(equivalentOption);
        TftpSessionAdmissionResult duplicate = lifecycle.AdmitRequest(
            firstPeer,
            equivalent);
        TftpSessionAdmissionResult otherPeer = lifecycle.AdmitRequest(
            secondPeer,
            equivalent);
        TftpPacket writeRequest = CreateRequest("same.bin");
        writeRequest.Opcode = TftpOpcode.WriteRequest;
        writeRequest.Options.Add(equivalentOption);
        TftpSessionAdmissionResult otherDirection = lifecycle.AdmitRequest(
            firstPeer,
            writeRequest);

        Assert.Multiple(() =>
        {
            Assert.That(first.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Duplicate));
            Assert.That(otherPeer.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(otherDirection.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
        });

        lifecycle.Stop(TftpServerStopMode.Immediate);
    }

    [Test]
    public void GracefulStopDrainsExistingTransfersAndRejectsNewOnes()
    {
        TftpServerOptions options = CreateOptions(2);
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        IPEndPoint client = new IPEndPoint(IPAddress.Loopback, 41000);
        TftpSessionAdmissionResult accepted = lifecycle.AdmitRequest(
            client,
            CreateRequest("active.bin"));

        lifecycle.Stop(TftpServerStopMode.Graceful);
        TftpServerStatus draining = lifecycle.GetStatusSnapshot();
        TftpSessionAdmissionResult rejected = lifecycle.AdmitRequest(
            client,
            CreateRequest("new.bin"));
        TftpSessionAdmissionResult duplicate = lifecycle.AdmitRequest(
            client,
            CreateRequest("active.bin"));
        bool completed = lifecycle.CompleteTransfer(
            accepted.RequestIdentity,
            true);
        TftpServerStatus stopped = lifecycle.GetStatusSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(draining.IsRunning, Is.True);
            Assert.That(draining.IsDraining, Is.True);
            Assert.That(draining.BoundEndpoints, Is.Empty);
            Assert.That(draining.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(rejected.Action,
                Is.EqualTo(TftpSessionAdmissionAction.ServerStopping));
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Duplicate));
            Assert.That(completed, Is.True);
            Assert.That(stopped.IsRunning, Is.False);
            Assert.That(stopped.IsDraining, Is.False);
            Assert.That(stopped.ActiveTransferCount, Is.Zero);
            Assert.That(stopped.CompletedTransferCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ImmediateStopCancelsAndAccountsForEveryActiveTransfer()
    {
        TftpServerOptions options = CreateOptions(2);
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        IPEndPoint client = new IPEndPoint(IPAddress.Loopback, 41000);
        TftpSessionAdmissionResult first = lifecycle.AdmitRequest(
            client,
            CreateRequest("first.bin"));
        lifecycle.AdmitRequest(client, CreateRequest("second.bin"));

        lifecycle.Stop(TftpServerStopMode.Immediate);
        bool lateCompletion = lifecycle.CompleteTransfer(
            first.RequestIdentity,
            true);
        TftpServerStatus status = lifecycle.GetStatusSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(status.IsRunning, Is.False);
            Assert.That(status.IsDraining, Is.False);
            Assert.That(status.ActiveTransferCount, Is.Zero);
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(2));
            Assert.That(status.CompletedTransferCount, Is.Zero);
            Assert.That(status.FailedTransferCount, Is.EqualTo(2));
            Assert.That(lateCompletion, Is.False);
        });
    }

    [Test]
    public void RejectsInvalidAdmissionAndLifecycleSettings()
    {
        TftpServerOptions invalidSessions = CreateOptions(0);
        TftpServerOptions invalidRequestLength = CreateOptions(1);
        invalidRequestLength.MaximumRequestPacketLength = 513;
        TftpServerOptions invalidBinding = CreateOptions(1);
        TftpServerBinding binding = CreateBinding();
        binding.Port = 65536;
        invalidBinding.Bindings.Add(binding);

        Assert.Multiple(() =>
        {
            Assert.That(
                Assert.Throws<TftpProtocolException>(() =>
                    new TftpServerListenerLifecycle(invalidSessions))!.Message,
                Does.Contain("session count"));
            Assert.That(
                Assert.Throws<TftpProtocolException>(() =>
                    new TftpServerListenerLifecycle(invalidRequestLength))!.Message,
                Does.Contain("request packet length"));
            Assert.That(
                Assert.Throws<TftpProtocolException>(() =>
                    new TftpServerListenerLifecycle(invalidBinding))!.Message,
                Does.Contain("port"));
        });

        TftpServerOptions options = CreateOptions(1);
        options.Bindings.Add(CreateBinding());
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        TftpSessionAdmissionResult notListening = lifecycle.AdmitRequest(
            new IPEndPoint(IPAddress.Loopback, 41000),
            CreateRequest("file.bin"));
        TftpPacket data = new TftpPacket();
        data.Opcode = TftpOpcode.Data;
        TftpSessionAdmissionResult invalidPacket = lifecycle.AdmitRequest(
            new IPEndPoint(IPAddress.Loopback, 41000),
            data);

        Assert.Multiple(() =>
        {
            Assert.That(notListening.Action,
                Is.EqualTo(TftpSessionAdmissionAction.ServerStopping));
            Assert.That(invalidPacket.Action,
                Is.EqualTo(TftpSessionAdmissionAction.InvalidRequest));
            Assert.Throws<TftpProtocolException>(() => lifecycle.Stop(
                (TftpServerStopMode)10));
            Assert.Throws<TftpProtocolException>(() => lifecycle.PollListeners(
                -1,
                CancellationToken.None));
        });
    }

    private static TftpServerOptions CreateOptions(int maximumSessions)
    {
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumSessions = maximumSessions;
        return options;
    }

    private static TftpServerBinding CreateBinding()
    {
        TftpServerBinding binding = new TftpServerBinding();
        binding.Address = IPAddress.Loopback;
        binding.Port = 0;
        return binding;
    }

    private static TftpPacket CreateRequest(string filename)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = filename;
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }
}
