using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace dns;

internal class DnsUdpServerBinding
{
    internal Socket? Socket;
    internal IPEndPoint? Endpoint;
}

internal static class UdpServerOperations
{
    const int StopPollMilliseconds = 10;

    internal static DnsServerRunResult Run(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        ValidateOptions(server, options, services);
        DnsServerRunResult result = new DnsServerRunResult();
        result.StartedAt = TransportTime.ReadClock();
        result.Status = DnsServerStatus.Running;
        if (IsStopRequested(server, options))
        {
            CompleteCancellation(result);
            return result;
        }

        if (!options.EnableUdp)
        {
            result.Status = DnsServerStatus.Stopped;
            result.CompletedAt = TransportTime.ReadClock();
            return result;
        }

        List<DnsUdpServerBinding> bindings = BindEndpoints(
            options,
            result);
        RunLoop(server, options, services, bindings, result);
        DisposeBindings(bindings);
        result.CompletedAt = TransportTime.ReadClock();
        return result;
    }

    static void ValidateOptions(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services)
    {
        if (server == null || options == null ||
            services == null)
        {
            throw new ArgumentNullException("server");
        }

        if (options.Authoritative == null ||
            options.Authoritative.Authorization == null ||
            options.Authoritative.Security == null ||
            options.Endpoints == null || options.TsigKeys == null)
        {
            throw new ArgumentException(
                "Complete authoritative server options are required.");
        }

        if (options.MaximumRequestLength < DnsProtocolLimits.HeaderLength ||
            options.MaximumRequestLength >
                DnsProtocolLimits.MaximumTcpMessageLength ||
            options.MaximumConcurrentRequests < 1 ||
            options.RequestTimeoutMilliseconds < 0 ||
            options.Authoritative.MaximumUdpResponseLength <
                DnsProtocolLimits.HeaderLength ||
            options.Authoritative.MaximumUdpResponseLength >
                DnsProtocolLimits.MaximumTcpMessageLength)
        {
            throw new ArgumentOutOfRangeException("options");
        }

        if (options.EnableUdp && options.Endpoints.Count == 0)
        {
            throw new ArgumentException(
                "At least one UDP server endpoint is required.");
        }
    }

    static List<DnsUdpServerBinding> BindEndpoints(
        DnsServerOptions options,
        DnsServerRunResult result)
    {
        List<DnsUdpServerBinding> bindings =
            new List<DnsUdpServerBinding>();
        int endpointIndex = 0;
        while (endpointIndex < options.Endpoints.Count)
        {
            List<IPEndPoint> endpoints = EndpointOperations.Resolve(
                options.Endpoints[endpointIndex]);
            int addressIndex = 0;
            while (addressIndex < endpoints.Count)
            {
                IPEndPoint endpoint = endpoints[addressIndex];
                Socket socket = new Socket(
                    endpoint.AddressFamily,
                    SocketType.Dgram,
                    ProtocolType.Udp);
                if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    socket.SetSocketOption(
                        SocketOptionLevel.IPv6,
                        SocketOptionName.IPv6Only,
                        true);
                }

                socket.Bind(endpoint);
                EndPoint? localObject = socket.LocalEndPoint;
                if (localObject == null)
                {
                    socket.Dispose();
                    throw new InvalidOperationException(
                        "The UDP socket did not expose its bound endpoint.");
                }

                IPEndPoint localEndpoint = (IPEndPoint)localObject;
                DnsUdpServerBinding binding = new DnsUdpServerBinding();
                binding.Socket = socket;
                binding.Endpoint = localEndpoint;
                bindings.Add(binding);
                result.BoundEndpoints.Add(
                    EndpointOperations.ToDnsEndpoint(localEndpoint));
                addressIndex = addressIndex + 1;
            }

            endpointIndex = endpointIndex + 1;
        }

        return bindings;
    }

    static void RunLoop(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        List<DnsUdpServerBinding> bindings,
        DnsServerRunResult result)
    {
        while (!IsStopRequested(server, options) &&
            result.Status == DnsServerStatus.Running)
        {
            bool processed = false;
            int bindingIndex = 0;
            while (bindingIndex < bindings.Count &&
                !IsStopRequested(server, options))
            {
                DnsUdpServerBinding binding = bindings[bindingIndex];
                Socket? socket = binding.Socket;
                if (socket != null &&
                    socket.Poll(0, SelectMode.SelectError))
                {
                    result.Status = DnsServerStatus.Failed;
                    AddDiagnostic(
                        result,
                        DnsDiagnosticCode.ResponseMismatch,
                        "socket",
                        "A UDP server socket entered an error state.");
                }
                else if (socket != null &&
                    socket.Poll(0, SelectMode.SelectRead))
                {
                    ProcessDatagram(
                        server,
                        options,
                        services,
                        binding,
                        result);
                    processed = true;
                }

                bindingIndex = bindingIndex + 1;
            }

            if (!processed && result.Status == DnsServerStatus.Running &&
                !IsStopRequested(server, options))
            {
                Thread.Sleep(StopPollMilliseconds);
            }
        }

        if (result.Status == DnsServerStatus.Running)
        {
            CompleteCancellation(result);
        }
        else if (result.Status == DnsServerStatus.Failed)
        {
            server.StopServer();
        }
    }

    static void ProcessDatagram(
        DnsServer server,
        DnsServerOptions options,
        DnsServerRequestServices services,
        DnsUdpServerBinding binding,
        DnsServerRunResult result)
    {
        Socket? socket = binding.Socket;
        IPEndPoint? localEndpoint = binding.Endpoint;
        if (socket == null || localEndpoint == null)
        {
            result.Status = DnsServerStatus.Failed;
            return;
        }

        int bufferLength = options.MaximumRequestLength;
        if (bufferLength < DnsProtocolLimits.MaximumTcpMessageLength)
        {
            bufferLength = bufferLength + 1;
        }

        byte[] buffer = new byte[bufferLength];
        EndPoint anyEndpoint = CreateAnyEndpoint(
            localEndpoint.AddressFamily);
        SocketReceiveFromResult received = socket.ReceiveFromAsync(
            new ArraySegment<byte>(buffer),
            SocketFlags.None,
            anyEndpoint).GetAwaiter().GetResult();
        result.RequestCount = result.RequestCount + 1;
        result.UdpRequestCount = result.UdpRequestCount + 1;
        if (received.ReceivedBytes > options.MaximumRequestLength)
        {
            RecordRequestFailure(
                result,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "requestBytes",
                "The UDP request exceeds the configured receive limit.");
            return;
        }

        byte[] requestBytes = CopyBytes(
            buffer,
            received.ReceivedBytes);
        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        decodeOptions.MaximumMessageLength = options.MaximumRequestLength;
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            requestBytes,
            decodeOptions);
        IPEndPoint remoteEndpoint = (IPEndPoint)received.RemoteEndPoint;
        if (!decoded.Success || decoded.Message == null)
        {
            result.FailedRequestCount = result.FailedRequestCount + 1;
            CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
            SendFormatError(socket, remoteEndpoint, requestBytes, options);
            return;
        }

        DnsServerRequestContext context = new DnsServerRequestContext();
        context.Message = decoded.Message;
        context.MessageBytes = requestBytes;
        context.ClientEndpoint = EndpointOperations.ToDnsEndpoint(
            remoteEndpoint);
        context.ServerEndpoint = EndpointOperations.ToDnsEndpoint(
            localEndpoint);
        context.Transport = DnsTransportKind.Udp;
        context.CancellationRequested = IsStopRequested(server, options);
        DnsServerDispatchResult dispatch =
            DnsServerRequestOperations.Dispatch(
                context,
                options,
                services);
        CopyDiagnostics(dispatch.Diagnostics, result.Diagnostics);
        if (dispatch.Responses.Count == 0)
        {
            result.FailedRequestCount = result.FailedRequestCount + 1;
            return;
        }

        byte[] responseBytes = dispatch.Responses[0];
        if (responseBytes.Length >
            options.Authoritative.MaximumUdpResponseLength)
        {
            RecordRequestFailure(
                result,
                DnsDiagnosticCode.ResourceLimitExceeded,
                "responseBytes",
                "The UDP response exceeds the configured response limit.");
            return;
        }

        int sent = socket.SendTo(
            responseBytes,
            SocketFlags.None,
            remoteEndpoint);
        if (sent != responseBytes.Length)
        {
            RecordRequestFailure(
                result,
                DnsDiagnosticCode.ResponseMismatch,
                "responseBytes",
                "The complete UDP response was not sent.");
        }
        else if (dispatch.Failed)
        {
            result.FailedRequestCount = result.FailedRequestCount + 1;
        }
    }

    static void SendFormatError(
        Socket socket,
        IPEndPoint remoteEndpoint,
        byte[] requestBytes,
        DnsServerOptions options)
    {
        if (requestBytes.Length < DnsProtocolLimits.HeaderLength ||
            options.Authoritative.MaximumUdpResponseLength <
                DnsProtocolLimits.HeaderLength)
        {
            return;
        }

        byte[] response = new byte[DnsProtocolLimits.HeaderLength];
        response[0] = requestBytes[0];
        response[1] = requestBytes[1];
        response[2] = 128;
        if (requestBytes.Length > 2)
        {
            response[2] = (byte)(128 | (requestBytes[2] & 121));
        }

        response[3] = (byte)DnsResponseCode.FormatError;
        socket.SendTo(response, SocketFlags.None, remoteEndpoint);
    }

    static EndPoint CreateAnyEndpoint(AddressFamily addressFamily)
    {
        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            return new IPEndPoint(IPAddress.IPv6Any, 0);
        }

        return new IPEndPoint(IPAddress.Any, 0);
    }

    static bool IsStopRequested(
        DnsServer server,
        DnsServerOptions options)
    {
        return options.StopRequested || server.ReadStopRequested();
    }

    static void CompleteCancellation(DnsServerRunResult result)
    {
        result.Status = DnsServerStatus.Cancelled;
        result.Cancelled = true;
        result.CompletedAt = TransportTime.ReadClock();
    }

    static void DisposeBindings(List<DnsUdpServerBinding> bindings)
    {
        int index = 0;
        while (index < bindings.Count)
        {
            Socket? socket = bindings[index].Socket;
            if (socket != null)
            {
                socket.Dispose();
            }

            index = index + 1;
        }
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

    static void RecordRequestFailure(
        DnsServerRunResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        result.FailedRequestCount = result.FailedRequestCount + 1;
        AddDiagnostic(result, code, field, reason);
    }

    static void AddDiagnostic(
        DnsServerRunResult result,
        DnsDiagnosticCode code,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = code;
        diagnostic.Field = field;
        diagnostic.Reason = reason;
        result.Diagnostics.Add(diagnostic);
    }
}
