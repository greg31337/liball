namespace ttf.Tests.Conformance;

public class GraphemeBreakConformanceTests
{
    [Test]
    public void MatchesUnicode17ExtendedGraphemeClusterVectors()
    {
        string archivePath = FindUnicodeArchive();
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        int vectorCount = 0;

        using (System.IO.Compression.ZipArchive archive =
            System.IO.Compression.ZipFile.OpenRead(archivePath))
        {
            System.IO.Compression.ZipArchiveEntry? entry = archive.GetEntry(
                "auxiliary/GraphemeBreakTest.txt");
            Assert.That(entry, Is.Not.Null);

            using (System.IO.StreamReader reader =
                new System.IO.StreamReader(entry!.Open()))
            {
                string? line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (RunVector(line, lineNumber, unicodeData))
                    {
                        vectorCount++;
                    }
                }
            }
        }

        Assert.That(vectorCount, Is.EqualTo(766));
    }

    static bool RunVector(
        string line,
        int lineNumber,
        UnicodeData unicodeData)
    {
        string content = line.Split('#')[0].Trim();

        if (content.Length == 0)
        {
            return false;
        }

        string[] tokens = content.Split(
            new char[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        List<int> boundaries = new List<int>();
        int runeCount = 0;
        int tokenIndex = 0;

        while (tokenIndex < tokens.Length)
        {
            string token = tokens[tokenIndex];

            if (token == "÷")
            {
                boundaries.Add(runeCount);
            }
            else if (token != "×")
            {
                int codePoint = Convert.ToInt32(token, 16);
                text.Append(char.ConvertFromUtf32(codePoint));
                runeCount++;
            }

            tokenIndex++;
        }

        RuneBuffer input = Utf16Decoder.Decode(
            text.ToString(),
            Utf16DecodingMode.Strict,
            runeCount);
        GraphemeClusterMap actual = GraphemeSegmenter.Segment(
            input,
            unicodeData,
            runeCount,
            runeCount);
        int expectedClusterCount = boundaries.Count - 1;

        if (actual.GetClusterCount() != expectedClusterCount)
        {
            throw new AssertionException(
                "Unicode GraphemeBreakTest line " + lineNumber +
                " expected " + expectedClusterCount + " clusters but got " +
                actual.GetClusterCount() + ". Vector: " + content);
        }

        int clusterIndex = 0;

        while (clusterIndex < expectedClusterCount)
        {
            int expectedStart = boundaries[clusterIndex];
            int expectedLength = boundaries[clusterIndex + 1] - expectedStart;

            if (actual.GetRuneStart(clusterIndex) != expectedStart ||
                actual.GetRuneLength(clusterIndex) != expectedLength)
            {
                throw new AssertionException(
                    "Unicode GraphemeBreakTest line " + lineNumber +
                    " differs at cluster " + clusterIndex + ". Vector: " +
                    content);
            }

            clusterIndex++;
        }

        return true;
    }

    static string FindUnicodeArchive()
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);

        while (directory != null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "specs",
                "unicode",
                "UCD_17.0.0.zip");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new AssertionException(
            "The pinned Unicode 17 UCD archive could not be located.");
    }
}
