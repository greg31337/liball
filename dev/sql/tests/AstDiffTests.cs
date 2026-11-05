using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstDiffTests
{
    [Test]
    public void DiffScripts_ReportsAllChangeKindsWithStablePathsAndSpans()
    {
        SqlScript original = Parse("select a, b, c from items");
        SqlScript modified = Parse("select a, x from items; select d from extra");
        ISqlAstTransformer transformer = new SqlAstTransformService();

        SqlAstDiffResult result = transformer.DiffScripts(original, modified);

        SqlAstDiffEntry root = Find(result, "$");
        SqlAstDiffEntry unchanged = Find(result, "$.Statements[0].Query.SelectCores[0].ResultColumns[0]");
        SqlAstDiffEntry replaced = Find(result, "$.Statements[0].Query.SelectCores[0].ResultColumns[1]");
        SqlAstDiffEntry removed = Find(result, "$.Statements[0].Query.SelectCores[0].ResultColumns[2]");
        SqlAstDiffEntry added = Find(result, "$.Statements[1]");

        Assert.That(root.Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(unchanged.Kind, Is.EqualTo(SqlAstDiffKind.Unchanged));
        Assert.That(replaced.Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(removed.Kind, Is.EqualTo(SqlAstDiffKind.Removed));
        Assert.That(added.Kind, Is.EqualTo(SqlAstDiffKind.Added));
        Assert.That(unchanged.NodeType, Is.EqualTo("SqlResultColumn"));
        Assert.That(unchanged.OriginalNode, Is.SameAs(original.Statements[0].Query!.SelectCores[0].ResultColumns[0]));
        Assert.That(unchanged.ModifiedNode, Is.SameAs(modified.Statements[0].Query!.SelectCores[0].ResultColumns[0]));
        Assert.That(replaced.OriginalSpan, Is.SameAs(original.Statements[0].Query!.SelectCores[0].ResultColumns[1].Span));
        Assert.That(replaced.ModifiedSpan, Is.SameAs(modified.Statements[0].Query!.SelectCores[0].ResultColumns[1].Span));
        Assert.That(removed.OriginalSpan, Is.Not.Null);
        Assert.That(removed.ModifiedSpan, Is.Null);
        Assert.That(added.OriginalSpan, Is.Null);
        Assert.That(added.ModifiedSpan, Is.SameAs(modified.Statements[1].Span));
        Assert.That(result.OriginalScript, Is.SameAs(original));
        Assert.That(result.ModifiedScript, Is.SameAs(modified));
    }

    [Test]
    public void DiffScripts_ReportsNestedExpressionReplacementAndUnchangedOperands()
    {
        SqlScript original = Parse("select id from items where score + bonus > 10");
        SqlScript modified = Parse("select id from items where score + bonus > 20");

        SqlAstDiffResult result = new SqlAstTransformService().DiffScripts(original, modified);

        string wherePath = "$.Statements[0].Query.SelectCores[0].Where";
        Assert.That(Find(result, wherePath).Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(Find(result, wherePath + ".Left").Kind, Is.EqualTo(SqlAstDiffKind.Unchanged));
        Assert.That(Find(result, wherePath + ".Right").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(Find(result, wherePath + ".Right.Literal").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
    }

    [Test]
    public void DiffScripts_IgnoresSourceWhitespaceAndSpanDifferencesForStructure()
    {
        SqlScript original = Parse("select id,name from items where id=1");
        SqlScript modified = Parse("  select  id, name\nfrom items\nwhere id = 1  ");

        SqlAstDiffResult result = new SqlAstTransformService().DiffScripts(original, modified);

        Assert.That(result.Entries.Length, Is.GreaterThan(1));
        Assert.That(Count(result, SqlAstDiffKind.Unchanged), Is.EqualTo(result.Entries.Length));
        Assert.That(Find(result, "$").OriginalSpan!.Length, Is.Not.EqualTo(Find(result, "$").ModifiedSpan!.Length));
    }

    [Test]
    public void DiffScripts_ReportsChangedStatementPayloads()
    {
        SqlScript original = Parse("delete from logs where id = 1");
        SqlScript modified = Parse("insert into logs values (1)");

        SqlAstDiffResult result = new SqlAstTransformService().DiffScripts(original, modified);

        Assert.That(Find(result, "$.Statements[0]").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(Find(result, "$.Statements[0].Delete").Kind, Is.EqualTo(SqlAstDiffKind.Removed));
        Assert.That(Find(result, "$.Statements[0].Insert").Kind, Is.EqualTo(SqlAstDiffKind.Added));
    }

    [Test]
    public void DiffScripts_ReportsChangesAcrossCtesWindowsAndLimitsWithoutMutation()
    {
        SqlScript original = Parse("with c(x) as (select 1) select sum(x) over win from c window win as (order by x rows between 1 preceding and current row) order by x limit 5 offset 1");
        SqlScript modified = Parse("with c(x) as (select 1) select sum(x) over win from c window win as (order by x rows between 2 preceding and current row) order by x limit 6 offset 1");
        SqlQuery originalQuery = original.Statements[0].Query!;
        SqlWindowDefinition originalWindow = originalQuery.SelectCores[0].Windows[0];

        SqlAstDiffResult result = new SqlAstTransformService().DiffScripts(original, modified);

        string queryPath = "$.Statements[0].Query";
        Assert.That(Find(result, queryPath + ".CommonTableExpressions[0].Query").Kind, Is.EqualTo(SqlAstDiffKind.Unchanged));
        Assert.That(Find(result, queryPath + ".SelectCores[0].Windows[0].Frame.Start.Expression.Literal").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(Find(result, queryPath + ".OrderBy[0]").Kind, Is.EqualTo(SqlAstDiffKind.Unchanged));
        Assert.That(Find(result, queryPath + ".Limit.LimitExpression.Literal").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(original.Statements[0].Query, Is.SameAs(originalQuery));
        Assert.That(originalQuery.SelectCores[0].Windows[0], Is.SameAs(originalWindow));
        Assert.That(result.OriginalScript, Is.SameAs(original));
        Assert.That(result.ModifiedScript, Is.SameAs(modified));
    }

    [Test]
    public void DiffScripts_ReportsChangesWithinSameDmlPayloadKind()
    {
        SqlScript original = Parse("update items set value = 1 where id = 2");
        SqlScript modified = Parse("update items set value = 3 where id = 2");

        SqlAstDiffResult result = new SqlAstTransformService().DiffScripts(original, modified);

        Assert.That(Find(result, "$.Statements[0]").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(Find(result, "$.Statements[0].Update").Kind, Is.EqualTo(SqlAstDiffKind.Replaced));
        Assert.That(Find(result, "$.Statements[0].Update").OriginalNode, Is.SameAs(original.Statements[0].Update));
        Assert.That(Find(result, "$.Statements[0].Update").ModifiedNode, Is.SameAs(modified.Statements[0].Update));
    }

    [Test]
    public void DiffScripts_RejectsNullInputs()
    {
        ISqlAstTransformer transformer = new SqlAstTransformService();
        SqlScript script = Parse("select 1");

        Assert.Throws<ArgumentNullException>(() => transformer.DiffScripts(null!, script));
        Assert.Throws<ArgumentNullException>(() => transformer.DiffScripts(script, null!));
    }

    static SqlAstDiffEntry Find(SqlAstDiffResult result, string path)
    {
        int index = 0;
        while (index < result.Entries.Length)
        {
            if (result.Entries[index].Path == path)
            {
                return result.Entries[index];
            }

            index = index + 1;
        }

        Assert.Fail("Missing diff path: " + path);
        return result.Entries[0];
    }

    static int Count(SqlAstDiffResult result, SqlAstDiffKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < result.Entries.Length)
        {
            if (result.Entries[index].Kind == kind)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static SqlScript Parse(string text)
    {
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.CreateSqlite());
        Assert.That(result.IsSuccess(), Is.True);
        return result.Script;
    }
}
