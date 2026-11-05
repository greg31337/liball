namespace cookies.Tests;

public class MemoryCookieStoreTests
{
    static readonly DateTimeOffset ReceiptTime =
        new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Test]
    public void AtomicUpsertPreservesStableStateAndOwnsEveryBoundary()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie original = Cookie("id", "example.test", "/", false, "");
        original.CreationTime = ReceiptTime;
        original.SequenceNumber = 88;

        CookieSetResult accepted = store.ApplyStoredCookie(original);
        original.Value = "changed-input";
        original.Extensions[0].Value = "changed-input";
        accepted.Cookie!.Value = "changed-result";
        accepted.Cookie.Extensions[0].Value = "changed-result";
        StoredCookie found = store.FindStoredCookie(Identity(original))!;

        Assert.That(accepted.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(accepted.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(accepted.Cookie.SequenceNumber, Is.Zero);
        Assert.That(found.Value, Is.EqualTo("value"));
        Assert.That(found.Extensions[0].Value, Is.EqualTo("High"));
        Assert.That(found, Is.Not.SameAs(original));
        Assert.That(found, Is.Not.SameAs(accepted.Cookie));

        StoredCookie replacement = Cookie("id", "example.test", "/", false, "");
        replacement.Value = "replacement";
        replacement.CreationTime = ReceiptTime.AddHours(1);
        replacement.LastAccessTime = ReceiptTime.AddHours(1);
        replacement.SequenceNumber = -10;
        CookieSetResult replaced = store.ApplyStoredCookie(replacement);

        Assert.That(replaced.Status, Is.EqualTo(CookieSetStatus.Replaced));
        Assert.That(replaced.Cookie!.Value, Is.EqualTo("replacement"));
        Assert.That(replaced.Cookie.CreationTime, Is.EqualTo(ReceiptTime));
        Assert.That(replaced.Cookie.LastAccessTime, Is.EqualTo(ReceiptTime.AddHours(1)));
        Assert.That(replaced.Cookie.SequenceNumber, Is.Zero);
        Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(1));

        found.Value = "changed-found";
        Assert.That(
            store.FindStoredCookie(Identity(replacement))!.Value,
            Is.EqualTo("replacement"));
    }

    [Test]
    public void IndexedQueryAppliesAllIdentityDomainPathPartitionAndTimeFilters()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie root = Cookie("root", "example.test", "/", false, "");
        root.Persistent = true;
        StoredCookie account = Cookie(
            "account",
            "shop.example.test",
            "/account",
            false,
            "");
        StoredCookie partitioned = Cookie(
            "partitioned",
            "shop.example.test",
            "/account",
            true,
            "https://top.test");
        partitioned.Persistent = true;
        partitioned.HasExpires = true;
        partitioned.Expires = ReceiptTime.AddDays(1);
        StoredCookie unrelated = Cookie("other", "other.test", "/", false, "");
        store.ApplyStoredCookie(root);
        store.ApplyStoredCookie(account);
        store.ApplyStoredCookie(partitioned);
        store.ApplyStoredCookie(unrelated);

        StoredCookie[] domainTree = store.QueryStoredCookies(new CookieStoreQuery
        {
            HasDomain = true,
            Domain = "example.test",
            IncludeSubdomains = true
        });
        StoredCookie[] exactConjunction = store.QueryStoredCookies(new CookieStoreQuery
        {
            HasDomain = true,
            Domain = "shop.example.test",
            HasPath = true,
            Path = "/account",
            HasName = true,
            Name = "account",
            FilterByPartition = true,
            HasPartitionKey = false
        });
        StoredCookie[] partition = store.QueryStoredCookies(new CookieStoreQuery
        {
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        });
        StoredCookie[] session = store.QueryStoredCookies(new CookieStoreQuery
        {
            SessionOnly = true
        });
        StoredCookie[] expired = store.QueryStoredCookies(new CookieStoreQuery
        {
            ExpiredOnly = true,
            HasEvaluationTime = true,
            EvaluationTime = ReceiptTime.AddDays(2)
        });
        CookieStoreQuery exactIdentity = new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = Identity(partitioned),
            HasName = true,
            Name = "different"
        };

        Assert.That(domainTree.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "root", "account", "partitioned" }));
        Assert.That(exactConjunction.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "account" }));
        Assert.That(partition.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "partitioned" }));
        Assert.That(session.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "account", "other" }));
        Assert.That(expired.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "partitioned" }));
        Assert.That(store.QueryStoredCookies(exactIdentity), Is.Empty);
    }

    [Test]
    public void FilteredRemovalAndImmediateDeletionKeepIndexesConsistent()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie root = Cookie("root", "example.test", "/", false, "");
        StoredCookie child = Cookie("child", "shop.example.test", "/", false, "");
        StoredCookie partitioned = Cookie(
            "partitioned",
            "shop.example.test",
            "/",
            true,
            "https://top.test");
        store.ApplyStoredCookie(root);
        store.ApplyStoredCookie(child);
        store.ApplyStoredCookie(partitioned);

        int removedPartition = store.RemoveStoredCookies(new CookieRemovalQuery
        {
            HasDomain = true,
            Domain = "shop.example.test",
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        });
        StoredCookie deletion = Cookie("child", "shop.example.test", "/", false, "");
        deletion.HasExpires = true;
        deletion.Expires = ReceiptTime.AddSeconds(-1);
        deletion.LastAccessTime = DateTimeOffset.MinValue;
        CookieSetResult deleted = store.ApplyStoredCookie(deletion);
        CookieSetResult ignored = store.ApplyStoredCookie(deletion);

        Assert.That(removedPartition, Is.EqualTo(1));
        Assert.That(deleted.Status, Is.EqualTo(CookieSetStatus.Deleted));
        Assert.That(deleted.ErrorCode, Is.EqualTo(CookieErrorCode.ExpiredDeletion));
        Assert.That(ignored.Status, Is.EqualTo(CookieSetStatus.Ignored));
        Assert.That(ignored.ErrorCode, Is.EqualTo(CookieErrorCode.ExpiredDeletion));
        Assert.That(store.FindStoredCookie(Identity(child)), Is.Null);
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery
        {
            FilterByPartition = true,
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        }), Is.Empty);
        Assert.That(store.RemoveStoredCookies(new CookieRemovalQuery()), Is.EqualTo(1));
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()), Is.Empty);
    }

    [Test]
    public void NoOpExpiredDeletionDoesNotConsumeASequence()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie deletion = Cookie("absent", "example.test", "/", false, "");
        deletion.HasExpires = true;
        deletion.Expires = ReceiptTime.AddSeconds(-1);
        deletion.LastAccessTime = DateTimeOffset.MinValue;

        CookieSetResult ignored = store.ApplyStoredCookie(deletion);
        StoredCookie insertion = Cookie("inserted", "example.test", "/", false, "");
        insertion.LastAccessTime = ReceiptTime.AddDays(10);
        CookieSetResult inserted = store.ApplyStoredCookie(insertion);

        Assert.That(ignored.Status, Is.EqualTo(CookieSetStatus.Ignored));
        Assert.That(ignored.ErrorCode, Is.EqualTo(CookieErrorCode.ExpiredDeletion));
        Assert.That(ignored.Cookie, Is.Null);
        Assert.That(inserted.Status, Is.EqualTo(CookieSetStatus.Accepted));
        Assert.That(inserted.Cookie!.SequenceNumber, Is.Zero);
        Assert.That(inserted.Cookie.LastAccessTime, Is.EqualTo(ReceiptTime));
        Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(1));
    }

    [Test]
    public void BatchAccessUpdateIsAtomicAndChangesOnlyExistingAccessTimes()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie first = Cookie("first", "example.test", "/", false, "");
        StoredCookie second = Cookie("second", "example.test", "/", false, "");
        first.Value = "first-value";
        second.Value = "second-value";
        store.ApplyStoredCookie(first);
        store.ApplyStoredCookie(second);
        CookieIdentity firstIdentity = Identity(first);
        CookieIdentity secondIdentity = Identity(second);
        CookieIdentity absentIdentity = new CookieIdentity
        {
            Name = "absent",
            Domain = "example.test",
            Path = "/"
        };
        DateTimeOffset accessTime = ReceiptTime.AddHours(2);

        StoredCookie[] updated = store.UpdateStoredCookieAccess(
            [firstIdentity, absentIdentity, secondIdentity],
            accessTime);
        firstIdentity.Name = "changed-input";
        updated[0].Value = "changed-output";
        StoredCookie[] stored = store.QueryStoredCookies(new CookieStoreQuery());

        Assert.That(updated.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "first", "second" }));
        Assert.That(stored.Select(cookie => cookie.Value),
            Is.EqualTo(new[] { "first-value", "second-value" }));
        Assert.That(stored.Select(cookie => cookie.LastAccessTime),
            Is.All.EqualTo(accessTime));

        DateTimeOffset early = accessTime.AddMinutes(1);
        DateTimeOffset late = accessTime.AddMinutes(2);
        CookieIdentity[] identities = [Identity(first), Identity(second)];
        Parallel.Invoke(
            () => store.UpdateStoredCookieAccess(identities, early),
            () => store.UpdateStoredCookieAccess(identities, late));
        DateTimeOffset[] finalTimes = store.QueryStoredCookies(new CookieStoreQuery())
            .Select(cookie => cookie.LastAccessTime)
            .ToArray();
        Assert.That(finalTimes.Distinct().ToArray(), Has.Length.EqualTo(1));
        Assert.That(finalTimes[0], Is.EqualTo(early).Or.EqualTo(late));
    }

    [Test]
    public void SnapshotReplacementIsOrderedDefensiveAndAtomicOnFailure()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie later = Cookie("later", "example.test", "/", false, "");
        later.SequenceNumber = 7;
        StoredCookie earlier = Cookie("earlier", "example.test", "/", false, "");
        earlier.SequenceNumber = 3;
        CookieJarSnapshot replacement = new CookieJarSnapshot
        {
            Cookies = [later, earlier],
            NextSequenceNumber = 8
        };

        store.ReplaceStoreSnapshot(replacement);
        later.Value = "changed-input";
        CookieJarSnapshot captured = store.CaptureStoreSnapshot();

        Assert.That(captured.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "earlier", "later" }));
        Assert.That(captured.Cookies[1].Value, Is.EqualTo("value"));
        Assert.That(captured.NextSequenceNumber, Is.EqualTo(8));

        captured.Cookies[0].Value = "changed-capture";
        Assert.That(store.FindStoredCookie(Identity(earlier))!.Value, Is.EqualTo("value"));

        StoredCookie duplicate = Cookie("later", "example.test", "/", false, "");
        duplicate.SequenceNumber = 6;
        CookieJarSnapshot invalid = new CookieJarSnapshot
        {
            Cookies = [later, duplicate],
            NextSequenceNumber = 8
        };
        Assert.That(
            () => store.ReplaceStoreSnapshot(invalid),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(store.CaptureStoreSnapshot().Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "earlier", "later" }));

        CookieSetResult inserted = store.ApplyStoredCookie(
            Cookie("new", "example.test", "/", false, ""));
        Assert.That(inserted.Cookie!.SequenceNumber, Is.EqualTo(8));
    }

    [Test]
    public void ConcurrentDistinctInsertsAndSameIdentityReplacementsAreAtomic()
    {
        ICookieStore store = new MemoryCookieStore();

        Parallel.For(0, 64, index =>
        {
            store.ApplyStoredCookie(Cookie(
                "cookie-" + index,
                "example.test",
                "/",
                false,
                ""));
        });
        Parallel.For(0, 64, index =>
        {
            StoredCookie shared = Cookie("shared", "example.test", "/", false, "");
            shared.Value = index.ToString();
            store.ApplyStoredCookie(shared);
        });

        StoredCookie[] all = store.QueryStoredCookies(new CookieStoreQuery());
        CookieJarSnapshot snapshot = store.CaptureStoreSnapshot();

        Assert.That(all, Has.Length.EqualTo(65));
        Assert.That(all.Select(cookie => cookie.SequenceNumber).Distinct().Count(),
            Is.EqualTo(65));
        Assert.That(all.Count(cookie => cookie.Name == "shared"), Is.EqualTo(1));
        Assert.That(snapshot.NextSequenceNumber, Is.EqualTo(65));
    }

    [Test]
    public void InvalidQueriesAndSnapshotsFailWithoutChangingState()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie cookie = Cookie("id", "example.test", "/", false, "");
        store.ApplyStoredCookie(cookie);

        Assert.That(
            () => store.FindStoredCookie(new CookieIdentity()),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(
            () => store.QueryStoredCookies(new CookieStoreQuery { ExpiredOnly = true }),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(
            () => store.QueryStoredCookies(new CookieStoreQuery
            {
                HasDomain = true,
                Domain = "EXAMPLE.TEST"
            }),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(
            () => store.RemoveStoredCookies(new CookieRemovalQuery
            {
                HasPath = true,
                Path = "relative"
            }),
            Throws.TypeOf<CookieStoreException>());
        StoredCookie noncanonical = Cookie("upper", "EXAMPLE.TEST", "/", false, "");
        Assert.That(
            () => store.ApplyStoredCookie(noncanonical),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(
            () => store.ReplaceStoreSnapshot(new CookieJarSnapshot
            {
                Version = CookieDefaults.SnapshotVersion + 1
            }),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery()), Has.Length.EqualTo(1));
    }

    static StoredCookie Cookie(
        string name,
        string domain,
        string path,
        bool hasPartitionKey,
        string partitionKey)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = domain,
            Path = path,
            CreationTime = ReceiptTime,
            LastAccessTime = ReceiptTime,
            HasPartitionKey = hasPartitionKey,
            PartitionKey = partitionKey,
            Extensions =
            [
                new CookieAttribute
                {
                    Name = "Priority",
                    HasValue = true,
                    Value = "High"
                }
            ]
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
