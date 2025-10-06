using Npgsql;

namespace SerGenX;

public sealed class PgDbExplorer(string connectionString) : DbExplorerBase(connectionString)
{
    public override async Task<List<(string Schema, string Table)>> ListDistinctTablesAsync()
    {
        const string sql = """
                           select distinct 綱要名稱, 原始表名
                           from core."表格翻譯對照表"
                           order by 綱要名稱, 原始表名;
                           """;

        var tables = new List<(string, string)>();
        await using var connection = new NpgsqlConnection(ConnStr);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add((reader.GetString(0), reader.GetString(1)));
        }

        return tables;
    }

    public override async Task<TableTranslation?> GetTableTranslationAsync(string schema, string table)
    {
        const string sql = """
                           select 綱要名稱, 原始表名, 翻譯後表名, 類別名稱, 模組名稱
                           from core."表格翻譯對照表"
                           where 綱要名稱 = @schema and 原始表名 = @table
                           limit 1;
                           """;

        await using var connection = new NpgsqlConnection(ConnStr);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TableTranslation(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)
            );
        }

        return null;
    }

    public override async Task<List<ColumnTranslation>> GetColumnTranslationsAsync(string schema, string table)
    {
        const string sql = """
                           select 綱要名稱, 表格名稱, 原始欄位名, 翻譯後欄位名, 屬性名稱
                           from core."表格欄位翻譯對照表"
                           where 綱要名稱 = @schema and 表格名稱 = @table
                           order by 原始欄位名;
                           """;

        var columnTranslations = new List<ColumnTranslation>();
        await using var connection = new NpgsqlConnection(ConnStr);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnTranslations.Add(new ColumnTranslation(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)
            ));
        }

        return columnTranslations;
    }

    public override async Task<TableDefinition> GetTableDefinitionAsync(string schema, string table)
    {
        const string columnsSql = """
                                  select c.column_name,
                                         c.data_type,
                                         (c.is_nullable = 'YES') as is_nullable,
                                         c.character_maximum_length,
                                         c.numeric_precision,
                                         c.numeric_scale
                                  from information_schema.columns c
                                  where c.table_schema = @schema and c.table_name = @table
                                  order by c.ordinal_position;
                                  """;

        const string primaryKeysSql = """
                                      select kcu.column_name
                                      from information_schema.table_constraints tc
                                      join information_schema.key_column_usage kcu
                                        on kcu.constraint_name = tc.constraint_name
                                       and kcu.table_schema = tc.table_schema
                                       and kcu.table_name = tc.table_name
                                      where tc.table_schema = @schema
                                        and tc.table_name = @table
                                        and tc.constraint_type = 'PRIMARY KEY'
                                      order by kcu.ordinal_position;
                                      """;

        // 使用 attidentity 或 pg_get_expr(adbin, adrelid) 判斷 identity / serial
        const string identityColumnsSql = """
                                          select a.attname
                                          from pg_catalog.pg_class t
                                          join pg_catalog.pg_namespace n on n.oid = t.relnamespace
                                          join pg_catalog.pg_attribute a on a.attrelid = t.oid
                                          left join pg_catalog.pg_attrdef d on d.adrelid = a.attrelid and d.adnum = a.attnum
                                          where n.nspname = @schema and t.relname = @table
                                            and a.attnum > 0 and not a.attisdropped
                                            and (
                                                 a.attidentity in ('a','d')
                                                 or (d.adbin is not null and pg_get_expr(d.adbin, d.adrelid) like 'nextval(%')
                                            );
                                          """;

        var columns = new List<ColumnDefinition>();
        var primaryKeys = new List<string>();
        var identityColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = new NpgsqlConnection(ConnStr);
        await connection.OpenAsync();

        // columns
        await using (var command = new NpgsqlCommand(columnsSql, connection))
        {
            command.Parameters.AddWithValue("schema", schema);
            command.Parameters.AddWithValue("table", table);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnDefinition(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetBoolean(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : Convert.ToInt32(reader.GetValue(4)),
                    reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5))
                ));
            }
        }

        // primary keys
        await using (var command = new NpgsqlCommand(primaryKeysSql, connection))
        {
            command.Parameters.AddWithValue("schema", schema);
            command.Parameters.AddWithValue("table", table);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                primaryKeys.Add(reader.GetString(0));
            }
        }

        // identity / serial
        await using (var command = new NpgsqlCommand(identityColumnsSql, connection))
        {
            command.Parameters.AddWithValue("schema", schema);
            command.Parameters.AddWithValue("table", table);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                identityColumns.Add(reader.GetString(0));
            }
        }

        return new TableDefinition(schema, table, columns, primaryKeys, identityColumns);
    }
}