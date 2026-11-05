namespace sql;

internal class SqlDialect : ISqlDialect
{
    internal SqlDialectKind Kind;

    internal SqlDialect(SqlDialectKind kind)
    {
        Kind = SqlDialectCatalog.NormalizeKind(kind);
    }

    public SqlDialectKind GetKind()
    {
        return Kind;
    }

    public string GetName()
    {
        return SqlDialectCatalog.GetNameCore(Kind);
    }

    public SqlDialectOptions CreateOptions()
    {
        return SqlDialectCatalog.CreateOptionsCore(Kind);
    }

    public ISqlDialectPolicy CreatePolicy()
    {
        return new SqlDialectPolicy(CreateOptions());
    }
}
