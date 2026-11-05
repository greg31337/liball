using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class EmailMethodTests
{
    [Test]
    public void GetAppliesEmailAndBodyDefaultsWithoutFetchingBodyValues()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.GetResponse.List.Add(StoredEmail("e1"));
        EmailGetRequest request = new EmailGetRequest { AccountId = "account-1" };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
        JsonElement textPart = item.GetProperty(JmapProperties.TextBody)[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                item.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(method.DefaultEmailProperties()));
            Assert.That(item.TryGetProperty(JmapProperties.Headers, out JsonElement unusedHeader), Is.False);
            Assert.That(item.TryGetProperty(JmapProperties.BodyStructure, out JsonElement unusedStructure), Is.False);
            Assert.That(item.GetProperty(JmapProperties.BodyValues).EnumerateObject(), Is.Empty);
            Assert.That(
                textPart.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(method.DefaultBodyProperties()));
            Assert.That(service.LastGetRequest!.FetchTextBodyValues, Is.False);
            Assert.That(service.LastGetRequest.FetchHtmlBodyValues, Is.False);
            Assert.That(service.LastGetRequest.FetchAllBodyValues, Is.False);
            Assert.That(service.LastGetRequest.Properties, Does.Not.Contain(JmapProperties.Headers));
            Assert.That(service.LastGetRequest.Properties, Does.Not.Contain(JmapProperties.BodyStructure));
        }
    }

    [Test]
    public void GetProjectsDynamicHeadersAndTruncatesSelectedHtmlBodyValues()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        Email stored = StoredEmail("e1");
        stored.Headers!.Add(Header("List-Post", "<https://example.test/list>"));
        stored.HtmlBody![0].Headers!.Add(Header("X-Trace", " trace value"));
        stored.BodyValues!["2"].Value = "<p>é🙂tail</p>";
        service.GetResponse.List.Add(stored);
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string>
            {
                JmapProperties.Subject,
                "header:List-Post:asURLs",
                JmapProperties.HtmlBody,
                JmapProperties.BodyValues
            },
            BodyProperties = new List<string>
            {
                JmapProperties.PartId,
                JmapProperties.Type,
                "header:X-Trace:asText"
            },
            FetchHtmlBodyValues = true,
            HasFetchHtmlBodyValues = true,
            MaxBodyValueBytes = 8,
            HasMaxBodyValueBytes = true
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
        JsonElement part = item.GetProperty(JmapProperties.HtmlBody)[0];
        JsonElement value = item.GetProperty(JmapProperties.BodyValues).GetProperty("2");
        EmailGetResponse mapped = method.MapGetResponse(
            Response(JmapMethods.EmailGet, "g1", invocation.Arguments.GetRawText()), "g1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                item.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[]
                {
                    JmapProperties.Id, JmapProperties.Subject, "header:List-Post:asURLs",
                    JmapProperties.HtmlBody, JmapProperties.BodyValues
                }));
            Assert.That(item.GetProperty("header:List-Post:asURLs")[0].GetString(),
                Is.EqualTo("https://example.test/list"));
            Assert.That(
                part.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { JmapProperties.PartId, JmapProperties.Type, "header:X-Trace:asText" }));
            Assert.That(part.GetProperty("header:X-Trace:asText").GetString(), Is.EqualTo("trace value"));
            Assert.That(value.GetProperty(JmapProperties.Value).GetString(), Is.EqualTo("<p>é"));
            Assert.That(value.GetProperty(JmapProperties.IsTruncated).GetBoolean(), Is.True);
            Assert.That(mapped.List[0].HeaderProperties.ContainsKey("header:List-Post:asURLs"), Is.True);
            Assert.That(service.LastGetRequest!.Properties, Does.Contain(JmapProperties.Headers));
            Assert.That(service.LastGetRequest.Properties, Does.Contain(JmapProperties.HtmlBody));
            Assert.That(service.LastGetRequest.Properties, Does.Contain(JmapProperties.BodyValues));
            Assert.That(service.LastGetRequest.BodyProperties, Does.Contain(JmapProperties.Headers));
            Assert.That(service.LastGetRequest.BodyProperties, Does.Contain(JmapProperties.SubParts));
            Assert.That(service.LastGetRequest.FetchHtmlBodyValues, Is.True);
            Assert.That(service.LastGetRequest.MaxBodyValueBytes, Is.EqualTo(8));
        }
    }

    [Test]
    public void BodyValueFetchFlagsFormAUnionAndFetchAllTakesPrecedence()
    {
        EmailMethod method = new EmailMethod();
        Email email = StoredEmail("e1");
        EmailBodyPart attachment = TextPart("3", "text/csv");
        email.BodyStructure!.SubParts!.Add(attachment);
        email.BodyValues!.Add("3", BodyValue("values"));
        EmailGetRequest selectedViews = new EmailGetRequest
        {
            FetchTextBodyValues = true,
            FetchHtmlBodyValues = true
        };
        EmailGetRequest allParts = new EmailGetRequest
        {
            FetchTextBodyValues = true,
            FetchAllBodyValues = true
        };

        Dictionary<string, string> selected = method.SelectBodyValueTypes(email, selectedViews);
        Dictionary<string, string> all = method.SelectBodyValueTypes(email, allParts);
        allParts.AccountId = "account-1";
        allParts.Properties = new List<string> { JmapProperties.BodyValues };
        EmailGetRequest backend = method.CompleteGetRequest(allParts);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selected.Keys, Is.EquivalentTo(new[] { "1", "2" }));
            Assert.That(all.Keys, Is.EquivalentTo(new[] { "1", "2", "3" }));
            Assert.That(backend.FetchAllBodyValues, Is.True);
            Assert.That(backend.FetchTextBodyValues, Is.False);
            Assert.That(backend.FetchHtmlBodyValues, Is.False);
            Assert.That(backend.Properties, Does.Contain(JmapProperties.BodyStructure));
            Assert.That(backend.Properties, Does.Not.Contain(JmapProperties.TextBody));
        }
    }

    [Test]
    public void EveryEmailPropertyCanBeProjectedIndividually()
    {
        string[] properties =
        {
            JmapProperties.Id, JmapProperties.BlobId, JmapProperties.ThreadId,
            JmapProperties.MailboxIds, JmapProperties.Keywords, JmapProperties.Size,
            JmapProperties.ReceivedAt, JmapProperties.Headers, JmapProperties.MessageId,
            JmapProperties.InReplyTo, JmapProperties.References, JmapProperties.Sender,
            JmapProperties.From, JmapProperties.To, JmapProperties.Cc, JmapProperties.Bcc,
            JmapProperties.ReplyTo, JmapProperties.Subject, JmapProperties.SentAt,
            JmapProperties.BodyStructure, JmapProperties.BodyValues, JmapProperties.TextBody,
            JmapProperties.HtmlBody, JmapProperties.Attachments,
            JmapProperties.HasAttachment, JmapProperties.Preview
        };
        EmailMethod method = new EmailMethod();

        foreach (string property in properties)
        {
            FakeEmailService service = new FakeEmailService();
            service.GetResponse.List.Add(ProjectionEmail());
            EmailGetRequest request = new EmailGetRequest
            {
                AccountId = "account-1",
                Properties = new List<string> { property }
            };
            JmapInvocation invocation = method.ProcessGetInvocation(
                method.CreateGetInvocation(request, "g1"), Session(true), service,
                new JmapRequestContext(), Options(10), CancellationToken.None);
            JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
            EmailGetResponse mapped = method.MapGetResponse(
                Response(JmapMethods.EmailGet, "g1", invocation.Arguments.GetRawText()),
                "g1", request);

            string[] expected = property == JmapProperties.Id ?
                new[] { JmapProperties.Id } : new[] { JmapProperties.Id, property };
            Assert.That(item.EnumerateObject().Select(member => member.Name),
                Is.EqualTo(expected), property);
            Assert.That(mapped.List, Has.Count.EqualTo(1), property);
        }
    }

    [Test]
    public void EveryBodyPropertyAndDynamicPartHeaderCanBeProjectedIndividually()
    {
        string[] properties =
        {
            JmapProperties.PartId, JmapProperties.BlobId, JmapProperties.Size,
            JmapProperties.Headers, JmapProperties.Name, JmapProperties.Type,
            JmapProperties.Charset, JmapProperties.Disposition, JmapProperties.Cid,
            JmapProperties.Language, JmapProperties.Location, JmapProperties.SubParts
        };
        EmailMethod method = new EmailMethod();

        foreach (string property in properties)
        {
            FakeEmailService service = new FakeEmailService();
            service.GetResponse.List.Add(ProjectionEmail());
            EmailGetRequest request = new EmailGetRequest
            {
                AccountId = "account-1",
                Properties = new List<string> { JmapProperties.BodyStructure },
                BodyProperties = new List<string> { property }
            };
            JmapInvocation invocation = method.ProcessGetInvocation(
                method.CreateGetInvocation(request, "g1"), Session(true), service,
                new JmapRequestContext(), Options(10), CancellationToken.None);
            JsonElement root = invocation.Arguments.GetProperty(JmapProperties.List)[0]
                .GetProperty(JmapProperties.BodyStructure);
            JsonElement leaf = root.GetProperty(JmapProperties.SubParts)[0];

            Assert.That(root.TryGetProperty(property, out JsonElement unusedRoot),
                Is.True, property);
            Assert.That(leaf.TryGetProperty(property, out JsonElement unusedLeaf),
                Is.True, property);
            Assert.That(() => method.MapGetResponse(
                Response(JmapMethods.EmailGet, "g1", invocation.Arguments.GetRawText()),
                "g1", request), Throws.Nothing, property);
        }

        FakeEmailService dynamicService = new FakeEmailService();
        dynamicService.GetResponse.List.Add(ProjectionEmail());
        string dynamicProperty = "header:X-Part:asText:all";
        EmailGetRequest dynamicRequest = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { JmapProperties.BodyStructure },
            BodyProperties = new List<string> { dynamicProperty }
        };
        JmapInvocation dynamicInvocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(dynamicRequest, "g2"), Session(true), dynamicService,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement selected = dynamicInvocation.Arguments.GetProperty(JmapProperties.List)[0]
            .GetProperty(JmapProperties.BodyStructure).GetProperty(dynamicProperty);
        EmailGetResponse dynamicMapped = method.MapGetResponse(
            Response(JmapMethods.EmailGet, "g2", dynamicInvocation.Arguments.GetRawText()),
            "g2", dynamicRequest);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selected.EnumerateArray().Select(value => value.GetString()),
                Is.EqualTo(new[] { "first part value", "second part value" }));
            Assert.That(dynamicMapped.List[0].BodyStructure!.HeaderProperties
                .ContainsKey(dynamicProperty), Is.True);
            Assert.That(dynamicService.LastGetRequest!.BodyProperties,
                Does.Contain(JmapProperties.Headers));
        }
    }

    [Test]
    public void DynamicHeaderFormsWithAllPreserveEveryOccurrenceAndValueShape()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.GetResponse.List.Add(ProjectionEmail());
        string[] properties =
        {
            "header:X-Raw:asRaw:all",
            "header:Subject:asText:all",
            "header:To:asAddresses:all",
            "header:From:asGroupedAddresses:all",
            "header:References:asMessageIds:all",
            "header:Date:asDate:all",
            "header:List-Help:asURLs:all"
        };
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string>(properties)
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];
        EmailGetResponse mapped = method.MapGetResponse(
            Response(JmapMethods.EmailGet, "g1", invocation.Arguments.GetRawText()),
            "g1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.GetProperty(properties[0]).EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { " raw one", " raw two" }));
            Assert.That(item.GetProperty(properties[1]).EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { "First subject", "日本語" }));
            Assert.That(item.GetProperty(properties[2])[0][0].GetProperty(JmapProperties.Email)
                .GetString(), Is.EqualTo("one@example.test"));
            Assert.That(item.GetProperty(properties[2])[1][0].GetProperty(JmapProperties.Email)
                .GetString(), Is.EqualTo("two@example.test"));
            Assert.That(item.GetProperty(properties[3])[0][0].GetProperty(JmapProperties.Name)
                .GetString(), Is.EqualTo("First Team"));
            Assert.That(item.GetProperty(properties[3])[1][0].GetProperty(JmapProperties.Name)
                .GetString(), Is.EqualTo("Second Team"));
            Assert.That(item.GetProperty(properties[4])[0].EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { "root@example.test" }));
            Assert.That(item.GetProperty(properties[4])[1].EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[] { "next@example.test" }));
            Assert.That(item.GetProperty(properties[5]).EnumerateArray()
                .Select(value => value.GetString()), Is.EqualTo(new[]
                {
                    "2026-09-11T14:05:06-07:00", "2026-09-12T10:00:00+00:00"
                }));
            Assert.That(item.GetProperty(properties[6])[0][0].GetString(),
                Is.EqualTo("https://example.test/first"));
            Assert.That(item.GetProperty(properties[6])[1][0].GetString(),
                Is.EqualTo("mailto:help@example.test"));
            Assert.That(mapped.List[0].HeaderProperties.Keys, Is.EquivalentTo(properties));
            Assert.That(service.LastGetRequest!.Properties,
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Headers }));
        }
    }

    [TestCase(false, false, false, "")]
    [TestCase(true, false, false, "1")]
    [TestCase(false, true, false, "2")]
    [TestCase(true, true, false, "1,2")]
    [TestCase(false, false, true, "1,2,3")]
    [TestCase(true, false, true, "1,2,3")]
    [TestCase(false, true, true, "1,2,3")]
    [TestCase(true, true, true, "1,2,3")]
    public void EveryBodyFetchFlagCombinationSelectsTheSpecifiedUnion(
        bool fetchText,
        bool fetchHtml,
        bool fetchAll,
        string expectedPartIds)
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.GetResponse.List.Add(ProjectionEmail());
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { JmapProperties.BodyValues },
            FetchTextBodyValues = fetchText,
            HasFetchTextBodyValues = true,
            FetchHtmlBodyValues = fetchHtml,
            HasFetchHtmlBodyValues = true,
            FetchAllBodyValues = fetchAll,
            HasFetchAllBodyValues = true
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement values = invocation.Arguments.GetProperty(JmapProperties.List)[0]
            .GetProperty(JmapProperties.BodyValues);
        string[] expected = expectedPartIds.Length == 0 ?
            Array.Empty<string>() : expectedPartIds.Split(',');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(values.EnumerateObject().Select(value => value.Name),
                Is.EquivalentTo(expected));
            Assert.That(service.LastGetRequest!.FetchTextBodyValues,
                Is.EqualTo(fetchText && !fetchAll));
            Assert.That(service.LastGetRequest.FetchHtmlBodyValues,
                Is.EqualTo(fetchHtml && !fetchAll));
            Assert.That(service.LastGetRequest.FetchAllBodyValues, Is.EqualTo(fetchAll));
        }
    }

    [TestCase(0, "Aé🙂Z", false)]
    [TestCase(1, "A", true)]
    [TestCase(2, "A", true)]
    [TestCase(3, "Aé", true)]
    [TestCase(4, "Aé", true)]
    [TestCase(6, "Aé", true)]
    [TestCase(7, "Aé🙂", true)]
    [TestCase(8, "Aé🙂Z", false)]
    public void BodyValueByteLimitTruncatesOnlyAtUtf8CharacterBoundaries(
        long maximumBytes,
        string expected,
        bool truncated)
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        Email email = ProjectionEmail();
        email.BodyValues!["1"].Value = "Aé🙂Z";
        service.GetResponse.List.Add(email);
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { JmapProperties.BodyValues },
            FetchTextBodyValues = true,
            HasFetchTextBodyValues = true,
            MaxBodyValueBytes = maximumBytes,
            HasMaxBodyValueBytes = true
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement value = invocation.Arguments.GetProperty(JmapProperties.List)[0]
            .GetProperty(JmapProperties.BodyValues).GetProperty("1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.GetProperty(JmapProperties.Value).GetString(), Is.EqualTo(expected));
            Assert.That(value.GetProperty(JmapProperties.IsTruncated).GetBoolean(),
                Is.EqualTo(truncated));
            Assert.That(Encoding.UTF8.GetByteCount(expected),
                Is.LessThanOrEqualTo(maximumBytes == 0 ? long.MaxValue : maximumBytes));
        }
    }

    [Test]
    public void ExplicitMetadataProjectionProducesAMetadataOnlyBackendRead()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.GetResponse.List.Add(new Email
        {
            Id = "e1",
            Keywords = new Dictionary<string, bool> { ["$seen"] = true },
            Subject = "Projected subject"
        });
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string>
            {
                JmapProperties.Id, JmapProperties.Keywords, JmapProperties.Subject, JmapProperties.Subject
            }
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement item = invocation.Arguments.GetProperty(JmapProperties.List)[0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                service.LastGetRequest!.Properties,
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Keywords, JmapProperties.Subject }));
            Assert.That(service.LastGetRequest.BodyProperties, Is.Null);
            Assert.That(service.LastGetRequest.HasFetchTextBodyValues, Is.False);
            Assert.That(service.LastGetRequest.HasFetchHtmlBodyValues, Is.False);
            Assert.That(service.LastGetRequest.HasFetchAllBodyValues, Is.False);
            Assert.That(
                item.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { JmapProperties.Id, JmapProperties.Keywords, JmapProperties.Subject }));
        }
    }

    [Test]
    public void GetReturnsRequestedOrderAndClassifiesMissingIds()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.GetResponse.List.Add(new Email { Id = "e2" });
        service.GetResponse.List.Add(new Email { Id = "e1" });
        service.GetResponse.NotFound.Add("missing");
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Ids = new List<string> { "e1", "missing", "e2", "e1" },
            Properties = new List<string> { JmapProperties.Id }
        };

        JmapInvocation invocation = method.ProcessGetInvocation(
            method.CreateGetInvocation(request, "g1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailGetResponse response = method.Codec.DecodeEmailGetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.List.Select(email => email.Id), Is.EqualTo(new[] { "e1", "e2" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "missing" }));
        }
    }

    [Test]
    public void GetRejectsInvalidProjectionOptionsAccountsAndLimits()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        JmapInvocation invalidEmailProperty = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailGet, "g1",
                "{\"accountId\":\"account-1\",\"properties\":[\"unknown\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation invalidHeaderForm = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailGet, "g2",
                "{\"accountId\":\"account-1\",\"properties\":[\"header:From:asDate\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation invalidBodyProperty = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailGet, "g3",
                "{\"accountId\":\"account-1\",\"bodyProperties\":[\"preview\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation negativeLimit = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailGet, "g4",
                "{\"accountId\":\"account-1\",\"maxBodyValueBytes\":-1}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation tooManyIds = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailGet, "g5",
                "{\"accountId\":\"account-1\",\"ids\":[\"e1\",\"e2\"]}"),
            Session(true), service, new JmapRequestContext(), Options(1), CancellationToken.None);
        JmapInvocation missingAccount = method.ProcessGetInvocation(
            RawInvocation(JmapMethods.EmailGet, "g6", "{\"accountId\":\"account-1\"}"),
            Session(false), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalidEmailProperty).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, invalidHeaderForm).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, invalidBodyProperty).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, negativeLimit).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, tooManyIds).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(Error(method, missingAccount).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(service.GetCalls, Is.Zero);
        }
    }

    [Test]
    public void ClientMappingRejectsMissingAndUnrequestedProjectedProperties()
    {
        EmailMethod method = new EmailMethod();
        EmailGetRequest request = new EmailGetRequest
        {
            AccountId = "account-1",
            Properties = new List<string> { JmapProperties.Subject }
        };
        JmapResponse missing = Response(
            JmapMethods.EmailGet, "g1",
            "{\"accountId\":\"account-1\",\"state\":\"s\",\"list\":[{\"id\":\"e1\"}],\"notFound\":[]}");
        JmapResponse extra = Response(
            JmapMethods.EmailGet, "g2",
            "{\"accountId\":\"account-1\",\"state\":\"s\",\"list\":[{\"id\":\"e1\",\"subject\":\"Hi\",\"size\":1}],\"notFound\":[]}");

        JmapProtocolException missingError = Assert.Throws<JmapProtocolException>(
            () => method.MapGetResponse(missing, "g1", request))!;
        JmapProtocolException extraError = Assert.Throws<JmapProtocolException>(
            () => method.MapGetResponse(extra, "g2", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingError.Path, Is.EqualTo("list/0/subject"));
            Assert.That(extraError.Path, Is.EqualTo("list/0/size"));
        }
    }

    [Test]
    public void ChangesPreservesEveryEmailMutationClassification()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.ChangesResponse.NewState = "partial-state";
        service.ChangesResponse.HasMoreChanges = true;
        service.ChangesResponse.Created.Add("created-email");
        service.ChangesResponse.Updated.Add("metadata-update");
        service.ChangesResponse.Updated.Add("body-update");
        service.ChangesResponse.Updated.Add("mailbox-update");
        service.ChangesResponse.Updated.Add("keyword-update");
        service.ChangesResponse.Updated.Add("thread-impact");
        service.ChangesResponse.Destroyed.Add("destroyed-email");
        EmailChangesRequest request = new EmailChangesRequest
        {
            AccountId = "account-1",
            SinceState = "old-state",
            MaxChanges = 7,
            HasMaxChanges = true
        };

        JmapInvocation invocation = method.ProcessChangesInvocation(
            method.CreateChangesInvocation(request, "c1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailChangesResponse response = method.Codec.DecodeEmailChangesResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.EmailChanges));
            Assert.That(response.OldState, Is.EqualTo("old-state"));
            Assert.That(response.NewState, Is.EqualTo("partial-state"));
            Assert.That(response.HasMoreChanges, Is.True);
            Assert.That(response.Created, Is.EqualTo(new[] { "created-email" }));
            Assert.That(response.Updated, Is.EqualTo(new[]
            {
                "metadata-update", "body-update", "mailbox-update", "keyword-update", "thread-impact"
            }));
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "destroyed-email" }));
            Assert.That(service.ChangesCalls, Is.EqualTo(1));
            Assert.That(service.LastChangesRequest!.SinceState, Is.EqualTo("old-state"));
            Assert.That(service.LastChangesRequest.MaxChanges, Is.EqualTo(7));
            Assert.That(service.LastChangesRequest.HasMaxChanges, Is.True);
        }
    }

    [Test]
    public void ChangesRejectsInvalidArgumentsAccountsAndBackendClassifications()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService invalidService = new FakeEmailService();
        JmapInvocation invalidMaximum = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.EmailChanges, "c1",
                "{\"accountId\":\"account-1\",\"sinceState\":\"old-state\",\"maxChanges\":0}"),
            Session(true), invalidService, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation missingAccount = method.ProcessChangesInvocation(
            RawInvocation(JmapMethods.EmailChanges, "c2",
                "{\"accountId\":\"account-1\",\"sinceState\":\"old-state\"}"),
            Session(false), invalidService, new JmapRequestContext(), Options(10), CancellationToken.None);

        FakeEmailService wrongStateService = new FakeEmailService();
        wrongStateService.ChangesResponse.OldState = "different-state";
        JmapProtocolException wrongState = Assert.Throws<JmapProtocolException>(
            () => method.ProcessChangesInvocation(
                method.CreateChangesInvocation(ChangesRequest(), "c3"), Session(true), wrongStateService,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        FakeEmailService duplicateService = new FakeEmailService();
        duplicateService.ChangesResponse.Created.Add("e1");
        duplicateService.ChangesResponse.Updated.Add("e1");
        JmapProtocolException duplicate = Assert.Throws<JmapProtocolException>(
            () => method.ProcessChangesInvocation(
                method.CreateChangesInvocation(ChangesRequest(), "c4"), Session(true), duplicateService,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        FakeEmailService oversizedService = new FakeEmailService();
        oversizedService.ChangesResponse.Created.Add("e1");
        oversizedService.ChangesResponse.Destroyed.Add("e2");
        EmailChangesRequest limited = ChangesRequest();
        limited.MaxChanges = 1;
        limited.HasMaxChanges = true;
        JmapProtocolException oversized = Assert.Throws<JmapProtocolException>(
            () => method.ProcessChangesInvocation(
                method.CreateChangesInvocation(limited, "c5"), Session(true), oversizedService,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalidMaximum).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, missingAccount).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(invalidService.ChangesCalls, Is.Zero);
            Assert.That(wrongState.Path, Is.EqualTo("methodResponses/1"));
            Assert.That(duplicate.Path, Is.EqualTo(JmapProperties.Updated));
            Assert.That(oversized.Path, Is.EqualTo("methodResponses/1"));
        }
    }

    [Test]
    public void SetCreatesMimeDraftsAppliesPatchesAndDestroysEmails()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.SetResponse.Created = new Dictionary<string, Email>
        {
            ["draft-1"] = new Email { Id = "email-new", BlobId = "raw-blob", ThreadId = "thread-new", Size = 321 }
        };
        service.SetResponse.Updated = new Dictionary<string, JsonElement> { ["email-old"] = Json("null") };
        service.SetResponse.Destroyed = new List<string> { "email-delete" };
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            IfInState = "email-state",
            Create = new Dictionary<string, Email> { ["draft-1"] = DraftEmail() },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["email-old"] = Patch(
                    ("mailboxIds/archive", "true"),
                    ("mailboxIds/inbox", "null"),
                    ("keywords/$seen", "true"))
            },
            Destroy = new List<string> { "email-delete" }
        };

        JmapInvocation encoded = method.CreateSetInvocation(request, "s1");
        JmapInvocation invocation = method.ProcessSetInvocation(
            encoded, Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSetResponse response = method.Codec.DecodeEmailSetResponse(invocation.Arguments);
        EmailSetResponse mapped = method.MapSetResponse(
            Response(JmapMethods.EmailSet, "s1", invocation.Arguments.GetRawText()), "s1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encoded.Arguments.GetProperty("create").GetProperty("draft-1")
                .GetProperty("textBody")[0].GetProperty("partId").GetString(), Is.EqualTo("text"));
            Assert.That(encoded.Arguments.GetProperty("create").GetProperty("draft-1")
                .GetProperty("attachments")[0].GetProperty("blobId").GetString(), Is.EqualTo("upload-blob"));
            Assert.That(response.OldState, Is.EqualTo("email-state"));
            Assert.That(response.NewState, Is.EqualTo("email-state-2"));
            Assert.That(response.Created!["draft-1"].BlobId, Is.EqualTo("raw-blob"));
            Assert.That(invocation.Arguments.GetProperty("created").GetProperty("draft-1")
                .EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { "id", "blobId", "threadId", "size" }));
            Assert.That(response.Updated!.ContainsKey("email-old"), Is.True);
            Assert.That(response.Destroyed, Is.EqualTo(new[] { "email-delete" }));
            Assert.That(mapped.Created!["draft-1"].ThreadId, Is.EqualTo("thread-new"));
            Assert.That(service.GetCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.EqualTo(1));
            Assert.That(service.LastSetRequest!.IfInState, Is.EqualTo("email-state"));
            Assert.That(service.LastSetRequest.Create!["draft-1"].BodyValues!["text"].Value,
                Is.EqualTo("Hello from a draft."));
            Assert.That(service.LastSetRequest.Create["draft-1"].Attachments![0].BlobId,
                Is.EqualTo("upload-blob"));
            Assert.That(service.LastSetRequest.Update!["email-old"].Values.Keys,
                Is.EquivalentTo(new[] { "mailboxIds/archive", "mailboxIds/inbox", "keywords/$seen" }));
        }
    }

    [Test]
    public void SetCreatesSimpleAndStructuredBodiesThatProduceEquivalentMimeParts()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.SetResponse.Created = new Dictionary<string, Email>
        {
            ["simple"] = new Email
            {
                Id = "email-simple", BlobId = "raw-simple", ThreadId = "thread-simple", Size = 120
            },
            ["structured"] = new Email
            {
                Id = "email-structured", BlobId = "raw-structured", ThreadId = "thread-structured", Size = 140
            }
        };
        Email simple = DraftEmail();
        EmailBodyPart structuredText = new EmailBodyPart
        {
            PartId = "structured-text",
            Type = "text/plain"
        };
        EmailBodyPart structuredAttachment = new EmailBodyPart
        {
            BlobId = "image-blob",
            Type = "image/png",
            Name = "pixel.png",
            Disposition = "attachment"
        };
        Email structured = new Email
        {
            MailboxIds = new Dictionary<string, bool> { ["drafts"] = true },
            Keywords = new Dictionary<string, bool> { [JmapMailKeywords.Draft] = true },
            Subject = "Structured draft",
            BodyStructure = new EmailBodyPart
            {
                Type = "multipart/mixed",
                SubParts = new List<EmailBodyPart> { structuredText, structuredAttachment }
            },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["structured-text"] = new EmailBodyValue { Value = "Structured text." }
            }
        };
        structured.HeaderProperties["header:X-Draft:asText"] = Json("\"structured\"");
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Email>
            {
                ["simple"] = simple,
                ["structured"] = structured
            }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s2"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSetResponse response = method.Codec.DecodeEmailSetResponse(invocation.Arguments);
        Dictionary<string, byte[]> blobs = new Dictionary<string, byte[]>
        {
            ["upload-blob"] = new byte[] { 1, 2, 3 },
            ["image-blob"] = new byte[] { 137, 80, 78, 71 }
        };
        MimeMessageReadResult simpleMime = new MimeMessageReader().ReadMessage(
            new MimeMessageWriter().WriteMessage(service.LastSetRequest!.Create!["simple"], blobs).Message);
        MimeMessageReadResult structuredMime = new MimeMessageReader().ReadMessage(
            new MimeMessageWriter().WriteMessage(service.LastSetRequest.Create["structured"], blobs).Message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!.Keys, Is.EquivalentTo(new[] { "simple", "structured" }));
            Assert.That(service.LastSetRequest.Create["simple"].BodyStructure, Is.Null);
            Assert.That(service.LastSetRequest.Create["simple"].TextBody, Has.Count.EqualTo(1));
            Assert.That(service.LastSetRequest.Create["structured"].BodyStructure!.Type,
                Is.EqualTo("multipart/mixed"));
            Assert.That(service.LastSetRequest.Create["structured"].TextBody, Is.Null);
            Assert.That(simpleMime.Root!.SubParts![0].DecodedText, Is.EqualTo("Hello from a draft."));
            Assert.That(simpleMime.Root.SubParts[1].DecodedBody, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(structuredMime.Root!.SubParts![0].DecodedText, Is.EqualTo("Structured text."));
            Assert.That(structuredMime.Root.SubParts[1].DecodedBody,
                Is.EqualTo(new byte[] { 137, 80, 78, 71 }));
            Assert.That(structuredMime.Headers.Any(header =>
                header.Name == "X-Draft" && header.Value == " structured"), Is.True);
        }
    }

    [Test]
    public void SetResolvesCreatedIdsInMailboxBlobAndPatchLocationsBeforeDispatch()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.SetResponse.Created = new Dictionary<string, Email>
        {
            ["draft"] = new Email
            {
                Id = "email-new", BlobId = "raw-blob", ThreadId = "thread-new", Size = 20
            }
        };
        service.SetResponse.Updated = new Dictionary<string, JsonElement> { ["email-old"] = Json("null") };
        Email draft = DraftEmail();
        draft.MailboxIds = new Dictionary<string, bool> { ["#drafts"] = true };
        draft.Attachments![0].BlobId = "#upload";
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Email> { ["draft"] = draft },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["email-old"] = Patch(("mailboxIds/#archive", "true"))
            }
        };
        JmapInvocation encoded = method.CreateSetInvocation(request, "s3");
        CreationIdResolver resolver = new CreationIdResolver(new Dictionary<string, string>
        {
            ["drafts"] = "mailbox-drafts",
            ["upload"] = "uploaded-blob",
            ["archive"] = "mailbox-archive"
        });
        CreationIdResolution resolution = resolver.ResolveInvocationData(encoded.Arguments);
        JmapInvocation resolved = new JmapInvocation
        {
            Name = encoded.Name,
            CallId = encoded.CallId,
            Arguments = resolution.Data
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            resolved, Session(true), service, new JmapRequestContext(), Options(10),
            CancellationToken.None);
        EmailSetResponse response = method.Codec.DecodeEmailSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resolution.IsSuccess, Is.True);
            Assert.That(response.Created!.ContainsKey("draft"), Is.True);
            Assert.That(response.Updated!.ContainsKey("email-old"), Is.True);
            Assert.That(service.LastSetRequest!.Create!["draft"].MailboxIds!.Keys,
                Is.EqualTo(new[] { "mailbox-drafts" }));
            Assert.That(service.LastSetRequest.Create["draft"].Attachments![0].BlobId,
                Is.EqualTo("uploaded-blob"));
            Assert.That(service.LastSetRequest.Update!["email-old"].Values.Keys,
                Is.EqualTo(new[] { "mailboxIds/mailbox-archive" }));
        }
    }

    [Test]
    public void SetPreclassifiesInvalidDraftsPatchesAndUpdatesThatWillBeDestroyed()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.SetResponse.Updated = new Dictionary<string, JsonElement> { ["valid-update"] = Json("null") };
        service.SetResponse.Destroyed = new List<string> { "will-destroy" };
        Email invalidDraft = DraftEmail();
        invalidDraft.Headers = new List<EmailHeader> { Header("X-Invalid", "raw headers are forbidden") };
        Email invalidContentHeader = DraftEmail();
        invalidContentHeader.HeaderProperties["header:Content-Type"] = Json("\"text/html\"");
        invalidContentHeader.TextBody![0].HeaderProperties["header:Content-Transfer-Encoding"] =
            Json("\"quoted-printable\"");
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Email>
            {
                ["bad-draft"] = invalidDraft,
                ["bad-content-headers"] = invalidContentHeader
            },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["bad-patch"] = Patch(("subject", "\"changed\"")),
                ["conflict"] = Patch(("keywords", "{}"), ("keywords/$seen", "true")),
                ["will-destroy"] = Patch(("keywords/$seen", "true")),
                ["valid-update"] = Patch(("keywords/$flagged", "true"))
            },
            Destroy = new List<string> { "will-destroy" }
        };

        JmapInvocation invocation = method.ProcessSetInvocation(
            method.CreateSetInvocation(request, "s2"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSetResponse response = method.Codec.DecodeEmailSetResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["bad-draft"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["bad-draft"].Properties, Does.Contain("headers"));
            Assert.That(response.NotCreated["bad-content-headers"].Kind,
                Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["bad-content-headers"].Properties,
                Does.Contain("header:Content-Type"));
            Assert.That(response.NotCreated["bad-content-headers"].Properties,
                Does.Contain("textBody/0/header:Content-Transfer-Encoding"));
            Assert.That(response.NotUpdated!["bad-patch"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotUpdated["bad-patch"].Properties, Does.Contain("subject"));
            Assert.That(response.NotUpdated["conflict"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidPatch));
            Assert.That(response.NotUpdated["will-destroy"].Kind, Is.EqualTo(JmapSetErrorKind.WillDestroy));
            Assert.That(response.Updated!.ContainsKey("valid-update"), Is.True);
            Assert.That(service.LastSetRequest!.Create, Is.Empty);
            Assert.That(service.LastSetRequest.Update!.Keys, Is.EqualTo(new[] { "valid-update" }));
        }
    }

    [Test]
    public void SetEnforcesStateReadOnlyAndCombinedActionLimitBeforeMutation()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        EmailSetRequest stale = new EmailSetRequest { AccountId = "account-1", IfInState = "stale" };
        JmapInvocation staleResult = method.ProcessSetInvocation(
            method.CreateSetInvocation(stale, "s3"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapSession readOnly = Session(true);
        readOnly.Accounts["account-1"].IsReadOnly = true;
        JmapInvocation readOnlyResult = method.ProcessSetInvocation(
            method.CreateSetInvocation(new EmailSetRequest { AccountId = "account-1" }, "s4"),
            readOnly, service, new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapServerOptions oneAction = Options(10);
        oneAction.MaxObjectsInSet = 1;
        EmailSetRequest oversized = new EmailSetRequest
        {
            AccountId = "account-1",
            Destroy = new List<string> { "email-1", "email-2" }
        };
        JmapInvocation oversizedResult = method.ProcessSetInvocation(
            method.CreateSetInvocation(oversized, "s5"), Session(true), service,
            new JmapRequestContext(), oneAction, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, staleResult).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, readOnlyResult).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, oversizedResult).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.SetCalls, Is.Zero);
        }
    }

    [Test]
    public void SetMapsAllEmailSpecificErrorsAndPreservesBlobDetails()
    {
        EmailMethod method = new EmailMethod();
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Email>
            {
                ["missing-blob"] = DraftEmail(),
                ["create-keywords"] = DraftEmail(),
                ["create-mailboxes"] = DraftEmail()
            },
            Update = new Dictionary<string, JmapPatchObject>
            {
                ["update-keywords"] = Patch(("keywords/$seen", "true")),
                ["update-mailboxes"] = Patch(("mailboxIds/archive", "true"))
            }
        };
        string json = "{\"accountId\":\"account-1\",\"oldState\":\"email-state\",\"newState\":\"email-state\"," +
            "\"notCreated\":{\"missing-blob\":{\"type\":\"blobNotFound\",\"notFound\":[\"blob-a\",\"blob-b\"]}," +
            "\"create-keywords\":{\"type\":\"tooManyKeywords\"}," +
            "\"create-mailboxes\":{\"type\":\"tooManyMailboxes\"}}," +
            "\"notUpdated\":{\"update-keywords\":{\"type\":\"tooManyKeywords\"}," +
            "\"update-mailboxes\":{\"type\":\"tooManyMailboxes\"}}}";

        EmailSetResponse response = method.MapSetResponse(Response(JmapMethods.EmailSet, "s6", json), "s6", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["missing-blob"].Kind, Is.EqualTo(JmapSetErrorKind.BlobNotFound));
            Assert.That(response.NotCreated["missing-blob"].NotFound, Is.EqualTo(new[] { "blob-a", "blob-b" }));
            Assert.That(response.NotCreated["create-keywords"].Kind, Is.EqualTo(JmapSetErrorKind.TooManyKeywords));
            Assert.That(response.NotCreated["create-mailboxes"].Kind, Is.EqualTo(JmapSetErrorKind.TooManyMailboxes));
            Assert.That(response.NotUpdated!["update-keywords"].Kind, Is.EqualTo(JmapSetErrorKind.TooManyKeywords));
            Assert.That(response.NotUpdated["update-mailboxes"].Kind, Is.EqualTo(JmapSetErrorKind.TooManyMailboxes));
        }
    }

    [Test]
    public void SetAcceptsEveryStandardAndEmailSpecificErrorInItsDefinedPartition()
    {
        EmailMethod method = new EmailMethod();
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Create = new[]
            {
                "create-forbidden", "create-quota", "create-large", "create-rate",
                "create-properties", "create-blob", "create-keywords", "create-mailboxes"
            }.ToDictionary(id => id, id => DraftEmail()),
            Update = new[]
            {
                "update-forbidden", "update-missing", "update-patch", "update-destroy",
                "update-properties", "update-keywords", "update-mailboxes"
            }.ToDictionary(id => id, id => Patch(("keywords/$seen", "true"))),
            Destroy = new List<string> { "destroy-forbidden", "destroy-missing" }
        };
        EmailSetResponse response = new EmailSetResponse
        {
            AccountId = "account-1",
            NewState = "email-state-2",
            NotCreated = new Dictionary<string, JmapSetError>
            {
                ["create-forbidden"] = SetFailure(JmapErrorCodes.Forbidden),
                ["create-quota"] = SetFailure(JmapErrorCodes.OverQuota),
                ["create-large"] = SetFailure(JmapErrorCodes.TooLarge),
                ["create-rate"] = SetFailure(JmapErrorCodes.RateLimit),
                ["create-properties"] = SetFailureWithProperties(
                    JmapErrorCodes.InvalidProperties, JmapProperties.Subject),
                ["create-blob"] = SetFailureWithMissingBlobs("missing-blob"),
                ["create-keywords"] = SetFailure(JmapErrorCodes.TooManyKeywords),
                ["create-mailboxes"] = SetFailure(JmapErrorCodes.TooManyMailboxes)
            },
            NotUpdated = new Dictionary<string, JmapSetError>
            {
                ["update-forbidden"] = SetFailure(JmapErrorCodes.Forbidden),
                ["update-missing"] = SetFailure(JmapErrorCodes.NotFound),
                ["update-patch"] = SetFailure(JmapErrorCodes.InvalidPatch),
                ["update-destroy"] = SetFailure(JmapErrorCodes.WillDestroy),
                ["update-properties"] = SetFailureWithProperties(
                    JmapErrorCodes.InvalidProperties, JmapProperties.Keywords),
                ["update-keywords"] = SetFailure(JmapErrorCodes.TooManyKeywords),
                ["update-mailboxes"] = SetFailure(JmapErrorCodes.TooManyMailboxes)
            },
            NotDestroyed = new Dictionary<string, JmapSetError>
            {
                ["destroy-forbidden"] = SetFailure(JmapErrorCodes.Forbidden),
                ["destroy-missing"] = SetFailure(JmapErrorCodes.NotFound)
            }
        };

        Assert.DoesNotThrow(() => method.ValidateSetResponse(response, request));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated.Values.Select(error => error.Kind), Is.EquivalentTo(new[]
            {
                JmapSetErrorKind.Forbidden,
                JmapSetErrorKind.OverQuota,
                JmapSetErrorKind.TooLarge,
                JmapSetErrorKind.RateLimit,
                JmapSetErrorKind.InvalidProperties,
                JmapSetErrorKind.BlobNotFound,
                JmapSetErrorKind.TooManyKeywords,
                JmapSetErrorKind.TooManyMailboxes
            }));
            Assert.That(response.NotUpdated.Values.Select(error => error.Kind), Is.EquivalentTo(new[]
            {
                JmapSetErrorKind.Forbidden,
                JmapSetErrorKind.NotFound,
                JmapSetErrorKind.InvalidPatch,
                JmapSetErrorKind.WillDestroy,
                JmapSetErrorKind.InvalidProperties,
                JmapSetErrorKind.TooManyKeywords,
                JmapSetErrorKind.TooManyMailboxes
            }));
            Assert.That(response.NotDestroyed.Values.Select(error => error.Kind),
                Is.EquivalentTo(new[] { JmapSetErrorKind.Forbidden, JmapSetErrorKind.NotFound }));
            Assert.That(response.NotCreated["create-blob"].NotFound,
                Is.EqualTo(new[] { "missing-blob" }));
        }
    }

    [Test]
    public void SetRejectsMissingErrorDetailsAndErrorsInTheWrongPartition()
    {
        EmailMethod method = new EmailMethod();
        EmailSetRequest request = new EmailSetRequest
        {
            AccountId = "account-1",
            Create = new Dictionary<string, Email> { ["item"] = DraftEmail() }
        };
        EmailSetResponse missingBlobs = FailedSetCreate(
            "item", SetFailure(JmapErrorCodes.BlobNotFound));
        EmailSetResponse missingProperties = FailedSetCreate(
            "item", SetFailure(JmapErrorCodes.InvalidProperties));
        EmailSetResponse wrongPartition = FailedSetCreate(
            "item", SetFailure(JmapErrorCodes.NotFound));

        JmapProtocolException blobError = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(missingBlobs, request))!;
        JmapProtocolException propertyError = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(missingProperties, request))!;
        JmapProtocolException partitionError = Assert.Throws<JmapProtocolException>(() =>
            method.ValidateSetResponse(wrongPartition, request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blobError.Path, Is.EqualTo("notCreated/item/notFound"));
            Assert.That(propertyError.Path, Is.EqualTo("notCreated/item/properties"));
            Assert.That(partitionError.Path, Is.EqualTo("notCreated/item/type"));
        }
    }

    [Test]
    public void SetAcceptsDefaultedBodyValueFlagsAndRejectsIncompleteCreatedMetadata()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.SetResponse.Created = new Dictionary<string, Email>
        {
            ["draft"] = new Email { Id = "email-new", BlobId = "raw-blob", ThreadId = "thread-new", Size = 25 }
        };
        JmapInvocation invocation = method.ProcessSetInvocation(
            RawInvocation(JmapMethods.EmailSet, "s7",
                "{\"accountId\":\"account-1\",\"create\":{\"draft\":{\"mailboxIds\":{\"drafts\":true}," +
                "\"keywords\":{\"$draft\":true},\"textBody\":[{\"partId\":\"text\",\"type\":\"text/plain\"}]," +
                "\"bodyValues\":{\"text\":{\"value\":\"defaults\"}}}}}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailSetResponse response = method.Codec.DecodeEmailSetResponse(invocation.Arguments);
        JmapResponse incomplete = Response(JmapMethods.EmailSet, "s8",
            "{\"accountId\":\"account-1\",\"oldState\":\"email-state\",\"newState\":\"email-state-2\"," +
            "\"created\":{\"draft\":{\"id\":\"email-new\",\"size\":25}}}");
        JmapProtocolException error = Assert.Throws<JmapProtocolException>(
            () => method.MapSetResponse(incomplete, "s8", new EmailSetRequest
            {
                AccountId = "account-1",
                Create = new Dictionary<string, Email> { ["draft"] = DraftEmail() }
            }))!;
        EmailBodyValue storedBodyValue = service.LastSetRequest!.Create!["draft"].BodyValues!["text"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Created!["draft"].Id, Is.EqualTo("email-new"));
            Assert.That(storedBodyValue.IsEncodingProblem, Is.False);
            Assert.That(storedBodyValue.IsTruncated, Is.False);
            Assert.That(error.Path, Is.EqualTo("created/draft"));
        }
    }

    [Test]
    public void CopyValidatesBothStatesAndPassesMutableOverridesAndDestroyOptions()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = CopyService();
        service.CopyResponse.Created = new Dictionary<string, Email>
        {
            ["copy-1"] = new Email { Id = "target-email", BlobId = "target-blob", ThreadId = "target-thread", Size = 44 }
        };
        service.CopyResponse.NotCreated = new Dictionary<string, JmapSetError>
        {
            ["copy-2"] = CopyError(JmapErrorCodes.NotFound)
        };
        EmailCopyRequest request = new EmailCopyRequest
        {
            FromAccountId = "source-account",
            IfFromInState = "source-state",
            AccountId = "account-1",
            IfInState = "email-state",
            Create = new Dictionary<string, Email>
            {
                ["copy-1"] = CopyEmail("source-1", "archive", JmapMailKeywords.Seen),
                ["copy-2"] = CopyEmail("source-2", "inbox", JmapMailKeywords.Flagged)
            },
            OnSuccessDestroyOriginal = true,
            HasOnSuccessDestroyOriginal = true,
            DestroyFromIfInState = "source-state"
        };

        JmapInvocation invocation = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp1"), CopySession(true, true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailCopyResponse response = method.Codec.DecodeEmailCopyResponse(invocation.Arguments);
        EmailCopyResponse mapped = method.MapCopyResponse(
            Response(JmapMethods.EmailCopy, "cp1", invocation.Arguments.GetRawText()), "cp1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.FromAccountId, Is.EqualTo("source-account"));
            Assert.That(response.OldState, Is.EqualTo("email-state"));
            Assert.That(response.NewState, Is.EqualTo("target-state-2"));
            Assert.That(response.Created!["copy-1"].BlobId, Is.EqualTo("target-blob"));
            Assert.That(response.NotCreated!["copy-2"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(invocation.Arguments.GetProperty("created").GetProperty("copy-1")
                .EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { "id", "blobId", "threadId", "size" }));
            Assert.That(mapped.Created!["copy-1"].ThreadId, Is.EqualTo("target-thread"));
            Assert.That(service.GetAccounts, Is.EqualTo(new[] { "source-account", "account-1" }));
            Assert.That(service.CopyCalls, Is.EqualTo(1));
            Assert.That(service.LastCopyRequest!.Create["copy-1"].MailboxIds!.Keys,
                Is.EqualTo(new[] { "archive" }));
            Assert.That(service.LastCopyRequest.Create["copy-1"].Keywords!.Keys,
                Is.EqualTo(new[] { JmapMailKeywords.Seen }));
            Assert.That(service.LastCopyRequest.OnSuccessDestroyOriginal, Is.True);
            Assert.That(service.LastCopyRequest.DestroyFromIfInState, Is.EqualTo("source-state"));
        }
    }

    [Test]
    public void CopyPreclassifiesForbiddenOverridesAndDuplicateSourceEmails()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = CopyService();
        service.CopyResponse.Created = new Dictionary<string, Email>
        {
            ["first"] = new Email { Id = "target-email", BlobId = "target-blob", ThreadId = "target-thread", Size = 10 }
        };
        string arguments = "{\"fromAccountId\":\"source-account\",\"accountId\":\"account-1\",\"create\":{" +
            "\"first\":{\"id\":\"source-1\",\"mailboxIds\":{\"archive\":true}}," +
            "\"duplicate\":{\"id\":\"source-1\",\"mailboxIds\":{\"inbox\":true}}," +
            "\"invalid\":{\"id\":\"source-3\",\"mailboxIds\":{\"inbox\":true},\"subject\":\"forbidden\"}}}";

        JmapInvocation invocation = method.ProcessCopyInvocation(
            RawInvocation(JmapMethods.EmailCopy, "cp2", arguments), CopySession(true, true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailCopyResponse response = method.Codec.DecodeEmailCopyResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.LastCopyRequest!.Create.Keys, Is.EqualTo(new[] { "first" }));
            Assert.That(response.Created!.ContainsKey("first"), Is.True);
            Assert.That(response.NotCreated!["duplicate"].Kind, Is.EqualTo(JmapSetErrorKind.AlreadyExists));
            Assert.That(response.NotCreated["duplicate"].ExistingId, Is.EqualTo("target-email"));
            Assert.That(response.NotCreated["invalid"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["invalid"].Properties, Does.Contain("subject"));
        }
    }

    [Test]
    public void CopyPropagatesPrimaryFailureToDuplicatesAndDefaultsDestructionOff()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = CopyService();
        service.CopyResponse.NotCreated = new Dictionary<string, JmapSetError>
        {
            ["a-primary"] = CopyError(JmapErrorCodes.NotFound)
        };
        EmailCopyRequest request = new EmailCopyRequest
        {
            FromAccountId = "source-account",
            AccountId = "account-1",
            Create = new Dictionary<string, Email>
            {
                ["a-primary"] = CopyEmail("source-1", "archive", JmapMailKeywords.Seen),
                ["z-duplicate"] = CopyEmail("source-1", "inbox", JmapMailKeywords.Flagged)
            }
        };
        JmapInvocation encoded = method.CreateCopyInvocation(request, "cp3");

        JmapInvocation invocation = method.ProcessCopyInvocation(
            encoded, CopySession(true, true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailCopyResponse response = method.Codec.DecodeEmailCopyResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!.Keys,
                Is.EquivalentTo(new[] { "a-primary", "z-duplicate" }));
            Assert.That(response.NotCreated["a-primary"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(response.NotCreated["z-duplicate"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
            Assert.That(service.LastCopyRequest!.Create.Keys, Is.EqualTo(new[] { "a-primary" }));
            Assert.That(service.LastCopyRequest.HasOnSuccessDestroyOriginal, Is.False);
            Assert.That(service.LastCopyRequest.OnSuccessDestroyOriginal, Is.False);
            Assert.That(service.LastCopyRequest.DestroyFromIfInState, Is.Null);
            Assert.That(service.GetAccounts, Is.EqualTo(new[] { "source-account", "account-1" }));
        }
    }

    [Test]
    public void CopyReturnsCrossAccountReadOnlyLimitAndStateErrorsBeforeCopying()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = CopyService();
        EmailCopyRequest request = CopyRequest();
        JmapInvocation missingSource = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp3"), CopySession(false, true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapSession unsupportedSourceSession = CopySession(true, true);
        unsupportedSourceSession.Accounts["source-account"].AccountCapabilities.Clear();
        JmapInvocation unsupportedSource = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp4"), unsupportedSourceSession, service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapInvocation missingTarget = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp4-target"), CopySession(true, false), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapSession unsupportedTargetSession = CopySession(true, true);
        unsupportedTargetSession.Accounts["account-1"].AccountCapabilities.Clear();
        JmapInvocation unsupportedTarget = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp4-unsupported-target"), unsupportedTargetSession, service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapSession readOnlyTargetSession = CopySession(true, true);
        readOnlyTargetSession.Accounts["account-1"].IsReadOnly = true;
        JmapInvocation readOnlyTarget = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp5"), readOnlyTargetSession, service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        EmailCopyRequest stale = CopyRequest();
        stale.IfFromInState = "stale";
        JmapInvocation staleSource = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(stale, "cp6"), CopySession(true, true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        EmailCopyRequest staleTargetRequest = CopyRequest();
        staleTargetRequest.IfInState = "stale";
        JmapInvocation staleTarget = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(staleTargetRequest, "cp6-target"), CopySession(true, true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapServerOptions noCopies = Options(10);
        noCopies.MaxObjectsInSet = 0;
        JmapInvocation limited = method.ProcessCopyInvocation(
            method.CreateCopyInvocation(request, "cp7"), CopySession(true, true), service,
            new JmapRequestContext(), noCopies, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, missingSource).Kind, Is.EqualTo(JmapMethodErrorKind.FromAccountNotFound));
            Assert.That(Error(method, unsupportedSource).Kind,
                Is.EqualTo(JmapMethodErrorKind.FromAccountNotSupportedByMethod));
            Assert.That(Error(method, missingTarget).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, unsupportedTarget).Kind,
                Is.EqualTo(JmapMethodErrorKind.AccountNotSupportedByMethod));
            Assert.That(Error(method, readOnlyTarget).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, staleSource).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, staleTarget).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, limited).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.CopyCalls, Is.Zero);
        }
    }

    [Test]
    public void CopyMapsAlreadyExistsAndEmailLimitErrorsAndRejectsInvalidCreatedMetadata()
    {
        EmailMethod method = new EmailMethod();
        EmailCopyRequest request = new EmailCopyRequest
        {
            FromAccountId = "source-account",
            AccountId = "account-1",
            Create = new Dictionary<string, Email>
            {
                ["existing"] = CopyEmail("source-1", "inbox", JmapMailKeywords.Seen),
                ["keywords"] = CopyEmail("source-2", "inbox", JmapMailKeywords.Seen),
                ["mailboxes"] = CopyEmail("source-3", "inbox", JmapMailKeywords.Seen)
            }
        };
        string errors = "{\"fromAccountId\":\"source-account\",\"accountId\":\"account-1\"," +
            "\"oldState\":\"email-state\",\"newState\":\"target-state-2\",\"notCreated\":{" +
            "\"existing\":{\"type\":\"alreadyExists\",\"existingId\":\"target-existing\"}," +
            "\"keywords\":{\"type\":\"tooManyKeywords\"}," +
            "\"mailboxes\":{\"type\":\"tooManyMailboxes\"}}}";
        EmailCopyResponse response = method.MapCopyResponse(
            Response(JmapMethods.EmailCopy, "cp8", errors), "cp8", request);
        JmapResponse incomplete = Response(JmapMethods.EmailCopy, "cp9",
            "{\"fromAccountId\":\"source-account\",\"accountId\":\"account-1\"," +
            "\"oldState\":\"email-state\",\"newState\":\"target-state-2\"," +
            "\"created\":{\"existing\":{\"id\":\"target-email\",\"size\":1}}," +
            "\"notCreated\":{\"keywords\":{\"type\":\"tooManyKeywords\"}," +
            "\"mailboxes\":{\"type\":\"tooManyMailboxes\"}}}" );

        JmapProtocolException invalid = Assert.Throws<JmapProtocolException>(
            () => method.MapCopyResponse(incomplete, "cp9", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.NotCreated!["existing"].Kind, Is.EqualTo(JmapSetErrorKind.AlreadyExists));
            Assert.That(response.NotCreated["existing"].ExistingId, Is.EqualTo("target-existing"));
            Assert.That(response.NotCreated["keywords"].Kind, Is.EqualTo(JmapSetErrorKind.TooManyKeywords));
            Assert.That(response.NotCreated["mailboxes"].Kind, Is.EqualTo(JmapSetErrorKind.TooManyMailboxes));
            Assert.That(invalid.Path, Is.EqualTo("created/existing"));
        }
    }

    [Test]
    public void ImportPassesBlobMetadataAndPartitionsAllSpecifiedMimeAndStorageErrors()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ImportService();
        service.ImportResponse.Created = new Dictionary<string, Email>
        {
            ["created"] = new Email { Id = "email-new", BlobId = "stored-blob", ThreadId = "thread-new", Size = 52 }
        };
        service.ImportResponse.NotCreated = new Dictionary<string, JmapSetError>
        {
            ["missing"] = ImportError(JmapErrorCodes.InvalidProperties,
                new List<string> { JmapProperties.BlobId }, null),
            ["malformed"] = ImportError(JmapErrorCodes.InvalidEmail, null, null),
            ["duplicate"] = ImportError(JmapErrorCodes.AlreadyExists, null, "email-existing"),
            ["quota"] = ImportError(JmapErrorCodes.OverQuota, null, null)
        };
        EmailImportRequest request = new EmailImportRequest
        {
            AccountId = "account-1",
            IfInState = "email-state",
            Emails = new Dictionary<string, EmailImport>
            {
                ["created"] = ImportEmail("upload-created", "archive", JmapMailKeywords.Seen),
                ["missing"] = ImportEmail("upload-missing", "inbox", JmapMailKeywords.Seen),
                ["malformed"] = ImportEmail("upload-malformed", "inbox", JmapMailKeywords.Seen),
                ["duplicate"] = ImportEmail("upload-duplicate", "inbox", JmapMailKeywords.Flagged),
                ["quota"] = ImportEmail("upload-quota", "inbox", JmapMailKeywords.Seen)
            }
        };

        JmapInvocation invocation = method.ProcessImportInvocation(
            method.CreateImportInvocation(request, "im1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailImportResponse response = method.Codec.DecodeEmailImportResponse(invocation.Arguments);
        EmailImportResponse mapped = method.MapImportResponse(
            Response(JmapMethods.EmailImport, "im1", invocation.Arguments.GetRawText()), "im1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.OldState, Is.EqualTo("email-state"));
            Assert.That(response.NewState, Is.EqualTo("import-state-2"));
            Assert.That(response.Created!["created"].BlobId, Is.EqualTo("stored-blob"));
            Assert.That(response.NotCreated!["missing"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["missing"].Properties, Is.EqualTo(new[] { "blobId" }));
            Assert.That(response.NotCreated["malformed"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidEmail));
            Assert.That(response.NotCreated["duplicate"].Kind, Is.EqualTo(JmapSetErrorKind.AlreadyExists));
            Assert.That(response.NotCreated["duplicate"].ExistingId, Is.EqualTo("email-existing"));
            Assert.That(response.NotCreated["quota"].Kind, Is.EqualTo(JmapSetErrorKind.OverQuota));
            Assert.That(invocation.Arguments.GetProperty("created").GetProperty("created")
                .EnumerateObject().Select(property => property.Name),
                Is.EqualTo(new[] { "id", "blobId", "threadId", "size" }));
            Assert.That(mapped.Created!["created"].ThreadId, Is.EqualTo("thread-new"));
            Assert.That(service.ImportCalls, Is.EqualTo(1));
            Assert.That(service.LastImportRequest!.Emails["created"].BlobId, Is.EqualTo("upload-created"));
            Assert.That(service.LastImportRequest.Emails["created"].MailboxIds.Keys,
                Is.EqualTo(new[] { "archive" }));
            Assert.That(service.LastImportRequest.Emails["created"].Keywords.Keys,
                Is.EqualTo(new[] { JmapMailKeywords.Seen }));
            Assert.That(service.LastImportRequest.Emails["created"].ReceivedAt,
                Is.EqualTo("2026-09-11T12:00:00Z"));
        }
    }

    [Test]
    public void ImportReturnsPerEmailInvalidPropertiesForMalformedMetadata()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ImportService();
        service.ImportResponse.Created = new Dictionary<string, Email>
        {
            ["valid"] = new Email { Id = "email-new", BlobId = "stored-blob", ThreadId = "thread-new", Size = 12 }
        };
        string arguments = "{\"accountId\":\"account-1\",\"emails\":{" +
            "\"valid\":{\"blobId\":\"blob-valid\",\"mailboxIds\":{\"inbox\":true}}," +
            "\"blob\":{\"mailboxIds\":{\"inbox\":true}}," +
            "\"mailbox\":{\"blobId\":\"blob-mailbox\",\"mailboxIds\":[]}," +
            "\"keyword\":{\"blobId\":\"blob-keyword\",\"mailboxIds\":{\"inbox\":true}," +
                "\"keywords\":{\"$recent\":true}}," +
            "\"date\":{\"blobId\":\"blob-date\",\"mailboxIds\":{\"inbox\":true},\"receivedAt\":7}}}";

        JmapInvocation invocation = method.ProcessImportInvocation(
            RawInvocation(JmapMethods.EmailImport, "im2", arguments), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailImportResponse response = method.Codec.DecodeEmailImportResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.LastImportRequest!.Emails.Keys, Is.EqualTo(new[] { "valid" }));
            Assert.That(response.Created!.ContainsKey("valid"), Is.True);
            Assert.That(response.NotCreated!["blob"].Properties, Does.Contain("blobId"));
            Assert.That(response.NotCreated["mailbox"].Properties!.Any(path => path.StartsWith("mailboxIds")),
                Is.True);
            Assert.That(response.NotCreated["keyword"].Properties, Does.Contain("keywords/$recent"));
            Assert.That(response.NotCreated["date"].Properties, Does.Contain("receivedAt"));
        }
    }

    [Test]
    public void ImportEnforcesAccountStateReadOnlyAndObjectLimitBeforeMutation()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ImportService();
        EmailImportRequest request = ImportRequest();
        JmapInvocation missingAccount = method.ProcessImportInvocation(
            method.CreateImportInvocation(request, "im3"), Session(false), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapSession readOnly = Session(true);
        readOnly.Accounts["account-1"].IsReadOnly = true;
        JmapInvocation readOnlyResult = method.ProcessImportInvocation(
            method.CreateImportInvocation(request, "im4"), readOnly, service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        EmailImportRequest stale = ImportRequest();
        stale.IfInState = "stale";
        JmapInvocation staleResult = method.ProcessImportInvocation(
            method.CreateImportInvocation(stale, "im5"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        JmapServerOptions noImports = Options(10);
        noImports.MaxObjectsInSet = 0;
        JmapInvocation limited = method.ProcessImportInvocation(
            method.CreateImportInvocation(request, "im6"), Session(true), service,
            new JmapRequestContext(), noImports, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, missingAccount).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, readOnlyResult).Kind, Is.EqualTo(JmapMethodErrorKind.AccountReadOnly));
            Assert.That(Error(method, staleResult).Kind, Is.EqualTo(JmapMethodErrorKind.StateMismatch));
            Assert.That(Error(method, limited).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.ImportCalls, Is.Zero);
        }
    }

    [Test]
    public void ImportClientMappingRejectsEmailSetBlobErrorsAndIncompleteCreatedMetadata()
    {
        EmailMethod method = new EmailMethod();
        EmailImportRequest request = ImportRequest();
        JmapResponse blobError = Response(JmapMethods.EmailImport, "im7",
            "{\"accountId\":\"account-1\",\"oldState\":\"email-state\",\"newState\":\"import-state-2\"," +
            "\"notCreated\":{\"created\":{\"type\":\"blobNotFound\",\"notFound\":[\"blob-a\"]}}}");
        JmapResponse incomplete = Response(JmapMethods.EmailImport, "im8",
            "{\"accountId\":\"account-1\",\"oldState\":\"email-state\",\"newState\":\"import-state-2\"," +
            "\"created\":{\"created\":{\"id\":\"email-new\",\"size\":1}}}");

        JmapProtocolException wrongError = Assert.Throws<JmapProtocolException>(
            () => method.MapImportResponse(blobError, "im7", request))!;
        JmapProtocolException missingMetadata = Assert.Throws<JmapProtocolException>(
            () => method.MapImportResponse(incomplete, "im8", request))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wrongError.Path, Is.EqualTo("notCreated/created/type"));
            Assert.That(missingMetadata.Path, Is.EqualTo("created/created"));
        }
    }

    [Test]
    public void ParseAppliesParseDefaultsAndPartitionsBlobsWithoutStoringEmails()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ParseService();
        service.ParseResponse.Parsed = new Dictionary<string, Email> { ["blob-good"] = ParsedEmail("blob-good") };
        service.ParseResponse.NotParsable = new List<string> { "blob-bad" };
        service.ParseResponse.NotFound = new List<string> { "blob-missing" };
        EmailParseRequest request = new EmailParseRequest
        {
            AccountId = "account-1",
            BlobIds = new List<string> { "blob-good", "blob-bad", "blob-missing" }
        };

        JmapInvocation invocation = method.ProcessParseInvocation(
            method.CreateParseInvocation(request, "p1"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailParseResponse response = method.Codec.DecodeEmailParseResponse(invocation.Arguments);
        EmailParseResponse mapped = method.MapParseResponse(
            Response(JmapMethods.EmailParse, "p1", invocation.Arguments.GetRawText()), "p1", request);
        JsonElement parsed = invocation.Arguments.GetProperty("parsed").GetProperty("blob-good");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.EnumerateObject().Select(property => property.Name),
                Is.EqualTo(method.DefaultParseProperties()));
            Assert.That(parsed.TryGetProperty("id", out JsonElement unusedId), Is.False);
            Assert.That(parsed.GetProperty("bodyValues").EnumerateObject(), Is.Empty);
            Assert.That(response.NotParsable, Is.EqualTo(new[] { "blob-bad" }));
            Assert.That(response.NotFound, Is.EqualTo(new[] { "blob-missing" }));
            Assert.That(mapped.Parsed!["blob-good"].Subject, Is.EqualTo("Example"));
            Assert.That(service.LastParseRequest!.Properties, Does.Not.Contain(JmapProperties.Id));
            Assert.That(service.LastParseRequest.Properties, Does.Contain(JmapProperties.TextBody));
            Assert.That(service.LastParseRequest.BodyProperties, Does.Contain(JmapProperties.SubParts));
            Assert.That(service.ParseCalls, Is.EqualTo(1));
            Assert.That(service.SetCalls, Is.Zero);
            Assert.That(service.ImportCalls, Is.Zero);
        }
    }

    [TestCase(true, false, false, "1")]
    [TestCase(false, true, false, "2")]
    [TestCase(true, true, false, "1,2")]
    [TestCase(false, false, true, "1,2")]
    public void ParseBodyFetchFlagsSelectTheRequestedBodyValueUnion(
        bool fetchText,
        bool fetchHtml,
        bool fetchAll,
        string expectedPartIds)
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ParseService();
        service.ParseResponse.Parsed = new Dictionary<string, Email>
        {
            ["blob-1"] = ParsedEmail("blob-1")
        };
        EmailParseRequest request = new EmailParseRequest
        {
            AccountId = "account-1",
            BlobIds = new List<string> { "blob-1" },
            Properties = new List<string> { JmapProperties.BodyValues },
            FetchTextBodyValues = fetchText,
            HasFetchTextBodyValues = true,
            FetchHtmlBodyValues = fetchHtml,
            HasFetchHtmlBodyValues = true,
            FetchAllBodyValues = fetchAll,
            HasFetchAllBodyValues = true
        };

        JmapInvocation invocation = method.ProcessParseInvocation(
            method.CreateParseInvocation(request, "p2"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement values = invocation.Arguments.GetProperty(JmapProperties.Parsed)
            .GetProperty("blob-1").GetProperty(JmapProperties.BodyValues);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(values.EnumerateObject().Select(value => value.Name),
                Is.EquivalentTo(expectedPartIds.Split(',')));
            Assert.That(service.LastParseRequest!.FetchTextBodyValues,
                Is.EqualTo(fetchText && !fetchAll));
            Assert.That(service.LastParseRequest.FetchHtmlBodyValues,
                Is.EqualTo(fetchHtml && !fetchAll));
            Assert.That(service.LastParseRequest.FetchAllBodyValues, Is.EqualTo(fetchAll));
        }
    }

    [Test]
    public void ParseProjectsRealMimePartsAndClassifiesMalformedMimeInAPartialBatch()
    {
        string validMessage =
            "Subject: MIME example\r\n" +
            "Content-Type: multipart/mixed; boundary=parts\r\n\r\n" +
            "--parts\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nHello MIME.\r\n" +
            "--parts\r\nContent-Type: application/octet-stream\r\n" +
            "Content-Disposition: attachment; filename=data.bin\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\nAQID\r\n" +
            "--parts--\r\n";
        string malformedMessage =
            "Content-Type: application/octet-stream\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\nYWJj$ZA";
        MimeMessageReader reader = new MimeMessageReader();
        MimeMessageReadResult validMime = reader.ReadMessage(Encoding.UTF8.GetBytes(validMessage));
        MimeMessageReadResult malformedMime = reader.ReadMessage(Encoding.ASCII.GetBytes(malformedMessage));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(validMime, 0);
        Email parsedEmail = new Email
        {
            BlobId = "blob-valid",
            Headers = validMime.Headers,
            BodyStructure = mapped.BodyStructure,
            BodyValues = mapped.BodyValues,
            TextBody = mapped.TextBody,
            HtmlBody = mapped.HtmlBody,
            Attachments = mapped.Attachments,
            HasAttachment = mapped.HasAttachment,
            Preview = mapped.Preview
        };
        FakeEmailService service = ParseService();
        service.ParseResponse.Parsed = new Dictionary<string, Email> { ["blob-valid"] = parsedEmail };
        service.ParseResponse.NotParsable = new List<string> { "blob-malformed" };
        EmailParseRequest request = new EmailParseRequest
        {
            AccountId = "account-1",
            BlobIds = new List<string> { "blob-valid", "blob-malformed" },
            Properties = new List<string>
            {
                JmapProperties.BodyStructure,
                JmapProperties.BodyValues,
                JmapProperties.TextBody,
                JmapProperties.Attachments,
                JmapProperties.HasAttachment,
                JmapProperties.Preview
            },
            FetchAllBodyValues = true,
            HasFetchAllBodyValues = true
        };
        EmailMethod method = new EmailMethod();

        JmapInvocation invocation = method.ProcessParseInvocation(
            method.CreateParseInvocation(request, "p3"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailParseResponse response = method.Codec.DecodeEmailParseResponse(invocation.Arguments);
        Email projected = response.Parsed!["blob-valid"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validMime.HasMalformedInput, Is.False);
            Assert.That(malformedMime.HasMalformedInput, Is.True);
            Assert.That(projected.BodyStructure!.Type, Is.EqualTo("multipart/mixed"));
            Assert.That(projected.BodyValues!["1"].Value, Is.EqualTo("Hello MIME."));
            Assert.That(projected.Attachments, Has.Count.EqualTo(1));
            Assert.That(projected.Attachments![0].Name, Is.EqualTo("data.bin"));
            Assert.That(projected.HasAttachment, Is.True);
            Assert.That(projected.Preview, Is.EqualTo("Hello MIME."));
            Assert.That(response.NotParsable, Is.EqualTo(new[] { "blob-malformed" }));
            Assert.That(response.NotFound, Is.Null);
        }
    }

    [Test]
    public void ParseProjectsNullStoreMetadataDynamicHeadersAndUtf8SafeBodyValues()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ParseService();
        Email email = ParsedEmail("blob-good");
        email.ThreadId = null;
        email.Headers!.Add(Header("List-Post", "<https://example.test/list>"));
        email.BodyValues!["2"].Value = "<p>é🙂tail</p>";
        service.ParseResponse.Parsed = new Dictionary<string, Email> { ["blob-good"] = email };
        EmailParseRequest request = new EmailParseRequest
        {
            AccountId = "account-1",
            BlobIds = new List<string> { "blob-good" },
            Properties = new List<string>
            {
                JmapProperties.Id, JmapProperties.MailboxIds, JmapProperties.Keywords,
                JmapProperties.ReceivedAt, JmapProperties.ThreadId, "header:List-Post:asURLs",
                JmapProperties.BodyStructure, JmapProperties.BodyValues
            },
            BodyProperties = new List<string> { JmapProperties.PartId, JmapProperties.Type },
            FetchAllBodyValues = true,
            HasFetchAllBodyValues = true,
            MaxBodyValueBytes = 8,
            HasMaxBodyValueBytes = true
        };

        JmapInvocation invocation = method.ProcessParseInvocation(
            method.CreateParseInvocation(request, "p2"), Session(true), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JsonElement parsed = invocation.Arguments.GetProperty("parsed").GetProperty("blob-good");
        JsonElement html = parsed.GetProperty("bodyValues").GetProperty("2");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.GetProperty("id").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(parsed.GetProperty("mailboxIds").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(parsed.GetProperty("keywords").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(parsed.GetProperty("receivedAt").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(parsed.GetProperty("threadId").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(parsed.GetProperty("header:List-Post:asURLs")[0].GetString(),
                Is.EqualTo("https://example.test/list"));
            Assert.That(html.GetProperty("value").GetString(), Is.EqualTo("<p>é"));
            Assert.That(html.GetProperty("isTruncated").GetBoolean(), Is.True);
            Assert.That(service.LastParseRequest!.FetchAllBodyValues, Is.True);
            Assert.That(service.LastParseRequest.Properties, Does.Contain(JmapProperties.Headers));
            Assert.That(service.LastParseRequest.Properties, Does.Contain(JmapProperties.BodyStructure));
            Assert.That(service.LastParseRequest.MaxBodyValueBytes, Is.EqualTo(8));
        }
    }

    [Test]
    public void ParseRejectsInvalidSelectionsAccountsAndBlobLimitsBeforeBackendAccess()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = ParseService();
        JmapInvocation invalidHeader = method.ProcessParseInvocation(
            RawInvocation(JmapMethods.EmailParse, "p3",
                "{\"accountId\":\"account-1\",\"blobIds\":[\"blob-1\"]," +
                "\"properties\":[\"header:From:asDate\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation invalidBlob = method.ProcessParseInvocation(
            RawInvocation(JmapMethods.EmailParse, "p4",
                "{\"accountId\":\"account-1\",\"blobIds\":[\"bad id\"]}"),
            Session(true), service, new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapInvocation missingAccount = method.ProcessParseInvocation(
            method.CreateParseInvocation(ParseRequest(), "p5"), Session(false), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        JmapServerOptions oneBlob = Options(1);
        EmailParseRequest tooMany = ParseRequest();
        tooMany.BlobIds.Add("blob-2");
        JmapInvocation limited = method.ProcessParseInvocation(
            method.CreateParseInvocation(tooMany, "p6"), Session(true), service,
            new JmapRequestContext(), oneBlob, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, invalidHeader).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, invalidBlob).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, missingAccount).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(Error(method, limited).Kind, Is.EqualTo(JmapMethodErrorKind.RequestTooLarge));
            Assert.That(service.ParseCalls, Is.Zero);
        }
    }

    [Test]
    public void ParseRejectsOverlappingPartitionsStoredMetadataAndUnrequestedProperties()
    {
        EmailMethod method = new EmailMethod();
        EmailParseRequest request = ParseRequest();
        FakeEmailService overlapping = ParseService();
        overlapping.ParseResponse.Parsed = new Dictionary<string, Email>
        {
            ["blob-1"] = ParsedEmail("blob-1")
        };
        overlapping.ParseResponse.NotFound = new List<string> { "blob-1" };
        JmapProtocolException overlap = Assert.Throws<JmapProtocolException>(
            () => method.ProcessParseInvocation(
                method.CreateParseInvocation(request, "p7"), Session(true), overlapping,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        FakeEmailService stored = ParseService();
        Email storedEmail = ParsedEmail("blob-1");
        storedEmail.Id = "stored-email";
        stored.ParseResponse.Parsed = new Dictionary<string, Email> { ["blob-1"] = storedEmail };
        JmapProtocolException metadata = Assert.Throws<JmapProtocolException>(
            () => method.ProcessParseInvocation(
                method.CreateParseInvocation(request, "p8"), Session(true), stored,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        EmailParseRequest subjectOnly = ParseRequest();
        subjectOnly.Properties = new List<string> { JmapProperties.Subject };
        JmapResponse extra = Response(JmapMethods.EmailParse, "p9",
            "{\"accountId\":\"account-1\",\"parsed\":{\"blob-1\":{\"subject\":\"Hi\",\"size\":2}}}");
        JmapProtocolException projection = Assert.Throws<JmapProtocolException>(
            () => method.MapParseResponse(extra, "p9", subjectOnly))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(overlap.Path, Is.EqualTo("notFound"));
            Assert.That(metadata.Path, Is.EqualTo("parsed/blob-1"));
            Assert.That(projection.Path, Is.EqualTo("parsed/blob-1/size"));
        }
    }

    [Test]
    public void ClientMappingHandlesEmailChangesAndStandardFailureResponses()
    {
        EmailMethod method = new EmailMethod();
        JmapResponse successful = Response(
            JmapMethods.EmailChanges,
            "c1",
            "{\"accountId\":\"account-1\",\"oldState\":\"old-state\",\"newState\":\"new-state\",\"hasMoreChanges\":false,\"created\":[\"e1\"],\"updated\":[\"e2\"],\"destroyed\":[\"e3\"]}");
        EmailChangesResponse mapped = method.MapChangesResponse(successful, "c1", ChangesRequest());
        JmapResponse failed = Response(
            ResponseMapper.ErrorResponseName,
            "c2",
            "{\"type\":\"cannotCalculateChanges\",\"description\":\"expired state\"}");
        JmapMethodException error = Assert.Throws<JmapMethodException>(
            () => method.MapChangesResponse(failed, "c2", ChangesRequest()))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.Created, Is.EqualTo(new[] { "e1" }));
            Assert.That(mapped.Updated, Is.EqualTo(new[] { "e2" }));
            Assert.That(mapped.Destroyed, Is.EqualTo(new[] { "e3" }));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.CannotCalculateChanges));
        }
    }

    [Test]
    public void QueryDispatchesFiltersSortingPaginationAndThreadCollapse()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.QueryResponse.Position = 2;
        service.QueryResponse.Ids.Add("e3");
        service.QueryResponse.Ids.Add("e4");
        service.QueryResponse.Total = 6;
        service.QueryResponse.HasTotal = true;
        EmailQueryRequest request = new EmailQueryRequest
        {
            AccountId = "account-1",
            Filter = new EmailFilter { HasKeyword = "$seen" },
            Sort = new List<EmailComparator>
            {
                new EmailComparator
                {
                    Property = JmapProperties.Subject,
                    IsAscending = true,
                    HasIsAscending = true,
                    Collation = "i;ascii-casemap"
                }
            },
            Position = 2,
            HasPosition = true,
            Limit = 2,
            HasLimit = true,
            CalculateTotal = true,
            HasCalculateTotal = true,
            CollapseThreads = true,
            HasCollapseThreads = true
        };

        JmapInvocation invocation = method.ProcessQueryInvocation(
            method.CreateQueryInvocation(request, "q1"), QuerySession(JmapProperties.Subject), service,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailQueryResponse response = method.QueryCodec.DecodeEmailQueryResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invocation.Name, Is.EqualTo(JmapMethods.EmailQuery));
            Assert.That(response.QueryState, Is.EqualTo("query-state"));
            Assert.That(response.Ids, Is.EqualTo(new[] { "e3", "e4" }));
            Assert.That(response.Position, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(6));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(service.QueryCalls, Is.EqualTo(1));
            Assert.That(service.LastQueryRequest!.Filter!.HasKeyword, Is.EqualTo("$seen"));
            Assert.That(service.LastQueryRequest.Sort![0].Property, Is.EqualTo(JmapProperties.Subject));
            Assert.That(service.LastQueryRequest.Position, Is.EqualTo(2));
            Assert.That(service.LastQueryRequest.Limit, Is.EqualTo(2));
            Assert.That(service.LastQueryRequest.CollapseThreads, Is.True);
            Assert.That(service.LastQueryRequest.HasCollapseThreads, Is.True);
        }
    }

    [Test]
    public void QueryPreservesAnchorEndRelativeAndServerLimitedWindows()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService anchorService = new FakeEmailService();
        anchorService.QueryResponse.Position = 48;
        anchorService.QueryResponse.Ids.AddRange(new[] { "e48", "e49" });
        anchorService.QueryResponse.Total = 100;
        anchorService.QueryResponse.HasTotal = true;
        anchorService.QueryResponse.Limit = 2;
        anchorService.QueryResponse.HasLimit = true;
        EmailQueryRequest anchorRequest = new EmailQueryRequest
        {
            AccountId = "account-1",
            Anchor = "e50",
            AnchorOffset = -2,
            HasAnchorOffset = true,
            Limit = 3,
            HasLimit = true,
            CalculateTotal = true,
            HasCalculateTotal = true
        };

        JmapInvocation anchorInvocation = method.ProcessQueryInvocation(
            method.CreateQueryInvocation(anchorRequest, "q1"), QuerySession(), anchorService,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailQueryResponse anchorResponse = method.MapQueryResponse(
            Response(JmapMethods.EmailQuery, "q1", anchorInvocation.Arguments.GetRawText()),
            "q1", anchorRequest);

        FakeEmailService relativeService = new FakeEmailService();
        relativeService.QueryResponse.Position = 98;
        relativeService.QueryResponse.Ids.AddRange(new[] { "e98", "e99" });
        relativeService.QueryResponse.Limit = 2;
        relativeService.QueryResponse.HasLimit = true;
        EmailQueryRequest relativeRequest = new EmailQueryRequest
        {
            AccountId = "account-1",
            Position = -2,
            HasPosition = true,
            HasLimit = true,
            IsLimitNull = true,
            CalculateTotal = false,
            HasCalculateTotal = true
        };

        JmapInvocation relativeInvocation = method.ProcessQueryInvocation(
            method.CreateQueryInvocation(relativeRequest, "q2"), QuerySession(), relativeService,
            new JmapRequestContext(), Options(10), CancellationToken.None);
        EmailQueryResponse relativeResponse = method.MapQueryResponse(
            Response(JmapMethods.EmailQuery, "q2", relativeInvocation.Arguments.GetRawText()),
            "q2", relativeRequest);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(anchorResponse.Position, Is.EqualTo(48));
            Assert.That(anchorResponse.Ids, Is.EqualTo(new[] { "e48", "e49" }));
            Assert.That(anchorResponse.Total, Is.EqualTo(100));
            Assert.That(anchorResponse.HasTotal, Is.True);
            Assert.That(anchorResponse.Limit, Is.EqualTo(2));
            Assert.That(anchorResponse.HasLimit, Is.True);
            Assert.That(anchorService.LastQueryRequest!.Anchor, Is.EqualTo("e50"));
            Assert.That(anchorService.LastQueryRequest.AnchorOffset, Is.EqualTo(-2));
            Assert.That(anchorService.LastQueryRequest.Limit, Is.EqualTo(3));
            Assert.That(relativeResponse.Position, Is.EqualTo(98));
            Assert.That(relativeResponse.Ids, Is.EqualTo(new[] { "e98", "e99" }));
            Assert.That(relativeResponse.HasTotal, Is.False);
            Assert.That(relativeResponse.Limit, Is.EqualTo(2));
            Assert.That(relativeService.LastQueryRequest!.Position, Is.EqualTo(-2));
            Assert.That(relativeService.LastQueryRequest.HasLimit, Is.True);
            Assert.That(relativeService.LastQueryRequest.IsLimitNull, Is.True);
            Assert.That(relativeService.LastQueryRequest.HasCalculateTotal, Is.True);
            Assert.That(relativeService.LastQueryRequest.CalculateTotal, Is.False);
        }
    }

    [Test]
    public void QueryRejectsMalformedUnsupportedAndInaccessibleRequests()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        JmapInvocation malformed = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailQuery, "q1",
                "{\"accountId\":\"account-1\",\"position\":1.5}"),
            QuerySession(JmapProperties.Size), service, new JmapRequestContext(), Options(10),
            CancellationToken.None);
        JmapInvocation unsupportedFilter = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailQuery, "q2",
                "{\"accountId\":\"account-1\",\"filter\":{\"vendorFilter\":true}}"),
            QuerySession(JmapProperties.Size), service, new JmapRequestContext(), Options(10),
            CancellationToken.None);
        JmapInvocation unsupportedSort = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailQuery, "q3",
                "{\"accountId\":\"account-1\",\"sort\":[{\"property\":\"subject\"}]}"),
            QuerySession(JmapProperties.Size), service, new JmapRequestContext(), Options(10),
            CancellationToken.None);
        JmapInvocation missingAccount = method.ProcessQueryInvocation(
            RawInvocation(JmapMethods.EmailQuery, "q4", "{\"accountId\":\"account-1\"}"),
            Session(false), service, new JmapRequestContext(), Options(10), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, malformed).Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(Error(method, unsupportedFilter).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedFilter));
            Assert.That(Error(method, unsupportedSort).Kind, Is.EqualTo(JmapMethodErrorKind.UnsupportedSort));
            Assert.That(Error(method, missingAccount).Kind, Is.EqualTo(JmapMethodErrorKind.AccountNotFound));
            Assert.That(service.QueryCalls, Is.Zero);
        }
    }

    [Test]
    public void QueryChangesCarriesCollapsedThreadIdentityAndRepresentativeReplacement()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.QueryChangesResponse.Total = 2;
        service.QueryChangesResponse.HasTotal = true;
        service.QueryChangesResponse.Removed.Add("old-thread-representative");
        service.QueryChangesResponse.Added.Add(new JmapAddedItem
        {
            Id = "new-thread-representative",
            Index = 0
        });
        EmailQueryChangesRequest request = new EmailQueryChangesRequest
        {
            AccountId = "account-1",
            Filter = new EmailFilter { InMailbox = "inbox" },
            Sort = new List<EmailComparator>
            {
                new EmailComparator
                {
                    Property = JmapProperties.ReceivedAt,
                    IsAscending = false,
                    HasIsAscending = true
                }
            },
            SinceQueryState = "old-query-state",
            MaxChanges = 2,
            HasMaxChanges = true,
            UpToId = "cached-email",
            CalculateTotal = true,
            HasCalculateTotal = true,
            CollapseThreads = true,
            HasCollapseThreads = true
        };

        JmapInvocation invocation = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(request, "qc1"),
            QuerySession(JmapProperties.ReceivedAt), service, new JmapRequestContext(), Options(10),
            CancellationToken.None);
        EmailQueryChangesResponse response = method.QueryCodec.DecodeEmailQueryChangesResponse(invocation.Arguments);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.OldQueryState, Is.EqualTo("old-query-state"));
            Assert.That(response.NewQueryState, Is.EqualTo("new-query-state"));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Total, Is.EqualTo(2));
            Assert.That(response.Removed, Is.EqualTo(new[] { "old-thread-representative" }));
            Assert.That(response.Added[0].Id, Is.EqualTo("new-thread-representative"));
            Assert.That(response.Added[0].Index, Is.Zero);
            Assert.That(service.QueryChangesCalls, Is.EqualTo(1));
            Assert.That(service.LastQueryChangesRequest!.CollapseThreads, Is.True);
            Assert.That(service.LastQueryChangesRequest.HasCollapseThreads, Is.True);
            Assert.That(service.LastQueryChangesRequest.UpToId, Is.EqualTo("cached-email"));
            Assert.That(service.LastQueryChangesRequest.Filter!.InMailbox, Is.EqualTo("inbox"));
            Assert.That(service.LastQueryChangesRequest.Sort![0].Property,
                Is.EqualTo(JmapProperties.ReceivedAt));
            Assert.That(service.LastQueryChangesRequest.Sort[0].IsAscending, Is.False);
            Assert.That(service.LastQueryChangesRequest.MaxChanges, Is.EqualTo(2));
            Assert.That(service.LastQueryChangesRequest.CalculateTotal, Is.True);
        }
    }

    [Test]
    public void QueryChangesPreservesAdditionsRemovalsMovesAndMixedFilterIdentity()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.QueryChangesResponse.HasTotal = true;
        service.QueryChangesResponse.Total = 5;
        service.QueryChangesResponse.Removed.Add("deleted");
        service.QueryChangesResponse.Removed.Add("moved");
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "inserted", Index = 1 });
        service.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "moved", Index = 3 });
        EmailQueryChangesRequest request = new EmailQueryChangesRequest
        {
            AccountId = "account-1",
            Filter = new EmailFilter
            {
                Before = "2026-09-11T12:00:00Z",
                HasKeyword = "$flagged"
            },
            Sort = new List<EmailComparator>
            {
                new EmailComparator { Property = JmapProperties.ReceivedAt }
            },
            SinceQueryState = "old-query-state",
            MaxChanges = 4,
            HasMaxChanges = true,
            UpToId = "cached-email",
            CalculateTotal = true,
            HasCalculateTotal = true,
            CollapseThreads = false,
            HasCollapseThreads = true
        };

        JmapInvocation invocation = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(request, "qc1"),
            QuerySession(JmapProperties.ReceivedAt), service, new JmapRequestContext(), Options(10),
            CancellationToken.None);
        EmailQueryChangesResponse response = method.MapQueryChangesResponse(
            Response(JmapMethods.EmailQueryChanges, "qc1", invocation.Arguments.GetRawText()),
            "qc1", request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Removed, Is.EqualTo(new[] { "deleted", "moved" }));
            Assert.That(response.Added.Select(item => item.Id),
                Is.EqualTo(new[] { "inserted", "moved" }));
            Assert.That(response.Added.Select(item => item.Index), Is.EqualTo(new long[] { 1, 3 }));
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Total, Is.EqualTo(5));
            Assert.That(service.LastQueryChangesRequest!.Filter!.Before,
                Is.EqualTo("2026-09-11T12:00:00Z"));
            Assert.That(service.LastQueryChangesRequest.Filter.HasKeyword, Is.EqualTo("$flagged"));
            Assert.That(service.LastQueryChangesRequest.UpToId, Is.EqualTo("cached-email"));
            Assert.That(service.LastQueryChangesRequest.CollapseThreads, Is.False);
            Assert.That(service.LastQueryChangesRequest.HasCollapseThreads, Is.True);
        }
    }

    [Test]
    public void QueryChangesCountsBothSidesOfAMoveAndRejectsInvalidBackendChanges()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService oversizedService = new FakeEmailService();
        oversizedService.QueryChangesResponse.Removed.Add("moved");
        oversizedService.QueryChangesResponse.Added.Add(
            new JmapAddedItem { Id = "moved", Index = 0 });
        EmailQueryChangesRequest limited = QueryChangesRequest();
        limited.MaxChanges = 1;
        limited.HasMaxChanges = true;
        JmapInvocation oversized = method.ProcessQueryChangesInvocation(
            method.CreateQueryChangesInvocation(limited, "qc1"), QuerySession(), oversizedService,
            new JmapRequestContext(), Options(10), CancellationToken.None);

        FakeEmailService invalidService = new FakeEmailService();
        invalidService.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "e2", Index = 2 });
        invalidService.QueryChangesResponse.Added.Add(new JmapAddedItem { Id = "e1", Index = 1 });
        JmapProtocolException invalid = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryChangesInvocation(
                method.CreateQueryChangesInvocation(QueryChangesRequest(), "qc2"), QuerySession(), invalidService,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Error(method, oversized).Kind, Is.EqualTo(JmapMethodErrorKind.TooManyChanges));
            Assert.That(invalid.Path, Is.EqualTo("added/1"));
        }
    }

    [Test]
    public void QueryChangesRejectsAStaleBackendState()
    {
        EmailMethod method = new EmailMethod();
        FakeEmailService service = new FakeEmailService();
        service.QueryChangesResponse.OldQueryState = "different-query-state";

        JmapProtocolException error = Assert.Throws<JmapProtocolException>(() =>
            method.ProcessQueryChangesInvocation(
                method.CreateQueryChangesInvocation(QueryChangesRequest(), "qc1"), QuerySession(), service,
                new JmapRequestContext(), Options(10), CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Path, Is.EqualTo("methodResponses/1"));
            Assert.That(service.QueryChangesCalls, Is.EqualTo(1));
        }
    }

    [Test]
    public void ClientMappingHandlesQueryResultsMovesAndStandardFailures()
    {
        EmailMethod method = new EmailMethod();
        EmailQueryRequest queryRequest = new EmailQueryRequest { AccountId = "account-1" };
        JmapResponse queryWire = Response(
            JmapMethods.EmailQuery,
            "q1",
            "{\"accountId\":\"account-1\",\"queryState\":\"q1\",\"canCalculateChanges\":true,\"position\":0,\"ids\":[\"e1\"]}");
        EmailQueryResponse query = method.MapQueryResponse(queryWire, "q1", queryRequest);
        EmailQueryChangesRequest changesRequest = QueryChangesRequest();
        JmapResponse changesWire = Response(
            JmapMethods.EmailQueryChanges,
            "qc1",
            "{\"accountId\":\"account-1\",\"oldQueryState\":\"old-query-state\",\"newQueryState\":\"new-query-state\",\"removed\":[\"e1\"],\"added\":[{\"id\":\"e1\",\"index\":1}]}" );
        EmailQueryChangesResponse changes = method.MapQueryChangesResponse(changesWire, "qc1", changesRequest);
        JmapResponse failed = Response(
            ResponseMapper.ErrorResponseName,
            "qc2",
            "{\"type\":\"cannotCalculateChanges\",\"description\":\"expired\"}");
        JmapMethodException error = Assert.Throws<JmapMethodException>(() =>
            method.MapQueryChangesResponse(failed, "qc2", changesRequest))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(query.Ids, Is.EqualTo(new[] { "e1" }));
            Assert.That(query.CanCalculateChanges, Is.True);
            Assert.That(changes.Removed, Is.EqualTo(new[] { "e1" }));
            Assert.That(changes.Added[0].Id, Is.EqualTo("e1"));
            Assert.That(changes.Added[0].Index, Is.EqualTo(1));
            Assert.That(error.Error!.Kind, Is.EqualTo(JmapMethodErrorKind.CannotCalculateChanges));
            Assert.That(error.Error.Description, Is.EqualTo("expired"));
        }
    }

    private static Email StoredEmail(string id)
    {
        EmailBodyPart text = TextPart("1", "text/plain");
        EmailBodyPart html = TextPart("2", "text/html");
        EmailBodyPart root = new EmailBodyPart
        {
            Size = 20,
            HasSize = true,
            Headers = new List<EmailHeader>(),
            Type = "multipart/alternative",
            SubParts = new List<EmailBodyPart> { text, html }
        };
        return new Email
        {
            Id = id,
            BlobId = "blob-" + id,
            ThreadId = "thread-1",
            MailboxIds = new Dictionary<string, bool> { ["inbox"] = true },
            Keywords = new Dictionary<string, bool>(),
            Size = 20,
            ReceivedAt = "2026-09-11T12:00:00Z",
            Headers = new List<EmailHeader> { Header("Subject", "Example") },
            Subject = "Example",
            BodyStructure = root,
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["1"] = BodyValue("plain text"),
                ["2"] = BodyValue("<p>html</p>")
            },
            TextBody = new List<EmailBodyPart> { text },
            HtmlBody = new List<EmailBodyPart> { html },
            Attachments = new List<EmailBodyPart>(),
            Preview = "Example preview"
        };
    }

    private static Email ProjectionEmail()
    {
        Email email = StoredEmail("projection-email");
        email.Headers = new List<EmailHeader>
        {
            Header("X-Raw", " raw one"),
            Header("X-Raw", " raw two"),
            Header("Subject", " First subject"),
            Header("Subject", " =?UTF-8?B?5pel5pys6Kqe?="),
            Header("To", " One <one@example.test>"),
            Header("To", " Two <two@example.test>"),
            Header("From", " First Team: first@example.test;"),
            Header("From", " Second Team: second@example.test;"),
            Header("References", " <root@example.test>"),
            Header("References", " <next@example.test>"),
            Header("Date", " Fri, 11 Sep 2026 14:05:06 -0700"),
            Header("Date", " Sat, 12 Sep 2026 10:00:00 +0000"),
            Header("List-Help", " <https://example.test/first>"),
            Header("List-Help", " <mailto:help@example.test>")
        };
        email.MessageId = new List<string> { "message@example.test" };
        email.InReplyTo = new List<string> { "parent@example.test" };
        email.References = new List<string> { "root@example.test", "parent@example.test" };
        email.Sender = Addresses("sender@example.test");
        email.From = Addresses("from@example.test");
        email.To = Addresses("to@example.test");
        email.Cc = Addresses("cc@example.test");
        email.Bcc = new List<EmailAddress>();
        email.ReplyTo = Addresses("reply@example.test");
        email.SentAt = "2026-09-11T14:05:06-07:00";
        email.HasAttachment = true;

        EmailBodyPart root = email.BodyStructure!;
        AddPartHeaders(root);
        root.Language = new List<string> { "en" };
        root.Location = "message/body";
        foreach (EmailBodyPart part in root.SubParts!)
        {
            AddPartHeaders(part);
            part.Name = "part-" + part.PartId + ".txt";
            part.Disposition = "inline";
            part.Cid = "part-" + part.PartId + "@example.test";
            part.Language = new List<string> { "en", "fr" };
            part.Location = "parts/" + part.PartId;
        }

        EmailBodyPart attachment = TextPart("3", "text/csv");
        AddPartHeaders(attachment);
        attachment.Name = "values.csv";
        attachment.Disposition = "attachment";
        attachment.Cid = "part-3@example.test";
        attachment.Language = new List<string> { "en" };
        attachment.Location = "files/values.csv";
        root.SubParts.Add(attachment);
        email.BodyValues!.Add("3", BodyValue("one,two"));
        email.Attachments!.Add(attachment);
        return email;
    }

    private static List<EmailAddress> Addresses(string address)
    {
        return new List<EmailAddress>
        {
            new EmailAddress { Name = "Person", Email = address }
        };
    }

    private static void AddPartHeaders(EmailBodyPart part)
    {
        part.Headers!.Add(Header("X-Part", " first part value"));
        part.Headers.Add(Header("X-Part", " second part value"));
    }

    private static EmailChangesRequest ChangesRequest()
    {
        EmailChangesRequest request = new EmailChangesRequest();
        request.AccountId = "account-1";
        request.SinceState = "old-state";
        return request;
    }

    private static Email DraftEmail()
    {
        EmailBodyPart text = new EmailBodyPart { PartId = "text", Type = "text/plain" };
        EmailBodyPart attachment = new EmailBodyPart
        {
            BlobId = "upload-blob",
            Type = "application/pdf",
            Name = "draft.pdf",
            Disposition = "attachment"
        };
        return new Email
        {
            MailboxIds = new Dictionary<string, bool> { ["drafts"] = true },
            Keywords = new Dictionary<string, bool> { [JmapMailKeywords.Draft] = true },
            Subject = "Draft subject",
            TextBody = new List<EmailBodyPart> { text },
            Attachments = new List<EmailBodyPart> { attachment },
            BodyValues = new Dictionary<string, EmailBodyValue>
            {
                ["text"] = new EmailBodyValue { Value = "Hello from a draft." }
            }
        };
    }

    private static Email CopyEmail(string id, string mailboxId, string keyword)
    {
        return new Email
        {
            Id = id,
            MailboxIds = new Dictionary<string, bool> { [mailboxId] = true },
            Keywords = new Dictionary<string, bool> { [keyword] = true },
            ReceivedAt = "2026-09-11T12:00:00Z"
        };
    }

    private static EmailCopyRequest CopyRequest()
    {
        return new EmailCopyRequest
        {
            FromAccountId = "source-account",
            AccountId = "account-1",
            Create = new Dictionary<string, Email>
            {
                ["copy-1"] = CopyEmail("source-1", "inbox", JmapMailKeywords.Seen)
            }
        };
    }

    private static JmapSetError CopyError(string type)
    {
        return new JmapSetError { Type = type };
    }

    private static JmapSetError SetFailure(string type)
    {
        return new JmapSetError { Type = type };
    }

    private static JmapSetError SetFailureWithProperties(string type, string property)
    {
        return new JmapSetError
        {
            Type = type,
            Properties = new List<string> { property }
        };
    }

    private static JmapSetError SetFailureWithMissingBlobs(string blobId)
    {
        return new JmapSetError
        {
            Type = JmapErrorCodes.BlobNotFound,
            NotFound = new List<string> { blobId }
        };
    }

    private static EmailSetResponse FailedSetCreate(string creationId, JmapSetError error)
    {
        return new EmailSetResponse
        {
            AccountId = "account-1",
            NewState = "email-state-2",
            NotCreated = new Dictionary<string, JmapSetError> { [creationId] = error }
        };
    }

    private static FakeEmailService CopyService()
    {
        FakeEmailService service = new FakeEmailService();
        service.AccountStates.Add("source-account", "source-state");
        service.AccountStates.Add("account-1", "email-state");
        service.CopyResponse.FromAccountId = "source-account";
        service.CopyResponse.AccountId = "account-1";
        service.CopyResponse.OldState = "email-state";
        service.CopyResponse.NewState = "target-state-2";
        return service;
    }

    private static EmailImport ImportEmail(string blobId, string mailboxId, string keyword)
    {
        return new EmailImport
        {
            BlobId = blobId,
            MailboxIds = new Dictionary<string, bool> { [mailboxId] = true },
            Keywords = new Dictionary<string, bool> { [keyword] = true },
            ReceivedAt = "2026-09-11T12:00:00Z"
        };
    }

    private static EmailImportRequest ImportRequest()
    {
        return new EmailImportRequest
        {
            AccountId = "account-1",
            Emails = new Dictionary<string, EmailImport>
            {
                ["created"] = ImportEmail("upload-blob", "inbox", JmapMailKeywords.Seen)
            }
        };
    }

    private static JmapSetError ImportError(string type, List<string>? properties, string? existingId)
    {
        return new JmapSetError { Type = type, Properties = properties, ExistingId = existingId };
    }

    private static FakeEmailService ImportService()
    {
        FakeEmailService service = new FakeEmailService();
        service.AccountStates.Add("account-1", "email-state");
        service.ImportResponse.AccountId = "account-1";
        service.ImportResponse.OldState = "email-state";
        service.ImportResponse.NewState = "import-state-2";
        return service;
    }

    private static Email ParsedEmail(string blobId)
    {
        Email email = StoredEmail("parsed");
        email.Id = null;
        email.BlobId = blobId;
        email.MailboxIds = null;
        email.Keywords = null;
        email.ReceivedAt = null;
        return email;
    }

    private static EmailParseRequest ParseRequest()
    {
        return new EmailParseRequest
        {
            AccountId = "account-1",
            BlobIds = new List<string> { "blob-1" }
        };
    }

    private static FakeEmailService ParseService()
    {
        FakeEmailService service = new FakeEmailService();
        service.ParseResponse.AccountId = "account-1";
        return service;
    }

    private static JmapPatchObject Patch(params (string Path, string JsonValue)[] values)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string jsonValue) in values)
        {
            patch.Values.Add(path, Json(jsonValue));
        }

        return patch;
    }

    private static EmailQueryChangesRequest QueryChangesRequest()
    {
        EmailQueryChangesRequest request = new EmailQueryChangesRequest();
        request.AccountId = "account-1";
        request.SinceQueryState = "old-query-state";
        return request;
    }

    private static EmailBodyPart TextPart(string partId, string type)
    {
        return new EmailBodyPart
        {
            PartId = partId,
            BlobId = "blob-part-" + partId,
            Size = 10,
            HasSize = true,
            Headers = new List<EmailHeader>(),
            Type = type,
            Charset = "utf-8"
        };
    }

    private static EmailBodyValue BodyValue(string value)
    {
        return new EmailBodyValue { Value = value };
    }

    private static EmailHeader Header(string name, string value)
    {
        return new EmailHeader { Name = name, Value = value };
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

    private static JmapSession CopySession(bool includeSource, bool includeTarget)
    {
        JmapSession session = new JmapSession();
        if (includeSource)
        {
            AddMailAccount(session, "source-account");
        }

        if (includeTarget)
        {
            AddMailAccount(session, "account-1");
        }

        return session;
    }

    private static void AddMailAccount(JmapSession session, string accountId)
    {
        JmapAccount account = new JmapAccount();
        JmapCapability mail = new JmapCapability();
        mail.Mail = new JmapMailCapabilities();
        account.AccountCapabilities.Add(JmapCapabilities.Mail, mail);
        session.Accounts.Add(accountId, account);
    }

    private static JmapSession QuerySession(params string[] sortProperties)
    {
        JmapSession session = Session(true);
        JmapMailCapabilities mail = session.Accounts["account-1"]
            .AccountCapabilities[JmapCapabilities.Mail].Mail!;
        mail.EmailQuerySortOptions.AddRange(sortProperties);
        JmapCapability core = new JmapCapability();
        core.Core = new JmapCoreCapabilities();
        core.Core.CollationAlgorithms.Add("i;ascii-casemap");
        session.Capabilities.Add(JmapCapabilities.Core, core);
        return session;
    }

    private static JmapServerOptions Options(int maximumObjectsInGet)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxObjectsInGet = maximumObjectsInGet;
        return options;
    }

    private static JmapInvocation RawInvocation(string name, string callId, string arguments)
    {
        JmapInvocation invocation = new JmapInvocation();
        invocation.Name = name;
        invocation.CallId = callId;
        invocation.Arguments = Json(arguments);
        return invocation;
    }

    private static JmapResponse Response(string name, string callId, string arguments)
    {
        JmapResponse response = new JmapResponse();
        response.MethodResponses.Add(RawInvocation(name, callId, arguments));
        return response;
    }

    private static JmapMethodError Error(EmailMethod method, JmapInvocation invocation)
    {
        Assert.That(invocation.Name, Is.EqualTo(ResponseMapper.ErrorResponseName));
        return method.Protocol.DecodeMethodError(invocation.Arguments);
    }

    private static JsonElement Json(string value)
    {
        return JsonDocument.Parse(value).RootElement.Clone();
    }

    private class FakeEmailService : IEmailService
    {
        public EmailGetResponse GetResponse = new EmailGetResponse
        {
            AccountId = "account-1",
            State = "email-state"
        };

        public EmailChangesResponse ChangesResponse = new EmailChangesResponse
        {
            AccountId = "account-1",
            OldState = "old-state",
            NewState = "new-state"
        };

        public EmailQueryResponse QueryResponse = new EmailQueryResponse
        {
            AccountId = "account-1",
            QueryState = "query-state",
            CanCalculateChanges = true
        };

        public EmailQueryChangesResponse QueryChangesResponse = new EmailQueryChangesResponse
        {
            AccountId = "account-1",
            OldQueryState = "old-query-state",
            NewQueryState = "new-query-state"
        };

        public EmailSetResponse SetResponse = new EmailSetResponse
        {
            AccountId = "account-1",
            OldState = "email-state",
            NewState = "email-state-2"
        };

        public EmailCopyResponse CopyResponse = new EmailCopyResponse();
        public EmailImportResponse ImportResponse = new EmailImportResponse();
        public EmailParseResponse ParseResponse = new EmailParseResponse();
        public Dictionary<string, string> AccountStates = new Dictionary<string, string>();
        public List<string> GetAccounts = new List<string>();

        public int GetCalls;
        public int ChangesCalls;
        public int QueryCalls;
        public int QueryChangesCalls;
        public int SetCalls;
        public int CopyCalls;
        public int ImportCalls;
        public int ParseCalls;
        public EmailGetRequest? LastGetRequest;
        public EmailChangesRequest? LastChangesRequest;
        public EmailQueryRequest? LastQueryRequest;
        public EmailQueryChangesRequest? LastQueryChangesRequest;
        public EmailSetRequest? LastSetRequest;
        public EmailCopyRequest? LastCopyRequest;
        public EmailImportRequest? LastImportRequest;
        public EmailParseRequest? LastParseRequest;

        public Task<EmailGetResponse> GetStoredEmailsAsync(
            EmailGetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            GetCalls = GetCalls + 1;
            LastGetRequest = request;
            if (request.AccountId != null && AccountStates.ContainsKey(request.AccountId))
            {
                GetAccounts.Add(request.AccountId);
                EmailGetResponse response = new EmailGetResponse();
                response.AccountId = request.AccountId;
                response.State = AccountStates[request.AccountId];
                return Task.FromResult(response);
            }

            return Task.FromResult(GetResponse);
        }

        public Task<EmailChangesResponse> GetStoredEmailChangesAsync(
            EmailChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ChangesCalls = ChangesCalls + 1;
            LastChangesRequest = request;
            return Task.FromResult(ChangesResponse);
        }

        public Task<EmailQueryResponse> QueryStoredEmailsAsync(
            EmailQueryRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            QueryCalls = QueryCalls + 1;
            LastQueryRequest = request;
            return Task.FromResult(QueryResponse);
        }

        public Task<EmailQueryChangesResponse> GetStoredEmailQueryChangesAsync(
            EmailQueryChangesRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            QueryChangesCalls = QueryChangesCalls + 1;
            LastQueryChangesRequest = request;
            return Task.FromResult(QueryChangesResponse);
        }

        public Task<EmailSetResponse> SetStoredEmailsAsync(
            EmailSetRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            SetCalls = SetCalls + 1;
            LastSetRequest = request;
            return Task.FromResult(SetResponse);
        }

        public Task<EmailCopyResponse> CopyStoredEmailsAsync(
            EmailCopyRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            CopyCalls = CopyCalls + 1;
            LastCopyRequest = request;
            return Task.FromResult(CopyResponse);
        }

        public Task<EmailImportResponse> ImportStoredEmailsAsync(
            EmailImportRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ImportCalls = ImportCalls + 1;
            LastImportRequest = request;
            return Task.FromResult(ImportResponse);
        }

        public Task<EmailParseResponse> ParseStoredEmailBlobsAsync(
            EmailParseRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            ParseCalls = ParseCalls + 1;
            LastParseRequest = request;
            return Task.FromResult(ParseResponse);
        }
    }
}
