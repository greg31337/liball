using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class StandardMethodJsonCodecTests
{
    [Test]
    public void GetCodecsMapRequiredOptionalOrderedAndUnknownData()
    {
        JmapGetRequest request = Decode("{\"accountId\":\"a\",\"ids\":null,\"properties\":[\"name\"],\"x\":{\"b\":2,\"a\":1}}", Codec().DecodeGetRequest);

        Assert.Multiple(() =>
        {
            Assert.That(request.AccountId, Is.EqualTo("a"));
            Assert.That(request.Ids, Is.Null);
            Assert.That(request.Properties, Is.EqualTo(new[] { "name" }));
            Assert.That(request.UnknownProperties["x"].GetProperty("a").GetInt32(), Is.EqualTo(1));
        });

        Assert.That(
            Encode(request, Codec().EncodeGetRequest),
            Is.EqualTo("{\"accountId\":\"a\",\"properties\":[\"name\"],\"x\":{\"a\":1,\"b\":2}}"));

        JmapGetResponse response = Decode(
            "{\"accountId\":\"a\",\"state\":\"s\",\"list\":[{\"id\":\"1\"},null],\"notFound\":[\"x\"]}",
            Codec().DecodeGetResponse);
        Assert.That(response.List[0].GetProperty("id").GetString(), Is.EqualTo("1"));
        Assert.That(response.List[1].ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(Encode(response, Codec().EncodeGetResponse), Is.EqualTo("{\"accountId\":\"a\",\"state\":\"s\",\"list\":[{\"id\":\"1\"},null],\"notFound\":[\"x\"]}"));
    }

    [Test]
    public void ChangesCodecsPreserveNullLimitFalseAndRequiredArrays()
    {
        JmapChangesRequest request = Decode("{\"accountId\":\"a\",\"sinceState\":\"s0\",\"maxChanges\":null}", Codec().DecodeChangesRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.IsMaxChangesNull, Is.True);
        });
        Assert.That(Encode(request, Codec().EncodeChangesRequest), Does.Contain("\"maxChanges\":null"));

        JmapChangesResponse response = Decode(
            "{\"accountId\":\"a\",\"oldState\":\"s0\",\"newState\":\"s1\",\"hasMoreChanges\":false,\"created\":[],\"updated\":[\"u\"],\"destroyed\":[]}",
            Codec().DecodeChangesResponse);
        Assert.That(response.HasMoreChanges, Is.False);
        Assert.That(
            Encode(response, Codec().EncodeChangesResponse),
            Is.EqualTo("{\"accountId\":\"a\",\"oldState\":\"s0\",\"newState\":\"s1\",\"hasMoreChanges\":false,\"created\":[],\"updated\":[\"u\"],\"destroyed\":[]}"));
    }

    [Test]
    public void SetCodecsMapPatchValuesResultsAndSetErrors()
    {
        StandardMethodJsonCodec codec = Codec();
        JmapSetRequest request = Decode(
            "{\"accountId\":\"a\",\"ifInState\":\"s\",\"create\":{\"c1\":{\"name\":\"N\"}},\"update\":{\"m1\":{\"name\":\"M\",\"parentId\":null}},\"destroy\":[\"m2\"]}",
            codec.DecodeSetRequest);

        Assert.Multiple(() =>
        {
            Assert.That(request.Create!["c1"].GetProperty("name").GetString(), Is.EqualTo("N"));
            Assert.That(request.Update!["m1"].Values["parentId"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(request.Destroy, Is.EqualTo(new[] { "m2" }));
        });
        Assert.That(Encode(request, Codec().EncodeSetRequest), Does.Contain("\"parentId\":null"));

        JmapSetResponse response = Decode(
            "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"s2\",\"created\":{\"c1\":{\"id\":\"m1\"}},\"updated\":{\"m2\":null},\"destroyed\":[],\"notCreated\":{\"c2\":{\"type\":\"invalidProperties\",\"properties\":[\"name\"],\"future\":7}}}",
            codec.DecodeSetResponse);
        Assert.Multiple(() =>
        {
            Assert.That(response.OldState, Is.Null);
            Assert.That(response.Updated!["m2"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(response.NotCreated!["c2"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
            Assert.That(response.NotCreated["c2"].UnknownProperties.ContainsKey("future"), Is.True);
        });
        Assert.That(Encode(response, Codec().EncodeSetResponse), Does.Contain("\"oldState\":null"));
    }

    [Test]
    public void CopyCodecsMapStatesFlagsAndCreationMaps()
    {
        string json = "{\"fromAccountId\":\"from\",\"ifFromInState\":\"f0\",\"accountId\":\"to\",\"ifInState\":\"t0\",\"create\":{\"c\":{\"id\":\"x\"}},\"onSuccessDestroyOriginal\":false,\"destroyFromIfInState\":\"f0\"}";
        JmapCopyRequest request = Decode(json, Codec().DecodeCopyRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.HasOnSuccessDestroyOriginal, Is.True);
            Assert.That(request.OnSuccessDestroyOriginal, Is.False);
            Assert.That(request.Create["c"].GetProperty("id").GetString(), Is.EqualTo("x"));
        });
        Assert.That(Encode(request, Codec().EncodeCopyRequest), Is.EqualTo(json));

        JmapCopyResponse response = Decode(
            "{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"oldState\":null,\"newState\":\"t1\",\"created\":null,\"notCreated\":null}",
            Codec().DecodeCopyResponse);
        Assert.That(response.OldState, Is.Null);
        Assert.That(Encode(response, Codec().EncodeCopyResponse), Is.EqualTo("{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"oldState\":null,\"newState\":\"t1\"}"));
    }

    [Test]
    public void QueryCodecsMapRecursiveFiltersComparatorsAndPresenceFlags()
    {
        string json = "{\"accountId\":\"a\",\"filter\":{\"operator\":\"AND\",\"conditions\":[{\"name\":\"x\"},{\"operator\":\"NOT\",\"conditions\":[{\"role\":null}]}]},\"sort\":[{\"property\":\"name\",\"isAscending\":false,\"collation\":\"i;ascii-casemap\",\"x\":1}],\"position\":-2,\"anchor\":\"m\",\"anchorOffset\":0,\"limit\":null,\"calculateTotal\":false}";
        JmapQueryRequest request = Decode(json, Codec().DecodeQueryRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.Filter!.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(request.Filter.Conditions![0].FilterProperties["name"].GetString(), Is.EqualTo("x"));
            Assert.That(request.Filter.Conditions[1].Operator, Is.EqualTo(JmapFilterOperator.Not));
            Assert.That(request.Sort![0].HasIsAscending, Is.True);
            Assert.That(request.Sort[0].IsAscending, Is.False);
            Assert.That(request.Position, Is.EqualTo(-2));
            Assert.That(request.HasAnchorOffset, Is.True);
            Assert.That(request.IsLimitNull, Is.True);
            Assert.That(request.HasCalculateTotal, Is.True);
        });
        Assert.That(Encode(request, Codec().EncodeQueryRequest), Is.EqualTo(json));

        JmapQueryResponse response = Decode(
            "{\"accountId\":\"a\",\"queryState\":\"q\",\"canCalculateChanges\":false,\"position\":0,\"ids\":[\"m\"],\"total\":0,\"limit\":0}",
            Codec().DecodeQueryResponse);
        Assert.That(response.HasTotal, Is.True);
        Assert.That(response.HasLimit, Is.True);
        Assert.That(Encode(response, Codec().EncodeQueryResponse), Does.Contain("\"total\":0,\"limit\":0"));
    }

    [Test]
    public void QueryChangesCodecsMapNullMaxChangesAndIndexedAdditions()
    {
        JmapQueryChangesRequest request = Decode(
            "{\"accountId\":\"a\",\"sinceQueryState\":\"q0\",\"maxChanges\":null,\"upToId\":\"m9\",\"calculateTotal\":true}",
            Codec().DecodeQueryChangesRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.HasMaxChanges, Is.True);
            Assert.That(request.IsMaxChangesNull, Is.True);
            Assert.That(request.CalculateTotal, Is.True);
        });

        JmapQueryChangesResponse response = Decode(
            "{\"accountId\":\"a\",\"oldQueryState\":\"q0\",\"newQueryState\":\"q1\",\"total\":2,\"removed\":[\"m0\"],\"added\":[{\"id\":\"m1\",\"index\":0,\"x\":true}]}",
            Codec().DecodeQueryChangesResponse);
        Assert.Multiple(() =>
        {
            Assert.That(response.HasTotal, Is.True);
            Assert.That(response.Added[0].Index, Is.Zero);
            Assert.That(response.Added[0].UnknownProperties["x"].GetBoolean(), Is.True);
        });
        Assert.That(Encode(response, Codec().EncodeQueryChangesResponse), Does.Contain("\"index\":0,\"x\":true"));
    }

    [Test]
    public void DecodeRejectsWrongShapesUnknownOperatorsAndNegativeUnsignedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"accountId\":\"a\",\"state\":\"s\",\"list\":{},\"notFound\":[]}", Codec().DecodeGetResponse), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"accountId\":\"a\",\"sinceState\":\"s\",\"maxChanges\":-1}", Codec().DecodeChangesRequest), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"operator\":\"X\",\"conditions\":[]}", value => Codec().DecodeFilterNode(value, "/filter")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"id\":\"x\",\"index\":-1}", value => Codec().DecodeAddedItem(value, "/added/0")), Throws.TypeOf<JmapProtocolException>());
        });
    }

    private static StandardMethodJsonCodec Codec()
    {
        return new StandardMethodJsonCodec(new JmapJsonReader(100000, 64));
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
