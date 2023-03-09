public class RandomGeneratorMock : RandomGenerator
{
    private int count = 0;
    public override byte[] GenerateBytes(int count)
    {
        if (count == 0)
        {
            count++;
            return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        }
        else
        {
            return new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        }
    }
}