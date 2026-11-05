using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class LogicalPlanConversionTests
{
    [Test]
    public void BuildPlan_ConvertsQueryPipelineAndResolvedJoins()
    {
        SqlCatalog catalog = Catalog("users", "departments");
        SqlScript script = Parse("select users.id from users join departments on users.department_id = departments.id where users.active = 1 order by users.id limit 5", SqlDialectKind.SQLite);
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();

        SqlLogicalPlanResult result = planner.BuildPlan(script, catalog);

        SqlLogicalPlanNode limit = result.Root.Inputs[0];
        SqlLogicalPlanNode sort = limit.Inputs[0];
        SqlLogicalPlanNode project = sort.Inputs[0];
        SqlLogicalPlanNode filter = project.Inputs[0];
        SqlLogicalPlanNode join = filter.Inputs[0];

        Assert.That(result.Root.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Script));
        Assert.That(limit.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Limit));
        Assert.That(sort.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Sort));
        Assert.That(project.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Project));
        Assert.That(project.Expressions.Length, Is.EqualTo(1));
        Assert.That(filter.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Filter));
        Assert.That(filter.Expressions.Length, Is.EqualTo(1));
        Assert.That(join.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Join));
        Assert.That(join.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Scan));
        Assert.That(join.Inputs[0].RelationName!.GetText(), Is.EqualTo("users"));
        Assert.That(join.Inputs[1].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Scan));
        Assert.That(join.Inputs[1].RelationName!.GetText(), Is.EqualTo("departments"));
        Assert.That(result.Root.IsComplete, Is.True);
        Assert.That(result.Diagnostics.Diagnostics, Is.Empty);
        Assert.That(result.Script, Is.SameAs(script));
        Assert.That(result.Catalog, Is.SameAs(catalog));
    }

    [Test]
    public void BuildPlan_ConvertsInsertUpdateDeleteAndMerge()
    {
        SqlCatalog catalog = Catalog("items", "changes");
        SqlScript dml = Parse("insert into items(id) values(1); update items set id = 2 where id = 1; delete from items where id = 2", SqlDialectKind.SQLite);
        SqlScript merge = Parse("merge into items i using changes c on i.id = c.id when matched then update set id = c.id", SqlDialectKind.SqlServer);
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();

        SqlLogicalPlanResult dmlResult = planner.BuildPlan(dml, catalog);
        SqlLogicalPlanResult mergeResult = planner.BuildPlan(merge, catalog);

        Assert.That(dmlResult.Root.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Insert));
        Assert.That(dmlResult.Root.Inputs[0].Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Scan));
        Assert.That(dmlResult.Root.Inputs[0].Inputs[1].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Values));
        Assert.That(dmlResult.Root.Inputs[1].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Update));
        Assert.That(dmlResult.Root.Inputs[1].Expressions.Length, Is.EqualTo(2));
        Assert.That(dmlResult.Root.Inputs[2].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Delete));
        Assert.That(dmlResult.Root.IsComplete, Is.True);
        Assert.That(mergeResult.Root.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Merge));
        Assert.That(mergeResult.Root.Inputs[0].Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Scan));
        Assert.That(mergeResult.Root.Inputs[0].Inputs[1].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Scan));
        Assert.That(mergeResult.Root.Inputs[0].Expressions.Length, Is.GreaterThanOrEqualTo(2));
        Assert.That(mergeResult.Root.IsComplete, Is.True);
    }

    [Test]
    public void BuildPlan_ConvertsValuesGroupingAndCompoundQueries()
    {
        SqlCatalog catalog = Catalog("items");
        SqlScript grouped = Parse("select category, count(id) from items group by category having count(id) > 1", SqlDialectKind.SQLite);
        SqlScript compound = Parse("values (1), (2) union all select 3", SqlDialectKind.SQLite);
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();

        SqlLogicalPlanResult groupedResult = planner.BuildPlan(grouped, catalog);
        SqlLogicalPlanResult compoundResult = planner.BuildPlan(compound, catalog);

        SqlLogicalPlanNode groupedProject = groupedResult.Root.Inputs[0];
        Assert.That(groupedProject.Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Project));
        Assert.That(groupedProject.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Filter));
        Assert.That(groupedProject.Inputs[0].Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Aggregate));
        Assert.That(compoundResult.Root.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.SetOperation));
        Assert.That(compoundResult.Root.Inputs[0].Inputs[0].Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Values));
        Assert.That(compoundResult.Root.IsComplete, Is.True);
    }

    [Test]
    public void BuildPlan_ReturnsUnknownNodesAndDiagnosticsForMissingSemantics()
    {
        SqlCatalog catalog = Catalog("known_items");
        SqlScript missing = Parse("select id from missing_items", SqlDialectKind.SQLite);
        SqlScript wildcard = Parse("select * from known_items", SqlDialectKind.SQLite);
        SqlScript ddl = Parse("create table later(id integer)", SqlDialectKind.SQLite);
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();

        SqlLogicalPlanResult missingResult = planner.BuildPlan(missing, catalog);
        SqlLogicalPlanResult wildcardResult = planner.BuildPlan(wildcard, catalog);
        SqlLogicalPlanResult ddlResult = planner.BuildPlan(ddl, catalog);

        SqlLogicalPlanNode missingUnknown = FindKind(missingResult.Root, SqlLogicalPlanNodeKind.Unknown);
        SqlLogicalPlanNode wildcardUnknown = FindKind(wildcardResult.Root, SqlLogicalPlanNodeKind.Unknown);
        Assert.That(missingUnknown.RelationName!.GetText(), Is.EqualTo("missing_items"));
        Assert.That(missingUnknown.IsComplete, Is.False);
        Assert.That(missingResult.Root.IsComplete, Is.False);
        Assert.That(HasCode(missingResult.Diagnostics, "SQLPLAN001"), Is.True);
        Assert.That(wildcardUnknown.Detail, Does.Contain("Wildcard"));
        Assert.That(HasCode(wildcardResult.Diagnostics, "SQLPLAN004"), Is.True);
        Assert.That(ddlResult.Root.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Unknown));
        Assert.That(HasCode(ddlResult.Diagnostics, "SQLPLAN002"), Is.True);
    }

    [Test]
    public void BuildPlan_ReportsScopeAndTableSourceLimitationsWithoutMutatingInput()
    {
        SqlCatalog catalog = Catalog("known_items");
        SqlScript cte = Parse("with source_rows as (select id from known_items) select id from source_rows", SqlDialectKind.SQLite);
        SqlScript function = Parse("select value from json_each('[1,2]')", SqlDialectKind.SQLite);
        SqlStatement originalStatement = cte.Statements[0];
        SqlQuery originalQuery = originalStatement.Query!;
        SqlCommonTableExpression originalCte = originalQuery.CommonTableExpressions[0];
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();

        SqlLogicalPlanResult cteResult = planner.BuildPlan(cte, catalog);
        SqlLogicalPlanResult functionResult = planner.BuildPlan(function, catalog);

        Assert.That(cteResult.Root.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Unknown));
        Assert.That(cteResult.Root.IsComplete, Is.False);
        Assert.That(HasCode(cteResult.Diagnostics, "SQLPLAN003"), Is.True);
        Assert.That(functionResult.Root.IsComplete, Is.False);
        Assert.That(FindKind(functionResult.Root, SqlLogicalPlanNodeKind.Unknown).Detail, Does.Contain("Table source kind"));
        Assert.That(HasCode(functionResult.Diagnostics, "SQLPLAN003"), Is.True);
        Assert.That(cte.Statements[0], Is.SameAs(originalStatement));
        Assert.That(cte.Statements[0].Query, Is.SameAs(originalQuery));
        Assert.That(originalQuery.CommonTableExpressions[0], Is.SameAs(originalCte));
        Assert.That(cteResult.Script, Is.SameAs(cte));
    }

    [Test]
    public void BuildPlan_ConvertsSubqueriesDefaultValuesInsertSelectAndOffsetFetch()
    {
        SqlCatalog catalog = Catalog("items");
        SqlScript subquery = Parse("select nested.id from (select id from items) as nested", SqlDialectKind.SQLite);
        SqlScript inserts = Parse("insert into items default values; insert into items select id from items", SqlDialectKind.SQLite);
        SqlScript offsetFetch = Parse("select id from items order by id offset 2 rows fetch next 3 rows only", SqlDialectKind.SqlServer);
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();

        SqlLogicalPlanResult subqueryResult = planner.BuildPlan(subquery, catalog);
        SqlLogicalPlanResult insertResult = planner.BuildPlan(inserts, catalog);
        SqlLogicalPlanResult offsetResult = planner.BuildPlan(offsetFetch, catalog);

        Assert.That(FindKind(subqueryResult.Root, SqlLogicalPlanNodeKind.Scan).RelationName!.GetText(), Is.EqualTo("items"));
        Assert.That(subqueryResult.Root.IsComplete, Is.True);
        Assert.That(insertResult.Root.Inputs[0].Inputs[1].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Values));
        Assert.That(insertResult.Root.Inputs[1].Inputs[1].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Project));
        Assert.That(insertResult.Root.IsComplete, Is.True);
        Assert.That(offsetResult.Root.Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Limit));
        Assert.That(offsetResult.Root.Inputs[0].Expressions.Length, Is.EqualTo(2));
        Assert.That(offsetResult.Root.Inputs[0].Inputs[0].Kind, Is.EqualTo(SqlLogicalPlanNodeKind.Sort));
        Assert.That(offsetResult.Root.IsComplete, Is.True);
    }

    [Test]
    public void BuildPlan_RejectsNullScriptAndCatalog()
    {
        ISqlLogicalPlanner planner = new SqlLogicalPlannerService();
        SqlScript script = Parse("select 1", SqlDialectKind.SQLite);
        SqlCatalog catalog = new SqlCatalog();

        Assert.Throws<ArgumentNullException>(() => planner.BuildPlan(null!, catalog));
        Assert.Throws<ArgumentNullException>(() => planner.BuildPlan(script, null!));
    }

    static SqlCatalog Catalog(params string[] tableNames)
    {
        SqlCatalog catalog = new SqlCatalog();
        SqlCatalogSchema schema = catalog.FindSchema(null)!;
        int index = 0;
        while (index < tableNames.Length)
        {
            schema.CreateTable(tableNames[index]);
            index = index + 1;
        }

        return catalog;
    }

    static SqlScript Parse(string text, SqlDialectKind kind)
    {
        SqlDialectOptions dialect = SqlDialectCatalog.GetDialect(kind).CreateOptions();
        SqlParseResult result = SqlParser.ParseScript(text, SqlParseOptions.Create(dialect));
        Assert.That(result.IsSuccess(), Is.True, Diagnostics(result.Diagnostics));
        return result.Script;
    }

    static SqlLogicalPlanNode FindKind(SqlLogicalPlanNode node, SqlLogicalPlanNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.Inputs.Length)
        {
            SqlLogicalPlanNode found = FindKindOrNull(node.Inputs[index], kind);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        Assert.Fail("Missing logical-plan node kind: " + kind.ToString());
        return node;
    }

    static SqlLogicalPlanNode FindKindOrNull(SqlLogicalPlanNode node, SqlLogicalPlanNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.Inputs.Length)
        {
            SqlLogicalPlanNode found = FindKindOrNull(node.Inputs[index], kind);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null!;
    }

    static bool HasCode(SqlDiagnosticBag diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.Diagnostics.Count)
        {
            if (diagnostics.Diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string Diagnostics(SqlDiagnosticBag diagnostics)
    {
        string text = "";
        int index = 0;
        while (index < diagnostics.Diagnostics.Count)
        {
            text = text + diagnostics.Diagnostics[index].Message + "\n";
            index = index + 1;
        }

        return text;
    }
}
