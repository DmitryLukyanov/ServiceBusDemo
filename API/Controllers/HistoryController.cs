using Models;
using API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HistoryController(
        IHistoryRepository historyRepository,
        ILogger<HistoryController> logger) : ControllerBase
    {
        private readonly IHistoryRepository _historyRepository = historyRepository;
        private readonly ILogger<HistoryController> _logger = logger;

        [HttpGet("GetHistory")]
        public async Task<IEnumerable<HistoryModel>> GetHistory(DateTime from)
        {
            _logger.LogInformation("Get history acquiring has been started..");

            var result = await _historyRepository.GetHistoryAsync(from);

            _logger.LogInformation("Get history acquiring has been finished..");
            
            return result;
        }
    }
}
