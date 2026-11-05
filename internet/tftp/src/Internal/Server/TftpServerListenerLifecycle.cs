using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace tftp;

internal class TftpServerListenerLifecycle
{
    internal TftpServerLifecycleState State;
    internal List<TftpServerBinding> ConfiguredBindings;
    internal int MaximumSessions;
    internal int MaximumRequestPacketLength;
    internal TftpServerStopMode CancellationStopMode;
    internal List<UdpDatagramSocket> ListenerSockets;
    internal Dictionary<string, string> ActiveRequests;
    internal long AcceptedTransferCount;
    internal long CompletedTransferCount;
    internal long FailedTransferCount;
    internal int NextListenerIndex;
    internal object SyncRoot;
    internal ManualResetEventSlim WakeSignal;

    internal TftpServerListenerLifecycle(TftpServerOptions options)
    {
        if (options == null)
        {
            throw new TftpProtocolException(
                "The server options cannot be null.",
                false,
                0);
        }

        ValidateSettings(options);
        State = TftpServerLifecycleState.Stopped;
        ConfiguredBindings = CopyConfiguredBindings(options.Bindings);
        MaximumSessions = options.MaximumSessions;
        MaximumRequestPacketLength = options.MaximumRequestPacketLength;
        CancellationStopMode = options.StopMode;
        ListenerSockets = [];
        ActiveRequests = new Dictionary<string, string>();
        AcceptedTransferCount = 0;
        CompletedTransferCount = 0;
        FailedTransferCount = 0;
        NextListenerIndex = 0;
        SyncRoot = new object();
        WakeSignal = new ManualResetEventSlim(false);
    }

    internal void BindListeners(CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            if (State != TftpServerLifecycleState.Stopped)
            {
                throw new TftpProtocolException(
                    "The server listener is already running.",
                    false,
                    0);
            }

            cancellationToken.ThrowIfCancellationRequested();
            State = TftpServerLifecycleState.Binding;
            WakeSignal.Reset();
            int bindingIndex = 0;
            while (bindingIndex < ConfiguredBindings.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TftpServerBinding binding = ConfiguredBindings[bindingIndex];
                UdpDatagramSocket listener = UdpDatagramSocket.BindLocalSocket(
                    binding.Address,
                    binding.Port,
                    CancellationToken.None);
                ListenerSockets.Add(listener);
                if (cancellationToken.IsCancellationRequested)
                {
                    CloseListenerSockets();
                    State = TftpServerLifecycleState.Stopped;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                bindingIndex += 1;
            }

            State = TftpServerLifecycleState.Listening;
            NextListenerIndex = 0;
        }
    }

    internal TftpListenerPollResult PollListeners(
        long waitMilliseconds,
        CancellationToken cancellationToken)
    {
        if (waitMilliseconds < 0)
        {
            throw new TftpProtocolException(
                "The listener wait duration cannot be negative.",
                false,
                0);
        }

        long startedMilliseconds = Environment.TickCount64;
        long elapsedMilliseconds = 0;
        while (elapsedMilliseconds <= waitMilliseconds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Stop(CancellationStopMode);
                cancellationToken.ThrowIfCancellationRequested();
            }

            lock (SyncRoot)
            {
                if (State != TftpServerLifecycleState.Listening)
                {
                    return new TftpListenerPollResult();
                }

                TftpListenerPollResult readyResult = PollReadyListener(
                    cancellationToken);
                if (readyResult.HasDatagram)
                {
                    return readyResult;
                }
            }

            if (elapsedMilliseconds == waitMilliseconds)
            {
                return new TftpListenerPollResult();
            }

            long remainingMilliseconds = waitMilliseconds - elapsedMilliseconds;
            int waitSliceMilliseconds = 25;
            if (remainingMilliseconds < waitSliceMilliseconds)
            {
                waitSliceMilliseconds = (int)remainingMilliseconds;
            }

            WakeSignal.Wait(waitSliceMilliseconds);
            long currentMilliseconds = Environment.TickCount64;
            if (currentMilliseconds < startedMilliseconds)
            {
                elapsedMilliseconds = 0;
            }
            else
            {
                elapsedMilliseconds = currentMilliseconds - startedMilliseconds;
            }

            if (elapsedMilliseconds > waitMilliseconds)
            {
                elapsedMilliseconds = waitMilliseconds;
            }
        }

        return new TftpListenerPollResult();
    }

    internal TftpSessionAdmissionResult AdmitRequest(
        IPEndPoint clientEndPoint,
        TftpPacket requestPacket)
    {
        if (clientEndPoint == null || requestPacket == null)
        {
            return InvalidAdmissionResult();
        }

        if (requestPacket.Opcode != TftpOpcode.ReadRequest &&
            requestPacket.Opcode != TftpOpcode.WriteRequest)
        {
            return InvalidAdmissionResult();
        }

        if (requestPacket.Filename == null ||
            requestPacket.Mode == null ||
            requestPacket.Options == null)
        {
            return InvalidAdmissionResult();
        }

        int validationOptionIndex = 0;
        while (validationOptionIndex < requestPacket.Options.Count)
        {
            TftpOption validationOption =
                requestPacket.Options[validationOptionIndex];
            if (validationOption == null ||
                validationOption.Name == null ||
                validationOption.Value == null)
            {
                return InvalidAdmissionResult();
            }

            validationOptionIndex += 1;
        }

        string requestIdentity = TftpServerRequestIdentity.CreateIdentity(
            clientEndPoint,
            requestPacket);
        lock (SyncRoot)
        {
            if (ActiveRequests.ContainsKey(requestIdentity))
            {
                TftpSessionAdmissionResult duplicateResult =
                    new TftpSessionAdmissionResult();
                duplicateResult.Action = TftpSessionAdmissionAction.Duplicate;
                duplicateResult.RequestIdentity = requestIdentity;
                return duplicateResult;
            }

            if (State != TftpServerLifecycleState.Listening)
            {
                TftpSessionAdmissionResult stoppingResult =
                    new TftpSessionAdmissionResult();
                stoppingResult.Action = TftpSessionAdmissionAction.ServerStopping;
                stoppingResult.RequestIdentity = requestIdentity;
                return stoppingResult;
            }

            if (ActiveRequests.Count >= MaximumSessions)
            {
                TftpSessionAdmissionResult limitedResult =
                    new TftpSessionAdmissionResult();
                limitedResult.Action = TftpSessionAdmissionAction.SessionLimitReached;
                limitedResult.RequestIdentity = requestIdentity;
                return limitedResult;
            }

            ActiveRequests.Add(requestIdentity, requestIdentity);
            AcceptedTransferCount += 1;
            TftpSessionAdmissionResult acceptedResult =
                new TftpSessionAdmissionResult();
            acceptedResult.Action = TftpSessionAdmissionAction.Accepted;
            acceptedResult.RequestIdentity = requestIdentity;
            return acceptedResult;
        }
    }

    internal bool CompleteTransfer(string requestIdentity, bool succeeded)
    {
        if (requestIdentity == null)
        {
            return false;
        }

        lock (SyncRoot)
        {
            if (!ActiveRequests.ContainsKey(requestIdentity))
            {
                return false;
            }

            ActiveRequests.Remove(requestIdentity);
            if (succeeded)
            {
                CompletedTransferCount += 1;
            }
            else
            {
                FailedTransferCount += 1;
            }

            if (State == TftpServerLifecycleState.Draining &&
                ActiveRequests.Count == 0)
            {
                State = TftpServerLifecycleState.Stopped;
                WakeSignal.Set();
            }

            return true;
        }
    }

    internal void Stop(TftpServerStopMode stopMode)
    {
        ValidateStopMode(stopMode);
        lock (SyncRoot)
        {
            if (State == TftpServerLifecycleState.Stopped)
            {
                return;
            }

            CloseListenerSockets();
            if (stopMode == TftpServerStopMode.Graceful &&
                ActiveRequests.Count > 0)
            {
                State = TftpServerLifecycleState.Draining;
            }
            else
            {
                if (stopMode == TftpServerStopMode.Immediate)
                {
                    FailedTransferCount += ActiveRequests.Count;
                    ActiveRequests.Clear();
                }

                State = TftpServerLifecycleState.Stopped;
            }

            WakeSignal.Set();
        }
    }

    internal TftpServerStatus GetStatusSnapshot()
    {
        lock (SyncRoot)
        {
            TftpServerStatus status = new TftpServerStatus();
            status.IsRunning = State == TftpServerLifecycleState.Listening ||
                State == TftpServerLifecycleState.Draining;
            status.IsDraining = State == TftpServerLifecycleState.Draining;
            status.ActiveTransferCount = ActiveRequests.Count;
            status.AcceptedTransferCount = AcceptedTransferCount;
            status.CompletedTransferCount = CompletedTransferCount;
            status.FailedTransferCount = FailedTransferCount;
            int listenerIndex = 0;
            while (listenerIndex < ListenerSockets.Count)
            {
                TftpServerBinding binding = new TftpServerBinding();
                binding.Address = ListenerSockets[listenerIndex].LocalEndPoint.Address;
                binding.Port = ListenerSockets[listenerIndex].LocalEndPoint.Port;
                status.BoundEndpoints.Add(binding);
                listenerIndex += 1;
            }

            return status;
        }
    }

    internal TftpListenerPollResult PollReadyListener(
        CancellationToken cancellationToken)
    {
        TftpListenerPollResult result = new TftpListenerPollResult();
        int checkedListenerCount = 0;
        while (checkedListenerCount < ListenerSockets.Count)
        {
            int listenerIndex =
                (NextListenerIndex + checkedListenerCount) % ListenerSockets.Count;
            UdpDatagramSocket listener = ListenerSockets[listenerIndex];
            if (listener.Socket.Poll(0, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[MaximumRequestPacketLength + 1];
                UdpDatagramReceiveResult datagram = listener.ReceiveBoundedDatagram(
                    buffer,
                    buffer.Length,
                    cancellationToken);
                NextListenerIndex = (listenerIndex + 1) % ListenerSockets.Count;
                result.HasDatagram = true;
                result.Listener = listener;
                result.Datagram = datagram;
                result.Buffer = buffer;
                return result;
            }

            checkedListenerCount += 1;
        }

        return result;
    }

    internal void CloseListenerSockets()
    {
        int listenerIndex = 0;
        while (listenerIndex < ListenerSockets.Count)
        {
            ListenerSockets[listenerIndex].CloseSocket();
            listenerIndex += 1;
        }

        ListenerSockets.Clear();
        NextListenerIndex = 0;
    }

    internal static List<TftpServerBinding> CopyConfiguredBindings(
        List<TftpServerBinding> sourceBindings)
    {
        List<TftpServerBinding> bindings = [];
        if (sourceBindings.Count == 0)
        {
            TftpServerBinding defaultBinding = new TftpServerBinding();
            bindings.Add(defaultBinding);
            return bindings;
        }

        int bindingIndex = 0;
        while (bindingIndex < sourceBindings.Count)
        {
            TftpServerBinding sourceBinding = sourceBindings[bindingIndex];
            TftpServerBinding copiedBinding = new TftpServerBinding();
            copiedBinding.Address = sourceBinding.Address;
            copiedBinding.Port = sourceBinding.Port;
            bindings.Add(copiedBinding);
            bindingIndex += 1;
        }

        return bindings;
    }

    internal static TftpSessionAdmissionResult InvalidAdmissionResult()
    {
        TftpSessionAdmissionResult result = new TftpSessionAdmissionResult();
        result.Action = TftpSessionAdmissionAction.InvalidRequest;
        return result;
    }

    internal static void ValidateSettings(TftpServerOptions options)
    {
        if (options.Bindings == null)
        {
            throw new TftpProtocolException(
                "The server bindings cannot be null.",
                false,
                0);
        }

        if (options.MaximumSessions < 1)
        {
            throw new TftpProtocolException(
                "The maximum session count must be positive.",
                false,
                0);
        }

        if (options.MaximumRequestPacketLength < 5 ||
            options.MaximumRequestPacketLength >
                TftpProtocolValues.MaximumRequestPacketLength)
        {
            throw new TftpProtocolException(
                "The maximum request packet length must be between 5 and 512 bytes.",
                false,
                0);
        }

        ValidateStopMode(options.StopMode);
        int bindingIndex = 0;
        while (bindingIndex < options.Bindings.Count)
        {
            TftpServerBinding binding = options.Bindings[bindingIndex];
            if (binding == null || binding.Address == null)
            {
                throw new TftpProtocolException(
                    "A server binding address cannot be null.",
                    false,
                    0);
            }

            if (binding.Address.AddressFamily != AddressFamily.InterNetwork &&
                binding.Address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new TftpProtocolException(
                    "A server binding address must use IPv4 or IPv6.",
                    false,
                    0);
            }

            if (binding.Port < 0 || binding.Port > 65535)
            {
                throw new TftpProtocolException(
                    "A server binding port is outside the valid range.",
                    false,
                    0);
            }

            int previousIndex = 0;
            while (previousIndex < bindingIndex)
            {
                TftpServerBinding previousBinding = options.Bindings[previousIndex];
                if (binding.Port != 0 &&
                    previousBinding.Port == binding.Port &&
                    previousBinding.Address.Equals(binding.Address))
                {
                    throw new TftpProtocolException(
                        "The server bindings contain a duplicate endpoint.",
                        false,
                        0);
                }

                previousIndex += 1;
            }

            bindingIndex += 1;
        }
    }

    internal static void ValidateStopMode(TftpServerStopMode stopMode)
    {
        if (stopMode != TftpServerStopMode.Graceful &&
            stopMode != TftpServerStopMode.Immediate)
        {
            throw new TftpProtocolException(
                "The server stop mode is invalid.",
                false,
                0);
        }
    }
}
