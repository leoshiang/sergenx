namespace SerGenX;

public record CodeGenOptions
{
    public required string RootNamespace { get; init; }
    public required string ConnectionKeyName { get; init; }
    public required string OutputRoot { get; init; }
    public required bool Overwrite { get; init; }
    public required bool UseDateTimeOffset { get; init; }
    public required DbKind DbKind { get; init; }
}