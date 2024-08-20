using API.Data;
using Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public interface IHistoryRepository
    {
        public Task<IEnumerable<HistoryModel>> GetHistoryAsync(DateTime from);
    }

    public class HistoryRepository(CoreDbContext _context) : IHistoryRepository
    {
        public async Task<IEnumerable<HistoryModel>> GetHistoryAsync(DateTime from)
        {
            var result = await _context.History.Where(i => i.Created >= from).ToListAsync();
            return result;
        }
    }
}
