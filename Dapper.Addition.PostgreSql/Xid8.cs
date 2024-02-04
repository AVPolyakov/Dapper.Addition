namespace Dapper.Addition.PostgreSql;

public struct Xid8
{
    public ulong Value { get; }

    public Xid8(ulong value)
    {
        Value = value;
    }
}