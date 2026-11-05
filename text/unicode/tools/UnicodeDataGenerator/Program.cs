namespace unicode.Tools.UnicodeDataGenerator;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "help")
        {
            PrintUsage();
            return 0;
        }

        if (args[0] == "version")
        {
            Console.WriteLine(UnicodeDataFiles.UnicodeVersion);
            return 0;
        }

        if (args[0] == "list")
        {
            PrintRequiredFiles();
            return 0;
        }

        if (args[0] == "verify")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("verify requires a data directory path.");
                return 2;
            }

            UnicodeDataManifest.Verify(args[1], Path.Combine(args[1], "checksums.sha256"));
            Console.WriteLine("Verified " + UnicodeDataFiles.UnicodeVersion + " Unicode data.");
            return 0;
        }

        if (args[0] == "download")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("download requires a data directory path.");
                return 2;
            }

            UnicodeDataManifest.DownloadAndVerify(args[1], Path.Combine(args[1], "checksums.sha256"));
            Console.WriteLine("Downloaded and verified " + UnicodeDataFiles.UnicodeVersion + " Unicode data.");
            return 0;
        }

        if (args[0] == "strip-comments")
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("strip-comments requires input and output paths.");
                return 2;
            }

            UnicodeTestDataWriter.WriteDataLines(args[2], UnicodeDataParser.ReadDataLines(args[1]));
            return 0;
        }

        if (args[0] == "parse-unicodedata")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-unicodedata requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadUnicodeData(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-property-aliases")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-property-aliases requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadPropertyAliases(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-property-value-aliases")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-property-value-aliases requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadPropertyValueAliases(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-property-ranges")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-property-ranges requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadPropertyRanges(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-scripts")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-scripts requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadScripts(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-script-extensions")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-script-extensions requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadScriptExtensions(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-blocks")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-blocks requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadBlocks(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-derived-age")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-derived-age requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadDerivedAge(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-east-asian-width")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-east-asian-width requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadEastAsianWidth(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-bidi-mirroring")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-bidi-mirroring requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadBidiMirroring(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-bidi-brackets")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-bidi-brackets requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadBidiBrackets(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-joining")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-joining requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadJoiningData(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-line-break")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-line-break requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadLineBreak(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-grapheme-break")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-grapheme-break requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadGraphemeBreakProperty(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-word-break")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-word-break requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadWordBreakProperty(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-sentence-break")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-sentence-break requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadSentenceBreakProperty(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-indic-conjunct-break")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-indic-conjunct-break requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadIndicConjunctBreak(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-hangul-syllable-type")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-hangul-syllable-type requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadHangulSyllableType(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-special-casing")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-special-casing requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadSpecialCasing(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-case-folding")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-case-folding requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadCaseFolding(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-normalization-quick-checks")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-normalization-quick-checks requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadNormalizationQuickChecks(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-composition-exclusions")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-composition-exclusions requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadCompositionExclusions(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-decomposition-mappings")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-decomposition-mappings requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadDecompositionMappings(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-normalization-test")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-normalization-test requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadNormalizationTest(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-grapheme-break-test")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-grapheme-break-test requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadGraphemeBreakTest(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-word-break-test")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-word-break-test requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadWordBreakTest(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-sentence-break-test")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-sentence-break-test requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadSentenceBreakTest(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-line-break-test")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-line-break-test requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadLineBreakTest(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-confusables")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-confusables requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadConfusables(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-identifier-status")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-identifier-status requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadIdentifierStatus(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-identifier-type")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-identifier-type requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadIdentifierType(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-emoji-data")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-emoji-data requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadEmojiData(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-emoji-sequences")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-emoji-sequences requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadEmojiSequences(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-emoji-zwj-sequences")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-emoji-zwj-sequences requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadEmojiZwjSequences(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "parse-emoji-variation-sequences")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("parse-emoji-variation-sequences requires an input path.");
                return 2;
            }

            Console.WriteLine(UnicodeDataParser.ReadEmojiVariationSequences(args[1]).Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        if (args[0] == "write-placeholder")
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("write-placeholder requires output path and class name.");
                return 2;
            }

            UnicodeTableWriter.WritePlaceholderSource(args[1], args[2]);
            return 0;
        }

        if (args[0] == "write-core-tables")
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("write-core-tables requires data directory and output directory paths.");
                return 2;
            }

            UnicodeTableWriter.WriteCoreTables(args[1], args[2]);
            return 0;
        }

        if (args[0] == "write-property-fixture")
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("write-property-fixture requires data directory and output path.");
                return 2;
            }

            UnicodeTestDataWriter.WritePropertyRegressionFixture(args[1], args[2]);
            return 0;
        }

        Console.Error.WriteLine("Unknown command: " + args[0]);
        return 1;
    }

    static void PrintUsage()
    {
        Console.WriteLine("UnicodeDataGenerator " + UnicodeDataFiles.UnicodeVersion);
        Console.WriteLine("Commands:");
        Console.WriteLine("  help");
        Console.WriteLine("  version");
        Console.WriteLine("  list");
        Console.WriteLine("  verify <dataDir>");
        Console.WriteLine("  download <dataDir>");
        Console.WriteLine("  strip-comments <input> <output>");
        Console.WriteLine("  parse-unicodedata <input>");
        Console.WriteLine("  parse-property-aliases <input>");
        Console.WriteLine("  parse-property-value-aliases <input>");
        Console.WriteLine("  parse-property-ranges <input>");
        Console.WriteLine("  parse-scripts <input>");
        Console.WriteLine("  parse-script-extensions <input>");
        Console.WriteLine("  parse-blocks <input>");
        Console.WriteLine("  parse-derived-age <input>");
        Console.WriteLine("  parse-east-asian-width <input>");
        Console.WriteLine("  parse-bidi-mirroring <input>");
        Console.WriteLine("  parse-bidi-brackets <input>");
        Console.WriteLine("  parse-joining <input>");
        Console.WriteLine("  parse-line-break <input>");
        Console.WriteLine("  parse-grapheme-break <input>");
        Console.WriteLine("  parse-word-break <input>");
        Console.WriteLine("  parse-sentence-break <input>");
        Console.WriteLine("  parse-indic-conjunct-break <input>");
        Console.WriteLine("  parse-hangul-syllable-type <input>");
        Console.WriteLine("  parse-special-casing <input>");
        Console.WriteLine("  parse-case-folding <input>");
        Console.WriteLine("  parse-normalization-quick-checks <input>");
        Console.WriteLine("  parse-composition-exclusions <input>");
        Console.WriteLine("  parse-decomposition-mappings <input>");
        Console.WriteLine("  parse-normalization-test <input>");
        Console.WriteLine("  parse-grapheme-break-test <input>");
        Console.WriteLine("  parse-word-break-test <input>");
        Console.WriteLine("  parse-sentence-break-test <input>");
        Console.WriteLine("  parse-line-break-test <input>");
        Console.WriteLine("  parse-confusables <input>");
        Console.WriteLine("  parse-identifier-status <input>");
        Console.WriteLine("  parse-identifier-type <input>");
        Console.WriteLine("  parse-emoji-data <input>");
        Console.WriteLine("  parse-emoji-sequences <input>");
        Console.WriteLine("  parse-emoji-zwj-sequences <input>");
        Console.WriteLine("  parse-emoji-variation-sequences <input>");
        Console.WriteLine("  write-placeholder <output> <className>");
        Console.WriteLine("  write-core-tables <dataDir> <outputDir>");
        Console.WriteLine("  write-property-fixture <dataDir> <outputPath>");
    }

    static void PrintRequiredFiles()
    {
        string[] fileNames = UnicodeDataFiles.GetRequiredFileNames();
        int index = 0;

        while (index < fileNames.Length)
        {
            Console.WriteLine(fileNames[index] + " " + UnicodeDataFiles.GetDownloadUrl(fileNames[index]));
            index = index + 1;
        }
    }
}
