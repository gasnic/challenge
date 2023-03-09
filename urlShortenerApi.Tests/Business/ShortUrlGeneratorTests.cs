using Moq;
using Cassandra;
using Microsoft.Extensions.Options;

public class ShortUrlGeneratorTests
{
    [Fact]
    public void GenerateUrl()
    {
        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute(It.IsAny<string>())
        ).Returns(() =>
        {
            return new RowSetMock();
        });

        IOptions<DatabaseConnectionConfigurations> options = Options.Create(new DatabaseConnectionConfigurations());

        var sessionFactoryMock = new Mock<DatabaseSessionFactory>(options);
        sessionFactoryMock.Setup(x => x.Create()).Returns(() => sessionMock.Object);

        var randomGenerator = new Mock<RandomGenerator>();
        randomGenerator.Setup(r => r.GenerateBytes(It.IsAny<int>())).Returns(() => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        ShortUrlGenerator generator = new ShortUrlGenerator(sessionFactoryMock.Object, randomGenerator.Object);
        var shortUrl = generator.GenerateUrl();

        Assert.Equal(12, shortUrl.Length);
        Assert.Equal("AQIDBAUGBwgJ", shortUrl);
    }

    [Fact]
    public void GenerateUrlIfHasCollision()
    {
        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute($"SELECT * FROM redirect_urls WHERE shorturl = 'AQIDBAUGBwgJ'")
        ).Returns(() =>
        {
            return new RowSetMock(true);
        });

        sessionMock.Setup(
            s => s.Execute($"SELECT * FROM redirect_urls WHERE shorturl = 'CQgHBgUEAwIB'")
        ).Returns(() =>
        {
            return new RowSetMock();
        });

        IOptions<DatabaseConnectionConfigurations> options = Options.Create(new DatabaseConnectionConfigurations());
        var sessionFactoryMock = new Mock<DatabaseSessionFactory>(options);
        sessionFactoryMock.Setup(x => x.Create()).Returns(() => sessionMock.Object);

        ShortUrlGenerator generator = new ShortUrlGenerator(sessionFactoryMock.Object, new RandomGeneratorMock());
        var shortUrl = generator.GenerateUrl();

        Assert.Equal(12, shortUrl.Length);
        Assert.Equal("CQgHBgUEAwIB", shortUrl);
    }
}