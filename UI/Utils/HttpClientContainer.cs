namespace UI.Utils
{
    public sealed class HttpClientContainer(HttpClient httpClient)
    {
        public HttpClient HttpClient => httpClient;
    }
}
