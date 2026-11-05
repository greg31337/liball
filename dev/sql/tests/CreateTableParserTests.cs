using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CreateTableParserTests
{
    [Test]
    public void StatementParser_AttachesCreateTableSchemaWithColumns()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create temp table if not exists main.items (id integer primary key, name text not null, created_at)", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.CreateTable));
        Assert.That(statement.CreateTable, Is.Not.Null);
        SqlTableDefinition table = statement.CreateTable!;
        Assert.That(table.IsTemporary, Is.True);
        Assert.That(table.IfNotExists, Is.True);
        Assert.That(table.Name.GetText(), Is.EqualTo("main.items"));
        Assert.That(table.GetColumnCount(), Is.EqualTo(3));
        Assert.That(table.GetColumnAt(0).Name.GetText(), Is.EqualTo("id"));
        Assert.That(table.GetColumnAt(0).TypeName, Is.EqualTo("integer"));
        Assert.That(table.GetColumnAt(0).SourceTypeName, Is.EqualTo("integer"));
        Assert.That(table.GetColumnAt(1).Name.GetText(), Is.EqualTo("name"));
        Assert.That(table.GetColumnAt(1).TypeName, Is.EqualTo("text"));
        Assert.That(table.GetColumnAt(2).Name.GetText(), Is.EqualTo("created_at"));
    }

    [Test]
    public void CreateTableParser_ParsesFlexibleColumnTypeNames()
    {
        SqlTableDefinition table = ParseTable("create table items (amount decimal(10, 2) not null, title double precision, payload varchar ( 255 ), flag)");

        Assert.That(table.GetColumnCount(), Is.EqualTo(4));
        Assert.That(table.GetColumnAt(0).TypeName, Is.EqualTo("decimal(10, 2)"));
        Assert.That(table.GetColumnAt(0).SourceTypeName, Is.EqualTo("decimal(10, 2)"));
        Assert.That(table.GetColumnAt(1).TypeName, Is.EqualTo("double precision"));
        Assert.That(table.GetColumnAt(2).TypeName, Is.EqualTo("varchar ( 255 )"));
        Assert.That(table.GetColumnAt(3).HasTypeName(), Is.False);
    }

    [Test]
    public void CreateTableParser_LeavesColumnTypeEmptyWhenConstraintStartsAfterName()
    {
        SqlTableDefinition table = ParseTable("create table items (id primary key, name unique)");

        Assert.That(table.GetColumnAt(0).Name.GetText(), Is.EqualTo("id"));
        Assert.That(table.GetColumnAt(0).HasTypeName(), Is.False);
        Assert.That(table.GetColumnAt(1).Name.GetText(), Is.EqualTo("name"));
        Assert.That(table.GetColumnAt(1).HasTypeName(), Is.False);
    }

    [Test]
    public void CreateTableParser_ParsesPrimaryKeyNotNullUniqueAndNamedColumnConstraints()
    {
        SqlTableDefinition table = ParseTable("create table items (id integer constraint pk_items primary key autoincrement, name text not null unique)");
        SqlColumnDefinition id = table.GetColumnAt(0);
        SqlColumnDefinition name = table.GetColumnAt(1);

        Assert.That(id.GetConstraintCount(), Is.EqualTo(1));
        Assert.That(id.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.PrimaryKey));
        Assert.That(id.GetConstraintAt(0).Name!.GetText(), Is.EqualTo("pk_items"));
        Assert.That(id.GetConstraintAt(0).IsAutoincrement, Is.True);
        Assert.That(name.GetConstraintCount(), Is.EqualTo(2));
        Assert.That(name.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.NotNull));
        Assert.That(name.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.Unique));
    }

    [Test]
    public void CreateTableParser_ParsesExpressionAndCollationColumnConstraints()
    {
        SqlTableDefinition table = ParseTable("create table items (qty integer check (qty > 0) default 1, name text collate nocase)");
        SqlColumnDefinition qty = table.GetColumnAt(0);
        SqlColumnDefinition name = table.GetColumnAt(1);

        Assert.That(qty.GetConstraintCount(), Is.EqualTo(2));
        Assert.That(qty.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.Check));
        Assert.That(qty.GetConstraintAt(0).HasExpression(), Is.True);
        Assert.That(qty.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.Default));
        Assert.That(qty.GetConstraintAt(1).Expression!.Kind, Is.EqualTo(SqlExpressionKind.Literal));
        Assert.That(name.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.Collate));
        Assert.That(name.GetConstraintAt(0).CollationName!.GetText(), Is.EqualTo("nocase"));
    }

    [Test]
    public void CreateTableParser_ParsesReferencesColumnConstraint()
    {
        SqlTableDefinition table = ParseTable("create table items (parent_id integer references parents(id))");
        SqlColumnConstraint constraint = table.GetColumnAt(0).GetConstraintAt(0);

        Assert.That(constraint.Kind, Is.EqualTo(SqlConstraintKind.References));
        Assert.That(constraint.HasReferences(), Is.True);
        Assert.That(constraint.References!.TableName.GetText(), Is.EqualTo("parents"));
        Assert.That(constraint.References.GetColumnNameAt(0).GetText(), Is.EqualTo("id"));
    }

    [Test]
    public void CreateTableParser_ParsesColumnConstraintConflictClauses()
    {
        SqlTableDefinition table = ParseTable("create table items (id integer primary key on conflict replace autoincrement, name text not null on conflict fail unique on conflict ignore)");
        SqlColumnConstraint idPrimaryKey = table.GetColumnAt(0).GetConstraintAt(0);
        SqlColumnConstraint nameNotNull = table.GetColumnAt(1).GetConstraintAt(0);
        SqlColumnConstraint nameUnique = table.GetColumnAt(1).GetConstraintAt(1);

        Assert.That(idPrimaryKey.ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
        Assert.That(idPrimaryKey.IsAutoincrement, Is.True);
        Assert.That(nameNotNull.ConflictAction, Is.EqualTo(SqlConflictActionKind.Fail));
        Assert.That(nameUnique.ConflictAction, Is.EqualTo(SqlConflictActionKind.Ignore));
    }

    [Test]
    public void CreateTableParser_ParsesGeneratedColumnConstraints()
    {
        SqlTableDefinition table = ParseTable("create table items (qty integer, price integer, total integer generated always as (qty * price) stored, label as (lower(total)) virtual)");
        SqlColumnDefinition total = table.GetColumnAt(2);
        SqlColumnConstraint totalConstraint = total.GetConstraintAt(0);
        SqlColumnDefinition label = table.GetColumnAt(3);
        SqlColumnConstraint labelConstraint = label.GetConstraintAt(0);

        Assert.That(total.HasGeneratedExpression(), Is.True);
        Assert.That(total.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Stored));
        Assert.That(total.GeneratedExpression!.Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
        Assert.That(totalConstraint.Kind, Is.EqualTo(SqlConstraintKind.Generated));
        Assert.That(totalConstraint.IsGenerated(), Is.True);
        Assert.That(totalConstraint.HasExpression(), Is.True);
        Assert.That(totalConstraint.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Stored));
        Assert.That(label.HasTypeName(), Is.False);
        Assert.That(label.HasGeneratedExpression(), Is.True);
        Assert.That(label.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Virtual));
        Assert.That(labelConstraint.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Virtual));
    }

    [Test]
    public void CreateTableParser_ParsesNamedGeneratedColumnConstraintWithDefaultVirtualKind()
    {
        SqlTableDefinition table = ParseTable("create table items (raw_name text, search_name text constraint gen_search generated always as (lower(raw_name)) not null)");
        SqlColumnDefinition searchName = table.GetColumnAt(1);
        SqlColumnConstraint generated = searchName.GetConstraintAt(0);

        Assert.That(searchName.HasGeneratedExpression(), Is.True);
        Assert.That(searchName.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Virtual));
        Assert.That(generated.Name!.GetText(), Is.EqualTo("gen_search"));
        Assert.That(generated.GeneratedKind, Is.EqualTo(SqlGeneratedColumnKind.Virtual));
        Assert.That(searchName.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.NotNull));
    }

    [Test]
    public void CreateTableParser_ReportsGeneratedColumnMissingAs()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create table items (total integer generated always (1))", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDDL025"));
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        Assert.That(table.GetColumnAt(0).GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.Generated));
    }

    [Test]
    public void CreateTableParser_ParsesTableConstraintConflictClauses()
    {
        SqlTableDefinition table = ParseTable("create table items (id integer, name text, primary key(id) on conflict rollback, unique(name) on conflict abort)");

        Assert.That(table.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.PrimaryKey));
        Assert.That(table.GetConstraintAt(0).ConflictAction, Is.EqualTo(SqlConflictActionKind.Rollback));
        Assert.That(table.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.Unique));
        Assert.That(table.GetConstraintAt(1).ConflictAction, Is.EqualTo(SqlConflictActionKind.Abort));
    }

    [Test]
    public void CreateTableParser_ParsesPrimaryKeyTableConstraint()
    {
        SqlTableDefinition table = ParseTable("create table items (id integer, constraint pk_items primary key (id))");
        SqlTableConstraint constraint = table.GetConstraintAt(0);

        Assert.That(table.GetColumnCount(), Is.EqualTo(1));
        Assert.That(table.GetColumnAt(0).Name.GetText(), Is.EqualTo("id"));
        Assert.That(table.GetConstraintCount(), Is.EqualTo(1));
        Assert.That(constraint.Kind, Is.EqualTo(SqlConstraintKind.PrimaryKey));
        Assert.That(constraint.Name!.GetText(), Is.EqualTo("pk_items"));
        Assert.That(constraint.GetColumnAt(0).GetText(), Is.EqualTo("id"));
    }

    [Test]
    public void CreateTableParser_ParsesUniqueAndCheckTableConstraints()
    {
        SqlTableDefinition table = ParseTable("create table items (id integer, name text, unique (name), check (id > 0))");

        Assert.That(table.GetConstraintCount(), Is.EqualTo(2));
        Assert.That(table.GetConstraintAt(0).Kind, Is.EqualTo(SqlConstraintKind.Unique));
        Assert.That(table.GetConstraintAt(0).GetColumnAt(0).GetText(), Is.EqualTo("name"));
        Assert.That(table.GetConstraintAt(1).Kind, Is.EqualTo(SqlConstraintKind.Check));
        Assert.That(table.GetConstraintAt(1).HasExpressions(), Is.True);
        Assert.That(table.GetConstraintAt(1).GetExpressionAt(0).Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
    }

    [Test]
    public void CreateTableParser_ParsesForeignKeyTableConstraint()
    {
        SqlTableDefinition table = ParseTable("create table items (parent_id integer, foreign key (parent_id) references parents(id))");
        SqlTableConstraint constraint = table.GetConstraintAt(0);

        Assert.That(constraint.Kind, Is.EqualTo(SqlConstraintKind.ForeignKey));
        Assert.That(constraint.GetColumnAt(0).GetText(), Is.EqualTo("parent_id"));
        Assert.That(constraint.HasReferences(), Is.True);
        Assert.That(constraint.References!.TableName.GetText(), Is.EqualTo("parents"));
        Assert.That(constraint.References.GetColumnNameAt(0).GetText(), Is.EqualTo("id"));
    }

    [Test]
    public void CreateTableParser_ParsesTemporaryKeywordVariant()
    {
        SqlTableDefinition table = ParseTable("create temporary table items (id)");

        Assert.That(table.IsTemporary, Is.True);
        Assert.That(table.Name.GetText(), Is.EqualTo("items"));
        Assert.That(table.GetColumnAt(0).Name.GetText(), Is.EqualTo("id"));
    }

    [Test]
    public void CreateTableParser_ParsesWithoutRowIdAndStrictTableOptions()
    {
        SqlTableDefinition withoutRowId = ParseTable("create table items (id integer primary key) without rowid");
        SqlTableDefinition strict = ParseTable("create table values_table (value integer) strict");
        SqlTableDefinition both = ParseTable("create table keyed_items (id integer primary key) strict, without rowid");
        SqlTableDefinition reversed = ParseTable("create table reversed_items (id integer primary key) without rowid, strict");

        Assert.That(withoutRowId.WithoutRowId, Is.True);
        Assert.That(withoutRowId.IsStrict, Is.False);
        Assert.That(strict.WithoutRowId, Is.False);
        Assert.That(strict.IsStrict, Is.True);
        Assert.That(both.WithoutRowId, Is.True);
        Assert.That(both.IsStrict, Is.True);
        Assert.That(reversed.WithoutRowId, Is.True);
        Assert.That(reversed.IsStrict, Is.True);
    }

    [Test]
    public void CreateTableParser_ReportsMalformedTableOptions()
    {
        SqlParseResult missingRowId = SqlStatementParser.ParseScript("create table items (id integer primary key) without", SqlParseOptions.CreateSqlite());
        SqlParseResult trailingComma = SqlStatementParser.ParseScript("create table items (id integer) strict,", SqlParseOptions.CreateSqlite());

        Assert.That(missingRowId.IsSuccess(), Is.False);
        Assert.That(missingRowId.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDDL028"));
        Assert.That(trailingComma.IsSuccess(), Is.False);
        Assert.That(trailingComma.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDDL027"));
    }

    [Test]
    public void CreateTableParser_ParsesCreateTableAsSelect()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create table archived_items as select id, name from items where active = 0 order by id limit 10", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        Assert.That(table.Name.GetText(), Is.EqualTo("archived_items"));
        Assert.That(table.HasColumns(), Is.False);
        Assert.That(table.HasConstraints(), Is.False);
        Assert.That(table.HasQuery(), Is.True);
        Assert.That(table.Query!.GetSelectCoreCount(), Is.EqualTo(1));
        Assert.That(table.Query.GetSelectCoreAt(0).GetResultColumnCount(), Is.EqualTo(2));
        Assert.That(table.Query.HasOrderBy(), Is.True);
        Assert.That(table.Query.HasLimit(), Is.True);
    }

    [Test]
    public void CreateTableParser_ParsesCreateTableAsWithQuery()
    {
        SqlTableDefinition table = ParseTable("create table copied_items as with source_items as (select id from items) select * from source_items");

        Assert.That(table.HasQuery(), Is.True);
        Assert.That(table.Query!.HasCommonTableExpressions(), Is.True);
        Assert.That(table.Query.GetCommonTableExpressionAt(0).Name.GetText(), Is.EqualTo("source_items"));
    }

    [Test]
    public void CreateTableParser_ReportsCreateTableAsMissingQuery()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create table copied_items as", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLQUERY001"));
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        Assert.That(table.HasQuery(), Is.True);
    }

    [Test]
    public void CreateTableParser_ReportsMissingColumnList()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create table items", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLDDL004"));
        SqlTableDefinition table = result.Script.GetStatementAt(0).CreateTable!;
        Assert.That(table.Name.GetText(), Is.EqualTo("items"));
        Assert.That(table.HasColumns(), Is.False);
    }

    [Test]
    public void AstSearch_FindsCreateTableNameAndColumns()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create table items (id, name)", SqlParseOptions.CreateSqlite());

        SqlName[] tables = SqlAstSearch.FindTables(result.Script.GetStatementAt(0));
        SqlName[] columns = SqlAstSearch.FindColumns(result.Script.GetStatementAt(0));

        Assert.That(tables, Has.Length.EqualTo(1));
        Assert.That(tables[0].GetText(), Is.EqualTo("items"));
        Assert.That(columns, Has.Length.EqualTo(2));
        Assert.That(columns[0].GetText(), Is.EqualTo("id"));
        Assert.That(columns[1].GetText(), Is.EqualTo("name"));
    }

    [Test]
    public void CreateTableParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlCreateTableParser(null!));
    }

    static SqlTableDefinition ParseTable(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).CreateTable!;
    }
}
