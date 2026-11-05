using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class SessionCache
{
    internal object Sync = new object();
    internal JmapClientOptions Options;
    internal SessionDiscovery Discovery;
    internal IJmapServiceLocator Locator = new JmapDnsServiceLocator();
    internal SessionValidator Validator = new SessionValidator();
    internal JmapDiscoveryOptions? CurrentDiscoveryOptions;
    internal JmapSession? CachedSession;
    internal string? ObservedSessionState;
    internal bool Stale;

    internal SessionCache(JmapClientOptions options, IJmapTransport transport)
    {
        if (options == null)
        {
            throw Validation("JMAP client options are required.", "options");
        }

        Options = options;
        Discovery = new SessionDiscovery(transport);
        if (options.ServiceLocator != null)
        {
            Locator = options.ServiceLocator;
        }

        CurrentDiscoveryOptions = options.DiscoveryOptions;
    }

    internal JmapSession? GetSession()
    {
        lock (Sync)
        {
            return CachedSession;
        }
    }

    internal bool IsSessionStale()
    {
        lock (Sync)
        {
            return Stale;
        }
    }

    internal Task<JmapSession> DiscoverSessionAsync(JmapDiscoveryOptions? options, CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw Validation("Session discovery options are required.", "discoveryOptions");
        }

        if (options.SessionUrl == null && options.Username == null)
        {
            throw Validation("A Session URL or discovery username is required.", "sessionUrl");
        }

        Task<string> located = Locator.LocateSessionUrlAsync(options, cancellationToken);
        TaskCompletionSource<JmapSession> completion = new TaskCompletionSource<JmapSession>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        located.ContinueWith(
            task => CompleteLocation(task, completion, options, cancellationToken),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal Task<JmapSession> RefreshSessionAsync(CancellationToken cancellationToken)
    {
        JmapDiscoveryOptions? discoveryOptions;
        lock (Sync)
        {
            discoveryOptions = CurrentDiscoveryOptions;
            if (CachedSession == null)
            {
                return DiscoverSessionAsync(discoveryOptions, cancellationToken);
            }
        }

        return FetchAndCacheSessionAsync(discoveryOptions, false, cancellationToken);
    }

    internal void CompleteLocation(
        Task<string> located,
        TaskCompletionSource<JmapSession> completion,
        JmapDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        if (located.IsCanceled)
        {
            completion.TrySetCanceled();
            return;
        }

        if (located.IsFaulted)
        {
            AggregateException? failure = located.Exception;
            if (failure == null)
            {
                completion.TrySetException(new InvalidOperationException("Session location failed without an exception."));
            }
            else
            {
                completion.TrySetException(failure.InnerExceptions);
            }

            return;
        }

        JmapDiscoveryOptions resolved = ResolvedDiscoveryOptions(options, located.Result);
        Task<JmapSession> fetched = FetchAndCacheSessionAsync(resolved, true, cancellationToken);
        fetched.ContinueWith(
            task => CompleteLocatedFetch(task, completion),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    internal void CompleteLocatedFetch(
        Task<JmapSession> fetched,
        TaskCompletionSource<JmapSession> completion)
    {
        if (fetched.IsCanceled)
        {
            completion.TrySetCanceled();
            return;
        }

        if (fetched.IsFaulted)
        {
            AggregateException? failure = fetched.Exception;
            if (failure == null)
            {
                completion.TrySetException(new InvalidOperationException("Session retrieval failed without an exception."));
            }
            else
            {
                completion.TrySetException(failure.InnerExceptions);
            }

            return;
        }

        completion.TrySetResult(fetched.Result);
    }

    internal JmapDiscoveryOptions ResolvedDiscoveryOptions(
        JmapDiscoveryOptions source,
        string sessionUrl)
    {
        JmapDiscoveryOptions result = new JmapDiscoveryOptions();
        result.SessionUrl = sessionUrl;
        result.Username = source.Username;
        result.MaximumRedirects = source.MaximumRedirects;
        result.DnsTimeoutMilliseconds = source.DnsTimeoutMilliseconds;
        result.AllowInsecureHttp = source.AllowInsecureHttp;
        return result;
    }

    internal Task<JmapSession> GetUsableSessionAsync(CancellationToken cancellationToken)
    {
        JmapSession? session;
        bool shouldRefresh;
        lock (Sync)
        {
            session = CachedSession;
            shouldRefresh = session == null || (Stale && Options.RefreshSessionOnStateChange);
        }

        if (!shouldRefresh && session != null)
        {
            return Task.FromResult(session);
        }

        return RefreshSessionAsync(cancellationToken);
    }

    internal void ObserveResponseSessionState(string? sessionState)
    {
        if (sessionState == null || !Validator.Values.IsValidUnicode(sessionState))
        {
            throw new JmapProtocolException(
                "A valid sessionState is required in a JMAP response.",
                "sessionState",
                default(JsonElement));
        }

        lock (Sync)
        {
            ObservedSessionState = sessionState;
            if (CachedSession != null && CachedSession.State != sessionState)
            {
                Stale = true;
            }
        }
    }

    internal Task<JmapSession> FetchAndCacheSessionAsync(
        JmapDiscoveryOptions? options,
        bool replacesDiscovery,
        CancellationToken cancellationToken)
    {
        Task<JmapSession> fetched = Discovery.RetrieveDirectSessionAsync(options, cancellationToken);
        TaskCompletionSource<JmapSession> completion = new TaskCompletionSource<JmapSession>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        fetched.ContinueWith(
            task => CompleteFetch(task, completion, options, replacesDiscovery),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return completion.Task;
    }

    internal void CompleteFetch(
        Task<JmapSession> fetched,
        TaskCompletionSource<JmapSession> completion,
        JmapDiscoveryOptions? options,
        bool replacesDiscovery)
    {
        if (fetched.IsCanceled)
        {
            completion.TrySetCanceled();
            return;
        }

        if (fetched.IsFaulted)
        {
            AggregateException? failure = fetched.Exception;
            if (failure == null)
            {
                completion.TrySetException(new InvalidOperationException("The Session retrieval task failed without an exception."));
            }
            else
            {
                completion.TrySetException(failure.InnerExceptions);
            }

            return;
        }

        JmapSession? session = fetched.Result;
        if (session == null)
        {
            completion.TrySetException(Validation("The Session endpoint returned no Session.", ""));
            return;
        }

        List<string> invalid = Validator.ValidateSession(session);
        if (invalid.Count != 0)
        {
            completion.TrySetException(new JmapValidationException(
                "The Session endpoint returned an invalid Session.",
                invalid,
                default(JsonElement)));
            return;
        }

        lock (Sync)
        {
            CachedSession = session;
            if (replacesDiscovery)
            {
                CurrentDiscoveryOptions = options;
                ObservedSessionState = null;
                Stale = false;
            }
            else
            {
                Stale = ObservedSessionState != null && ObservedSessionState != session.State;
            }
        }

        completion.TrySetResult(session);
    }

    internal JmapValidationException Validation(string message, string path)
    {
        List<string> invalid = new List<string>();
        invalid.Add(path);
        return new JmapValidationException(message, invalid, default(JsonElement));
    }
}
