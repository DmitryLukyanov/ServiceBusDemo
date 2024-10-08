﻿using BackgroundWorker.Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace BackgroundWorker.Repositories
{
    public interface IHistoryRepository
    {
        Task CreateHistoryRecordsAsync(IEnumerable<HistoryModel> histories, CancellationToken cancellationToken = default);
        Task UpdateHistoryRecordAsync(Guid id, Uri resultedLink, TimeSpan duration, DateTime completed, CancellationToken cancellationToken = default);
    }

    public class HistoryRepository(CoreDbContext context) : IHistoryRepository
    {
        private readonly CoreDbContext _context = context;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task CreateHistoryRecordsAsync(IEnumerable<HistoryModel> histories, CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //await _context.History.AddRangeAsync(histories, cancellationToken);
            //await _context.SaveChangesAsync(cancellationToken);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task UpdateHistoryRecordAsync(Guid id, Uri resultedLink, TimeSpan duration, DateTime completed, CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //await _context.History
            //    .Where(i => i.Id == id)
            //    .ExecuteUpdateAsync(h =>
            //        h
            //        .SetProperty(i => i.ResultLink, resultedLink)
            //        .SetProperty(i => i.Duration, duration)
            //        .SetProperty(i => i.Completed, completed));
            //await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
