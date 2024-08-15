using Microsoft.AspNetCore.Mvc;

namespace BackgroundWorker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigurationController(ILogger<ConfigurationController> logger) : ControllerBase
    {
        private readonly ILogger<ConfigurationController> _logger = logger;

        [HttpGet("GetConfiguration")]
        public Task<string?> GetEnvironmentVariable(string environmentVariable)
        {
            _logger.LogInformation($"Getting env variable {environmentVariable} value..");

            var task = Task<string?>.FromResult(Environment.GetEnvironmentVariable(environmentVariable));

            _logger.LogInformation($"Env variable {environmentVariable} value has been shown.");

            return task;
        }
    }
}
