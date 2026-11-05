using System.Text;
using NUnit.Framework;
using unicode.Tools.UnicodeDataGenerator;

namespace unicode.Tests;

[TestFixture]
public class UnicodeBoundaryConformanceTests
{
    [Test]
    public void GraphemeBreakTest_MatchesEveryUnicode17Record()
    {
        UnicodeBoundaryTestRecord[] records = UnicodeDataParser.ReadGraphemeBreakTest(UnicodeDataPath("auxiliary", "GraphemeBreakTest.txt"));

        Assert.That(records, Has.Length.GreaterThan(700));
        AssertBoundaryRecords(records, new UnicodeSegments().GetGraphemeBreaks, false);
    }

    [Test]
    public void WordBreakTest_MatchesEveryUnicode17Record()
    {
        UnicodeBoundaryTestRecord[] records = UnicodeDataParser.ReadWordBreakTest(UnicodeDataPath("auxiliary", "WordBreakTest.txt"));

        Assert.That(records, Has.Length.GreaterThan(1900));
        AssertBoundaryRecords(records, new UnicodeSegments().GetWordBreaks, false);
    }

    [Test]
    public void SentenceBreakTest_MatchesEveryUnicode17Record()
    {
        UnicodeBoundaryTestRecord[] records = UnicodeDataParser.ReadSentenceBreakTest(UnicodeDataPath("auxiliary", "SentenceBreakTest.txt"));

        Assert.That(records, Has.Length.GreaterThan(500));
        AssertBoundaryRecords(records, new UnicodeSegments().GetSentenceBreaks, false);
    }

    [Test]
    public void LineBreakTest_MatchesEveryUnicode17RecordAndStatus()
    {
        UnicodeBoundaryTestRecord[] records = UnicodeDataParser.ReadLineBreakTest(UnicodeDataPath("auxiliary", "LineBreakTest.txt"));
        UnicodeSegments segments = new UnicodeSegments();

        Assert.That(records, Has.Length.GreaterThan(19000));
        AssertBoundaryRecords(records, segments.GetLineBreaks, true);
        AssertLineBreakStatuses(records, segments);
    }

    [Test]
    public void EmojiSequenceFiles_KeepEveryUnicode17SequenceInOneGraphemeCluster()
    {
        UnicodeEmojiSequenceRecord[] sequences = UnicodeDataParser.ReadEmojiSequences(UnicodeDataPath("emoji", "emoji-sequences.txt"));
        UnicodeEmojiSequenceRecord[] zwjSequences = UnicodeDataParser.ReadEmojiZwjSequences(UnicodeDataPath("emoji", "emoji-zwj-sequences.txt"));
        UnicodeEmojiSequenceRecord[] variationSequences = UnicodeDataParser.ReadEmojiVariationSequences(UnicodeDataPath("emoji", "emoji-variation-sequences.txt"));

        Assert.That(sequences, Has.Length.GreaterThan(1400));
        Assert.That(zwjSequences, Has.Length.GreaterThan(1600));
        Assert.That(variationSequences, Has.Length.GreaterThan(700));
        AssertEmojiRecords(sequences);
        AssertEmojiRecords(zwjSequences);
        AssertEmojiRecords(variationSequences);
    }

    static void AssertBoundaryRecords(UnicodeBoundaryTestRecord[] records, Func<string, int[]> getBreaks, bool addApiStart)
    {
        Assert.That(records, Has.Length.GreaterThan(0));

        int index = 0;
        while (index < records.Length)
        {
            string text = ToText(records[index].CodePoints);
            int[] expected = ExpectedUtf16Breaks(records[index]);
            if (addApiStart)
            {
                if (expected.Length == 0 || expected[0] != 0)
                {
                    expected = new int[] { 0 }.Concat(expected).ToArray();
                }
            }

            Assert.That(getBreaks(text), Is.EqualTo(expected), records[index].Comment);
            index = index + 1;
        }
    }

    static void AssertLineBreakStatuses(UnicodeBoundaryTestRecord[] records, UnicodeSegments segments)
    {
        int recordIndex = 0;
        while (recordIndex < records.Length)
        {
            UnicodeBoundaryTestRecord record = records[recordIndex];
            string text = ToText(record.CodePoints);
            UnicodeLineBreakRecord[] actual = segments.GetLineBreakOpportunities(text);
            List<int> expectedOffsets = new List<int>();
            List<UnicodeLineBreakStatus> expectedStatuses = new List<UnicodeLineBreakStatus>();
            int utf16Offset = 0;
            int boundaryIndex = 0;

            while (boundaryIndex < record.BreakBefore.Length)
            {
                if (boundaryIndex > 0 && record.BreakBefore[boundaryIndex])
                {
                    expectedOffsets.Add(utf16Offset);
                    expectedStatuses.Add(ExpectedLineBreakStatus(record, boundaryIndex));
                }

                if (boundaryIndex < record.CodePoints.Length)
                {
                    utf16Offset = utf16Offset + Utf16Length(record.CodePoints[boundaryIndex]);
                }

                boundaryIndex = boundaryIndex + 1;
            }

            Assert.That(actual.Select(result => result.Utf16Index), Is.EqualTo(expectedOffsets), record.Comment);
            Assert.That(actual.Select(result => result.Status), Is.EqualTo(expectedStatuses), record.Comment);
            recordIndex = recordIndex + 1;
        }
    }

    static UnicodeLineBreakStatus ExpectedLineBreakStatus(UnicodeBoundaryTestRecord record, int boundaryIndex)
    {
        if (boundaryIndex == record.CodePoints.Length)
        {
            return UnicodeLineBreakStatus.Mandatory;
        }

        UnicodeLineBreakClass previous = new UnicodeSegments().GetLineBreakClass(record.CodePoints[boundaryIndex - 1]);
        if (previous == UnicodeLineBreakClass.MandatoryBreak ||
            previous == UnicodeLineBreakClass.CarriageReturn ||
            previous == UnicodeLineBreakClass.LineFeed ||
            previous == UnicodeLineBreakClass.NextLine)
        {
            return UnicodeLineBreakStatus.Mandatory;
        }

        return UnicodeLineBreakStatus.Optional;
    }

    static void AssertEmojiRecords(UnicodeEmojiSequenceRecord[] records)
    {
        UnicodeSegments segments = new UnicodeSegments();
        int recordIndex = 0;

        while (recordIndex < records.Length)
        {
            UnicodeEmojiSequenceRecord record = records[recordIndex];
            int[] codePoints = (int[])record.StartCodePoints.Clone();
            bool complete = false;

            while (!complete)
            {
                string text = ToText(codePoints);
                Assert.That(segments.GetGraphemeBreaks(text), Is.EqualTo(new int[] { 0, text.Length }), record.Comment);
                complete = !IncrementSequence(codePoints, record.StartCodePoints, record.EndCodePoints);
            }

            recordIndex = recordIndex + 1;
        }
    }

    static bool IncrementSequence(int[] codePoints, int[] starts, int[] ends)
    {
        int index = codePoints.Length - 1;
        while (index >= 0)
        {
            if (codePoints[index] < ends[index])
            {
                codePoints[index] = codePoints[index] + 1;
                return true;
            }

            codePoints[index] = starts[index];
            index = index - 1;
        }

        return false;
    }

    static int[] ExpectedUtf16Breaks(UnicodeBoundaryTestRecord record)
    {
        List<int> breaks = new List<int>();
        int offset = 0;
        int index = 0;

        while (index < record.BreakBefore.Length)
        {
            if (record.BreakBefore[index])
            {
                breaks.Add(offset);
            }

            if (index < record.CodePoints.Length)
            {
                offset = offset + Utf16Length(record.CodePoints[index]);
            }

            index = index + 1;
        }

        return breaks.ToArray();
    }

    static int Utf16Length(int codePoint)
    {
        if (codePoint > 0xFFFF)
        {
            return 2;
        }

        return 1;
    }

    static string ToText(int[] codePoints)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < codePoints.Length)
        {
            builder.Append(new UnicodeScalar().ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    static string UnicodeDataPath(params string[] parts)
    {
        List<string> pathParts = new List<string>();
        pathParts.Add(FindRepositoryRoot());
        pathParts.Add("data");
        pathParts.Add("Unicode17.0.0");
        pathParts.AddRange(parts);
        return Path.Combine(pathParts.ToArray());
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
