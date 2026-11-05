using System.Text;
using System.Text.Json;
using jmap;

namespace jmap.Tests;

public class EmailMethodAndQueryJsonCodecTests
{
    [Test]
    public void EmailGetCodecsPreserveFetchFlagsZeroLimitAndTypedResults()
    {
        string requestJson = "{\"accountId\":\"a\",\"ids\":null,\"properties\":[\"id\",\"header:List-Post:asURLs\"],\"bodyProperties\":[\"partId\",\"type\"],\"fetchTextBodyValues\":false,\"fetchHTMLBodyValues\":true,\"fetchAllBodyValues\":false,\"maxBodyValueBytes\":0,\"x\":1}";
        EmailGetRequest request = Decode(requestJson, MethodCodec().DecodeEmailGetRequest);
        Assert.Multiple(() =>
        {
            Assert.That(request.Ids, Is.Null);
            Assert.That(request.HasFetchTextBodyValues, Is.True);
            Assert.That(request.FetchTextBodyValues, Is.False);
            Assert.That(request.HasFetchHtmlBodyValues, Is.True);
            Assert.That(request.FetchHtmlBodyValues, Is.True);
            Assert.That(request.HasFetchAllBodyValues, Is.True);
            Assert.That(request.HasMaxBodyValueBytes, Is.True);
            Assert.That(request.MaxBodyValueBytes, Is.Zero);
            Assert.That(request.UnknownProperties.ContainsKey("x"), Is.True);
        });
        Assert.That(Encode(request, MethodCodec().EncodeEmailGetRequest), Is.EqualTo(requestJson.Replace("\"ids\":null,", string.Empty)));

        EmailGetResponse response = Decode(
            "{\"accountId\":\"a\",\"state\":\"s\",\"list\":[{\"id\":\"e1\",\"size\":0,\"hasAttachment\":false,\"header:X:asText\":\"v\"}],\"notFound\":[]}",
            MethodCodec().DecodeEmailGetResponse);
        Assert.That(response.List[0].HeaderProperties["header:X:asText"].GetString(), Is.EqualTo("v"));
        Assert.That(Encode(response, MethodCodec().EncodeEmailGetResponse), Does.Contain("\"header:X:asText\":\"v\""));
    }

    [Test]
    public void EmailChangesSetAndCopyCodecsMapTypedModelsAndErrors()
    {
        EmailChangesRequest changesRequest = Decode(
            "{\"accountId\":\"a\",\"sinceState\":\"s0\",\"maxChanges\":null}",
            MethodCodec().DecodeEmailChangesRequest);
        Assert.That(changesRequest.IsMaxChangesNull, Is.True);
        Assert.That(Encode(changesRequest, MethodCodec().EncodeEmailChangesRequest), Does.Contain("\"maxChanges\":null"));

        EmailChangesResponse changesResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":\"s0\",\"newState\":\"s1\",\"hasMoreChanges\":false,\"created\":[\"e1\"],\"updated\":[],\"destroyed\":[]}",
            MethodCodec().DecodeEmailChangesResponse);
        Assert.That(Encode(changesResponse, MethodCodec().EncodeEmailChangesResponse), Does.Contain("\"hasMoreChanges\":false"));

        string setJson = "{\"accountId\":\"a\",\"create\":{\"c1\":{\"mailboxIds\":{\"drafts\":true},\"keywords\":{\"$draft\":true},\"subject\":\"New\"}},\"update\":{\"e1\":{\"keywords/$seen\":true}},\"destroy\":[\"e2\"]}";
        EmailSetRequest setRequest = Decode(setJson, MethodCodec().DecodeEmailSetRequest);
        Assert.That(setRequest.Create!["c1"].Subject, Is.EqualTo("New"));
        Assert.That(setRequest.Update!["e1"].Values["keywords/$seen"].GetBoolean(), Is.True);
        Assert.That(Encode(setRequest, MethodCodec().EncodeEmailSetRequest), Is.EqualTo(setJson));

        EmailSetResponse setResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"s2\",\"created\":{\"c1\":{\"id\":\"e3\"}},\"updated\":{\"e1\":null},\"notCreated\":{\"c2\":{\"type\":\"invalidProperties\",\"properties\":[\"subject\"]}}}",
            MethodCodec().DecodeEmailSetResponse);
        Assert.That(setResponse.Created!["c1"].Id, Is.EqualTo("e3"));
        Assert.That(setResponse.NotCreated!["c2"].Kind, Is.EqualTo(JmapSetErrorKind.InvalidProperties));
        Assert.That(Encode(setResponse, MethodCodec().EncodeEmailSetResponse), Does.Contain("\"oldState\":null"));

        string copyJson = "{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"create\":{\"c1\":{\"id\":\"e1\",\"mailboxIds\":{\"archive\":true}}},\"onSuccessDestroyOriginal\":false}";
        EmailCopyRequest copyRequest = Decode(copyJson, MethodCodec().DecodeEmailCopyRequest);
        Assert.That(copyRequest.Create["c1"].Id, Is.EqualTo("e1"));
        Assert.That(copyRequest.HasOnSuccessDestroyOriginal, Is.True);
        Assert.That(Encode(copyRequest, MethodCodec().EncodeEmailCopyRequest), Is.EqualTo(copyJson));

        EmailCopyResponse copyResponse = Decode(
            "{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"oldState\":null,\"newState\":\"s3\",\"created\":null,\"notCreated\":null}",
            MethodCodec().DecodeEmailCopyResponse);
        Assert.That(Encode(copyResponse, MethodCodec().EncodeEmailCopyResponse), Is.EqualTo("{\"fromAccountId\":\"from\",\"accountId\":\"to\",\"oldState\":null,\"newState\":\"s3\"}"));
    }

    [Test]
    public void ImportAndParseCodecsMapDefaultsBodyOptionsAndFailures()
    {
        string importJson = "{\"accountId\":\"a\",\"ifInState\":\"s0\",\"emails\":{\"c1\":{\"blobId\":\"b1\",\"mailboxIds\":{\"inbox\":true},\"receivedAt\":\"2026-09-10T12:00:00Z\",\"x\":1}},\"future\":true}";
        EmailImportRequest importRequest = Decode(importJson, MethodCodec().DecodeEmailImportRequest);
        Assert.Multiple(() =>
        {
            Assert.That(importRequest.Emails["c1"].Keywords, Is.Empty);
            Assert.That(importRequest.Emails["c1"].UnknownProperties.ContainsKey("x"), Is.True);
            Assert.That(importRequest.UnknownProperties.ContainsKey("future"), Is.True);
        });
        string encodedImport = Encode(importRequest, MethodCodec().EncodeEmailImportRequest);
        Assert.That(encodedImport, Does.Contain("\"keywords\":{}"));

        EmailImportResponse importResponse = Decode(
            "{\"accountId\":\"a\",\"oldState\":null,\"newState\":\"s1\",\"created\":null,\"notCreated\":{\"c2\":{\"type\":\"notFound\"}}}",
            MethodCodec().DecodeEmailImportResponse);
        Assert.That(importResponse.NotCreated!["c2"].Kind, Is.EqualTo(JmapSetErrorKind.NotFound));
        Assert.That(Encode(importResponse, MethodCodec().EncodeEmailImportResponse), Does.Contain("\"oldState\":null"));

        string parseJson = "{\"accountId\":\"a\",\"blobIds\":[\"b1\",\"b2\"],\"properties\":[\"subject\"],\"bodyProperties\":[\"type\"],\"fetchTextBodyValues\":false,\"fetchHTMLBodyValues\":false,\"fetchAllBodyValues\":true,\"maxBodyValueBytes\":0}";
        EmailParseRequest parseRequest = Decode(parseJson, MethodCodec().DecodeEmailParseRequest);
        Assert.That(parseRequest.HasFetchTextBodyValues, Is.True);
        Assert.That(parseRequest.FetchTextBodyValues, Is.False);
        Assert.That(parseRequest.FetchAllBodyValues, Is.True);
        Assert.That(parseRequest.HasMaxBodyValueBytes, Is.True);
        Assert.That(Encode(parseRequest, MethodCodec().EncodeEmailParseRequest), Is.EqualTo(parseJson));

        EmailParseResponse parseResponse = Decode(
            "{\"accountId\":\"a\",\"parsed\":{\"b1\":{\"subject\":\"Hi\"}},\"notParsable\":[\"b2\"],\"notFound\":null}",
            MethodCodec().DecodeEmailParseResponse);
        Assert.That(parseResponse.Parsed!["b1"].Subject, Is.EqualTo("Hi"));
        Assert.That(parseResponse.NotParsable, Is.EqualTo(new[] { "b2" }));
        Assert.That(parseResponse.NotFound, Is.Null);
        Assert.That(Encode(parseResponse, MethodCodec().EncodeEmailParseResponse), Does.Not.Contain("\"notFound\""));
    }

    [Test]
    public void EmailQueryCodecsMapAllFilterFieldsComparatorAndWindowFlags()
    {
        string json = "{\"accountId\":\"a\",\"filter\":{\"operator\":\"AND\",\"conditions\":[{\"inMailbox\":\"inbox\",\"inMailboxOtherThan\":[\"trash\"],\"before\":\"2026-10-01T00:00:00Z\",\"after\":\"2026-09-01T00:00:00Z\",\"minSize\":0,\"maxSize\":10,\"allInThreadHaveKeyword\":\"$seen\",\"someInThreadHaveKeyword\":\"$flagged\",\"noneInThreadHaveKeyword\":\"$junk\",\"hasKeyword\":\"$seen\",\"notKeyword\":\"$draft\",\"hasAttachment\":false,\"text\":\"project\",\"from\":\"alice\",\"to\":\"bob\",\"cc\":\"carol\",\"bcc\":\"dave\",\"subject\":\"update\",\"body\":\"milestone\",\"header\":[\"List-Id\",\"example\"],\"future\":1}]},\"sort\":[{\"property\":\"someInThreadHaveKeyword\",\"keyword\":\"$flagged\",\"isAscending\":false,\"collation\":\"i;ascii-casemap\"}],\"position\":-1,\"anchor\":\"e1\",\"anchorOffset\":0,\"limit\":null,\"calculateTotal\":false,\"collapseThreads\":false}";
        EmailQueryRequest request = Decode(json, QueryCodec().DecodeEmailQueryRequest);
        EmailFilter condition = request.Filter!.Conditions![0];

        Assert.Multiple(() =>
        {
            Assert.That(request.Filter.Operator, Is.EqualTo(JmapFilterOperator.And));
            Assert.That(condition.HasMinSize, Is.True);
            Assert.That(condition.MinSize, Is.Zero);
            Assert.That(condition.HasMaxSize, Is.True);
            Assert.That(condition.HasHasAttachment, Is.True);
            Assert.That(condition.HasAttachment, Is.False);
            Assert.That(condition.Header, Is.EqualTo(new[] { "List-Id", "example" }));
            Assert.That(condition.UnknownProperties.ContainsKey("future"), Is.True);
            Assert.That(request.Sort![0].Keyword, Is.EqualTo("$flagged"));
            Assert.That(request.Sort[0].IsAscending, Is.False);
            Assert.That(request.IsLimitNull, Is.True);
            Assert.That(request.HasCollapseThreads, Is.True);
            Assert.That(request.CollapseThreads, Is.False);
        });
        Assert.That(Encode(request, QueryCodec().EncodeEmailQueryRequest), Is.EqualTo(json));

        EmailQueryResponse response = Decode(
            "{\"accountId\":\"a\",\"queryState\":\"q\",\"canCalculateChanges\":true,\"position\":0,\"ids\":[\"e1\"],\"total\":0,\"limit\":0}",
            QueryCodec().DecodeEmailQueryResponse);
        Assert.That(response.HasTotal, Is.True);
        Assert.That(response.HasLimit, Is.True);
        Assert.That(Encode(response, QueryCodec().EncodeEmailQueryResponse), Does.Contain("\"total\":0,\"limit\":0"));
    }

    [Test]
    public void QueryChangesAndSearchSnippetCodecsPreserveNullsAndIndexedChanges()
    {
        string requestJson = "{\"accountId\":\"a\",\"filter\":{\"hasKeyword\":\"$seen\"},\"sinceQueryState\":\"q0\",\"maxChanges\":null,\"upToId\":\"e9\",\"calculateTotal\":false,\"collapseThreads\":true}";
        EmailQueryChangesRequest request = Decode(requestJson, QueryCodec().DecodeEmailQueryChangesRequest);
        Assert.That(request.IsMaxChangesNull, Is.True);
        Assert.That(request.HasCollapseThreads, Is.True);
        Assert.That(request.CollapseThreads, Is.True);
        Assert.That(Encode(request, QueryCodec().EncodeEmailQueryChangesRequest), Is.EqualTo(requestJson));

        EmailQueryChangesResponse response = Decode(
            "{\"accountId\":\"a\",\"oldQueryState\":\"q0\",\"newQueryState\":\"q1\",\"total\":0,\"removed\":[\"e0\"],\"added\":[{\"id\":\"e1\",\"index\":0}]}",
            QueryCodec().DecodeEmailQueryChangesResponse);
        Assert.That(response.Added[0].Index, Is.Zero);
        Assert.That(Encode(response, QueryCodec().EncodeEmailQueryChangesResponse), Does.Contain("\"added\":[{\"id\":\"e1\",\"index\":0}]"));

        SearchSnippetGetRequest snippetRequest = Decode(
            "{\"accountId\":\"a\",\"filter\":{\"text\":\"project\"},\"emailIds\":[\"e1\",\"e2\"]}",
            QueryCodec().DecodeSearchSnippetGetRequest);
        Assert.That(snippetRequest.Filter!.Text, Is.EqualTo("project"));
        Assert.That(Encode(snippetRequest, QueryCodec().EncodeSearchSnippetGetRequest), Does.Contain("\"emailIds\":[\"e1\",\"e2\"]"));

        string snippetJson = "{\"accountId\":\"a\",\"list\":[{\"emailId\":\"e1\",\"subject\":null,\"preview\":\"a <mark>project</mark>\",\"x\":1}],\"notFound\":null}";
        SearchSnippetGetResponse snippetResponse = Decode(snippetJson, QueryCodec().DecodeSearchSnippetGetResponse);
        Assert.That(snippetResponse.List[0].Subject, Is.Null);
        Assert.That(snippetResponse.List[0].UnknownProperties.ContainsKey("x"), Is.True);
        Assert.That(snippetResponse.NotFound, Is.Null);
        Assert.That(
            Encode(snippetResponse, QueryCodec().EncodeSearchSnippetGetResponse),
            Is.EqualTo("{\"accountId\":\"a\",\"list\":[{\"emailId\":\"e1\",\"subject\":null,\"preview\":\"a \\u003Cmark\\u003Eproject\\u003C/mark\\u003E\",\"x\":1}],\"notFound\":null}"));
    }

    [Test]
    public void EmailMethodAndQueryCodecsRejectWrongShapesAndUnsignedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Decode("{\"accountId\":\"a\",\"maxBodyValueBytes\":-1}", MethodCodec().DecodeEmailGetRequest), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"accountId\":\"a\",\"blobIds\":null}", MethodCodec().DecodeEmailParseRequest), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"minSize\":-1}", value => QueryCodec().DecodeEmailFilter(value, "/filter")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"property\":null}", value => QueryCodec().DecodeEmailComparator(value, "/sort/0")), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => Decode("{\"emailId\":\"e1\",\"subject\":null}", value => QueryCodec().DecodeSearchSnippet(value, "/list/0")), Throws.TypeOf<JmapProtocolException>());
        });
    }

    private static EmailMethodJsonCodec MethodCodec()
    {
        return new EmailMethodJsonCodec(new JmapJsonReader(300000, 64));
    }

    private static EmailQueryJsonCodec QueryCodec()
    {
        return new EmailQueryJsonCodec(new JmapJsonReader(300000, 64));
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
}
