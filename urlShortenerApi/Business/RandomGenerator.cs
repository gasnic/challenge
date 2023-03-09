using System.Security.Cryptography;

public class RandomGenerator
{
    public virtual byte[] GenerateBytes(int count)
    {
        return RandomNumberGenerator.GetBytes(count);
    }
}