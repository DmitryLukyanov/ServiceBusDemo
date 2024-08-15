using BackgroundWorker.Data;

namespace BackgroundWorker.Repositories
{
    public interface IHistoryRepository
    {
        Task CreateHistoryRecordAsync(IEnumerable<HistoryModel> histories, CancellationToken cancellationToken = default);
    }

    public class HistoryRepository(CoreDbContext _context) : IHistoryRepository
    {
        public async Task CreateHistoryRecordAsync(IEnumerable<HistoryModel> histories, CancellationToken cancellationToken = default)
        {
            await _context.History.AddRangeAsync(histories, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
