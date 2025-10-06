using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace SerGenX;

public static class RoslynCodeWriter
{
    public static async Task WriteRowEntityAsync(string moduleDir, CodeGenOptions options, TableTranslation table,
        List<ColumnTranslation> columns, TableDefinition definition)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}";
        var className = $"{table.CSharpClassName}Row";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Serenity.ComponentModel"),
                UsingDirective("Serenity.Data"),
                UsingDirective("Serenity.Data.Mapping"));

        var permissionName = table.OriginalTable;

        var attributeLists = new[]
        {
            AttributeList(Attribute("ConnectionKey", StringArg(options.ConnectionKeyName))),
            AttributeList(Attribute("Module", StringArg(table.ModuleName))),
            AttributeList(Attribute("TableName",
                StringArg(BuildTableNameLiteral(options.DbKind, table.SchemaName, table.OriginalTable)))),
            AttributeList(Attribute("DisplayName", StringArg(permissionName))),
            AttributeList(Attribute("InstanceName", StringArg(permissionName))),
            AttributeList(Attribute("GenerateFields")),
            AttributeList(Attribute("ReadPermission", StringArg($"{permissionName}:讀取"))),
            AttributeList(Attribute("ModifyPermission", StringArg($"{permissionName}:修改"))),
            AttributeList(Attribute("ServiceLookupPermission", StringArg($"{permissionName}:查表"))),
            AttributeList(Attribute("LookupScript")),
            AttributeList(Attribute("DataAuditLog")),
        };

        var baseTypes = new List<BaseTypeSyntax>
        {
            SimpleBaseType("IIdRow"),
            SimpleBaseType("INameRow")
        };

        var propertyMembers = BuildRowProperties(columns, definition, options);

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword),
                Token(SyntaxKind.PartialKeyword))
            .AddAttributeLists(attributeLists)
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(baseTypes)))
            .AddMembers(propertyMembers.ToArray());

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(moduleDir, $"{table.CSharpClassName}Row.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteColumnDefinitionsAsync(string moduleDir, CodeGenOptions options,
        TableTranslation table,
        List<ColumnTranslation> columns, TableDefinition definition)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}.Columns";
        var className = $"{table.CSharpClassName}Columns";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(UsingDirective("Serenity.ComponentModel"));

        var attributeLists = new[]
        {
            AttributeList(Attribute("ColumnsScript")),
            AttributeList(Attribute("BasedOnRow",
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(ParseType($"{table.CSharpClassName}Row"))),
                NamedAttributeArgument("CheckNames",
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))))
        };

        var properties = new List<MemberDeclarationSyntax>();
        foreach (var column in columns)
        {
            var property = SyntaxFactory.PropertyDeclaration(ParseType("string"), column.CSharpPropertyName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(AttributeList(Attribute("EditLink")))
                .AddAccessorListAccessors(
                    AutoAccessor(SyntaxKind.GetAccessorDeclaration),
                    AutoAccessor(SyntaxKind.SetAccessorDeclaration));

            properties.Add(property);
        }

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
            .AddAttributeLists(attributeLists)
            .AddMembers(properties.ToArray());

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(moduleDir, $"{table.CSharpClassName}Columns.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteFormDefinitionAsync(string moduleDir, CodeGenOptions options, TableTranslation table,
        List<ColumnTranslation> columns, TableDefinition definition)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}.Forms";
        var className = $"{table.CSharpClassName}Form";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(UsingDirective("Serenity.ComponentModel"));

        var attributeLists = new[]
        {
            AttributeList(Attribute("FormScript", StringArg($"{table.ModuleName}.{table.CSharpClassName}"))),
            AttributeList(Attribute("BasedOnRow",
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(ParseType($"{table.CSharpClassName}Row"))),
                NamedAttributeArgument("CheckNames",
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))))
        };

        var properties = new List<MemberDeclarationSyntax>();
        foreach (var columnTranslation in columns)
        {
            var columnDefinition = definition.Columns.FirstOrDefault(c =>
                c.Name.Equals(columnTranslation.OriginalColumn, StringComparison.OrdinalIgnoreCase));
            if (columnDefinition is null) continue;

            var csharpType = TypeMapper.MapDbTypeToCSharp(columnDefinition.DbType, columnDefinition.IsNullable,
                options.DbKind, options.UseDateTimeOffset);
            var property = SyntaxFactory
                .PropertyDeclaration(ParseType(csharpType), columnTranslation.CSharpPropertyName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AutoAccessor(SyntaxKind.GetAccessorDeclaration),
                    AutoAccessor(SyntaxKind.SetAccessorDeclaration));

            properties.Add(property);
        }

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddAttributeLists(attributeLists)
            .AddMembers(properties.ToArray());

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(moduleDir, $"{table.CSharpClassName}Form.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteApiEndpointAsync(string moduleDir, CodeGenOptions options, TableTranslation table)
    {
        var rootNamespace = options.RootNamespace;
        var moduleNamespace = $"{rootNamespace}.{table.ModuleName}";
        var endpointsNamespace = $"{moduleNamespace}.Endpoints";

        var rowTypeName = $"{table.CSharpClassName}Row";
        var columnsTypeName = $"{table.CSharpClassName}Columns";
        var endpointClassName = $"{table.CSharpClassName}Endpoint";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Microsoft.AspNetCore.Mvc"),
                UsingDirective($"{moduleNamespace}.Columns"),
                UsingDirective("Serenity.Services"),
                UsingDirective("Serenity.Data"),
                UsingDirective("Serenity.Reporting"),
                UsingDirective("System"),
                UsingDirective("System.Data"),
                UsingDirective("System.Globalization"));

        var rowAlias = SyntaxFactory.UsingDirective(ParseName($"{moduleNamespace}.{rowTypeName}"))
            .WithAlias(SyntaxFactory.NameEquals("MyRow"));
        compilationUnit = compilationUnit.AddUsings(rowAlias);

        var classAttributes = new[]
        {
            AttributeList(
                Attribute("Route", StringArg($"Services/{table.ModuleName}/{table.CSharpClassName}/[action]"))),
            AttributeList(Attribute("ConnectionKey",
                SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(ParseType("MyRow"))))),
            AttributeList(Attribute("ServiceAuthorize",
                SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(ParseType("MyRow")))))
        };

        var createMethod = SyntaxFactory.ParseMemberDeclaration(
            $$"""
              [HttpPost]
              [AuthorizeCreate(typeof(MyRow))]
              public SaveResponse Create(IUnitOfWork uow, SaveRequest<MyRow> request,
                  [FromServices] I{{table.CSharpClassName}}SaveHandler handler)
              {
                  return handler.Create(uow, request);
              }

              """)!;

        var updateMethod = SyntaxFactory.ParseMemberDeclaration(
            $$"""

              [HttpPost]
              [AuthorizeUpdate(typeof(MyRow))]
              public SaveResponse Update(IUnitOfWork uow, SaveRequest<MyRow> request,
                  [FromServices] I{{table.CSharpClassName}}SaveHandler handler)
              {
                  return handler.Update(uow, request);
              }

              """)!;

        var deleteMethod = SyntaxFactory.ParseMemberDeclaration(
            $$"""

              [HttpPost]
              [AuthorizeDelete(typeof(MyRow))]
              public DeleteResponse Delete(IUnitOfWork uow, DeleteRequest request,
                  [FromServices] I{{table.CSharpClassName}}DeleteHandler handler)
              {
                  return handler.Delete(uow, request);
              }

              """)!;

        var retrieveMethod = SyntaxFactory.ParseMemberDeclaration(
            $$"""

              [HttpPost]
              public RetrieveResponse<MyRow> Retrieve(IDbConnection connection, RetrieveRequest request,
                  [FromServices] I{{table.CSharpClassName}}RetrieveHandler handler)
              {
                  return handler.Retrieve(connection, request);
              }

              """)!;

        var listMethod = SyntaxFactory.ParseMemberDeclaration($$"""

                                                                [HttpPost]
                                                                [AuthorizeList(typeof(MyRow))]
                                                                public ListResponse<MyRow> List(IDbConnection connection, ListRequest request,
                                                                    [FromServices] I{{table.CSharpClassName}}ListHandler handler)
                                                                {
                                                                    return handler.List(connection, request);
                                                                }

                                                                """)!;

        var listExcelMethod = SyntaxFactory.ParseMemberDeclaration(
            $$"""

              [HttpPost]
              [AuthorizeList(typeof(MyRow))]
              public FileContentResult ListExcel(IDbConnection connection, ListRequest request,
                  [FromServices] I{{table.CSharpClassName}}ListHandler handler,
                  [FromServices] IExcelExporter exporter)
              {
                  var data = List(connection, request, handler).Entities;
                  var bytes = exporter.Export(data, typeof({{columnsTypeName}}), request.ExportColumns);
                  return ExcelContentResult.Create(bytes, "{{table.CSharpClassName}}List_" +
                      DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".xlsx");
              }

              """)!;

        var endpointClass = SyntaxFactory.ClassDeclaration(endpointClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(ParseType("ServiceEndpoint")))
            .AddAttributeLists(classAttributes)
            .AddMembers(createMethod, updateMethod, deleteMethod, retrieveMethod, listMethod, listExcelMethod);

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(endpointsNamespace))
            .AddMembers(endpointClass);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(moduleDir, $"{table.CSharpClassName}Endpoint.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WritePageControllerAsync(string moduleDir, CodeGenOptions options, TableTranslation table)
    {
        var pagesNamespace = $"{options.RootNamespace}.{table.ModuleName}.Pages";
        var rowTypeName = $"{table.CSharpClassName}Row";
        var pageClassName = $"{table.CSharpClassName}Page";
        var routePath = $"{table.ModuleName}/{table.CSharpClassName}";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Microsoft.AspNetCore.Mvc"),
                UsingDirective("Serenity.Web"),
                UsingDirective($"{options.RootNamespace}.{table.ModuleName}"));

        var classAttributes = new[]
        {
            AttributeList(Attribute("PageAuthorize",
                SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(ParseType(rowTypeName)))))
        };

        var classDeclaration = SyntaxFactory.ClassDeclaration(pageClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(ParseType("Controller")))
            .AddAttributeLists(classAttributes);

        var indexMethod = SyntaxFactory.ParseMemberDeclaration($$"""

                                                                 [Route("{{routePath}}")]
                                                                 public ActionResult Index()
                                                                 {
                                                                     return this.GridPage<{{rowTypeName}}>("@/{{table.ModuleName}}/{{table.CSharpClassName}}/{{table.CSharpClassName}}Page");
                                                                 }

                                                                 """)!;

        classDeclaration = classDeclaration.AddMembers(indexMethod);

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(pagesNamespace))
            .AddMembers(classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(moduleDir, $"{table.CSharpClassName}Page.cs");
        await WriteFileAsync(filePath, code, overwrite: options.Overwrite);
    }

    public static async Task WriteSaveHandlerAsync(string handlerDirectory, CodeGenOptions options,
        TableTranslation table)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}";
        var rowFullName = $"{namespaceName}.{table.CSharpClassName}Row";
        var interfaceName = $"I{table.CSharpClassName}SaveHandler";
        var className = $"{table.CSharpClassName}SaveHandler";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Serenity.Services"),
                UsingDirective("Serenity.Data"))
            .AddUsings(AliasUsing("MyRequest", $"Serenity.Services.SaveRequest<{rowFullName}>"))
            .AddUsings(AliasUsing("MyResponse", "Serenity.Services.SaveResponse"))
            .AddUsings(AliasUsing("MyRow", rowFullName));

        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SimpleBaseType("ISaveHandler<MyRow, MyRequest, MyResponse>"));

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SimpleBaseType("SaveRequestHandler<MyRow, MyRequest, MyResponse>"),
                SimpleBaseType(interfaceName))
            .AddMembers(
                SyntaxFactory.ConstructorDeclaration(className)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                            .WithType(ParseType("IRequestContext")))
                    .WithInitializer(SyntaxFactory.ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("context"))))))
                    .WithBody(SyntaxFactory.Block()));

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(interfaceDeclaration, classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(handlerDirectory, $"{table.CSharpClassName}SaveHandler.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteDeleteHandlerAsync(string handlerDirectory, CodeGenOptions options,
        TableTranslation table)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}";
        var rowFullName = $"{namespaceName}.{table.CSharpClassName}Row";
        var interfaceName = $"I{table.CSharpClassName}DeleteHandler";
        var className = $"{table.CSharpClassName}DeleteHandler";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Serenity.Services"),
                UsingDirective("Serenity.Data"))
            .AddUsings(AliasUsing("MyRequest", "Serenity.Services.DeleteRequest"))
            .AddUsings(AliasUsing("MyResponse", "Serenity.Services.DeleteResponse"))
            .AddUsings(AliasUsing("MyRow", rowFullName));

        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SimpleBaseType("IDeleteHandler<MyRow, MyRequest, MyResponse>"));

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SimpleBaseType("DeleteRequestHandler<MyRow, MyRequest, MyResponse>"),
                SimpleBaseType(interfaceName))
            .AddMembers(
                SyntaxFactory.ConstructorDeclaration(className)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                            .WithType(ParseType("IRequestContext")))
                    .WithInitializer(SyntaxFactory.ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("context"))))))
                    .WithBody(SyntaxFactory.Block()));

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(interfaceDeclaration, classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(handlerDirectory, $"{table.CSharpClassName}DeleteHandler.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteRetrieveHandlerAsync(string handlerDirectory, CodeGenOptions options,
        TableTranslation table)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}";
        var rowFullName = $"{namespaceName}.{table.CSharpClassName}Row";
        var interfaceName = $"I{table.CSharpClassName}RetrieveHandler";
        var className = $"{table.CSharpClassName}RetrieveHandler";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Serenity.Services"),
                UsingDirective("Serenity.Data"))
            .AddUsings(AliasUsing("MyRequest", "Serenity.Services.RetrieveRequest"))
            .AddUsings(AliasUsing("MyResponse", $"Serenity.Services.RetrieveResponse<{rowFullName}>"))
            .AddUsings(AliasUsing("MyRow", rowFullName));

        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SimpleBaseType("IRetrieveHandler<MyRow, MyRequest, MyResponse>"));

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SimpleBaseType("RetrieveRequestHandler<MyRow, MyRequest, MyResponse>"),
                SimpleBaseType(interfaceName))
            .AddMembers(
                SyntaxFactory.ConstructorDeclaration(className)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                            .WithType(ParseType("IRequestContext")))
                    .WithInitializer(SyntaxFactory.ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("context"))))))
                    .WithBody(SyntaxFactory.Block()));

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(interfaceDeclaration, classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(handlerDirectory, $"{table.CSharpClassName}RetrieveHandler.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteListHandlerAsync(string handlerDirectory, CodeGenOptions options,
        TableTranslation table)
    {
        var namespaceName = $"{options.RootNamespace}.{table.ModuleName}";
        var rowFullName = $"{namespaceName}.{table.CSharpClassName}Row";
        var interfaceName = $"I{table.CSharpClassName}ListHandler";
        var className = $"{table.CSharpClassName}ListHandler";

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                UsingDirective("Serenity.Services"),
                UsingDirective("Serenity.Data"))
            .AddUsings(AliasUsing("MyRequest", "Serenity.Services.ListRequest"))
            .AddUsings(AliasUsing("MyResponse", $"Serenity.Services.ListResponse<{rowFullName}>"))
            .AddUsings(AliasUsing("MyRow", rowFullName));

        var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SimpleBaseType("IListHandler<MyRow, MyRequest, MyResponse>"));

        var classDeclaration = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(
                SimpleBaseType("ListRequestHandler<MyRow, MyRequest, MyResponse>"),
                SimpleBaseType(interfaceName))
            .AddMembers(
                SyntaxFactory.ConstructorDeclaration(className)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("context"))
                            .WithType(ParseType("IRequestContext")))
                    .WithInitializer(SyntaxFactory.ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("context"))))))
                    .WithBody(SyntaxFactory.Block()),
                SyntaxFactory.MethodDeclaration(ParseType("void"), "ApplyFilters")
                    .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("query")).WithType(ParseType("SqlQuery")))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement("base.ApplyFilters(query);")))
            );

        var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(ParseName(namespaceName))
            .AddMembers(interfaceDeclaration, classDeclaration);

        compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);
        var code = await FormatAsync(compilationUnit);
        var filePath = Path.Combine(handlerDirectory, $"{table.CSharpClassName}ListHandler.cs");
        await WriteFileAsync(filePath, code, options.Overwrite);
    }

    public static async Task WriteTypeScriptFilesAsync(string classDirectory, CodeGenOptions options,
        TableTranslation table)
    {
        var className = table.CSharpClassName;
        var moduleNamespaceTs = $"{options.RootNamespace}.{table.ModuleName}";

        var pageTypeScript =
            $$"""
              import { gridPageInit } from '@serenity-is/corelib';
              import { {{className}}Grid } from './{{className}}Grid';

              export default () => gridPageInit({{className}}Grid);
              """;
        await WriteTextFileAsync(Path.Combine(classDirectory, $"{className}Page.tsx"), pageTypeScript,
            options.Overwrite);

        var dialogTypeScript =
            $$"""
              import { EntityDialog } from '@serenity-is/corelib';
              import { {{className}}Form, {{className}}Row, {{className}}Service } from '../../ServerTypes/{{table.ModuleName}}';

              export class {{className}}Dialog extends EntityDialog<{{className}}Row, any> {
                  static [Symbol.typeInfo] = this.registerClass("{{moduleNamespaceTs}}.");

                  protected getFormKey() { return {{className}}Form.formKey; }
                  protected getRowDefinition() { return {{className}}Row; }
                  protected getService() { return {{className}}Service.baseUrl; }

                  protected form = new {{className}}Form(this.idPrefix);
              }
              """;
        await WriteTextFileAsync(Path.Combine(classDirectory, $"{className}Dialog.tsx"), dialogTypeScript,
            options.Overwrite);

        var gridTypeScript =
            $$"""
              import { EntityGrid } from '@serenity-is/corelib';
              import { {{className}}Columns, {{className}}Row, {{className}}Service } from '../../ServerTypes/{{table.ModuleName}}';
              import { {{className}}Dialog } from './{{className}}Dialog';

              export class {{className}}Grid extends EntityGrid<{{className}}Row> {
                  static [Symbol.typeInfo] = this.registerClass("{{moduleNamespaceTs}}.");

                  protected getColumnsKey() { return {{className}}Columns.columnsKey; }
                  protected getDialogType() { return {{className}}Dialog; }
                  protected getRowDefinition() { return {{className}}Row; }
                  protected getService() { return {{className}}Service.baseUrl; }
              }
              """;
        await WriteTextFileAsync(Path.Combine(classDirectory, $"{className}Grid.tsx"), gridTypeScript,
            options.Overwrite);
    }

    // helpers
    private static string BuildTableNameLiteral(DbKind databaseKind, string schemaName, string tableName) =>
        databaseKind switch
        {
            DbKind.Pgsql => $"\"{schemaName.ToLowerInvariant()}\".\"{tableName.ToLowerInvariant()}\"",
            DbKind.Mssql => $"[{schemaName}].[{tableName}]",
            DbKind.Sqlite => $"[main].[{tableName}]",
            _ => $"[{schemaName}].[{tableName}]"
        };

    private static List<MemberDeclarationSyntax> BuildRowProperties(
        List<ColumnTranslation> columns, TableDefinition definition, CodeGenOptions options)
    {
        var properties = new List<MemberDeclarationSyntax>();

        foreach (var columnTranslation in columns)
        {
            var columnDefinition = definition.Columns.FirstOrDefault(c =>
                c.Name.Equals(columnTranslation.OriginalColumn, StringComparison.OrdinalIgnoreCase));
            if (columnDefinition is null) continue;

            var csharpType = TypeMapper.MapDbTypeToCSharp(columnDefinition.DbType, columnDefinition.IsNullable,
                options.DbKind, options.UseDateTimeOffset);
            var isPrimaryKey =
                definition.PrimaryKeys.Any(k => k.Equals(columnDefinition.Name, StringComparison.OrdinalIgnoreCase));
            var isIdentity = definition.IdentityColumns.Contains(columnDefinition.Name);
            var isCompositeKey = definition.PrimaryKeys.Count > 1;

            var attributeLists = new List<AttributeListSyntax>
            {
                AttributeList(Attribute("DisplayName", StringArg(columnTranslation.OriginalColumn))),
                AttributeList(Attribute("Column", StringArg(columnTranslation.OriginalColumn)))
            };
            switch (isCompositeKey)
            {
                case true when isPrimaryKey:
                    attributeLists.Add(AttributeList(Attribute("PrimaryKey")));
                    break;
                case false when isPrimaryKey:
                    attributeLists.Add(AttributeList(Attribute("IdProperty")));
                    attributeLists.Add(AttributeList(Attribute("NameProperty")));
                    break;
            }

            if (isIdentity) attributeLists.Add(AttributeList(Attribute("Identity")));

            // 產生 fields 委派樣式的屬性
            var property = SyntaxFactory
                .PropertyDeclaration(ParseType(csharpType), columnTranslation.CSharpPropertyName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(attributeLists.ToArray())
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List([
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithExpressionBody(
                                    SyntaxFactory.ArrowExpressionClause(
                                        SyntaxFactory.ElementAccessExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("fields"),
                                                    SyntaxFactory.IdentifierName(columnTranslation.CSharpPropertyName)))
                                            .WithArgumentList(
                                                SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(SyntaxFactory.ThisExpression()))))))
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithExpressionBody(
                                    SyntaxFactory.ArrowExpressionClause(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.ElementAccessExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName("fields"),
                                                        SyntaxFactory.IdentifierName(columnTranslation
                                                            .CSharpPropertyName)))
                                                .WithArgumentList(
                                                    SyntaxFactory.BracketedArgumentList(
                                                        SyntaxFactory.SingletonSeparatedList(
                                                            SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))),
                                            SyntaxFactory.IdentifierName("value"))))
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                        ])));

            properties.Add(property);
        }

        return properties;
    }

    private static UsingDirectiveSyntax UsingDirective(string name) =>
        SyntaxFactory.UsingDirective(ParseName(name));

    private static UsingDirectiveSyntax AliasUsing(string alias, string typeName) =>
        SyntaxFactory.UsingDirective(ParseName(typeName)).WithAlias(SyntaxFactory.NameEquals(alias));

    private static AttributeListSyntax AttributeList(params AttributeSyntax[] attributes) =>
        SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(attributes));

    private static AttributeSyntax Attribute(string name, params AttributeArgumentSyntax[] args) =>
        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(name),
            args.Length == 0 ? null : SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(args)));

    private static AttributeArgumentSyntax StringArg(string value) =>
        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(value)));

    private static AttributeArgumentSyntax NamedAttributeArgument(string name, ExpressionSyntax expr) =>
        SyntaxFactory.AttributeArgument(expr).WithNameEquals(SyntaxFactory.NameEquals(name));

    private static TypeSyntax ParseType(string typeName) =>
        SyntaxFactory.ParseTypeName(typeName);

    private static NameSyntax ParseName(string name) =>
        SyntaxFactory.ParseName(name);

    private static SyntaxToken Token(SyntaxKind kind) =>
        SyntaxFactory.Token(kind);

    private static AccessorDeclarationSyntax AutoAccessor(SyntaxKind kind) =>
        SyntaxFactory.AccessorDeclaration(kind).WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static BaseTypeSyntax SimpleBaseType(string type) =>
        SyntaxFactory.SimpleBaseType(ParseType(type));

    private static Task<string> FormatAsync(CompilationUnitSyntax compilationUnit)
    {
        using var workspace = new AdhocWorkspace();
        var formatted = Formatter.Format(compilationUnit, workspace);
        return Task.FromResult(formatted.ToFullString());
    }

    private static async Task WriteFileAsync(string path, string content, bool overwrite)
    {
        if (File.Exists(path) && !overwrite) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }

    private static async Task WriteTextFileAsync(string path, string content, bool overwrite)
    {
        if (File.Exists(path) && !overwrite) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }
}