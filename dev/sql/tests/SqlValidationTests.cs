using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlValidationTests
{
    [Test]
    public void SqlValidation_ValidatesKnownSelectTablesAndColumns()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select u.id, o.total from users as u join orders as o on o.user_id = u.id where o.id = ?");

        SqlValidationResult result = SqlValidation.Validate(script, catalog);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Catalog, Is.SameAs(catalog));
    }

    [Test]
    public void SqlValidation_ReportsUnknownTablesAndColumns()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select missing from users; select id from missing_table");

        SqlValidationResult result = SqlValidation.Validate(script, catalog);

        Assert.That(result.IsSuccess(), Is.False);
        AssertHasDiagnostic(result, "SQLVAL001");
        AssertHasDiagnostic(result, "SQLVAL002");
    }

    [Test]
    public void SqlValidation_ValidatesDmlTargetsAndColumns()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript valid = ParseScript("insert into users(id, name) values(1, 'a'); update users set name = 'b' where id = 1; delete from orders where user_id = 1");
        SqlScript invalid = ParseScript("insert into users(missing) values(1); update users set missing = 1; delete from missing_table where id = 1");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True);
        Assert.That(invalidResult.IsSuccess(), Is.False);
        AssertHasDiagnostic(invalidResult, "SQLVAL001");
        AssertHasDiagnostic(invalidResult, "SQLVAL002");
    }

    [Test]
    public void SqlValidation_ReportsQualifiedAndAmbiguousColumnProblems()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select bad.id from users as u; select id from users join orders on orders.user_id = users.id");

        SqlValidationResult result = SqlValidation.Validate(script, catalog);

        Assert.That(result.IsSuccess(), Is.False);
        AssertHasDiagnostic(result, "SQLVAL003");
        AssertHasDiagnostic(result, "SQLVAL004");
    }

    [Test]
    public void SqlValidation_ValidatesDdlQueriesAndIndexColumns()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript valid = ParseScript("create table archive as select id from users; create view user_totals as select total from orders; create index idx_orders_user on orders(user_id)");
        SqlScript invalid = ParseScript("create table archive as select missing from users; create index idx_orders_missing on orders(missing)");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True);
        Assert.That(invalidResult.IsSuccess(), Is.False);
        AssertHasDiagnostic(invalidResult, "SQLVAL002");
    }

    [Test]
    public void SqlValidation_OptionsCanDisableTableOrColumnChecks()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select missing from missing_table");
        SqlValidationOptions options = new SqlValidationOptions();
        options.ValidateTableNames = false;
        options.ValidateColumnNames = false;

        SqlValidationResult result = SqlValidation.Validate(script, catalog, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Options.ValidateTableNames, Is.False);
        Assert.That(result.Options.ValidateColumnNames, Is.False);
    }

    [Test]
    public void SqlValidation_RejectsInvalidInputs()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseScript("select id from users");
        SqlValidationOptions options = new SqlValidationOptions();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => SqlValidation.Validate(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => SqlValidation.Validate(script, null!));
        Assert.Throws<ArgumentNullException>(() => SqlValidation.Validate(script, catalog, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlValidationResult(null!, catalog, options, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlValidationResult(script, null!, options, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlValidationResult(script, catalog, null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new SqlValidationResult(script, catalog, options, null!));
    }

    static SqlCatalog CreateCatalog()
    {
        SqlCatalog catalog = SqlCatalog.CreateDefault();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogTable users = schema.CreateTable("users");
        users.CreateColumn("id", "integer");
        users.CreateColumn("name", "text");
        SqlCatalogTable orders = schema.CreateTable("orders");
        orders.CreateColumn("id", "integer");
        orders.CreateColumn("user_id", "integer");
        orders.CreateColumn("total", "real");
        return catalog;
    }

    static SqlScript ParseScript(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script;
    }

    static void AssertHasDiagnostic(SqlValidationResult result, string code)
    {
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if (result.GetDiagnosticAt(index).Code == code)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Expected diagnostic " + code + ".");
    }
}
