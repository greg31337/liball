namespace cookies.Tests;

public class CookieConcurrencyConformanceTests
{
    static readonly DateTimeOffset EvaluationTime =
        new DateTimeOffset(2038, 7, 8, 9, 10, 11, TimeSpan.Zero);

    [Test]
    public void OneParserInstanceCanServeCoordinatedConcurrentCalls()
    {
        const int count = 24;
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };
        SetCookieParseResult[] setResults = new SetCookieParseResult[count];
        CookieHeaderParseResult[] headerResults = new CookieHeaderParseResult[count];
        using CountdownEvent ready = new CountdownEvent(count);
        using ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task[] tasks = new Task[count];

        for (int index = 0; index < count; index++)
        {
            int captured = index;
            tasks[index] = StartParticipant(() =>
            {
                ready.Signal();
                start.Wait();
                setResults[captured] = parser.TryParseSetCookie(
                    "set-" + captured + "=value-" + captured + "; Path=/",
                    options);
                headerResults[captured] = parser.TryParseCookieFields(
                    ["request-" + captured + "=value-" + captured],
                    options);
            });
        }

        Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True);
        start.Set();
        Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(10)), Is.True);

        for (int index = 0; index < count; index++)
        {
            SetCookie parsed = setResults[index].Cookie!;
            Assert.That(setResults[index].Success, Is.True);
            Assert.That(parsed.Name, Is.EqualTo("set-" + index));
            Assert.That(parsed.Value,
                Is.EqualTo("value-" + index));
            Assert.That(headerResults[index].Success, Is.True);
            Assert.That(headerResults[index].Pairs.Single().Name,
                Is.EqualTo("request-" + index));
            Assert.That(headerResults[index].Pairs.Single().Value,
                Is.EqualTo("value-" + index));
        }

        setResults[0].Cookie!.Name = "mutated";
        Assert.That(parser.ParseSetCookie("fresh=value", options).Name,
            Is.EqualTo("fresh"));
    }

    [Test]
    public void LookupUpsertAndDeleteCanStartTogetherWithoutCrossIdentityLoss()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie retained = Cookie("retained");
        StoredCookie removed = Cookie("removed");
        store.ApplyStoredCookie(retained);
        store.ApplyStoredCookie(removed);
        using Barrier barrier = new Barrier(4);
        StoredCookie? found = null;
        CookieSetResult? applied = null;
        int removedCount = -1;

        Task lookup = Task.Run(() =>
        {
            barrier.SignalAndWait();
            found = store.FindStoredCookie(Identity(retained));
        });
        Task upsert = Task.Run(() =>
        {
            barrier.SignalAndWait();
            applied = store.ApplyStoredCookie(Cookie("added"));
        });
        Task deletion = Task.Run(() =>
        {
            barrier.SignalAndWait();
            removedCount = store.RemoveStoredCookies(new CookieRemovalQuery
            {
                HasIdentity = true,
                Identity = Identity(removed)
            });
        });

        barrier.SignalAndWait();
        Assert.That(Task.WaitAll([lookup, upsert, deletion], TimeSpan.FromSeconds(10)),
            Is.True);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("retained"));
        Assert.That(applied!.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(removedCount, Is.EqualTo(1));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "retained", "added" }));
        Assert.That(store.FindStoredCookie(Identity(removed)), Is.Null);
    }

    [Test]
    public void SimultaneousExactIdentityReplacementsPreserveOneStableIdentity()
    {
        const int count = 20;
        ICookieStore store = new MemoryCookieStore();
        StoredCookie original = Cookie("shared");
        original.CreationTime = EvaluationTime.AddDays(-1);
        original.LastAccessTime = original.CreationTime;
        store.ApplyStoredCookie(original);
        CookieSetResult[] results = new CookieSetResult[count];
        using CountdownEvent ready = new CountdownEvent(count);
        using ManualResetEventSlim start = new ManualResetEventSlim(false);
        Task[] tasks = new Task[count];

        for (int index = 0; index < count; index++)
        {
            int captured = index;
            tasks[index] = StartParticipant(() =>
            {
                StoredCookie replacement = Cookie("shared");
                replacement.Value = "replacement-" + captured;
                replacement.CreationTime = EvaluationTime.AddMinutes(captured + 1);
                replacement.LastAccessTime = replacement.CreationTime;
                ready.Signal();
                start.Wait();
                results[captured] = store.ApplyStoredCookie(replacement);
            });
        }

        Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True);
        start.Set();
        Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(10)), Is.True);
        StoredCookie[] final = store.QueryStoredCookies(new CookieStoreQuery());
        CookieJarSnapshot snapshot = store.CaptureStoreSnapshot();

        Assert.That(results.Select(result => result.Status),
            Is.All.EqualTo(CookieSetStatus.Replaced));
        Assert.That(final, Has.Length.EqualTo(1));
        Assert.That(final[0].Name, Is.EqualTo("shared"));
        Assert.That(final[0].Value, Does.StartWith("replacement-"));
        Assert.That(final[0].CreationTime, Is.EqualTo(EvaluationTime.AddDays(-1)));
        Assert.That(final[0].SequenceNumber, Is.Zero);
        Assert.That(snapshot.NextSequenceNumber, Is.EqualTo(1));
    }

    [Test]
    public void SelectionPausedAfterLookupCannotResurrectAConcurrentlyExpiredCookie()
    {
        MemoryCookieStore inner = new MemoryCookieStore();
        StoredCookie expiring = Cookie("expiring");
        expiring.HasExpires = true;
        expiring.Persistent = true;
        expiring.Expires = EvaluationTime.AddMinutes(1);
        inner.ApplyStoredCookie(expiring);
        CoordinatedQueryCookieStore store = new CoordinatedQueryCookieStore(inner);
        ConcurrencyClock clock = new ConcurrencyClock(EvaluationTime);
        ICookieJar jar = Jar(store, clock);
        CookieSelectionResult? selection = null;

        Task selecting = Task.Run(() =>
        {
            selection = jar.SelectRequestCookies(RequestContext());
        });

        Assert.That(store.QueryReady.Wait(TimeSpan.FromSeconds(5)), Is.True);
        clock.Value = EvaluationTime.AddMinutes(2);
        int purged = jar.PurgeExpiredCookies();
        store.ReleaseQuery.Set();
        Assert.That(selecting.Wait(TimeSpan.FromSeconds(10)), Is.True);

        Assert.That(purged, Is.EqualTo(1));
        Assert.That(selection, Is.Not.Null);
        Assert.That(selection!.Success, Is.True);
        Assert.That(selection.Cookies, Is.Empty);
        Assert.That(selection.Matches.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.Expired));
        Assert.That(inner.QueryStoredCookies(new CookieStoreQuery()), Is.Empty);
    }

    [Test]
    public void AccessUpdateAndRemovalAreAtomicAndRemovalDoesNotResurrectState()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie removed = Cookie("removed");
        StoredCookie retained = Cookie("retained");
        store.ApplyStoredCookie(removed);
        store.ApplyStoredCookie(retained);
        DateTimeOffset accessTime = EvaluationTime.AddHours(1);
        using Barrier barrier = new Barrier(3);
        StoredCookie[]? updated = null;
        int removedCount = -1;

        Task updating = Task.Run(() =>
        {
            barrier.SignalAndWait();
            updated = store.UpdateStoredCookieAccess(
                [Identity(removed), Identity(retained)],
                accessTime);
        });
        Task deleting = Task.Run(() =>
        {
            barrier.SignalAndWait();
            removedCount = store.RemoveStoredCookies(new CookieRemovalQuery
            {
                HasIdentity = true,
                Identity = Identity(removed)
            });
        });

        barrier.SignalAndWait();
        Assert.That(Task.WaitAll([updating, deleting], TimeSpan.FromSeconds(10)),
            Is.True);
        StoredCookie[] final = store.QueryStoredCookies(new CookieStoreQuery());

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "retained" }).Or.EqualTo(new[] { "removed", "retained" }));
        Assert.That(removedCount, Is.EqualTo(1));
        Assert.That(final.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "retained" }));
        Assert.That(final[0].LastAccessTime, Is.EqualTo(accessTime));
    }

    [Test]
    public void ConcurrentSnapshotsAreCoherentDefensiveAndLoseNoDistinctUpserts()
    {
        const int count = 24;
        ICookieStore store = new MemoryCookieStore();
        using CountdownEvent ready = new CountdownEvent(count + 1);
        using ManualResetEventSlim start = new ManualResetEventSlim(false);
        using ManualResetEventSlim observerStarted = new ManualResetEventSlim(false);
        Task[] writers = new Task[count];
        int completed = 0;
        global::System.Collections.Concurrent.ConcurrentBag<int> observedCounts =
            new global::System.Collections.Concurrent.ConcurrentBag<int>();

        for (int index = 0; index < count; index++)
        {
            int captured = index;
            writers[index] = StartParticipant(() =>
            {
                ready.Signal();
                start.Wait();
                observerStarted.Wait();
                store.ApplyStoredCookie(Cookie("cookie-" + captured));
                global::System.Threading.Interlocked.Increment(ref completed);
            });
        }

        Task observer = StartParticipant(() =>
        {
            ready.Signal();
            start.Wait();
            bool keepObserving = true;
            while (keepObserving)
            {
                CookieJarSnapshot snapshot = store.CaptureStoreSnapshot();
                observedCounts.Add(snapshot.Cookies.Length);
                Assert.That(snapshot.NextSequenceNumber,
                    Is.EqualTo(snapshot.Cookies.Length));
                Assert.That(snapshot.Cookies.Select(cookie => cookie.SequenceNumber),
                    Is.EqualTo(Enumerable.Range(0, snapshot.Cookies.Length)
                        .Select(value => (long)value)));
                Assert.That(snapshot.Cookies.Select(cookie => cookie.Name).Distinct().Count(),
                    Is.EqualTo(snapshot.Cookies.Length));
                if (!observerStarted.IsSet)
                {
                    observerStarted.Set();
                }

                if (snapshot.Cookies.Length > 0)
                {
                    StoredCookie capturedCookie = snapshot.Cookies[0];
                    capturedCookie.Value = "mutated-snapshot";
                    Assert.That(store.FindStoredCookie(Identity(capturedCookie))!.Value,
                        Is.EqualTo("value"));
                }

                keepObserving = global::System.Threading.Volatile.Read(ref completed) < count;
                global::System.Threading.Thread.Yield();
            }
        });

        Assert.That(ready.Wait(TimeSpan.FromSeconds(5)), Is.True);
        start.Set();
        Task[] allTasks = new Task[count + 1];
        Array.Copy(writers, allTasks, count);
        allTasks[count] = observer;
        Assert.That(Task.WaitAll(allTasks, TimeSpan.FromSeconds(15)), Is.True);
        CookieJarSnapshot final = store.CaptureStoreSnapshot();

        Assert.That(observedCounts, Does.Contain(0));
        Assert.That(final.Cookies, Has.Length.EqualTo(count));
        Assert.That(final.NextSequenceNumber, Is.EqualTo(count));
        Assert.That(final.Cookies.Select(cookie => cookie.Name).Distinct().Count(),
            Is.EqualTo(count));
        Assert.That(final.Cookies.Select(cookie => cookie.Value),
            Is.All.EqualTo("value"));
    }

    static ICookieJar Jar(ICookieStore store, ConcurrencyClock clock)
    {
        CookieJarOptions options = new CookieJarOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            RequirePublicSuffixList = true
        };
        options.ParseOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        options.SerializationOptions.ComplianceMode = CookieComplianceMode.Rfc6265Bis;
        DefaultCookiePolicy policy = new DefaultCookiePolicy(
            options,
            new ConcurrencyPublicSuffixList(),
            new DefaultSecureOriginPolicy(false),
            clock);
        return new CookieJar(
            options,
            new CookieParser(),
            policy,
            store,
            new CookieSerializer(),
            clock);
    }

    static Task StartParticipant(Action action)
    {
        return Task.Factory.StartNew(
            action,
            global::System.Threading.CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    static CookieRequestContext RequestContext()
    {
        return new CookieRequestContext
        {
            RequestUri = new Uri("https://shop.example.test/account"),
            RequestMethod = "GET",
            SameSiteContext = CookieSameSiteContext.SameSite,
            AccessKind = CookieAccessKind.Http,
            HasTopLevelSite = true,
            TopLevelSite = "https://top.example"
        };
    }

    static StoredCookie Cookie(string name)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = "shop.example.test",
            Path = "/",
            CreationTime = EvaluationTime,
            LastAccessTime = EvaluationTime
        };
    }

    static CookieIdentity Identity(StoredCookie cookie)
    {
        return new CookieIdentity
        {
            Name = cookie.Name,
            Domain = cookie.Domain,
            Path = cookie.Path,
            HasPartitionKey = cookie.HasPartitionKey,
            PartitionKey = cookie.PartitionKey
        };
    }
}

public class CoordinatedQueryCookieStore : ICookieStore
{
    public ICookieStore Inner;
    public ManualResetEventSlim QueryReady;
    public ManualResetEventSlim ReleaseQuery;
    int PauseClaimed;

    public CoordinatedQueryCookieStore(ICookieStore inner)
    {
        Inner = inner;
        QueryReady = new ManualResetEventSlim(false);
        ReleaseQuery = new ManualResetEventSlim(false);
    }

    public StoredCookie? FindStoredCookie(CookieIdentity identity)
    {
        return Inner.FindStoredCookie(identity);
    }

    public StoredCookie[] QueryStoredCookies(CookieStoreQuery query)
    {
        StoredCookie[] result = Inner.QueryStoredCookies(query);
        if (result.Length > 0 &&
            global::System.Threading.Interlocked.CompareExchange(
                ref PauseClaimed, 1, 0) == 0)
        {
            QueryReady.Set();
            if (!ReleaseQuery.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new CookieStoreException("The coordinated query timed out.");
            }
        }

        return result;
    }

    public CookieSetResult ApplyStoredCookie(StoredCookie cookie)
    {
        return Inner.ApplyStoredCookie(cookie);
    }

    public StoredCookie[] UpdateStoredCookieAccess(
        CookieIdentity[] identities,
        DateTimeOffset accessTime)
    {
        return Inner.UpdateStoredCookieAccess(identities, accessTime);
    }

    public int RemoveStoredCookies(CookieRemovalQuery query)
    {
        return Inner.RemoveStoredCookies(query);
    }

    public CookieJarSnapshot CaptureStoreSnapshot()
    {
        return Inner.CaptureStoreSnapshot();
    }

    public void ReplaceStoreSnapshot(CookieJarSnapshot snapshot)
    {
        Inner.ReplaceStoreSnapshot(snapshot);
    }
}

public class ConcurrencyClock : ICookieClock
{
    public DateTimeOffset Value;

    public ConcurrencyClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        return Value;
    }
}

public class ConcurrencyPublicSuffixList : IPublicSuffixList
{
    public string? GetPublicSuffix(string canonicalDomain)
    {
        return "test";
    }

    public string? GetRegistrableDomain(string canonicalDomain)
    {
        if (canonicalDomain == "test")
        {
            return null;
        }

        return "example.test";
    }

    public bool IsPublicSuffix(string canonicalDomain)
    {
        return canonicalDomain == "test";
    }
}
