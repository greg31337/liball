using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DmlCompletionTests
{
    [Test]
    public void UpdateParser_ExposesTypedSingleAndMultiColumnAssignments()
    {
        SqlStatement statement = ParseStatement("update items set (name, updated_at) = (source.name, source.updated_at), active = 1 from source where source.id = items.id returning id order by id limit 1", SqlDialectKind.SQLite);
        SqlUpdateStatement update = statement.Update!;

        Assert.That(update.Assignments.Length, Is.EqualTo(2));
        Assert.That(update.Assignments[0].Columns.Length, Is.EqualTo(2));
        Assert.That(update.Assignments[0].Columns[0].GetText(), Is.EqualTo("name"));
        Assert.That(update.Assignments[0].Columns[1].GetText(), Is.EqualTo("updated_at"));
        Assert.That(update.Assignments[0].Value.Kind, Is.EqualTo(SqlExpressionKind.RowValue));
        Assert.That(update.Assignments[1].Columns[0].GetText(), Is.EqualTo("active"));
        Assert.That(update.From.Length, Is.EqualTo(1));
        Assert.That(update.Returning, Is.Not.Null);
        Assert.That(update.OrderBy.Length, Is.EqualTo(1));
        Assert.That(update.Limit, Is.Not.Null);
        Assert.That(SqlGenerator.GenerateStatement(statement), Is.EqualTo("UPDATE items SET (name, updated_at) = (source.name, source.updated_at), active = 1 FROM source WHERE source.id = items.id RETURNING id ORDER BY id LIMIT 1"));
    }

    [Test]
    public void UpsertParser_ExposesTypedMultiColumnAssignment()
    {
        SqlStatement statement = ParseStatement("insert into items values (1, 'a') on conflict(id) do update set (name, updated_at) = (excluded.name, excluded.updated_at)", SqlDialectKind.SQLite);
        SqlUpsertClause upsert = statement.Insert!.UpsertClauses[0];

        Assert.That(upsert.SyntaxKind, Is.EqualTo(SqlUpsertSyntaxKind.OnConflict));
        Assert.That(upsert.Assignments.Length, Is.EqualTo(1));
        Assert.That(upsert.Assignments[0].Columns.Length, Is.EqualTo(2));
        Assert.That(upsert.Assignments[0].Value.Kind, Is.EqualTo(SqlExpressionKind.RowValue));
        Assert.That(SqlGenerator.GenerateStatement(statement), Is.EqualTo("INSERT INTO items VALUES (1, 'a') ON CONFLICT(id) DO UPDATE SET (name, updated_at) = (excluded.name, excluded.updated_at)"));
    }

    [Test]
    public void PostgreSqlUpsert_ExposesNamedConstraintTarget()
    {
        SqlStatement statement = ParseStatement("insert into items(id) values (1) on conflict on constraint items_pkey do nothing", SqlDialectKind.PostgreSql);
        SqlUpsertClause upsert = statement.Insert!.UpsertClauses[0];

        Assert.That(upsert.ConflictTarget, Is.Not.Null);
        Assert.That(upsert.ConflictTarget!.Kind, Is.EqualTo(SqlConflictTargetKind.Constraint));
        Assert.That(upsert.ConflictTarget.ConstraintName!.GetText(), Is.EqualTo("items_pkey"));
        Assert.That(upsert.ActionKind, Is.EqualTo(SqlUpsertActionKind.DoNothing));
        Assert.That(SqlGenerator.GenerateStatement(statement), Is.EqualTo("INSERT INTO items (id) VALUES (1) ON CONFLICT ON CONSTRAINT items_pkey DO NOTHING"));
    }

    [Test]
    public void MySqlUpsert_ExposesDuplicateKeySyntaxAndAssignments()
    {
        SqlStatement statement = ParseStatement("insert into items(id, name) values (1, 'a') on duplicate key update name = values(name)", SqlDialectKind.MySql);
        SqlUpsertClause upsert = statement.Insert!.UpsertClauses[0];

        Assert.That(upsert.SyntaxKind, Is.EqualTo(SqlUpsertSyntaxKind.OnDuplicateKey));
        Assert.That(upsert.ConflictTarget!.Kind, Is.EqualTo(SqlConflictTargetKind.DuplicateKey));
        Assert.That(upsert.Assignments.Length, Is.EqualTo(1));
        Assert.That(upsert.Assignments[0].Columns[0].GetText(), Is.EqualTo("name"));
        Assert.That(upsert.Assignments[0].Value.Kind, Is.EqualTo(SqlExpressionKind.FunctionCall));
        Assert.That(SqlGenerator.GenerateStatement(statement), Is.EqualTo("INSERT INTO items (id, name) VALUES (1, 'a') ON DUPLICATE KEY UPDATE name = values(name)"));
    }

    [Test]
    public void MergeParser_ExposesMatchedAndNotMatchedActions()
    {
        string text = "merge into target as t using source as s on t.id = s.id when matched and s.deleted = 1 then delete when matched then update set t.name = s.name, (t.a, t.b) = (s.a, s.b) when not matched by target then insert (id, name) values (s.id, s.name) when not matched by source then update set t.active = 0";
        SqlStatement statement = ParseStatement(text, SqlDialectKind.SqlServer);
        SqlMergeStatement merge = statement.Merge!;

        Assert.That(statement.Kind, Is.EqualTo(SqlStatementKind.Merge));
        Assert.That(merge.TableName.GetText(), Is.EqualTo("target"));
        Assert.That(merge.Alias!.GetText(), Is.EqualTo("t"));
        Assert.That(merge.Source.Name!.GetText(), Is.EqualTo("source"));
        Assert.That(merge.Source.Alias!.GetText(), Is.EqualTo("s"));
        Assert.That(merge.Condition.Kind, Is.EqualTo(SqlExpressionKind.Binary));
        Assert.That(merge.Clauses.Length, Is.EqualTo(4));
        Assert.That(merge.Clauses[0].MatchKind, Is.EqualTo(SqlMergeMatchKind.Matched));
        Assert.That(merge.Clauses[0].Condition, Is.Not.Null);
        Assert.That(merge.Clauses[0].ActionKind, Is.EqualTo(SqlMergeActionKind.Delete));
        Assert.That(merge.Clauses[1].ActionKind, Is.EqualTo(SqlMergeActionKind.Update));
        Assert.That(merge.Clauses[1].Assignments.Length, Is.EqualTo(2));
        Assert.That(merge.Clauses[1].Assignments[1].Columns.Length, Is.EqualTo(2));
        Assert.That(merge.Clauses[2].MatchKind, Is.EqualTo(SqlMergeMatchKind.NotMatchedByTarget));
        Assert.That(merge.Clauses[2].InsertColumns.Length, Is.EqualTo(2));
        Assert.That(merge.Clauses[2].InsertValues.Length, Is.EqualTo(2));
        Assert.That(merge.Clauses[3].MatchKind, Is.EqualTo(SqlMergeMatchKind.NotMatchedBySource));
        Assert.That(merge.Clauses[3].ActionKind, Is.EqualTo(SqlMergeActionKind.Update));
    }

    [Test]
    public void MergeGeneratorAndWalker_CoverTypedPayloads()
    {
        SqlStatement statement = ParseStatement("merge into target t using source s on t.id = s.id when matched then update set t.name = s.name when not matched then insert (id, name) values (s.id, s.name)", SqlDialectKind.SqlServer);
        SqlAstWalker walker = new SqlAstWalker();

        walker.WalkStatement(statement);

        Assert.That(SqlGenerator.GenerateStatement(statement), Is.EqualTo("MERGE INTO target AS t USING source AS s ON t.id = s.id WHEN MATCHED THEN UPDATE SET t.name = s.name WHEN NOT MATCHED BY TARGET THEN INSERT (id, name) VALUES (s.id, s.name)"));
        Assert.That(walker.MergeStatements, Does.Contain(statement.Merge));
        Assert.That(walker.MergeClauses.Count, Is.EqualTo(2));
        Assert.That(walker.Assignments.Count, Is.EqualTo(1));
        Assert.That(walker.Names.Exists(name => name.GetText() == "target"), Is.True);
        Assert.That(walker.Names.Exists(name => name.GetText() == "source"), Is.True);
    }

    [Test]
    public void ReplaceReturningAndLimitedDeleteRemainExplicitlyRepresented()
    {
        SqlInsertStatement replace = ParseStatement("replace into items values (1) returning id", SqlDialectKind.SQLite).Insert!;
        SqlDeleteStatement delete = ParseStatement("delete from items where active = 0 returning id order by id limit 3 offset 1", SqlDialectKind.SQLite).Delete!;

        Assert.That(replace.IsReplaceKeyword, Is.True);
        Assert.That(replace.ConflictAction, Is.EqualTo(SqlConflictActionKind.Replace));
        Assert.That(replace.Returning, Is.Not.Null);
        Assert.That(delete.Returning, Is.Not.Null);
        Assert.That(delete.OrderBy.Length, Is.EqualTo(1));
        Assert.That(delete.Limit!.OffsetExpression, Is.Not.Null);
    }

    static SqlStatement ParseStatement(string text, SqlDialectKind dialectKind)
    {
        SqlParseOptions options = SqlParseOptions.Create(SqlDialectCatalog.CreateOptions(dialectKind));
        SqlParseResult result = SqlStatementParser.ParseScript(text, options);
        Assert.That(result.IsSuccess(), Is.True, DiagnosticMessage(result));
        return result.Script.Statements[0];
    }

    static string DiagnosticMessage(SqlParseResult result)
    {
        if (result.Diagnostics.IsEmpty())
        {
            return "";
        }

        return result.Diagnostics.GetAt(0).Code + ": " + result.Diagnostics.GetAt(0).Message;
    }
}
