using Microsoft.AspNetCore.Mvc;

namespace UI.Controllers
{
    [Route("configuration")]
    public class ConfigurationController(IConfiguration configuration) : Controller
    {
        [HttpGet("get/{environmentVariable}")]
        public IActionResult Get(string environmentVariable)
        {
            var envResult = (Environment.GetEnvironmentVariable(environmentVariable) ?? "not found");
            var configurationResult = configuration.GetValue<string>(environmentVariable) ?? "not configured";
            return Ok(string.Concat(envResult, "#", configurationResult));
        }
    }
}
