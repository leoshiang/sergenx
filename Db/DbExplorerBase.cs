namespace SerGenX;

public abstract class DbExplorerBase(string connStr) : IDbExplorer
{
    protected readonly string ConnStr = connstrGuard(connStr);

    public abstract Task<List<(string Schema, string Table)>> ListDistinctTablesAsync();
    public abstract Task<TableTranslation?> GetTableTranslationAsync(string schema, string table);
    public abstract Task<List<ColumnTranslation>> GetColumnTranslationsAsync(string schema, string table);
    public abstract Task<TableDefinition> GetTableDefinitionAsync(string schema, string table);

    static string connstrGuard(string s) =>
        string.IsNullOrWhiteSpace(s) ? throw new ArgumentException("connectionString") : s;
}