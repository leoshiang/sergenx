namespace SerGenX;

public static class TypeMapper
{
    public static string MapDbTypeToCSharp(string dbType, bool isNullable, DbKind dbKind, bool useDateTimeOffset)
    {
        var normalizedDbType = dbType.ToLowerInvariant();
        var csharpType = "string";

        switch (dbKind)
        {
            case DbKind.Pgsql when normalizedDbType is "smallint" or "int2":
                csharpType = "short";
                break;
            case DbKind.Pgsql when normalizedDbType is "integer" or "int4":
                csharpType = "int";
                break;
            case DbKind.Pgsql when normalizedDbType is "bigint" or "int8":
                csharpType = "long";
                break;
            case DbKind.Pgsql when normalizedDbType is "real" or "float4":
                csharpType = "float";
                break;
            case DbKind.Pgsql when normalizedDbType is "double precision" or "float8":
                csharpType = "double";
                break;
            case DbKind.Pgsql when normalizedDbType is "numeric" or "decimal":
                csharpType = "decimal";
                break;
            case DbKind.Pgsql when normalizedDbType is "boolean" or "bool":
                csharpType = "bool";
                break;
            case DbKind.Pgsql when normalizedDbType is "date":
                csharpType = "DateTime";
                break;
            case DbKind.Pgsql
                when normalizedDbType is "time" or "time without time zone" or "timetz" or "time with time zone":
                csharpType = "TimeSpan";
                break;
            case DbKind.Pgsql when normalizedDbType is "timestamp" or "timestamp without time zone":
                csharpType = "DateTime";
                break;
            case DbKind.Pgsql when normalizedDbType is "timestamptz" or "timestamp with time zone":
                csharpType = useDateTimeOffset ? "DateTimeOffset" : "DateTime";
                break;
            case DbKind.Pgsql
                when normalizedDbType is "text" or "varchar" or "char" or "character varying" or "character":
                csharpType = "string";
                break;
            case DbKind.Pgsql when normalizedDbType is "bytea":
                csharpType = "byte[]";
                break;
            case DbKind.Pgsql when normalizedDbType is "uuid":
                csharpType = "Guid";
                break;
            case DbKind.Pgsql:
            {
                if (normalizedDbType is "json" or "jsonb") csharpType = "string";
                break;
            }
            case DbKind.Mssql when normalizedDbType is "smallint":
                csharpType = "short";
                break;
            case DbKind.Mssql when normalizedDbType is "int":
                csharpType = "int";
                break;
            case DbKind.Mssql when normalizedDbType is "bigint":
                csharpType = "long";
                break;
            case DbKind.Mssql when normalizedDbType is "real":
                csharpType = "float";
                break;
            case DbKind.Mssql when normalizedDbType is "float":
                csharpType = "double";
                break;
            case DbKind.Mssql when normalizedDbType is "numeric" or "decimal" or "money" or "smallmoney":
                csharpType = "decimal";
                break;
            case DbKind.Mssql when normalizedDbType is "bit":
                csharpType = "bool";
                break;
            case DbKind.Mssql when normalizedDbType is "date" or "datetime" or "smalldatetime" or "datetime2":
                csharpType = "DateTime";
                break;
            case DbKind.Mssql when normalizedDbType is "datetimeoffset":
                csharpType = useDateTimeOffset ? "DateTimeOffset" : "DateTime";
                break;
            case DbKind.Mssql when normalizedDbType is "time":
                csharpType = "TimeSpan";
                break;
            case DbKind.Mssql
                when normalizedDbType is "nvarchar" or "varchar" or "nchar" or "char" or "text" or "ntext":
                csharpType = "string";
                break;
            case DbKind.Mssql when normalizedDbType is "varbinary" or "binary" or "image":
                csharpType = "byte[]";
                break;
            case DbKind.Mssql:
            {
                if (normalizedDbType is "uniqueidentifier") csharpType = "Guid";
                break;
            }
            case DbKind.Sqlite when normalizedDbType.Contains("int"):
                csharpType = "long";
                break;
            case DbKind.Sqlite when normalizedDbType.Contains("char") || normalizedDbType.Contains("text") ||
                                    normalizedDbType.Contains("clob"):
                csharpType = "string";
                break;
            case DbKind.Sqlite when normalizedDbType.Contains("blob"):
                csharpType = "byte[]";
                break;
            case DbKind.Sqlite when normalizedDbType.Contains("real") || normalizedDbType.Contains("floa") ||
                                    normalizedDbType.Contains("doub"):
                csharpType = "double";
                break;
            case DbKind.Sqlite when normalizedDbType.Contains("numeric") || normalizedDbType.Contains("decim"):
                csharpType = "decimal";
                break;
            case DbKind.Sqlite when normalizedDbType.Contains("bool"):
                csharpType = "bool";
                break;
            case DbKind.Sqlite:
            {
                if (normalizedDbType.Contains("date") || normalizedDbType.Contains("time")) csharpType = "DateTime";
                break;
            }
            case DbKind.Unknown:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dbKind), dbKind, null);
        }

        // 對於參考型別(string、byte[])保持原樣；其他型別一律標記為可空
        if (csharpType is "string" or "byte[]")
            return csharpType;

        // 一律回傳可空值型別（忽略 isNullable）
        return csharpType + "?";
    }
}