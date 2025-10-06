namespace SerGenX;

public static class CsprojReader
{
    public static async Task<string?> ReadRootNamespaceAsync(string directory)
    {
        var csprojFile = Directory.GetFiles(directory, "*.csproj").FirstOrDefault();
        if (csprojFile is null) return null;

        var fileContent = await File.ReadAllTextAsync(csprojFile);
        var openTagIndex = fileContent.IndexOf("<RootNamespace>", StringComparison.OrdinalIgnoreCase);
        var closeTagIndex = fileContent.IndexOf("</RootNamespace>", StringComparison.OrdinalIgnoreCase);
        if (openTagIndex < 0 || closeTagIndex < 0 || closeTagIndex <= openTagIndex) return null;

        openTagIndex += "<RootNamespace>".Length;
        var rootNamespace = fileContent.Substring(openTagIndex, closeTagIndex - openTagIndex).Trim();
        return string.IsNullOrWhiteSpace(rootNamespace) ? null : rootNamespace;
    }
}