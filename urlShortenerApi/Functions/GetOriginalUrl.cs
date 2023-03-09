using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Meli.Function
{
    public class GetOriginalUrl
    {
        private readonly ILogger _logger;
        private readonly DatabaseSessionFactory _databaseSessionFactory;

        public GetOriginalUrl(ILoggerFactory loggerFactory, DatabaseSessionFactory databaseSessionFactory)
        {
            _logger = loggerFactory.CreateLogger<RedirectUrl>();
            _databaseSessionFactory = databaseSessionFactory;
        }

        [Function("get_original_url")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "url/{shortUrl}")]
            HttpRequestData req, string shortUrl)
        {
            if (string.IsNullOrEmpty(shortUrl))
            {
                _logger.LogInformation($"ShortUrl is required");

                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteStringAsync($"ShortUrl is required");
                return response;
            }

            var session = _databaseSessionFactory.Create();

            var rs = session.Execute($"SELECT * FROM redirect_urls WHERE shorturl = '{shortUrl}'");

            string originalUrl = "";

            foreach (var row in rs)
            {
                originalUrl = row.GetValue<string>("originalurl");
            }

            if (string.IsNullOrEmpty(originalUrl))
            {
                _logger.LogInformation($"Url does not exists");

                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteStringAsync($"Url does not exists");
                return response;
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            await okResponse.WriteStringAsync(JsonSerializer.Serialize(new { Url = originalUrl }));
            return okResponse;
        }
    }
}
