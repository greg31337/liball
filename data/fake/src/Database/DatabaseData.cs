namespace fake;

internal static class DatabaseData
{
    internal const string Source = "Project-authored conventional database metadata fixtures";
    internal const string License = "project-license";

    internal static readonly string[] DatabaseNames = new string[]
    {
        "analytics", "application", "catalog", "example", "reporting",
        "warehouse"
    };

    internal static readonly string[] SchemaNames = new string[]
    {
        "app", "audit", "core", "public", "reporting", "staging"
    };

    internal static readonly string[] TableNames = new string[]
    {
        "accounts", "audit_events", "customers", "inventory_items",
        "order_lines", "orders", "products", "users"
    };

    internal static readonly string[] ColumnNames = new string[]
    {
        "created_at", "customer_id", "display_name", "id", "is_active",
        "order_id", "quantity", "status", "updated_at", "user_id"
    };

    internal static readonly string[] SqlTypes = new string[]
    {
        "BIGINT", "BOOLEAN", "DATE", "DECIMAL(18,2)", "INTEGER", "JSON",
        "TEXT", "TIMESTAMP", "UUID", "VARCHAR(255)"
    };

    internal static readonly string[] Collations = new string[]
    {
        "BINARY", "Latin1_General_100_CI_AS", "NOCASE", "en_US.utf8",
        "utf8mb4_unicode_ci"
    };

    internal static readonly string[] Engines = new string[]
    {
        "MariaDB", "Microsoft SQL Server", "MySQL", "Oracle Database",
        "PostgreSQL", "SQLite"
    };

    internal static readonly string[] ReservedWords = new string[]
    {
        "alter", "constraint", "create", "delete", "drop", "foreign",
        "from", "group", "index", "insert", "join", "null", "order",
        "primary", "references", "select", "table", "update", "user",
        "where"
    };
}
