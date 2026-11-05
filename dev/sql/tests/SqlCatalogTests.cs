using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class SqlCatalogTests
{
    [Test]
    public void SqlCatalog_CreatesDefaultSchemaAndFindsTablesAndColumns()
    {
        SqlCatalog catalog = SqlCatalog.CreateDefault();
        SqlCatalogSchema schema = catalog.FindSchema(null)!;
        SqlCatalogTable table = schema.CreateTable("Items");
        SqlCatalogColumn id = table.CreateColumn("Id", "integer");

        SqlCatalogTable? foundByString = catalog.FindTable("items");
        SqlCatalogTable? foundByName = catalog.FindTableByName(Name("main.Items"));
        SqlCatalogColumn? foundColumn = table.FindColumn("id", catalog.CaseSensitiveNames);

        Assert.That(schema.Name, Is.EqualTo("main"));
        Assert.That(foundByString, Is.SameAs(table));
        Assert.That(foundByName, Is.SameAs(table));
        Assert.That(foundColumn, Is.SameAs(id));
        Assert.That(id.IsNullable, Is.True);
        Assert.That(id.IsPrimaryKey, Is.False);
    }

    [Test]
    public void SqlCatalog_RespectsCaseSensitiveLookup()
    {
        SqlCatalog catalog = SqlCatalog.Create(true, "main");
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogTable table = schema.CreateTable("Items");

        Assert.That(catalog.FindTable("Items"), Is.SameAs(table));
        Assert.That(catalog.FindTable("items"), Is.Null);
        Assert.That(table.FindColumn("missing", catalog.CaseSensitiveNames), Is.Null);
    }

    [Test]
    public void SqlCatalog_FindsQualifiedTablesInNamedSchemas()
    {
        SqlCatalog catalog = SqlCatalog.CreateDefault();
        SqlCatalogSchema aux = catalog.CreateSchema("aux");
        SqlCatalogTable table = aux.CreateTable("items");

        Assert.That(catalog.FindTableByName(Name("aux.items")), Is.SameAs(table));
        Assert.That(catalog.FindTableByName(Name("missing.items")), Is.Null);
    }

    [Test]
    public void SqlCatalog_StoresIndexesFunctionsAndCollations()
    {
        SqlCatalog catalog = SqlCatalog.CreateDefault();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogIndex index = new SqlCatalogIndex("idx_items_id", "items", new string[] { "id" });
        SqlCatalogFunction function = new SqlCatalogFunction("count", -1, true);
        SqlCatalogCollation collation = new SqlCatalogCollation("nocase");

        schema.AddIndex(index);
        catalog.AddFunction(function);
        catalog.AddCollation(collation);

        Assert.That(schema.FindIndex("IDX_ITEMS_ID", catalog.CaseSensitiveNames), Is.SameAs(index));
        Assert.That(index.ColumnNames[0], Is.EqualTo("id"));
        Assert.That(catalog.FindFunction("COUNT", 1), Is.SameAs(function));
        Assert.That(catalog.FindFunction("count", 0)!.IsAggregate, Is.True);
        Assert.That(catalog.FindCollation("NOCASE"), Is.SameAs(collation));
    }

    [Test]
    public void SqlCatalog_RejectsInvalidInputs()
    {
        SqlCatalog catalog = SqlCatalog.CreateDefault();
        SqlCatalogSchema schema = catalog.FindSchema("main")!;
        SqlCatalogTable table = SqlCatalogTable.Create("items");

        Assert.Throws<ArgumentNullException>(() => SqlCatalog.Create(false, null!));
        Assert.Throws<ArgumentException>(() => new SqlCatalogSchema(" "));
        Assert.Throws<ArgumentNullException>(() => catalog.AddSchema(null!));
        Assert.Throws<ArgumentNullException>(() => catalog.FindTable((string)null!));
        Assert.Throws<ArgumentNullException>(() => schema.AddTable(null!));
        Assert.Throws<ArgumentNullException>(() => table.AddColumn(null!));
        Assert.Throws<ArgumentNullException>(() => SqlCatalogColumn.Create("id", null!));
        Assert.Throws<ArgumentNullException>(() => new SqlCatalogIndex("idx", "items", null!));
        Assert.Throws<ArgumentNullException>(() => new SqlCatalogIndex("idx", "items", new string[] { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlCatalogFunction("f", -2, false));
        Assert.Throws<ArgumentNullException>(() => catalog.AddFunction(null!));
        Assert.Throws<ArgumentNullException>(() => catalog.AddCollation(null!));
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
