namespace whois;

/// <summary>
/// Performs bounded RFC 3912 exchanges through injectable resolution, policy, and connection boundaries.
/// </summary>
public class WhoisTcpTransport : IWhoisTransport
{
    IWhoisQueryCodec queryCodec;
    IWhoisHostResolver hostResolver;
    IWhoisEndpointPolicy directEndpointPolicy;
    IWhoisEndpointPolicy automaticEndpointPolicy;
    IWhoisConnectionFactory connectionFactory;
    WhoisAddressPreference addressPreference;

    /// <summary>
    /// Initializes a TCP transport with its framing and network dependencies.
    /// </summary>
    /// <param name="codec">The strict query codec.</param>
    /// <param name="resolver">The host resolver.</param>
    /// <param name="directPolicy">The policy for caller-directed endpoints.</param>
    /// <param name="automaticPolicy">The policy for automatically selected endpoints.</param>
    /// <param name="factory">The approved-address connection factory.</param>
    /// <param name="preference">The deterministic address-family preference.</param>
    /// <exception cref="System.ArgumentNullException">A required dependency is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The address preference is not supported.</exception>
    public WhoisTcpTransport(IWhoisQueryCodec codec, IWhoisHostResolver resolver, IWhoisEndpointPolicy directPolicy, IWhoisEndpointPolicy automaticPolicy, IWhoisConnectionFactory factory, WhoisAddressPreference preference)
    {
        InputGuard.RequireReference(codec, "codec");
        InputGuard.RequireReference(resolver, "resolver");
        InputGuard.RequireReference(directPolicy, "directPolicy");
        InputGuard.RequireReference(automaticPolicy, "automaticPolicy");
        InputGuard.RequireReference(factory, "factory");
        WhoisDnsResolver.ValidatePreference(preference);
        queryCodec = codec;
        hostResolver = resolver;
        directEndpointPolicy = directPolicy;
        automaticEndpointPolicy = automaticPolicy;
        connectionFactory = factory;
        addressPreference = preference;
    }

    /// <summary>
    /// Sends one exact query and reads response bytes until EOF or failure.
    /// </summary>
    /// <param name="request">The exact exchange request.</param>
    /// <returns>The raw response and exchange status.</returns>
    /// <exception cref="System.ArgumentNullException">The request or a required nested request value is <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The request endpoint, query, limits, or timeouts are invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The endpoint source is not supported.</exception>
    /// <exception cref="System.InvalidOperationException">A dependency returns an invalid success result.</exception>
    public WhoisRawResponse Exchange(WhoisRequest request)
    {
        InputGuard.RequireReference(request, "request");
        InputGuard.ValidateEndpoint(request.Endpoint, request.QueryText, WhoisQueryKind.FreeForm);
        InputGuard.ValidateLimits(request.Limits, request.QueryText, WhoisQueryKind.FreeForm);
        InputGuard.ValidateTimeouts(request.Timeouts, request.QueryText, WhoisQueryKind.FreeForm);
        if (request.Source != WhoisEndpointSource.Direct && request.Source != WhoisEndpointSource.InitialSelection && request.Source != WhoisEndpointSource.Referral)
        {
            throw new System.ArgumentOutOfRangeException("request.Source");
        }

        byte[] payload = queryCodec.EncodeQuery(request);
        WhoisRawResponse response = new WhoisRawResponse();
        response.Request = request;
        response.StartedAt = System.DateTimeOffset.UtcNow;
        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        TimeoutBudget budget = new TimeoutBudget(request.Timeouts, request.CancellationToken, System.TimeProvider.System);

        if (request.CancellationToken.IsCancellationRequested)
        {
            return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Resolve, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
        }

        budget.BeginStage(WhoisOperationStage.Resolve);
        WhoisResolution resolution;
        int resolveMilliseconds = budget.GetRemainingMilliseconds();
        using (System.Threading.CancellationTokenSource resolveCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken))
        {
            resolveCancellation.CancelAfter(resolveMilliseconds);
            resolution = hostResolver.ResolveHost(request.Endpoint, addressPreference, resolveCancellation.Token);
            resolveCancellation.Cancel();
        }
        if (resolution == null)
        {
            throw new System.InvalidOperationException("The host resolver returned no result.");
        }
        if (resolution.Status != WhoisExchangeStatus.Complete && resolution.Status != WhoisExchangeStatus.ResolveFailed && resolution.Status != WhoisExchangeStatus.TimedOut && resolution.Status != WhoisExchangeStatus.Cancelled)
        {
            throw new System.InvalidOperationException("The host resolver returned an invalid status.");
        }
        if (request.CancellationToken.IsCancellationRequested)
        {
            return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Resolve, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
        }
        if (budget.GetInterruptionStatus() == WhoisExchangeStatus.TimedOut || resolution.Status == WhoisExchangeStatus.TimedOut || resolution.Status == WhoisExchangeStatus.Cancelled)
        {
            System.Exception? timeoutError = resolution.Error;
            if (timeoutError == null)
            {
                timeoutError = new System.TimeoutException("Host resolution timed out.");
            }
            return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Resolve, timeoutError, System.Array.Empty<byte>(), false);
        }
        if (resolution.Status != WhoisExchangeStatus.Complete)
        {
            return SetResponseOutcome(response, timer, WhoisExchangeStatus.ResolveFailed, WhoisOperationStage.Resolve, resolution.Error, System.Array.Empty<byte>(), false);
        }
        if (resolution.Addresses == null)
        {
            throw new System.InvalidOperationException("The host resolver returned a null address array.");
        }
        if (resolution.Addresses.Length == 0)
        {
            throw new System.InvalidOperationException("The host resolver returned a successful result without addresses.");
        }
        if (resolution.Error != null)
        {
            throw new System.InvalidOperationException("The host resolver returned an error for a successful result.");
        }

        IWhoisEndpointPolicy endpointPolicy = automaticEndpointPolicy;
        if (request.Source == WhoisEndpointSource.Direct)
        {
            endpointPolicy = directEndpointPolicy;
        }

        System.IO.Stream? selectedStream = null;
        System.Exception? connectError = null;
        System.Collections.Generic.List<System.Exception> connectErrors = new System.Collections.Generic.List<System.Exception>();
        bool allowedAddressFound = false;
        bool connectTimedOut = false;
        int addressIndex = 0;
        while (addressIndex < resolution.Addresses.Length && selectedStream == null)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Connect, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
            }
            System.Net.IPAddress address = resolution.Addresses[addressIndex];
            if (address == null)
            {
                throw new System.InvalidOperationException("The host resolver returned a null address candidate.");
            }
            if (endpointPolicy.AllowEndpoint(request.Endpoint, address, request.Source))
            {
                allowedAddressFound = true;
                response.ApprovedAddress = address;
                budget.BeginStage(WhoisOperationStage.Connect);
                if (request.CancellationToken.IsCancellationRequested)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Connect, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
                }
                if (budget.GetInterruptionStatus() == WhoisExchangeStatus.TimedOut)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Connect, new System.TimeoutException("The overall timeout expired before connection establishment."), System.Array.Empty<byte>(), false);
                }

                int connectMilliseconds = budget.GetRemainingMilliseconds();
                WhoisConnection connection;
                using (System.Threading.CancellationTokenSource connectCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken))
                {
                    connectCancellation.CancelAfter(connectMilliseconds);
                    connection = connectionFactory.OpenConnection(request.Endpoint, address, request.Timeouts, connectCancellation.Token);
                    connectCancellation.Cancel();
                }
                if (connection == null)
                {
                    throw new System.InvalidOperationException("The connection factory returned no result.");
                }
                if (request.CancellationToken.IsCancellationRequested)
                {
                    if (connection.Stream != null)
                    {
                        connection.Stream.Dispose();
                    }
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Connect, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
                }
                if (connection.Status != WhoisExchangeStatus.Complete && connection.Status != WhoisExchangeStatus.ConnectFailed && connection.Status != WhoisExchangeStatus.TimedOut && connection.Status != WhoisExchangeStatus.Cancelled)
                {
                    if (connection.Stream != null)
                    {
                        connection.Stream.Dispose();
                    }
                    throw new System.InvalidOperationException("The connection factory returned an invalid status.");
                }
                if (connection.ApprovedAddress == null || !connection.ApprovedAddress.Equals(address))
                {
                    if (connection.Stream != null)
                    {
                        connection.Stream.Dispose();
                    }
                    throw new System.InvalidOperationException("The connection factory returned a result for a different approved address.");
                }
                if (connection.Status == WhoisExchangeStatus.Complete)
                {
                    if (connection.Stream == null || connection.Error != null)
                    {
                        if (connection.Stream != null)
                        {
                            connection.Stream.Dispose();
                        }
                        throw new System.InvalidOperationException("The connection factory returned an invalid successful connection.");
                    }
                    selectedStream = connection.Stream;
                }
                else
                {
                    if (connection.Stream != null)
                    {
                        connection.Stream.Dispose();
                        throw new System.InvalidOperationException("The connection factory returned a stream for a failed connection.");
                    }
                    connectError = connection.Error;
                    if (connectError == null)
                    {
                        if (connection.Status == WhoisExchangeStatus.TimedOut || connection.Status == WhoisExchangeStatus.Cancelled)
                        {
                            connectError = new System.TimeoutException($"Connection to approved address {address} timed out.");
                        }
                        else
                        {
                            connectError = new System.InvalidOperationException($"Connection to approved address {address} failed without error details.");
                        }
                    }
                    connectErrors.Add(connectError);
                    if (connection.Status == WhoisExchangeStatus.TimedOut || connection.Status == WhoisExchangeStatus.Cancelled)
                    {
                        connectTimedOut = true;
                    }
                }
            }
            addressIndex = addressIndex + 1;
        }

        if (selectedStream == null)
        {
            if (request.CancellationToken.IsCancellationRequested)
            {
                return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Connect, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
            }
            if (!allowedAddressFound)
            {
                connectError = new System.InvalidOperationException("No resolved address passed endpoint policy.");
            }
            else if (connectErrors.Count > 1)
            {
                connectError = new System.AggregateException("All approved address connection attempts failed.", connectErrors);
            }
            if (connectTimedOut || budget.GetInterruptionStatus() == WhoisExchangeStatus.TimedOut)
            {
                if (connectError == null)
                {
                    connectError = new System.TimeoutException("TCP connection establishment timed out.");
                }
                return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Connect, connectError, System.Array.Empty<byte>(), false);
            }
            return SetResponseOutcome(response, timer, WhoisExchangeStatus.ConnectFailed, WhoisOperationStage.Connect, connectError, System.Array.Empty<byte>(), false);
        }

        using (System.IO.Stream stream = selectedStream)
        {
            budget.BeginStage(WhoisOperationStage.Write);
            int writeOffset = 0;
            while (writeOffset < payload.Length)
            {
                if (request.CancellationToken.IsCancellationRequested)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Write, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
                }
                int writeMilliseconds = budget.GetRemainingMilliseconds();
                if (writeMilliseconds == 0)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Write, new System.TimeoutException("Query transmission timed out."), System.Array.Empty<byte>(), false);
                }

                System.Threading.Tasks.Task writeTask;
                System.Threading.Tasks.Task completedWriteTask;
                using (System.Threading.CancellationTokenSource writeCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken))
                {
                    writeCancellation.CancelAfter(writeMilliseconds);
                    writeTask = stream.WriteAsync(payload, writeOffset, 1, writeCancellation.Token);
                    System.Threading.Tasks.Task writeCancellationTask = System.Threading.Tasks.Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, writeCancellation.Token);
                    completedWriteTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { writeTask, writeCancellationTask }).Result;
                    writeCancellation.Cancel();
                }
                if (request.CancellationToken.IsCancellationRequested)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Write, new System.OperationCanceledException(request.CancellationToken), System.Array.Empty<byte>(), false);
                }
                if (completedWriteTask != writeTask || writeTask.IsCanceled)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Write, new System.TimeoutException("Query transmission timed out."), System.Array.Empty<byte>(), false);
                }
                if (writeTask.IsFaulted)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.WriteFailed, WhoisOperationStage.Write, GetTaskError(writeTask), System.Array.Empty<byte>(), false);
                }
                if (!writeTask.IsCompletedSuccessfully)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.WriteFailed, WhoisOperationStage.Write, new System.InvalidOperationException("Query transmission did not reach a terminal state."), System.Array.Empty<byte>(), false);
                }
                writeOffset = writeOffset + 1;
            }

            ByteAccumulator accumulator = new ByteAccumulator(request.Limits.MaximumResponseBytes);
            byte[] readBuffer = new byte[8192];
            budget.BeginStage(WhoisOperationStage.Read);
            while (true)
            {
                if (request.CancellationToken.IsCancellationRequested)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Read, new System.OperationCanceledException(request.CancellationToken), accumulator.GetBytes(), false);
                }
                int readMilliseconds = budget.GetRemainingMilliseconds();
                if (readMilliseconds == 0)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Read, new System.TimeoutException("Response reception timed out."), accumulator.GetBytes(), false);
                }

                System.Threading.Tasks.Task<int> readTask;
                System.Threading.Tasks.Task completedReadTask;
                using (System.Threading.CancellationTokenSource readCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken))
                {
                    readCancellation.CancelAfter(readMilliseconds);
                    readTask = stream.ReadAsync(readBuffer, 0, readBuffer.Length, readCancellation.Token);
                    System.Threading.Tasks.Task readCancellationTask = System.Threading.Tasks.Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, readCancellation.Token);
                    completedReadTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { readTask, readCancellationTask }).Result;
                    readCancellation.Cancel();
                }
                if (request.CancellationToken.IsCancellationRequested)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Cancelled, WhoisOperationStage.Read, new System.OperationCanceledException(request.CancellationToken), accumulator.GetBytes(), false);
                }
                if (completedReadTask != readTask || readTask.IsCanceled)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.TimedOut, WhoisOperationStage.Read, new System.TimeoutException("Response reception timed out."), accumulator.GetBytes(), false);
                }
                if (readTask.IsFaulted)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.ReadFailed, WhoisOperationStage.Read, GetTaskError(readTask), accumulator.GetBytes(), false);
                }
                if (!readTask.IsCompletedSuccessfully)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.ReadFailed, WhoisOperationStage.Read, new System.InvalidOperationException("Response reception did not reach a terminal state."), accumulator.GetBytes(), false);
                }

                int bytesRead = readTask.Result;
                if (bytesRead < 0 || bytesRead > readBuffer.Length)
                {
                    throw new System.InvalidOperationException("The response stream returned an invalid byte count.");
                }
                if (bytesRead == 0)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.Complete, WhoisOperationStage.None, null, accumulator.GetBytes(), false);
                }
                bool accepted = accumulator.Append(System.MemoryExtensions.AsSpan(readBuffer, 0, bytesRead));
                if (!accepted)
                {
                    return SetResponseOutcome(response, timer, WhoisExchangeStatus.ResponseLimitReached, WhoisOperationStage.Read, new System.InvalidOperationException("The response byte limit was reached."), accumulator.GetBytes(), true);
                }
                budget.ResetReadIdle();
            }
        }
    }

    internal static System.Exception? GetTaskError(System.Threading.Tasks.Task task)
    {
        System.Exception? error = task.Exception;
        if (task.Exception != null && task.Exception.InnerException != null)
        {
            error = task.Exception.InnerException;
        }
        return error;
    }

    internal static WhoisRawResponse SetResponseOutcome(WhoisRawResponse response, System.Diagnostics.Stopwatch timer, WhoisExchangeStatus status, WhoisOperationStage stage, System.Exception? error, byte[] body, bool truncated)
    {
        timer.Stop();
        response.Duration = timer.Elapsed;
        response.Status = status;
        response.Stage = stage;
        response.Error = error;
        response.Body = body;
        response.IsTruncated = truncated;
        return response;
    }
}
