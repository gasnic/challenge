public static class Helper
{
    public static async Task<string> GetBodyResponse(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        string requestBody = String.Empty;
        using (StreamReader streamReader = new StreamReader(stream))
        {
            requestBody = await streamReader.ReadToEndAsync();
        }
        return requestBody;
    }
}