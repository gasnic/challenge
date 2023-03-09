using Cassandra;

class RowMock : Row
{
    public override T GetValue<T>(string name)
    {
        return default(T);
    }
}