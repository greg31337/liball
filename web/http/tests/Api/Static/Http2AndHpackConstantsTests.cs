using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace http.Tests;

public class Http2AndHpackConstantsTests
{
    [Test]
    public void Http2ConstantsMatchWireDefaultsAndBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(Http2Constants.ClientConnectionPreface),
                Is.EqualTo("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"));
            Assert.That(Http2Constants.FrameHeaderSize, Is.EqualTo(9));
            Assert.That(Http2Constants.SettingSize, Is.EqualTo(6));
            Assert.That(Http2Constants.PriorityPayloadSize, Is.EqualTo(5));
            Assert.That(Http2Constants.ResetStreamPayloadSize, Is.EqualTo(4));
            Assert.That(Http2Constants.PingPayloadSize, Is.EqualTo(8));
            Assert.That(Http2Constants.GoAwayMinimumPayloadSize, Is.EqualTo(8));
            Assert.That(Http2Constants.WindowUpdatePayloadSize, Is.EqualTo(4));
            Assert.That(Http2Constants.DefaultHeaderTableSize, Is.EqualTo(4096));
            Assert.That(Http2Constants.DefaultEnablePush, Is.EqualTo(1));
            Assert.That(Http2Constants.DefaultInitialWindowSize, Is.EqualTo(65535));
            Assert.That(Http2Constants.MaximumWindowSize, Is.EqualTo(0x7fffffff));
            Assert.That(Http2Constants.DefaultMaximumFrameSize, Is.EqualTo(16384));
            Assert.That(Http2Constants.MaximumFrameSize, Is.EqualTo(0xffffff));
            Assert.That(Http2Constants.MaximumStreamIdentifier, Is.EqualTo(0x7fffffff));
            Assert.That(Http2Constants.ReservedBitMask, Is.EqualTo(0x80000000u));
            Assert.That(Http2Constants.StreamIdentifierMask, Is.EqualTo(0x7fffffffu));
            Assert.That(Http2Constants.EndStreamFlag, Is.EqualTo(0x01));
            Assert.That(Http2Constants.AckFlag, Is.EqualTo(0x01));
            Assert.That(Http2Constants.EndHeadersFlag, Is.EqualTo(0x04));
            Assert.That(Http2Constants.PaddedFlag, Is.EqualTo(0x08));
            Assert.That(Http2Constants.PriorityFlag, Is.EqualTo(0x20));
        });
    }

    [Test]
    public void ReasonPhrasesExposeConventionalText()
    {
        Dictionary<int, string> expected = ReadStatusDescriptions();
        FieldInfo[] statusFields = ReadConstantFields(typeof(HttpStatusCodes));
        FieldInfo[] reasonFields = ReadConstantFields(typeof(HttpReasonPhrases));

        Assert.That(reasonFields.Select(field => field.Name),
            Is.EquivalentTo(statusFields.Select(field => field.Name)));
        Assert.Multiple(() =>
        {
            foreach (FieldInfo statusField in statusFields)
            {
                int statusCode = (int)statusField.GetRawConstantValue()!;
                FieldInfo reasonField = reasonFields.Single(
                    field => field.Name == statusField.Name);
                string reason = (string)reasonField.GetRawConstantValue()!;
                Assert.That(reason, Is.EqualTo(expected[statusCode]),
                    $"Unexpected reason phrase for status {statusCode}.");
            }
        });
    }

    [Test]
    public void HpackStaticTableMatchesRfc7541AppendixA()
    {
        string[] appendix = ReadRfc7541Appendix(
            "Appendix A.  Static Table Definition",
            "Appendix B.  Huffman Code");
        Regex row = new(
            @"^\s*\|\s*(\d+)\s*\|\s*([^|]*?)\s*\|\s*([^|]*?)\s*\|\s*$",
            RegexOptions.CultureInvariant);
        List<int> expectedIndexes = [];
        List<string> expectedNames = [];
        List<string> expectedValues = [];

        foreach (string line in appendix)
        {
            Match match = row.Match(line);
            if (match.Success)
            {
                expectedIndexes.Add(int.Parse(match.Groups[1].Value,
                    NumberStyles.None, CultureInfo.InvariantCulture));
                expectedNames.Add(match.Groups[2].Value.Trim());
                expectedValues.Add(match.Groups[3].Value.Trim());
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(expectedIndexes,
                Is.EqualTo(Enumerable.Range(1, 61)));
            Assert.That(HpackStaticTable.Names, Is.EqualTo(expectedNames));
            Assert.That(HpackStaticTable.Values, Is.EqualTo(expectedValues));
        });
    }

    [Test]
    public void HpackHuffmanTablesMatchRfc7541AppendixB()
    {
        string[] appendix = ReadRfc7541Appendix(
            "Appendix B.  Huffman Code",
            "Appendix C.  Examples");
        Regex row = new(
            @"(?:EOS\s+)?\(\s*(\d+)\).*?\s([0-9a-f]+)\s+\[\s*(\d+)\]",
            RegexOptions.CultureInvariant);
        List<int> expectedSymbols = [];
        List<uint> expectedCodes = [];
        List<byte> expectedLengths = [];

        foreach (string line in appendix)
        {
            Match match = row.Match(line);
            if (match.Success)
            {
                expectedSymbols.Add(int.Parse(match.Groups[1].Value,
                    NumberStyles.None, CultureInfo.InvariantCulture));
                expectedCodes.Add(uint.Parse(match.Groups[2].Value,
                    NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                expectedLengths.Add(byte.Parse(match.Groups[3].Value,
                    NumberStyles.None, CultureInfo.InvariantCulture));
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(expectedSymbols,
                Is.EqualTo(Enumerable.Range(0, 257)));
            Assert.That(HpackStaticTable.HuffmanCodes, Is.EqualTo(expectedCodes));
            Assert.That(HpackStaticTable.HuffmanCodeLengths, Is.EqualTo(expectedLengths));
        });
    }

    private static Dictionary<int, string> ReadStatusDescriptions()
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "specs", "iana",
            "http-status-codes.csv");
        Dictionary<int, string> descriptions = [];
        foreach (string line in File.ReadLines(Path.GetFullPath(path)).Skip(1))
        {
            List<string> cells = ReadCsvCells(line);
            if (cells.Count < 2 || cells[0].Contains('-') ||
                cells[1] == "Unassigned")
            {
                continue;
            }

            string description = cells[1];
            int qualifier = description.IndexOf(" (TEMPORARY", StringComparison.Ordinal);
            if (qualifier < 0)
            {
                qualifier = description.IndexOf(" (OBSOLETED", StringComparison.Ordinal);
            }

            if (qualifier >= 0)
            {
                description = description.Substring(0, qualifier);
            }

            descriptions.Add(int.Parse(cells[0], CultureInfo.InvariantCulture),
                description);
        }

        return descriptions;
    }

    private static List<string> ReadCsvCells(string line)
    {
        List<string> cells = [];
        StringBuilder cell = new();
        bool quoted = false;
        for (int index = 0; index < line.Length; index++)
        {
            char current = line[index];
            if (current == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    cell.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (current == ',' && !quoted)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else
            {
                cell.Append(current);
            }
        }

        cells.Add(cell.ToString());
        return cells;
    }

    private static FieldInfo[] ReadConstantFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .OrderBy(field => field.MetadataToken)
            .ToArray();
    }

    private static string[] ReadRfc7541Appendix(
        string startMarker,
        string endMarker)
    {
        string path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "specs", "rfcs", "rfc7541.txt");
        return File.ReadLines(Path.GetFullPath(path))
            .SkipWhile(line => !line.StartsWith(startMarker,
                StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(line => !line.StartsWith(endMarker,
                StringComparison.Ordinal))
            .ToArray();
    }
}
