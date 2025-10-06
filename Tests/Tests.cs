namespace SerGenX;

public static class Tests
{
    public static int Run()
    {
        var t1 = TypeMapper.MapDbTypeToCSharp("integer", false, DbKind.Pgsql, false);
        if (t1 != "int") return -1;

        var t2 = TypeMapper.MapDbTypeToCSharp("timestamptz", true, DbKind.Pgsql, true);
        if (t2 != "DateTimeOffset?") return -2;

        var t3 = TypeMapper.MapDbTypeToCSharp("bytea", false, DbKind.Pgsql, false);
        if (t3 != "byte[]") return -3;

        Console.WriteLine("TypeMapper tests passed.");
        return 0;
    }
}