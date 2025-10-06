namespace SerGenX;

public static class DbExplorerFactory
{
    public static IDbExplorer Create(DbKind kind, string connectionString) => kind switch
    {
        DbKind.Pgsql => new PgDbExplorer(connectionString),
        DbKind.Mssql => new MsDbExplorer(connectionString),
        DbKind.Sqlite => new SqliteDbExplorer(connectionString),
        _ => throw new NotSupportedException("Unsupported DB kind")
    };
}