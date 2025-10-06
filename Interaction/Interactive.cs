namespace SerGenX;

public static class Interactive
{
    public static (string? Name, string? ConnStr) SelectConnection(Dictionary<string, string> dataConnections)
    {
        if (dataConnections.Count == 0)
        {
            Console.WriteLine("appsettings.json 未找到 Data 區段或連線設定。");
            return (null, null);
        }

        Console.WriteLine("請選擇連線名稱（↑/↓ 移動，PgUp/PgDn 翻頁，Enter 確認）：");
        var connectionNames = dataConnections.Keys.OrderBy(name => name).ToList();
        var cursorPosition = 0;

        static ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

        // 顯示清單的起始行位置
        var listStartLine = Console.CursorTop;

        // 計算每頁可顯示的行數（保留 2 行提示空間）
        int PageSize()
        {
            var usable = Math.Max(1, Console.WindowHeight - 4);
            return usable;
        }

        var pageIndex = 0;
        int pageSize;
        int pageCount;

        void DrawList()
        {
            pageSize = PageSize();
            pageCount = Math.Max(1, (int)Math.Ceiling(connectionNames.Count / (double)pageSize));
            pageIndex = Math.Clamp(pageIndex, 0, pageCount - 1);

            var start = pageIndex * pageSize;
            var count = Math.Min(pageSize, connectionNames.Count - start);

            EnsureBufferForRows(count + 2);

            // 清空可視區域
            for (var i = 0; i < pageSize + 2; i++)
            {
                Console.SetCursorPosition(0, listStartLine + i);
                Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - 1)));
            }

            for (var i = 0; i < count; i++)
            {
                var globalIndex = start + i;
                Console.SetCursorPosition(0, listStartLine + i);
                var selectionPrefix = globalIndex == cursorPosition ? "> " : "  ";
                var displayLine = $"{selectionPrefix}[{globalIndex + 1}] {connectionNames[globalIndex]}";
                if (globalIndex == cursorPosition)
                {
                    var previousForegroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(displayLine);
                    Console.ForegroundColor = previousForegroundColor;
                }
                else
                {
                    Console.Write(displayLine);
                }
            }

            // 頁腳提示
            var footerTop = listStartLine + count;
            Console.SetCursorPosition(0, footerTop);
            Console.Write($"頁面 {pageIndex + 1}/{pageCount}，↑/↓ 移動，PgUp/PgDn 翻頁，數字跳轉，Enter 確認");
        }

        // 初次繪製
        DrawList();

        while (true)
        {
            var pressedKey = ReadKey();
            if (pressedKey.Key == ConsoleKey.UpArrow)
            {
                cursorPosition = (cursorPosition - 1 + connectionNames.Count) % connectionNames.Count;
                var topOfPage = pageIndex * pageSize;
                if (cursorPosition < topOfPage) pageIndex = Math.Max(0, pageIndex - 1);
                DrawList();
            }
            else if (pressedKey.Key == ConsoleKey.DownArrow)
            {
                cursorPosition = (cursorPosition + 1) % connectionNames.Count;
                var topOfPage = pageIndex * pageSize;
                var bottomOfPage = topOfPage + pageSize - 1;
                if (cursorPosition > bottomOfPage) pageIndex = Math.Min(pageCount - 1, pageIndex + 1);
                DrawList();
            }
            else if (pressedKey.Key == ConsoleKey.PageUp)
            {
                pageIndex = Math.Max(0, pageIndex - 1);
                cursorPosition = Math.Max(0, Math.Min(cursorPosition, connectionNames.Count - 1));
                DrawList();
            }
            else if (pressedKey.Key == ConsoleKey.PageDown)
            {
                pageIndex = Math.Min(pageCount - 1, pageIndex + 1);
                cursorPosition = Math.Max(0, Math.Min(cursorPosition, connectionNames.Count - 1));
                DrawList();
            }
            else if (pressedKey.Key == ConsoleKey.Enter)
            {
                break;
            }
            else if (pressedKey.KeyChar is >= '1' and <= '9')
            {
                var numberInput = pressedKey.KeyChar - '0';
                if (numberInput >= 1 && numberInput <= connectionNames.Count)
                {
                    cursorPosition = numberInput - 1;
                    pageIndex = cursorPosition / pageSize;
                    DrawList();
                }
            }
            else if (pressedKey.Key == ConsoleKey.Home)
            {
                cursorPosition = 0;
                pageIndex = 0;
                DrawList();
            }
            else if (pressedKey.Key == ConsoleKey.End)
            {
                cursorPosition = connectionNames.Count - 1;
                pageIndex = Math.Max(0, pageCount - 1);
                DrawList();
            }
        }

        Console.CursorVisible = true;
        var finalCursorLine = listStartLine + Math.Min(pageSize, connectionNames.Count - pageIndex * pageSize) + 1;
        if (Console.BufferHeight <= finalCursorLine) Console.BufferHeight = finalCursorLine + 1;
        Console.SetCursorPosition(0, finalCursorLine);

        var selectedConnectionName = connectionNames[cursorPosition];
        return (selectedConnectionName, dataConnections[selectedConnectionName]);

        void EnsureBufferForRows(int rows)
        {
            var requiredBufferHeight = listStartLine + rows + 2;
            if (Console.BufferHeight < requiredBufferHeight)
                Console.BufferHeight = requiredBufferHeight;
        }
    }

    public static List<(string Schema, string Table)> SelectTables(List<(string Schema, string Table)> tables,
        DbKind databaseKind)
    {
        Console.WriteLine("請以上下鍵移動，空白鍵勾選，PgUp/PgDn 翻頁，Enter 完成選擇：");
        var sortedTables = tables.OrderBy(t => t.Schema).ThenBy(t => t.Table).ToList();
        var selectedIndices = new HashSet<int>();
        var cursorPosition = 0;

        var listStartLine = Console.CursorTop;

        int PageSize()
        {
            var usable = Math.Max(1, Console.WindowHeight - 5);
            return usable;
        }

        var pageIndex = 0;
        int pageSize;
        int pageCount;

        void EnsureBufferForRows(int requiredRowsBelowStart)
        {
            var requiredBufferHeight = listStartLine + requiredRowsBelowStart + 3;
            if (Console.BufferHeight < requiredBufferHeight)
                Console.BufferHeight = requiredBufferHeight;
        }

        void DrawList()
        {
            pageSize = PageSize();
            pageCount = Math.Max(1, (int)Math.Ceiling(sortedTables.Count / (double)pageSize));
            pageIndex = Math.Clamp(pageIndex, 0, pageCount - 1);

            var start = pageIndex * pageSize;
            var count = Math.Min(pageSize, sortedTables.Count - start);

            EnsureBufferForRows(count + 2);

            // 清空可視區域
            for (var i = 0; i < pageSize + 3; i++)
            {
                Console.SetCursorPosition(0, listStartLine + i);
                Console.Write(new string(' ', Math.Max(0, Console.WindowWidth - 1)));
            }

            for (var i = 0; i < count; i++)
            {
                var globalIndex = start + i;
                Console.SetCursorPosition(0, listStartLine + i);
                var isSelected = selectedIndices.Contains(globalIndex);
                var checkboxDisplay = isSelected ? "[x]" : "[ ]";
                var cursorPointer = globalIndex == cursorPosition ? ">" : " ";
                var displayText =
                    $"{cursorPointer} {checkboxDisplay} {globalIndex + 1}. {sortedTables[globalIndex].Schema}.{sortedTables[globalIndex].Table}";
                if (globalIndex == cursorPosition)
                {
                    var previousForegroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(displayText);
                    Console.ForegroundColor = previousForegroundColor;
                }
                else
                {
                    Console.Write(displayText);
                }
            }

            // 頁腳提示
            var footerTop = listStartLine + count;
            Console.SetCursorPosition(0, footerTop);
            Console.Write($"頁面 {pageIndex + 1}/{pageCount}，↑/↓ 移動，空白鍵 勾選/取消，PgUp/PgDn 翻頁，Enter 完成");
        }

        DrawList();
        Console.CursorVisible = false;

        while (true)
        {
            var pressedKey = Console.ReadKey(intercept: true);
            switch (pressedKey.Key)
            {
                case ConsoleKey.UpArrow:
                    cursorPosition = (cursorPosition - 1 + sortedTables.Count) % sortedTables.Count;
                    if (cursorPosition < pageIndex * pageSize) pageIndex = Math.Max(0, pageIndex - 1);
                    DrawList();
                    break;
                case ConsoleKey.DownArrow:
                    cursorPosition = (cursorPosition + 1) % sortedTables.Count;
                    if (cursorPosition >= (pageIndex + 1) * pageSize)
                        pageIndex = Math.Min(pageCount - 1, pageIndex + 1);
                    DrawList();
                    break;
                case ConsoleKey.PageUp:
                    pageIndex = Math.Max(0, pageIndex - 1);
                    DrawList();
                    break;
                case ConsoleKey.PageDown:
                    pageIndex = Math.Min(pageCount - 1, pageIndex + 1);
                    DrawList();
                    break;
                case ConsoleKey.Spacebar:
                    if (!selectedIndices.Add(cursorPosition)) selectedIndices.Remove(cursorPosition);
                    DrawList();
                    break;
                case ConsoleKey.Home:
                    cursorPosition = 0;
                    pageIndex = 0;
                    DrawList();
                    break;
                case ConsoleKey.End:
                    cursorPosition = sortedTables.Count - 1;
                    pageIndex = Math.Max(0, pageCount - 1);
                    DrawList();
                    break;
                case ConsoleKey.Enter:
                    Console.CursorVisible = true;
                    var finalCursorLine =
                        listStartLine + Math.Min(pageSize, sortedTables.Count - pageIndex * pageSize) + 2;
                    if (Console.BufferHeight <= finalCursorLine) Console.BufferHeight = finalCursorLine + 1;
                    Console.SetCursorPosition(0, finalCursorLine);
                    return selectedIndices.OrderBy(index => index).Select(index => sortedTables[index]).ToList();
                default:
                    // 支援數字快速跳轉（單位數，絕對索引）
                    if (pressedKey.KeyChar is >= '1' and <= '9')
                    {
                        var numberInput = pressedKey.KeyChar - '0';
                        if (numberInput >= 1 && numberInput <= sortedTables.Count)
                        {
                            cursorPosition = numberInput - 1;
                            pageIndex = cursorPosition / pageSize;
                            DrawList();
                        }
                    }

                    break;
            }
        }
    }
}