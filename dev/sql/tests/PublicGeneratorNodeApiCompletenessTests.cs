using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class PublicGeneratorNodeApiCompletenessTests
{
    [Test]
    public void GeneratorService_AllExpressionQueryStatementAndScriptOperationsAgree()
    {
        SqlParseResult parsed = ParseScript("select MixedName + 1 as result from SomeTable", SqlDialectKind.SQLite);
        SqlStatement statement = parsed.Script.Statements[0];
        SqlQuery query = statement.Query!;
        SqlResultColumn column = (SqlResultColumn)query.SelectCores[0].ResultColumns[0];
        SqlExpression expression = column.Expression!;
        ISqlGenerator generator = new SqlGeneratorService();

        SqlFormatOptions format = new SqlFormatOptions();
        format.KeywordCase = SqlKeywordCase.Lower;
        format.IdentifierCase = SqlIdentifierCase.Upper;
        SqlGenerateOptions generate = new SqlGenerateOptions();
        generate.FormatOptions = format;
        generate.WriteDialect = new SqlDialectCatalogService().CreateOptions(SqlDialectKind.SQLite);
        generate.IncludeSemicolons = false;

        Assert.That(generator.GenerateExpression(expression), Is.EqualTo("MixedName + 1"));
        Assert.That(generator.GenerateExpressionWithOptions(expression, format), Is.EqualTo("MIXEDNAME + 1"));
        Assert.That(generator.GenerateExpressionWithGenerateOptions(expression, generate), Is.EqualTo("MIXEDNAME + 1"));
        Assert.That(generator.GenerateQuery(query), Is.EqualTo("SELECT MixedName + 1 AS result FROM SomeTable"));
        Assert.That(generator.GenerateQueryWithOptions(query, format), Is.EqualTo("select MIXEDNAME + 1 as RESULT from SOMETABLE"));
        Assert.That(generator.GenerateQueryWithGenerateOptions(query, generate), Is.EqualTo("select MIXEDNAME + 1 as RESULT from SOMETABLE"));
        Assert.That(generator.GenerateStatement(statement), Is.EqualTo("SELECT MixedName + 1 AS result FROM SomeTable"));
        Assert.That(generator.GenerateStatementWithOptions(statement, format), Is.EqualTo("select MIXEDNAME + 1 as RESULT from SOMETABLE"));
        Assert.That(generator.GenerateStatementWithGenerateOptions(statement, generate), Is.EqualTo("select MIXEDNAME + 1 as RESULT from SOMETABLE"));
        Assert.That(generator.GenerateScript(parsed.Script), Is.EqualTo("SELECT MixedName + 1 AS result FROM SomeTable;"));
        Assert.That(generator.GenerateScriptWithFormatOptions(parsed.Script, format), Is.EqualTo("select MIXEDNAME + 1 as RESULT from SOMETABLE;"));
        Assert.That(generator.GenerateScriptWithOptions(parsed.Script, generate), Is.EqualTo("select MIXEDNAME + 1 as RESULT from SOMETABLE"));
        Assert.That(generator.GenerateResultColumn(column), Is.EqualTo("MixedName + 1 AS result"));
    }

    [Test]
    public void GeneratorService_AllDmlPayloadOperationsMatchStatementGeneration()
    {
        ISqlGenerator generator = new SqlGeneratorService();
        SqlStatement insert = ParseStatement("insert into items(id) values (1) returning id", SqlDialectKind.SQLite);
        SqlStatement update = ParseStatement("update items set id = 2 where id = 1 returning id", SqlDialectKind.SQLite);
        SqlStatement delete = ParseStatement("delete from items where id = 2 returning id", SqlDialectKind.SQLite);
        SqlStatement merge = ParseStatement(
            "merge into target t using source s on t.id = s.id when matched then delete when not matched then insert(id) values(s.id)",
            SqlDialectKind.SqlServer);

        Assert.That(generator.GenerateInsert(insert.Insert!), Is.EqualTo(generator.GenerateStatement(insert)));
        Assert.That(generator.GenerateUpdate(update.Update!), Is.EqualTo(generator.GenerateStatement(update)));
        Assert.That(generator.GenerateDelete(delete.Delete!), Is.EqualTo(generator.GenerateStatement(delete)));
        Assert.That(generator.GenerateMerge(merge.Merge!), Is.EqualTo(generator.GenerateStatement(merge)));
    }

    [Test]
    public void GeneratorService_AllDdlPayloadOperationsMatchStatementGeneration()
    {
        ISqlGenerator generator = new SqlGeneratorService();
        SqlStatement createTable = ParseStatement("create table items(id integer primary key, name text)", SqlDialectKind.SQLite);
        SqlStatement alterTable = ParseStatement("alter table items add column active integer default 1", SqlDialectKind.SQLite);
        SqlStatement dropTable = ParseStatement("drop table if exists items", SqlDialectKind.SQLite);
        SqlStatement createIndex = ParseStatement("create index idx_items on items(name) where name is not null", SqlDialectKind.SQLite);
        SqlStatement dropIndex = ParseStatement("drop index if exists idx_items", SqlDialectKind.SQLite);
        SqlStatement createView = ParseStatement("create view item_view as select id from items", SqlDialectKind.SQLite);
        SqlStatement dropView = ParseStatement("drop view if exists item_view", SqlDialectKind.SQLite);
        SqlStatement createVirtualTable = ParseStatement("create virtual table docs using fts5(content)", SqlDialectKind.SQLite);
        SqlStatement createTrigger = ParseStatement("create trigger tr_items after insert on items begin select 1; end", SqlDialectKind.SQLite);
        SqlStatement dropTrigger = ParseStatement("drop trigger if exists tr_items", SqlDialectKind.SQLite);

        Assert.That(generator.GenerateCreateTable(createTable.CreateTable!), Is.EqualTo(generator.GenerateStatement(createTable)));
        Assert.That(generator.GenerateAlterTable(alterTable.AlterTable!), Is.EqualTo(generator.GenerateStatement(alterTable)));
        Assert.That(generator.GenerateDropTable(dropTable.DropTable!), Is.EqualTo(generator.GenerateStatement(dropTable)));
        Assert.That(generator.GenerateCreateIndex(createIndex.CreateIndex!), Is.EqualTo(generator.GenerateStatement(createIndex)));
        Assert.That(generator.GenerateDropIndex(dropIndex.DropIndex!), Is.EqualTo(generator.GenerateStatement(dropIndex)));
        Assert.That(generator.GenerateCreateView(createView.CreateView!), Is.EqualTo(generator.GenerateStatement(createView)));
        Assert.That(generator.GenerateDropView(dropView.DropView!), Is.EqualTo(generator.GenerateStatement(dropView)));
        Assert.That(generator.GenerateCreateVirtualTable(createVirtualTable.CreateVirtualTable!), Is.EqualTo(generator.GenerateStatement(createVirtualTable)));
        Assert.That(generator.GenerateCreateTrigger(createTrigger.CreateTrigger!), Is.EqualTo(generator.GenerateStatement(createTrigger)));
        Assert.That(generator.GenerateDropTrigger(dropTrigger.DropTrigger!), Is.EqualTo(generator.GenerateStatement(dropTrigger)));
    }

    [Test]
    public void GeneratorService_AllUtilityAndTransactionPayloadOperationsMatchStatementGeneration()
    {
        ISqlGenerator generator = new SqlGeneratorService();
        SqlStatement pragma = ParseStatement("pragma main.cache_size = -2000", SqlDialectKind.SQLite);
        SqlStatement analyze = ParseStatement("analyze main.items", SqlDialectKind.SQLite);
        SqlStatement vacuum = ParseStatement("vacuum main into 'backup.db'", SqlDialectKind.SQLite);
        SqlStatement attach = ParseStatement("attach database 'aux.db' as aux", SqlDialectKind.SQLite);
        SqlStatement detach = ParseStatement("detach database aux", SqlDialectKind.SQLite);
        SqlStatement reindex = ParseStatement("reindex main.idx_items", SqlDialectKind.SQLite);
        SqlStatement transaction = ParseStatement("rollback transaction to savepoint work", SqlDialectKind.SQLite);
        SqlStatement savepoint = ParseStatement("savepoint work", SqlDialectKind.SQLite);
        SqlStatement release = ParseStatement("release savepoint work", SqlDialectKind.SQLite);
        SqlStatement explain = ParseStatement("explain query plan select id from items", SqlDialectKind.SQLite);

        Assert.That(generator.GeneratePragma(pragma.Pragma!), Is.EqualTo(generator.GenerateStatement(pragma)));
        Assert.That(generator.GenerateAnalyze(analyze.Analyze!), Is.EqualTo(generator.GenerateStatement(analyze)));
        Assert.That(generator.GenerateVacuum(vacuum.Vacuum!), Is.EqualTo(generator.GenerateStatement(vacuum)));
        Assert.That(generator.GenerateAttach(attach.Attach!), Is.EqualTo(generator.GenerateStatement(attach)));
        Assert.That(generator.GenerateDetach(detach.Detach!), Is.EqualTo(generator.GenerateStatement(detach)));
        Assert.That(generator.GenerateReindex(reindex.Reindex!), Is.EqualTo(generator.GenerateStatement(reindex)));
        Assert.That(generator.GenerateTransaction(transaction.Transaction!), Is.EqualTo(generator.GenerateStatement(transaction)));
        Assert.That(generator.GenerateSavepoint(savepoint.Savepoint!), Is.EqualTo(generator.GenerateStatement(savepoint)));
        Assert.That(generator.GenerateRelease(release.Release!), Is.EqualTo(generator.GenerateStatement(release)));
        Assert.That(generator.GenerateExplain(explain.Explain!), Is.EqualTo(generator.GenerateStatement(explain)));
    }

    [Test]
    public void GeneratorService_UnsupportedPayloadGenerationPreservesExactMarkerText()
    {
        SqlParseResult parsed = ParseScript("frobnicate [odd thing];", SqlDialectKind.SQLite, false);
        SqlStatement statement = parsed.Script.Statements[0];
        ISqlGenerator generator = new SqlGeneratorService();

        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Unknown));
        Assert.That(statement.UnsupportedSyntax, Has.Length.EqualTo(1));
        Assert.That(generator.GenerateUnsupportedSyntax(statement.UnsupportedSyntax[0]), Is.EqualTo("frobnicate [odd thing];"));
        Assert.That(generator.GenerateStatement(statement), Is.EqualTo("frobnicate [odd thing]"));
    }

    static SqlStatement ParseStatement(string source, SqlDialectKind kind)
    {
        return ParseScript(source, kind).Script.Statements[0];
    }

    static SqlParseResult ParseScript(string source, SqlDialectKind kind)
    {
        return ParseScript(source, kind, true);
    }

    static SqlParseResult ParseScript(string source, SqlDialectKind kind, bool requireSuccess)
    {
        SqlParseOptions options = SqlParseOptions.Create(new SqlDialectCatalogService().CreateOptions(kind));
        SqlParseResult result = new SqlParserService().ParseScript(source, options);
        if (requireSuccess)
        {
            Assert.That(result.Diagnostics.HasErrors(), Is.False, FirstDiagnostic(result.Diagnostics));
        }

        Assert.That(result.Script.Statements, Is.Not.Empty);
        return result;
    }

    static string FirstDiagnostic(SqlDiagnosticBag diagnostics)
    {
        if (diagnostics.IsEmpty())
        {
            return "";
        }

        return diagnostics.GetAt(0).Code + ": " + diagnostics.GetAt(0).Message;
    }
}
