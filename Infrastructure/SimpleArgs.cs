namespace SerGenX;

public static class SimpleArgs
{
    public static Dictionary<string, string?> Parse(string[] args)
    {
        var arguments = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var currentArg = args[i];
            if (!currentArg.StartsWith("--") && !currentArg.StartsWith("-")) continue;

            string? value = null;
            if (i + 1 < args.Length && !(args[i + 1].StartsWith("--") || args[i + 1].StartsWith("-")))
            {
                value = args[i + 1];
                i++;
            }

            arguments[currentArg] = value;
        }

        return arguments;
    }

    public static string? Get(this Dictionary<string, string?> arguments, string key) =>
        arguments.GetValueOrDefault(key);

    public static bool Has(this Dictionary<string, string?> arguments, string key) =>
        arguments.ContainsKey(key);
}