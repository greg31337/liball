using System.Text;

namespace dns.Tests.Api;

public class MemoryDnsCacheTests
{
    [Test]
    public void CanonicalKeysStoreEveryEntryKindWithIndependentOwnership()
    {
        DnsCacheEntryKind[] kinds =
        [
            DnsCacheEntryKind.Positive,
            DnsCacheEntryKind.NegativeName,
            DnsCacheEntryKind.NegativeData,
            DnsCacheEntryKind.Failure
        ];
        int index = 0;
        while (index < kinds.Length)
        {
            MemoryDnsCache cache = new MemoryDnsCache();
            DnsCacheEntry entry = Entry("WWW", kinds[index], 10);
            cache.Store(entry);
            entry.Name.Labels[0][0] = (byte)'x';
            entry.RecordSets[0].Records[0].RawData[0] = 99;

            DnsCacheLookup first = cache.Lookup(
                Request("www"),
                Clock(2000));
            Assert.That(first.Found, Is.True);
            Assert.That(first.Entry, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(first.Entry!.Kind, Is.EqualTo(kinds[index]));
                Assert.That(first.Entry.Name.Labels[0],
                    Is.EqualTo(Encoding.ASCII.GetBytes("www")));
                Assert.That(first.Entry.RecordTypeValue, Is.EqualTo(1));
                Assert.That(first.Entry.ClassValue, Is.EqualTo(1));
                Assert.That(first.Entry.RecordSets[0].Records[0].RawData[0],
                    Is.EqualTo(1));
            });

            first.Entry!.RecordSets[0].Records[0].RawData[0] = 77;
            DnsCacheLookup second = cache.Lookup(
                Request("WWW"),
                Clock(2000));
            Assert.That(second.Entry!.RecordSets[0].Records[0].RawData[0],
                Is.EqualTo(1));
            index = index + 1;
        }
    }

    [Test]
    public void CanonicalKeysSeparateRawTypesClassesAndAbsoluteNames()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry entry = Entry(
            "MiXeD.Example",
            DnsCacheEntryKind.Positive,
            60);
        entry.RecordType = DnsRecordType.A;
        entry.RecordTypeValue = 65400;
        entry.Class = DnsClass.None;
        entry.ClassValue = 65410;
        cache.Store(entry);

        DnsResolveRequest matching = Request("mixed.example");
        matching.RecordType = DnsRecordType.A;
        matching.RecordTypeValue = 65400;
        matching.Class = DnsClass.None;
        matching.ClassValue = 65410;
        DnsResolveRequest otherType = Request("mixed.example");
        otherType.RecordType = DnsRecordType.A;
        otherType.RecordTypeValue = 65401;
        otherType.Class = DnsClass.None;
        otherType.ClassValue = 65410;
        DnsResolveRequest otherClass = Request("mixed.example");
        otherClass.RecordType = DnsRecordType.A;
        otherClass.RecordTypeValue = 65400;
        otherClass.Class = DnsClass.None;
        otherClass.ClassValue = 65411;
        DnsResolveRequest relative = Request("mixed.example");
        relative.Name.IsAbsolute = false;
        relative.RecordType = DnsRecordType.A;
        relative.RecordTypeValue = 65400;
        relative.Class = DnsClass.None;
        relative.ClassValue = 65410;

        DnsCacheLookup found = cache.Lookup(matching, Clock(2000));

        Assert.Multiple(() =>
        {
            Assert.That(found.Found, Is.True);
            Assert.That(found.Entry!.RecordTypeValue, Is.EqualTo(65400));
            Assert.That(found.Entry.ClassValue, Is.EqualTo(65410));
            Assert.That(found.Entry.Name.Labels[0],
                Is.EqualTo(Encoding.ASCII.GetBytes("mixed")));
            Assert.That(cache.Lookup(otherType, Clock(2000)).Found, Is.False);
            Assert.That(cache.Lookup(otherClass, Clock(2000)).Found, Is.False);
            Assert.That(cache.Lookup(relative, Clock(2000)).Found, Is.False);
        });
    }

    [Test]
    public void LookupAgesTimeToLiveAndExpiresAtTheMonotonicDeadline()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry entry = Entry("age", DnsCacheEntryKind.Positive, 10);
        entry.RecordSets[0].Records[0].TimeToLive = 15;
        entry.Message = new DnsMessage();
        entry.Message.Answers.Add(Record("age", 20, 1));
        DnsResourceRecord opt = Record("age", 32768, 41);
        entry.Message.Additionals.Add(opt);
        entry.NegativeSoa = Record("age", 10, 6);
        cache.Store(entry);

        DnsCacheLookup lookup = cache.Lookup(Request("age"), Clock(3500));

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Found, Is.True);
            Assert.That(lookup.Fresh, Is.True);
            Assert.That(lookup.Stale, Is.False);
            Assert.That(lookup.HasRemainingTimeToLive, Is.True);
            Assert.That(lookup.RemainingTimeToLive, Is.EqualTo(8));
            Assert.That(lookup.LookedUpAt.MonotonicMilliseconds,
                Is.EqualTo(3500));
            Assert.That(lookup.Entry!.RecordSets[0].TimeToLive,
                Is.EqualTo(8));
            Assert.That(lookup.Entry.RecordSets[0].Records[0].TimeToLive,
                Is.EqualTo(13));
            Assert.That(lookup.Entry.Message!.Answers[0].TimeToLive,
                Is.EqualTo(18));
            Assert.That(lookup.Entry.Message.Additionals[0].TimeToLive,
                Is.EqualTo(32768));
            Assert.That(lookup.Entry.NegativeSoa!.TimeToLive,
                Is.EqualTo(8));
        });

        DnsCacheLookup finalSecond = cache.Lookup(
            Request("age"),
            Clock(10999));
        DnsCacheLookup expired = cache.Lookup(
            Request("age"),
            Clock(11000));
        Assert.Multiple(() =>
        {
            Assert.That(finalSecond.Found, Is.True);
            Assert.That(finalSecond.RemainingTimeToLive, Is.EqualTo(1));
            Assert.That(expired.Found, Is.False);
            Assert.That(expired.Entry, Is.Null);
        });
    }

    [Test]
    public void LookupUsesMonotonicTimeAndRoundsPartialSecondsUp()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry entry = Entry(
            "monotonic.example",
            DnsCacheEntryKind.Positive,
            10);
        entry.StoredAt.UnixTimeMilliseconds = 500000;
        entry.ExpiresAt.UnixTimeMilliseconds = 510000;
        cache.Store(entry);
        DnsClockValue firstClock = Clock(1501);
        firstClock.UnixTimeMilliseconds = 900000;
        DnsClockValue secondClock = Clock(2001);
        secondClock.UnixTimeMilliseconds = 100;

        DnsCacheLookup partialFirstSecond = cache.Lookup(
            Request("monotonic.example"),
            firstClock);
        DnsCacheLookup partialSecondSecond = cache.Lookup(
            Request("monotonic.example"),
            secondClock);

        Assert.Multiple(() =>
        {
            Assert.That(partialFirstSecond.Found, Is.True);
            Assert.That(partialFirstSecond.RemainingTimeToLive,
                Is.EqualTo(10));
            Assert.That(partialFirstSecond.Entry!.RecordSets[0].TimeToLive,
                Is.EqualTo(10));
            Assert.That(partialSecondSecond.Found, Is.True);
            Assert.That(partialSecondSecond.RemainingTimeToLive,
                Is.EqualTo(9));
            Assert.That(partialSecondSecond.Entry!.RecordSets[0].TimeToLive,
                Is.EqualTo(9));
            Assert.That(partialSecondSecond.LookedUpAt.UnixTimeMilliseconds,
                Is.EqualTo(100));
        });
    }

    [Test]
    public void ZeroTimeToLiveIsNotCachedAndRemovesAnExistingKey()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.Store(Entry("zero", DnsCacheEntryKind.Positive, 10));
        DnsCacheEntry zero = Entry("ZERO", DnsCacheEntryKind.Positive, 0);
        zero.ExpiresAt.MonotonicMilliseconds = 2000;

        cache.Store(zero);

        Assert.Multiple(() =>
        {
            Assert.That(cache.Entries, Is.Empty);
            Assert.That(cache.Lookup(Request("zero"), Clock(1000)).Found,
                Is.False);
        });
    }

    [Test]
    public void CapacityEvictsTheLeastRecentlyUsedEntry()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.MaximumEntryCount = 2;
        cache.Store(Entry("one", DnsCacheEntryKind.Positive, 60));
        cache.Store(Entry("two", DnsCacheEntryKind.Positive, 60));
        Assert.That(cache.Lookup(Request("one"), Clock(2000)).Found, Is.True);

        cache.Store(Entry("three", DnsCacheEntryKind.Positive, 60));

        Assert.Multiple(() =>
        {
            Assert.That(cache.Entries, Has.Count.EqualTo(2));
            Assert.That(cache.Lookup(Request("one"), Clock(2000)).Found,
                Is.True);
            Assert.That(cache.Lookup(Request("two"), Clock(2000)).Found,
                Is.False);
            Assert.That(cache.Lookup(Request("three"), Clock(2000)).Found,
                Is.True);
        });
    }

    [Test]
    public void SubtreeInvalidationAndFlushRemoveTheIntendedEntries()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.Store(Entry("example", DnsCacheEntryKind.Positive, 60));
        cache.Store(Entry("www.example", DnsCacheEntryKind.Positive, 60));
        cache.Store(Entry("deep.www.example", DnsCacheEntryKind.Positive, 60));
        cache.Store(Entry("example.net", DnsCacheEntryKind.Positive, 60));

        cache.InvalidateSubtree(Name("EXAMPLE"));

        Assert.Multiple(() =>
        {
            Assert.That(cache.Lookup(Request("example"), Clock(2000)).Found,
                Is.False);
            Assert.That(cache.Lookup(Request("www.example"), Clock(2000)).Found,
                Is.False);
            Assert.That(cache.Lookup(Request("deep.www.example"), Clock(2000)).Found,
                Is.False);
            Assert.That(cache.Lookup(Request("example.net"), Clock(2000)).Found,
                Is.True);
        });

        cache.Flush();
        Assert.That(cache.Entries, Is.Empty);
    }

    [Test]
    public void NegativeEntriesDeriveAndAgeTheLesserSoaTimeToLive()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry entry = Entry(
            "nodata.example",
            DnsCacheEntryKind.NegativeData,
            900);
        entry.NegativeSoa = SoaRecord("example", 120, 30);
        entry.Message = new DnsMessage();
        entry.Message.Authorities.Add(SoaRecord("example", 120, 30));

        cache.Store(entry);
        DnsCacheLookup stored = cache.Lookup(
            Request("nodata.example"),
            Clock(1000));
        DnsCacheLookup aged = cache.Lookup(
            Request("nodata.example"),
            Clock(3000));

        Assert.Multiple(() =>
        {
            Assert.That(stored.Found, Is.True);
            Assert.That(stored.Entry!.OriginalTimeToLive, Is.EqualTo(30));
            Assert.That(stored.Entry.ExpiresAt.MonotonicMilliseconds,
                Is.EqualTo(31000));
            Assert.That(stored.Entry.NegativeSoa!.TimeToLive,
                Is.EqualTo(30));
            Assert.That(stored.Entry.Message!.Authorities[0].TimeToLive,
                Is.EqualTo(30));
            Assert.That(aged.RemainingTimeToLive, Is.EqualTo(28));
            Assert.That(aged.Entry!.NegativeSoa!.TimeToLive, Is.EqualTo(28));
        });

        MemoryDnsCache soaTtlCache = new MemoryDnsCache();
        DnsCacheEntry shorterSoa = Entry(
            "short.example",
            DnsCacheEntryKind.NegativeName,
            900);
        shorterSoa.NegativeSoa = SoaRecord("example", 20, 90);
        soaTtlCache.Store(shorterSoa);
        Assert.That(soaTtlCache.Lookup(
            Request("short.example"),
            Clock(1000)).Entry!.OriginalTimeToLive, Is.EqualTo(20));
    }

    [Test]
    public void NegativeEntryWithoutUsableSoaIsNotCached()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry missing = Entry(
            "missing-soa.example",
            DnsCacheEntryKind.NegativeName,
            60);
        missing.NegativeSoa = null;
        DnsCacheEntry zero = Entry(
            "zero-soa.example",
            DnsCacheEntryKind.NegativeData,
            60);
        zero.NegativeSoa = SoaRecord("example", 60, 0);

        cache.Store(missing);
        cache.Store(zero);

        Assert.Multiple(() =>
        {
            Assert.That(cache.Lookup(
                Request("missing-soa.example"), Clock(1000)).Found,
                Is.False);
            Assert.That(cache.Lookup(
                Request("zero-soa.example"), Clock(1000)).Found,
                Is.False);
        });
    }

    [Test]
    public void NameErrorsFormCutsWhileNoDataRemainsTypeSpecific()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.Store(Entry(
            "child.missing.example",
            DnsCacheEntryKind.Positive,
            60));
        cache.Store(Entry(
            "missing.example",
            DnsCacheEntryKind.NegativeName,
            60));

        DnsCacheLookup sameName = cache.Lookup(
            RequestForType("missing.example", DnsRecordType.Aaaa),
            Clock(2000));
        DnsCacheLookup descendant = cache.Lookup(
            RequestForType("deep.child.missing.example", DnsRecordType.Txt),
            Clock(2000));

        Assert.Multiple(() =>
        {
            Assert.That(sameName.Entry!.Kind,
                Is.EqualTo(DnsCacheEntryKind.NegativeName));
            Assert.That(descendant.Entry!.Kind,
                Is.EqualTo(DnsCacheEntryKind.NegativeName));
            Assert.That(cache.Lookup(Request("example"), Clock(2000)).Found,
                Is.False);
            Assert.That(cache.Lookup(
                Request("other.example"), Clock(2000)).Found,
                Is.False);
            Assert.That(cache.Entries, Has.Count.EqualTo(1));
        });

        DnsCacheEntry noData = Entry(
            "typed.example",
            DnsCacheEntryKind.NegativeData,
            60);
        noData.RecordType = DnsRecordType.Aaaa;
        noData.RecordTypeValue = 0;
        cache.Store(noData);
        Assert.Multiple(() =>
        {
            Assert.That(cache.Lookup(
                RequestForType("typed.example", DnsRecordType.Aaaa),
                Clock(2000)).Found, Is.True);
            Assert.That(cache.Lookup(
                RequestForType("typed.example", DnsRecordType.A),
                Clock(2000)).Found, Is.False);
        });
    }

    [Test]
    public void FailureEntriesAreBoundedFromOneSecondToFiveMinutes()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.Store(Entry("brief.example", DnsCacheEntryKind.Failure, 0));
        cache.Store(Entry("long.example", DnsCacheEntryKind.Failure, 900));

        DnsCacheLookup brief = cache.Lookup(
            Request("brief.example"),
            Clock(1000));
        DnsCacheLookup longFailure = cache.Lookup(
            Request("long.example"),
            Clock(1000));

        Assert.Multiple(() =>
        {
            Assert.That(brief.Entry!.OriginalTimeToLive, Is.EqualTo(1));
            Assert.That(brief.Entry.ExpiresAt.MonotonicMilliseconds,
                Is.EqualTo(2000));
            Assert.That(longFailure.Entry!.OriginalTimeToLive,
                Is.EqualTo(300));
            Assert.That(longFailure.Entry.ExpiresAt.MonotonicMilliseconds,
                Is.EqualTo(301000));
            Assert.That(cache.Lookup(
                RequestForType("long.example", DnsRecordType.Aaaa),
                Clock(1000)).Found, Is.False);
            Assert.That(cache.Lookup(
                Request("brief.example"), Clock(2000)).Found, Is.False);
            Assert.That(cache.Lookup(
                Request("long.example"), Clock(301000)).Found, Is.False);
        });
    }

    [Test]
    public void StaleEntriesRequireOptInAndUseBoundedPositiveTimeToLive()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry entry = Entry(
            "stale.example",
            DnsCacheEntryKind.Positive,
            60);
        entry.HasStaleUntil = true;
        entry.StaleUntil = Clock(121000);
        entry.RecordSets[0].Records[0].TimeToLive = 5;
        entry.Message = new DnsMessage();
        entry.Message.Answers.Add(Record("stale.example", 60, 1));
        entry.Message.Additionals.Add(Record("stale.example", 32768, 41));
        cache.Store(entry);

        DnsCacheLookup disabled = cache.Lookup(
            Request("stale.example"),
            Clock(62000));
        DnsResolveRequest allowedRequest = Request("stale.example");
        allowedRequest.Options.AllowStale = true;
        DnsCacheLookup allowed = cache.Lookup(
            allowedRequest,
            Clock(62000));

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Found, Is.False);
            Assert.That(cache.Entries, Has.Count.EqualTo(1));
            Assert.That(allowed.Found, Is.True);
            Assert.That(allowed.Fresh, Is.False);
            Assert.That(allowed.Stale, Is.True);
            Assert.That(allowed.HasRemainingTimeToLive, Is.True);
            Assert.That(allowed.RemainingTimeToLive, Is.Zero);
            Assert.That(allowed.NeedsPrefetch, Is.False);
            Assert.That(allowed.Entry!.OriginalTimeToLive, Is.EqualTo(60));
            Assert.That(allowed.Entry.RecordSets[0].TimeToLive,
                Is.EqualTo(30));
            Assert.That(allowed.Entry.RecordSets[0].Records[0].TimeToLive,
                Is.EqualTo(5));
            Assert.That(allowed.Entry.Message!.Answers[0].TimeToLive,
                Is.EqualTo(30));
            Assert.That(allowed.Entry.Message.Additionals[0].TimeToLive,
                Is.EqualTo(32768));
        });
    }

    [Test]
    public void ConfiguredStaleWindowRetainsThenRemovesExpiredData()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.Store(Entry(
            "window.example",
            DnsCacheEntryKind.Positive,
            60));
        DnsResolveRequest request = Request("window.example");
        request.Options.AllowStale = true;
        request.Options.ServeStaleWindowSeconds = 10;

        DnsCacheLookup withinWindow = cache.Lookup(request, Clock(70000));
        DnsCacheLookup atDeadline = cache.Lookup(request, Clock(71000));

        Assert.Multiple(() =>
        {
            Assert.That(withinWindow.Found, Is.True);
            Assert.That(withinWindow.Stale, Is.True);
            Assert.That(atDeadline.Found, Is.False);
            Assert.That(cache.Entries, Is.Empty);
        });
    }

    [Test]
    public void PrefetchIsSignaledAtTheConfiguredFreshnessThreshold()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        cache.Store(Entry(
            "prefetch.example",
            DnsCacheEntryKind.Positive,
            100));
        DnsResolveRequest request = Request("prefetch.example");
        request.Options.Prefetch = true;
        request.Options.PrefetchThresholdPercent = 10;

        DnsCacheLookup aboveThreshold = cache.Lookup(
            request,
            Clock(90000));
        DnsCacheLookup atThreshold = cache.Lookup(
            request,
            Clock(91000));
        request.Options.Prefetch = false;
        DnsCacheLookup disabled = cache.Lookup(
            request,
            Clock(91000));

        Assert.Multiple(() =>
        {
            Assert.That(aboveThreshold.RemainingTimeToLive, Is.EqualTo(11));
            Assert.That(aboveThreshold.NeedsPrefetch, Is.False);
            Assert.That(atThreshold.RemainingTimeToLive, Is.EqualTo(10));
            Assert.That(atThreshold.NeedsPrefetch, Is.True);
            Assert.That(atThreshold.Entry!.OriginalTimeToLive,
                Is.EqualTo(100));
            Assert.That(disabled.NeedsPrefetch, Is.False);
        });
    }

    [Test]
    public void RefreshFailuresDoNotReplaceFreshOrRetainedStaleAnswers()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry answer = Entry(
            "refresh.example",
            DnsCacheEntryKind.Positive,
            60);
        answer.HasStaleUntil = true;
        answer.StaleUntil = Clock(121000);
        cache.Store(answer);
        DnsCacheEntry failure = Entry(
            "refresh.example",
            DnsCacheEntryKind.Failure,
            10);
        failure.StoredAt = Clock(2000);

        cache.Store(failure);
        DnsCacheLookup fresh = cache.Lookup(
            Request("refresh.example"),
            Clock(2000));
        failure.StoredAt = Clock(62000);
        cache.Store(failure);
        DnsResolveRequest staleRequest = Request("refresh.example");
        staleRequest.Options.AllowStale = true;
        DnsCacheLookup stale = cache.Lookup(staleRequest, Clock(62000));

        Assert.Multiple(() =>
        {
            Assert.That(fresh.Entry!.Kind,
                Is.EqualTo(DnsCacheEntryKind.Positive));
            Assert.That(fresh.Fresh, Is.True);
            Assert.That(stale.Entry!.Kind,
                Is.EqualTo(DnsCacheEntryKind.Positive));
            Assert.That(stale.Stale, Is.True);
        });

        failure.StoredAt = Clock(121000);
        cache.Store(failure);
        Assert.That(cache.Lookup(
            Request("refresh.example"),
            Clock(121000)).Entry!.Kind,
            Is.EqualTo(DnsCacheEntryKind.Failure));
    }

    [Test]
    public void LowerCredibilityDataCannotReplaceAuthoritativeSecureAnswer()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry trusted = Entry(
            "quality.example",
            DnsCacheEntryKind.Positive,
            60);
        trusted.Credibility = 4;
        trusted.Authoritative = true;
        trusted.SecurityStatus = DnsSecurityStatus.Secure;
        cache.Store(trusted);
        DnsCacheEntry glue = Entry(
            "quality.example",
            DnsCacheEntryKind.Positive,
            120);
        glue.Credibility = 2;
        glue.SecurityStatus = DnsSecurityStatus.Insecure;
        glue.StoredAt = Clock(2000);
        glue.ExpiresAt = Clock(122000);
        glue.RecordSets[0].Records[0].RawData = [9, 9, 9, 9];

        cache.Store(glue);

        DnsCacheLookup retained = cache.Lookup(
            Request("quality.example"),
            Clock(2000));
        Assert.Multiple(() =>
        {
            Assert.That(retained.Entry!.Credibility, Is.EqualTo(4));
            Assert.That(retained.Entry.Authoritative, Is.True);
            Assert.That(retained.Entry.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(retained.Entry.RecordSets[0].Records[0].RawData,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void ExpiredProtectedDataCanBeReplacedAtItsDeadline()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry protectedEntry = Entry(
            "expired-quality.example",
            DnsCacheEntryKind.Positive,
            60);
        protectedEntry.Credibility = 4;
        protectedEntry.Authoritative = true;
        protectedEntry.SecurityStatus = DnsSecurityStatus.Secure;
        cache.Store(protectedEntry);
        DnsCacheEntry replacement = Entry(
            "EXPIRED-QUALITY.EXAMPLE",
            DnsCacheEntryKind.Positive,
            120);
        replacement.Credibility = 1;
        replacement.Authoritative = false;
        replacement.SecurityStatus = DnsSecurityStatus.Insecure;
        replacement.StoredAt = Clock(61000);
        replacement.ExpiresAt = Clock(181000);
        replacement.RecordSets[0].Records[0].RawData = [9, 8, 7, 6];

        cache.Store(replacement);
        DnsCacheLookup lookup = cache.Lookup(
            Request("expired-quality.example"),
            Clock(61000));

        Assert.Multiple(() =>
        {
            Assert.That(lookup.Found, Is.True);
            Assert.That(lookup.Entry!.Credibility, Is.EqualTo(1));
            Assert.That(lookup.Entry.Authoritative, Is.False);
            Assert.That(lookup.Entry.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(lookup.Entry.RecordSets[0].Records[0].RawData,
                Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(lookup.RemainingTimeToLive, Is.EqualTo(120));
        });
    }

    [Test]
    public void AuthoritativeAnswerReplacesGlueAndProtectedDataSurvivesNegativeCut()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry glue = Entry(
            "host.example",
            DnsCacheEntryKind.Positive,
            60);
        glue.Credibility = 2;
        cache.Store(glue);
        DnsCacheEntry answer = Entry(
            "host.example",
            DnsCacheEntryKind.Positive,
            60);
        answer.Credibility = 4;
        answer.Authoritative = true;
        answer.StoredAt = Clock(2000);
        answer.ExpiresAt = Clock(62000);
        answer.RecordSets[0].Records[0].RawData = [8, 8, 8, 8];
        cache.Store(answer);
        DnsCacheEntry negative = Entry(
            "example",
            DnsCacheEntryKind.NegativeName,
            30);
        negative.Credibility = 3;
        negative.StoredAt = Clock(3000);
        cache.Store(negative);

        DnsCacheLookup retained = cache.Lookup(
            Request("host.example"),
            Clock(3000));
        DnsCacheLookup cut = cache.Lookup(
            Request("other.example"),
            Clock(3000));
        Assert.Multiple(() =>
        {
            Assert.That(retained.Entry!.Credibility, Is.EqualTo(4));
            Assert.That(retained.Entry.RecordSets[0].Records[0].RawData,
                Is.EqualTo(new byte[] { 8, 8, 8, 8 }));
            Assert.That(cut.Entry!.Kind,
                Is.EqualTo(DnsCacheEntryKind.NegativeName));
        });
    }

    [Test]
    public void SecureDnskeyValidationStateRejectsInsecureReplacement()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnsCacheEntry secure = Entry(
            "keys.example",
            DnsCacheEntryKind.Positive,
            60);
        SetType(secure, DnsRecordType.Dnskey);
        secure.Credibility = 4;
        secure.SecurityStatus = DnsSecurityStatus.Secure;
        cache.Store(secure);
        DnsCacheEntry insecure = Entry(
            "keys.example",
            DnsCacheEntryKind.Positive,
            120);
        SetType(insecure, DnsRecordType.Dnskey);
        insecure.Credibility = 4;
        insecure.SecurityStatus = DnsSecurityStatus.Insecure;
        insecure.StoredAt = Clock(2000);
        insecure.ExpiresAt = Clock(122000);
        insecure.RecordSets[0].Records[0].RawData = [7, 7, 7, 7];

        cache.Store(insecure);

        DnsCacheLookup retained = cache.Lookup(
            RequestForType("keys.example", DnsRecordType.Dnskey),
            Clock(2000));
        Assert.Multiple(() =>
        {
            Assert.That(retained.Entry!.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(retained.Entry.RecordSets[0].Records[0].RawData,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    static DnsCacheEntry Entry(
        string text,
        DnsCacheEntryKind kind,
        uint timeToLive)
    {
        DnsCacheEntry result = new DnsCacheEntry();
        result.Kind = kind;
        result.Name = Name(text);
        result.RecordType = DnsRecordType.A;
        result.Class = DnsClass.Internet;
        result.StoredAt = Clock(1000);
        result.ExpiresAt = Clock(1000 + (long)timeToLive * 1000);
        result.OriginalTimeToLive = timeToLive;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = Name(text);
        set.RecordType = DnsRecordType.A;
        set.Class = DnsClass.Internet;
        set.TimeToLive = timeToLive;
        set.Records.Add(Record(text, timeToLive, 1));
        result.RecordSets.Add(set);
        if (kind == DnsCacheEntryKind.NegativeName ||
            kind == DnsCacheEntryKind.NegativeData)
        {
            result.NegativeSoa = SoaRecord("example", timeToLive, timeToLive);
        }

        return result;
    }

    static DnsResourceRecord SoaRecord(
        string text,
        uint timeToLive,
        uint minimum)
    {
        DnsResourceRecord result = Record(text, timeToLive, 6);
        result.Data = new DnsRecordData();
        result.Data.Soa = new DnsSoaData();
        result.Data.Soa.Minimum = minimum;
        return result;
    }

    static void SetType(DnsCacheEntry entry, DnsRecordType recordType)
    {
        entry.RecordType = recordType;
        entry.RecordTypeValue = (int)recordType;
        entry.RecordSets[0].RecordType = recordType;
        entry.RecordSets[0].RecordTypeValue = (int)recordType;
        entry.RecordSets[0].Records[0].RecordType = recordType;
        entry.RecordSets[0].Records[0].RecordTypeValue = (int)recordType;
    }

    static DnsResourceRecord Record(
        string text,
        uint timeToLive,
        int typeValue)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Name(text);
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = timeToLive;
        result.RawData = [1, 2, 3, 4];
        return result;
    }

    static DnsResolveRequest Request(string text)
    {
        DnsResolveRequest result = new DnsResolveRequest();
        result.Name = Name(text);
        result.RecordType = DnsRecordType.A;
        result.Class = DnsClass.Internet;
        result.ClassValue = 0;
        return result;
    }

    static DnsResolveRequest RequestForType(
        string text,
        DnsRecordType recordType)
    {
        DnsResolveRequest result = Request(text);
        result.RecordType = recordType;
        result.RecordTypeValue = 0;
        return result;
    }

    static DnsName Name(string text)
    {
        DnsName result = new DnsName();
        string[] labels = text.Split('.');
        int index = 0;
        while (index < labels.Length)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        result.IsAbsolute = true;
        return result;
    }

    static DnsClockValue Clock(long monotonicMilliseconds)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = monotonicMilliseconds;
        result.MonotonicMilliseconds = monotonicMilliseconds;
        return result;
    }
}
