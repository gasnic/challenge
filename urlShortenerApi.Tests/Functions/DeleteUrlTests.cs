using Meli.Function;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Cassandra;
using System.Net;
using Microsoft.Extensions.Options;

public class DeleteUrlTests
{

    public DeleteUrlTests()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(
            m => m.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()));

        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);

        var sMock = new Mock<BoundStatement>();

        var statementMock = new Mock<PreparedStatement>();
        statementMock.Setup(s => s.Bind(It.IsAny<string>(), It.IsAny<string>())).Returns(() => sMock.Object);

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Prepare(It.IsAny<string>())
        ).Returns(() => statementMock.Object);

        IOptions<DatabaseConnectionConfigurations> options = Options.Create(new DatabaseConnectionConfigurations());

        var databaseSessionFactoryMock = new Mock<DatabaseSessionFactory>(options);
        databaseSessionFactoryMock.Setup(x => x.Create()).Returns(() => sessionMock.Object);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        this.context = context.Object;

        this.function = new DeleteUrl(mockLoggerFactory.Object, databaseSessionFactoryMock.Object);
    }

    private readonly FunctionContext context;
    private readonly DeleteUrl function;

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task NullOrEmptyShortUrl(string shortUrl)
    {
        await ExecuteTest(shortUrl, HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(bodyResponse, $"ShortUrl is required");
        });
    }

    private async Task ExecuteTest(string shortUrl, HttpStatusCode expectedCode, Action<HttpResponseData> assertions)
    {
        var request = new Mock<HttpRequestData>(context);
        request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        var result = await function.Run(request.Object, shortUrl);

        Assert.Equal(result.StatusCode, expectedCode);
        assertions(result);
    }
}