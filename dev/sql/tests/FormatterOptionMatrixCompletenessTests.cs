using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class FormatterOptionMatrixCompletenessTests
{
    static readonly SqlKeywordCase[] KeywordCases = new SqlKeywordCase[]
    {
        SqlKeywordCase.Preserve,
        SqlKeywordCase.Upper,
        SqlKeywordCase.Lower
    };

    static readonly SqlIdentifierCase[] IdentifierCases = new SqlIdentifierCase[]
    {
        SqlIdentifierCase.Preserve,
        SqlIdentifierCase.Upper,
        SqlIdentifierCase.Lower
    };

    static readonly SqlCommaStyle[] CommaStyles = new SqlCommaStyle[]
    {
        SqlCommaStyle.Inline,
        SqlCommaStyle.Leading
    };

    static readonly SqlFormatMode[] Modes = new SqlFormatMode[]
    {
        SqlFormatMode.Pretty,
        SqlFormatMode.Compact,
        SqlFormatMode.Minimal,
        SqlFormatMode.RoundTrip
    };

    [Test]
    public void Formatter_AllOptionCombinationsProduceStableParseableOutput()
    {
        const string source = "-- lead\nSeLeCt [Mixed Name] As MixedAlias,total+tax AmountDue FrOm [Order Table] WhErE active=true OrDeR By MixedAlias";
        int keywordIndex = 0;
        while (keywordIndex < KeywordCases.Length)
        {
            int identifierIndex = 0;
            while (identifierIndex < IdentifierCases.Length)
            {
                int commaIndex = 0;
                while (commaIndex < CommaStyles.Length)
                {
                    int modeIndex = 0;
                    while (modeIndex < Modes.Length)
                    {
                        AssertCombinationSet(source, KeywordCases[keywordIndex], IdentifierCases[identifierIndex], CommaStyles[commaIndex], Modes[modeIndex]);
                        modeIndex = modeIndex + 1;
                    }

                    commaIndex = commaIndex + 1;
                }

                identifierIndex = identifierIndex + 1;
            }

            keywordIndex = keywordIndex + 1;
        }
    }

    [TestCase(SqlFormatMode.Pretty, "SELECT MixedName, OtherName FROM SomeTable;")]
    [TestCase(SqlFormatMode.Compact, "SELECT MixedName, OtherName FROM SomeTable;")]
    [TestCase(SqlFormatMode.Minimal, "SELECT MixedName,OtherName FROM SomeTable;")]
    [TestCase(SqlFormatMode.RoundTrip, "select MixedName,OtherName from SomeTable")]
    public void Formatter_ModesHaveGoldenOutput(SqlFormatMode mode, string expected)
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.Mode = mode;
        options.KeywordCase = SqlKeywordCase.Upper;

        string output = new SqlFormatterService(new SqlParserService()).Format("select MixedName,OtherName from SomeTable", options);

        Assert.That(output, Is.EqualTo(expected));
    }

    [TestCase(SqlKeywordCase.Preserve, SqlIdentifierCase.Preserve, "SELECT MixedName FROM SomeTable;")]
    [TestCase(SqlKeywordCase.Upper, SqlIdentifierCase.Upper, "SELECT MIXEDNAME FROM SOMETABLE;")]
    [TestCase(SqlKeywordCase.Lower, SqlIdentifierCase.Lower, "select mixedname from sometable;")]
    [TestCase(SqlKeywordCase.Upper, SqlIdentifierCase.Lower, "SELECT mixedname FROM sometable;")]
    [TestCase(SqlKeywordCase.Lower, SqlIdentifierCase.Upper, "select MIXEDNAME from SOMETABLE;")]
    public void Formatter_KeywordAndIdentifierCaseHaveIndependentGoldenOutput(
        SqlKeywordCase keywordCase,
        SqlIdentifierCase identifierCase,
        string expected)
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = keywordCase;
        options.IdentifierCase = identifierCase;

        Assert.That(SqlFormatter.Format("select MixedName from SomeTable", options), Is.EqualTo(expected));
    }

    [Test]
    public void Formatter_ReadWriteDialectAndTrailingCommaOptionsCompose()
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.ReadDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.SqlServer);
        options.WriteDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.BigQuery);
        options.TrailingCommaPolicy = SqlTrailingCommaPolicy.WhenSupported;
        options.IdentifierCase = SqlIdentifierCase.Lower;

        string output = SqlFormatter.Format("select [Display Name] as DisplayAlias, [Order] from [Sales Data]", options);
        SqlParseResult reparsed = new SqlParserService().ParseScript(
            output,
            SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(SqlDialectKind.BigQuery)));

        Assert.That(output, Is.EqualTo("SELECT `Display Name` AS displayalias, `Order`, FROM `Sales Data`;"));
        Assert.That(reparsed.Diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Formatter_KeywordDialectControlsWhichGeneratedWordsAreCased()
    {
        SqlFormatOptions sqliteKeywords = new SqlFormatOptions();
        sqliteKeywords.KeywordCase = SqlKeywordCase.Lower;
        SqlFormatOptions genericKeywords = sqliteKeywords.Copy();
        genericKeywords.KeywordDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.Generic);

        Assert.That(SqlFormatter.Format("pragma cache_size = 1", sqliteKeywords), Is.EqualTo("pragma cache_size = 1;"));
        Assert.That(SqlFormatter.Format("pragma cache_size = 1", genericKeywords), Is.EqualTo("PRAGMA cache_size = 1;"));
    }

    static void AssertCombinationSet(
        string source,
        SqlKeywordCase keywordCase,
        SqlIdentifierCase identifierCase,
        SqlCommaStyle commaStyle,
        SqlFormatMode mode)
    {
        bool[] commentValues = new bool[] { false, true };
        int[] widthValues = new int[] { 0, 34 };
        SqlTrailingCommaPolicy[] trailingValues = new SqlTrailingCommaPolicy[]
        {
            SqlTrailingCommaPolicy.Never,
            SqlTrailingCommaPolicy.WhenSupported
        };
        int commentIndex = 0;
        while (commentIndex < commentValues.Length)
        {
            int widthIndex = 0;
            while (widthIndex < widthValues.Length)
            {
                int trailingIndex = 0;
                while (trailingIndex < trailingValues.Length)
                {
                    AssertCombination(
                        source,
                        keywordCase,
                        identifierCase,
                        commaStyle,
                        mode,
                        commentValues[commentIndex],
                        widthValues[widthIndex],
                        trailingValues[trailingIndex]);
                    trailingIndex = trailingIndex + 1;
                }

                widthIndex = widthIndex + 1;
            }

            commentIndex = commentIndex + 1;
        }
    }

    static void AssertCombination(
        string source,
        SqlKeywordCase keywordCase,
        SqlIdentifierCase identifierCase,
        SqlCommaStyle commaStyle,
        SqlFormatMode mode,
        bool preserveComments,
        int targetLineWidth,
        SqlTrailingCommaPolicy trailingCommaPolicy)
    {
        SqlFormatOptions options = new SqlFormatOptions();
        options.KeywordCase = keywordCase;
        options.IdentifierCase = identifierCase;
        options.CommaStyle = commaStyle;
        options.Mode = mode;
        options.PreserveComments = preserveComments;
        options.TargetLineWidth = targetLineWidth;
        options.ReadDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.SQLite);
        options.WriteDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.BigQuery);
        options.TrailingCommaPolicy = trailingCommaPolicy;

        string output = SqlFormatter.Format(source, options);
        string description = keywordCase + "/" + identifierCase + "/" + commaStyle + "/" + mode + "/comments=" + preserveComments + "/width=" + targetLineWidth + "/trailing=" + trailingCommaPolicy;
        if (mode == SqlFormatMode.RoundTrip)
        {
            Assert.That(output, Is.EqualTo(source), description);
            return;
        }

        SqlParseOptions parseOptions = SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(SqlDialectKind.BigQuery));
        SqlParseResult reparsed = new SqlParserService().ParseScript(output, parseOptions);
        Assert.That(reparsed.Diagnostics.HasErrors(), Is.False, description + FirstDiagnostic(reparsed.Diagnostics));

        SqlFormatOptions secondOptions = options.Copy();
        secondOptions.ReadDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.BigQuery);
        string second = SqlFormatter.Format(output, secondOptions);
        Assert.That(second, Is.EqualTo(output), description);
    }

    static string FirstDiagnostic(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.IsEmpty())
        {
            return "";
        }

        return ": " + diagnostics.GetAt(0).Code + " " + diagnostics.GetAt(0).Message;
    }
}
