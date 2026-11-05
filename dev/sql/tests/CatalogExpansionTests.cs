using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class CatalogExpansionTests
{
    [Test]
    public void Catalog_PropagatesQualificationThroughSchemasTablesColumnsAndIndexes()
    {
        SqlCatalog catalog = new SqlCatalog();
        catalog.DefaultCatalogName = "warehouse";
        SqlCatalogSchema schema = catalog.CreateSchema("analytics");
        SqlCatalogTable table = new SqlCatalogTable("Events");
        SqlCatalogColumn column = new SqlCatalogColumn("EventId", "INTEGER");
        SqlCatalogIndex index = new SqlCatalogIndex("idx_events_id", "Events", new string[] { "EventId" });
        index.IsUnique = true;
        table.Columns.Add(column);
        table.Indexes.Add(index);

        schema.AddTable(table);

        Assert.That(schema.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(table.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(table.SchemaName, Is.EqualTo("analytics"));
        Assert.That(column.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(column.SchemaName, Is.EqualTo("analytics"));
        Assert.That(column.RelationName, Is.EqualTo("Events"));
        Assert.That(index.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(index.SchemaName, Is.EqualTo("analytics"));
        Assert.That(index.IsUnique, Is.True);
        Assert.That(catalog.FindSchemaByName(Name("warehouse.analytics")), Is.SameAs(schema));
        Assert.That(catalog.FindTableByName(Name("warehouse.analytics.events")), Is.SameAs(table));
        Assert.That(catalog.FindTableByName(Name("other.analytics.events")), Is.Null);
    }

    [Test]
    public void Catalog_StoresQualifiedViewsColumnsAndDefinitionQueries()
    {
        SqlCatalog catalog = new SqlCatalog();
        catalog.DefaultCatalogName = "warehouse";
        SqlCatalogSchema schema = catalog.CreateSchema("reporting");
        SqlCatalogView view = new SqlCatalogView("ActiveItems");
        SqlCatalogColumn column = new SqlCatalogColumn("Name", "varchar(100)");
        column.Affinity = SqliteTypeAffinityKind.Text;
        column.CollationName = "nocase";
        view.Columns.Add(column);
        view.Query = new SqlParserService().ParseQuery("select name from items", SqlParseOptions.CreateSqlite()).Query;

        schema.AddView(view);

        Assert.That(view.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(view.SchemaName, Is.EqualTo("reporting"));
        Assert.That(view.Query, Is.Not.Null);
        Assert.That(column.RelationName, Is.EqualTo("ActiveItems"));
        Assert.That(column.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Text));
        Assert.That(column.CollationName, Is.EqualTo("nocase"));
        Assert.That(view.FindColumn("name", false), Is.SameAs(column));
        Assert.That(catalog.FindViewByName(Name("warehouse.reporting.activeitems")), Is.SameAs(view));
        Assert.That(catalog.FindViewByName(Name("wrong.reporting.activeitems")), Is.Null);
    }

    [Test]
    public void Catalog_ResolvesSchemaFunctionsAndCollationsWithReturnAffinity()
    {
        SqlCatalog catalog = new SqlCatalog();
        catalog.DefaultCatalogName = "warehouse";
        SqlCatalogSchema schema = catalog.CreateSchema("analytics");
        SqlCatalogFunction function = new SqlCatalogFunction("normalize", 1, false);
        function.ReturnTypeName = "REAL";
        function.ReturnAffinity = SqliteTypeAffinityKind.Real;
        SqlCatalogCollation collation = new SqlCatalogCollation("unicode_ci");
        schema.AddFunction(function);
        schema.AddCollation(collation);

        Assert.That(function.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(function.SchemaName, Is.EqualTo("analytics"));
        Assert.That(function.ReturnTypeName, Is.EqualTo("REAL"));
        Assert.That(function.ReturnAffinity, Is.EqualTo(SqliteTypeAffinityKind.Real));
        Assert.That(collation.CatalogName, Is.EqualTo("warehouse"));
        Assert.That(collation.SchemaName, Is.EqualTo("analytics"));
        Assert.That(catalog.FindFunctionByName(Name("warehouse.analytics.NORMALIZE"), 1), Is.SameAs(function));
        Assert.That(catalog.FindFunctionByName(Name("warehouse.analytics.normalize"), 2), Is.Null);
        Assert.That(catalog.FindCollationByName(Name("warehouse.analytics.UNICODE_CI")), Is.SameAs(collation));
    }

    [Test]
    public void Catalog_StoresScopedAliasesAndQualifiedTargets()
    {
        SqlCatalog catalog = new SqlCatalog();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogTable table = schema.CreateTable("orders");
        SqlCatalogColumn column = table.CreateColumn("customer_id", "integer");
        SqlCatalogAlias tableAlias = new SqlCatalogAlias("o", SqlCatalogAliasKind.Table, "orders");
        tableAlias.TargetSchemaName = "main";
        SqlCatalogAlias columnAlias = new SqlCatalogAlias("customer", SqlCatalogAliasKind.Column, "orders");
        columnAlias.TargetSchemaName = "main";
        columnAlias.TargetColumnName = "customer_id";

        catalog.AddAlias(tableAlias);
        schema.AddAlias(tableAlias);
        table.AddTableAlias(tableAlias);
        column.Aliases.Add(columnAlias);

        Assert.That(catalog.FindAlias("O"), Is.SameAs(tableAlias));
        Assert.That(schema.FindAlias("o", false), Is.SameAs(tableAlias));
        Assert.That(table.Aliases[0], Is.SameAs(tableAlias));
        Assert.That(column.Aliases[0], Is.SameAs(columnAlias));
        Assert.That(columnAlias.TargetSchemaName, Is.EqualTo("main"));
        Assert.That(columnAlias.TargetName, Is.EqualTo("orders"));
        Assert.That(columnAlias.TargetColumnName, Is.EqualTo("customer_id"));
    }

    [Test]
    public void CatalogExpansion_UsesSafeDefaultsAndRejectsInvalidValues()
    {
        SqlCatalogColumn column = new SqlCatalogColumn("payload", "");
        SqlCatalogFunction function = new SqlCatalogFunction("value", 0, false);
        SqlCatalogSchema schema = new SqlCatalogSchema("main");
        SqlCatalogView view = new SqlCatalogView("items_view");

        Assert.That(column.Affinity, Is.EqualTo(SqliteTypeAffinityKind.Unknown));
        Assert.That(column.CollationName, Is.Null);
        Assert.That(function.ReturnAffinity, Is.EqualTo(SqliteTypeAffinityKind.Unknown));
        Assert.That(function.ReturnTypeName, Is.Empty);
        Assert.Throws<ArgumentException>(() => new SqlCatalogView(" "));
        Assert.Throws<ArgumentNullException>(() => new SqlCatalogAlias(null!, SqlCatalogAliasKind.Table, "items"));
        Assert.Throws<ArgumentNullException>(() => new SqlCatalogAlias("i", SqlCatalogAliasKind.Table, null!));
        Assert.Throws<ArgumentNullException>(() => schema.AddView(null!));
        Assert.Throws<ArgumentNullException>(() => view.AddColumn(null!));
        Assert.Throws<ArgumentNullException>(() => view.AddAlias(null!));
    }

    static SqlName Name(string text)
    {
        string[] parts = text.Split('.');
        SqlNamePart[] nameParts = new SqlNamePart[parts.Length];
        int start = 0;
        int index = 0;
        while (index < parts.Length)
        {
            nameParts[index] = SqlNamePart.Create(parts[index], parts[index], new SqlTextSpan(start, parts[index].Length), SqlNameQuoteStyle.None);
            start = start + parts[index].Length + 1;
            index = index + 1;
        }

        return new SqlName(nameParts, new SqlTextSpan(0, text.Length));
    }
}
