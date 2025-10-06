namespace SerGenX;

public sealed record ColumnDefinition(
    string Name,
    string DbType,
    bool IsNullable,
    int? Length,
    int? Precision,
    int? Scale);