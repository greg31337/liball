using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class ColumnLineageAnalysisTests
{
    [Test]
    public void ColumnLineage_MapsDirectAndExpressionQueryOutputsWithoutMutation()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("select u.id as user_id, u.name || o.total as label, 1 as constant_value from users u join orders o on o.user_id = u.id");
        SqlQuery query = script.Statements[0].Query!;
        SqlExpression labelExpression = query.SelectCores[0].ResultColumns[1].Expression!;

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry userId = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "user_id", query);
        SqlColumnLineageEntry label = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "label", query);
        SqlColumnLineageEntry constant = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "constant_value", query);
        Assert.That(userId.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.DirectColumn));
        AssertSource(userId, "users", "id");
        Assert.That(label.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Expression));
        AssertSource(label, "users", "name");
        AssertSource(label, "orders", "total");
        Assert.That(constant.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Expression));
        Assert.That(constant.Sources, Is.Empty);
        Assert.That(result.IsComplete(), Is.True, DiagnosticText(result));
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Catalog, Is.SameAs(catalog));
        Assert.That(query.SelectCores[0].ResultColumns[1].Expression, Is.SameAs(labelExpression));
    }

    [Test]
    public void ColumnLineage_PropagatesCteDerivedTableAndWildcardSources()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("with recent as (select o.user_id as owner_id from orders o) select r.owner_id from recent r; select q.user_id from (select user_id from orders) q; select u.* from users u");
        SqlQuery cteQuery = script.Statements[0].Query!;
        SqlQuery derivedQuery = script.Statements[1].Query!;
        SqlQuery wildcardQuery = script.Statements[2].Query!;

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry cteOutput = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "owner_id", cteQuery);
        SqlColumnLineageEntry derivedOutput = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "user_id", derivedQuery);
        AssertSource(cteOutput, "orders", "user_id");
        AssertSource(derivedOutput, "orders", "user_id");
        AssertSource(FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "id", wildcardQuery), "users", "id");
        AssertSource(FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "name", wildcardQuery), "users", "name");
    }

    [Test]
    public void ColumnLineage_PreservesUnknownAndAmbiguousOrigins()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("select id, missing from users join orders on orders.user_id = users.id");
        SqlQuery query = script.Statements[0].Query!;

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry ambiguous = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "id", query);
        SqlColumnLineageEntry unknown = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "missing", query);
        Assert.That(ambiguous.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Ambiguous));
        Assert.That(ambiguous.Sources, Has.Length.EqualTo(2));
        Assert.That(unknown.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Unknown));
        Assert.That(unknown.Sources, Has.Length.EqualTo(1));
        Assert.That(unknown.Sources[0].Column, Is.Null);
        Assert.That(result.IsComplete(), Is.False);
        AssertHasDiagnostic(result, "SQLLINEAGE001");
        AssertHasDiagnostic(result, "SQLLINEAGE002");
    }

    [Test]
    public void ColumnLineage_MapsInsertUpdateAndReturningTargets()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("insert into users(id, name) select user_id, cast(total as text) from orders; update users as u set name = o.total from orders o where o.user_id = u.id returning u.id, u.name || '!' as changed_name");

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry insertId = FindEntry(result, SqlColumnLineageTargetKind.InsertAssignment, "id", null);
        SqlColumnLineageEntry insertName = FindEntry(result, SqlColumnLineageTargetKind.InsertAssignment, "name", null);
        SqlColumnLineageEntry updateName = FindEntry(result, SqlColumnLineageTargetKind.UpdateAssignment, "name", null);
        SqlColumnLineageEntry returnedId = FindEntry(result, SqlColumnLineageTargetKind.ReturningOutput, "id", null);
        SqlColumnLineageEntry returnedName = FindEntry(result, SqlColumnLineageTargetKind.ReturningOutput, "changed_name", null);
        AssertSource(insertId, "orders", "user_id");
        AssertSource(insertName, "orders", "total");
        Assert.That(insertName.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Expression));
        AssertSource(updateName, "orders", "total");
        Assert.That(updateName.TargetTable!.Name, Is.EqualTo("users"));
        AssertSource(returnedId, "users", "id");
        AssertSource(returnedName, "users", "name");
    }

    [Test]
    public void ColumnLineage_MapsMergeUpdateAndInsertAssignments()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlServer("merge into users u using orders o on u.id = o.user_id when matched then update set u.name = o.total when not matched then insert (id, name) values (o.user_id, o.total)");

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry[] mergeEntries = EntriesOfKind(result, SqlColumnLineageTargetKind.MergeAssignment);
        Assert.That(mergeEntries, Has.Length.EqualTo(3), DiagnosticText(result));
        AssertSource(mergeEntries[0], "orders", "total");
        AssertSource(mergeEntries[1], "orders", "user_id");
        AssertSource(mergeEntries[2], "orders", "total");
        Assert.That(mergeEntries[0].TargetTable!.Name, Is.EqualTo("users"));
    }

    [Test]
    public void ColumnLineage_CombinesCompoundBranchesAndMapsUpsertAssignments()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("select id as source_id from users union all select user_id from orders; insert into users(id, name) values(1, 'x') on conflict(id) do update set name = users.name || '!'");
        SqlQuery query = script.Statements[0].Query!;

        SqlColumnLineageResult result = new SqlAnalyzerService().AnalyzeColumnLineage(script, catalog);

        SqlColumnLineageEntry compound = FindEntry(result, SqlColumnLineageTargetKind.QueryOutput, "source_id", query);
        SqlColumnLineageEntry upsert = FindEntry(result, SqlColumnLineageTargetKind.UpsertAssignment, "name", null);
        Assert.That(compound.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Expression));
        AssertSource(compound, "users", "id");
        AssertSource(compound, "orders", "user_id");
        Assert.That(upsert.OriginKind, Is.EqualTo(SqlColumnLineageOriginKind.Expression));
        AssertSource(upsert, "users", "name");
    }

    [Test]
    public void ColumnLineage_ValidatesRequiredInputs()
    {
        SqlCatalog catalog = CreateCatalog();
        SqlScript script = ParseSqlite("select id from users");
        SqlAnalyzerService analyzer = new SqlAnalyzerService();

        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeColumnLineage(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeColumnLineage(script, null!));
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

    static SqlColumnLineageEntry FindEntry(SqlColumnLineageResult result, SqlColumnLineageTargetKind targetKind, string outputName, SqlQuery? query)
    {
        int index = 0;
        while (index < result.Entries.Length)
        {
            SqlColumnLineageEntry entry = result.Entries[index];
            if (entry.TargetKind == targetKind && entry.OutputName == outputName && (query == null || entry.Query == query))
            {
                return entry;
            }

            index = index + 1;
        }

        Assert.Fail("Missing lineage entry " + targetKind.ToString() + ":" + outputName + ".\n" + EntryText(result));
        return null!;
    }

    static SqlColumnLineageEntry[] EntriesOfKind(SqlColumnLineageResult result, SqlColumnLineageTargetKind targetKind)
    {
        List<SqlColumnLineageEntry> entries = new List<SqlColumnLineageEntry>();
        int index = 0;
        while (index < result.Entries.Length)
        {
            if (result.Entries[index].TargetKind == targetKind)
            {
                entries.Add(result.Entries[index]);
            }

            index = index + 1;
        }

        return entries.ToArray();
    }

    static void AssertSource(SqlColumnLineageEntry entry, string tableName, string columnName)
    {
        int index = 0;
        while (index < entry.Sources.Length)
        {
            SqlColumnLineageSource source = entry.Sources[index];
            if (source.Table != null && source.Table.Name == tableName && source.Column != null && source.Column.Name == columnName)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Missing source " + tableName + "." + columnName + " for " + entry.OutputName + ".");
    }

    static void AssertHasDiagnostic(SqlColumnLineageResult result, string code)
    {
        int index = 0;
        while (index < result.Diagnostics.Diagnostics.Count)
        {
            if (result.Diagnostics.Diagnostics[index].Code == code)
            {
                return;
            }

            index = index + 1;
        }

        Assert.Fail("Missing diagnostic " + code + ".\n" + DiagnosticText(result));
    }

    static string EntryText(SqlColumnLineageResult result)
    {
        string text = "";
        int index = 0;
        while (index < result.Entries.Length)
        {
            text = text + result.Entries[index].TargetKind.ToString() + ":" + result.Entries[index].OutputName + ":" + result.Entries[index].OriginKind.ToString() + "\n";
            index = index + 1;
        }

        return text;
    }

    static string DiagnosticText(SqlColumnLineageResult result)
    {
        string text = "";
        int index = 0;
        while (index < result.Diagnostics.Diagnostics.Count)
        {
            text = text + result.Diagnostics.Diagnostics[index].Code + ":" + result.Diagnostics.Diagnostics[index].Message + "\n";
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
