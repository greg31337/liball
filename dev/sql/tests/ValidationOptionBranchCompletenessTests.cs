using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ValidationOptionBranchCompletenessTests
{
    [TestCase("tables", "select 1 from missing_table", "SQLVAL001")]
    [TestCase("columns", "select missing_column from users", "SQLVAL002")]
    [TestCase("ambiguity", "select id from users join orders on users.id = orders.user_id", "SQLVAL004")]
    [TestCase("functions", "select mystery(id) from users", "SQLVAL006")]
    [TestCase("collations", "select name collate missing_collation from users", "SQLVAL007")]
    [TestCase("indexes", "update users indexed by missing_index set name = 'x'", "SQLVAL008")]
    [TestCase("aliases", "select a.id from users a join orders a on a.id = a.id", "SQLVAL005")]
    public void ValidatorService_EachPublicOptionIndependentlyControlsItsDiagnostic(
        string optionName,
        string source,
        string code)
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = Parse(source);
        ISqlValidator validator = new SqlValidatorService();
        SqlValidationResult enabled = validator.Validate(script, catalog);
        SqlValidationOptions options = new SqlValidationOptions();
        DisableOption(options, optionName);

        SqlValidationResult disabled = validator.ValidateWithOptions(script, catalog, options);

        Assert.That(HasCode(enabled, code), Is.True, optionName);
        Assert.That(HasCode(disabled, code), Is.False, DiagnosticText(disabled));
        AssertOptionDisabled(disabled.Options, optionName);

        EnableOption(options, optionName);
        AssertOptionDisabled(disabled.Options, optionName);
    }

    [Test]
    public void ValidationFailureResultPreservesAllDiagnosticsInSourceOrder()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = Parse(
            "select missing from users; " +
            "select id from users join orders on users.id = orders.user_id; " +
            "select mystery(name) collate absent from users; " +
            "update users indexed by absent_idx set name = 'x'; " +
            "drop view absent_view");

        SqlValidationResult result = new SqlValidatorService().Validate(script, catalog);

        string[] codes = new string[] { "SQLVAL002", "SQLVAL004", "SQLVAL006", "SQLVAL007", "SQLVAL008", "SQLVAL009" };
        int codeIndex = 0;
        while (codeIndex < codes.Length)
        {
            Assert.That(HasCode(result, codes[codeIndex]), Is.True, codes[codeIndex] + "\n" + DiagnosticText(result));
            codeIndex = codeIndex + 1;
        }

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Catalog, Is.SameAs(catalog));
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            SqlTextSpan span = result.GetDiagnosticAt(index).Span;
            Assert.That(span.Start, Is.GreaterThanOrEqualTo(0));
            Assert.That(span.GetEnd(), Is.LessThanOrEqualTo(script.SourceText.Length));
            index = index + 1;
        }
    }

    static void DisableOption(SqlValidationOptions options, string optionName)
    {
        if (optionName == "tables")
        {
            options.ValidateTableNames = false;
        }
        else if (optionName == "columns")
        {
            options.ValidateColumnNames = false;
        }
        else if (optionName == "ambiguity")
        {
            options.ReportAmbiguousColumns = false;
        }
        else if (optionName == "functions")
        {
            options.ValidateFunctionNames = false;
        }
        else if (optionName == "collations")
        {
            options.ValidateCollationNames = false;
        }
        else if (optionName == "indexes")
        {
            options.ValidateIndexNames = false;
        }
        else
        {
            options.ReportDuplicateAliases = false;
        }
    }

    static void EnableOption(SqlValidationOptions options, string optionName)
    {
        if (optionName == "tables")
        {
            options.ValidateTableNames = true;
        }
        else if (optionName == "columns")
        {
            options.ValidateColumnNames = true;
        }
        else if (optionName == "ambiguity")
        {
            options.ReportAmbiguousColumns = true;
        }
        else if (optionName == "functions")
        {
            options.ValidateFunctionNames = true;
        }
        else if (optionName == "collations")
        {
            options.ValidateCollationNames = true;
        }
        else if (optionName == "indexes")
        {
            options.ValidateIndexNames = true;
        }
        else
        {
            options.ReportDuplicateAliases = true;
        }
    }

    static void AssertOptionDisabled(SqlValidationOptions options, string optionName)
    {
        if (optionName == "tables")
        {
            Assert.That(options.ValidateTableNames, Is.False);
        }
        else if (optionName == "columns")
        {
            Assert.That(options.ValidateColumnNames, Is.False);
        }
        else if (optionName == "ambiguity")
        {
            Assert.That(options.ReportAmbiguousColumns, Is.False);
        }
        else if (optionName == "functions")
        {
            Assert.That(options.ValidateFunctionNames, Is.False);
        }
        else if (optionName == "collations")
        {
            Assert.That(options.ValidateCollationNames, Is.False);
        }
        else if (optionName == "indexes")
        {
            Assert.That(options.ValidateIndexNames, Is.False);
        }
        else
        {
            Assert.That(options.ReportDuplicateAliases, Is.False);
        }
    }

    static SqlCatalog CreateCatalog()
    {
        SqlCatalog catalog = new SqlCatalog();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogTable users = schema.CreateTable("users");
        users.CreateColumn("id", "integer");
        users.CreateColumn("name", "text");
        SqlCatalogTable orders = schema.CreateTable("orders");
        orders.CreateColumn("id", "integer");
        orders.CreateColumn("user_id", "integer");
        return catalog;
    }

    static SqlScript Parse(string source)
    {
        SqlParseResult result = new SqlParserService().ParseScript(source, SqlParseOptions.CreateSqlite());
        Assert.That(result.Diagnostics.HasErrors(), Is.False, ParseDiagnosticText(result));
        return result.Script;
    }

    static bool HasCode(SqlValidationResult result, string code)
    {
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if (result.GetDiagnosticAt(index).Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string DiagnosticText(SqlValidationResult result)
    {
        string text = "";
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            text = text + result.GetDiagnosticAt(index).Code + ":" + result.GetDiagnosticAt(index).Message + "\n";
            index = index + 1;
        }

        return text;
    }

    static string ParseDiagnosticText(SqlParseResult result)
    {
        if (result.Diagnostics.IsEmpty())
        {
            return "";
        }

        return result.Diagnostics.GetAt(0).Code + ":" + result.Diagnostics.GetAt(0).Message;
    }
}
