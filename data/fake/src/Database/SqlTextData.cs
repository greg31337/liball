namespace fake;

internal static class SqlTextData
{
    internal const string Source = "Project-authored inert SQL and connection-string fixtures";
    internal const string License = "project-license";

    internal static readonly string[] ConnectionStrings = new string[]
    {
        "Host=example.invalid;Port=5432;Database=example;Username=example_user;Password=not-a-real-password;",
        "Server=example.invalid;Port=3306;Database=example;User ID=example_user;Password=not-a-real-password;",
        "Server=tcp:example.invalid,1433;Initial Catalog=example;User ID=example_user;Password=not-a-real-password;Encrypt=True;",
        "Data Source=:memory:;Mode=Memory;Cache=Shared;"
    };

    internal static readonly string[] InsertTables = new string[]
    {
        "accounts", "audit_events", "customers", "inventory_items",
        "order_lines", "orders", "products", "users"
    };

    internal static readonly string[] InsertColumns = new string[]
    {
        "status", "display_name", "display_name", "status", "quantity",
        "status", "display_name", "display_name"
    };

    internal static readonly string[] InsertValues = new string[]
    {
        "active", "O'Brien", "Example customer", "in_stock", "4",
        "pending", "Sample product", "Quoted 'example'"
    };
}
