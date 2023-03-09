namespace Meli.Function
{
    class UrlToProcessResponse
    {
        public UrlToProcessResponse(string url)
        {
            Url = url;
        }
        public string Url { get; private set; }
    }
}