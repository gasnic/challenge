using System.Net;
using Cassandra;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Meli.Function
{
    public class DeleteUrl
    {
        private readonly ILogger _logger;
        private readonly DatabaseSessionFactory _databaseSessionFactory;

        public DeleteUrl(ILoggerFactory loggerFactory, DatabaseSessionFactory databaseSessionFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteUrl>();
            _databaseSessionFactory = databaseSessionFactory;
        }

        [Function("delete_url")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "url/{shortUrl}")] HttpRequestData req,
            string shortUrl)
        {
            _logger.LogInformation(shortUrl);

            if (string.IsNullOrEmpty(shortUrl))
            {
                _logger.LogInformation($"ShortUrl is required");

                var res = req.CreateResponse(HttpStatusCode.BadRequest);
                res.WriteString($"ShortUrl is required");
                return res;
            }

            var session = _databaseSessionFactory.Create();

            var deleteStatement = session.Prepare($"DELETE FROM redirect_urls WHERE shorturl = ?");

            var batch = new BatchStatement()
                .Add(deleteStatement.Bind(shortUrl));

            session.Execute(batch);

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            return response;
        }
    }
}
