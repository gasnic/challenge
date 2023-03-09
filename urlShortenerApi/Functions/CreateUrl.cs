using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Cassandra;

namespace Meli.Function
{
    public class CreateUrl
    {
        private const int maxUrlSize = 2000;
        private readonly ILogger _logger;
        private readonly DatabaseSessionFactory _databaseSessionFactory;
        private readonly ShortUrlGenerator _shortUrlGenerator;

        public CreateUrl(ILoggerFactory loggerFactory, DatabaseSessionFactory databaseSessionFactory, ShortUrlGenerator shortUrlGenerator)
        {
            _logger = loggerFactory.CreateLogger<CreateUrl>();
            _databaseSessionFactory = databaseSessionFactory;
            _shortUrlGenerator = shortUrlGenerator;
        }

        [Function("create-url")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "url")] HttpRequestData req)
        {
            var session = _databaseSessionFactory.Create();
            var url = "";

            try
            {
                var data = await GetParsedBodyResponse<UrlToProcessRequest>(req.Body);
                url = data?.Url;
            }
            catch(Exception e)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync($"Url is required");
                return badResponse;
            }
            
            if (string.IsNullOrEmpty(url))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync($"Url is required");
                return badResponse;
            }

            if (url.Length > maxUrlSize)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync($"Url can not be greater than {maxUrlSize} caracters");
                return badResponse;
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync($"{url} is not a valid Url");
                return badResponse;
            }

            var userTrackStmt = session.Prepare("INSERT INTO redirect_urls (originalUrl, shortUrl) VALUES (?, ?)");

            var key = _shortUrlGenerator.GenerateUrl();

            var batch = new BatchStatement()
                .Add(userTrackStmt.Bind(url, key));

            session.Execute(batch);

            var response = string.IsNullOrEmpty(url) ?
                req.CreateResponse(HttpStatusCode.BadRequest) :
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteStringAsync(JsonSerializer.Serialize(new { Url = key }));

            return response;
        }

        private async Task<string> GetBodyResponse(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(stream))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            return requestBody;
        }

        private async Task<T?> GetParsedBodyResponse<T>(Stream stream) where T : class
        {
            var data = await GetBodyResponse(stream);
            return JsonSerializer.Deserialize<T>(data);
        }
    }
}
