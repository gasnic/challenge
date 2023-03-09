using Meli.Function;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Cassandra;
using System.Net;
using Microsoft.Extensions.Options;

public class RedirectUrlTests
{

    public void Initialize(Mock<ISession>? session = null)
    {
        // Logger
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

        // Database
        var sMock = new Mock<BoundStatement>();

        IEnumerator<Row> rows()
        {
            yield return new Row();
        }

        var rowsMock = new Mock<RowSet>();
        rowsMock.Setup(
            r => r.GetEnumerator()
        ).Returns(() => rows());

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute(It.IsAny<string>())
        ).Returns(() =>
        {
            return rowsMock.Object;
        });

        IOptions<DatabaseConnectionConfigurations> options = Options.Create(new DatabaseConnectionConfigurations());

        var databaseSessionFactoryMock = new Mock<DatabaseSessionFactory>(options);
        databaseSessionFactoryMock.Setup(x => x.Create()).Returns(() => session is null ? sessionMock.Object : session.Object);

        // DI
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = new Mock<FunctionContext>();
        context.SetupProperty(c => c.InstanceServices, serviceProvider);

        this.context = context.Object;

        this.function = new RedirectUrl(mockLoggerFactory.Object, databaseSessionFactoryMock.Object);
    }

    private FunctionContext? context;
    private RedirectUrl? function;


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task InvalidUrl(string shortUrl)
    {
        Initialize();
        await ExecuteTest(shortUrl, HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal("ShortUrl is required", bodyResponse);
        });
    }

    [Theory]
    [InlineData("invalid")]
    public async Task UrlDoesNotExists(string shortUrl)
    {
        IEnumerator<Row> rows()
        {
            List<RowMock> arr = new List<RowMock>();
            return arr.GetEnumerator();
        }

        var rowsMock = new Mock<RowSet>();
        rowsMock.Setup(
            r => r.GetEnumerator()
        ).Returns(() => rows());

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute(It.IsAny<string>())
        ).Returns(() =>
        {
            return rowsMock.Object;
        });

        Initialize(sessionMock);
        await ExecuteTest(shortUrl, HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal("Url does not exists", bodyResponse);
        });
    }

    [Theory]
    [InlineData("12345")]
    public async Task UrlExists(string shortUrl)
    {
        var expectedUrl = "https://google.com/";

        var rowMock = new Mock<Row>();
        rowMock.Setup(r => r.GetValue<string>(It.IsAny<string>()))
            .Returns(() => expectedUrl);

        IEnumerator<Row> rows()
        {
            List<Row> arr = new List<Row>();
            arr.Add(rowMock.Object);
            return arr.GetEnumerator();
        }

        var rowsMock = new Mock<RowSet>();
        rowsMock.Setup(
            r => r.GetEnumerator()
        ).Returns(() => rows());

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute(It.IsAny<string>())
        ).Returns(() =>
        {
            return rowsMock.Object;
        });

        Initialize(sessionMock);
        await ExecuteTest(shortUrl, HttpStatusCode.Redirect, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(result.Headers.GetValues("Location").ElementAt(0), expectedUrl);
        });
    }

    [Theory]
    [InlineData("null")]
    [InlineData("empty")]
    public async Task UrlExistsAndIsNullOrEmpty(string shortUrl)
    {
        var rowMock = new Mock<Row>();
        rowMock
            .Setup(r =>
                r.GetValue<string>("null"))
            .Returns(() => null);
        rowMock
           .Setup(r =>
               r.GetValue<string>("empty"))
           .Returns(() => "");

        IEnumerator<Row> rows()
        {
            List<Row> arr = new List<Row>();
            arr.Add(rowMock.Object);
            return arr.GetEnumerator();
        }

        var rowsMock = new Mock<RowSet>();
        rowsMock.Setup(
            r => r.GetEnumerator()
        ).Returns(() => rows());

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute(It.IsAny<string>())
        ).Returns(() =>
        {
            return rowsMock.Object;
        });

        Initialize(sessionMock);
        await ExecuteTest(shortUrl, HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal("Url does not exists", bodyResponse);
        });
    }

    [Theory]
    [InlineData("12345")]
    public async Task ExistsTwoUrlsForSameShortUrl(string shortUrl)
    {
        var expectedUrl = "https://google.com/";
        var noExpectedUrl = "https://noExpected.com/";

        var rowMock = new Mock<Row>();
        rowMock.Setup(r => r.GetValue<string>(It.IsAny<string>()))
            .Returns(() => expectedUrl);

        var otherRowMock = new Mock<Row>();
        otherRowMock.Setup(r => r.GetValue<string>(It.IsAny<string>()))
            .Returns(() => noExpectedUrl);

        IEnumerator<Row> rows()
        {
            List<Row> arr = new List<Row>();
            arr.Add(otherRowMock.Object);
            arr.Add(rowMock.Object);
            return arr.GetEnumerator();
        }

        var rowsMock = new Mock<RowSet>();
        rowsMock.Setup(
            r => r.GetEnumerator()
        ).Returns(() => rows());

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(
            s => s.Execute(It.IsAny<string>())
        ).Returns(() =>
        {
            return rowsMock.Object;
        });

        Initialize(sessionMock);
        await ExecuteTest(shortUrl, HttpStatusCode.Redirect, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(result.Headers.GetValues("Location").ElementAt(0), expectedUrl);
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

