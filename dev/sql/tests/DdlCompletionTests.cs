using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DdlCompletionTests
{
    [Test]
    public void CreateTable_RetainsTypedDeclaredTypesAndIndexedColumns()
    {
        SqlTableDefinition table = ParseTable("create table items (id integer primary key desc, name double precision, unique (name collate nocase asc))");
        SqlColumnDefinition name = table.GetColumnAt(1);
        SqlColumnConstraint primaryKey = table.GetColumnAt(0).GetConstraintAt(0);
        SqlTableConstraint unique = table.GetConstraintAt(0);

        Assert.That(name.DeclaredType, Is.Not.Null);
        Assert.That(name.DeclaredType!.NormalizedText, Is.EqualTo("double precision"));
        Assert.That(name.DeclaredType.SourceText, Is.EqualTo("double precision"));
        Assert.That(primaryKey.PrimaryKeyDirection, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(unique.IndexedColumns, Has.Length.EqualTo(1));
        Assert.That(unique.IndexedColumns[0].Collation!.GetText(), Is.EqualTo("nocase"));
        Assert.That(unique.IndexedColumns[0].Direction, Is.EqualTo(SqlOrderingDirection.Asc));
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkTableDefinition(table);
        Assert.That(walker.Expressions, Does.Contain(unique.IndexedColumns[0].Expression));
        Assert.That(walker.Names, Does.Contain(unique.IndexedColumns[0].Collation));
        Assert.That(Generate(table), Is.EqualTo("CREATE TABLE items (id integer PRIMARY KEY DESC, name double precision, UNIQUE (name COLLATE nocase ASC))"));
    }

    [Test]
    public void CreateTable_RetainsForeignKeyClauseOrderActionsAndDeferral()
    {
        SqlTableDefinition table = ParseTable("create table child (parent_id integer references parent(id) match full on update cascade not deferrable initially immediate on delete set null)");
        SqlForeignKeyReference reference = table.GetColumnAt(0).GetConstraintAt(0).References!;

        Assert.That(reference.OnUpdate, Is.EqualTo(SqlForeignKeyActionKind.Cascade));
        Assert.That(reference.OnDelete, Is.EqualTo(SqlForeignKeyActionKind.SetNull));
        Assert.That(reference.Deferrability, Is.EqualTo(SqlForeignKeyDeferrabilityKind.NotDeferrable));
        Assert.That(reference.InitialTiming, Is.EqualTo(SqlForeignKeyInitialTimingKind.Immediate));
        Assert.That(reference.Clauses, Has.Length.EqualTo(4));
        Assert.That(reference.Clauses[0].Kind, Is.EqualTo(SqlForeignKeyClauseKind.Match));
        Assert.That(reference.Clauses[1].Kind, Is.EqualTo(SqlForeignKeyClauseKind.OnUpdate));
        Assert.That(reference.Clauses[2].Kind, Is.EqualTo(SqlForeignKeyClauseKind.Deferrability));
        Assert.That(reference.Clauses[3].Kind, Is.EqualTo(SqlForeignKeyClauseKind.OnDelete));
        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkForeignKeyReference(reference);
        Assert.That(walker.Names, Does.Contain(reference.Clauses[0].MatchName));
        Assert.That(Generate(table), Is.EqualTo("CREATE TABLE child (parent_id integer REFERENCES parent(id) MATCH full ON UPDATE CASCADE NOT DEFERRABLE INITIALLY IMMEDIATE ON DELETE SET NULL)"));
    }

    [Test]
    public void CreateTable_RetainsGeneratedSpellingDefaultExpressionAndOptionOrder()
    {
        SqlTableDefinition table = ParseTable("create table items (name text default 'x', slug text as (lower(name)), cached text generated always as (upper(name)) stored, primary key (name collate binary desc)) without rowid, strict");
        SqlColumnDefinition name = table.GetColumnAt(0);
        SqlColumnConstraint slug = table.GetColumnAt(1).GetConstraintAt(0);
        SqlColumnConstraint cached = table.GetColumnAt(2).GetConstraintAt(0);

        Assert.That(name.DefaultExpression, Is.SameAs(name.GetConstraintAt(0).Expression));
        Assert.That(slug.IsGeneratedAlways, Is.False);
        Assert.That(slug.HasGeneratedStorageKind, Is.False);
        Assert.That(cached.IsGeneratedAlways, Is.True);
        Assert.That(cached.HasGeneratedStorageKind, Is.True);
        Assert.That(table.Options, Is.EqualTo(new SqlTableOptionKind[] { SqlTableOptionKind.WithoutRowId, SqlTableOptionKind.Strict }));
        Assert.That(Generate(table), Is.EqualTo("CREATE TABLE items (name text DEFAULT 'x', slug text AS (lower(name)), cached text GENERATED ALWAYS AS (upper(name)) STORED, PRIMARY KEY (name COLLATE binary DESC)) WITHOUT ROWID, STRICT"));
    }

    [Test]
    public void CreateIndex_ExposesIndexedColumnsAndPartialPredicate()
    {
        SqlParseResult result = Parse("create unique index idx_active on items ((lower(name)) desc) where active = 1");
        SqlIndexDefinition index = result.Script.GetStatementAt(0).CreateIndex!;

        Assert.That(index.IndexedColumns, Is.SameAs(index.KeyExpressions));
        Assert.That(index.IndexedColumns[0].Expression.Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
        Assert.That(index.Predicate, Is.SameAs(index.WhereExpression));
        Assert.That(SqlGenerator.GenerateStatement(result.Script.GetStatementAt(0)), Is.EqualTo("CREATE UNIQUE INDEX idx_active ON items ((lower(name)) DESC) WHERE active = 1"));
    }

    [Test]
    public void CreateVirtualTable_RetainsSignificantModuleArgumentText()
    {
        SqlParseResult result = Parse("create virtual table docs using custom(column text, tokenize = 'porter unicode61', prefix = '2 3')");
        SqlVirtualTableDefinition table = result.Script.GetStatementAt(0).CreateVirtualTable!;

        Assert.That(table.Arguments, Has.Length.EqualTo(3));
        Assert.That(table.Arguments[0].SourceText, Is.EqualTo("column text"));
        Assert.That(table.Arguments[1].SourceText, Is.EqualTo("tokenize = 'porter unicode61'"));
        Assert.That(table.Arguments[2].SourceText, Is.EqualTo("prefix = '2 3'"));
        Assert.That(SqlGenerator.GenerateStatement(result.Script.GetStatementAt(0)), Is.EqualTo("CREATE VIRTUAL TABLE docs USING custom(column text,tokenize = 'porter unicode61',prefix = '2 3')"));
    }

    static SqlTableDefinition ParseTable(string text)
    {
        return Parse(text).Script.GetStatementAt(0).CreateTable!;
    }

    static SqlParseResult Parse(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result;
    }

    static string Generate(SqlTableDefinition table)
    {
        return SqlGenerator.GenerateCreateTable(table);
    }

}
