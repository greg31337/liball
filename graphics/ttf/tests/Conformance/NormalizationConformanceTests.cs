namespace ttf.Tests.Conformance;

public class NormalizationConformanceTests
{
    const int BatchSize = 512;
    const int ExpectedVectorCount = 20034;
    const int IdentityBatchSize = 4096;
    const int ExpectedIdentityCount = 280248;

    [TestCaseSource(nameof(Batches))]
    public void MatchesUnicode17NormalizationVectors(
        NormalizationConformanceBatch batch)
    {
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        int index = 0;

        while (index < batch.GetCount())
        {
            ValidateVector(unicodeData, batch.GetVector(index));
            index++;
        }
    }

    [TestCaseSource(nameof(IdentityBatches))]
    public void LeavesOtherAssignedUnicode17ScalarsUnchanged(
        NormalizationIdentityBatch batch)
    {
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        int index = 0;

        while (index < batch.GetCount())
        {
            int codePoint = batch.GetCodePoint(index);
            AssertIdentity(unicodeData, codePoint, UnicodeNormalizationMode.Nfc);
            AssertIdentity(unicodeData, codePoint, UnicodeNormalizationMode.Nfd);
            AssertIdentity(unicodeData, codePoint, UnicodeNormalizationMode.Nfkc);
            AssertIdentity(unicodeData, codePoint, UnicodeNormalizationMode.Nfkd);
            index++;
        }
    }

    static NUnit.Framework.TestCaseData[] Batches()
    {
        List<NUnit.Framework.TestCaseData> cases =
            new List<NUnit.Framework.TestCaseData>();
        List<NormalizationConformanceVector> vectors =
            new List<NormalizationConformanceVector>();
        int part = -1;
        int partBatch = 0;
        int vectorCount = 0;
        string archivePath = FindUnicodeArchive();

        using (System.IO.Compression.ZipArchive archive =
            System.IO.Compression.ZipFile.OpenRead(archivePath))
        {
            System.IO.Compression.ZipArchiveEntry entry = RequireEntry(
                archive,
                "NormalizationTest.txt");

            using (System.IO.StreamReader reader =
                new System.IO.StreamReader(entry.Open()))
            {
                string? line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    string content = RemoveComment(line).Trim();

                    if (content.StartsWith(
                        "@Part",
                        System.StringComparison.Ordinal))
                    {
                        AddBatch(cases, vectors, part, partBatch);
                        vectors = new List<NormalizationConformanceVector>();
                        part = ParsePart(content, lineNumber);
                        partBatch = 0;
                    }
                    else if (content.Length > 0 && content[0] != '@')
                    {
                        vectors.Add(ParseVector(content, part, lineNumber));
                        vectorCount++;

                        if (vectors.Count == BatchSize)
                        {
                            AddBatch(cases, vectors, part, partBatch);
                            vectors =
                                new List<NormalizationConformanceVector>();
                            partBatch++;
                        }
                    }
                }
            }
        }

        AddBatch(cases, vectors, part, partBatch);

        if (vectorCount != ExpectedVectorCount)
        {
            throw new AssertionException(
                "Unicode NormalizationTest vector count differs: expected " +
                ExpectedVectorCount + ", actual " + vectorCount + ".");
        }

        return cases.ToArray();
    }

    static NUnit.Framework.TestCaseData[] IdentityBatches()
    {
        List<NUnit.Framework.TestCaseData> cases =
            new List<NUnit.Framework.TestCaseData>();
        List<int> codePoints = new List<int>();
        int identityCount = 0;
        string archivePath = FindUnicodeArchive();

        using (System.IO.Compression.ZipArchive archive =
            System.IO.Compression.ZipFile.OpenRead(archivePath))
        {
            HashSet<int> changed = ReadPartOneCodePoints(archive);
            System.IO.Compression.ZipArchiveEntry entry = RequireEntry(
                archive,
                "UnicodeData.txt");

            using (System.IO.StreamReader reader =
                new System.IO.StreamReader(entry.Open()))
            {
                string? line;
                int lineNumber = 0;
                int rangeStart = -1;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    string[] fields = line.Split(';');

                    if (fields.Length < 2)
                    {
                        throw new AssertionException(
                            "Invalid UnicodeData record at line " +
                            lineNumber + ".");
                    }

                    int codePoint = ParseUnicodeDataCodePoint(
                        fields[0],
                        lineNumber);
                    string name = fields[1];

                    if (name.EndsWith(
                        ", First>",
                        System.StringComparison.Ordinal))
                    {
                        if (rangeStart >= 0)
                        {
                            throw new AssertionException(
                                "Nested UnicodeData range at line " +
                                lineNumber + ".");
                        }

                        rangeStart = codePoint;
                    }
                    else if (name.EndsWith(
                        ", Last>",
                        System.StringComparison.Ordinal))
                    {
                        if (rangeStart < 0 || rangeStart > codePoint)
                        {
                            throw new AssertionException(
                                "Invalid UnicodeData range at line " +
                                lineNumber + ".");
                        }

                        int current = rangeStart;

                        while (current <= codePoint)
                        {
                            if (AddIdentityCodePoint(
                                cases,
                                codePoints,
                                changed,
                                current))
                            {
                                identityCount++;
                            }

                            current++;
                        }

                        rangeStart = -1;
                    }
                    else
                    {
                        if (AddIdentityCodePoint(
                            cases,
                            codePoints,
                            changed,
                            codePoint))
                        {
                            identityCount++;
                        }
                    }
                }

                if (rangeStart >= 0)
                {
                    throw new AssertionException(
                        "UnicodeData ends inside a range.");
                }
            }
        }

        AddIdentityBatch(cases, codePoints);

        if (identityCount != ExpectedIdentityCount)
        {
            throw new AssertionException(
                "Unicode normalization identity count differs: expected " +
                ExpectedIdentityCount + ", actual " + identityCount + ".");
        }

        return cases.ToArray();
    }

    static HashSet<int> ReadPartOneCodePoints(
        System.IO.Compression.ZipArchive archive)
    {
        HashSet<int> codePoints = new HashSet<int>();
        System.IO.Compression.ZipArchiveEntry entry = RequireEntry(
            archive,
            "NormalizationTest.txt");

        using (System.IO.StreamReader reader =
            new System.IO.StreamReader(entry.Open()))
        {
            string? line;
            int lineNumber = 0;
            int part = -1;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                string content = RemoveComment(line).Trim();

                if (content.StartsWith(
                    "@Part",
                    System.StringComparison.Ordinal))
                {
                    part = ParsePart(content, lineNumber);
                }
                else if (part == 1 && content.Length > 0)
                {
                    string[] fields = content.Split(';');
                    string[] tokens = fields[0].Trim().Split(
                        new char[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length != 1)
                    {
                        throw new AssertionException(
                            "NormalizationTest Part 1 source is not one " +
                            "code point at line " + lineNumber + ".");
                    }

                    int codePoint = ParseHexCodePoint(
                        tokens[0],
                        "NormalizationTest",
                        lineNumber);

                    if (!codePoints.Add(codePoint))
                    {
                        throw new AssertionException(
                            "Duplicate NormalizationTest Part 1 source at " +
                            "line " + lineNumber + ".");
                    }
                }
            }
        }

        return codePoints;
    }

    static bool AddIdentityCodePoint(
        List<NUnit.Framework.TestCaseData> cases,
        List<int> codePoints,
        HashSet<int> changed,
        int codePoint)
    {
        if (!IsIdentityCodePoint(changed, codePoint))
        {
            return false;
        }

        codePoints.Add(codePoint);

        if (codePoints.Count == IdentityBatchSize)
        {
            AddIdentityBatch(cases, codePoints);
            codePoints.Clear();
        }

        return true;
    }

    static bool IsIdentityCodePoint(HashSet<int> changed, int codePoint)
    {
        return (codePoint < 0xD800 || codePoint > 0xDFFF) &&
            !changed.Contains(codePoint);
    }

    static void AddIdentityBatch(
        List<NUnit.Framework.TestCaseData> cases,
        List<int> codePoints)
    {
        if (codePoints.Count == 0)
        {
            return;
        }

        NormalizationIdentityBatch value = new NormalizationIdentityBatch(
            codePoints.ToArray());
        NUnit.Framework.TestCaseData testCase =
            new NUnit.Framework.TestCaseData(value);
        testCase.SetName(
            "Unicode17_Normalization_Identity_Batch" + cases.Count);
        cases.Add(testCase);
    }

    static void AddBatch(
        List<NUnit.Framework.TestCaseData> cases,
        List<NormalizationConformanceVector> vectors,
        int part,
        int batch)
    {
        if (vectors.Count == 0)
        {
            return;
        }

        NormalizationConformanceBatch value =
            new NormalizationConformanceBatch(
                vectors.ToArray());
        NUnit.Framework.TestCaseData testCase =
            new NUnit.Framework.TestCaseData(value);
        testCase.SetName(
            "Unicode17_Normalization_Part" + part + "_Batch" + batch);
        cases.Add(testCase);
    }

    static NormalizationConformanceVector ParseVector(
        string content,
        int part,
        int lineNumber)
    {
        if (part < 0)
        {
            throw new AssertionException(
                "Normalization vector precedes a part at line " +
                lineNumber + ".");
        }

        string[] fields = content.Split(';');

        if (fields.Length < 6)
        {
            throw new AssertionException(
                "Invalid NormalizationTest record at line " +
                lineNumber + ".");
        }

        return new NormalizationConformanceVector(
            part,
            lineNumber,
            ParseCodePoints(fields[0], lineNumber),
            ParseCodePoints(fields[1], lineNumber),
            ParseCodePoints(fields[2], lineNumber),
            ParseCodePoints(fields[3], lineNumber),
            ParseCodePoints(fields[4], lineNumber));
    }

    static string ParseCodePoints(string field, int lineNumber)
    {
        string[] tokens = field.Trim().Split(
            new char[] { ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;

        while (index < tokens.Length)
        {
            int codePoint = ParseHexCodePoint(
                tokens[index],
                "NormalizationTest",
                lineNumber);

            text.Append(new System.Text.Rune(codePoint).ToString());
            index++;
        }

        return text.ToString();
    }

    static int ParseHexCodePoint(
        string value,
        string source,
        int lineNumber)
    {
        int codePoint = System.Convert.ToInt32(value, 16);

        if (!System.Text.Rune.IsValid(codePoint))
        {
            throw new AssertionException(
                "Invalid code point in " + source + " at line " +
                lineNumber + ".");
        }

        return codePoint;
    }

    static int ParseUnicodeDataCodePoint(string value, int lineNumber)
    {
        int codePoint = System.Convert.ToInt32(value, 16);

        if (codePoint < 0 || codePoint > 0x10FFFF)
        {
            throw new AssertionException(
                "Invalid code point in UnicodeData at line " +
                lineNumber + ".");
        }

        return codePoint;
    }

    static int ParsePart(string content, int lineNumber)
    {
        if (content.Length != 6 ||
            content[5] < '0' || content[5] > '9')
        {
            throw new AssertionException(
                "Invalid NormalizationTest part at line " + lineNumber + ".");
        }

        return content[5] - '0';
    }

    static void ValidateVector(
        UnicodeData unicodeData,
        NormalizationConformanceVector vector)
    {
        string c1 = vector.GetColumn(0);
        string c2 = vector.GetColumn(1);
        string c3 = vector.GetColumn(2);
        string c4 = vector.GetColumn(3);
        string c5 = vector.GetColumn(4);

        AssertForm(unicodeData, vector, 1, c1,
            UnicodeNormalizationMode.Nfc, c2);
        AssertForm(unicodeData, vector, 2, c2,
            UnicodeNormalizationMode.Nfc, c2);
        AssertForm(unicodeData, vector, 3, c3,
            UnicodeNormalizationMode.Nfc, c2);
        AssertForm(unicodeData, vector, 4, c4,
            UnicodeNormalizationMode.Nfc, c4);
        AssertForm(unicodeData, vector, 5, c5,
            UnicodeNormalizationMode.Nfc, c4);

        AssertForm(unicodeData, vector, 1, c1,
            UnicodeNormalizationMode.Nfd, c3);
        AssertForm(unicodeData, vector, 2, c2,
            UnicodeNormalizationMode.Nfd, c3);
        AssertForm(unicodeData, vector, 3, c3,
            UnicodeNormalizationMode.Nfd, c3);
        AssertForm(unicodeData, vector, 4, c4,
            UnicodeNormalizationMode.Nfd, c5);
        AssertForm(unicodeData, vector, 5, c5,
            UnicodeNormalizationMode.Nfd, c5);

        int column = 0;

        while (column < 5)
        {
            AssertForm(
                unicodeData,
                vector,
                column + 1,
                vector.GetColumn(column),
                UnicodeNormalizationMode.Nfkc,
                c4);
            AssertForm(
                unicodeData,
                vector,
                column + 1,
                vector.GetColumn(column),
                UnicodeNormalizationMode.Nfkd,
                c5);
            column++;
        }
    }

    static void AssertForm(
        UnicodeData unicodeData,
        NormalizationConformanceVector vector,
        int column,
        string input,
        UnicodeNormalizationMode mode,
        string expected)
    {
        RuneBuffer inputBuffer = Utf16Decoder.Decode(
            input,
            Utf16DecodingMode.Strict,
            CountRunes(input));
        RuneBuffer actualBuffer = UnicodeNormalizer.Normalize(
            inputBuffer,
            unicodeData,
            mode,
            vector.GetMaximumRuneCount());
        string actual = ToText(actualBuffer);

        if (actual != expected)
        {
            Assert.Fail(
                "Unicode NormalizationTest mismatch in part " +
                vector.GetPart() + ", line " + vector.GetLineNumber() +
                ", column c" + column + ", form " + mode +
                ". Expected " + ToCodePoints(expected) + ", actual " +
                ToCodePoints(actual) + ".");
        }
    }

    static void AssertIdentity(
        UnicodeData unicodeData,
        int codePoint,
        UnicodeNormalizationMode mode)
    {
        string expected = new System.Text.Rune(codePoint).ToString();
        RuneBuffer input = Utf16Decoder.Decode(
            expected,
            Utf16DecodingMode.Strict,
            1);
        RuneBuffer actualBuffer = UnicodeNormalizer.Normalize(
            input,
            unicodeData,
            mode,
            32);
        string actual = ToText(actualBuffer);

        if (actual != expected)
        {
            Assert.Fail(
                "Unicode NormalizationTest identity mismatch for U+" +
                codePoint.ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture) +
                ", form " + mode + ". Actual " +
                ToCodePoints(actual) + ".");
        }
    }

    static int CountRunes(string text)
    {
        int count = 0;
        int index = 0;

        while (index < text.Length)
        {
            index += char.IsHighSurrogate(text[index]) ? 2 : 1;
            count++;
        }

        return count;
    }

    static string ToText(RuneBuffer buffer)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;

        while (index < buffer.GetCount())
        {
            text.Append(buffer.GetRune(index).ToString());
            index++;
        }

        return text.ToString();
    }

    static string ToCodePoints(string text)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;

        while (index < text.Length)
        {
            int codePoint = text[index];
            int consumed = 1;

            if (char.IsHighSurrogate(text[index]))
            {
                if (index + 1 >= text.Length ||
                    !char.IsLowSurrogate(text[index + 1]))
                {
                    throw new AssertionException(
                        "Normalization result contains invalid UTF-16.");
                }

                codePoint = 0x10000 +
                    ((text[index] - 0xD800) << 10) +
                    (text[index + 1] - 0xDC00);
                consumed = 2;
            }

            if (result.Length > 0)
            {
                result.Append(' ');
            }

            result.Append(codePoint.ToString(
                "X4",
                System.Globalization.CultureInfo.InvariantCulture));
            index += consumed;
        }

        return result.ToString();
    }

    static string RemoveComment(string line)
    {
        int comment = line.IndexOf('#');
        return comment < 0 ? line : line.Substring(0, comment);
    }

    static System.IO.Compression.ZipArchiveEntry RequireEntry(
        System.IO.Compression.ZipArchive archive,
        string name)
    {
        System.IO.Compression.ZipArchiveEntry? entry = archive.GetEntry(name);

        if (entry == null)
        {
            throw new AssertionException(
                name + " is missing from the Unicode archive.");
        }

        return entry;
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

public class NormalizationConformanceBatch
{
    NormalizationConformanceVector[] vectors;

    internal NormalizationConformanceBatch(
        NormalizationConformanceVector[] vectors)
    {
        this.vectors = vectors;
    }

    internal int GetCount()
    {
        return vectors.Length;
    }

    internal NormalizationConformanceVector GetVector(int index)
    {
        return vectors[index];
    }
}

public class NormalizationIdentityBatch
{
    int[] codePoints;

    internal NormalizationIdentityBatch(int[] codePoints)
    {
        this.codePoints = codePoints;
    }

    internal int GetCount()
    {
        return codePoints.Length;
    }

    internal int GetCodePoint(int index)
    {
        return codePoints[index];
    }
}

public class NormalizationConformanceVector
{
    int part;
    int lineNumber;
    string[] columns;
    int maximumRuneCount;

    internal NormalizationConformanceVector(
        int part,
        int lineNumber,
        string c1,
        string c2,
        string c3,
        string c4,
        string c5)
    {
        this.part = part;
        this.lineNumber = lineNumber;
        columns = new string[] { c1, c2, c3, c4, c5 };
        maximumRuneCount = CalculateMaximumRuneCount();
    }

    internal int GetPart()
    {
        return part;
    }

    internal int GetLineNumber()
    {
        return lineNumber;
    }

    internal string GetColumn(int index)
    {
        return columns[index];
    }

    internal int GetMaximumRuneCount()
    {
        return maximumRuneCount;
    }

    int CalculateMaximumRuneCount()
    {
        int maximum = 0;
        int column = 0;

        while (column < columns.Length)
        {
            int count = 0;
            int index = 0;

            while (index < columns[column].Length)
            {
                index += char.IsHighSurrogate(columns[column][index]) ? 2 : 1;
                count++;
            }

            if (count > maximum)
            {
                maximum = count;
            }

            column++;
        }

        return maximum;
    }
}
