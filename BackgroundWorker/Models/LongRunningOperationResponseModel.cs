using Models;

namespace BackgroundWorker.Models
{
    public sealed class LongRunningOperationResponseModel(LongRunningOperationRequestModel request, IEnumerable<dynamic> queryResult)
    {
        public LongRunningOperationRequestModel Request => request ?? throw new ArgumentNullException(nameof(request));
        public IEnumerable<dynamic> QueryResult => queryResult ?? throw new ArgumentNullException(nameof(queryResult));
    }
}
