namespace ttf.Tests.Conformance;

public class LineBreakConformanceTests
{
    [Test]
    public void MatchesUnicode17DefaultLineBreakVectors()
    {
        string archivePath = FindUnicodeArchive();
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        int vectorCount = 0;

        using (System.IO.Compression.ZipArchive archive =
            System.IO.Compression.ZipFile.OpenRead(archivePath))
        {
            System.IO.Compression.ZipArchiveEntry? entry = archive.GetEntry(
                "auxiliary/LineBreakTest.txt");
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

        Assert.That(vectorCount, Is.EqualTo(19338));
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
        List<bool> expected = new List<bool>();
        int tokenIndex = 0;
        while (tokenIndex < tokens.Length)
        {
            string token = tokens[tokenIndex];
            if (token == "÷")
            {
                expected.Add(true);
            }
            else if (token == "×")
            {
                expected.Add(false);
            }
            else
            {
                int codePoint = Convert.ToInt32(token, 16);
                text.Append(char.ConvertFromUtf32(codePoint));
            }

            tokenIndex++;
        }

        int runeCount = expected.Count - 1;
        RuneBuffer input = Utf16Decoder.Decode(
            text.ToString(),
            Utf16DecodingMode.Strict,
            runeCount);
        LineBreakMap actual = LineBreakAlgorithm.Analyze(
            input,
            unicodeData,
            runeCount);
        int boundary = 0;
        while (boundary < expected.Count)
        {
            if (actual.CanBreak(boundary) != expected[boundary])
            {
                throw new AssertionException(
                    "Unicode LineBreakTest line " + lineNumber +
                    " differs at boundary " + boundary + ". Expected " +
                    (expected[boundary] ? "÷" : "×") + " but got " +
                    (actual.CanBreak(boundary) ? "÷" : "×") +
                    ". Vector: " + content);
            }

            boundary++;
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
