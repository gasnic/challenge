using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Meli.Function
{
    public class RedirectUrl
    {
        private readonly ILogger _logger;
        private readonly DatabaseSessionFactory _databaseSessionFactory;

        public RedirectUrl(ILoggerFactory loggerFactory, DatabaseSessionFactory databaseSessionFactory)
        {
            _logger = loggerFactory.CreateLogger<RedirectUrl>();
            _databaseSessionFactory = databaseSessionFactory;
        }

        [Function("redirect_uri")]
        public Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{shortUrl}")]
            HttpRequestData req, string shortUrl)
        {
            _logger.LogInformation($"Request redirect to {shortUrl}");

            if (string.IsNullOrEmpty(shortUrl))
            {
                _logger.LogInformation($"ShortUrl is required");

                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.WriteString($"ShortUrl is required");
                return Task.FromResult(response);
            }

            var session = _databaseSessionFactory.Create();

            var rs = session.Execute($"SELECT * FROM redirect_urls WHERE shorturl = '{shortUrl}'");

            string redirectUrl = "";

            foreach (var row in rs)
            {
                redirectUrl = row.GetValue<string>("originalurl");
            }

            if (string.IsNullOrEmpty(redirectUrl))
            {
                _logger.LogInformation($"Url does not exists");

                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.WriteString($"Url does not exists");
                return Task.FromResult(response);
            }

            var res = req.CreateResponse(HttpStatusCode.Redirect);
            res.Headers.Add("Location", redirectUrl);
            return Task.FromResult(res);
        }
    }
}
