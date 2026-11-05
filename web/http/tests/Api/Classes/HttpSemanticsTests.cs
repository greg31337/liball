using System.Text;
using System.Reflection;

namespace http.Tests;

public class HttpSemanticsTests
{
    [Test]
    public void PublicServiceExposesCompletedCommonSemantics()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest request = Request(HttpMethods.Get, "/", "example.com");
        request.Scheme = "https";
        HttpResponse response = new HttpResponse();
        response.StatusCode = HttpStatusCodes.Ok;

        Assert.Multiple(() =>
        {
            Assert.That(semantics.DescribeMethod(HttpMethods.Get).IsSafe, Is.True);
            Assert.That(semantics.DescribeStatus(HttpStatusCodes.Ok).ReasonPhrase,
                Is.EqualTo(HttpReasonPhrases.Ok));
            Assert.That(semantics.ClassifyStatus(404), Is.EqualTo(HttpStatusClass.ClientError));
            Assert.That(semantics.DetermineRequestTarget(request),
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(semantics.BuildEffectiveRequestUri(request),
                Is.EqualTo("https://example.com/"));
            Assert.That(semantics.DetermineResponseContent(request, response), Is.True);
        });
    }

    [Test]
    public void HopFieldDetectionIncludesFixedAndConnectionNominatedFields()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpHeaders headers = new HttpHeaders();
        AddField(headers, HttpHeaderNames.Connection, "X-Hop, close");
        AddField(headers, "X-Hop", "private");
        AddField(headers, "Proxy-Connection", "keep-alive");
        AddField(headers, HttpHeaderNames.KeepAlive, "timeout=5");
        AddField(headers, HttpHeaderNames.TE, "trailers");
        AddField(headers, HttpHeaderNames.Trailer, "Digest");
        AddField(headers, HttpHeaderNames.TransferEncoding, "chunked");
        AddField(headers, HttpHeaderNames.Upgrade, "websocket");
        AddField(headers, "X-End-To-End", "retained");
        AddField(headers, HttpHeaderNames.Via, "1.1 proxy");

        HttpHeaders hop = semantics.DetermineHopFields(headers);

        Assert.Multiple(() =>
        {
            Assert.That(hop.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                HttpHeaderNames.Connection,
                "X-Hop",
                "Proxy-Connection",
                HttpHeaderNames.KeepAlive,
                HttpHeaderNames.TE,
                HttpHeaderNames.TransferEncoding,
                HttpHeaderNames.Upgrade
            }));
            Assert.That(Values(hop, HttpHeaderNames.Trailer), Is.Empty);
            Assert.That(hop.Fields[0], Is.Not.SameAs(headers.Fields[0]));
            Assert.That(hop.Fields[0].Value, Is.Not.SameAs(headers.Fields[0].Value));
        });
    }

    [Test]
    public void ForwardingRemovesHopFieldsRetainsTeTrailersAndNormalizesVia()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest request = Request(HttpMethods.Options, "/status", "example.com");
        AddField(request.Headers, HttpHeaderNames.Connection, "X-Hop, TE");
        AddField(request.Headers, "X-Hop", "private");
        AddField(request.Headers, HttpHeaderNames.KeepAlive, "timeout=5");
        AddField(request.Headers, HttpHeaderNames.TE, "gzip;q=0.5, trailers");
        AddField(request.Headers, HttpHeaderNames.Trailer, "Digest, X-Hop");
        AddField(request.Headers, HttpHeaderNames.MaxForwards, "3");
        AddField(request.Headers, HttpHeaderNames.Via, "1.0 first");
        AddField(request.Headers, HttpHeaderNames.Via, "HTTP/1.1 second:8080 (edge)");
        AddField(request.Headers, "X-End-To-End", "retained");
        AddField(request.Trailers, "Digest", "complete");
        AddField(request.Trailers, "X-Hop", "private-trailer");

        HttpRequest forwarded = semantics.NormalizeForwardedMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(Values(forwarded.Headers, "X-Hop"), Is.Empty);
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.KeepAlive), Is.Empty);
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.TE),
                Is.EqualTo(new[] { "trailers" }));
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.Connection),
                Is.EqualTo(new[] { HttpHeaderNames.TE }));
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.Trailer),
                Is.EqualTo(new[] { "Digest, X-Hop" }));
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.MaxForwards),
                Is.EqualTo(new[] { "2" }));
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.Via),
                Is.EqualTo(new[] { "1.0 first, HTTP/1.1 second:8080 (edge)" }));
            Assert.That(forwarded.ViaEntries, Has.Count.EqualTo(2));
            Assert.That(Values(forwarded.Headers, "X-End-To-End"),
                Is.EqualTo(new[] { "retained" }));
            Assert.That(Values(forwarded.Trailers, "Digest"),
                Is.EqualTo(new[] { "complete" }));
            Assert.That(Values(forwarded.Trailers, "X-Hop"), Is.Empty);
            Assert.That(Values(request.Headers, "X-Hop"), Is.EqualTo(new[] { "private" }));
            Assert.That(Values(request.Headers, HttpHeaderNames.MaxForwards),
                Is.EqualTo(new[] { "3" }));
            Assert.That(forwarded.Headers, Is.Not.SameAs(request.Headers));
        });
    }

    [Test]
    public void ForwardedRequestOwnsMutableCopiedData()
    {
        HttpRequest request = Request(HttpMethods.Get, "/", "example.com");
        request.Content = new byte[] { 1, 2, 3 };
        HttpWeightedValue preference = new HttpWeightedValue();
        preference.Value = "text/plain";
        HttpParameter parameter = new HttpParameter();
        parameter.Name = "level";
        parameter.HasValue = true;
        parameter.Value = new byte[] { 1 };
        preference.Parameters.Add(parameter);
        request.WeightedPreferences.Add(preference);
        HttpAuthenticationValue authentication = new HttpAuthenticationValue();
        authentication.Scheme = "Bearer";
        authentication.Token68 = new byte[] { 2 };
        request.AuthenticationValues.Add(authentication);

        HttpRequest forwarded = new HttpSemantics().NormalizeForwardedMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(forwarded.Content, Is.Not.SameAs(request.Content));
            Assert.That(forwarded.WeightedPreferences[0],
                Is.Not.SameAs(request.WeightedPreferences[0]));
            Assert.That(forwarded.WeightedPreferences[0].Parameters[0],
                Is.Not.SameAs(request.WeightedPreferences[0].Parameters[0]));
            Assert.That(forwarded.WeightedPreferences[0].Parameters[0].Value,
                Is.Not.SameAs(request.WeightedPreferences[0].Parameters[0].Value));
            Assert.That(forwarded.AuthenticationValues[0].Token68,
                Is.Not.SameAs(request.AuthenticationValues[0].Token68));
        });
    }

    [Test]
    public void MaxForwardsHonorsZeroAndRecipientLimit()
    {
        HttpRequest exhausted = Request(HttpMethods.Trace, "/", "example.com");
        AddField(exhausted.Headers, HttpHeaderNames.MaxForwards, "0");
        HttpRequest capped = Request(HttpMethods.Options, "/", "example.com");
        AddField(capped.Headers, HttpHeaderNames.MaxForwards, "100");
        HttpRequest ignored = Request(HttpMethods.Get, "/", "example.com");
        AddField(ignored.Headers, HttpHeaderNames.MaxForwards, "5");

        bool canForward = IntermediarySemantics.ApplyMaxForwards(capped, 10);
        bool ignoredResult = IntermediarySemantics.ApplyMaxForwards(ignored, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                new HttpSemantics().NormalizeForwardedMessage(exhausted));
            Assert.That(canForward, Is.True);
            Assert.That(Values(capped.Headers, HttpHeaderNames.MaxForwards),
                Is.EqualTo(new[] { "10" }));
            Assert.That(ignoredResult, Is.True);
            Assert.That(Values(ignored.Headers, HttpHeaderNames.MaxForwards),
                Is.EqualTo(new[] { "5" }));
        });
    }

    [Test]
    public void AbsoluteForwardingReplacesHostFromRequestTarget()
    {
        HttpRequest request = Request(
            HttpMethods.Get,
            "http://target.example:8080/path?q=1",
            "untrusted.example");

        HttpRequest forwarded = new HttpSemantics().NormalizeForwardedMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.Host),
                Is.EqualTo(new[] { "target.example:8080" }));
            Assert.That(forwarded.Scheme, Is.EqualTo("http"));
            Assert.That(forwarded.Authority, Is.EqualTo("target.example:8080"));
            Assert.That(forwarded.Path, Is.EqualTo("/path"));
            Assert.That(forwarded.HasQuery, Is.True);
            Assert.That(forwarded.Query, Is.EqualTo("q=1"));
            Assert.That(Values(request.Headers, HttpHeaderNames.Host),
                Is.EqualTo(new[] { "untrusted.example" }));
        });
    }

    [Test]
    public void SameOriginComparisonIsSchemeAndDefaultPortAware()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IntermediarySemantics.AreSameOrigin(
                "http://EXAMPLE.com:80/a",
                "http://example.com/b?q=1"), Is.True);
            Assert.That(IntermediarySemantics.AreSameOrigin(
                "https://%65xample.com:/a",
                "https://example.com:443/b"), Is.True);
            Assert.That(IntermediarySemantics.AreSameOrigin(
                "http://example.com/",
                "https://example.com/"), Is.False);
            Assert.That(IntermediarySemantics.AreSameOrigin(
                "http://example.com:81/",
                "http://example.com/"), Is.False);
            Assert.That(IntermediarySemantics.AreSameOrigin(
                "http://user@example.com/",
                "http://example.com/"), Is.False);
            Assert.That(IntermediarySemantics.AreSameOrigin(
                "/relative",
                "http://example.com/"), Is.False);
        });
    }

    [Test]
    public void MalformedConnectionTeViaAndMaxForwardsAreRejected()
    {
        HttpRequest connection = Request(HttpMethods.Get, "/", "example.com");
        AddField(connection.Headers, HttpHeaderNames.Connection, "bad value");
        HttpRequest te = Request(HttpMethods.Get, "/", "example.com");
        AddField(te.Headers, HttpHeaderNames.TE, "trailers;q=1");
        HttpRequest via = Request(HttpMethods.Get, "/", "example.com");
        AddField(via.Headers, HttpHeaderNames.Via, "1.1");
        HttpRequest forwards = Request(HttpMethods.Options, "/", "example.com");
        AddField(forwards.Headers, HttpHeaderNames.MaxForwards, "-1");
        HttpRequest trailers = Request(HttpMethods.Get, "/", "example.com");
        AddField(trailers.Trailers, HttpHeaderNames.ContentLength, "1");

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                new HttpSemantics().DetermineHopFields(connection.Headers));
            Assert.Throws<HttpProtocolException>(() =>
                new HttpSemantics().NormalizeForwardedMessage(te));
            Assert.Throws<HttpProtocolException>(() =>
                new HttpSemantics().NormalizeForwardedMessage(via));
            Assert.Throws<HttpProtocolException>(() =>
                new HttpSemantics().NormalizeForwardedMessage(forwards));
            Assert.Throws<HttpProtocolException>(() =>
                new HttpSemantics().NormalizeForwardedMessage(trailers));
        });
    }

    [Test]
    public void EveryRegisteredMethodMatchesTheIanaSafetyAndIdempotencySnapshot()
    {
        IHttpSemantics semantics = new HttpSemantics();
        string[] rows = ReadRegistryLines("http-methods.csv");

        Assert.Multiple(() =>
        {
            foreach (string row in rows)
            {
                string[] columns = row.Split(',');
                string method = columns[0];
                HttpMethodMetadata metadata = semantics.DescribeMethod(method);
                Assert.That(metadata.Method, Is.EqualTo(method), method);
                Assert.That(metadata.IsRegistered, Is.True, method);
                Assert.That(metadata.IsSafe, Is.EqualTo(columns[1] == "yes"), method);
                Assert.That(metadata.IsIdempotent, Is.EqualTo(columns[2] == "yes"), method);
            }
        });
    }

    [Test]
    public void EveryCoreMethodExposesItsRequestContentAndCacheSemantics()
    {
        IHttpSemantics semantics = new HttpSemantics();
        string[] core =
        [
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Delete,
            HttpMethods.Connect,
            HttpMethods.Options,
            HttpMethods.Trace
        ];

        Assert.Multiple(() =>
        {
            foreach (string method in core)
            {
                Assert.That(semantics.DescribeMethod(method).IsCore, Is.True, method);
            }

            Assert.That(semantics.DescribeMethod(HttpMethods.Get).IsCacheable, Is.True);
            Assert.That(semantics.DescribeMethod(HttpMethods.Head).IsCacheable, Is.True);
            Assert.That(semantics.DescribeMethod(HttpMethods.Post).IsCacheable, Is.True);
            Assert.That(semantics.DescribeMethod(HttpMethods.Put).IsCacheable, Is.False);
            Assert.That(semantics.DescribeMethod(HttpMethods.Post)
                .HasDefinedRequestContentSemantics, Is.True);
            Assert.That(semantics.DescribeMethod(HttpMethods.Put)
                .HasDefinedRequestContentSemantics, Is.True);
            Assert.That(semantics.DescribeMethod(HttpMethods.Delete)
                .HasDefinedRequestContentSemantics, Is.False);
        });
    }

    [Test]
    public void ExtensionMethodsAreCaseSensitiveConservativeAndValidatedAsTokens()
    {
        IHttpSemantics semantics = new HttpSemantics();
        string[] extensions = ["get", "CUSTOM", "X-DO!THING", "123", "#"];
        string[] invalid = ["", "HAS SPACE", "GET/", "GET\t", "méthode", "GET\r\nHost"];

        Assert.Multiple(() =>
        {
            foreach (string method in extensions)
            {
                HttpMethodMetadata metadata = semantics.DescribeMethod(method);
                Assert.That(metadata.Method, Is.EqualTo(method), method);
                Assert.That(metadata.IsRegistered, Is.False, method);
                Assert.That(metadata.IsCore, Is.False, method);
                Assert.That(metadata.IsSafe, Is.False, method);
                Assert.That(metadata.IsIdempotent, Is.False, method);
                Assert.That(metadata.IsCacheable, Is.False, method);
                Assert.That(metadata.HasDefinedRequestContentSemantics, Is.False, method);
            }

            foreach (string method in invalid)
            {
                Assert.Throws<HttpProtocolException>(() => semantics.DescribeMethod(method), method);
            }
        });
    }

    [Test]
    public void EveryRegisteredStatusConstantHasPublicMetadataAndTheExpectedClass()
    {
        IHttpSemantics semantics = new HttpSemantics();
        int[] registered = typeof(HttpStatusCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (int)field.GetRawConstantValue()!)
            .ToArray();

        Assert.Multiple(() =>
        {
            foreach (int statusCode in registered)
            {
                HttpStatusMetadata metadata = semantics.DescribeStatus(statusCode);
                Assert.That(metadata.StatusCode, Is.EqualTo(statusCode), statusCode.ToString());
                Assert.That(metadata.StatusClass,
                    Is.EqualTo(ExpectedStatusClass(statusCode)), statusCode.ToString());
                Assert.That(metadata.IsRegistered, Is.True, statusCode.ToString());
                Assert.That(metadata.ReasonPhrase, Is.Not.Null.And.Not.Empty, statusCode.ToString());
            }
        });
    }

    [Test]
    public void ClassificationAndUnknownStatusMetadataCoverEveryThreeDigitCode()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HashSet<int> registered = typeof(HttpStatusCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (int)field.GetRawConstantValue()!)
            .ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(semantics.ClassifyStatus(99), Is.EqualTo(HttpStatusClass.Invalid));
            Assert.That(semantics.ClassifyStatus(1000), Is.EqualTo(HttpStatusClass.Invalid));
            for (int statusCode = 100; statusCode <= 999; statusCode++)
            {
                HttpStatusClass expected = ExpectedStatusClass(statusCode);
                HttpStatusMetadata metadata = semantics.DescribeStatus(statusCode);
                Assert.That(semantics.ClassifyStatus(statusCode),
                    Is.EqualTo(expected), statusCode.ToString());
                Assert.That(metadata.StatusClass, Is.EqualTo(expected), statusCode.ToString());
                Assert.That(metadata.IsRegistered,
                    Is.EqualTo(registered.Contains(statusCode)), statusCode.ToString());
                if (!registered.Contains(statusCode))
                {
                    Assert.That(metadata.ReasonPhrase, Is.Null, statusCode.ToString());
                    Assert.That(metadata.IsCacheableByDefault, Is.False, statusCode.ToString());
                    Assert.That(metadata.IsRedirect, Is.False, statusCode.ToString());
                    Assert.That(metadata.RedirectsPostToGet, Is.False, statusCode.ToString());
                    Assert.That(metadata.IsRetryable, Is.False, statusCode.ToString());
                }
            }

            Assert.Throws<HttpProtocolException>(() => semantics.DescribeStatus(99));
            Assert.Throws<HttpProtocolException>(() => semantics.DescribeStatus(1000));
        });
    }

    [Test]
    public void RegisteredStatusesExposeDefinedCacheRedirectAndRetrySets()
    {
        IHttpSemantics semantics = new HttpSemantics();
        int[] defaultCacheable = [200, 203, 204, 206, 300, 301, 308, 404, 405, 410, 414, 501];
        int[] redirects = [300, 301, 302, 303, 305, 307, 308];
        int[] postToGet = [301, 302, 303];
        int[] retryable = [408, 413, 421, 425, 429, 500, 502, 503, 504];
        int[] registered = typeof(HttpStatusCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (int)field.GetRawConstantValue()!)
            .ToArray();

        Assert.Multiple(() =>
        {
            foreach (int statusCode in registered)
            {
                HttpStatusMetadata metadata = semantics.DescribeStatus(statusCode);
                Assert.That(metadata.IsCacheableByDefault,
                    Is.EqualTo(defaultCacheable.Contains(statusCode)), statusCode.ToString());
                Assert.That(metadata.IsRedirect,
                    Is.EqualTo(redirects.Contains(statusCode)), statusCode.ToString());
                Assert.That(metadata.RedirectsPostToGet,
                    Is.EqualTo(postToGet.Contains(statusCode)), statusCode.ToString());
                Assert.That(metadata.IsRetryable,
                    Is.EqualTo(retryable.Contains(statusCode)), statusCode.ToString());
            }
        });
    }

    [Test]
    public void PublicTargetDeterminationCoversAllFormsAndMethodRestrictions()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest origin = Request(HttpMethods.Get, "/items?q=one", "example.com");
        HttpRequest absolute = Request(
            HttpMethods.Get,
            "http://target.example:8080/items?q=one",
            "ignored.example");
        HttpRequest authority = Request(HttpMethods.Connect, "example.com:443", "example.com");
        HttpRequest asterisk = Request(HttpMethods.Options, "*", "example.com");

        Assert.Multiple(() =>
        {
            Assert.That(semantics.DetermineRequestTarget(origin),
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(semantics.DetermineRequestTarget(absolute),
                Is.EqualTo(HttpRequestTargetForm.Absolute));
            Assert.That(semantics.DetermineRequestTarget(authority),
                Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(semantics.DetermineRequestTarget(asterisk),
                Is.EqualTo(HttpRequestTargetForm.Asterisk));
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(
                Request(HttpMethods.Get, "*", "example.com")));
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(
                Request(HttpMethods.Connect, "/", "example.com")));
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(
                Request(HttpMethods.Options, "relative", "example.com")));
        });
    }

    [Test]
    public void EffectiveUrisCoverFormsQueriesIpv6AndTargetAuthorityPrecedence()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest origin = Request(HttpMethods.Get, "/path?", "example.com:8443");
        origin.Scheme = "https";
        HttpRequest absolute = Request(
            HttpMethods.Get,
            "http://target.example/resource?q=1",
            "different.example");
        HttpRequest authority = Request(HttpMethods.Connect, "[2001:db8::1]:443", "proxy.example");
        authority.Scheme = "https";
        HttpRequest asterisk = Request(HttpMethods.Options, "*", "example.com");

        Assert.Multiple(() =>
        {
            Assert.That(semantics.BuildEffectiveRequestUri(origin),
                Is.EqualTo("https://example.com:8443/path?"));
            Assert.That(semantics.BuildEffectiveRequestUri(absolute),
                Is.EqualTo("http://target.example/resource?q=1"));
            Assert.That(semantics.BuildEffectiveRequestUri(authority),
                Is.EqualTo("https://[2001:db8::1]:443"));
            Assert.That(semantics.BuildEffectiveRequestUri(asterisk),
                Is.EqualTo("http://example.com"));
        });
    }

    [Test]
    public void TargetAndEffectiveUriValidationRejectsEveryInvalidContextCategory()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest missingHost = new HttpRequest();
        missingHost.ProtocolVersion = HttpProtocolVersion.Http11;
        missingHost.Method = HttpMethods.Get;
        missingHost.Target = "/";
        missingHost.Scheme = "http";
        HttpRequest duplicateHost = Request(HttpMethods.Get, "/", "one.example");
        AddField(duplicateHost.Headers, HttpHeaderNames.Host, "two.example");
        HttpRequest missingScheme = Request(HttpMethods.Get, "/", "example.com");
        missingScheme.Scheme = null;
        string[] malformed = ["", "relative/path", "/bad path", "/bad%2", "/path#part"];

        Assert.Multiple(() =>
        {
            foreach (string target in malformed)
            {
                Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(
                    Request(HttpMethods.Get, target, "example.com")), target);
            }

            Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(missingHost));
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(duplicateHost));
            Assert.Throws<HttpProtocolException>(() =>
                semantics.BuildEffectiveRequestUri(missingScheme));
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineRequestTarget(null!));
            Assert.Throws<HttpProtocolException>(() => semantics.BuildEffectiveRequestUri(null!));
        });
    }

    [Test]
    public void ResponseContentRulesCoverEveryStatusForGetHeadAndConnect()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest get = Request(HttpMethods.Get, "/", "example.com");
        HttpRequest head = Request(HttpMethods.Head, "/", "example.com");
        HttpRequest connect = Request(HttpMethods.Connect, "example.com:443", "example.com");

        Assert.Multiple(() =>
        {
            for (int statusCode = 100; statusCode <= 999; statusCode++)
            {
                HttpResponse response = Response(statusCode);
                response.IsInformational = statusCode < 200;
                bool statusAllows = statusCode >= 200 && statusCode != 204 &&
                    statusCode != 205 && statusCode != 304;
                Assert.That(semantics.DetermineResponseContent(get, response),
                    Is.EqualTo(statusAllows), "GET " + statusCode);
                Assert.That(semantics.DetermineResponseContent(head, response),
                    Is.False, "HEAD " + statusCode);
                Assert.That(semantics.DetermineResponseContent(connect, response),
                    Is.EqualTo(statusAllows && (statusCode < 200 || statusCode >= 300)),
                    "CONNECT " + statusCode);
            }
        });
    }

    [Test]
    public void ResponseContentValidationRejectsInvalidOrInconsistentModels()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest request = Request(HttpMethods.Get, "/", "example.com");
        HttpResponse missingInformationalFlag = Response(103);
        HttpResponse extraInformationalFlag = Response(200);
        extraInformationalFlag.IsInformational = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() =>
                semantics.DetermineResponseContent(request, missingInformationalFlag));
            Assert.Throws<HttpProtocolException>(() =>
                semantics.DetermineResponseContent(request, extraInformationalFlag));
            Assert.Throws<HttpProtocolException>(() =>
                semantics.DetermineResponseContent(request, Response(99)));
            Assert.Throws<HttpProtocolException>(() =>
                semantics.DetermineResponseContent(null!, Response(200)));
            Assert.Throws<HttpProtocolException>(() =>
                semantics.DetermineResponseContent(request, null!));
        });
    }

    [Test]
    public void HopDetectionHandlesEveryFixedNameDuplicatesAndCaseInsensitiveNominations()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpHeaders headers = new HttpHeaders();
        AddField(headers, "connection", "X-One");
        AddField(headers, HttpHeaderNames.Connection, "x-two, keep-alive");
        AddField(headers, "X-One", "1");
        AddField(headers, "x-Two", "2");
        AddField(headers, "Proxy-Connection", "legacy");
        AddField(headers, HttpHeaderNames.KeepAlive, "timeout=5");
        AddField(headers, HttpHeaderNames.TE, "trailers");
        AddField(headers, HttpHeaderNames.TransferEncoding, "chunked");
        AddField(headers, HttpHeaderNames.Upgrade, "websocket");
        AddField(headers, HttpHeaderNames.Trailer, "Digest");
        AddField(headers, "X-End-To-End", "yes");

        HttpHeaders hop = semantics.DetermineHopFields(headers);

        Assert.Multiple(() =>
        {
            Assert.That(hop.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "connection",
                HttpHeaderNames.Connection,
                "X-One",
                "x-Two",
                "Proxy-Connection",
                HttpHeaderNames.KeepAlive,
                HttpHeaderNames.TE,
                HttpHeaderNames.TransferEncoding,
                HttpHeaderNames.Upgrade
            }));
            Assert.That(Values(hop, HttpHeaderNames.Trailer), Is.Empty);
            Assert.That(Values(hop, "X-End-To-End"), Is.Empty);
        });
    }

    [Test]
    public void ForwardingPreservesExpectationStateAndClearsHopScopedUpgradeState()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest request = Request(HttpMethods.Post, "/upload", "example.com");
        AddField(request.Headers, HttpHeaderNames.Expect, "100-continue, custom");
        AddField(request.Headers, HttpHeaderNames.Connection, "Upgrade");
        AddField(request.Headers, HttpHeaderNames.Upgrade, "websocket");
        request.HasContinueExpectation = true;
        request.ShouldSendContinue = true;
        request.HasUnsupportedExpectations = true;
        request.IsUpgradeRequested = true;
        request.UpgradeProtocols.Add("websocket");

        HttpRequest forwarded = semantics.NormalizeForwardedMessage(request);

        Assert.Multiple(() =>
        {
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.Expect),
                Is.EqualTo(new[] { "100-continue, custom" }));
            Assert.That(forwarded.HasContinueExpectation, Is.True);
            Assert.That(forwarded.ShouldSendContinue, Is.True);
            Assert.That(forwarded.HasUnsupportedExpectations, Is.True);
            Assert.That(Values(forwarded.Headers, HttpHeaderNames.Upgrade), Is.Empty);
            Assert.That(forwarded.IsUpgradeRequested, Is.False);
            Assert.That(forwarded.UpgradeProtocols, Is.Empty);
        });
    }

    [Test]
    public void TeTrailersForwardingDependsOnProtocolVersion()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest http10 = Request(HttpMethods.Get, "/", "example.com");
        http10.ProtocolVersion = HttpProtocolVersion.Http10;
        AddField(http10.Headers, HttpHeaderNames.TE, "trailers");
        HttpRequest http11 = Request(HttpMethods.Get, "/", "example.com");
        AddField(http11.Headers, HttpHeaderNames.TE, "gzip, trailers");
        HttpRequest http2 = Request(HttpMethods.Get, "/", "example.com");
        http2.ProtocolVersion = HttpProtocolVersion.Http2;
        AddField(http2.Headers, HttpHeaderNames.TE, "trailers");

        HttpRequest forwarded10 = semantics.NormalizeForwardedMessage(http10);
        HttpRequest forwarded11 = semantics.NormalizeForwardedMessage(http11);
        HttpRequest forwarded2 = semantics.NormalizeForwardedMessage(http2);

        Assert.Multiple(() =>
        {
            Assert.That(Values(forwarded10.Headers, HttpHeaderNames.TE), Is.Empty);
            Assert.That(Values(forwarded10.Headers, HttpHeaderNames.Connection), Is.Empty);
            Assert.That(Values(forwarded11.Headers, HttpHeaderNames.TE),
                Is.EqualTo(new[] { "trailers" }));
            Assert.That(Values(forwarded11.Headers, HttpHeaderNames.Connection),
                Is.EqualTo(new[] { HttpHeaderNames.TE }));
            Assert.That(Values(forwarded2.Headers, HttpHeaderNames.TE),
                Is.EqualTo(new[] { "trailers" }));
            Assert.That(Values(forwarded2.Headers, HttpHeaderNames.Connection), Is.Empty);
        });
    }

    [Test]
    public void NormalizationPopulatesMetadataForOriginAuthorityAsteriskAndAbsoluteTargets()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpRequest origin = Request(HttpMethods.Get, "/path?q=1", "example.com");
        HttpRequest authority = Request(HttpMethods.Connect, "example.com:443", "example.com");
        HttpRequest asterisk = Request(HttpMethods.Options, "*", "example.com");
        HttpRequest absolute = Request(
            HttpMethods.Get,
            "https://target.example:8443/path?",
            "ignored.example");

        HttpRequest forwardedOrigin = semantics.NormalizeForwardedMessage(origin);
        HttpRequest forwardedAuthority = semantics.NormalizeForwardedMessage(authority);
        HttpRequest forwardedAsterisk = semantics.NormalizeForwardedMessage(asterisk);
        HttpRequest forwardedAbsolute = semantics.NormalizeForwardedMessage(absolute);

        Assert.Multiple(() =>
        {
            Assert.That(forwardedOrigin.TargetForm, Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(forwardedOrigin.Path, Is.EqualTo("/path"));
            Assert.That(forwardedOrigin.HasQuery, Is.True);
            Assert.That(forwardedOrigin.Query, Is.EqualTo("q=1"));
            Assert.That(forwardedAuthority.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(forwardedAuthority.Authority, Is.EqualTo("example.com:443"));
            Assert.That(forwardedAuthority.Path, Is.Null);
            Assert.That(forwardedAsterisk.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Asterisk));
            Assert.That(forwardedAsterisk.Path, Is.Null);
            Assert.That(forwardedAbsolute.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Absolute));
            Assert.That(forwardedAbsolute.Scheme, Is.EqualTo("https"));
            Assert.That(forwardedAbsolute.Authority, Is.EqualTo("target.example:8443"));
            Assert.That(forwardedAbsolute.Path, Is.EqualTo("/path"));
            Assert.That(forwardedAbsolute.HasQuery, Is.True);
            Assert.That(forwardedAbsolute.Query, Is.Empty);
            Assert.That(Values(forwardedAbsolute.Headers, HttpHeaderNames.Host),
                Is.EqualTo(new[] { "target.example:8443" }));
            Assert.That(forwardedOrigin.MethodMetadata!.Method, Is.EqualTo(HttpMethods.Get));
        });
    }

    [Test]
    public void PublicOperationsRejectNullOrMalformedIntermediaryInputs()
    {
        IHttpSemantics semantics = new HttpSemantics();
        HttpHeaders malformed = new HttpHeaders();
        malformed.Fields.Add(new HttpHeaderField { Name = "Bad Name" });

        Assert.Multiple(() =>
        {
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineHopFields(null!));
            Assert.Throws<HttpProtocolException>(() => semantics.DetermineHopFields(malformed));
            Assert.Throws<HttpProtocolException>(() => semantics.NormalizeForwardedMessage(null!));
        });
    }

    static HttpRequest Request(string method, string target, string host)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        request.Scheme = "http";
        AddField(request.Headers, HttpHeaderNames.Host, host);
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
        return response;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static string[] Values(HttpHeaders headers, string name)
    {
        return headers.Fields
            .Where(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))
            .Select(field => Encoding.ASCII.GetString(field.Value))
            .ToArray();
    }

    static HttpStatusClass ExpectedStatusClass(int statusCode)
    {
        if (statusCode < 100 || statusCode > 999)
        {
            return HttpStatusClass.Invalid;
        }

        if (statusCode < 200)
        {
            return HttpStatusClass.Informational;
        }

        if (statusCode < 300)
        {
            return HttpStatusClass.Successful;
        }

        if (statusCode < 400)
        {
            return HttpStatusClass.Redirection;
        }

        if (statusCode < 500)
        {
            return HttpStatusClass.ClientError;
        }

        if (statusCode < 600)
        {
            return HttpStatusClass.ServerError;
        }

        return HttpStatusClass.Extension;
    }

    static string[] ReadRegistryLines(string fileName)
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "specs", "iana", fileName);
        return File.ReadLines(Path.GetFullPath(path))
            .Skip(1)
            .Where(line => line.Length > 0)
            .ToArray();
    }
}
