using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SemanticValidationCompletionTests
{
    [Test]
    public void Validation_ResolvesCorrelatedSubqueriesAndAliasShadowing()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript valid = ParseSqlite("select u.id from users u where exists (select 1 from orders o where o.user_id = u.id); select u.id from users u where exists (select 1 from orders u where u.user_id = 1)");
        SqlScript invalid = ParseSqlite("select u.id from users u where exists (select 1 from orders o where o.user_id = missing.id)");

        SqlValidationResult validResult = new SqlValidatorService().Validate(valid, catalog);
        SqlValidationResult invalidResult = new SqlValidatorService().Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        AssertHasCode(invalidResult, "SQLVAL003");
    }

    [Test]
    public void Validation_ResolvesCteAndDerivedTableColumns()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript valid = ParseSqlite("with recent(order_id, owner_id) as (select id, user_id from orders) select r.order_id from recent r; select q.user_id from (select user_id from orders) q");
        SqlScript invalid = ParseSqlite("with recent(order_id) as (select id from orders) select r.missing from recent r; select q.missing from (select user_id from orders) q; with first as (select id from later), later as (select id from users) select id from first");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        AssertHasCode(invalidResult, "SQLVAL002");
        AssertHasCode(invalidResult, "SQLVAL001");
        Assert.That(CountCode(invalidResult, "SQLVAL002"), Is.EqualTo(2));
    }

    [Test]
    public void Validation_ResolvesViewsAndReportsDuplicateOrHiddenAliases()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogView view = schema.CreateView("active_users");
        view.AddColumn(new SqlCatalogColumn("id", "integer"));
        view.AddColumn(new SqlCatalogColumn("name", "text"));
        SqlScript valid = ParseSqlite("select a.id, a.name from active_users a");
        SqlScript invalid = ParseSqlite("select users.id from users u; select a.id from users a join orders a on a.id = a.id");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        AssertHasCode(invalidResult, "SQLVAL003");
        AssertHasCode(invalidResult, "SQLVAL005");
    }

    [Test]
    public void Validation_ChecksFunctionsCollationsAndArity()
    {
        SqlCatalog catalog = CreateCatalog();
        catalog.AddFunction(new SqlCatalogFunction("lower", 1, false));
        catalog.AddCollation(new SqlCatalogCollation("nocase"));
        SqlScript valid = ParseSqlite("select lower(name), name collate nocase from users order by name collate nocase");
        SqlScript invalid = ParseSqlite("select lower(name, id), mystery(name), name collate missing from users");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        AssertHasCode(invalidResult, "SQLVAL006");
        AssertHasCode(invalidResult, "SQLVAL007");
        Assert.That(CountCode(invalidResult, "SQLVAL006"), Is.EqualTo(2));
    }

    [Test]
    public void Validation_ChecksDmlAndDdlIndexReferences()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        schema.AddIndex(new SqlCatalogIndex("idx_users_name", "users", new string[] { "name" }));
        SqlScript valid = ParseSqlite("update users indexed by idx_users_name set name = 'x'; drop index idx_users_name");
        SqlScript invalid = ParseSqlite("delete from users indexed by missing_index where id = 1; drop index missing_index");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        Assert.That(CountCode(invalidResult, "SQLVAL008"), Is.EqualTo(2));
    }

    [Test]
    public void Validation_ChecksDdlTablesColumnsViewsAndForeignKeys()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        schema.CreateView("known_view").AddColumn(new SqlCatalogColumn("id", "integer"));
        catalog.AddCollation(new SqlCatalogCollation("nocase"));
        SqlScript valid = ParseSqlite("create table child(parent_id integer references users(id), label text collate nocase, created text default current_timestamp, foreign key(parent_id) references users(id)); alter table users rename column name to display_name; drop table users; drop view known_view; drop table if exists absent; drop view if exists absent; drop index if exists absent");
        SqlScript invalid = ParseSqlite("create table bad(a integer references missing_table(id), b integer references users(missing)); alter table users rename column missing to replacement; drop table absent; drop view absent; drop index absent");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        AssertHasCode(invalidResult, "SQLVAL001");
        AssertHasCode(invalidResult, "SQLVAL002");
        AssertHasCode(invalidResult, "SQLVAL008");
        AssertHasCode(invalidResult, "SQLVAL009");
    }

    [Test]
    public void Validation_ChecksMergeTargetsSourceColumnsAndAssignments()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript valid = ParseSqlServer("merge into users u using orders o on u.id = o.user_id when matched then update set u.name = 'matched' when not matched then insert (id, name) values (o.user_id, 'new')");
        SqlScript invalid = ParseSqlServer("merge into missing u using orders o on u.id = o.user_id when matched then update set u.bad = o.missing");

        SqlValidationResult validResult = SqlValidation.Validate(valid, catalog);
        SqlValidationResult invalidResult = SqlValidation.Validate(invalid, catalog);

        Assert.That(validResult.IsSuccess(), Is.True, DiagnosticText(validResult));
        AssertHasCode(invalidResult, "SQLVAL001");
        AssertHasCode(invalidResult, "SQLVAL002");
    }

    [Test]
    public void ValidationOptions_DisableNewSemanticChecksWithoutExceptions()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("select mystery(name) collate missing from users; update users indexed by missing_index set name = 'x'; drop index missing_index");
        SqlValidationOptions options = new SqlValidationOptions();
        options.ValidateFunctionNames = false;
        options.ValidateCollationNames = false;
        options.ValidateIndexNames = false;
        options.ReportDuplicateAliases = false;

        SqlValidationResult result = SqlValidation.Validate(script, catalog, options);

        Assert.That(result.IsSuccess(), Is.True, DiagnosticText(result));
        Assert.That(result.Options.ValidateFunctionNames, Is.False);
        Assert.That(result.Options.ValidateCollationNames, Is.False);
        Assert.That(result.Options.ValidateIndexNames, Is.False);
        Assert.That(result.Options.ReportDuplicateAliases, Is.False);
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
        orders.CreateColumn("total", "real");
        return catalog;
    }

    static SqlScript ParseSqlite(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True, ParseDiagnosticText(result));
        return result.Script;
    }

    static SqlScript ParseSqlServer(string text)
    {
        SqlDialectOptions dialect = SqlDialectCatalog.CreateOptions(SqlDialectKind.SqlServer);
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.Create(dialect));
        Assert.That(result.IsSuccess(), Is.True, ParseDiagnosticText(result));
        return result.Script;
    }

    static int CountCode(SqlValidationResult result, string code)
    {
        int count = 0;
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            if (result.GetDiagnosticAt(index).Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static void AssertHasCode(SqlValidationResult result, string code)
    {
        Assert.That(CountCode(result, code), Is.GreaterThan(0), DiagnosticText(result));
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
        string text = "";
        int index = 0;
        while (index < result.GetDiagnosticCount())
        {
            text = text + result.GetDiagnosticAt(index).Code + ":" + result.GetDiagnosticAt(index).Message + "\n";
            index = index + 1;
        }

        return text;
    }
}
