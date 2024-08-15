using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Diagnostics;
using System.Text.Json;
using UI.Models;
using UI.Utils;

namespace UI.Controllers
{
    public class HomeController(HttpClientContainer httpClientContainer, ILogger<HomeController> logger) : Controller
    {
        //private const string GetHistoryUri = "History/GetHistory";
        //private const string SendRequestUri = "ServiceBus/GenerateMessages";
        
        private readonly HttpClientContainer _httpClientContainer = httpClientContainer;
        private readonly ILogger<HomeController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            //IEnumerable<HistoryModel> histories;
            //try
            //{
            //    var response = await _httpClientContainer.HttpClient.GetAsync(GetHistoryUri);
            //    response.EnsureSuccessStatusCode();
            //    var stream = await response.Content.ReadAsStreamAsync();
            //    var serializeOptions = new JsonSerializerOptions
            //    {
            //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            //    };
            //    histories = (await JsonSerializer.DeserializeAsync<IEnumerable<HistoryModel>>(stream, serializeOptions))!;
            //}
            //catch (HttpRequestException ex) // TODO: move to middleware
            //{
            //    _logger.LogError(ex, "Getting history failed");
            //    throw;
            //}

            return View(Enumerable.Empty<HistoryModel>());
        }

        public IActionResult SendRequest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string query)
        {
        //    try
        //    {
        //        var content = new [] { query };
        //        var response = await _httpClientContainer.HttpClient.PostAsJsonAsync(SendRequestUri, content);
        //        response.EnsureSuccessStatusCode();
        //    }
        //    catch (HttpRequestException ex) // TODO: move to middleware
        //    {
        //        _logger.LogError(ex, "Getting history failed");
        //        throw;
        //    }

        //    ViewBag.Message = "Sent successfully";
            return View();
        }

        public async Task<IActionResult> OpenFile(string url)
        {
            try
            {
                var response = await _httpClientContainer.HttpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = (await response.Content.ReadFromJsonAsync<IEnumerable<object[]>>())!;
                var preparedArray = ConvertArray(content.ToArray());
                return View(preparedArray);
            }
            catch (HttpRequestException ex) // TODO: move to middleware
            {
                _logger.LogError(ex, "Getting url content failed");
                throw;
            }

            T[,] ConvertArray<T>(T[][] array)
            {
                T[,] result = new T[array.Length, array[0].Length];
                for (int i = 0; i < array.Length; i++)
                {
                    T[] row = array[i];
                    for (int j = 0; j < array[i].Length; j++)
                    {
                        if (row[j] is not JsonElement je || je.ValueKind != JsonValueKind.Null && je.ToString() != "{}")
                        {
                            // remote "{}" empty or null values
                            result[i, j] = row[j];
                        }
                    }
                }
                return result;
            }
        }

        [HttpGet]
        public PartialViewResult Popup()
        {
            return PartialView();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            return View(
                new ErrorViewModel 
                { 
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = exceptionHandlerPathFeature?.Error?.Message
                });
        }
    }
}
