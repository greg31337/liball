namespace cookies.Tests;

public class CookieIdentityUtilityTests
{
    [Test]
    public void IdentityUsesCanonicalStorageComponentsAndNormalizesAbsentPartition()
    {
        StoredCookie cookie = Cookie();
        cookie.PartitionKey = "ignored-stale-key";

        CookieIdentity identity = CookieIdentityUtility.CreateFromStoredCookie(cookie);

        Assert.That(identity.Name, Is.EqualTo("Session"));
        Assert.That(identity.Domain, Is.EqualTo("example.test"));
        Assert.That(identity.Path, Is.EqualTo("/account"));
        Assert.That(identity.HasPartitionKey, Is.False);
        Assert.That(identity.PartitionKey, Is.Empty);
        Assert.That(CookieIdentityUtility.MatchesStoredCookie(cookie, identity), Is.True);
    }

    [Test]
    public void ExactIdentityIncludesPartitionPresenceAndKey()
    {
        CookieIdentity unpartitioned = Identity(false, "ignored");
        CookieIdentity unpartitionedWithStaleKey = Identity(false, "different");
        CookieIdentity partitioned = Identity(true, "https://top.test");
        CookieIdentity samePartition = Identity(true, "https://top.test");
        CookieIdentity otherPartition = Identity(true, "https://other.test");

        Assert.That(
            CookieIdentityUtility.AreEqual(unpartitioned, unpartitionedWithStaleKey),
            Is.True);
        Assert.That(CookieIdentityUtility.AreEqual(partitioned, samePartition), Is.True);
        Assert.That(CookieIdentityUtility.AreEqual(unpartitioned, partitioned), Is.False);
        Assert.That(CookieIdentityUtility.AreEqual(partitioned, otherPartition), Is.False);

        samePartition.Name = "session";
        Assert.That(CookieIdentityUtility.AreEqual(partitioned, samePartition), Is.False);
        samePartition.Name = "Session";
        samePartition.Domain = "EXAMPLE.TEST";
        Assert.That(CookieIdentityUtility.AreEqual(partitioned, samePartition), Is.False);
        samePartition.Domain = "example.test";
        samePartition.Path = "/Account";
        Assert.That(CookieIdentityUtility.AreEqual(partitioned, samePartition), Is.False);
    }

    [Test]
    public void StorageKeyIsDeterministicAndCollisionSafeAcrossComponents()
    {
        CookieIdentity first = new CookieIdentity
        {
            Name = "a",
            Domain = "bc",
            Path = "/x:1",
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        };
        CookieIdentity concatenationCollision = new CookieIdentity
        {
            Name = "ab",
            Domain = "c",
            Path = "/x:1",
            HasPartitionKey = true,
            PartitionKey = "https://top.test"
        };
        StoredCookie cookie = Cookie();
        cookie.HasPartitionKey = true;
        cookie.PartitionKey = "https://top.test";
        CookieIdentity cookieIdentity = CookieIdentityUtility.CreateFromStoredCookie(cookie);

        Assert.That(
            CookieIdentityUtility.CreateKey(first),
            Is.Not.EqualTo(CookieIdentityUtility.CreateKey(concatenationCollision)));
        Assert.That(
            CookieIdentityUtility.CreateStoredCookieKey(cookie),
            Is.EqualTo(CookieIdentityUtility.CreateKey(cookieIdentity)));
        Assert.That(
            CookieIdentityUtility.CreateKey(cookieIdentity),
            Is.EqualTo(CookieIdentityUtility.CreateKey(cookieIdentity)));
    }

    static CookieIdentity Identity(bool hasPartitionKey, string partitionKey)
    {
        return new CookieIdentity
        {
            Name = "Session",
            Domain = "example.test",
            Path = "/account",
            HasPartitionKey = hasPartitionKey,
            PartitionKey = partitionKey
        };
    }

    static StoredCookie Cookie()
    {
        return new StoredCookie
        {
            Name = "Session",
            Value = "value",
            Domain = "example.test",
            Path = "/account"
        };
    }
}
