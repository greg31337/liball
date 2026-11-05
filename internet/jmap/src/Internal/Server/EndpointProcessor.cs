using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class EndpointBodyReadResult
{
    internal byte[] Bytes = Array.Empty<byte>();
    internal bool TooLarge;
}

internal class EndpointProcessor
{
    internal JmapServerOptions Options;
    internal JmapServerServices Services;
    internal RequestDispatcher Dispatcher;
    internal RequestLimitGate Limits;
    internal BlobEndpointProcessor Blobs;
    internal EventSourceHub Events;
    internal EventSourceResponsePump EventPump;
    internal ProblemFactory Problems = new ProblemFactory();
    internal SessionValidator Sessions = new SessionValidator();
    internal ValueValidator Values = new ValueValidator();
    internal JmapJsonReader Reader;
    internal ProtocolJsonCodec Protocol;
    internal SessionJsonCodec SessionCodec;
    internal SecurityErrorRedactor Redactor = new SecurityErrorRedactor();

    internal EndpointProcessor(
        JmapServerOptions options,
        JmapServerServices services,
        RequestDispatcher dispatcher,
        EventSourceHub events)
    {
        if (options == null || services == null || dispatcher == null || events == null)
        {
            throw new ArgumentNullException("options");
        }

        Options = options;
        Services = services;
        Dispatcher = dispatcher;
        Limits = new RequestLimitGate(options);
        Blobs = new BlobEndpointProcessor(options, services);
        Events = events;
        EventPump = new EventSourceResponsePump(events);
        Reader = new JmapJsonReader(options.MaxRequestBytes, options.MaxJsonDepth);
        Protocol = new ProtocolJsonCodec(Reader);
        SessionCodec = new SessionJsonCodec(Reader);
    }

    internal Task<JmapEndpointResponse> ProcessSessionEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return RunEndpoint(
            () => ProcessSessionEndpoint(request, cancellationToken),
            cancellationToken);
    }

    internal Task<JmapEndpointResponse> ProcessApiEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return RunEndpoint(
            () => ProcessApiEndpoint(request, cancellationToken),
            cancellationToken);
    }

    internal Task<JmapEndpointResponse> ProcessUploadEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return RunEndpoint(
            () => Blobs.ProcessUploadEndpoint(request, cancellationToken),
            cancellationToken);
    }

    internal Task<JmapEndpointResponse> ProcessDownloadEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return RunEndpoint(
            () => Blobs.ProcessDownloadEndpoint(request, cancellationToken),
            cancellationToken);
    }

    internal Task<JmapEndpointResponse> ProcessEventSourceEndpointAsync(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        return RunEndpoint(
            () => ProcessEventSourceEndpoint(request, cancellationToken),
            cancellationToken);
    }

    internal Task<JmapEndpointResponse> RunEndpoint(
        Func<JmapEndpointResponse> operation,
        CancellationToken cancellationToken)
    {
        Task<JmapEndpointResponse> work = Task.Run(operation, cancellationToken);
        TaskCompletionSource<JmapEndpointResponse> completion =
            new TaskCompletionSource<JmapEndpointResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        work.ContinueWith(
            task => CompleteEndpoint(task, completion, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal void CompleteEndpoint(
        Task<JmapEndpointResponse> work,
        TaskCompletionSource<JmapEndpointResponse> completion,
        CancellationToken cancellationToken)
    {
        if (work.IsCanceled || cancellationToken.IsCancellationRequested)
        {
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (work.IsFaulted)
        {
            AggregateException? failure = work.Exception;
            completion.TrySetResult(Problems.CreateHttpProblemResponse(
                500,
                "Server Failure",
                Redactor.RedactEndpointFailure(failure),
                null,
                null));
            return;
        }

        completion.TrySetResult(work.Result);
    }

    internal JmapEndpointResponse ProcessSessionEndpoint(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        JmapEndpointResponse? basic = ValidateBasicRequest(request, "GET", "Session");
        if (basic != null)
        {
            return basic;
        }

        IJmapSessionService? service = Services.SessionService;
        if (service == null)
        {
            return HttpProblem(500, "Session Service Unavailable",
                "The Session backend is not configured.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        JmapSession session = service.GetServerSessionAsync(request.Context, cancellationToken)
            .GetAwaiter().GetResult();
        List<string> invalid = Sessions.ValidateSession(session);
        if (invalid.Count != 0)
        {
            return HttpProblem(500, "Invalid Session",
                "The Session backend returned an invalid Session resource.");
        }

        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        SessionCodec.EncodeSession(session, writer);
        writer.DisposeWriter();
        byte[] body = stream.ToArray();
        JmapEndpointResponse response = BufferedJsonResponse(body);
        response.Headers.Add("Cache-Control", "private, no-cache");
        AddContentLanguage(response, RequestLanguage(request));
        return response;
    }

    internal JmapEndpointResponse ProcessApiEndpoint(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        JmapEndpointResponse? basic = ValidateBasicRequest(request, "POST", "API");
        if (basic != null)
        {
            return basic;
        }

        if (!IsJsonMediaType(request.ContentType))
        {
            return HttpProblem(415, "Unsupported Media Type",
                "The API endpoint requires application/json.");
        }

        if (request.Body == null || !request.Body.CanRead)
        {
            return Problems.CreateProblemResponse(
                JmapProblemKind.NotRequest,
                "A readable JMAP request body is required.",
                null);
        }

        if (request.HasContentLength && request.ContentLength > Options.MaxRequestBytes)
        {
            return Problems.CreateProblemResponse(
                JmapProblemKind.Limit,
                "The request exceeds the configured byte limit.",
                JmapProperties.MaxSizeRequest);
        }

        if (request.HasContentLength && request.ContentLength < 0L)
        {
            return Problems.CreateProblemResponse(
                JmapProblemKind.NotRequest,
                "The request Content-Length is invalid.",
                null);
        }

        EndpointBodyReadResult body = ReadRequestBody(
            request.Body, Options.MaxRequestBytes, cancellationToken);
        if (body.TooLarge)
        {
            return Problems.CreateProblemResponse(
                JmapProblemKind.Limit,
                "The request exceeds the configured byte limit.",
                JmapProperties.MaxSizeRequest);
        }

        Task<JsonElement> parsing = Task.Run(
            () => Reader.ReadUtf8Memory(body.Bytes), cancellationToken);
        Task.WhenAny(parsing).GetAwaiter().GetResult();
        cancellationToken.ThrowIfCancellationRequested();
        if (parsing.IsFaulted || parsing.IsCanceled)
        {
            return Problems.CreateProblemResponse(
                JmapProblemKind.NotJson,
                "The request body is not valid JSON.",
                null);
        }

        JsonElement json = parsing.Result;
        Task<JmapRequest> decoding = Task.Run(
            () => Protocol.DecodeRequest(json), cancellationToken);
        Task.WhenAny(decoding).GetAwaiter().GetResult();
        cancellationToken.ThrowIfCancellationRequested();
        if (decoding.IsFaulted || decoding.IsCanceled)
        {
            return Problems.CreateProblemResponse(
                JmapProblemKind.NotRequest,
                "The JSON value is not a valid JMAP Request object.",
                null);
        }

        JmapRequest decoded = decoding.Result;
        RequestLimitAdmission admission = Limits.AcquireApiRequest(
            body.Bytes.Length, decoded.MethodCalls.Count, cancellationToken);
        if (!admission.IsAllowed)
        {
            return Problems.CreateValidationResponse(admission.Problem);
        }

        RequestLimitLease? requestLease = admission.Lease;
        if (requestLease == null)
        {
            return HttpProblem(500, "Server Failure",
                "The request limit gate did not return a lease.");
        }

        using RequestLimitLease lease = requestLease;
        IJmapSessionService? service = Services.SessionService;
        if (service == null)
        {
            return HttpProblem(500, "Session Service Unavailable",
                "The Session backend is not configured.");
        }

        JmapSession session = service.GetServerSessionAsync(request.Context, cancellationToken)
            .GetAwaiter().GetResult();
        if (Sessions.ValidateSession(session).Count != 0)
        {
            return HttpProblem(500, "Invalid Session",
                "The Session backend returned an invalid Session resource.");
        }

        RequestValidationResult validation = Dispatcher.ValidateDispatchRequest(
            decoded, session, request.Context, body.Bytes.Length);
        if (!validation.IsValid)
        {
            return Problems.CreateValidationResponse(validation);
        }

        JmapResponse dispatched = Dispatcher.DispatchRequest(
            decoded, session, request.Context, body.Bytes.Length, cancellationToken);
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Protocol.EncodeResponse(dispatched, writer);
        writer.DisposeWriter();
        JmapEndpointResponse response = BufferedJsonResponse(stream.ToArray());
        response.Headers.Add("Cache-Control", "no-store");
        if (Services.StringLocalizer != null && ContainsUserVisibleError(dispatched))
        {
            AddContentLanguage(response, RequestLanguage(request));
        }

        return response;
    }

    internal JmapEndpointResponse ProcessEventSourceEndpoint(
        JmapEndpointRequest request,
        CancellationToken cancellationToken)
    {
        JmapEndpointResponse? basic = ValidateBasicRequest(request, "GET", "EventSource");
        if (basic != null)
        {
            return basic;
        }

        EventSourceOptions? options = DecodeEventSourceOptions(request);
        if (options == null)
        {
            return HttpProblem(400, "Invalid EventSource Request",
                "The EventSource query options or Last-Event-ID header are invalid.");
        }

        Task<EventSourceOptions> normalizing = Task.Run(
            () => Events.NormalizeOptions(options), cancellationToken);
        Task.WhenAny(normalizing).GetAwaiter().GetResult();
        cancellationToken.ThrowIfCancellationRequested();
        if (normalizing.IsFaulted || normalizing.IsCanceled)
        {
            return HttpProblem(400, "Invalid EventSource Request",
                "The EventSource query options or Last-Event-ID header are invalid.");
        }

        if (Events.GetSubscriberCount() >= Events.MaximumSubscribers)
        {
            return HttpProblem(503, "EventSource Capacity Reached",
                "The EventSource subscriber limit was reached.");
        }

        EventSourceSubscriber subscriber = Events.Subscribe(
            normalizing.Result, cancellationToken);
        Pipe pipe = new Pipe();
        Stream input = pipe.Reader.AsStream();
        Stream output = pipe.Writer.AsStream();
        EventPump.Start(subscriber, output, cancellationToken);

        JmapEndpointResponse response = new JmapEndpointResponse();
        response.StatusCode = 200;
        response.ContentType = JmapMediaTypes.EventStream;
        response.BodyStream = input;
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");
        return response;
    }

    internal JmapEndpointResponse? ValidateBasicRequest(
        JmapEndpointRequest request,
        string method,
        string endpointName)
    {
        if (request == null)
        {
            return HttpProblem(400, "Invalid " + endpointName + " Request",
                "An endpoint request is required.");
        }

        if (!string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase))
        {
            JmapEndpointResponse response = HttpProblem(405, "Method Not Allowed",
                "The " + endpointName + " endpoint requires " + method + ".");
            response.Headers.Add("Allow", method);
            return response;
        }

        if (request.Context == null || request.Context.UserId == null ||
            request.Context.UserId.Length == 0 ||
            !Values.IsValidUnicode(request.Context.UserId))
        {
            return HttpProblem(401, "Authentication Required",
                "An authenticated user is required.");
        }

        return null;
    }

    internal EndpointBodyReadResult ReadRequestBody(
        Stream input,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        using MemoryStream output = new MemoryStream();
        EndpointBodyReadResult result = new EndpointBodyReadResult();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long remaining = maximumBytes - output.Length;
            int requested = buffer.Length;
            if (remaining < requested)
            {
                requested = (int)remaining + 1;
            }

            int read = input.Read(buffer, 0, requested);
            if (read == 0)
            {
                result.Bytes = output.ToArray();
                return result;
            }

            output.Write(buffer, 0, read);
            if (output.Length > maximumBytes)
            {
                result.TooLarge = true;
                return result;
            }
        }
    }

    internal EventSourceOptions? DecodeEventSourceOptions(JmapEndpointRequest request)
    {
        EventSourceOptions options = new EventSourceOptions();
        if (request.QueryParameters.ContainsKey(JmapProperties.Types))
        {
            string value = request.QueryParameters[JmapProperties.Types];
            if (value.Length == 0)
            {
                return null;
            }

            options.Types = new List<string>(value.Split(','));
        }

        if (request.QueryParameters.ContainsKey(JmapProperties.CloseAfter))
        {
            string close = request.QueryParameters[JmapProperties.CloseAfter];
            if (close == "state")
            {
                options.CloseAfterState = true;
            }
            else if (close != "no")
            {
                return null;
            }
        }

        if (request.QueryParameters.ContainsKey(JmapProperties.Ping))
        {
            long ping = ParseUnsignedDecimal(request.QueryParameters[JmapProperties.Ping]);
            if (ping < 0L)
            {
                return null;
            }

            options.PingIntervalSeconds = ping;
        }

        if (request.Headers.ContainsKey("Last-Event-ID"))
        {
            options.LastEventId = request.Headers["Last-Event-ID"];
        }

        return options;
    }

    internal long ParseUnsignedDecimal(string value)
    {
        if (value.Length == 0 || value.Length > 15)
        {
            return -1L;
        }

        long result = 0L;
        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return -1L;
            }

            result = result * 10L + character - '0';
            if (result > JmapProtocolLimits.MaximumUnsignedInt)
            {
                return -1L;
            }
        }

        return result;
    }

    internal JmapEndpointResponse BufferedJsonResponse(byte[] body)
    {
        JmapEndpointResponse response = new JmapEndpointResponse();
        response.StatusCode = 200;
        response.ContentType = JmapMediaTypes.Json;
        response.BodyBytes = body;
        response.ContentLength = body.Length;
        response.HasContentLength = true;
        return response;
    }

    internal JmapEndpointResponse HttpProblem(int status, string title, string detail)
    {
        return Problems.CreateHttpProblemResponse(status, title, detail, null, null);
    }

    internal bool IsJsonMediaType(string? contentType)
    {
        if (contentType == null)
        {
            return false;
        }

        int separator = contentType.IndexOf(';');
        string mediaType = separator < 0 ? contentType.Trim() :
            contentType.Substring(0, separator).Trim();
        return string.Equals(mediaType, JmapMediaTypes.Json,
            StringComparison.OrdinalIgnoreCase);
    }

    internal bool ContainsUserVisibleError(JmapResponse response)
    {
        foreach (JmapInvocation invocation in response.MethodResponses)
        {
            if (ContainsForbiddenToSend(invocation.Arguments))
            {
                return true;
            }
        }

        return false;
    }

    internal bool ContainsForbiddenToSend(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            JsonElement type = Protocol.Values.GetMember(value, JmapProperties.Type);
            if (type.ValueKind == JsonValueKind.String &&
                type.GetString() == JmapErrorCodes.ForbiddenToSend)
            {
                return true;
            }

            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (ContainsForbiddenToSend(property.Value))
                {
                    return true;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (ContainsForbiddenToSend(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal string? RequestLanguage(JmapEndpointRequest request)
    {
        string? accepted = request.Context.AcceptLanguage;
        if ((accepted == null || accepted.Length == 0) &&
            request.Headers.ContainsKey("Accept-Language"))
        {
            accepted = request.Headers["Accept-Language"];
        }

        return SelectLanguage(accepted);
    }

    internal string? SelectLanguage(string? accepted)
    {
        if (accepted == null || accepted.Length == 0 ||
            accepted.IndexOf('\r') >= 0 || accepted.IndexOf('\n') >= 0)
        {
            return null;
        }

        string? selected = null;
        int selectedQuality = -1;
        string[] ranges = accepted.Split(',');
        foreach (string range in ranges)
        {
            string trimmed = range.Trim();
            int separator = trimmed.IndexOf(';');
            string tag = separator < 0 ? trimmed : trimmed.Substring(0, separator).Trim();
            int quality = separator < 0 ? 1000 :
                ParseLanguageQuality(trimmed.Substring(separator + 1).Trim());
            if (tag != "*" && IsLanguageTag(tag) && quality > selectedQuality)
            {
                selected = tag;
                selectedQuality = quality;
            }
        }

        return selectedQuality <= 0 ? null : selected;
    }

    internal int ParseLanguageQuality(string parameter)
    {
        if (!parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        string value = parameter.Substring(2);
        if (value == "0")
        {
            return 0;
        }

        if (value == "1")
        {
            return 1000;
        }

        if (value.Length < 3 || value.Length > 5 || value[1] != '.' ||
            value[0] != '0' && value[0] != '1')
        {
            return -1;
        }

        int result = 0;
        int index = 2;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9' ||
                value[0] == '1' && character != '0')
            {
                return -1;
            }

            result = result * 10 + character - '0';
            index = index + 1;
        }

        while (index < 5)
        {
            result = result * 10;
            index = index + 1;
        }

        return value[0] == '1' ? 1000 : result;
    }

    internal bool IsLanguageTag(string value)
    {
        if (value.Length == 0 || value[0] == '-' || value[value.Length - 1] == '-')
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!(character >= 'a' && character <= 'z') &&
                !(character >= 'A' && character <= 'Z') &&
                !(character >= '0' && character <= '9') && character != '-')
            {
                return false;
            }
        }

        return true;
    }

    internal void AddContentLanguage(
        JmapEndpointResponse response,
        string? language)
    {
        if (language != null)
        {
            response.Headers.Add("Content-Language", language);
        }
    }
}
