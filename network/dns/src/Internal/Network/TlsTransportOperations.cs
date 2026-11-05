using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class TlsConnectionResult
{
    internal Socket? Socket;
    internal SslStream? Stream;
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool Closed;
    internal bool AuthenticationFailed;
}

internal static class TlsTransportOperations
{
    internal static DnsTransportResult Exchange(
        DnsTransportRequest request,
        DnsTlsTransport transport)
    {
        ValidateRequest(request, transport);
        DnsEndpoint tlsEndpoint = ApplyDefaultPort(request.Endpoint);
        DnsTransportResult result = CreateResult(tlsEndpoint);
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

        List<IPEndPoint> endpoints = EndpointOperations.Resolve(tlsEndpoint);
        IPEndPoint remoteEndpoint = endpoints[0];
        result.Endpoint = EndpointOperations.ToDnsEndpoint(remoteEndpoint);
        string authenticationName = ReadAuthenticationName(
            request, remoteEndpoint);
        TlsConnectionResult connection = AcquireConnection(
            transport,
            remoteEndpoint,
            authenticationName,
            request,
            deadline);
        if (!connection.Success || connection.Socket == null ||
            connection.Stream == null)
        {
            ApplyConnectionFailure(result, connection);
            return CompleteResult(result);
        }

        Socket socket = connection.Socket;
        SslStream stream = connection.Stream;
        TransportCancellationState cancellation =
            new TransportCancellationState();
        cancellation.Request = request;
        byte[] frame = CreateFrame(request.QueryBytes);
        TlsIoResult sent = TlsStreamOperations.WriteAll(
            stream,
            frame,
            0,
            frame.Length,
            deadline,
            cancellation);
        if (!sent.Success)
        {
            ApplyIoFailure(result, sent);
            RetireConnection(transport, stream, socket);
            return CompleteResult(result);
        }

        TlsIoResult prefix = TlsStreamOperations.ReadExact(
            stream,
            DnsProtocolLimits.TcpLengthPrefixLength,
            deadline,
            cancellation);
        if (!prefix.Success)
        {
            ApplyFrameIoFailure(
                result, prefix, "DNS-over-TLS response length prefix");
            RetireConnection(transport, stream, socket);
            return CompleteResult(result);
        }

        int responseLength = (prefix.Bytes[0] << 8) | prefix.Bytes[1];
        if (responseLength == 0)
        {
            result.Status = DnsTransportStatus.ProtocolError;
            AddFrameDiagnostic(
                result,
                "tlsFrame.messageLength",
                "A DNS-over-TLS frame cannot declare a zero-length message.");
            RetireConnection(transport, stream, socket);
            return CompleteResult(result);
        }

        if (responseLength > request.Options.MaximumResponseLength)
        {
            result.Status = DnsTransportStatus.ResponseTooLarge;
            AddResponseSizeDiagnostic(result);
            RetireConnection(transport, stream, socket);
            return CompleteResult(result);
        }

        TlsIoResult payload = TlsStreamOperations.ReadExact(
            stream,
            responseLength,
            deadline,
            cancellation);
        if (!payload.Success)
        {
            result.ResponseBytes = payload.Bytes;
            ApplyFrameIoFailure(
                result, payload, "DNS-over-TLS response payload");
            RetireConnection(transport, stream, socket);
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
            RetireConnection(transport, stream, socket);
            return CompleteResult(result);
        }

        if (!ResponseMatches(query, decoded.Message))
        {
            result.Status = DnsTransportStatus.ProtocolError;
            AddMismatchDiagnostic(result);
            RetireConnection(transport, stream, socket);
            return CompleteResult(result);
        }

        result.Status = DnsTransportStatus.Success;
        result.ResponseMessage = decoded.Message;
        result.Truncated =
            (MessageOperations.EffectiveFlags(decoded.Message.Header) &
             (int)DnsHeaderFlag.Truncated) != 0;
        bool reuseConnection = request.Options.ReuseConnections &&
            request.Options.Tls.ReuseConnection;
        PreserveConnection(
            transport, stream, socket, reuseConnection);
        return CompleteResult(result);
    }

    internal static DnsEndpoint ApplyDefaultPort(DnsEndpoint endpoint)
    {
        if (endpoint == null)
        {
            throw new ArgumentNullException("endpoint");
        }

        DnsEndpoint result = CopyEndpoint(endpoint);
        if (result.Port == DnsPorts.Dns)
        {
            result.Port = DnsPorts.DnsOverTls;
        }

        return result;
    }

    static void ValidateRequest(
        DnsTransportRequest request,
        DnsTlsTransport transport)
    {
        if (request == null || transport == null)
        {
            throw new ArgumentNullException("request");
        }

        if (request.Endpoint == null || request.Options == null ||
            request.Options.Tls == null || request.QueryBytes == null ||
            request.Deadline == null)
        {
            throw new ArgumentException(
                "A complete DNS transport request is required.");
        }

        if (request.Options.Kind != DnsTransportKind.Tls)
        {
            throw new ArgumentException(
                "The TLS transport requires TLS transport options.");
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
                "A complete DNS-over-TLS query is required.");
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
                "The TLS query bytes must contain a valid DNS message.");
        }

        return decoded.Message;
    }

    static DnsTransportResult CreateResult(DnsEndpoint endpoint)
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Status = DnsTransportStatus.NetworkError;
        result.Transport = DnsTransportKind.Tls;
        result.Endpoint = CopyEndpoint(endpoint);
        result.StartedAt = TransportTime.ReadClock();
        return result;
    }

    internal static string ReadAuthenticationName(
        DnsTransportRequest request,
        IPEndPoint remoteEndpoint)
    {
        string? configured = request.Options.Tls.AuthenticationName;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (!string.IsNullOrWhiteSpace(request.Endpoint.Host))
        {
            return request.Endpoint.Host;
        }

        return remoteEndpoint.Address.ToString();
    }

    static TlsConnectionResult AcquireConnection(
        DnsTlsTransport transport,
        IPEndPoint remoteEndpoint,
        string authenticationName,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        TlsConnectionResult result = new TlsConnectionResult();
        if (CanReuseConnection(
            transport,
            remoteEndpoint,
            authenticationName,
            request.Options))
        {
            result.Success = true;
            result.Socket = transport.Connection;
            result.Stream = transport.Stream;
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
                result.Closed = transport.Closed;
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

        NetworkStream networkStream = new NetworkStream(socket, true);
        SslStream stream;
        if (request.Options.Tls.ValidateCertificate)
        {
            stream = new SslStream(networkStream, false);
        }
        else
        {
            stream = new SslStream(
                networkStream, false, AcceptCertificate);
        }

        SslClientAuthenticationOptions authenticationOptions =
            new SslClientAuthenticationOptions();
        authenticationOptions.TargetHost = authenticationName;
        if (request.Options.Tls.CheckCertificateRevocation)
        {
            authenticationOptions.CertificateRevocationCheckMode =
                X509RevocationMode.Online;
        }
        else
        {
            authenticationOptions.CertificateRevocationCheckMode =
                X509RevocationMode.NoCheck;
        }

        Task authenticationTask = stream.AuthenticateAsClientAsync(
            authenticationOptions, CancellationToken.None);
        while (!authenticationTask.IsCompleted)
        {
            if (transport.Closed || request.CancellationRequested)
            {
                stream.Close();
                result.Cancelled = request.CancellationRequested;
                result.Closed = transport.Closed;
                return result;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                stream.Close();
                result.TimedOut = true;
                return result;
            }

            Thread.Sleep(1);
        }

        if (!authenticationTask.IsCompletedSuccessfully ||
            !stream.IsAuthenticated || !stream.IsEncrypted)
        {
            stream.Close();
            result.AuthenticationFailed = true;
            return result;
        }

        DnsClockValue connectedAt = TransportTime.ReadClock();
        transport.Connection = socket;
        transport.Stream = stream;
        transport.ConnectionEndpoint = remoteEndpoint;
        transport.ConnectionAuthenticationName = authenticationName;
        transport.ConnectionValidated =
            request.Options.Tls.ValidateCertificate;
        transport.ConnectionCheckRevocation =
            request.Options.Tls.CheckCertificateRevocation;
        transport.ConnectionCreatedAt = connectedAt;
        transport.ConnectionLastUsedAt = connectedAt;
        result.Success = true;
        result.Socket = socket;
        result.Stream = stream;
        return result;
    }

    static bool CanReuseConnection(
        DnsTlsTransport transport,
        IPEndPoint remoteEndpoint,
        string authenticationName,
        DnsTransportOptions options)
    {
        if (!options.ReuseConnections || !options.Tls.ReuseConnection ||
            transport.Connection == null || transport.Stream == null ||
            transport.ConnectionEndpoint == null ||
            transport.ConnectionAuthenticationName != authenticationName ||
            transport.ConnectionValidated !=
                options.Tls.ValidateCertificate ||
            transport.ConnectionCheckRevocation !=
                options.Tls.CheckCertificateRevocation ||
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

        return transport.Connection.Connected &&
            transport.Stream.IsAuthenticated;
    }

    static bool AcceptCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        return true;
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

    static void ApplyConnectionFailure(
        DnsTransportResult result,
        TlsConnectionResult connection)
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
        else if (connection.Closed)
        {
            result.Status = DnsTransportStatus.Closed;
        }
        else if (connection.AuthenticationFailed)
        {
            result.Status = DnsTransportStatus.AuthenticationFailed;
        }
        else
        {
            result.Status = DnsTransportStatus.NetworkError;
        }
    }

    static void ApplyIoFailure(
        DnsTransportResult result,
        TlsIoResult io)
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
        TlsIoResult io,
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
            "tlsFrame",
            $"The {field} ended before all declared bytes were received.");
    }

    static void PreserveConnection(
        DnsTlsTransport transport,
        SslStream stream,
        Socket socket,
        bool reuseConnection)
    {
        if (!reuseConnection)
        {
            RetireConnection(transport, stream, socket);
            return;
        }

        transport.Connection = socket;
        transport.Stream = stream;
        transport.ConnectionLastUsedAt = TransportTime.ReadClock();
    }

    static void RetireConnection(
        DnsTlsTransport transport,
        SslStream stream,
        Socket socket)
    {
        stream.Close();
        socket.Close();
        if (transport.Stream == stream)
        {
            transport.Stream = null;
        }

        if (transport.Connection == socket)
        {
            transport.Connection = null;
            transport.ConnectionEndpoint = null;
            transport.ConnectionAuthenticationName = null;
        }
    }

    static void CloseRetainedConnection(DnsTlsTransport transport)
    {
        if (transport.Stream != null)
        {
            transport.Stream.Close();
            transport.Stream = null;
        }

        if (transport.Connection != null)
        {
            transport.Connection.Close();
            transport.Connection = null;
        }

        transport.ConnectionEndpoint = null;
        transport.ConnectionAuthenticationName = null;
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
            "The DNS-over-TLS response exceeds the configured receive limit.";
        result.Diagnostics.Add(diagnostic);
    }

    static void AddMismatchDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
        diagnostic.Field = "response";
        diagnostic.Reason =
            "The DNS-over-TLS response identifier, direction, or question does not match the query.";
        result.Diagnostics.Add(diagnostic);
    }
}
