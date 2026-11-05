using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CreateIndexParserTests
{
    [Test]
    public void StatementParser_AttachesCreateIndexSchema()
    {
        SqlParseResult result = SqlStatementParser.ParseScript("create unique index if not exists idx_items_name on items (name collate nocase desc, lower(title)) where active = 1", SqlParseOptions.CreateSqlite());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.Script.GetStatementCount(), Is.EqualTo(1));
        SqlStatement statement = result.Script.GetStatementAt(0);
        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.CreateIndex));
        Assert.That(statement.CreateIndex, Is.Not.Null);
        SqlIndexDefinition index = statement.CreateIndex!;
        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.IfNotExists, Is.True);
        Assert.That(index.Name.GetText(), Is.EqualTo("idx_items_name"));
        Assert.That(index.TableName.GetText(), Is.EqualTo("items"));
        Assert.That(index.GetKeyExpressionCount(), Is.EqualTo(2));
        Assert.That(index.GetKeyExpressionAt(0).Expression.Kind, Is.EqualTo(SqlExpressionKind.Name));
        Assert.That(index.GetKeyExpressionAt(0).Collation!.GetText(), Is.EqualTo("nocase"));
        Assert.That(index.GetKeyExpressionAt(0).Direction, Is.EqualTo(SqlOrderingDirection.Desc));
        Assert.That(index.GetKeyExpressionAt(1).Expression.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(index.HasWhereExpression(), Is.True);
        Assert.That(index.WhereExpression!.Kind, Is.EqualTo(SqlExpressionKind.Binary));
    }

    [Test]
    public void CreateIndexParser_ParsesExpressionKeyOrderingAndQualifiedNames()
    {
        SqlIndexDefinition index = ParseIndex("create index main.idx_items_score on main.items ((score + 1) asc nulls last)");
        SqlOrderingTerm key = index.GetKeyExpressionAt(0);

        Assert.That(index.IsUnique, Is.False);
        Assert.That(index.IfNotExists, Is.False);
        Assert.That(index.Name.GetText(), Is.EqualTo("main.idx_items_score"));
        Assert.That(index.TableName.GetText(), Is.EqualTo("main.items"));
        Assert.That(key.Expression.Kind, Is.EqualTo(SqlExpressionKind.Parenthesized));
        Assert.That(key.Direction, Is.EqualTo(SqlOrderingDirection.Asc));
        Assert.That(key.NullOrdering, Is.EqualTo(SqlNullOrdering.Last));
        Assert.That(index.HasWhereExpression(), Is.False);
    }

    [Test]
    public void CreateIndexParser_ReportsMalformedInputs()
    {
        SqlParseResult missingOn = SqlStatementParser.ParseScript("create index idx_items items(id)", SqlParseOptions.CreateSqlite());
        SqlParseResult missingKeyList = SqlStatementParser.ParseScript("create index idx_items on items", SqlParseOptions.CreateSqlite());
        SqlParseResult malformedIf = SqlStatementParser.ParseScript("create index if idx_items on items(id)", SqlParseOptions.CreateSqlite());

        Assert.That(missingOn.IsSuccess(), Is.False);
        Assert.That(missingOn.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINDEX007"));
        Assert.That(missingKeyList.IsSuccess(), Is.False);
        Assert.That(missingKeyList.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINDEX009"));
        Assert.That(malformedIf.IsSuccess(), Is.False);
        Assert.That(malformedIf.Diagnostics.GetAt(0).Code, Is.EqualTo("SQLINDEX003"));
    }

    [Test]
    public void CreateIndexParser_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlCreateIndexParser(null!));
    }

    static SqlIndexDefinition ParseIndex(string text)
    {
        SqlParseResult result = SqlStatementParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script.GetStatementAt(0).CreateIndex!;
    }
}
