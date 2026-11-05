using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class IdentityVacationJsonCodecTests
{
    [Test]
    public void IdentityGetCodecsMapAddressesSignaturesPermissionsAndUnknownData()
    {
        string identityJson = "{\"id\":\"i1\",\"name\":\"Alice\",\"email\":\"alice@example.com\",\"replyTo\":[{\"name\":null,\"email\":\"reply@example.com\"}],\"bcc\":[{\"name\":\"Archive\",\"email\":\"archive@example.com\"}],\"textSignature\":\"Thanks\",\"htmlSignature\":\"<b>Thanks</b>\",\"mayDelete\":false,\"future\":1}";
        IdentityGetResponse response = Decode(
            "{\"accountId\":\"a\",\"state\":\"s\",\"list\":[" + identityJson + "],\"notFound\":[]}",
            Codec().DecodeIdentityGetResponse);

        Assert.Multiple(() =>
        {
            Assert.That(response.List[0].ReplyTo![0].Name, Is.Null);
            Assert.That(response.List[0].Bcc![0].Email, Is.EqualTo("archive@example.com"));
            Assert.That(response.List[0].MayDelete, Is.False);
            Assert.That(response.List[0].UnknownProperties.ContainsKey("future"), Is.True);
        });
        string encoded = Encode(response, Codec().EncodeIdentityGetResponse);
        Assert.That(encoded, Does.Contain("\"mayDelete\":false,\"future\":1"));
        Assert.That(encoded, Does.Contain("\\u003Cb\\u003EThanks\\u003C/b\\u003E"));

        IdentityGetRequest request = Decode("{\"accountId\":\"a\",\"ids\":null,\"properties\":[\"name\"]}", Codec().DecodeIdentityGetRequest);
        Assert.That(Encode(request, Codec().EncodeIdentityGetRequest), Is.EqualTo("{\"accountId\":\"a\",\"properties\":[\"name\"]}"));
    }

    [Test]
    public void IdentityChangesAndSetCodecsReuseStandardPresenceAndErrorRules()
    {
        IdentityChangesRequest changesRequest = Decode(
            "{\"accountId\":\"a\",\"sinceState\":\"s0\",\"maxChanges\":null}",
            Codec().DecodeIdentityChangesRequest);
        Assert.That(changesRequest.IsMaxChangesNull, Is.True);
        Assert.That(Encode(changesRequest, Codec().EncodeIdentityChangesRequest), Does.Contain("\"maxChanges\":null"));

        IdentityChangesResponse changesResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":\"s0\",\"newState\":\"s1\",\"hasMoreChanges\":false,\"created\":[],\"updated\":[\"i1\"],\"destroyed\":[]}",
            Codec().DecodeIdentityChangesResponse);
        Assert.That(Encode(changesResponse, Codec().EncodeIdentityChangesResponse), Does.Contain("\"hasMoreChanges\":false"));

        string setJson = "{\"accountId\":\"a\",\"create\":{\"c1\":{\"name\":\"Alice\",\"email\":\"alice@example.com\",\"textSignature\":\"Thanks\"}},\"update\":{\"i1\":{\"name\":\"A.\"}},\"destroy\":[\"i2\"]}";
        IdentitySetRequest setRequest = Decode(setJson, Codec().DecodeIdentitySetRequest);
        Assert.That(setRequest.Create!["c1"].Email, Is.EqualTo("alice@example.com"));
        Assert.That(setRequest.Update!["i1"].Values["name"].GetString(), Is.EqualTo("A."));
        Assert.That(Encode(setRequest, Codec().EncodeIdentitySetRequest), Is.EqualTo(setJson));

        IdentitySetResponse setResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"s2\",\"created\":{\"c1\":{\"id\":\"i3\",\"mayDelete\":true}},\"updated\":{\"i1\":null},\"notDestroyed\":{\"i2\":{\"type\":\"forbidden\"}}}",
            Codec().DecodeIdentitySetResponse);
        Assert.That(setResponse.Created!["c1"].Id, Is.EqualTo("i3"));
        Assert.That(setResponse.NotDestroyed!["i2"].Kind, Is.EqualTo(JmapSetErrorKind.Forbidden));
        Assert.That(Encode(setResponse, Codec().EncodeIdentitySetResponse), Does.Contain("\"oldState\":null"));
    }

    [Test]
    public void VacationGetAndSetCodecsPreserveExplicitNullsFalseAndSingletonData()
    {
        string vacation = "{\"id\":\"singleton\",\"isEnabled\":false,\"fromDate\":null,\"toDate\":null,\"subject\":null,\"textBody\":\"Away\",\"htmlBody\":null,\"locale\":\"en\"}";
        VacationResponseGetResponse response = Decode(
            "{\"accountId\":\"a\",\"state\":\"s\",\"list\":[" + vacation + "],\"notFound\":[]}",
            VacationCodec().DecodeVacationGetResponse);
        Assert.Multiple(() =>
        {
            Assert.That(response.List[0].IsEnabled, Is.False);
            Assert.That(response.List[0].FromDate, Is.Null);
            Assert.That(response.List[0].TextBody, Is.EqualTo("Away"));
            Assert.That(response.List[0].UnknownProperties.ContainsKey("locale"), Is.True);
        });
        Assert.That(Encode(response, VacationCodec().EncodeVacationGetResponse), Does.Contain(vacation));

        VacationResponseGetRequest getRequest = Decode("{\"accountId\":\"a\",\"ids\":null}", VacationCodec().DecodeVacationGetRequest);
        Assert.That(Encode(getRequest, VacationCodec().EncodeVacationGetRequest), Is.EqualTo("{\"accountId\":\"a\"}"));

        string setJson = "{\"accountId\":\"a\",\"ifInState\":\"s\",\"create\":{\"c1\":{\"isEnabled\":true,\"fromDate\":null,\"toDate\":null,\"subject\":null,\"textBody\":\"Away\",\"htmlBody\":null}},\"update\":{\"singleton\":{\"isEnabled\":false}},\"destroy\":[\"singleton\"]}";
        VacationResponseSetRequest setRequest = Decode(setJson, VacationCodec().DecodeVacationSetRequest);
        Assert.That(setRequest.Create!["c1"].IsEnabled, Is.True);
        Assert.That(Encode(setRequest, VacationCodec().EncodeVacationSetRequest), Is.EqualTo(setJson));

        VacationResponseSetResponse setResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"s2\",\"created\":null,\"updated\":{\"singleton\":null},\"notCreated\":{\"c1\":{\"type\":\"singleton\"}}}",
            VacationCodec().DecodeVacationSetResponse);
        Assert.That(setResponse.NotCreated!["c1"].Kind, Is.EqualTo(JmapSetErrorKind.Singleton));
        Assert.That(Encode(setResponse, VacationCodec().EncodeVacationSetResponse), Does.Contain("\"oldState\":null"));
    }

    [Test]
    public void IdentityAndVacationCodecsRejectInvalidRequiredShapes()
    {
        Identity incomplete = new Identity();
        incomplete.Name = "No email";

        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"accountId\":\"a\",\"state\":\"s\",\"list\":{},\"notFound\":[]}", Codec().DecodeIdentityGetResponse), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Encode(incomplete, (item, writer) => Codec().EncodeIdentityCreate(item, writer, "/create/c1")), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => Decode("{\"id\":\"singleton\",\"isEnabled\":0}", value => VacationCodec().DecodeVacationResponse(value, "/vacation")), Throws.TypeOf<JmapProtocolException>());
        });
    }

    private static IdentityJsonCodec Codec()
    {
        return new IdentityJsonCodec(new JmapJsonReader(200000, 64));
    }

    private static VacationResponseJsonCodec VacationCodec()
    {
        return new VacationResponseJsonCodec(new JmapJsonReader(200000, 64));
    }

    private static T Decode<T>(string json, Func<JsonElement, T> decode)
    {
        JmapJsonReader reader = new JmapJsonReader(200000, 64);
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
