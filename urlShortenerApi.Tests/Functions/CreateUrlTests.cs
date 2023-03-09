using Meli.Function;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Cassandra;
using System.Net;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

public class CreateUrlTests
{

    public CreateUrlTests()
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

        var randomGenerator = new Mock<RandomGenerator>();

        var shortUrlGeneratorMock = new Mock<ShortUrlGenerator>(databaseSessionFactoryMock.Object, randomGenerator.Object);
        shortUrlGeneratorMock.Setup(x => x.GenerateUrl()).Returns(() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(9)));

        this.function = new CreateUrl(mockLoggerFactory.Object, databaseSessionFactoryMock.Object, shortUrlGeneratorMock.Object);
    }

    private readonly FunctionContext context;
    private readonly CreateUrl function;


    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("http://www.google.com")]
    [InlineData("https://www.google.com/")]
    [InlineData("https://www.google")]
    [InlineData("https://google.com")]
    [InlineData("https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-6-0")]
    [InlineData("https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json?param=how-to?pivots=dotnet-6-0")]
    public async Task ValidUrl(string url)
    {
        var body = new
        {
            Url = url
        };

        await ExecuteTest(JsonSerializer.Serialize(body), HttpStatusCode.OK, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(bodyResponse, "");
        });
    }

    [Theory]
    [InlineData("https:/www.google.com?Lorem ipsum dolor sit amet, consectetur adipiscing elit.")]
    [InlineData("https:/www.google.com")]
    [InlineData("google")]
    [InlineData("")]
    [InlineData(null)]
    public async Task InvalidUrl(string url)
    {
        var body = new
        {
            Url = url
        };

        await ExecuteTest(JsonSerializer.Serialize(body), HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(bodyResponse, $"{url} is not a valid Url");
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task UrlIsNullOrEmpty(string url)
    {
        var body = new
        {
            Url = url
        };

        await ExecuteTest(JsonSerializer.Serialize(body), HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(bodyResponse, $"Url is required");
        });
    }

    [Fact]
    public async Task UrlIsTooLarge()
    {
        var body = new
        {
            Url = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In a scelerisque metus, sollicitudin sodales ex. Mauris tellus enim, finibus vitae iaculis euismod, sagittis at tortor. Ut eleifend felis sit amet semper efficitur. Nulla a tristique tortor. Aliquam vestibulum feugiat posuere. Quisque sed dapibus erat. Nunc vestibulum turpis venenatis massa hendrerit pharetra Donec ornare massa eu sapien viverra, vel cursus lacus tincidunt. Nulla ut tellus venenatis, lobortis urna sed, pulvinar arcu. Ut rutrum bibendum est quis tristique. Duis fringilla elementum erat, dictum tristique nisl molestie suscipit. Quisque lobortis felis efficitur, gravida turpis at, mollis erat. Nullam malesuada ex et urna tristique, a viverra metus sagittis. In congue hendrerit nibh, sit amet pulvinar odio Praesent non condimentum sapien. Mauris id dolor eu elit pretium mollis vitae non felis. Donec velit lectus, imperdiet sit amet venenatis sit amet, auctor eget quam. Maecenas a lectus leo. Suspendisse id eros odio. Sed purus mauris, tempus eu vehicula vitae, efficitur eget augue. Aenean gravida, eros ut rhoncus volutpat, velit risus consequat mauris, a dapibus sapien dolor et odio Suspendisse porta sollicitudin dapibus. Sed auctor velit at rhoncus aliquam. Phasellus erat nibh, maximus eu ex vel, accumsan tempor justo. Aliquam ac leo lobortis, interdum odio a, dignissim ligula. Praesent suscipit auctor odio quis tristique. Phasellus convallis velit vitae sapien semper volutpat. Morbi pellentesque ligula non nisi euismod, eu blandit nunc tristique. Nam eleifend a ipsum vitae aliquet. Etiam sit amet tellus ut dolor accumsan vehicula. Proin eget ex consectetur ex fringilla bibendum id a nulla. Curabitur facilisis, sapien quis feugiat faucibus, sapien ante pharetra nunc, at efficitur nisl tellus quis lectus. Suspendisse potenti. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas Morbi viverra erat non mauris molestie, vel rhoncus ante tincidunt. Mauris vitae massa posuere, euismod magna in, finibus ligula asdf"
        };

        await ExecuteTest(JsonSerializer.Serialize(body), HttpStatusCode.BadRequest, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(bodyResponse, $"Url can not be greater than 2000 caracters");
        });
    }

    public async Task DatabaseAccessError()
    {
        await ExecuteTest("{ \"Url\": \"https://www.google.com\" }", HttpStatusCode.InternalServerError, async (result) =>
        {
            var bodyResponse = await Helper.GetBodyResponse(result.Body);
            Assert.IsType<string>(bodyResponse);
            Assert.NotNull(bodyResponse);
            Assert.Equal(bodyResponse, "");
        });
    }

    private async Task ExecuteTest(string bodyRequest, HttpStatusCode expectedCode, Action<HttpResponseData> assertions)
    {
        var byteArray = Encoding.ASCII.GetBytes(bodyRequest);
        var bodyStream = new MemoryStream(byteArray);

        var request = new Mock<HttpRequestData>(context);
        request.Setup(r => r.Body).Returns(bodyStream);
        request.Setup(r => r.CreateResponse()).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context);
            response.SetupProperty(r => r.Headers, new HttpHeadersCollection());
            response.SetupProperty(r => r.StatusCode);
            response.SetupProperty(r => r.Body, new MemoryStream());
            return response.Object;
        });

        var result = await function.Run(request.Object);

        Assert.Equal(result.StatusCode, expectedCode);
        assertions(result);
    }
}