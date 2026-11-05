using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqliteSyntaxDiagramConstraintTests
{
    [TestCase("typeless column", "create table flexible(value)")]
    [TestCase("multiword type name", "create table flexible(value unsigned big int)")]
    [TestCase("type name signed numbers", "create table flexible(value decimal(10, -2))")]
    [TestCase("column primary key", "create table items(id integer primary key desc on conflict rollback autoincrement)")]
    [TestCase("column not null", "create table items(value text constraint required_value not null on conflict abort)")]
    [TestCase("column unique", "create table items(value text unique on conflict fail)")]
    [TestCase("column check", "create table items(value integer check(value >= 0))")]
    [TestCase("column signed default", "create table items(value integer default -1)")]
    [TestCase("column literal default", "create table items(created text default current_timestamp)")]
    [TestCase("column expression default", "create table items(value text default (lower('VALUE')))")]
    [TestCase("column collate", "create table items(value text collate nocase)")]
    [TestCase("column foreign key", "create table child(parent_id references parent(id))")]
    [TestCase("generated virtual column", "create table items(value text, normalized text generated always as (lower(value)) virtual)")]
    [TestCase("generated stored column", "create table items(value text, normalized text as (lower(value)) stored)")]
    [TestCase("table primary key", "create table items(first integer, second integer, constraint item_pk primary key(first asc, second collate binary desc) on conflict replace)")]
    [TestCase("table unique", "create table items(first text, second text, unique(first, second) on conflict ignore)")]
    [TestCase("table check", "create table items(value integer, constraint positive_value check(value > 0))")]
    [TestCase("table foreign key", "create table child(parent_id integer, foreign key(parent_id) references parent(id))")]
    [TestCase("foreign key no action restrict", "create table child(parent_id references parent(id) on delete no action on update restrict)")]
    [TestCase("foreign key set null default", "create table child(parent_id references parent(id) on delete set null on update set default)")]
    [TestCase("foreign key cascade", "create table child(parent_id references parent(id) on delete cascade on update cascade)")]
    [TestCase("foreign key match deferrable", "create table child(parent_id references parent(id) match full deferrable initially deferred)")]
    [TestCase("foreign key not deferrable", "create table child(parent_id references parent(id) not deferrable initially immediate)")]
    [TestCase("table options without rowid", "create table items(id integer primary key) without rowid")]
    [TestCase("table options strict", "create table items(id integer) strict")]
    [TestCase("table options combined", "create table items(id integer primary key) without rowid, strict")]
    public void OfficialCreateTableSubgrammar_ParsesWithoutDiagnostics(string diagramForm, string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True, diagramForm + DiagnosticText(result));
        Assert.That(result.Script.GetStatementAt(0).CreateTable, Is.Not.Null, diagramForm);
    }

    [Test]
    public void ForeignKeyClauseDiagram_PreservesActionOrderAndDeferralPayloads()
    {
        const string text = "create table child(parent_id references parent(id) match full on update cascade deferrable initially deferred on delete set default)";

        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        SqlForeignKeyReference reference = result.Script.GetStatementAt(0).CreateTable!.GetColumnAt(0).GetConstraintAt(0).References!;

        Assert.That(result.IsSuccess(), Is.True, DiagnosticText(result));
        Assert.That(reference.OnUpdate, Is.EqualTo(SqlForeignKeyActionKind.Cascade));
        Assert.That(reference.OnDelete, Is.EqualTo(SqlForeignKeyActionKind.SetDefault));
        Assert.That(reference.Deferrability, Is.EqualTo(SqlForeignKeyDeferrabilityKind.Deferrable));
        Assert.That(reference.InitialTiming, Is.EqualTo(SqlForeignKeyInitialTimingKind.Deferred));
        Assert.That(reference.Clauses, Has.Length.EqualTo(4));
        Assert.That(reference.Clauses[0].Kind, Is.EqualTo(SqlForeignKeyClauseKind.Match));
        Assert.That(reference.Clauses[1].Kind, Is.EqualTo(SqlForeignKeyClauseKind.OnUpdate));
        Assert.That(reference.Clauses[2].Kind, Is.EqualTo(SqlForeignKeyClauseKind.Deferrability));
        Assert.That(reference.Clauses[3].Kind, Is.EqualTo(SqlForeignKeyClauseKind.OnDelete));
    }

    [Test]
    public void ConflictClauseDiagram_PreservesEveryConflictAction()
    {
        const string text = "create table conflicts(a integer primary key on conflict rollback, b integer not null on conflict abort, c integer unique on conflict fail, d integer not null on conflict ignore, e integer unique on conflict replace)";

        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;

        Assert.That(result.IsSuccess(), Is.True, DiagnosticText(result));
        Assert.That(table.GetColumnAt(0).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Rollback));
        Assert.That(table.GetColumnAt(1).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Abort));
        Assert.That(table.GetColumnAt(2).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Fail));
        Assert.That(table.GetColumnAt(3).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Ignore));
        Assert.That(table.GetColumnAt(4).GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
    }

    static string DiagnosticText(SqlParseResult result)
    {
        if (result.Diagnostics.GetCount() == 0)
        {
            return "";
        }

        SqlDiagnostic diagnostic = result.Diagnostics.GetAt(0);
        return ": " + diagnostic.Code + " " + diagnostic.Message;
    }
}
