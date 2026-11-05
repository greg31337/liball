using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace http.Tests;

public class HpackHttp2JpCorpusTests
{
    string extractionRoot = "";
    string corpusRoot = "";

    [OneTimeSetUp]
    public void ExtractArchivedCorpus()
    {
        string archive = FindCorpusArchive();
        extractionRoot = Path.Combine(
            Path.GetTempPath(),
            "http2jp-hpack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractionRoot);
        using FileStream archiveStream = File.OpenRead(archive);
        using GZipStream gzip = new GZipStream(
            archiveStream,
            CompressionMode.Decompress);
        TarFile.ExtractToDirectory(gzip, extractionRoot, false);
        corpusRoot = Path.Combine(extractionRoot, "hpack-test-case-master");

        Assert.That(Directory.Exists(corpusRoot), Is.True,
            "The archived http2jp corpus did not contain its expected root.");
    }

    [OneTimeTearDown]
    public void RemoveExtractedCorpus()
    {
        if (extractionRoot.Length != 0 && Directory.Exists(extractionRoot))
        {
            Directory.Delete(extractionRoot, true);
        }
    }

    [Test]
    public void DecoderConsumesEveryCompatibleWireStoryInSharedContext()
    {
        int storyCount = 0;
        int caseCount = 0;
        string[] implementations = Directory.GetDirectories(corpusRoot);
        Array.Sort(implementations, StringComparer.Ordinal);
        foreach (string implementation in implementations)
        {
            string name = Path.GetFileName(implementation);
            if (name == "raw-data" || name == "util")
            {
                continue;
            }

            string[] stories = Directory.GetFiles(
                implementation,
                "story_*.json");
            Array.Sort(stories, StringComparer.Ordinal);
            foreach (string story in stories)
            {
                DecodeWireStory(name, story, ref caseCount);
                storyCount = storyCount + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(storyCount, Is.EqualTo(446));
            Assert.That(caseCount, Is.EqualTo(47142));
        });
    }

    [Test]
    public void EncoderOutputDecodesAcrossEveryRawSharedContext()
    {
        string rawRoot = Path.Combine(corpusRoot, "raw-data");
        string[] stories = Directory.GetFiles(rawRoot, "story_*.json");
        Array.Sort(stories, StringComparer.Ordinal);
        int caseCount = 0;
        foreach (string story in stories)
        {
            EncodeRawStory(story, ref caseCount);
        }

        Assert.Multiple(() =>
        {
            Assert.That(stories.Length, Is.EqualTo(32));
            Assert.That(caseCount, Is.EqualTo(3384));
        });
    }

    void DecodeWireStory(
        string implementation,
        string storyPath,
        ref int totalCases)
    {
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllBytes(storyPath));
        HpackDecoder decoder = new HpackDecoder();
        int advertisedTableSize = Http2Constants.DefaultHeaderTableSize;
        int storyCase = 0;
        foreach (JsonElement item in document.RootElement
            .GetProperty("cases").EnumerateArray())
        {
            JsonElement tableSize;
            if (item.TryGetProperty("header_table_size", out tableSize) &&
                tableSize.ValueKind == JsonValueKind.Number)
            {
                advertisedTableSize = tableSize.GetInt32();
                HpackResult update = decoder.UpdateDecoderTableLimit(
                    advertisedTableSize);
                Assert.That(update.Status, Is.EqualTo(HttpOperationStatus.Done),
                    Context(implementation, storyPath, storyCase));
            }

            string wireText = item.GetProperty("wire").GetString() ?? "";
            byte[] wire = Convert.FromHexString(wireText);
            HpackOptions options = new HpackOptions();
            options.MaximumDynamicTableSize = advertisedTableSize;
            HpackResult decoded = decoder.DecodeHeaderBlock(wire, options);
            HttpHeaders expected = ReadHeaders(item);
            string context = Context(implementation, storyPath, storyCase);

            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done),
                context + ": " + decoded.ErrorCode);
            Assert.That(decoded.Consumed, Is.EqualTo(wire.Length), context);
            AssertHeaders(decoded.Headers, expected, context);
            storyCase = storyCase + 1;
            totalCases = totalCases + 1;
        }
    }

    void EncodeRawStory(string storyPath, ref int totalCases)
    {
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllBytes(storyPath));
        HpackEncoder encoder = new HpackEncoder();
        HpackDecoder decoder = new HpackDecoder();
        int storyCase = 0;
        foreach (JsonElement item in document.RootElement
            .GetProperty("cases").EnumerateArray())
        {
            HttpHeaders headers = ReadHeaders(item);
            HpackResult encoded = encoder.EncodeHeaderBlock(
                headers,
                new HpackOptions());
            HpackResult decoded = decoder.DecodeHeaderBlock(
                encoded.Bytes,
                new HpackOptions());
            string context = Context("encoder", storyPath, storyCase);

            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done),
                context + ": " + encoded.ErrorCode);
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done),
                context + ": " + decoded.ErrorCode);
            AssertHeaders(decoded.Headers, headers, context);
            AssertTablesEqual(
                encoded.DynamicTable,
                decoded.DynamicTable,
                context);
            storyCase = storyCase + 1;
            totalCases = totalCases + 1;
        }
    }

    static HttpHeaders ReadHeaders(JsonElement item)
    {
        HttpHeaders headers = new HttpHeaders();
        foreach (JsonElement header in item.GetProperty("headers")
            .EnumerateArray())
        {
            JsonElement.ObjectEnumerator properties = header.EnumerateObject();
            Assert.That(properties.MoveNext(), Is.True);
            JsonProperty property = properties.Current;
            HttpHeaderField field = new HttpHeaderField();
            field.Name = property.Name;
            field.Value = Encoding.UTF8.GetBytes(property.Value.GetString() ?? "");
            headers.Fields.Add(field);
            Assert.That(properties.MoveNext(), Is.False,
                "Each corpus header object must contain exactly one field.");
        }

        return headers;
    }

    static void AssertHeaders(
        HttpHeaders actual,
        HttpHeaders expected,
        string context)
    {
        Assert.That(actual.Fields.Count, Is.EqualTo(expected.Fields.Count),
            context);
        int index = 0;
        while (index < expected.Fields.Count)
        {
            Assert.That(actual.Fields[index].Name,
                Is.EqualTo(expected.Fields[index].Name),
                context + $" header {index} name");
            Assert.That(actual.Fields[index].Value,
                Is.EqualTo(expected.Fields[index].Value),
                context + $" header {index} value");
            index = index + 1;
        }
    }

    static void AssertTablesEqual(
        List<HpackTableEntry> encoded,
        List<HpackTableEntry> decoded,
        string context)
    {
        Assert.That(decoded.Count, Is.EqualTo(encoded.Count), context);
        int index = 0;
        while (index < encoded.Count)
        {
            Assert.That(decoded[index].Name,
                Is.EqualTo(encoded[index].Name),
                context + $" table {index} name");
            Assert.That(decoded[index].Value,
                Is.EqualTo(encoded[index].Value),
                context + $" table {index} value");
            Assert.That(decoded[index].Size,
                Is.EqualTo(encoded[index].Size),
                context + $" table {index} size");
            index = index + 1;
        }
    }

    static string Context(
        string implementation,
        string storyPath,
        int storyCase)
    {
        return implementation + "/" + Path.GetFileName(storyPath) +
            $" case {storyCase}";
    }

    static string FindCorpusArchive()
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "specs",
                "test-vectors",
                "hpack-test-case-master.tar.gz");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate the archived http2jp HPACK corpus.");
    }
}
