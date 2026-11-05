namespace cookies.Tests;

public class NetscapeCookieFileCodecTests
{
    static readonly DateTimeOffset Now = new DateTimeOffset(
        2030,
        1,
        1,
        0,
        0,
        0,
        TimeSpan.Zero);

    [Test]
    public void ReaderParsesMozillaAndCurlFieldsCommentsAndHttpOnlyPrefix()
    {
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        string text =
            "# Netscape HTTP Cookie File\r\n" +
            ".Example.COM\tTRUE\t/account\tTRUE\t1893456001\tid\tvalue\r\n" +
            "#HttpOnly_host.example\tFALSE\t\tFALSE\t0\tsession\t\r\n" +
            "   # a Mozilla comment\r\n";

        NetscapeCookieFileParseResult result =
            codec.TryParseNetscapeCookieFile(text, new NetscapeCookieFileOptions());
        StoredCookie[] throwing =
            codec.ParseNetscapeCookieFile(text, new NetscapeCookieFileOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookies, Has.Length.EqualTo(2));
        StoredCookie persistent = result.Cookies[0];
        Assert.That(persistent.Name, Is.EqualTo("id"));
        Assert.That(persistent.Value, Is.EqualTo("value"));
        Assert.That(persistent.Domain, Is.EqualTo("example.com"));
        Assert.That(persistent.Path, Is.EqualTo("/account"));
        Assert.That(persistent.HostOnly, Is.False);
        Assert.That(persistent.Secure, Is.True);
        Assert.That(persistent.HttpOnly, Is.False);
        Assert.That(persistent.HasExpires, Is.True);
        Assert.That(persistent.Persistent, Is.True);
        Assert.That(persistent.Expires, Is.EqualTo(
            new DateTimeOffset(2030, 1, 1, 0, 0, 1, TimeSpan.Zero)));
        Assert.That(persistent.CreationTime, Is.EqualTo(Now));
        Assert.That(persistent.LastAccessTime, Is.EqualTo(Now));
        Assert.That(persistent.SequenceNumber, Is.Zero);
        StoredCookie session = result.Cookies[1];
        Assert.That(session.Name, Is.EqualTo("session"));
        Assert.That(session.Value, Is.Empty);
        Assert.That(session.Domain, Is.EqualTo("host.example"));
        Assert.That(session.Path, Is.EqualTo("/"));
        Assert.That(session.HostOnly, Is.True);
        Assert.That(session.HttpOnly, Is.True);
        Assert.That(session.HasExpires, Is.False);
        Assert.That(session.Persistent, Is.False);
        Assert.That(session.SequenceNumber, Is.EqualTo(1));
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new[] { CookieErrorCode.LossyImport, CookieErrorCode.LossyImport }));
        Assert.That(result.Issues.All(issue =>
            issue.Severity == CookieParseSeverity.Warning &&
            issue.HasOffset && issue.SafeFragment == ""), Is.True);
        Assert.That(throwing.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "id", "session" }));
    }

    [Test]
    public void ReaderUsesOptionsAndClockForSessionAndExpiredEntries()
    {
        NetscapeCodecClock clock = new NetscapeCodecClock(Now);
        INetscapeCookieFileCodec codec = new NetscapeCookieFileCodec(clock);
        string text =
            "example.test\tFALSE\t/\tFALSE\t0\tsession\tone\n" +
            "example.test\tFALSE\t/\tFALSE\t1\texpired\ttwo\n" +
            "example.test\tFALSE\t/\tFALSE\t1893456001\tfuture\tthree";

        NetscapeCookieFileParseResult defaults =
            codec.TryParseNetscapeCookieFile(text, new NetscapeCookieFileOptions());
        NetscapeCookieFileOptions inverseOptions = new NetscapeCookieFileOptions();
        inverseOptions.ImportSessionCookies = false;
        inverseOptions.ImportExpiredCookies = true;
        NetscapeCookieFileParseResult inverse =
            codec.TryParseNetscapeCookieFile(text, inverseOptions);

        Assert.That(defaults.Success, Is.True);
        Assert.That(defaults.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "session", "future" }));
        Assert.That(inverse.Success, Is.True);
        Assert.That(inverse.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "expired", "future" }));
        Assert.That(defaults.Issues, Has.Length.EqualTo(3));
        Assert.That(inverse.Issues, Has.Length.EqualTo(3));
        Assert.That(clock.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void ReaderReportsMalformedLinesAndRetainsValidEntriesInTryResult()
    {
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        string valid = "example.test\tFALSE\t/\tFALSE\t0\tgood\tvalue";
        string text =
            "bad fields\n" +
            "example.test\tMAYBE\t/\tFALSE\t0\tbad\tvalue\n" +
            "example.test\tFALSE\t/\tFALSE\t-1\tbad\tvalue\n" +
            "example.test\tFALSE\t/\tFALSE\t0\tbad\tvalue\textra\n" +
            valid;

        NetscapeCookieFileParseResult result =
            codec.TryParseNetscapeCookieFile(text, new NetscapeCookieFileOptions());
        CookieParseException exception = Assert.Throws<CookieParseException>(() =>
            codec.ParseNetscapeCookieFile(text, new NetscapeCookieFileOptions()));

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedNetscapeLine));
        Assert.That(result.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "good" }));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.MalformedNetscapeLine), Is.EqualTo(4));
        Assert.That(result.Issues.Last().ErrorCode,
            Is.EqualTo(CookieErrorCode.LossyImport));
        Assert.That(result.Issues.All(issue => issue.SafeFragment == ""), Is.True);
        Assert.That(exception.Message,
            Is.EqualTo("Cookie parsing failed with error code 42."));
        Assert.That(exception.Message, Does.Not.Contain("value"));
    }

    [Test]
    public void ReaderEnforcesInputFieldAndCookieLimits()
    {
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        string first = "example.test\tFALSE\t/\tFALSE\t0\ta\t1";
        string second = "example.test\tFALSE\t/\tFALSE\t0\tb\t2";
        NetscapeCookieFileOptions lineLimit = new NetscapeCookieFileOptions();
        lineLimit.Limits.MaximumInputLineOctets = 3;
        NetscapeCookieFileOptions pairLimit = new NetscapeCookieFileOptions();
        pairLimit.Limits.MaximumNameValueOctets = 1;
        NetscapeCookieFileOptions countLimit = new NetscapeCookieFileOptions();
        countLimit.Limits.MaximumCookies = 1;

        NetscapeCookieFileParseResult line =
            codec.TryParseNetscapeCookieFile(first, lineLimit);
        NetscapeCookieFileParseResult pair =
            codec.TryParseNetscapeCookieFile(first, pairLimit);
        NetscapeCookieFileParseResult count =
            codec.TryParseNetscapeCookieFile(first + "\n" + second, countLimit);

        Assert.That(line.ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedNetscapeLine));
        Assert.That(pair.ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedNetscapeLine));
        Assert.That(count.Success, Is.False);
        Assert.That(count.ErrorCode,
            Is.EqualTo(CookieErrorCode.TotalLimitExceeded));
        Assert.That(count.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "a" }));
        Assert.That(count.Issues.Last().ErrorCode,
            Is.EqualTo(CookieErrorCode.TotalLimitExceeded));
    }

    [Test]
    public void ReaderRejectsInvalidConfigurationAndClockFailures()
    {
        string text = "example.test\tFALSE\t/\tFALSE\t0\ta\t1";
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        NetscapeCookieFileOptions invalid = new NetscapeCookieFileOptions();
        invalid.Limits.MaximumCookies = -1;
        CookieSerializationException missingClock =
            Assert.Throws<CookieSerializationException>(() =>
                new NetscapeCookieFileCodec(null!));
        INetscapeCookieFileCodec failingClock =
            new NetscapeCookieFileCodec(new FailingNetscapeCodecClock());

        NetscapeCookieFileParseResult invalidResult =
            codec.TryParseNetscapeCookieFile(text, invalid);
        NetscapeCookieFileParseResult failingResult =
            failingClock.TryParseNetscapeCookieFile(
                text,
                new NetscapeCookieFileOptions());

        Assert.That(invalidResult.ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));
        Assert.That(missingClock.Message,
            Is.EqualTo("Cookie service configuration failed with error code 43."));
        Assert.That(failingResult.ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidConfiguration));
        Assert.That(invalidResult.Issues.Single().HasOffset, Is.False);
        Assert.That(failingResult.Issues.Single().HasOffset, Is.False);
    }

    [Test]
    public void ReaderOwnsReturnedCookieAndIssueGraphs()
    {
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        string text = "example.test\tFALSE\t/\tFALSE\t0\ta\t1";

        NetscapeCookieFileParseResult first =
            codec.TryParseNetscapeCookieFile(text, new NetscapeCookieFileOptions());
        first.Cookies[0].Name = "changed";
        first.Issues[0].SafeFragment = "changed";
        NetscapeCookieFileParseResult second =
            codec.TryParseNetscapeCookieFile(text, new NetscapeCookieFileOptions());

        Assert.That(second.Cookies[0].Name, Is.EqualTo("a"));
        Assert.That(second.Issues[0].SafeFragment, Is.Empty);
    }

    [Test]
    public void WriterEmitsStableCurlCompatibleTextInSequenceOrder()
    {
        NetscapeCodecClock clock = new NetscapeCodecClock(Now);
        INetscapeCookieFileCodec codec = new NetscapeCookieFileCodec(clock);
        StoredCookie persistent = SerializableCookie("id", "value", 2);
        persistent.Domain = "example.com";
        persistent.Path = "/account";
        persistent.HostOnly = false;
        persistent.Secure = true;
        persistent.HasExpires = true;
        persistent.Persistent = true;
        persistent.Expires = Now.AddSeconds(1);
        persistent.HasSameSite = true;
        persistent.SameSiteMode = CookieSameSiteMode.Strict;
        persistent.Extensions =
        [
            new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" }
        ];
        StoredCookie session = SerializableCookie("session", "", 1);
        session.Domain = "host.example";
        session.HttpOnly = true;

        string text = codec.SerializeNetscapeCookieFile(
            [persistent, session],
            new NetscapeCookieFileOptions());
        NetscapeCookieFileParseResult parsed = codec.TryParseNetscapeCookieFile(
            text,
            new NetscapeCookieFileOptions());

        string expected =
            "# Netscape HTTP Cookie File\n" +
            "# https://curl.se/docs/http-cookies.html\n" +
            "# This file was generated by cookies. Edit at your own risk.\n\n" +
            "#HttpOnly_host.example\tFALSE\t/\tFALSE\t0\tsession\t\n" +
            ".example.com\tTRUE\t/account\tTRUE\t1893456001\tid\tvalue\n";
        Assert.That(text, Is.EqualTo(expected));
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Cookies.Select(cookie => cookie.Name),
            Is.EqualTo(new[] { "session", "id" }));
        Assert.That(parsed.Cookies[0].HttpOnly, Is.True);
        Assert.That(parsed.Cookies[1].HostOnly, Is.False);
        Assert.That(parsed.Cookies[1].Secure, Is.True);
        Assert.That(parsed.Issues.All(issue =>
            issue.ErrorCode == CookieErrorCode.LossyImport), Is.True);
        Assert.That(clock.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void WriterFiltersSessionAndExpiredCookiesUsingOptionsAndClock()
    {
        NetscapeCodecClock clock = new NetscapeCodecClock(Now);
        INetscapeCookieFileCodec codec = new NetscapeCookieFileCodec(clock);
        StoredCookie session = SerializableCookie("session", "one", 1);
        StoredCookie expired = SerializableCookie("expired", "two", 2);
        expired.HasExpires = true;
        expired.Persistent = true;
        expired.Expires = Now.AddSeconds(-1);
        StoredCookie future = SerializableCookie("future", "three", 3);
        future.HasExpires = true;
        future.Persistent = true;
        future.Expires = Now.AddSeconds(1);
        StoredCookie[] cookies = [future, expired, session];

        string defaults = codec.SerializeNetscapeCookieFile(
            cookies,
            new NetscapeCookieFileOptions());
        NetscapeCookieFileOptions inverseOptions = new NetscapeCookieFileOptions();
        inverseOptions.IncludeSessionCookies = false;
        inverseOptions.IncludeExpiredCookies = true;
        string inverse = codec.SerializeNetscapeCookieFile(cookies, inverseOptions);

        Assert.That(defaults, Does.Contain("\tsession\tone\n"));
        Assert.That(defaults, Does.Contain("\tfuture\tthree\n"));
        Assert.That(defaults, Does.Not.Contain("\texpired\ttwo\n"));
        Assert.That(inverse, Does.Not.Contain("\tsession\tone\n"));
        Assert.That(inverse, Does.Contain("\texpired\ttwo\n"));
        Assert.That(inverse, Does.Contain("\tfuture\tthree\n"));
        Assert.That(clock.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void WriterRejectsPartitionLossByDefaultAndProjectedIdentityAmbiguity()
    {
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        StoredCookie first = SerializableCookie("id", "one", 1);
        first.HasPartitionKey = true;
        first.PartitionKey = "https://top-a.example";
        NetscapeCookieFileOptions allowLoss = new NetscapeCookieFileOptions();
        allowLoss.RejectPartitionedCookies = false;

        CookieSerializationException rejected =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile(
                    [first],
                    new NetscapeCookieFileOptions()));
        string lossy = codec.SerializeNetscapeCookieFile([first], allowLoss);
        NetscapeCookieFileParseResult parsed = codec.TryParseNetscapeCookieFile(
            lossy,
            new NetscapeCookieFileOptions());
        StoredCookie second = SerializableCookie("id", "two", 2);
        second.HasPartitionKey = true;
        second.PartitionKey = "https://top-b.example";
        CookieSerializationException ambiguous =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile([first, second], allowLoss));

        Assert.That(rejected.Message,
            Is.EqualTo("Cookie serialization failed with error code 41."));
        Assert.That(lossy, Does.Contain("\tid\tone\n"));
        Assert.That(parsed.Cookies.Single().HasPartitionKey, Is.False);
        Assert.That(parsed.Issues.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.LossyImport));
        Assert.That(ambiguous.Message,
            Is.EqualTo("Cookie serialization failed with error code 41."));
    }

    [Test]
    public void WriterRejectsUnsafeInconsistentAndNonRoundTrippableStateSafely()
    {
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        StoredCookie injected = SerializableCookie("id", "private\tsecret", 1);
        StoredCookie inconsistent = SerializableCookie("id", "value", 1);
        inconsistent.Persistent = true;
        StoredCookie fractional = SerializableCookie("id", "value", 1);
        fractional.HasExpires = true;
        fractional.Persistent = true;
        fractional.Expires = Now.AddTicks(1);

        CookieSerializationException injectionFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile(
                    [injected],
                    new NetscapeCookieFileOptions()));
        CookieSerializationException inconsistentFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile(
                    [inconsistent],
                    new NetscapeCookieFileOptions()));
        CookieSerializationException fractionalFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile(
                    [fractional],
                    new NetscapeCookieFileOptions()));

        Assert.That(injectionFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 2."));
        Assert.That(injectionFailure.Message, Does.Not.Contain("private"));
        Assert.That(injectionFailure.Message, Does.Not.Contain("secret"));
        Assert.That(inconsistentFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 36."));
        Assert.That(fractionalFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 36."));
    }

    [Test]
    public void WriterEnforcesConfigurationAndOutputLimits()
    {
        StoredCookie cookie = SerializableCookie("id", "value", 1);
        INetscapeCookieFileCodec codec =
            new NetscapeCookieFileCodec(new NetscapeCodecClock(Now));
        NetscapeCookieFileOptions invalid = new NetscapeCookieFileOptions();
        invalid.Limits.MaximumCookies = -1;
        NetscapeCookieFileOptions lineLimit = new NetscapeCookieFileOptions();
        lineLimit.Limits.MaximumInputLineOctets = 3;
        NetscapeCookieFileOptions pairLimit = new NetscapeCookieFileOptions();
        pairLimit.Limits.MaximumNameValueOctets = 2;
        NetscapeCookieFileOptions countLimit = new NetscapeCookieFileOptions();
        countLimit.Limits.MaximumCookies = 0;

        CookieSerializationException invalidFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile([cookie], invalid));
        CookieSerializationException lineFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile([cookie], lineLimit));
        CookieSerializationException pairFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile([cookie], pairLimit));
        CookieSerializationException countFailure =
            Assert.Throws<CookieSerializationException>(() =>
                codec.SerializeNetscapeCookieFile([cookie], countLimit));
        INetscapeCookieFileCodec failingClock =
            new NetscapeCookieFileCodec(new FailingNetscapeCodecClock());
        CookieSerializationException clockFailure =
            Assert.Throws<CookieSerializationException>(() =>
                failingClock.SerializeNetscapeCookieFile(
                    [cookie],
                    new NetscapeCookieFileOptions()));

        Assert.That(invalidFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 43."));
        Assert.That(lineFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 31."));
        Assert.That(pairFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 29."));
        Assert.That(countFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 34."));
        Assert.That(clockFailure.Message,
            Is.EqualTo("Cookie serialization failed with error code 43."));
    }

    static StoredCookie SerializableCookie(
        string name,
        string value,
        long sequenceNumber)
    {
        return new StoredCookie
        {
            Name = name,
            Value = value,
            Domain = "example.test",
            Path = "/",
            HostOnly = true,
            CreationTime = Now,
            LastAccessTime = Now,
            SequenceNumber = sequenceNumber
        };
    }
}

public class NetscapeCodecClock : ICookieClock
{
    public DateTimeOffset Value;
    public int CallCount;

    public NetscapeCodecClock(DateTimeOffset value)
    {
        Value = value;
    }

    public DateTimeOffset GetUtcNow()
    {
        CallCount = CallCount + 1;
        return Value;
    }
}

public class FailingNetscapeCodecClock : ICookieClock
{
    public FailingNetscapeCodecClock()
    {
    }

    public DateTimeOffset GetUtcNow()
    {
        throw new InvalidOperationException("clock failure");
    }
}
