namespace whois;

/// <summary>
/// Opens TCP connections directly to previously approved IP addresses.
/// </summary>
public class WhoisTcpConnectionFactory : IWhoisConnectionFactory
{
    /// <summary>
    /// Initializes the built-in TCP connection factory.
    /// </summary>
    public WhoisTcpConnectionFactory()
    {
    }

    /// <summary>
    /// Opens a connection to an approved literal address without resolving the advertised host again.
    /// </summary>
    /// <param name="endpoint">The advertised endpoint metadata.</param>
    /// <param name="approvedAddress">The policy-approved literal address.</param>
    /// <param name="timeouts">The configured exchange timeouts.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The status-bearing connection result.</returns>
    /// <exception cref="System.ArgumentNullException">The endpoint, approved address, or timeout options are <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The endpoint or timeout options are invalid.</exception>
    public WhoisConnection OpenConnection(WhoisEndpoint endpoint, System.Net.IPAddress approvedAddress, WhoisTimeoutOptions timeouts, System.Threading.CancellationToken cancellationToken)
    {
        InputGuard.RequireReference(endpoint, "endpoint");
        InputGuard.RequireReference(approvedAddress, "approvedAddress");
        InputGuard.RequireReference(timeouts, "timeouts");
        InputGuard.ValidateEndpoint(endpoint, string.Empty, WhoisQueryKind.FreeForm);
        InputGuard.ValidateTimeouts(timeouts, string.Empty, WhoisQueryKind.FreeForm);

        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        if (cancellationToken.IsCancellationRequested)
        {
            timer.Stop();
            WhoisConnection cancelled = new WhoisConnection();
            cancelled.ApprovedAddress = approvedAddress;
            cancelled.Status = WhoisExchangeStatus.Cancelled;
            cancelled.Duration = timer.Elapsed;
            cancelled.Error = new System.OperationCanceledException(cancellationToken);
            return cancelled;
        }

        TimeoutBudget budget = new TimeoutBudget(timeouts, cancellationToken, System.TimeProvider.System);
        budget.BeginStage(WhoisOperationStage.Connect);
        int remainingMilliseconds = budget.GetRemainingMilliseconds();
        System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(approvedAddress.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        using (System.Threading.CancellationTokenSource operationCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            operationCancellation.CancelAfter(remainingMilliseconds);
            System.Threading.Tasks.ValueTask connectOperation = socket.ConnectAsync(approvedAddress, endpoint.Port, operationCancellation.Token);
            System.Threading.Tasks.Task connectTask = connectOperation.AsTask();
            System.Threading.Tasks.Task cancellationTask = System.Threading.Tasks.Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, operationCancellation.Token);
            System.Threading.Tasks.Task completedTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { connectTask, cancellationTask }).Result;
            timer.Stop();
            WhoisConnection result = CreateConnectionFromTask(connectTask, completedTask, socket, approvedAddress, cancellationToken, timer.Elapsed);
            operationCancellation.Cancel();
            return result;
        }
    }

    internal static WhoisConnection CreateConnectionFromTask(System.Threading.Tasks.Task connectTask, System.Threading.Tasks.Task completedTask, System.Net.Sockets.Socket socket, System.Net.IPAddress approvedAddress, System.Threading.CancellationToken cancellationToken, System.TimeSpan duration)
    {
        InputGuard.RequireReference(connectTask, "connectTask");
        InputGuard.RequireReference(completedTask, "completedTask");
        InputGuard.RequireReference(socket, "socket");
        InputGuard.RequireReference(approvedAddress, "approvedAddress");

        WhoisConnection result = new WhoisConnection();
        result.ApprovedAddress = approvedAddress;
        result.Duration = duration;

        if (cancellationToken.IsCancellationRequested)
        {
            result.Status = WhoisExchangeStatus.Cancelled;
            result.Error = new System.OperationCanceledException(cancellationToken);
        }
        else if (completedTask != connectTask || connectTask.IsCanceled)
        {
            result.Status = WhoisExchangeStatus.TimedOut;
            result.Error = new System.TimeoutException("TCP connection establishment timed out.");
        }
        else if (connectTask.IsFaulted)
        {
            result.Status = WhoisExchangeStatus.ConnectFailed;
            result.Error = connectTask.Exception;
            if (connectTask.Exception != null && connectTask.Exception.InnerException != null)
            {
                result.Error = connectTask.Exception.InnerException;
            }
        }
        else if (connectTask.IsCompletedSuccessfully && socket.Connected)
        {
            result.Stream = new System.Net.Sockets.NetworkStream(socket, true);
            result.Status = WhoisExchangeStatus.Complete;
        }
        else
        {
            result.Status = WhoisExchangeStatus.ConnectFailed;
            result.Error = new System.InvalidOperationException("TCP connection establishment did not produce a connected socket.");
        }

        if (result.Status != WhoisExchangeStatus.Complete)
        {
            socket.Dispose();
        }
        return result;
    }
}
