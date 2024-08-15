using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public interface IHistoryRepository
    {
        public Task<IEnumerable<HistoryModel>> GetHistoryAsync();
    }

    public class HistoryRepository(CoreDbContext _context) : IHistoryRepository
    {
        public async Task<IEnumerable<HistoryModel>> GetHistoryAsync()
        {
            var result = await _context.History.ToListAsync();
            return result;
        }
    }
}
