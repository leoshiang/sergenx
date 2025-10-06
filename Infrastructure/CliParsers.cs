namespace SerGenX;

public static class CliParsers
{
    public static List<(string Schema, string Table)> ParseTablesArgument(string tablesArgument, DbKind databaseKind)
    {
        var parsedTables = new List<(string, string)>();
        foreach (var tableItem in tablesArgument.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = tableItem.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                var defaultSchema = databaseKind == DbKind.Sqlite ? "main" : "dbo";
                parsedTables.Add((defaultSchema, parts[0]));
            }
            else
            {
                parsedTables.Add((parts[0], parts[1]));
            }
        }

        return parsedTables;
    }
}