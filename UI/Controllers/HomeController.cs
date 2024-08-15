using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Models;

namespace UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index() => 
            View(
                model: new[]
                {
                    (Query: "SELECT 1", Started: DateTime.UtcNow.AddDays(-2), Duration: TimeSpan.FromSeconds(7), ResultedLink: "www.google.com"),
                    (Query: "SELECT 2", Started: DateTime.UtcNow.AddDays(-10), Duration: TimeSpan.FromSeconds(13), ResultedLink: "www.google1.com"),
                    (Query: "SELECT 3", Started: DateTime.UtcNow.AddHours(-22), Duration: TimeSpan.FromSeconds(1), ResultedLink: "www.google2.com")
                });

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
