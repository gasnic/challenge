using Cassandra;

public class RowSetMock : RowSet
{
    private readonly bool _hasAny;
    private readonly Row? _row;

    public RowSetMock(bool hasAny = false, Row? row = null)
    {
        _hasAny = hasAny;
        _row = row;
    }
    public virtual bool Any()
    {
        return _hasAny;
    }

    public virtual Row? FirstOrDefault()
    {
        return _row;
    }
}