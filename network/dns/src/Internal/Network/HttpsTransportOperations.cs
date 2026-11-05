using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace dns;

internal class HttpsWaitResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
}

internal class HttpsBodyResult
{
    internal bool Success;
    internal bool TimedOut;
    internal bool Cancelled;
    internal bool NetworkError;
    internal bool TooLarge;
    internal byte[] Bytes = [];
}

internal static class HttpsTransportOperations
{
    const string DnsMediaType = "application/dns-message";

    internal static DnsTransportResult Exchange(
        DnsTransportRequest request,
        DnsHttpsTransport transport)
    {
        Uri initialUri = ValidateRequest(request, transport);
        DnsTransportResult result = CreateResult(request, initialUri);
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

        bool ownsHandler = transport.MessageHandler == null;
        HttpMessageHandler handler;
        if (transport.MessageHandler == null)
        {
            handler = CreateHandler(request, initialUri, deadline);
        }
        else
        {
            handler = transport.MessageHandler;
        }

        HttpClient client = new HttpClient(handler, ownsHandler);
        client.Timeout = Timeout.InfiniteTimeSpan;
        CancellationTokenSource cancellation = new CancellationTokenSource();
        Uri currentUri = initialUri;
        bool useGet = request.Options.Https.UseGet;
        int redirectCount = 0;
        while (true)
        {
            HttpRequestMessage message = CreateRequestMessage(
                currentUri,
                request.QueryBytes,
                useGet,
                request.Options.Https.MediaType);
            Task<HttpResponseMessage> responseTask = client.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellation.Token);
            HttpsWaitResult responseWait = WaitForResponseTask(
                responseTask, request, deadline);
            if (!responseWait.Success)
            {
                ApplyWaitFailure(result, responseWait);
                cancellation.Cancel();
                return DisposeAndComplete(
                    result, client, cancellation, message, null);
            }

            HttpResponseMessage response = responseTask.Result;
            int statusCode = (int)response.StatusCode;
            if (IsRedirectStatus(statusCode))
            {
                Uri? nextUri = ReadRedirectUri(
                    response,
                    currentUri,
                    initialUri,
                    request.Options.Https,
                    redirectCount,
                    result);
                if (nextUri == null)
                {
                    result.Status = DnsTransportStatus.ProtocolError;
                    return DisposeAndComplete(
                        result, client, cancellation, message, response);
                }

                if (RedirectChangesToGet(statusCode))
                {
                    useGet = true;
                }

                redirectCount = redirectCount + 1;
                currentUri = nextUri;
                response.Dispose();
                message.Dispose();
                continue;
            }

            if (statusCode != (int)HttpStatusCode.OK)
            {
                result.Status = DnsTransportStatus.ProtocolError;
                AddHttpDiagnostic(
                    result,
                    "http.statusCode",
                    $"The DNS-over-HTTPS server returned HTTP status {statusCode}.");
                return DisposeAndComplete(
                    result, client, cancellation, message, response);
            }

            string? responseMediaType = null;
            if (response.Content.Headers.ContentType != null)
            {
                responseMediaType =
                    response.Content.Headers.ContentType.MediaType;
            }

            if (!string.Equals(
                responseMediaType,
                request.Options.Https.MediaType,
                StringComparison.OrdinalIgnoreCase))
            {
                result.Status = DnsTransportStatus.ProtocolError;
                AddHttpDiagnostic(
                    result,
                    "http.contentType",
                    "The DNS-over-HTTPS response has an unexpected media type.");
                return DisposeAndComplete(
                    result, client, cancellation, message, response);
            }

            if (response.Content.Headers.ContentLength.HasValue &&
                response.Content.Headers.ContentLength.Value >
                    request.Options.MaximumResponseLength)
            {
                result.Status = DnsTransportStatus.ResponseTooLarge;
                AddResponseSizeDiagnostic(result);
                return DisposeAndComplete(
                    result, client, cancellation, message, response);
            }

            HttpsBodyResult body = ReadResponseBody(
                response,
                request.Options.MaximumResponseLength,
                request,
                deadline,
                cancellation.Token);
            if (!body.Success)
            {
                ApplyBodyFailure(result, body);
                cancellation.Cancel();
                return DisposeAndComplete(
                    result, client, cancellation, message, response);
            }

            result.ResponseBytes = body.Bytes;
            DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
            decodeOptions.MaximumMessageLength =
                request.Options.MaximumResponseLength;
            DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
                body.Bytes, decodeOptions);
            if (!decoded.Success || decoded.Message == null)
            {
                result.Status = DnsTransportStatus.ProtocolError;
                CopyDiagnostics(decoded.Diagnostics, result.Diagnostics);
                return DisposeAndComplete(
                    result, client, cancellation, message, response);
            }

            if (!ResponseMatches(query, decoded.Message))
            {
                result.Status = DnsTransportStatus.ProtocolError;
                AddMismatchDiagnostic(result);
                return DisposeAndComplete(
                    result, client, cancellation, message, response);
            }

            result.Status = DnsTransportStatus.Success;
            result.ResponseMessage = decoded.Message;
            result.Truncated =
                (MessageOperations.EffectiveFlags(decoded.Message.Header) &
                 (int)DnsHeaderFlag.Truncated) != 0;
            return DisposeAndComplete(
                result, client, cancellation, message, response);
        }
    }

    internal static DnsEndpoint ApplyBootstrapPort(
        DnsEndpoint endpoint,
        int originPort)
    {
        if (endpoint == null)
        {
            throw new ArgumentNullException("endpoint");
        }

        DnsEndpoint result = CopyEndpoint(endpoint);
        if (result.Port == DnsPorts.Dns)
        {
            result.Port = originPort;
        }

        return result;
    }

    internal static string CreateGetQueryValue(byte[] queryBytes)
    {
        if (queryBytes == null)
        {
            throw new ArgumentNullException("queryBytes");
        }

        return Convert.ToBase64String(queryBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    static Uri ValidateRequest(
        DnsTransportRequest request,
        DnsHttpsTransport transport)
    {
        if (request == null || transport == null)
        {
            throw new ArgumentNullException("request");
        }

        if (request.Endpoint == null || request.Options == null ||
            request.Options.Https == null || request.QueryBytes == null ||
            request.Deadline == null)
        {
            throw new ArgumentException(
                "A complete DNS transport request is required.");
        }

        if (request.Options.Kind != DnsTransportKind.Https)
        {
            throw new ArgumentException(
                "The HTTPS transport requires HTTPS transport options.");
        }

        if (request.Options.AttemptTimeoutMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                "request.Options.AttemptTimeoutMilliseconds");
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
                "A complete DNS-over-HTTPS query is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Options.Https.Uri))
        {
            throw new ArgumentException(
                "An absolute DNS-over-HTTPS URI is required.");
        }

        Uri uri = new Uri(request.Options.Https.Uri, UriKind.Absolute);
        ValidateHttpsUri(uri);
        if (!string.Equals(
            request.Options.Https.MediaType,
            DnsMediaType,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The DNS-over-HTTPS media type must be application/dns-message.");
        }

        if (request.Options.Https.AllowRedirects &&
            request.Options.Https.MaximumRedirects <= 0)
        {
            throw new ArgumentOutOfRangeException(
                "request.Options.Https.MaximumRedirects");
        }

        if (request.Options.Https.MaximumRedirects < 0)
        {
            throw new ArgumentOutOfRangeException(
                "request.Options.Https.MaximumRedirects");
        }

        if (request.Options.Https.BootstrapEndpoint != null)
        {
            DnsEndpoint bootstrap = ApplyBootstrapPort(
                request.Options.Https.BootstrapEndpoint, uri.Port);
            if (!bootstrap.HasAddress)
            {
                throw new ArgumentException(
                    "A DNS-over-HTTPS bootstrap endpoint must be numeric.");
            }

            EndpointOperations.CreateNumeric(bootstrap);
        }

        return uri;
    }

    static void ValidateHttpsUri(Uri uri)
    {
        if (!string.Equals(
            uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                "A DNS-over-HTTPS URI must be an absolute HTTPS URI without user information or a fragment.");
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
                "The HTTPS query bytes must contain a valid DNS message.");
        }

        return decoded.Message;
    }

    static DnsTransportResult CreateResult(
        DnsTransportRequest request,
        Uri uri)
    {
        DnsTransportResult result = new DnsTransportResult();
        result.Status = DnsTransportStatus.NetworkError;
        result.Transport = DnsTransportKind.Https;
        result.StartedAt = TransportTime.ReadClock();
        if (request.Options.Https.BootstrapEndpoint != null)
        {
            DnsEndpoint bootstrap = ApplyBootstrapPort(
                request.Options.Https.BootstrapEndpoint, uri.Port);
            result.Endpoint = EndpointOperations.ToDnsEndpoint(
                EndpointOperations.CreateNumeric(bootstrap));
        }
        else
        {
            result.Endpoint.Host = uri.DnsSafeHost;
            result.Endpoint.Port = uri.Port;
        }

        return result;
    }

    static HttpMessageHandler CreateHandler(
        DnsTransportRequest request,
        Uri uri,
        DnsClockValue deadline)
    {
        SocketsHttpHandler handler = new SocketsHttpHandler();
        handler.AllowAutoRedirect = false;
        handler.UseProxy = false;
        handler.AutomaticDecompression = DecompressionMethods.None;
        if (request.Options.Https.BootstrapEndpoint != null)
        {
            DnsEndpoint bootstrap = ApplyBootstrapPort(
                request.Options.Https.BootstrapEndpoint, uri.Port);
            IPEndPoint endpoint = EndpointOperations.CreateNumeric(bootstrap);
            handler.ConnectCallback = (context, cancellationToken) =>
                ConnectBootstrap(
                    endpoint, request, deadline, cancellationToken);
        }

        return handler;
    }

    static ValueTask<Stream> ConnectBootstrap(
        IPEndPoint endpoint,
        DnsTransportRequest request,
        DnsClockValue deadline,
        CancellationToken cancellationToken)
    {
        Socket socket = new Socket(
            endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        Task connectTask = socket.ConnectAsync(endpoint);
        while (!connectTask.IsCompleted)
        {
            if (TransportCancellation.IsRequested(
                request.CancellationRequested, cancellationToken))
            {
                socket.Close();
                throw new OperationCanceledException();
            }

            if (TransportTime.RemainingMilliseconds(deadline) == 0)
            {
                socket.Close();
                throw new IOException(
                    "The DNS-over-HTTPS bootstrap connection timed out.");
            }

            Thread.Sleep(1);
        }

        if (!connectTask.IsCompletedSuccessfully)
        {
            socket.Close();
            throw new IOException(
                "The DNS-over-HTTPS bootstrap connection failed.");
        }

        Stream stream = new NetworkStream(socket, true);
        return new ValueTask<Stream>(stream);
    }

    static HttpRequestMessage CreateRequestMessage(
        Uri uri,
        byte[] queryBytes,
        bool useGet,
        string mediaType)
    {
        Uri requestUri = uri;
        HttpMethod method = HttpMethod.Post;
        if (useGet)
        {
            method = HttpMethod.Get;
            UriBuilder builder = new UriBuilder(uri);
            string query = builder.Query;
            if (query.Length > 0 && query[0] == '?')
            {
                query = query.Substring(1);
            }

            string dnsParameter = "dns=" + CreateGetQueryValue(queryBytes);
            if (query.Length == 0)
            {
                builder.Query = dnsParameter;
            }
            else
            {
                builder.Query = query + "&" + dnsParameter;
            }

            requestUri = builder.Uri;
        }

        HttpRequestMessage message = new HttpRequestMessage(
            method, requestUri);
        message.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(mediaType));
        if (!useGet)
        {
            ByteArrayContent content = new ByteArrayContent(queryBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            message.Content = content;
        }

        return message;
    }

    static HttpsWaitResult WaitForResponseTask(
        Task<HttpResponseMessage> task,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        HttpsWaitResult result = new HttpsWaitResult();
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

        result.Success = task.IsCompletedSuccessfully;
        return result;
    }

    static HttpsBodyResult ReadResponseBody(
        HttpResponseMessage response,
        int maximumLength,
        DnsTransportRequest request,
        DnsClockValue deadline,
        CancellationToken cancellationToken)
    {
        HttpsBodyResult result = new HttpsBodyResult();
        Task<Stream> streamTask = response.Content.ReadAsStreamAsync(
            cancellationToken);
        HttpsWaitResult streamWait = WaitForStreamTask(
            streamTask, request, deadline);
        if (!streamWait.Success)
        {
            result.TimedOut = streamWait.TimedOut;
            result.Cancelled = streamWait.Cancelled;
            result.NetworkError = !streamWait.TimedOut &&
                !streamWait.Cancelled;
            return result;
        }

        Stream stream = streamTask.Result;
        byte[] buffer = new byte[maximumLength + 1];
        int length = 0;
        while (length < buffer.Length)
        {
            Task<int> readTask = stream.ReadAsync(
                buffer.AsMemory(length, buffer.Length - length),
                cancellationToken).AsTask();
            HttpsWaitResult readWait = WaitForReadTask(
                readTask, request, deadline);
            if (!readWait.Success)
            {
                result.TimedOut = readWait.TimedOut;
                result.Cancelled = readWait.Cancelled;
                result.NetworkError = !readWait.TimedOut &&
                    !readWait.Cancelled;
                return result;
            }

            int read = readTask.Result;
            if (read == 0)
            {
                result.Bytes = CopyPrefix(buffer, length);
                result.Success = true;
                return result;
            }

            length = length + read;
        }

        result.TooLarge = true;
        return result;
    }

    static HttpsWaitResult WaitForStreamTask(
        Task<Stream> task,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        HttpsWaitResult result = new HttpsWaitResult();
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

        result.Success = task.IsCompletedSuccessfully;
        return result;
    }

    static HttpsWaitResult WaitForReadTask(
        Task<int> task,
        DnsTransportRequest request,
        DnsClockValue deadline)
    {
        HttpsWaitResult result = new HttpsWaitResult();
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

        result.Success = task.IsCompletedSuccessfully;
        return result;
    }

    static Uri? ReadRedirectUri(
        HttpResponseMessage response,
        Uri currentUri,
        Uri initialUri,
        DnsHttpsOptions options,
        int redirectCount,
        DnsTransportResult result)
    {
        if (!options.AllowRedirects)
        {
            AddHttpDiagnostic(
                result,
                "http.redirect",
                "The DNS-over-HTTPS server returned a disallowed redirect.");
            return null;
        }

        if (redirectCount >= options.MaximumRedirects)
        {
            AddHttpDiagnostic(
                result,
                "http.redirect",
                "The DNS-over-HTTPS redirect limit was exceeded.");
            return null;
        }

        Uri? location = response.Headers.Location;
        if (location == null)
        {
            AddHttpDiagnostic(
                result,
                "http.redirect",
                "The DNS-over-HTTPS redirect has no destination.");
            return null;
        }

        Uri destination;
        if (location.IsAbsoluteUri)
        {
            destination = location;
        }
        else
        {
            destination = new Uri(currentUri, location);
        }

        if (!IsValidRedirectDestination(destination))
        {
            AddHttpDiagnostic(
                result,
                "http.redirect",
                "The DNS-over-HTTPS redirect destination is not a safe HTTPS URI.");
            return null;
        }

        if (options.BootstrapEndpoint != null &&
            (!string.Equals(
                destination.Host,
                initialUri.Host,
                StringComparison.OrdinalIgnoreCase) ||
             destination.Port != initialUri.Port))
        {
            AddHttpDiagnostic(
                result,
                "http.redirect",
                "A bootstrapped DNS-over-HTTPS request cannot redirect to another origin.");
            return null;
        }

        return destination;
    }

    static bool IsValidRedirectDestination(Uri uri)
    {
        return string.Equals(
            uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            string.IsNullOrEmpty(uri.Fragment);
    }

    static bool IsRedirectStatus(int statusCode)
    {
        switch (statusCode)
        {
            case 300:
            case 301:
            case 302:
            case 303:
            case 307:
            case 308:
                return true;
            default:
                return false;
        }
    }

    static bool RedirectChangesToGet(int statusCode)
    {
        return statusCode == 301 || statusCode == 302 ||
            statusCode == 303;
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

    static void ApplyWaitFailure(
        DnsTransportResult result,
        HttpsWaitResult wait)
    {
        if (wait.Cancelled)
        {
            result.Status = DnsTransportStatus.Cancelled;
            result.Cancelled = true;
        }
        else if (wait.TimedOut)
        {
            result.Status = DnsTransportStatus.TimedOut;
            result.TimedOut = true;
        }
        else
        {
            result.Status = DnsTransportStatus.NetworkError;
        }
    }

    static void ApplyBodyFailure(
        DnsTransportResult result,
        HttpsBodyResult body)
    {
        if (body.Cancelled)
        {
            result.Status = DnsTransportStatus.Cancelled;
            result.Cancelled = true;
        }
        else if (body.TimedOut)
        {
            result.Status = DnsTransportStatus.TimedOut;
            result.TimedOut = true;
        }
        else if (body.TooLarge)
        {
            result.Status = DnsTransportStatus.ResponseTooLarge;
            AddResponseSizeDiagnostic(result);
        }
        else
        {
            result.Status = DnsTransportStatus.NetworkError;
        }
    }

    static DnsTransportResult DisposeAndComplete(
        DnsTransportResult result,
        HttpClient client,
        CancellationTokenSource cancellation,
        HttpRequestMessage message,
        HttpResponseMessage? response)
    {
        if (response != null)
        {
            response.Dispose();
        }

        message.Dispose();
        cancellation.Dispose();
        client.Dispose();
        return CompleteResult(result);
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

    static byte[] CopyPrefix(byte[] source, int length)
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

    static void AddHttpDiagnostic(
        DnsTransportResult result,
        string field,
        string reason)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidRecordData;
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
            "The DNS-over-HTTPS response exceeds the configured body limit.";
        result.Diagnostics.Add(diagnostic);
    }

    static void AddMismatchDiagnostic(DnsTransportResult result)
    {
        DnsDiagnostic diagnostic = new DnsDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.ResponseMismatch;
        diagnostic.Field = "response";
        diagnostic.Reason =
            "The DNS-over-HTTPS response identifier, direction, or question does not match the query.";
        result.Diagnostics.Add(diagnostic);
    }
}
