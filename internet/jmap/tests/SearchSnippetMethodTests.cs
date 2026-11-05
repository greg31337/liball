using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class SearchSnippetMethodTests
{
    [Test]
    public void DispatchesFilterAndIdsAndPreservesServerSelectedOrderAndNotFound()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        FakeSearchSnippetService service = new FakeSearchSnippetService();
        service.Response.List.Add(new SearchSnippet
        {
            EmailId = "e2",
            Subject = "The <mark>Foo</mark> &amp; bar report",
            Preview = "...a <mark>foo</mark> result returns &lt;1 item..."
        });
        service.Response.List.Add(new SearchSnippet { EmailId = "e1" });
        service.Response.NotFound = new List<string> { "missing" };
        SearchSnippetGetRequest request = new SearchSnippetGetRequest
        {
            AccountId = "account-1",
            Filter = new EmailFilter { Text = "foo" },
            EmailIds = new List<string> { "e1", "missing", "e2" }
        };

        JmapInvocation invocation = method.ProcessInvocation(
            method.CreateInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        SearchSnippetGetResponse decoded = method.Codec.DecodeSearchSnippetGetResponse(invocation.Arguments);
        SearchSnippetGetResponse mapped = method.MapResponse(
            Response(JmapMethods.SearchSnippetGet, "s1", invocation.Arguments.GetRawText()), "s1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.SearchSnippetGet));
            Assert.That(decoded.List.Select(item => item.EmailId), Is.EqualTo(new[] { "e2", "e1" }));
            Assert.That(decoded.NotFound, Is.EqualTo(new[] { "missing" }));
            Assert.That(mapped.List[0].Subject, Is.EqualTo("The <mark>Foo</mark> &amp; bar report"));
            Assert.That(service.Calls, Is.EqualTo(1));
            Assert.That(service.LastRequest!.Filter!.Text, Is.EqualTo("foo"));
            Assert.That(service.LastRequest.EmailIds, Is.EqualTo(request.EmailIds));
        }
    }

    [Test]
    public void GeneratesEscapedHighlightsFromPositiveSubjectPlainAndHtmlTerms()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        Email email = new Email
        {
            Id = "e1",
            Subject = "Café <foo> & report",
            BodyStructure = new EmailBodyPart
            {
                Type = "multipart/alternative",
                SubParts = new List<EmailBodyPart>
                {
                    new EmailBodyPart { PartId = "plain", Type = "text/plain" },
                    new EmailBodyPart { PartId = "html", Type = "text/html" },
                    new EmailBodyPart { PartId = "binary", Type = "application/octet-stream" }
                }
            },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["plain"] = new EmailBodyValue { Value = "Plain café <unsafe> text" },
                ["html"] = new EmailBodyValue
                {
                    Value = "<p title='Café title'>Visible &lt;thing&gt; FOO" +
                        "<script>secret foo</script></p>"
                },
                ["binary"] = new EmailBodyValue { Value = "foo binary secret" }
            }
        };
        EmailFilter filter = new EmailFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.And,
            Conditions = new List<EmailFilter>
            {
                new EmailFilter { Subject = "café" },
                new EmailFilter { Text = "foo" },
                new EmailFilter { Body = "café unsafe" },
                new EmailFilter
                {
                    HasOperator = true,
                    Operator = JmapFilterOperator.Not,
                    Conditions = new List<EmailFilter> { new EmailFilter { Text = "secret" } }
                }
            }
        };

        SearchSnippet snippet = method.CreateSnippet(email, filter);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(snippet.Subject,
                Is.EqualTo("<mark>Café</mark> &lt;<mark>foo</mark>&gt; &amp; report"));
            Assert.That(snippet.Preview, Does.Contain("<mark>café</mark> &lt;<mark>unsafe</mark>&gt;"));
            Assert.That(snippet.Preview, Does.Contain("Visible &lt;thing&gt; <mark>FOO</mark>"));
            Assert.That(snippet.Preview, Does.Not.Contain("script"));
            Assert.That(snippet.Preview, Does.Not.Contain("secret"));
            Assert.That(snippet.Preview, Does.Not.Contain("binary"));
            Assert.That(method.HasSafeHighlightedMarkup(snippet.Subject), Is.True);
            Assert.That(method.HasSafeHighlightedMarkup(snippet.Preview), Is.True);
        }
    }

    [Test]
    public void HighlightsQuotedUnicodePhrasesFromSubjectHeadersAndPlainOrHtmlBodies()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        Email email = new Email
        {
            Id = "e1",
            Subject = "Weekly Cafe\u0301 東京 status",
            Headers = new List<EmailHeader>
            {
                new EmailHeader { Name = "Subject", Value = "Weekly =?UTF-8?Q?Caf=C3=A9?= 東京 status" }
            },
            BodyStructure = new EmailBodyPart
            {
                Type = "multipart/alternative",
                SubParts = new List<EmailBodyPart>
                {
                    new EmailBodyPart { PartId = "plain", Type = "text/plain" },
                    new EmailBodyPart { PartId = "html", Type = "text/html" }
                }
            },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["plain"] = new EmailBodyValue { Value = "The QUICK\n brown launch is ready." },
                ["html"] = new EmailBodyValue
                {
                    Value = "<p>Re\u0301sume\u0301 &amp; tea</p><script>quick brown secret</script>"
                }
            }
        };
        EmailFilter filter = new EmailFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.And,
            Conditions = new List<EmailFilter>
            {
                new EmailFilter { Header = new List<string> { "subject", "\"CAFÉ 東京\"" } },
                new EmailFilter { Body = "\"quick brown\" \"RÉSUMÉ & tea\"" }
            }
        };

        SearchSnippet snippet = method.CreateSnippet(email, filter);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(snippet.Subject, Is.EqualTo("Weekly <mark>Café 東京</mark> status"));
            Assert.That(snippet.Preview, Does.Contain("<mark>QUICK brown</mark> launch"));
            Assert.That(snippet.Preview, Does.Contain("<mark>Résumé &amp; tea</mark>"));
            Assert.That(snippet.Preview, Does.Not.Contain("script"));
            Assert.That(snippet.Preview, Does.Not.Contain("secret"));
        }
    }

    [Test]
    public void LeavesFieldsNullForPhraseAndNonDisplayHeaderNonmatches()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        Email email = new Email
        {
            Id = "e1",
            Subject = "Café 東京 update",
            Headers = new List<EmailHeader>
            {
                new EmailHeader { Name = "X-Trace", Value = "quick brown" }
            },
            BodyStructure = new EmailBodyPart { PartId = "plain", Type = "text/plain" },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["plain"] = new EmailBodyValue { Value = "The quick brown launch" }
            }
        };

        SearchSnippet reversedPhrases = method.CreateSnippet(email, new EmailFilter
        {
            HasOperator = true,
            Operator = JmapFilterOperator.And,
            Conditions = new List<EmailFilter>
            {
                new EmailFilter { Subject = "\"東京 café\"" },
                new EmailFilter { Body = "\"brown quick\"" }
            }
        });
        SearchSnippet headerOnly = method.CreateSnippet(email, new EmailFilter
        {
            Header = new List<string> { "X-Trace", "quick brown" }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reversedPhrases.Subject, Is.Null);
            Assert.That(reversedPhrases.Preview, Is.Null);
            Assert.That(headerOnly.Subject, Is.Null);
            Assert.That(headerOnly.Preview, Is.Null);
        }
    }

    [Test]
    public void AcceptsNullNotFoundWhenEveryRequestedEmailHasAResult()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        FakeSearchSnippetService service = new FakeSearchSnippetService();
        service.Response.List.Add(new SearchSnippet { EmailId = "e2", Preview = "<mark>two</mark>" });
        service.Response.List.Add(new SearchSnippet { EmailId = "e1", Subject = "<mark>one</mark>" });
        SearchSnippetGetRequest request = new SearchSnippetGetRequest
        {
            AccountId = "account-1",
            Filter = new EmailFilter { Text = "one two" },
            EmailIds = new List<string> { "e1", "e2" }
        };

        SearchSnippetGetResponse response = method.Codec.DecodeSearchSnippetGetResponse(
            Invoke(method, service, request).Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.List.Select(item => item.EmailId), Is.EqualTo(new[] { "e2", "e1" }));
            Assert.That(response.NotFound, Is.Null);
        }
    }

    [Test]
    public void BuildsUtf8BoundedPreviewAroundTheFirstMatchWithoutSplittingUnicode()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        Email email = new Email
        {
            Id = "e1",
            BodyStructure = new EmailBodyPart { PartId = "plain", Type = "text/plain" },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["plain"] = new EmailBodyValue
                {
                    Value = new string('x', 300) + "🙂 needle < & > café " + new string('y', 300)
                }
            }
        };

        SearchSnippet snippet = method.CreateSnippet(email, new EmailFilter { Body = "needle café" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(snippet.Preview, Is.Not.Null);
            Assert.That(Encoding.UTF8.GetByteCount(snippet.Preview!),
                Is.LessThanOrEqualTo(JmapProtocolLimits.MaximumSearchSnippetPreviewSize));
            Assert.That(snippet.Preview, Does.StartWith("..."));
            Assert.That(snippet.Preview, Does.EndWith("..."));
            Assert.That(snippet.Preview, Does.Contain("<mark>needle</mark>"));
            Assert.That(snippet.Preview, Does.Contain("<mark>café</mark>"));
            Assert.That(snippet.Preview, Does.Contain("&lt; &amp; &gt;"));
            Assert.That(snippet.Preview, Does.Contain("🙂"));
        }
    }

    [Test]
    public void RejectsMalformedUnsupportedOversizedAndInaccessibleRequestsBeforeServiceAccess()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        FakeSearchSnippetService service = new FakeSearchSnippetService();
        JmapInvocation malformed = method.ProcessInvocation(
            RawInvocation("s1", "{\"accountId\":\"account-1\",\"emailIds\":\"e1\"}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation unsupported = method.ProcessInvocation(
            RawInvocation("s2", "{\"accountId\":\"account-1\",\"filter\":{\"vendorText\":\"x\"},\"emailIds\":[]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation oversized = method.ProcessInvocation(
            RawInvocation("s3", "{\"accountId\":\"account-1\",\"emailIds\":[\"e1\",\"e2\"]}"),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);
        JmapInvocation inaccessible = method.ProcessInvocation(
            RawInvocation("s4", "{\"accountId\":\"account-1\",\"emailIds\":[]}"),
            Session(false), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, malformed).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, unsupported).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(Error(method, oversized).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, inaccessible).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(service.Calls, Is.Zero);
        }
    }

    [Test]
    public void RejectsOverlappingIncompleteUnsafeAndOversizedBackendResults()
    {
        SearchSnippetMethod method = new SearchSnippetMethod();
        SearchSnippetGetRequest request = new SearchSnippetGetRequest
        {
            AccountId = "account-1",
            Filter = new EmailFilter { Text = "x" },
            EmailIds = new List<string> { "e1" }
        };
        FakeSearchSnippetService overlap = ServiceWithSnippet("<mark>x</mark>", null);
        overlap.Response.NotFound = new List<string> { "e1" };
        FakeSearchSnippetService incomplete = new FakeSearchSnippetService();
        incomplete.Response.NotFound = new List<string> { "missing" };
        FakeSearchSnippetService unsafeMarkup = ServiceWithSnippet("<mark>x</mark><script>bad</script>", null);
        FakeSearchSnippetService oversized = ServiceWithSnippet(null,
            "<mark>x</mark>" + new string('z', JmapProtocolLimits.MaximumSearchSnippetPreviewSize));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => Invoke(method, overlap, request), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Invoke(method, incomplete, request), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Invoke(method, unsafeMarkup, request), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Invoke(method, oversized, request), Throws.TypeOf<JmapProtocolException>());
        }
    }

    private static JmapInvocation Invoke(
        SearchSnippetMethod method,
        FakeSearchSnippetService service,
        SearchSnippetGetRequest request)
    {
        return method.ProcessInvocation(method.CreateInvocation(request, "s1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
    }

    private static FakeSearchSnippetService ServiceWithSnippet(string? subject, string? preview)
    {
        FakeSearchSnippetService service = new FakeSearchSnippetService();
        service.Response.List.Add(new SearchSnippet { EmailId = "e1", Subject = subject, Preview = preview });
        return service;
    }

    private static JmapSession Session(bool includeAccount)
    {
        JmapSession session = new JmapSession();
        if (includeAccount)
        {
            JmapAccount account = new JmapAccount();
            JmapCapability mail = new JmapCapability();
            mail.Mail = new JmapMailCapabilities();
            account.AccountCapabilities.Add(JmapCapabilities.Mail, mail);
            session.Accounts.Add("account-1", account);
        }

        return session;
    }

    private static JmapServerOptions Options(int maximumObjectsInGet)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxObjectsInGet = maximumObjectsInGet;
        return options;
    }

    private static JmapInvocation RawInvocation(string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = JmapMethods.SearchSnippetGet;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        return invocation;
    }

    private static JmapResponse Response(string name, string callId, string arguments)
    {
        JmapResponse response = new JmapResponse();
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        response.MethodResponses.Add(invocation);
        return response;
    }

    private static JmapMethodError Error(SearchSnippetMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeSearchSnippetService : ISearchSnippetService
    {
        public SearchSnippetGetResponse Response = new SearchSnippetGetResponse { AccountId = "account-1" };
        public int Calls;
        public SearchSnippetGetRequest? LastRequest;

        public Task<SearchSnippetGetResponse> GetStoredSearchSnippetsAsync(
            SearchSnippetGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            Calls = Calls + 1;
            LastRequest = request;
            return Task.FromResult(Response);
        }
    }
}
