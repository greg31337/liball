namespace cookies.Tests;

public class CookieSnapshotCodecTests
{
    [Test]
    public void WriterEmitsEveryFieldInStableSequenceAndExtensionOrder()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        StoredCookie later = CompleteCookie();
        StoredCookie earlier = new StoredCookie
        {
            Name = "earlier",
            Value = "first",
            Domain = "example.test",
            Path = "/",
            CreationTime = new DateTimeOffset(
                2029,
                1,
                2,
                3,
                4,
                5,
                TimeSpan.Zero),
            LastAccessTime = new DateTimeOffset(
                2029,
                1,
                2,
                3,
                4,
                6,
                TimeSpan.Zero),
            SequenceNumber = 3
        };
        CookieJarSnapshot snapshot = new CookieJarSnapshot
        {
            Cookies = [later, earlier],
            NextSequenceNumber = 8
        };

        string text = codec.SerializeJarSnapshot(snapshot);

        using System.Text.Json.JsonDocument document =
            System.Text.Json.JsonDocument.Parse(text);
        System.Text.Json.JsonElement root = document.RootElement;
        System.Text.Json.JsonElement cookies = root.GetProperty("cookies");
        System.Text.Json.JsonElement cookie = cookies[1];
        Assert.That(root.GetProperty("version").GetInt32(),
            Is.EqualTo(CookieDefaults.SnapshotVersion));
        Assert.That(root.GetProperty("nextSequenceNumber").GetInt64(),
            Is.EqualTo(8));
        Assert.That(cookies.GetArrayLength(), Is.EqualTo(2));
        Assert.That(cookies[0].GetProperty("name").GetString(),
            Is.EqualTo("earlier"));
        Assert.That(cookie.GetProperty("name").GetString(), Is.EqualTo(later.Name));
        Assert.That(cookie.GetProperty("value").GetString(), Is.EqualTo(later.Value));
        Assert.That(cookie.GetProperty("domain").GetString(), Is.EqualTo(later.Domain));
        Assert.That(cookie.GetProperty("path").GetString(), Is.EqualTo(later.Path));
        Assert.That(cookie.GetProperty("hasExpires").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("expires").GetString(),
            Is.EqualTo("2030-06-09T04:48:14.0000000+00:00"));
        Assert.That(cookie.GetProperty("creationTime").GetString(),
            Is.EqualTo("2030-06-09T08:18:14.0000000+00:00"));
        Assert.That(cookie.GetProperty("lastAccessTime").GetString(),
            Is.EqualTo("2030-06-09T14:18:14.0000000+00:00"));
        Assert.That(cookie.GetProperty("persistent").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("hostOnly").GetBoolean(), Is.False);
        Assert.That(cookie.GetProperty("secure").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("httpOnly").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("hasSameSite").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("sameSiteMode").GetInt32(),
            Is.EqualTo((int)CookieSameSiteMode.Strict));
        Assert.That(cookie.GetProperty("hasPartitionKey").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("partitionKey").GetString(),
            Is.EqualTo("https://top.example"));
        Assert.That(cookie.GetProperty("isValueQuoted").GetBoolean(), Is.True);
        Assert.That(cookie.GetProperty("sequenceNumber").GetInt64(), Is.EqualTo(7));
        System.Text.Json.JsonElement extensions = cookie.GetProperty("extensions");
        Assert.That(extensions.GetArrayLength(), Is.EqualTo(2));
        Assert.That(extensions[0].GetProperty("name").GetString(),
            Is.EqualTo("Priority"));
        Assert.That(extensions[0].GetProperty("hasValue").GetBoolean(), Is.True);
        Assert.That(extensions[0].GetProperty("value").GetString(),
            Is.EqualTo("Hígh"));
        Assert.That(extensions[1].GetProperty("name").GetString(),
            Is.EqualTo("Flag"));
        Assert.That(extensions[1].GetProperty("hasValue").GetBoolean(), Is.False);
        Assert.That(extensions[1].GetProperty("value").GetString(), Is.Empty);
    }

    [Test]
    public void WriterIsDeterministicForEquivalentInputOrderAndEmptyState()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        StoredCookie later = CompleteCookie();
        StoredCookie earlier = new StoredCookie
        {
            Name = "earlier",
            Value = "value",
            Domain = "example.test",
            Path = "/",
            CreationTime = DateTimeOffset.MinValue,
            LastAccessTime = DateTimeOffset.MinValue,
            SequenceNumber = 2
        };
        CookieJarSnapshot first = new CookieJarSnapshot
        {
            Cookies = [later, earlier],
            NextSequenceNumber = 8
        };
        CookieJarSnapshot second = new CookieJarSnapshot
        {
            Cookies = [earlier, later],
            NextSequenceNumber = 8
        };

        string firstText = codec.SerializeJarSnapshot(first);
        string secondText = codec.SerializeJarSnapshot(second);
        string emptyText = codec.SerializeJarSnapshot(new CookieJarSnapshot());

        Assert.That(secondText, Is.EqualTo(firstText));
        Assert.That(emptyText,
            Is.EqualTo("{\"version\":1,\"nextSequenceNumber\":0,\"cookies\":[]}"));
        Assert.That(first.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "id", "earlier" }));
    }

    [Test]
    public void WriterRejectsInvalidVersionIdentitySequenceAndSensitiveDataSafely()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        CookieSnapshotException nullFailure = Assert.Throws<CookieSnapshotException>(
            () => codec.SerializeJarSnapshot(null!));
        CookieJarSnapshot unsupported = new CookieJarSnapshot
        {
            Version = CookieDefaults.SnapshotVersion + 1
        };
        CookieSnapshotException versionFailure = Assert.Throws<CookieSnapshotException>(
            () => codec.SerializeJarSnapshot(unsupported));
        StoredCookie first = CompleteCookie();
        StoredCookie duplicate = CompleteCookie();
        duplicate.SequenceNumber = 6;
        CookieJarSnapshot duplicateIdentity = new CookieJarSnapshot
        {
            Cookies = [first, duplicate],
            NextSequenceNumber = 8
        };
        CookieSnapshotException identityFailure = Assert.Throws<CookieSnapshotException>(
            () => codec.SerializeJarSnapshot(duplicateIdentity));
        duplicate.Name = "different";
        duplicate.SequenceNumber = first.SequenceNumber;
        CookieJarSnapshot duplicateSequence = new CookieJarSnapshot
        {
            Cookies = [first, duplicate],
            NextSequenceNumber = 8
        };
        CookieSnapshotException sequenceFailure = Assert.Throws<CookieSnapshotException>(
            () => codec.SerializeJarSnapshot(duplicateSequence));
        StoredCookie injected = CompleteCookie();
        injected.Value = "private\r\nInjected: secret";
        CookieJarSnapshot injection = new CookieJarSnapshot
        {
            Cookies = [injected],
            NextSequenceNumber = 8
        };
        CookieSnapshotException injectionFailure = Assert.Throws<CookieSnapshotException>(
            () => codec.SerializeJarSnapshot(injection));

        Assert.That(nullFailure.Message,
            Is.EqualTo("Cookie snapshot failed with error code 38."));
        Assert.That(versionFailure.Message,
            Is.EqualTo("Cookie snapshot failed with error code 39."));
        Assert.That(identityFailure.Message,
            Is.EqualTo("Cookie snapshot failed with error code 40."));
        Assert.That(sequenceFailure.Message,
            Is.EqualTo("Cookie snapshot failed with error code 38."));
        Assert.That(injectionFailure.Message,
            Is.EqualTo("Cookie snapshot failed with error code 38."));
        Assert.That(injectionFailure.Message, Does.Not.Contain("private"));
        Assert.That(injectionFailure.Message, Does.Not.Contain("secret"));
    }

    [Test]
    public void ReaderRoundTripsEveryFieldAndOwnsEachReturnedGraph()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        StoredCookie cookie = CompleteCookie();
        CookieJarSnapshot source = new CookieJarSnapshot
        {
            Cookies = [cookie],
            NextSequenceNumber = 8
        };
        string text = codec.SerializeJarSnapshot(source);

        CookieSnapshotParseResult result = codec.TryParseJarSnapshot(text);
        CookieJarSnapshot throwing = codec.ParseJarSnapshot(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Issues, Is.Empty);
        Assert.That(result.Snapshot, Is.Not.Null);
        StoredCookie parsed = result.Snapshot!.Cookies.Single();
        Assert.That(parsed.Name, Is.EqualTo(cookie.Name));
        Assert.That(parsed.Value, Is.EqualTo(cookie.Value));
        Assert.That(parsed.Domain, Is.EqualTo(cookie.Domain));
        Assert.That(parsed.Path, Is.EqualTo(cookie.Path));
        Assert.That(parsed.HasExpires, Is.EqualTo(cookie.HasExpires));
        Assert.That(parsed.Expires, Is.EqualTo(cookie.Expires));
        Assert.That(parsed.CreationTime, Is.EqualTo(cookie.CreationTime));
        Assert.That(parsed.LastAccessTime, Is.EqualTo(cookie.LastAccessTime));
        Assert.That(parsed.Persistent, Is.EqualTo(cookie.Persistent));
        Assert.That(parsed.HostOnly, Is.EqualTo(cookie.HostOnly));
        Assert.That(parsed.Secure, Is.EqualTo(cookie.Secure));
        Assert.That(parsed.HttpOnly, Is.EqualTo(cookie.HttpOnly));
        Assert.That(parsed.HasSameSite, Is.EqualTo(cookie.HasSameSite));
        Assert.That(parsed.SameSiteMode, Is.EqualTo(cookie.SameSiteMode));
        Assert.That(parsed.HasPartitionKey, Is.EqualTo(cookie.HasPartitionKey));
        Assert.That(parsed.PartitionKey, Is.EqualTo(cookie.PartitionKey));
        Assert.That(parsed.IsValueQuoted, Is.EqualTo(cookie.IsValueQuoted));
        Assert.That(parsed.SequenceNumber, Is.EqualTo(cookie.SequenceNumber));
        Assert.That(parsed.Extensions.Select(extension => extension.Name),
            Is.EqualTo(new[] { "Priority", "Flag" }));
        Assert.That(throwing.NextSequenceNumber, Is.EqualTo(8));

        parsed.Value = "changed-result";
        parsed.Extensions[0].Value = "changed-result";
        throwing.Cookies[0].Value = "changed-throwing";
        CookieJarSnapshot repeated = codec.ParseJarSnapshot(text);
        Assert.That(repeated.Cookies[0].Value, Is.EqualTo(cookie.Value));
        Assert.That(repeated.Cookies[0].Extensions[0].Value, Is.EqualTo("Hígh"));
    }

    [Test]
    public void ReaderRejectsMalformedMissingUnknownDuplicateAndWrongTypedSchema()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        string[] invalid =
        [
            "{",
            "{\"version\":1,\"nextSequenceNumber\":0}",
            "{\"version\":1,\"nextSequenceNumber\":0,\"cookies\":[],\"unknown\":true}",
            "{\"version\":1,\"version\":1,\"nextSequenceNumber\":0,\"cookies\":[]}",
            "{\"version\":1.0,\"nextSequenceNumber\":0,\"cookies\":[]}"
        ];

        int index = 0;
        while (index < invalid.Length)
        {
            CookieSnapshotParseResult result =
                codec.TryParseJarSnapshot(invalid[index]);
            CookieSnapshotException exception =
                Assert.Throws<CookieSnapshotException>(
                    () => codec.ParseJarSnapshot(invalid[index]));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Snapshot, Is.Null);
            Assert.That(result.ErrorCode,
                Is.EqualTo(CookieErrorCode.InvalidSnapshot));
            Assert.That(result.Issues.Single().ErrorCode,
                Is.EqualTo(CookieErrorCode.InvalidSnapshot));
            Assert.That(exception.Message,
                Is.EqualTo("Cookie snapshot failed with error code 38."));
            Assert.That(exception.Message, Does.Not.Contain(invalid[index]));
            index++;
        }
    }

    [Test]
    public void ReaderReportsUnsupportedVersionAndDuplicateIdentityPrecisely()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        string unsupported =
            "{\"version\":2,\"nextSequenceNumber\":0,\"cookies\":[]}";
        StoredCookie first = CompleteCookie();
        StoredCookie second = CompleteCookie();
        second.Name = "different";
        second.SequenceNumber = 6;
        string unique = codec.SerializeJarSnapshot(new CookieJarSnapshot
        {
            Cookies = [first, second],
            NextSequenceNumber = 8
        });
        string duplicate = unique.Replace(
            "\"name\":\"different\"",
            "\"name\":\"id\"");

        CookieSnapshotParseResult versionResult =
            codec.TryParseJarSnapshot(unsupported);
        CookieSnapshotParseResult duplicateResult =
            codec.TryParseJarSnapshot(duplicate);

        Assert.That(versionResult.ErrorCode,
            Is.EqualTo(CookieErrorCode.UnsupportedSnapshotVersion));
        Assert.That(duplicateResult.ErrorCode,
            Is.EqualTo(CookieErrorCode.DuplicateIdentity));
        Assert.That(
            Assert.Throws<CookieSnapshotException>(
                () => codec.ParseJarSnapshot(unsupported)).Message,
            Is.EqualTo("Cookie snapshot failed with error code 39."));
        Assert.That(
            Assert.Throws<CookieSnapshotException>(
                () => codec.ParseJarSnapshot(duplicate)).Message,
            Is.EqualTo("Cookie snapshot failed with error code 40."));
    }

    [Test]
    public void ReaderEnforcesCanonicalStateTimestampAndSizeLimits()
    {
        ICookieSnapshotCodec codec = new CookieSnapshotCodec();
        string valid = codec.SerializeJarSnapshot(new CookieJarSnapshot
        {
            Cookies = [CompleteCookie()],
            NextSequenceNumber = 8
        });
        string noncanonical = valid.Replace("example.test", "EXAMPLE.TEST");
        string invalidTime = valid.Replace(
            "\"expires\":\"2030-06-09T04:48:14.0000000\\u002B00:00\"",
            "\"expires\":\"not-a-time\"");
        string oversizedValue = new string(
            'x',
            CookieDefaults.MaximumNameValueOctets + 1);
        StoredCookie sizeCookie = CompleteCookie();
        sizeCookie.Value = "v";
        string sizeText = codec.SerializeJarSnapshot(new CookieJarSnapshot
        {
            Cookies = [sizeCookie],
            NextSequenceNumber = 8
        });
        string oversized = sizeText.Replace(
            "\"value\":\"v\"",
            "\"value\":\"" + oversizedValue + "\"");

        Assert.That(codec.TryParseJarSnapshot(noncanonical).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidSnapshot));
        Assert.That(codec.TryParseJarSnapshot(invalidTime).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidSnapshot));
        Assert.That(codec.TryParseJarSnapshot(oversized).ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidSnapshot));
    }

    static StoredCookie CompleteCookie()
    {
        return new StoredCookie
        {
            Name = "id",
            Value = "quoted ü value",
            Domain = "example.test",
            Path = "/account",
            HasExpires = true,
            Expires = new DateTimeOffset(
                2030,
                6,
                9,
                10,
                18,
                14,
                new TimeSpan(5, 30, 0)),
            CreationTime = new DateTimeOffset(
                2030,
                6,
                9,
                10,
                18,
                14,
                new TimeSpan(2, 0, 0)),
            LastAccessTime = new DateTimeOffset(
                2030,
                6,
                9,
                10,
                18,
                14,
                new TimeSpan(-4, 0, 0)),
            Persistent = true,
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict,
            HasPartitionKey = true,
            PartitionKey = "https://top.example",
            IsValueQuoted = true,
            Extensions =
            [
                new CookieAttribute
                {
                    Name = "Priority",
                    HasValue = true,
                    Value = "Hígh"
                },
                new CookieAttribute
                {
                    Name = "Flag"
                }
            ],
            SequenceNumber = 7
        };
    }
}
