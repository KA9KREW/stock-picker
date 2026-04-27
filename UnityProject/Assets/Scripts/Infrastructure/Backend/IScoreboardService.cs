using System.Threading;
using System.Threading.Tasks;

namespace StockPicker.Infrastructure.Backend
{
    public interface IScoreboardService
    {
        Task<ScoreSubmissionResult> SubmitSeasonScoreAsync(SeasonScoreSubmission submission, CancellationToken cancellationToken = default);
        Task<ScoreboardFetchResult> FetchTopAsync(int count, CancellationToken cancellationToken = default);
    }
}
