using System.Text.Json;

namespace SerGenX;

public static class AppSettingsReader
{
    public static async Task<Dictionary<string, string>> ReadDataConnectionsAsync(string appSettingsPath)
    {
        var connections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(appSettingsPath)) return connections;

        await using var fileStream = File.OpenRead(appSettingsPath);
        using var document = await JsonDocument.ParseAsync(fileStream);
        if (!document.RootElement.TryGetProperty("Data", out var dataSection))
            return connections;

        foreach (var property in dataSection.EnumerateObject())
        {
            connections[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString()!,
                JsonValueKind.Object when property.Value.TryGetProperty("ConnectionString",
                                              out var connectionStringElement) &&
                                          connectionStringElement.ValueKind == JsonValueKind.String =>
                    connectionStringElement.GetString()!,
                _ => connections[property.Name]
            };
        }

        return connections;
    }
}