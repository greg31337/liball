using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class PublicOptionContractTests
{
    [Test]
    public void FormattingOptions_DefaultsAndCopyIncludeEverySetting()
    {
        SqlFormatOptions options = new SqlFormatOptions();

        Assert.That(options.KeywordCase, Is.EqualTo(SqlKeywordCase.Preserve));
        Assert.That(options.IdentifierCase, Is.EqualTo(SqlIdentifierCase.Preserve));
        Assert.That(options.IndentText, Is.EqualTo("    "));
        Assert.That(options.CommaStyle, Is.EqualTo(SqlCommaStyle.Inline));
        Assert.That(options.TrailingCommaPolicy, Is.EqualTo(SqlTrailingCommaPolicy.Never));
        Assert.That(options.Mode, Is.EqualTo(SqlFormatMode.Pretty));
        Assert.That(options.TargetLineWidth, Is.EqualTo(0));
        Assert.That(options.PreserveComments, Is.False);
        Assert.That(options.KeywordDialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(options.ReadDialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(options.WriteDialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));

        options.IdentifierCase = SqlIdentifierCase.Lower;
        options.TrailingCommaPolicy = SqlTrailingCommaPolicy.WhenSupported;
        options.Mode = SqlFormatMode.RoundTrip;
        options.TargetLineWidth = 120;
        options.ReadDialect = GenericDialect.CreateOptions();
        options.WriteDialect = SqlDialectCatalog.CreateOptions(SqlDialectKind.PostgreSql);

        SqlFormatOptions copy = options.Copy();

        Assert.That(copy, Is.Not.SameAs(options));
        Assert.That(copy.IdentifierCase, Is.EqualTo(SqlIdentifierCase.Lower));
        Assert.That(copy.TrailingCommaPolicy, Is.EqualTo(SqlTrailingCommaPolicy.WhenSupported));
        Assert.That(copy.Mode, Is.EqualTo(SqlFormatMode.RoundTrip));
        Assert.That(copy.TargetLineWidth, Is.EqualTo(120));
        Assert.That(copy.ReadDialect, Is.SameAs(options.ReadDialect));
        Assert.That(copy.WriteDialect, Is.SameAs(options.WriteDialect));
    }

    [Test]
    public void FormattingOptions_RejectEveryInvalidReferenceEnumAndBoundary()
    {
        AssertInvalidFormat((options) => options.IndentText = null!, typeof(ArgumentNullException));
        AssertInvalidFormat((options) => options.KeywordDialect = null!, typeof(ArgumentNullException));
        AssertInvalidFormat((options) => options.ReadDialect = null!, typeof(ArgumentNullException));
        AssertInvalidFormat((options) => options.WriteDialect = null!, typeof(ArgumentNullException));
        AssertInvalidFormat((options) => options.KeywordCase = (SqlKeywordCase)(-1), typeof(ArgumentOutOfRangeException));
        AssertInvalidFormat((options) => options.IdentifierCase = (SqlIdentifierCase)99, typeof(ArgumentOutOfRangeException));
        AssertInvalidFormat((options) => options.CommaStyle = (SqlCommaStyle)99, typeof(ArgumentOutOfRangeException));
        AssertInvalidFormat((options) => options.TrailingCommaPolicy = (SqlTrailingCommaPolicy)99, typeof(ArgumentOutOfRangeException));
        AssertInvalidFormat((options) => options.Mode = (SqlFormatMode)99, typeof(ArgumentOutOfRangeException));
        AssertInvalidFormat((options) => options.TargetLineWidth = -1, typeof(ArgumentOutOfRangeException));

        SqlFormatOptions zeroWidth = new SqlFormatOptions();
        zeroWidth.TargetLineWidth = 0;
        Assert.DoesNotThrow(() => zeroWidth.Validate());
    }

    [Test]
    public void ParseOptions_DescriptorFactoryValidatesDescriptorAndReturnedOptions()
    {
        Assert.Throws<ArgumentNullException>(() => SqlParseOptions.CreateForDialect(null!));
        Assert.Throws<ArgumentNullException>(() => SqlParseOptions.CreateForDialect(new NullOptionsDialect()));

        SqlParseOptions options = SqlParseOptions.CreateForDialect(SqlDialectCatalog.GetDialect(SqlDialectKind.Ansi));

        Assert.That(options.GetDialectKind(), Is.EqualTo(SqlDialectKind.Ansi));
    }

    [Test]
    public void TransformOptions_ExposeStableDefaultsAndValidateNestedSettings()
    {
        SqlCanonicalizationOptions canonical = new SqlCanonicalizationOptions();
        SqlNormalizationOptions normalization = new SqlNormalizationOptions();
        SqlTranspileOptions transpile = new SqlTranspileOptions();
        SqlRewriteOptions rewrite = new SqlRewriteOptions();

        Assert.That(canonical.Dialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(canonical.KeywordCase, Is.EqualTo(SqlKeywordCase.Upper));
        Assert.That(canonical.IdentifierCase, Is.EqualTo(SqlIdentifierCase.Lower));
        Assert.That(canonical.PreserveComments, Is.False);
        Assert.That(canonical.IncludeSemicolons, Is.True);
        Assert.That(normalization.Dialect.Kind, Is.EqualTo(SqlDialectKind.SQLite));
        Assert.That(normalization.KeywordCase, Is.EqualTo(SqlKeywordCase.Upper));
        Assert.That(normalization.IdentifierCase, Is.EqualTo(SqlIdentifierCase.Preserve));
        Assert.That(normalization.Mode, Is.EqualTo(SqlFormatMode.Compact));
        Assert.That(normalization.PreserveComments, Is.False);
        Assert.That(normalization.IncludeSemicolons, Is.True);
        Assert.That(transpile.ReadDialect.Kind, Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(transpile.WriteDialect.Kind, Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(transpile.FormatOptions.Mode, Is.EqualTo(SqlFormatMode.Compact));
        Assert.That(transpile.IncludeSemicolons, Is.True);
        Assert.That(rewrite.PreserveSpans, Is.True);
        Assert.That(rewrite.PreserveTokens, Is.True);
        Assert.That(rewrite.PreserveTrivia, Is.True);
        Assert.That(rewrite.PreserveSourceText, Is.True);
        Assert.That(rewrite.PreserveDiagnostics, Is.True);

        canonical.Dialect = null!;
        normalization.Dialect = null!;
        Assert.Throws<ArgumentNullException>(() => canonical.Validate());
        Assert.Throws<ArgumentNullException>(() => normalization.Validate());
    }

    [Test]
    public void GenerateAndValidationOptions_CopyAllSettingsIndependently()
    {
        SqlGenerateOptions generate = new SqlGenerateOptions();
        generate.FormatOptions.KeywordCase = SqlKeywordCase.Lower;
        generate.WriteDialect = SqlDialectCatalog.CreateOptions(SqlDialectKind.BigQuery);
        generate.IncludeSemicolons = false;
        generate.PreserveSourceText = true;

        SqlGenerateOptions generateCopy = generate.Copy();

        Assert.That(generateCopy.FormatOptions, Is.Not.SameAs(generate.FormatOptions));
        Assert.That(generateCopy.FormatOptions.KeywordCase, Is.EqualTo(SqlKeywordCase.Lower));
        Assert.That(generateCopy.WriteDialect, Is.SameAs(generate.WriteDialect));
        Assert.That(generateCopy.IncludeSemicolons, Is.False);
        Assert.That(generateCopy.PreserveSourceText, Is.True);

        SqlValidationOptions validation = new SqlValidationOptions();
        validation.ValidateTableNames = false;
        validation.ValidateColumnNames = false;
        validation.ReportAmbiguousColumns = false;
        validation.ValidateFunctionNames = false;
        validation.ValidateCollationNames = false;
        validation.ValidateIndexNames = false;
        validation.ReportDuplicateAliases = false;

        SqlValidationOptions validationCopy = validation.Copy();

        Assert.That(validationCopy, Is.Not.SameAs(validation));
        Assert.That(validationCopy.ValidateTableNames, Is.False);
        Assert.That(validationCopy.ValidateColumnNames, Is.False);
        Assert.That(validationCopy.ReportAmbiguousColumns, Is.False);
        Assert.That(validationCopy.ValidateFunctionNames, Is.False);
        Assert.That(validationCopy.ValidateCollationNames, Is.False);
        Assert.That(validationCopy.ValidateIndexNames, Is.False);
        Assert.That(validationCopy.ReportDuplicateAliases, Is.False);
        Assert.DoesNotThrow(() => validationCopy.Validate());
    }

    [Test]
    public void GenerateOptions_RejectMissingAndInvalidNestedSettings()
    {
        SqlGenerateOptions options = new SqlGenerateOptions();
        options.FormatOptions = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());

        options = new SqlGenerateOptions();
        options.WriteDialect = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());

        options = new SqlGenerateOptions();
        options.FormatOptions.TargetLineWidth = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    static void AssertInvalidFormat(Action<SqlFormatOptions> mutation, Type exceptionType)
    {
        SqlFormatOptions options = new SqlFormatOptions();
        mutation(options);

        Assert.That(() => options.Validate(), Throws.TypeOf(exceptionType));
    }

    class NullOptionsDialect : ISqlDialect
    {
        public SqlDialectKind GetKind()
        {
            return SqlDialectKind.Generic;
        }

        public string GetName()
        {
            return "Null options";
        }

        public SqlDialectOptions CreateOptions()
        {
            return null!;
        }

        public ISqlDialectPolicy CreatePolicy()
        {
            return new SqlDialectPolicy(new SqlDialectOptions());
        }
    }
}
