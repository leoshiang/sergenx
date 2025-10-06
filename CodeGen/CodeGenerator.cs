namespace SerGenX;

public sealed class CodeGenerator(CodeGenOptions options)
{
    public async Task GenerateAllFilesForTableAsync(TableTranslation table, List<ColumnTranslation> columns,
        TableDefinition tableDefinition)
    {
        var translatedColumns = columns
            .Where(columnTranslation => tableDefinition.Columns.Any(definitionColumn =>
                definitionColumn.Name.Equals(columnTranslation.OriginalColumn, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var moduleDirectory = Path.Combine(options.OutputRoot, table.ModuleName, table.CSharpClassName);
        Directory.CreateDirectory(moduleDirectory);

        await RoslynCodeWriter.WriteRowEntityAsync(moduleDirectory, options, table, translatedColumns, tableDefinition);
        await RoslynCodeWriter.WriteColumnDefinitionsAsync(moduleDirectory, options, table, translatedColumns, tableDefinition);
        await RoslynCodeWriter.WriteFormDefinitionAsync(moduleDirectory, options, table, translatedColumns, tableDefinition);
        await RoslynCodeWriter.WriteApiEndpointAsync(moduleDirectory, options, table);
        await RoslynCodeWriter.WritePageControllerAsync(moduleDirectory, options, table);

        // RequestHandlers
        var requestHandlersDirectory = Path.Combine(moduleDirectory, "RequestHandlers");
        Directory.CreateDirectory(requestHandlersDirectory);
        await RoslynCodeWriter.WriteSaveHandlerAsync(requestHandlersDirectory, options, table);
        await RoslynCodeWriter.WriteDeleteHandlerAsync(requestHandlersDirectory, options, table);
        await RoslynCodeWriter.WriteRetrieveHandlerAsync(requestHandlersDirectory, options, table);
        await RoslynCodeWriter.WriteListHandlerAsync(requestHandlersDirectory, options, table);

        // TypeScript files (Page.tsx, Dialog.tsx, Grid.tsx)
        await RoslynCodeWriter.WriteTypeScriptFilesAsync(moduleDirectory, options, table);
    }
}