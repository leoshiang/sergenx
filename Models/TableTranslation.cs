namespace SerGenX;

public sealed record TableTranslation(
    string SchemaName,
    string OriginalTable,
    string TranslatedTable,
    string CSharpClassName,
    string ModuleName);