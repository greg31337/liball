using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class TcpConnectionResult
{
    internal Socket? Socket;
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
}

internal static class TcpTransportOperations
{
    internal static DnsTransportResult Exchange(
        DnsTransportRequest request,
        DnsTcpTransport transport)
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
        TcpConnectionResult connection = AcquireConnection(
            transport, remoteEndpoint, request, deadline);
        if (!connection.Success || connection.Socket == null)
        {
            if (connection.Cancelled)
            {
                result.Status = DnsTransportStatus.Cancelled;
                result.Cancelled = true;
            }
            else if (connection.TimedOut)
            {
                result.Status = DnsTransportStatus.TimedOut;
                result.TimedOut = true;
            }
            else
            {
                result.Status = DnsTransportStatus.NetworkError;
            }

            return CompleteResult(result);
        }

        Socket socket = connection.Socket;
        TransportCancellationState cancellation =
            new TransportCancellationState();
        cancellation.Request = request;
        byte[] frame = CreateFrame(request.QueryBytes);
        SocketIoResult sent = SocketOperations.SendAll(
            socket,
            frame,
            0,
            frame.Length,
            deadline,
            cancellation);
        if (!sent.Success)
        {
            ApplyIoFailure(result, sent);
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        SocketIoResult prefix = SocketOperations.ReceiveExact(
            socket,
            DnsProtocolLimits.TcpLengthPrefixLength,
            deadline,
            cancellation);
        if (!prefix.Success)
        {
            ApplyFrameIoFailure(result, prefix, "TCP response length prefix");
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        int responseLength = (prefix.Bytes[0] << 8) | prefix.Bytes[1];
        if (responseLength == 0)
        {
            result.Status = DnsTransportStatus.ProtocolError;
            AddFrameDiagnostic(
                result,
                "tcpFrame.messageLength",
                "A DNS-over-TCP frame cannot declare a zero-length message.");
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        if (responseLength > request.Options.MaximumResponseLength)
        {
            result.Status = DnsTransportStatus.ResponseTooLarge;
            AddResponseSizeDiagnostic(result);
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        SocketIoResult payload = SocketOperations.ReceiveExact(
            socket,
            responseLength,
            deadline,
            cancellation);
        if (!payload.Success)
        {
            result.ResponseBytes = payload.Bytes;
            ApplyFrameIoFailure(result, payload, "TCP response payload");
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        result.ResponseBytes = payload.Bytes;
        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        decodeOptions.MaximumMessageLength =
            request.Options.MaximumResponseLength;
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            payload.Bytes, decodeOptions);
        if (!decoded.Success || decoded.Message == null)
        {
            result.Status = DnsTransportStatus.ProtocolError;
            CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        if (!ResponseMatches(query, decoded.Message))
        {
            result.Status = DnsTransportStatus.ProtocolError;
            AddMismatchDiagnostic(result);
            RetireConnection(transport, socket);
            return CompleteResult(result);
        }

        result.Status = DnsTransportStatus.Success;
        result.ResponseMessage = decoded.Message;
        result.Truncated =
            (MessageOperations.EffectiveFlags(decoded.Message.Header) &
             (int)DnsHeaderFlag.Truncated) != 0;
        PreserveConnection(transport, socket, request.Options.ReuseConnections);
        return CompleteResult(result);
    }

    static void ValidateRequest(
        DnsTransportRequest request,
        DnsTcpTransport transport)
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

        if (request.Options.Kind != DnsTransportKind.Tcp)
        {
            throw new ArgumentException(
                "The TCP transport requires TCP transport options.");
        }

        if (request.Options.AttemptTimeoutMilliseconds < 0 ||
            request.Options.IdleConnectionTimeoutMilliseconds < 0 ||
            request.Options.ConnectionLifetimeMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("request.Options");
        }

        if (request.Options.MaximumResponseLength < 1 ||
            request.Options.MaximumResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentOutOfRangeException(
                "request.Options.MaximumResponseLength");
        }

        if (request.QueryBytes.Length < DnsProtocolLimits.HeaderLength ||
            request.QueryBytes.Length >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentException(
                "A complete TCP DNS query is required.");
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
                "The TCP query bytes must contain a valid DNS message.");
        }

        return decoded.Message;
    }

    static DnsTransportResult CreateResult(DnsTransportRequest request)
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Status = DnsTransportStatus.NetworkError;
        result.Transport = DnsTransportKind.Tcp;
        result.Endpoint = CopyEndpoint(request.Endpoint);
        result.StartedAt = TransportTime.ReadClock();
        return result;
    }

    static TcpConnectionResult AcquireConnection(
        DnsTcpTransport transport,
        IPEndPoint remoteEndpoint,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        TcpConnectionResult result = new TcpConnectionResult();
        if (CanReuseConnection(transport, remoteEndpoint, request.Options))
        {
            result.Success = true;
            result.Socket = transport.Connection;
            return result;
        }

        CloseRetainedConnection(transport);
        Socket socket = new Socket(
            remoteEndpoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        Task connectTask = socket.ConnectAsync(remoteEndpoint);
        while (!connectTask.IsCompleted)
        {
            if (transport.Closed || request.CancellationRequested)
            {
                socket.Close();
                result.Cancelled = request.CancellationRequested;
                return result;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                socket.Close();
                result.TimedOut = true;
                return result;
            }

            Thread.Sleep(1);
        }

        if (!connectTask.IsCompletedSuccessfully)
        {
            socket.Close();
            return result;
        }

        DnsClockValue connectedAt = TransportTime.ReadClock();
        transport.Connection = socket;
        transport.ConnectionEndpoint = remoteEndpoint;
        transport.ConnectionCreatedAt = connectedAt;
        transport.ConnectionLastUsedAt = connectedAt;
        result.Success = true;
        result.Socket = socket;
        return result;
    }

    static bool CanReuseConnection(
        DnsTcpTransport transport,
        IPEndPoint remoteEndpoint,
        DnsTransportOptions options)
    {
        if (!options.ReuseConnections || transport.Connection == null ||
            transport.ConnectionEndpoint == null ||
            !EndpointOperations.EndpointsEqual(
                transport.ConnectionEndpoint, remoteEndpoint))
        {
            return false;
        }

        DnsClockValue now = TransportTime.ReadClock();
        long idle = TransportTime.ElapsedMilliseconds(
            transport.ConnectionLastUsedAt, now);
        long lifetime = TransportTime.ElapsedMilliseconds(
            transport.ConnectionCreatedAt, now);
        if (idle >= options.IdleConnectionTimeoutMilliseconds ||
            lifetime >= options.ConnectionLifetimeMilliseconds)
        {
            return false;
        }

        if (transport.Connection.Poll(0, SelectMode.SelectRead) &&
            transport.Connection.Available == 0)
        {
            return false;
        }

        return transport.Connection.Connected;
    }

    static byte[] CreateFrame(byte[] messageBytes)
    {
        byte[] frame = new byte[
            DnsProtocolLimits.TcpLengthPrefixLength + messageBytes.Length];
        frame[0] = (byte)(messageBytes.Length >> 8);
        frame[1] = (byte)(messageBytes.Length & 255);
        Array.Copy(
            messageBytes,
            0,
            frame,
            DnsProtocolLimits.TcpLengthPrefixLength,
            messageBytes.Length);
        return frame;
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

    static void ApplyIoFailure(
        DnsTransportResult result,
        SocketIoResult io)
    {
        if (io.Cancelled)
        {
            result.Status = DnsTransportStatus.Cancelled;
            result.Cancelled = true;
        }
        else if (io.TimedOut)
        {
            result.Status = DnsTransportStatus.TimedOut;
            result.TimedOut = true;
        }
        else
        {
            result.Status = DnsTransportStatus.NetworkError;
        }
    }

    static void ApplyFrameIoFailure(
        DnsTransportResult result,
        SocketIoResult io,
        string field)
    {
        if (io.Cancelled || io.TimedOut)
        {
            ApplyIoFailure(result, io);
            return;
        }

        result.Status = DnsTransportStatus.ProtocolError;
        AddFrameDiagnostic(
            result,
            "tcpFrame",
            $"The {field} ended before all declared bytes were received.");
    }

    static void PreserveConnection(
        DnsTcpTransport transport,
        Socket socket,
        bool reuseConnection)
    {
        if (!reuseConnection)
        {
            RetireConnection(transport, socket);
            return;
        }

        transport.Connection = socket;
        transport.ConnectionLastUsedAt = TransportTime.ReadClock();
    }

    static void RetireConnection(
        DnsTcpTransport transport,
        Socket socket)
    {
        socket.Close();
        if (transport.Connection == socket)
        {
            transport.Connection = null;
            transport.ConnectionEndpoint = null;
        }
    }

    static void CloseRetainedConnection(DnsTcpTransport transport)
    {
        if (transport.Connection != null)
        {
            transport.Connection.Close();
            transport.Connection = null;
            transport.ConnectionEndpoint = null;
        }
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
        result.Address = CopyBytes(source.Address);
        return result;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
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

    static void AddFrameDiagnostic(
        DnsTransportResult result,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidTcpFrame;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void AddResponseSizeDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResourceLimitExceeded;
        diagnostic.Field = "responseBytes";
        diagnostic.Reason =
            "The TCP response exceeds the configured receive limit.";
        result.Diagnostics.Add(diagnostic);
    }

    static void AddMismatchDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
        diagnostic.Field = "response";
        diagnostic.Reason =
            "The TCP response identifier, direction, or question does not match the query.";
        result.Diagnostics.Add(diagnostic);
    }
}
