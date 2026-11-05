namespace cookies.Tests;

public class CookieSequenceAssignerTests
{
    static readonly DateTimeOffset OriginalCreation =
        new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
    static readonly DateTimeOffset ReplacementTime = OriginalCreation.AddHours(1);

    [Test]
    public void InsertAssignsNextStableSequenceAndOwnsTheCandidateGraph()
    {
        StoredCookie first = Cookie("first", false, "");
        first.SequenceNumber = 91;
        first.LastAccessTime = OriginalCreation;

        CookieSequenceAssignment firstAssignment =
            CookieSequenceAssigner.Assign(first, null, 7);
        StoredCookie second = Cookie("second", false, "");
        CookieSequenceAssignment secondAssignment = CookieSequenceAssigner.Assign(
            second,
            null,
            firstAssignment.NextSequenceNumber);

        Assert.That(firstAssignment.Replaced, Is.False);
        Assert.That(firstAssignment.Cookie.SequenceNumber, Is.EqualTo(7));
        Assert.That(firstAssignment.Cookie.LastAccessTime, Is.EqualTo(ReplacementTime));
        Assert.That(firstAssignment.NextSequenceNumber, Is.EqualTo(8));
        Assert.That(secondAssignment.Cookie.SequenceNumber, Is.EqualTo(8));
        Assert.That(secondAssignment.NextSequenceNumber, Is.EqualTo(9));
        Assert.That(firstAssignment.Cookie, Is.Not.SameAs(first));
        Assert.That(firstAssignment.Cookie.Extensions, Is.Not.SameAs(first.Extensions));
        Assert.That(firstAssignment.Cookie.Extensions[0], Is.Not.SameAs(first.Extensions[0]));

        first.Value = "changed-input";
        first.Extensions[0].Value = "changed-input";
        Assert.That(firstAssignment.Cookie.Value, Is.EqualTo("value"));
        Assert.That(firstAssignment.Cookie.Extensions[0].Value, Is.EqualTo("High"));
    }

    [Test]
    public void ExactReplacementPreservesCreationAndSequenceWithoutConsumption()
    {
        StoredCookie existing = Cookie("id", true, "https://top.test");
        existing.CreationTime = OriginalCreation;
        existing.LastAccessTime = OriginalCreation;
        existing.SequenceNumber = 4;
        StoredCookie replacement = Cookie("id", true, "https://top.test");
        replacement.Value = "replacement";
        replacement.CreationTime = ReplacementTime;
        replacement.LastAccessTime = ReplacementTime.AddDays(10);
        replacement.SequenceNumber = 99;
        replacement.Secure = true;

        CookieSequenceAssignment assignment = CookieSequenceAssigner.Assign(
            replacement,
            existing,
            12);

        Assert.That(assignment.Replaced, Is.True);
        Assert.That(assignment.NextSequenceNumber, Is.EqualTo(12));
        Assert.That(assignment.Cookie.CreationTime, Is.EqualTo(OriginalCreation));
        Assert.That(assignment.Cookie.SequenceNumber, Is.EqualTo(4));
        Assert.That(assignment.Cookie.Value, Is.EqualTo("replacement"));
        Assert.That(assignment.Cookie.LastAccessTime, Is.EqualTo(ReplacementTime));
        Assert.That(assignment.Cookie.Secure, Is.True);
        Assert.That(assignment.Cookie, Is.Not.SameAs(replacement));
        Assert.That(assignment.Cookie, Is.Not.SameAs(existing));

        assignment.Cookie.Value = "changed-output";
        assignment.Cookie.Extensions[0].Value = "changed-output";
        Assert.That(replacement.Value, Is.EqualTo("replacement"));
        Assert.That(replacement.Extensions[0].Value, Is.EqualTo("High"));
        Assert.That(existing.Value, Is.EqualTo("value"));
        Assert.That(existing.Extensions[0].Value, Is.EqualTo("High"));
    }

    [Test]
    public void PartitionIdentityDifferencesAllocateNewSequences()
    {
        StoredCookie existing = Cookie("id", true, "https://first.test");
        existing.CreationTime = OriginalCreation;
        existing.SequenceNumber = 2;
        StoredCookie otherPartition = Cookie("id", true, "https://second.test");
        StoredCookie unpartitioned = Cookie("id", false, "");

        CookieSequenceAssignment partitionAssignment = CookieSequenceAssigner.Assign(
            otherPartition,
            existing,
            20);
        CookieSequenceAssignment presenceAssignment = CookieSequenceAssigner.Assign(
            unpartitioned,
            existing,
            partitionAssignment.NextSequenceNumber);

        Assert.That(partitionAssignment.Replaced, Is.False);
        Assert.That(partitionAssignment.Cookie.SequenceNumber, Is.EqualTo(20));
        Assert.That(presenceAssignment.Replaced, Is.False);
        Assert.That(presenceAssignment.Cookie.SequenceNumber, Is.EqualTo(21));
        Assert.That(presenceAssignment.NextSequenceNumber, Is.EqualTo(22));
    }

    [Test]
    public void InvalidOrExhaustedInsertionSequenceFailsWithoutWrapping()
    {
        StoredCookie candidate = Cookie("id", false, "");

        Assert.That(
            () => CookieSequenceAssigner.Assign(candidate, null, -1),
            Throws.TypeOf<CookieStoreException>());
        Assert.That(
            () => CookieSequenceAssigner.Assign(candidate, null, long.MaxValue),
            Throws.TypeOf<CookieStoreException>());
    }

    [Test]
    public void ReplacementDoesNotRequireAnotherAvailableSequence()
    {
        StoredCookie existing = Cookie("id", false, "");
        existing.CreationTime = OriginalCreation;
        existing.SequenceNumber = long.MaxValue - 1;
        StoredCookie replacement = Cookie("id", false, "");

        CookieSequenceAssignment assignment = CookieSequenceAssigner.Assign(
            replacement,
            existing,
            long.MaxValue);

        Assert.That(assignment.Replaced, Is.True);
        Assert.That(assignment.Cookie.SequenceNumber, Is.EqualTo(long.MaxValue - 1));
        Assert.That(assignment.NextSequenceNumber, Is.EqualTo(long.MaxValue));
    }

    static StoredCookie Cookie(
        string name,
        bool hasPartitionKey,
        string partitionKey)
    {
        return new StoredCookie
        {
            Name = name,
            Value = "value",
            Domain = "example.test",
            Path = "/",
            CreationTime = ReplacementTime,
            LastAccessTime = ReplacementTime,
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
}
