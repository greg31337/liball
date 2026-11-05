using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class SessionJsonCodecTests
{
    [Test]
    public void DecodeSessionPopulatesTypedAndOpaqueCapabilities()
    {
        JmapSession session = Decode(FullSessionJson());
        JmapCoreCapabilities core = session.Capabilities[JmapCapabilities.Core].Core!;
        JmapAccount account = session.Accounts["account-1"];
        JmapMailCapabilities mail = account.AccountCapabilities[JmapCapabilities.Mail].Mail!;
        JmapSubmissionCapabilities submission = account.AccountCapabilities[JmapCapabilities.Submission].Submission!;
        JmapVacationResponseCapabilities vacation = account.AccountCapabilities[JmapCapabilities.VacationResponse].VacationResponse!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Username, Is.EqualTo("user@example.test"));
            Assert.That(session.ApiUrl, Is.EqualTo("https://example.test/api"));
            Assert.That(session.PrimaryAccounts[JmapCapabilities.Mail], Is.EqualTo("account-1"));
            Assert.That(session.UnknownProperties["vendorSession"].GetProperty("enabled").GetBoolean(), Is.True);

            Assert.That(core.MaxSizeUpload, Is.EqualTo(50000000));
            Assert.That(core.MaxConcurrentUpload, Is.EqualTo(8));
            Assert.That(core.MaxSizeRequest, Is.EqualTo(10000000));
            Assert.That(core.MaxConcurrentRequests, Is.EqualTo(4));
            Assert.That(core.MaxCallsInRequest, Is.EqualTo(16));
            Assert.That(core.MaxObjectsInGet, Is.EqualTo(500));
            Assert.That(core.MaxObjectsInSet, Is.EqualTo(500));
            Assert.That(core.CollationAlgorithms, Is.EqualTo(new[] { "i;ascii-casemap", "i;unicode-casemap" }));
            Assert.That(core.UnknownProperties["vendorCore"].GetString(), Is.EqualTo("kept"));

            Assert.That(session.Capabilities[JmapCapabilities.Mail].Mail, Is.Null);
            Assert.That(session.Capabilities["https://example.test/capability/widgets"].UnknownProperties["maxWidgets"].GetInt32(), Is.EqualTo(42));

            Assert.That(account.Name, Is.EqualTo("Personal"));
            Assert.That(account.IsPersonal, Is.True);
            Assert.That(account.IsReadOnly, Is.False);
            Assert.That(account.UnknownProperties["vendorAccount"].GetString(), Is.EqualTo("account-data"));

            Assert.That(mail.HasMaxMailboxesPerEmail, Is.True);
            Assert.That(mail.IsMaxMailboxesPerEmailNull, Is.True);
            Assert.That(mail.HasMaxMailboxDepth, Is.True);
            Assert.That(mail.IsMaxMailboxDepthNull, Is.False);
            Assert.That(mail.MaxMailboxDepth, Is.EqualTo(10));
            Assert.That(mail.MaxSizeMailboxName, Is.EqualTo(255));
            Assert.That(mail.MaxSizeAttachmentsPerEmail, Is.EqualTo(50000000));
            Assert.That(mail.EmailQuerySortOptions, Is.EqualTo(new[] { JmapProperties.ReceivedAt, JmapProperties.Size }));
            Assert.That(mail.MayCreateTopLevelMailbox, Is.True);
            Assert.That(mail.UnknownProperties["vendorMail"].GetInt32(), Is.EqualTo(7));

            Assert.That(submission.MaxDelayedSend, Is.EqualTo(3600));
            Assert.That(submission.SubmissionExtensions["FUTURERELEASE"], Is.EqualTo(new[] { "T", "+" }));
            Assert.That(submission.SubmissionExtensions["SIZE"], Is.Empty);
            Assert.That(submission.UnknownProperties["vendorSubmission"].GetBoolean(), Is.True);

            Assert.That(vacation.UnknownProperties["vendorVacation"].GetString(), Is.EqualTo("yes"));
            Assert.That(account.AccountCapabilities["https://example.test/capability/account-widget"].UnknownProperties["level"].GetInt32(), Is.EqualTo(3));
        }
    }

    [Test]
    public void MailNullableLimitsPreserveMissingNullAndNumericForms()
    {
        string prefix = "{\"maxSizeMailboxName\":100,\"maxSizeAttachmentsPerEmail\":1000,\"emailQuerySortOptions\":[],\"mayCreateTopLevelMailbox\":false";
        SessionJsonCodec codec = Codec();

        JmapMailCapabilities missing = codec.DecodeMailCapability(Json(prefix + "}"), "/mail");
        JmapMailCapabilities explicitNull = codec.DecodeMailCapability(Json(prefix + ",\"maxMailboxesPerEmail\":null,\"maxMailboxDepth\":null}"), "/mail");
        JmapMailCapabilities numeric = codec.DecodeMailCapability(Json(prefix + ",\"maxMailboxesPerEmail\":5,\"maxMailboxDepth\":12}"), "/mail");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.HasMaxMailboxesPerEmail, Is.False);
            Assert.That(missing.HasMaxMailboxDepth, Is.False);
            Assert.That(explicitNull.HasMaxMailboxesPerEmail, Is.True);
            Assert.That(explicitNull.IsMaxMailboxesPerEmailNull, Is.True);
            Assert.That(explicitNull.HasMaxMailboxDepth, Is.True);
            Assert.That(explicitNull.IsMaxMailboxDepthNull, Is.True);
            Assert.That(numeric.HasMaxMailboxesPerEmail, Is.True);
            Assert.That(numeric.IsMaxMailboxesPerEmailNull, Is.False);
            Assert.That(numeric.MaxMailboxesPerEmail, Is.EqualTo(5));
            Assert.That(numeric.HasMaxMailboxDepth, Is.True);
            Assert.That(numeric.MaxMailboxDepth, Is.EqualTo(12));
        }
    }

    [Test]
    public void UnknownSessionAccountAndCapabilityDataSurvivesSourceDisposal()
    {
        SessionJsonCodec codec = Codec();
        JmapSession session;

        using (JsonDocument document = JsonDocument.Parse(FullSessionJson()))
        {
            session = codec.DecodeSession(document.RootElement);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.UnknownProperties["vendorSession"].GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(session.Accounts["account-1"].UnknownProperties["vendorAccount"].GetString(), Is.EqualTo("account-data"));
            Assert.That(session.Capabilities["https://example.test/capability/widgets"].UnknownProperties["nested"].GetProperty("code").GetInt32(), Is.EqualTo(17));
        }
    }

    [Test]
    public void EncodeSessionUsesStableFieldAndMapOrdering()
    {
        JmapCoreCapabilities core = new JmapCoreCapabilities();
        core.CollationAlgorithms.Add("i;unicode-casemap");
        JmapCapability coreCapability = new JmapCapability();
        coreCapability.Core = core;
        JmapCapability zCapability = new JmapCapability();
        zCapability.UnknownProperties.Add("z", Json("2"));
        zCapability.UnknownProperties.Add("a", Json("1"));

        JmapSession session = new JmapSession();
        session.Capabilities.Add("https://z.example/capability", zCapability);
        session.Capabilities.Add(JmapCapabilities.Core, coreCapability);
        session.Username = "user";
        session.ApiUrl = "https://example.test/api";
        session.DownloadUrl = "https://example.test/download/{accountId}/{blobId}/{name}?type={type}";
        session.UploadUrl = "https://example.test/upload/{accountId}";
        session.EventSourceUrl = "https://example.test/events?types={types}&closeafter={closeafter}&ping={ping}";
        session.State = "s1";
        session.UnknownProperties.Add("zVendor", Json("true"));
        session.UnknownProperties.Add("aVendor", Json("null"));

        byte[] first = Encode(session);
        byte[] second = Encode(session);
        string json = Encoding.UTF8.GetString(first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(json.IndexOf("https://z.example/capability", StringComparison.Ordinal), Is.LessThan(json.IndexOf(JmapCapabilities.Core, StringComparison.Ordinal)));
            Assert.That(json.IndexOf("\"a\":1", StringComparison.Ordinal), Is.LessThan(json.IndexOf("\"z\":2", StringComparison.Ordinal)));
            Assert.That(json.IndexOf("\"aVendor\"", StringComparison.Ordinal), Is.LessThan(json.IndexOf("\"zVendor\"", StringComparison.Ordinal)));
        }

        JmapSession roundTrip = Decode(json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.State, Is.EqualTo("s1"));
            Assert.That(roundTrip.Capabilities[JmapCapabilities.Core].Core!.CollationAlgorithms, Is.EqualTo(new[] { "i;unicode-casemap" }));
            Assert.That(roundTrip.Capabilities["https://z.example/capability"].UnknownProperties["a"].GetInt32(), Is.EqualTo(1));
            Assert.That(roundTrip.UnknownProperties["aVendor"].ValueKind, Is.EqualTo(JsonValueKind.Null));
        }
    }

    [Test]
    public void FullSessionRoundTripPreservesTypedOptionalAndExtensionData()
    {
        JmapSession first = Decode(FullSessionJson());
        JmapSession second = Decode(Encoding.UTF8.GetString(Encode(first)));
        JmapMailCapabilities mail = second.Accounts["account-1"].AccountCapabilities[JmapCapabilities.Mail].Mail!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.Username, Is.EqualTo(first.Username));
            Assert.That(second.PrimaryAccounts, Is.EqualTo(first.PrimaryAccounts));
            Assert.That(mail.HasMaxMailboxesPerEmail, Is.True);
            Assert.That(mail.IsMaxMailboxesPerEmailNull, Is.True);
            Assert.That(mail.MaxMailboxDepth, Is.EqualTo(10));
            Assert.That(mail.UnknownProperties["vendorMail"].GetInt32(), Is.EqualTo(7));
            Assert.That(second.Capabilities["https://example.test/capability/widgets"].UnknownProperties["nested"].GetProperty("code").GetInt32(), Is.EqualTo(17));
        }
    }

    [Test]
    public void DecodeRejectsMissingWrongTypeAndNegativeCapabilityValues()
    {
        SessionJsonCodec codec = Codec();
        JsonElement missingUsername = Json("{\"capabilities\":{},\"accounts\":{},\"primaryAccounts\":{},\"apiUrl\":\"a\",\"downloadUrl\":\"d\",\"uploadUrl\":\"u\",\"eventSourceUrl\":\"e\",\"state\":\"s\"}");
        JsonElement wrongAccounts = Json("{\"capabilities\":{},\"accounts\":[],\"primaryAccounts\":{},\"username\":\"u\",\"apiUrl\":\"a\",\"downloadUrl\":\"d\",\"uploadUrl\":\"u\",\"eventSourceUrl\":\"e\",\"state\":\"s\"}");
        JsonElement negativeCore = Json("{\"maxSizeUpload\":-1,\"maxConcurrentUpload\":1,\"maxSizeRequest\":1,\"maxConcurrentRequests\":1,\"maxCallsInRequest\":1,\"maxObjectsInGet\":1,\"maxObjectsInSet\":1,\"collationAlgorithms\":[]}");

        JmapProtocolException missing = Assert.Throws<JmapProtocolException>(() => codec.DecodeSession(missingUsername))!;
        JmapProtocolException wrong = Assert.Throws<JmapProtocolException>(() => codec.DecodeSession(wrongAccounts))!;
        JmapProtocolException negative = Assert.Throws<JmapProtocolException>(() => codec.DecodeCoreCapability(negativeCore, "/core"))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Path, Is.EqualTo("/username"));
            Assert.That(wrong.Path, Is.EqualTo("/accounts"));
            Assert.That(negative.Path, Is.EqualTo("/core/maxSizeUpload"));
        }
    }

    [Test]
    public void EncodeRejectsMissingRequiredAndOutOfRangeValues()
    {
        JmapSession missingUsername = MinimalSession();
        missingUsername.Username = null;

        JmapSession invalidCoreSession = MinimalSession();
        JmapCoreCapabilities core = new JmapCoreCapabilities();
        core.MaxSizeUpload = -1;
        JmapCapability capability = new JmapCapability();
        capability.Core = core;
        invalidCoreSession.Capabilities.Add(JmapCapabilities.Core, capability);

        Assert.That(() => Encode(missingUsername), Throws.TypeOf<JmapValidationException>());
        Assert.That(() => Encode(invalidCoreSession), Throws.TypeOf<JmapValidationException>());
    }

    private static JmapSession MinimalSession()
    {
        JmapSession session = new JmapSession();
        session.Username = "user";
        session.ApiUrl = "https://example.test/api";
        session.DownloadUrl = "https://example.test/download/{accountId}/{blobId}/{name}?type={type}";
        session.UploadUrl = "https://example.test/upload/{accountId}";
        session.EventSourceUrl = "https://example.test/events?types={types}&closeafter={closeafter}&ping={ping}";
        session.State = "s1";
        return session;
    }

    private static SessionJsonCodec Codec()
    {
        return new SessionJsonCodec(new JmapJsonReader(100000, 32));
    }

    private static JmapSession Decode(string json)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 32);
        SessionJsonCodec codec = new SessionJsonCodec(reader);
        return codec.DecodeSession(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static byte[] Encode(JmapSession session)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeSession(session, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }

    private static string FullSessionJson()
    {
        return """
            {
              "capabilities": {
                "urn:ietf:params:jmap:core": {
                  "maxSizeUpload": 50000000,
                  "maxConcurrentUpload": 8,
                  "maxSizeRequest": 10000000,
                  "maxConcurrentRequests": 4,
                  "maxCallsInRequest": 16,
                  "maxObjectsInGet": 500,
                  "maxObjectsInSet": 500,
                  "collationAlgorithms": ["i;ascii-casemap", "i;unicode-casemap"],
                  "vendorCore": "kept"
                },
                "urn:ietf:params:jmap:mail": {},
                "urn:ietf:params:jmap:submission": {},
                "urn:ietf:params:jmap:vacationresponse": {},
                "https://example.test/capability/widgets": {
                  "maxWidgets": 42,
                  "nested": {"code": 17}
                }
              },
              "accounts": {
                "account-1": {
                  "name": "Personal",
                  "isPersonal": true,
                  "isReadOnly": false,
                  "accountCapabilities": {
                    "urn:ietf:params:jmap:mail": {
                      "maxMailboxesPerEmail": null,
                      "maxMailboxDepth": 10,
                      "maxSizeMailboxName": 255,
                      "maxSizeAttachmentsPerEmail": 50000000,
                      "emailQuerySortOptions": ["receivedAt", "size"],
                      "mayCreateTopLevelMailbox": true,
                      "vendorMail": 7
                    },
                    "urn:ietf:params:jmap:submission": {
                      "maxDelayedSend": 3600,
                      "submissionExtensions": {
                        "FUTURERELEASE": ["T", "+"],
                        "SIZE": []
                      },
                      "vendorSubmission": true
                    },
                    "urn:ietf:params:jmap:vacationresponse": {
                      "vendorVacation": "yes"
                    },
                    "https://example.test/capability/account-widget": {
                      "level": 3
                    }
                  },
                  "vendorAccount": "account-data"
                }
              },
              "primaryAccounts": {
                "urn:ietf:params:jmap:mail": "account-1",
                "urn:ietf:params:jmap:submission": "account-1"
              },
              "username": "user@example.test",
              "apiUrl": "https://example.test/api",
              "downloadUrl": "https://example.test/download/{accountId}/{blobId}/{name}?type={type}",
              "uploadUrl": "https://example.test/upload/{accountId}",
              "eventSourceUrl": "https://example.test/events?types={types}&closeafter={closeafter}&ping={ping}",
              "state": "session-1",
              "vendorSession": {"enabled": true}
            }
            """;
    }
}
