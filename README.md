# SerGenX

SerGenX 是一個以 .NET 開發的自動化程式碼產生器，根據資料庫與翻譯對照表，產生後端 C# 類別（Row、Columns、Form、Endpoint、RequestHandlers）以及前端 TypeScript（Page.tsx、Dialog.tsx、Grid.tsx），協助快速建立模組化的 CRUD 介面。

## 功能特色

- 依據翻譯對照表自動產生：
  - C# 類別：Row、Columns、Form、Endpoint
  - RequestHandlers：Save/Delete/Retrieve/List
  - TypeScript：Page、Dialog、Grid
- 互動模式選擇連線與資料表（支援翻頁）
- 支援多資料庫類型：
  - PostgreSQL
  - Microsoft SQL Server
  - SQLite
- 型別對應（Type Mapping）與 DateTimeOffset 選項
- 可指定輸出目錄與覆寫行為
- 透過 Roslyn 組合語法樹輸出格式化的 C# 程式碼

## 安裝需求

- .NET 9.0 SDK
- 可存取的資料庫連線
- 專案根目錄需有 appsettings.json，包含 Data 區段的連線設定

## 快速開始

1) 在專案根目錄建立或確認 appsettings.json，包含資料庫連線：
```json 
{ "Data": { "Default": "Host=localhost;Port=5432;Database=app;Username=app;Password=pass" } }
```

2) 在專案根目錄執行：
```bash
dotnet run -- --db-type pgsql --tables core.Users,core.Roles --overwrite
``` 

3) 互動模式（不提供連線參數時）：
- 啟動後會讓你選擇連線名稱與資料表（支援 ↑/↓、空白鍵勾選、PgUp/PgDn 翻頁、Enter 確認）。

## 指令參數

- --connection, -c           指定 appsettings.json Data 節點中的連線名稱
- --connection-string        直接提供連線字串（優先於 --connection）
- --db-type, -t              指定資料庫類型：pgsql | mssql | sqlite（若不提供，會從連線字串判斷）
- --tables, -T               指定要產生的資料表，使用逗號分隔，支援 schema.table 或 table（未指定時進入互動選單）
- --output-dir, -o           指定輸出目錄（預設為 ./Modules）
- --overwrite                允許覆寫已存在檔案
- --use-datetime-offset      針對含時區時間欄位使用 DateTimeOffset
- --run-tests                執行內建測試流程

範例：
```bash
dotnet run --
--connection Default
--db-type pgsql
--tables core.Users,core.Roles
--output-dir Generated
--overwrite
--use-datetime-offset
```

## 互動選單操作

- 連線選單：↑/↓ 移動、PgUp/PgDn 翻頁、數字跳轉、Enter 確認
- 資料表選單：↑/↓ 移動、空白鍵 勾選/取消、PgUp/PgDn 翻頁、Enter 完成

## 翻譯表定義

SerGenX 根據以下兩張資料表決定要產生哪些實體與對應命名。以下為 PostgreSQL 建議結構：
```sql
create table core.表格翻譯對照表
(
    表格翻譯對照表id serial
        constraint pk_表格翻譯對照表
            primary key,
    綱要名稱         text not null,
    原始表名         text not null,
    翻譯後表名       text not null,
    類別名稱         text not null,
    模組名稱         text,
    constraint unq_表格翻譯對照表
        unique (綱要名稱, 原始表名)
);

create table core.表格欄位翻譯對照表
(
    表格欄位翻譯對照表id serial
        constraint pk_表格欄位翻譯對照表
            primary key,
    綱要名稱             text not null,
    表格名稱             text not null,
    原始欄位名           text not null,
    翻譯後欄位名         text not null,
    屬性名稱             text not null,
    constraint unq_表格欄位翻譯對照表
        unique (綱要名稱, 表格名稱, 原始欄位名)
);
``` 

- 表格翻譯對照表：決定模組名稱、類別名稱（C# Class）、輸出檔名等
- 表格欄位翻譯對照表：決定欄位的中文翻譯與對應的 C# 屬性名稱（Property）

## 產出內容

- 後端
  - {Module}/{ClassName}/{ClassName}Row.cs
  - {Module}/{ClassName}/{ClassName}Columns.cs
  - {Module}/{ClassName}/{ClassName}Form.cs
  - {Module}/{ClassName}/Endpoints/{ClassName}Endpoint.cs
  - {Module}/{ClassName}/RequestHandlers/{ClassName}{Save|Delete|Retrieve|List}Handler.cs
- 前端
  - {Module}/{ClassName}/{ClassName}Page.tsx
  - {Module}/{ClassName}/{ClassName}Dialog.tsx
  - {Module}/{ClassName}/{ClassName}Grid.tsx

## 命名與型別對應

- 依據翻譯對照表自動映射欄位至 C# 屬性名稱
- 型別對應涵蓋 PgSQL/MSSQL/SQLite 常用型別
- 可選擇將含時區時間映射為 DateTimeOffset

## 錯誤與警告

- 當資料表在翻譯表中無定義時會跳過並顯示警告摘要
- 欄位翻譯缺失會列出未翻欄位（不阻斷產生流程）

## 開發建議

- 新增資料庫支援：實作對應的 DbExplorer
- 調整輸出模板：修改 Roslyn 產生器與 TypeScript 模版
- 依需擴充權限屬性與額外註解

## 授權

MIT License
