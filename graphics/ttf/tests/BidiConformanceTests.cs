namespace ttf.Tests;

public class BidiConformanceTests
{
    const string ArchiveName = "UCD_17.0.0.zip";

    [Test]
    public void PassesUnicode17BidiCharacterTest()
    {
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        int caseCount = 0;
        using (System.IO.Compression.ZipArchive archive = OpenArchive())
        using (System.IO.StreamReader reader = OpenEntry(
            archive,
            "BidiCharacterTest.txt"))
        {
            string? line;
            int lineNumber = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                line = RemoveComment(line).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                ValidateCharacterCase(
                    unicodeData,
                    line,
                    lineNumber);
                caseCount++;
            }
        }

        Assert.That(caseCount, Is.EqualTo(91707));
    }

    [Test]
    public void PassesUnicode17BidiTestForEveryParagraphMode()
    {
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        string[] expectedLevels = System.Array.Empty<string>();
        int[] expectedOrder = System.Array.Empty<int>();
        int dataCaseCount = 0;
        int paragraphCaseCount = 0;
        using (System.IO.Compression.ZipArchive archive = OpenArchive())
        using (System.IO.StreamReader reader = OpenEntry(
            archive,
            "BidiTest.txt"))
        {
            string? line;
            int lineNumber = 0;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                line = RemoveComment(line).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("@Levels:",
                    System.StringComparison.Ordinal))
                {
                    expectedLevels = SplitTokens(line.Substring(8));
                }
                else if (line.StartsWith("@Reorder:",
                    System.StringComparison.Ordinal))
                {
                    expectedOrder = ParseDecimalTokens(line.Substring(9));
                }
                else if (line[0] != '@')
                {
                    paragraphCaseCount += ValidateClassCase(
                        unicodeData,
                        line,
                        lineNumber,
                        expectedLevels,
                        expectedOrder);
                    dataCaseCount++;
                }
            }
        }

        Assert.That(dataCaseCount, Is.EqualTo(490846));
        Assert.That(paragraphCaseCount, Is.GreaterThan(dataCaseCount));
    }

    static void ValidateCharacterCase(
        UnicodeData unicodeData,
        string line,
        int lineNumber)
    {
        string[] fields = line.Split(';');
        if (fields.Length != 5)
        {
            Assert.Fail("Invalid BidiCharacterTest record at line " +
                lineNumber + ".");
        }

        string[] codePointTokens = SplitTokens(fields[0]);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < codePointTokens.Length)
        {
            int codePoint = System.Convert.ToInt32(
                codePointTokens[index],
                16);
            builder.Append(new System.Text.Rune(codePoint).ToString());
            index++;
        }

        int directionValue = int.Parse(fields[1].Trim());
        BidiParagraphDirection direction = CharacterTestDirection(
            directionValue,
            lineNumber);
        int expectedParagraphLevel = int.Parse(fields[2].Trim());
        string[] expectedLevels = SplitTokens(fields[3]);
        int[] expectedOrder = ParseDecimalTokens(fields[4]);
        BidiResult result = Resolve(
            builder.ToString(),
            unicodeData,
            direction,
            codePointTokens.Length);
        ValidateResult(
            result,
            expectedParagraphLevel,
            expectedLevels,
            expectedOrder,
            "BidiCharacterTest",
            lineNumber);
    }

    static int ValidateClassCase(
        UnicodeData unicodeData,
        string line,
        int lineNumber,
        string[] expectedLevels,
        int[] expectedOrder)
    {
        string[] fields = line.Split(';');
        if (fields.Length != 2)
        {
            Assert.Fail("Invalid BidiTest record at line " + lineNumber + ".");
        }

        string[] bidiClasses = SplitTokens(fields[0]);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < bidiClasses.Length)
        {
            builder.Append(new System.Text.Rune(
                RepresentativeCodePoint(bidiClasses[index], lineNumber))
                .ToString());
            index++;
        }

        int paragraphModes = System.Convert.ToInt32(fields[1].Trim(), 16);
        int validated = 0;
        if ((paragraphModes & 1) != 0)
        {
            ValidateClassMode(
                builder.ToString(),
                unicodeData,
                bidiClasses,
                BidiParagraphDirection.Auto,
                expectedLevels,
                expectedOrder,
                lineNumber);
            validated++;
        }

        if ((paragraphModes & 2) != 0)
        {
            ValidateClassMode(
                builder.ToString(),
                unicodeData,
                bidiClasses,
                BidiParagraphDirection.LeftToRight,
                expectedLevels,
                expectedOrder,
                lineNumber);
            validated++;
        }

        if ((paragraphModes & 4) != 0)
        {
            ValidateClassMode(
                builder.ToString(),
                unicodeData,
                bidiClasses,
                BidiParagraphDirection.RightToLeft,
                expectedLevels,
                expectedOrder,
                lineNumber);
            validated++;
        }

        return validated;
    }

    static void ValidateClassMode(
        string text,
        UnicodeData unicodeData,
        string[] bidiClasses,
        BidiParagraphDirection direction,
        string[] expectedLevels,
        int[] expectedOrder,
        int lineNumber)
    {
        int expectedParagraphLevel;
        if (direction == BidiParagraphDirection.LeftToRight)
        {
            expectedParagraphLevel = 0;
        }
        else if (direction == BidiParagraphDirection.RightToLeft)
        {
            expectedParagraphLevel = 1;
        }
        else
        {
            expectedParagraphLevel = AutoParagraphLevel(bidiClasses);
        }

        BidiResult result = Resolve(
            text,
            unicodeData,
            direction,
            bidiClasses.Length);
        ValidateResult(
            result,
            expectedParagraphLevel,
            expectedLevels,
            expectedOrder,
            "BidiTest " + direction,
            lineNumber);
    }

    static void ValidateResult(
        BidiResult result,
        int expectedParagraphLevel,
        string[] expectedLevels,
        int[] expectedOrder,
        string corpus,
        int lineNumber)
    {
        if (result.GetParagraphLevel() != expectedParagraphLevel)
        {
            Assert.Fail(corpus + " paragraph level mismatch at line " +
                lineNumber + ": expected " + expectedParagraphLevel +
                ", actual " + result.GetParagraphLevel() + ".");
        }

        if (result.GetRuneCount() != expectedLevels.Length)
        {
            Assert.Fail(corpus + " level count mismatch at line " +
                lineNumber + ".");
        }

        int index = 0;
        while (index < expectedLevels.Length)
        {
            string expected = expectedLevels[index];
            if (expected == "x")
            {
                if (!result.IsRemovedFromVisualOrder(index) ||
                    result.GetVisualIndex(index) != -1)
                {
                    Assert.Fail(corpus + " X9 removal mismatch at line " +
                        lineNumber + ", index " + index + ".");
                }
            }
            else
            {
                int expectedLevel = int.Parse(expected);
                if (result.GetLevel(index) != expectedLevel)
                {
                    Assert.Fail(corpus + " level mismatch at line " +
                        lineNumber + ", index " + index + ": expected " +
                        expectedLevel + ", actual " +
                        result.GetLevel(index) + ".");
                }
            }

            index++;
        }

        if (result.GetVisualRuneCount() != expectedOrder.Length)
        {
            Assert.Fail(corpus + " visual count mismatch at line " +
                lineNumber + ": expected " + expectedOrder.Length +
                ", actual " + result.GetVisualRuneCount() + ".");
        }

        index = 0;
        while (index < expectedOrder.Length)
        {
            if (result.GetLogicalIndex(index) != expectedOrder[index])
            {
                Assert.Fail(corpus + " visual order mismatch at line " +
                    lineNumber + ", visual index " + index + ": expected " +
                    expectedOrder[index] + ", actual " +
                    result.GetLogicalIndex(index) + ".");
            }

            index++;
        }
    }

    static BidiResult Resolve(
        string text,
        UnicodeData unicodeData,
        BidiParagraphDirection direction,
        int runeCount)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            runeCount);
        return BidiAlgorithm.Resolve(
            input,
            unicodeData,
            direction,
            runeCount,
            runeCount);
    }

    static BidiParagraphDirection CharacterTestDirection(
        int value,
        int lineNumber)
    {
        if (value == 0)
        {
            return BidiParagraphDirection.LeftToRight;
        }

        if (value == 1)
        {
            return BidiParagraphDirection.RightToLeft;
        }

        if (value == 2)
        {
            return BidiParagraphDirection.Auto;
        }

        Assert.Fail("Invalid paragraph direction at line " + lineNumber + ".");
        return BidiParagraphDirection.Auto;
    }

    static int AutoParagraphLevel(string[] bidiClasses)
    {
        int isolateDepth = 0;
        int index = 0;
        while (index < bidiClasses.Length)
        {
            string bidiClass = bidiClasses[index];
            if (bidiClass == "LRI" ||
                bidiClass == "RLI" ||
                bidiClass == "FSI")
            {
                isolateDepth++;
            }
            else if (bidiClass == "PDI")
            {
                if (isolateDepth > 0)
                {
                    isolateDepth--;
                }
            }
            else if (isolateDepth == 0)
            {
                if (bidiClass == "L")
                {
                    return 0;
                }

                if (bidiClass == "R" || bidiClass == "AL")
                {
                    return 1;
                }
            }

            index++;
        }

        return 0;
    }

    static int RepresentativeCodePoint(string bidiClass, int lineNumber)
    {
        if (bidiClass == "L")
        {
            return 0x0041;
        }

        if (bidiClass == "R")
        {
            return 0x05D0;
        }

        if (bidiClass == "AL")
        {
            return 0x0627;
        }

        if (bidiClass == "EN")
        {
            return 0x0030;
        }

        if (bidiClass == "ES")
        {
            return 0x002B;
        }

        if (bidiClass == "ET")
        {
            return 0x0024;
        }

        if (bidiClass == "AN")
        {
            return 0x0660;
        }

        if (bidiClass == "CS")
        {
            return 0x002C;
        }

        if (bidiClass == "NSM")
        {
            return 0x0300;
        }

        if (bidiClass == "BN")
        {
            return 0x00AD;
        }

        if (bidiClass == "B")
        {
            return 0x2029;
        }

        if (bidiClass == "S")
        {
            return 0x0009;
        }

        if (bidiClass == "WS")
        {
            return 0x0020;
        }

        if (bidiClass == "ON")
        {
            return 0x0021;
        }

        if (bidiClass == "LRE")
        {
            return 0x202A;
        }

        if (bidiClass == "LRO")
        {
            return 0x202D;
        }

        if (bidiClass == "RLE")
        {
            return 0x202B;
        }

        if (bidiClass == "RLO")
        {
            return 0x202E;
        }

        if (bidiClass == "PDF")
        {
            return 0x202C;
        }

        if (bidiClass == "LRI")
        {
            return 0x2066;
        }

        if (bidiClass == "RLI")
        {
            return 0x2067;
        }

        if (bidiClass == "FSI")
        {
            return 0x2068;
        }

        if (bidiClass == "PDI")
        {
            return 0x2069;
        }

        Assert.Fail("Unknown bidi class " + bidiClass +
            " at line " + lineNumber + ".");
        return 0;
    }

    static string RemoveComment(string line)
    {
        int comment = line.IndexOf('#');
        return comment < 0 ? line : line.Substring(0, comment);
    }

    static string[] SplitTokens(string value)
    {
        return value.Split(
            new char[] { ' ', '\t' },
            System.StringSplitOptions.RemoveEmptyEntries);
    }

    static int[] ParseDecimalTokens(string value)
    {
        string[] tokens = SplitTokens(value);
        int[] result = new int[tokens.Length];
        int index = 0;
        while (index < tokens.Length)
        {
            result[index] = int.Parse(tokens[index]);
            index++;
        }

        return result;
    }

    static System.IO.Compression.ZipArchive OpenArchive()
    {
        System.IO.DirectoryInfo? directory = new System.IO.DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string path = System.IO.Path.Combine(
                directory.FullName,
                "specs",
                "unicode",
                ArchiveName);
            if (System.IO.File.Exists(path))
            {
                return System.IO.Compression.ZipFile.OpenRead(path);
            }

            directory = directory.Parent;
        }

        throw new System.IO.FileNotFoundException(
            "The pinned Unicode archive could not be located.",
            ArchiveName);
    }

    static System.IO.StreamReader OpenEntry(
        System.IO.Compression.ZipArchive archive,
        string name)
    {
        System.IO.Compression.ZipArchiveEntry? entry = archive.GetEntry(name);
        if (entry == null)
        {
            throw new System.IO.InvalidDataException(
                "The pinned Unicode archive does not contain " + name + ".");
        }

        return new System.IO.StreamReader(entry.Open());
    }
}
