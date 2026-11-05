using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class PublicDialectAndEnumContractTests
{
    [Test]
    public void DialectCatalogService_ExposesEveryDescriptorConsistently()
    {
        SqlDialectCatalogService catalog = new SqlDialectCatalogService();
        SqlDialectKind[] kinds = new SqlDialectKind[]
        {
            SqlDialectKind.Generic,
            SqlDialectKind.SQLite,
            SqlDialectKind.PostgreSql,
            SqlDialectKind.MySql,
            SqlDialectKind.MariaDb,
            SqlDialectKind.SqlServer,
            SqlDialectKind.Oracle,
            SqlDialectKind.DuckDb,
            SqlDialectKind.BigQuery,
            SqlDialectKind.Snowflake,
            SqlDialectKind.Redshift,
            SqlDialectKind.Databricks,
            SqlDialectKind.Ansi
        };
        string[] names = new string[]
        {
            "Generic", "SQLite", "PostgreSQL", "MySQL", "MariaDB", "SQL Server", "Oracle",
            "DuckDB", "BigQuery", "Snowflake", "Redshift", "Databricks", "ANSI"
        };

        int index = 0;
        while (index < kinds.Length)
        {
            ISqlDialect descriptor = catalog.GetDialect(kinds[index]);
            SqlDialectOptions first = descriptor.CreateOptions();
            SqlDialectOptions second = catalog.CreateOptions(kinds[index]);
            ISqlDialectPolicy policy = descriptor.CreatePolicy();

            Assert.That(descriptor.GetKind(), Is.EqualTo(kinds[index]));
            Assert.That(descriptor.GetName(), Is.EqualTo(names[index]));
            Assert.That(catalog.GetName(kinds[index]), Is.EqualTo(names[index]));
            Assert.That(first.Kind, Is.EqualTo(kinds[index]));
            Assert.That(second.Kind, Is.EqualTo(kinds[index]));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(policy.GetKind(), Is.EqualTo(kinds[index]));
            index = index + 1;
        }
    }

    [Test]
    public void DialectCatalog_NormalizesUnknownDiscriminantsToGeneric()
    {
        SqlDialectCatalogService catalog = new SqlDialectCatalogService();
        SqlDialectKind invalid = (SqlDialectKind)int.MaxValue;

        ISqlDialect descriptor = catalog.GetDialect(invalid);

        Assert.That(descriptor.GetKind(), Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(descriptor.GetName(), Is.EqualTo("Generic"));
        Assert.That(descriptor.CreateOptions().Kind, Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(descriptor.CreatePolicy().GetKind(), Is.EqualTo(SqlDialectKind.Generic));
        Assert.That(catalog.GetName(invalid), Is.EqualTo("Generic"));
        Assert.That(catalog.CreateOptions(invalid).Kind, Is.EqualTo(SqlDialectKind.Generic));
    }

    [Test]
    public void DialectPolicy_HandlesCaseSensitiveUnknownAndInvalidLookups()
    {
        SqlDialectOptions options = new SqlDialectOptions();
        options.CaseInsensitiveKeywords = false;
        options.CustomKeywords = new string[] { "EXACT" };
        options.AllowCommonTableExpressions = false;
        options.AllowWindowFunctions = false;
        options.AllowReturningClause = false;
        options.AllowUpsertClause = false;
        ISqlDialectPolicy policy = new SqlDialectPolicy(options);

        Assert.That(policy.IsKeyword("SELECT"), Is.True);
        Assert.That(policy.IsKeyword("select"), Is.False);
        Assert.That(policy.IsKeyword("EXACT"), Is.True);
        Assert.That(policy.IsKeyword("exact"), Is.False);
        Assert.Throws<ArgumentNullException>(() => policy.IsKeyword(null!));
        Assert.That(policy.GetUnaryPrecedence(SqlUnaryOperatorKind.None), Is.EqualTo(0));
        Assert.That(policy.GetUnaryPrecedence((SqlUnaryOperatorKind)99), Is.EqualTo(0));
        Assert.That(policy.GetBinaryPrecedence(SqlBinaryOperatorKind.None), Is.EqualTo(0));
        Assert.That(policy.GetBinaryPrecedence((SqlBinaryOperatorKind)99), Is.EqualTo(0));
        Assert.That(policy.GetCustomOperatorPrecedence("missing"), Is.EqualTo(0));
        Assert.Throws<ArgumentNullException>(() => policy.GetCustomOperatorPrecedence(null!));
        Assert.That(policy.SupportsStatement((SqlStatementKind)99), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.CommonTableExpression), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Window), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Returning), Is.False);
        Assert.That(policy.SupportsClause(SqlDialectClauseKind.Upsert), Is.False);
        Assert.That(policy.SupportsClause((SqlDialectClauseKind)99), Is.False);

        SqlDialectOptions doubleQuotedStrings = new SqlDialectOptions();
        doubleQuotedStrings.AllowDoubleQuotedStrings = true;
        ISqlDialectPolicy doubleQuotedPolicy = new SqlDialectPolicy(doubleQuotedStrings);
        Assert.That(doubleQuotedPolicy.GetStringQuoteStyles(), Does.Contain(SqlStringQuoteStyle.DoubleQuote));

        SqlDialectOptions noStrings = new SqlDialectOptions();
        noStrings.AllowSingleQuotedStrings = false;
        noStrings.AllowDoubleQuotedStrings = false;
        noStrings.AllowEscapedStrings = false;
        noStrings.AllowUnicodeStrings = false;
        noStrings.AllowDollarQuotedStrings = false;
        ISqlDialectPolicy noStringPolicy = new SqlDialectPolicy(noStrings);
        Assert.That(noStringPolicy.GetStringQuoteStyles(), Is.Empty);
        Assert.That(noStringPolicy.GetLiteralPolicies(), Does.Not.Contain(SqlDialectLiteralPolicyKind.String));
    }

    [Test]
    public void DialectOptions_ValidateAllCustomizationArraysAndOperatorRules()
    {
        SqlDialectOptions options = new SqlDialectOptions();
        options.AllowDollarParameters = false;
        options.AllowDollarNumberParameters = true;
        Assert.That(options.IsParameterPrefixAllowed('$'), Is.True);
        Assert.That(options.IsIdentifierQuoteAllowed('#'), Is.False);
        Assert.That(options.GetCustomOperatorPrecedence("missing"), Is.EqualTo(0));
        Assert.That(options.IsCustomOperatorRightAssociative("missing"), Is.False);
        Assert.Throws<ArgumentNullException>(() => options.GetCustomOperatorPrecedence(null!));
        Assert.Throws<ArgumentNullException>(() => options.IsCustomOperatorRightAssociative(null!));
        Assert.That(options.SupportsStatement(SqlStatementKind.Select), Is.True);
        Assert.That(options.SupportsStatement((SqlStatementKind)99), Is.False);

        options.CustomKeywords = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());
        options = new SqlDialectOptions();
        options.CustomOperators = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());
        options = new SqlDialectOptions();
        options.SupportedStatements = null!;
        Assert.Throws<ArgumentNullException>(() => options.Validate());
        options = new SqlDialectOptions();
        options.CustomKeywords = new string[] { null! };
        Assert.Throws<ArgumentNullException>(() => options.Validate());
        options = new SqlDialectOptions();
        options.CustomOperators = new SqlOperatorPolicy[] { null! };
        Assert.Throws<ArgumentNullException>(() => options.Validate());

        options = new SqlDialectOptions();
        options.AddSupportedStatement(SqlStatementKind.Select);
        Assert.That(options.SupportedStatements.Length, Is.EqualTo(28));
        options.AddSupportedStatement(SqlStatementKind.Merge);
        Assert.That(options.SupportsStatement(SqlStatementKind.Merge), Is.True);

        SqlOperatorPolicy mutable = new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false);
        mutable.Text = null!;
        AssertInvalidCustomOperator(mutable, typeof(ArgumentNullException));
        mutable = new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false);
        mutable.Text = "";
        AssertInvalidCustomOperator(mutable, typeof(ArgumentException));
        mutable = new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false);
        mutable.Precedence = 0;
        AssertInvalidCustomOperator(mutable, typeof(ArgumentOutOfRangeException));

        AssertInvalidCustomOperator(new SqlOperatorPolicy("word", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false), typeof(ArgumentException));
        AssertInvalidCustomOperator(new SqlOperatorPolicy("#", SqlTokenKind.Plus, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 10, false), typeof(ArgumentException));
        AssertInvalidCustomOperator(new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.Positive, SqlBinaryOperatorKind.None, 10, false), typeof(ArgumentException));
        AssertInvalidCustomOperator(new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.Add, 10, false), typeof(ArgumentException));
    }

    [Test]
    public void OperatorPolicy_StoresEveryValueAndRejectsAllInvalidBoundaries()
    {
        SqlOperatorPolicy policy = new SqlOperatorPolicy("**", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, int.MaxValue, true);

        Assert.That(policy.Text, Is.EqualTo("**"));
        Assert.That(policy.TokenKind, Is.EqualTo(SqlTokenKind.CustomOperator));
        Assert.That(policy.UnaryOperator, Is.EqualTo(SqlUnaryOperatorKind.None));
        Assert.That(policy.BinaryOperator, Is.EqualTo(SqlBinaryOperatorKind.None));
        Assert.That(policy.Precedence, Is.EqualTo(int.MaxValue));
        Assert.That(policy.IsRightAssociative, Is.True);
        Assert.Throws<ArgumentNullException>(() => new SqlOperatorPolicy(null!, SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 1, false));
        Assert.Throws<ArgumentException>(() => new SqlOperatorPolicy("", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 1, false));
        Assert.Throws<ArgumentException>(() => new SqlOperatorPolicy("\t", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 1, false));
        Assert.Throws<ArgumentException>(() => new SqlOperatorPolicy("+", SqlTokenKind.Plus, SqlUnaryOperatorKind.Positive, SqlBinaryOperatorKind.Add, 1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, 0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlOperatorPolicy("#", SqlTokenKind.CustomOperator, SqlUnaryOperatorKind.None, SqlBinaryOperatorKind.None, int.MinValue, false));
    }

    [Test]
    public void PublicEnums_AreCompleteInt32DiscriminatedContracts()
    {
        string[] expectedNames = ExpectedEnumNames();
        Type[] exportedTypes = typeof(SqlDialectKind).Assembly.GetExportedTypes();
        List<string> actualNames = new List<string>();
        int typeIndex = 0;
        while (typeIndex < exportedTypes.Length)
        {
            if (exportedTypes[typeIndex].IsEnum)
            {
                actualNames.Add(exportedTypes[typeIndex].Name);
            }

            typeIndex = typeIndex + 1;
        }

        actualNames.Sort(StringComparer.Ordinal);
        Assert.That(actualNames.ToArray(), Is.EqualTo(expectedNames));

        typeIndex = 0;
        while (typeIndex < exportedTypes.Length)
        {
            Type type = exportedTypes[typeIndex];
            if (type.IsEnum)
            {
                Assert.That(Enum.GetUnderlyingType(type), Is.EqualTo(typeof(int)), type.Name);
                Assert.That(Enum.IsDefined(type, 0), Is.True, type.Name + " must define its default value");
                Array values = Enum.GetValues(type);
                HashSet<int> distinctValues = new HashSet<int>();
                int valueIndex = 0;
                while (valueIndex < values.Length)
                {
                    distinctValues.Add(Convert.ToInt32(values.GetValue(valueIndex)));
                    valueIndex = valueIndex + 1;
                }

                Assert.That(distinctValues.Count, Is.EqualTo(values.Length), type.Name + " must not alias values");
            }

            typeIndex = typeIndex + 1;
        }
    }

    static void AssertInvalidCustomOperator(SqlOperatorPolicy policy, Type exceptionType)
    {
        SqlDialectOptions options = new SqlDialectOptions();
        options.CustomOperators = new SqlOperatorPolicy[] { policy };

        Assert.That(() => options.Validate(), Throws.TypeOf(exceptionType));
    }

    static string[] ExpectedEnumNames()
    {
        return new string[]
        {
            "SqlAlterTableActionKind", "SqlAstDiffKind", "SqlBinaryOperatorKind", "SqlCastSyntaxKind", "SqlCatalogAliasKind",
            "SqlColumnLineageOriginKind", "SqlColumnLineageTargetKind", "SqlCommaStyle", "SqlCompatibilityIssueKind",
            "SqlCompoundOperatorKind", "SqlConflictActionKind", "SqlConflictTargetKind", "SqlConstraintKind", "SqlCteMaterializationKind",
            "SqlDiagnosticSeverity", "SqlDialectClauseKind", "SqlDialectKind", "SqlDialectLiteralPolicyKind", "SqlDistinctnessKind",
            "SqlExplainKind", "SqlExpressionKind", "SqlFetchDirectionKind", "SqlForeignKeyActionKind", "SqlForeignKeyClauseKind",
            "SqlForeignKeyDeferrabilityKind", "SqlForeignKeyInitialTimingKind", "SqlFormatMode", "SqlGeneratedColumnKind", "SqlIdentifierCase",
            "SqlInTargetKind", "SqlInsertSourceKind", "SqlJoinConstraintKind", "SqlJoinKind", "SqlJoinModifierKind", "SqlKeywordCase",
            "SqlLiteralKind", "SqlLogicalPlanNodeKind", "SqlMergeActionKind", "SqlMergeMatchKind", "SqlNameQuoteStyle",
            "SqlNormalizationMarkerKind", "SqlNullOrdering", "SqlOperatorExpressionKind", "SqlOrderingDirection", "SqlParameterStyle",
            "SqlPragmaValueKind", "SqlStatementKind", "SqlStringQuoteStyle", "SqlTableOptionKind", "SqlTableReferenceKind", "SqlTokenKind",
            "SqlTrailingCommaPolicy", "SqlTransactionActionKind", "SqlTransactionMode", "SqlTranspileMappingKind", "SqlTriggerEvent",
            "SqlTriggerTiming", "SqlTriviaKind", "SqlUnaryOperatorKind", "SqlUnsupportedQueryClauseKind", "SqlUnsupportedSyntaxKind",
            "SqlUpsertActionKind", "SqlUpsertSyntaxKind", "SqlWindowFrameBoundaryKind", "SqlWindowFrameExclusionKind", "SqlWindowFrameKind",
            "SqliteTypeAffinityKind", "SqliteTypeAffinityTargetKind"
        };
    }
}
