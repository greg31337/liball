using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class MailboxThreadJsonCodecTests
{
    private const string Rights = "{\"mayReadItems\":true,\"mayAddItems\":false,\"mayRemoveItems\":true,\"maySetSeen\":false,\"maySetKeywords\":true,\"mayCreateChild\":false,\"mayRename\":true,\"mayDelete\":false,\"maySubmit\":true,\"future\":1}";
    private const string MailboxValue = "{\"id\":\"m1\",\"name\":\"Inbox\",\"parentId\":null,\"role\":\"inbox\",\"sortOrder\":0,\"totalEmails\":2,\"unreadEmails\":1,\"totalThreads\":2,\"unreadThreads\":1,\"myRights\":" + Rights + ",\"isSubscribed\":false,\"x\":true}";

    [Test]
    public void MailboxGetCodecsMapMailboxRightsAndUnknownProperties()
    {
        MailboxJsonCodec codec = MailboxCodec();
        MailboxGetResponse response = Decode(
            "{\"accountId\":\"a\",\"state\":\"s\",\"list\":[" + MailboxValue + "],\"notFound\":[\"missing\"],\"future\":9}",
            codec.DecodeMailboxGetResponse);

        Assert.Multiple(() =>
        {
            Assert.That(response.List[0].Name, Is.EqualTo("Inbox"));
            Assert.That(response.List[0].ParentId, Is.Null);
            Assert.That(response.List[0].MyRights!.MayReadItems, Is.True);
            Assert.That(response.List[0].MyRights!.MayAddItems, Is.False);
            Assert.That(response.List[0].MyRights!.UnknownProperties["future"].GetInt32(), Is.EqualTo(1));
            Assert.That(response.UnknownProperties["future"].GetInt32(), Is.EqualTo(9));
        });

        string encoded = Encode(response, MailboxCodec().EncodeMailboxGetResponse);
        Assert.That(encoded, Does.Contain("\"parentId\":null,\"role\":\"inbox\",\"sortOrder\":0"));
        Assert.That(encoded, Does.Contain("\"mayAddItems\":false"));
        Assert.That(encoded, Does.Contain("\"isSubscribed\":false,\"x\":true"));

        MailboxGetRequest request = Decode("{\"accountId\":\"a\",\"ids\":null,\"properties\":[\"name\"]}", MailboxCodec().DecodeMailboxGetRequest);
        Assert.That(Encode(request, MailboxCodec().EncodeMailboxGetRequest), Is.EqualTo("{\"accountId\":\"a\",\"properties\":[\"name\"]}"));
    }

    [Test]
    public void MailboxChangesCodecsMapNullLimitsAndUpdatedProperties()
    {
        MailboxChangesRequest request = Decode(
            "{\"accountId\":\"a\",\"sinceState\":\"s0\",\"maxChanges\":null}",
            MailboxCodec().DecodeMailboxChangesRequest);
        Assert.That(request.IsMaxChangesNull, Is.True);
        Assert.That(Encode(request, MailboxCodec().EncodeMailboxChangesRequest), Does.Contain("\"maxChanges\":null"));

        MailboxChangesResponse response = Decode(
            "{\"accountId\":\"a\",\"oldState\":\"s0\",\"newState\":\"s1\",\"hasMoreChanges\":false,\"created\":[],\"updated\":[\"m1\"],\"destroyed\":[],\"updatedProperties\":[\"totalEmails\"],\"x\":1}",
            MailboxCodec().DecodeMailboxChangesResponse);
        Assert.Multiple(() =>
        {
            Assert.That(response.UpdatedProperties, Is.EqualTo(new[] { "totalEmails" }));
            Assert.That(response.UnknownProperties.ContainsKey("updatedProperties"), Is.False);
            Assert.That(response.UnknownProperties.ContainsKey("x"), Is.True);
        });
        Assert.That(Encode(response, MailboxCodec().EncodeMailboxChangesResponse), Does.Contain("\"updatedProperties\":[\"totalEmails\"],\"x\":1"));
    }

    [Test]
    public void MailboxQueryCodecsMapTypedRecursiveFiltersAndTreeFlags()
    {
        string json = "{\"accountId\":\"a\",\"filter\":{\"operator\":\"OR\",\"conditions\":[{\"parentId\":null,\"role\":null,\"hasAnyRole\":false,\"isSubscribed\":false,\"future\":1},{\"name\":\"Work\"}]},\"sort\":[{\"property\":\"name\",\"isAscending\":false}],\"position\":0,\"anchorOffset\":-1,\"limit\":0,\"calculateTotal\":false,\"sortAsTree\":false,\"filterAsTree\":true}";
        MailboxQueryRequest request = Decode(json, MailboxCodec().DecodeMailboxQueryRequest);

        MailboxFilter condition = request.Filter!.Conditions![0];
        Assert.Multiple(() =>
        {
            Assert.That(request.Filter.Operator, Is.EqualTo(JmapFilterOperator.Or));
            Assert.That(condition.HasParentId, Is.True);
            Assert.That(condition.ParentId, Is.Null);
            Assert.That(condition.HasRole, Is.True);
            Assert.That(condition.Role, Is.Null);
            Assert.That(condition.HasHasAnyRole, Is.True);
            Assert.That(condition.HasAnyRole, Is.False);
            Assert.That(condition.HasIsSubscribed, Is.True);
            Assert.That(condition.IsSubscribed, Is.False);
            Assert.That(request.HasPosition, Is.True);
            Assert.That(request.HasSortAsTree, Is.True);
            Assert.That(request.SortAsTree, Is.False);
            Assert.That(request.FilterAsTree, Is.True);
        });
        Assert.That(Encode(request, MailboxCodec().EncodeMailboxQueryRequest), Is.EqualTo(json));

        MailboxQueryResponse response = Decode(
            "{\"accountId\":\"a\",\"queryState\":\"q\",\"canCalculateChanges\":true,\"position\":0,\"ids\":[],\"total\":0}",
            MailboxCodec().DecodeMailboxQueryResponse);
        Assert.That(response.HasTotal, Is.True);
        Assert.That(Encode(response, MailboxCodec().EncodeMailboxQueryResponse), Does.Contain("\"total\":0"));
    }

    [Test]
    public void MailboxQueryChangesCodecsMapTypedFilterAndIndexedChanges()
    {
        MailboxQueryChangesRequest request = Decode(
            "{\"accountId\":\"a\",\"filter\":{\"role\":\"inbox\"},\"sinceQueryState\":\"q0\",\"maxChanges\":0,\"calculateTotal\":false,\"sortAsTree\":true,\"filterAsTree\":false}",
            MailboxCodec().DecodeMailboxQueryChangesRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.Filter!.HasRole, Is.True);
            Assert.That(request.Filter.Role, Is.EqualTo("inbox"));
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.MaxChanges, Is.Zero);
            Assert.That(request.HasSortAsTree, Is.True);
            Assert.That(request.SortAsTree, Is.True);
            Assert.That(request.HasFilterAsTree, Is.True);
            Assert.That(request.FilterAsTree, Is.False);
        });
        Assert.That(
            Encode(request, MailboxCodec().EncodeMailboxQueryChangesRequest),
            Is.EqualTo("{\"accountId\":\"a\",\"filter\":{\"role\":\"inbox\"},\"sinceQueryState\":\"q0\",\"maxChanges\":0,\"calculateTotal\":false,\"sortAsTree\":true,\"filterAsTree\":false}"));

        MailboxQueryChangesResponse response = Decode(
            "{\"accountId\":\"a\",\"oldQueryState\":\"q0\",\"newQueryState\":\"q1\",\"removed\":[],\"added\":[{\"id\":\"m1\",\"index\":0}]}",
            MailboxCodec().DecodeMailboxQueryChangesResponse);
        Assert.That(response.Added[0].Id, Is.EqualTo("m1"));
        Assert.That(Encode(response, MailboxCodec().EncodeMailboxQueryChangesResponse), Does.Contain("\"added\":[{\"id\":\"m1\",\"index\":0}]"));
    }

    [Test]
    public void MailboxSetCodecsSeparateWritableCreateDataFromServerFields()
    {
        string requestJson = "{\"accountId\":\"a\",\"ifInState\":\"s0\",\"create\":{\"c1\":{\"name\":\"Archive\",\"parentId\":null,\"role\":null,\"sortOrder\":0,\"isSubscribed\":false}},\"update\":{\"m1\":{\"name\":\"Renamed\"}},\"destroy\":[\"m2\"],\"onDestroyRemoveEmails\":false}";
        MailboxSetRequest request = Decode(requestJson, MailboxCodec().DecodeMailboxSetRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.Create!["c1"].Name, Is.EqualTo("Archive"));
            Assert.That(request.Update!["m1"].Values["name"].GetString(), Is.EqualTo("Renamed"));
            Assert.That(request.HasOnDestroyRemoveEmails, Is.True);
            Assert.That(request.OnDestroyRemoveEmails, Is.False);
        });
        Assert.That(Encode(request, MailboxCodec().EncodeMailboxSetRequest), Is.EqualTo(requestJson));

        string responseJson = "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"s1\",\"created\":{\"c1\":" + MailboxValue + "},\"updated\":{\"m1\":null},\"destroyed\":[],\"notDestroyed\":{\"m3\":{\"type\":\"notFound\"}}}";
        MailboxSetResponse response = Decode(responseJson, MailboxCodec().DecodeMailboxSetResponse);
        Assert.Multiple(() =>
        {
            Assert.That(response.Created!["c1"].Id, Is.EqualTo("m1"));
            Assert.That(response.OldState, Is.Null);
            Assert.That(response.NotDestroyed!["m3"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
        });
        Assert.That(Encode(response, MailboxCodec().EncodeMailboxSetResponse), Does.Contain("\"oldState\":null"));
    }

    [Test]
    public void ThreadCodecsMapTypedGetAndChangesModels()
    {
        ThreadGetRequest getRequest = Decode(
            "{\"accountId\":\"a\",\"ids\":[\"t1\"],\"properties\":null,\"x\":1}",
            ThreadCodec().DecodeThreadGetRequest);
        Assert.That(getRequest.UnknownProperties.ContainsKey("x"), Is.True);
        Assert.That(Encode(getRequest, ThreadCodec().EncodeThreadGetRequest), Is.EqualTo("{\"accountId\":\"a\",\"ids\":[\"t1\"],\"x\":1}"));

        ThreadGetResponse getResponse = Decode(
            "{\"accountId\":\"a\",\"state\":\"s\",\"list\":[{\"id\":\"t1\",\"emailIds\":[\"e1\",\"e2\"],\"future\":true}],\"notFound\":[]}",
            ThreadCodec().DecodeThreadGetResponse);
        Assert.Multiple(() =>
        {
            Assert.That(getResponse.List[0].EmailIds, Is.EqualTo(new[] { "e1", "e2" }));
            Assert.That(getResponse.List[0].UnknownProperties["future"].GetBoolean(), Is.True);
        });
        Assert.That(Encode(getResponse, ThreadCodec().EncodeThreadGetResponse), Does.Contain("\"emailIds\":[\"e1\",\"e2\"],\"future\":true"));

        ThreadChangesRequest changesRequest = Decode(
            "{\"accountId\":\"a\",\"sinceState\":\"s0\",\"maxChanges\":3}",
            ThreadCodec().DecodeThreadChangesRequest);
        Assert.That(changesRequest.MaxChanges, Is.EqualTo(3));
        Assert.That(Encode(changesRequest, ThreadCodec().EncodeThreadChangesRequest), Does.Contain("\"maxChanges\":3"));

        ThreadChangesResponse changesResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":\"s0\",\"newState\":\"s1\",\"hasMoreChanges\":true,\"created\":[\"t1\"],\"updated\":[],\"destroyed\":[]}",
            ThreadCodec().DecodeThreadChangesResponse);
        Assert.That(changesResponse.HasMoreChanges, Is.True);
        Assert.That(Encode(changesResponse, ThreadCodec().EncodeThreadChangesResponse), Does.Contain("\"hasMoreChanges\":true"));
    }

    [Test]
    public void TypedCodecsRejectInvalidRequiredValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"id\":\"m\",\"name\":\"n\",\"myRights\":null}", value => MailboxCodec().DecodeMailbox(value, "/mailbox")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"operator\":\"NOR\",\"conditions\":[]}", value => MailboxCodec().DecodeMailboxFilter(value, "/filter")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"id\":\"t\",\"emailIds\":null}", value => ThreadCodec().DecodeThread(value, "/thread")), Throws.TypeOf<JmapProtocolException>());
        });
    }

    private static MailboxJsonCodec MailboxCodec()
    {
        return new MailboxJsonCodec(new JmapJsonReader(100000, 64));
    }

    private static ThreadJsonCodec ThreadCodec()
    {
        return new ThreadJsonCodec(new JmapJsonReader(100000, 64));
    }

    private static T Decode<T>(string json, Func<JsonElement, T> decode)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 64);
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
}
