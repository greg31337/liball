using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dns;

internal class UdpReceiveResult
{
    internal int Length;
    internal IPEndPoint? RemoteEndpoint;
}

internal static class UdpTransportOperations
{
    const int CancellationPollMilliseconds = 50;

    internal static DnsTransportResult Exchange(
        DnsTransportRequest request,
        DnsUdpTransport transport)
    {
        ValidateRequest(request, transport);
        DnsTransportResult result = CreateResult(request);
        if (transport.Closed)
        {
            result.Status = DnsTransportStatus.Closed;
            return CompleteResult(result);
        }

        if (request.CancellationRequested)
        {
            result.Status = DnsTransportStatus.Cancelled;
            result.Cancelled = true;
            return CompleteResult(result);
        }

        DnsMessage query = ReadQuery(request);
        DnsClockValue deadline = TransportTime.CreateDeadline(
            request.Options.AttemptTimeoutMilliseconds,
            request.HasDeadline,
            request.Deadline);
        if (TransportTime.RemainingMilliseconds(deadline) == 0)
        {
            result.Status = DnsTransportStatus.TimedOut;
            result.TimedOut = true;
            return CompleteResult(result);
        }

        List<IPEndPoint> endpoints = EndpointOperations.Resolve(
            request.Endpoint);
        IPEndPoint remoteEndpoint = endpoints[0];
        result.Endpoint = EndpointOperations.ToDnsEndpoint(remoteEndpoint);
        using (Socket socket = new Socket(
            remoteEndpoint.AddressFamily,
            SocketType.Dgram,
            ProtocolType.Udp))
        {
            int sent = SendQuery(
                socket,
                remoteEndpoint,
                request.QueryBytes,
                request.Options.ConnectUdpSocket);
            if (sent != request.QueryBytes.Length)
            {
                result.Status = DnsTransportStatus.NetworkError;
                return CompleteResult(result);
            }

            return ReceiveResponse(
                socket,
                remoteEndpoint,
                query,
                request,
                deadline,
                result);
        }
    }

    static void ValidateRequest(
        DnsTransportRequest request,
        DnsUdpTransport transport)
    {
        if (request == null || transport == null)
        {
            throw new ArgumentNullException("request");
        }

        if (request.Endpoint == null || request.Options == null ||
            request.QueryBytes == null || request.Deadline == null)
        {
            throw new ArgumentException(
                "A complete DNS transport request is required.");
        }

        if (request.Options.Kind != DnsTransportKind.Udp)
        {
            throw new ArgumentException(
                "The UDP transport requires UDP transport options.");
        }

        if (request.Options.AttemptTimeoutMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                "request.Options.AttemptTimeoutMilliseconds");
        }

        if (request.Options.MaximumResponseLength < 1 ||
            request.Options.MaximumResponseLength >
                DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            throw new ArgumentOutOfRangeException(
                "request.Options.MaximumResponseLength");
        }

        if (request.QueryBytes.Length < DnsProtocolLimits.HeaderLength ||
            request.QueryBytes.Length >
                DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            throw new ArgumentException(
                "A complete UDP DNS query is required.");
        }
    }

    static DnsMessage ReadQuery(DnsTransportRequest request)
    {
        if (request.QueryMessage != null)
        {
            return request.QueryMessage;
        }

        DnsDecodeOptions options = new DnsDecodeOptions();
        options.MaximumMessageLength = request.QueryBytes.Length;
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            request.QueryBytes, options);
        if (!decoded.Success || decoded.Message == null)
        {
            throw new ArgumentException(
                "The UDP query bytes must contain a valid DNS message.");
        }

        return decoded.Message;
    }

    static DnsTransportResult CreateResult(DnsTransportRequest request)
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Status = DnsTransportStatus.NetworkError;
        result.Transport = DnsTransportKind.Udp;
        result.Endpoint = CopyEndpoint(request.Endpoint);
        result.StartedAt = TransportTime.ReadClock();
        return result;
    }

    static int SendQuery(
        Socket socket,
        IPEndPoint remoteEndpoint,
        byte[] queryBytes,
        bool connectSocket)
    {
        if (connectSocket)
        {
            socket.Connect(remoteEndpoint);
            return socket.Send(queryBytes, SocketFlags.None);
        }

        return socket.SendTo(
            queryBytes, SocketFlags.None, remoteEndpoint);
    }

    static DnsTransportResult ReceiveResponse(
        Socket socket,
        IPEndPoint remoteEndpoint,
        DnsMessage query,
        DnsTransportRequest request,
        DnsClockValue deadline,
        DnsTransportResult result)
    {
        int bufferLength = request.Options.MaximumResponseLength;
        if (bufferLength < DnsProtocolLimits.MaximumEdnsUdpPayloadSize)
        {
            bufferLength = bufferLength + 1;
        }

        byte[] buffer = new byte[bufferLength];
        TransportCancellationState cancellation =
            new TransportCancellationState();
        cancellation.Request = request;
        while (true)
        {
            if (request.CancellationRequested)
            {
                result.Status = DnsTransportStatus.Cancelled;
                result.Cancelled = true;
                return CompleteResult(result);
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                result.Status = DnsTransportStatus.TimedOut;
                result.TimedOut = true;
                return CompleteResult(result);
            }

            DnsClockValue pollDeadline = TransportTime.CreateDeadline(
                CancellationPollMilliseconds, true, deadline);
            SocketPollResult poll = SocketOperations.WaitForReadable(
                socket,
                pollDeadline,
                cancellation);
            if (!poll.Ready)
            {
                if (poll.Error)
                {
                    result.Status = DnsTransportStatus.NetworkError;
                    return CompleteResult(result);
                }

                continue;
            }

            UdpReceiveResult received = ReceiveDatagram(
                socket,
                buffer,
                remoteEndpoint,
                request.Options.ConnectUdpSocket);
            if (!request.Options.ConnectUdpSocket &&
                !EndpointOperations.EndpointsEqual(
                    received.RemoteEndpoint, remoteEndpoint))
            {
                continue;
            }

            if (!IdentifierMatches(
                buffer, received.Length, query.Header.Identifier))
            {
                continue;
            }

            if (received.Length > request.Options.MaximumResponseLength)
            {
                result.Status = DnsTransportStatus.ResponseTooLarge;
                result.Truncated = true;
                result.ResponseBytes = CopyBytes(buffer, received.Length);
                AddResponseSizeDiagnostic(result);
                return CompleteResult(result);
            }

            byte[] responseBytes = CopyBytes(buffer, received.Length);
            DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
            decodeOptions.MaximumMessageLength =
                request.Options.MaximumResponseLength;
            DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
                responseBytes, decodeOptions);
            if (!decoded.Success || decoded.Message == null)
            {
                result.Status = DnsTransportStatus.ProtocolError;
                result.ResponseBytes = responseBytes;
                CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
                return CompleteResult(result);
            }

            if (!ResponseMatches(query, decoded.Message))
            {
                continue;
            }

            result.Status = DnsTransportStatus.Success;
            result.ResponseBytes = responseBytes;
            result.ResponseMessage = decoded.Message;
            result.Truncated =
                (MessageOperations.EffectiveFlags(decoded.Message.Header) &
                 (int)DnsHeaderFlag.Truncated) != 0;
            return CompleteResult(result);
        }
    }

    static UdpReceiveResult ReceiveDatagram(
        Socket socket,
        byte[] buffer,
        IPEndPoint remoteEndpoint,
        bool connected)
    {
        UdpReceiveResult result = new UdpReceiveResult();
        if (connected)
        {
            result.Length = socket.Receive(buffer, SocketFlags.None);
            result.RemoteEndpoint = remoteEndpoint;
            return result;
        }

        EndPoint anyEndpoint = CreateAnyEndpoint(remoteEndpoint.AddressFamily);
        SocketReceiveFromResult received = socket.ReceiveFromAsync(
            new ArraySegment<byte>(buffer),
            SocketFlags.None,
            anyEndpoint).GetAwaiter().GetResult();
        result.Length = received.ReceivedBytes;
        result.RemoteEndpoint = (IPEndPoint)received.RemoteEndPoint;
        return result;
    }

    static EndPoint CreateAnyEndpoint(AddressFamily addressFamily)
    {
        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            return new IPEndPoint(IPAddress.IPv6Any, 0);
        }

        return new IPEndPoint(IPAddress.Any, 0);
    }

    static bool IdentifierMatches(
        byte[] response,
        int responseLength,
        ushort identifier)
    {
        if (responseLength < 2)
        {
            return true;
        }

        int responseIdentifier = (response[0] << 8) | response[1];
        return responseIdentifier == identifier;
    }

    static bool ResponseMatches(DnsMessage query, DnsMessage response)
    {
        if (response.Header.Identifier != query.Header.Identifier)
        {
            return false;
        }

        int responseFlags = MessageOperations.EffectiveFlags(response.Header);
        if ((responseFlags & (int)DnsHeaderFlag.Response) == 0)
        {
            return false;
        }

        return MessageOperations.QuestionsEqual(
            query.Questions, response.Questions);
    }

    static DnsTransportResult CompleteResult(DnsTransportResult result)
    {
        result.CompletedAt = TransportTime.ReadClock();
        result.ElapsedMilliseconds = TransportTime.ElapsedMilliseconds(
            result.StartedAt, result.CompletedAt);
        return result;
    }

    static DnsEndpoint CopyEndpoint(DnsEndpoint source)
    {
        DnsEndpoint result = new DnsEndpoint();
        result.Host = source.Host;
        result.HasAddress = source.HasAddress;
        result.AddressFamily = source.AddressFamily;
        result.Port = source.Port;
        result.Address = CopyBytes(source.Address, source.Address.Length);
        return result;
    }

    static byte[] CopyBytes(byte[] source, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(source, result, length);
        return result;
    }

    static void CopyDiagnostics(
        List<DnsDiagnostic> source,
        List<DnsDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static void AddResponseSizeDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResourceLimitExceeded;
        diagnostic.Field = "responseBytes";
        diagnostic.Reason =
            "The UDP response exceeds the configured receive limit.";
        result.Diagnostics.Add(diagnostic);
    }
}
