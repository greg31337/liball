namespace cookies.Tests;

public class MemoryCookieStoreConformanceTests
{
    static readonly DateTimeOffset InitialTime =
        new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);

    [Test]
    public void EveryIdentityComponentCanCoexistAndExpiryDeletesOnlyTheExactIdentity()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie exact = Cookie("session", "example.test", "/", false, "");
        StoredCookie otherName = Cookie("other", "example.test", "/", false, "");
        StoredCookie otherDomain = Cookie("session", "shop.example.test", "/", false, "");
        StoredCookie otherPath = Cookie(
            "session", "example.test", "/account", false, "");
        StoredCookie partitioned = Cookie(
            "session", "example.test", "/", true, "https://first.test");
        StoredCookie otherPartition = Cookie(
            "session", "example.test", "/", true, "https://second.test");

        foreach (StoredCookie cookie in new[]
        {
            exact,
            otherName,
            otherDomain,
            otherPath,
            partitioned,
            otherPartition
        })
        {
            Assert.That(
                store.ApplyStoredCookie(cookie).Status,
                Is.EqualTo(CookieSetStatus.Accepted));
        }

        StoredCookie replacement = Cookie(
            "session", "example.test", "/", false, "stale-ignored-key");
        replacement.Value = "replacement";
        replacement.CreationTime = InitialTime.AddHours(1);
        CookieSetResult replaced = store.ApplyStoredCookie(replacement);

        StoredCookie deletion = Cookie(
            "session", "example.test", "/", false, "another-stale-key");
        deletion.CreationTime = InitialTime.AddHours(2);
        deletion.HasExpires = true;
        deletion.Expires = deletion.CreationTime;
        CookieSetResult deleted = store.ApplyStoredCookie(deletion);

        Assert.That(replaced.Status, Is.EqualTo(CookieSetStatus.Replaced));
        Assert.That(replaced.Cookie!.CreationTime, Is.EqualTo(InitialTime));
        Assert.That(replaced.Cookie.SequenceNumber, Is.Zero);
        Assert.That(deleted.Status, Is.EqualTo(CookieSetStatus.Deleted));
        Assert.That(deleted.ErrorCode, Is.EqualTo(CookieErrorCode.ExpiredDeletion));
        Assert.That(store.FindStoredCookie(Identity(exact)), Is.Null);
        Assert.That(store.QueryStoredCookies(new CookieStoreQuery())
            .Select(cookie => cookie.Name + "|" + cookie.Domain + "|" +
                cookie.Path + "|" + cookie.PartitionKey),
            Is.EqualTo(new[]
            {
                "other|example.test|/|",
                "session|shop.example.test|/|",
                "session|example.test|/account|",
                "session|example.test|/|https://first.test",
                "session|example.test|/|https://second.test"
            }));
        Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(6));
    }

    [Test]
    public void ReplacementPreservesStableFieldsAndRefreshesEveryOtherAttribute()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie original = Cookie(
            "session", "example.test", "/account", true, "https://top.test");
        original.Value = "old";
        original.SequenceNumber = 77;
        store.ApplyStoredCookie(original);

        StoredCookie replacement = Cookie(
            "session", "example.test", "/account", true, "https://top.test");
        replacement.Value = "new";
        replacement.CreationTime = InitialTime.AddDays(1);
        replacement.LastAccessTime = InitialTime.AddDays(9);
        replacement.HasExpires = true;
        replacement.Expires = InitialTime.AddDays(30);
        replacement.Persistent = true;
        replacement.HostOnly = true;
        replacement.Secure = true;
        replacement.HttpOnly = true;
        replacement.HasSameSite = true;
        replacement.SameSiteMode = CookieSameSiteMode.Strict;
        replacement.IsValueQuoted = true;
        replacement.Extensions =
        [
            new CookieAttribute
            {
                Name = "Priority",
                HasValue = true,
                Value = "Low"
            },
            new CookieAttribute
            {
                Name = "Flag",
                HasValue = false,
                Value = ""
            }
        ];
        replacement.SequenceNumber = 99;

        CookieSetResult result = store.ApplyStoredCookie(replacement);
        StoredCookie stored = store.FindStoredCookie(Identity(original))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CookieSetStatus.Replaced));
            Assert.That(stored.Name, Is.EqualTo("session"));
            Assert.That(stored.Domain, Is.EqualTo("example.test"));
            Assert.That(stored.Path, Is.EqualTo("/account"));
            Assert.That(stored.HasPartitionKey, Is.True);
            Assert.That(stored.PartitionKey, Is.EqualTo("https://top.test"));
            Assert.That(stored.CreationTime, Is.EqualTo(InitialTime));
            Assert.That(stored.SequenceNumber, Is.Zero);
            Assert.That(stored.Value, Is.EqualTo("new"));
            Assert.That(stored.LastAccessTime, Is.EqualTo(InitialTime.AddDays(1)));
            Assert.That(stored.HasExpires, Is.True);
            Assert.That(stored.Expires, Is.EqualTo(InitialTime.AddDays(30)));
            Assert.That(stored.Persistent, Is.True);
            Assert.That(stored.HostOnly, Is.True);
            Assert.That(stored.Secure, Is.True);
            Assert.That(stored.HttpOnly, Is.True);
            Assert.That(stored.HasSameSite, Is.True);
            Assert.That(stored.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Strict));
            Assert.That(stored.IsValueQuoted, Is.True);
            Assert.That(stored.Extensions.Select(attribute => attribute.Name),
                Is.EqualTo(new[] { "Priority", "Flag" }));
            Assert.That(stored.Extensions.Select(attribute => attribute.Value),
                Is.EqualTo(new[] { "Low", "" }));
            Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void EqualTimeOrderingRemainsStableAcrossReplacementDeletionAndInsertion()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie zebra = Cookie("zebra", "example.test", "/", false, "");
        StoredCookie alpha = Cookie("alpha", "example.test", "/", false, "");
        StoredCookie middle = Cookie("middle", "example.test", "/", false, "");
        store.ApplyStoredCookie(zebra);
        store.ApplyStoredCookie(alpha);
        store.ApplyStoredCookie(middle);

        StoredCookie alphaReplacement = Cookie(
            "alpha", "example.test", "/", false, "");
        alphaReplacement.Value = "new-alpha";
        store.ApplyStoredCookie(alphaReplacement);

        StoredCookie zebraDeletion = Cookie(
            "zebra", "example.test", "/", false, "");
        zebraDeletion.HasExpires = true;
        zebraDeletion.Expires = zebraDeletion.CreationTime;
        store.ApplyStoredCookie(zebraDeletion);
        store.ApplyStoredCookie(Cookie("beta", "example.test", "/", false, ""));

        StoredCookie[] ordered = store.QueryStoredCookies(new CookieStoreQuery());

        Assert.That(ordered.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "alpha", "middle", "beta" }));
        Assert.That(ordered.Select(cookie => cookie.SequenceNumber),
            Is.EqualTo(new long[] { 1, 2, 3 }));
        Assert.That(ordered.Select(cookie => cookie.CreationTime),
            Is.All.EqualTo(InitialTime));
        Assert.That(ordered.Single(cookie => cookie.Name == "alpha").Value,
            Is.EqualTo("new-alpha"));
    }

    [Test]
    public void EveryStoreInterfaceOperationMaintainsDefensiveOwnership()
    {
        ICookieStore store = new MemoryCookieStore();
        StoredCookie seed = Cookie("seed", "example.test", "/", false, "");
        seed.SequenceNumber = 4;
        CookieJarSnapshot replacement = new CookieJarSnapshot
        {
            Cookies = [seed],
            NextSequenceNumber = 5
        };
        store.ReplaceStoreSnapshot(replacement);
        seed.Value = "changed-replacement-input";
        seed.Extensions[0].Value = "changed-replacement-input";

        CookieIdentity seedIdentity = Identity(seed);
        StoredCookie found = store.FindStoredCookie(seedIdentity)!;
        found.Value = "changed-find-output";
        found.Extensions[0].Value = "changed-find-output";

        StoredCookie[] queried = store.QueryStoredCookies(new CookieStoreQuery());
        queried[0].Value = "changed-query-output";
        queried[0].Extensions[0].Value = "changed-query-output";

        StoredCookie added = Cookie("added", "example.test", "/", false, "");
        CookieSetResult applied = store.ApplyStoredCookie(added);
        applied.Cookie!.Value = "changed-apply-output";
        applied.Cookie.Extensions[0].Value = "changed-apply-output";

        DateTimeOffset accessTime = InitialTime.AddHours(3);
        StoredCookie[] updated = store.UpdateStoredCookieAccess(
            [seedIdentity, Identity(added)], accessTime);
        updated[0].Value = "changed-update-output";
        updated[0].Extensions[0].Value = "changed-update-output";

        CookieJarSnapshot captured = store.CaptureStoreSnapshot();
        captured.Cookies[0].Value = "changed-capture-output";
        captured.Cookies[0].Extensions[0].Value = "changed-capture-output";

        int removed = store.RemoveStoredCookies(new CookieRemovalQuery
        {
            HasIdentity = true,
            Identity = Identity(added)
        });
        StoredCookie retained = store.FindStoredCookie(seedIdentity)!;

        Assert.That(removed, Is.EqualTo(1));
        Assert.That(store.FindStoredCookie(Identity(added)), Is.Null);
        Assert.That(retained.Value, Is.EqualTo("value"));
        Assert.That(retained.Extensions[0].Value, Is.EqualTo("Medium"));
        Assert.That(retained.LastAccessTime, Is.EqualTo(accessTime));
        Assert.That(retained.SequenceNumber, Is.EqualTo(4));
        Assert.That(store.CaptureStoreSnapshot().NextSequenceNumber, Is.EqualTo(6));
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
            CreationTime = InitialTime,
            LastAccessTime = InitialTime,
            HasPartitionKey = hasPartitionKey,
            PartitionKey = partitionKey,
            Extensions =
            [
                new CookieAttribute
                {
                    Name = "Priority",
                    HasValue = true,
                    Value = "Medium"
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
