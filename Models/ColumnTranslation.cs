namespace SerGenX;

public sealed record ColumnTranslation(
    string SchemaName,
    string TableName,
    string OriginalColumn,
    string TranslatedColumn,
    string CSharpPropertyName);