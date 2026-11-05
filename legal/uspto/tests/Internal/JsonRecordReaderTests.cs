using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class JsonRecordReaderTests
{
    [Test]
    public void ArrayRecordsAreReadInBoundedBatchesWithDiscoveredHeaders()
    {
        string json = "[ {\"id\":1,\"name\":\"alpha\"}, {\"id\":2,\"active\":true}, {\"name\":null,\"extra\":{\"x\":3}} ]";
        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(json)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        JsonRecordReader reader = new JsonRecordReader(input, false, "records.json", CreateLimits(32, 100, 2, 1000), CancellationToken.None);

        JsonRecord[] first = reader.ReadBatch(2);
        JsonRecord[] second = reader.ReadBatch(2);
        JsonRecord[] finished = reader.ReadBatch(2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first[0].RecordNumber, Is.EqualTo(1L));
            Assert.That(first[0].RawJson, Is.EqualTo("{\"id\":1,\"name\":\"alpha\"}"));
            Assert.That(first[0].Headers, Is.EqualTo(new string[] { "id", "name" }));
            Assert.That(first[0].ReadValue("id"), Is.EqualTo("1"));
            Assert.That(first[0].ReadValue("name"), Is.EqualTo("\"alpha\""));
            Assert.That(first[0].Element.GetProperty("name").GetString(), Is.EqualTo("alpha"));
            Assert.That(first[1].ReadValue("active"), Is.EqualTo("true"));
            Assert.That(second.Single().ReadValue("extra"), Is.EqualTo("{\"x\":3}"));
            Assert.That(reader.Headers, Is.EqualTo(new string[] { "id", "name", "active", "extra" }));
            Assert.That(finished, Is.Empty);
        });
    }

    [Test]
    public void RootObjectIsOneRecordAndRetainsRawFieldValues()
    {
        string json = " \n {\"text\":\"Résumé\",\"items\":[1,2],\"nothing\":null} \t";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonRecordReader reader = new JsonRecordReader(input, false, "object.json", CreateLimits(32, 100, 5, 1000), CancellationToken.None);

        JsonRecord record = RequireRecord(reader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(record.RecordNumber, Is.EqualTo(1L));
            Assert.That(record.StartLineNumber, Is.EqualTo(2));
            Assert.That(record.Headers, Is.EqualTo(new string[] { "text", "items", "nothing" }));
            Assert.That(record.ReadValue("text"), Is.EqualTo("\"Résumé\""));
            Assert.That(record.ReadValue("items"), Is.EqualTo("[1,2]"));
            Assert.That(record.ReadValue("nothing"), Is.EqualTo("null"));
            Assert.That(record.ReadValue("missing"), Is.Null);
            Assert.That(reader.ReadRecord(), Is.Null);
        });

        reader.Close();
        Assert.That(input.CanRead, Is.True);
    }

    [Test]
    public void JsonLinesSkipsBlankLinesAndReportsSourceLocations()
    {
        string jsonLines = "\n  {\"a\":1}\r\n\t\r\n{\"b\":2}\n {\"a\":3,\"c\":4}  ";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(jsonLines));
        JsonRecordReader reader = new JsonRecordReader(input, true, "records.jsonl", CreateLimits(32, 100, 2, 1000), CancellationToken.None);

        JsonRecord[] first = reader.ReadBatch(2);
        JsonRecord[] second = reader.ReadBatch(2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(record => record.RawJson), Is.EqualTo(new string[] { "{\"a\":1}", "{\"b\":2}" }));
            Assert.That(first[0].StartLineNumber, Is.EqualTo(2));
            Assert.That(first[0].StartOffset, Is.EqualTo(3L));
            Assert.That(first[1].StartLineNumber, Is.EqualTo(4));
            Assert.That(second.Single().StartLineNumber, Is.EqualTo(5));
            Assert.That(second.Single().ReadValue("c"), Is.EqualTo("4"));
            Assert.That(reader.Headers, Is.EqualTo(new string[] { "a", "b", "c" }));
            Assert.That(reader.ReadBatch(2), Is.Empty);
        });
    }

    [Test]
    public void Utf16AndUtf32BomsAreDetectedWithoutChangingRawRecords()
    {
        const string jsonLines = "{\"text\":\"Résumé 漢字\"}\n42\n";
        Encoding utf16 = Encoding.BigEndianUnicode;
        Encoding utf32 = new UTF32Encoding(true, true, true);
        byte[] utf16Bytes = utf16.GetPreamble().Concat(utf16.GetBytes(jsonLines)).ToArray();
        byte[] utf32Bytes = utf32.GetPreamble().Concat(utf32.GetBytes("[{\"id\":1},[2,3]]")).ToArray();
        using MemoryStream utf16Input = new MemoryStream(utf16Bytes);
        using MemoryStream utf32Input = new MemoryStream(utf32Bytes);
        JsonRecordReader utf16Reader = new JsonRecordReader(utf16Input, true, "utf16.jsonl", CreateLimits(32, 100, 10, 1000), CancellationToken.None);
        JsonRecordReader utf32Reader = new JsonRecordReader(utf32Input, false, "utf32.json", CreateLimits(32, 100, 10, 1000), CancellationToken.None);

        JsonRecord[] utf16Records = utf16Reader.ReadBatch(10);
        JsonRecord[] utf32Records = utf32Reader.ReadBatch(10);

        Assert.Multiple(() =>
        {
            Assert.That(utf16Records.Select(item => item.RawJson), Is.EqualTo(new[] { "{\"text\":\"Résumé 漢字\"}", "42" }));
            Assert.That(utf16Records[0].ReadValue("text"), Is.EqualTo("\"Résumé 漢字\""));
            Assert.That(utf16Records[1].Headers, Is.Empty);
            Assert.That(utf32Records.Select(item => item.RawJson), Is.EqualTo(new[] { "{\"id\":1}", "[2,3]" }));
            Assert.That(utf32Records[1].Element.ValueKind, Is.EqualTo(JsonValueKind.Array));
        });
    }

    [Test]
    public void NonObjectArrayValuesRemainAvailableAsRawRecords()
    {
        string json = "[1, \"two\", null, [3,4]]";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonRecordReader reader = new JsonRecordReader(input, false, "values.json", CreateLimits(32, 100, 10, 1000), CancellationToken.None);

        JsonRecord[] records = reader.ReadBatch(10);

        Assert.Multiple(() =>
        {
            Assert.That(records.Select(record => record.RawJson), Is.EqualTo(new string[] { "1", "\"two\"", "null", "[3,4]" }));
            Assert.That(records.Select(record => record.Element.ValueKind), Is.EqualTo(new JsonValueKind[] { JsonValueKind.Number, JsonValueKind.String, JsonValueKind.Null, JsonValueKind.Array }));
            Assert.That(records.All(record => record.Headers.Length == 0), Is.True);
            Assert.That(reader.Headers, Is.Empty);
        });
    }

    [Test]
    public void StringLimitsIncludeNestedValuesAndPropertyNames()
    {
        using MemoryStream valueInput = new MemoryStream(Encoding.UTF8.GetBytes("[{\"outer\":{\"name\":\"abcdef\"}}]"));
        JsonRecordReader valueReader = new JsonRecordReader(valueInput, false, "strings.json", CreateLimits(32, 5, 10, 1000), CancellationToken.None);
        ParseException valueFailure = Assert.Throws<ParseException>(() => valueReader.ReadRecord());

        using MemoryStream nameInput = new MemoryStream(Encoding.UTF8.GetBytes("[{\"longer\":1}]"));
        JsonRecordReader nameReader = new JsonRecordReader(nameInput, false, "names.json", CreateLimits(32, 5, 10, 1000), CancellationToken.None);
        ParseException nameFailure = Assert.Throws<ParseException>(() => nameReader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(valueFailure.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(valueFailure.SourceName, Is.EqualTo("strings.json"));
            Assert.That(valueFailure.DataPath, Is.EqualTo("$[1][\"outer\"][\"name\"]"));
            Assert.That(valueFailure.Message, Does.Contain("JSON string"));
            Assert.That(nameFailure.DataPath, Is.EqualTo("$[1][\"longer\"]"));
            Assert.That(nameFailure.Message, Does.Contain("property name"));
        });
    }

    [Test]
    public void NestedStringLimitReportsTheSpecificValuePath()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("[{\"x\":{\"y\":\"abcd\"}}]"));
        JsonRecordReader reader = new JsonRecordReader(input, false, "nested.json", CreateLimits(32, 3, 10, 1000), CancellationToken.None);

        ParseException failure = Assert.Throws<ParseException>(() => reader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(failure.DataPath, Is.EqualTo("$[1][\"x\"][\"y\"]"));
            Assert.That(failure.HasOffset, Is.True);
            Assert.That(failure.HasLineNumber, Is.True);
            Assert.That(failure.LineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void DepthAndRawRecordLimitsAreEnforced()
    {
        using MemoryStream depthInput = new MemoryStream(Encoding.UTF8.GetBytes("[{\"a\":{\"b\":1}}]"));
        JsonRecordReader depthReader = new JsonRecordReader(depthInput, false, "deep.json", CreateLimits(1, 100, 10, 1000), CancellationToken.None);
        JsonException depthFailure = Assert.Catch<JsonException>(() => depthReader.ReadRecord());

        using MemoryStream rawInput = new MemoryStream(Encoding.UTF8.GetBytes("[{\"abcdef\":1}]"));
        JsonRecordReader rawReader = new JsonRecordReader(rawInput, false, "large.json", CreateLimits(32, 100, 10, 5), CancellationToken.None);
        ParseException rawFailure = Assert.Throws<ParseException>(() => rawReader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(depthFailure.Message, Does.Contain("maximum configured depth"));
            Assert.That(rawFailure.SourceName, Is.EqualTo("large.json"));
            Assert.That(rawFailure.DataPath, Is.EqualTo("$[1]"));
            Assert.That(rawFailure.Message, Does.Contain("raw character limit"));
        });
    }

    [Test]
    public void MalformedContainersAndDocumentRootsAreRejectedWithLocations()
    {
        using MemoryStream delimiterInput = new MemoryStream(Encoding.UTF8.GetBytes("[{\"a\":1]]"));
        JsonRecordReader delimiterReader = new JsonRecordReader(delimiterInput, false, "broken.json", CreateLimits(32, 100, 10, 1000), CancellationToken.None);
        ParseException delimiterFailure = Assert.Throws<ParseException>(() => delimiterReader.ReadRecord());

        using MemoryStream rootInput = new MemoryStream(Encoding.UTF8.GetBytes("42"));
        JsonRecordReader rootReader = new JsonRecordReader(rootInput, false, "scalar.json", CreateLimits(32, 100, 10, 1000), CancellationToken.None);
        ParseException rootFailure = Assert.Throws<ParseException>(() => rootReader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(delimiterFailure.DataPath, Is.EqualTo("$[1]"));
            Assert.That(delimiterFailure.Offset, Is.EqualTo(7L));
            Assert.That(rootFailure.DataPath, Is.EqualTo("$"));
            Assert.That(rootFailure.Offset, Is.EqualTo(0L));
        });
    }

    [Test]
    public void InvalidBatchSizesAndCancellationAreHonored()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("[]"));
        JsonRecordReader reader = new JsonRecordReader(input, false, "batch.json", CreateLimits(32, 100, 2, 1000), CancellationToken.None);
        ValidationException batchFailure = Assert.Throws<ValidationException>(() => reader.ReadBatch(3));

        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using MemoryStream cancelledInput = new MemoryStream(Encoding.UTF8.GetBytes("[{\"id\":1}]"));
        JsonRecordReader cancelledReader = new JsonRecordReader(cancelledInput, false, "cancelled.json", CreateLimits(32, 100, 2, 1000), cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(batchFailure.ErrorCode, Is.EqualTo("BATCH_SIZE_OUT_OF_RANGE"));
            Assert.Throws<OperationCanceledException>(() => cancelledReader.ReadRecord());
        });
    }

    [Test]
    public void JsonLinesRequireOneCompleteValuePerNonBlankLine()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":1} {\"id\":2}\n"));
        JsonRecordReader reader = new JsonRecordReader(input, true, "invalid.jsonl", CreateLimits(32, 100, 10, 1000), CancellationToken.None);

        JsonException failure = Assert.Catch<JsonException>(() => reader.ReadRecord());

        Assert.That(failure.BytePositionInLine, Is.GreaterThan(0L));
    }

    private static JsonRecord RequireRecord(JsonRecord? record)
    {
        return record ?? throw new AssertionException("Expected another JSON record.");
    }

    private static UsptoLimits CreateLimits(int maximumDepth, int maximumStringCharacters, int maximumResults, long maximumRawCharacters)
    {
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumJsonDepth = maximumDepth;
        limits.MaximumStringCharacters = maximumStringCharacters;
        limits.MaximumResults = maximumResults;
        limits.MaximumRawResponseBytes = maximumRawCharacters;
        return limits;
    }
}
