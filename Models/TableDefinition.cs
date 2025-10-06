namespace SerGenX;

public sealed record TableDefinition(
    string Schema,
    string Name,
    List<ColumnDefinition> Columns,
    List<string> PrimaryKeys,
    HashSet<string> IdentityColumns);