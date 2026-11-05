using System;
using System.Collections.Generic;

namespace odf;

internal static class DatabaseDocumentModel
{
    internal static void LoadDatabase(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for database materialization.");
        }
        OdfNode? node = FindDatabase(document);
        if (node == null)
        {
            document.Database = null;
        }
        else
        {
            document.Database = LoadDocument(document, node);
        }
        DatabaseDocumentState state = new DatabaseDocumentState();
        state.Database = document.Database;
        document.DatabaseState = state;
    }

    internal static void SynchronizeDatabase(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for database synchronization.");
        }
        if (document.Database == null)
        {
            return;
        }
        SynchronizeSource(document.Database.Source);
        SynchronizeQueries(document.Database.Queries);
        SynchronizeTables(document.Database.Tables);
        SynchronizeComponents(document.Database.Forms);
        SynchronizeComponents(document.Database.Reports);
        if (document.DatabaseState == null)
        {
            document.DatabaseState = new DatabaseDocumentState();
        }
        document.DatabaseState.Database = document.Database;
    }

    static OdfDatabaseDocument LoadDocument(
        OdfDocument document,
        OdfNode node)
    {
        OdfDatabaseDocument result = new OdfDatabaseDocument();
        result.Node = node;
        OdfNode? source = FindDirect(node, "data-source");
        if (source != null)
        {
            result.Source = LoadSource(document, source);
        }
        OdfNode? queries = FindDirect(node, "queries");
        result.Queries = LoadQueries(queries);
        result.Tables = LoadTables(node);
        OdfNode? forms = FindDirect(node, "forms");
        result.Forms = LoadComponents(document, forms,
            OdfDatabaseComponentKind.Form);
        OdfNode? reports = FindDirect(node, "reports");
        result.Reports = LoadComponents(document, reports,
            OdfDatabaseComponentKind.Report);
        return result;
    }

    static OdfDatabaseSource LoadSource(
        OdfDocument document,
        OdfNode node)
    {
        OdfDatabaseSource result = new OdfDatabaseSource();
        result.Node = node;
        OdfNode? connectionData = FindDirect(node, "connection-data");
        if (connectionData != null)
        {
            OdfNode? connection = FindConnection(connectionData);
            if (connection != null)
            {
                result.Connection = LoadConnection(document, connection);
            }
            OdfNode? login = FindDirect(connectionData, "login");
            if (login != null)
            {
                result.Login = LoadLogin(login);
            }
        }
        OdfNode? driver = FindDirect(node, "driver-settings");
        if (driver != null)
        {
            result.DriverSettings = LoadDriverSettings(driver);
        }
        OdfNode? application = FindDirect(
            node,
            "application-connection-settings");
        if (application != null)
        {
            result.ApplicationConnectionAttributes =
                application.Attributes;
            result.IncludedTableFilters = LoadFilterPatterns(
                application,
                "include-filter");
            result.ExcludedTableFilters = LoadFilterPatterns(
                application,
                "exclude-filter");
            result.TableTypeFilters = LoadTableTypes(application);
            result.Settings = LoadSettings(application);
        }
        return result;
    }

    static OdfNode? FindConnection(OdfNode connectionData)
    {
        OdfNode? resource = FindDirect(
            connectionData,
            "connection-resource");
        if (resource != null)
        {
            return resource;
        }
        OdfNode? description = FindDirect(
            connectionData,
            "database-description");
        if (description == null)
        {
            return null;
        }
        OdfNode? file = FindDirect(description, "file-based-database");
        if (file != null)
        {
            return file;
        }
        return FindDirect(description, "server-database");
    }

    static OdfDatabaseConnection LoadConnection(
        OdfDocument document,
        OdfNode node)
    {
        OdfDatabaseConnection result = new OdfDatabaseConnection();
        result.Node = node;
        string localName = node.Name == null ? "" : node.Name.LocalName;
        if (localName == "connection-resource")
        {
            result.Kind = OdfDatabaseConnectionKind.Resource;
        }
        else if (localName == "file-based-database")
        {
            result.Kind = OdfDatabaseConnectionKind.File;
        }
        else if (localName == "server-database")
        {
            result.Kind = OdfDatabaseConnectionKind.Server;
        }
        result.Href = Read(node, OdfNamespaces.XLink, "href");
        result.LinkType = Read(node, OdfNamespaces.XLink, "type");
        result.Show = Read(node, OdfNamespaces.XLink, "show");
        result.Actuate = Read(node, OdfNamespaces.XLink, "actuate");
        result.MediaType = Read(node, OdfNamespaces.Database, "media-type");
        result.Extension = Read(node, OdfNamespaces.Database, "extension");
        result.DatabaseType = Read(node, OdfNamespaces.Database, "type");
        result.HostName = Read(node, OdfNamespaces.Database, "hostname");
        result.Port = Read(node, OdfNamespaces.Database, "port");
        result.LocalSocket = Read(node, OdfNamespaces.Database, "local-socket");
        result.DatabaseName = Read(node, OdfNamespaces.Database, "database-name");
        ResolveReference(document, node.Location.PartPath, result);
        CaptureConnection(result);
        return result;
    }

    static OdfDatabaseLogin LoadLogin(OdfNode node)
    {
        OdfDatabaseLogin result = new OdfDatabaseLogin();
        result.Node = node;
        result.UserName = Read(node, OdfNamespaces.Database, "user-name");
        result.UseSystemUser = Read(
            node,
            OdfNamespaces.Database,
            "use-system-user");
        result.IsPasswordRequired = Read(
            node,
            OdfNamespaces.Database,
            "is-password-required");
        result.LoginTimeout = Read(
            node,
            OdfNamespaces.Database,
            "login-timeout");
        CaptureLogin(result);
        return result;
    }

    static OdfDatabaseDriverSettings LoadDriverSettings(OdfNode node)
    {
        OdfDatabaseDriverSettings result =
            new OdfDatabaseDriverSettings();
        result.Node = node;
        result.ShowDeleted = Read(node, OdfNamespaces.Database, "show-deleted");
        result.SystemDriverSettings = Read(
            node,
            OdfNamespaces.Database,
            "system-driver-settings");
        result.BaseDn = Read(node, OdfNamespaces.Database, "base-dn");
        result.IsFirstRowHeaderLine = Read(
            node,
            OdfNamespaces.Database,
            "is-first-row-header-line");
        result.ParameterNameSubstitution = Read(
            node,
            OdfNamespaces.Database,
            "parameter-name-substitution");
        OdfNode? increment = FindDirect(node, "auto-increment");
        if (increment != null)
        {
            result.AdditionalColumnStatement = Read(
                increment,
                OdfNamespaces.Database,
                "additional-column-statement");
            result.RowRetrievingStatement = Read(
                increment,
                OdfNamespaces.Database,
                "row-retrieving-statement");
        }
        OdfNode? delimiter = FindDirect(node, "delimiter");
        if (delimiter != null)
        {
            result.FieldDelimiter = Read(
                delimiter,
                OdfNamespaces.Database,
                "field-delimiter");
            result.StringDelimiter = Read(
                delimiter,
                OdfNamespaces.Database,
                "string-delimiter");
            result.DecimalDelimiter = Read(
                delimiter,
                OdfNamespaces.Database,
                "decimal-delimiter");
            result.ThousandsDelimiter = Read(
                delimiter,
                OdfNamespaces.Database,
                "thousand-delimiter");
        }
        OdfNode? characterSet = FindDirect(node, "character-set");
        if (characterSet != null)
        {
            result.Encoding = Read(
                characterSet,
                OdfNamespaces.Database,
                "encoding");
        }
        CaptureDriver(result);
        return result;
    }

    static OdfDatabaseSetting[] LoadSettings(OdfNode application)
    {
        List<OdfDatabaseSetting> result = new List<OdfDatabaseSetting>();
        OdfNode? settings = FindDirect(application, "data-source-settings");
        if (settings == null)
        {
            return result.ToArray();
        }
        int index = 0;
        while (index < settings.Children.Length)
        {
            OdfNode node = settings.Children[index];
            if (IsElement(node, "data-source-setting"))
            {
                OdfDatabaseSetting setting = new OdfDatabaseSetting();
                setting.Node = node;
                setting.Name = Read(node, OdfNamespaces.Database, "name");
                setting.Type = Read(node, OdfNamespaces.Database, "type");
                setting.IsList = Read(node, OdfNamespaces.Database, "is-list");
                List<string> values = new List<string>();
                int valueIndex = 0;
                while (valueIndex < node.Children.Length)
                {
                    OdfNode child = node.Children[valueIndex];
                    if (IsElement(child, "data-source-setting-value"))
                    {
                        values.Add(TextContent(child));
                    }
                    valueIndex = valueIndex + 1;
                }
                setting.Values = values.ToArray();
                CaptureSetting(setting);
                result.Add(setting);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfDatabaseQuery[] LoadQueries(OdfNode? parent)
    {
        List<OdfDatabaseQuery> result = new List<OdfDatabaseQuery>();
        if (parent == null)
        {
            return result.ToArray();
        }
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode node = parent.Children[index];
            if (IsElement(node, "query")
                || IsElement(node, "query-collection"))
            {
                OdfDatabaseQuery query = new OdfDatabaseQuery();
                query.IsCollection = IsElement(node, "query-collection");
                query.Node = node;
                query.Name = Read(node, OdfNamespaces.Database, "name");
                query.Title = Read(node, OdfNamespaces.Database, "title");
                query.Description = Read(
                    node,
                    OdfNamespaces.Database,
                    "description");
                query.Command = Read(node, OdfNamespaces.Database, "command");
                query.EscapeProcessing = Read(
                    node,
                    OdfNamespaces.Database,
                    "escape-processing");
                query.OrderStatement = Statement(node, "order-statement");
                query.FilterStatement = Statement(node, "filter-statement");
                query.Children = LoadQueries(node);
                CaptureQuery(query);
                result.Add(query);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfDatabaseTable[] LoadTables(OdfNode database)
    {
        List<OdfDatabaseTable> result = new List<OdfDatabaseTable>();
        OdfNode? representations = FindDirect(
            database,
            "table-representations");
        LoadTableChildren(representations, false, result);
        OdfNode? schema = FindDirect(database, "schema-definition");
        OdfNode? definitions = schema == null
            ? null
            : FindDirect(schema, "table-definitions");
        LoadTableChildren(definitions, true, result);
        return result.ToArray();
    }

    static void LoadTableChildren(
        OdfNode? parent,
        bool definition,
        List<OdfDatabaseTable> result)
    {
        if (parent == null)
        {
            return;
        }
        string expected = definition
            ? "table-definition"
            : "table-representation";
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode node = parent.Children[index];
            if (IsElement(node, expected))
            {
                OdfDatabaseTable table = new OdfDatabaseTable();
                table.IsDefinition = definition;
                table.Node = node;
                table.Name = Read(node, OdfNamespaces.Database, "name");
                table.CatalogName = Read(
                    node,
                    OdfNamespaces.Database,
                    "catalog-name");
                table.SchemaName = Read(
                    node,
                    OdfNamespaces.Database,
                    "schema-name");
                table.Type = Read(node, OdfNamespaces.Database, "type");
                table.Title = Read(node, OdfNamespaces.Database, "title");
                table.Description = Read(
                    node,
                    OdfNamespaces.Database,
                    "description");
                table.OrderStatement = Statement(node, "order-statement");
                table.FilterStatement = Statement(node, "filter-statement");
                table.Columns = FindDescendants(node, "column", "column-definition");
                table.Keys = FindDescendants(node, "key", "");
                table.Indices = FindDescendants(node, "index", "");
                CaptureTable(table);
                result.Add(table);
            }
            index = index + 1;
        }
    }

    static OdfDatabaseComponent[] LoadComponents(
        OdfDocument document,
        OdfNode? parent,
        OdfDatabaseComponentKind leafKind)
    {
        List<OdfDatabaseComponent> result =
            new List<OdfDatabaseComponent>();
        if (parent == null)
        {
            return result.ToArray();
        }
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode node = parent.Children[index];
            if (IsElement(node, "component")
                || IsElement(node, "component-collection"))
            {
                OdfDatabaseComponent component =
                    new OdfDatabaseComponent();
                component.Kind = IsElement(node, "component-collection")
                    ? OdfDatabaseComponentKind.Collection
                    : leafKind;
                component.Node = node;
                component.Name = Read(
                    node,
                    OdfNamespaces.Database,
                    "name");
                component.Title = Read(
                    node,
                    OdfNamespaces.Database,
                    "title");
                component.Description = Read(
                    node,
                    OdfNamespaces.Database,
                    "description");
                component.Href = Read(node, OdfNamespaces.XLink, "href");
                component.AsTemplate = Read(
                    node,
                    OdfNamespaces.Database,
                    "as-template");
                component.InlineDocument = FindOfficeDocument(node);
                component.Children = LoadComponents(
                    document,
                    node,
                    leafKind);
                ResolveComponentReference(document, component);
                CaptureComponent(component);
                result.Add(component);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static string[] LoadFilterPatterns(OdfNode application, string kind)
    {
        List<string> result = new List<string>();
        OdfNode? filter = FindDirect(application, "table-filter");
        if (filter == null)
        {
            return result.ToArray();
        }
        int index = 0;
        while (index < filter.Children.Length)
        {
            OdfNode node = filter.Children[index];
            if (IsElement(node, kind))
            {
                result.Add(Read(node, OdfNamespaces.Database, "pattern"));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static string[] LoadTableTypes(OdfNode application)
    {
        List<string> result = new List<string>();
        OdfNode? filter = FindDirect(application, "table-type-filter");
        if (filter == null)
        {
            return result.ToArray();
        }
        int index = 0;
        while (index < filter.Children.Length)
        {
            OdfNode node = filter.Children[index];
            if (IsElement(node, "table-type"))
            {
                result.Add(TextContent(node));
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static void SynchronizeSource(OdfDatabaseSource source)
    {
        if (source.Connection != null)
        {
            SynchronizeConnection(source.Connection);
        }
        if (source.Login != null)
        {
            SynchronizeLogin(source.Login);
        }
        if (source.DriverSettings != null)
        {
            SynchronizeDriver(source.DriverSettings);
        }
        SynchronizeSettings(source.Settings);
    }

    static void SynchronizeConnection(OdfDatabaseConnection connection)
    {
        OdfNode? node = connection.Node;
        if (node == null)
        {
            return;
        }
        WriteChanged(node, OdfNamespaces.XLink, "href",
            OdfNamespaces.XLinkPrefix, connection.Href,
            connection.OriginalHref);
        WriteChanged(node, OdfNamespaces.XLink, "type",
            OdfNamespaces.XLinkPrefix, connection.LinkType,
            connection.OriginalLinkType);
        WriteChanged(node, OdfNamespaces.XLink, "show",
            OdfNamespaces.XLinkPrefix, connection.Show,
            connection.OriginalShow);
        WriteChanged(node, OdfNamespaces.XLink, "actuate",
            OdfNamespaces.XLinkPrefix, connection.Actuate,
            connection.OriginalActuate);
        WriteDbChanged(node, "media-type", connection.MediaType,
            connection.OriginalMediaType);
        WriteDbChanged(node, "extension", connection.Extension,
            connection.OriginalExtension);
        WriteDbChanged(node, "type", connection.DatabaseType,
            connection.OriginalDatabaseType);
        WriteDbChanged(node, "hostname", connection.HostName,
            connection.OriginalHostName);
        WriteDbChanged(node, "port", connection.Port,
            connection.OriginalPort);
        WriteDbChanged(node, "local-socket", connection.LocalSocket,
            connection.OriginalLocalSocket);
        WriteDbChanged(node, "database-name", connection.DatabaseName,
            connection.OriginalDatabaseName);
        CaptureConnection(connection);
    }

    static void SynchronizeLogin(OdfDatabaseLogin login)
    {
        if (login.Node == null)
        {
            return;
        }
        WriteDbChanged(login.Node, "user-name", login.UserName,
            login.OriginalUserName);
        WriteDbChanged(login.Node, "use-system-user", login.UseSystemUser,
            login.OriginalUseSystemUser);
        WriteDbChanged(login.Node, "is-password-required",
            login.IsPasswordRequired, login.OriginalIsPasswordRequired);
        WriteDbChanged(login.Node, "login-timeout", login.LoginTimeout,
            login.OriginalLoginTimeout);
        CaptureLogin(login);
    }

    static void SynchronizeDriver(OdfDatabaseDriverSettings driver)
    {
        if (driver.Node == null)
        {
            return;
        }
        WriteDbChanged(driver.Node, "show-deleted", driver.ShowDeleted,
            driver.OriginalShowDeleted);
        WriteDbChanged(driver.Node, "system-driver-settings",
            driver.SystemDriverSettings, driver.OriginalSystemDriverSettings);
        WriteDbChanged(driver.Node, "base-dn", driver.BaseDn,
            driver.OriginalBaseDn);
        WriteDbChanged(driver.Node, "is-first-row-header-line",
            driver.IsFirstRowHeaderLine,
            driver.OriginalIsFirstRowHeaderLine);
        WriteDbChanged(driver.Node, "parameter-name-substitution",
            driver.ParameterNameSubstitution,
            driver.OriginalParameterNameSubstitution);
        OdfNode? increment = FindDirect(driver.Node, "auto-increment");
        if (increment != null)
        {
            WriteDbChanged(increment, "additional-column-statement",
                driver.AdditionalColumnStatement,
                driver.OriginalAdditionalColumnStatement);
            WriteDbChanged(increment, "row-retrieving-statement",
                driver.RowRetrievingStatement,
                driver.OriginalRowRetrievingStatement);
        }
        OdfNode? delimiter = FindDirect(driver.Node, "delimiter");
        if (delimiter != null)
        {
            WriteDbChanged(delimiter, "field-delimiter",
                driver.FieldDelimiter, driver.OriginalFieldDelimiter);
            WriteDbChanged(delimiter, "string-delimiter",
                driver.StringDelimiter, driver.OriginalStringDelimiter);
            WriteDbChanged(delimiter, "decimal-delimiter",
                driver.DecimalDelimiter, driver.OriginalDecimalDelimiter);
            WriteDbChanged(delimiter, "thousand-delimiter",
                driver.ThousandsDelimiter,
                driver.OriginalThousandsDelimiter);
        }
        OdfNode? characterSet = FindDirect(driver.Node, "character-set");
        if (characterSet != null)
        {
            WriteDbChanged(characterSet, "encoding", driver.Encoding,
                driver.OriginalEncoding);
        }
        CaptureDriver(driver);
    }

    static void SynchronizeSettings(OdfDatabaseSetting[] settings)
    {
        int index = 0;
        while (index < settings.Length)
        {
            OdfDatabaseSetting setting = settings[index];
            if (setting.Node != null)
            {
                WriteDbChanged(setting.Node, "name", setting.Name,
                    setting.OriginalName);
                WriteDbChanged(setting.Node, "type", setting.Type,
                    setting.OriginalType);
                WriteDbChanged(setting.Node, "is-list", setting.IsList,
                    setting.OriginalIsList);
                SynchronizeSettingValues(setting);
                CaptureSetting(setting);
            }
            index = index + 1;
        }
    }

    static void SynchronizeSettingValues(OdfDatabaseSetting setting)
    {
        if (setting.Node == null
            || SameStrings(setting.Values, setting.OriginalValues))
        {
            return;
        }
        List<OdfNode> valueNodes = new List<OdfNode>();
        int index = 0;
        while (index < setting.Node.Children.Length)
        {
            OdfNode node = setting.Node.Children[index];
            if (IsElement(node, "data-source-setting-value"))
            {
                valueNodes.Add(node);
            }
            index = index + 1;
        }
        index = 0;
        while (index < setting.Values.Length && index < valueNodes.Count)
        {
            SetTextContent(valueNodes[index], setting.Values[index]);
            index = index + 1;
        }
    }

    static void SynchronizeQueries(OdfDatabaseQuery[] queries)
    {
        int index = 0;
        while (index < queries.Length)
        {
            OdfDatabaseQuery query = queries[index];
            if (query.Node != null)
            {
                WriteDbChanged(query.Node, "name", query.Name,
                    query.OriginalName);
                WriteDbChanged(query.Node, "title", query.Title,
                    query.OriginalTitle);
                WriteDbChanged(query.Node, "description", query.Description,
                    query.OriginalDescription);
                WriteDbChanged(query.Node, "command", query.Command,
                    query.OriginalCommand);
                WriteDbChanged(query.Node, "escape-processing",
                    query.EscapeProcessing, query.OriginalEscapeProcessing);
                SynchronizeStatement(query.Node, "order-statement",
                    query.OrderStatement, query.OriginalOrderStatement);
                SynchronizeStatement(query.Node, "filter-statement",
                    query.FilterStatement, query.OriginalFilterStatement);
                CaptureQuery(query);
            }
            SynchronizeQueries(query.Children);
            index = index + 1;
        }
    }

    static void SynchronizeTables(OdfDatabaseTable[] tables)
    {
        int index = 0;
        while (index < tables.Length)
        {
            OdfDatabaseTable table = tables[index];
            if (table.Node != null)
            {
                WriteDbChanged(table.Node, "name", table.Name,
                    table.OriginalName);
                WriteDbChanged(table.Node, "catalog-name", table.CatalogName,
                    table.OriginalCatalogName);
                WriteDbChanged(table.Node, "schema-name", table.SchemaName,
                    table.OriginalSchemaName);
                WriteDbChanged(table.Node, "type", table.Type,
                    table.OriginalType);
                WriteDbChanged(table.Node, "title", table.Title,
                    table.OriginalTitle);
                WriteDbChanged(table.Node, "description", table.Description,
                    table.OriginalDescription);
                SynchronizeStatement(table.Node, "order-statement",
                    table.OrderStatement, table.OriginalOrderStatement);
                SynchronizeStatement(table.Node, "filter-statement",
                    table.FilterStatement, table.OriginalFilterStatement);
                CaptureTable(table);
            }
            index = index + 1;
        }
    }

    static void SynchronizeComponents(OdfDatabaseComponent[] components)
    {
        int index = 0;
        while (index < components.Length)
        {
            OdfDatabaseComponent component = components[index];
            if (component.Node != null)
            {
                WriteDbChanged(component.Node, "name", component.Name,
                    component.OriginalName);
                WriteDbChanged(component.Node, "title", component.Title,
                    component.OriginalTitle);
                WriteDbChanged(component.Node, "description",
                    component.Description, component.OriginalDescription);
                WriteChanged(component.Node, OdfNamespaces.XLink, "href",
                    OdfNamespaces.XLinkPrefix, component.Href,
                    component.OriginalHref);
                WriteDbChanged(component.Node, "as-template",
                    component.AsTemplate, component.OriginalAsTemplate);
                CaptureComponent(component);
            }
            SynchronizeComponents(component.Children);
            index = index + 1;
        }
    }

    static void SynchronizeStatement(
        OdfNode parent,
        string localName,
        string value,
        string original)
    {
        if (value == original)
        {
            return;
        }
        OdfNode? node = FindDirect(parent, localName);
        if (node != null)
        {
            WriteOptional(node, OdfNamespaces.Database, "command",
                OdfNamespaces.DatabasePrefix, value);
        }
    }

    static string Statement(OdfNode parent, string localName)
    {
        OdfNode? node = FindDirect(parent, localName);
        return node == null
            ? ""
            : Read(node, OdfNamespaces.Database, "command");
    }

    static OdfNode[] FindDescendants(
        OdfNode parent,
        string firstName,
        string secondName)
    {
        List<OdfNode> result = new List<OdfNode>();
        FindDescendantsCore(parent, firstName, secondName, result);
        return result.ToArray();
    }

    static void FindDescendantsCore(
        OdfNode parent,
        string firstName,
        string secondName,
        List<OdfNode> result)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode node = parent.Children[index];
            if (IsElement(node, firstName)
                || (secondName.Length > 0 && IsElement(node, secondName)))
            {
                result.Add(node);
            }
            FindDescendantsCore(node, firstName, secondName, result);
            index = index + 1;
        }
    }

    static OdfNode? FindOfficeDocument(OdfNode parent)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode node = parent.Children[index];
            if (node.Kind == OdfNodeKind.Element
                && node.Name != null
                && node.Name.NamespaceName == OdfNamespaces.Office
                && node.Name.LocalName == "document")
            {
                return node;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDatabase(OdfDocument document)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfNode? body = document.Parts[index].Body;
            if (body != null)
            {
                OdfNode? result = FindDirectOffice(body, "database");
                if (result != null)
                {
                    return result;
                }
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectOffice(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode node = parent.Children[index];
            if (node.Kind == OdfNodeKind.Element
                && node.Name != null
                && node.Name.NamespaceName == OdfNamespaces.Office
                && node.Name.LocalName == localName)
            {
                return node;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirect(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (IsElement(parent.Children[index], localName))
            {
                return parent.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Database
            && node.Name.LocalName == localName;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static string TextContent(OdfNode node)
    {
        string result = "";
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind == OdfNodeKind.Text)
            {
                result = result + node.Children[index].Value;
            }
            index = index + 1;
        }
        return result;
    }

    static void SetTextContent(OdfNode node, string value)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (node.Children[index].Kind == OdfNodeKind.Text)
            {
                node.Children[index].Value = value;
                return;
            }
            index = index + 1;
        }
    }

    static void ResolveReference(
        OdfDocument document,
        string partPath,
        OdfDatabaseConnection connection)
    {
        connection.IsExternal = IsExternal(document, connection.Href);
        connection.PackagePath = "";
        connection.PackageEntryExists = false;
        if (connection.Href.Length == 0 || connection.IsExternal)
        {
            return;
        }
        connection.PackagePath = ResolvePath(partPath, connection.Href);
        connection.PackageEntryExists = EntryExists(
            document,
            connection.PackagePath);
    }

    static void ResolveComponentReference(
        OdfDocument document,
        OdfDatabaseComponent component)
    {
        component.IsExternal = IsExternal(document, component.Href);
        component.PackagePath = "";
        component.PackageEntryExists = false;
        if (component.Href.Length == 0 || component.IsExternal)
        {
            return;
        }
        string partPath = component.Node == null
            ? ""
            : component.Node.Location.PartPath;
        component.PackagePath = ResolvePath(partPath, component.Href);
        component.PackageEntryExists = EntryExists(
            document,
            component.PackagePath);
    }

    static bool IsExternal(OdfDocument document, string href)
    {
        if (href.Length == 0)
        {
            return false;
        }
        if (OdfIriResolver.IsAbsoluteIri(href)
            || href.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }
        return document.PackageKind == OdfPackageKind.Flat
            && !href.StartsWith("#", StringComparison.Ordinal);
    }

    static string ResolvePath(string partPath, string href)
    {
        if (!OdfIriResolver.IsPackageRelativeReference(href))
        {
            return "";
        }
        int end = href.Length;
        int query = href.IndexOf('?');
        if (query >= 0 && query < end)
        {
            end = query;
        }
        int fragment = href.IndexOf('#');
        if (fragment >= 0 && fragment < end)
        {
            end = fragment;
        }
        string path = href.Substring(0, end);
        if (path.Length == 0)
        {
            return partPath;
        }
        return PackagePathRules.ResolveRelativeReference(partPath, path);
    }

    static bool EntryExists(OdfDocument document, string path)
    {
        int index = 0;
        while (index < document.PackageEntries.Length)
        {
            OdfPackageEntry entry = document.PackageEntries[index];
            if (entry.Path == path || entry.CanonicalPath == path)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void WriteDbChanged(
        OdfNode node,
        string localName,
        string value,
        string original)
    {
        WriteChanged(node, OdfNamespaces.Database, localName,
            OdfNamespaces.DatabasePrefix, value, original);
    }

    static void WriteChanged(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original)
    {
        if (value != original)
        {
            WriteOptional(node, namespaceName, localName, prefix, value);
        }
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static bool SameStrings(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static string[] CopyStrings(string[] values)
    {
        string[] result = new string[values.Length];
        global::System.Array.Copy(values, result, values.Length);
        return result;
    }

    static void CaptureConnection(OdfDatabaseConnection value)
    {
        value.OriginalHref = value.Href;
        value.OriginalLinkType = value.LinkType;
        value.OriginalShow = value.Show;
        value.OriginalActuate = value.Actuate;
        value.OriginalMediaType = value.MediaType;
        value.OriginalExtension = value.Extension;
        value.OriginalDatabaseType = value.DatabaseType;
        value.OriginalHostName = value.HostName;
        value.OriginalPort = value.Port;
        value.OriginalLocalSocket = value.LocalSocket;
        value.OriginalDatabaseName = value.DatabaseName;
    }

    static void CaptureLogin(OdfDatabaseLogin value)
    {
        value.OriginalUserName = value.UserName;
        value.OriginalUseSystemUser = value.UseSystemUser;
        value.OriginalIsPasswordRequired = value.IsPasswordRequired;
        value.OriginalLoginTimeout = value.LoginTimeout;
    }

    static void CaptureDriver(OdfDatabaseDriverSettings value)
    {
        value.OriginalShowDeleted = value.ShowDeleted;
        value.OriginalSystemDriverSettings = value.SystemDriverSettings;
        value.OriginalBaseDn = value.BaseDn;
        value.OriginalIsFirstRowHeaderLine = value.IsFirstRowHeaderLine;
        value.OriginalParameterNameSubstitution =
            value.ParameterNameSubstitution;
        value.OriginalAdditionalColumnStatement =
            value.AdditionalColumnStatement;
        value.OriginalRowRetrievingStatement = value.RowRetrievingStatement;
        value.OriginalFieldDelimiter = value.FieldDelimiter;
        value.OriginalStringDelimiter = value.StringDelimiter;
        value.OriginalDecimalDelimiter = value.DecimalDelimiter;
        value.OriginalThousandsDelimiter = value.ThousandsDelimiter;
        value.OriginalEncoding = value.Encoding;
    }

    static void CaptureSetting(OdfDatabaseSetting value)
    {
        value.OriginalName = value.Name;
        value.OriginalType = value.Type;
        value.OriginalIsList = value.IsList;
        value.OriginalValues = CopyStrings(value.Values);
    }

    static void CaptureQuery(OdfDatabaseQuery value)
    {
        value.OriginalName = value.Name;
        value.OriginalTitle = value.Title;
        value.OriginalDescription = value.Description;
        value.OriginalCommand = value.Command;
        value.OriginalEscapeProcessing = value.EscapeProcessing;
        value.OriginalOrderStatement = value.OrderStatement;
        value.OriginalFilterStatement = value.FilterStatement;
    }

    static void CaptureTable(OdfDatabaseTable value)
    {
        value.OriginalName = value.Name;
        value.OriginalCatalogName = value.CatalogName;
        value.OriginalSchemaName = value.SchemaName;
        value.OriginalType = value.Type;
        value.OriginalTitle = value.Title;
        value.OriginalDescription = value.Description;
        value.OriginalOrderStatement = value.OrderStatement;
        value.OriginalFilterStatement = value.FilterStatement;
    }

    static void CaptureComponent(OdfDatabaseComponent value)
    {
        value.OriginalName = value.Name;
        value.OriginalTitle = value.Title;
        value.OriginalDescription = value.Description;
        value.OriginalHref = value.Href;
        value.OriginalAsTemplate = value.AsTemplate;
    }
}
