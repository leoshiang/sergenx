namespace SerGenX;

public interface IDbExplorer
{
    Task<List<(string Schema, string Table)>> ListDistinctTablesAsync();
    Task<TableTranslation?> GetTableTranslationAsync(string schema, string table);
    Task<List<ColumnTranslation>> GetColumnTranslationsAsync(string schema, string table);
    Task<TableDefinition> GetTableDefinitionAsync(string schema, string table);
}