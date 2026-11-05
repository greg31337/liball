namespace unicode.Tools.UnicodeDataGenerator;

public static class UnicodeTestDataWriter
{
    public static void WriteDataLines(string outputPath, string[] lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(lines);

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(outputPath, lines);
    }

    public static void WritePropertyRegressionFixture(string dataDirectory, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        UnicodeDataRecord[] unicodeData = UnicodeDataParser.ReadUnicodeData(Path.Combine(dataDirectory, "UnicodeData.txt"));
        UnicodePropertyRangeRecord[] propList = UnicodeDataParser.ReadPropList(Path.Combine(dataDirectory, "PropList.txt"));
        UnicodePropertyRangeRecord[] derivedCoreProperties = UnicodeDataParser.ReadDerivedCoreProperties(Path.Combine(dataDirectory, "DerivedCoreProperties.txt"));
        UnicodePropertyRangeRecord[] scripts = UnicodeDataParser.ReadScripts(Path.Combine(dataDirectory, "Scripts.txt"));
        UnicodePropertyRangeRecord[] blocks = UnicodeDataParser.ReadBlocks(Path.Combine(dataDirectory, "Blocks.txt"));
        UnicodePropertyRangeRecord[] eastAsianWidths = UnicodeDataParser.ReadEastAsianWidth(Path.Combine(dataDirectory, "EastAsianWidth.txt"));
        UnicodePropertyRangeRecord[] binaryProperties = CombinePropertyRanges(propList, derivedCoreProperties);
        Dictionary<int, UnicodeDataRecord> recordsByCodePoint = CreateRecordLookup(unicodeData);
        SortedSet<int> codePoints = new SortedSet<int>();

        AddUnicodeDataCodePoints(codePoints, unicodeData);
        AddRangeSampleCodePoints(codePoints, propList);
        AddRangeSampleCodePoints(codePoints, derivedCoreProperties);
        AddRangeSampleCodePoints(codePoints, scripts);
        AddRangeSampleCodePoints(codePoints, blocks);
        AddRangeSampleCodePoints(codePoints, eastAsianWidths);

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using StreamWriter writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("CodePoint\tName\tGeneralCategory\tCombiningClass\tBidiClass\tScript\tBlock\tEastAsianWidth\tNumericValue\tBinaryProperties");

        foreach (int codePoint in codePoints)
        {
            UnicodeDataRecord record = FindUnicodeDataRecord(recordsByCodePoint, codePoint);
            writer.WriteLine(
                FormatCodePoint(codePoint) + "\t" +
                CleanField(GetName(record)) + "\t" +
                CleanField(GetGeneralCategory(record)) + "\t" +
                GetCombiningClass(record).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\t" +
                CleanField(GetBidiClass(record)) + "\t" +
                CleanField(FindRangeValue(scripts, codePoint, "Unknown")) + "\t" +
                CleanField(FindRangeValue(blocks, codePoint, "No_Block")) + "\t" +
                CleanField(FindRangeValue(eastAsianWidths, codePoint, "N")) + "\t" +
                CleanField(GetNumericValue(record)) + "\t" +
                CleanField(GetBinaryProperties(binaryProperties, codePoint)));
        }
    }

    static UnicodePropertyRangeRecord[] CombinePropertyRanges(UnicodePropertyRangeRecord[] first, UnicodePropertyRangeRecord[] second)
    {
        UnicodePropertyRangeRecord[] combined = new UnicodePropertyRangeRecord[first.Length + second.Length];
        int index = 0;

        while (index < first.Length)
        {
            combined[index] = first[index];
            index = index + 1;
        }

        int secondIndex = 0;

        while (secondIndex < second.Length)
        {
            combined[index] = second[secondIndex];
            index = index + 1;
            secondIndex = secondIndex + 1;
        }

        return combined;
    }

    static Dictionary<int, UnicodeDataRecord> CreateRecordLookup(UnicodeDataRecord[] records)
    {
        Dictionary<int, UnicodeDataRecord> lookup = new Dictionary<int, UnicodeDataRecord>();
        int index = 0;

        while (index < records.Length)
        {
            lookup[records[index].CodePoint] = records[index];
            index = index + 1;
        }

        return lookup;
    }

    static void AddUnicodeDataCodePoints(SortedSet<int> codePoints, UnicodeDataRecord[] records)
    {
        int index = 0;

        while (index < records.Length)
        {
            codePoints.Add(records[index].CodePoint);
            index = index + 1;
        }
    }

    static void AddRangeSampleCodePoints(SortedSet<int> codePoints, UnicodePropertyRangeRecord[] records)
    {
        int index = 0;

        while (index < records.Length)
        {
            codePoints.Add(records[index].StartCodePoint);
            codePoints.Add(records[index].EndCodePoint);
            codePoints.Add(records[index].StartCodePoint + ((records[index].EndCodePoint - records[index].StartCodePoint) / 2));
            index = index + 1;
        }
    }

    static UnicodeDataRecord FindUnicodeDataRecord(Dictionary<int, UnicodeDataRecord> recordsByCodePoint, int codePoint)
    {
        if (recordsByCodePoint.TryGetValue(codePoint, out UnicodeDataRecord record))
        {
            return record;
        }

        return null;
    }

    static string GetName(UnicodeDataRecord record)
    {
        if (record == null)
        {
            return "";
        }

        return record.Name;
    }

    static string GetGeneralCategory(UnicodeDataRecord record)
    {
        if (record == null)
        {
            return "Cn";
        }

        return record.GeneralCategory;
    }

    static int GetCombiningClass(UnicodeDataRecord record)
    {
        if (record == null)
        {
            return 0;
        }

        return record.CombiningClass;
    }

    static string GetBidiClass(UnicodeDataRecord record)
    {
        if (record == null)
        {
            return "L";
        }

        return record.BidiClass;
    }

    static string GetNumericValue(UnicodeDataRecord record)
    {
        if (record == null)
        {
            return "";
        }

        if (!record.NumericValue.HasValue)
        {
            return "";
        }

        return record.NumericValue.Text;
    }

    static string FindRangeValue(UnicodePropertyRangeRecord[] records, int codePoint, string defaultValue)
    {
        int index = 0;

        while (index < records.Length)
        {
            if (records[index].Contains(codePoint))
            {
                return records[index].PropertyName;
            }

            index = index + 1;
        }

        return defaultValue;
    }

    static string GetBinaryProperties(UnicodePropertyRangeRecord[] records, int codePoint)
    {
        List<string> properties = new List<string>();
        int index = 0;

        while (index < records.Length)
        {
            if (records[index].Contains(codePoint))
            {
                properties.Add(records[index].PropertyName);
            }

            index = index + 1;
        }

        return string.Join("|", properties);
    }

    static string FormatCodePoint(int codePoint)
    {
        if (codePoint <= 65535)
        {
            return codePoint.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
        }

        return codePoint.ToString("X", System.Globalization.CultureInfo.InvariantCulture);
    }

    static string CleanField(string value)
    {
        return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
    }
}
