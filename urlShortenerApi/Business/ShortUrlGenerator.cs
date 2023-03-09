public class ShortUrlGenerator
{
    private readonly DatabaseSessionFactory _sessionFactory;
    private readonly RandomGenerator _randomGenerator;
    public ShortUrlGenerator(DatabaseSessionFactory sessionFactory, RandomGenerator randomGenerator)
    {
        _sessionFactory = sessionFactory;
        _randomGenerator = randomGenerator;
    }

    public virtual string GenerateUrl()
    {
        do
        {
            var shortUrl = Convert.ToBase64String(_randomGenerator.GenerateBytes(9));

            var session = _sessionFactory.Create();

            var rs = session.Execute($"SELECT * FROM redirect_urls WHERE shorturl = '{shortUrl}'");
            
            if (!rs.Any())
            {
                return shortUrl;
            }
        }
        while (true);
    }
}