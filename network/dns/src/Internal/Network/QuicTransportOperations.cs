using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class QuicProviderResult
{
    internal DnsTransportStatus Status = DnsTransportStatus.NetworkError;
    internal bool TimedOut;
    internal bool Cancelled;
    internal byte[] ResponseBytes = [];
    internal string? DiagnosticReason;
}

internal class QuicWaitResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal DnsTransportStatus FailureStatus = DnsTransportStatus.NetworkError;
}

internal interface IQuicExchangeProvider
{
    bool IsSupported();

    QuicProviderResult Exchange(
        DnsTransportRequest request,
        IPEndPoint endpoint,
        string authenticationName,
        DnsClockValue deadline);
}

internal class BuiltInQuicExchangeProvider : IQuicExchangeProvider
{
    public bool IsSupported()
    {
        return QuicConnection.IsSupported;
    }

    public QuicProviderResult Exchange(
        DnsTransportRequest request,
        IPEndPoint endpoint,
        string authenticationName,
        DnsClockValue deadline)
    {
        QuicProviderResult result = new QuicProviderResult();
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() &&
            !OperatingSystem.IsWindows())
        {
            result.Status = DnsTransportStatus.Unsupported;
            return result;
        }

        CancellationTokenSource cancellation = new CancellationTokenSource();
        SslClientAuthenticationOptions authentication =
            new SslClientAuthenticationOptions();
        authentication.TargetHost = authenticationName;
        authentication.ApplicationProtocols =
            new List<SslApplicationProtocol>();
        authentication.ApplicationProtocols.Add(
            new SslApplicationProtocol("doq"));
        if (!request.Options.Quic.ValidateCertificate)
        {
            authentication.RemoteCertificateValidationCallback =
                AcceptCertificate;
        }

        QuicClientConnectionOptions options =
            new QuicClientConnectionOptions();
        options.RemoteEndPoint = endpoint;
        options.ClientAuthenticationOptions = authentication;
        options.DefaultCloseErrorCode = 0;
        options.DefaultStreamErrorCode = 1;
        options.IdleTimeout = TimeSpan.FromMilliseconds(
            request.Options.Quic.IdleTimeoutMilliseconds);
        options.HandshakeTimeout = TimeSpan.FromMilliseconds(
            TransportTime.RemainingMilliseconds(deadline));
        Task<QuicConnection> connectionTask = QuicConnection.ConnectAsync(
            options, cancellation.Token).AsTask();
        QuicWaitResult connectionWait = WaitForTask(
            connectionTask, request, deadline);
        if (!connectionWait.Success)
        {
            ApplyWaitFailure(result, connectionWait);
            cancellation.Cancel();
            cancellation.Dispose();
            return result;
        }

        QuicConnection connection = connectionTask.Result;
        Task<QuicStream> streamTask = connection.OpenOutboundStreamAsync(
            QuicStreamType.Bidirectional, cancellation.Token).AsTask();
        QuicWaitResult streamWait = WaitForTask(
            streamTask, request, deadline);
        if (!streamWait.Success)
        {
            ApplyWaitFailure(result, streamWait);
            cancellation.Cancel();
            return DisposeProviderResult(
                result, connection, null, cancellation, false);
        }

        QuicStream stream = streamTask.Result;
        QuicFrameResult write = QuicFrameOperations.WriteQuery(
            stream,
            request.QueryBytes,
            request,
            deadline,
            cancellation.Token);
        if (!write.Success)
        {
            ApplyFrameFailure(result, write);
            cancellation.Cancel();
            return DisposeProviderResult(
                result, connection, stream, cancellation, true);
        }

        stream.CompleteWrites();
        QuicFrameResult read = QuicFrameOperations.ReadResponse(
            stream,
            request.Options.MaximumResponseLength,
            request,
            deadline,
            cancellation.Token);
        result.ResponseBytes = read.Bytes;
        if (!read.Success)
        {
            ApplyFrameFailure(result, read);
            cancellation.Cancel();
            return DisposeProviderResult(
                result, connection, stream, cancellation, true);
        }

        result.Status = DnsTransportStatus.Success;
        return DisposeProviderResult(
            result, connection, stream, cancellation, false);
    }

    static QuicWaitResult WaitForTask(
        Task task,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        QuicWaitResult result = new QuicWaitResult();
        while (!task.IsCompleted)
        {
            if (request.CancellationRequested)
            {
                result.Cancelled = true;
                return result;
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                result.TimedOut = true;
                return result;
            }

            Thread.Sleep(1);
        }

        if (!task.IsCompletedSuccessfully)
        {
            result.FailureStatus = QuicFailureOperations.ReadTaskStatus(task);
            result.TimedOut =
                result.FailureStatus == DnsTransportStatus.TimedOut;
            return result;
        }

        result.Success = true;
        return result;
    }

    static void ApplyWaitFailure(
        QuicProviderResult result,
        QuicWaitResult wait)
    {
        result.Cancelled = wait.Cancelled;
        result.TimedOut = wait.TimedOut;
        if (wait.Cancelled)
        {
            result.Status = DnsTransportStatus.Cancelled;
        }
        else if (wait.TimedOut)
        {
            result.Status = DnsTransportStatus.TimedOut;
        }
        else
        {
            result.Status = wait.FailureStatus;
        }
    }

    static void ApplyFrameFailure(
        QuicProviderResult result,
        QuicFrameResult frame)
    {
        result.Cancelled = frame.Cancelled;
        result.TimedOut = frame.TimedOut;
        result.DiagnosticReason = frame.DiagnosticReason;
        if (frame.Cancelled)
        {
            result.Status = DnsTransportStatus.Cancelled;
        }
        else if (frame.TimedOut)
        {
            result.Status = DnsTransportStatus.TimedOut;
        }
        else if (frame.TooLarge)
        {
            result.Status = DnsTransportStatus.ResponseTooLarge;
        }
        else if (frame.ProtocolError)
        {
            result.Status = DnsTransportStatus.ProtocolError;
        }
        else
        {
            result.Status = frame.FailureStatus;
        }
    }

    static QuicProviderResult DisposeProviderResult(
        QuicProviderResult result,
        QuicConnection connection,
        QuicStream? stream,
        CancellationTokenSource cancellation,
        bool abortStream)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() &&
            !OperatingSystem.IsWindows())
        {
            cancellation.Dispose();
            return result;
        }

        if (stream != null)
        {
            if (abortStream)
            {
                long errorCode = 2;
                if (result.Status == DnsTransportStatus.Cancelled ||
                    result.Status == DnsTransportStatus.TimedOut)
                {
                    errorCode = 3;
                }
                else if (result.Status == DnsTransportStatus.NetworkError)
                {
                    errorCode = 1;
                }

                stream.Abort(QuicAbortDirection.Both, errorCode);
            }

            Task streamDisposeTask = stream.DisposeAsync().AsTask();
            WaitForCleanupTask(streamDisposeTask);
        }

        if (!abortStream)
        {
            Task closeTask = connection.CloseAsync(
                0, CancellationToken.None).AsTask();
            WaitForCleanupTask(closeTask);
        }

        Task connectionDisposeTask = connection.DisposeAsync().AsTask();
        WaitForCleanupTask(connectionDisposeTask);
        cancellation.Dispose();
        return result;
    }

    static void WaitForCleanupTask(Task task)
    {
        int waitCount = 0;
        while (!task.IsCompleted && waitCount < 50)
        {
            Thread.Sleep(1);
            waitCount = waitCount + 1;
        }
    }

    static bool AcceptCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        return true;
    }
}

internal static class QuicFailureOperations
{
    internal static DnsTransportStatus ReadTaskStatus(Task task)
    {
        if (task == null)
        {
            throw new ArgumentNullException("task");
        }

        Exception? exception = task.Exception;
        while (exception != null && exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        if (exception == null)
        {
            return DnsTransportStatus.NetworkError;
        }

        string? typeName = exception.GetType().FullName;
        if (typeName == "System.Security.Authentication.AuthenticationException")
        {
            return DnsTransportStatus.AuthenticationFailed;
        }

        if (typeName == "System.Net.Quic.QuicException" &&
            (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ||
             OperatingSystem.IsWindows()))
        {
            QuicException quicException = (QuicException)exception;
            return MapQuicError(quicException.QuicError);
        }

        return DnsTransportStatus.NetworkError;
    }

    internal static DnsTransportStatus MapQuicError(QuicError error)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() &&
            !OperatingSystem.IsWindows())
        {
            return DnsTransportStatus.Unsupported;
        }

        if (error == QuicError.ConnectionIdle ||
            error == QuicError.ConnectionTimeout)
        {
            return DnsTransportStatus.TimedOut;
        }

        if (error == QuicError.ConnectionAborted ||
            error == QuicError.StreamAborted ||
            error == QuicError.TransportError ||
            error == QuicError.VersionNegotiationError)
        {
            return DnsTransportStatus.ProtocolError;
        }

        return DnsTransportStatus.NetworkError;
    }
}

internal static class QuicTransportOperations
{
    internal static DnsTransportResult Exchange(
        DnsTransportRequest request,
        DnsQuicTransport transport)
    {
        ValidateRequest(request, transport);
        DnsEndpoint quicEndpoint = ApplyDefaultPort(request.Endpoint);
        DnsTransportResult result = CreateResult(quicEndpoint);
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

        IQuicExchangeProvider provider;
        if (transport.ExchangeProvider == null)
        {
            provider = new BuiltInQuicExchangeProvider();
        }
        else
        {
            provider = transport.ExchangeProvider;
        }

        if (!provider.IsSupported())
        {
            result.Status = DnsTransportStatus.Unsupported;
            AddUnsupportedDiagnostic(result);
            return CompleteResult(result);
        }

        DnsMessage query = ReadQuery(request);
        List<IPEndPoint> endpoints = EndpointOperations.Resolve(quicEndpoint);
        IPEndPoint remoteEndpoint = endpoints[0];
        result.Endpoint = EndpointOperations.ToDnsEndpoint(remoteEndpoint);
        string authenticationName = ReadAuthenticationName(
            request, remoteEndpoint);
        QuicProviderResult exchange = provider.Exchange(
            request, remoteEndpoint, authenticationName, deadline);
        result.Status = exchange.Status;
        result.ResponseBytes = exchange.ResponseBytes;
        result.TimedOut = exchange.TimedOut;
        result.Cancelled = exchange.Cancelled;
        if (exchange.Status == DnsTransportStatus.ResponseTooLarge)
        {
            AddResponseSizeDiagnostic(result);
            return CompleteResult(result);
        }

        if (exchange.Status == DnsTransportStatus.ProtocolError &&
            exchange.DiagnosticReason != null)
        {
            AddQuicDiagnostic(result, exchange.DiagnosticReason);
            return CompleteResult(result);
        }

        if (exchange.Status != DnsTransportStatus.Success)
        {
            if (exchange.Status == DnsTransportStatus.ProtocolError)
            {
                AddQuicDiagnostic(
                    result,
                    "The peer terminated the DNS-over-QUIC exchange with a protocol error.");
            }

            return CompleteResult(result);
        }

        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        decodeOptions.MaximumMessageLength =
            request.Options.MaximumResponseLength;
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            exchange.ResponseBytes, decodeOptions);
        if (!decoded.Success || decoded.Message == null)
        {
            result.Status = DnsTransportStatus.ProtocolError;
            CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
            return CompleteResult(result);
        }

        if (!ResponseMatches(query, decoded.Message))
        {
            result.Status = DnsTransportStatus.ProtocolError;
            AddMismatchDiagnostic(result);
            return CompleteResult(result);
        }

        result.ResponseMessage = decoded.Message;
        result.Truncated =
            (MessageOperations.EffectiveFlags(decoded.Message.Header) &
             (int)DnsHeaderFlag.Truncated) != 0;
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
            result.Port = DnsPorts.DnsOverQuic;
        }

        return result;
    }

    internal static string ReadAuthenticationName(
        DnsTransportRequest request,
        IPEndPoint remoteEndpoint)
    {
        string? configured = request.Options.Quic.AuthenticationName;
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

    static void ValidateRequest(
        DnsTransportRequest request,
        DnsQuicTransport transport)
    {
        if (request == null || transport == null)
        {
            throw new ArgumentNullException("request");
        }

        if (request.Endpoint == null || request.Options == null ||
            request.Options.Quic == null || request.QueryBytes == null ||
            request.Deadline == null)
        {
            throw new ArgumentException(
                "A complete DNS transport request is required.");
        }

        if (request.Options.Kind != DnsTransportKind.Quic)
        {
            throw new ArgumentException(
                "The QUIC transport requires QUIC transport options.");
        }

        if (request.Options.AttemptTimeoutMilliseconds < 0 ||
            request.Options.Quic.IdleTimeoutMilliseconds < 0)
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
                "A complete DNS-over-QUIC query is required.");
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
                "The QUIC query bytes must contain a valid DNS message.");
        }

        return decoded.Message;
    }

    static DnsTransportResult CreateResult(DnsEndpoint endpoint)
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Status = DnsTransportStatus.NetworkError;
        result.Transport = DnsTransportKind.Quic;
        result.Endpoint = CopyEndpoint(endpoint);
        result.StartedAt = TransportTime.ReadClock();
        return result;
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

    static void AddUnsupportedDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.UnsupportedVersion;
        diagnostic.Field = "transport.quic";
        diagnostic.Reason =
            "DNS over QUIC is not supported by the current platform.";
        result.Diagnostics.Add(diagnostic);
    }

    static void AddResponseSizeDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResourceLimitExceeded;
        diagnostic.Field = "responseBytes";
        diagnostic.Reason =
            "The DNS-over-QUIC response exceeds the configured receive limit.";
        result.Diagnostics.Add(diagnostic);
    }

    static void AddQuicDiagnostic(
        DnsTransportResult result,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
        diagnostic.Field = "quic.stream";
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }

    static void AddMismatchDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
        diagnostic.Field = "response";
        diagnostic.Reason =
            "The DNS-over-QUIC response identifier, direction, or question does not match the query.";
        result.Diagnostics.Add(diagnostic);
    }
}
