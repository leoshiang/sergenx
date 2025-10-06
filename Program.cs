namespace SerGenX;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cliArguments = SimpleArgs.Parse(args);

        if (cliArguments.Has("--run-tests"))
            return Tests.Run();

        var currentWorkingDirectory = Directory.GetCurrentDirectory();

        // 1) RootNamespace
        var rootNamespace = await CsprojReader.ReadRootNamespaceAsync(currentWorkingDirectory);
        if (string.IsNullOrWhiteSpace(rootNamespace))
        {
            await Console.Error.WriteLineAsync("找不到 .csproj 或 RootNamespace 設定。");
            return 1;
        }

        // 2) 讀取連線設定
        var dataConnections =
            await AppSettingsReader.ReadDataConnectionsAsync(Path.Combine(currentWorkingDirectory, "appsettings.json"));

        var connectionName = cliArguments.Get("--connection") ?? cliArguments.Get("-c");
        var connectionString = cliArguments.Get("--connection-string");
        var cliDbType = cliArguments.Get("--db-type") ?? cliArguments.Get("-t");
        var tablesArgument = cliArguments.Get("--tables") ?? cliArguments.Get("-T");
        var outputDirectory = cliArguments.Get("--output-dir") ?? cliArguments.Get("-o");
        var overwriteFiles = cliArguments.Has("--overwrite");
        var useDateTimeOffset = cliArguments.Has("--use-datetime-offset");

        var effectiveConnectionString = connectionString;
        var effectiveConnectionName = connectionName;

        // 互動模式：未指定 connection / connection-string
        if (string.IsNullOrWhiteSpace(effectiveConnectionString) && string.IsNullOrWhiteSpace(effectiveConnectionName))
        {
            (effectiveConnectionName, effectiveConnectionString) = Interactive.SelectConnection(dataConnections);
            if (effectiveConnectionString is null)
            {
                await Console.Error.WriteLineAsync("未選擇連線，結束。");
                return 2;
            }
        }
        else
        {
            // 非互動：connection-string 優先
            if (string.IsNullOrWhiteSpace(effectiveConnectionString) &&
                !string.IsNullOrWhiteSpace(effectiveConnectionName))
            {
                if (!dataConnections.TryGetValue(effectiveConnectionName, out var resolvedConnStr))
                {
                    await Console.Error.WriteLineAsync(
                        $"在 appsettings.json 的 Data 節點找不到連線名稱：{effectiveConnectionName}");
                    return 3;
                }

                effectiveConnectionString = resolvedConnStr;
            }
            else if (!string.IsNullOrWhiteSpace(effectiveConnectionString) &&
                     string.IsNullOrWhiteSpace(effectiveConnectionName))
            {
                effectiveConnectionName = "Default";
            }
        }

        // 3) 判斷 DB 類型
        var databaseKind = DbTypeDetector.Detect(cliDbType, effectiveConnectionString!);
        if (databaseKind == DbKind.Unknown)
        {
            await Console.Error.WriteLineAsync("無法判斷資料庫類型，請以 --db-type 指定：pgsql | mssql | sqlite");
            return 4;
        }

        // 4) 取得【選取的資料表列表】
        List<(string Schema, string Table)> selectedTables;
        var dbExplorer = DbExplorerFactory.Create(databaseKind, effectiveConnectionString!);

        if (string.IsNullOrWhiteSpace(tablesArgument))
        {
            var translatableTables = await dbExplorer.ListDistinctTablesAsync();
            selectedTables = Interactive.SelectTables(translatableTables, databaseKind);
            if (selectedTables.Count == 0)
            {
                Console.WriteLine("未選擇任何資料表，結束。");
                return 0;
            }
        }
        else
        {
            selectedTables = CliParsers.ParseTablesArgument(tablesArgument, databaseKind);
        }

        // 5) 產檔
        var generator = new CodeGenerator(new CodeGenOptions
        {
            RootNamespace = rootNamespace,
            ConnectionKeyName = effectiveConnectionName!,
            OutputRoot = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.Combine(currentWorkingDirectory, "Modules")
                : Path.GetFullPath(Path.Combine(currentWorkingDirectory, outputDirectory)),
            Overwrite = overwriteFiles,
            UseDateTimeOffset = useDateTimeOffset,
            DbKind = databaseKind
        });

        var warnings = new List<string>();

        foreach (var (schema, table) in selectedTables)
        {
            var tableTranslation = await dbExplorer.GetTableTranslationAsync(schema, table);
            if (tableTranslation is null)
            {
                warnings.Add($"跳過：翻譯表缺少定義 -> {schema}.{table}");
                continue;
            }

            var columnTranslations = await dbExplorer.GetColumnTranslationsAsync(schema, table);
            var tableDefinition = await dbExplorer.GetTableDefinitionAsync(schema, table);

            var missingColumns = tableDefinition.Columns
                .Where(column => columnTranslations.All(ct =>
                    !ct.OriginalColumn.Equals(column.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(column => column.Name)
                .ToList();
            if (missingColumns.Count > 0)
                warnings.Add($"表 {schema}.{table} 欄位翻譯缺失：{string.Join(",", missingColumns)}（已跳過未翻欄位）");

            await generator.GenerateAllFilesForTableAsync(tableTranslation, columnTranslations, tableDefinition);
        }

        if (warnings.Count > 0)
        {
            Console.WriteLine("警告摘要：");
            foreach (var warning in warnings) Console.WriteLine($"- {warning}");
        }

        Console.WriteLine("完成。");
        return 0;
    }
}