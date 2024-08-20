namespace UI.Utils
{
    public sealed class HttpClientContainer(HttpClient _httpClient)
    {
        public HttpClient HttpClient => _httpClient;
    }
}
