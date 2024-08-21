using BackgroundWorker.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackgroundWorker.Repositories
{
    public interface ILongRunningOperationRepository
    {
        Task<IEnumerable<dynamic>> GetLongRunningOperationResultAsync(
            string query,
            bool longRunning = false,
            CancellationToken cancellationToken = default);
    }

    public class LongRunningOperationRepository(CoreDbSettings backgroundWorkerSettings) : ILongRunningOperationRepository
    {
        private readonly CoreDbSettings _backgroundWorkerSettings = backgroundWorkerSettings;

        public async Task<IEnumerable<dynamic>> GetLongRunningOperationResultAsync(
            string query,
            bool longRunning = false,
            CancellationToken cancellationToken = default)
        {
            // NOTE: use Ado.NET since EF supports arbitrary query,
            // but doesn't support arbitrary return type

            await using var sqlConnection = new SqlConnection(_backgroundWorkerSettings.CoreConnectionString);
            await using var sqlCommand = new SqlCommand(query, sqlConnection);
            await sqlConnection.OpenAsync(cancellationToken);
            using var sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            var dataTable = new DataTable(tableName: "Result");
            sqlDataAdapter.Fill(dataTable);
            if (longRunning)
            {
                var randomDelay = new Random().Next(5, 15);
                await Task.Delay(TimeSpan.FromSeconds(randomDelay), cancellationToken); // emulate long running call
            }

            return Enumerable.Concat<dynamic>(
                new[] { dataTable.Columns.OfType<DataColumn>().Select(i => i.ColumnName).ToArray() },
                dataTable
                    .Rows
                    .OfType<DataRow>()
                    .Select(i => i.ItemArray)
                    .ToArray());
        }
    }

    //public class LongRunningOperationRepository(CoreDbContext _context) : ILongRunningOperationRepository
    //{
    //    public async Task<IEnumerable<dynamic>> GetLongRunningOperationResultAsync(
    //        string query,
    //        bool longRunning = false,
    //        CancellationToken cancellationToken = default)
    //    {
    //        // TODO: check main application, if it's not possible to use a single return type, then
    //        // use a different approach, for example ADO.NET or Dapper

    //        var formattableString = FormattableStringFactory.Create(query);
    //        var sqlResult = await _context.Database.SqlQuery<TemporaryContainer>(formattableString).ToListAsync(cancellationToken);

    //        if (longRunning)
    //        {
    //            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // emulate long running call
    //        }
    //        return sqlResult;
    //    }

    //    /// <summary>
    //    /// TODO: validate whether this case will work for application. Must be removed eventially
    //    /// </summary>
    //    public class TemporaryContainer
    //    {
    //        public string? Value { get; set; }
    //    }
    //}
}
