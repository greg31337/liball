using System.Text;

namespace odf.Tests;

public class DatabaseDocumentModelTests
{
    [Test]
    public void MaterializesDatabaseFrontEndStructures()
    {
        OdfDocument document = ReadFlat(DatabaseXml());
        OdfDatabaseDocument? database = document.Database;
        Assert.That(database, Is.Not.Null);
        OdfDatabaseSource source = database.Source;
        OdfDatabaseConnection? connection = source.Connection;
        OdfDatabaseLogin? login = source.Login;
        OdfDatabaseDriverSettings? driver = source.DriverSettings;

        Assert.Multiple(() =>
        {
            Assert.That(document.Kind, Is.EqualTo(OdfDocumentKind.Database));
            Assert.That(connection?.Kind,
                Is.EqualTo(OdfDatabaseConnectionKind.Server));
            Assert.That(connection?.DatabaseType, Is.EqualTo("postgresql"));
            Assert.That(connection?.HostName, Is.EqualTo("db.example.test"));
            Assert.That(connection?.Port, Is.EqualTo("5432"));
            Assert.That(connection?.DatabaseName, Is.EqualTo("inventory"));
            Assert.That(login?.UserName, Is.EqualTo("reader"));
            Assert.That(login?.IsPasswordRequired, Is.EqualTo("true"));
            Assert.That(login?.LoginTimeout, Is.EqualTo("15"));
            Assert.That(driver?.ShowDeleted, Is.EqualTo("false"));
            Assert.That(driver?.AdditionalColumnStatement,
                Is.EqualTo("SERIAL"));
            Assert.That(driver?.FieldDelimiter, Is.EqualTo(","));
            Assert.That(driver?.Encoding, Is.EqualTo("UTF-8"));
            Assert.That(source.IncludedTableFilters,
                Is.EqualTo(new[] { "public.%" }));
            Assert.That(source.ExcludedTableFilters,
                Is.EqualTo(new[] { "public.tmp_%" }));
            Assert.That(source.TableTypeFilters,
                Is.EqualTo(new[] { "TABLE", "VIEW" }));
            Assert.That(source.Settings, Has.Length.EqualTo(1));
            Assert.That(source.Settings[0].Values,
                Is.EqualTo(new[] { "25", "50" }));
            Assert.That(database.Queries, Has.Length.EqualTo(2));
            Assert.That(database.Queries[0].Command,
                Is.EqualTo("SELECT * FROM products"));
            Assert.That(database.Queries[1].IsCollection, Is.True);
            Assert.That(database.Queries[1].Children[0].Name,
                Is.EqualTo("Recent"));
            Assert.That(database.Tables, Has.Length.EqualTo(2));
            Assert.That(database.Tables[0].IsDefinition, Is.False);
            Assert.That(database.Tables[0].Columns, Has.Length.EqualTo(1));
            Assert.That(database.Tables[1].IsDefinition, Is.True);
            Assert.That(database.Tables[1].Keys, Has.Length.EqualTo(1));
            Assert.That(database.Tables[1].Indices, Has.Length.EqualTo(1));
            Assert.That(database.Forms[0].Kind,
                Is.EqualTo(OdfDatabaseComponentKind.Collection));
            Assert.That(database.Forms[0].Children[0].Kind,
                Is.EqualTo(OdfDatabaseComponentKind.Form));
            Assert.That(database.Reports[0].Kind,
                Is.EqualTo(OdfDatabaseComponentKind.Report));
        });
    }

    [Test]
    public void WritesDatabaseConnectionQueryTableComponentAndSettingsChanges()
    {
        OdfDocument document = ReadFlat(DatabaseXml());
        OdfDatabaseDocument database = document.Database
            ?? throw new InvalidOperationException();
        OdfDatabaseConnection connection = database.Source.Connection
            ?? throw new InvalidOperationException();
        OdfDatabaseLogin login = database.Source.Login
            ?? throw new InvalidOperationException();
        OdfDatabaseDriverSettings driver = database.Source.DriverSettings
            ?? throw new InvalidOperationException();
        connection.HostName = "database.internal";
        connection.DatabaseName = "warehouse";
        login.UserName = "analyst";
        login.LoginTimeout = "30";
        driver.Encoding = "ISO-8859-1";
        database.Source.Settings[0].Values[1] = "100";
        database.Queries[0].Command = "SELECT sku FROM products";
        database.Queries[0].FilterStatement = "active = TRUE";
        database.Tables[0].Title = "Available products";
        database.Forms[0].Children[0].Href = "Forms/EditProduct";
        database.Reports[0].Title = "Quarterly stock";

        OdfDocument reread = ReadFlat(Encoding.UTF8.GetString(
            Write(document)));
        OdfDatabaseDocument changed = reread.Database
            ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Source.Connection?.HostName,
                Is.EqualTo("database.internal"));
            Assert.That(changed.Source.Connection?.DatabaseName,
                Is.EqualTo("warehouse"));
            Assert.That(changed.Source.Login?.UserName,
                Is.EqualTo("analyst"));
            Assert.That(changed.Source.Login?.LoginTimeout,
                Is.EqualTo("30"));
            Assert.That(changed.Source.DriverSettings?.Encoding,
                Is.EqualTo("ISO-8859-1"));
            Assert.That(changed.Source.Settings[0].Values[1],
                Is.EqualTo("100"));
            Assert.That(changed.Queries[0].Command,
                Is.EqualTo("SELECT sku FROM products"));
            Assert.That(changed.Queries[0].FilterStatement,
                Is.EqualTo("active = TRUE"));
            Assert.That(changed.Tables[0].Title,
                Is.EqualTo("Available products"));
            Assert.That(changed.Forms[0].Children[0].Href,
                Is.EqualTo("Forms/EditProduct"));
            Assert.That(changed.Reports[0].Title,
                Is.EqualTo("Quarterly stock"));
        });
    }

    [Test]
    public void ClassifiesExternalResourcesAndPreservesInlineContent()
    {
        string xml = FlatXml(
            "<db:data-source><db:connection-data>"
            + "<db:connection-resource xlink:type='simple' "
            + "xlink:href='https://db.example.test/source'/>"
            + "</db:connection-data></db:data-source>"
            + "<db:forms><db:component db:name='Inline' "
            + "xlink:href='https://forms.example.test/edit'>"
            + "<office:document office:version='1.4'>"
            + "<office:body><office:text/></office:body>"
            + "</office:document></db:component></db:forms>");
        OdfDocument document = ReadFlat(xml);
        OdfDatabaseDocument database = document.Database
            ?? throw new InvalidOperationException();
        string output = Encoding.UTF8.GetString(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(database.Source.Connection?.Kind,
                Is.EqualTo(OdfDatabaseConnectionKind.Resource));
            Assert.That(database.Source.Connection?.IsExternal, Is.True);
            Assert.That(database.Source.Connection?.PackagePath, Is.Empty);
            Assert.That(database.Forms[0].IsExternal, Is.True);
            Assert.That(database.Forms[0].InlineDocument, Is.Not.Null);
            Assert.That(output, Does.Contain("https://db.example.test/source"));
            Assert.That(output, Does.Contain("https://forms.example.test/edit"));
            Assert.That(output, Does.Contain("<office:document"));
        });
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "database.odb",
            new OdfLoadOptions());
    }

    static string DatabaseXml()
    {
        return FlatXml(
            "<db:data-source>"
            + "<db:connection-data><db:database-description>"
            + "<db:server-database db:type='postgresql' "
            + "db:hostname='db.example.test' db:port='5432' "
            + "db:database-name='inventory'/>"
            + "</db:database-description><db:login db:user-name='reader' "
            + "db:is-password-required='true' db:login-timeout='15'/>"
            + "</db:connection-data>"
            + "<db:driver-settings db:show-deleted='false' "
            + "db:system-driver-settings='sslmode=require' "
            + "db:parameter-name-substitution='true'>"
            + "<db:auto-increment db:additional-column-statement='SERIAL' "
            + "db:row-retrieving-statement='RETURNING id'/>"
            + "<db:delimiter db:field-delimiter=',' db:string-delimiter='&quot;' "
            + "db:decimal-delimiter='.' db:thousand-delimiter=' '/>"
            + "<db:character-set db:encoding='UTF-8'/></db:driver-settings>"
            + "<db:application-connection-settings db:max-row-count='500'>"
            + "<db:table-filter><db:include-filter db:pattern='public.%'/>"
            + "<db:exclude-filter db:pattern='public.tmp_%'/>"
            + "</db:table-filter><db:table-type-filter>"
            + "<db:table-type>TABLE</db:table-type>"
            + "<db:table-type>VIEW</db:table-type></db:table-type-filter>"
            + "<db:data-source-settings><db:data-source-setting "
            + "db:name='FetchSize' db:type='int' db:is-list='true'>"
            + "<db:data-source-setting-value>25</db:data-source-setting-value>"
            + "<db:data-source-setting-value>50</db:data-source-setting-value>"
            + "</db:data-source-setting></db:data-source-settings>"
            + "</db:application-connection-settings></db:data-source>"
            + "<db:queries><db:query db:name='Products' "
            + "db:title='All products' db:command='SELECT * FROM products' "
            + "db:escape-processing='true'>"
            + "<db:order-statement db:command='sku ASC'/>"
            + "<db:filter-statement db:command='active = 1'/>"
            + "</db:query><db:query-collection db:name='Saved'>"
            + "<db:query db:name='Recent' "
            + "db:command='SELECT * FROM products WHERE recent = 1'/>"
            + "</db:query-collection></db:queries>"
            + "<db:table-representations><db:table-representation "
            + "db:name='products' db:title='Products'>"
            + "<db:columns><db:column db:name='sku'/></db:columns>"
            + "</db:table-representation></db:table-representations>"
            + "<db:forms><db:component-collection db:name='Product forms'>"
            + "<db:component db:name='Edit' db:title='Edit product' "
            + "xlink:type='simple' xlink:href='Forms/Edit' "
            + "db:as-template='false'/></db:component-collection></db:forms>"
            + "<db:reports><db:component db:name='Stock' "
            + "db:title='Current stock' xlink:type='simple' "
            + "xlink:href='Reports/Stock'/></db:reports>"
            + "<db:schema-definition><db:table-definitions>"
            + "<db:table-definition db:name='products' db:type='TABLE'>"
            + "<db:column-definitions><db:column-definition db:name='id' "
            + "db:data-type='INTEGER'/></db:column-definitions>"
            + "<db:key db:name='pk_products' db:type='PRIMARY'>"
            + "<db:key-column db:name='id'/></db:key>"
            + "<db:index db:name='ix_products' db:is-unique='false'>"
            + "<db:index-column db:name='id' db:is-ascending='true'/>"
            + "</db:index></db:table-definition>"
            + "</db:table-definitions></db:schema-definition>");
    }

    static string FlatXml(string databaseChildren)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document "
            + "xmlns:office='urn:oasis:names:tc:opendocument:xmlns:office:1.0' "
            + "xmlns:db='urn:oasis:names:tc:opendocument:xmlns:database:1.0' "
            + "xmlns:xlink='http://www.w3.org/1999/xlink' "
            + "office:version='1.4' "
            + "office:mimetype='application/vnd.oasis.opendocument.base'>"
            + "<office:body><office:database>" + databaseChildren
            + "</office:database></office:body></office:document>";
    }
}
