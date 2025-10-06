namespace SerGenX;

public static class DbTypeDetector
{
    public static DbKind Detect(string? cliDbType, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(cliDbType))
        {
            return cliDbType.ToLowerInvariant() switch
            {
                "pgsql" => DbKind.Pgsql,
                "mssql" => DbKind.Mssql,
                "sqlite" => DbKind.Sqlite,
                _ => DbKind.Unknown
            };
        }

        var lowerConnectionString = connectionString.ToLowerInvariant();
        if (lowerConnectionString.Contains("host=") ||
            lowerConnectionString.Contains("username=") ||
            lowerConnectionString.StartsWith("postgresql://") ||
            lowerConnectionString.Contains("postgres"))
            return DbKind.Pgsql;

        if (lowerConnectionString.Contains("initial catalog=") ||
            (lowerConnectionString.Contains("data source=") && !lowerConnectionString.Contains(".sqlite")) ||
            lowerConnectionString.Contains("server="))
            return DbKind.Mssql;

        if (lowerConnectionString.Contains(".db") ||
            (lowerConnectionString.Contains("data source=") && lowerConnectionString.Contains(".sqlite")))
            return DbKind.Sqlite;

        return DbKind.Unknown;
    }
}