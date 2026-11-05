namespace whois;

/// <summary>
/// Resolves IP literals and DNS host names with deterministic address ordering.
/// </summary>
public class WhoisDnsResolver : IWhoisHostResolver
{
    /// <summary>
    /// Initializes the built-in resolver with the default resolution timeout.
    /// </summary>
    public WhoisDnsResolver()
    {
    }

    /// <summary>
    /// Resolves an endpoint host into ordered, distinct IP address candidates.
    /// </summary>
    /// <param name="endpoint">The endpoint whose host is resolved.</param>
    /// <param name="preference">The preferred address-family ordering.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The status-bearing resolution result.</returns>
    /// <exception cref="System.ArgumentNullException">The endpoint is <c>null</c>.</exception>
    /// <exception cref="WhoisQueryException">The endpoint host or port is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">The address preference is not supported.</exception>
    public WhoisResolution ResolveHost(WhoisEndpoint endpoint, WhoisAddressPreference preference, System.Threading.CancellationToken cancellationToken)
    {
        InputGuard.RequireReference(endpoint, "endpoint");
        InputGuard.ValidateEndpoint(endpoint, string.Empty, WhoisQueryKind.FreeForm);
        ValidatePreference(preference);

        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        if (cancellationToken.IsCancellationRequested)
        {
            timer.Stop();
            return CreateResolutionFromTask(null, null, preference, cancellationToken, timer.Elapsed);
        }

        System.UriHostNameType hostType = System.Uri.CheckHostName(endpoint.Host);
        if (hostType == System.UriHostNameType.IPv4 || hostType == System.UriHostNameType.IPv6)
        {
            System.Net.IPAddress address = System.Net.IPAddress.Parse(endpoint.Host);
            timer.Stop();
            WhoisResolution literalResult = new WhoisResolution();
            literalResult.Addresses = new System.Net.IPAddress[] { address };
            literalResult.Status = WhoisExchangeStatus.Complete;
            literalResult.Duration = timer.Elapsed;
            return literalResult;
        }

        using (System.Threading.CancellationTokenSource operationCancellation = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            operationCancellation.CancelAfter(WhoisDefaults.ResolveTimeout);
            System.Threading.Tasks.Task<System.Net.IPAddress[]> resolutionTask = System.Net.Dns.GetHostAddressesAsync(endpoint.Host, operationCancellation.Token);
            System.Threading.Tasks.Task cancellationTask = System.Threading.Tasks.Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, operationCancellation.Token);
            System.Threading.Tasks.Task completedTask = System.Threading.Tasks.Task.WhenAny(new System.Threading.Tasks.Task[] { resolutionTask, cancellationTask }).Result;
            timer.Stop();
            WhoisResolution result = CreateResolutionFromTask(resolutionTask, completedTask, preference, cancellationToken, timer.Elapsed);
            operationCancellation.Cancel();
            return result;
        }
    }

    internal static System.Net.IPAddress[] OrderAddresses(System.Net.IPAddress[] addresses, WhoisAddressPreference preference)
    {
        InputGuard.RequireReference(addresses, "addresses");
        ValidatePreference(preference);

        System.Collections.Generic.List<System.Net.IPAddress> distinct = new System.Collections.Generic.List<System.Net.IPAddress>();
        int index = 0;
        while (index < addresses.Length)
        {
            System.Net.IPAddress address = addresses[index];
            if (address != null && !distinct.Contains(address))
            {
                distinct.Add(address);
            }
            index = index + 1;
        }

        if (preference == WhoisAddressPreference.System)
        {
            return distinct.ToArray();
        }

        System.Net.Sockets.AddressFamily preferredFamily = System.Net.Sockets.AddressFamily.InterNetwork;
        if (preference == WhoisAddressPreference.IPv6First)
        {
            preferredFamily = System.Net.Sockets.AddressFamily.InterNetworkV6;
        }

        System.Collections.Generic.List<System.Net.IPAddress> ordered = new System.Collections.Generic.List<System.Net.IPAddress>();
        index = 0;
        while (index < distinct.Count)
        {
            if (distinct[index].AddressFamily == preferredFamily)
            {
                ordered.Add(distinct[index]);
            }
            index = index + 1;
        }
        index = 0;
        while (index < distinct.Count)
        {
            if (distinct[index].AddressFamily != preferredFamily)
            {
                ordered.Add(distinct[index]);
            }
            index = index + 1;
        }
        return ordered.ToArray();
    }

    internal static WhoisResolution CreateResolutionFromTask(System.Threading.Tasks.Task<System.Net.IPAddress[]>? resolutionTask, System.Threading.Tasks.Task? completedTask, WhoisAddressPreference preference, System.Threading.CancellationToken cancellationToken, System.TimeSpan duration)
    {
        WhoisResolution result = new WhoisResolution();
        result.Duration = duration;

        if (cancellationToken.IsCancellationRequested)
        {
            result.Status = WhoisExchangeStatus.Cancelled;
            result.Error = new System.OperationCanceledException(cancellationToken);
            return result;
        }
        if (resolutionTask == null || completedTask != resolutionTask)
        {
            result.Status = WhoisExchangeStatus.TimedOut;
            result.Error = new System.TimeoutException("Host resolution timed out.");
            return result;
        }
        if (resolutionTask.IsCanceled)
        {
            result.Status = WhoisExchangeStatus.TimedOut;
            result.Error = new System.TimeoutException("Host resolution timed out.");
            return result;
        }
        if (resolutionTask.IsFaulted)
        {
            result.Status = WhoisExchangeStatus.ResolveFailed;
            result.Error = resolutionTask.Exception;
            if (resolutionTask.Exception != null && resolutionTask.Exception.InnerException != null)
            {
                result.Error = resolutionTask.Exception.InnerException;
            }
            return result;
        }
        if (!resolutionTask.IsCompletedSuccessfully)
        {
            result.Status = WhoisExchangeStatus.ResolveFailed;
            result.Error = new System.InvalidOperationException("Host resolution did not reach a terminal state.");
            return result;
        }

        result.Addresses = OrderAddresses(resolutionTask.Result, preference);
        if (result.Addresses.Length == 0)
        {
            result.Status = WhoisExchangeStatus.ResolveFailed;
            result.Error = new System.InvalidOperationException("Host resolution returned no addresses.");
            return result;
        }
        result.Status = WhoisExchangeStatus.Complete;
        return result;
    }

    internal static void ValidatePreference(WhoisAddressPreference preference)
    {
        if (preference != WhoisAddressPreference.System && preference != WhoisAddressPreference.IPv4First && preference != WhoisAddressPreference.IPv6First)
        {
            throw new System.ArgumentOutOfRangeException("preference");
        }
    }
}
