using API.Data;
using API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HistoryController(
        IHistoryRepository _historyRepository,
        ILogger<HistoryController> _logger) : ControllerBase
    {
        [HttpGet("GetHistory")]
        public async Task<IEnumerable<HistoryModel>> GetHistory()
        {
            _logger.LogInformation("Get history acquiring has been started..");

            var result = await _historyRepository.GetHistoryAsync();

            _logger.LogInformation("Get history acquiring has been finished..");
            
            return result;
        }
    }
}
