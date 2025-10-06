namespace SerGenX;

public sealed class MsDbExplorer(string connStr) : DbExplorerBase(connStr)
{
    public override Task<List<(string Schema, string Table)>> ListDistinctTablesAsync()
    {
        // TODO: MSSQL 實作
        return Task.FromResult(new List<(string, string)>());
    }

    public override Task<TableTranslation?> GetTableTranslationAsync(string schema, string table)
    {
        // TODO: MSSQL 實作
        return Task.FromResult<TableTranslation?>(null);
    }

    public override Task<List<ColumnTranslation>> GetColumnTranslationsAsync(string schema, string table)
    {
        // TODO: MSSQL 實作
        return Task.FromResult(new List<ColumnTranslation>());
    }

    public override Task<TableDefinition> GetTableDefinitionAsync(string schema, string table)
    {
        // TODO: MSSQL 實作
        return Task.FromResult(new TableDefinition(schema, table, new(), new(), new()));
    }
}