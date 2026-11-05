using uspto;

namespace uspto.Tests;

public class DataFallbackCoordinatorTests
{
    [Test]
    public void PublicClientResolvesConfiguredOnlineSource()
    {
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.OnlineCapability = Capability(FallbackAccess.Online, Provenance(DataProvenance.Odp, true, false, Time()));
        source.OnlineValue = Value("online", source.OnlineCapability.Provenance);
        IDataFallbackClient client = new DataFallbackClient(Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Online }));

        DataFallbackResult result = client.ResolveDataFallback(source, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Value, Is.EqualTo("online"));
            Assert.That(result.SelectedAccess, Is.EqualTo(FallbackAccess.Online));
        });
    }

    [Test]
    public void CallerOrderSelectsFreshCacheBeforeOnline()
    {
        DateTimeOffset now = Time();
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.CacheCapability = Capability(FallbackAccess.Cache, Provenance(DataProvenance.Odp, true, true, now.AddSeconds(-30)));
        source.CacheValue = Value("cached-official", source.CacheCapability.Provenance);
        source.OnlineCapability = Capability(FallbackAccess.Online, Provenance(DataProvenance.Odp, true, false, now));
        source.OnlineValue = Value("online-official", source.OnlineCapability.Provenance);
        DataFallbackPolicy policy = Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Cache, FallbackAccess.Online });
        policy.MaximumCacheAgeSeconds = 60;

        DataFallbackPolicy configured = Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Online });
        RequestContext context = new RequestContext { Fallback = policy };

        DataFallbackResult result = new DataFallbackCoordinator(configured).Resolve(source, context, now);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Value, Is.EqualTo("cached-official"));
            Assert.That(result.SelectedAccess, Is.EqualTo(FallbackAccess.Cache));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.FromCache, Is.True);
            Assert.That(result.Attempts, Has.Length.EqualTo(1));
            Assert.That(result.Attempts[0].CapabilityChecked, Is.True);
            Assert.That(result.Attempts[0].IsSelected, Is.True);
            Assert.That(source.CacheCapabilityCalls, Is.EqualTo(1));
            Assert.That(source.CacheLoadCalls, Is.EqualTo(1));
            Assert.That(source.OnlineCapabilityCalls, Is.Zero);
            Assert.That(source.OnlineLoadCalls, Is.Zero);
        });
    }

    [Test]
    public void StaleAndUnavailableCandidatesFallThroughToFreshLocalData()
    {
        DateTimeOffset now = Time();
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.CacheCapability = Capability(FallbackAccess.Cache, Provenance(DataProvenance.Odp, true, true, now.AddMinutes(-10)));
        source.OnlineCapability = Capability(FallbackAccess.Online, Provenance(DataProvenance.Odp, true, false, now));
        source.OnlineCapability.IsAvailable = false;
        source.LocalCapability = Capability(FallbackAccess.Local, Provenance(DataProvenance.LocalMirror, true, false, now.AddMinutes(-5)));
        source.LocalValue = Value("local-official", source.LocalCapability.Provenance);
        DataFallbackPolicy policy = Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Cache, FallbackAccess.Online, FallbackAccess.Local });
        policy.MaximumCacheAgeSeconds = 60;
        policy.MaximumLocalAgeSeconds = 600;

        DataFallbackResult result = new DataFallbackCoordinator(policy).Resolve(source, new RequestContext(), now);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.SelectedAccess, Is.EqualTo(FallbackAccess.Local));
            Assert.That(result.Value, Is.EqualTo("local-official"));
            Assert.That(result.Attempts.Select(item => item.RejectionCode), Is.EqualTo(new[] { "CACHE_STALE", "SOURCE_UNAVAILABLE", null }));
            Assert.That(result.Attempts[0].LoadAttempted, Is.False);
            Assert.That(result.Attempts[1].LoadAttempted, Is.False);
            Assert.That(result.Attempts[2].IsSelected, Is.True);
            Assert.That(source.CacheLoadCalls, Is.Zero);
            Assert.That(source.OnlineLoadCalls, Is.Zero);
            Assert.That(source.LocalLoadCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ResearchLocalDataCannotSilentlyReplaceOfficialOnlineData()
    {
        DateTimeOffset now = Time();
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        PatentDataProvenance mislabeledResearch = Provenance(DataProvenance.LocalMirror, true, false, now);
        mislabeledResearch.FieldSources[0].Source = DataProvenance.PatentsView;
        mislabeledResearch.FieldSources[0].IsAuthoritative = false;
        source.LocalCapability = Capability(FallbackAccess.Local, mislabeledResearch);
        source.LocalValue = Value("research-local", source.LocalCapability.Provenance);
        source.OnlineCapability = Capability(FallbackAccess.Online, Provenance(DataProvenance.Odp, true, false, now));
        source.OnlineValue = Value("official-online", source.OnlineCapability.Provenance);
        DataFallbackPolicy policy = Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Local, FallbackAccess.Online });

        DataFallbackResult result = new DataFallbackCoordinator(policy).Resolve(source, new RequestContext(), now);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo("official-online"));
            Assert.That(result.SelectedAccess, Is.EqualTo(FallbackAccess.Online));
            Assert.That(result.Attempts[0].RejectionCode, Is.EqualTo("AUTHORITY_MISMATCH"));
            Assert.That(result.Attempts[0].LoadAttempted, Is.False);
            Assert.That(source.LocalLoadCalls, Is.Zero);
            Assert.That(source.OnlineLoadCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExplicitResearchRequirementCanSelectResearchLocalData()
    {
        DateTimeOffset now = Time();
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.LocalCapability = Capability(FallbackAccess.Local, Provenance(DataProvenance.LocalMirror, false, false, now));
        source.LocalValue = Value("research-local", source.LocalCapability.Provenance);
        DataFallbackPolicy policy = Policy(FallbackAuthority.ResearchOnly, new[] { FallbackAccess.Local });

        DataFallbackResult result = new DataFallbackCoordinator(policy).Resolve(source, new RequestContext(), now);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Value, Is.EqualTo("research-local"));
            Assert.That(result.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.SelectedAccess, Is.EqualTo(FallbackAccess.Local));
        });
    }

    [Test]
    public void UnsupportedCapabilityIsSkippedWithoutLoading()
    {
        DateTimeOffset now = Time();
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.LocalCapability = Capability(FallbackAccess.Local, Provenance(DataProvenance.LocalMirror, true, false, now));
        source.LocalCapability.SupportsOperation = false;
        DataFallbackPolicy policy = Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Local });

        DataFallbackResult result = new DataFallbackCoordinator(policy).Resolve(source, new RequestContext(), now);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.SelectedAccess, Is.EqualTo(FallbackAccess.Unknown));
            Assert.That(result.Attempts.Single().RejectionCode, Is.EqualTo("CAPABILITY_UNSUPPORTED"));
            Assert.That(result.Attempts.Single().CapabilityChecked, Is.True);
            Assert.That(source.LocalLoadCalls, Is.Zero);
        });
    }

    [Test]
    public void LoadFailureAndMislabeledResultAreReportedBeforeNextCandidate()
    {
        DateTimeOffset now = Time();
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        source.CacheCapability = Capability(FallbackAccess.Cache, Provenance(DataProvenance.Odp, true, true, now));
        source.CacheFailure = new ApiException("Cached value could not be decoded.", ApiService.Odp, 500, true, "CACHE_PARSE", null, "cache-request", false, null);
        source.LocalCapability = Capability(FallbackAccess.Local, Provenance(DataProvenance.LocalMirror, true, false, now));
        source.LocalValue = Value("research-result", Provenance(DataProvenance.LocalMirror, false, false, now));
        source.OnlineCapability = Capability(FallbackAccess.Online, Provenance(DataProvenance.Odp, true, false, now));
        source.OnlineValue = Value("official-result", source.OnlineCapability.Provenance);
        DataFallbackPolicy policy = Policy(FallbackAuthority.AuthoritativeOnly, new[] { FallbackAccess.Cache, FallbackAccess.Local, FallbackAccess.Online });

        DataFallbackResult result = new DataFallbackCoordinator(policy).Resolve(source, new RequestContext(), now);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Value, Is.EqualTo("official-result"));
            Assert.That(result.Attempts[0].RejectionCode, Is.EqualTo("LOAD_FAILED"));
            Assert.That(result.Attempts[1].RejectionCode, Is.EqualTo("RESULT_AUTHORITY_MISMATCH"));
            Assert.That(result.Attempts[2].IsSelected, Is.True);
            Assert.That(result.Errors, Has.Length.EqualTo(1));
            Assert.That(result.Errors[0].Section, Is.EqualTo("cache"));
            Assert.That(result.Errors[0].ErrorCode, Is.EqualTo("CACHE_PARSE"));
        });
    }

    [Test]
    public void InvalidPolicyAndCancellationStopBeforeSourceAccess()
    {
        DataFallbackPolicy duplicate = Policy(FallbackAuthority.Either, new[] { FallbackAccess.Online, FallbackAccess.Online });
        ValidationException duplicateFailure = Assert.Throws<ValidationException>(() => new DataFallbackCoordinator(duplicate))!;
        DataFallbackPolicy negative = Policy(FallbackAuthority.Either, new[] { FallbackAccess.Cache });
        negative.MaximumCacheAgeSeconds = -1;
        ValidationException freshnessFailure = Assert.Throws<ValidationException>(() => new DataFallbackCoordinator(negative))!;
        RecordingDataFallbackSource source = new RecordingDataFallbackSource();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => new DataFallbackCoordinator(Policy(FallbackAuthority.Either, new[] { FallbackAccess.Online })).Resolve(source, new RequestContext { CancellationToken = cancellation.Token }, Time()));

        Assert.Multiple(() =>
        {
            Assert.That(duplicateFailure.ErrorCode, Is.EqualTo("DUPLICATE_FALLBACK_ACCESS"));
            Assert.That(freshnessFailure.ErrorCode, Is.EqualTo("INVALID_FALLBACK_FRESHNESS"));
            Assert.That(source.OnlineCapabilityCalls, Is.Zero);
        });
    }

    private static DataFallbackPolicy Policy(FallbackAuthority authority, FallbackAccess[] accessOrder)
    {
        return new DataFallbackPolicy
        {
            Authority = authority,
            AccessOrder = accessOrder,
            MaximumCacheAgeSeconds = 300,
            HasMaximumCacheAge = true,
            MaximumLocalAgeSeconds = 3600,
            HasMaximumLocalAge = true
        };
    }

    private static DataFallbackCapability Capability(FallbackAccess access, PatentDataProvenance provenance)
    {
        return new DataFallbackCapability
        {
            Access = access,
            IsAvailable = true,
            SupportsOperation = true,
            Provenance = provenance
        };
    }

    private static DataFallbackValue Value(object value, PatentDataProvenance provenance)
    {
        return new DataFallbackValue { Value = value, Provenance = provenance };
    }

    private static PatentDataProvenance Provenance(DataProvenance source, bool authoritative, bool fromCache, DateTimeOffset timestamp)
    {
        PatentDataProvenance result = new PatentDataProvenance
        {
            Source = source,
            RetrievedAt = timestamp,
            HasRetrievedAt = true,
            FromCache = fromCache,
            IsAuthoritative = authoritative
        };

        if (source == DataProvenance.LocalMirror)
        {
            result.FieldSources = new[]
            {
                new FieldDataProvenance
                {
                    FieldPath = "*",
                    Source = authoritative ? DataProvenance.Odp : DataProvenance.PatentsView,
                    Delivery = DataProvenance.LocalMirror,
                    IsAuthoritative = authoritative
                }
            };
        }

        return result;
    }

    private static DateTimeOffset Time()
    {
        return new DateTimeOffset(2026, 9, 11, 20, 0, 0, TimeSpan.Zero);
    }
}

internal class RecordingDataFallbackSource : IDataFallbackSource
{
    internal DataFallbackCapability OnlineCapability = new DataFallbackCapability();
    internal DataFallbackCapability CacheCapability = new DataFallbackCapability();
    internal DataFallbackCapability LocalCapability = new DataFallbackCapability();
    internal DataFallbackValue OnlineValue = new DataFallbackValue();
    internal DataFallbackValue CacheValue = new DataFallbackValue();
    internal DataFallbackValue LocalValue = new DataFallbackValue();
    internal Exception? OnlineFailure = null;
    internal Exception? CacheFailure = null;
    internal Exception? LocalFailure = null;
    internal int OnlineCapabilityCalls = 0;
    internal int CacheCapabilityCalls = 0;
    internal int LocalCapabilityCalls = 0;
    internal int OnlineLoadCalls = 0;
    internal int CacheLoadCalls = 0;
    internal int LocalLoadCalls = 0;

    public DataFallbackCapability GetOnlineFallbackCapability(RequestContext context)
    {
        OnlineCapabilityCalls += 1;
        return OnlineCapability;
    }

    public DataFallbackCapability GetCacheFallbackCapability(RequestContext context)
    {
        CacheCapabilityCalls += 1;
        return CacheCapability;
    }

    public DataFallbackCapability GetLocalFallbackCapability(RequestContext context)
    {
        LocalCapabilityCalls += 1;
        return LocalCapability;
    }

    public DataFallbackValue LoadOnlineFallbackValue(RequestContext context)
    {
        OnlineLoadCalls += 1;
        if (OnlineFailure != null)
        {
            throw OnlineFailure;
        }

        return OnlineValue;
    }

    public DataFallbackValue LoadCacheFallbackValue(RequestContext context)
    {
        CacheLoadCalls += 1;
        if (CacheFailure != null)
        {
            throw CacheFailure;
        }

        return CacheValue;
    }

    public DataFallbackValue LoadLocalFallbackValue(RequestContext context)
    {
        LocalLoadCalls += 1;
        if (LocalFailure != null)
        {
            throw LocalFailure;
        }

        return LocalValue;
    }
}
