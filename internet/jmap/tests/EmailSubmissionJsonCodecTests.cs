using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class EmailSubmissionJsonCodecTests
{
    private const string Submission = "{\"id\":\"s1\",\"identityId\":\"i1\",\"emailId\":\"e1\",\"threadId\":\"t1\",\"envelope\":{\"mailFrom\":{\"email\":\"\",\"parameters\":{\"RET\":\"FULL\",\"SMTPUTF8\":null}},\"rcptTo\":[{\"email\":\"to@example.com\",\"parameters\":{\"NOTIFY\":\"SUCCESS,FAILURE\"}}],\"route\":\"primary\"},\"sendAt\":\"2026-09-10T12:00:00Z\",\"undoStatus\":\"final\",\"deliveryStatus\":{\"to@example.com\":{\"smtpReply\":\"250 2.0.0 OK\",\"delivered\":\"yes\",\"displayed\":\"unknown\",\"attempts\":1}},\"dsnBlobIds\":[\"d1\"],\"mdnBlobIds\":[\"m1\"],\"future\":true}";

    [Test]
    public void SubmissionCodecMapsEnvelopeParametersStatusNotificationsAndUnknownData()
    {
        EmailSubmission submission = Decode(Submission, value => Codec().DecodeSubmission(value, "/submission"));

        Assert.Multiple(() =>
        {
            Assert.That(submission.Envelope!.MailFrom!.Email, Is.Empty);
            Assert.That(submission.Envelope.MailFrom.Parameters!["SMTPUTF8"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(submission.Envelope.RcptTo[0].Parameters!["NOTIFY"].GetString(), Is.EqualTo("SUCCESS,FAILURE"));
            Assert.That(submission.Envelope.UnknownProperties.ContainsKey("route"), Is.True);
            Assert.That(submission.HasUndoStatus, Is.True);
            Assert.That(submission.UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Final));
            Assert.That(submission.DeliveryStatus!["to@example.com"].Delivered, Is.EqualTo("yes"));
            Assert.That(submission.DeliveryStatus["to@example.com"].UnknownProperties.ContainsKey("attempts"), Is.True);
            Assert.That(submission.DsnBlobIds, Is.EqualTo(new[] { "d1" }));
            Assert.That(submission.UnknownProperties.ContainsKey("future"), Is.True);
        });

        string encoded = Encode(submission, (item, writer) => Codec().EncodeSubmission(item, writer, "/submission"));
        Assert.That(encoded, Is.EqualTo(Submission));
    }

    [Test]
    public void SubmissionGetAndChangesCodecsMapTypedStandardMethods()
    {
        EmailSubmissionGetRequest getRequest = Decode(
            "{\"accountId\":\"a\",\"ids\":null,\"properties\":[\"undoStatus\"]}",
            Codec().DecodeSubmissionGetRequest);
        Assert.That(Encode(getRequest, Codec().EncodeSubmissionGetRequest), Is.EqualTo("{\"accountId\":\"a\",\"properties\":[\"undoStatus\"]}"));

        EmailSubmissionGetResponse getResponse = Decode(
            "{\"accountId\":\"a\",\"state\":\"st\",\"list\":[" + Submission + "],\"notFound\":[]}",
            Codec().DecodeSubmissionGetResponse);
        Assert.That(getResponse.List[0].UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Final));
        Assert.That(Encode(getResponse, Codec().EncodeSubmissionGetResponse), Does.Contain("\"undoStatus\":\"final\""));

        EmailSubmissionChangesRequest changesRequest = Decode(
            "{\"accountId\":\"a\",\"sinceState\":\"st0\",\"maxChanges\":null}",
            Codec().DecodeSubmissionChangesRequest);
        Assert.That(changesRequest.IsMaxChangesNull, Is.True);
        Assert.That(Encode(changesRequest, Codec().EncodeSubmissionChangesRequest), Does.Contain("\"maxChanges\":null"));

        EmailSubmissionChangesResponse changesResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":\"st0\",\"newState\":\"st1\",\"hasMoreChanges\":false,\"created\":[],\"updated\":[\"s1\"],\"destroyed\":[]}",
            Codec().DecodeSubmissionChangesResponse);
        Assert.That(Encode(changesResponse, Codec().EncodeSubmissionChangesResponse), Does.Contain("\"hasMoreChanges\":false"));
    }

    [Test]
    public void SubmissionQueryCodecsMapRecursiveFiltersStatusesComparatorsAndWindows()
    {
        string requestJson = "{\"accountId\":\"a\",\"filter\":{\"operator\":\"NOT\",\"conditions\":[{\"identityIds\":[\"i1\"],\"emailIds\":[\"e1\"],\"threadIds\":[\"t1\"],\"undoStatus\":\"pending\",\"before\":\"2026-10-01T00:00:00Z\",\"after\":\"2026-09-01T00:00:00Z\",\"future\":1}]},\"sort\":[{\"property\":\"sendAt\",\"isAscending\":false,\"collation\":\"i;ascii-casemap\"}],\"position\":-1,\"anchor\":\"s1\",\"anchorOffset\":0,\"limit\":null,\"calculateTotal\":false}";
        EmailSubmissionQueryRequest request = Decode(requestJson, Codec().DecodeSubmissionQueryRequest);
        EmailSubmissionFilter filter = request.Filter!.Conditions![0];

        Assert.Multiple(() =>
        {
            Assert.That(request.Filter.Operator, Is.EqualTo(JmapFilterOperator.Not));
            Assert.That(filter.IdentityIds, Is.EqualTo(new[] { "i1" }));
            Assert.That(filter.UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Pending));
            Assert.That(filter.HasUndoStatus, Is.True);
            Assert.That(filter.UnknownProperties.ContainsKey("future"), Is.True);
            Assert.That(request.Sort![0].IsAscending, Is.False);
            Assert.That(request.HasAnchorOffset, Is.True);
            Assert.That(request.IsLimitNull, Is.True);
        });
        Assert.That(Encode(request, Codec().EncodeSubmissionQueryRequest), Is.EqualTo(requestJson));

        EmailSubmissionQueryResponse response = Decode(
            "{\"accountId\":\"a\",\"queryState\":\"q1\",\"canCalculateChanges\":true,\"position\":0,\"ids\":[\"s1\"],\"total\":0,\"limit\":0}",
            Codec().DecodeSubmissionQueryResponse);
        Assert.That(response.HasTotal, Is.True);
        Assert.That(response.HasLimit, Is.True);
        Assert.That(Encode(response, Codec().EncodeSubmissionQueryResponse), Does.Contain("\"total\":0,\"limit\":0"));
    }

    [Test]
    public void SubmissionQueryChangesAndSetCodecsMapIndexedChangesAndImplicitEmailActions()
    {
        string changesJson = "{\"accountId\":\"a\",\"filter\":{\"undoStatus\":\"canceled\"},\"sinceQueryState\":\"q0\",\"maxChanges\":null,\"upToId\":\"s9\",\"calculateTotal\":false}";
        EmailSubmissionQueryChangesRequest changesRequest = Decode(changesJson, Codec().DecodeSubmissionQueryChangesRequest);
        Assert.That(changesRequest.Filter!.UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Canceled));
        Assert.That(changesRequest.IsMaxChangesNull, Is.True);
        Assert.That(Encode(changesRequest, Codec().EncodeSubmissionQueryChangesRequest), Is.EqualTo(changesJson));

        EmailSubmissionQueryChangesResponse changesResponse = Decode(
            "{\"accountId\":\"a\",\"oldQueryState\":\"q0\",\"newQueryState\":\"q1\",\"removed\":[\"s0\"],\"added\":[{\"id\":\"s1\",\"index\":0}]}",
            Codec().DecodeSubmissionQueryChangesResponse);
        Assert.That(changesResponse.Added[0].Index, Is.Zero);
        Assert.That(Encode(changesResponse, Codec().EncodeSubmissionQueryChangesResponse), Does.Contain("\"index\":0"));

        string setJson = "{\"accountId\":\"a\",\"ifInState\":\"st0\",\"create\":{\"c1\":{\"identityId\":\"i1\",\"emailId\":\"e1\",\"sendAt\":\"2026-09-10T12:00:00Z\"}},\"update\":{\"s1\":{\"undoStatus\":\"canceled\"}},\"destroy\":[\"s2\"],\"onSuccessUpdateEmail\":{\"#c1\":{\"keywords/$draft\":null}},\"onSuccessDestroyEmail\":[\"s2\"]}";
        EmailSubmissionSetRequest setRequest = Decode(setJson, Codec().DecodeSubmissionSetRequest);
        Assert.That(setRequest.Create!["c1"].IdentityId, Is.EqualTo("i1"));
        Assert.That(setRequest.OnSuccessUpdateEmail!["#c1"].Values["keywords/$draft"].ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(Encode(setRequest, Codec().EncodeSubmissionSetRequest), Is.EqualTo(setJson));

        EmailSubmissionSetResponse setResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"st1\",\"created\":{\"c1\":{\"id\":\"s3\",\"undoStatus\":\"pending\"}},\"notUpdated\":{\"s1\":{\"type\":\"cannotUnsend\"}}}",
            Codec().DecodeSubmissionSetResponse);
        Assert.That(setResponse.Created!["c1"].UndoStatus, Is.EqualTo(JmapSubmissionUndoStatus.Pending));
        Assert.That(setResponse.NotUpdated!["s1"].Kind, Is.EqualTo(JmapSetErrorKind.CannotUnsend));
        Assert.That(Encode(setResponse, Codec().EncodeSubmissionSetResponse), Does.Contain("\"oldState\":null"));
    }

    [Test]
    public void SubmissionCodecRejectsUnknownStatusesAndInvalidEnvelopeParameters()
    {
        EmailSubmissionAddress address = new EmailSubmissionAddress();
        address.Email = "a@example.com";
        address.Parameters = new Dictionary<string, JsonElement> { { "SIZE", Json("1") } };

        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"undoStatus\":\"paused\"}", value => Codec().DecodeSubmission(value, "/submission")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"email\":\"a@example.com\",\"parameters\":{\"SIZE\":1}}", value => Codec().DecodeSubmissionAddress(value, "/address")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Encode(address, (item, writer) => Codec().EncodeSubmissionAddress(item, writer, "/address")), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => Decode("{\"smtpReply\":\"250 OK\",\"delivered\":\"yes\"}", value => Codec().DecodeDeliveryStatus(value, "/status")), Throws.TypeOf<JmapProtocolException>());
        });
    }

    private static EmailSubmissionJsonCodec Codec()
    {
        return new EmailSubmissionJsonCodec(new JmapJsonReader(300000, 64));
    }

    private static T Decode<T>(string json, Func<JsonElement, T> decode)
    {
        JmapJsonReader reader = new JmapJsonReader(300000, 64);
        return decode(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static string Encode<T>(T value, Action<T, JmapJsonWriter> encode)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        encode(value, writer);
        writer.DisposeWriter();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
